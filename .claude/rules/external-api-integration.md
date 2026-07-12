# Rule: External API Integration (SysWebApi / SysWebApiRoute)

## 🎯 Context (Khi nào áp dụng)
Khi tạo mới hoặc sửa bất kỳ tích hợp gọi **API của đối tác/dịch vụ bên ngoài** (Loyalty/AkaChain,
GotIT, Urbox, và các partner tương lai trong `PartnerEnum` chưa có AppService — CAP/ONEU/WINX/
Giftee/Capillary...) từ `POS.Api` hoặc `POS.Worker`. Pattern này đã chạy tốt trong thực tế qua
`LoyaltyController` (AkaChain/FMV) và `PaymentController` (GotIT/Urbox) — mọi tích hợp mới **bắt
buộc** đi theo đúng cơ chế đã kiểm chứng dưới đây, không tự nghĩ cách lấy config riêng.

## ✅ DO (Bắt buộc làm)
- **Nguồn config DUY NHẤT là DB**: bảng `SysWebApi` (1 dòng/`AppCode` — Host, UserName, Password,
  Authorization, PublicKey, PrivateKey, Version, HttpProxy, Bypasslist) + `SysWebApiRoute` (N
  dòng/`AppCode`, `Name` → `Route`). KHÔNG lưu Base URL/Endpoint/API Key trong `appsettings.json`
  hay hardcode trong code C#.
- **Lấy config LUÔN qua `ICentralMDRepository.GetSysWebApiAsync(appCode, ct)`**
  (`src/POS.Infrastructure/Repositories/MasterData/CentralMDRepository.cs:122-138`) — method này
  tự cache-aside: Redis `HashGet` key `MD:SysWebApi` field=`appCode` → miss → `SELECT` cả
  `SysWebApi` và `SysWebApiRoute` (`WHERE Blocked = 0`) → `HashSet` TTL 43200s (12 giờ). KHÔNG tự
  viết SQL SELECT trực tiếp 2 bảng này ở bất kỳ Repository/AppService khác.
- **Theo đúng AppService 3-layer** đã chốt ở `.claude/rules/architecture-layers.md`:
  `Controller → I{Name}Service (Application, thin wrapper) → I{Name}AppService (Infrastructure)`.
  Tầng Infrastructure AppService là nơi **DUY NHẤT** gọi `GetSysWebApiAsync` và tạo `HttpClient` —
  xem ví dụ đầy đủ `src/POS.Infrastructure/AppServices/Partner/AkaChainLoyaltyAppService.cs`
  (helpers `GetFMVConfigAsync`/`GetRoute`/`GetTimeout` L48-56, build request `CallApiAsync`
  L178-232).
- Dùng 3 helper chuẩn trong AppService: `GetConfigAsync(ct) => centralMDRepository
  .GetSysWebApiAsync(appCode, ct)`; `GetRoute(config, routeName)` tìm trong
  `config.SysWebApiRoute` theo `Name` (case-insensitive); `GetTimeout(config)` parse
  `config.Version` (field bị **overload** — có thể là version string `"V2"`/`"V6"` HOẶC timeout
  tính bằng giây tùy partner, `int.TryParse` fallback 30s nếu không parse được). Guard
  `if (config == null) return Fail(...)` ngay đầu mỗi method nghiệp vụ.
- **`appCode`**: ưu tiên `PartnerEnum.X.ToString()` (`src/POS.Common/Enums/PartnerEnum.cs`) khi
  partner đã có trong enum — vd `GotITService.cs`/`UrboxService.cs` dùng
  `PartnerEnum.GOTIT.ToString()`/`PartnerEnum.URBOX.ToString()`. Partner hoàn toàn mới chưa có
  trong `PartnerEnum` (vd Giftee, Capillary) → dùng string literal khớp đúng cột `AppCode` trong
  DB. *(Lưu ý: `PartnerEnum.FMV` đã tồn tại (=13) nhưng
  `AkaChainLoyaltyAppService.cs:49` vẫn dùng literal `"FMV"` — inconsistency có sẵn trong code, không
  phải mẫu chuẩn để copy khi viết AppService mới.)*
- Tạo `HttpClient` qua `IHttpClientFactory.CreateClient("{PartnerName}")` (named client đăng ký
  trong `src/POS.Infrastructure/DependencyInjection.cs`), build URL = `config.Host + route`.
- **OAuth2 token** (nếu partner cần Bearer token) cache riêng bằng `IRedisService` **String** key
  theo partner (vd `"AkaChain:FMV:AccessToken"`) — KHÔNG lồng vào `SysWebApiDto`/hash
  `MD:SysWebApi` vì token là runtime state per-partner, không phải master data dùng chung. Chi
  tiết pattern đầy đủ: `.claude/skills/cache/SKILLS.md` mục "Pattern 3: OAuth2 token caching".
- Cần thêm route mới cho `AppCode` đã có → yêu cầu DBA thêm dòng vào `SysWebApiRoute`
  (`Name`/`Route`/`Version`) — KHÔNG thêm field cứng vào code để giữ path.
- Trước khi code 1 partner hoàn toàn mới → xác nhận đã có dòng `SysWebApi` + đủ dòng
  `SysWebApiRoute` trong DB (checklist đầy đủ + template code từng bước:
  `.claude/skills/api/SKILLS.md`).

## ❌ DON'T (Tuyệt đối cấm)
- Cấm hardcode Base URL/Endpoint/API Key/Credentials của partner trong `appsettings.json` hoặc
  trực tiếp trong code C#.
- Cấm bỏ qua `ICentralMDRepository.GetSysWebApiAsync` để tự `SELECT` trực tiếp `SysWebApi`/
  `SysWebApiRoute`, hoặc tự tạo cache key khác ngoài `MD:SysWebApi` cho cùng mục đích — chỉ 1
  nguồn/1 cơ chế cache duy nhất.
- Cấm Controller gọi thẳng Infrastructure `I{Name}AppService` — phải qua Application
  `I{Name}Service` (nhắc lại `.claude/rules/architecture-layers.md`, áp dụng bắt buộc cho mọi
  external API).
- Cấm dùng `RedisConst.Redis_Key_SysWebApi`/`Redis_Key_SysWebApiUser`
  (`src/POS.Common/Const/RedisConst.cs:21-22`) — hằng số đã **dead/lệch**, không khớp key thật
  `"MD:SysWebApi"` đang dùng trong `CentralMDRepository` và không được reference ở đâu khác.
- Cấm tự chế cơ chế invalidate cache `MD:SysWebApi` khi chưa được giao task riêng — cơ chế hiện
  tại là **TTL-only** (12 giờ): đổi config trong DB có hiệu lực sau tối đa 12h, hoặc DBA tự xóa
  key Redis. Đây là hành vi đã biết và được chấp nhận có chủ đích, không phải bug tự ý "sửa".
- Cấm quên: `KeyExpireAsync` (dùng trong `HashSetAsync`,
  `src/POS.Infrastructure/Cache/RedisManager.cs:189-206`) set TTL trên **toàn bộ hash key**
  `MD:SysWebApi`, không phải riêng field — refresh cache của 1 `appCode` sẽ reset TTL của MỌI
  `appCode` khác đang nằm trong cùng hash. Không thiết kế logic dựa trên TTL lệch pha giữa các
  partner.
- Cấm nhầm `SysWebApiConfig` (`src/POS.Common/Dtos/CentralMD/CentralMDDto.cs` — dùng cho
  DB-shard routing nội bộ `StoreSetConfig`) với `SysWebApiDto`/`SysWebApi`
  (`src/POS.Common/Dtos/SysWebApiDto.cs` — dùng cho external partner HTTP config) — 2 khái niệm
  tên gần giống nhưng mục đích hoàn toàn khác nhau.

---

> Chi tiết template code từng bước, bảng ý nghĩa field `SysWebApiDto`, checklist tạo AppService
> mới: **`.claude/skills/api/SKILLS.md`** — đọc file đó để lấy code mẫu, KHÔNG lặp lại ở đây.
