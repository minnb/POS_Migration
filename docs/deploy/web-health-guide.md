# POS.Web — Hướng dẫn cấu hình Health Check (`/ops/health`)

> Runbook ngắn cho DevOps khi deploy/đổi môi trường POS.Web. Chi tiết đầy đủ (lý do thiết kế,
> lịch sử phát hiện gap): `docs/ROLLOUT.md` §O9, `docs/CHANGELOG.md` [2026-07-11].

## 1. Trang & 8 mục monitor

`HealthPage.razor` (`/ops/health`, policy `OpsAndAbove`) gọi
`IHealthCheckService.CheckAllAsync` (`POS.Application/Features/Common/HealthCheckService.cs`),
chạy song song 8 check:

| Mục | Cấu hình (đọc từ `IConfiguration`) |
|---|---|
| Redis | `Redis:SentinelHosts` |
| RabbitMQ | `RabbitMQ:Host`, `RabbitMQ:Port` |
| SQL:CentralMD / CentralGeneral / CentralSale | `ConnectionStrings:CentralMD/CentralGeneral/CentralSale` |
| SQL:CentralSaleTemplate | `SetDb:DB1` + `ConnectionStrings:CentralSaleTemplate` |
| POS.Api (HTTP) | `HealthCheck:PosApiBaseUrl` |
| Worker heartbeat | `HealthCheck:WorkerName` (đọc Redis key `Worker:Heartbeat:{WorkerName}`) |

## 2. Section `HealthCheck` (POS.Web `appsettings.*.json`)

```json
"HealthCheck": {
  "PosApiBaseUrl": "http://<host>:<port>",
  "WorkerName": "PosSalesConsumer",
  "StaleAfterSeconds": 45,
  "CheckTimeoutSeconds": 8,
  "SqlConnectTimeoutSeconds": 5,
  "HttpTimeoutSeconds": 5
}
```

| Key | Default | Ý nghĩa |
|---|---|---|
| `PosApiBaseUrl` | — (**bắt buộc điền theo môi trường**) | Base URL POS.Web gọi tới POS.Api để check `/health`. **KHÔNG kèm sẵn `/health`** — code tự nối suffix (`CheckWebApiAsync`), điền kèm sẽ ra `.../health/health` (404). Chỉ điền `scheme://host:port`. |
| `WorkerName` | `PosSalesConsumer` | Tên worker đọc heartbeat Redis — phải khớp `WorkerHeartbeat:WorkerName` bên POS.Worker (mục 3), nếu không mục "Worker" luôn báo "Key không tồn tại". |
| `StaleAfterSeconds` | 45 | Ngưỡng coi worker "mất tín hiệu" nếu heartbeat cuối cũ hơn N giây. |
| `CheckTimeoutSeconds` | 8 | Timeout tối đa mỗi check (toàn bộ 8 mục) trước khi báo "Timeout". |
| `SqlConnectTimeoutSeconds` | 5 | `ConnectTimeout` khi mở connection test SQL. |
| `HttpTimeoutSeconds` | 5 | Timeout `HttpClient` khi gọi `GET {PosApiBaseUrl}/health`. |

**Trạng thái hiện tại theo môi trường** (`src/POS.Web/appsettings.*.json`):

| Môi trường | `PosApiBaseUrl` |
|---|---|
| Development (base) | `http://localhost:8080` |
| UAT | `<UAT_POS_API_BASE_URL>` — **placeholder, Ops phải điền trước go-live** |
| Production | `http://localhost:5001/health` — ⚠️ **đang sai**, xem mục 4 |

## 3. Section `WorkerHeartbeat` (POS.Worker `appsettings.*.json`)

```json
"WorkerHeartbeat": {
  "WorkerName": "PosSalesConsumer",
  "QueueName": "pos_sales",
  "IntervalSeconds": 15,
  "NormalTtlSeconds": 60,
  "StoppedTtlSeconds": 300
}
```

Ghi heartbeat vào Redis key `Worker:Heartbeat:{WorkerName}` mỗi `IntervalSeconds`, TTL
`NormalTtlSeconds` lúc `Running` / `StoppedTtlSeconds` lúc dừng có chủ ý. Giá trị mặc định giống
nhau ở cả Dev/UAT/Production — chỉ cần đổi nếu Ops muốn tinh chỉnh, không có key nào bắt buộc phải
khác theo môi trường.

## 4. ⚠️ Việc cần làm trước khi go-live (checklist)

- [ ] **UAT**: điền `HealthCheck:PosApiBaseUrl` đúng URL nội bộ POS.Web gọi được tới POS.Api trong
      UAT (thay `<UAT_POS_API_BASE_URL>`). Xác định qua `docker network inspect` / nginx vhost /
      hỏi team hạ tầng nếu 2 service khác host.
- [ ] **Production**: giá trị hiện tại `http://localhost:5001/health` bị **thừa suffix `/health`**
      — code tự nối thêm `/health` nên request thật sẽ gọi `.../health/health` (404). Nếu `5001`
      đúng là cổng POS.Api thật, sửa lại thành `http://localhost:5001` (bỏ `/health`).
- [ ] Xác nhận `HealthCheck:WorkerName` (POS.Web) khớp `WorkerHeartbeat:WorkerName` (POS.Worker) ở
      cùng môi trường — mặc định cả 2 đều `PosSalesConsumer`, chỉ cần lưu ý nếu sau này đổi tên.
- [ ] Verify: mở `/ops/health` trên môi trường vừa deploy, mục "POS.Api" phải trả `HTTP 200` (không
      phải lỗi kết nối/timeout/404).

## 5. Tham chiếu

- Code: `src/POS.Application/Features/Common/HealthCheckService.cs`,
  `src/POS.Infrastructure/Workers/{WorkerHeartbeatService,WorkerHeartbeatOptions}.cs`
- Rollout chi tiết: `docs/ROLLOUT.md` §O9
- Danh sách placeholder cần điền khi deploy UAT: `docs/guide-deploy.md`
- Giới hạn đã biết, chưa xử lý: trang chỉ giám sát **1** worker theo `HealthCheck:WorkerName` dù hệ
  thống có nhiều worker ghi heartbeat riêng (`PosFileImport`, `MasterDataZipGenerator`) — xem
  `docs/worker/worker_status.md` mục 5.
