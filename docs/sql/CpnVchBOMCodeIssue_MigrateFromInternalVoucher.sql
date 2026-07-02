/* ============================================================================
   Migrate dữ liệu Internal_Voucher (SAP voucher thật) → CpnVchBOMCodeIssue (Source='SAP')
   RPOSMasterData.

   IDEMPOTENT — dùng WHERE NOT EXISTS nên chạy lại bao nhiêu lần cũng không tạo trùng.
   Chạy sau CpnVchBOMCodeIssue_ExtendSchema.sql + Voucher_Read.sql + Voucher_Save.sql.

   Chạy 2 LẦN theo kế hoạch rollout (docs/ROLLOUT.md §D6):
     - Lần 1: trước khi deploy code mới (di chuyển dữ liệu tồn kho).
     - Lần 2: NGAY SAU khi deploy xong (vét dữ liệu ghi bởi code cũ trong lúc rolling deploy
       có khoảng chồng lấp giữa các instance POS.Api chạy code cũ/mới).
   ============================================================================ */
USE [RPOSMasterData];
GO

INSERT INTO dbo.CpnVchBOMCodeIssue
    (Code, Enabled, CreatedDate, Source, Status, [Return], ActicleNo, ActicleType, Value,
     Voucher_Currency, Validity_From_Date, Expiry_Date, CompanyCode, Partner, IsEmployee,
     PhoneNumber, VoucherType, AmountUsed, OrderUsed)
SELECT i.VoucherNumber, 1, i.CreatedDate, 'SAP', i.Status, i.[Return], i.ActicleNo, i.ActicleType,
       i.Value, i.Voucher_Currency, i.Validity_From_Date, i.Expiry_Date, i.CompanyCode, i.Partner,
       i.IsEmployee, i.PhoneNumber, i.VoucherType, i.AmountUsed, i.OrderUsed
FROM dbo.Internal_Voucher i
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CpnVchBOMCodeIssue c WHERE c.Code = i.VoucherNumber AND c.Source = 'SAP'
);
GO

/* ── Verify trước khi cutover — cả 2 phải cho kết quả khớp/rỗng ────────────── */
-- SELECT COUNT(*) AS CountInternalVoucher FROM dbo.Internal_Voucher;
-- SELECT COUNT(*) AS CountMigratedSap FROM dbo.CpnVchBOMCodeIssue WHERE Source = 'SAP';

-- Phải RỖNG — nếu còn dòng nghĩa là migrate thiếu:
-- SELECT i.VoucherNumber
-- FROM dbo.Internal_Voucher i
-- WHERE NOT EXISTS (
--     SELECT 1 FROM dbo.CpnVchBOMCodeIssue c WHERE c.Code = i.VoucherNumber AND c.Source = 'SAP'
-- );
