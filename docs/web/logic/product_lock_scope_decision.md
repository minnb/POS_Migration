# Quyết định phạm vi — "Khóa sản phẩm" (`catalog/product-lock`)

> Ghi lại 2026-07-06 sau Gap Analysis đối chiếu legacy `ProductController.ProductLock`
> (`src/legacy/VCM.BLUEPOS/Controllers/ProductController.cs:342-1225`) với
> `ProductLockPage.razor`. Mục đích: xác nhận 2 khoảng trống lớn nhất phát hiện được là **quyết
> định có chủ đích, không phải thiếu sót** — tránh lần sau ai đó đọc code cũ rồi tưởng cần port lại.

## 1. Không port tích hợp GrabFood API

Legacy sau mỗi lần khóa/mở khóa gọi song song REST API GrabFood
(`SetupLockItemByGrabFoodAPIV3`/`SetupActiveItemByGrabFoodAPIV3`,
`ProductController.cs:684-942`) để đồng bộ trạng thái món trên kênh bán GrabFood theo thời gian
thực.

**Quyết định (business xác nhận)**: tính năng ở bản mới không phải "khóa món" đồng bộ đa kênh bán
kiểu GrabFood, mà là **"Block sản phẩm"** — đơn thuần ngừng bán 1 sản phẩm tại cửa hàng (khóa
`dbo.ItemBlock`). Không cần đồng bộ realtime ra kênh bán ngoài nào. **Không port** tích hợp
GrabFood.

## 2. Không port chế độ ghi trực tiếp CSDL máy POS (POS-direct-write)

Legacy có "Loại kết nối" = `POS`, ghi trực tiếp xuống CSDL cục bộ máy POS
(`StorePLH`) qua kết nối SQL tới IP terminal (`ProductData.POS_CreateProductLockOrUnlock`,
`ProductData.cs:1757-1900`), song song với ghi Central, để khóa có hiệu lực ngay lập tức tại quầy
mà không cần đợi đồng bộ.

**Quyết định (business xác nhận)**: cơ chế Sync Master Data theo lịch hiện có
(`IMasterDataSyncService`, xem CLAUDE.md mục "Sinh file master data .zip cho POS") đã đủ nhanh để
phản ánh thay đổi `dbo.ItemBlock` xuống máy POS. **Không cần** port chế độ ghi trực tiếp máy POS,
`PingIP`, dropdown "Loại kết nối"/"Vùng Set"/"Máy POS", lưới 2-panel cửa hàng, hay phân quyền
theo role kiểu legacy (`R0007` vs `R0000/R0001/R0002/R0009`) gắn liền với 2 chế độ đó.

## Hệ quả

`ProductLockPage.razor` giữ nguyên thiết kế hiện tại: 1 chế độ Central duy nhất, chọn 1 cửa hàng
qua autocomplete, lưới item với toggle đơn + bulk action. Đây là bản đầy đủ theo đúng phạm vi
nghiệp vụ thực tế — không phải bản rút gọn tạm thời.

Nếu sau này business đổi ý cần khóa có hiệu lực tức thời tại quầy (không đợi chu kỳ Sync), cần mở
lại task riêng, thiết kế mới hoàn toàn cho kết nối máy POS (kiến trúc hiện tại chưa có
`IDbConnectionFactory` nào tương đương route tới IP terminal — xem `docs/migrations/MIGRATION_MAP.md`
mục 4.7).
