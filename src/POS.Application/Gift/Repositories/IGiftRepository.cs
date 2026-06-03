using POS.Application.Gift.DTOs;

namespace POS.Application.Gift.Repositories;

public interface IGiftRepository
{
    // POSGiftInfo — CentralGeneral DB
    Task<List<POSGiftInfoDto>> InsertPOSGiftInfoAsync(List<POSGiftInfoDto> giftInfos);
    Task<List<POSGiftInfoDto>> GetByGiftCodeAsync(string[] giftCodes);
    Task<bool> UpdateGiftStatusAsync(string[] giftCodes, string status, string posUsed);

    // MMLScheme — CentralMD DB
    Task<MMLSchemeHeaderDto?> GetMMLSchemeHeaderAsync(string code);
    Task<List<MMLSchemeItemDto>> GetMMLSchemeItemsAsync();
    Task<MMLSchemeResponseDto?> GetMMLSchemeResponseAsync(string headerCode, string journeyCode);
}
