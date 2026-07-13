---
name: web-component-patterns
description: Code pattern thực thi cho POS.Web — load nhiều nguồn độc lập (tách try/catch), modal nhiều tab lazy-load, truyền row object vào dialog, MudTreeView lazy-load. Rules nền tảng ở .claude/rules/blazor-web-app.md.
---

# Skill: Component Patterns POS.Web (HOW)

> **Đọc file này khi:** cần code 1 trong các pattern component dưới đây. Đây là các **kỹ năng thực
> thi** (code mẫu) — luật nền tảng (lifecycle 3-state, try/catch circuit, DI, performance
> `@key`/`IAsyncDisposable`/`CancellationToken`) là **Rules** ở
> **`.claude/rules/blazor-web-app.md`** (§5, §17.1).

---

## 1. Load nhiều nguồn độc lập trong `OnInitializedAsync` — tránh crash circuit

> Áp dụng khi: page/dialog load ≥2 nguồn dữ liệu ĐỘC LẬP (list chính + dropdown/lookup, hoặc ≥2
> dropdown không liên quan) lúc khởi tạo.
> Rút ra từ sự cố lặp lại **5 lần** (`BankPosPage`, `BankPosDetailDialog`, `ProductDetailDialog`,
> `SpecialComboPage`, `PromotionSetupPage`): nhiều `await` (song song `Task.WhenAll` HAY tuần tự
> — **cả hai lỗi y hệt**) trong 1 `try/catch` DUY NHẤT. 1 nguồn lỗi (SP/bảng thiếu ở DEV) → các
> dòng SAU nó không chạy → dropdown trống dù nguồn đó lẽ ra load được. Exception chưa bắt trong
> lifecycle method còn **sập luôn circuit** Blazor Server.

**Nhận diện "độc lập" (tách try/catch) vs "cùng 1 báo cáo" (chung 1 catch):**

| Tình huống | Độc lập hay cùng báo cáo? | Xử lý |
|---|---|---|
| `_articleTypes` + `_unitOfMeasures` + `_vatCodes` cho 3 dropdown KHÁC NHAU | Độc lập | Tách 3 try/catch |
| `_salesTypes` + `_memberCodes` + `_allStores` cho 3 filter/dropdown KHÁC NHAU | Độc lập | Tách 3 try/catch |
| Summary + Detail list của CÙNG 1 domain | Cùng báo cáo | 1 try/catch OK |
| Kỳ hiện tại + kỳ so sánh của CÙNG 1 metric | Cùng báo cáo | 1 try/catch OK |
| Order lines + payment entries của CÙNG 1 đơn hàng | Cùng báo cáo | 1 try/catch OK |

Quy tắc nhanh: 2 nguồn từ 2 **domain nghiệp vụ khác nhau** feed vào 2 **control UI khác nhau** →
tách. 2 GÓC NHÌN của CÙNG 1 dữ liệu (chi tiết vs tổng hợp, kỳ này vs kỳ trước) → gộp 1 catch.

```csharp
protected override async Task OnInitializedAsync()
{
    // Await + try/catch RIÊNG từng nguồn ĐỘC LẬP — 1 nguồn lỗi không kéo sập nguồn khác,
    // và không để exception thoát khỏi OnInitializedAsync (circuit crash).
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

- Áp dụng cho CẢ page lẫn dialog con (`MudDialog` — dễ bị bỏ sót vì hay copy pattern gọn 1 try).
- KHÔNG `await Task.WhenAll(taskA, taskB)` rồi try/catch NGOÀI `WhenAll` nếu muốn 1 nguồn lỗi không
  ảnh hưởng nguồn còn lại — `WhenAll` ném exception task đầu fail, `.Result` các task khác không
  được gán vì nằm sau dòng `await` đã throw.
- Rà soát code cũ: search cả `OnInitializedAsync` có ≥2 dòng `await Repo.GetXxxAsync()` tuần tự
  trong 1 try, feed vào ≥2 field khác nhau — không chỉ nhìn `Task.WhenAll`.

> Ví dụ thực tế: `BankPosPage.razor`, `BankPosDetailDialog.razor`, `ProductDetailDialog.razor`,
> `SpecialComboPage.razor`, `PromotionSetupPage.razor`.

---

## 2. Modal chi tiết nhiều tab — lazy-load theo tab active (miễn phí)

> Áp dụng khi: dialog nhiều tab (`MudTabs`/`MudTabPanel`), mỗi tab tự load dữ liệu riêng, muốn
> tránh gọi hết N service cùng lúc lúc mở modal.

`MudTabs` mặc định **không giữ panel không active trong DOM** (không `KeepPanelsAlive="true"`) →
nội dung mỗi `MudTabPanel` (kể cả `MudTable ServerData`) chỉ render lần đầu khi tab được kích hoạt,
`ServerData` chỉ gọi tại thời điểm đó. → **Không cần** tự viết `HashSet<int> _loadedTabs` hay
`OnActivePanelIndexChanged` — đặt thẳng `MudTable ServerData="LoadXxxAsync"` vào từng `MudTabPanel`.

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

- Tab đầu (mặc định active) vẫn load ngay — nếu là 1-record đơn giản (không `MudTable`), gọi thẳng
  trong `OnInitializedAsync`.
- Quay lại tab đã xem sẽ gọi lại `ServerData` (không cache) — chấp nhận với modal read-only; cần
  cache thì tự thêm field lưu kết quả.

> Ví dụ thực tế: `Promotion/Offers/Dialogs/OfferDetailDialog.razor` (6 tab).

---

## 3. Truyền thẳng row object vào dialog chi tiết — KHÔNG tra cứu lại theo key đơn

> Áp dụng khi: nút "Xem chi tiết" trong `RowTemplate` của `MudTable` mở dialog cho đúng dòng click.
> Bug thực tế: `MemberPointsPage` tra lại dòng bằng `_currentPageItems.FirstOrDefault(x => x.OrderNo
> == invoiceNo)` — `OrderNo` không unique (PK composite) → luôn trả dòng đầu tiên trùng, mở nhầm.

```razor
@* SAI — tra cứu lại theo 1 cột không đảm bảo unique *@
<MudIconButton OnClick="@(() => OpenDetailDialog(context.OrderNo))"/>

@* ĐÚNG — truyền thẳng object đang render *@
<MudIconButton OnClick="@(() => OpenDetailDialog(context))"/>
@code {
    private Task OpenDetailDialog(MyRowDto row) =>
        DialogService.ShowAsync<MyDetailDialog>("Chi tiết",
            new DialogParameters<MyDetailDialog> { { x => x.Item, row } });
}
```

- `context` trong `RowTemplate` **là chính object của dòng đó** — không tra cứu lại qua list trang
  hiện tại bằng 1 khóa đơn.
- Loại bỏ luôn field phụ `_currentPageItems` (giữ chỉ để tra cứu lại).

> Ví dụ thực tế: `MemberPointsPage.razor`, tiền lệ `OffersPage.razor` (`OpenDetailDialogAsync(context)`).

---

## 4. MudTreeView lazy-load (`ServerData`) — không đệ quy toàn cây

> Áp dụng khi: cây **thật sự sâu/nhiều nhánh** (nhiều cấp, hàng trăm+ node) mà đệ quy load 1 lần sẽ
> tốn kém — và người dùng cần nhìn thấy toàn bộ cấu trúc phân cấp cùng lúc (nhiều nhánh mở song
> song). **KHÔNG** dùng cho hierarchy nông (2-3 cấp cố định, mục đích chỉ là "duyệt xuống 1 nhánh
> tại 1 thời điểm") — xem mục 4b bên dưới, đơn giản hơn nhiều và không có lớp bug expand-vs-select.

`ServerData` nhận **`Value` (kiểu `T`) của node cha**, KHÔNG phải `TreeItemData<T>?` — gọi khi user
bấm expand 1 node **đã tồn tại**, KHÔNG tự gọi cho top-level. Top-level nạp qua `Items` (1 lần, 1
cấp). `Items` + `ServerData` dùng **song song**, không phải chọn 1.

```razor
<MudTreeView T="string" Items="_rootItems" ServerData="LoadChildrenAsync"
             SelectedValue="_selected" SelectedValueChanged="OnSelectedAsync"
             SelectionMode="SelectionMode.SingleSelection" Hover="true" Dense="true" ExpandOnClick="true"/>
```
```csharp
private async Task<IReadOnlyCollection<TreeItemData<string>>> LoadChildrenAsync(string parentValue)
    => (await Repo.GetSubItemsAsync(parentValue)).Select(x => new TreeItemData<string>
    {
        Text = x.Name, Value = x.Path, Icon = Icons.Material.Filled.Folder,
        Expandable = true   // KHÔNG gán HasChildren — computed read-only (Children?.Count > 0), CS0200
    }).ToList();
```

> ⚠️ **`ExpandOnClick="true"` vs `SelectedValueChanged`**: nếu bật `ExpandOnClick` VÀ cần phân biệt
> "click để chọn" khác "click để mở rộng", click thân node bị nuốt thành hành động expand, KHÔNG
> fire `SelectedValueChanged` — user thấy như click không phản ứng. Nếu chỉ cần 1 hành động (không
> phân biệt select/expand), giữ `ExpandOnClick="true"` là đủ và không gặp bug này.
>
> **2026-07-13 — KHÔNG còn ví dụ thực tế nào trong repo dùng pattern này**: `Admin/LogFilePage.razor`
> (ví dụ gốc của pattern) đã đổi hẳn sang breadcrumb + drill-down (mục 4b) vì hierarchy log server
> chỉ nông 2-3 cấp cố định — dùng MudTreeView cho trường hợp này là over-engineering, tự tạo bug
> expand-vs-select không cần thiết. Giữ pattern này lại làm tài liệu tham khảo cho use-case cây THẬT
> SỰ sâu/nhiều nhánh trong tương lai — không xóa, nhưng ưu tiên xét mục 4b trước khi chọn TreeView.

## 4b. Breadcrumb + drill-down list — duyệt hierarchy nông (2-3 cấp cố định)

> Áp dụng khi: duyệt cấu trúc phân cấp NÔNG (thư mục log, danh mục 2-3 cấp...) mà tại 1 thời điểm
> chỉ cần xem 1 nhánh (không cần nhìn nhiều nhánh mở song song như tree). Đơn giản hơn MudTreeView —
> chỉ 1 hành động "click để đi xuống", không có khái niệm expand tách biệt select nên không có lớp
> bug ở mục 4 phía trên. Không dùng `MudBreadcrumbs` (component đó thiết kế cho `Href`/URL
> navigation thật) — breadcrumb thủ công bằng `MudLink` đổi state nội bộ trong cùng 1 trang.

```razor
<MudPaper Elevation="1" Class="pa-3 mb-4 d-flex align-center flex-wrap gap-1">
    <MudLink OnClick="NavigateToRootAsync" Style="cursor:pointer">Gốc</MudLink>
    @for (var i = 0; i < _segments.Count; i++)
    {
        var idx = i; // capture đúng biến vòng lặp cho closure OnClick
        <MudIcon Icon="@Icons.Material.Filled.ChevronRight" Size="Size.Small"/>
        <MudLink OnClick="@(() => NavigateToBreadcrumbAsync(idx))" Style="cursor:pointer">@_segments[idx]</MudLink>
    }
</MudPaper>

<MudList T="string" Clickable="true">
    @foreach (var folder in _folders)
    {
        <MudListItem T="string" @key="folder.RelativePath" Icon="@Icons.Material.Filled.Folder"
                     OnClick="@(() => NavigateToFolderAsync(folder))">@folder.Name</MudListItem>
    }
</MudList>
```
```csharp
private string _currentPath = "";
private IReadOnlyList<string> _segments => string.IsNullOrEmpty(_currentPath) ? [] : _currentPath.Split('/');

private async Task LoadAsync(string path)
{
    var result = await Repo.GetDirectoryListingAsync(path); // trả CẢ folders+files của ĐÚNG 1 cấp path này
    _currentPath = result.CurrentRelativePath;
    _folders = result.Folders;
}

private Task NavigateToFolderAsync(FolderInfo folder) => LoadAsync(folder.RelativePath);
private Task NavigateToBreadcrumbAsync(int index) => LoadAsync(string.Join('/', _segments.Take(index + 1)));
private Task NavigateToRootAsync() => LoadAsync("");
```

> Ví dụ thực tế: `Admin/LogFilePage.razor` + `Services/{ILogFileService,LogFileService}.cs`
> (`GetDirectoryListingAsync(relativePath)` trả folders+files của đúng 1 cấp — không cần API "get
> subfolders" riêng, tái dùng thẳng cho cả tree lẫn drill-down).
