using POS.Application.Gift.DTOs;

namespace POS.Application.Gift.Services;

public interface IGiftService
{
    Task<(List<MMLSchemeResponseDto> Items, string Message, string MessageTechnical)> ClaimMMLSchemeAsync(
        MMLSchemeRequest request, CancellationToken ct);

    Task<(int HttpStatus, int Status, string Message, object? Data)> PostGiftCheckAsync(GiftDataRequest request);
}
