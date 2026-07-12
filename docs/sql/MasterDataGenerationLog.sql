USE [RPOSMasterData]
GO

/*
 ============================================================================
  [dbo].[MasterDataGenerationLog] — log mỗi lần SINH file master data .zip
 ============================================================================
  Bổ sung cho MasterDataDownloadLog (log POS TẢI/XÓA file). Bảng này ghi 1 dòng
  cho MỖI file .zip được publish trong EnsureMasterDataFileAsync — phủ MỌI luồng
  sinh file, phân biệt bằng cột TriggerSource:
    'AutoChange' = MasterDataZipGeneratorWorker (watermark-driven, IsChangeMode='C')
    'ManualSync' = nút "Đồng bộ dữ liệu" trên PosMapPage (PushStartOfDayDataAsync)
    'PosPull'    = POS gọi GetFileFromFTP?typeSync=ALL (SyncDataPosController)
  Status='Success' = zip đã publish; 'Error' = cả lượt sinh lỗi (KHÔNG publish
  file thiếu bảng — 1 dòng Error/lượt, FileName=NULL).
  Ghi log fail-safe ở app: nếu bảng chưa tồn tại, app KHÔNG crash (nuốt lỗi).
  Đối soát "đã sinh ↔ POS đã tải": JOIN MasterDataDownloadLog theo FileName.
 ============================================================================
*/

IF OBJECT_ID('dbo.MasterDataGenerationLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterDataGenerationLog
    (
        Id            bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_MasterDataGenerationLog PRIMARY KEY,
        StoreNo       varchar(10)   NULL,
        PosNo         varchar(10)   NULL,
        FileName      nvarchar(260) NULL,
        FilePath      nvarchar(1000) NULL,
        FileSizeBytes bigint        NOT NULL CONSTRAINT DF_MasterDataGenerationLog_Size DEFAULT(0),
        TableCount    int           NOT NULL CONSTRAINT DF_MasterDataGenerationLog_Tbl  DEFAULT(0),
        DurationMs    bigint        NOT NULL CONSTRAINT DF_MasterDataGenerationLog_Dur  DEFAULT(0),
        TriggerSource varchar(20)   NULL,
        IsChangeMode  varchar(2)    NULL,
        Status        varchar(20)   NOT NULL,
        Message       nvarchar(500) NULL,
        InstanceId    varchar(100)  NULL,
        GeneratedAt   datetime      NOT NULL CONSTRAINT DF_MasterDataGenerationLog_At DEFAULT(GETDATE())
    );

    CREATE INDEX IX_MasterDataGenerationLog_Site_At
        ON dbo.MasterDataGenerationLog (StoreNo, PosNo, GeneratedAt);

    CREATE INDEX IX_MasterDataGenerationLog_At
        ON dbo.MasterDataGenerationLog (GeneratedAt);

    CREATE INDEX IX_MasterDataGenerationLog_File
        ON dbo.MasterDataGenerationLog (FileName);
END
GO
