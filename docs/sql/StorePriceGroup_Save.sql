/* ============================================================================
   Danh mục nhóm giá — SP lưu (thêm/sửa) 1 nhóm giá + danh sách cửa hàng — RPOSMasterData
   Dùng bởi dialog "Danh mục nhóm giá" (PriceGroupSetupDialog) trong menu Giá bán.

   NGHIỆP VỤ (port tham chiếu VCM.BLUEPOS PriceData.SaveStorePriceGroup):
     - Header: bảng dbo.StorePriceGroupHeader (1 dòng/nhóm) — upsert theo PriceGroupCode (UNIQUE).
     - Chi tiết: bảng dbo.StorePriceGroup (N dòng store/nhóm) — Pkey = '{Store}-{PriceGroupCode}'.
     - Priority áp CẤP NHÓM (mọi dòng store cùng nhóm dùng chung 1 Priority người dùng nhập).
     - ADD-ONLY: chỉ INSERT store chưa có + UPDATE Priority/PriceGroupName/Counter cho store đã có.
       KHÔNG DELETE dòng store (không bỏ được store đã gán — theo quyết định nghiệp vụ).
     - Type = 1 cố định. Counter = ReplicationCounter = MAX(Counter)+1 toàn bảng StorePriceGroup
       (tính 1 lần cho cả batch — cơ chế client polling/replication tăng dần).

   Kết quả trả qua OUTPUT param @Ok bit + @Message nvarchar(400). Lỗi → @Ok=0 + ERROR_MESSAGE(),
   KHÔNG throw ra ngoài (repository đọc output param).
   ----------------------------------------------------------------------------
   CHẠY 1 LẦN trên RPOSMasterData. Script tự DROP PROCEDURE cũ nếu đã tồn tại.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* 1) TVP danh sách cửa hàng ------------------------------------------------- */
IF TYPE_ID(N'dbo.StorePriceGroupStoreTVP') IS NULL
CREATE TYPE dbo.StorePriceGroupStoreTVP AS TABLE
(
    Store nvarchar(10) NOT NULL
);
GO

/* 2) SP lưu nhóm giá -------------------------------------------------------- */
IF OBJECT_ID(N'dbo.usp_StorePriceGroup_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_StorePriceGroup_Save;
GO

CREATE PROCEDURE dbo.usp_StorePriceGroup_Save
(
    @PriceGroupCode varchar(20),
    @PriceGroupName nvarchar(200),
    @Priority       int,
    @Stores         dbo.StorePriceGroupStoreTVP READONLY,
    @UserName       nvarchar(50) = NULL,
    @Ok             bit           OUTPUT,
    @Message        nvarchar(400) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        /* Counter dùng chung cho cả batch (bump replication) */
        DECLARE @newCounter bigint =
            ISNULL((SELECT MAX([Counter]) FROM dbo.StorePriceGroup WITH (UPDLOCK, HOLDLOCK)), 0) + 1;

        /* ── Header: upsert theo PriceGroupCode ── */
        IF EXISTS (SELECT 1 FROM dbo.StorePriceGroupHeader WHERE PriceGroupCode = @PriceGroupCode)
        BEGIN
            UPDATE dbo.StorePriceGroupHeader
            SET    PriceGroupName = @PriceGroupName,
                   [Counter]      = @newCounter
            WHERE  PriceGroupCode = @PriceGroupCode;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.StorePriceGroupHeader (PriceGroupCode, PriceGroupName, [Counter], Pkey)
            VALUES (@PriceGroupCode, @PriceGroupName, @newCounter, @PriceGroupCode);
        END

        /* ── Chi tiết: chuẩn hóa danh sách store (distinct, bỏ rỗng) + Pkey ── */
        DECLARE @src TABLE (Store nvarchar(10) NOT NULL, Pkey varchar(50) NOT NULL);
        INSERT INTO @src (Store, Pkey)
        SELECT DISTINCT LTRIM(RTRIM(s.Store)),
               LTRIM(RTRIM(s.Store)) + '-' + @PriceGroupCode
        FROM   @Stores s
        WHERE  ISNULL(LTRIM(RTRIM(s.Store)), '') <> '';

        /* UPDATE các store đã tồn tại (đồng bộ Priority/Name + bump Counter) */
        UPDATE sg
        SET    sg.[Priority]         = @Priority,
               sg.PriceGroupName     = @PriceGroupName,
               sg.[Counter]          = @newCounter,
               sg.ReplicationCounter = @newCounter
        FROM   dbo.StorePriceGroup sg
        JOIN   @src x ON x.Pkey = sg.Pkey;

        /* INSERT các store chưa có (add-only) */
        INSERT INTO dbo.StorePriceGroup
            (Store, PriceGroupCode, [Type], [Priority], ReplicationCounter, [Counter], Pkey, PriceGroupName)
        SELECT x.Store, @PriceGroupCode, 1, @Priority, @newCounter, @newCounter, x.Pkey, @PriceGroupName
        FROM   @src x
        WHERE  NOT EXISTS (SELECT 1 FROM dbo.StorePriceGroup sg WHERE sg.Pkey = x.Pkey);

        COMMIT TRANSACTION;
        SET @Ok = 1;
        SET @Message = N'Lưu nhóm giá thành công';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @Ok = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO
