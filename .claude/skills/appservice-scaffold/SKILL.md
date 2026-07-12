---
name: appservice-scaffold
description: Scaffold đúng chuẩn pattern 3 lớp AppService cho external HTTP client mới (GotIT, Urbox, AkaChain-style) — sinh Infrastructure AppService, Application thin wrapper, đăng ký DI 2 tầng, và test contract JSON. Dùng khi cần tích hợp một external partner API mới (voucher, loyalty, payment...) vào POS_Migration.
---

# AppService Scaffold — POS_Migration

Dùng skill này khi cần tạo **service gọi external HTTP API mới** (partner voucher, loyalty, payment...).
Toàn bộ template dưới đây rút từ 2 implementation thật trong repo: `GotITService` (không OAuth) và
`AkaChainLoyaltyAppService` (có OAuth + Redis) — không suy diễn, không bịa dependency.

## Bước 0 — Thu thập input trước khi sinh code

1. Tên Partner/Domain, VD `Urbox` → dùng cho `{Name}` trong mọi file/class.
2. Domain folder: mặc định `Partner` — **tất cả partner hiện tại đều gom chung** namespace
   `POS.Infrastructure.AppServices.Partner` / `POS.Application.Features.Partner` (không tách folder
   riêng mỗi partner). Chỉ tạo domain folder khác nếu đây rõ ràng không phải partner integration
   (VD: DataSync, Ops) — tham khảo `.claude/skills/codebase-map/SKILL.md`.
3. Danh sách method + request/response DTO (đặt trong `src/POS.Common/Dtos/{Domain}/` — dùng skill
   `/add-dto-common` nếu port từ legacy).
4. Partner có OAuth2 (client_credentials) hay dùng API key tĩnh? → quyết định có cần khối Redis token
   cache hay không (xem bảng quyết định cuối file).
5. Return type: mặc định **`ResultResponse`** (khuyến nghị cho mọi partner mới). Chỉ dùng
   `Tuple<bool, string, List<T>>` khi **thêm method vào 1 partner cũ đã dùng Tuple** (GotIT, Urbox) —
   để nhất quán trong cùng file, không trộn 2 kiểu return trong 1 class.

## Bước 1 — Infrastructure interface: `I{Name}AppService.cs`

Path: `src/POS.Infrastructure/AppServices/Partner/I{Name}AppService.cs`

```csharp
using POS.Common;
using POS.Common.Dtos.{Domain};

namespace POS.Infrastructure.AppServices.Partner;

/// <summary>
/// HTTP client wrapper cho {Name} API. Chỉ chịu trách nhiệm: serialize request → gọi HTTP →
/// deserialize response. Không chứa business rule.
/// </summary>
public interface I{Name}AppService
{
    Task<ResultResponse> {MethodName}Async({RequestDto} request, CancellationToken ct = default);
}
```

## Bước 2 — Infrastructure implementation: `{Name}Service.cs`

Path: `src/POS.Infrastructure/AppServices/Partner/{Name}Service.cs`

### 2a. Case không cần OAuth (giống GotIT — API key tĩnh/không auth)

```csharp
using System.Net;
using Newtonsoft.Json;
using POS.Common;
using POS.Common.Dtos.{Domain};
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.AppServices.Partner;

public sealed class {Name}Service(
    ICentralMDRepository centralMDRepository,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper,
    IHttpClientFactory httpClientFactory
) : I{Name}AppService
{
    private const string NotFoundConfig = "Không tìm thấy thông tin cấu hình";

    public async Task<ResultResponse> {MethodName}Async({RequestDto} request, CancellationToken ct = default)
    {
        const string endpoint = "{Name}.{MethodName}";
        try
        {
            var config = await centralMDRepository.GetSysWebApiAsync("{PartnerCode}", ct);
            if (config == null) return MakeResponse(HttpStatusCode.BadRequest, NotFoundConfig, null);

            var route = config.SysWebApiRoute?.FirstOrDefault(x =>
                string.Equals(x.Name, "{RouteName}", StringComparison.OrdinalIgnoreCase));
            if (route == null) return MakeResponse(HttpStatusCode.BadRequest, NotFoundConfig, null);

            var bodyJson = JsonConvert.SerializeObject(request);
            kibanaService.LogRequest(config.Host + route.Route, request.PosNo, bodyJson);

            var client = httpClientFactory.CreateClient("{Name}");
            client.BaseAddress = new Uri(config.Host!);
            client.Timeout = TimeSpan.FromSeconds(35);

            var response = await client.PostAsync(route.Route,
                new StringContent(bodyJson, System.Text.Encoding.UTF8, "application/json"), ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            kibanaService.LogResponse(config.Host + route.Route, request.PosNo, 0, "", responseBody);

            if (response.StatusCode != HttpStatusCode.OK)
                return MakeResponse((HttpStatusCode)response.StatusCode, "Partner error", responseBody);

            var data = JsonConvert.DeserializeObject<{ResponseDto}>(responseBody);
            return MakeResponse(HttpStatusCode.OK, "Success", data);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs(endpoint, ex);
            kibanaService.LogException(endpoint, request.PosNo, 0, "", ex.Message);
            return MakeResponse(HttpStatusCode.Conflict, ex.Message, null, JsonConvert.SerializeObject(ex));
        }
    }

    private static ResultResponse MakeResponse(HttpStatusCode status, string message, object? data, string technical = "")
        => new() { Status = status, Message = message, Data = data, MessageTechnical = technical };
}
```

### 2b. Case cần OAuth2 + Redis token cache (giống AkaChain/FMV)

Thêm `IRedisService redis` vào constructor, và chèn khối sau **trước** method public:

```csharp
using POS.Infrastructure.Redis;

private const string TokenCacheKey = "{Partner}:{Service}:AccessToken";

private async Task<string?> GetAccessTokenAsync(SysWebApiDto config)
{
    var cached = redis.StringGetRaw(TokenCacheKey);
    if (!string.IsNullOrEmpty(cached)) return cached;

    var tokenRoute = config.SysWebApiRoute?.FirstOrDefault(x =>
        string.Equals(x.Name, "GetToken", StringComparison.OrdinalIgnoreCase));
    if (tokenRoute == null) return null;

    var client = httpClientFactory.CreateClient("{Name}");
    using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, config.Host + tokenRoute.Route);
    var bodyBytes = System.Text.Encoding.UTF8.GetBytes(
        $"grant_type=client_credentials&client_id={Uri.EscapeDataString(config.UserName ?? "")}" +
        $"&client_secret={Uri.EscapeDataString(config.Password ?? "")}");
    tokenRequest.Content = new ByteArrayContent(bodyBytes);
    tokenRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

    var response = await client.SendAsync(tokenRequest);
    var responseString = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        fileLogHelper.WriteExpLogs("{Name}Service.GetAccessTokenAsync", new Exception(responseString));
        return null;
    }

    var dataToken = JsonConvert.DeserializeAnonymousType(responseString, new { access_token = "", expires_in = 0 });
    if (string.IsNullOrEmpty(dataToken?.access_token)) return null;

    var ttl = dataToken.expires_in > 0 ? Math.Max(dataToken.expires_in - 60, 60) : 240;
    redis.StringSetRaw(TokenCacheKey, dataToken.access_token, TimeSpan.FromSeconds(ttl));
    return dataToken.access_token;
}
```

Rồi thêm header `Authorization: Bearer {token}` khi gọi API chính (xem `CallApiAsync` trong
`AkaChainLoyaltyAppService.cs` nếu cần đối chiếu chi tiết hơn).

**Dependency cố định của lớp Infrastructure** (không bịa thêm ngoài danh sách này trừ khi thật sự cần):

| Dependency | Namespace | Vai trò |
|---|---|---|
| `ICentralMDRepository` | `POS.Infrastructure.Repositories.Interfaces` | Lấy config partner (`GetSysWebApiAsync`) |
| `IKibanaService` | `POS.Infrastructure.Logging` | `LogRequest/LogResponse/LogInfo/LogException` |
| `IFileLogHelper` | `POS.Infrastructure.Logging` | `WriteExpLogs(context, ex)` |
| `IHttpClientFactory` | built-in | `CreateClient("{Name}")` |
| `IRedisService` | `POS.Infrastructure.Redis` | **Chỉ khi có OAuth** — `StringGetRaw`/`StringSetRaw` |

## Bước 3 — Infrastructure DI

File: `src/POS.Infrastructure/DependencyInjection.cs`

```csharp
// BaseAddress/Timeout KHÔNG set ở đây — đọc từ DB (SysWebApi.Host) per-request.
services.AddHttpClient("{Name}");
services.AddScoped<I{Name}AppService, {Name}Service>();
```

## Bước 4 — Application interface: `I{Name}Service.cs`

Path: `src/POS.Application/Features/Partner/I{Name}Service.cs` — **copy y hệt signature** của
`I{Name}AppService`, chỉ đổi namespace:

```csharp
using POS.Common;
using POS.Common.Dtos.{Domain};

namespace POS.Application.Features.Partner;

public interface I{Name}Service
{
    Task<ResultResponse> {MethodName}Async({RequestDto} request, CancellationToken ct = default);
}
```

## Bước 5 — Application thin wrapper: `{Name}Service.cs`

Path: `src/POS.Application/Features/Partner/{Name}Service.cs`

```csharp
using POS.Common;
using POS.Common.Dtos.{Domain};
using POS.Infrastructure.AppServices.Partner;

namespace POS.Application.Features.Partner;

public sealed class {Name}Service(I{Name}AppService appService) : I{Name}Service
{
    public Task<ResultResponse> {MethodName}Async({RequestDto} request, CancellationToken ct = default)
        => appService.{MethodName}Async(request, ct);
}
```

**Quy tắc cứng:** mỗi method Application chỉ là `=> appService.Method(args)` — KHÔNG try/catch,
KHÔNG log, KHÔNG business logic. Nếu cần logic (validate, map dữ liệu, gọi thêm repository khác),
logic đó thuộc về Application nhưng vẫn KHÔNG re-implement lại việc gọi HTTP — chỉ điều phối thêm
trên kết quả trả về từ `appService`.

## Bước 6 — Application DI

File: `src/POS.Application/DependencyInjection.cs`

```csharp
services.AddScoped<I{Name}Service, {Name}Service>();
```

## Bước 7 — Controller wiring

Inject `I{Name}Service` (Application) — **KHÔNG** inject `I{Name}AppService`. Mẫu response mapping
(lấy từ `PaymentController.cs`):

```csharp
[HttpPost]
[Route("{route}")]
public async Task<IActionResult> {Action}([FromBody] {RequestDto} request, CancellationToken ct = default)
{
    var sw = Stopwatch.StartNew();
    const string endpoint = "{route}";
    kibanaService.LogRequest(endpoint, request.PosNo, JsonConvert.SerializeObject(request));
    try
    {
        var result = await {name}Service.{MethodName}Async(request, ct);
        sw.Stop();
        kibanaService.LogResponse(endpoint, request.PosNo, sw.ElapsedMilliseconds, "", JsonConvert.SerializeObject(result.Data));
        return StatusCode((int)result.Status, result);   // ResultResponse tự mang HTTP status
    }
    catch (Exception ex)
    {
        sw.Stop();
        fileLogHelper.WriteExpLogs("{Controller}.{Action}", ex);
        kibanaService.LogException(endpoint, request.PosNo, 0, "", ex.Message);
        return StatusCode((int)HttpStatusCode.InternalServerError, new ResultResponse
        {
            Message = $"Exception:{ex.Message}",
            Status = HttpStatusCode.InternalServerError,
            MessageTechnical = ex.Message
        });
    }
}
```

Nếu return type là `Tuple` (case mở rộng partner cũ) thay vì `StatusCode((int)result.Status, result)`,
dùng: thành công → `OkResult(tuple.Item3, tuple.Item2)`; thất bại →
`StatusCode((int)HttpStatusCode.BadRequest, new ResultResponse { Status = ..., Message = tuple.Item2, Data = tuple.Item3 })`.

## Bước 8 — Contract test bắt buộc (thủ công, KHÔNG tự động qua reflection)

**Lưu ý quan trọng:** DI validation test (`DependencyInjectionTests.cs`) **tự pass** nếu bước 3 và 6
đúng — không cần sửa file test đó. Nhưng **JSON contract test PHẢI thêm thủ công** cho mọi DTO response
mới trả về POS client, trong `tests/POS.ContractTests/JsonFieldContractTests.cs`:

```csharp
[Fact]
public void {ResponseDto}_locked()
    => AssertFields(typeof({ResponseDto}), "Field1", "Field2", "Field3");
```

Danh sách field truyền vào `AssertFields` phải khớp chính xác tên JSON hiệu lực của DTO (tôn trọng
`[JsonProperty("...")]`, loại trừ `[JsonIgnore]`) — xem `JsonContract.EffectiveFieldNames`.

## Checklist cuối cùng (đối chiếu CLAUDE.md — "Checklist khi tạo service HTTP client mới")

```
□ I{Name}AppService.cs         → src/POS.Infrastructure/AppServices/Partner/
□ {Name}Service.cs (Infra)     → cùng folder, cùng namespace với interface
□ AddHttpClient("{Name}") + AddScoped<I{Name}AppService, {Name}Service>()
                                 → src/POS.Infrastructure/DependencyInjection.cs
□ I{Name}Service.cs            → src/POS.Application/Features/Partner/, cùng signature AppService
□ {Name}Service.cs (App)       → thin wrapper, mỗi method chỉ => appService.Method(...)
□ AddScoped<I{Name}Service, {Name}Service>()
                                 → src/POS.Application/DependencyInjection.cs
□ Controller                    → inject I{Name}Service, KHÔNG inject AppService
□ [Fact] AssertFields(...)      → tests/POS.ContractTests/JsonFieldContractTests.cs cho DTO response mới
□ dotnet test tests/POS.ContractTests xanh trước khi commit (xem skill git-workflow)
```

## Bảng quyết định nhanh

| Quyết định | Khi nào chọn |
|---|---|
| Return `ResultResponse` | Mặc định cho mọi partner mới |
| Return `Tuple<bool,string,List<T>>` | Chỉ khi thêm method vào partner cũ đã dùng Tuple (GotIT, Urbox) — để nhất quán trong file |
| Thêm khối OAuth + Redis (Bước 2b) | Partner yêu cầu access token (client_credentials/OAuth2) |
| Bỏ qua OAuth (Bước 2a) | Partner dùng API key tĩnh trong header/query (giống GotIT) |
