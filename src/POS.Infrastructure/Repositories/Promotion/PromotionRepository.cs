using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using POS.Common.Dtos.Promotion;
using POS.Infrastructure.Database;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;
using POS.Infrastructure.Sync;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

namespace POS.Infrastructure.Repositories;

/// <summary>
/// Danh mục khuyến mãi (Offer Header) — DB RPOSMasterData (CentralMD).
/// SP server-side paging trả Total trên mỗi row (giống pattern CentralMDRepository.GetEmployeeListAsync).
/// </summary>
public sealed class PromotionRepository(
    CentralMDConnectionFactory connectionFactory,
    IRedisService redis,
    IFileLogHelper fileLogHelper,
    ISyncTableTrackerService syncTracker)
    : BaseRepository(connectionFactory), IPromotionRepository
{
    private const string KeyOfferTypeOptions      = "MD:OfferTypeOptions";
    private const string KeySalesOrderTypeOptions = "MD:SalesOrderTypeOptions";
    private const string KeySiteGroupOptions      = "MD:SiteGroupOptions";
    private const string KeyMemberCodeOptions     = "MD:MemberCodeOptions";

    // Dải error number nghiệp vụ có chủ đích (THROW trong SP, xem SetupPromotion_Save.sql
    // "BẢN SỬA LẦN 3") — an toàn hiện y nguyên message cho end-user. Lỗi SqlException khác
    // (kết nối/timeout/SP chưa deploy...) bị coi là kỹ thuật, KHÔNG hiện message thô ra UI.
    private static readonly HashSet<int> KnownBusinessErrorNumbers = [51001, 51002];

    // PageSize lớn để lấy toàn bộ theo filter khi export (SP vẫn OFFSET/FETCH, constant memory phía SQL).
    private const int ExportPageSize = 100_000;

    public async Task<(List<OfferHeaderListItemDto> Items, int Total)> GetOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default)
    {
        var items = (await QueryAsync<OfferHeaderListItemDto>(Sql, BuildParams(filter,
            Math.Max(0, filter.PageNumber), Math.Max(1, filter.PageSize)),
            commandTimeout: 120, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<List<OfferHeaderListItemDto>> ExportOfferHeaderListAsync(
        OfferListFilter filter, CancellationToken ct = default)
        => (await QueryAsync<OfferHeaderListItemDto>(Sql, BuildParams(filter, 0, ExportPageSize),
            commandTimeout: 120, ct: ct)).ToList();

    public async Task<List<OfferTypeOptionDto>> GetOfferTypeOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OfferTypeOptionDto>>(KeyOfferTypeOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [OfferType] AS Value, [OfferType] + '-' + [OfferName] AS Text,
                                    ISNULL([IsTotalBill],0) AS IsTotalBill, ISNULL([IsSetupBuy],0) AS IsSetupBuy,
                                    ISNULL([IsSetupGet],0) AS IsSetupGet, ISNULL([IsVoucher],0) AS IsVoucher,
                                    ISNULL([IsGift],0) AS IsGift, ISNULL([UserGuide],'') AS UserGuide
                             FROM   dbo.OfferType (NOLOCK)
                             WHERE  ISNULL([Enabled], 0) = 1
                             ORDER  BY [OfferType]";
        var data = (await QueryAsync<OfferTypeOptionDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyOfferTypeOptions, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<OptionItemDto>> GetSalesOrderTypeOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OptionItemDto>>(KeySalesOrderTypeOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [Code] AS Value, [Description] AS Text
                             FROM   dbo.SalesOrderType (NOLOCK)
                             WHERE  ISNULL([IsActive], 0) = 1
                             ORDER  BY [Code]";
        var data = (await QueryAsync<OptionItemDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeySalesOrderTypeOptions, data, ttlSeconds: 43200);
        return data;
    }

    // ── Cài đặt CTKM (11.1) ──────────────────────────────────────────────────

    public async Task<(List<PromotionSetupListItemDto> Items, int Total)> GetSetupListAsync(
        PromotionSetupListFilter filter, CancellationToken ct = default)
    {
        // @ItemNo khớp barcode ở SetupPromotionBUY.MAT_NR / SetupPromotionGET.MATERIALCODE —
        // giống tinh thần UNION OfferBuy/OfferGet/OfferBenefits của SP GetPromotionOfferHeaderList
        // (OffersPage.razor) nhưng ở đây query trực tiếp 2 bảng flat (không có OfferBenefits ở
        // nhóm Setup). @FromDate lọc CTKM còn hiệu lực từ ngày này trở đi (VALIDTO >= @FromDate,
        // string yyyyMMdd — CHỈ lọc mốc đầu, giống quyết định đã áp dụng ở GetPromotionOfferHeaderList,
        // xem docs/sql/GetPromotionOfferHeaderList_AddDateRangeFilter.sql).
        const string sql = @"
            SELECT  H.BBYNR              AS No,
                    ISNULL(H.BBYTEXT,'') AS Description,
                    ISNULL(H.BBYTYPE,'') AS OfferType,
                    ISNULL(H.SalesType,'') AS SalesType,
                    ISNULL(H.STATUS,'')  AS Status,
                    ISNULL(H.VALIDFROM,'') AS ValidFrom,
                    ISNULL(H.VALIDTO,'') AS ValidTo,
                    ISNULL(H.IsApprove,0) AS IsApprove,
                    COUNT(*) OVER()    AS Total
            FROM    dbo.SetupPromotionHEADER H (NOLOCK)
            WHERE   (@OfferNo = '' OR H.BBYNR = @OfferNo)
              AND   (@OfferName = '' OR H.BBYTEXT LIKE '%' + @OfferName + '%')
              AND   (@ApproveStatus = ''
                     OR ISNULL(H.IsApprove,0) = CASE WHEN @ApproveStatus = '1' THEN 1 ELSE 0 END)
              AND   (@OfferType = '' OR H.BBYTYPE = @OfferType)
              AND   (@Status = '' OR ISNULL(H.STATUS,'') = @Status)
              AND   (@FromDate = '' OR ISNULL(H.VALIDTO,'') >= @FromDate)
              AND   (@ItemNo = '' OR EXISTS (
                        SELECT 1 FROM dbo.SetupPromotionBUY B (NOLOCK)
                        WHERE B.BBYNR = H.BBYNR AND B.MAT_NR = @ItemNo
                        UNION ALL
                        SELECT 1 FROM dbo.SetupPromotionGET G (NOLOCK)
                        WHERE G.BBYNR = H.BBYNR AND G.MATERIALCODE = @ItemNo
                    ))
            ORDER BY H.BBYNR DESC
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await QueryAsync<PromotionSetupListItemDto>(sql, new
        {
            OfferNo       = (filter.OfferNo ?? string.Empty).Trim(),
            OfferName     = (filter.OfferName ?? string.Empty).Trim(),
            ApproveStatus = (filter.ApproveStatus ?? string.Empty).Trim(),
            ItemNo        = (filter.ItemNo ?? string.Empty).Trim(),
            OfferType     = (filter.OfferType ?? string.Empty).Trim(),
            Status        = (filter.Status ?? string.Empty).Trim(),
            FromDate      = filter.FromDate?.ToString("yyyyMMdd") ?? string.Empty,
            PageSize      = Math.Max(1, filter.PageSize),
            PageNumber    = Math.Max(0, filter.PageNumber)
        }, commandTimeout: 120, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<PromotionSetupDetailDto?> GetSetupDetailAsync(string bbynr, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  BBYNR AS No, ISNULL(BBYTEXT,'') AS Description, ISNULL(SalesType,'') AS SalesType,
                    ISNULL(BBYTYPE,'') AS OfferType, ISNULL(STATUS,'1') AS Status,
                    ISNULL(VALIDFROM,'') AS StartingDate, ISNULL(VALIDTO,'') AS EndingDate,
                    ISNULL(IsVoucher,0) AS IsVoucher, ISNULL(IsApprove,0) AS IsApprove,
                    CASE WHEN BUYLINKCAT='O' THEN 'OR' ELSE 'AND' END AS ConditionBuy,
                    CASE WHEN GETLINKCAT='O' THEN 'OR' ELSE 'AND' END AS ConditionGet,
                    ISNULL(TRY_CONVERT(decimal(18,3), LIMIT),0) AS LimitQty,
                    CAST(CASE WHEN VINID='X' THEN 1 ELSE 0 END AS bit) AS MemberOnly,
                    ISNULL(MemberCode,'') AS MemberCode,
                    ISNULL(TRY_CONVERT(int, ZPRIOR),1) AS PriorityBBY,
                    ISNULL(TRY_CONVERT(int, NUMOFDAYS),0) AS NumOfDays,
                    ISNULL(ZVCDATE_ST,'') AS VoucherFromDate,
                    ISNULL(ZVCDATE_EN,'') AS VoucherToDate,
                    ISNULL(TRY_CONVERT(int, ZVCDATE_VA),0) AS VoucherValidDay,
                    ISNULL(TRY_CONVERT(int, LIMITNR),0) AS VoucherLimitNumber,
                    ISNULL(TRY_CONVERT(int, ZVCDAY_AFTER),0) AS AllowUseAfterDay,
                    ISNULL(ZVCTIME_AFTER,'') AS AllowUseAfterTime,
                    ISNULL(TIMEFROM,'') AS FromTime, ISNULL(TIMETO,'') AS ToTime,
                    CAST(CASE WHEN MON='X' THEN 1 ELSE 0 END AS bit) AS Mon,
                    CAST(CASE WHEN TUE='X' THEN 1 ELSE 0 END AS bit) AS Tue,
                    CAST(CASE WHEN WED='X' THEN 1 ELSE 0 END AS bit) AS Wed,
                    CAST(CASE WHEN THUR='X' THEN 1 ELSE 0 END AS bit) AS Thu,
                    CAST(CASE WHEN FRI='X' THEN 1 ELSE 0 END AS bit) AS Fri,
                    CAST(CASE WHEN SAT='X' THEN 1 ELSE 0 END AS bit) AS Sat,
                    CAST(CASE WHEN SUN='X' THEN 1 ELSE 0 END AS bit) AS Sun,
                    ISNULL(TRY_CONVERT(decimal(18,3), MINVALUE),0) AS MinValue,
                    CAST(CASE WHEN TOTALDISCOUNT='X' THEN 1 ELSE 0 END AS bit) AS CheckTotalDiscount,
                    ISNULL(TRY_CONVERT(int, TOTALDISCOUNTTYPE),0) AS TotalDiscountType,
                    ISNULL(TRY_CONVERT(decimal(18,3), TOTALDISCOUNTVALUE),0) AS TotalDiscountValue
            FROM    dbo.SetupPromotionHEADER (NOLOCK) WHERE BBYNR = @bbynr;

            -- Đọc riêng NUMOFDAYSLIST (JSON array nhiều ngày trong tháng, cột mới) — KHÔNG map
            -- trực tiếp vào PromotionSetupHeaderDto vì tên cột không khớp property nào (tránh
            -- Dapper cố ép kiểu) — parse thủ công vào header.ApplyDaysOfMonth bên dưới.
            SELECT  ISNULL(NUMOFDAYSLIST,'') AS Raw
            FROM    dbo.SetupPromotionHEADER (NOLOCK) WHERE BBYNR = @bbynr;

            SELECT  CASE WHEN b.BUYTYPE='MGP' THEN 1 ELSE 0 END AS LineType,
                    ISNULL(b.MAT_NR,'') AS No, ISNULL(b.MATGROUP,'') AS GroupCode,
                    ISNULL(i.Description,'') AS Description,
                    ISNULL(b.MEINH,'') AS UnitOfMeasure,
                    ISNULL(TRY_CONVERT(decimal(18,3), b.MAT_QUAN),0) AS Quantity,
                    ISNULL(b.ScaleType,'C') AS ScaleType
            FROM    dbo.SetupPromotionBUY (NOLOCK) b
            LEFT JOIN dbo.Item (NOLOCK) i ON i.No = b.MAT_NR
            WHERE   b.BBYNR = @bbynr ORDER BY b.ID;

            SELECT  CASE WHEN g.GETTYPE='MGP' THEN 1 ELSE 0 END AS LineType,
                    ISNULL(g.MATERIALCODE,'') AS No, ISNULL(g.MATGROUP,'') AS GroupCode,
                    ISNULL(i.Description,'') AS Description,
                    ISNULL(g.MEINH,'') AS UnitOfMeasure,
                    ISNULL(TRY_CONVERT(decimal(18,3), g.QTY),0) AS Quantity,
                    ISNULL(g.SCALETYPE,'C') AS ScaleType,
                    CASE g.DISTYPE WHEN '%' THEN 0 WHEN 'R' THEN 1 WHEN 'P' THEN 2 ELSE 0 END AS DiscountType,
                    ISNULL(TRY_CONVERT(decimal(18,3), CASE WHEN g.DISTYPE='%' THEN g.BBYPER ELSE g.BBYVAL END),0) AS DiscountValue
            FROM    dbo.SetupPromotionGET (NOLOCK) g
            LEFT JOIN dbo.Item (NOLOCK) i ON i.No = g.MATERIALCODE
            WHERE   g.BBYNR = @bbynr ORDER BY g.ID;

            SELECT  DISTINCT s.SITEGROUPCODE AS SiteGroupCode, ISNULL(g.GroupName,'') AS GroupName
            FROM    dbo.SetupPromotionSITE (NOLOCK) s
            LEFT JOIN dbo.SetupGroupSite (NOLOCK) g ON g.GroupCode = s.SITEGROUPCODE
            WHERE   s.BBYNR = @bbynr;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(sql, new { bbynr }, commandTimeout: 120, cancellationToken: ct));

        var header = await multi.ReadFirstOrDefaultAsync<PromotionSetupHeaderDto>();
        if (header == null) return null;

        var numOfDaysListRaw = await multi.ReadFirstOrDefaultAsync<string>() ?? string.Empty;
        header.ApplyDaysOfMonth = ParseDaysOfMonth(numOfDaysListRaw);

        var buys = (await multi.ReadAsync<OfferBuyLineDto>()).ToList();
        var gets = (await multi.ReadAsync<OfferGetLineDto>()).ToList();
        var sites = (await multi.ReadAsync<OfferSiteLineDto>()).ToList();

        // VALIDFROM/VALIDTO lưu yyyyMMdd → đổi sang dd/MM/yyyy cho form
        header.StartingDate = YmdToDisplay(header.StartingDate);
        header.EndingDate = YmdToDisplay(header.EndingDate);
        // Voucher date: sentinel 19000101 / length≠8 → rỗng
        header.VoucherFromDate = YmdToVoucherDisplay(header.VoucherFromDate);
        header.VoucherToDate = YmdToVoucherDisplay(header.VoucherToDate);

        // Bản ghi cũ tạo trước khi có field Mon..Sun → cả 7 cột rỗng ở DB → fallback cả tuần
        // để tránh hiển thị sai "CTKM cũ không áp dụng ngày nào".
        if (!header.Mon && !header.Tue && !header.Wed && !header.Thu && !header.Fri && !header.Sat && !header.Sun)
        {
            header.Mon = header.Tue = header.Wed = header.Thu = header.Fri = header.Sat = header.Sun = true;
        }

        return new PromotionSetupDetailDto { Header = header, BuyRows = buys, GetRows = gets, SiteRows = sites };
    }

    public async Task<(bool Ok, string Message, string BBYNR)> SaveSetupAsync(
        PromotionSetupSaveRequest request, CancellationToken ct = default)
    {
        var h = request.Header;

        // ── Validate ──
        if (string.IsNullOrWhiteSpace(h.Description))
            return (false, "Vui lòng nhập tên chương trình khuyến mãi", string.Empty);
        if (string.IsNullOrWhiteSpace(h.SalesType))
            return (false, "Vui lòng chọn hình thức bán hàng", string.Empty);
        if (string.IsNullOrWhiteSpace(h.OfferType) && string.IsNullOrWhiteSpace(h.No))
            return (false, "Vui lòng chọn loại CTKM", string.Empty);
        if (!new[] { "0", "1", "2" }.Contains(h.Status))
            return (false, "Trạng thái không hợp lệ", string.Empty);
        if (!DateTime.TryParseExact(h.StartingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            return (false, "Ngày bắt đầu không đúng định dạng (dd/MM/yyyy)", string.Empty);
        if (!DateTime.TryParseExact(h.EndingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            return (false, "Ngày kết thúc không đúng định dạng (dd/MM/yyyy)", string.Empty);
        if (endDate < startDate)
            return (false, "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu", string.Empty);
        if (h.CheckTotalDiscount && h.TotalDiscountValue <= 0)
            return (false, "Vui lòng nhập giá trị khuyến mãi cho giảm giá tổng bill", string.Empty);
        if (!string.IsNullOrEmpty(h.FromTime) || !string.IsNullOrEmpty(h.ToTime))
        {
            if (string.IsNullOrEmpty(h.FromTime) || string.IsNullOrEmpty(h.ToTime))
                return (false, "Vui lòng nhập đủ Từ giờ và Đến giờ", string.Empty);
            if (!TimeSpan.TryParseExact(h.FromTime, "hh\\:mm", CultureInfo.InvariantCulture, out var fromTime)
                || !TimeSpan.TryParseExact(h.ToTime, "hh\\:mm", CultureInfo.InvariantCulture, out var toTime))
                return (false, "Từ giờ/Đến giờ không đúng định dạng (HH:mm)", string.Empty);
            if (toTime <= fromTime)
                return (false, "Đến giờ phải lớn hơn Từ giờ", string.Empty);
        }

        // ── Expand nhóm cửa hàng → (SiteGroupCode, SiteCode) ──
        var siteTable = await BuildSiteTableAsync(request.SiteGroupCodes, ct);

        var p = new DynamicParameters();
        p.Add("@BBYNR", string.IsNullOrWhiteSpace(h.No) ? string.Empty : h.No.Trim(),
              DbType.String, ParameterDirection.InputOutput, 20);
        p.Add("@SalesType", h.SalesType);
        p.Add("@Description", h.Description);
        p.Add("@OfferType", h.OfferType ?? string.Empty);
        p.Add("@Status", h.Status);
        p.Add("@ValidFrom", startDate.ToString("yyyyMMdd"));
        p.Add("@ValidTo", endDate.ToString("yyyyMMdd"));
        p.Add("@IsVoucher", h.IsVoucher);
        p.Add("@BuyLinkCat", h.ConditionBuy == "OR" ? "O" : "A");
        p.Add("@GetLinkCat", h.ConditionGet == "OR" ? "O" : "A");
        // ── Advanced (Phase 2) ──
        p.Add("@LimitQty", h.LimitQty.ToString(CultureInfo.InvariantCulture));
        p.Add("@MemberOnly", h.MemberOnly);
        p.Add("@MemberCode", h.MemberCode ?? string.Empty);
        p.Add("@Priority", (h.PriorityBBY <= 0 ? 1 : h.PriorityBBY).ToString(CultureInfo.InvariantCulture));
        p.Add("@NumOfDays", h.NumOfDays.ToString(CultureInfo.InvariantCulture));
        // Cột mới NUMOFDAYSLIST — nhiều ngày trong tháng, JSON array. KHÔNG đụng @NumOfDays/
        // NUMOFDAYS ở trên (SP legacy Setup_Promotion_Insert publish sang OfferHeader qua
        // Convert(int, NUMOFDAYS) — đổi sang mảng sẽ làm lỗi convert lúc Duyệt CTKM).
        p.Add("@NumOfDaysList", JsonConvert.SerializeObject(
            h.ApplyDaysOfMonth.Where(d => d is >= 1 and <= 31).Distinct().OrderBy(d => d).ToList()));
        p.Add("@VoucherFrom", DmyToYmd(h.VoucherFromDate));
        p.Add("@VoucherTo", DmyToYmd(h.VoucherToDate));
        p.Add("@VoucherValidDay", h.VoucherValidDay.ToString(CultureInfo.InvariantCulture));
        p.Add("@VoucherLimitNumber", h.VoucherLimitNumber.ToString(CultureInfo.InvariantCulture));
        p.Add("@AllowUseAfterDay", h.AllowUseAfterDay.ToString(CultureInfo.InvariantCulture));
        p.Add("@AllowUseAfterTime", h.AllowUseAfterTime ?? string.Empty);
        p.Add("@FromTime", h.FromTime ?? string.Empty);
        p.Add("@ToTime", h.ToTime ?? string.Empty);
        p.Add("@Mon", h.Mon ? "X" : "");
        p.Add("@Tue", h.Tue ? "X" : "");
        p.Add("@Wed", h.Wed ? "X" : "");
        p.Add("@Thu", h.Thu ? "X" : "");
        p.Add("@Fri", h.Fri ? "X" : "");
        p.Add("@Sat", h.Sat ? "X" : "");
        p.Add("@Sun", h.Sun ? "X" : "");
        p.Add("@MinValue", h.MinValue.ToString(CultureInfo.InvariantCulture));
        p.Add("@CheckTotalDiscount", h.CheckTotalDiscount ? "X" : "");
        p.Add("@TotalDiscountType", h.TotalDiscountType.ToString(CultureInfo.InvariantCulture));
        p.Add("@TotalDiscountValue", h.TotalDiscountValue.ToString(CultureInfo.InvariantCulture));
        p.Add("@Buy", BuildBuyTable(request.BuyRows).AsTableValuedParameter("dbo.SetupPromotionBuyTVP"));
        p.Add("@Get", BuildGetTable(request.GetRows).AsTableValuedParameter("dbo.SetupPromotionGetTVP"));
        p.Add("@Site", siteTable.AsTableValuedParameter("dbo.SetupPromotionSiteTVP"));

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SaveSetupCTKMAll", p,
                commandType: CommandType.StoredProcedure, commandTimeout: 120, cancellationToken: ct));

            var savedNo = p.Get<string>("@BBYNR");
            return (true, $"Lưu CTKM {savedNo} thành công", savedNo);
        }
        catch (SqlException ex) when (KnownBusinessErrorNumbers.Contains(ex.Number))
        {
            // Lỗi nghiệp vụ có chủ đích (THROW 51001 trong usp_SaveSetupCTKMAll) — an toàn
            // hiện nguyên message cho end-user, message đã được viết sẵn để hiển thị.
            return (false, ex.Message, string.Empty);
        }
        catch (Exception ex)
        {
            // Lỗi kỹ thuật (mất kết nối, SP chưa deploy, timeout...) — KHÔNG hiện message
            // SQL thô ra Snackbar, chỉ log đầy đủ để Dev debug qua file log.
            fileLogHelper.WriteExpLogs("PromotionRepository.SaveSetupAsync", ex);
            return (false, "Lỗi hệ thống, vui lòng thử lại hoặc liên hệ IT.", string.Empty);
        }
    }

    public async Task<(bool Ok, string Message)> ApproveSetupAsync(string bbynr, CancellationToken ct = default)
    {
        try
        {
            var p = new DynamicParameters();
            p.Add("@BBYNR", bbynr);
            p.Add("@OutOfferHeaderCounter", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("@OutOfferBuyCounter", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("@OutOfferGetCounter", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("@OutOfferBenefitsCounter", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("@OutOfferSiteCounter", dbType: DbType.Int64, direction: ParameterDirection.Output);

            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SetupPromotion_Approve", p,
                commandType: CommandType.StoredProcedure, commandTimeout: 300, cancellationToken: ct));

            // Đẩy Counter vừa bump (qua SP legacy Setup_Promotion_Insert) vào SyncTableList.POSLastCounter
            // (bất đồng bộ, non-blocking) — xem comment đầu SetupPromotion_ApproveAndStatus.sql (Pilot D).
            syncTracker.Track("OfferHeader", p.Get<long>("@OutOfferHeaderCounter"));
            syncTracker.Track("OfferBuy", p.Get<long>("@OutOfferBuyCounter"));
            syncTracker.Track("OfferGet", p.Get<long>("@OutOfferGetCounter"));
            syncTracker.Track("OfferBenefits", p.Get<long>("@OutOfferBenefitsCounter"));
            syncTracker.Track("OfferSite", p.Get<long>("@OutOfferSiteCounter"));

            return (true, $"Duyệt CTKM {bbynr} thành công");
        }
        catch (SqlException ex) when (KnownBusinessErrorNumbers.Contains(ex.Number))
        {
            // Lỗi nghiệp vụ có chủ đích (THROW 51002 trong usp_SetupPromotion_Approve).
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("PromotionRepository.ApproveSetupAsync", ex);
            return (false, "Lỗi hệ thống, vui lòng thử lại hoặc liên hệ IT.");
        }
    }

    public async Task<bool> UpdateSetupStatusAsync(string bbynr, string status, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            var rows = await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SetupPromotion_UpdateStatus", new { BBYNR = bbynr, Status = status },
                commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));
            return rows > 0;
        }
        catch { return false; }
    }

    public async Task<List<ItemOptionDto>> SearchItemsAsync(string keyword, CancellationToken ct = default)
    {
        const string sql = @"SELECT TOP 50 [No] AS No, ISNULL([Description],'') AS Description,
                                    ISNULL([BaseUnitOfMeasure],'') AS Uom
                             FROM   dbo.Item (NOLOCK)
                             WHERE  (@kw = '' OR [No] LIKE '%' + @kw + '%' OR [Description] LIKE '%' + @kw + '%')
                             ORDER  BY [No]";
        return (await QueryAsync<ItemOptionDto>(sql, new { kw = (keyword ?? string.Empty).Trim() }, ct: ct)).ToList();
    }

    public async Task<List<OptionItemDto>> GetMemberCodeOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OptionItemDto>>(KeyMemberCodeOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [Code] AS Value, ISNULL([Description],[Code]) AS Text
                             FROM   dbo.OptionData (NOLOCK)
                             WHERE  ISNULL([Status], 0) = 1 AND [Caption] = 'MEMBERCODETYPE'
                             ORDER  BY [Order]";
        var data = (await QueryAsync<OptionItemDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeyMemberCodeOptions, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<List<OfferSiteLineDto>> GetSiteGroupOptionsAsync(CancellationToken ct = default)
    {
        var cached = await redis.StringGetAsync<List<OfferSiteLineDto>>(KeySiteGroupOptions);
        if (cached?.Count > 0) return cached;

        const string sql = @"SELECT [GroupCode] AS SiteGroupCode, ISNULL([GroupName],'') AS GroupName
                             FROM   dbo.SetupGroupSite (NOLOCK)
                             WHERE  [Status] = 1
                             ORDER  BY [GroupCode]";
        var data = (await QueryAsync<OfferSiteLineDto>(sql, ct: ct)).ToList();
        if (data.Count > 0)
            redis.StringSet(KeySiteGroupOptions, data, ttlSeconds: 43200);
        return data;
    }

    public async Task<(bool Ok, string Message)> SaveSiteGroupAsync(
        SiteGroupSaveRequest request, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.GroupCode))
            return (false, "Vui lòng nhập mã nhóm");
        if (string.IsNullOrWhiteSpace(request.GroupName))
            return (false, "Vui lòng nhập tên nhóm");

        var raw = (request.StoreListRaw ?? string.Empty).Trim();
        string listStoreJson;
        if (string.Equals(raw, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            listStoreJson = "ALL";
        }
        else
        {
            var stores = Regex.Split(raw, @"[;,\s]+").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            if (stores.Count == 0)
                return (false, "Vui lòng nhập danh sách cửa hàng hoặc gõ 'ALL'");
            listStoreJson = JsonConvert.SerializeObject(stores);
        }

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SetupGroupSite_Save",
                new { GroupCode = request.GroupCode.Trim(), GroupName = request.GroupName.Trim(), ListStore = listStoreJson, UserName = actor },
                commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));

            redis.Delete(KeySiteGroupOptions);
            return (true, $"Lưu nhóm cửa hàng {request.GroupCode} thành công");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(List<SiteGroupListItemDto> Items, int Total)> GetSiteGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  GroupCode, ISNULL(GroupName,'') AS GroupName, ISNULL(ListStore,'') AS ListStoreRaw,
                    ISNULL(Status,0) AS Status, LastUpdateDate,
                    COUNT(*) OVER() AS Total
            FROM    dbo.SetupGroupSite (NOLOCK)
            WHERE   (@GroupCode = '' OR GroupCode LIKE '%' + @GroupCode + '%')
              AND   (@GroupName = '' OR GroupName LIKE '%' + @GroupName + '%')
            ORDER BY GroupCode
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var rows = (await QueryAsync<SiteGroupListRow>(sql, new
        {
            GroupCode = (groupCode ?? string.Empty).Trim(),
            GroupName = (groupName ?? string.Empty).Trim(),
            PageSize = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, ct: ct)).ToList();

        var items = rows.Select(r => new SiteGroupListItemDto
        {
            GroupCode = r.GroupCode,
            GroupName = r.GroupName,
            Status = r.Status,
            LastUpdateDate = r.LastUpdateDate,
            StoreCount = CountStores(r.ListStore)
        }).ToList();

        var total = rows.Count > 0 ? rows[0].Total : 0;
        return (items, total);
    }

    public async Task<List<SiteGroupStoreItemDto>> GetSiteGroupStoresAsync(
        string groupCode, string storeNo, string storeName, CancellationToken ct = default)
    {
        const string groupSql = @"SELECT ISNULL(ListStore,'') FROM dbo.SetupGroupSite (NOLOCK) WHERE GroupCode = @groupCode";
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var listStore = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(groupSql, new { groupCode }, cancellationToken: ct)) ?? string.Empty;

        var no = (storeNo ?? string.Empty).Trim();
        var name = (storeName ?? string.Empty).Trim();

        if (string.Equals(listStore, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            const string sql = @"SELECT No AS StoreNo, ISNULL(Name,'') AS StoreName
                                 FROM   dbo.Store (NOLOCK)
                                 WHERE  ClosingMethod = 0
                                   AND  (@no = '' OR No LIKE '%' + @no + '%')
                                   AND  (@name = '' OR Name LIKE '%' + @name + '%')
                                 ORDER  BY No";
            return (await QueryAsync<SiteGroupStoreItemDto>(sql, new { no, name }, ct: ct)).ToList();
        }

        List<string>? codes = null;
        try { codes = JsonConvert.DeserializeObject<List<string>>(listStore); } catch { /* ignore bad json */ }
        codes ??= [];
        if (codes.Count == 0) return [];

        const string joinSql = @"SELECT No AS StoreNo, ISNULL(Name,'') AS StoreName
                                 FROM   dbo.Store (NOLOCK)
                                 WHERE  No IN @codes
                                   AND  (@no = '' OR No LIKE '%' + @no + '%')
                                   AND  (@name = '' OR Name LIKE '%' + @name + '%')
                                 ORDER  BY No";
        return (await QueryAsync<SiteGroupStoreItemDto>(joinSql, new { codes, no, name }, ct: ct)).ToList();
    }

    public async Task<(bool Ok, string Message)> SaveItemGroupAsync(
        ItemGroupSaveRequest request, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.GroupCode))
            return (false, "Vui lòng nhập mã nhóm");
        if (string.IsNullOrWhiteSpace(request.GroupName))
            return (false, "Vui lòng nhập tên nhóm");

        var itemNos = request.ItemNos.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        if (itemNos.Count == 0)
            return (false, "Vui lòng thêm ít nhất 1 sản phẩm vào nhóm");

        // Validate toàn bộ item tồn tại trong dbo.Item — khớp business rule legacy
        // SetupGroupBuyItem (insert toàn bộ hoặc không insert gì).
        const string checkSql = "SELECT No FROM dbo.Item (NOLOCK) WHERE No IN @itemNos";
        var validNos = (await QueryAsync<string>(checkSql, new { itemNos }, ct: ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invalid = itemNos.Where(no => !validNos.Contains(no)).ToList();
        if (invalid.Count > 0)
            return (false, $"Sản phẩm không tồn tại: {string.Join(", ", invalid)}");

        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SetupGroupItem_Save",
                new { request.GroupCode, request.GroupName, ListItemNo = JsonConvert.SerializeObject(itemNos), UserName = actor },
                commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));

            return (true, $"Lưu nhóm sản phẩm {request.GroupCode} thành công");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(List<ItemGroupListItemDto> Items, int Total)> GetItemGroupListAsync(
        string groupCode, string groupName, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  GroupCode, ISNULL(GroupName,'') AS GroupName, ISNULL(ListItemNo,'') AS ListItemNoRaw,
                    ISNULL(Status,0) AS Status, LastUpdateDate,
                    COUNT(*) OVER() AS Total
            FROM    dbo.SetupGroupItem (NOLOCK)
            WHERE   (@GroupCode = '' OR GroupCode LIKE '%' + @GroupCode + '%')
              AND   (@GroupName = '' OR GroupName LIKE '%' + @GroupName + '%')
            ORDER BY GroupCode
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var rows = (await QueryAsync<ItemGroupListRow>(sql, new
        {
            GroupCode = (groupCode ?? string.Empty).Trim(),
            GroupName = (groupName ?? string.Empty).Trim(),
            PageSize = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, ct: ct)).ToList();

        var items = rows.Select(r => new ItemGroupListItemDto
        {
            GroupCode = r.GroupCode,
            GroupName = r.GroupName,
            Status = r.Status,
            LastUpdateDate = r.LastUpdateDate,
            ItemCount = CountItemNos(r.ListItemNoRaw)
        }).ToList();

        var total = rows.Count > 0 ? rows[0].Total : 0;
        return (items, total);
    }

    public async Task<List<ItemGroupItemDto>> GetItemGroupItemsAsync(
        string groupCode, string itemNo, string itemName, CancellationToken ct = default)
    {
        const string groupSql = @"SELECT ISNULL(ListItemNo,'') FROM dbo.SetupGroupItem (NOLOCK) WHERE GroupCode = @groupCode";
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var listItemNo = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(groupSql, new { groupCode }, cancellationToken: ct)) ?? string.Empty;

        List<string>? itemNos = null;
        try { itemNos = JsonConvert.DeserializeObject<List<string>>(listItemNo); } catch { /* ignore bad json */ }
        itemNos ??= [];
        if (itemNos.Count == 0) return [];

        var no = (itemNo ?? string.Empty).Trim();
        var name = (itemName ?? string.Empty).Trim();
        const string sql = @"SELECT [No], ISNULL([Description],'') AS Description, ISNULL([BaseUnitOfMeasure],'') AS Uom
                             FROM   dbo.Item (NOLOCK)
                             WHERE  [No] IN @itemNos
                               AND  (@no = '' OR [No] LIKE '%' + @no + '%')
                               AND  (@name = '' OR [Description] LIKE '%' + @name + '%')
                             ORDER  BY [No]";
        return (await QueryAsync<ItemGroupItemDto>(sql, new { itemNos, no, name }, ct: ct)).ToList();
    }

    // ── Modal "Xem chi tiết" 1 offer đã publish (dbo.OfferHeader/OfferBuy/OfferGet/OfferBenefits/OfferSite/OfferPriority) ──
    // Ported from src/legacy/VCM.BLUEPOS.Data/Promotion/PromotionData.cs:115-731 (EF LINQ trực tiếp trên bảng,
    // không qua SP như bảng chính GetOfferHeaderListAsync). Viết SQL Dapper trực tiếp — bảng chỉ đọc, đã có
    // tiền lệ GetSetupListAsync/GetSetupDetailAsync ở trên trong cùng file, không cần SP mới.

    public async Task<OfferHeaderDetailDto?> GetOfferHeaderDetailAsync(string offerNo, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  [No] AS OfferNo, [Type], [Description], [Status], [OfferType], [PriceGroup],
                    [RoundingMethod], [CurrencyCode], [LastDateModified], [ValidationPeriodID],
                    [ValidationDescription], [StartingDate], [EndingDate],
                    CAST([BlockPeriodicDiscount] AS bit) AS BlockPeriodicDiscount,
                    [DealPrice], [ShowDealLines], [SalesTypeFilter], [SelectionType], [CustomerDiscGroup],
                    [MemberValue], [DiscountTrackingNo], [CouponCode], [CouponQtyNeeded], [MemberType],
                    [MemberAttribute], [MemberAttributeValue],
                    CAST([BlockSalesCommission] AS bit) AS BlockSalesCommission,
                    CAST([BlockManualPriceChange] AS bit) AS BlockManualPriceChange,
                    CAST([BlockInfoCodeDiscount] AS bit) AS BlockInfoCodeDiscount,
                    CAST([BlockLineDiscountOffer] AS bit) AS BlockLineDiscountOffer,
                    CAST([BlockTotalDiscountOffer] AS bit) AS BlockTotalDiscountOffer,
                    CAST([BlockTenderTypeDiscount] AS bit) AS BlockTenderTypeDiscount,
                    CAST([BlockMemberPoints] AS bit) AS BlockMemberPoints,
                    [ConditionBuy],
                    CASE WHEN [ConditionBuy] = 1 THEN 'OR' ELSE 'AND' END AS ConditionBuyStr,
                    CAST([MemberOnly] AS bit) AS MemberOnly,
                    [ConditionGet],
                    CASE WHEN [ConditionGet] = 1 THEN 'OR' ELSE 'AND' END AS ConditionGetStr,
                    [NoSeries], [FromTime], [ToTime],
                    CAST([Mon] AS bit) AS Mon, CAST([Tue] AS bit) AS Tue, CAST([Wed] AS bit) AS Wed,
                    CAST([Thu] AS bit) AS Thu, CAST([Fri] AS bit) AS Fri, CAST([Sat] AS bit) AS Sat,
                    CAST([Sun] AS bit) AS Sun,
                    [NumOfDays], [DayOfWeek], [TenderTypeCode], [TenderTypeValue], [TenderTypeOfferPercent],
                    [TenderTypeOfferAmount], [BankCode], [LocalSiteGroup], [LimitQty],
                    [VoucherFromDate], [VoucherToDate], [VoucherValidDay], [VoucherLimitNumber],
                    [PromotionNo], [PriorityBBY], [Counter], [Pkey], [MinValue], [TotalDiscountType],
                    [TotalDiscountValue], [IsVoucher], [IsTotalBill], [IsGift], [MemberCode],
                    [DiscountAmountMax], [IsFullPrice], [Remark], [SalesType]
            FROM    dbo.OfferHeader (NOLOCK)
            WHERE   [No] = @offerNo";
        return await QueryFirstOrDefaultAsync<OfferHeaderDetailDto>(sql, new { offerNo }, commandTimeout: 60, ct: ct);
    }

    public async Task<(List<OfferBuyDetailLineDto> Items, int Total)> GetOfferBuyDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  a.[OfferNo], a.[LineNo], a.[LineType], a.[No], a.[Description], a.[UnitOfMeasure],
                    a.[DiscountType],
                    CASE a.[DiscountType] WHEN 0 THEN N'%' WHEN 1 THEN N'Amount' WHEN 2 THEN N'Price' ELSE '' END AS DiscountTypeStr,
                    a.[DiscountValue], a.[Quantity], a.[Step], a.[BonusBuyNo], a.[LineGroup], a.[ScaleType],
                    CASE a.[ScaleType] WHEN 'A' THEN 'From' WHEN 'B' THEN 'UpTo' WHEN 'C' THEN 'Equal' ELSE ISNULL(a.[ScaleType],'') END AS ScaleTypeStr,
                    a.[Counter], a.[Pkey],
                    COUNT(*) OVER() AS Total
            FROM    dbo.OfferBuy (NOLOCK) a
            WHERE   a.[OfferNo] = @offerNo
              AND   (@search = '' OR a.[No] LIKE '%' + @search + '%')
            ORDER BY a.[LineNo]
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await QueryAsync<OfferBuyDetailLineDto>(sql, new
        {
            offerNo,
            search = (search ?? string.Empty).Trim(),
            PageSize = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, commandTimeout: 60, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<(List<OfferBenefitLineDto> Items, int Total)> GetOfferBenefitDetailAsync(
        string offerNo, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  a.[OfferNo], a.[LineNo], a.[Type], a.[No], a.[VariantCode], a.[Description],
                    a.[ValueType],
                    CASE a.[ValueType] WHEN 0 THEN N'%' WHEN 1 THEN N'Amount' WHEN 2 THEN N'Price' ELSE '' END AS ValueTypeStr,
                    a.[Value], a.[StepAmount], a.[LineGroup], a.[Quantity], a.[UnitOfMeasure],
                    a.[Counter], a.[Pkey],
                    COUNT(*) OVER() AS Total
            FROM    dbo.OfferBenefits (NOLOCK) a
            WHERE   a.[OfferNo] = @offerNo
            ORDER BY a.[LineNo]
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await QueryAsync<OfferBenefitLineDto>(sql, new
        {
            offerNo,
            PageSize = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, commandTimeout: 60, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<(List<OfferGetDetailLineDto> Items, int Total)> GetOfferGetDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  a.[OfferNo], a.[LineNo], a.[LineType], a.[No], a.[Description], a.[UnitOfMeasure],
                    a.[DiscountType],
                    CASE a.[DiscountType] WHEN 0 THEN N'%' WHEN 1 THEN N'Amount' WHEN 2 THEN N'Price' ELSE '' END AS DiscountTypeStr,
                    a.[DiscountValue], a.[Quantity], a.[Step], a.[BonusBuyNo], a.[LineGroup], a.[ScaleType],
                    CASE a.[ScaleType] WHEN 'A' THEN 'From' WHEN 'B' THEN 'UpTo' WHEN 'C' THEN 'Equal' ELSE ISNULL(a.[ScaleType],'') END AS ScaleTypeStr,
                    a.[Counter], a.[Pkey],
                    COUNT(*) OVER() AS Total
            FROM    dbo.OfferGet (NOLOCK) a
            WHERE   a.[OfferNo] = @offerNo
              AND   (@search = '' OR a.[No] LIKE '%' + @search + '%')
            ORDER BY a.[LineNo]
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await QueryAsync<OfferGetDetailLineDto>(sql, new
        {
            offerNo,
            search = (search ?? string.Empty).Trim(),
            PageSize = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, commandTimeout: 60, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<(List<OfferSiteLineDetailDto> Items, int Total)> GetOfferSiteDetailAsync(
        string offerNo, string? search, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  a.[OfferNo], a.[PriceGroupCode], a.[StoreNo], ISNULL(c.[StyleProfile],'') AS StyleProfile,
                    a.[Counter], a.[Pkey],
                    COUNT(*) OVER() AS Total
            FROM    dbo.OfferSite (NOLOCK) a
            LEFT JOIN dbo.Store (NOLOCK) c ON c.[No] = a.[StoreNo]
            WHERE   a.[OfferNo] = @offerNo
              AND   (@search = '' OR a.[StoreNo] LIKE '%' + @search + '%')
            ORDER BY a.[StoreNo]
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await QueryAsync<OfferSiteLineDetailDto>(sql, new
        {
            offerNo,
            search = (search ?? string.Empty).Trim(),
            PageSize = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, commandTimeout: 60, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<(List<OfferPriorityLineDto> Items, int Total)> GetOfferPriorityDetailAsync(
        string offerType, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT  a.[OfferType], a.[Priority], a.[IsMember], a.[IsDuplicate], a.[Counter], a.[Pkey],
                    COUNT(*) OVER() AS Total
            FROM    dbo.OfferPriority (NOLOCK) a
            WHERE   a.[OfferType] = @offerType
            ORDER BY a.[Priority]
            OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await QueryAsync<OfferPriorityLineDto>(sql, new
        {
            offerType,
            PageSize = Math.Max(1, pageSize),
            PageNumber = Math.Max(0, pageNumber)
        }, commandTimeout: 60, ct: ct)).ToList();

        var total = items.Count > 0 ? items[0].Total : 0;
        return (items, total);
    }

    public async Task<(bool Ok, string Message)> DeactivateOfferAsync(string offerNo, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                "dbo.usp_OfferHeader_Deactivate", new { OfferNo = offerNo },
                commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));
            return (true, $"Đã deactive khuyến mãi {offerNo}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static int CountItemNos(string listItemNoRaw)
    {
        try
        {
            var list = JsonConvert.DeserializeObject<List<string>>(listItemNoRaw);
            return list?.Count ?? 0;
        }
        catch { return 0; }
    }

    // NUMOFDAYSLIST lưu JSON array (vd "[1,15,20]"). Cột mới, tách biệt hoàn toàn với
    // NUMOFDAYS cũ — không có dữ liệu legacy dạng số đơn cần fallback ở đây.
    private static List<int> ParseDaysOfMonth(string numOfDaysListRaw)
    {
        if (string.IsNullOrWhiteSpace(numOfDaysListRaw)) return [];
        try
        {
            var list = JsonConvert.DeserializeObject<List<int>>(numOfDaysListRaw);
            return list?.Where(d => d is >= 1 and <= 31).Distinct().OrderBy(d => d).ToList() ?? [];
        }
        catch { return []; }
    }

    private sealed class ItemGroupListRow
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string ListItemNoRaw { get; set; } = string.Empty;
        public bool Status { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int Total { get; set; }
    }

    private static int CountStores(string listStore)
    {
        if (string.Equals(listStore, "ALL", StringComparison.OrdinalIgnoreCase)) return -1;
        try
        {
            var list = JsonConvert.DeserializeObject<List<string>>(listStore);
            return list?.Count ?? 0;
        }
        catch { return 0; }
    }

    private sealed class SiteGroupListRow
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string ListStore { get; set; } = string.Empty;
        public bool Status { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int Total { get; set; }
    }

    // ── Helpers (TVP builders + site expansion) ──────────────────────────────

    private async Task<DataTable> BuildSiteTableAsync(List<string> groupCodes, CancellationToken ct)
    {
        var table = new DataTable();
        table.Columns.Add("SiteGroupCode", typeof(string));
        table.Columns.Add("SiteCode", typeof(string));

        var codes = groupCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().ToList();
        if (codes.Count == 0) return table;

        const string sql = @"SELECT [GroupCode] AS GroupCode, ISNULL([ListStore],'') AS ListStore
                             FROM   dbo.SetupGroupSite (NOLOCK)
                             WHERE  [GroupCode] IN @codes";
        var groups = (await QueryAsync<SiteGroupRow>(sql, new { codes }, ct: ct)).ToList();

        foreach (var g in groups)
        {
            if (string.Equals(g.ListStore, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                table.Rows.Add(g.GroupCode, "ALL");
                continue;
            }
            List<string>? stores = null;
            try { stores = JsonConvert.DeserializeObject<List<string>>(g.ListStore); } catch { /* ignore bad json */ }
            if (stores == null || stores.Count == 0)
            {
                table.Rows.Add(g.GroupCode, "ALL");
                continue;
            }
            foreach (var s in stores.Where(s => !string.IsNullOrWhiteSpace(s)))
                table.Rows.Add(g.GroupCode, s);
        }
        return table;
    }

    private static DataTable BuildBuyTable(List<OfferBuyLineDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("LineType", typeof(int));
        t.Columns.Add("ItemNo", typeof(string));
        t.Columns.Add("GroupCode", typeof(string));
        t.Columns.Add("Quantity", typeof(string));
        t.Columns.Add("Uom", typeof(string));
        t.Columns.Add("ScaleType", typeof(string));
        foreach (var r in rows)
            t.Rows.Add(r.LineType, r.No ?? "", r.GroupCode ?? "",
                       r.Quantity.ToString(CultureInfo.InvariantCulture), r.UnitOfMeasure ?? "", r.ScaleType ?? "C");
        return t;
    }

    private static DataTable BuildGetTable(List<OfferGetLineDto> rows)
    {
        var t = new DataTable();
        t.Columns.Add("LineType", typeof(int));
        t.Columns.Add("ItemNo", typeof(string));
        t.Columns.Add("GroupCode", typeof(string));
        t.Columns.Add("Quantity", typeof(string));
        t.Columns.Add("Uom", typeof(string));
        t.Columns.Add("ScaleType", typeof(string));
        t.Columns.Add("DiscountType", typeof(int));
        t.Columns.Add("DiscountValue", typeof(string));
        foreach (var r in rows)
            t.Rows.Add(r.LineType, r.No ?? "", r.GroupCode ?? "",
                       r.Quantity.ToString(CultureInfo.InvariantCulture), r.UnitOfMeasure ?? "", r.ScaleType ?? "C",
                       r.DiscountType, r.DiscountValue.ToString(CultureInfo.InvariantCulture));
        return t;
    }

    private static string YmdToDisplay(string ymd)
        => DateTime.TryParseExact(ymd, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd/MM/yyyy") : string.Empty;

    // Voucher date: bỏ sentinel 19000101 / giá trị "0" / length≠8 → rỗng
    private static string YmdToVoucherDisplay(string ymd)
        => (string.IsNullOrEmpty(ymd) || ymd == "0" || ymd.Length != 8 || ymd == "19000101")
            ? string.Empty : YmdToDisplay(ymd);

    private static string DmyToYmd(string dmy)
        => DateTime.TryParseExact(dmy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("yyyyMMdd") : string.Empty;

    private sealed class SiteGroupRow
    {
        public string GroupCode { get; set; } = string.Empty;
        public string ListStore { get; set; } = string.Empty;
    }

    // SP [dbo].[GetPromotionOfferHeaderList] (bản cập nhật) 10 tham số — đã bỏ @StyleProfile và
    // @SalesType so với legacy, thêm @FromDate (CTKM còn hiệu lực từ ngày này trở đi — chỉ lọc
    // theo mốc đầu, KHÔNG lọc theo mốc cuối vì nhiều CTKM có EndingDate đặt xa (vd tới 2030),
    // xem docs/sql/GetPromotionOfferHeaderList_AddDateRangeFilter.sql).
    // Bản fix 2026-07-10 (docs/sql/GetPromotionOfferHeaderList_FixDuplicateRows.sql): bỏ LEFT
    // JOIN OfferSite (gây fan-out N dòng trùng lặp/CTKM do @StoreNo luôn rỗng — StoreNo=empty
    // ở BuildParams bên dưới không bao giờ lọc, JOIN chỉ để nhân dòng) + đổi LastDateModified
    // sang CONVERT style 120 (giữ giờ:phút:giây thay vì 103 date-only) để OffersPage.razor hiển
    // thị đúng "Ngày cập nhật" kèm giờ thật.
    private const string Sql =
        "[dbo].[GetPromotionOfferHeaderList] @No,@Description,@Status,@OfferType,@ItemNo,@StoreNo,@FromDate,@Exp,@PageSize,@PageNumber";

    private static object BuildParams(OfferListFilter f, int pageNumber, int pageSize) => new
    {
        No          = (f.TextSearch ?? string.Empty).Trim(),
        Description = (f.PromotionName ?? string.Empty).Trim(),
        Status      = string.IsNullOrWhiteSpace(f.Status) ? "-1" : f.Status.Trim(),
        OfferType   = (f.OfferType ?? string.Empty).Trim(),
        ItemNo      = (f.ItemNo ?? string.Empty).Trim(),
        StoreNo     = string.Empty,         // không lọc theo store (parity legacy)
        FromDate    = f.FromDate,
        Exp         = 0,
        PageSize    = pageSize,
        PageNumber  = pageNumber
    };
}
