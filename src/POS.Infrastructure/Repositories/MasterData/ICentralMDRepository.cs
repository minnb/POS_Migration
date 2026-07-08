using POS.Common.Dtos;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.Ops;
using POS.Common.Dtos.POS.Common;

namespace POS.Infrastructure.Repositories.Interfaces;

public interface ICentralMDRepository
{
    Task<MMLSchemeHeader?> GetMMLSchemeHeaderAsync(string code, CancellationToken ct = default);
    Task<List<MMLSchemeItem>?> GetMMLSchemeItemAsync(CancellationToken ct = default);
    Task<LoyaltyRateDto?> GetLoyaltyRateDataAsync(string code, CancellationToken ct = default);
    Task<List<string>?> GetSyncTableListAsync(CancellationToken ct = default);
    Task<ItemPointsMemberDto?> GetItemPointsMemberAsync(string pointsCode, string itemNo, string uom, CancellationToken ct = default);
    Task<SysWebApiDto?> GetSysWebApiAsync(string appCode, CancellationToken ct = default);

    /// <summary>Thay MemoryCacheService.GetPOSDataSetup cũ — full bảng POSDataSetup, cache Redis 12h.</summary>
    Task<List<POSDataSetupModel>?> GetPOSDataSetupAsync(CancellationToken ct = default);

    /// <summary>Thay MemoryCacheService.GetStoreSetConfig cũ — mapping store → Kafka topic, cache Redis 12h.</summary>
    Task<List<StoreSetConfig>?> GetStoreSetConfigAsync(CancellationToken ct = default);

    /// <summary>Danh sách cửa hàng (StoreNo + Name) từ bảng Store, cache Redis 12h. Dùng cho UI store picker.</summary>
    Task<List<StoreDto>> GetStoreListAsync(CancellationToken ct = default);

    /// <summary>Danh sách chi nhánh (No + Description) từ bảng Branch, cache Redis 12h. Dùng cho combobox Chi nhánh.</summary>
    Task<List<BranchDto>> GetBranchListAsync(CancellationToken ct = default);

    // ── Danh mục Chi nhánh / Tỉnh-Thành (Branch) — trang catalog/provinces ───

    /// <summary>Toàn bộ danh sách chi nhánh (No, Description, Address, VATRegistrationNo) dùng cho trang quản trị. Không cache.</summary>
    Task<List<BranchAdminDto>> GetBranchAdminListAsync(CancellationToken ct = default);

    /// <summary>EXISTS dbo.Branch theo No (mã chi nhánh) — check trùng trước khi tạo mới.</summary>
    Task<bool> BranchCodeExistsAsync(string branchNo, CancellationToken ct = default);

    /// <summary>
    /// INSERT dbo.Branch — tạo mới chi nhánh. Các cột NOT NULL còn lại ngoài form (PhoneNo, FaxNo,
    /// BankAccountNo, BankName, BankAddress, BankAcountName, VietnameseDescription, VietnameseAddress,
    /// UrlElecInvoice) được điền chuỗi rỗng. Counter = MAX(Counter)+1 toàn bảng (để POS sync incremental
    /// nhận thay đổi), Pkey = No. Trả false nếu lỗi/trùng PK. Invalidate MD:BranchList sau khi tạo.
    /// </summary>
    Task<bool> CreateBranchAsync(BranchCreateDto dto, CancellationToken ct = default);

    /// <summary>
    /// UPDATE dbo.Branch SET Description, Address, VATRegistrationNo theo No. Counter = MAX(Counter)+1
    /// (để POS sync incremental nhận thay đổi). Trả false nếu lỗi/không tìm thấy. Invalidate MD:BranchList
    /// sau khi update (Description thay đổi ảnh hưởng combobox Chi nhánh).
    /// </summary>
    Task<bool> UpdateBranchInfoAsync(string branchNo, string description, string? address, string? vatRegistrationNo, CancellationToken ct = default);

    /// <summary>Danh mục hình thức thanh toán (Code + Description) từ TenderTypeSetup, cache Redis 12h.
    /// Dùng để resolve tên HTTT cho báo cáo thanh toán.</summary>
    Task<List<TenderTypeSetupDto>> GetTenderTypesAsync(CancellationToken ct = default);

    // ── CommonService (migrated từ CommonData — phần CentralMD) ──────────────

    /// <summary>SP [dbo].[POSMonitorInsert] — POS heartbeat/monitor. Lỗi → trả model rỗng (parity cũ).</summary>
    Task<POSMonitorInsertResponse?> POSMonitorInsertAsync(POSMonitorInsertRequest model, CancellationToken ct = default);

    /// <summary>SELECT POSTerminals theo IPAddress. Không có → null.</summary>
    Task<PosTerminalModel?> CheckIPaddressPosAsync(string ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Endpoint api/common/POSDataSetup — query DB trực tiếp KHÔNG cache
    /// (parity CommonData.GetDataSetup cũ; bản cache 12h GetPOSDataSetupAsync chỉ dùng internal).
    /// </summary>
    Task<List<POSDataSetupModel>?> GetDataSetupListAsync(CancellationToken ct = default);

    /// <summary>SELECT full bảng POSVersion.</summary>
    Task<List<POSVersionModel>?> GetPOSVersionAsync(CancellationToken ct = default);

    /// <summary>EXISTS CpnVchBOMLine theo ItemNo + Barcode.</summary>
    Task<bool> CheckCouponLineAsync(string itemNo, string barCode, CancellationToken ct = default);

    /// <summary>True nếu ItemNo tồn tại trong CpnVchBOMHeader (master Coupon/Voucher). Cache Redis Hash MD:CpnVchBOMHeader (positive-only).</summary>
    Task<bool> CpnVchBOMHeaderExistsAsync(string itemNo, CancellationToken ct = default);

    /// <summary>INSERT SignalStore. Lỗi → false (parity cũ).</summary>
    Task<bool> InsertSignalStoreAsync(SignalStoreModel model, CancellationToken ct = default);

    /// <summary>SELECT toàn bộ POSMonitor — không cache, cần fresh data cho real-time monitoring.</summary>
    Task<List<PosMonitorStatusDto>> GetPosMonitorStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Danh sách POS từ POSTerminal (master config) LEFT JOIN POSMonitor (live heartbeat) —
    /// dùng cho page ops/pos-map. Không cache (real-time).
    /// storeNo = null → toàn bộ hệ thống (hành vi cũ, dùng cho ops/pos-map);
    /// storeNo có giá trị → chỉ terminal của 1 cửa hàng (dùng cho Store Dashboard, tránh quét
    /// toàn bộ ~5.000 terminal chỉ để lấy 2-6 dòng liên quan).
    /// </summary>
    Task<List<PosTerminalListDto>> GetPosTerminalListAsync(string? storeNo = null, CancellationToken ct = default);

    /// <summary>Cập nhật 3 field cho phép Admin sửa: IPAddress, Status, BillNoseri.</summary>
    Task<bool> UpdatePosTerminalAsync(
        string posNo, string ipAddress, bool? status, string? billNoseri,
        string updatedBy, CancellationToken ct = default);

    /// <summary>Toàn bộ danh sách cửa hàng (kể cả đã đóng) dùng cho trang quản trị Store.</summary>
    Task<List<StoreListDto>> GetStoreAdminListAsync(CancellationToken ct = default);

    /// <summary>EXISTS dbo.Store theo No (mã cửa hàng) — check trùng trước khi tạo mới.</summary>
    Task<bool> StoreCodeExistsAsync(string storeNo, CancellationToken ct = default);

    /// <summary>
    /// INSERT dbo.Store — tạo mới cửa hàng. Counter = MAX(Counter)+1 toàn bảng (bắt buộc để POS
    /// sync incremental nhận thay đổi), Pkey = No, LastDateModified = GETDATE(). Trả false nếu lỗi/trùng PK.
    /// </summary>
    Task<bool> CreateStoreAsync(StoreCreateDto dto, CancellationToken ct = default);

    /// <summary>
    /// UPDATE dbo.Store SET ClosingMethod — đổi trạng thái đóng/mở cửa hàng. Counter = MAX(Counter)+1
    /// (bắt buộc để POS sync incremental nhận thay đổi), LastDateModified = GETDATE(). Trả false nếu lỗi/không tìm thấy.
    /// </summary>
    Task<bool> UpdateStoreClosingMethodAsync(string storeNo, int closingMethod, CancellationToken ct = default);

    // ── Danh mục Nhân viên (Staff) — migrate từ legacy MasterData/EmployeeList ─

    /// <summary>
    /// SP [dbo].[GetEmployeeList] — danh sách nhân viên có lọc + phân trang server-side.
    /// Trả (Items, Total); Total lấy từ field Total mà SP nhồi vào mỗi row. Không cache.
    /// </summary>
    Task<(List<EmployeeListItemDto> Items, int Total)> GetEmployeeListAsync(
        EmployeeListFilter filter, CancellationToken ct = default);

    /// <summary>
    /// SP [dbo].[GetEmployeeList_Export] — toàn bộ nhân viên theo bộ lọc (không phân trang),
    /// dùng cho xuất Excel. Không cache.
    /// </summary>
    Task<List<EmployeeListItemDto>> ExportEmployeeListAsync(
        EmployeeListFilter filter, CancellationToken ct = default);

    /// <summary>EXISTS dbo.Staff theo ID (mã NV) — check trùng trước khi tạo mới.</summary>
    Task<bool> StaffCodeExistsAsync(string staffCode, CancellationToken ct = default);

    /// <summary>
    /// INSERT dbo.Staff — tạo mới nhân viên POS. Password lưu plain text (contract POS terminal).
    /// Counter = MAX(Counter)+1 toàn bảng (bắt buộc để POS sync incremental nhận thay đổi),
    /// Pkey = StaffCode, LastDateModified = GETDATE(). Trả false nếu lỗi/trùng PK.
    /// </summary>
    Task<bool> CreateEmployeeAsync(EmployeeCreateDto dto, CancellationToken ct = default);

    /// <summary>
    /// UPDATE dbo.Staff SET Password — đổi mật khẩu đăng nhập POS. Chỉ áp dụng khi nhân viên
    /// đang hoạt động (Blocked = 0/NULL, theo legacy ChangePassWord). Counter = MAX+1 để POS
    /// sync nhận thay đổi. Trả false nếu không tìm thấy hoặc đã ngưng hoạt động.
    /// </summary>
    Task<bool> ChangeEmployeePasswordAsync(string staffCode, string newPassword, CancellationToken ct = default);

    // ── POSDataSetup CRUD (Web admin UI) ─────────────────────────────────────

    /// <summary>SELECT đủ 5 cột (Code, Value, Description, StoreNo, Counter) — không cache, dùng cho admin UI.</summary>
    Task<List<POSDataSetupAdminDto>> GetPOSDataSetupAdminListAsync(CancellationToken ct = default);

    /// <summary>SELECT 1 dòng theo Code.</summary>
    Task<POSDataSetupAdminDto?> GetPOSDataSetupByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>INSERT dòng mới. Trả (success=false, duplicateCode=true) nếu Code đã tồn tại. Invalidate Redis cache sau khi insert.</summary>
    Task<(bool success, bool duplicateCode)> InsertPOSDataSetupAsync(POSDataSetupAdminDto dto, CancellationToken ct = default);

    /// <summary>UPDATE Value, Description, StoreNo theo Code — KHÔNG đụng Counter/Pkey. Invalidate Redis cache sau khi update.</summary>
    Task<bool> UpdatePOSDataSetupAsync(POSDataSetupAdminDto dto, CancellationToken ct = default);

    /// <summary>DELETE theo Code. Invalidate Redis cache sau khi xóa.</summary>
    Task<bool> DeletePOSDataSetupAsync(string code, CancellationToken ct = default);

    // ── BankPOS — Máy POS Ngân hàng (migrate 5.5) ───────────────────────────

    /// <summary>SP [dbo].[GetBankPOSList] với @Export=2 — toàn bộ danh sách (không phân trang), dùng cho client-side filter.</summary>
    Task<List<BankPOSListDto>> GetBankPOSListAsync(CancellationToken ct = default);

    /// <summary>INSERT (dto.IsNew=true) hoặc UPDATE (dto.IsNew=false) bảng POSTerminalBanks. Trả false nếu thất bại.</summary>
    Task<(bool success, bool duplicateCode)> SaveBankPOSAsync(BankPOSSaveDto dto, string actor, CancellationToken ct = default);

    /// <summary>DELETE theo BankPOSCode.</summary>
    Task<bool> DeleteBankPOSAsync(string bankPOSCode, CancellationToken ct = default);

    /// <summary>SELECT BankCode, BankName FROM dbo.Banks — dropdown danh sách ngân hàng, cache Redis 12h.</summary>
    Task<List<BankDropdownDto>> GetBankListForDropdownAsync(CancellationToken ct = default);

    // ── Product List — Danh mục SP / Barcode (migrate 6.1) ──────────────────

    /// <summary>
    /// SP [dbo].[GetProductList] — server-side paging.
    /// Tham số SP: @ItemCode, @ItemName, @BarCode, @TaxCode, @PageSize, @PageNumber.
    /// Total lấy từ row đầu tiên (SP inject vào mọi row).
    /// </summary>
    Task<(List<ProductListItemDto> Items, int Total)> GetProductListAsync(
        ProductListFilter filter, CancellationToken ct = default);

    /// <summary>SP [dbo].[GetProductList_Export] — toàn bộ không phân trang, dùng cho xuất Excel.</summary>
    Task<List<ProductListItemDto>> ExportProductListAsync(
        ProductListFilter filter, CancellationToken ct = default);

    /// <summary>
    /// SELECT Code, Description FROM dbo.POSVATCode — dropdown thuế suất, cache Redis 12h.
    /// </summary>
    Task<List<PosVatCodeDto>> GetPosVatCodesAsync(CancellationToken ct = default);

    // ── Product Create — Tạo sản phẩm mới (migrate 6.2) ─────────────────────

    /// <summary>SELECT Code FROM dbo.ArticleType — dropdown loại hàng, cache Redis 12h.</summary>
    Task<List<ArticleTypeDto>> GetArticleTypesAsync(CancellationToken ct = default);

    /// <summary>SELECT Code FROM dbo.UnitOfMeasure — dropdown đơn vị đo, cache Redis 12h.</summary>
    Task<List<UnitOfMeasureDto>> GetUnitOfMeasuresAsync(CancellationToken ct = default);

    /// <summary>
    /// INSERT dbo.Item + N rows dbo.Barcode trong transaction.
    /// ItemNo auto-generated (Max+1, bắt đầu "1000000001").
    /// BarcodeNo phải là số; BarcodeList không được rỗng — validate ở caller.
    /// </summary>
    Task<(bool Success, string ItemNo, string Message)> CreateProductAsync(
        ProductCreateDto dto, CancellationToken ct = default);

    /// <summary>
    /// UPSERT dbo.ProductImage theo khóa ghép (ItemNo, Uom) — 1 ảnh đại diện/sản phẩm,
    /// mã hóa base64 (nvarchar(max)). Upload lại sẽ ghi đè ảnh cũ.
    /// </summary>
    Task<(bool Success, string Message)> SaveProductImageAsync(
        ProductImageDto dto, CancellationToken ct = default);

    /// <summary>
    /// Chi tiết 1 sản phẩm để xem — dbo.Item + dbo.Barcodes + dbo.ProductImage (nếu có).
    /// Trả null nếu ItemNo không tồn tại.
    /// </summary>
    Task<ProductDetailDto?> GetProductDetailAsync(string itemNo, CancellationToken ct = default);

    // ── Product Lock — Khóa sản phẩm (migrate 6.4) ───────────────────────────

    /// <summary>
    /// JOIN dbo.Item + dbo.ItemBlock: danh sách sản phẩm + trạng thái khóa của 1 store.
    /// StoreNo bắt buộc. Status: -1=all, 0=active, 1=locked.
    /// Total lấy từ row đầu tiên (COUNT(*) OVER() inject mọi row).
    /// </summary>
    Task<(List<ProductLockItemDto> Items, int Total)> GetProductLockListAsync(
        ProductLockFilter filter, CancellationToken ct = default);

    /// <summary>
    /// UPSERT dbo.ItemBlock cho nhiều ItemNo trong 1 transaction.
    /// Pkey = "{StoreNo}-{ItemNo}". TargetLock=true → khóa, false → mở khóa.
    /// </summary>
    Task<(bool Success, string Message)> SaveProductLockAsync(
        ProductLockSaveDto dto, CancellationToken ct = default);

    // ── Dashboard Audit Log ──────────────────────────────────────────────────

    /// <summary>
    /// Ghi 1 dòng vào DashboardAuditLog. try/catch nội bộ — caller không cần bọc thêm;
    /// audit failure không làm gián đoạn main flow.
    /// </summary>
    Task InsertDashboardAuditLogAsync(
        string actor, string action, string entityType, string entityKey,
        string? oldValueJson = null, string? newValueJson = null,
        CancellationToken ct = default);
}
