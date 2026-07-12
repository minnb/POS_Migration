---
name: payment-test-generator
description: Sinh unit test (xUnit + Moq + FluentAssertions) trong project tests/POS.UnitTests cho
  luồng Payment (voucher validate/redeem qua GotIT/Urbox) và các service tầng Application. Mã hóa
  Nguyên tắc Mock — test qua seam interface Application (IGotITService/IUrboxService), dựng Tuple
  return của partner, phủ kịch bản routing-theo-partner / success=false→400 / default→BadRequest /
  exception→500 / delegate thin-wrapper. Đọc TRƯỚC khi viết bất kỳ unit test nào cho Payment hoặc
  service Application mới.
---

# Skill: Payment Test Generator (xUnit + Moq + FluentAssertions)

> **Áp dụng khi:** cần sinh/bổ sung unit test cho `PaymentController` hoặc service voucher partner
> tầng Application (`GotITService`/`UrboxService`, và mọi `I{Name}Service` tương lai). KHÔNG dùng
> cho contract/guardrail test — đó là `tests/POS.ContractTests`, giữ nguyên, không đụng.

---

## 0. Tại sao skill này tồn tại

Luồng Payment (`api/v2/partner` — validate & redeem voucher GotIT/Urbox) hiện có **0 unit test**.
Regression về routing-theo-partner, nhánh `success=false`, nhánh exception chỉ lộ ở production.
Skill này đóng gói quy trình sinh test **đúng chuẩn dự án** để lặp lại nhanh cho mọi partner.

## 1. Nguyên tắc Mock (CỐT LÕI — bắt buộc tuân thủ)

1. **Test vào seam interface Application**, KHÔNG test thẳng Infrastructure AppService, KHÔNG gọi
   HTTP/DB thật. Controller phụ thuộc `IGotITService`/`IUrboxService` → mock đúng 2 interface này.
2. **Mock bằng Moq**, assert bằng **FluentAssertions**. Framework test: **xUnit** (đồng bộ toàn repo).
3. **Không thêm production code, không đổi field JSON** của DTO response (contract 5.000 POS bất biến).
   Skill chỉ *đọc* DTO `POS.Common.Dtos.PartnerApi`.
4. **Không phá Clean Architecture**: test là consumer của interface, dependency flow không đổi.
5. **Tách project**: test đặt ở `tests/POS.UnitTests` (mới) — KHÔNG trộn vào `POS.ContractTests`.

## 2. Setup project `tests/POS.UnitTests` (làm 1 lần)

Tạo `tests/POS.UnitTests/POS.UnitTests.csproj` (`net10.0`, nullable enabled), reference các project
production cần dùng và các package:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
  <ItemGroup>
    <!-- BẮT BUỘC có POS.Infrastructure: ctor PaymentController cần IKibanaService/IFileLogHelper
         (POS.Infrastructure.Logging); delegation test cần IGotITAppService/IUrboxAppService. -->
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <ProjectReference Include="..\..\src\POS.Common\POS.Common.csproj" />
    <ProjectReference Include="..\..\src\POS.Application\POS.Application.csproj" />
    <ProjectReference Include="..\..\src\POS.Infrastructure\POS.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\POS.Api\POS.Api.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

> `FrameworkReference Microsoft.AspNetCore.App` cần khai báo tường minh để test truy cập
> `IActionResult`/`ObjectResult`/`OkObjectResult`. Version package chốt lúc restore thật — **NuGet
> dùng OS trust store nên restore Moq/FluentAssertions qua được TLS interception công ty** (khác
> npm/uvx bị chặn — xem `.mcp.json` blockers).

Đăng ký vào `POS.slnx` (trong `<Folder Name="/tests/">`):
```xml
<Project Path="tests/POS.UnitTests/POS.UnitTests.csproj" />
```

## 3. Bản đồ target test luồng Payment (đọc code thật trước khi sinh)

| Lớp | File nguồn | Public API | Kịch bản bắt buộc |
|---|---|---|---|
| Controller | `src/POS.Api/Controllers/PaymentController.cs` | `ValidateVoucher(CheckVoucherPartnerPOSRequest, CancellationToken)` POST `voucher/check` | `Partner="GOTIT"`→gọi `CheckMultiple`; `="URBOX"`→`CheckSerialUrbox`; khác→BadRequest(400); mock trả `success=false`→StatusCode 400; mock throw→StatusCode 500 |
| Controller | 〃 | `UpdateStatusVoucher(UpdateStatusVoucherPartnerRequest, CancellationToken)` POST `voucher/update-status` | tương tự với `MarkUseMultiple` / `PayCodelUrbox` |
| Application | `src/POS.Application/Features/Partner/GotITService.cs` | thin wrapper `IGotITService` | mỗi method **delegate đúng 1 lần** sang `IGotITAppService` với cùng tham số |
| Application | `src/POS.Application/Features/Partner/UrboxService.cs` | thin wrapper `IUrboxService` | 〃 sang `IUrboxAppService` |

> **BẮT BUỘC đọc `BaseController`** (`OkResult(...)` / `StatusCode(...)` trả kiểu gì) trước khi
> assert — chỉ assert trên `ObjectResult.StatusCode` khi đã xác nhận, KHÔNG đoán shape response.

## 4. Dựng `Tuple` return của partner (bẫy hay gặp nhất)

Return type là **positional Tuple** — nhớ đúng thứ tự `Item`:

| Service | Kiểu trả về | Ý nghĩa Item |
|---|---|---|
| GotIT (`CheckMultiple`/`MarkUseMultiple`) | `Tuple<bool, string, List<DataVoucherPartnerResponse>>` | `Item1`=success, `Item2`=message, `Item3`=data |
| Urbox (`CheckSerialUrbox`/`PayCodelUrbox`) | `Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>` | +`Item4`=danh sách `UrboxProducts` |

DTO tại `src/POS.Common/Dtos/PartnerApi/` (`CheckVoucherPartnerPOSRequest` có property `Partner`).

## 5. Quy ước đặt tên & bố cục

- Namespace: `namespace POS.UnitTests.Features.Partner;` (file-scoped).
- Tên method test: `Method_condition_expectedResult`
  (vd `ValidateVoucher_gotitFails_returns400`, `ValidateVoucher_unknownPartner_returnsBadRequest`).
- Bố cục: **Arrange–Act–Assert** rõ ràng. Mỗi `[Fact]` một hành vi; dùng `[Theory]`+`[InlineData]`
  khi cùng logic khác partner code.
- File: `tests/POS.UnitTests/Features/Partner/{PaymentControllerTests,GotITServiceTests,UrboxServiceTests}.cs`.

## 6. Template snippet (điều chỉnh theo signature thật khi sinh)

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using POS.Api.Controllers;
using POS.Common.Dtos.PartnerApi;
using POS.Application.Features.Partner;
using Xunit;

namespace POS.UnitTests.Features.Partner;

public class PaymentControllerTests
{
    private static PaymentController BuildSut(
        Mock<IGotITService>? gotit = null, Mock<IUrboxService>? urbox = null)
    {
        gotit ??= new Mock<IGotITService>();
        urbox ??= new Mock<IUrboxService>();
        // TODO khi sinh: đọc ctor thật của PaymentController + BaseController để truyền đủ
        // IKibanaService, IFileLogHelper (mock), và thứ tự tham số chính xác.
        return new PaymentController(/* kibana */ null!, /* fileLog */ null!,
                                     gotit.Object, urbox.Object);
    }

    [Fact]
    public async Task ValidateVoucher_gotitFails_returns400()
    {
        var gotit = new Mock<IGotITService>();
        gotit.Setup(x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                         It.IsAny<CancellationToken>()))
             .ReturnsAsync(Tuple.Create(false, "voucher invalid",
                                        new List<DataVoucherPartnerResponse>()));
        var sut = BuildSut(gotit: gotit);

        var result = await sut.ValidateVoucher(
            new CheckVoucherPartnerPOSRequest { Partner = "GOTIT" }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(400);
        gotit.Verify(x => x.CheckMultiple(It.IsAny<CheckVoucherPartnerPOSRequest>(),
                                          It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

Delegation test (thin wrapper) — ví dụ `GotITServiceTests`:
```csharp
[Fact]
public async Task CheckMultiple_delegates_to_appService()
{
    var app = new Mock<IGotITAppService>();   // POS.Infrastructure.AppServices.Partner
    var expected = Tuple.Create(true, "ok", new List<DataVoucherPartnerResponse>());
    var req = new CheckVoucherPartnerPOSRequest();
    app.Setup(x => x.CheckMultiple(req, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
    var sut = new GotITService(app.Object);

    var actual = await sut.CheckMultiple(req, CancellationToken.None);

    actual.Should().BeSameAs(expected);
    app.Verify(x => x.CheckMultiple(req, It.IsAny<CancellationToken>()), Times.Once);
}
```
> `IGotITAppService`/`IUrboxAppService` ở `src/POS.Infrastructure/AppServices/Partner/`.
>
> ⚠️ **Bẫy CS0104 (ambiguous):** class `GotITService`/`UrboxService` **trùng tên** ở CẢ
> `POS.Application.Features.Partner` (thin wrapper) lẫn `POS.Infrastructure.AppServices.Partner`
> (HTTP client) — hệ quả AppService 3-layer. File delegation import cả 2 namespace sẽ lỗi
> ambiguous. Fix: alias namespace Application rồi gọi đích danh —
> `using AppPartner = POS.Application.Features.Partner;` → `new AppPartner.GotITService(app.Object)`.
> (PaymentControllerTests KHÔNG dính vì chỉ dùng interface `IGotITService`/`IUrboxService`, không
> import namespace Infrastructure.AppServices.)

## 7. Checklist trước khi báo "xong" (bằng chứng bắt buộc)

- [ ] `dotnet test tests/POS.UnitTests` **PASS** — dán số test passed làm bằng chứng.
- [ ] `dotnet test tests/POS.ContractTests` **vẫn PASS** — không hồi quy guardrail.
- [ ] `dotnet build POS.slnx -clp:ErrorsOnly` = 0 error.
- [ ] Không thêm production code; không đổi DTO/field JSON.
- [ ] Đã đọc `BaseController` để assert đúng shape response (không đoán).
- [ ] Ba file test tồn tại: `PaymentControllerTests.cs`, `GotITServiceTests.cs`, `UrboxServiceTests.cs`.
