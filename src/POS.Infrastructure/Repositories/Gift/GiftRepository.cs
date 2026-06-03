using Dapper;
using Microsoft.Extensions.Caching.Memory;
using POS.Application.Gift.DTOs;
using POS.Application.Gift.Repositories;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Repositories;

namespace POS.Infrastructure.Repositories.Gift;

public class GiftRepository : BaseRepository, IGiftRepository
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public GiftRepository(IDbConnectionFactory connectionFactory, IMemoryCache cache)
        : base(connectionFactory)
    {
        _cache = cache;
    }

    // ── POSGiftInfo (CentralGeneral DB) ───────────────────────────────────

    public async Task<List<POSGiftInfoDto>> InsertPOSGiftInfoAsync(List<POSGiftInfoDto> giftInfos)
    {
        if (giftInfos.Count == 0) return new List<POSGiftInfoDto>();

        using var conn = CreateConnectionByName("CentralGeneral");
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var codes = giftInfos.Select(x => x.GiftCode).ToArray();

            var existing = (await conn.QueryAsync<POSGiftInfoDto>(
                "SELECT * FROM POSGiftInfo (NOLOCK) WHERE GiftCode IN @codes",
                new { codes }, tx)).ToList();

            var existingCodes = existing.Select(x => x.GiftCode).ToHashSet();
            var toInsert = giftInfos.Where(x => !existingCodes.Contains(x.GiftCode)).ToList();

            if (toInsert.Count > 0)
            {
                const string sql = @"
INSERT INTO POSGiftInfo (GiftCode, GiftType, GiftValue, GiftStatus, PublishDate, FromDate, ToDate, PosCreated, CreatedTime, PosUsed, UsedTime)
VALUES (@GiftCode, @GiftType, @GiftValue, @GiftStatus, @PublishDate, @FromDate, @ToDate, @PosCreated, @CreatedTime, @PosUsed, @UsedTime)";
                await conn.ExecuteAsync(sql, toInsert, tx);
            }

            tx.Commit();

            var result = new List<POSGiftInfoDto>(toInsert);
            result.AddRange(existing);
            return result;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<POSGiftInfoDto>> GetByGiftCodeAsync(string[] giftCodes)
    {
        using var conn = CreateConnectionByName("CentralGeneral");
        conn.Open();
        var result = await conn.QueryAsync<POSGiftInfoDto>(
            "SELECT * FROM POSGiftInfo (NOLOCK) WHERE GiftCode IN @giftCodes",
            new { giftCodes });
        return result.ToList();
    }

    public async Task<bool> UpdateGiftStatusAsync(string[] giftCodes, string status, string posUsed)
    {
        using var conn = CreateConnectionByName("CentralGeneral");
        conn.Open();
        int rows = await conn.ExecuteAsync(
            "UPDATE POSGiftInfo SET GiftStatus = @status, PosUsed = @posUsed, UsedTime = @now WHERE GiftCode IN @giftCodes",
            new { status, posUsed, now = DateTime.Now, giftCodes });
        return rows > 0;
    }

    // ── MMLScheme (CentralMD DB) ──────────────────────────────────────────

    public async Task<MMLSchemeHeaderDto?> GetMMLSchemeHeaderAsync(string code)
    {
        string cacheKey = $"MMLSchemeHeader:{code}";
        if (_cache.TryGetValue(cacheKey, out MMLSchemeHeaderDto? cached))
            return cached;

        const string sql = @"
SELECT [Code] AS HeaderCode, [FromDate], [ToDate], [IsMember], [IsCallAPI], [MinAmount], [Ref1], [Ref2], [Ref3], [Ref4], [Ref5]
FROM MMLSchemeHeader (NOLOCK)
WHERE CAST([FromDate] AS DATE) <= CAST(GETDATE() AS DATE)
  AND CAST([ToDate] AS DATE) >= CAST(GETDATE() AS DATE)
  AND ISNULL([Enabled], 0) = 1
  AND Code = @code";

        using var conn = CreateConnection();
        conn.Open();
        var data = await conn.QueryFirstOrDefaultAsync<MMLSchemeHeaderDto>(sql, new { code });
        if (data != null)
            _cache.Set(cacheKey, data, CacheDuration);

        return data;
    }

    public async Task<List<MMLSchemeItemDto>> GetMMLSchemeItemsAsync()
    {
        const string cacheKey = "MMLSchemeItems";
        if (_cache.TryGetValue(cacheKey, out List<MMLSchemeItemDto>? cached) && cached!.Count > 0)
            return cached;

        const string sql = @"
SELECT I.*
FROM MMLSchemeHeader (NOLOCK) H
INNER JOIN MMLSchemeItem (NOLOCK) I ON H.Code = I.HeaderCode
WHERE CAST(H.[FromDate] AS DATE) <= CAST(GETDATE() AS DATE)
  AND CAST(H.[ToDate] AS DATE) >= CAST(GETDATE() AS DATE)
  AND ISNULL(H.[Enabled], 0) = 1";

        using var conn = CreateConnection();
        conn.Open();
        var data = (await conn.QueryAsync<MMLSchemeItemDto>(sql)).ToList();
        if (data.Count > 0)
            _cache.Set(cacheKey, data, CacheDuration);

        return data;
    }

    public async Task<MMLSchemeResponseDto?> GetMMLSchemeResponseAsync(string headerCode, string journeyCode)
    {
        string cacheKey = $"MMLSchemeResponse:{headerCode}:{journeyCode}";
        if (_cache.TryGetValue(cacheKey, out MMLSchemeResponseDto? cached))
            return cached;

        const string sql = @"
SELECT DISTINCT I.*
FROM MMLSchemeHeader (NOLOCK) H
INNER JOIN MMLSchemeResponse (NOLOCK) I ON H.Code = I.HeaderCode
WHERE CAST(H.[FromDate] AS DATE) <= CAST(GETDATE() AS DATE)
  AND CAST(H.[ToDate] AS DATE) >= CAST(GETDATE() AS DATE)
  AND ISNULL(H.[Enabled], 0) = 1
  AND I.HeaderCode = @headerCode
  AND I.Code = @journeyCode";

        using var conn = CreateConnection();
        conn.Open();
        var data = await conn.QueryFirstOrDefaultAsync<MMLSchemeResponseDto>(sql, new { headerCode, journeyCode });
        if (data != null)
            _cache.Set(cacheKey, data, CacheDuration);

        return data;
    }
}
