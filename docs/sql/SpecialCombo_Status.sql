/* ============================================================================
   11.2 Special Combo — SP bật/tắt + xóa — RPOSMasterData
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Bật/tắt combo (IsEnable header) ────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SpecialCombo_UpdateStatus', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SpecialCombo_UpdateStatus;
GO

CREATE PROCEDURE dbo.usp_SpecialCombo_UpdateStatus
(
    @Code     nvarchar(50),
    @IsEnable bit,
    @Actor    nvarchar(100) = ''
)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.SpecialComboHeader
    SET    IsEnable = @IsEnable, UpdatedDate = GETDATE(), UpdatedBy = @Actor
    WHERE  Code = @Code;
END
GO

/* ── Xóa combo (header + lines + stores) ────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_SpecialCombo_Delete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SpecialCombo_Delete;
GO

CREATE PROCEDURE dbo.usp_SpecialCombo_Delete
(
    @Code  nvarchar(50)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;
        DELETE FROM dbo.SpecialComboLine  WHERE Code = @Code;
        DELETE FROM dbo.SpecialComboStore WHERE Code = @Code;
        DELETE FROM dbo.SpecialComboHeader WHERE Code = @Code;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
