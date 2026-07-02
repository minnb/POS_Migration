/* ============================================================================
   SAP Internal Voucher — SP tạo mới (idempotent) + redeem hàng loạt — RPOSMasterData
   Bảng dùng chung dbo.CpnVchBOMCodeIssue (Source='SAP'|'COUPON'). Port từ SAPVoucherRepository
   (InsertAsync/RedeemVouchersAsync trên Internal_Voucher cũ), giữ NGUYÊN business rule +
   message tiếng Việt để không đổi hành vi trả về cho POS/SAP.

   usp_Voucher_Redeem KHÔNG còn lọc theo Source — redeem áp dụng chung cho cả SAP Voucher lẫn
   Coupon (POS.Web phát hành), vì 2 luồng liên thông nhau (POS dùng cả 2 qua cùng endpoint
   api/sap/winlife/redeemCpnVch). usp_Voucher_Create vẫn giữ Source='SAP' khi tạo mới (đã có
   guard chặn trùng Code khác Source ở trên).

   CHẠY 1 LẦN trên RPOSMasterData, SAU KHI đã chạy CpnVchBOMCodeIssue_ExtendSchema.sql VÀ
   CpnVchBOMCodeIssue_ItemNoHardening.sql (mở rộng ItemNo varchar(50) — cần thiết vì
   usp_Voucher_Create nay set ItemNo=@ActicleNo).
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── TVP cho redeem hàng loạt ────────────────────────────────────────────── */
IF TYPE_ID(N'dbo.VoucherRedeemTVP') IS NULL
CREATE TYPE dbo.VoucherRedeemTVP AS TABLE
(
    Code          varchar(50)     NULL,
    AmountRedeem  decimal(18,2)   NULL
);
GO

/* ── Tạo mới voucher SAP (idempotent — đã tồn tại thì trả bản ghi cũ, không lỗi) ── */
IF OBJECT_ID(N'dbo.usp_Voucher_Create', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Voucher_Create;
GO

CREATE PROCEDURE dbo.usp_Voucher_Create
(
    @Code                varchar(50),
    @ActicleNo           varchar(50)   = NULL,
    @ActicleType         varchar(20)   = NULL,
    @Value               varchar(50)   = NULL,   -- input string (khớp kiểu DTO C#), CAST sang decimal khi insert
    @Voucher_Currency    varchar(10)   = NULL,
    @Validity_From_Date  varchar(20)   = NULL,   -- input "dd/MM/yyyy"
    @Expiry_Date         varchar(20)   = NULL,   -- input "dd/MM/yyyy"
    @CompanyCode         varchar(20)   = NULL,
    @Partner             varchar(50)   = NULL,
    @PhoneNumber         varchar(20)   = NULL,
    @VoucherType         varchar(50)   = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Code đã UNIQUE toàn bảng (UX_CpnVchBOMCodeIssue_Code, dùng chung Coupon+SAP Voucher) —
        -- nếu Code này đang tồn tại ở NGUỒN KHÁC (Coupon), phải chặn rõ ràng ở đây bằng message
        -- nghiệp vụ, nếu không INSERT bên dưới sẽ vi phạm unique index và ném SqlException thô.
        IF EXISTS (
            SELECT 1 FROM dbo.CpnVchBOMCodeIssue WITH (UPDLOCK, HOLDLOCK)
            WHERE Code = @Code AND Source <> 'SAP'
        )
        BEGIN
            DECLARE @conflictMsg nvarchar(200) =
                N'Mã ' + @Code + N' đã tồn tại dưới dạng mã coupon nội bộ, không thể tạo voucher SAP trùng mã';
            THROW 51000, @conflictMsg, 1;
        END

        IF NOT EXISTS (
            SELECT 1 FROM dbo.CpnVchBOMCodeIssue WITH (UPDLOCK, HOLDLOCK)
            WHERE Code = @Code AND Source = 'SAP'
        )
        BEGIN
            -- ItemNo = ActicleNo (mirror) — cho phép tra cứu chéo theo ItemNo bất kể nguồn gốc
            -- Coupon hay SAP Voucher (xem usp_SetupCoupon_SaveIssue Section 3a set ActicleNo=ItemNo
            -- theo chiều ngược lại). Đòi hỏi ItemNo đã mở rộng varchar(50), xem
            -- CpnVchBOMCodeIssue_ItemNoHardening.sql — PHẢI chạy trước script này.
            INSERT INTO dbo.CpnVchBOMCodeIssue
                (ItemNo, Code, Enabled, CreatedDate, Source, Status, [Return], ActicleNo, ActicleType,
                 Value, Voucher_Currency, Validity_From_Date, Expiry_Date, CompanyCode, Partner,
                 PhoneNumber, VoucherType)
            VALUES
                (@ActicleNo, @Code, 1, GETDATE(), 'SAP', 'SOLD', 0, @ActicleNo, @ActicleType,
                 TRY_CONVERT(decimal(18,2), @Value), @Voucher_Currency,
                 TRY_CONVERT(date, @Validity_From_Date, 103),
                 TRY_CONVERT(date, @Expiry_Date, 103),
                 @CompanyCode, @Partner, @PhoneNumber, @VoucherType);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    -- Luôn trả lại đúng 1 dòng hiện tại (dù vừa tạo hay đã tồn tại từ trước) — Repository
    -- map VoucherStatusResponse đọc thật từ DB, không dùng object C# chưa xác nhận ghi.
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
    WHERE Code = @Code AND Source = 'SAP';
END
GO

/* ── Redeem hàng loạt (transaction UPDLOCK, giữ nguyên message tiếng Việt) ───── */
IF OBJECT_ID(N'dbo.usp_Voucher_Redeem', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_Voucher_Redeem;
GO

CREATE PROCEDURE dbo.usp_Voucher_Redeem
(
    @Lines                dbo.VoucherRedeemTVP READONLY,
    @OrderNo              nvarchar(50),
    @RequiredVoucherType  varchar(50) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Success   bit = 0;
    DECLARE @Message   nvarchar(400) = N'';
    DECLARE @LineCount int = (SELECT COUNT(*) FROM @Lines);

    DECLARE @Found TABLE
    (
        Code varchar(50), Status varchar(20), VoucherType varchar(50), Value decimal(18,2)
    );

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Không lọc theo Source — Code đã unique toàn bảng (UX_CpnVchBOMCodeIssue_Code), redeem
        -- áp dụng chung cho cả Coupon (POS.Web phát hành) và SAP Voucher (POS.Api tạo).
        INSERT INTO @Found (Code, Status, VoucherType, Value)
        SELECT c.Code, c.Status, c.VoucherType, c.Value
        FROM dbo.CpnVchBOMCodeIssue c WITH (UPDLOCK, HOLDLOCK)
        WHERE c.Code IN (SELECT Code FROM @Lines);

        IF (SELECT COUNT(*) FROM @Found) <> @LineCount
        BEGIN
            SET @Message = N'Một hoặc nhiều voucher không tồn tại trong hệ thống';
            ROLLBACK TRANSACTION;
        END
        ELSE IF EXISTS (SELECT 1 FROM @Found WHERE Status <> 'SOLD')
        BEGIN
            SELECT TOP 1 @Message = N'Voucher ' + Code + N' không ở trạng thái SOLD (hiện tại: ' + ISNULL(Status, '') + N')'
            FROM @Found WHERE Status <> 'SOLD';
            ROLLBACK TRANSACTION;
        END
        ELSE IF @RequiredVoucherType IS NOT NULL AND EXISTS (SELECT 1 FROM @Found WHERE VoucherType <> @RequiredVoucherType)
        BEGIN
            SELECT TOP 1 @Message = N'Voucher ' + Code + N' không phải loại ' + @RequiredVoucherType + N' (hiện tại: ' + ISNULL(VoucherType, '') + N')'
            FROM @Found WHERE VoucherType <> @RequiredVoucherType;
            ROLLBACK TRANSACTION;
        END
        -- "f.Value IS NULL" chặn luôn trường hợp mệnh giá chưa xác định (vd coupon chưa từng
        -- chạy usp_SetupCoupon_SaveAdvanced) — so sánh "AmountRedeem > NULL" luôn UNKNOWN nên
        -- không tự chặn được, phải liệt kê tường minh.
        ELSE IF EXISTS (
            SELECT 1 FROM @Found f JOIN @Lines l ON l.Code = f.Code
            WHERE l.AmountRedeem < 0 OR f.Value IS NULL OR l.AmountRedeem > f.Value
        )
        BEGIN
            SELECT TOP 1 @Message = N'Voucher ' + f.Code + N': số tiền redeem ' + CONVERT(nvarchar(50), l.AmountRedeem) +
                   N' không hợp lệ (mệnh giá: ' + ISNULL(CONVERT(nvarchar(50), f.Value), N'chưa xác định') + N')'
            FROM @Found f JOIN @Lines l ON l.Code = f.Code
            WHERE l.AmountRedeem < 0 OR f.Value IS NULL OR l.AmountRedeem > f.Value;
            ROLLBACK TRANSACTION;
        END
        ELSE
        BEGIN
            -- Enabled=0 (MỚI, áp dụng cả 2 Source) — đồng bộ hiển thị "Locked" ở
            -- usp_SetupCoupon_GetCodes (tab "Mã coupon đã phát hành", POS.Web), vì SP đó chỉ đọc
            -- cột Enabled chứ không đọc Status.
            UPDATE c
            SET c.Status = 'RDM', c.AmountUsed = l.AmountRedeem, c.OrderUsed = @OrderNo, c.Enabled = 0
            FROM dbo.CpnVchBOMCodeIssue c
            JOIN @Lines l ON l.Code = c.Code;

            SET @Success = 1;
            SET @Message = N'OK';
            COMMIT TRANSACTION;
        END
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @Success AS Success, @Message AS Message;

    IF @Success = 1
    BEGIN
        SELECT
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
        WHERE Code IN (SELECT Code FROM @Lines);
    END
    ELSE
    BEGIN
        SELECT TOP 0
            CAST(NULL AS varchar(20))   AS [Status],
            CAST(NULL AS varchar(10))   AS [Return],
            CAST(NULL AS varchar(50))   AS [ActicleNo],
            CAST(NULL AS varchar(20))   AS [ActicleType],
            CAST(NULL AS varchar(50))   AS [VoucherNumber],
            CAST(NULL AS varchar(50))   AS [Value],
            CAST(NULL AS varchar(10))   AS [Voucher_Currency],
            CAST(NULL AS varchar(10))   AS [Validity_From_Date],
            CAST(NULL AS varchar(10))   AS [Expiry_Date],
            CAST(NULL AS varchar(20))   AS [CompanyCode],
            CAST(NULL AS varchar(50))   AS [Partner],
            CAST(NULL AS bit)           AS [IsEmployee],
            CAST(NULL AS varchar(20))   AS [PhoneNumber],
            CAST(NULL AS varchar(50))   AS [VoucherType],
            CAST(NULL AS decimal(18,2)) AS [AmountUsed],
            CAST(NULL AS nvarchar(50))  AS [OrderUsed];
    END
END
GO
