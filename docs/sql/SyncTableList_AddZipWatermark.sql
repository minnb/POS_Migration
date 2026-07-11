-- Thêm cột ZipWatermarkCounter vào dbo.SyncTableList (CentralMD) — watermark bền vững thay thế
-- Redis Hash "Worker:Watermark:MasterDataZip" của MasterDataZipGeneratorWorker.
--
-- BỐI CẢNH: trước script này, MasterDataZipGeneratorWorker so sánh POSLastCounter với 1 watermark
-- lưu trong Redis Hash. Nếu Redis Hash bị mất (xóa tay, restart mất persistence, evict...), code coi
-- đó là "lần đầu chạy" → seed lại watermark bằng counter HIỆN TẠI rồi bỏ qua generate — mọi thay đổi
-- xảy ra trước khi mất key sẽ KHÔNG BAO GIỜ được trigger sinh zip (watermark "nhảy cóc" qua thay đổi
-- chưa xử lý), lỗi silent, không throw exception. Xem thảo luận đầy đủ trong lịch sử quyết định tại
-- .claude/rules/masterdata-sync.md mục "Worker sinh zip theo watermark + quarantine".
--
-- THIẾT KẾ: watermark chuyển từ Redis (in-memory, có thể mất) sang 1 cột mới trên chính bảng
-- SyncTableList (SQL Server, bền vững) — không đụng đến cột POSLastCounter/SP
-- usp_SyncTableList_BulkUpdateCounter hiện có (đang chạy ổn định, ghi bởi SyncTableCounterFlushWorker
-- từ cả POS.Api và POS.Web mỗi 5s cho nhiều bảng — Pilot A/B/C/D). ZipWatermarkCounter là cột RIÊNG,
-- CHỈ MasterDataZipGeneratorWorker đọc/ghi, không liên quan luồng ghi counter hiện có.
--
-- So sánh phát hiện đổi: POSLastCounter > ZipWatermarkCounter (worker đọc 2 cột trong 1 dòng SP1
-- [SyncTable_Get] @IsChange='C', không cần Redis, không cần round-trip thứ 2).
-- ACK sau khi generate xong (chỉ khi mọi terminal active thành công): UPDATE ZipWatermarkCounter =
-- giá trị POSLastCounter đã SNAPSHOT lúc đầu cycle (KHÔNG re-read DB tại thời điểm ACK — nếu có
-- write-path bump counter giữa lúc đọc và lúc ACK, re-read sẽ vô tình "nuốt" thay đổi mới chưa kịp
-- generate, lặp lại đúng lớp lỗi ban đầu chỉ khác nguyên nhân).
--
-- Áp dụng: chạy thủ công 1 lần trên RPOSMasterData (CentralMD), BẮT BUỘC TRƯỚC KHI deploy code
-- MasterDataZipGeneratorWorker mới (code mới SELECT cột ZipWatermarkCounter từ SyncTable_Get và gọi
-- SP usp_SyncTableList_BulkUpdateZipWatermark — nếu deploy code trước, worker sẽ lỗi mỗi cycle).
-- Backfill NGAY trong script: ZipWatermarkCounter = POSLastCounter hiện tại → cycle đầu tiên sau khi
-- deploy code mới sẽ thấy "không đổi gì" (tương đương hành vi seed cũ), không cần logic "seed lần
-- đầu" trong C# nữa (worker mới đã bỏ hẳn logic đó).
-- Sau khi chạy xong, xóa cache Redis "MD:SyncTableList:C" (TTL 3600s) để có hiệu lực ngay:
-- DEL MD:SyncTableList:C

ALTER TABLE dbo.SyncTableList
    ADD ZipWatermarkCounter BIGINT NULL CONSTRAINT DF_SyncTableList_ZipWatermarkCounter DEFAULT (0);
GO

UPDATE dbo.SyncTableList
SET ZipWatermarkCounter = ISNULL(POSLastCounter, 0);
GO

/* ── TVP riêng cho ACK watermark của zip-worker (KHÔNG tái dùng TVP_SyncCounterUpdate — tránh
   nhầm ngữ nghĩa giữa "counter nguồn dữ liệu" và "watermark ACK của 1 worker cụ thể") ──────── */
IF TYPE_ID(N'dbo.TVP_ZipWatermarkUpdate') IS NULL
CREATE TYPE dbo.TVP_ZipWatermarkUpdate AS TABLE
(
    TableName varchar(50) NOT NULL,
    Counter   bigint      NOT NULL
);
GO

/* ── Batch update ZipWatermarkCounter — gọi bởi MasterDataZipGeneratorWorker sau khi generate
   xong VÀ mọi terminal active thành công trong cycle (all-or-nothing per cycle). Giá trị @Counters
   PHẢI là giá trị POSLastCounter đã snapshot lúc đầu cycle, KHÔNG re-read DB lúc ACK. ──────────── */
CREATE OR ALTER PROCEDURE dbo.usp_SyncTableList_BulkUpdateZipWatermark
(
    @Counters dbo.TVP_ZipWatermarkUpdate READONLY
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE s
    SET    s.ZipWatermarkCounter = t.Counter
    FROM   dbo.SyncTableList s
    INNER JOIN @Counters t ON t.TableName = s.TableName
    WHERE  t.Counter > ISNULL(s.ZipWatermarkCounter, 0);
END
GO

/* ── SP [SyncTable_Get] — thêm ZipWatermarkCounter vào SELECT của nhánh @IsChange='C' (nhánh DUY
   NHẤT cần giá trị này). Nhánh 'A'/'W' KHÔNG đổi (vẫn hardcode POSLastCounter=1, không dùng
   ZipWatermarkCounter). Copy nguyên cả 3 nhánh từ docs/sql/SyncTableList_AddAction.sql để không mất
   nội dung. ──────────────────────────────────────────────────────────────────────────────────── */
ALTER PROCEDURE [dbo].[SyncTable_Get]
(
@IsChange varchar(1) = 'A',
@IsByStore int = -1,
@GroupName varchar(50)=''
)
AS
/*
[SyncTable_Get] 'A'
[SyncTable_Get] 'W'
*/
BEGIN
	IF @IsChange = 'C' -- Hien tai k dung @IsChange='C'
	BEGIN
		SELECT TableName,POSLastCounter,ISNULL(ZipWatermarkCounter,0) AS ZipWatermarkCounter,[Procedure],[OrderByName],IsByStore,ISNULL(ColumnFilter,'') ColumnFilter,GroupName,ISNULL(IsFirstDataAll,0) AS IsFirstDataAll,ISNULL(IsSingleFile,0) AS IsSingleFile,ISNULL(Action,'TRUNC-INSERT') AS Action
		FROM SyncTableList T (Nolock)
		WHERE [Enabled] = 1
		AND IsOnlyChange = 1
		AND (@IsByStore = -1 OR IsByStore = @IsByStore)
		AND (ISNULL(T.GroupName,'') = @GroupName OR @GroupName='')
		ORDER BY [OrderByName],TableName
	END
	IF @IsChange = 'A'
	BEGIN
		SELECT TableName,POSLastCounter = 1,[Procedure],[OrderByName],IsByStore,ISNULL(ColumnFilter,'') ColumnFilter,GroupName,ISNULL(IsFirstDataAll,0) AS IsFirstDataAll,ISNULL(IsSingleFile,0) AS IsSingleFile,ISNULL(Action,'TRUNC-INSERT') AS Action
		FROM SyncTableList T (Nolock)
		WHERE [Enabled] = 1
		--AND IsAll = 1
		AND (@IsByStore = -1 OR IsByStore = @IsByStore)
		AND (ISNULL(T.GroupName,'') = @GroupName OR @GroupName='')
		ORDER BY [OrderByName],TableName
	END
	IF @IsChange = 'W' -- Web Sync / push 1 POS (PosMapPage "Đồng bộ") — Action luôn DELETE-INSERT
	BEGIN
		SELECT TableName,POSLastCounter = 1,[Procedure],[OrderByName],IsByStore,ISNULL(ColumnFilter,'') ColumnFilter,GroupName,ISNULL(IsFirstDataAll,0) AS IsFirstDataAll,ISNULL(IsSingleFile,0) AS IsSingleFile,'DELETE-INSERT' AS Action
		FROM SyncTableList T (Nolock)
		WHERE [Enabled] = 1
		AND (@IsByStore = -1 OR IsByStore = @IsByStore)
		AND (ISNULL(T.GroupName,'') = @GroupName OR @GroupName='')
		ORDER BY [OrderByName],TableName
	END
END
GO
