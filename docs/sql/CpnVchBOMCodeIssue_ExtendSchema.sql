/* ============================================================================
   Gộp Internal_Voucher vào CpnVchBOMCodeIssue — mở rộng schema — RPOSMasterData

   Mục tiêu: CpnVchBOMCodeIssue trở thành bảng dùng CHUNG cho 2 nguồn dữ liệu,
   phân biệt bằng cột Source:
     - Source = 'COUPON' : mã coupon nội bộ, phát hành hàng loạt qua POS.Web
                           (usp_SetupCoupon_SaveIssue) — hành vi cũ giữ nguyên.
     - Source = 'SAP'    : voucher SAP tích hợp ERP, real-time qua POS.Api
                           (usp_Voucher_*) — thay thế bảng Internal_Voucher cũ.

   Chạy 1 LẦN trên RPOSMasterData, THEO ĐÚNG THỨ TỰ Phase A → B → C dưới đây.
   Backup bảng trước khi chạy (đặc biệt Phase B — rebuild bảng).
   Sau script này: chạy tiếp SetupCoupon_Save.sql, SetupCoupon_Read.sql (bản đã sửa),
   Voucher_Read.sql, Voucher_Save.sql, rồi mới tới CpnVchBOMCodeIssue_MigrateFromInternalVoucher.sql.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ============================================================================
   PHASE A — thêm cột mới (online, không khoá bảng lâu)
   ============================================================================ */
ALTER TABLE dbo.CpnVchBOMCodeIssue ALTER COLUMN Code varchar(50) NULL;
GO

ALTER TABLE dbo.CpnVchBOMCodeIssue ADD
    Source              varchar(10)     NOT NULL CONSTRAINT DF_CpnVchBOMCodeIssue_Source DEFAULT ('COUPON'),
    Status              varchar(20)     NULL,
    [Return]            int             NULL,
    ActicleNo           varchar(50)     NULL,
    ActicleType         varchar(20)     NULL,
    Value               decimal(18,2)   NULL,
    Voucher_Currency    varchar(10)     NULL,
    Validity_From_Date  date            NULL,
    Expiry_Date         date            NULL,
    CompanyCode         varchar(20)     NULL,
    Partner             varchar(50)     NULL,
    IsEmployee          bit             NULL,
    PhoneNumber         varchar(20)     NULL,
    VoucherType         varchar(50)     NULL,
    AmountUsed          decimal(18,2)   NULL,
    OrderUsed           nvarchar(50)    NULL;
GO

ALTER TABLE dbo.CpnVchBOMCodeIssue
    ADD CONSTRAINT CK_CpnVchBOMCodeIssue_Source CHECK (Source IN ('COUPON', 'SAP'));
GO

/* ============================================================================
   PHASE B — rebuild bảng để thêm IDENTITY + PK CLUSTERED trên ID
   (không thể ALTER COLUMN thêm IDENTITY vào cột đã có dữ liệu)
   Tái dùng đúng pattern PK_Internal_Voucher đang chạy ổn định với traffic SAP thật.
   ============================================================================ */
CREATE TABLE dbo.CpnVchBOMCodeIssue_New
(
    ID                  bigint          IDENTITY(1,1) NOT NULL,
    ItemNo              varchar(20)     NULL,
    Code                varchar(50)     NULL,
    EmployeeCode        varchar(30)     NULL,
    Enabled             bit             NULL,
    CreatedDate         datetime        NULL,
    Counter             bigint          NULL,
    Pkey                varchar(50)     NULL,
    Source              varchar(10)     NOT NULL CONSTRAINT DF_CpnVchBOMCodeIssue_New_Source DEFAULT ('COUPON'),
    Status              varchar(20)     NULL,
    [Return]            int             NULL,
    ActicleNo           varchar(50)     NULL,
    ActicleType         varchar(20)     NULL,
    Value               decimal(18,2)   NULL,
    Voucher_Currency    varchar(10)     NULL,
    Validity_From_Date  date            NULL,
    Expiry_Date         date            NULL,
    CompanyCode         varchar(20)     NULL,
    Partner             varchar(50)     NULL,
    IsEmployee          bit             NULL,
    PhoneNumber         varchar(20)     NULL,
    VoucherType         varchar(50)     NULL,
    AmountUsed          decimal(18,2)   NULL,
    OrderUsed           nvarchar(50)    NULL,
    CONSTRAINT PK_CpnVchBOMCodeIssue PRIMARY KEY CLUSTERED (ID ASC)
);
GO

SET IDENTITY_INSERT dbo.CpnVchBOMCodeIssue_New ON;
GO

INSERT INTO dbo.CpnVchBOMCodeIssue_New
    (ID, ItemNo, Code, EmployeeCode, Enabled, CreatedDate, Counter, Pkey, Source, Status,
     [Return], ActicleNo, ActicleType, Value, Voucher_Currency, Validity_From_Date, Expiry_Date,
     CompanyCode, Partner, IsEmployee, PhoneNumber, VoucherType, AmountUsed, OrderUsed)
SELECT ID, ItemNo, Code, EmployeeCode, Enabled, CreatedDate, Counter, Pkey, Source, Status,
       [Return], ActicleNo, ActicleType, Value, Voucher_Currency, Validity_From_Date, Expiry_Date,
       CompanyCode, Partner, IsEmployee, PhoneNumber, VoucherType, AmountUsed, OrderUsed
FROM dbo.CpnVchBOMCodeIssue;
GO

SET IDENTITY_INSERT dbo.CpnVchBOMCodeIssue_New OFF;
GO

DBCC CHECKIDENT ('dbo.CpnVchBOMCodeIssue_New', RESEED, (SELECT ISNULL(MAX(ID), 0) FROM dbo.CpnVchBOMCodeIssue_New));
GO

EXEC sp_rename 'dbo.CpnVchBOMCodeIssue', 'CpnVchBOMCodeIssue_OldHeap';
GO
EXEC sp_rename 'dbo.CpnVchBOMCodeIssue_New', 'CpnVchBOMCodeIssue';
GO
EXEC sp_rename 'dbo.DF_CpnVchBOMCodeIssue_New_Source', 'DF_CpnVchBOMCodeIssue_Source';
GO

-- Sau khi verify (COUNT(*) 2 bảng khớp, spot-check vài dòng) mới DROP bảng heap cũ:
-- DROP TABLE dbo.CpnVchBOMCodeIssue_OldHeap;

/* ============================================================================
   PHASE C — UNIQUE index trên Code (khóa nghiệp vụ thật, tránh double coupon/voucher)
   Chạy pre-check trước — phải rỗng mới được tạo index:
   SELECT Code, COUNT(*) FROM dbo.CpnVchBOMCodeIssue WHERE Code IS NOT NULL
   GROUP BY Code HAVING COUNT(*) > 1;
   ============================================================================ */
CREATE UNIQUE NONCLUSTERED INDEX UX_CpnVchBOMCodeIssue_Code
    ON dbo.CpnVchBOMCodeIssue (Code)
    WHERE Code IS NOT NULL;
GO
