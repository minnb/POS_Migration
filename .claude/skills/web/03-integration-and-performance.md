---
name: web-integration-and-performance
description: Luật bắt buộc DI/state/error-handling/JS Interop/performance cho POS.Web, kèm pattern load nhiều nguồn độc lập, modal lazy-load tab, truyền row object vào dialog. Đọc khi viết @code gọi data/xử lý async.
---

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

## 6. Pattern: Load nhiều nguồn độc lập trong `OnInitializedAsync` — tránh crash circuit
> Áp dụng khi: page/dialog cần load ≥2 nguồn dữ liệu ĐỘC LẬP (list chính + dropdown/lookup, hoặc
> ≥2 dropdown/lookup không liên quan nhau — vd danh sách cửa hàng + danh sách ngân hàng + danh sách
> loại hàng) lúc khởi tạo.
> Rút ra từ sự cố thực tế (lặp lại **5 lần** trong 1 session — `BankPosPage`, `BankPosDetailDialog`,
> `ProductDetailDialog`, `SpecialComboPage`, `PromotionSetupPage`): nhiều lệnh `await` (dù chạy song
> song qua `Task.WhenAll` hay chạy tuần tự từng dòng — **hai cách đều lỗi y hệt nhau**) nằm trong
> 1 `try/catch` DUY NHẤT. Chỉ 1 nguồn lỗi (SP/bảng thiếu ở môi trường DEV) làm exception ném ra giữa
> chừng — các dòng SAU nó KHÔNG BAO GIỜ CHẠY, nên dropdown tương ứng trống dù bản thân nguồn đó lẽ ra
> load được bình thường. Nếu method KHÔNG có `try/catch` bao ngoài nào cả (hay chỉ page có, dialog
> quên) → exception chưa bắt trong lifecycle method còn làm sập luôn CIRCUIT Blazor Server, không
> riêng gì phần data bị lỗi.

**Cách nhận diện "độc lập" (PHẢI tách try/catch) vs "cùng 1 báo cáo" (được dùng chung 1 catch):**

| Tình huống | Độc lập hay cùng báo cáo? | Xử lý |
|---|---|---|
| `_articleTypes` + `_unitOfMeasures` + `_vatCodes` cho 3 dropdown KHÁC NHAU trong form | Độc lập | Tách 3 try/catch |
| `_salesTypes` + `_memberCodes` + `_allStores` cho 3 filter/dropdown KHÁC NHAU | Độc lập | Tách 3 try/catch |
| Summary + Detail list của CÙNG 1 domain (vd DataRawLog summary + DataRawLog list) | Cùng báo cáo | 1 try/catch OK |
| Kỳ hiện tại + kỳ so sánh của CÙNG 1 metric (vd Revenue kỳ này + kỳ trước) | Cùng báo cáo | 1 try/catch OK |
| Order lines + payment entries của CÙNG 1 đơn hàng (dialog chi tiết giao dịch) | Cùng báo cáo | 1 try/catch OK |

Quy tắc nhanh: nếu 2 nguồn dữ liệu đến từ 2 **domain nghiệp vụ khác nhau** (cửa hàng vs ngân hàng vs
loại hàng vs hạng thẻ...) và feed vào 2 **control UI khác nhau** — tách. Nếu chúng chỉ là 2 GÓC NHÌN
của CÙNG 1 dữ liệu/báo cáo (chi tiết vs tổng hợp, kỳ này vs kỳ trước) — gộp 1 catch là hợp lý, vì cả
trang/dialog vốn dĩ vô nghĩa nếu thiếu 1 trong 2.

```csharp
protected override async Task OnInitializedAsync()
{
    // Await + try/catch RIÊNG từng nguồn ĐỘC LẬP — 1 nguồn lỗi không kéo sập các nguồn khác,
    // và quan trọng nhất: không để exception thoát khỏi OnInitializedAsync (circuit crash).
    // Sai y hệt nếu viết tuần tự 3 dòng await trong CÙNG 1 try — không liên quan gì Task.WhenAll.
    try { _stores = await Repo.GetStoreListAsync(); }
    catch (Exception ex)
    {
        FileLogger.WriteExpLogs("MyPage.LoadStores", ex);
        Snackbar.Add("Không tải được danh sách cửa hàng.", Severity.Warning);
    }

    try { _banks = await Repo.GetBankListAsync(); }
    catch (Exception ex)
    {
        FileLogger.WriteExpLogs("MyPage.LoadBanks", ex);
        Snackbar.Add("Không tải được danh sách ngân hàng.", Severity.Warning);
    }
}
```

**Quan trọng:**
- Áp dụng cho CẢ page lẫn dialog con (`MudDialog` component) — dialog dễ bị bỏ sót vì hay copy pattern gọn (1 try bọc hết) nhưng không có `try/catch` bao ngoài như page.
- KHÔNG dùng `await Task.WhenAll(taskA, taskB)` rồi `try/catch` bao NGOÀI `WhenAll` nếu muốn 1 nguồn lỗi không ảnh hưởng nguồn còn lại — `WhenAll` ném exception của task đầu tiên fail, các task còn lại tuy vẫn chạy xong nhưng `.Result` không bao giờ được gán vì nằm sau dòng `await` đã throw.
- KHÔNG chỉ nhìn `Task.WhenAll` khi rà soát code cũ — search cả các `OnInitializedAsync` có ≥2 dòng `await Repo.GetXxxAsync()` tuần tự trong 1 try, feed vào ≥2 field khác nhau.
- Nếu component đã có sẵn kiểu báo lỗi khác (vd `Snackbar.Add(...)` đã dùng ở method khác trong CÙNG file) → dùng lại kiểu đó cho nhất quán, không cần thêm field `_errorMsg` + `MudAlert` mới nếu file chưa có.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosPage.razor` (`LoadDataAsync`),
> `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosDetailDialog.razor` (`OnInitializedAsync`),
> `src/POS.Web/Components/Pages/Catalog/Product/Dialogs/ProductDetailDialog.razor` (`OnInitializedAsync`),
> `src/POS.Web/Components/Pages/Promotion/Offers/SpecialComboPage.razor` (`OnInitializedAsync`),
> `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` (`OnInitializedAsync`)

---

## 7. Pattern: Modal chi tiết nhiều tab — lazy-load theo tab active (miễn phí, không cần state thủ công)
> Áp dụng khi: dialog/modal có nhiều tab (`MudTabs`/`MudTabPanel`), mỗi tab tự load dữ liệu riêng
> (thường mỗi tab 1 `MudTable ServerData` hoặc 1 lệnh gọi service riêng) và muốn tránh gọi hết
> N service cùng lúc lúc mở modal (khác hành vi legacy DataTables — thường load lại TOÀN BỘ mọi
> tab mỗi lần đổi tab, gây gọi API thừa).

`MudTabs` mặc định **không giữ panel không active trong DOM** (không có `KeepPanelsAlive="true"`)
— nghĩa là nội dung mỗi `MudTabPanel` (kể cả `MudTable ServerData` bên trong) chỉ thực sự
**render lần đầu khi tab đó được kích hoạt**, và `ServerData` chỉ được gọi tại thời điểm đó.
→ **Không cần tự viết `HashSet<int> _loadedTabs` hay `OnActivePanelIndexChanged` để lazy-load
thủ công** — đặt thẳng `MudTable ServerData="LoadXxxAsync"` vào từng `MudTabPanel` là đã lazy-load
đúng theo tab, miễn phí nhờ hành vi mặc định của framework.

```razor
<MudTabs Outlined="true" PanelClass="pa-3">
    <MudTabPanel Text="Tab A">
        <MudTable @ref="_tableA" ServerData="LoadAAsync" T="RowA">...</MudTable>
    </MudTabPanel>
    <MudTabPanel Text="Tab B">
        <MudTable @ref="_tableB" ServerData="LoadBAsync" T="RowB">...</MudTable>
    </MudTabPanel>
</MudTabs>
```

- Tab đầu tiên (mặc định active khi mở dialog) **vẫn load ngay** — nếu là dữ liệu 1-record đơn
  giản (không phải `MudTable`), gọi thẳng trong `OnInitializedAsync` thay vì đợi tab activate.
- Quay lại tab đã xem trước đó sẽ gọi lại `ServerData` (không cache) — chấp nhận được cho modal
  read-only tra cứu; nếu cần cache qua lại giữa các lần xem tab trong CÙNG 1 lần mở dialog, tự
  thêm field lưu kết quả và check trước khi gọi lại.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Promotion/Offers/Dialogs/OfferDetailDialog.razor`
> (6 tab: Header/Buy/Benefits/Get/Site/Priority — port từ legacy modal 6 tab load-tất-cả-mỗi-lần-đổi-tab).

---

## 8. Pattern: Truyền thẳng row object vào dialog chi tiết — KHÔNG tra cứu lại theo key đơn
> Áp dụng khi: nút "Xem chi tiết" trong `RowTemplate` của `MudTable` mở dialog hiển thị dữ liệu
> của đúng dòng đã click.
> Rút ra từ bug thực tế: `MemberPointsPage.OpenDetailDialog` tra cứu lại dòng bằng
> `_currentPageItems.FirstOrDefault(x => x.OrderNo == invoiceNo)` — chỉ so khớp 1 cột không unique
> (`LoggingLoyalty` có PK composite `OrderNo+ActionType+TransactionType`, 1 `OrderNo` có thể sinh
> nhiều dòng khác `ActionType`, vd `EARN` và `REDEEM`) → luôn trả về dòng **đầu tiên** trùng
> `OrderNo`, mở nhầm dữ liệu dòng khác khi click dòng thứ 2+.

```razor
@* SAI — tra cứu lại theo 1 cột không đảm bảo unique *@
<MudIconButton OnClick="@(() => OpenDetailDialog(context.OrderNo))"/>
@code {
    private Task OpenDetailDialog(string orderNo)
    {
        var row = _currentPageItems.FirstOrDefault(x => x.OrderNo == orderNo); // có thể sai dòng
        ...
    }
}

@* ĐÚNG — truyền thẳng object đang render, không tra cứu lại *@
<MudIconButton OnClick="@(() => OpenDetailDialog(context))"/>
@code {
    private Task OpenDetailDialog(MyRowDto row) =>
        DialogService.ShowAsync<MyDetailDialog>("Chi tiết",
            new DialogParameters<MyDetailDialog> { { x => x.Item, row } });
}
```

- `context` trong `RowTemplate` **là chính object của dòng đó** — không có lý do gì phải tra cứu
  lại qua danh sách trang hiện tại bằng 1 khóa đơn (id/code) rồi mới mở dialog.
- Cách này còn loại bỏ luôn field phụ kiểu `_currentPageItems` (giữ list trang hiện tại chỉ để
  tra cứu lại) — không cần thiết khi đã có sẵn object.
- Tiền lệ cùng pattern: `OffersPage.razor` (`OpenDetailDialogAsync(context)`).

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/Transactions/MemberPointsPage.razor`.

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
