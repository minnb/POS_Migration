USE [RPOSMasterData]
GO

/*
 ============================================================================
  [dbo].[MasterDataDownloadLog] — log mỗi lượt POS tải file qua DowloadFileStream
 ============================================================================
  API ghi 1 dòng sau mỗi lượt download (thành công hoặc bị ngắt). Best-effort:
  Status='Success' = API đã gửi đủ byte không bị client ngắt (KHÔNG đảm bảo POS
  đã lưu xong); Status='Aborted' = client ngắt giữa chừng; 'Error' = lỗi khác.
  Ghi log fail-safe ở app: nếu bảng chưa tồn tại, app KHÔNG crash (nuốt lỗi).
 ============================================================================
*/

IF OBJECT_ID('dbo.MasterDataDownloadLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterDataDownloadLog
    (
        Id            bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MasterDataDownloadLog PRIMARY KEY,
        SiteCode      varchar(50)   NULL,
        PosTerminal   varchar(50)   NULL,
        FileName      nvarchar(260) NULL,
        FilePath      nvarchar(1000) NULL,
        FileSizeBytes bigint        NOT NULL CONSTRAINT DF_MasterDataDownloadLog_Size DEFAULT(0),
        DurationMs    bigint        NOT NULL CONSTRAINT DF_MasterDataDownloadLog_Dur  DEFAULT(0),
        Status        varchar(20)   NOT NULL,
        ClientIp      varchar(64)   NULL,
        DownloadedAt  datetime      NOT NULL CONSTRAINT DF_MasterDataDownloadLog_At DEFAULT(GETDATE())
    );

    CREATE INDEX IX_MasterDataDownloadLog_Site_At
        ON dbo.MasterDataDownloadLog (SiteCode, PosTerminal, DownloadedAt);
END
GO

/*
 ============================================================================
  Bổ sung DeletedAt/DeleteStatus — POS gọi DeleteFileFromFTP sau khi xử lý
  xong file tải về; API cập nhật 2 cột này vào đúng bản ghi download log
  tương ứng (FileName + SiteCode/PosTerminal + DownloadedAt mới nhất) để
  biết POS đã xóa file xong hay chưa. DeleteStatus: 'Success' | 'Failed'.
 ============================================================================
*/

IF COL_LENGTH('dbo.MasterDataDownloadLog', 'DeletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.MasterDataDownloadLog ADD DeletedAt datetime NULL;
END
GO

IF COL_LENGTH('dbo.MasterDataDownloadLog', 'DeleteStatus') IS NULL
BEGIN
    ALTER TABLE dbo.MasterDataDownloadLog ADD DeleteStatus varchar(20) NULL;
END
GO
