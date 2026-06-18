using Microsoft.AspNetCore.Components;

namespace POS.Web.Components.Shared;

/// <summary>
/// Base class cho tất cả DataTable page trong POS.Web.
/// Cung cấp: sort (single-column), phân trang, FormatVND.
/// Subclass chỉ cần implement <see cref="SortedFiltered"/>.
/// </summary>
public abstract class PosTableBase<T> : ComponentBase
{
    protected const int PageSize = 10;

    protected string _sortCol = string.Empty;
    protected bool   _sortAsc = true;
    protected int    _page    = 1;

    /// <summary>
    /// Subclass implement: trả về data đã filter + sort.
    /// Dùng _sortCol / _sortAsc để quyết định thứ tự.
    /// </summary>
    protected abstract IEnumerable<T> SortedFiltered { get; }

    protected IEnumerable<T> PagedItems =>
        SortedFiltered.Skip((_page - 1) * PageSize).Take(PageSize);

    protected int TotalFiltered => SortedFiltered.Count();
    protected int PageCount     => (int)Math.Ceiling(TotalFiltered / (double)PageSize);

    protected void SortBy(string col)
    {
        if (_sortCol == col) _sortAsc = !_sortAsc;
        else { _sortCol = col; _sortAsc = true; }
        _page = 1;
    }

    /// <summary>Sort icon: ⇅ chưa sort | ↑ asc | ↓ desc</summary>
    protected string SI(string col) =>
        _sortCol == col ? (_sortAsc ? " ↑" : " ↓") : " ⇅";

    protected static string FormatVND(decimal v) => $"{v:N0} ₫";
}
