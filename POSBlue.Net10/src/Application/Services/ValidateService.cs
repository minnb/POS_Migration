using System.Net;
using Newtonsoft.Json;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Application.Services;

/// <summary>
/// Toàn bộ use case ValidateController — port logic từ ValidateController + DataRawService + MemberBusinessService cũ.
/// </summary>
public sealed class ValidateService : IValidateService
{
    private const string RedisKeyCheck = "InvoiceCreated:CHECK_";
    private const string RedisKeyPrimary = "InvoiceCreated:PRIMARY_";
    private const string MemberBusinessParent = "MemberBusiness";

    private readonly IMemoryCacheService _cache;
    private readonly IApiCallClient _apiClient;
    private readonly ISmsService _smsService;
    private readonly IValidateRepository _validateRepo;
    private readonly IRedisService _redis;

    public ValidateService(
        IMemoryCacheService cache,
        IApiCallClient apiClient,
        ISmsService smsService,
        IValidateRepository validateRepo,
        IRedisService redis)
    {
        _cache = cache;
        _apiClient = apiClient;
        _smsService = smsService;
        _validateRepo = validateRepo;
        _redis = redis;
    }

    // ── Telegram send ─────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> TelegramSendAsync(
        MessageTelegramRequest request)
    {
        await _smsService.SendMessageSmsAsync(request.Message, request.MessageType, request.MessageType);
        return (HttpStatusCode.OK, "Gửi thành công", null);
    }

    // ── ValidateTaxCode ───────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> ValidateTaxCodeAsync(string taxCode)
    {
        var cfg = _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
                        .FirstOrDefault(x => string.Equals(x.AppCode, "PARTNER", StringComparison.OrdinalIgnoreCase));
        if (cfg == null)
            return (HttpStatusCode.Conflict, "Chưa khai báo webApi VietQR", null);

        var route = cfg.SysWebApiRoute?
            .FirstOrDefault(x => string.Equals(x.Name, "ValidateTaxCode", StringComparison.OrdinalIgnoreCase));
        var endpoint = (route?.Route ?? "") + "/" + taxCode;

        var req = new ApiCallRequest
        {
            Host = cfg.Host ?? "",
            Endpoint = endpoint,
            WebMethod = "GET",
            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
        };
        var result = await _apiClient.SendAsync(req);

        if (!string.IsNullOrEmpty(result.Response))
        {
            var resp = ConvertHelper.ToObject<PartnerApiResponse<TaxCustInfo>>(result.Response);
            if (resp?.Meta.Code == 200 && resp.Data != null)
                return (HttpStatusCode.OK, "Success", resp.Data);
            if (resp?.Meta.Code == 9998)
                return (HttpStatusCode.Conflict, "Có lỗi gọi webApi VietQR", null);
            return (HttpStatusCode.NotFound, resp?.Meta.Message ?? "", null);
        }

        return (HttpStatusCode.Conflict, result.Error?.Message ?? "Lỗi gọi PARTNER API", null);
    }

    // ── ValidateTransaction ───────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> ValidateTransactionAsync(string orderNo)
    {
        // Format check
        if (orderNo.Length != 15)
            return (HttpStatusCode.BadRequest, "Phiếu tính tiền không đúng", null);
        if (!orderNo.All(char.IsLetterOrDigit))
            return (HttpStatusCode.BadRequest, "Phiếu tính tiền không đúng định dạng", null);

        // Store check
        var storeNo = StringHelper.Left(orderNo, 4);
        var store = _cache.GetStores()?.FirstOrDefault(x => x.StoreNo == storeNo);
        if (store == null)
            return (HttpStatusCode.Conflict,
                $"Thông tin phiếu tính tiền {orderNo} không hợp lệ", null);

        // PRINTEINVOICENUMBER setup check
        var storeSetup = _cache.GetStoreSetup();
        if (storeSetup?.Count > 0)
        {
            var isAll = storeSetup.Any(x =>
                string.Equals(x.Code, "PRINTEINVOICENUMBER", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Value, "ALL", StringComparison.OrdinalIgnoreCase));
            if (!isAll)
            {
                var allowed = storeSetup.Any(x =>
                    string.Equals(x.Code, "PRINTEINVOICENUMBER", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.StoreNo, store.StoreNo, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.Value, "YES", StringComparison.OrdinalIgnoreCase) &&
                    x.Status);
                if (!allowed)
                    return (HttpStatusCode.BadRequest,
                        $"Mã cửa hàng {store.StoreNo} " + GetVatMessage("NotExistsStore"), null);
            }
        }

        // DB queries
        var (header, lines, cashSent, existingTemp) =
            await _validateRepo.GetTransactionDataAsync(store.ConnectionString!, orderNo);

        // Already submitted to EInvoice_Temp
        if (existingTemp != null)
        {
            string mes = GetVatMessage("ExistsTemp");
            mes += !string.IsNullOrEmpty(existingTemp.TaxCode)
                ? $" cho MST {existingTemp.TaxCode}"
                : $" cho khách hàng {existingTemp.CustomerName}";

            if (cashSent != null)
            {
                var vatInfo = new ValidateTransactionDto
                {
                    OrderNo = orderNo,
                    IsEOD = cashSent.IsEOD,
                    Key = cashSent.Key,
                    CompTaxCode = cashSent.CompTaxCode,
                };
                return (HttpStatusCode.Created, GetVatMessage("ExistsEOD"), vatInfo);
            }
            return (HttpStatusCode.BadRequest, mes, null);
        }

        // Transaction not found
        if (header == null)
            return (HttpStatusCode.BadRequest, GetVatMessage("NotExistsTrans"), null);

        // Return order
        if (header.SalesIsReturn > 0)
            return (HttpStatusCode.BadRequest, GetVatMessage("OrderReturned"), null);

        // Already sent to EOD cash table
        if (cashSent != null)
        {
            header.IsEOD = cashSent.IsEOD;
            header.Key = cashSent.Key;
            header.CompTaxCode = cashSent.CompTaxCode;
            header.TransLine = lines;
            header.StoreNo = store.StoreNo;
            header.StoreName = store.Name;
            header.Address = store.Address;
            header.TotalAmount = lines.Sum(x => x.LineAmountIncVAT);
            return (HttpStatusCode.Created, GetVatMessage("ExistsEOD"), header);
        }

        // Time limit check
        var minuteSetup = _cache.GetPOSDataSetup("WWW_INVOICE_MINUTE");
        int minuteLimit = int.TryParse(minuteSetup?.Value, out var mv) ? mv : 60;
        if ((int)(DateTime.Now - header.CreatedDate).TotalMinutes > minuteLimit)
            return (HttpStatusCode.BadRequest, GetVatMessage("OverTime"), null);

        // Date check
        if (header.OrderDate.Date < DateTime.Now.Date)
            return (HttpStatusCode.BadRequest, GetVatMessage("OverDate"), null);

        // Build result and set Redis check key
        header.TransLine = lines;
        header.StoreNo = store.StoreNo;
        header.StoreName = store.Name;
        header.Address = store.Address;
        header.TotalAmount = lines.Sum(x => x.LineAmountIncVAT);
        header.IsEOD = false;
        header.Key = "";
        header.CompTaxCode = "";

        var redisKey = RedisKeyCheck + orderNo;
        if (!_redis.CheckKeyExists(redisKey))
            _redis.StringSetString(redisKey, JsonConvert.SerializeObject(header), 3660);

        return (HttpStatusCode.OK, "OK", header);
    }

    // ── InvoiceCreate ─────────────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> InvoiceCreateAsync(
        InvoiceCreatedRequest request)
    {
        // Store check
        var store = _cache.GetStores()?.FirstOrDefault(x => x.StoreNo == request.SiteNo);
        if (store == null)
            return (HttpStatusCode.BadRequest,
                $"Không tìm thấy cấu hình StoreSet của mã storeNo {request.SiteNo}", null);

        // All orderNo must share the same 4-char prefix as SiteNo
        var badOrder = request.OrderNo.FirstOrDefault(o => StringHelper.Left(o, 4) != request.SiteNo);
        if (badOrder != null)
            return (HttpStatusCode.BadRequest, "Danh sách phiếu tính tiền xuất hóa đơn không hợp lệ", null);

        // Build invoice items with Redis pre-checks
        string batchId = Guid.NewGuid().ToString();
        var items = new List<InvoiceCreatedItem>();
        foreach (var orderNo in request.OrderNo)
        {
            // Must have been validated first
            if (!_redis.CheckKeyExists(RedisKeyCheck + orderNo))
                return (HttpStatusCode.BadRequest,
                    $"Phiếu tính tiền {orderNo} không hợp lệ", null);

            // Duplicate check
            var dupCheck = await _redis.GetStringAsync(RedisKeyPrimary + orderNo);
            if (!string.IsNullOrEmpty(dupCheck))
                return (HttpStatusCode.BadRequest,
                    $"Phiếu tính tiền {orderNo} đã được nhập thông tin HD GTGT", null);

            items.Add(new InvoiceCreatedItem
            {
                OrderNo = orderNo,
                SiteNo = request.SiteNo,
                CompanyName = request.CompanyName ?? "",
                CustomerName = request.CustomerName ?? "",
                TaxCode = request.TaxCode ?? "",
                Address = request.Address ?? "",
                PhoneNumber = request.PhoneNumber ?? "",
                Email = request.Email ?? "",
                IsNotVAT = false,
                IsNotVATPartner = "WCM",
                Id = batchId,
                Passport = request.Passport ?? "",
                CCCD = request.CCCD ?? "",
                DVQHNS = request.DVQHNS ?? "",
            });
        }

        var (success, message, technical) =
            await _validateRepo.InsertInvoiceCreatedAsync(items, store.ConnectionString!);

        if (success)
        {
            // Clear check keys after successful insert
            _ = Task.Run(() =>
            {
                foreach (var item in items)
                    _redis.Delete(RedisKeyCheck + item.OrderNo);
            });

            // Fire-and-forget: notify PARTNER
            _ = Task.Run(async () =>
            {
                try
                {
                    var cfg = _cache.GetSysWebApi(_cache.GetConnectStringCentralMD())?
                                    .FirstOrDefault(x => string.Equals(x.AppCode, "PARTNER",
                                        StringComparison.OrdinalIgnoreCase));
                    if (cfg != null)
                    {
                        var route = cfg.SysWebApiRoute?
                            .FirstOrDefault(x => string.Equals(x.Name, "UpdateTaxInfo",
                                StringComparison.OrdinalIgnoreCase));
                        await _apiClient.SendAsync(new ApiCallRequest
                        {
                            Host = cfg.Host ?? "",
                            Endpoint = route?.Route ?? "",
                            WebMethod = "POST",
                            JsonData = JsonConvert.SerializeObject(items.FirstOrDefault()),
                            Timeout = NumberHelper.GetTimeOutApi(cfg.Version),
                        });
                    }
                }
                catch { /* fire-and-forget: không chặn luồng chính */ }
            });

            return (HttpStatusCode.OK, "Success", null);
        }

        return (HttpStatusCode.BadRequest, message, technical);
    }

    // ── ValidateMemberBusiness ────────────────────────────────────────────────

    public async Task<(HttpStatusCode Status, string Message, object? Data)> ValidateMemberBusinessAsync(
        ValidateMemberBusinessRequest request)
    {
        request.MemberCard = FormatHelper.PhoneNumberVietNam(request.MemberCard);

        // Verify Key contains StoreNo_PosNo_MemberCard
        var decodedKey = EncryptionHelper.Base64Decode(request.Key);
        if (string.IsNullOrEmpty(decodedKey) ||
            !decodedKey.Contains(string.Join("_", request.StoreNo, request.PosNo, request.MemberCard)))
        {
            return (HttpStatusCode.BadRequest,
                $"Key không đúng của máy POS {request.PosNo}", null);
        }

        // Check Redis for existing session
        var allKeys = _redis.GetAllKeys($"{MemberBusinessParent}:*");
        bool sessionFound = allKeys.Contains(decodedKey);

        if (sessionFound)
        {
            var cachedData = await _redis.StringGetAsync<MemberBusinessData>(decodedKey);
            if (cachedData != null)
            {
                // Check if another POS has an active session for this member
                var existsOther = FindOtherSession(allKeys, decodedKey, request.MemberCard);
                if (existsOther != null && existsOther.KeyExists != cachedData.Key)
                    cachedData.ExistsProcessing = existsOther;

                return (HttpStatusCode.OK, "OK", new CheckMemberBusinessData
                {
                    IsChecked = true,
                    Description = "Key hợp lệ cho việc thanh toán đơn hàng",
                    MemberBusinessData = cachedData,
                });
            }
        }

        // Session not found — query DB and build new session
        var connStr = _cache.GetStringDbLoyalty();
        var memberDb = await _validateRepo.GetMemberBusinessAsync(request.MemberCard, connStr);
        if (memberDb == null)
        {
            return (HttpStatusCode.OK, "OK", new CheckMemberBusinessData
            {
                IsChecked = false,
                Description = "Key không hợp lệ, lấy thông tin HVDN thất bại, vui lòng thử lại",
                MemberBusinessData = null,
            });
        }

        // Different store — no quota access
        if (memberDb.StoreNo != request.StoreNo)
        {
            return (HttpStatusCode.OK, "OK", new CheckMemberBusinessData
            {
                IsChecked = false,
                Description = "Key không hợp lệ, vui lòng sử dụng key mới",
                MemberBusinessData = new MemberBusinessData
                {
                    CardLevel = memberDb.CardLevel,
                    Month = DateTime.Now.ToString("yyyyMM"),
                    IsDiscount = false,
                    MemberCard = request.MemberCard,
                    MemberStore = memberDb.MemberStore,
                },
            });
        }

        // Build full session data with quotas
        var month = DateTime.Now.ToString("yyyyMM");
        var itemsDb = await _validateRepo.GetMemberBusinessItemsAsync(request.MemberCard, month, connStr);

        var quotas = itemsDb.Select(i => new MemberBusinessItemQuota
        {
            ItemNo = i.ItemNo,
            Uom = i.Uom,
            MaxValue = i.MaxValue,
            UsedValue = i.UsedValue,
            RemnValue = i.RemnValue,
        }).ToList();

        // Clear any existing keys for same POS+member before creating new session
        foreach (var k in allKeys.Where(k => k.Contains(string.Join("_", request.StoreNo, request.PosNo, request.MemberCard))))
            _redis.Delete(k);

        var subKey = string.Join("_", request.StoreNo, request.PosNo, request.MemberCard, DateTimeHelper.UnixTimestampString());
        var fullRedisKey = $"{MemberBusinessParent}:{subKey}";
        var encodedKey = EncryptionHelper.Base64Encode(fullRedisKey);

        var memberData = new MemberBusinessData
        {
            CardLevel = memberDb.CardLevel,
            Month = month,
            IsDiscount = itemsDb.Count > 0,
            MemberCard = request.MemberCard,
            MemberStore = memberDb.MemberStore,
            Key = encodedKey,
            Items = quotas.Count > 0 ? quotas : null,
        };

        // Check for other active sessions (different POS for same member)
        var existsOtherNew = FindOtherSession(allKeys, fullRedisKey, request.MemberCard);
        if (existsOtherNew != null && existsOtherNew.KeyExists != encodedKey)
            memberData.ExistsProcessing = existsOtherNew;

        _redis.StringSet<MemberBusinessData>(fullRedisKey, memberData, DateTimeHelper.GetSecondsUntilMidnight());

        return (HttpStatusCode.OK, "OK", new CheckMemberBusinessData
        {
            IsChecked = false,
            Description = "Key không hợp lệ, vui lòng sử dụng key mới",
            MemberBusinessData = memberData,
        });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string GetVatMessage(string actionType)
    {
        try
        {
            return _cache.GetNotifyConfig(_cache.GetConnectStringCentralMD())?
                .Where(x => string.Equals(x.AppCode, "VAT", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(x => string.Equals(x.Status, actionType, StringComparison.OrdinalIgnoreCase))?
                .Message ?? "Phiếu tính tiền không hợp lệ";
        }
        catch { return "Phiếu tính tiền không hợp lệ"; }
    }

    private static ExistsProcessing? FindOtherSession(List<string> allKeys, string currentKey, string memberCard)
    {
        foreach (var k in allKeys)
        {
            if (k == currentKey) continue;
            if (!k.Contains(memberCard)) continue;

            // Key format: MemberBusiness:{storeNo}_{posNo}_{memberCard}_{timestamp}
            var parts = k.Split(':');
            if (parts.Length >= 2)
            {
                var subParts = parts[1].Split('_');
                if (subParts.Length >= 2)
                {
                    return new ExistsProcessing
                    {
                        PosNo = subParts[1],
                        KeyExists = EncryptionHelper.Base64Encode(k),
                    };
                }
            }
        }
        return null;
    }
}
