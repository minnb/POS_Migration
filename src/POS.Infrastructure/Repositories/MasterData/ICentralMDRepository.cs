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

    /// <summary>INSERT SignalStore. Lỗi → false (parity cũ).</summary>
    Task<bool> InsertSignalStoreAsync(SignalStoreModel model, CancellationToken ct = default);

    /// <summary>SELECT toàn bộ POSMonitor — không cache, cần fresh data cho real-time monitoring.</summary>
    Task<List<PosMonitorStatusDto>> GetPosMonitorStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Danh sách POS từ POSTerminal (master config) LEFT JOIN POSMonitor (live heartbeat) —
    /// dùng cho page ops/pos-map. Không cache (real-time).
    /// </summary>
    Task<List<PosTerminalListDto>> GetPosTerminalListAsync(CancellationToken ct = default);

    /// <summary>Cập nhật 3 field cho phép Admin sửa: IPAddress, Status, BillNoseri.</summary>
    Task<bool> UpdatePosTerminalAsync(
        string posNo, string ipAddress, bool? status, string? billNoseri,
        string updatedBy, CancellationToken ct = default);

    /// <summary>Toàn bộ danh sách cửa hàng (kể cả đã đóng) dùng cho trang quản trị Store.</summary>
    Task<List<StoreListDto>> GetStoreAdminListAsync(CancellationToken ct = default);

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
