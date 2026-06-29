-- Migration 003: tạo bảng DashboardAuditLog
-- Database  : RPOSMasterData (ConnectionStrings:CentralMD)
-- Mục đích  : ghi lại mọi CRUD action của user trên POS Web
--             (POSDataSetup, DashboardUser, PosTerminal, Store...)
-- Idempotent : có thể chạy lại nhiều lần, không lỗi

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'DashboardAuditLog' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[DashboardAuditLog]
    (
        [Id]         BIGINT        IDENTITY(1,1) NOT NULL,
        [Actor]      NVARCHAR(100) NOT NULL,        -- username từ Identity.Name
        [Action]     NVARCHAR(20)  NOT NULL,        -- CREATE | UPDATE | DELETE
        [EntityType] NVARCHAR(50)  NOT NULL,        -- POSDataSetup | DashboardUser | ...
        [EntityKey]  NVARCHAR(200) NOT NULL,        -- PK của dòng bị tác động
        [OldValue]   NVARCHAR(MAX) NULL,            -- JSON state trước thay đổi (NULL khi CREATE)
        [NewValue]   NVARCHAR(MAX) NULL,            -- JSON state sau thay đổi (NULL khi DELETE)
        [ActedAt]    DATETIME2(3)  NOT NULL
            CONSTRAINT [DF_DashboardAuditLog_ActedAt] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_DashboardAuditLog] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Truy vấn theo thời gian (xem log gần nhất, range filter)
    CREATE NONCLUSTERED INDEX [IX_DashboardAuditLog_ActedAt]
        ON [dbo].[DashboardAuditLog] ([ActedAt] DESC);

    -- Truy vấn "user X đã làm gì gần đây"
    CREATE NONCLUSTERED INDEX [IX_DashboardAuditLog_Actor]
        ON [dbo].[DashboardAuditLog] ([Actor] ASC, [ActedAt] DESC);

    -- Truy vấn "ai đã sửa entity Y"
    CREATE NONCLUSTERED INDEX [IX_DashboardAuditLog_Entity]
        ON [dbo].[DashboardAuditLog] ([EntityType] ASC, [EntityKey] ASC, [ActedAt] DESC);

    PRINT 'DashboardAuditLog: bảng và 3 index đã tạo thành công.';
END
ELSE
BEGIN
    PRINT 'DashboardAuditLog: đã tồn tại, bỏ qua.';
END
