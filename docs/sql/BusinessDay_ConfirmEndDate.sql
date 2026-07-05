/* ============================================================================
   Xác nhận kết thúc ngày (Business Day Confirm) — CHẠY TRÊN DB "CentralSale"
   THEO TỪNG STORE (shard DB routed qua StoreRoutedConnectionFactory), KHÔNG
   PHẢI RPOSMasterData/CentralMD.

   Lý do đặt ở đây: bảng dbo.BussinessDateOpen (ngày kinh doanh hiện tại của
   POS terminal, dùng bởi API GetBusinessDate) đã tồn tại sẵn trên chính DB
   này. Đặt ledger xác nhận cùng DB để 2 thao tác "advance BussinessDateOpen"
   + "ghi ledger xác nhận" chạy chung 1 transaction (atomic tuyệt đối) — không
   thể làm vậy nếu ledger nằm ở CentralMD (DB khác, không thể transaction chung
   qua network).

   Port logic từ: src/legacy/VCM.BLUEPOS.Data/StoreActivities/StoreActivitiesData.cs
   (ConfirmBusinessDate, dòng 750) — rút gọn: bỏ EInvoice/EODCreatedInvoice
   (ngoài phạm vi port), giữ đúng 2 việc cốt lõi: ghi ledger + advance ngày.

   CHẠY 1 LẦN trên DB CentralSale của MỖI store (không phải chạy 1 lần duy nhất
   như các script RPOSMasterData khác).
   ============================================================================ */

/* ── Bảng ledger xác nhận kết thúc ngày ────────────────────────────────────── */
IF OBJECT_ID(N'dbo.BusinessDayConfirm', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessDayConfirm
    (
        Code          varchar(20)    NOT NULL,   -- StoreNo + yyyyMMdd
        StoreNo       varchar(10)    NOT NULL,
        BusinessDate  date           NOT NULL,
        TotalRevenue  decimal(18,2)  NOT NULL DEFAULT (0),
        TotalShifts   int            NOT NULL DEFAULT (0),
        ConfirmedBy   nvarchar(100)  NOT NULL,
        ConfirmedDate datetime       NOT NULL DEFAULT (GETDATE()),
        CONSTRAINT PK_BusinessDayConfirm PRIMARY KEY (Code)
    );

    CREATE UNIQUE INDEX UX_BusinessDayConfirm_Store_Date
        ON dbo.BusinessDayConfirm (StoreNo, BusinessDate);
END
GO

/* ── usp_BusinessDay_ConfirmEndDate ────────────────────────────────────────
   1 transaction: (1) chặn xác nhận trùng, (2) insert ledger,
   (3) advance BussinessDateOpen +1 ngày (chỉ khi đang đúng = @BusinessDate —
   đây cũng là cơ chế optimistic-check, tự chặn nếu ngày đã bị đổi trước đó).
   Rule "tất cả máy POS đã đóng ngày" được validate ở Application layer
   (BusinessDayService) trước khi gọi SP này — SP không lặp lại rule đó.
   ────────────────────────────────────────────────────────────────────────── */
IF OBJECT_ID(N'dbo.usp_BusinessDay_ConfirmEndDate', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_BusinessDay_ConfirmEndDate;
GO

CREATE PROCEDURE dbo.usp_BusinessDay_ConfirmEndDate
(
    @StoreNo      varchar(10),
    @BusinessDate date,
    @TotalRevenue decimal(18,2),
    @TotalShifts  int,
    @ConfirmedBy  nvarchar(100)
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.BusinessDayConfirm WHERE StoreNo = @StoreNo AND BusinessDate = @BusinessDate)
        BEGIN
            ;THROW 50001, N'Ngày kinh doanh này đã được xác nhận trước đó.', 1;
        END

        UPDATE dbo.BussinessDateOpen
        SET    BussinessDate = DATEADD(DAY, 1, @BusinessDate)
        WHERE  StoreNo = @StoreNo AND CAST(BussinessDate AS DATE) = @BusinessDate;

        IF @@ROWCOUNT = 0
        BEGIN
            ;THROW 50002, N'Không tìm thấy ngày kinh doanh hiện tại của cửa hàng, hoặc ngày kinh doanh đã bị thay đổi. Vui lòng tải lại trang.', 1;
        END

        INSERT INTO dbo.BusinessDayConfirm (Code, StoreNo, BusinessDate, TotalRevenue, TotalShifts, ConfirmedBy, ConfirmedDate)
        VALUES (@StoreNo + CONVERT(varchar(8), @BusinessDate, 112), @StoreNo, @BusinessDate, @TotalRevenue, @TotalShifts, @ConfirmedBy, GETDATE());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
