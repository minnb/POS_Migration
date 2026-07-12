---
name: contract-test-guardian
description: Đảm bảo mọi DTO response trả về cho 5.000 máy POS đều được khóa field JSON trong tests/POS.ContractTests/JsonFieldContractTests.cs — audit DTO thiếu test, sinh đúng stanza AssertFields, và xử lý đúng quy trình khi cố ý đổi contract. Dùng khi tạo DTO response mới, sửa property của DTO đã có, hoặc trước khi commit thay đổi trong src/POS.Common/Dtos/.
---

# Contract Test Guardian — POS_Migration

Bảo vệ hợp đồng JSON quan trọng nhất của dự án: **tên field response mà 5.000 máy POS đang parse**.
Dùng skill này bất cứ khi nào tạo DTO response mới, sửa property của DTO đã tồn tại, hoặc trước khi
commit thay đổi trong `src/POS.Common/Dtos/`.

Cơ chế thật (đã đọc trực tiếp từ `tests/POS.ContractTests/JsonContract.cs` và
`JsonFieldContractTests.cs` — không suy diễn):

```csharp
// JsonContract.EffectiveFieldNames(type) — cách tính "tên field JSON hiệu dụng"
type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null)
    .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? p.Name)
```

→ Field bị `[JsonIgnore]` sẽ **không** xuất hiện trong danh sách. Field có `[JsonProperty("X")]` thì
dùng tên `"X"`, không dùng tên property C#. Thứ tự không quan trọng (assertion tự sort 2 phía).

## Bước 1 — Xác định DTO nào cần khóa

Không phải DTO nào cũng cần test — chỉ DTO **thật sự đi ra ngoài** tới POS client (trực tiếp là return
type của Controller action, hoặc lồng trong `ResultResponse.Data`, `List<T>`, `Tuple<..., List<T>>`).
DTO nội bộ thuần túy (request-only, hoặc dùng nội bộ giữa Repository/Service) không cần khóa.

```bash
# Kiểm tra 1 DTO cụ thể có được Controller nào trả về không
grep -rn "{DtoName}" src/POS.Api/Controllers/
```

## Bước 2 — Kiểm tra DTO đã được khóa test chưa

```bash
grep -n "typeof({DtoName})" tests/POS.ContractTests/JsonFieldContractTests.cs
```

- Có kết quả → đã khóa, chuyển sang Bước 5 (kiểm tra nếu bạn vừa ĐỔI field) thay vì tạo test mới.
- Không có kết quả → cần thêm test mới (Bước 3–4).

## Bước 3 — Trích danh sách field JSON hiệu dụng của DTO (thủ công, chính xác)

Mở file DTO (`src/POS.Common/Dtos/{Domain}/{DtoName}.cs`) và với **mỗi public property**:
1. Nếu có `[JsonIgnore]` ngay phía trên → **bỏ qua**, không đưa vào danh sách.
2. Nếu có `[JsonProperty("X")]` → dùng đúng chuỗi `"X"` (không dùng tên property C#).
3. Ngược lại → dùng nguyên tên property C#.

Không đoán field — đọc trực tiếp file, vì đây là hợp đồng production, sai 1 ký tự là vỡ 5.000 máy POS.

Có thể quét nhanh bằng bash để không bỏ sót property nào (chỉ để đối chiếu, vẫn phải tự đọc attribute):

```bash
grep -nP '^\s*public\s+\S+\??\s+\w+\s*\{' src/POS.Common/Dtos/{Domain}/{DtoName}.cs
```

## Bước 4 — Sinh test stanza

Thêm vào cuối `tests/POS.ContractTests/JsonFieldContractTests.cs` (thêm `using` namespace của DTO ở
đầu file nếu chưa có):

```csharp
[Fact]
public void {DtoName}_locked()
    => AssertFields(typeof({DtoName}),
        "Field1", "Field2", "Field3");
```

Tên method test: `{DtoName}_locked` — đúng convention 4 test hiện có (`ResultResponse_envelope_locked`,
`InfoMemberModel_locked`, `PaymentEntryLoyalty_locked`, `GiftDataRespone_locked`).

## Bước 5 — Khi CỐ Ý đổi field của DTO đã khóa

Nếu bạn chủ động rename/thêm/xóa field của DTO **đã có** entry trong `JsonFieldContractTests.cs`:
1. Cập nhật đúng danh sách `AssertFields(...)` **trong cùng commit** với thay đổi DTO — đây là dấu vết
   bắt buộc cho thấy thay đổi contract là có chủ đích (theo đúng docstring đầu file test).
2. Nêu rõ lý do đổi field trong commit message (xem skill `git-workflow`).
3. Nếu KHÔNG chủ động đổi mà test đỏ → đây là bug thật, sửa code để field JSON không đổi, không sửa test.

## Bước 6 — Audit coverage gap (dùng khi rà soát định kỳ, không phải mỗi lần thêm 1 DTO)

Script heuristic để tìm DTO có khả năng đang được Controller trả về nhưng **chưa** có entry khóa test
— đây là quét bằng grep nên có thể có false positive/negative, luôn xác nhận thủ công trước khi thêm:

```bash
# Danh sách DTO đã được khóa test
grep -oP 'typeof\(\K[A-Za-z0-9_]+' tests/POS.ContractTests/JsonFieldContractTests.cs | sort -u > /tmp/locked.txt

# Danh sách kiểu dữ liệu xuất hiện trong OkResult(...) / ActionResult<T> / StatusCode(..., new ResultResponse) ở Controllers
grep -rhoP '(?<=OkResult\()[A-Za-z0-9_]+|(?<=ActionResult<)[A-Za-z0-9_]+' src/POS.Api/Controllers/*.cs \
  | sort -u > /tmp/exposed_candidates.txt

comm -23 /tmp/exposed_candidates.txt /tmp/locked.txt
```

Với mỗi kết quả in ra: mở Controller để xác nhận đây thật sự là 1 DTO response (không phải biến cục bộ
hay kiểu built-in như `int`/`string`), rồi làm lại từ Bước 1.

## Bước 7 — Verify trước khi commit

```bash
dotnet test tests/POS.ContractTests --filter FullyQualifiedName~JsonFieldContractTests
```

Xanh → an toàn để commit (kết hợp với skill `git-workflow` để chạy full `dotnet test tests/POS.ContractTests`
trước khi commit, vì còn 2 vành đai bảo vệ khác — DI test và Exception middleware test — không thuộc
phạm vi skill này).

## Checklist nhanh

```
□ DTO có thật sự đi ra POS client không? (Bước 1) — nếu không, KHÔNG cần khóa test
□ Đã kiểm tra chưa có entry trùng trong JsonFieldContractTests.cs (Bước 2)
□ Field list lấy từ đọc trực tiếp source DTO, tôn trọng [JsonProperty]/[JsonIgnore] (Bước 3)
□ Test method đặt tên {DtoName}_locked, đúng convention (Bước 4)
□ Nếu đổi field DTO đã khóa → cập nhật AssertFields cùng commit + giải thích lý do (Bước 5)
□ dotnet test tests/POS.ContractTests --filter FullyQualifiedName~JsonFieldContractTests xanh (Bước 7)
```
