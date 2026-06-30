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

    /// <summary>Dropdown Loại CTKM từ dbo.OfferType (Enabled=1), cache Redis 12h.</summary>
    Task<List<OptionItemDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default);

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

    /// <summary>Dropdown nhóm cửa hàng (dbo.SetupGroupSites), cache Redis 12h.</summary>
    Task<List<OfferSiteLineDto>> GetSiteGroupOptionsAsync(CancellationToken ct = default);

    /// <summary>Dropdown hạng thẻ (dbo.OptionData, Caption='MEMBERCODETYPE'), cache Redis 12h.</summary>
    Task<List<OptionItemDto>> GetMemberCodeOptionsAsync(CancellationToken ct = default);
}
