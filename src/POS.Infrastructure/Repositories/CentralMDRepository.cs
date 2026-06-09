using POS.Common.Dtos;
using POS.Common.Dtos.CentralMD;
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
}
