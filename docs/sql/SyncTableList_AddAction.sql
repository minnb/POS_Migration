-- Thêm cột Action vào dbo.SyncTableList (CentralMD) + cập nhật SP [SyncTable_Get].
-- Mục đích: trả Action ("TRUNC-INSERT"/"INSERT"/"DELETE-INSERT"...) NGAY TỪ SP thay vì hardcode
-- ở C# (MasterDataSyncService) — DBA cấu hình Action theo từng bảng trong SyncTableList.
-- Áp dụng: chạy thủ công 1 lần trên RPOSMasterData (CentralMD). Sau khi chạy xong, xoá cache
-- Redis key "MD:SyncTableList:A", "MD:SyncTableList:W" và "MD:SyncTableList:C" (TTL 3600s,
-- ":C" dùng bởi MasterDataZipGeneratorWorker/PushMasterDataChangeAsync) để có hiệu lực ngay:
-- DEL MD:SyncTableList:A
-- DEL MD:SyncTableList:W
-- DEL MD:SyncTableList:C
-- Action mặc định = 'TRUNC-INSERT' cho mọi bảng hiện có → hành vi ALL sync KHÔNG đổi cho đến khi
-- DBA chủ động UPDATE Action cho bảng cần khác.
--
-- Thêm nhánh MỚI @IsChange='W' (Web Sync/push 1 POS — PosMapPage nút "Đồng bộ" /
-- SyncDataPosService.PushStartOfDayDataAsync): cùng WHERE/filter như nhánh 'A', nhưng Action luôn
-- literal 'DELETE-INSERT' (thay cho GetMasterDataFileRequest.SyncAction ép cứng ở C# trước đây) —
-- áp dụng cho MỌI batch của MỌI bảng khi push 1 POS.

ALTER TABLE dbo.SyncTableList
    ADD Action VARCHAR(20) NOT NULL CONSTRAINT DF_SyncTableList_Action DEFAULT ('TRUNC-INSERT');
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
[SyncTable_Get] 'W'
*/
BEGIN
	IF @IsChange = 'C' -- Hien tai k dung @IsChange='C'
	BEGIN
		SELECT TableName,POSLastCounter,[Procedure],[OrderByName],IsByStore,ISNULL(ColumnFilter,'') ColumnFilter,GroupName,ISNULL(IsFirstDataAll,0) AS IsFirstDataAll,ISNULL(IsSingleFile,0) AS IsSingleFile,ISNULL(Action,'TRUNC-INSERT') AS Action
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

-- Ví dụ DBA đổi Action mặc định cho 1 bảng cụ thể (điều chỉnh danh sách theo thực tế nghiệp vụ):
-- UPDATE dbo.SyncTableList SET Action = 'DELETE-INSERT' WHERE TableName IN ('SomeTable');
