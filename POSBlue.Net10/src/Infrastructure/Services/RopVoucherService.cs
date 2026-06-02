using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Infrastructure.Services;

/// <summary>
/// Implement IRopVoucherService — gọi ROP (Retail Operation Platform) REST API qua IApiCallClient.
/// Auth: Basic (Base64 UserName:Password) từ SysWebApi AppCode="ROP".
/// Routes: lấy từ SysWebApiRoute theo Name.
/// Port logic từ ROPVoucherService cũ; bỏ FileHelper, KibanaService (đã có IRequestLogger chung).
/// </summary>
public sealed class RopVoucherService : IRopVoucherService
{
    // ROP voucher status IDs (từ VoucherROPConst/VoucherStatusEnum)
    private static readonly int[] ValidCheckStatusIds = { 40 }; // InStock = 40 → OK to use
    private static readonly Dictionary<string, int> StatusCodeToId = new()
    {
        { "NOTEXIST", 0 }, { "NEW", 10 }, { "AVL", 20 }, { "PEDDINGPRINT", 30 },
        { "INSTOCK", 40 }, { "SOLD", 50 }, { "RDM", 60 }, { "CANCELLED", 70 },
        { "EXPIRED", 80 }, { "EXP", 90 },
    };

    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly IRedisService _redis;

    public RopVoucherService(IMemoryCacheService cache, IApiCallClient apiClient, IRedisService redis)
    {
        _cache = cache;
        _apiClient = apiClient;
        _redis = redis;
    }

    public bool IsRopVoucher(string voucherNumber, string ropPrefix)
    {
        if (string.IsNullOrEmpty(voucherNumber) || string.IsNullOrEmpty(ropPrefix)) return false;
        var prefixes = ropPrefix.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string prefix3 = StringHelper.Left(voucherNumber, 3).ToUpper();
        string suffix2 = voucherNumber.Length >= 2
            ? voucherNumber[^2..].ToUpper()
            : "";
        if (prefixes.Contains(prefix3)) return true;
        if (prefixes.Contains(suffix2) && voucherNumber.Length >= 19) return true;
        return false;
    }

    public async Task<(bool Success, string Message, VoucherStatusResponse? Data)> CheckVoucherBySerialAsync(
        string voucherNumber, string siteNo, string posTerminal)
    {
        var (cfg, token) = GetCfgAndToken();
        if (cfg == null) return (false, "Chưa cấu hình WebApi ROP", null);

        var route = GetRoute(cfg, "CheckVoucherBySerials");
        var payload = JsonConvert.SerializeObject(new RopCheckRequest
        {
            VoucherNumbers = [voucherNumber],
            Site = siteNo,
            PosTerminal = posTerminal,
        });
        var resp = await CallRopAsync(cfg.Host!, route, token, payload);
        if (resp?.ResponseCode == 200)
        {
            var info = ParseSingleVoucher(resp.Data);
            if (info == null) return (false, "Lỗi parse dữ liệu ROP", null);
            bool isValid = ValidCheckStatusIds.Contains(info.StatusId);
            var result = MapRopVoucherToResponse(info, voucherNumber);
            return (isValid && info.StorageSite == siteNo, GetRopMessage("CheckVoucherBySerials", info.StatusId), result);
        }
        return (false, resp?.Message ?? "Lỗi gọi API ROP", null);
    }

    public async Task<(bool Success, string Message, VoucherStatusResponse? Data)> CheckVoucherAsync(
        string voucherNumber, string siteNo, string posTerminal)
    {
        var (cfg, token) = GetCfgAndToken();
        if (cfg == null) return (false, "Chưa cấu hình WebApi ROP", null);

        var route = GetRoute(cfg, "CheckVouchers");
        var payload = JsonConvert.SerializeObject(new RopCheckRequest
        {
            VoucherNumbers = [voucherNumber],
            Site = siteNo,
            PosTerminal = posTerminal,
        });
        var resp = await CallRopAsync(cfg.Host!, route, token, payload);
        if (resp?.ResponseCode == 200)
        {
            var info = ParseSingleVoucher(resp.Data);
            if (info == null) return (false, "Lỗi parse dữ liệu ROP", null);
            bool isValid = ValidCheckStatusIds.Contains(info.StatusId);
            var result = MapRopVoucherToResponse(info, voucherNumber);
            return (isValid, GetRopMessage("CheckVouchers", info.StatusId), result);
        }
        return (false, resp?.Message ?? "Lỗi gọi API ROP", null);
    }

    public async Task<(bool Success, string Message, List<VoucherStatusResponse>? Data)> CreateVoucherAsync(
        List<CreateVoucherModel> model, string voucherType, int statusId)
    {
        var (cfg, token) = GetCfgAndToken();
        if (cfg == null) return (false, "Chưa cấu hình WebApi ROP", null);

        var first = model[0];
        var request = new RopInsertRequest
        {
            POSTerminal = first.POSTerminal,
            SiteCode = first.SiteCode,
            ArticleType = voucherType,
            Vouchers = model
                .Where(x => !string.IsNullOrEmpty(x.VoucherNumber))
                .Select(x => new RopInsertVoucherItem
                {
                    OrderNumber = x.OrderNo,
                    VoucherCode = x.VoucherNumber,
                    VoucherValue = x.Value,
                    BonusBuy = x.BonusBuy ?? "",
                    SAPCode = x.Article_No ?? "",
                    StatusId = statusId,
                    VoucherPercent = 0,
                    ValidFrom = FormatDateForRop(x.From_Date),
                    ValidTo = FormatDateForRop(x.Expiry_Date),
                }).ToList(),
        };

        var route = GetRoute(cfg, "InsertVoucher");
        var resp = await CallRopAsync(cfg.Host!, route, token, JsonConvert.SerializeObject(request));
        if (resp?.ResponseCode == 200)
        {
            var result = request.Vouchers!.Select(x => new VoucherStatusResponse
            {
                Status = "Created",
                Return = "0",
                VoucherNumber = x.VoucherCode,
                Value = x.VoucherValue.ToString(),
            }).ToList();
            return (true, resp.Message ?? "Thành công", result);
        }
        string errMsg = resp?.TechnicalMessage?.FirstOrDefault() ?? resp?.Message ?? "Có lỗi API từ ROP";
        return (false, errMsg, null);
    }

    public async Task<(bool Success, string Message, List<VoucherStatusResponse>? Data)> UpdateVoucherStatusAsync(
        VoucherUpdateModel model)
    {
        var (cfg, token) = GetCfgAndToken();
        if (cfg == null) return (false, "Chưa cấu hình WebApi ROP", null);

        var vouchers = model.ListSeriNo?
            .Where(x => x.voucherNumber.Length > 15
                        && !IsCapillaryVoucher(x.voucherNumber))
            .Select(x => x.voucherNumber)
            .ToArray() ?? [];

        string statusCode = model.ListSeriNo?.FirstOrDefault()?.status ?? "RDM";
        int statusId = StatusCodeToId.TryGetValue(statusCode.ToUpper(), out var sid) ? sid : 60;

        var request = new RopUpdateStatusRequest
        {
            PosTerminal = model.POSTerminal,
            Site = model.SiteCode,
            Status = statusId,
            OrderNumber = model.OrderNo ?? "",
            VoucherNumbers = vouchers,
        };

        var route = GetRoute(cfg, "UpdateVoucherStatus");
        var resp = await CallRopAsync(cfg.Host!, route, token, JsonConvert.SerializeObject(request));
        if (resp?.ResponseCode == 200)
        {
            var result = vouchers.Select(v => new VoucherStatusResponse
            {
                Return = "0",
                Status = "RDM",
                VoucherNumber = v,
                Expiry_Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            }).ToList();

            // Cache for retry
            _ = Task.Run(() =>
            {
                try { _redis.HashSet<List<VoucherStatusResponse>>("BLUEPOS:UpdateVoucherStatus", model.OrderNo ?? "", result); }
                catch { }
            });

            return (true, resp.Message ?? "Thành công", result);
        }

        if (resp?.ResponseCode == 1006)
        {
            var errorData = ParseNullableVoucherList(resp.Data)
                ?.Select(x => new VoucherStatusResponse
                {
                    Status = GetRopMessage("UpdateVoucherStatus", x.StatusId),
                    Return = "E16",
                    Partner = "ROP",
                    CompanyCode = x.StorageSite,
                    VoucherNumber = x.VoucherCode ?? x.SerialNumber,
                }).ToList();
            string msg = errorData?.FirstOrDefault()?.Status ?? resp.Message ?? "Lỗi ROP";
            return (false, msg, errorData);
        }
        return (false, resp?.Message ?? "Lỗi gọi API ROP", null);
    }

    public async Task<List<VoucherStatusResponse>?> RetryUpdateVoucherStatusAsync(string orderNo)
    {
        return _redis.HashGet<List<VoucherStatusResponse>>("BLUEPOS:UpdateVoucherStatus", orderNo);
    }

    public async Task UpdateRedeemCpnVchCodeSendsAsync()
    {
        // TODO: port UpdateRedeemCpnVchCodeSends from legacy ROPVoucherService
        await Task.CompletedTask;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private (SysWebApiDto? cfg, string token) GetCfgAndToken()
    {
        var cfg = _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
            .FirstOrDefault(x => string.Equals(x.AppCode, "ROP", StringComparison.OrdinalIgnoreCase) && !x.Blocked);
        if (cfg == null) return (null, "");
        string token = EncryptionHelper.Base64Encode($"{cfg.UserName}:{cfg.Password}");
        return (cfg, token);
    }

    private static string GetRoute(SysWebApiDto cfg, string name)
        => cfg.SysWebApiRoute?
               .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))?
               .Route ?? "";

    private async Task<RopApiResponse?> CallRopAsync(string host, string route, string token, string payload)
    {
        var result = await _apiClient.SendAsync(new ApiCallRequest
        {
            Host = host,
            Endpoint = route,
            WebMethod = "POST",
            JsonData = payload,
            Timeout = 30000,
            AccessToken = token,
            TokenType = "Basic",
        });
        return ConvertHelper.ToObject<RopApiResponse>(result.Response);
    }

    private static RopVoucherInfo? ParseSingleVoucher(object? data)
    {
        if (data == null) return null;
        try { return JsonConvert.DeserializeObject<List<RopVoucherInfo>>(JsonConvert.SerializeObject(data))?.FirstOrDefault(); }
        catch { return null; }
    }

    private static List<RopVoucherInfoNullable>? ParseNullableVoucherList(object? data)
    {
        if (data == null) return null;
        try { return JsonConvert.DeserializeObject<List<RopVoucherInfoNullable>>(JsonConvert.SerializeObject(data)); }
        catch { return null; }
    }

    private static VoucherStatusResponse MapRopVoucherToResponse(RopVoucherInfo info, string voucherNumber)
        => new()
        {
            Status = GetStatusCodeById(info.StatusId),
            Return = "0",
            ActicleNo = info.SAPCode?.Trim(),
            ActicleType = info.SAPArticleType ?? "",
            Value = info.VoucherValue.ToString(),
            VoucherNumber = info.VoucherCode ?? voucherNumber,
            Voucher_Currency = "VND",
            Validity_From_Date = info.ValidFrom.ToString("dd/MM/yyyy"),
            Expiry_Date = info.ValidTo.ToString("dd/MM/yyyy"),
            CompanyCode = info.CompanyCode?.Trim(),
            Partner = "ROP",
            IsEmployee = false,
        };

    private string GetRopMessage(string actionType, int statusId)
    {
        try
        {
            var configs = _cache.GetNotifyConfig(_cache.GetConnectStringCentralMD())?
                .Where(x => string.Equals(x.AppCode, "ROP", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return configs?.FirstOrDefault(x =>
                string.Equals(x.ActionType, actionType, StringComparison.OrdinalIgnoreCase) &&
                x.Status == statusId.ToString())?.Message ?? "Tham số truyền vào không hợp lệ";
        }
        catch { return "Tham số truyền vào không hợp lệ"; }
    }

    private static string GetStatusCodeById(int id)
    {
        return id switch
        {
            0 => "NOTEXIST", 10 => "NEW", 20 => "AVL", 30 => "PEDDINGPRINT",
            40 => "INSTOCK", 50 => "SOLD", 60 => "RDM", 70 => "CANCELLED",
            80 => "EXPIRED", 90 => "EXP", _ => id.ToString(),
        };
    }

    private static bool IsCapillaryVoucher(string v)
    {
        var capPrefixes = new[] { "CAP", "CDC", "CPV" };
        return (v.Length >= 7 && v.Length <= 10) ||
               (capPrefixes.Contains(StringHelper.Left(v, 3).ToUpper()) && v.Length <= 15);
    }

    private static string FormatDateForRop(string date)
    {
        // Legacy chuyển từ "dd/MM/yyyy" → "yyyy-MM-dd"
        if (date.Contains('/'))
        {
            var parts = date.Split('/');
            if (parts.Length == 3) return $"{parts[2]}-{parts[1]}-{parts[0]}";
        }
        return date;
    }
}
