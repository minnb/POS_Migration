using POS.Common.Dtos.CentralMD;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface ICentralMDRepository
{
    Task<MMLSchemeHeader?> GetMMLSchemeHeaderAsync(string code, CancellationToken ct = default);
    Task<List<MMLSchemeItem>?> GetMMLSchemeItemAsync(CancellationToken ct = default);
    Task<MMLSchemeResponse?> GetMMLSchemeResponseAsync(string headerCode, string code, CancellationToken ct = default);
    Task<LoyaltyRateDto?> GetLoyaltyRateDataAsync(string code, CancellationToken ct = default);
    Task<List<string>?> GetSyncTableListAsync(CancellationToken ct = default);
    Task<ItemPointsMemberDto?> GetItemPointsMemberAsync(string pointsCode, string itemNo, string uom, CancellationToken ct = default);
}
