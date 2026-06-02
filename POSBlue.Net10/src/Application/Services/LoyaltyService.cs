using System.Net;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Enums;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Toàn bộ use case Loyalty — gọi VINID REST API, ScanAndGo API, Capillary API qua IApiCallClient;
/// SOAP TransactionEnquiry qua IOneFlexiAxisClient. Không tham chiếu trực tiếp Infrastructure.
/// </summary>
public sealed class LoyaltyService : ILoyaltyService
{
    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly ILoyaltyRepository _loyaltyRepo;
    private readonly IOneFlexiAxisClient _soapClient;
    private readonly IRequestLogger _logger;
    private readonly ICXService _cxService;

    public LoyaltyService(
        IMemoryCacheService cache,
        IApiCallClient apiClient,
        ILoyaltyRepository loyaltyRepo,
        IOneFlexiAxisClient soapClient,
        IRequestLogger logger,
        ICXService cxService)
    {
        _cache = cache;
        _apiClient = apiClient;
        _loyaltyRepo = loyaltyRepo;
        _soapClient = soapClient;
        _logger = logger;
        _cxService = cxService;
    }

    // ── Config helpers ────────────────────────────────────────────────────

    private SysWebApiDto? GetVinIdCfg() =>
        _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
              .FirstOrDefault(x => string.Equals(x.AppCode, AppCodeEnum.VINID.ToString(), StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private SysWebApiDto? GetScanAndGoCfg() =>
        _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
              .FirstOrDefault(x => string.Equals(x.AppCode, "VINCART", StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private SysWebApiDto? GetCapillaryCfg() =>
        _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
              .FirstOrDefault(x => string.Equals(x.AppCode, AppCodeEnum.CAPILLARY.ToString(), StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private SysWebApiDto? GetWinLifeCfg() =>
        _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
              .FirstOrDefault(x => string.Equals(x.AppCode, AppCodeEnum.WINLIFE.ToString(), StringComparison.OrdinalIgnoreCase) && !x.Blocked);

    private static Dictionary<string, string> VinIdHeaders(SysWebApiDto cfg) => new()
    {
        { "X-Api-Key",    cfg.UserName ?? "" },
        { "X-Api-Secret", cfg.Password ?? "" },
        { "X-Lang",       "vn" },
        { "X-Request-ID", Guid.NewGuid().ToString() },
    };

    private string VinPaySign(string data, SysWebApiDto cfg)
        => EncryptionHelper.VinPaySignature(data, cfg.PrivateKey ?? "");

    private StoreMappingModel? GetStoreMapping(string storeNo, string posId)
    {
        try
        {
            return _loyaltyRepo.GetLoyaltyStoreMapping()
                .FirstOrDefault(x => x.NewStoreID == storeNo && x.NewTerminalID == posId);
        }
        catch { return null; }
    }

    // ── LoyaltyRetry ──────────────────────────────────────────────────────

    public Task LoyaltyRetryAsync()
    {
        // Retry logic (ghi log, đẩy lại RabbitMQ) — sẽ implement khi migrate LoyaltyOfflineService
        return Task.CompletedTask;
    }

    // ── GetCustomerDetail (route VINID vs Capillary) ─────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> GetCustomerDetailAsync(
        string numberCard, string posId, string storeNo,
        string clubCode = "", bool isMobile = false, bool isLog = true)
    {
        if (string.IsNullOrEmpty(numberCard) || numberCard.Length < 9)
            return (HttpStatusCode.BadRequest, "Số điện thoại/số thẻ đang bị trống hoặc không đủ chiều dài ký tự", null);

        var checkCapillary = NumberHelper.IsMemberCapillary(numberCard, isMobile);

        if (LoyaltyHelper.IsVINID(numberCard) && !checkCapillary.IsCapillary)
        {
            if (LoyaltyHelper.IsHFStore(_cache, storeNo))
                return (HttpStatusCode.BadRequest, $"Mã CH/ST {storeNo} HF không được tích/tiêu VINID", null);

            return await GetVinIdMemberCoreAsync(numberCard, posId, storeNo, isLog);
        }
        else if (checkCapillary.IsCapillary)
        {
            return await GetCapillaryMemberAsync(numberCard, storeNo, posId, checkCapillary.MemberType, clubCode, isMobile);
        }

        return (HttpStatusCode.BadRequest, LoyaltyHelper.MessageNotValidPhone(numberCard), null);
    }

    // ── CustomerRegistration ──────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CustomerRegistrationAsync(
        CustomerRegisterRequest request, string channel)
    {
        request.phoneNo = FormatHelper.PhoneNumberVietNam(request.phoneNo);
        if (!NumberHelper.IsPhoneNumber(request.phoneNo))
            return (HttpStatusCode.BadRequest, LoyaltyHelper.MessageNotValidPhone(request.phoneNo), null);

        var otpResult = await _cxService.VerifyOTPAsync(request.posCode, request.phoneNo, request.otp, CXEnum.WIN_MEMBER_REGISTER.ToString());
        if (!otpResult.IsValid)
            return (HttpStatusCode.OK, otpResult.Message, otpResult.Data);  // Conflict dùng HTTP 200 body status = Conflict như legacy

        return await CallCapillaryAsync("CustomerRegistration",
            JsonConvert.SerializeObject(request), request.phoneNo, request.posCode ?? "");
    }

    // ── CustomerUpdate ────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> CustomerUpdateAsync(
        CustomerUpdateRequest request, string channel)
    {
        if (!NumberHelper.IsPhoneNumber(request.phoneNo))
            return (HttpStatusCode.BadRequest, LoyaltyHelper.MessageNotValidPhone(request.phoneNo ?? ""), null);

        return await CallCapillaryAsync("CustomerUpdate",
            JsonConvert.SerializeObject(request), request.phoneNo ?? "", request.posCode ?? "");
    }

    // ── AddTransaction (route VINID vs Capillary) ─────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> AddTransactionAsync(VinIdSalesRequest model)
    {
        if (LoyaltyHelper.IsVINID(model.CardNumber))
        {
            if (LoyaltyHelper.IsHFStore(_cache, model.MerchantId ?? ""))
                return (HttpStatusCode.BadRequest, $"Mã CH/ST {model.MerchantId} không được tích/tiêu VINID", null);

            return VinIdSalesCore(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId,
                model.InvoiceNo, (int)model.SpendPoints, model.BillAmount,
                model.OrderNo, model.IsOffline, model.OrderAmount, model.VirtualCard,
                model.AmountToEarn);
        }
        else if (NumberHelper.IsPhoneNumber(model.CardNumber))
        {
            return await CallCapillaryAsync("AddTransaction",
                JsonConvert.SerializeObject(model), model.CardNumber, model.PosID ?? "");
        }

        return (HttpStatusCode.BadRequest, LoyaltyHelper.MessageNotValidPhone(model.CardNumber), null);
    }

    // ── RefundTransaction (route VINID vs Capillary) ───────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> RefundTransactionAsync(VinIdRefundRequest model)
    {
        if (LoyaltyHelper.IsVINID(model.CardNumber))
        {
            return VinIdRefundCore(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId,
                model.InvoiceNo, model.OrderNo, model.OrigOrderNo,
                (int)model.SpendPoints, model.RefundAmount, model.IsOffline,
                model.OrderAmount, model.IsScanAndGo, model.CXRefundAmount);
        }
        else if (NumberHelper.IsPhoneNumber(model.CardNumber))
        {
            return await CallCapillaryAsync("RefundTransaction",
                JsonConvert.SerializeObject(model), model.CardNumber, "");
        }

        return (HttpStatusCode.BadRequest, LoyaltyHelper.MessageNotValidPhone(model.CardNumber), null);
    }

    // ── OtherStatusUpdate ─────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> OtherStatusUpdateAsync(OtherStatusUpdateRequest request)
    {
        request.PhoneNumber = FormatHelper.PhoneNumberVietNam(request.PhoneNumber);
        return await CallCapillaryAsync("OtherStatusUpdate",
            JsonConvert.SerializeObject(request), request.PhoneNumber, request.PosNo);
    }

    // ── VinIdGetInfoMember ────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdGetInfoMemberAsync(
        string numberCard, string posId, string storeNo, string clubCode = "", bool isLog = true)
    {
        if (string.IsNullOrEmpty(numberCard))
            return (HttpStatusCode.BadRequest, "numberCard đang bị trống", null);

        if (LoyaltyHelper.IsVINID(numberCard))
        {
            if (LoyaltyHelper.IsHFStore(_cache, storeNo))
                return (HttpStatusCode.BadRequest, $"Mã CH/ST {storeNo} HF không được tích/tiêu VINID", null);

            return await GetVinIdMemberCoreAsync(numberCard, posId, storeNo, isLog);
        }

        // Capillary phone fallback
        return await GetCapillaryMemberAsync(numberCard, storeNo, posId, "PHONE");
    }

    // ── VinIdSales (sync — legacy compatibility) ──────────────────────────

    public (HttpStatusCode Status, string Message, object? Data) VinIdSales(
        string qrCode, string cardNumber, string merchantId, string terminalId,
        string invoiceNo, int spendPoints, decimal billAmount,
        string orderNo, bool isOffline, decimal orderAmount, string virtualCard = "")
    {
        return VinIdSalesCore(qrCode, cardNumber, merchantId, terminalId, invoiceNo,
            spendPoints, billAmount, orderNo, isOffline, orderAmount, virtualCard, null);
    }

    // ── VinIdSalesV2 (sync) ───────────────────────────────────────────────

    public (HttpStatusCode Status, string Message, object? Data) VinIdSalesV2(VinIdSalesRequest model)
    {
        return VinIdSalesCore(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId,
            model.InvoiceNo, (int)model.SpendPoints, model.BillAmount,
            model.OrderNo, model.IsOffline, model.OrderAmount, model.VirtualCard,
            model.AmountToEarn);
    }

    // ── VinIdRefund (sync) ────────────────────────────────────────────────

    public (HttpStatusCode Status, string Message, object? Data) VinIdRefund(
        string qrCode, string cardNumber, string merchantId, string terminalId,
        string invoiceNo, string orderNo, string origOrderNo,
        int spendPoints, decimal refundAmount, bool isOffline,
        decimal orderAmount, bool isScanAndGo = false)
    {
        return VinIdRefundCore(qrCode, cardNumber, merchantId, terminalId, invoiceNo, orderNo, origOrderNo,
            spendPoints, refundAmount, isOffline, orderAmount, isScanAndGo, null);
    }

    // ── VinIdRefundV2 (async wrapper) ─────────────────────────────────────

    public Task<(HttpStatusCode Status, string Message, object? Data)> VinIdRefundV2Async(VinIdRefundRequest model)
    {
        var result = VinIdRefundCore(model.QRCode, model.CardNumber, model.MerchantId, model.TerminalId,
            model.InvoiceNo, model.OrderNo, model.OrigOrderNo, (int)model.SpendPoints,
            model.RefundAmount, model.IsOffline, model.OrderAmount, model.IsScanAndGo, model.CXRefundAmount);
        return Task.FromResult(result);
    }

    // ── VinIdInitTransaction ──────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdInitTransactionAsync(PosTransactionRequest request)
    {
        if (string.IsNullOrEmpty(request.QRCode))
            return (HttpStatusCode.BadRequest, "Mã QRCode đang bị trống", null);
        if (string.IsNullOrEmpty(request.MerchantId))
            return (HttpStatusCode.BadRequest, "Mã siêu thị đang bị trống", null);
        if (string.IsNullOrEmpty(request.TerminalId))
            return (HttpStatusCode.BadRequest, "Mã POS đang bị trống", null);
        if (string.IsNullOrEmpty(request.InvoiceNo))
            return (HttpStatusCode.BadRequest, "InvoiceNo đang bị trống", null);
        if (request.Amount <= 0)
            return (HttpStatusCode.BadRequest, "Amount nhỏ hơn 0", null);
        if (request.BillAmount <= 0)
            return (HttpStatusCode.BadRequest, "BillAmount nhỏ hơn 0", null);
        if (StringHelper.Left(request.QRCode, 4) == "8888" || StringHelper.Left(request.QRCode, 5) == "66668")
            return (HttpStatusCode.BadRequest, $"Mã QRCode {request.QRCode} không hợp lệ", null);

        var cfg = GetVinIdCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var posStoreNo = request.MerchantId;
        var posTerminal = request.TerminalId;
        var storeMapping = GetStoreMapping(request.MerchantId!, request.TerminalId!);
        if (storeMapping != null)
        {
            request.TerminalId = storeMapping.OldVinPayTerminalID;
            request.MerchantId = storeMapping.OldVinPayStoreID;
        }

        var channelCode = "VINMART";
        var transactionRefNumber = request.InvoiceNo + "" + new Random().Next(0, 9999);
        int currentTimestamp = (int)(DateTime.UtcNow - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)).TotalSeconds;
        var orderCreatedAt = currentTimestamp.ToString();
        int billAmount = Convert.ToInt32(request.BillAmount);
        int amount = Convert.ToInt32(request.Amount);
        string plainText = "|" + request.QRCode + "|" + channelCode + "|" + request.MerchantId + "|" + request.TerminalId + "|" + transactionRefNumber + "|" + request.InvoiceNo + "|" + "VND" + "|" + orderCreatedAt + "|" + billAmount + "|" + amount;
        var model = new VinIdTransactionApiRequest
        {
            transaction_ref_number = transactionRefNumber,
            code_value = request.QRCode,
            description = "Thanh toán ví VINID",
            merchant_id = request.MerchantId,
            terminal_id = request.TerminalId,
            order_created_at = orderCreatedAt,
            currency = "VND",
            invoice_no = request.InvoiceNo,
            bill_amount = billAmount.ToString(),
            amount = amount.ToString(),
            channel_code = channelCode,
            signature = VinPaySign(plainText, cfg),
        };

        var apiReq = BuildVinIdRequest(cfg, "/internal/checkout/v1/transactions", JsonConvert.SerializeObject(model));
        _logger.LogRequest(cfg.Host + "/internal/checkout/v1/transactions", posTerminal, "", request.QRCode);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, posTerminal, result.ResponseTimeMs, "InitTransaction", result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdTransactionApiData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data != null)
        {
            return (HttpStatusCode.OK, "Thành công", new ResultTransactionDto
            {
                Code = resp.Meta.Code,
                TransactionID = resp.Data.transaction_id,
                Message = resp.Meta.Message,
                TransactionStatus = resp.Data.transactionStatus,
                TransactionWalletNumber = resp.Data.transaction_wallet_number,
            });
        }
        return BuildVinIdError(resp?.Meta);
    }

    // ── VinIdGetTransaction ───────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdGetTransactionAsync(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId))
            return (HttpStatusCode.BadRequest, "Mã giao dịch không được rỗng", null);

        var cfg = GetVinIdCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var endpoint = "/internal/checkout/v1/transactions/?transaction_id=" + transactionId;
        var apiReq = BuildVinIdRequest(cfg, endpoint, null, "GET");
        var result = await _apiClient.SendAsync(apiReq);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdTransactionApiData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data != null)
        {
            return (HttpStatusCode.OK, "Thành công", new ResultTransactionDto
            {
                Code = resp.Meta.Code,
                TransactionID = resp.Data.transaction_id,
                Message = resp.Meta.Message,
                TransactionStatus = resp.Data.transaction_status,
            });
        }
        return BuildVinIdError(resp?.Meta);
    }

    // ── VinIdCancelTransaction ────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdCancelTransactionAsync(PosCancelTransactionRequest request)
    {
        if (string.IsNullOrEmpty(request.TransactionId))
            return (HttpStatusCode.BadRequest, "Mã giao dịch không được trống.", null);

        var cfg = GetVinIdCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var channelCode = "VINMART";
        string plainText = request.TransactionId + "|" + channelCode;
        var model = new VinIdCancelApiRequest
        {
            channel_code = channelCode,
            transaction_id = request.TransactionId,
            signature = VinPaySign(plainText, cfg),
        };

        var apiReq = BuildVinIdRequest(cfg, "/internal/checkout/v1/transactions/cancel", JsonConvert.SerializeObject(model));
        var result = await _apiClient.SendAsync(apiReq);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdTransactionApiData>>(result.Response);
        if (resp?.Meta.Code == 200)
            return (HttpStatusCode.OK, "Thành công", new CancelTransactionResultDto { Status = true });

        var (status, msg, _) = BuildVinIdError(resp?.Meta);
        return (status, msg, new CancelTransactionResultDto { Status = false });
    }

    // ── VinIdScanAndGo ────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdScanAndGoAsync(string barcode)
    {
        var cfg = GetScanAndGoCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var endpoint = "/vincart/v1/vcm?barcode=" + barcode;
        var apiReq = new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
        };
        _logger.LogRequest(cfg.Host + endpoint, barcode, "", barcode);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, barcode, result.ResponseTimeMs, "ScanAndGo", result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdScanAndGoData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data != null)
        {
            var d = resp.Data;
            if (d.is_expired == true)
                return (HttpStatusCode.BadRequest, "Đơn hàng đã hết hiệu lực thanh toán", null);
            if (d.status != 1)
                return (HttpStatusCode.BadRequest, "Không thể thực hiện tính tiền cho đơn hàng này", null);

            return (HttpStatusCode.OK, "Thành công", new ScanAndGoPosResponseDto
            {
                OrderNo = d.order_no,
                CustomerId = d.customer_id,
                CustomerName = d.customer_name,
                CustomerPhone = d.customer_phone,
                OrderDate = d.order_date,
                Amount = d.amount,
                AmountDeducted = d.amount_deducted,
                VinIdPaid = d.vin_id_paid,
                AmountCollect = d.amount_collect,
                VoucherId = d.voucher_id,
                VoucherName = d.voucher_name,
                VinIdCardNumber = d.vin_id_card_number,
                VinIdCardName = d.vin_id_card_name,
                DeliveryType = d.delivery_type,
                DeliveryTypeName = d.delivery_type_name,
                Status = d.status,
                Address = d.address,
                Fee = d.fee,
                IsExpired = d.is_expired,
                SiteCode = d.site_code,
                PricingStoreCode = d.pricing_store_code,
                VinIdEarn = d.vin_id_earn,
                HasVatInvoice = d.has_vat_invoice,
                ListPayments = d.payments?.Select(p => new ScanPaymentItem
                {
                    PaymentMethod = p.payment_method,
                    PaidAmount = p.paid_amount,
                    TransactionId = p.transaction_id,
                }).ToList(),
                ListItems = d.items?.Select(i => new ScanItemDto
                {
                    Id = i.id,
                    ArticleId = i.article_id,
                    ArticleName = i.article_name,
                    SoldPrice = i.sold_price,
                    Quantity = i.quantity,
                    UnitCode = i.unit_code,
                    ProductBarcode = i.product_barcode,
                    OriginBarcode = i.origin_barcode,
                    ItemType = i.item_type,
                    VatGroup = i.vat_group,
                    VatPercent = i.vat_percent,
                    NetPrice = i.net_price,
                    PromotionCode = i.product_barcode,
                    PromotionValue = i.promotion_value,
                    UnitQuantity = i.unit_quantity,
                    SaleQuantity = i.sale_quantity,
                    IsFreshfood = i.is_freshfood,
                    UnitSoldPrice = i.unit_sold_price,
                    TotalSoldPrice = i.total_sold_price,
                }).ToList(),
                BillingInfo = d.billing_info == null ? null : new BillInfoDto
                {
                    CustomerName = d.billing_info.customer_name,
                    Address = d.billing_info.address,
                    Email = d.billing_info.email,
                    CompanyName = d.billing_info.company_name,
                    TaxCode = d.billing_info.tax_code,
                    PhoneNumber = d.billing_info.phone_number,
                    SiteCode = d.billing_info.site_code,
                },
            });
        }
        return (HttpStatusCode.BadRequest, resp?.Meta.Message ?? "Lỗi ScanAndGo API", null);
    }

    // ── VinIdUpdateStatusOrder ────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdUpdateStatusOrderAsync(ScanAndGoStatusOrderRequest request)
    {
        var cfg = GetScanAndGoCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var endpoint = $"/vincart/v1/vcm/update-order-status?orderNo={request.ScanAndGoOrderNo}&status={request.Status}&collectedAmount={request.CollectedAmount}";
        var apiReq = new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "PATCH",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
        };
        _logger.LogRequest(cfg.Host + endpoint, request.POSTerminal ?? "", "", JsonConvert.SerializeObject(request));
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, request.POSTerminal ?? "", result.ResponseTimeMs, "UpdateStatusOrder", result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdStatusOrderApiData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data != null)
        {
            var d = resp.Data;
            return (HttpStatusCode.OK, "Thành công", new StatusOrderPosDto
            {
                ReceiptNumber = d.receipt_number,
                TransactionType = d.transaction_type,
                StoreCode = d.store_code,
                PosCode = d.pos_code,
                CashierCode = d.pos_code,
                MerchantId = d.merchant_id,
                TerminalId = d.terminal_id,
                CustomerName = d.customer_name,
                VinidCardNumber = d.vinid_card_number,
                VinidCSN = d.vinid_csn,
                TotalBillAmount = d.total_bill_amount,
                ReferenceNumber = d.reference_number,
                ExtraEarnByItems = d.extra_earn_by_items,
                ExtraEarnByCampaign = d.extra_earn_by_campaign,
                VinIdEarn = d.vin_id_earn,
                IsEarnSuccess = d.is_earn_success,
                OverQuota = d.over_quota,
                BillLines = d.bill_lines?.Select(b => new StatusBillLineDto
                {
                    RecordNo = b.record_no,
                    Barcode = b.barcode,
                    Article = b.article,
                    ArticleName = b.article_name,
                    UOM = b.uom,
                    Quantity = b.quantity,
                    SalePrice = b.sale_price,
                    Amount = b.amount,
                    DiscountAmount = b.discount_amount,
                    LineAmount = b.line_amount,
                    ExtraQuantityEarn = b.extra_quantity_earn,
                    ExtraAmountEarn = b.extra_amount_earn,
                    VCMUnitPrice = b.vcm_unit_price,
                }).ToList(),
            });
        }
        return (HttpStatusCode.BadRequest, resp?.Meta.Message ?? "Lỗi UpdateStatusOrder API", null);
    }

    // ── VinIdExtraSales ───────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdExtraSalesAsync(ExtraSalesPosRequest request)
    {
        var vinIdCfg = GetVinIdCfg();
        var scanGoCfg = GetScanAndGoCfg();
        if (vinIdCfg == null || scanGoCfg == null)
            return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var terminalId = request.PosTerminal;
        var merchantId = request.StoreCode;
        var storeMapping = GetStoreMapping(request.StoreCode ?? "", request.PosTerminal ?? "");
        if (storeMapping != null)
        {
            terminalId = storeMapping.OldVinPayTerminalID;
            merchantId = storeMapping.OldVinPayStoreID;
        }

        var posCode = (request.PosTerminal?.Length > 4) ? request.PosTerminal.Substring(4) : request.PosTerminal;
        long timeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long timeUnixOrderDate = timeUnix;
        try
        {
            var offset = new DateTimeOffset(request.OrderDate);
            timeUnixOrderDate = offset.ToUnixTimeSeconds();
        }
        catch { }

        long totalBill = (long)(request.TotalBillAmount ?? 0);
        string plainText = request.VinidCSN + "|" + request.OrderNo + "|" + request.StoreCode + "|" + posCode + "|" + merchantId + "|" + terminalId + "|" + totalBill + "|" + timeUnix + "|" + request.ReferenceNumber;

        var model = new VinIdExtraSaleApiRequest
        {
            receipt_number = request.OrderNo,
            transaction_type = "0",
            store_code = request.StoreCode,
            pos_code = posCode,
            merchant_id = merchantId,
            terminal_id = terminalId,
            cashier_code = request.CashierCode,
            transaction_time = timeUnix,
            business_time = timeUnixOrderDate,
            customer_name = request.CustomerName,
            vinid_card_number = request.VinidCardNumber,
            vinid_csn = request.VinidCSN,
            total_bill_amount = request.TotalBillAmount,
            reference_number = request.ReferenceNumber,
            signature = VinPaySign(plainText, vinIdCfg),
            bill_lines = request.BillLines?.Select(x => new VinIdExtraBillLine
            {
                record_no = (float)x.RecordNo,
                barcode = x.Barcode,
                article = x.Article,
                article_name = x.ArticleName,
                uom = x.UOM,
                quantity = x.Quantity,
                sale_price = x.SalePrice,
                amount = x.Amount,
                discount_amount = x.DiscountAmount,
                line_amount = x.LineAmount,
            }).ToList(),
        };

        // ExtraSales dùng ScanAndGo host nhưng VINID API Key
        var apiReq = new ApiCallRequest
        {
            Host = scanGoCfg.Host ?? "",
            Endpoint = "/loyalty-extra-point/v1/sale",
            Headers = VinIdHeaders(vinIdCfg),
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(vinIdCfg.Version),
            ProxyHttp = scanGoCfg.HttpProxy, BypassList = scanGoCfg.Bypasslist,
        };
        _logger.LogRequest(scanGoCfg.Host + "/loyalty-extra-point/v1/sale", request.PosTerminal ?? "", "", apiReq.JsonData);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(scanGoCfg.Host!, request.PosTerminal ?? "", result.ResponseTimeMs, "ExtraSales", result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdExtraSaleApiData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data != null)
        {
            var d = resp.Data;
            return (HttpStatusCode.OK, "Thành công", new StatusOrderPosDto
            {
                ReceiptNumber = d.receipt_number,
                TransactionType = d.transaction_type,
                CustomerName = d.customer_name,
                VinidCardNumber = d.vinid_card_number,
                VinidCSN = d.vinid_csn,
                TotalBillAmount = d.total_bill_amount,
                ReferenceNumber = d.reference_number,
                OverQuota = d.over_quota,
                ExtraEarnByItems = d.extra_earn_by_items,
                ExtraEarnByCampaign = d.extra_earn_by_campaign,
                VinIdEarn = d.vin_id_earn,
                BillLines = d.bill_lines?.Select(b => new StatusBillLineDto
                {
                    RecordNo = b.record_no, Barcode = b.barcode, Article = b.article, ArticleName = b.article_name,
                    UOM = b.uom, Quantity = b.quantity, SalePrice = b.sale_price, Amount = b.amount,
                    DiscountAmount = b.discount_amount, LineAmount = b.line_amount,
                    ExtraQuantityEarn = b.extra_quantity_earn, ExtraAmountEarn = b.extra_amount_earn,
                    VCMUnitPrice = b.vcm_unit_price,
                }).ToList(),
            });
        }
        return (HttpStatusCode.BadRequest, resp?.Meta.Message ?? "Lỗi ExtraSales API", null);
    }

    // ── VinIdExtraRefund ──────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdExtraRefundAsync(ExtraRefundPosRequest request)
    {
        var vinIdCfg = GetVinIdCfg();
        var scanGoCfg = GetScanAndGoCfg();
        if (vinIdCfg == null || scanGoCfg == null)
            return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var model = new VinIdExtraRefundApiRequest
        {
            card_number = request.CardNumber,
            customer_name = request.CustomerName,
            receipt_number = request.ReceiptNumber,
            order_no = request.OrderNo,
            store_code = request.StoreCode,
            pos_terminal = request.PosTerminal,
            cashier_code = request.CashierCode,
            total_bill_amount = request.TotalBillAmount,
            original_receipt_number = request.OriginalReceiptNumber,
            original_pos_number = request.OriginalPosNumber,
            original_reference_number = request.OriginalReferenceNumber,
            reference_number = request.ReferenceNumber,
            earn_by_default = request.EarnByDefault,
            refund_amount = request.RefundAmount,
            refund_point_default = request.RefundPointDefault,
            bill_lines = request.BillLines?.Select(x => new VinIdExtraRefundLine
            {
                record_no = x.RecordNo, barcode = x.Barcode, article = x.Article, article_name = x.ArticleName,
                uom = x.UOM, quantity = x.Quantity, refund_quantity = x.RefundQuantity,
                sale_price = x.SalePrice, amount = x.Amount, discount_amount = x.DiscountAmount, line_amount = x.LineAmount,
            }).ToList(),
        };

        var apiReq = new ApiCallRequest
        {
            Host = scanGoCfg.Host ?? "",
            Endpoint = "/loyalty-extra-point/v1/return",
            Headers = VinIdHeaders(vinIdCfg),
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(vinIdCfg.Version),
            ProxyHttp = scanGoCfg.HttpProxy, BypassList = scanGoCfg.Bypasslist,
        };
        _logger.LogRequest(scanGoCfg.Host + "/loyalty-extra-point/v1/return", request.PosTerminal ?? "", "", apiReq.JsonData);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(scanGoCfg.Host!, request.PosTerminal ?? "", result.ResponseTimeMs, "ExtraRefund", result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdExtraSaleApiData>>(result.Response);
        if (resp?.Meta.Code == 200)
            return (HttpStatusCode.OK, "Thành công", resp.Data);

        return (HttpStatusCode.BadRequest, resp?.Meta.Message ?? "Lỗi ExtraRefund API", null);
    }

    // ── VinIdSnGRefund ────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdSnGRefundAsync(ScanAndGoRefundRequest request)
    {
        var cfg = GetScanAndGoCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var model = new VinIdSnGRefundApiRequest
        {
            return_no = request.ReturnNo,
            return_date = DateTime.Now.ToString("ddMMyyyy"),
            return_reason = "order_return",
            note = "Hoàn do hủy/trả sản phẩm",
            redeeming_point = request.RedeemingPoint,
            items = request.Items?.Select(x => new VinIdSnGRefundItem
            {
                note = x.Note,
                order_item_code = x.BarcodeItem,
                order_item_id = x.OrderItemId,
                return_sale_quantity = x.ReturnSaleQuantity,
                return_unit_quantity = x.ReturnUnitQuantity,
            }).ToList(),
        };

        var endpoint = $"/vincart/v1/vcm/order-returns/{request.BarcodeOrder}";
        var apiReq = new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            Headers = new Dictionary<string, string>
            {
                { "Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{cfg.UserName}:{cfg.Password}")) }
            },
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
        };
        _logger.LogRequest(cfg.Host + endpoint, request.POSTerminal ?? "", "", apiReq.JsonData);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, request.POSTerminal ?? "", result.ResponseTimeMs, "SnGRefund", result.Response);

        if (!string.IsNullOrEmpty(result.Response))
            return (HttpStatusCode.OK, "Thành công", ConvertHelper.ToObject<object>(result.Response));

        return (HttpStatusCode.BadRequest, result.Error?.Message ?? "Lỗi SnGRefund API", null);
    }

    // ── VinIdSnGTopup ─────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdSnGTopupAsync(ScanAndGoTopupRequest request)
    {
        var cfg = GetScanAndGoCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var model = new VinIdSnGTopupApiRequest
        {
            topup_no = request.TopupNo,
            topup_data = request.TopupData?.Select(x => new VinIdTopupItem { amount = x.Amount, type = x.Type }).ToList(),
        };

        var endpoint = $"/vincart-vinmart-order/v2/vcm/{request.BarcodeOrder}/topup";
        var apiReq = new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            Headers = new Dictionary<string, string>
            {
                { "Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{cfg.UserName}:{cfg.Password}")) }
            },
            WebMethod = "POST",
            JsonData = JsonConvert.SerializeObject(model),
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
        };
        _logger.LogRequest(cfg.Host + endpoint, request.POSTerminal ?? "", "", apiReq.JsonData);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, request.POSTerminal ?? "", result.ResponseTimeMs, "SnGTopup", result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<object>>(result.Response);
        if (resp?.Meta.Code == 200)
            return (HttpStatusCode.OK, "Thành công", resp.Data);

        return (HttpStatusCode.BadRequest, resp?.Meta.Message ?? "Lỗi SnGTopup API", null);
    }

    // ── VinIdTransactionEnquiry (SOAP) ────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> VinIdTransactionEnquiryAsync(
        string memberCard, string pageNo, string totalRecordPerPage, string startDate, string endDate)
    {
        var cfg = GetWinLifeCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        try
        {
            // Login để lấy access token (cache theo ngày)
            var cacheKey = "WinLife_AccessToken_" + DateTime.Now.ToString("yyyyMMdd");
            var accessToken = _cache.Get<string>(cacheKey) ?? await LoginSoapAsync(cfg, cacheKey);
            if (string.IsNullOrEmpty(accessToken))
                return (HttpStatusCode.InternalServerError, "Không thể đăng nhập SOAP OneFlexiAxis", null);

            var date = DateTime.Now.ToString("yyyyMMdd");
            var time = DateTime.Now.ToString("HHmmss");
            var merchantId = cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "MerchantId")?.Notes ?? "";
            var terminalId = cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "TerminalId")?.Notes ?? "";
            var timeZone  = cfg.Description ?? "UTC+7";
            var versionNo = cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "VersionNo")?.Notes ?? "1.0";
            var appVersion= cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "AppVersion")?.Notes ?? "1.0";

            var xmlRq = $@"<TransactionEnquiryRq>
  <RqHeader>
    <AccessToken>{accessToken}</AccessToken>
    <Date>{date}</Date>
    <Time>{time}</Time>
    <TimeZone>{timeZone}</TimeZone>
    <MessageType>BALANCE_ENQUIRY</MessageType>
    <VersionNo>{versionNo}</VersionNo>
    <AppVersion>{appVersion}</AppVersion>
    <MerchantId>{merchantId}</MerchantId>
    <TerminalId>{terminalId}</TerminalId>
    <ChannelId>POS</ChannelId>
    <LastHostRefNo></LastHostRefNo>
    <OTP></OTP>
  </RqHeader>
  <CustomerIdentifier>
    <IDType>CARD</IDType>
    <ID>{memberCard}</ID>
  </CustomerIdentifier>
  <PageNo>{pageNo}</PageNo>
  <TotalRecordPerPage>{totalRecordPerPage}</TotalRecordPerPage>
  <StartDate>{startDate ?? ""}</StartDate>
  <EndDate>{endDate ?? ""}</EndDate>
</TransactionEnquiryRq>";

            var responseXml = await _soapClient.ProcessRequestAsync(xmlRq, "", "", "");

            // Kiểm tra ResponseCode == "00"
            if (responseXml.Contains("<ResponseCode>00</ResponseCode>"))
                return (HttpStatusCode.OK, "Thành công", responseXml);

            // Lấy ErrorMessage từ XML
            var errorStart = responseXml.IndexOf("<ErrorMessage>");
            var errorEnd   = responseXml.IndexOf("</ErrorMessage>");
            var errorMsg = (errorStart >= 0 && errorEnd > errorStart)
                ? responseXml.Substring(errorStart + 14, errorEnd - errorStart - 14)
                : "Lỗi TransactionEnquiry";
            return (HttpStatusCode.BadRequest, errorMsg, null);
        }
        catch (Exception ex)
        {
            return (HttpStatusCode.InternalServerError, ex.Message, null);
        }
    }

    // ── Private core methods ──────────────────────────────────────────────

    private async Task<(HttpStatusCode Status, string Message, object? Data)> GetVinIdMemberCoreAsync(
        string numberCard, string posId, string storeNo, bool isLog)
    {
        var cfg = GetVinIdCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        numberCard = numberCard.Trim();

        // Validate ký tự hợp lệ (chỉ alphanumeric)
        if (!System.Text.RegularExpressions.Regex.IsMatch(numberCard, "^[a-zA-Z0-9 ]*$"))
            return (HttpStatusCode.BadRequest, "Mã thẻ VINID hoặc QR VINPAY không hợp lệ, vui lòng scan lại thông tin", null);

        if (StringHelper.Left(numberCard, 4) != "8888" && StringHelper.Left(numberCard, 5) != "66668" && StringHelper.Left(numberCard, 1).ToUpper() != "P")
            return (HttpStatusCode.BadRequest, "Mã thẻ VINID hoặc QR VINPAY không hợp lệ, vui lòng scan lại thông tin", null);

        // Kiểm tra off/on VINID GetInfo
        if (StringHelper.Left(numberCard, 4) == "8888" || StringHelper.Left(numberCard, 5) == "66668")
        {
            var (isOff, offMsg) = LoyaltyHelper.IsOffVinID(_cache, "GetInfo");
            if (isOff) return (HttpStatusCode.BadRequest, offMsg, null);
        }

        if (string.IsNullOrEmpty(storeNo))
            return (HttpStatusCode.BadRequest, "StoreNo đang bị trống", null);
        if (string.IsNullOrEmpty(posId))
            return (HttpStatusCode.BadRequest, "PosID đang bị trống", null);

        var posStoreNo = storeNo;
        var posTerminal = posId;
        var storeMapping = GetStoreMapping(storeNo, posId);
        if (storeMapping != null)
        {
            posId = storeMapping.OldTerminalID ?? posId;
            storeNo = storeMapping.OldStoreID ?? storeNo;
        }

        var model = new VinIdInfoMemberRequest
        {
            code_value = numberCard,
            merchant_id = storeNo,
            terminal_id = posId,
            date = DateTime.Now.ToString("yyyyMMdd"),
            time = DateTime.Now.ToString("HHmmss"),
            time_zone = "GMT+07:00",
        };

        var apiReq = BuildVinIdRequest(cfg, "/internal/checkout/v1/get-loyalty-information", JsonConvert.SerializeObject(model));
        _logger.LogRequest(cfg.Host + "/internal/checkout/v1/get-loyalty-information", posTerminal, "", numberCard);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, posTerminal, result.ResponseTimeMs, "GetInfoMember", numberCard + "@" + result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdInfoMemberData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data?.oe_info != null)
        {
            var oe = resp.Data.oe_info;
            var memberPoint = ParsePoolUnits(oe.member_balance?.pool?.redeemable_pool_units);
            var totalPoint  = ParsePoolUnits(oe.member_balance?.pool?.total_pool_units);

            var cardnum = "";
            var qrCode = "";
            var cardVirtual = "";

            var accounts = oe.member_accounts?.OrderByDescending(a => a.account_no).ToList() ?? new();
            foreach (var acc in accounts)
            {
                if ((StringHelper.Left(acc.account_no, 4) == "8888" || StringHelper.Left(acc.account_no, 5) == "66668")
                    && acc.account_status == "A" && acc.block_code == "0")
                {
                    cardnum = acc.account_no;
                    break;
                }
            }

            if (string.IsNullOrEmpty(cardnum))
                return (HttpStatusCode.BadRequest, "Không tồn tại tài khoản hoặc thẻ đã bị khóa, vui lòng liên hệ DVKH để kiểm tra", null);

            if (StringHelper.Left(numberCard, 1).ToUpper() == "P") qrCode = numberCard;
            if (StringHelper.Left(numberCard, 5) == "66668") cardVirtual = numberCard;

            var phoneNumber = oe.member_profile?.contact_details?.FirstOrDefault(d => !string.IsNullOrEmpty(d.mobile_number))?.mobile_number ?? "";

            return (HttpStatusCode.OK, "Thành công", new InfoMemberDto
            {
                CardNumber   = cardnum,
                VirtualCard  = string.IsNullOrEmpty(cardVirtual) ? cardnum : cardVirtual,
                CMND         = oe.member_profile?.client_id_1,
                MemberName   = oe.member_profile?.full_name,
                CardLevel    = accounts.FirstOrDefault()?.account_type,
                MemberCSN    = oe.member_profile?.csn,
                PhoneNumber  = phoneNumber,
                QRCode       = qrCode,
                MemberPoint  = memberPoint,
                TotalPoint   = totalPoint,
                ExtraPoint   = oe.has_extra_point,
                CurrentRate  = 10,
                IsOfflineVinID = false,
                IsRedeem     = true,
                Status       = "Hoạt động",
                System       = "OE",
            });
        }

        // Xử lý các error code đặc biệt
        return MapVinIdMemberError(resp?.Meta, numberCard);
    }

    private (HttpStatusCode Status, string Message, object? Data) VinIdSalesCore(
        string? qrCode, string cardNumber, string? merchantId, string? terminalId,
        string? invoiceNo, int spendPoints, decimal billAmount,
        string orderNo, bool isOffline, decimal orderAmount,
        string? virtualCard, List<AmountToEarnItem>? listAmountEarn)
    {
        var cfg = GetVinIdCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var posStoreNo = merchantId;
        var posTerminal = terminalId;
        var storeMapping = GetStoreMapping(merchantId ?? "", terminalId ?? "");
        if (storeMapping != null)
        {
            terminalId = storeMapping.OldTerminalID ?? terminalId;
            merchantId = storeMapping.OldStoreID ?? merchantId;
        }

        // Kiểm tra off/on EarnPoint & SpendPoints
        var (earnOff, _) = LoyaltyHelper.IsOffVinID(_cache, "EarnPoint");
        if (earnOff) { billAmount = 0; orderAmount = 0; }

        var (spendOff, spendMsg) = LoyaltyHelper.IsOffVinID(_cache, "SpendPoints");
        if (spendOff)
            return (HttpStatusCode.ServiceUnavailable, spendMsg, null);

        var channelCode = "VINMART";
        var dateTime = DateTime.Now.ToString("yyMMddHHmmssff");
        var transRefNum = invoiceNo + "_" + dateTime;
        string plainText = "|" + cardNumber + "|" + channelCode + "|" + merchantId + "|" + terminalId + "|" + transRefNum + "|" + invoiceNo + "|" + billAmount + "|" + spendPoints;

        var model = new VinIdSalesApiRequest
        {
            transaction_ref_number = transRefNum,
            description = "Sale Points",
            code_value = cardNumber,
            date = DateTime.Now.ToString("yyyyMMdd"),
            time = DateTime.Now.ToString("HHmmss"),
            time_zone = "GMT+07:00",
            merchant_id = merchantId,
            terminal_id = terminalId,
            channel_code = channelCode,
            invoice_no = invoiceNo,
            spend_points = spendPoints.ToString(),
            bill_amount = billAmount.ToString(),
            signature = VinPaySign(plainText, cfg),
        };

        var apiReq = BuildVinIdRequest(cfg, "/internal/checkout/v1/sale-loyalty-points", JsonConvert.SerializeObject(model));
        _logger.LogRequest(cfg.Host + "/internal/checkout/v1/sale-loyalty-points", posTerminal ?? "", "", cardNumber);
        var result = _apiClient.SendAsync(apiReq).GetAwaiter().GetResult();
        _logger.LogResponse(cfg.Host!, posTerminal ?? "", result.ResponseTimeMs, "Sales", cardNumber + "@" + result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdSalesData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data != null)
        {
            var d = resp.Data;
            var poolUnits   = d.member_balance?.awards?.Sum(a => ParseDouble(a.pool_units)) ?? 0;
            var redeemUnits = d.redeem_pool_list?.Sum(r => ParseDouble(r.pool_units_to_redeem)) ?? 0;

            var extraList = listAmountEarn?.Select(a => new PointEarnCampaignItem
            {
                LoyaltyMerchantId = "VCM",
                Amount = a.amount,
                EarnedPoints = poolUnits,
            }).ToList();

            return (HttpStatusCode.OK, "Thành công", new PointDto
            {
                PointEarn = (int)poolUnits,
                PointRedeem = (int)redeemUnits,
                CurrentRate = 10,
                IsOfflineVinID = false,
                OrderNo = orderNo,
                ExtraEarnByCampaign = extraList,
            });
        }
        return BuildVinIdError(resp?.Meta);
    }

    private (HttpStatusCode Status, string Message, object? Data) VinIdRefundCore(
        string? qrCode, string cardNumber, string? merchantId, string? terminalId,
        string? invoiceNo, string? orderNo, string origOrderNo,
        int spendPoints, decimal refundAmount, bool isOffline,
        decimal orderAmount, bool isScanAndGo, List<CxRefundAmountItem>? listAmountEarn)
    {
        var cfg = GetVinIdCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);

        var posStoreNo = merchantId;
        var posTerminal = terminalId;
        var storeMapping = GetStoreMapping(merchantId ?? "", terminalId ?? "");
        if (storeMapping != null)
        {
            terminalId = storeMapping.OldTerminalID ?? terminalId;
            merchantId = storeMapping.OldStoreID ?? merchantId;
        }

        var channelCode = "VINMART";
        var dateTime = DateTime.Now.ToString("yyMMddHHmmssff");
        var transRefNum = invoiceNo + "_" + dateTime;
        string plainText = "|" + cardNumber + "|" + channelCode + "|" + merchantId + "|" + terminalId + "|" + transRefNum + "|" + invoiceNo + "|" + origOrderNo + "|" + refundAmount + "|" + spendPoints;

        var model = new VinIdRefundApiRequest
        {
            transaction_ref_number = transRefNum,
            description = "Refund Points",
            code_value = cardNumber,
            date = DateTime.Now.ToString("yyyyMMdd"),
            time = DateTime.Now.ToString("HHmmss"),
            time_zone = "GMT+07:00",
            merchant_id = merchantId,
            terminal_id = terminalId,
            channel_code = channelCode,
            invoice_no = invoiceNo,
            refund_ref_number = origOrderNo,
            spend_points = spendPoints.ToString(),
            refund_amount = refundAmount.ToString(),
            signature = VinPaySign(plainText, cfg),
        };

        var apiReq = BuildVinIdRequest(cfg, "/internal/checkout/v1/refund-loyalty-points", JsonConvert.SerializeObject(model));
        _logger.LogRequest(cfg.Host + "/internal/checkout/v1/refund-loyalty-points", posTerminal ?? "", "", cardNumber);
        var result = _apiClient.SendAsync(apiReq).GetAwaiter().GetResult();
        _logger.LogResponse(cfg.Host!, posTerminal ?? "", result.ResponseTimeMs, "Refund", cardNumber + "@" + result.Response);

        var resp = ConvertHelper.ToObject<VinIdApiResponse<VinIdSalesData>>(result.Response);
        if (resp?.Meta.Code == 200 && resp.Data != null)
        {
            var d = resp.Data;
            var poolUnits   = d.member_balance?.awards?.Sum(a => ParseDouble(a.pool_units)) ?? 0;
            var redeemUnits = d.redeem_pool_list?.Sum(r => ParseDouble(r.pool_units_to_redeem)) ?? 0;
            return (HttpStatusCode.OK, "Thành công", new PointDto
            {
                PointEarn = (int)poolUnits,
                PointRedeem = (int)redeemUnits,
                CurrentRate = 10,
                IsOfflineVinID = false,
                OrderNo = orderNo,
            });
        }
        return BuildVinIdError(resp?.Meta);
    }

    // ── Capillary generic call ────────────────────────────────────────────

    private async Task<(HttpStatusCode Status, string Message, object? Data)> GetCapillaryMemberAsync(
        string numberCard, string storeNo, string posId, string memberType,
        string clubCode = "", bool isMobile = false)
    {
        var cfg = GetCapillaryCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);
        var router = cfg.SysWebApiRoute?.FirstOrDefault(x => string.Equals(x.Name, "GetInfoMember", StringComparison.OrdinalIgnoreCase) && !x.Blocked);
        if (router == null) return (HttpStatusCode.InternalServerError, MessageConst.ApiNotFounDataConfig, null);

        var payload = JsonConvert.SerializeObject(new { numberCard, storeNo, posId, memberType, clubCode });
        var apiReq = new ApiCallRequest
        {
            Host = cfg.Host ?? "", Endpoint = router.Route ?? "",
            Headers = new Dictionary<string, string> { { "X-API-KEY", cfg.UserName ?? "" } },
            TokenType = TokenTypeEnum.Basic.ToString(), WebMethod = "POST",
            JsonData = payload,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version), ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
        };
        _logger.LogRequest(cfg.Host + router.Route, posId, "", numberCard);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, posId, result.ResponseTimeMs, "GetInfoMember", numberCard + "@" + result.Response);

        if (string.IsNullOrEmpty(result.Response))
            return (HttpStatusCode.BadRequest, "Không có phản hồi từ Capillary API", null);

        var dto = ConvertHelper.ToObject<InfoMemberDto>(result.Response);
        return (HttpStatusCode.OK, "OK", dto);
    }

    private async Task<(HttpStatusCode Status, string Message, object? Data)> CallCapillaryAsync(
        string routeName, string jsonData, string cardNumber, string posId)
    {
        var cfg = GetCapillaryCfg();
        if (cfg == null) return (HttpStatusCode.InternalServerError, MessConst.NotFounDataConfig, null);
        var router = cfg.SysWebApiRoute?.FirstOrDefault(x => string.Equals(x.Name, routeName, StringComparison.OrdinalIgnoreCase) && !x.Blocked);
        if (router == null) return (HttpStatusCode.InternalServerError, MessageConst.ApiNotFounDataConfig, null);

        var apiReq = new ApiCallRequest
        {
            Host = cfg.Host ?? "", Endpoint = router.Route ?? "",
            Headers = new Dictionary<string, string> { { "X-API-KEY", cfg.UserName ?? "" } },
            TokenType = TokenTypeEnum.Basic.ToString(), WebMethod = "POST",
            JsonData = jsonData,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version), ProxyHttp = cfg.HttpProxy, BypassList = cfg.Bypasslist,
        };
        _logger.LogRequest(cfg.Host + router.Route, posId, "", cardNumber + "@" + jsonData);
        var result = await _apiClient.SendAsync(apiReq);
        _logger.LogResponse(cfg.Host!, posId, result.ResponseTimeMs, routeName, cardNumber + "@" + result.Response);

        if (string.IsNullOrEmpty(result.Response))
            return (HttpStatusCode.BadRequest, "Không có phản hồi từ Capillary", null);

        return (HttpStatusCode.OK, "OK", ConvertHelper.ToObject<object>(result.Response));
    }

    // ── SOAP login helper ─────────────────────────────────────────────────

    private async Task<string?> LoginSoapAsync(SysWebApiDto cfg, string cacheKey)
    {
        var merchantId = cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "MerchantId")?.Notes ?? "";
        var terminalId = cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "TerminalId")?.Notes ?? "";
        var operatorId = cfg.UserName ?? "";
        var password   = cfg.Password ?? "";
        var timeZone   = cfg.Description ?? "UTC+7";
        var versionNo  = cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "VersionNo")?.Notes ?? "1.0";
        var appVersion = cfg.SysWebApiRoute?.FirstOrDefault(x => x.Name == "AppVersion")?.Notes ?? "1.0";

        var loginXml = $@"<OperatorLoginRq>
  <RqHeader>
    <Date>{DateTime.Now:yyyyMMdd}</Date>
    <Time>{DateTime.Now:HHmmss}</Time>
    <TimeZone>{timeZone}</TimeZone>
    <MessageType>LGN</MessageType>
    <VersionNo>{versionNo}</VersionNo>
    <AppVersion>{appVersion}</AppVersion>
    <MerchantId>{merchantId}</MerchantId>
    <TerminalId>{terminalId}</TerminalId>
    <ChannelId>POS</ChannelId>
    <LastHostRefNo></LastHostRefNo>
    <OTP></OTP>
  </RqHeader>
  <CustomerIdentifier>
    <IDType>OPERATOR</IDType>
    <ID>{operatorId}</ID>
  </CustomerIdentifier>
  <LoginInfo>
    <Mode>O</Mode>
    <Password>{password}</Password>
  </LoginInfo>
</OperatorLoginRq>";

        var responseXml = await _soapClient.ProcessRequestAsync(loginXml, "", "", "");
        var tokenStart = responseXml.IndexOf("<AccessToken>");
        var tokenEnd   = responseXml.IndexOf("</AccessToken>");
        if (tokenStart < 0 || tokenEnd <= tokenStart) return null;

        var token = responseXml.Substring(tokenStart + 13, tokenEnd - tokenStart - 13);
        if (!string.IsNullOrEmpty(token))
            _cache.Set(cacheKey, token);
        return token;
    }

    // ── Utility helpers ───────────────────────────────────────────────────

    private ApiCallRequest BuildVinIdRequest(SysWebApiDto cfg, string endpoint, string? body, string method = "POST") =>
        new()
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            Headers = VinIdHeaders(cfg),
            TokenType = TokenTypeEnum.Basic.ToString(),
            WebMethod = method,
            JsonData = body,
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
            ProxyHttp = cfg.HttpProxy,
            BypassList = cfg.Bypasslist,
        };

    private static (HttpStatusCode Status, string Message, object? Data) BuildVinIdError(VinIdMeta? meta)
    {
        if (meta == null) return (HttpStatusCode.InternalServerError, "Lỗi hệ thống VINID", null);
        var msg = meta.Message ?? "Lỗi";
        var parts = msg.Split('\t');
        if (parts.Length > 1) msg = parts[1];
        return (HttpStatusCode.Conflict, msg, null);
    }

    private static (HttpStatusCode Status, string Message, object? Data) MapVinIdMemberError(VinIdMeta? meta, string card)
    {
        return meta?.Code switch
        {
            81910018 => (HttpStatusCode.Conflict, $"Thẻ VINID {card} chưa được đăng ký", null),
            81910007 => (HttpStatusCode.Conflict, $"Thẻ VINID {card} không tồn tại", null),
            81910010 => (HttpStatusCode.Conflict, $"Thẻ VINID {card} đã hết hạn. Vui lòng scan lại thẻ VINID", null),
            81910011 => (HttpStatusCode.Conflict, $"Thẻ VINID {card} đã bị khóa, vui lòng liên hệ DVKH để kiểm tra", null),
            _ => BuildVinIdError(meta),
        };
    }

    private static long ParsePoolUnits(string? value)
        => string.IsNullOrEmpty(value) ? 0 : (long)Convert.ToDecimal(value);

    private static double ParseDouble(string? value)
        => string.IsNullOrEmpty(value) ? 0 : Convert.ToDouble(value);
}
