# PosSalesConsumerWorker — Chi tiết kỹ thuật

> Tổng hợp logic kỹ thuật, ràng buộc DB, và các "gotcha" phát hiện được khi rà soát code (2026-07-10).
> Tổng quan/inventory: `docs/worker/worker_status.md` mục 1.1 (#2). Luồng producer→consumer đầy đủ
> (kèm sequence diagram): `docs/api/PushSales_Flow.md`. File này đào sâu **cơ chế cụ thể bên trong
> worker consumer** — không lặp lại nội dung 2 file trên.

---

## 1. Vị trí file

| File | Vai trò |
|---|---|
| `src/POS.Infrastructure/Workers/PosSalesConsumerWorker.cs` | `BackgroundService` — RabbitMQ consumer queue `pos_sales` |
| `src/POS.Infrastructure/Messaging/RabbitMQProducer.cs` | Producer (phía đối xứng — publish vào cùng queue) |
| `src/POS.Infrastructure/Messaging/RabbitMQOptions.cs` | Options bind section `"RabbitMQ"` |
| `src/POS.Infrastructure/Repositories/Sale/CentralSaleRepository.cs` — `InInsertToTableByJson` | Nơi thật sự ghi dữ liệu vào DB (**dùng chung với `PosFileImportWorker`** — xem `docs/worker/PosFileImportWorker.md` §3) |
| `docs/sql/database/CentralSale.sql` (UTF-16) | SP `Sale_InsertDataByOrder_KAFKA`, `Sale_InsertToTableByJsonV2` |
| `src/POS.Infrastructure/Workers/WorkerHealthState.cs` | Singleton chia sẻ trạng thái — worker này là 1 trong 2 writer (xem `docs/worker/WorkerHeartbeatService.md`) |

---

## 2. Logic kỹ thuật

### 2.1 Vòng lặp kết nối ngoài + consumer bên trong

```
while (!stoppingToken.IsCancellationRequested)
{
    conn = factory.CreateConnectionAsync(...)          // RabbitMQOptions: Host/Port/Username/Password/VirtualHost/RequestedHeartbeat
    channel = conn.CreateChannelAsync(...)
    channel.QueueDeclareAsync("pos_sales", durable:true, exclusive:false, autoDelete:false, arguments:null)
    channel.BasicQosAsync(prefetchSize:0, prefetchCount:1, global:false)   // 1 message/lượt — tránh overload khi DB chậm
    consumer = new AsyncEventingBasicConsumer(channel)
    consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea)
    channel.BasicConsumeAsync("pos_sales", autoAck:false, consumer, ...)
    healthState.Status = "Running"

    // chờ tới khi connection bị rớt (ConnectionShutdownAsync) hoặc token bị hủy
    connDropped = TaskCompletionSource(...)
    conn.ConnectionShutdownAsync += (_, _) => connDropped.TrySetResult()
    await Task.WhenAny(connDropped.Task, Task.Delay(Timeout.Infinite, stoppingToken))
}
catch (Exception ex) { healthState.Status = "Degraded"; log "retry in 10s"; }
finally { đóng/dispose channel + conn (bọc try/catch rỗng) }
await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken)   // backoff trước khi reconnect từ đầu
```

**`prefetchCount=1`** — nhận đúng 1 message chưa ack tại một thời điểm; đây là consumer **tuần tự nghiêm ngặt**, không xử lý song song nhiều message.

**`autoAck:false`** — bắt buộc ack/nack thủ công, khớp với thiết kế "xử lý xong mới nhận tiếp".

### 2.2 Hai cơ chế phục hồi khác nhau — dễ nhầm vì cùng dùng số "10s"

| Cơ chế | Kích hoạt bởi | Hành vi |
|---|---|---|
| `RabbitMQ.Client` tự động (`AutomaticRecoveryEnabled` + `NetworkRecoveryInterval=10s`) | Đứt mạng TCP tạm thời (blip) | Tự khôi phục **bên trong** cùng 1 `conn`/`channel`, KHÔNG cần vòng `while` ngoài can thiệp |
| Backoff của chính worker (`Task.Delay(10s)` sau `finally`) | `ConnectionShutdownAsync` bắn (kết nối đứt hẳn) hoặc exception lúc setup (declare/consume) | **Đóng hẳn** `conn`/`channel` cũ, tạo lại **từ đầu** (declare queue mới, consumer mới) sau 10s |

Hai cơ chế độc lập, không phải 1 cái gọi lại cái kia — cần phân biệt khi đọc log ("Connection error — retry in 10s" chỉ log ở cơ chế thứ 2).

### 2.3 `HandleMessageAsync` — xử lý 1 message

```csharp
msg = JsonConvert.DeserializeObject<KafkaMessageDto>(body);   // Newtonsoft
if (msg is null) { BasicNackAsync(requeue:false); return; }    // JSON hỏng → DROP, không DLQ

result = await repo.InInsertToTableByJson(
    StringHelper.Left(msg.TransactionId, 4),   // storeNo — TÍNH RA NHƯNG KHÔNG DÙNG (xem Gotcha #2)
    StringHelper.Left(msg.TransactionId, 6),   // posNo
    msg.TransactionId, msg.Message ?? "", "WORKER");
    // LƯU Ý: KHÔNG truyền ct — mặc định CancellationToken.None, việc DB không bị hủy khi app shutdown

if (result.Item1) { BasicAckAsync(...); healthState.IncrementProcessed(); }
else              { BasicNackAsync(requeue:false); }   // lỗi nghiệp vụ HAY lỗi kết nối DB đều DROP giống nhau
```

Toàn bộ khối `catch` ngoài cùng cũng `BasicNackAsync(requeue:false)`, bọc `try/catch{}` rỗng — nếu `BasicNackAsync` chính nó lỗi (channel đã hỏng), message vẫn nằm "chưa ack" trên broker, chỉ được redeliver khi kết nối đứt hẳn (hành vi phía broker, không do code chủ động).

**Không có Dead-Letter Queue** — `arguments:null` ở cả 2 nơi `QueueDeclareAsync` (consumer lẫn producer), không có `x-dead-letter-exchange`. Mọi message lỗi (JSON hỏng, DB lỗi, exception bất kỳ) đều bị `nack(requeue:false)` → **mất vĩnh viễn**, không có nơi nào giữ lại để soát sau.

---

## 3. Ràng buộc DB — dùng chung 100% với `PosFileImportWorker`

Gọi cùng `ICentralSaleRepository.InInsertToTableByJson` → cùng SP `Sale_InsertDataByOrder_KAFKA` → cùng `Sale_InsertToTableByJsonV2`. **Toàn bộ chi tiết STATUS code (0/1/2/7), transaction, dedupe theo `OrderNo`/`DocumentNo`, hardcode skip `TransBonus`, và hành vi `message.Replace("'","")` xóa dấu nháy đơn đã ghi đầy đủ ở `docs/worker/PosFileImportWorker.md` §3 — không lặp lại ở đây, chỉ khác biệt so với luồng file-import được nêu dưới**:

- Khác biệt duy nhất về tham số gọi: `source = "WORKER"` (thay vì `"FILE"`) — dùng để phân biệt trong `DataRawJson.Source`.
- `ct` không được truyền khi gọi từ consumer (khác `PosFileImportService` cũng không truyền — cả 2 giống nhau ở điểm này, method mặc định `CancellationToken.None`).
- **Connection dùng `CentralSaleConnectionFactory` cố định** (không qua `StoreRoutedConnectionFactory`/StoreSetServer routing theo store) — `storeNo` tính ra từ `TransactionId` **không hề được dùng** bên trong `InInsertToTableByJson` (tham số chết, xác nhận qua đọc code — có thể là tàn dư từ trước khi đổi sang "bỏ StoreSetServer routing").

---

## 4. Cấu hình (`RabbitMQOptions`, section `"RabbitMQ"`)

| Field | Vai trò |
|---|---|
| `Host`/`Port`/`Username`/`Password`/`VirtualHost` | Kết nối broker — dùng chung bởi cả producer (`RabbitMQProducer`) và consumer |
| `RequestedHeartbeat` | Governs phát hiện kết nối chết (mặc định 60s) — ảnh hưởng tốc độ `ConnectionShutdownAsync` bắn, từ đó ảnh hưởng thời điểm backoff 10s ở mục 2.2 kích hoạt |

Tên queue **`"pos_sales"` hardcode độc lập ở 3 nơi** (`PosSalesConsumerWorker.cs`, `KafkaAppService.cs` — producer, `WorkerHeartbeatService.cs` — chỉ dùng để hiển thị `QueueName` trong heartbeat), không có hằng số dùng chung.

Queue khai báo `durable:true, exclusive:false, autoDelete:false` **khớp cả 2 phía** producer/consumer — không dùng exchange riêng (publish vào default exchange, `routingKey = queueName`, tự bind theo quy ước RabbitMQ mặc định).

---

## 5. Gotchas (điểm cần lưu ý)

1. **Không có Dead-Letter Queue — mọi message lỗi bị mất vĩnh viễn.** JSON hỏng, lỗi DB (timeout, connection down), exception bất kỳ → đều `nack(requeue:false)`, không retry, không lưu lại nơi nào để soát. Khác hẳn kỳ vọng thường thấy ở hệ thống queue "at-least-once" — đây thực chất là "best-effort, silent-drop-on-failure".
2. **`storeNo` tính ra từ `TransactionId` nhưng KHÔNG được dùng** trong `InInsertToTableByJson` — công tính toán `StringHelper.Left(msg.TransactionId, 4)` là dead work, dấu hiệu code cũ chưa dọn sau khi đổi kiến trúc routing.
3. **STATUS 2/7 (trùng SALE/VOID) và ORIGSALE (no-op) đều được coi là "thành công"** ở tầng C# — xem chi tiết đầy đủ ở `PosFileImportWorker.md` §3 (áp dụng y hệt cho consumer này vì dùng chung code path).
4. **`message.Replace("'", "")` xóa mọi dấu nháy đơn trong TOÀN BỘ payload** trước khi gửi SP — tên khách hàng/sản phẩm có `'` (vd `O'Brien`) mất ký tự vĩnh viễn, không log cảnh báo. Đây là hành vi ở tầng `CentralSaleRepository`, ảnh hưởng **cả 2 nguồn** (RabbitMQ consumer lẫn file-import).
5. **`WorkerHealthState` bị 2 worker khác nhau ghi chung** (`PosSalesConsumerWorker` VÀ `Rpt_ReportSaleDetail_Insert`) — heartbeat publish dưới tên cứng `"PosSalesConsumer"` có thể phản ánh sai trạng thái thật của RabbitMQ consumer khi lỗi thực chất đến từ SQL report job. Xem phân tích đầy đủ + kịch bản cụ thể ở `docs/worker/WorkerHeartbeatService.md` §3 — **đây là gotcha nghiêm trọng nhất liên quan tới worker này vì có thể khiến `/ops/health` chẩn đoán sai** ("mất kết nối RabbitMQ" trong khi RabbitMQ vẫn khỏe).
6. **2 cơ chế reconnect dễ nhầm lẫn khi đọc log** — RabbitMQ.Client tự phục hồi network blip (không log gì ở tầng worker) khác với backoff 10s của chính worker khi kết nối đứt hẳn (có log "retry in 10s") — xem mục 2.2.
7. **Không truyền `CancellationToken` khi gọi `InInsertToTableByJson`** — request DB không bị hủy khi app đang shutdown, có thể kéo dài thời gian graceful-shutdown nếu SP đang chạy chậm.
8. **`BasicNackAsync` trong catch cũng bị bọc `try/catch{}` rỗng** — nếu channel đã hỏng lúc gọi nack, message ở trạng thái "chưa ack" vô thời hạn cho tới khi kết nối bị đứt hẳn (broker tự redeliver khi đó) — không có gì chủ động xử lý tình huống này ở tầng ứng dụng.

---

## 6. Trạng thái verify

- Đã đọc toàn bộ `PosSalesConsumerWorker.cs`, phần liên quan của `RabbitMQProducer.cs`/`RabbitMQOptions.cs`, `CentralSaleRepository.InInsertToTableByJson` (đầy đủ, dùng chung với `PosFileImportWorker`), SP `Sale_InsertDataByOrder_KAFKA`/`Sale_InsertToTableByJsonV2`.
- **CHƯA chạy thử thật** (cần RabbitMQ + SQL Server CentralSale reachable) — mọi phát hiện dựa trên đọc code tĩnh, không phải quan sát runtime. Đặc biệt hành vi "2 cơ chế reconnect" và "mất message không DLQ" chưa được kiểm chứng bằng cách chủ động ngắt kết nối/gửi message lỗi trong môi trường thật.
