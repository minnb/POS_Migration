using POS.Common.Dtos.Promotion;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface IPromotionRepository
{
    /// <summary>
    /// SP [dbo].[GetPromotionOfferHeaderList] — danh mục khuyến mãi có lọc + phân trang server-side.
    /// Trả (Items, Total); Total lấy từ field Total mà SP nhồi vào mỗi row. Không cache.
    /// </summary>
    Task<(List<OfferHeaderListItemDto> Items, int Total)> GetOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Toàn bộ khuyến mãi theo bộ lọc (không phân trang) để xuất Excel — gọi cùng SP với
    /// PageNumber=0 và PageSize lớn. Không cache.
    /// </summary>
    Task<List<OfferHeaderListItemDto>> ExportOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default);

    /// <summary>Dropdown Loại CTKM từ dbo.OfferType (Enabled=1), kèm cờ điều khiển UI, cache Redis 12h.</summary>
    Task<List<OfferTypeOptionDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default);

    /// <summary>Dropdown Hình thức bán từ dbo.SalesOrderType (IsActive=1), cache Redis 12h.</summary>
    Task<List<OptionItemDto>> GetSalesOrderTypeOptionsAsync(CancellationToken ct = default);

    // ── Cài đặt CTKM (11.1) ──────────────────────────────────────────────────

    /// <summary>Danh sách CTKM draft (SetupPromotionHEADER) có lọc + phân trang server-side.</summary>
    Task<(List<PromotionSetupListItemDto> Items, int Total)> GetSetupListAsync(
        PromotionSetupListFilter filter, CancellationToken ct = default);

    /// <summary>Chi tiết 1 CTKM draft (header + Buy/Get/Site) để mở sửa. Null nếu không tồn tại.</summary>
    Task<PromotionSetupDetailDto?> GetSetupDetailAsync(string bbynr, CancellationToken ct = default);

    /// <summary>
    /// Lưu CTKM (upsert header + replace Buy/Get/Site) qua SP [dbo].[usp_SaveSetupCTKMAll] (transaction).
    /// Trả (ok, message, bbynr). bbynr auto-gen khi tạo mới.
    /// </summary>
    Task<(bool Ok, string Message, string BBYNR)> SaveSetupAsync(
        PromotionSetupSaveRequest request, CancellationToken ct = default);

    /// <summary>Duyệt CTKM: SP [dbo].[usp_SetupPromotion_Approve] (publish draft → Offer*).</summary>
    Task<(bool Ok, string Message)> ApproveSetupAsync(string bbynr, CancellationToken ct = default);

    /// <summary>Đổi trạng thái CTKM: SP [dbo].[usp_SetupPromotion_UpdateStatus].</summary>
    Task<bool> UpdateSetupStatusAsync(string bbynr, string status, CancellationToken ct = default);

    /// <summary>Lookup sản phẩm (dbo.Item) cho dòng Buy/Get — top 50 theo keyword.</summary>
    Task<List<ItemOptionDto>> SearchItemsAsync(string keyword, CancellationToken ct = default);

    /// <summary>Dropdown nhóm cửa hàng (dbo.SetupGroupSite), cache Redis 12h.</summary>
    Task<List<OfferSiteLineDto>> GetSiteGroupOptionsAsync(CancellationToken ct = default);

    /// <summary>Dropdown hạng thẻ (dbo.OptionData, Caption='MEMBERCODETYPE'), cache Redis 12h.</summary>
    Task<List<OptionItemDto>> GetMemberCodeOptionsAsync(CancellationToken ct = default);

    /// <summary>Tạo/sửa 1 nhóm cửa hàng (dbo.SetupGroupSite) qua SP usp_SetupGroupSite_Save. Invalidate cache SiteGroupOptions.</summary>
    Task<(bool Ok, string Message)> SaveSiteGroupAsync(SiteGroupSaveRequest request, string actor, CancellationToken ct = default);

    /// <summary>Danh sách đầy đủ nhóm cửa hàng (lọc + phân trang server-side) cho sub-tab "Danh sách nhóm CH/ST".</summary>
    Task<(List<SiteGroupListItemDto> Items, int Total)> GetSiteGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>Danh sách store cụ thể trong 1 nhóm (bung "ALL" hoặc JSON array ListStore), lọc theo mã/tên.</summary>
    Task<List<SiteGroupStoreItemDto>> GetSiteGroupStoresAsync(
        string groupCode, string storeNo, string storeName, CancellationToken ct = default);

    /// <summary>Tạo/sửa 1 nhóm sản phẩm (dbo.SetupGroupItem) qua SP usp_SetupGroupItem_Save. Nhóm đã tồn tại chỉ sửa
    /// được GroupName (khớp hạn chế legacy SetupGroupBuyItem — không ghi đè lại ListItemNo).</summary>
    Task<(bool Ok, string Message)> SaveItemGroupAsync(ItemGroupSaveRequest request, string actor, CancellationToken ct = default);

    /// <summary>Danh sách đầy đủ nhóm sản phẩm (lọc + phân trang server-side) cho sub-tab "Danh sách nhóm sản phẩm".</summary>
    Task<(List<ItemGroupListItemDto> Items, int Total)> GetItemGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default);

    /// <summary>Danh sách sản phẩm cụ thể trong 1 nhóm (bung JSON ListItemNo, JOIN dbo.Item), lọc theo mã/tên.</summary>
    Task<List<ItemGroupItemDto>> GetItemGroupItemsAsync(
        string groupCode, string itemNo, string itemName, CancellationToken ct = default);
}
