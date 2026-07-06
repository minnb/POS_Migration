/* ============================================================================
   8.3 Phát hành THÊM voucher (nhiều lô) — SP thêm 1 lô mã Auto MỚI vào voucher
   ĐÃ TỒN TẠI, KHÔNG đổi header, KHÔNG guard tồn tại (khác usp_SetupVoucher_SaveIssue
   — SP đó chỉ insert mã 1 lần duy nhất, gọi lại với ItemNo đã có mã sẽ bị bỏ qua).
   RPOSMasterData (CentralMD). Chỉ hỗ trợ Auto — KHÔNG áp dụng cho Import.
   Tái dùng dbo.CouponCodeTVP đã tạo ở SetupVoucher_SaveIssue.sql.
   CHẠY 1 LẦN trên RPOSMasterData (dev/UAT/PROD).
   ============================================================================ */
USE [RPOSMasterData];
GO

IF TYPE_ID(N'dbo.CouponCodeTVP') IS NULL
CREATE TYPE dbo.CouponCodeTVP AS TABLE
(
    Code  nvarchar(50)  NULL
);
GO

IF OBJECT_ID(N'dbo.usp_SetupVoucher_IssueMore', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_IssueMore;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_IssueMore
(
    @ItemNo           nvarchar(20),          -- BẮT BUỘC đã tồn tại — không tạo header mới
    @Codes            dbo.CouponCodeTVP READONLY,
    @OutQuantityAdded int OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo)
        ;THROW 50002, N'Không tìm thấy voucher (ItemNo không tồn tại)', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @now datetime = GETDATE();
        DECLARE @cntCode bigint = (SELECT ISNULL(MAX(Counter), 0) FROM dbo.CpnVchBOMCodeIssue);

        -- Counter dùng CHUNG cho cả lô (giống usp_SetupVoucher_SaveIssue — "@cntCode+1/lô",
        -- KHÔNG tăng theo từng dòng — khớp mô tả Counter trong database-schema.md).
        INSERT INTO dbo.CpnVchBOMCodeIssue
            (ItemNo, Code, Enabled, CreatedDate, Counter, Pkey, Source,
             ActicleNo, ActicleType, Validity_From_Date, Expiry_Date, Voucher_Currency,
             CompanyCode, Status, [Return], Value, VoucherType)
        SELECT @ItemNo, t.Code, 1, @now, @cntCode + 1, @ItemNo, 'VOUCHER',
               @ItemNo, h.ArticleType, CONVERT(date, h.StartingDate), CONVERT(date, h.EndingDate),
               'VND', 'WCM', 'SOLD', 0, CAST(h.DiscountValue AS decimal(18,2)), h.ArticleType
        FROM   @Codes t
        CROSS JOIN (SELECT ArticleType, StartingDate, EndingDate, DiscountValue
                     FROM dbo.CpnVchBOMHeader WHERE ItemNo = @ItemNo) h
        WHERE  ISNULL(t.Code, '') <> '';

        SET @OutQuantityAdded = @@ROWCOUNT;

        UPDATE dbo.CpnVchBOMHeader
        SET    LastDateModified = @now, Counter = @cntCode + 1
        WHERE  ItemNo = @ItemNo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
