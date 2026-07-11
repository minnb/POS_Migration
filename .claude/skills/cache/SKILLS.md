---
name: cache-redis-pattern
description: Pattern cache Redis StandAlone (IRedisService/IRedisManager) — key convention, TTL, Hash/String, OAuth2 token (nguồn canonical), report cache, distributed lock/throttle, diagnostics. Đọc TRƯỚC khi thêm cache cho bất kỳ data nào.
---

# Skill: Redis Cache Pattern

> **Áp dụng khi:** thêm cache cho bất kỳ master data / config nào đọc từ DB nhiều lần
> (SysWebApi, stores, rates, card level...). Đọc file này TRƯỚC khi thêm chỗ nào dùng cache.

---

## Quy tắc cốt lõi

Cache dùng **Redis StandAlone** (`IRedisService`) — cross-process, survive restart.
**KHÔNG** dùng in-memory cache (IIS `MemoryCache`, `IMemoryCache`) cho dữ liệu chia sẻ giữa
nhiều instance/process.

> Mọi master data từ DB cần đọc nhiều lần phải có Redis cache tương ứng qua `IRedisService`.

---

## IRedisService — API tham khảo

```csharp
// Hash (cho collection có key phụ — truy cập theo field)
Task<T?> HashGetAsync<T>(string key, string field);
T?       HashGet<T>(string key, string field);
void     HashSet<T>(string key, string field, T value, int? ttlSeconds = null);
void     HashDelete(string key, string field);

// String (cho list hoặc single object)
Task<T?> StringGetAsync<T>(string key);
string?  StringGetRaw(string key);           // dùng cho token (string thuần)
void     StringSet<T>(string key, T value, int? ttlSeconds = null);
void     StringSetRaw(string key, string value, TimeSpan? ttl = null); // dùng cho token
```

---

## Redis key naming convention

| Loại dữ liệu | Redis key | Redis type | Field |
|---|---|---|---|
| SysWebApi config | `MD:SysWebApi` | Hash | `{appCode}` (e.g. `"FMV"`) |
| SysWebApi users | `MD:SysWebApiUser` | String | — (serialize cả list) |
| LoyaltyRate | `MD:LoyaltyRate` | Hash | `{code}` |
| CardLevel | `MD:CardLevel` | String | — |
| Stores | `MD:Store` | String | — |
| StoreSetup | `MD:StoreSetup` | String | — |
| StoreSetConfig | `MD:StoreSetConfig` | String | — |
| StoreMappingVinID | `MD:StoreMappingVinID` | String | — |
| WinCode | `MD:WinCode` | String | — |
| StagingDBConfig | `MD:StagingDBConfig` | String | — |
| MemberBusiness | `MD:MemberBusiness` | String | — |
| WinPayAccumulateSetup | `MD:WinPayAccumulateSetup` | String | — |
| WinMoneyConversion | `MD:WinMoneyConversion` | String | — |
| MemoryCacheConfig | `MD:MemoryCacheConfig` | String | — |
| NotifyConfig | `MD:NotifyConfig` | String | — |
| POSDataSetup | `MD:POSDataSetup` | Hash | `{code}` |
| OfferStaffSetup | `MD:OfferStaffSetup` | String | — |
| MMLSchemeHeader | `MD:MMLSchemeHeader` | Hash | `{code}` |
| MMLSchemeItem | `MD:MMLSchemeItem` | String | — |
| MMLSchemeResponse | `MD:MMLSchemeResponse` | Hash | `{headerCode}-{code}` |
| ItemPointsMember | `MD:ItemPointsMember` | Hash | `{pointsCode}-{itemNo}-{uom}` |
| OAuth2 access token | `{Partner}:{Service}:AccessToken` | StringRaw | — |

> **Prefix `MD:`** = Master Data từ CentralMD DB.
> **OAuth tokens** dùng key riêng không có prefix `MD:`.

---

## TTL strategy

| Loại | TTL | Lý do |
|---|---|---|
| Config tĩnh (SysWebApi, Store, CardLevel…) | `43200s` (12h) | Thay đổi ít, refresh 2 lần/ngày |
| Rate/price (LoyaltyRate) | `3600s` (1h) | Có thể cập nhật trong ngày |
| Short-lived data (ItemPointsMember) | `360s` (6 phút) | Dữ liệu promotion thay đổi thường xuyên |
| OAuth2 access token | `expires_in - 60s` | Từ response, buffer 60s để tránh race |
| No TTL | Không dùng | Không dùng TTL vô hạn trong production |

---

## Code pattern bắt buộc

### Pattern 1 — Hash (data có key phụ)

```csharp
// Dùng khi: data lookup theo code/id/appCode
public async Task<SomeDto?> GetSomethingAsync(string code, CancellationToken ct = default)
{
    // 1. Check Redis
    var cached = redis.HashGet<SomeDto>(KeySomething, code);
    if (cached != null) return cached;

    // 2. Query DB
    const string sql = "SELECT * FROM SomeTable (NOLOCK) WHERE Blocked = 0 AND Code = @code;";
    var data = await QueryFirstOrDefaultAsync<SomeDto>(sql, new { code }, ct: ct);

    // 3. Cache nếu có data
    if (data != null)
        redis.HashSet(KeySomething, code, data, ttlSeconds: 43200);

    return data;
}
```

### Pattern 2 — String (list/collection)

```csharp
// Dùng khi: toàn bộ list, không lookup theo field
public async Task<List<SomeDto>?> GetAllSomethingAsync(CancellationToken ct = default)
{
    // 1. Check Redis
    var cached = await redis.StringGetAsync<List<SomeDto>>(KeyAllSomething);
    if (cached?.Count > 0) return cached;

    // 2. Query DB
    const string sql = "SELECT * FROM SomeTable (NOLOCK) WHERE Blocked = 0;";
    var data = (await QueryAsync<SomeDto>(sql, ct: ct)).ToList();

    // 3. Cache nếu có data
    if (data.Count > 0)
        redis.StringSet(KeyAllSomething, data, ttlSeconds: 43200);

    return data;
}
```

### Pattern 3 — OAuth2 token caching

```csharp
// Dùng khi: external API cần bearer token, refresh khi hết hạn
private const string TokenCacheKey = "{Partner}:{Service}:AccessToken";

private async Task<string?> GetAccessTokenAsync()
{
    // 1. Check Redis (raw string)
    var cached = redis.StringGetRaw(TokenCacheKey);
    if (!string.IsNullOrEmpty(cached)) return cached;

    // 2. Lấy token từ OAuth endpoint
    // ... (POST form với client_id, client_secret)

    // 3. Cache với TTL = expires_in - 60s
    var expiresIn = int.TryParse(tokenData.Expires_in, out var e) ? Math.Max(e - 60, 60) : 240;
    redis.StringSetRaw(TokenCacheKey, tokenData.Access_token, TimeSpan.FromSeconds(expiresIn));

    return tokenData.Access_token;
}
```

---

## Nơi đặt logic cache

```
POS.Infrastructure/
└── Repositories/
    ├── Interfaces/ICentralMDRepository.cs   ← thêm method interface
    ├── CentralMDRepository.cs               ← implement pattern Redis+DB
    └── (hoặc LoyaltyRepository nếu từ Loyalty DB)

POS.Infrastructure/
└── AppServices/
    └── {Name}AppService.cs                  ← chỉ token caching ở đây, KHÔNG cache config
```

> **KHÔNG** đặt Redis cache config (SysWebApi, stores, rates...) trực tiếp trong AppService hay Service.
> Config cache PHẢI qua Repository → Repository handles Redis + DB fallback.
> AppService chỉ được cache OAuth token (vì token là per-partner, không phải master data).

---

## Mapping MemoryCacheConst → Redis key

Khi thấy `_memoryCacheService.GetCache<T>("MemoryXxx")` trong code cũ:

| `MemoryCacheConst` cũ | Redis key mới | Ghi chú |
|---|---|---|
| `MemoryCacheSysWebApi` | `MD:SysWebApi` (Hash, field=appCode) | Thêm `GetSysWebApiAsync(appCode)` vào ICentralMDRepository |
| `MemoryCacheSysWebUserApi` | `MD:SysWebApiUser` | String, full list |
| `MemoryCardLevel` | `MD:CardLevel` | String, full list |
| `MemoryCacheStores` | `MD:Store` | String, full list |
| `MemoryCacheStoreSetup` | `MD:StoreSetup` | String, full list |
| `MemoryStoreSetConfig` | `MD:StoreSetConfig` | String, full list |
| `MemoryCacheStoreMappingVinID` | `MD:StoreMappingVinID` | String, full list — inject LoyaltyRepository |
| `MemoryCacheWinCode` | `MD:WinCode` | String, full list |
| `MemoryCacheStagingDBConfig` | `MD:StagingDBConfig` | String, full list |
| `MemoryMemberBusiness` | `MD:MemberBusiness` | String, full list |
| `WinPayAccumulateSetup` | `MD:WinPayAccumulateSetup` | String, full list |
| `WinMoneyConversion` | `MD:WinMoneyConversion` | String, full list |
| `MemoryCacheConfigLoyalty` | `MD:MemoryCacheConfig` | String, full list |
| `MemoryGetNotifyConfig` | `MD:NotifyConfig` | String, full list |
| `MemoryGetPOSDataSetup` | `MD:POSDataSetup` (Hash, field=code) | Lookup theo code |
| `MemoryOfferStaffSetup` | `MD:OfferStaffSetup` | String, full list |
| `Redis_Key_LoyaltyRate` | `MD:LoyaltyRate` (Hash, field=code) | Đã có trong CentralMDRepository |

---

## Checklist khi thêm cache cho một loại data mới

- [ ] Đặt tên Redis key theo convention (xem bảng trên)
- [ ] Nếu method chưa có trong Repository interface → thêm vào `ICentralMDRepository` hoặc `ILoyaltyRepository`
- [ ] Implement trong Repository theo Pattern 1 hoặc 2 (KHÔNG bỏ TTL)
- [ ] AppService/Service inject Repository (KHÔNG inject IRedisService trực tiếp cho config data)
- [ ] Đối với OAuth token → dùng Pattern 3 trong AppService, inject IRedisService
- [ ] Build pass + kiểm tra Redis key được set sau lần gọi đầu

---

## Pattern 4: Cache report query (SP) — TTL theo độ mới + bỏ result-set dư

> Áp dụng khi: report repository gọi SP nặng (aggregate bảng lớn ~10M dòng) theo
> tham số `(store, from, to, groupBy)`, bị gọi nhiều lần / nhiều user. KHÔNG cache vô thời hạn.

```csharp
var range     = $"{from:yyyyMMdd}:{to:yyyyMMdd}";
var seriesKey = $"MD:RptSaleByTime:{groupBy}:{store}:{range}";
var kpiKey    = $"MD:RptSaleByTime:KPI:{store}:{range}";    // KPI tách riêng → tái dùng cross-groupBy

// đọc cache (Redis lỗi → bỏ qua, rơi xuống DB; KHÔNG để cache hỏng làm trả rỗng)
var cached = await redis.StringGetAsync<List<TSeries>>(seriesKey);
if (cached != null && (!includeKpi || (kpi = await redis.StringGetAsync<TKpi>(kpiKey)) != null))
    return (kpi ?? new(), cached);

// miss → exec SP, rồi:
var ttl = to.Date >= DateTime.Today ? 180 : 43200;   // có hôm nay → ngắn; quá khứ bất biến → 12h
redis.StringSet(seriesKey, series, ttl);
redis.StringSet(kpiKey, kpi, ttl);                   // luôn cache KPI từ RS1 (miễn phí)
```

**Nguyên tắc:**
- **TTL theo độ mới**: range chứa hôm nay → TTL ngắn (khớp nhịp worker rebuild); range quá khứ → TTL dài.
- **Tách KPI khỏi series**: cùng (store,range) nhưng khác groupBy vẫn dùng chung 1 KPI → tránh tính lặp.
- **`includeKpi` flag**: caller chỉ cần series (vd chart HOUR/WEEKDAY) → bỏ qua đọc KPI.
- **Fail-fast timeout** (vd 45s) riêng cho report, KHÔNG dùng chung 120s — quá ngưỡng = thiếu index, fail nhanh.
- Cache get/set bọc try riêng → Redis down vẫn fallback DB.

> Ví dụ thực tế: `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs` → `GetSaleByTimeAsync`

---

## Pattern 5: Existence-check cache (positive-only) — validate master data trước khi ghi

> Áp dụng khi: cần validate 1 khóa (FK-logic) **tồn tại** trong bảng master trước một thao tác
> ghi (vd `ActicleNo` phải có trong `CpnVchBOMHeader` trước khi tạo voucher). Cache kết quả để
> không query DB mỗi request, nhưng **CHỈ cache kết quả dương** (tồn tại).

```csharp
private const string KeyCpnVchBOMHeader = "MD:CpnVchBOMHeader"; // Hash, field = itemNo, value = true

public async Task<bool> CpnVchBOMHeaderExistsAsync(string itemNo, CancellationToken ct = default)
{
    if (string.IsNullOrWhiteSpace(itemNo)) return false;

    var cached = redis.HashGet<bool?>(KeyCpnVchBOMHeader, itemNo);   // miss → null
    if (cached == true) return true;

    const string sql = "SELECT TOP 1 1 FROM dbo.CpnVchBOMHeader (NOLOCK) WHERE [ItemNo] = @itemNo;";
    var exists = await QueryFirstOrDefaultAsync<int?>(sql, new { itemNo }, ct: ct) != null;
    if (exists) redis.HashSet(KeyCpnVchBOMHeader, itemNo, true, ttlSeconds: 43200); // CHỈ cache dương
    return exists;
}
```

**Nguyên tắc:**
- **Chỉ cache dương** (không cache negative): đã tồn tại thì luôn tồn tại → không lo stale
  false-negative; khóa mới thêm vào master được nhận ngay ở lần query kế; khóa sai (hiếm) luôn
  re-check DB. KHÔNG dùng Pattern 1 (cache cả object) cho check tồn tại — thừa payload + cache âm
  gây từ chối nhầm khi master vừa thêm.
- Query DB dùng `SELECT TOP 1 1 ... WHERE key=@key` (point-lookup) + map `int?`, so `!= null`.
- Redis lỗi → `HashGet` nuốt lỗi trả `default` (null) → tự fallback DB (không bao giờ chặn nhầm).

> Ví dụ thực tế: `CentralMDRepository.CpnVchBOMHeaderExistsAsync` (gọi từ
> `SAPService.CreateNewVoucherAsync` — validate toàn bộ `Article_No` TRƯỚC vòng lặp tạo để tránh
> tạo dở dang khi batch có phần tử sai).

---

## Pattern 6: Distributed lock (Redis) — serialize thao tác xuyên nhiều instance

> Áp dụng khi: cần chặn 2 request chạy đồng thời cùng 1 đoạn code có race condition
> (check-tồn-tại rồi insert, sinh số/mã cần unique...), và hệ thống có thể scale-out nhiều
> instance sau load balancer → khóa in-process (`SemaphoreSlim`, xem `ISyncFileLock`) không đủ vì
> mỗi instance có bộ nhớ riêng. Dùng `IRedisManager` (KHÔNG dùng `IRedisService` — chưa có
> primitive lock atomic), vì Redis là external shared store, xuyên mọi instance.

```csharp
// IRedisManager — 2 method dựng sẵn (POS.Infrastructure/Cache/)
Task<string?> AcquireLockAsync(string key, TimeSpan ttl);   // SET key token NX PX ttl (atomic)
Task<bool> ReleaseLockAsync(string key, string token);      // Lua script: so token khớp mới DEL

// Wrapper domain-specific — key CỐ ĐỊNH (không theo id) nếu muốn serialize TOÀN BỘ thao tác
public sealed class VoucherIssueLock(IRedisManager redis) : IVoucherIssueLock
{
    private const string Key = "Lock:VoucherIssue";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);      // đủ 1 lần sinh+insert
    private static readonly TimeSpan PollDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(15); // timeout chờ

    public async Task<IAsyncDisposable?> AcquireAsync(CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + MaxWait;
        while (DateTime.UtcNow < deadline)
        {
            var token = await redis.AcquireLockAsync(Key, Ttl);
            if (token != null) return new Releaser(redis, token);
            await Task.Delay(PollDelay, ct);
        }
        return null;   // timeout — request khác đang giữ khóa quá lâu
    }

    private sealed class Releaser(IRedisManager redis, string token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await redis.ReleaseLockAsync(Key, token);
    }
}

// Dùng trong Service — bọc TOÀN BỘ đoạn có race (kể cả bước insert DB, không chỉ bước check)
await using var @lock = await issueLock.AcquireAsync(ct);
if (@lock == null) return Fail("Hệ thống đang xử lý thao tác khác, vui lòng thử lại sau.");
```

**Nguyên tắc:**
- `SET NX PX` là atomic (built-in `StackExchange.Redis`) — không tự ghép `EXISTS` rồi `SET` riêng
  (race giữa 2 lệnh).
- Release PHẢI so khớp token qua Lua script trước khi `DEL` — nếu instance A hết TTL (xử lý chậm
  bất thường) rồi bị instance B acquire, A không được tự ý xoá lock của B.
- TTL là an toàn dự phòng nếu process giữ lock crash/restart giữa chừng — không bị deadlock vĩnh
  viễn.
- Lock phải bọc **đến hết bước ghi DB**, không chỉ bước check-tồn-tại — nếu chỉ lock phần check
  rồi nhả trước khi insert, race vẫn còn nguyên.
- Đây là lock đơn giản cho Redis StandAlone 1 node — KHÔNG cần thuật toán Redlock multi-node.

> Ví dụ thực tế: `IVoucherIssueLock`/`VoucherIssueLock` (`POS.Infrastructure/Locking/`), dùng trong
> `VoucherService.SaveIssueAsync`/`IssueMoreAsync` để chặn sinh mã Auto voucher trùng khi 2 user/2
> instance phát hành đồng thời. Từ 2026-07-07, `CouponService.IssueMoreAsync` cũng inject và dùng
> CHUNG lock này (key `"Lock:VoucherIssue"` không đổi — doc comment `IVoucherIssueLock` từ đầu đã
> ghi rõ "sinh mã Auto voucher/coupon") — không tạo lock riêng cho Coupon.

---

## Pattern 7: Server diagnostics (PING/INFO/DBSIZE) — cho admin dashboard, KHÔNG phải cache data

> Áp dụng khi: cần biết **tình trạng vận hành** của chính Redis instance (online/offline, bộ nhớ,
> số client, uptime, tỷ lệ cache hit) — khác hẳn Pattern 1-6 (cache *data* từ DB). Trước
> 2026-07-08, codebase chưa từng gọi `PING`/`INFO` thật — `HealthCheckService.CheckRedisAsync` chỉ
> đo latency bằng round-trip `StringSet`/`StringGetAsync`, không phản ánh bộ nhớ/uptime/hit-rate.

```csharp
// IRedisManager (POS.Infrastructure/Cache/) — do IConnectionMultiplexer/IDatabase KHÔNG public,
// các method diagnostics này lấy IServer nội bộ qua endpoint non-replica đầu tiên
Task<(bool IsOnline, long PingMs, string? Endpoint, string? Error)> PingAsync();
Task<IDictionary<string, string>> GetServerInfoAsync();   // INFO, flatten mọi section thành field→value
Task<long> GetDbSizeAsync();                              // DBSIZE tại DefaultDatabase
int DefaultDatabase { get; }                               // passthrough RedisOptions.DefaultDatabase

// Field cần đọc từ GetServerInfoAsync() cho dashboard vận hành:
// used_memory_human, connected_clients, uptime_in_seconds, keyspace_hits, keyspace_misses, role
```

**Nguyên tắc:**
- Không expose `IConnectionMultiplexer`/`IDatabase`/`IServer` ra ngoài `IRedisManager` — chỉ trả
  dữ liệu đã parse (tuple/dictionary/primitive), giữ nguyên tắc "IRedisManager là lớp bọc duy nhất
  chạm StackExchange.Redis trực tiếp" của toàn bộ file này.
- `HitRatePercent` tính ở tầng gọi (`hits/(hits+misses)*100`), **không** tính trong `IRedisManager`
  — manager chỉ trả field thô, business logic (tính %, format Dto) thuộc Application layer.
- Offline (`PingAsync` throw/false) → toàn bộ field số trả mặc định (0/null), **không** ném
  exception lên UI — dashboard hiển thị trạng thái OFFLINE + message, không crash trang.

> Ví dụ thực tế: `IRedisManagementService.GetServerStatusAsync` (`POS.Application/Features/Redis/`)
> dùng cho `RedisDashboardPage.razor` (`/ops/redis`) — status card + KPI row (Bộ nhớ/Clients/Tổng
> Key/Cache Hit %/Uptime), style tái dùng từ `HealthPage.razor` (`CardStyle`/`LatencyDisplay`).

---

## Pattern 8: Distributed throttle (sliding-window ZSET) — giới hạn N tác vụ đồng thời xuyên nhiều instance

> Áp dụng khi: cần giới hạn **tổng số** tác vụ tốn tài nguyên (CPU/IO) chạy đồng thời trên toàn cụm
> (nhiều instance sau load balancer), khác Pattern 6 (distributed lock — chỉ 1 tác vụ tại 1 thời
> điểm, dùng cho race condition). Throttle cho phép N tác vụ song song, không phải 1.
> Không dùng nhiều String key riêng + `SCAN`/`DBSIZE` để đếm — tốn O(N) dưới tải cao và có race
> condition (TOCTOU) giữa bước đếm và bước ghi key nếu nhiều request đến cùng lúc.

```csharp
// IRedisManager — 2 method dựng sẵn (POS.Infrastructure/Cache/)
Task<bool> TryAcquireSlotAsync(string setKey, string slotId, int maxSlots, TimeSpan staleAfter);
Task ReleaseSlotAsync(string setKey, string slotId);   // KHÔNG có CancellationToken — luôn nhả được

// Acquire trước khối việc, release trong finally
var slotId = Guid.NewGuid().ToString("N");
if (!await redis.TryAcquireSlotAsync("MD:SomeTask:Slots", slotId, maxSlots: 3, TimeSpan.FromMinutes(10)))
    throw new SomeThrottleException("Hệ thống đang bận, vui lòng thử lại sau.");
try
{
    // ... việc tốn tài nguyên ...
}
finally
{
    await redis.ReleaseSlotAsync("MD:SomeTask:Slots", slotId);   // không truyền ct
}
```

**Cơ chế bên trong `TryAcquireSlotAsync`** (1 Sorted Set `setKey`, member = `slotId`, score =
timestamp ms lúc acquire) — atomic qua 1 Lua script (`ScriptEvaluateAsync`), không có khoảng hở
giữa đếm và ghi:
1. `ZREMRANGEBYSCORE` xoá member có score `< now - staleAfter` — dọn slot "mồ côi" (process giữ
   slot bị crash/không release được) tự chữa lành mà **không cần TTL thật ở cấp key** (Set/ZSET
   không hỗ trợ TTL theo từng member, khác String key).
2. `ZCARD` đếm số member còn lại (O(log N), rẻ hơn `SCAN` prefix nhiều String key).
3. Nếu `count < maxSlots` → `ZADD setKey now slotId`, trả `1`; ngược lại trả `0`.

**Nguyên tắc:**
- `ReleaseSlotAsync` **không nhận `CancellationToken`** — gọi trong `finally` KHÔNG dùng `ct` của
  request (dùng `ct` gốc có thể đã bị hủy → tự throw trước khi kịp xoá key). Giống pattern
  `ct=CancellationToken.None` đã dùng cho ghi log best-effort (`MasterDataSyncService.LogDownloadAsync`).
- Đặt acquire **trước** mọi khóa in-process khác (`ISyncFileLock`/`SemaphoreSlim`) nếu có, vì
  throttle là giới hạn tài nguyên chung toàn cụm, không phải khóa chống trùng theo 1 key nghiệp vụ.
- `staleAfter` nên đặt dư dả (>> thời gian thực thi kỳ vọng) — chỉ là lưới an toàn cho trường hợp
  crash, không phải cơ chế timeout chính.
- Đặt throttle tại **đúng 1 điểm nghẽn cổ chai** (nơi thực sự tốn tài nguyên), không đặt riêng lẻ ở
  từng controller/caller gọi vào — nếu có nhiều luồng gọi cùng 1 hàm tốn tài nguyên, throttle phải
  nằm trong hàm đó để bảo vệ được tất cả các luồng.

> Ví dụ thực tế: `MasterDataSyncService.EnsureMasterDataFileAsync` (`POS.Application/Features/DataSync/`)
> — giới hạn `MasterDataSyncOptions.MaxConcurrentGeneration` (mặc định 3) lượt sinh file .zip master
> data chạy đồng thời trên toàn cụm, key `RedisConst.Redis_Key_CreateMasterDataSlots`
> (`"MD:CreateMasterData:Slots"`). Throttle đầy → ném `MasterDataThrottleException`;
> `SyncDataPosController.GetFileFromFTP` bắt riêng exception này để trả đúng contract (HTTP 200 +
> body `Status=429`, KHÔNG đổi HTTP status code thật — endpoint POS terminal có hợp đồng riêng).
