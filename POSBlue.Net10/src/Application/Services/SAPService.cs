using System.Globalization;
using System.Net;
using System.Text;
using Dapper;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Business logic của SAPController — port từ SAPController + ROPVoucherService + IssueVoucherService cũ.
/// Routing: ROP (nếu prefix khớp) → SAP SOAP (fallback) | PLG API | Capillary API | Olala partner.
/// </summary>
public sealed class SAPService : ISAPService
{
    private const int MaxVoucherLength = 20;
    private const int MaxCapillaryVoucherLength = 15;
    private static readonly string[] ValidStatuses = { "SOLD", "RDM", "EXP" };
    private static readonly int[] RopValidCheckStatuses = { 40 }; // InStock = 40

    // Redis key prefixes for SendCodeCpnVch quota
    private const string RedisKeyCpnVch = "CpnVchCodeSend:";
    private const string RedisKeyQuota = "CpnVchCodeSendQuota:";
    private const string RedisKeyRemnQuota = "CpnVchCodeSendRemnQuota:";

    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly IRedisService _redis;
    private readonly IRequestLogger _logger;
    private readonly ISapVoucherClient _sapClient;
    private readonly IRopVoucherService _ropSvc;
    private readonly ISqlConnectionFactory _dbFactory;

    public SAPService(
        IMemoryCacheService cache,
        IApiCallClient apiClient,
        IRedisService redis,
        IRequestLogger logger,
        ISapVoucherClient sapClient,
        IRopVoucherService ropSvc,
        ISqlConnectionFactory dbFactory)
    {
        _cache = cache;
        _apiClient = apiClient;
        _redis = redis;
        _logger = logger;
        _sapClient = sapClient;
        _ropSvc = ropSvc;
        _dbFactory = dbFactory;
    }

    // ── CheckVoucherBySerials ─────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CheckVoucherBySerialsAsync(
        string voucherNumber, string posTerminal, bool isLog)
    {
        var siteNo = StringHelper.Left(posTerminal, 4);
        var ropPrefix = GetRopPrefix();

        if (IsRopConfigAvailable() && _ropSvc.IsRopVoucher(voucherNumber, ropPrefix))
        {
            var (ok, msg, data) = await _ropSvc.CheckVoucherBySerialAsync(voucherNumber, siteNo, posTerminal);
            if (!ok && data?.ActicleType == "ZPOS")
                return (HttpStatusCode.BadRequest, $"{voucherNumber} không phải voucher, vui lòng kiểm tra lại", data);
            return ok
                ? (HttpStatusCode.OK, "Success", data)
                : (HttpStatusCode.BadRequest, msg, data);
        }

        return (HttpStatusCode.BadRequest, "Chưa cấu hình WebApi, vui lòng liên hệ IT", null);
    }

    // ── CheckVoucher ──────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CheckVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal,
        string companyCode, string isEmployee, string partner,
        string isCardVoucher, int quantity, bool isVoucher,
        string phoneNumber, bool isLog)
    {
        // 1. WinX dynamic voucher
        if (IsDynamicVoucher(voucherNumber))
        {
            var resolved = await ResolveWinXVoucherAsync(voucherNumber, posTerminal, phoneNumber);
            if (resolved == null)
                return (HttpStatusCode.BadRequest, $"Không thể xử lý voucher WinX {voucherNumber}", null);
            voucherNumber = resolved;
        }

        // 2. Capillary voucher
        if (IsVoucherCapillary(voucherNumber, partner))
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return (HttpStatusCode.BadRequest, "Vui lòng nhập số điện thoại hội viên WIN", null);
            return await CheckCapillaryCouponAsync(siteNo, posTerminal, phoneNumber, voucherNumber);
        }

        // 3. PLG (Phúc Long Group)
        if (!string.IsNullOrEmpty(companyCode) && companyCode == "PLG")
        {
            var plgStore = GetPlgStoreMapping(siteNo);
            if (plgStore == null)
                return (HttpStatusCode.BadRequest, "Không tìm thấy cửa hàng PHÚC LONG, Vui lòng liên hệ IT để setup", null);

            return isVoucher
                ? await CheckPlgVoucherAsync(voucherNumber, siteNo, posTerminal, partner, isEmployee, isCardVoucher)
                : await CheckPlgCouponAsync(voucherNumber, siteNo, posTerminal, quantity, isEmployee);
        }

        // 4. Olala phone number check
        if (NumberHelper.IsPhoneNumber(voucherNumber))
        {
            return await CheckOlalaByPhoneAsync(voucherNumber, posTerminal);
        }

        // 5. ROP or SAP
        var ropPrefix = GetRopPrefix();
        if (IsRopConfigAvailable() && _ropSvc.IsRopVoucher(voucherNumber, ropPrefix))
        {
            var (ok, msg, data) = await _ropSvc.CheckVoucherAsync(voucherNumber, siteNo, posTerminal);
            if (ok && data?.ActicleType == "ZPOS")
                return (HttpStatusCode.BadRequest, $"{voucherNumber} không phải voucher/coupon, vui lòng kiểm tra lại", data);
            return ok
                ? (HttpStatusCode.OK, "Success", data)
                : (HttpStatusCode.BadRequest, msg, data);
        }

        // SAP SOAP fallback
        if (voucherNumber.Trim().Length > MaxVoucherLength)
            return (HttpStatusCode.BadRequest, $"Mã Voucher/Coupon {voucherNumber} không được lớn hơn {MaxVoucherLength} ký tự", null);

        return await CheckSapVoucherAsync(voucherNumber, siteNo, posTerminal, isLog);
    }

    // ── CreateNewVoucher ──────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CreateNewVoucherAsync(
        List<CreateVoucherModel> model)
    {
        if (model == null || model.Count == 0)
            return (HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
            return (HttpStatusCode.BadRequest, "Voucher/Coupon Number đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.From_Date)))
            return (HttpStatusCode.BadRequest, "From Date đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.Expiry_Date)))
            return (HttpStatusCode.BadRequest, "Expiry_Date đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.SiteCode)))
            return (HttpStatusCode.BadRequest, "Sitecode đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.Article_No)))
            return (HttpStatusCode.BadRequest, "Mã article đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.BonusBuy)))
            return (HttpStatusCode.BadRequest, "BonusBuy đang bị trống", null);
        if (model.Any(x => x.Value <= 0))
            return (HttpStatusCode.BadRequest, "Value không hợp lệ", null);
        if (model.Any(x => x.VoucherNumber.Length > MaxVoucherLength))
            return (HttpStatusCode.BadRequest, $"Mã Voucher/Coupon không được lớn hơn {MaxVoucherLength} ký tự", null);

        var ropPrefix = GetRopPrefix();
        if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), ropPrefix))
            return (HttpStatusCode.BadRequest, "Danh sách voucher/coupon không đồng nhất", null);

        if (IsRopConfigAvailable() && IsUpdateCreateRequestROP(model.Select(x => x.VoucherNumber).FirstOrDefault() ?? "", ropPrefix))
        {
            string voucherType = StringHelper.Right(StringHelper.Left(model[0].VoucherNumber, 7), 1) == "3"
                ? "ZCPN" : "ZVCN";
            var (ok, msg, data) = await _ropSvc.CreateVoucherAsync(model, voucherType, 50); // 50=Actived
            return ok
                ? (HttpStatusCode.OK, "Success", data)
                : (HttpStatusCode.BadRequest, msg ?? "Có lỗi API từ ROP", null);
        }

        // SAP SOAP
        var inputs = model
            .Where(x => !string.IsNullOrEmpty(x.VoucherNumber))
            .Select(x => new SapVoucherInput
            {
                VoucherType = "V",
                ArticleNo = x.Article_No ?? "",
                SerialNo = x.VoucherNumber,
                VoucherValue = x.Value.ToString(),
                VoucherCurrency = "VND",
                ValidityFromDate = x.From_Date,
                ExpiryDate = x.Expiry_Date,
                ProcessingType = "A",
                Status = "A",
                BonusBuy = x.BonusBuy ?? "",
                Site = x.SiteCode,
            }).ToList();

        var resp = await _sapClient.ProcessVouchersAsync(inputs);
        if (resp?.Count > 0)
            return (HttpStatusCode.OK, "Success",
                resp.Select(x => new VoucherStatusResponse { Status = x.Status, Return = x.Return?.Trim() }).ToList());
        return (HttpStatusCode.BadRequest, "Không có phản hồi từ SAP", null);
    }

    // ── UpdateVoucher ─────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UpdateVoucherAsync(
        List<VoucherUpdateRequest> model)
    {
        if (model == null || model.Count == 0)
            return (HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);

        var listCompanyCode = model.GroupBy(d => d.CompanyCode).ToList();
        if (listCompanyCode.Count > 1)
            return (HttpStatusCode.BadRequest, "Vui lòng thanh toán voucher/coupon trên cùng 1 companyCode", null);

        if (model.Any(x => string.IsNullOrEmpty(x.Status)))
            return (HttpStatusCode.BadRequest, "Trạng thái không được trống.", null);
        if (model.Any(x => !ValidStatuses.Contains(x.Status)))
            return (HttpStatusCode.BadRequest, "Trạng thái không hợp lệ.", null);
        if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
            return (HttpStatusCode.BadRequest, "Voucher/Coupon không được trống.", null);
        if (model.Any(x => string.IsNullOrEmpty(x.ArticleNo)))
            return (HttpStatusCode.BadRequest, "ArticleNo không được trống.", null);
        if (model.Any(x => string.IsNullOrEmpty(x.ArticleType)))
            return (HttpStatusCode.BadRequest, "ActicleType không được trống.", null);
        if (model.Any(x => x.VoucherNumber.Length > MaxVoucherLength))
            return (HttpStatusCode.BadRequest, $"Mã Voucher/Coupon không được lớn hơn {MaxVoucherLength} ký tự", null);

        var ropPrefix = GetRopPrefix();
        if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), ropPrefix))
            return (HttpStatusCode.BadRequest, "Danh sách voucher/coupon không đồng nhất", null);

        if (IsRopConfigAvailable() && IsUpdateCreateRequestROP(model[0].VoucherNumber, ropPrefix))
        {
            var updateModel = BuildVoucherUpdateModel(model, model[0].SiteCode, model[0].POSTerminal, model[0].OrderNo ?? "");
            var (ok, msg, data) = await _ropSvc.UpdateVoucherStatusAsync(updateModel);
            return ok
                ? (HttpStatusCode.OK, "Success", data)
                : (HttpStatusCode.BadRequest, msg ?? "Có lỗi API từ ROP", data);
        }

        // SAP SOAP
        return await UpdateSapVouchersAsync(model);
    }

    // ── RedeemCpnVch ─────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> RedeemCpnVchAsync(VoucherUpdateModel model)
    {
        var resultRedeem = new List<VoucherStatusResponse>();
        var resultErrors = new List<string>();
        bool isRedeem = false;
        string voucherMapping = model.ListSeriNo?.FirstOrDefault()?.voucherNumber ?? "";
        int qtyVoucher = model.ListSeriNo?.Count ?? 0;
        int qtyPayment = 0;
        var lstVoucherUsed = new List<string>();

        // 1. Phone number → resolve to actual voucher (Olala flow)
        if (NumberHelper.IsPhoneNumber(voucherMapping))
        {
            if (voucherMapping.Length == 8 && model.ListSeriNo?.FirstOrDefault()?.articleNo == "11111111")
            {
                // Staff discount (DRW) — return OK stub
                return (HttpStatusCode.OK, "OK", null);
            }

            var (okOlala, msgOlala, dataOlala) = await CheckOlalaByPhoneAsync(voucherMapping, model.POSTerminal);
            if (okOlala && dataOlala is VoucherStatusResponse olalaVoucher)
                model.ListSeriNo![0].voucherNumber = olalaVoucher.VoucherNumber ?? voucherMapping;
            else
                return (HttpStatusCode.BadRequest,
                    msgOlala ?? $"Số điện thoại {voucherMapping} không có mã giảm giá", null);
        }

        var ropPrefix = GetRopPrefix();

        // 2. ROP vouchers (length > 15, not Capillary)
        var listROP = model.ListSeriNo?
            .Where(x => x.voucherNumber.Length > MaxCapillaryVoucherLength
                        && IsUpdateCreateRequestROP(x.voucherNumber, ropPrefix))
            .ToList() ?? [];

        if (listROP.Count > 0)
        {
            // Retry check
            if (model.IsRetry)
            {
                var retryData = await _ropSvc.RetryUpdateVoucherStatusAsync(model.OrderNo);
                if (retryData != null)
                {
                    var arrayA = model.ListSeriNo!.Select(x => x.voucherNumber).OrderBy(x => x).ToArray();
                    var arrayB = retryData.Select(x => x.VoucherNumber).OrderBy(x => x).ToArray();
                    if (arrayA.SequenceEqual(arrayB))
                        return (HttpStatusCode.OK, $"Bill {model.OrderNo} retry sử dụng voucher thành công", retryData);
                    return (HttpStatusCode.BadRequest,
                        $"Số lượng voucher retry không khớp với request của bill {model.OrderNo}", null);
                }
            }

            var ropModel = new VoucherUpdateModel
            {
                OrderNo = model.OrderNo,
                SiteCode = model.SiteCode,
                POSTerminal = model.POSTerminal,
                TotalBill = model.TotalBill,
                ListSeriNo = listROP,
            };
            var (okRop, msgRop, dataRop) = await _ropSvc.UpdateVoucherStatusAsync(ropModel);
            if (okRop && dataRop != null)
            {
                isRedeem = true;
                resultRedeem.AddRange(dataRop);
                lstVoucherUsed.AddRange(dataRop.Select(x => x.VoucherNumber ?? ""));
                qtyPayment += dataRop.Count;
            }
            else
            {
                if (dataRop != null) resultErrors.AddRange(dataRop.Select(x => x.VoucherNumber ?? ""));
                return (HttpStatusCode.BadRequest, msgRop ?? "Có lỗi API từ ROP", dataRop);
            }
        }

        // 3. Capillary vouchers
        var listCAP = model.ListSeriNo?
            .Where(x => x.voucherNumber.Length <= MaxCapillaryVoucherLength
                        && IsVoucherCapillary(x.voucherNumber, x.Partner))
            .ToList() ?? [];

        if (listCAP.Count > 0)
        {
            if (string.IsNullOrEmpty(model.PhoneNumber))
                return (HttpStatusCode.BadRequest, "Vui lòng nhập số điện thoại hội viên WIN", null);

            var (okCap, msgCap, dataCap) = await RedeemCapillaryCouponsAsync(
                model.SiteCode, model.POSTerminal, model.PhoneNumber, model.OrderNo, listCAP);

            if (okCap)
            {
                isRedeem = true;
                resultRedeem.AddRange(dataCap ?? []);
                lstVoucherUsed.AddRange(listCAP.Select(x => x.voucherNumber));
                qtyPayment += listCAP.Count;
            }
            else
            {
                resultErrors.AddRange(listCAP.Select(x => x.voucherNumber));
                return (dataCap != null ? HttpStatusCode.BadRequest : HttpStatusCode.BadRequest,
                    msgCap ?? "Có lỗi trong quá trình redeem voucher", null);
            }
        }

        // 4. Remaining SAP/PLG vouchers
        if (qtyPayment < qtyVoucher)
        {
            var remaining = model.ListSeriNo!
                .Where(x => !lstVoucherUsed.Contains(x.voucherNumber))
                .GroupBy(x => x.CompanyCode)
                .ToList();

            var resultFinal = new List<VoucherStatusResponse>();

            foreach (var grp in remaining)
            {
                var companyCode = grp.Key;
                var listData = grp.ToList();

                if (companyCode == "PLG")
                {
                    var plgStore = GetPlgStoreMapping(model.SiteCode);
                    if (plgStore == null)
                        return (HttpStatusCode.BadRequest, "Không tìm thấy cửa hàng PHÚC LONG, Vui lòng liên hệ IT để setup", null);

                    var plgResult = model.IsVoucher
                        ? await RedeemPlgVouchersAsync(model, listData, plgStore, companyCode)
                        : await RedeemPlgCouponsAsync(model, listData, plgStore, companyCode);

                    if (plgResult.Any(x => x.Return != "0")) isRedeem = false;
                    else isRedeem = true;
                    resultFinal.AddRange(plgResult);
                }
                else
                {
                    // SAP SOAP
                    if (listData.Any(x => string.IsNullOrEmpty(x.articleNo)))
                        return (HttpStatusCode.BadRequest, "ArticleNo không được trống", null);
                    if (listData.Any(x => string.IsNullOrEmpty(x.articleType)))
                        return (HttpStatusCode.BadRequest, "ActicleType không được trống.", null);
                    if (listData.Any(x => x.voucherNumber.Length > MaxVoucherLength))
                        return (HttpStatusCode.BadRequest, $"Mã Voucher/Coupon không được lớn hơn {MaxVoucherLength} ký tự", null);

                    var sapResult = await RedeemSapVouchersAsync(listData, companyCode, model.POSTerminal);
                    resultFinal.AddRange(sapResult);
                    isRedeem = sapResult.All(x => x.Return == "0");
                }
            }

            // Update Olala status after successful redeem
            if (resultFinal.FirstOrDefault()?.Return == "0")
                await UpdateOlalaStatusAsync(model);

            return (HttpStatusCode.OK, "OK", resultFinal);
        }

        if (isRedeem)
            return (HttpStatusCode.OK, $"Bill {model.OrderNo} sử dụng voucher thành công", resultRedeem);

        return (HttpStatusCode.BadRequest,
            resultErrors.Count > 0 ? $"Sử dụng voucher {resultErrors.First()} thất bại" : "Sử dụng voucher thất bại",
            null);
    }

    // ── CheckReturnVoucher ────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CheckReturnVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal, bool isLog)
    {
        if (string.IsNullOrEmpty(voucherNumber))
            return (HttpStatusCode.BadRequest, "Mã BNMH không được rỗng", null);
        if (voucherNumber.Length > MaxVoucherLength)
            return (HttpStatusCode.BadRequest, $"Mã BNMH không được lớn hơn {MaxVoucherLength} ký tự", null);

        var ropPrefix = GetRopPrefix();
        if (IsRopConfigAvailable() && _ropSvc.IsRopVoucher(voucherNumber, ropPrefix))
        {
            var (ok, msg, data) = await _ropSvc.CheckVoucherAsync(voucherNumber, siteNo, posTerminal);
            if (ok && data?.ActicleType != "ZPOS")
                return (HttpStatusCode.BadRequest, $"{voucherNumber} Biên nhận mua hàng không tồn tại", data);
            return ok ? (HttpStatusCode.OK, "Success", data) : (HttpStatusCode.BadRequest, msg, data);
        }

        // SAP SOAP — Voucher_Type = "R"
        var inputs = new List<SapVoucherInput>
        {
            new() { VoucherType = "R", SerialNo = voucherNumber, VoucherCurrency = "VND", ProcessingType = "C", Status = "C" }
        };
        var resp = await _sapClient.ProcessReturnVouchersAsync(inputs);
        if (resp?.Count > 0)
        {
            var r = resp[0];
            if (r.Return?.Trim() == "4") return (HttpStatusCode.BadRequest, "Mã BNMH không đúng", null);
            if (r.Return?.Trim() != "0") return (HttpStatusCode.BadRequest, r.Status ?? "", null);
            if (r.Status?.Trim().ToUpper() == "RDM")
                return (HttpStatusCode.BadRequest, "Mã BNMH đã được sử dụng",
                    MapSapOutput(r, voucherNumber));
            if (r.Status?.Trim().ToUpper() == "EXP")
                return (HttpStatusCode.BadRequest, "Mã BNMH đã được hủy",
                    MapSapOutput(r, voucherNumber));
            if (IsExpiredDate(r.Expiry_Date))
                return (HttpStatusCode.BadRequest, "Mã BNMH đã hết hạn sử dụng",
                    MapSapOutput(r, voucherNumber));
            return (HttpStatusCode.OK, "Success",
                MapSapOutput(r, voucherNumber, partner: "SAP"));
        }
        return (HttpStatusCode.BadRequest, "Không có phản hồi từ SAP", null);
    }

    // ── UpdateReturnVoucher ───────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> UpdateReturnVoucherAsync(
        List<VoucherUpdateRequest> model)
    {
        if (model == null || model.Count == 0)
            return (HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.Status)))
            return (HttpStatusCode.BadRequest, "Trạng thái không được trống.", null);
        if (model.Any(x => !new[] { "RDM", "EXP" }.Contains(x.Status)))
            return (HttpStatusCode.BadRequest, "Trạng thái không hợp lệ.", null);
        if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
            return (HttpStatusCode.BadRequest, "BNMH không được trống.", null);
        if (model.Any(x => x.VoucherNumber.Length > MaxVoucherLength))
            return (HttpStatusCode.BadRequest, $"Mã BNMH không được lớn hơn {MaxVoucherLength} ký tự", null);

        var ropPrefix = GetRopPrefix();
        if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), ropPrefix))
            return (HttpStatusCode.BadRequest, "Danh sách voucher/coupon không đồng nhất", null);

        if (IsRopConfigAvailable() && IsUpdateCreateRequestROP(model[0].VoucherNumber, ropPrefix))
        {
            var updateModel = BuildVoucherUpdateModel(model, model[0].SiteCode, model[0].POSTerminal, model[0].OrderNo ?? "");
            var (ok, msg, data) = await _ropSvc.UpdateVoucherStatusAsync(updateModel);
            return ok
                ? (HttpStatusCode.OK, "Success", data)
                : (HttpStatusCode.BadRequest, msg ?? "Có lỗi API từ ROP", data);
        }

        // SAP SOAP return voucher — detect DRW terminal (prefix "P")
        string? articleNoOverride = null;
        if (StringHelper.Left(model[0].POSTerminal, 1) == "P")
        {
            var setup = _cache.GetPOSDataSetup("WWW_ARTICLE_DRW");
            if (setup != null) articleNoOverride = setup.Value;
        }

        var inputs = model
            .Where(x => !string.IsNullOrEmpty(x.VoucherNumber))
            .Select(x => new SapVoucherInput
            {
                VoucherType = "R",
                ArticleNo = articleNoOverride ?? x.ArticleNo ?? "",
                SerialNo = x.VoucherNumber,
                ProcessingType = "U",
                Status = x.Status,
            }).ToList();

        var resp = await _sapClient.ProcessReturnVouchersAsync(inputs);
        if (resp?.Count > 0)
            return (HttpStatusCode.OK, resp[0].Return ?? "",
                resp.Select(x => new VoucherStatusResponse
                {
                    Status = x.Status,
                    Return = x.Return?.Trim(),
                    Value = x.Voucher_Value?.Trim(),
                    Voucher_Currency = x.Voucher_Currency,
                    Expiry_Date = x.Expiry_Date,
                }).ToList());
        return (HttpStatusCode.BadRequest, "Không có phản hồi từ SAP", null);
    }

    // ── CreateReturnVoucher ───────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CreateReturnVoucherAsync(
        List<CreateVoucherModel> model)
    {
        if (model == null || model.Count == 0)
            return (HttpStatusCode.BadRequest, "Dữ liệu đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.VoucherNumber)))
            return (HttpStatusCode.BadRequest, "VoucherNumber đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.From_Date)))
            return (HttpStatusCode.BadRequest, "From Date đang bị trống", null);
        if (model.Any(x => string.IsNullOrEmpty(x.Expiry_Date)))
            return (HttpStatusCode.BadRequest, "Expiry_Date đang bị trống", null);
        if (model.Any(x => x.Value <= 0))
            return (HttpStatusCode.BadRequest, "Value không hợp lệ", null);
        if (model.Any(x => x.VoucherNumber.Length > MaxVoucherLength))
            return (HttpStatusCode.BadRequest, $"Mã BNTT không được lớn hơn {MaxVoucherLength} ký tự", null);

        var ropPrefix = GetRopPrefix();
        if (!IsValidateVoucherROP(model.Select(x => x.VoucherNumber).ToArray(), ropPrefix))
            return (HttpStatusCode.BadRequest, "Danh sách voucher/coupon không đồng nhất", null);

        if (IsRopConfigAvailable() && IsUpdateCreateRequestROP(model[0].VoucherNumber, ropPrefix))
        {
            var bnttArticle = _cache.GetStoreSetConfig()
                ?.FirstOrDefault(x => x.Code == "SAPCODE_BNTT_ROP");
            if (bnttArticle == null)
                return (HttpStatusCode.BadRequest, "Chưa khai báo mã phát hàng Biên nhận thanh toán/Vui lòng liên hệ IT Helpdesk", null);
            model.ForEach(x => x.Article_No = bnttArticle.Value);

            var (ok, msg, data) = await _ropSvc.CreateVoucherAsync(model, "ZPOS", 50);
            return ok
                ? (HttpStatusCode.OK, "Success", data)
                : (HttpStatusCode.BadRequest, msg ?? "Có lỗi API từ ROP", null);
        }

        // SAP SOAP — Voucher_Type="R"
        string? articleOverride = null;
        if (model.Count > 0 && StringHelper.Left(model[0].POSTerminal, 1) == "P")
        {
            var setup = _cache.GetPOSDataSetup("WWW_ARTICLE_DRW");
            if (setup != null) articleOverride = setup.Value;
        }

        var inputs = model
            .Where(x => !string.IsNullOrEmpty(x.VoucherNumber))
            .Select(x => new SapVoucherInput
            {
                VoucherType = "R",
                ArticleNo = articleOverride ?? "",
                SerialNo = x.VoucherNumber,
                VoucherValue = x.Value.ToString(),
                VoucherCurrency = "VND",
                ValidityFromDate = x.From_Date,
                ExpiryDate = x.Expiry_Date,
                ProcessingType = "A",
                Status = "A",
            }).ToList();

        var resp = await _sapClient.ProcessReturnVouchersAsync(inputs);
        if (resp?.Count > 0)
            return (HttpStatusCode.OK, "Success",
                resp.Select(x => new VoucherStatusResponse { Status = x.Status, Return = x.Return?.Trim() }).ToList());
        return (HttpStatusCode.BadRequest, "Không có phản hồi từ SAP", null);
    }

    // ── EVoucher ──────────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> EVoucherAsync(
        string csn, string storeNo, string posTerminal, bool isLog)
    {
        if (string.IsNullOrEmpty(csn))
            return (HttpStatusCode.BadRequest, "CSN không được rỗng", null);

        var inputs = new List<SapEVoucherInput> { new() { CSN = csn, SerialNo = "" } };
        var raw = await _sapClient.GetEVouchersAsync(inputs);

        var result = raw?
            .Where(x => x.Return == "0")
            .Select(x => new EVoucherResponse
            {
                CSN = x.CSN,
                SerialNo = x.SerialNo,
                ArticleNo = x.ArticleNo,
                Amount = decimal.TryParse(x.Amount, out var amt) ? amt : null,
                ValidFrom = ParseSapDate(x.ValidFrom),
                ValidTo = ParseSapDate(x.ValidTo),
                Description = x.Description,
                Type = x.Type,
                Return = x.Return,
                Message = x.Message,
            }).ToList() ?? [];

        if (result.Count == 0)
            return (HttpStatusCode.BadRequest, "Không tồn tại E-Voucher tương ứng với thẻ VinID này", null);

        return (HttpStatusCode.OK, "Success", result);
    }

    // ── SendCodeCpnVch ────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> SendCodeCpnVchAsync(
        string storeNo, string posID, string bussinessDate, string orderNo, string itemNo, string phoneNumber)
    {
        if (string.IsNullOrEmpty(storeNo)) return (HttpStatusCode.BadRequest, "storeNo không được rỗng", null);
        if (string.IsNullOrEmpty(posID)) return (HttpStatusCode.BadRequest, "posID không được rỗng", null);
        if (string.IsNullOrEmpty(orderNo)) return (HttpStatusCode.BadRequest, "orderNo không được rỗng", null);
        if (string.IsNullOrEmpty(bussinessDate)) return (HttpStatusCode.BadRequest, "bussinessDate không được rỗng", null);
        if (string.IsNullOrEmpty(itemNo)) return (HttpStatusCode.BadRequest, "itemNo không được rỗng", null);

        if (!DateTime.TryParse(bussinessDate, out var businessDt))
            return (HttpStatusCode.BadRequest, "bussinessDate không hợp lệ", null);
        if (businessDt.Date != DateTime.Now.Date)
            return (HttpStatusCode.BadRequest, "Ngày giao dịch không hợp lệ", null);

        // Check if already issued for this order
        var existingCode = await _redis.StringGetAsync<CpnVchCodeSendModel>(RedisKeyCpnVch + orderNo);
        if (existingCode?.ItemNo == itemNo)
            return (HttpStatusCode.OK, $"Đơn hàng {orderNo} đã được cấp mã voucher {existingCode.SerialNumber}", existingCode);

        // Get quota config
        var quotaList = await _redis.StringGetAsync<List<CpnVchCodeSendQuota>>(RedisKeyQuota + itemNo) ?? [];
        int qtyOfDay = 0;
        int limitQty = 0;

        if (!string.IsNullOrEmpty(phoneNumber) && quotaList.Count > 0)
        {
            var quotaItem = quotaList.FirstOrDefault(x => x.ItemNo == itemNo && !x.Blocked);
            if (quotaItem != null)
            {
                if (quotaItem.QtyIssue == 0)
                    return (HttpStatusCode.NotFound, $"ItemNo {itemNo} đã cấp hết số lượng voucher", null);

                limitQty = quotaItem.LimitQty;
                if (quotaItem.QtyOfDay > 0)
                {
                    qtyOfDay = quotaItem.QtyOfDay;
                    var remnQuota = await _redis.StringGetAsync<CpnVchCodeQuotaRemn>(RedisKeyRemnQuota + phoneNumber);
                    if (remnQuota?.OrderDate.ToString("yyyyMMdd") == DateTime.Now.ToString("yyyyMMdd")
                        && remnQuota.Qty >= remnQuota.InitQuota)
                        return (HttpStatusCode.Conflict,
                            $"Số điện thoại {phoneNumber} đã được cấp voucher hết hạn mức ngày {remnQuota.OrderDate:dd-MM-yyyy}", null);
                }
            }
        }

        // Call SP on CentralMD
        var spResult = await CallSendCodeSpAsync(storeNo, posID, businessDt, orderNo, itemNo, phoneNumber, qtyOfDay, limitQty);
        if (!spResult.Success)
            return (spResult.Status, spResult.Message, spResult.Data);

        var codeData = spResult.Data as CpnVchCodeSendModel;

        // Cache result and update quota if qtyOfDay is active
        if (qtyOfDay > 0 && codeData != null && !string.IsNullOrEmpty(codeData.SerialNumber))
        {
            var fullData = new CpnVchCodeSendModel
            {
                SerialNumber = codeData.SerialNumber,
                ItemNo = itemNo,
                ItemName = codeData.ItemName,
                OrderNo = orderNo,
                StatusCode = codeData.StatusCode,
                Status = codeData.Status,
                StoreNo = storeNo,
                PosID = posID,
                ActicleType = codeData.ActicleType,
                IsCheckItem = codeData.IsCheckItem,
                MaxQtyUse = codeData.MaxQtyUse,
                MaxAmount = codeData.MaxAmount,
                FromDate = codeData.FromDate,
                ToDate = codeData.ToDate,
                ValueCpnVch = codeData.ValueCpnVch,
                PhoneNumber = phoneNumber,
            };
            _redis.StringSet(RedisKeyCpnVch + orderNo, fullData, DateTimeHelper.GetSecondsUntilMidnight());

            var remnNew = new CpnVchCodeQuotaRemn
            {
                ItemNo = itemNo,
                OrderDate = businessDt.Date,
                PhoneNumber = phoneNumber,
                Qty = 1,
                InitQuota = qtyOfDay,
            };
            _redis.StringSet(RedisKeyRemnQuota + phoneNumber, remnNew, DateTimeHelper.GetSecondsUntilMidnight());
        }

        return (spResult.Status, spResult.Message, codeData);
    }

    // ── Retry ─────────────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> RetryAsync()
    {
        await _ropSvc.UpdateRedeemCpnVchCodeSendsAsync();
        return (HttpStatusCode.OK, "Success", null);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string GetRopPrefix()
    {
        return _cache.GetPOSDataSetup("WWW_ROP_V_PREFIX")?.Value ?? "";
    }

    private bool IsRopConfigAvailable()
    {
        return _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .Any(x => string.Equals(x.AppCode, "ROP", StringComparison.OrdinalIgnoreCase) && !x.Blocked) == true;
    }

    private bool IsRopVoucher(string voucher, string ropPrefix)
        => _ropSvc.IsRopVoucher(voucher, ropPrefix);

    private bool IsUpdateCreateRequestROP(string voucher, string ropPrefix)
        => _ropSvc.IsRopVoucher(voucher, ropPrefix);

    private bool IsValidateVoucherROP(string[] vouchers, string ropPrefix)
    {
        int rop = vouchers.Count(v => IsRopVoucher(v, ropPrefix));
        int sap = vouchers.Length - rop;
        return rop == 0 || sap == 0;
    }

    private bool IsDynamicVoucher(string voucher)
        => voucher.Length == 25 && StringHelper.Left(voucher, 3) == "WDV";

    private bool IsVoucherCapillary(string voucher, string partner)
    {
        if (voucher.Length == 25 && StringHelper.Left(voucher, 3) == "WDV") return true;
        if (voucher.Length >= 7 && voucher.Length <= 10 && !string.Equals(partner, "PLG", StringComparison.OrdinalIgnoreCase)) return true;
        var capPrefixes = new[] { "CAP", "CDC", "CPV" };
        if (capPrefixes.Contains(StringHelper.Left(voucher, 3).ToUpper()) && voucher.Length <= 15) return true;
        return false;
    }

    private async Task<string?> ResolveWinXVoucherAsync(string voucherNumber, string posTerminal, string phoneNumber)
    {
        // WinX dynamic voucher resolution — calls WinX API via SysWebApi AppCode="WINX"
        // Returns resolved voucherNumber or null on failure.
        // TODO: implement when WinX API config is available
        return null;
    }

    private async Task<(HttpStatusCode, string, object?)> CheckCapillaryCouponAsync(
        string siteNo, string posTerminal, string phoneNumber, string voucherNumber)
    {
        var cfg = _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "CAPILLARY", StringComparison.OrdinalIgnoreCase) && !x.Blocked);
        if (cfg == null) return (HttpStatusCode.BadRequest, MessConst.NotFounDataConfig, null);

        var route = cfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "ValidateCoupon", StringComparison.OrdinalIgnoreCase));
        var endpoint = route?.Route ?? "";
        var payload = JsonConvert.SerializeObject(new
        {
            storeNo = siteNo, posTerminal, phoneNumber, voucherCode = voucherNumber
        });
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK)
            return (HttpStatusCode.OK, "Success", resp.Data);
        return (HttpStatusCode.BadRequest, resp?.Message ?? "Lỗi kết nối Capillary", resp?.Data);
    }

    private async Task<(bool, string, List<VoucherStatusResponse>?)> RedeemCapillaryCouponsAsync(
        string siteNo, string posTerminal, string phoneNumber, string orderNo,
        List<VoucherUpdateSerial> vouchers)
    {
        var cfg = _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "CAPILLARY", StringComparison.OrdinalIgnoreCase) && !x.Blocked);
        if (cfg == null) return (false, MessConst.NotFounDataConfig, null);

        var route = cfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "RedeemCoupon", StringComparison.OrdinalIgnoreCase));
        var payload = JsonConvert.SerializeObject(new
        {
            partner = "CAP",
            posNo = posTerminal,
            storeNo = siteNo,
            phoneNumber,
            orderNo,
            serialNo = vouchers.Select(v => new { code = v.voucherNumber, amount = (decimal)v.value, description = "" })
        });
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = route?.Route ?? "",
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK)
        {
            var redeemed = vouchers.Select(v => new VoucherStatusResponse
            {
                Return = "0",
                Status = "RDM",
                VoucherNumber = v.voucherNumber,
            }).ToList();
            return (true, "Success", redeemed);
        }
        return (false, resp?.Message ?? "Có lỗi trong quá trình redeem voucher", null);
    }

    private SysWebApiDto? GetPlgConfig()
        => _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "PLG", StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private object? GetPlgStoreMapping(string siteNo)
    {
        // Store mapping for PLG (Phúc Long Group stores) via StoreSetConfig
        return _cache.GetStoreSetConfig()
            ?.FirstOrDefault(x => string.Equals(x.StoreNo, siteNo, StringComparison.OrdinalIgnoreCase)
                                  && string.Equals(x.Code, "PLG_STORE", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(HttpStatusCode, string, object?)> CheckPlgVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal,
        string partner, string isEmployee, string isCardVoucher)
    {
        var plgCfg = GetPlgConfig();
        if (plgCfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình PLG WebApi", null);

        var route = plgCfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "CheckVoucher", StringComparison.OrdinalIgnoreCase));
        bool isEmp = isEmployee == "X";
        var serials = voucherNumber
            .Split(new[] { ';', ',', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Select(s => new { seriNo = s, isEmployee = isEmp })
            .ToList();

        var payload = JsonConvert.SerializeObject(new
        {
            isWeb = false,
            storeNo = siteNo,
            posID = posTerminal,
            partner,
            isVoucher = isCardVoucher == "X",
            listSeriNo = serials,
        });
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = plgCfg.Host ?? "",
            Endpoint = route?.Route ?? "",
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(plgCfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{plgCfg.UserName}:{plgCfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK)
        {
            var dataList = ConvertHelper.ToObject<List<PlgVoucherResponse>>(
                JsonConvert.SerializeObject(resp.Data));
            var data = dataList?.FirstOrDefault();
            if (data == null) return (HttpStatusCode.BadRequest, "Không có dữ liệu từ PLG", null);

            var plhResp = new VoucherStatusResponse
            {
                Status = data.DescVoucher,
                Return = data.StatusVoucher,
                ActicleType = data.TypeVoucher,
                Value = data.Value.ToString().Replace(".", ""),
                VoucherNumber = voucherNumber,
                Voucher_Currency = "VNĐ",
                CompanyCode = "PLG",
            };
            return data.StatusVoucher == "A"
                ? (HttpStatusCode.OK, "OK", plhResp)
                : (HttpStatusCode.BadRequest, data.DescVoucher ?? "", plhResp);
        }
        return (resp?.Status ?? HttpStatusCode.BadRequest, $"Lỗi {resp?.Message}", null);
    }

    private async Task<(HttpStatusCode, string, object?)> CheckPlgCouponAsync(
        string voucherNumber, string siteNo, string posTerminal, int quantity, string isEmployee)
    {
        var plgCfg = GetPlgConfig();
        if (plgCfg == null) return (HttpStatusCode.BadRequest, "Chưa cấu hình PLG WebApi", null);

        var route = plgCfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "CheckCoupon", StringComparison.OrdinalIgnoreCase));
        bool isEmp = isEmployee == "X";
        var serials = voucherNumber
            .Split(new[] { ';', ',', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Select(s => new { seriNo = s, quantity, isEmployee = isEmp })
            .ToList();

        var payload = JsonConvert.SerializeObject(new
        {
            isWeb = false, storeNo = siteNo, posID = posTerminal, userPOS = "WCM", listSeriNo = serials
        });
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = plgCfg.Host ?? "",
            Endpoint = route?.Route ?? "",
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(plgCfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{plgCfg.UserName}:{plgCfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(result.Response);
        if (resp?.Status == HttpStatusCode.OK)
        {
            var dataList = ConvertHelper.ToObject<List<PlgCouponResponse>>(
                JsonConvert.SerializeObject(resp.Data));
            var data = dataList?.FirstOrDefault();
            if (data == null) return (HttpStatusCode.BadRequest, "Không có dữ liệu từ PLG", null);

            var plhResp = new VoucherStatusResponse
            {
                Status = data.DescCoupon,
                Return = data.StatusCoupon,
                ActicleNo = data.ItemNo,
                ActicleType = "CPN",
                Value = "" + data.Quantity,
                VoucherNumber = voucherNumber,
                Voucher_Currency = "VNĐ",
                CompanyCode = "PLG",
                IsEmployee = data.IsEmployee,
            };
            return data.StatusCoupon == "A"
                ? (HttpStatusCode.OK, "OK", plhResp)
                : (HttpStatusCode.BadRequest, data.DescCoupon ?? "", plhResp);
        }
        return (resp?.Status ?? HttpStatusCode.BadRequest, $"Lỗi {resp?.Message}", null);
    }

    private async Task<List<VoucherStatusResponse>> RedeemPlgVouchersAsync(
        VoucherUpdateModel model, List<VoucherUpdateSerial> listData,
        object? plgStore, string companyCode)
    {
        var plgCfg = GetPlgConfig();
        if (plgCfg == null)
            return ErrorVoucherList(listData.Select(x => x.voucherNumber), companyCode, "Chưa cấu hình PLG WebApi");

        var route = plgCfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "RedeemVoucher", StringComparison.OrdinalIgnoreCase));
        var resultFinal = new List<VoucherStatusResponse>();

        foreach (var partnerGroup in listData.GroupBy(x => x.Partner))
        {
            var payload = JsonConvert.SerializeObject(new
            {
                partner = partnerGroup.Key,
                orderNo = model.OrderNo,
                storeNo = (plgStore as StoreSetConfig)?.Value ?? model.SiteCode,
                posID = model.POSTerminal,
                totalBill = model.TotalBill,
                staffCode = "WCM",
                listSeriNo = partnerGroup.Select(a => new { seriNo = a.voucherNumber, isVoucher = a.isVoucher, value = a.value })
            });
            var apiResult = await _apiClient.SendAsync(new ApiCallRequest
            {
                Host = plgCfg.Host ?? "",
                Endpoint = route?.Route ?? "",
                WebMethod = "POST",
                JsonData = payload,
                Timeout = NumberHelper.GetTimeOutApi(plgCfg.Version),
                AccessToken = EncryptionHelper.Base64Encode($"{plgCfg.UserName}:{plgCfg.Password}"),
                TokenType = "Basic",
            });
            var resp = ConvertHelper.ToObject<ResultResponse>(apiResult.Response);
            if (resp?.Status == HttpStatusCode.OK)
            {
                var dataList = ConvertHelper.ToObject<List<PlgRedeemVoucherResponse>>(
                    JsonConvert.SerializeObject(resp.Data));
                if (dataList == null)
                {
                    resultFinal.AddRange(ErrorVoucherList(partnerGroup.Select(x => x.voucherNumber), companyCode, "Lỗi thanh toán voucher Phúc Long"));
                    continue;
                }
                resultFinal.AddRange(dataList.Select(a => new VoucherStatusResponse
                {
                    Status = a.Message,
                    VoucherNumber = a.SeriNo,
                    Value = a.Amount.ToString().Replace(".", ""),
                    Return = a.StatusVoucher ? "0" : "E",
                    CompanyCode = companyCode,
                    Partner = partnerGroup.Key,
                }));
            }
            else
            {
                resultFinal.AddRange(ErrorVoucherList(partnerGroup.Select(x => x.voucherNumber), companyCode, resp?.Message ?? "Lỗi PLG"));
            }
        }
        return resultFinal;
    }

    private async Task<List<VoucherStatusResponse>> RedeemPlgCouponsAsync(
        VoucherUpdateModel model, List<VoucherUpdateSerial> listData,
        object? plgStore, string companyCode)
    {
        var plgCfg = GetPlgConfig();
        if (plgCfg == null)
            return ErrorVoucherList(listData.Select(x => x.voucherNumber), companyCode, "Chưa cấu hình PLG WebApi");

        var route = plgCfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "RedeemCoupon", StringComparison.OrdinalIgnoreCase));
        var payload = JsonConvert.SerializeObject(new
        {
            orderNo = model.OrderNo,
            storeNo = (plgStore as StoreSetConfig)?.Value ?? model.SiteCode,
            posID = model.POSTerminal,
            staffCode = "WCM",
            listSeriNo = listData.Select(a => new { seriNo = a.voucherNumber, quantityRedeem = (int)a.value })
        });
        var apiResult = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = plgCfg.Host ?? "",
            Endpoint = route?.Route ?? "",
            WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(plgCfg.Version),
            AccessToken = EncryptionHelper.Base64Encode($"{plgCfg.UserName}:{plgCfg.Password}"),
            TokenType = "Basic",
        });
        var resp = ConvertHelper.ToObject<ResultResponse>(apiResult.Response);
        if (resp?.Status == HttpStatusCode.OK)
        {
            var dataList = ConvertHelper.ToObject<List<PlgRedeemCouponResponse>>(
                JsonConvert.SerializeObject(resp.Data));
            if (dataList == null)
                return ErrorVoucherList(listData.Select(x => x.voucherNumber), companyCode, "Lỗi thanh toán coupon Phúc Long");
            return dataList.Select(a => new VoucherStatusResponse
            {
                Status = a.Message,
                VoucherNumber = a.SeriNo,
                Value = "" + a.QuantityRemain,
                Return = a.StatusCode ? "0" : "E",
                CompanyCode = companyCode,
            }).ToList();
        }
        return ErrorVoucherList(listData.Select(x => x.voucherNumber), companyCode, resp?.Message ?? "Lỗi PLG");
    }

    private async Task<List<VoucherStatusResponse>> RedeemSapVouchersAsync(
        List<VoucherUpdateSerial> listData, string companyCode, string posTerminal)
    {
        string? firstType = listData.FirstOrDefault()?.articleType;
        bool isNewFormat = firstType == "ZVCN" || firstType == "ZCPN";

        var inputs = listData
            .Where(x => !string.IsNullOrEmpty(x.voucherNumber))
            .Select(x => new SapVoucherInput
            {
                VoucherType = "V",
                ArticleNo = x.articleNo?.Trim() ?? "",
                SerialNo = x.voucherNumber,
                ProcessingType = "U",
                Status = x.status ?? "RDM",
            }).ToList();

        var resp = isNewFormat
            ? await _sapClient.ProcessVouchersAsync(inputs)
            : await _sapClient.ProcessReturnVouchersAsync(inputs);

        if (resp?.Count > 0)
            return resp.Select(x => new VoucherStatusResponse
            {
                Status = x.Status,
                ActicleNo = x.Article_No?.Trim(),
                Return = x.Return?.Trim(),
                Value = x.Voucher_Value?.Trim(),
                Voucher_Currency = x.Voucher_Currency,
                Expiry_Date = x.Expiry_Date,
                CompanyCode = companyCode,
                ActicleType = isNewFormat ? firstType : "VC_CU",
            }).ToList();

        return ErrorVoucherList(listData.Select(x => x.voucherNumber), companyCode, "Không có phản hồi từ SAP");
    }

    private async Task<(HttpStatusCode, string, object?)> CheckSapVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal, bool isLog)
    {
        var inputs = new List<SapVoucherInput>
        {
            new() { SerialNo = voucherNumber, VoucherCurrency = "VND", ProcessingType = "C", Status = "C" }
        };
        var resp = await _sapClient.ProcessVouchersAsync(inputs);
        if (resp?.Count > 0)
        {
            var r = resp[0];
            if (r.Return?.Trim() == "0")
            {
                if (r.Status?.Trim().ToUpper() == "RDM")
                    return (HttpStatusCode.BadRequest, "Mã Voucher/Coupon đã được sử dụng", MapSapOutput(r, voucherNumber));
                if (r.Status?.Trim().ToUpper() == "EXP")
                    return (HttpStatusCode.BadRequest, "Mã Voucher/Coupon đã bị hủy", MapSapOutput(r, voucherNumber));
                if (r.Status?.Trim().ToUpper() == "AVL")
                    return (HttpStatusCode.BadRequest, $"Mã Voucher/Coupon này ({voucherNumber}) chưa được kích hoạt ({r.Status})", MapSapOutput(r, voucherNumber));
                if (IsExpiredDate(r.Expiry_Date))
                    return (HttpStatusCode.BadRequest, "Mã Voucher/Coupon đã hết hạn sử dụng", MapSapOutput(r, voucherNumber));
                return (HttpStatusCode.OK, "Success", MapSapOutput(r, voucherNumber, partner: "SAP"));
            }
            if (r.Return?.Trim() == "E4")
                return (HttpStatusCode.BadRequest, "Mã Voucher/Coupon không tồn tại", MapSapOutput(r, voucherNumber));
            return (HttpStatusCode.BadRequest, r.Status ?? "", MapSapOutput(r, voucherNumber));
        }
        return (HttpStatusCode.BadRequest, $"Mã Voucher/Coupon này {voucherNumber} không đúng", null);
    }

    private async Task<(HttpStatusCode, string, object?)> UpdateSapVouchersAsync(List<VoucherUpdateRequest> model)
    {
        bool isNewFormat = model[0].ArticleType == "ZVCN" || model[0].ArticleType == "ZCPN";

        var inputs = model
            .Where(x => !string.IsNullOrEmpty(x.VoucherNumber))
            .Select(x => new SapVoucherInput
            {
                VoucherType = "V",
                ArticleNo = x.ArticleNo ?? "",
                SerialNo = x.VoucherNumber,
                ProcessingType = "U",
                Status = x.Status,
            }).ToList();

        var resp = isNewFormat
            ? await _sapClient.ProcessVouchersAsync(inputs)
            : await _sapClient.ProcessReturnVouchersAsync(inputs);

        if (resp?.Count > 0)
            return (HttpStatusCode.OK, "Success",
                resp.Select(x => new VoucherStatusResponse
                {
                    Status = x.Status,
                    Return = x.Return?.Trim(),
                    Value = x.Voucher_Value?.Trim(),
                    Voucher_Currency = x.Voucher_Currency,
                    Expiry_Date = x.Expiry_Date,
                }).ToList());
        return (HttpStatusCode.BadRequest, "Không có phản hồi từ SAP", null);
    }

    private async Task<(HttpStatusCode, string, object?)> CheckOlalaByPhoneAsync(string phone, string posTerminal)
    {
        // Olala partner — not implemented in this phase
        // TODO: implement when UsingVoucherPartnerService is ported
        await Task.CompletedTask;
        return (HttpStatusCode.BadRequest, $"Số điện thoại {phone} không có mã giảm giá", null);
    }

    private async Task UpdateOlalaStatusAsync(VoucherUpdateModel model)
    {
        // Fire-and-forget Olala status update — stub
        await Task.CompletedTask;
    }

    private async Task<(bool Success, HttpStatusCode Status, string Message, object? Data)> CallSendCodeSpAsync(
        string storeNo, string posID, DateTime businessDt, string orderNo,
        string itemNo, string phoneNumber, int qtyOfDay, int limitQty)
    {
        try
        {
            var connStr = _cache.GetConnectStringCentralMD();
            using var conn = _dbFactory.CreateConnectionFromRaw(connStr);
            conn.Open();

            var modelData = (await conn.QueryAsync<CpnVchCodeSendModel>(
                "[dbo].[SP_CpnVchCodeSend] @ItemNo,@StoreNo,@PosID,@OrderNo,@PhoneNumber,@QtyOfDay,@LimitQty",
                new { ItemNo = itemNo, StoreNo = storeNo, PosID = posID, OrderNo = orderNo, PhoneNumber = phoneNumber, QtyOfDay = qtyOfDay, LimitQty = limitQty }
            )).FirstOrDefault();

            if (modelData == null)
                return (false, HttpStatusCode.NotFound, $"Không tìm thấy mã Coupon/voucher cho item {itemNo} này", null);

            if (!string.IsNullOrEmpty(modelData.ItemNo) && string.IsNullOrEmpty(modelData.SerialNumber))
            {
                string msg = modelData.ItemNo == "OUT"
                    ? $"Số điện thoại {phoneNumber} đã hết hạn mức nhận {limitQty} voucher"
                    : modelData.ItemNo == "OUT_OF_DAY"
                        ? $"Số điện thoại {phoneNumber} đã hết hạn mức nhận {qtyOfDay} voucher của ngày hôm nay"
                        : "Không tìm thấy mã Coupon/voucher";
                return (false, HttpStatusCode.NotFound, msg, modelData);
            }

            // Insert send record
            await conn.ExecuteAsync(
                @"INSERT INTO [dbo].[CpnVchCodeSend]([StoreNo],[PosID],[Date],[OrderNo],[ItemNo],[Code],[PhoneNumber],[CreatedDate])
                  VALUES (@StoreNo,@PosID,@Date,@OrderNo,@ItemNo,@Code,@PhoneNumber,getdate())",
                new CpnVchCodeSendInsertDto
                {
                    StoreNo = storeNo, PosID = posID, Date = businessDt, OrderNo = orderNo,
                    ItemNo = itemNo, Code = modelData.SerialNumber, PhoneNumber = phoneNumber ?? "",
                });

            return (true, HttpStatusCode.OK, "Success", modelData);
        }
        catch (Exception ex)
        {
            return (false, HttpStatusCode.BadRequest, ex.Message, null);
        }
    }

    private static VoucherUpdateModel BuildVoucherUpdateModel(
        List<VoucherUpdateRequest> model, string siteCode, string posTerminal, string orderNo)
    {
        return new VoucherUpdateModel
        {
            SiteCode = siteCode,
            POSTerminal = posTerminal,
            OrderNo = orderNo,
            IsVoucher = false,
            TotalBill = 0,
            ListSeriNo = model.Select(x => new VoucherUpdateSerial
            {
                Partner = "",
                CompanyCode = x.CompanyCode,
                isVoucher = false,
                articleNo = x.ArticleNo,
                articleType = x.ArticleType,
                value = 0,
                status = x.Status,
                voucherNumber = x.VoucherNumber,
            }).ToList(),
        };
    }

    private static VoucherStatusResponse MapSapOutput(SapVoucherOutput r, string voucherNumber, string? partner = null)
        => new()
        {
            Status = r.Status,
            Return = r.Return?.Trim(),
            ActicleNo = r.Article_No?.Trim(),
            ActicleType = r.Article_Type,
            Value = r.Voucher_Value?.Trim().Replace(".", ""),
            VoucherNumber = voucherNumber,
            Voucher_Currency = r.Voucher_Currency,
            Validity_From_Date = r.Validity_From_Date,
            Expiry_Date = r.Expiry_Date,
            CompanyCode = r.CompanyCode,
            Partner = partner,
        };

    private static bool IsExpiredDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return false;
        try
        {
            var d = DateTime.ParseExact(dateStr.Replace(".", "/"), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            return d < DateTime.Now.Date;
        }
        catch { return false; }
    }

    private static DateTime? ParseSapDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        return null;
    }

    private static List<VoucherStatusResponse> ErrorVoucherList(
        IEnumerable<string> vouchers, string companyCode, string message)
        => vouchers.Select(v => new VoucherStatusResponse
        {
            Status = message,
            VoucherNumber = v,
            Value = "0",
            Return = "E",
            CompanyCode = companyCode,
        }).ToList();
}
