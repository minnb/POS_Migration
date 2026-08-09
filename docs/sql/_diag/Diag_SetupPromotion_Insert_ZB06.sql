/* ============================================================================
   CHẨN ĐOÁN (READ-ONLY-ish) — Duyệt CTKM ZB06 fail: SQL Error 266 che lỗi thật
   ----------------------------------------------------------------------------
   ⚠️  KHÔNG đăng ký docs/sql/manifest.json. KHÔNG deploy tự động. Chỉ chạy TAY
       trên SSMS/sqlcmd (DB: RPOSMasterData) để LỘ lỗi nghiệp vụ thật bên trong
       SP legacy [dbo].[Setup_Promotion_Insert] — hiện đang bị Error 266 (trancount
       mismatch) do usp_SetupPromotion_Approve bọc BEGIN TRANSACTION bên ngoài che.

   Bối cảnh: PromotionRepository.ApproveSetupAsync (dòng 402) EXEC
   usp_SetupPromotion_Approve → SP này BEGIN TRAN rồi EXEC Setup_Promotion_Insert.
   Với ZB06 (tổng bill), SP legacy chạy nhánh lỗi + ROLLBACK nội bộ → trancount
   1→0 → Error 266 nuốt mất lỗi gốc.

   MỤC ĐÍCH: chạy SP legacy TRỰC TIẾP, KHÔNG bọc transaction ngoài → lỗi gốc nổi lên.

   ⚠️  Bước 2 (EXEC) GHI DỮ LIỆU (publish sang Offer*) nếu SP chạy được tới cuối —
       chỉ chạy trên môi trường DEV/UAT với BBYNR test, KHÔNG chạy trên PROD.
       Nếu chỉ muốn xem body SP (không ghi gì) → chỉ chạy Bước 1 + Bước 3.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Bước 1: Đọc body SP legacy (KHÔNG ghi gì) — tìm nhánh ROLLBACK + chỗ
      Convert(int, NUMOFDAYS) / phép chia / cột NOT NULL cho nhánh total-bill ── */
PRINT '===== BODY: dbo.Setup_Promotion_Insert =====';
EXEC sp_helptext N'dbo.Setup_Promotion_Insert';
GO

/* ── Bước 2: Chạy SP legacy TRỰC TIẾP để lộ lỗi thật (GHI DỮ LIỆU — xem cảnh báo) ──
      Đổi @BBY sang đúng mã CTKM ZB06 test chưa publish được (log gần nhất: 6000000042). */
DECLARE @BBY nvarchar(20) = N'6000000042';
PRINT '===== EXEC trực tiếp Setup_Promotion_Insert @BBY = ' + @BBY + ' =====';
BEGIN TRY
    EXEC [dbo].[Setup_Promotion_Insert] @BBY = @BBY;
    PRINT 'OK — SP legacy chạy KHÔNG lỗi khi gọi trực tiếp (=> vấn đề chỉ là trancount/266 do wrapper).';
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER()   AS ErrNumber,
           ERROR_SEVERITY() AS ErrSeverity,
           ERROR_STATE()    AS ErrState,
           ERROR_LINE()     AS ErrLine,
           ERROR_PROCEDURE()AS ErrProc,
           ERROR_MESSAGE()  AS ErrMessage;
END CATCH
GO

/* ── Bước 3: Dump giá trị draft để đối chiếu nhánh nghi ngờ (KHÔNG ghi gì) ── */
SELECT  BBYNR,
        BBYTYPE,          -- loại CTKM (vd ZB06) — KHÔNG phải cột 'OFFERTYPE'
        NUMOFDAYS,
        NUMOFDAYSLIST,    -- thêm bởi SetupPromotion_AddNumOfDaysList.sql
        TOTALMINVALUE,
        MINVALUE,
        IsVoucher,
        IsApprove
FROM    dbo.SetupPromotionHEADER
WHERE   BBYNR = N'6000000042';
GO
