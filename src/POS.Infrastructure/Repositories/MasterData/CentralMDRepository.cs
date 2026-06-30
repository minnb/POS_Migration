using POS.Common.Dtos;
using POS.Common.Dtos.CentralMD;
using POS.Common.Dtos.Ops;
using POS.Common.Dtos.POS.Common;
using POS.Infrastructure.Database;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Repositories;

public sealed class CentralMDRepository(
    CentralMDConnectionFactory connectionFactory,
    IRedisService redis)
    : BaseRepository(connectionFactory), ICentralMDRepository
{
    private const string KeyMMLSchemeHeader   = "MD:MMLSchemeHeader";
    private const string KeyMMLSchemeItem     = "MD:MMLSchemeItem";
    private const string KeyMMLSchemeResponse = "MD:MMLSchemeResponse";
    private const string KeyLoyaltyRate       = "MD:LoyaltyRate";
    private const string KeyItemPointsMember  = "MD:ItemPointsMember";
    private const string KeySysWebApi         = "MD:SysWebApi";
    private const string KeyPOSDataSetup      = "MD:POSDataSetup";
    private const string KeyStoreSetConfig    = "MD:StoreSetConfig";
    private const string KeyStoreList         = "MD:StoreList";
    private const string KeyTenderTypeSetup   = "MD:TenderTypeSetup";

    public async Task<MMLSchemeHeader?> GetMMLSchemeHeaderAsync(string code, CancellationToken ct = default)
    {
        var cached = await redis.HashGetAsync<MMLSchemeHeader>(KeyMMLSchemeHeader, code);
        if (cached != null) return cached;

        const string sql = @"SELECT [Code] AS HeaderCode,[FromDate],[ToDate],[IsMember],[IsCallAPI],[MinAmount],[Enabled],[Ref1],[Ref2],[Ref3],[Ref4],[Ref5]
                             FROM MMLSchemeHeader (NOLOCK)
                             WHERE CAST([FromDate] AS DATE) <= CAST(getdate() AS DATE)
                               AND CAST([ToDate] AS DATE) >= CAST(getdate() AS DATE)
                               AND ISNULL([Enabled], 0) = 1 AND Code = @code;";

        var data = await QueryFirstOrDefaultAsync<MMLSchemeHeader>(sql, new { code }, ct: ct);
        if (data != null)
            redis.HashSet(KeyMMLSchemeHeader, code, data);
        return data;
    }

    public async Task<List<MMLSchemeItem>?> GetMMLSchemeItemAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<MMLSchemeItem>>(KeyMMLSchemeItem);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT I.*
                             FROM MMLSchemeHeader (NOLOCK) H
                             INNER JOIN MMLSchemeItem (NOLOCK) I ON H.Code = I.HeaderCode
                             WHERE CAST(H.[FromDate] AS DATE) <= CAST(getdate() AS DATE)
                               AND CAST(H.[ToDate] AS DATE) >= CAST(getdate() AS DATE)
                               AND ISNULL(H.[Enabled], 0) = 1;";

        var data = (await QueryAsync<MMLSchemeItem>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyMMLSchemeItem, data);
        return data;
    }

    public async Task<MMLSchemeResponse?> GetMMLSchemeResponseAsync(string headerCode, string code, CancellationToken ct = default)
    {
        string field = $"{headerCode}-{code}";
        var cached = redis.HashGet<MMLSchemeResponse>(KeyMMLSchemeResponse, field);
        if (cached != null) return cached;

        const string sql = @"SELECT DISTINCT I.*
                             FROM MMLSchemeHeader (NOLOCK) H
                             INNER JOIN MMLSchemeResponse (NOLOCK) I ON H.Code = I.HeaderCode
                             WHERE CAST(H.[FromDate] AS DATE) <= CAST(getdate() AS DATE)
                               AND CAST(H.[ToDate] AS DATE) >= CAST(getdate() AS DATE)
                               AND ISNULL(H.[Enabled], 0) = 1
                               AND I.HeaderCode = @headerCode AND I.Code = @code;";

        var data = await QueryFirstOrDefaultAsync<MMLSchemeResponse>(sql, new { headerCode, code }, ct: ct);
        if (data != null)
            redis.HashSet(KeyMMLSchemeResponse, field, data);
        return data;
    }

    public async Task<LoyaltyRateDto?> GetLoyaltyRateDataAsync(string code, CancellationToken ct = default)
    {
        var cached = redis.HashGet<LoyaltyRateDto>(KeyLoyaltyRate, code);
        if (cached != null) return cached;

        const string sql = @"SELECT FromDate, ToDate, Code, Rate, IIF([Enable] = 1, 0, 1) AS Blocked, Pkey, CardType
                             FROM LoyaltyRate (NOLOCK) WHERE [Enable] = 1 AND [Code] = @code;";

        var data = await QueryFirstOrDefaultAsync<LoyaltyRateDto>(sql, new { code }, ct: ct);
        if (data != null)
            redis.HashSet(KeyLoyaltyRate, code, data);
        return data;
    }

    public async Task<List<string>?> GetSyncTableListAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT TableName FROM SyncTableList (NOLOCK) WHERE IsAll = 1 GROUP BY TableName ORDER BY TableName;";
        return (await QueryAsync<string>(sql, ct: ct)).ToList();
    }

    public async Task<ItemPointsMemberDto?> GetItemPointsMemberAsync(string pointsCode, string itemNo, string uom, CancellationToken ct = default)
    {
        string field = $"{pointsCode}-{itemNo}-{uom}";
        var cached = redis.HashGet<ItemPointsMemberDto>(KeyItemPointsMember, field);
        if (cached != null) return cached;

        const string sql = "SELECT * FROM ItemPointsMember (NOLOCK) WHERE Blocked = 0 AND PointsCode = @pointsCode AND ItemNo = @itemNo AND Uom = @uom;";
        var data = await QueryFirstOrDefaultAsync<ItemPointsMemberDto>(sql, new { pointsCode, itemNo, uom }, ct: ct);
        if (data != null)
            redis.HashSet(KeyItemPointsMember, field, data, ttlSeconds: 360);
        return data;
    }

    public async Task<SysWebApiDto?> GetSysWebApiAsync(string appCode, CancellationToken ct = default)
    {
        var cached = redis.HashGet<SysWebApiDto>(KeySysWebApi, appCode);
        if (cached != null) return cached;

        const string sqlApi   = "SELECT * FROM SysWebApi (NOLOCK) WHERE Blocked = 0 AND AppCode = @appCode;";
        const string sqlRoute = "SELECT * FROM SysWebApiRoute (NOLOCK) WHERE Blocked = 0 AND AppCode = @appCode;";

        var dto    = await QueryFirstOrDefaultAsync<SysWebApiDto>(sqlApi, new { appCode }, ct: ct);
        if (dto == null) return null;

        var routes = (await QueryAsync<SysWebApiRoute>(sqlRoute, new { appCode }, ct: ct)).ToList();
        dto.SysWebApiRoute = routes;

        redis.HashSet(KeySysWebApi, appCode, dto, ttlSeconds: 43200); // 12 giờ
        return dto;
    }

    public async Task<List<POSDataSetupModel>?> GetPOSDataSetupAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<POSDataSetupModel>>(KeyPOSDataSetup);
        if (cached?.Count > 0) return cached;

        const string sql = "SELECT [Code], [Value] FROM POSDataSetup (NOLOCK);";
        var data = (await QueryAsync<POSDataSetupModel>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyPOSDataSetup, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<StoreSetConfig>?> GetStoreSetConfigAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<StoreSetConfig>>(KeyStoreSetConfig);
        if (cached?.Count > 0) return cached;

        // Giữ nguyên SQL từ MemoryCacheService.GetStoreSetConfig cũ
        const string sql = @"SELECT DISTINCT A.StoreNo, B.*
                             FROM CentralGeneral.dbo.[StoreSetServer] (NOLOCK) A
                             INNER JOIN SysWebApiConfig (NOLOCK) B ON B.[Prefix] = A.[ServerIP]
                             WHERE B.Blocked = 0 AND A.[Status] = 1;";
        var data = (await QueryAsync<StoreSetConfig>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyStoreSetConfig, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<StoreDto>> GetStoreListAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<StoreDto>>(KeyStoreList);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT No AS StoreNo, Name
                             FROM dbo.Store (NOLOCK)
                             WHERE ClosingMethod = 0
                             ORDER BY No";
        var data = (await QueryAsync<StoreDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyStoreList, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<TenderTypeSetupDto>> GetTenderTypesAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<TenderTypeSetupDto>>(KeyTenderTypeSetup);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [Code], [Description]
                             FROM dbo.TenderTypeSetup (NOLOCK)
                             GROUP BY [Code], [Description]
                             ORDER BY [Code]";
        var data = (await QueryAsync<TenderTypeSetupDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyTenderTypeSetup, data, ttlSeconds: 43200);
        return data;
    }

    // ── CommonService (migrated từ CommonData — phần CentralMD) ──────────────
    // Parity với CommonData cũ: catch → log/nuốt → trả default; timeout 120s.

    public async Task<POSMonitorInsertResponse?> POSMonitorInsertAsync(POSMonitorInsertRequest model, CancellationToken ct = default)
    {
        try
        {
            const string sql = @"[dbo].[POSMonitorInsert] @StoreNo,@IpAddress,@PosTerminalID,@BluePosVersion,@BluePosVersionUpdate,@BluePosDatabaseStatus,@IsOpenBluePos,@DateTimePos,@IntervalJob,@LastTimeInsertAll,@LastTimeInsertChange,@JobVersion,@ScriptVersion,@ComputerName";
            return await QueryFirstOrDefaultAsync<POSMonitorInsertResponse>(sql, new
            {
                model.StoreNo,
                model.IpAddress,
                model.PosTerminalID,
                BluePosVersion = model.BluePosVersion ?? "",
                model.BluePosVersionUpdate,
                model.BluePosDatabaseStatus,
                model.IsOpenBluePos,
                model.DateTimePos,
                model.IntervalJob,
                model.LastTimeInsertAll,
                model.LastTimeInsertChange,
                JobVersion = model.JobVersion ?? "",
                ScriptVersion = model.ScriptVersion ?? "",
                model.ComputerName
            }, commandTimeout: 120, ct: ct);
        }
        catch
        {
            return new POSMonitorInsertResponse();
        }
    }

    public async Task<PosTerminalModel?> CheckIPaddressPosAsync(string ipAddress, CancellationToken ct = default)
    {
        try
        {
            const string sql = @"SELECT IPAddress, StoreNo, [No] AS TerminalPOS, TerminalNetworkID, StyleProfile,
                                        DefaultSalesType, SalesTypeFilter, Pkey, DualDisHost, BillNoseri, Placement,
                                        ISNULL(StatementMethod, 0) AS StatementMethod, TerminalStatement,
                                        ISNULL(TerminalConnection, 0) AS TerminalConnection, PrintReceiptLogo,
                                        CustomerDisplayText1, CustomerDisplayText2,
                                        ISNULL(PrintReceiptBCType, 0) AS PrintReceiptBCType, InterfaceProfile
                                 FROM POSTerminal (NOLOCK) WHERE IPAddress = @ipAddress;";
            return await QueryFirstOrDefaultAsync<PosTerminalModel>(sql, new { ipAddress }, commandTimeout: 120, ct: ct);
        }
        catch
        {
            return default;
        }
    }

    public async Task<List<POSDataSetupModel>?> GetDataSetupListAsync(CancellationToken ct = default)
    {
        try
        {
            const string sql = "SELECT [Code], [Value] FROM POSDataSetup (NOLOCK);";
            return (await QueryAsync<POSDataSetupModel>(sql, commandTimeout: 120, ct: ct)).ToList();
        }
        catch
        {
            return default;
        }
    }

    public async Task<List<POSVersionModel>?> GetPOSVersionAsync(CancellationToken ct = default)
    {
        try
        {
            const string sql = @"SELECT LastVersion, CurVersion, UpdateTime, [Counter], Source, Pkey, IsUpdate, Folder
                                 FROM POSVersion (NOLOCK);";
            return (await QueryAsync<POSVersionModel>(sql, commandTimeout: 120, ct: ct)).ToList();
        }
        catch (Exception)
        {
            return default;
        }
    }

    public async Task<bool> CheckCouponLineAsync(string itemNo, string barCode, CancellationToken ct = default)
    {
        try
        {
            const string sql = @"SELECT CASE WHEN EXISTS (
                                     SELECT 1 FROM CpnVchBOMLine (NOLOCK) WHERE ItemNo = @itemNo AND Barcode = @barCode
                                 ) THEN 1 ELSE 0 END;";
            return await QueryFirstOrDefaultAsync<bool>(sql, new { itemNo, barCode }, commandTimeout: 120, ct: ct);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> InsertSignalStoreAsync(SignalStoreModel model, CancellationToken ct = default)
    {
        try
        {
            const string sql = @"INSERT INTO SignalStore (StoreNO, POSTerminalID, BusinessDate, CreatedDate)
                                 VALUES (@StoreNO, @POSTerminalID, @BusinessDate, @CreatedDate);";
            await ExecuteAsync(sql,
                new { model.StoreNO, model.POSTerminalID, model.BusinessDate, model.CreatedDate },
                commandTimeout: 120, ct: ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PosMonitorStatusDto>> GetPosMonitorStatusAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT StoreNo, PosTerminalID, IpAddress, ComputerName,
                                    BluePosVersion, BluePosVersionUpdate, BluePosDatabaseStatus,
                                    IsOpenBluePos, DateTimePos,
                                    LastTimeInsertAll, LastTimeInsertChange, DateTimePos AS UpdatedAt
                             FROM POSMonitor (NOLOCK)
                             ORDER BY StoreNo, PosTerminalID;";
        return (await QueryAsync<PosMonitorStatusDto>(sql, ct: ct)).ToList();
    }

    public async Task<List<PosTerminalListDto>> GetPosTerminalListAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT pt.[No], pt.StoreNo, pt.IPAddress, pt.MACAddress, pt.Description,
                                    pt.Placement, pt.PrintReceiptLogo, pt.CustomerDisplayText1, pt.CustomerDisplayText2,
                                    pt.StyleProfile, pt.BillNoseri, pt.DefaultPriceGroup, pt.TerminalNetworkID,
                                    pt.AutoLogoffAfter_Min, pt.[Status], pt.LastDateModified,
                                    pt.CreatedDate, pt.CreatedBy, pt.UpdatedDate, pt.UpdatedBy,
                                    pm.ComputerName, pm.BluePosVersion, pm.BluePosVersionUpdate,
                                    pm.BluePosDatabaseStatus, pm.IsOpenBluePos, pm.DateTimePos
                             FROM POSTerminal pt WITH (NOLOCK)
                             OUTER APPLY (
                                 SELECT TOP 1 m.ComputerName, m.BluePosVersion, m.BluePosVersionUpdate,
                                        m.BluePosDatabaseStatus, m.IsOpenBluePos, m.DateTimePos
                                 FROM POSMonitor m WITH (NOLOCK)
                                 WHERE m.StoreNo = pt.StoreNo AND m.PosTerminalID = pt.[No]
                                 ORDER BY m.DateTimePos DESC
                             ) pm
                             ORDER BY pt.StoreNo, pt.[No];";
        return (await QueryAsync<PosTerminalListDto>(sql, commandTimeout: 120, ct: ct)).ToList();
    }

    public async Task<bool> UpdatePosTerminalAsync(
        string posNo, string ipAddress, bool? status, string? billNoseri,
        string updatedBy, CancellationToken ct = default)
    {
        const string sql = @"UPDATE POSTerminal
                             SET IPAddress   = @ipAddress,
                                 [Status]    = @status,
                                 BillNoseri  = @billNoseri,
                                 UpdatedDate = GETDATE(),
                                 UpdatedBy   = @updatedBy
                             WHERE [No] = @posNo;";
        try
        {
            var rows = await ExecuteAsync(sql,
                new { posNo, ipAddress, status, billNoseri, updatedBy }, ct: ct);
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<List<StoreListDto>> GetStoreAdminListAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT No              AS StoreNo,
                                    Name,
                                    Address,
                                    PhoneNo,
                                    StyleProfile,
                                    PrintReceiptLogo,
                                    BranchNo,
                                    LastDateModified,
                                    ClosingMethod
                             FROM   dbo.Store (NOLOCK)
                             ORDER  BY No";
        return (await QueryAsync<StoreListDto>(sql, ct: ct)).ToList();
    }

    // ── Danh mục Nhân viên (Staff) ───────────────────────────────────────────

    public async Task<(List<EmployeeListItemDto> Items, int Total)> GetEmployeeListAsync(
        EmployeeListFilter filter, CancellationToken ct = default)
    {
        const string sql = "[dbo].[GetEmployeeList] @StaffCode,@StaffName,@StoreNo,@TypeGroup,@Status,@PageSize,@PageNumber";
        var items = (await QueryAsync<EmployeeListItemDto>(sql, new
        {
            StaffCode  = (filter.StaffCode ?? string.Empty).Trim(),
            StaffName  = (filter.StaffName ?? string.Empty).Trim(),
            StoreNo    = (filter.StoreNo ?? string.Empty).Trim(),
            TypeGroup  = string.IsNullOrWhiteSpace(filter.TypeGroup) ? "-1" : filter.TypeGroup.Trim(),
            Status     = string.IsNullOrWhiteSpace(filter.Status) ? "-1" : filter.Status.Trim(),
            PageSize   = Math.Max(1, filter.PageSize),
            PageNumber = Math.Max(0, filter.PageNumber)
        }, commandTimeout: 120, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<List<EmployeeListItemDto>> ExportEmployeeListAsync(
        EmployeeListFilter filter, CancellationToken ct = default)
    {
        const string sql = "[dbo].[GetEmployeeList_Export] @StaffCode,@StaffName,@StoreNo,@TypeGroup,@Status";
        return (await QueryAsync<EmployeeListItemDto>(sql, new
        {
            StaffCode = (filter.StaffCode ?? string.Empty).Trim(),
            StaffName = (filter.StaffName ?? string.Empty).Trim(),
            StoreNo   = (filter.StoreNo ?? string.Empty).Trim(),
            TypeGroup = string.IsNullOrWhiteSpace(filter.TypeGroup) ? "-1" : filter.TypeGroup.Trim(),
            Status    = string.IsNullOrWhiteSpace(filter.Status) ? "-1" : filter.Status.Trim()
        }, commandTimeout: 120, ct: ct)).ToList();
    }

    // ── POSDataSetup CRUD (Web admin UI) ─────────────────────────────────────

    public async Task<List<POSDataSetupAdminDto>> GetPOSDataSetupAdminListAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT Code, Value, Description, StoreNo, Counter
                             FROM   dbo.POSDataSetup (NOLOCK)
                             ORDER  BY StoreNo, Code";
        return (await QueryAsync<POSDataSetupAdminDto>(sql, ct: ct)).ToList();
    }

    public async Task<POSDataSetupAdminDto?> GetPOSDataSetupByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = @"SELECT Code, Value, Description, StoreNo, Counter
                             FROM   dbo.POSDataSetup (NOLOCK)
                             WHERE  Code = @code";
        return await QueryFirstOrDefaultAsync<POSDataSetupAdminDto>(sql, new { code }, ct: ct);
    }

    public async Task<(bool success, bool duplicateCode)> InsertPOSDataSetupAsync(POSDataSetupAdminDto dto, CancellationToken ct = default)
    {
        // PK là Code — kiểm tra trùng trước khi insert
        const string checkSql = "SELECT COUNT(1) FROM dbo.POSDataSetup WHERE Code = @Code;";
        var exists = await QueryFirstOrDefaultAsync<int>(checkSql, new { dto.Code }, ct: ct);
        if (exists > 0) return (false, true);

        const string sql = @"INSERT INTO dbo.POSDataSetup (Code, Value, Description, StoreNo)
                             VALUES (@Code, @Value, @Description, @StoreNo);";
        try
        {
            await ExecuteAsync(sql, new { dto.Code, dto.Value, dto.Description, dto.StoreNo }, ct: ct);
            redis.Delete(KeyPOSDataSetup); // invalidate POS machine cache
            return (true, false);
        }
        catch { return (false, false); }
    }

    public async Task<bool> UpdatePOSDataSetupAsync(POSDataSetupAdminDto dto, CancellationToken ct = default)
    {
        // KHÔNG cập nhật Counter/Pkey — chỉ 3 field được phép sửa
        const string sql = @"UPDATE dbo.POSDataSetup
                             SET    Value       = @Value,
                                    Description = @Description,
                                    StoreNo     = @StoreNo
                             WHERE  Code = @Code;";
        try
        {
            var rows = await ExecuteAsync(sql, new { dto.Value, dto.Description, dto.StoreNo, dto.Code }, ct: ct);
            if (rows > 0) redis.Delete(KeyPOSDataSetup); // invalidate POS machine cache
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<bool> DeletePOSDataSetupAsync(string code, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.POSDataSetup WHERE Code = @code;";
        try
        {
            var rows = await ExecuteAsync(sql, new { code }, ct: ct);
            if (rows > 0) redis.Delete(KeyPOSDataSetup); // invalidate POS machine cache
            return rows > 0;
        }
        catch { return false; }
    }

    // ── Dashboard Audit Log ──────────────────────────────────────────────────

    public async Task InsertDashboardAuditLogAsync(
        string actor, string action, string entityType, string entityKey,
        string? oldValueJson = null, string? newValueJson = null,
        CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.DashboardAuditLog
                                 (Actor, Action, EntityType, EntityKey, OldValue, NewValue)
                             VALUES (@actor, @action, @entityType, @entityKey, @oldValueJson, @newValueJson);";
        try
        {
            await ExecuteAsync(sql,
                new { actor, action, entityType, entityKey, oldValueJson, newValueJson },
                ct: ct);
        }
        catch { /* audit failure must not interrupt main flow */ }
    }
}
