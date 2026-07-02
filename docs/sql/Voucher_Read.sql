/* ============================================================================
   SAP Internal Voucher — SP đọc voucher theo Code — RPOSMasterData
   Bảng dùng chung dbo.CpnVchBOMCodeIssue (Source='SAP'|'COUPON'). Không lọc theo Source — Code
   đã unique toàn bảng, và mã Coupon (POS.Web phát hành) cũng cần check được qua endpoint này
   (api/sap/CheckVoucher) vì 2 luồng liên thông nhau.
   Port từ SAPVoucherRepository.GetByVoucherNumberAsync (raw SQL trên Internal_Voucher cũ).
   CHẠY 1 LẦN trên RPOSMasterData, SAU KHI đã chạy CpnVchBOMCodeIssue_ExtendSchema.sql VÀ
   CpnVchBOMCodeIssue_ItemNoHardening.sql.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_Voucher_GetByCode', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Voucher_GetByCode;
GO

CREATE PROCEDURE dbo.usp_Voucher_GetByCode
(
    @Code varchar(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        [Status],
        [Return]              = CAST([Return] AS VARCHAR),
        [ActicleNo],
        [ActicleType],
        [VoucherNumber]       = [Code],
        [Value]               = CAST([Value] AS VARCHAR),
        [Voucher_Currency],
        [Validity_From_Date]  = CONVERT(VARCHAR, Validity_From_Date, 103),
        [Expiry_Date]         = CONVERT(VARCHAR, Expiry_Date, 103),
        [CompanyCode],
        [Partner],
        [IsEmployee],
        [PhoneNumber],
        [VoucherType],
        [AmountUsed],
        [OrderUsed]
    FROM dbo.CpnVchBOMCodeIssue (NOLOCK)
    WHERE Code = @Code;
END
GO
