# /web-add-feature — Tạo feature đầy đủ cho POS.Web

Dùng lệnh này khi cần tạo một feature có đủ 3 tầng: **Page + local Service + ViewModel**.
Phù hợp với feature phức tạp hơn một page đơn giản.

---

## Cách dùng

```
/web-add-feature
```

Hoặc cung cấp thông tin luôn:
```
/web-add-feature RevenueChart Store services=ICentralSaleRepository data="daily revenue + hourly breakdown"
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin (nếu chưa có)
1. Tên feature (PascalCase, ví dụ: `RevenueChart`, `ShiftReport`, `LoyaltyStats`)
2. Section: `Store` / `Ops` / `Admin`
3. Services backend cần dùng (từ danh sách CLAUDE.md mục 4)
4. Dữ liệu cần hiển thị (mô tả ngắn — ví dụ: "daily revenue + hourly breakdown")

### Bước 2 — Tạo 3 files

#### File 1: ViewModel
`src/POS.Web/Features/{Section}/{Feature}/{Feature}Model.cs`
- Chứa ViewModel (dữ liệu để render UI, không phải DB model)
- Namespace: `POS.Web.Features.{Section}.{Feature}`
- Ví dụ:
  ```csharp
  namespace POS.Web.Features.Store.RevenueChart;
  public class RevenueSummaryViewModel { public decimal TodayRevenue; public int OrderCount; }
  public class RevenueDailyViewModel   { public DateTime Date; public decimal Net; }
  ```

#### File 2: Local Service
`src/POS.Web/Features/{Section}/{Feature}/{Feature}Service.cs`
- Inject repository/service từ DI
- Method async, có `CancellationToken ct = default`
- Transform DB data → ViewModel
- Try/catch, log KibanaService khi exception
- Namespace: `POS.Web.Features.{Section}.{Feature}`
- Ví dụ:
  ```csharp
  namespace POS.Web.Features.Store.RevenueChart;
  public class RevenueChartService(ICentralSaleRepository repo, IKibanaService kibana)
  {
      public async Task<RevenueSummaryViewModel> GetSummaryAsync(CancellationToken ct = default)
      {
          try { var data = await repo.GetRevenueSummaryAsync(DateTime.Today, ct); return new() { ... }; }
          catch (Exception ex) { kibana.LogException("RevenueChartService.GetSummary", "", 0, "", ex.Message); return new(); }
      }
  }
  ```

#### File 3: Page Component
`src/POS.Web/Features/{Section}/{Feature}/{Feature}Page.razor`
- Route: `/section/feature-name` (kebab-case)
- Inject `{Feature}Service` (local service vừa tạo)
- Inject `IKibanaService`, `ISnackbar`
- `[CascadingParameter] AuthState` (nếu Store section)
- Loading/Error state chuẩn
- Render ViewModel bằng MudBlazor components

### Bước 3 — Đăng ký DI (nếu local service là Scoped)
Hỏi: "Có cần thêm DI registration cho `{Feature}Service` không?"
- Nếu service chỉ dùng trong 1 page → có thể inject trực tiếp qua `@inject` (Blazor tự tạo)
- Nếu dùng nhiều nơi → thêm vào `Program.cs`: `builder.Services.AddScoped<{Feature}Service>()`

### Bước 4 — Xác nhận
Liệt kê 3 files đã tạo, route để truy cập, và gợi ý test.

---

## Lưu ý quan trọng

- `{Feature}Model.cs` = ViewModel dùng cho UI — KHÔNG phải DTO từ DB (DTO đã có trong POS.Common)
- `{Feature}Service.cs` = orchestration layer — gọi Repository → transform → trả ViewModel
- Page component KHÔNG gọi Repository trực tiếp — chỉ gọi qua `{Feature}Service`
- Nếu cần chart: xem CLAUDE.md "MudBlazor v9 Breaking Changes" — dùng `<Line T="double">` / `<Bar T="double">`
- Newtonsoft.Json cho serialization, KHÔNG dùng System.Text.Json
