/* ============================================================================
   8.3 Danh mục Voucher — SP đọc (list + detail) — RPOSMasterData
   Bảng: CpnVchBOMHeader (voucher/coupon BOM), CpnVchBOMLine (sản phẩm áp dụng).
   Port từ VCM.BLUEPOS VoucherData (SP GetUpdateVoucherHeaderList + EF).

   PHÂN TÁCH voucher ↔ coupon (dùng chung bảng CpnVchBOMHeader):
     - Voucher (8.3): header KHÔNG có CpnVchBOMIssueRule (nhập serial thủ công).
     - Coupon (8.1/8.2): header CÓ CpnVchBOMIssueRule (sinh mã Auto/Import).
   => List voucher lọc NOT EXISTS CpnVchBOMIssueRule.  // TODO: confirm quy tắc với DBA.
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Danh sách voucher (filter + paging, Total/row) ─────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupVoucher_GetList', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_GetList;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_GetList
(
    @ItemNo       nvarchar(20)  = '',
    @ItemName     nvarchar(300) = '',
    @Serial       nvarchar(50)  = '',      -- CouponCode
    @ArticleType  nvarchar(10)  = '',
    @Status       varchar(10)   = '-1',    -- '-1' tất cả | '1' hiệu lực | '0' hết hiệu lực
    @PageSize     int,
    @PageNumber   int                        -- 0-based
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @today date = CONVERT(date, GETDATE());

    ;WITH Data AS (
        SELECT  h.ItemNo,
                ISNULL(h.CouponCode, '')      AS SerialNo,
                ISNULL(h.ItemName, '')        AS ItemName,
                ISNULL(h.ArticleType, '')     AS ArticleType,
                ISNULL(h.UnitOfMeasure, '')   AS UnitOfMeasure,
                ISNULL(h.DiscountType, 0)     AS DiscountType,
                CAST(ISNULL(h.DiscountValue, 0)  AS decimal(18,2)) AS DiscountValue,
                CAST(ISNULL(h.ValueOfVoucher, 0) AS decimal(18,2)) AS ValueOfVoucher,
                CAST(ISNULL(h.MaxAmount, 0)      AS decimal(18,2)) AS MaxAmount,
                ISNULL(h.LimitQty, 0)         AS LimitQty,
                CAST(ISNULL(h.IsCheckItem, 0) AS bit) AS IsCheckItem,
                h.StartingDate, h.EndingDate, h.LastDateModified,
                CAST(CASE WHEN h.Blocked = 1 THEN 0
                          WHEN CONVERT(date, h.EndingDate) < @today THEN 0
                          ELSE 1 END AS bit)  AS [Status]
        FROM    dbo.CpnVchBOMHeader h (NOLOCK)
        WHERE   NOT EXISTS (SELECT 1 FROM dbo.CpnVchBOMIssueRule r (NOLOCK) WHERE r.ItemNo = h.ItemNo)
          AND   (@ItemNo      = '' OR h.ItemNo = @ItemNo)
          AND   (@ItemName    = '' OR h.ItemName LIKE '%' + @ItemName + '%')
          AND   (@Serial      = '' OR h.CouponCode LIKE '%' + @Serial + '%')
          AND   (@ArticleType = '' OR h.ArticleType = @ArticleType)
    )
    SELECT  ItemNo, SerialNo, ItemName, ArticleType, UnitOfMeasure, DiscountType, DiscountValue,
            ValueOfVoucher, MaxAmount, LimitQty, IsCheckItem,
            CONVERT(varchar(10), StartingDate, 103) AS StartingDate,
            CONVERT(varchar(10), EndingDate,   103) AS EndingDate,
            CONVERT(varchar(10), LastDateModified, 103) AS LastDateModified,
            [Status],
            COUNT(*) OVER() AS Total
    FROM    Data
    WHERE   (@Status = '-1' OR [Status] = CASE WHEN @Status = '1' THEN 1 ELSE 0 END)
    ORDER BY ItemNo DESC
    OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

/* ── Chi tiết 1 voucher (header + sản phẩm áp dụng) ─────────────────────── */
IF OBJECT_ID(N'dbo.usp_SetupVoucher_GetDetail', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupVoucher_GetDetail;
GO

CREATE PROCEDURE dbo.usp_SetupVoucher_GetDetail
(
    @ItemNo  nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    -- RS1: header
    SELECT  h.ItemNo,
            ISNULL(h.CouponCode, '')      AS SerialNo,
            ISNULL(h.ItemName, '')        AS ItemName,
            ISNULL(h.ArticleType, '')     AS ArticleType,
            ISNULL(h.UnitOfMeasure, '')   AS UnitOfMeasure,
            ISNULL(h.DiscountType, 0)     AS DiscountType,
            CAST(ISNULL(h.DiscountValue, 0)  AS decimal(18,2)) AS DiscountValue,
            CAST(ISNULL(h.ValueOfVoucher, 0) AS decimal(18,2)) AS ValueOfVoucher,
            CAST(ISNULL(h.MaxAmount, 0)      AS decimal(18,2)) AS MaxAmount,
            ISNULL(h.LimitQty, 0)         AS LimitQty,
            CAST(ISNULL(h.IsCheckItem, 0) AS bit) AS IsCheckItem,
            CASE WHEN h.Blocked = 1 THEN 1 ELSE 0 END AS Blocked,
            CONVERT(varchar(10), h.StartingDate, 103) AS StartingDate,
            CONVERT(varchar(10), h.EndingDate,   103) AS EndingDate
    FROM    dbo.CpnVchBOMHeader h (NOLOCK)
    WHERE   h.ItemNo = @ItemNo;

    -- RS2: sản phẩm áp dụng
    SELECT  LineItemNo               AS ItemNo,
            ISNULL([Description], '') AS ItemName,
            ISNULL(UnitOfMeasure, '') AS UOM,
            ISNULL(Barcode, '')       AS Barcode
    FROM    dbo.CpnVchBOMLine (NOLOCK)
    WHERE   ItemNo = @ItemNo
    ORDER BY LineNo;
END
GO
