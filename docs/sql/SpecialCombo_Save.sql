/* ============================================================================
   11.2 Special Combo — SP lưu (header + lines + stores) — RPOSMasterData
   Chiến lược REPLACE-ON-SAVE: upsert header, xóa-rồi-chèn lại Lines/Stores theo Code.
   PriceMode (TVP): 0 = thường, 1 = mặc định (giá tĩnh), 2 = mặc định + giá động.
     → IsDefault      = CASE WHEN PriceMode = 0 THEN 0 ELSE 1
       IsDynamicPrice = CASE WHEN PriceMode = 2 THEN 1 ELSE 0
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF TYPE_ID(N'dbo.SpecialComboLineTVP') IS NULL
CREATE TYPE dbo.SpecialComboLineTVP AS TABLE
(
    GroupCode        nvarchar(50)   NULL,
    GroupName        nvarchar(200)  NULL,
    ItemNo           nvarchar(50)   NULL,
    ItemName         nvarchar(250)  NULL,
    UOM              nvarchar(50)   NULL,
    MinimumQuantity  decimal(18,3)  NULL,
    MaximumQuantity  decimal(18,3)  NULL,
    [Order]          int            NULL,
    PriceMode        int            NULL,
    IsRequired       bit            NULL,
    IsSendSAP        bit            NULL
);
GO

IF TYPE_ID(N'dbo.SpecialComboStoreTVP') IS NULL
CREATE TYPE dbo.SpecialComboStoreTVP AS TABLE
(
    StoreNo   nvarchar(50)  NULL,
    IsEnable  bit           NULL
);
GO

IF OBJECT_ID(N'dbo.usp_SpecialCombo_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SpecialCombo_Save;
GO

CREATE PROCEDURE dbo.usp_SpecialCombo_Save
(
    @Code          nvarchar(50),         -- repository tự gen khi tạo mới
    @SalesType     nvarchar(50),
    @Name          nvarchar(250),
    @ComboQuantity decimal(18,3),
    @Amount        decimal(18,3),
    @IsMember      bit,
    @MemberCode    nvarchar(50),
    @FromDate      datetime,
    @ToDate        datetime,
    @IsEnable      bit,
    @Actor         nvarchar(100),
    @Lines         dbo.SpecialComboLineTVP  READONLY,
    @Stores        dbo.SpecialComboStoreTVP READONLY
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @now datetime = GETDATE();

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1) Upsert HEADER
        IF EXISTS (SELECT 1 FROM dbo.SpecialComboHeader WHERE Code = @Code)
        BEGIN
            DECLARE @cnt bigint = (SELECT ISNULL(MAX(Counter),0) FROM dbo.SpecialComboHeader);
            UPDATE dbo.SpecialComboHeader
            SET    SalesType     = @SalesType,
                   Name          = @Name,
                   ComboQuantity = @ComboQuantity,
                   Amount        = @Amount,
                   IsMember      = @IsMember,
                   MemberCode    = @MemberCode,
                   FromDate      = @FromDate,
                   ToDate        = @ToDate,
                   IsEnable      = @IsEnable,
                   Counter       = @cnt + 1,
                   UpdatedDate   = @now,
                   UpdatedBy     = @Actor
            WHERE  Code = @Code;
        END
        ELSE
        BEGIN
            DECLARE @cntNew bigint = (SELECT ISNULL(MAX(Counter),0) FROM dbo.SpecialComboHeader);
            INSERT INTO dbo.SpecialComboHeader
                (Code, SalesType, Name, ComboQuantity, Amount, IsMember, MemberCode,
                 FromDate, ToDate, IsEnable, Pkey, Counter, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy)
            VALUES
                (@Code, @SalesType, @Name, @ComboQuantity, @Amount, @IsMember, @MemberCode,
                 @FromDate, @ToDate, @IsEnable, @Code, @cntNew + 1, @now, @Actor, @now, @Actor);
        END

        -- 2) LINES: replace
        DELETE FROM dbo.SpecialComboLine WHERE Code = @Code;
        INSERT INTO dbo.SpecialComboLine
            (Code, ItemNo, ItemName, UOM, GroupCode, GroupName, IsRequired, IsDefault, IsSendSAP,
             MinimumQuantity, MaximumQuantity, [Order], IsEnable, IsDynamicPrice, Pkey, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy)
        SELECT @Code, ISNULL(l.ItemNo,''), ISNULL(l.ItemName,''), ISNULL(l.UOM,''),
               ISNULL(l.GroupCode,''), ISNULL(l.GroupName,''),
               ISNULL(l.IsRequired,0),
               CASE WHEN l.PriceMode = 0 THEN 0 ELSE 1 END,   -- IsDefault
               ISNULL(l.IsSendSAP,1),
               l.MinimumQuantity, l.MaximumQuantity, l.[Order], 1,
               CASE WHEN l.PriceMode = 2 THEN 1 ELSE 0 END,   -- IsDynamicPrice
               @Code + '-' + ISNULL(l.GroupCode,'') + '-' + ISNULL(l.ItemNo,''),
               @now, @Actor, @now, @Actor
        FROM @Lines l;

        -- 3) STORES: replace
        DELETE FROM dbo.SpecialComboStore WHERE Code = @Code;
        INSERT INTO dbo.SpecialComboStore
            (Code, StoreNo, IsEnable, Pkey, CreatedDate, CreatedBy, UpdatedDate, UpdatedBy)
        SELECT @Code, s.StoreNo, ISNULL(s.IsEnable,1),
               @Code + '-' + s.StoreNo, @now, @Actor, @now, @Actor
        FROM @Stores s;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
