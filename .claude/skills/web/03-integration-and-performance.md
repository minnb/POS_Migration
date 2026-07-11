# Skill: Integration, State & Performance POS.Web (Blazor Server + MudBlazor 9.5.0)

> **Đọc file này khi:** viết `@code` gọi data, chia sẻ state giữa component, xử lý sự kiện async,
> dùng JS Interop, hoặc tối ưu hiệu năng render trong `src/POS.Web/` — đây là bản "hiến pháp" rút gọn
> LUẬT BẮT BUỘC về tích hợp, state, error handling và performance.
>
> **Quan hệ với các file khác** (file này là index luật, chi tiết + code mẫu ở nơi được trỏ):
> - Kiến trúc/auth/lifecycle 3-state/DI/logging nền tảng: **`01-architecture-and-logic.md`**.
> - Layout/styling/component UI: **`02-ui-ux-and-components.md`**.
> - Pattern DI đầy đủ, danh sách service inject được: **`SKILLS.md`**.
>
> Khi luật ở đây và file chi tiết lệch nhau → file chi tiết thắng; sửa lại file này cho khớp trong cùng commit.

---

## 1. API & Service Injection

- **CẤM tuyệt đối** `@inject HttpClient` (hoặc field `HttpClient`) trong `.razor`/`.razor.cs` — dự án
  hiện tại **không có** file nào vi phạm, giữ nguyên trạng thái này.
- Mọi thao tác lấy/ghi dữ liệu **BẮT BUỘC** qua Service (`POS.Application`) hoặc Repository
  (`POS.Infrastructure`) inject qua DI — POS.Web đã đăng ký sẵn `AddInfrastructure()` +
  `AddApplication()`, không cần gọi HTTP nội bộ tới `POS.Api`.
- Chi tiết đầy đủ + danh sách service inject được: `01-architecture-and-logic.md` §4, `SKILLS.md`
  §"Services có thể inject".

## 2. Quản lý State giữa Component

- **Chia sẻ state trong 1 circuit** (giữa nhiều component của cùng 1 user session): dùng **Scoped
  Service** (`AddScoped<T>`) inject qua DI — KHÔNG dùng `static`/`Singleton` để giữ state theo user
  (Blazor Server 1 circuit = 1 user, Singleton bị chia sẻ chéo giữa mọi user đang online).
- **Truyền dữ liệu/context từ layout hoặc component cha xuống nhiều component con**: dùng
  `[CascadingParameter]` — pattern đã dùng rộng rãi trong dự án (`Task<AuthenticationState> AuthState`
  ở mọi page, xem `01-architecture-and-logic.md` §2/§3).
- **JS Interop / `localStorage`**: hạn chế tối đa. Dự án hiện **không dùng `localStorage`** ở bất kỳ
  đâu — chỉ cân nhắc khi có yêu cầu bắt buộc phải duy trì state qua **reload trình duyệt / ngoài
  vòng đời circuit** (vd nhớ tab đang chọn, nhớ filter giữa các lần F5). State trong phạm vi 1
  circuit (không qua reload) **luôn** dùng Scoped Service/CascadingParameter ở trên, không dùng
  `localStorage`.

## 3. Xử lý sự kiện & Bắt lỗi (Error Handling)

- **BẮT BUỘC** mọi hàm xử lý sự kiện async (`@onclick="SaveAsync"`, `@onchange`, callback dialog...)
  bọc trong `try/catch` **đầy đủ** — không chỉ `try/finally`. Xem lý do kỹ thuật (crash circuit
  SignalR) ở `01-architecture-and-logic.md` §3.
- **Hiển thị lỗi cho người dùng**: BẮT BUỘC qua `ISnackbar` (`Snackbar.Add("...", Severity.Error)`)
  — **CẤM** dùng `alert()`/`confirm()` JS native hoặc `window.confirm` qua JS Interop cho mục đích
  thông báo lỗi.
- **Log chi tiết**: BẮT BUỘC `IKibanaService.LogException("Page.MethodName", "", 0, "", ex.Message)`
  trong `catch` — **KHÔNG** log dữ liệu nhạy cảm (card number, password, token, PII).
- **Mẫu chuẩn:**
  ```csharp
  private async Task SaveAsync()
  {
      try
      {
          await MyService.SaveAsync(_model);
          Snackbar.Add("Lưu thành công", Severity.Success);
          await LoadDataAsync();
      }
      catch (Exception ex)
      {
          Snackbar.Add("Không thể lưu dữ liệu. Vui lòng thử lại.", Severity.Error);
          KibanaService.LogException("PageName.SaveAsync", "", 0, "", ex.Message);
      }
  }
  ```
- `OnInitializedAsync`/lifecycle method có quy tắc riêng (đã có ở `01-architecture-and-logic.md` §3)
  — mục này áp dụng cho **event handler** runtime (người dùng bấm nút/nhập liệu), không thay thế.

## 4. JS Interop — phương án cuối cùng

- Chỉ dùng JS Interop cho tác vụ **đặc thù không có API Blazor Server tương đương**: kết nối máy in,
  focus element nâng cao (auto-focus sau dialog mở), tải file stream về trình duyệt (download/PDF
  blob). Mọi nhu cầu khác ưu tiên component/API .NET có sẵn.
- **Gọi 1-shot, không giữ state/module** (vd `InvokeVoidAsync` gọi hàm JS toàn cục có sẵn trong
  `wwwroot/js/*.js`): đóng gói thành **static extension method** trên `IJSRuntime` — không cần
  `IAsyncDisposable` vì không giữ tài nguyên nào giữa các lần gọi. Tham chiếu có sẵn:
  `Services/JsDownloadExtensions.cs` (`SaveAsFileAsync`, `CreatePdfBlobUrlAsync`...).
- **Gọi lặp lại/giữ state hoặc load JS module** (`IJSObjectReference` qua
  `JSRuntime.InvokeAsync<IJSObjectReference>("import", "./module.js")`, kết nối thiết bị lâu dài như
  máy in): BẮT BUỘC đóng gói vào **1 service C# riêng implement `IAsyncDisposable`**, giải phóng
  `IJSObjectReference`/kết nối trong `DisposeAsync()`. Dự án hiện **chưa có** ví dụ pattern này —
  khi thêm mới, đặt trong `Services/`, đăng ký `AddScoped<T>` (state theo circuit, không phải
  Singleton).
- **CẤM** gọi `IJSRuntime.InvokeAsync` rải rác trực tiếp trong `@code` của nhiều page cho cùng 1 tác
  vụ — tập trung vào đúng 1 service/extension class dùng chung.

## 5. Tối ưu hiệu năng (Performance)

- **`CancellationToken` cho Service/Repository call dài**: truyền `CancellationToken` vào các
  phương thức async gọi DB/HTTP tốn thời gian, để hủy khi user điều hướng sang trang khác giữa
  chừng (tránh lãng phí tài nguyên server + tránh `StateHasChanged` gọi trên component đã unmount).
  Component implement `IDisposable`/`IAsyncDisposable` tạo `CancellationTokenSource` riêng, `Cancel()`
  trong `Dispose`/`DisposeAsync`, truyền `.Token` vào lời gọi Service.
  ```csharp
  private readonly CancellationTokenSource _cts = new();

  private async Task LoadDataAsync()
  {
      try { _data = await SaleRepo.GetSalesAsync(..., _cts.Token); }
      catch (OperationCanceledException) { /* component đã dispose, bỏ qua */ }
  }

  public void Dispose() => _cts.Cancel();
  ```
- **`@key` BẮT BUỘC** trong mọi `@foreach` sinh ra UI element (row, card, component con lặp) — dùng
  giá trị định danh ổn định của item (Id/mã, không dùng index) để Blazor Diffing Engine tái sử dụng
  đúng element thay vì re-render toàn bộ danh sách.
  ```razor
  @foreach (var item in _rows)
  {
      <MudTr @key="item.Id">
          ...
      </MudTr>
  }
  ```
  > Lưu ý: `<MudTable>` (dùng `Items=`/`ServerData`) tự quản lý diffing nội bộ — quy tắc này áp dụng
  > cho `@foreach` viết tay sinh markup/component (card list, chip list, custom row, dynamic form
  > field...), không phải mọi bảng trong app đều cần `@key` thủ công.
- **`IAsyncDisposable` BẮT BUỘC** cho component đăng ký event có vòng đời dài hơn 1 lần render
  (`NavigationManager.LocationChanged`, JS interop giữ `IJSObjectReference`, timer/`PeriodicTimer`) —
  gỡ đăng ký trong `DisposeAsync()`, nếu không sẽ rò rỉ handler mỗi lần circuit tái tạo component.
  Tham chiếu có sẵn: `Components/Layout/MainLayout.razor` (`Nav.LocationChanged -= OnLocationChanged`
  trong `DisposeAsync`).

---

## Checklist nhanh trước khi báo "xong" (Integration & Performance)

```
□ Không @inject HttpClient trong .razor — mọi data access qua Service/Repository DI
□ State dùng chung giữa component → Scoped Service hoặc CascadingParameter (không Singleton/static)
□ Không dùng localStorage trừ khi thật sự cần duy trì qua reload
□ Mọi event handler async có try/catch đầy đủ → Snackbar báo lỗi + KibanaService.LogException
□ Không dùng alert()/confirm() JS native cho thông báo lỗi
□ JS Interop 1-shot → static extension trên IJSRuntime; JS Interop giữ state/module → service riêng implement IAsyncDisposable
□ CancellationToken truyền vào Service/Repository call dài, hủy trong Dispose
□ @foreach sinh UI element (viết tay, ngoài MudTable) có @key theo Id ổn định
□ Component đăng ký LocationChanged/JS interop/timer → implement IAsyncDisposable, gỡ đăng ký trong DisposeAsync
```
