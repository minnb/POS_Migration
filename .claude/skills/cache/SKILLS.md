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
