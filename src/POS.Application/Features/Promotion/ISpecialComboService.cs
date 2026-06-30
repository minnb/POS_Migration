using POS.Common.Dtos.Promotion;

namespace POS.Application.Features.Promotion;

/// <summary>Special Combo (11.2) — Application layer, dùng chung cho POS.Web và POS.Api.</summary>
public interface ISpecialComboService
{
    Task<(List<SpecialComboListItemDto> Items, int Total)> GetListAsync(
        SpecialComboListFilter filter, CancellationToken ct = default);

    Task<SpecialComboDetailDto?> GetDetailAsync(string code, CancellationToken ct = default);

    Task<(bool Ok, string Message, string Code)> SaveAsync(
        SpecialComboSaveRequest request, string actor, CancellationToken ct = default);

    Task<bool> UpdateStatusAsync(string code, bool isEnable, string actor, CancellationToken ct = default);

    Task<bool> DeleteAsync(string code, CancellationToken ct = default);
}
