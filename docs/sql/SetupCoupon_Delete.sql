/* ============================================================================
   8.1 Setup Coupon — SP xóa coupon — RPOSMasterData
   Guard: chỉ xóa khi CHƯA phát sinh mã (parity legacy DeleteItemCoupon).
   Xóa CpnVchBOMIssueRule + CpnVchBOMHeader (+ dọn Line/Store rác nếu có).
   Trả (Deleted bit, Message).
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_SetupCoupon_Delete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupCoupon_Delete;
GO

CREATE PROCEDURE dbo.usp_SetupCoupon_Delete
(
    @ItemNo  nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.CpnVchBOMIssueRule WHERE ItemNo = @ItemNo)
    BEGIN
        SELECT CAST(0 AS bit) AS Deleted, N'Không tìm thấy dữ liệu cần xóa' AS [Message];
        RETURN;
    END

    DECLARE @qty int = (SELECT COUNT(*) FROM dbo.CpnVchBOMCodeIssue (NOLOCK) WHERE ItemNo = @ItemNo);
    IF @qty > 0
    BEGIN
        SELECT CAST(0 AS bit) AS Deleted,
               N'Item này ' + @ItemNo + N' đã tồn tại ' + CONVERT(nvarchar(20), @qty) + N' mã coupon' AS [Message];
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM dbo.CpnVchBOMLine       WHERE ItemNo = @ItemNo;
        DELETE FROM dbo.CpnVchBOMStore      WHERE ItemNo = @ItemNo;
        DELETE FROM dbo.CpnVchBOMIssueRule  WHERE ItemNo = @ItemNo;
        DELETE FROM dbo.CpnVchBOMHeader     WHERE ItemNo = @ItemNo;

        COMMIT TRANSACTION;
        SELECT CAST(1 AS bit) AS Deleted, N'Success' AS [Message];
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SELECT CAST(0 AS bit) AS Deleted, ERROR_MESSAGE() AS [Message];
    END CATCH
END
GO
