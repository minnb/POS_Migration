# Copilot Instructions — API_BLUEPOS

## Quy tắc bắt buộc

**KHÔNG được chỉnh sửa bất kỳ file code nào trong dự án này.**

Copilot chỉ được phép:
- Đọc và phân tích code
- Trả lời câu hỏi về code
- Đưa ra gợi ý/báo cáo dạng văn bản

Copilot **KHÔNG** được phép:
- Tạo file mới
- Chỉnh sửa file hiện có
- Xóa file
- Thực thi lệnh thay đổi codebase

## Bối cảnh dự án

- **Framework:** ASP.NET Web API 2, .NET 4.6.2, IIS
- **Mục đích:** API trung gian cho hệ thống loyalty POS (10.000 máy POS)
- **External systems:** Capillary (loyalty), VINID/VinPay, CrowX (WinLife), WinCustomer, RabbitMQ, Redis Sentinel
- **Hot routes:** `api/v2/loyalty/customer/get`, `api/v2/loyalty/transaction/add`