Khi có DTO mới, bạn chỉ cần nhắn 1 trong 3 cách:

Cách 1 — Cung cấp đường dẫn file (nhanh nhất):


Thêm DTO mới: POS.Backend/API_Common/Dtos/NewDomain/NewDto.cs
Cách 2 — Cung cấp tên class:


Thêm DTO mới: class NewOrderDto, domain Orders, thuộc API_Common
Cách 3 — Nhiều DTO cùng lúc:


Có DTO mới từ commit abc123, tìm và tạo hết vào src/POS.Common/