# Rule: Caching Standards — Redis StandAlone (key convention, TTL, layering)

## 🎯 Context (Khi nào áp dụng)
Khi thêm/sửa bất kỳ chỗ nào cache dữ liệu (master data, config, token, report...) trong
`POS.Infrastructure`/`POS.Application`. Đây là **tiêu chuẩn bắt buộc** (WHAT/WHY). Code mẫu +
các bước thực thi (Pattern 1–8) nằm ở **`.claude/skills/cache/SKILLS.md`**.

## ✅ DO (Bắt buộc làm)
- Cache **BẮT BUỘC** dùng **Redis StandAlone** qua `IRedisService` (cross-process, survive
  restart). Mọi master data từ DB cần đọc nhiều lần phải có Redis cache tương ứng.
- **Redis key naming convention** — nguồn sự thật:

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

  > **Prefix `MD:`** = Master Data từ CentralMD DB. **OAuth token** dùng key riêng KHÔNG có
  > prefix `MD:`.

- **TTL strategy** — bắt buộc theo bảng, KHÔNG dùng TTL vô hạn trong production:

  | Loại | TTL | Lý do |
  |---|---|---|
  | Config tĩnh (SysWebApi, Store, CardLevel…) | `43200s` (12h) | Thay đổi ít, refresh 2 lần/ngày |
  | Rate/price (LoyaltyRate) | `3600s` (1h) | Có thể cập nhật trong ngày |
  | Short-lived (ItemPointsMember) | `360s` (6 phút) | Promotion thay đổi thường xuyên |
  | Report cache (range có hôm nay) | `180s` | Khớp nhịp worker rebuild |
  | Report cache (range quá khứ) | `43200s` (12h) | Dữ liệu bất biến |
  | OAuth2 access token | `expires_in - 60s` | Từ response, buffer 60s tránh race |

- **Phân tầng cache** (BẮT BUỘC):
  - Config/master data (SysWebApi, stores, rates...) cache **trong Repository**
    (`CentralMDRepository`/`LoyaltyRepository`) — Repository lo Redis + DB fallback.
  - **CHỈ** OAuth token được cache trực tiếp trong AppService (inject `IRedisService`), vì token
    là per-partner runtime state, không phải master data.
- Khi port code cũ dùng `MemoryCacheConst` (`_memoryCacheService.GetCache<T>("MemoryXxx")`) →
  map sang Redis key mới theo bảng:

  | `MemoryCacheConst` cũ | Redis key mới | Ghi chú |
  |---|---|---|
  | `MemoryCacheSysWebApi` | `MD:SysWebApi` (Hash, field=appCode) | `GetSysWebApiAsync(appCode)` |
  | `MemoryCacheSysWebUserApi` | `MD:SysWebApiUser` | String, full list |
  | `MemoryCardLevel` | `MD:CardLevel` | String, full list |
  | `MemoryCacheStores` | `MD:Store` | String, full list |
  | `MemoryCacheStoreSetup` | `MD:StoreSetup` | String, full list |
  | `MemoryStoreSetConfig` | `MD:StoreSetConfig` | String, full list |
  | `MemoryCacheStoreMappingVinID` | `MD:StoreMappingVinID` | String — inject LoyaltyRepository |
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

- Cache report query (SP nặng): TTL theo độ mới (range chứa hôm nay → ngắn; quá khứ → 12h); tách
  KPI khỏi series để tái dùng cross-groupBy; cache get/set bọc try riêng để Redis down vẫn fallback DB.
- Existence-check cache: **CHỈ cache kết quả dương** (tồn tại) — không cache negative.
- Distributed lock/throttle dùng `IRedisManager` (`SET NX PX` atomic / ZSET + Lua) — KHÔNG tự
  ghép `EXISTS`+`SET` hay `SCAN`/`DBSIZE` đếm (race TOCTOU). Release phải so token qua Lua trước `DEL`.

## ❌ DON'T (Tuyệt đối cấm)
- Cấm dùng in-memory cache (`MemoryCache`/`IMemoryCache`) cho dữ liệu chia sẻ giữa nhiều
  instance/process.
- Cấm đặt cache config (SysWebApi, stores, rates...) trực tiếp trong AppService hoặc Service —
  phải qua Repository.
- Cấm AppService/Service inject `IRedisService` để cache config (chỉ được inject cho OAuth token).
- Cấm dùng TTL vô hạn (no-TTL) trong production.
- Cấm tự đặt key khác convention `MD:` cho cùng mục đích, hoặc tạo cache key thứ hai cho data đã có key.

---

> Code mẫu bắt buộc (API `IRedisService`, Pattern 1–8: Hash/String/OAuth/report/existence/lock/
> diagnostics/throttle), checklist thực thi: **`.claude/skills/cache/SKILLS.md`** — đọc file đó để
> lấy code, KHÔNG lặp lại bảng convention/TTL ở đây.
