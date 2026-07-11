---
name: web-image-upload
description: Pattern upload ảnh trong POS.Web — lưu base64 nvarchar(max) thay vì varbinary, preview trong dialog trước khi lưu. Đọc khi cần lưu ảnh đại diện cho 1 entity.
---

# Pattern: Upload ảnh → base64 + preview trong dialog (không dùng varbinary)

> Áp dụng khi: cần lưu 1 ảnh nhỏ (≤2-5MB) đại diện cho 1 entity. Rút ra từ upload ảnh sản phẩm
> (`ProductDetailDialog`) — dự án dùng `nvarchar(max)` base64 cho ảnh (xem tiền lệ
> `dbo.TenderTypeImage.Image`), KHÔNG `varbinary`.

- `MudFileUpload T="IBrowserFile"` (`Accept=".jpg,.jpeg,.png"`, `MaximumFileCount="1"`) →
  `FilesChanged` validate đuôi file + `file.Size` **trước** khi đọc, không đợi Lưu mới báo lỗi.
- Đọc `file.OpenReadStream(maxAllowedSize).CopyToAsync(ms)` → `Convert.ToBase64String(ms.ToArray())`
  → build `data:{mime};base64,{...}` hiển thị `<MudImage>` preview **ngay trong dialog trước khi
  Lưu** (không cần round-trip DB để xem lại).
- **Không lưu thêm cột MIME type** — khi cần hiển thị lại ảnh đã lưu (base64 thuần, không prefix),
  suy đoán PNG/JPEG từ magic-byte prefix của base64: PNG luôn bắt đầu `"iVBORw0KGgo"`, còn lại mặc
  định JPEG. Đủ dùng cho 2 định dạng phổ biến, tránh migration thêm cột.
- Lưu ảnh là **thao tác phụ, tách khỏi transaction chính**: entity chính tạo/lưu thành công trước
  → gọi lưu ảnh sau; nếu lưu ảnh lỗi chỉ Snackbar cảnh báo + log, **không rollback** entity chính đã
  tạo (ảnh là optional, entity không phụ thuộc ảnh để tồn tại hợp lệ).

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/Product/Dialogs/ProductDetailDialog.razor`
> (upload), `Dialogs/ProductViewDialog.razor` (hiển thị lại); bảng `dbo.ProductImage`, SQL
> `docs/sql/ProductImage_Save.sql`.
