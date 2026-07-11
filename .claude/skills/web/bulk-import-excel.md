---
name: web-bulk-import-excel
description: Pattern bulk import Excel trong POS.Web — lưới preview validate qua TVP + sửa lỗi inline trước khi lưu. Đọc khi tạo page nhập liệu hàng loạt từ file Excel.
---

# Pattern: Bulk import Excel → lưới preview validate + sửa inline

> Áp dụng khi: page nhập liệu hàng loạt từ Excel cần validate DB (item/uom/barcode tồn tại...) rồi cho
> user sửa lỗi trước khi lưu. Rút ra từ 9.3 Setup giá (`PriceSetupPage`).

- Upload `MudFileUpload T="IBrowserFile"` + nút "Nạp" riêng (KHÔNG auto-validate lúc chọn file) → đọc bằng
  **ClosedXML** (`XLWorkbook`, bỏ header dòng 1, `ws.Row(r).IsEmpty()` để skip). Ngày: check
  `cell.DataType == XLDataType.DateTime` trước khi `GetString()`.
- Validate DB qua service→repo **TVP** (không temp-table/SqlBulkCopy): `DataTable.AsTableValuedParameter("dbo.XxxTVP")`
  chạy query `LEFT JOIN` inline → trả từng dòng kèm `ErrorMessage` (rỗng = hợp lệ).
- Lưới sửa dùng `MudTable Items` + view-model có cờ `HasError`; **`RowStyleFunc`** tô nền dòng lỗi
  (`background-color:#fdecea`); ô Giá/Ngày là `MudTextField`/`MudDatePicker` bind thẳng row.
- **Chặn Lưu khi còn dòng lỗi** (`_errorCount > 0` → Snackbar warning); chip đếm Tổng/Lỗi trên toolbar.
- Save chạy validate nghiệp vụ lần cuối ở Application service (port 100% điều kiện legacy) → SP TVP; audit log sau khi Ok.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/Price/PriceSetupPage.razor` +
> `Dialogs/PriceItemPickerDialog.razor`; repo `src/POS.Infrastructure/Repositories/Price/PriceRepository.cs`;
> SQL `docs/sql/SetupSalePrice_Save.sql` (TVP validate + TVP save).
