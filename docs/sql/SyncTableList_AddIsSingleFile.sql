-- Thêm cột IsSingleFile vào dbo.SyncTableList (CentralMD) + cập nhật SP [SyncTable_Get].
-- Mục đích: cho phép DBA đánh dấu 1 bảng cần đóng gói RIÊNG 1 file .zip (thay vì gom chung
-- vào zip "common") khi sinh master data cho POS — giảm dung lượng zip gộp, tránh timeout
-- download trên máy POS khi tải file lớn.
-- Áp dụng: chạy thủ công 1 lần trên RPOSMasterData (CentralMD). Sau khi chạy xong, xoá cache
-- Redis key "MD:SyncTableList" (TTL 3600s) để có hiệu lực ngay: DEL MD:SyncTableList
-- IsSingleFile mặc định = 0 cho mọi bảng hiện có → KHÔNG đổi hành vi sinh file hiện tại cho
-- đến khi DBA chủ động UPDATE IsSingleFile = 1 cho bảng cần tách riêng.

ALTER TABLE dbo.SyncTableList
    ADD IsSingleFile BIT NOT NULL CONSTRAINT DF_SyncTableList_IsSingleFile DEFAULT (0);
GO

ALTER PROCEDURE [dbo].[SyncTable_Get]
(
@IsChange varchar(1) = 'A',
@IsByStore int = -1,
@GroupName varchar(50)=''
)
AS
/*
[SyncTable_Get] 'A'
*/
BEGIN
	IF @IsChange = 'C' -- Hien tai k dung @IsChange='C'
	BEGIN
		SELECT TableName,POSLastCounter,[Procedure],[OrderByName],IsByStore,ISNULL(ColumnFilter,'') ColumnFilter,GroupName,ISNULL(IsFirstDataAll,0) AS IsFirstDataAll,ISNULL(IsSingleFile,0) AS IsSingleFile
		FROM SyncTableList T (Nolock)
		WHERE [Enabled] = 1
		AND IsOnlyChange = 1
		AND (@IsByStore = -1 OR IsByStore = @IsByStore)
		AND (ISNULL(T.GroupName,'') = @GroupName OR @GroupName='')
		ORDER BY [OrderByName],TableName
	END
	IF @IsChange = 'A'
	BEGIN
		SELECT TableName,POSLastCounter,[Procedure],[OrderByName],IsByStore,ISNULL(ColumnFilter,'') ColumnFilter,GroupName,ISNULL(IsFirstDataAll,0) AS IsFirstDataAll,ISNULL(IsSingleFile,0) AS IsSingleFile
		FROM SyncTableList T (Nolock)
		WHERE [Enabled] = 1
		--AND IsAll = 1
		AND (@IsByStore = -1 OR IsByStore = @IsByStore)
		AND (ISNULL(T.GroupName,'') = @GroupName OR @GroupName='')
		ORDER BY [OrderByName],TableName
	END
END
GO

-- Ví dụ DBA đánh dấu bảng cần tách riêng 1 zip (điều chỉnh danh sách theo thực tế kích thước dữ liệu):
-- UPDATE dbo.SyncTableList SET IsSingleFile = 1 WHERE TableName IN ('Barcodes', 'Item', 'SalesPrice');
