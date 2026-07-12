-- Migration 004: tạo bảng SqlAuditRun + SqlAuditFinding
-- Database  : RPOSMasterData (ConnectionStrings:CentralMD)
-- Mục đích  : lưu lịch sử các lần chạy SQL Stored Procedure Audit CLI (tools/POS.SqlAuditCli)
--             — kiểm kê/phân loại stored procedure, đề xuất migrate sang code hoặc dọn dẹp.
-- Idempotent : có thể chạy lại nhiều lần, không lỗi

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'SqlAuditRun' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[SqlAuditRun]
    (
        [RunId]                     BIGINT        IDENTITY(1,1) NOT NULL,
        [RunStartedUtc]             DATETIME2(3)  NOT NULL,
        [RunFinishedUtc]            DATETIME2(3)  NOT NULL,
        [DatabasesScanned]          NVARCHAR(200) NOT NULL,   -- "CentralMD,CentralSale,Loyalty"
        [TotalProcedures]           INT           NOT NULL,
        [MigrationCandidateCount]   INT           NOT NULL,
        [CleanupCandidateCount]     INT           NOT NULL,
        [ProceduresTruncated]       BIT           NOT NULL CONSTRAINT [DF_SqlAuditRun_Truncated] DEFAULT (0),
        [ErrorMessage]              NVARCHAR(MAX) NULL,       -- lỗi từng phần (1 DB lỗi không hỏng cả run)
        [CreatedUtc]                DATETIME2(3)  NOT NULL
            CONSTRAINT [DF_SqlAuditRun_CreatedUtc] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_SqlAuditRun] PRIMARY KEY CLUSTERED ([RunId] ASC)
    );

    -- Dashboard đọc lần chạy gần nhất
    CREATE NONCLUSTERED INDEX [IX_SqlAuditRun_RunFinishedUtc]
        ON [dbo].[SqlAuditRun] ([RunFinishedUtc] DESC);

    PRINT 'SqlAuditRun: bảng và index đã tạo thành công.';
END
ELSE
BEGIN
    PRINT 'SqlAuditRun: đã tồn tại, bỏ qua.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'SqlAuditFinding' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[SqlAuditFinding]
    (
        [FindingId]         BIGINT        IDENTITY(1,1) NOT NULL,
        [RunId]             BIGINT        NOT NULL,
        [SchemaName]        NVARCHAR(50)  NOT NULL,
        [ProcedureName]     NVARCHAR(200) NOT NULL,
        [DatabaseKey]       NVARCHAR(50)  NOT NULL,   -- CentralMD | CentralSale | Loyalty
        [Complexity]        NVARCHAR(20)  NOT NULL,   -- Simple | Moderate | Complex
        [ExecutionCount]    BIGINT        NOT NULL,
        [LastExecutionAt]   DATETIME2(3)  NULL,        -- giờ local server, không convert UTC
        [Recommendation]    NVARCHAR(30)  NOT NULL,    -- MigrationCandidate | CleanupCandidate | KeepAsIs
        [Note]              NVARCHAR(500) NULL,

        CONSTRAINT [PK_SqlAuditFinding] PRIMARY KEY CLUSTERED ([FindingId] ASC),
        CONSTRAINT [FK_SqlAuditFinding_SqlAuditRun] FOREIGN KEY ([RunId])
            REFERENCES [dbo].[SqlAuditRun] ([RunId])
    );

    -- Dashboard đọc toàn bộ finding của 1 run
    CREATE NONCLUSTERED INDEX [IX_SqlAuditFinding_RunId]
        ON [dbo].[SqlAuditFinding] ([RunId] ASC);

    PRINT 'SqlAuditFinding: bảng và index đã tạo thành công.';
END
ELSE
BEGIN
    PRINT 'SqlAuditFinding: đã tồn tại, bỏ qua.';
END
