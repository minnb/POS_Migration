/* ============================================================================
   Batch-update SyncTableList.POSLastCounter — SyncTableCounterFlushWorker
   (POS.Infrastructure/Sync/) gọi định kỳ (mặc định mỗi 5s) qua Channel drain,
   KHÔNG bao giờ gọi trực tiếp trên hot path CRUD (tránh row-level lock contention
   trên SyncTableList khi nhiều request ghi master data đồng thời).

   - TVP_SyncCounterUpdate: (TableName, Counter) — 1 dòng/bảng, giá trị Max đã
     coalesce phía C# trước khi gửi xuống.
   - usp_SyncTableList_BulkUpdateCounter: UPDATE "chỉ tăng" — điều kiện
     Counter > ISNULL(POSLastCounter,0) làm câu lệnh IDEMPOTENT, an toàn khi
     POS.Api và POS.Web (2 tiến trình riêng, mỗi tiến trình có Channel/Worker
     riêng) cùng flush đồng thời cho cùng 1 bảng.

   CHẠY 1 LẦN trên RPOSMasterData (CentralMD). Script idempotent (kiểm tra tồn
   tại trước khi tạo TYPE; CREATE OR ALTER cho SP) — chạy lại an toàn.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── TVP ────────────────────────────────────────────────────────────────── */
IF TYPE_ID(N'dbo.TVP_SyncCounterUpdate') IS NULL
CREATE TYPE dbo.TVP_SyncCounterUpdate AS TABLE
(
    TableName varchar(50) NOT NULL,
    Counter   bigint      NOT NULL
);
GO

/* ── Batch update POSLastCounter ─────────────────────────────────────────── */
CREATE OR ALTER PROCEDURE dbo.usp_SyncTableList_BulkUpdateCounter
(
    @Counters dbo.TVP_SyncCounterUpdate READONLY
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE s
    SET    s.POSLastCounter = t.Counter,
           s.LastUpdated    = GETDATE()
    FROM   dbo.SyncTableList s
    INNER JOIN @Counters t ON t.TableName = s.TableName
    WHERE  t.Counter > ISNULL(s.POSLastCounter, 0);
END
GO
