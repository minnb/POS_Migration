# /add-dto-common — Thêm DTO mới vào src/POS.Common/

Dùng lệnh này khi có class DTO/model mới được thêm vào source cũ (`POS.Backend/`) và cần tạo bản tương ứng trong `src/POS.Common/`.

---

## Cách dùng

**Cách 1 — Cung cấp đường dẫn file (nhanh nhất):**
```
/add-dto-common POS.Backend/API_Common/Dtos/NewDomain/NewDto.cs
```

**Cách 2 — Cung cấp tên class:**
```
/add-dto-common class NewOrderDto, domain Orders, thuộc API_Common
```

**Cách 3 — Nhiều DTO cùng lúc (từ commit hoặc ngày):**
```
/add-dto-common từ commit abc1234
/add-dto-common từ ngày 2026-06-01
```

---

## Quy trình Claude thực hiện

### Bước 1: Đọc file nguồn
Đọc file từ `POS.Backend/API_Common/Dtos/` hoặc `POS.Backend/API_BLUEPOS/Model/`.
- Nếu class trùng tên giữa API_Common và BLUEPOS → dùng tên API_Common, bỏ bản BLUEPOS.

### Bước 2: Xác định file đích

| Điều kiện | Hành động |
|---|---|
| Domain đã có file trong `src/POS.Common/Dtos/{Domain}/` | Thêm class vào file hiện có |
| Domain chưa có | Tạo file mới `{Domain}Dto.cs` |

### Bước 3: Áp dụng quy tắc bắt buộc
- Namespace: `POS.Common.Dtos.{Domain};` (file-scoped)
- `[JsonProperty("tên_gốc")]` từ `Newtonsoft.Json` — giữ nguyên tên JSON field
- KHÔNG dùng `System.Text.Json`; convert `[JsonPropertyName]` → `[JsonProperty]`, `JsonElement` → `object?`
- Nullable: `?` cho reference types, `= string.Empty` cho required non-null strings
- Giữ nguyên: computed properties, inheritance, DataAnnotations

### Bước 4: Xác nhận
Liệt kê class và file đã tạo/cập nhật, ghi chú nếu có quyết định merge.

---

## Mapping namespace (tham khảo nhanh)

| Cũ | Mới |
|---|---|
| `TCX.API.Common.Dtos.{X}` | `POS.Common.Dtos.{X}` |
| `VCM.POSBLUE.Model.{X}` | `POS.Common.Dtos.{X}` |
| `VCM.POSBLUE.Model.Dtos.{X}` | `POS.Common.Dtos.{X}` |
