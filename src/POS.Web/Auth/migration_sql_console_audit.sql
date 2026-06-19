-- Migration: tạo bảng audit log cho SQL Console (UPDATE actions)
-- Chạy trên database: RPOSMasterData
-- Ngày tạo: 2026-06-19

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'SqlConsoleAuditLog'
)
BEGIN
    CREATE TABLE SqlConsoleAuditLog (
        Id           BIGINT        IDENTITY(1,1) PRIMARY KEY,
        Actor        NVARCHAR(100) NOT NULL,        -- username người chạy
        DbKey        NVARCHAR(50)  NOT NULL,        -- CentralSale, CentralMD, ...
        DbCatalog    NVARCHAR(100) NOT NULL,        -- RPOSCentralSales, RPOSMasterData, ...
        SqlText      NVARCHAR(MAX) NOT NULL,        -- câu SQL đầy đủ
        RowsAffected INT           NOT NULL,
        HasWhere     BIT           NOT NULL,
        Status       NVARCHAR(20)  NOT NULL,        -- COMMITTED / ROLLED_BACK / AUTO_ROLLBACK
        ElapsedMs    BIGINT        NOT NULL,        -- thời gian chạy UPDATE (ms)
        ExecutedAt   DATETIME2     NOT NULL,        -- khi BEGIN transaction + chạy UPDATE
        DecidedAt    DATETIME2     NOT NULL DEFAULT GETDATE()  -- khi Commit/Rollback xảy ra
    );

    CREATE INDEX IX_SqlConsoleAuditLog_Actor
        ON SqlConsoleAuditLog (Actor);

    CREATE INDEX IX_SqlConsoleAuditLog_ExecutedAt
        ON SqlConsoleAuditLog (ExecutedAt DESC);

    CREATE INDEX IX_SqlConsoleAuditLog_Status
        ON SqlConsoleAuditLog (Status);

    PRINT 'SqlConsoleAuditLog created.';
END
ELSE
BEGIN
    PRINT 'SqlConsoleAuditLog already exists, skipped.';
END
