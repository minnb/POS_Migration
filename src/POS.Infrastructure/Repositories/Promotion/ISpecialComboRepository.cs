using POS.Common.Dtos.Promotion;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface ISpecialComboRepository
{
    /// <summary>SP usp_SpecialCombo_GetList — danh sách combo có lọc + phân trang server-side.</summary>
    Task<(List<SpecialComboListItemDto> Items, int Total)> GetListAsync(
        SpecialComboListFilter filter, CancellationToken ct = default);

    /// <summary>SP usp_SpecialCombo_GetDetail — header + lines + stores của 1 combo. Null nếu không tồn tại.</summary>
    Task<SpecialComboDetailDto?> GetDetailAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// SP usp_SpecialCombo_Save — upsert header + replace lines/stores (transaction).
    /// Validate ≤1 item giá động. Auto-gen Code khi tạo mới. Trả (ok, message, code).
    /// </summary>
    Task<(bool Ok, string Message, string Code)> SaveAsync(
        SpecialComboSaveRequest request, string actor, CancellationToken ct = default);

    /// <summary>SP usp_SpecialCombo_UpdateStatus — bật/tắt combo.</summary>
    Task<bool> UpdateStatusAsync(string code, bool isEnable, string actor, CancellationToken ct = default);

    /// <summary>SP usp_SpecialCombo_Delete — xóa combo (header + lines + stores).</summary>
    Task<bool> DeleteAsync(string code, CancellationToken ct = default);
}
