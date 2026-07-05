Phân tích đầy đủ chức năng "<TênChứcNăng>" trong dự án cũ src/legacy/

Bước 1: ĐỌC SOURCE CŨ, PHÂN TÍCH VÀ LẬP BÁO CÁO
Hãy tìm kiếm và đọc các file mã nguồn liên quan đến chức năng trên ở dự án MVC cũ (bao gồm Controllers, Views, Models/ViewModels, BLO, DAL). 
Sau đó, xuất ra một báo cáo phân tích chi tiết với cấu trúc sau:

			-Mô tả luồng nghiệp vụ (Business Logic): Chức năng này giải quyết bài toán gì? Luồng đi từ UI -> Controller -> BLO -> DAL như thế nào? Liệt kê các rule validate, điều kiện logic hoặc các API bên thứ 3 được gọi (nếu có).
			-Phân tích Database & Data Access:
			-Các Stored Procedure (SP) hoặc câu truy vấn Entity Framework nào đang được sử dụng? CẦN LIỆT KÊ RA ĐỂ TÔI BỔ SUNG sang database RPOSMasterData (CentralMD)
			-Tác động lên các Table/View nào trong cơ sở dữ liệu?
			-Cấu trúc source code legacy: Liệt kê tên file Controller, Action, ViewModel, và class DAL/BLO đang đảm nhận chức năng này.
			-Đề án thiết kế cho Blazor Server (.NET Core):
			-Các DTOs / Request / Response Models cần tạo mới.
			-Đề xuất cấu trúc Interface/Service để thay thế cho BLO/DAL cũ.
			-Đề xuất file UI Component (.razor) và dự kiến cách quản lý state (ví dụ: dùng EditForm, data binding...).	Lưu ý là UI phải theo chuẩn dự án hiện tại.
					
BƯỚC 2: DỪNG LẠI VÀ CHỜ PHÊ DUYỆT
			Sau khi in ra báo cáo ở Bước 1, bạn PHẢI DỪNG LẠI và hỏi tôi: "Báo cáo phân tích đã hoàn tất. 
			Bạn có muốn điều chỉnh gì không, hay gõ 'OK' để tôi bắt đầu viết code cho dự án Blazor?".
			Chỉ khi tôi trả lời "OK" hoặc xác nhận đồng ý, bạn mới được phép chuyển sang Bước 3 là implement code thực tế (viết file .razor, Services, DTOs...).
			Đã rõ yêu cầu chưa? Nếu rồi, hãy bắt đầu thực hiện.
			
Bước 3: Xuất ra FEATURE_<TênChứcNăng>_ANALYSIS.md gồm:

- Sơ đồ luồng dạng bullet (từ request tới response).
- Danh sách business rule đánh số, mỗi rule kèm trích dẫn nguồn.
- Nói thẳng nếu yêu cầu có vấn đề hoặc có cách làm tốt hơn. Không chắc thì HỎI thay vì đoán.

Tuyệt đối không suy diễn logic không có trong code cũ.