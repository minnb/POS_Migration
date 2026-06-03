using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Application.Gift.DTOs;
using POS.Application.Gift.Repositories;

namespace POS.Application.Gift.Services;

public class GiftService : IGiftService
{
    private static readonly string[] CheckCreateStatuses = { "CHECK", "CREATE" };
    private const string StatusAvailable = "AVAILABLE";
    private const string StatusUsed = "USED";

    private readonly IGiftRepository _repo;
    private readonly IWinXService _winXService;
    private readonly ILogger<GiftService> _logger;

    public GiftService(IGiftRepository repo, IWinXService winXService, ILogger<GiftService> logger)
    {
        _repo = repo;
        _winXService = winXService;
        _logger = logger;
    }

    public async Task<(List<MMLSchemeResponseDto> Items, string Message, string MessageTechnical)> ClaimMMLSchemeAsync(
        MMLSchemeRequest request, CancellationToken ct)
    {
        _logger.LogInformation("ClaimMMLScheme {OrderNo} {StoreNo} {Code}", request.OrderNo, request.StoreNo, request.Code);

        var results = new List<MMLSchemeResponseDto>();
        string message = string.Empty;
        string messageTechnical = string.Empty;

        if (!string.Equals(request.Code, "THEWINX_QRCode", StringComparison.OrdinalIgnoreCase))
        {
            var winXTask = _winXService.PosPostTransactionsAsync(request);
            var mmlTask = GetMMLSchemeItemInternalAsync(request);
            await Task.WhenAll(winXTask, mmlTask);

            var winXResult = winXTask.Result;
            if (winXResult.Result != null)
            {
                results.Add(new MMLSchemeResponseDto
                {
                    HeaderCode = "THEWINX_QRCode",
                    Code = "999",
                    Title = winXResult.Result.Title,
                    Link = winXResult.Result.Url,
                    IsGenQR = true,
                    Enabled = true,
                    Description = winXResult.Result.Content
                });
            }

            var mmlResult = mmlTask.Result;
            _logger.LogInformation("ClaimMMLScheme {OrderNo} MML result: {Message}", request.OrderNo, mmlResult.Message);
            message = mmlResult.Message;
            if (mmlResult.Item != null)
                results.Add(mmlResult.Item);

            if (results.Count == 0)
                messageTechnical = JsonConvert.SerializeObject(mmlResult);
        }
        else
        {
            var winXResult = await _winXService.PosPostTransactionsAsync(request);
            if (winXResult.Result != null)
            {
                results.Add(new MMLSchemeResponseDto
                {
                    HeaderCode = "THEWINX_QRCode",
                    Code = "999",
                    Title = winXResult.Result.Title,
                    Link = winXResult.Result.Url,
                    IsGenQR = true,
                    Enabled = true,
                    Description = winXResult.Result.Content
                });
            }
            messageTechnical = winXResult.Message;
        }

        _logger.LogInformation("ClaimMMLScheme {OrderNo} response: {Count} items", request.OrderNo, results.Count);
        return (results, message, messageTechnical);
    }

    public async Task<(int HttpStatus, int Status, string Message, object? Data)> PostGiftCheckAsync(GiftDataRequest request)
    {
        _logger.LogInformation("PostGiftCheck {PosNo} {Status} codes={Count}", request.PosNo, request.Status, request.GiftCode.Length);

        string giftStatus = request.Status.ToUpper();

        if (CheckCreateStatuses.Contains(giftStatus))
        {
            var giftInfos = BuildGiftInfoList(request);
            var dataGiftCheck = await _repo.InsertPOSGiftInfoAsync(giftInfos);

            if (dataGiftCheck == null || dataGiftCheck.Count == 0)
            {
                return (200, 200, "GiftCode đã sử dụng hoặc không tìm thấy", request.GiftCode);
            }

            var giftError = dataGiftCheck.Where(x => x.GiftStatus != StatusAvailable).ToList();
            if (giftError.Count > 0)
            {
                _logger.LogWarning("PostGiftCheck {PosNo} gift errors: {Count}", request.PosNo, giftError.Count);
                return (502, 400, "GiftCode đã sử dụng hoặc không tìm thấy", giftError);
            }

            return (200, 200, "Thành công", dataGiftCheck);
        }

        if (giftStatus == StatusUsed)
        {
            var listGift = await _repo.GetByGiftCodeAsync(request.GiftCode);

            if (listGift == null || listGift.Count == 0)
            {
                return (502, 400, "Dữ liệu không hợp lệ", null);
            }

            var notAvailable = listGift.FirstOrDefault(x => x.GiftStatus != StatusAvailable);
            if (notAvailable != null)
            {
                return (502, 400, $"GiftCode: {notAvailable.GiftCode} không hợp lệ", null);
            }

            bool updated = await _repo.UpdateGiftStatusAsync(request.GiftCode, StatusUsed, request.PosNo);
            if (!updated)
            {
                return (400, 400, "Có lỗi xảy ra, vui lòng thử lại.", null);
            }

            var now = DateTime.Now;
            listGift.ForEach(x => { x.GiftStatus = StatusUsed; x.PosUsed = request.PosNo; x.UsedTime = now; });
            return (200, 200, "Thành công", listGift);
        }

        return (502, 400, $"Status {giftStatus} không hợp lệ", null);
    }

    private async Task<(MMLSchemeResponseDto? Item, string Message)> GetMMLSchemeItemInternalAsync(MMLSchemeRequest request)
    {
        try
        {
            var header = await _repo.GetMMLSchemeHeaderAsync(request.Code);
            if (header == null)
                return (null, "MMLSchemeHeader not found");

            var items = await _repo.GetMMLSchemeItemsAsync();
            if (items == null || items.Count == 0)
                return (null, "MMLSchemeItem not found");

            var filteredItem = items.FirstOrDefault(x => x.HeaderCode == header.HeaderCode);
            if (filteredItem == null)
                return (null, $"Chưa khai báo sản phẩm cho {request.Code}");

            var billItemKeys = request.Items.Select(x => x.ItemNo + x.UOM).ToList();
            var schemeItemKeys = items.Where(x => x.HeaderCode == header.HeaderCode)
                                     .Select(x => x.ItemNo + x.UOM).ToList();
            var schemeSet = new HashSet<string>(schemeItemKeys);

            if (!billItemKeys.Any(k => schemeSet.Contains(k)))
                return (null, "Hóa đơn không có sản phẩm MML, WECO");

            var matchedKeys = billItemKeys.Intersect(schemeItemKeys).ToList();
            var wecoItems = items.Where(x => matchedKeys.Contains(x.ItemNo + x.UOM) && x.CategoryCode == "WECO").ToList();
            var mmlItems = items.Where(x => matchedKeys.Contains(x.ItemNo + x.UOM) && x.CategoryCode == "MML").ToList();

            string journeyCode;
            if (wecoItems.Count > 0 && mmlItems.Count == 0)
                journeyCode = "Journey_1";
            else if (wecoItems.Count > 0 && mmlItems.Count > 0)
                journeyCode = "Journey_2";
            else if (wecoItems.Count == 0 && mmlItems.Count > 0)
                journeyCode = "Journey_3";
            else
                return (null, "Không xác định được Hành trình");

            var mmlResponse = await _repo.GetMMLSchemeResponseAsync(request.Code, journeyCode);
            if (mmlResponse == null)
                return (null, "Không tìm thấy Hành trình đủ điều kiện");

            mmlResponse.Link += request.OrderNo;
            return (mmlResponse, "Success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMMLSchemeItemInternal failed for {OrderNo}", request.OrderNo);
            return (null, "Exception: " + ex.Message);
        }
    }

    private static List<POSGiftInfoDto> BuildGiftInfoList(GiftDataRequest request)
    {
        var now = DateTime.Now;
        return request.GiftCode.Select(code => new POSGiftInfoDto
        {
            GiftCode = code,
            GiftType = request.GiftType,
            GiftStatus = StatusAvailable,
            GiftValue = 0,
            CreatedTime = now,
            PosCreated = request.PosNo,
            UsedTime = now,
            PosUsed = string.Empty,
            FromDate = now.Date,
            ToDate = now.Date,
            PublishDate = now
        }).ToList();
    }
}
