-- Chạy trên DB: RPOSMasterData (ConnectionStrings:CentralMD)
-- Tạo bảng DashboardUsers cho Blazor Dashboard app
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DashboardUsers' AND xtype='U')
CREATE TABLE DashboardUsers (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    FullName     NVARCHAR(200) NOT NULL,
    Role         NVARCHAR(50)  NOT NULL,
    -- 'StoreOperator' | 'ITOps' | 'SystemAdmin'
    StoreCodes   NVARCHAR(MAX) NULL,
    -- NULL = xem tất cả; JSON array string: '["S001","S002"]' = chỉ xem các store này
    IsActive     BIT NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt    DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT UQ_DashboardUsers_Username UNIQUE (Username)
);

-- Seed admin mặc định — BCrypt.Net-Next workfactor 11, password: Admin@0987
IF NOT EXISTS (SELECT 1 FROM DashboardUsers WHERE Username = 'admin')
INSERT INTO DashboardUsers (Username, PasswordHash, FullName, Role, StoreCodes)
VALUES ('admin', '$2a$11$ZKuiKzkGYaoXbIFpuog1ZOKsGOLTk.w/1L/wrFiRUTitQfQC7t9k.', N'Quản trị viên hệ thống', 'SystemAdmin', NULL);
