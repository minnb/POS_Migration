using System.Data;
using Dapper;
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
    private const string KeyBankList          = "MD:BankList";
    private const string KeyBranchList        = "MD:BranchList";
    private const string KeyCpnVchBOMHeader   = "MD:CpnVchBOMHeader";

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

    public async Task<List<BranchDto>> GetBranchListAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<BranchDto>>(KeyBranchList);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT No, Description
                             FROM dbo.Branch (NOLOCK)
                             ORDER BY No";
        var data = (await QueryAsync<BranchDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyBranchList, data, ttlSeconds: 43200);
        return data;
    }

    // ── Danh mục Chi nhánh / Tỉnh-Thành (Branch) ─────────────────────────────

    public async Task<List<BranchAdminDto>> GetBranchAdminListAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT No, Description, Address, VATRegistrationNo
                             FROM   dbo.Branch (NOLOCK)
                             ORDER  BY No";
        return (await QueryAsync<BranchAdminDto>(sql, ct: ct)).ToList();
    }

    public async Task<bool> BranchCodeExistsAsync(string branchNo, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Branch (NOLOCK) WHERE No = @branchNo;";
        var count = await QueryFirstOrDefaultAsync<int>(sql, new { branchNo = branchNo.Trim() }, ct: ct);
        return count > 0;
    }

    public async Task<bool> CreateBranchAsync(BranchCreateDto dto, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.Branch
                                 (No, Description, Address, VATRegistrationNo,
                                  PhoneNo, FaxNo, BankAccountNo, BankName, BankAddress, BankAcountName,
                                  VietnameseDescription, VietnameseAddress, UrlElecInvoice,
                                  Counter, Pkey)
                             VALUES
                                 (@No, @Description, @Address, @VATRegistrationNo,
                                  '', '', '', '', '', '',
                                  '', '', '',
                                  (SELECT ISNULL(MAX(Counter), 0) + 1 FROM dbo.Branch), @No);";
        try
        {
            var rows = await ExecuteAsync(sql, new
            {
                No                = dto.No.Trim(),
                Description       = dto.Description.Trim(),
                Address           = dto.Address?.Trim() ?? string.Empty,
                VATRegistrationNo = dto.VATRegistrationNo?.Trim() ?? string.Empty
            }, ct: ct);
            if (rows > 0) redis.Delete(KeyBranchList); // invalidate combobox Chi nhánh cache
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateBranchInfoAsync(string branchNo, string description, string? address, string? vatRegistrationNo, CancellationToken ct = default)
    {
        const string sql = @"UPDATE dbo.Branch
                             SET    Description       = @description,
                                    Address           = @address,
                                    VATRegistrationNo = @vatRegistrationNo,
                                    Counter           = (SELECT ISNULL(MAX(Counter), 0) + 1 FROM dbo.Branch)
                             WHERE  No = @branchNo;";
        try
        {
            var rows = await ExecuteAsync(sql, new
            {
                branchNo          = branchNo.Trim(),
                description       = description.Trim(),
                address           = address?.Trim() ?? string.Empty,
                vatRegistrationNo = vatRegistrationNo?.Trim() ?? string.Empty
            }, ct: ct);
            if (rows > 0) redis.Delete(KeyBranchList); // invalidate combobox Chi nhánh cache (Description đổi)
            return rows > 0;
        }
        catch { return false; }
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

    public async Task<bool> CpnVchBOMHeaderExistsAsync(string itemNo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(itemNo)) return false;

        // Cache dương (positive-only): đã xác nhận tồn tại thì luôn tồn tại → không lo stale
        // false-negative. Mã mới thêm vào CpnVchBOMHeader được nhận ngay ở lần query kế; mã không
        // tồn tại (hiếm, chỉ khi request sai) luôn re-check DB.
        var cached = redis.HashGet<bool?>(KeyCpnVchBOMHeader, itemNo);
        if (cached == true) return true;

        const string sql = "SELECT TOP 1 1 FROM dbo.CpnVchBOMHeader (NOLOCK) WHERE [ItemNo] = @itemNo;";
        var exists = await QueryFirstOrDefaultAsync<int?>(sql, new { itemNo }, ct: ct) != null;
        if (exists)
            redis.HashSet(KeyCpnVchBOMHeader, itemNo, true, ttlSeconds: 43200); // 12h — static master data
        return exists;
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

    public async Task<bool> StoreCodeExistsAsync(string storeNo, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Store (NOLOCK) WHERE No = @storeNo;";
        var count = await QueryFirstOrDefaultAsync<int>(sql, new { storeNo = storeNo.Trim() }, ct: ct);
        return count > 0;
    }

    public async Task<bool> CreateStoreAsync(StoreCreateDto dto, CancellationToken ct = default)
    {
        const string sql = @"INSERT INTO dbo.Store
                                 (No, Name, Address, BranchNo, ClosingMethod,
                                  LastDateModified, Counter, Pkey)
                             VALUES
                                 (@StoreNo, @Name, @Address, @BranchNo, @ClosingMethod,
                                  GETDATE(),
                                  (SELECT ISNULL(MAX(Counter), 0) + 1 FROM dbo.Store),
                                  @StoreNo);";
        try
        {
            var rows = await ExecuteAsync(sql, new
            {
                StoreNo = dto.StoreNo.Trim(),
                Name    = dto.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                BranchNo = string.IsNullOrWhiteSpace(dto.BranchNo) ? null : dto.BranchNo.Trim(),
                dto.ClosingMethod
            }, ct: ct);
            if (rows > 0) redis.Delete(KeyStoreList); // invalidate store picker cache
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<bool> UpdateStoreClosingMethodAsync(string storeNo, int closingMethod, CancellationToken ct = default)
    {
        const string sql = @"UPDATE dbo.Store
                             SET    ClosingMethod    = @closingMethod,
                                    LastDateModified = GETDATE(),
                                    Counter          = (SELECT ISNULL(MAX(Counter), 0) + 1 FROM dbo.Store)
                             WHERE  No = @storeNo;";
        try
        {
            var rows = await ExecuteAsync(sql, new { storeNo = storeNo.Trim(), closingMethod }, ct: ct);
            if (rows > 0) redis.Delete(KeyStoreList); // invalidate store picker cache (WHERE ClosingMethod = 0)
            return rows > 0;
        }
        catch { return false; }
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

    public async Task<bool> StaffCodeExistsAsync(string staffCode, CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Staff (NOLOCK) WHERE ID = @staffCode;";
        var count = await QueryFirstOrDefaultAsync<int>(sql, new { staffCode = staffCode.Trim() }, ct: ct);
        return count > 0;
    }

    public async Task<bool> CreateEmployeeAsync(EmployeeCreateDto dto, CancellationToken ct = default)
    {
        // Password plain text theo contract POS terminal (máy POS đọc trực tiếp cột này).
        // Counter = MAX+1 toàn bảng — bắt buộc, POS sync incremental lọc WHERE Counter > N.
        const string sql = @"INSERT INTO dbo.Staff
                                 (ID, Password, StoreNo, VoidTransaction, FirstName, LastName,
                                  EmploymentType, Blocked, PermissionGroup, HomePhoneNo,
                                  LastDateModified, Counter, Pkey)
                             VALUES
                                 (@StaffCode, @Password, @StoreNo, @VoidTransaction, @StaffName, @StaffName,
                                  @EmploymentType, @Blocked, @PermissionGroup, @HomePhoneNo,
                                  GETDATE(),
                                  (SELECT ISNULL(MAX(Counter), 0) + 1 FROM dbo.Staff),
                                  @StaffCode);";
        try
        {
            var rows = await ExecuteAsync(sql, new
            {
                StaffCode = dto.StaffCode.Trim(),
                dto.Password,
                StoreNo   = dto.StoreNo.Trim(),
                dto.VoidTransaction,
                StaffName = dto.StaffName.Trim(),
                dto.EmploymentType,
                Blocked   = (int)dto.Blocked,
                dto.PermissionGroup,
                HomePhoneNo = string.IsNullOrWhiteSpace(dto.HomePhoneNo) ? null : dto.HomePhoneNo.Trim()
            }, ct: ct);
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<bool> ChangeEmployeePasswordAsync(string staffCode, string newPassword, CancellationToken ct = default)
    {
        // Theo legacy ChangePassWord: chỉ đổi khi đang hoạt động (Blocked = 0); Pkey = ID.
        const string sql = @"UPDATE dbo.Staff
                             SET    Password         = @newPassword,
                                    LastDateModified = GETDATE(),
                                    Counter          = (SELECT ISNULL(MAX(Counter), 0) + 1 FROM dbo.Staff),
                                    Pkey             = ID
                             WHERE  ID = @staffCode
                               AND  (Blocked = 0 OR Blocked IS NULL);";
        try
        {
            var rows = await ExecuteAsync(sql, new { staffCode = staffCode.Trim(), newPassword = newPassword.Trim() }, ct: ct);
            return rows > 0;
        }
        catch { return false; }
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

    // ── BankPOS — Máy POS Ngân hàng (migrate 5.5) ───────────────────────────

    public async Task<List<BankPOSListDto>> GetBankPOSListAsync(CancellationToken ct = default)
    {
        // SP không có @Export; IsOnline/Status trả về text tiếng Việt (IIF), Counter/ngày đã
        // Convert/Format sẵn thành chuỗi — map qua BankPOSListRow rồi convert sang kiểu UI cần.
        const string sql = "[dbo].[GetBankPOSList] @StoreNo,@TextSearch,@BankCode,@Status,@PageSize,@PageNumber";
        var rows = await QueryAsync<BankPOSListRow>(sql, new
        {
            StoreNo    = string.Empty,
            TextSearch = string.Empty,
            BankCode   = string.Empty,
            Status     = string.Empty,
            PageSize   = 9999,
            PageNumber = 0
        }, commandTimeout: 60, ct: ct);

        return rows.Select(r => new BankPOSListDto
        {
            BankPOSCode    = r.BankPOSCode,
            BankPOSName    = r.BankPOSName,
            BankCode       = r.BankCode,
            StoreNo        = r.StoreNo,
            StoreName      = r.StoreName,
            POSNo          = r.POSNo,
            POSTerminal    = r.POSTerminal,
            AccessKey      = r.AccessKey,
            PartnerId      = r.PartnerId,
            IsOnline       = r.IsOnline == "Có",
            Status         = r.Status == "Đang được sử dụng" ? 1 : 0,
            StatusText     = r.Status,
            Counter        = r.Counter,
            CreatedDateStr = r.CreatedDateStr,
            CreatedUser    = r.CreatedUser,
            UpdatedDateStr = r.UpdatedDateStr,
            UpdatedUser    = r.UpdatedUser
        }).ToList();
    }

    // Khớp đúng cột SP [dbo].[GetBankPOSList] trả về — không dùng trực tiếp BankPOSListDto vì
    // IsOnline/Status là text tiếng Việt (IIF) và Counter/ngày đã format sẵn thành chuỗi.
    private sealed class BankPOSListRow
    {
        public string  BankPOSCode    { get; set; } = string.Empty;
        public string? BankPOSName    { get; set; }
        public string? BankCode       { get; set; }
        public string? StoreNo        { get; set; }
        public string? StoreName      { get; set; }
        public string? POSNo          { get; set; }
        public string? POSTerminal    { get; set; }
        public string? AccessKey      { get; set; }
        public string? PartnerId      { get; set; }
        public string  IsOnline       { get; set; } = string.Empty;
        public string? Status         { get; set; }
        public string? Counter        { get; set; }
        public string? CreatedDateStr { get; set; }
        public string? CreatedUser    { get; set; }
        public string? UpdatedDateStr { get; set; }
        public string? UpdatedUser    { get; set; }
    }

    public async Task<(bool success, bool duplicateCode)> SaveBankPOSAsync(
        BankPOSSaveDto dto, string actor, CancellationToken ct = default)
    {
        if (dto.IsNew)
        {
            const string checkSql = "SELECT COUNT(1) FROM dbo.POSTerminalBank (NOLOCK) WHERE BankPOSCode = @BankPOSCode;";
            var exists = await QueryFirstOrDefaultAsync<int>(checkSql, new { dto.BankPOSCode }, ct: ct);
            if (exists > 0) return (false, true);

            const string sql = @"INSERT INTO dbo.POSTerminalBank
                                     (BankPOSCode, BankPOSName, BankCode, StoreNo, StoreNoFull,
                                      POSNo, POSTerminal, AccessKey, PartnerId,
                                      IsOnline, Status, Counter, CreatedDate, CreatedUser)
                                 VALUES
                                     (@BankPOSCode, @BankPOSName, @BankCode, @StoreNo, @StoreNo,
                                      @POSNo, @POSTerminal, @AccessKey, @PartnerId,
                                      @IsOnline, @Status, 1, GETDATE(), @Actor);";
            try
            {
                await ExecuteAsync(sql, new
                {
                    dto.BankPOSCode, dto.BankPOSName, dto.BankCode, dto.StoreNo,
                    dto.POSNo, dto.POSTerminal, dto.AccessKey, dto.PartnerId,
                    dto.IsOnline, dto.Status, Actor = actor
                }, ct: ct);
                return (true, false);
            }
            catch { return (false, false); }
        }
        else
        {
            const string sql = @"UPDATE dbo.POSTerminalBank
                                 SET    BankPOSName  = @BankPOSName,
                                        BankCode     = @BankCode,
                                        POSNo        = @POSNo,
                                        POSTerminal  = @POSTerminal,
                                        AccessKey    = @AccessKey,
                                        PartnerId    = @PartnerId,
                                        IsOnline     = @IsOnline,
                                        Status       = @Status,
                                        Counter      = ISNULL(Counter, 0) + 1,
                                        UpdatedDate  = GETDATE(),
                                        UpdatedUser  = @Actor
                                 WHERE  BankPOSCode  = @BankPOSCode;";
            try
            {
                var rows = await ExecuteAsync(sql, new
                {
                    dto.BankPOSCode, dto.BankPOSName, dto.BankCode,
                    dto.POSNo, dto.POSTerminal, dto.AccessKey, dto.PartnerId,
                    dto.IsOnline, dto.Status, Actor = actor
                }, ct: ct);
                return (rows > 0, false);
            }
            catch { return (false, false); }
        }
    }

    public async Task<bool> DeleteBankPOSAsync(string bankPOSCode, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dbo.POSTerminalBank WHERE BankPOSCode = @bankPOSCode;";
        try
        {
            var rows = await ExecuteAsync(sql, new { bankPOSCode }, ct: ct);
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<List<BankDropdownDto>> GetBankListForDropdownAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<BankDropdownDto>>(KeyBankList);
        if (cached?.Count > 0) return cached;

        const string sql = "SELECT BankCode, BankName FROM dbo.Bank (NOLOCK) ORDER BY BankCode;";
        var data = (await QueryAsync<BankDropdownDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyBankList, data, ttlSeconds: 43200);
        return data;
    }

    // ── Product Create — Tạo sản phẩm mới (migrate 6.2) ─────────────────────

    private const string KeyArticleTypes   = "MD:ArticleTypes";
    private const string KeyUnitOfMeasures = "MD:UnitOfMeasures";

    public async Task<List<ArticleTypeDto>> GetArticleTypesAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<ArticleTypeDto>>(KeyArticleTypes);
        if (cached?.Count > 0) return cached;

        // Tên cột "Code" — xác nhận với DBA nếu khác.
        const string sql = "SELECT Code FROM dbo.ArticleType (NOLOCK) ORDER BY Code;";
        var data = (await QueryAsync<ArticleTypeDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyArticleTypes, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<UnitOfMeasureDto>> GetUnitOfMeasuresAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<UnitOfMeasureDto>>(KeyUnitOfMeasures);
        if (cached?.Count > 0) return cached;

        // Tên cột "Code" — xác nhận với DBA nếu khác.
        const string sql = "SELECT Code FROM dbo.UnitOfMeasure (NOLOCK) ORDER BY Code;";
        var data = (await QueryAsync<UnitOfMeasureDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyUnitOfMeasures, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<(bool Success, string ItemNo, string Message)> CreateProductAsync(
        ProductCreateDto dto, CancellationToken ct = default)
    {
        var p = new DynamicParameters();
        p.Add("@ItemName", dto.ItemName);
        p.Add("@ItemNameFull", dto.ItemNameFull);
        p.Add("@BaseUnitOfMeasure", dto.BaseUnitOfMeasure);
        p.Add("@SalesUnitOfMeasure", dto.SalesUnitOfMeasure);
        p.Add("@ItemFamilyCode", dto.ItemFamilyCode);
        p.Add("@TaxGroupCode", dto.TaxGroupCode == "-1" ? string.Empty : dto.TaxGroupCode);
        p.Add("@Blocked", dto.Blocked);
        p.Add("@BlockedVINID", dto.BlockedVINID);
        p.Add("@Barcodes", BuildProductBarcodeTable(dto.Barcodes).AsTableValuedParameter("dbo.ProductBarcodeTVP"));
        p.Add("@OutItemNo", dbType: DbType.String, direction: ParameterDirection.Output, size: 20);

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("dbo.usp_Product_Save", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));

        var itemNo = p.Get<string>("@OutItemNo") ?? string.Empty;
        return (true, itemNo, $"Thêm mới thành công. Mã sản phẩm: {itemNo}");
    }

    private static DataTable BuildProductBarcodeTable(IEnumerable<BarcodeRowDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("BarcodeNo", typeof(string));
        t.Columns.Add("UnitOfMeasureCode", typeof(string));
        foreach (var r in rows)
            t.Rows.Add(r.BarcodeNo, r.UnitOfMeasureCode);
        return t;
    }

    // ── Product Lock — Khóa sản phẩm (migrate 6.4) ───────────────────────────

    public async Task<(List<ProductLockItemDto> Items, int Total)> GetProductLockListAsync(
        ProductLockFilter filter, CancellationToken ct = default)
    {
        const string sql = @"
SELECT  i.No               AS ItemNo,
        i.Description      AS ItemName,
        i.SalesUnitOfMeasure AS UnitOfMeasure,
        ISNULL(CAST(ib.Status AS BIT), 0) AS IsLocked,
        ib.UpdatedDate,
        COUNT(*) OVER()    AS Total
FROM    dbo.Item i (NOLOCK)
LEFT JOIN dbo.ItemBlock ib (NOLOCK)
       ON ib.ItemNo = i.No AND ib.StoreNo = @StoreNo
WHERE   (@ItemNo   = '' OR i.No          LIKE '%' + @ItemNo   + '%')
  AND   (@ItemName = '' OR i.Description LIKE '%' + @ItemName + '%')
  AND   (@Status = -1
         OR (@Status = 1 AND ib.Status = 1)
         OR (@Status = 0 AND (ib.Status = 0 OR ib.Status IS NULL)))
ORDER BY i.No
OFFSET  @PageNumber * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        var rows = (await QueryAsync<ProductLockItemDto>(sql, new
        {
            filter.StoreNo,
            ItemNo    = filter.ItemNo.Trim(),
            ItemName  = filter.ItemName.Trim(),
            filter.Status,
            PageSize   = Math.Max(1, filter.PageSize),
            PageNumber = Math.Max(0, filter.PageNumber)
        }, commandTimeout: 60, ct: ct)).ToList();

        var total = rows.FirstOrDefault()?.Total ?? 0;
        return (rows, total);
    }

    public async Task<(bool Success, string Message)> SaveProductLockAsync(
        ProductLockSaveDto dto, CancellationToken ct = default)
    {
        if (dto.ItemNos.Count == 0)
            return (false, "Danh sách sản phẩm không được rỗng");

        // Fetch UoM for each item in one query
        var itemNoList = string.Join(",", dto.ItemNos.Select(n => $"'{n.Replace("'", "''")}'"));
        var sqlUom = $"SELECT No, SalesUnitOfMeasure FROM dbo.Item (NOLOCK) WHERE No IN ({itemNoList});";
        var uomMap = (await QueryAsync<(string No, string SalesUnitOfMeasure)>(sqlUom, commandTimeout: 30, ct: ct))
            .ToDictionary(x => x.No, x => x.SalesUnitOfMeasure ?? string.Empty);

        const string sqlUpsert = @"
                        IF EXISTS (SELECT 1 FROM dbo.ItemBlock WHERE Pkey = @Pkey)
                            UPDATE dbo.ItemBlock
                            SET    Status = @Status, UpdatedDate = GETDATE(), Counter = Counter + 1
                            WHERE  Pkey = @Pkey
                        ELSE
                            INSERT INTO dbo.ItemBlock (ItemNo, UnitOfMeasure, StoreNo, Status, UpdatedDate, Counter, Pkey)
                            VALUES (@ItemNo, @UnitOfMeasure, @StoreNo, @Status, GETDATE(), 1, @Pkey);";

        await ExecuteInTransactionAsync(async (conn, tx) =>
        {
            foreach (var itemNo in dto.ItemNos)
            {
                var pkey = $"{dto.StoreNo}-{itemNo}";
                uomMap.TryGetValue(itemNo, out var uom);
                await conn.ExecuteAsync(
                    new CommandDefinition(sqlUpsert, new
                    {
                        Pkey          = pkey,
                        Status        = dto.TargetLock,
                        ItemNo        = itemNo,
                        UnitOfMeasure = uom ?? string.Empty,
                        dto.StoreNo
                    }, transaction: tx, commandTimeout: 30, cancellationToken: ct));
            }
        }, ct: ct);

        var action = dto.TargetLock ? "Khóa" : "Mở khóa";
        return (true, $"{action} thành công {dto.ItemNos.Count} sản phẩm");
    }

    // ── Dashboard Audit Log ──────────────────────────────────────────────────

    // ── Product List — Danh mục SP / Barcode (migrate 6.1) ──────────────────

    private const string KeyPosVatCodes = "MD:PosVatCodes";

    public async Task<(List<ProductListItemDto> Items, int Total)> GetProductListAsync(
        ProductListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var rows = (await conn.QueryAsync<ProductListItemDto>(
            new CommandDefinition(
                "[dbo].[GetProductList]",
                new
                {
                    ItemCode   = filter.ItemNo.Trim(),
                    ItemName   = filter.ItemName.Trim(),
                    BarCode    = filter.BarcodeNo.Trim(),
                    TaxCode    = filter.TaxCode.Trim(),
                    PageSize   = Math.Max(1, filter.PageSize),
                    PageNumber = Math.Max(0, filter.PageNumber)
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120,
                cancellationToken: ct))).ToList();

        var total = rows.FirstOrDefault()?.Total ?? 0;
        return (rows, total);
    }

    public async Task<List<ProductListItemDto>> ExportProductListAsync(
        ProductListFilter filter, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        return (await conn.QueryAsync<ProductListItemDto>(
            new CommandDefinition(
                "[dbo].[GetProductList_Export]",
                new
                {
                    ItemCode = filter.ItemNo.Trim(),
                    ItemName = filter.ItemName.Trim(),
                    BarCode  = filter.BarcodeNo.Trim(),
                    TaxCode  = filter.TaxCode.Trim()
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120,
                cancellationToken: ct))).ToList();
    }

    public async Task<List<PosVatCodeDto>> GetPosVatCodesAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<PosVatCodeDto>>(KeyPosVatCodes);
        if (cached?.Count > 0) return cached;

        // Tên cột (Code, Description) cần xác nhận với DBA nếu bảng dùng tên khác.
        const string sql = "SELECT VATCode AS Code, Description FROM dbo.POSVATCode (NOLOCK) ORDER BY Code;";
        var data = (await QueryAsync<PosVatCodeDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyPosVatCodes, data, ttlSeconds: 43200);
        return data;
    }

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
