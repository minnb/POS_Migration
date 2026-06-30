using POS.Common.Dtos.Promotion;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.Promotion;

/// <summary>Thin wrapper — delegate xuống ISpecialComboRepository.</summary>
public sealed class SpecialComboService(ISpecialComboRepository repository) : ISpecialComboService
{
    public Task<(List<SpecialComboListItemDto> Items, int Total)> GetListAsync(
        SpecialComboListFilter filter, CancellationToken ct = default)
        => repository.GetListAsync(filter, ct);

    public Task<SpecialComboDetailDto?> GetDetailAsync(string code, CancellationToken ct = default)
        => repository.GetDetailAsync(code, ct);

    public Task<(bool Ok, string Message, string Code)> SaveAsync(
        SpecialComboSaveRequest request, string actor, CancellationToken ct = default)
        => repository.SaveAsync(request, actor, ct);

    public Task<bool> UpdateStatusAsync(string code, bool isEnable, string actor, CancellationToken ct = default)
        => repository.UpdateStatusAsync(code, isEnable, actor, ct);

    public Task<bool> DeleteAsync(string code, CancellationToken ct = default)
        => repository.DeleteAsync(code, ct);
}
