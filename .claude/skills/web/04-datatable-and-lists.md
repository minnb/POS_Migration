# Skill: DataTable & Danh sách POS.Web (MudBlazor 9.5.0)

> **Đọc file này khi:** tạo/sửa bất kỳ danh sách dữ liệu, bảng biểu, filter panel gắn kèm bảng trong
> `src/POS.Web/` — đây là bản "hiến pháp" rút gọn LUẬT BẮT BUỘC cho DataTable & Lists.
>
> **Quan hệ với các file khác** (file này là index luật, chi tiết + code mẫu ở nơi được trỏ):
> - Layout/Elevation/Button convention/KPI card nền tảng: **`02-ui-ux-and-components.md`** +
>   **`.claude/rules/mudblazor-flat-ui.md`** (nguồn sự thật Elevation/màu — **KHÔNG lặp giá trị ở đây**).
> - CancellationToken/error handling cho hàm fetch data: **`03-integration-and-performance.md`**.
> - Pattern MudTable đầy đủ (client/server/cột động/footer tổng): **`datatable.md`**.
> - Store selector filter: **`filter-store.md`**.
>
> ⚠️ **Elevation bảng = `"2"` (shadow thật), KHÔNG phải `"0"`+Outlined** — kiểu flat+viền là v2 đã bị
> thay thế bởi v3 (xem `mudblazor-flat-ui.md` §1 + "Đã cân nhắc và loại bỏ"). File này tuân theo v3.
> Đặt `Elevation="2"` **trực tiếp trên `<MudTable>`** (đã rollout thật, vd `TransactionsPage.razor`) —
> KHÔNG bọc thêm `MudPaper` ngoài chỉ để mang Elevation.
>
> Khi luật ở đây và file chi tiết lệch nhau → file chi tiết thắng; sửa lại file này cho khớp trong cùng commit.

---

## 1. Lựa chọn Component — MudTable vs MudDataGrid

- **`MudTable`** là component **mặc định BẮT BUỘC** cho danh sách dữ liệu — Master Data, báo cáo cơ
  bản, lịch sử giao dịch. Toàn bộ dự án hiện tại (100% các trang danh sách) dùng `MudTable`.
- **`MudDataGrid`** chỉ cân nhắc khi nghiệp vụ thật sự cần tính năng client-side phức tạp mà người
  dùng thao tác trực tiếp trên lưới (Grouping động nhiều cột, Advanced Filter cấp user tự cấu hình)
  — **KHÔNG** dùng chỉ vì "trông hiện đại hơn". Dự án hiện **chưa có trang nào** dùng `MudDataGrid`;
  nếu đây là lần đầu, xác nhận lại nhu cầu nghiệp vụ trước khi phá vỡ nhất quán 100% `MudTable`.

## 2. Chuẩn UI/UX thiết kế bảng

- **`Elevation="2"` đặt TRỰC TIẾP trên `<MudTable>`** — theo `mudblazor-flat-ui.md` §1 (card có
  shadow thật) + pattern đã rollout thật (`datatable.md`, vd `TransactionsPage.razor:131`).
  **KHÔNG** bọc thêm `MudPaper` ngoài `MudTable` chỉ để đặt Elevation — `MudTable` tự render container
  riêng, bọc thêm `MudPaper` tạo lồng 2 lớp elevation thừa. KHÔNG thêm `Outlined`/viền thủ công,
  KHÔNG hạ `Elevation="0"`.
- **Thuộc tính bắt buộc trên `<MudTable>`:** `Hover="true"` `Striped="true"` `Dense="true"`
  `HorizontalScrollbar="true"` `Elevation="2"` (Density Standard + chống clip mobile — xem
  `02-ui-ux-and-components.md` §3).
- **Typography header/cell:** đã chuẩn hóa toàn cục trong `app.css`/`PosTheme.cs` — header uppercase/
  muted, cell `0.78125rem` (12.5px). **Không cần tự set** khi tạo bảng mới, chỉ set khi có nhu cầu
  lệch chuẩn thật sự (xem `mudblazor-flat-ui.md` mục 4 + mục 11).
- **Empty state — BẮT BUỘC cấu hình `<NoRecordsContent>`**, KHÔNG để bảng trống trơn không thông báo.
  Dự án đã có ~15 trang dùng `<NoRecordsContent>` nhưng phần lớn chỉ có text màu hardcode
  (`Style="color:#9e9e9e"`) — dùng đúng pattern đã chốt ở `ui-polish-standard.md` §3 (icon +
  `var(--mud-palette-text-secondary)`, KHÔNG hex cứng):
  ```razor
  <NoRecordsContent>
      <div class="d-flex flex-column align-center pa-4" style="color:var(--mud-palette-text-secondary)">
          <MudIcon Icon="@Icons.Material.Filled.Inbox" Size="Size.Large" Class="mb-2"/>
          <MudText Typo="Typo.body2">Không có dữ liệu. Hãy điều chỉnh bộ lọc và thử lại.</MudText>
      </div>
  </NoRecordsContent>
  ```

## 3. Khu vực bộ lọc & tìm kiếm (`pos-filter-panel`)

- Search box/date picker/dropdown lọc nhóm chung trong `MudPaper Elevation="1"
  Class="pos-filter-panel pa-4 mb-4"` (nền trắng + border, class đã có sẵn trong `app.css`), đặt
  ngay phía trên bảng — trùng khớp `02-ui-ux-and-components.md` §1, không định nghĩa lại ở đây.
- **Debounce ô tìm kiếm text tự do** (keyword/barcode): BẮT BUỘC `Immediate="true"
  DebounceInterval="500"` trên `MudTextField` — đã là pattern phổ biến trong dự án (26 file đang
  dùng), giữ nguyên chuẩn này cho mọi ô search mới.
  ```razor
  <MudTextField @bind-Value="_keyword" Label="Tìm kiếm" Variant="Variant.Outlined" Margin="Margin.Dense"
                Immediate="true" DebounceInterval="500" ValueChanged="@(async (string v) => { _keyword = v; await SearchAsync(); })"
                Adornment="Adornment.Start" AdornmentIcon="@Icons.Material.Filled.Search"/>
  ```
- **Nút thao tác** (theo Button convention `mudblazor-flat-ui.md` §3, áp dụng cho filter panel):
  - Tìm/Áp dụng bộ lọc/Thêm mới → `Variant="Variant.Filled" Color="Color.Primary"`.
  - Xóa bộ lọc/Export Excel → `Variant="Variant.Outlined"` (không đặt `Color` — trung tính).

## 4. Tối ưu hiệu năng & phân trang (Server-side)

- **BẮT BUỘC `ServerData`** (trả `TableData<T>`) cho mọi danh sách có khả năng vượt 100 dòng —
  pattern đã dùng ở 21 trang trong dự án. **TUYỆT ĐỐI KHÔNG** load toàn bộ bảng về client
  (`.ToListAsync()` không giới hạn) rồi tự phân trang bằng UI.
  ```csharp
  private async Task<TableData<MyDto>> ServerReload(TableState state, CancellationToken ct)
  {
      var (items, total) = await MyRepo.GetPagedAsync(_keyword, state.Page, state.PageSize, ct);
      return new TableData<MyDto> { Items = items, TotalItems = total };
  }
  ```
- **`CancellationToken` xuyên suốt**: hàm bind vào `ServerData` nhận `CancellationToken` do
  `MudTable` tự cấp và truyền **tận xuống Repository** (Dapper/EF) — hủy query SQL khi user chuyển
  trang giữa chừng. Chi tiết + rationale đầy đủ: `03-integration-and-performance.md` §5.
- **Loading indicator**: gắn `Loading="@_loading"` trên `<MudTable>` để tự hiện `MudProgressLinear`
  ở header khi fetch — tránh UI "đơ" không phản hồi khi chờ data.

## 5. Thao tác trên dòng (Row Actions)

- Cột thao tác (Xem/Sửa/Xóa/Phân quyền) luôn là **cột cuối cùng**, căn giữa
  (`<MudTd DataLabel="Thao tác" Style="text-align:center">`).
- Ưu tiên `MudIconButton Size="Size.Small"` thay vì nút chữ để tiết kiệm không gian. **Mọi icon
  button BẮT BUỘC bọc trong `MudTooltip`** giải thích chức năng — KHÔNG chỉ dùng thuộc tính
  `Title="..."` (native HTML tooltip, đã dùng ở một số page cũ nhưng không còn là chuẩn từ nay).
  ```razor
  <MudTd DataLabel="Thao tác" Style="text-align:center">
      <MudTooltip Text="Chỉnh sửa">
          <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small" Color="Color.Primary"
                         OnClick="@(() => EditAsync(context))"/>
      </MudTooltip>
      <MudTooltip Text="Xóa">
          <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error"
                         OnClick="@(() => DeleteAsync(context))"/>
      </MudTooltip>
  </MudTd>
  ```
- **Màu sắc ngữ nghĩa** (áp Button convention `mudblazor-flat-ui.md` §3 xuống icon button):
  - An toàn (Xem/Chi tiết) → `Color.Default` hoặc `Color.Info`.
  - Hành động chính (Sửa/Cập nhật) → `Color.Primary`.
  - Nguy hiểm (Xóa/Deactive) → `Color.Error`, **luôn** đi kèm `MudMessageBox @ref` xác nhận trước
    khi thực thi — xem pattern `SKILLS.md` §"MudMessageBox @ref", KHÔNG dùng
    `DialogService.ShowAsync<MudMessageBox>`.
- **Ngăn Event Bubbling**: nếu bảng dùng `OnRowClick` (chọn cả dòng), mọi nút thao tác bên trong
  cell BẮT BUỘC gắn `@onclick:stopPropagation="true"` — tránh kích hoạt nhầm sự kiện click dòng.
  ```razor
  <MudIconButton ... @onclick:stopPropagation="true" OnClick="@(() => EditAsync(context))"/>
  ```

---

## Checklist nhanh trước khi báo "xong" (DataTable & Lists)

```
□ Dùng MudTable (mặc định) — chỉ chuyển MudDataGrid khi có lý do nghiệp vụ rõ ràng
□ Elevation="2" đặt trực tiếp trên MudTable — KHÔNG bọc thêm MudPaper, KHÔNG Elevation="0"/Outlined
□ MudTable: Hover/Striped/Dense/HorizontalScrollbar="true"
□ <NoRecordsContent> theo pattern ui-polish-standard.md §3 (icon Filled.Inbox + var(--mud-palette-text-secondary)) — không để bảng trống trơn
□ Filter panel bọc MudPaper Elevation="1" pos-filter-panel, đặt trên bảng
□ Ô search text tự do có Immediate="true" DebounceInterval="500"
□ Nút Tìm/Thêm = Filled/Primary; Xóa lọc/Export = Outlined trung tính
□ Danh sách >100 dòng → ServerData + CancellationToken truyền tới Repository
□ MudTable có Loading="@_loading"
□ Cột thao tác ở cuối, căn giữa; MudIconButton Size.Small bọc MudTooltip (không chỉ Title=)
□ Màu icon button theo ngữ nghĩa; Xóa/Deactive luôn có MudMessageBox xác nhận
□ OnRowClick + nút trong cell → @onclick:stopPropagation="true"
```
