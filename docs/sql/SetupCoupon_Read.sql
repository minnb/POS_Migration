/* ============================================================================
   8.1 / 8.2 Setup Coupon — SP đọc (list + codes + detail) — RPOSMasterData
   Bảng: CpnVchBOMIssueRule (rule sinh mã), CpnVchBOMHeader (header coupon),
         CpnVchBOMCodeIssue (mã coupon), CpnVchBOMLine (sản phẩm áp dụng).
   Port từ VCM.BLUEPOS SetupCouponData (EF LINQ) — legacy KHÔNG có SP.
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── 8.1 Danh sách coupon (join IssueRule + Header, filter + paging, Total/row) ─ */
IF OBJECT_ID(N'dbo.usp_SetupCoupon_GetList', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupCoupon_GetList;
GO

CREATE PROCEDURE dbo.usp_SetupCoupon_GetList
(
    @IssueType    varchar(20)   = '',      -- '' = tất cả; 'Auto' | 'Import'
    @ItemNo       nvarchar(20)  = '',
    @Description  nvarchar(300) = '',
    @Status       varchar(10)   = '-1',    -- '-1' = tất cả; '1' = hiệu lực; '0' = hết hiệu lực
    @PageSize     int,
    @PageNumber   int                        -- 0-based
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @today date = CONVERT(date, GETDATE());

    ;WITH Data AS (
        SELECT  r.ItemNo,
                ISNULL(h.ItemName, '')          AS Description,
                ISNULL(r.Prefix, '')            AS Prefix,
                ISNULL(r.LenCode, 0)            AS LenCode,
                ISNULL(r.IssueType, '')         AS IssueType,
                ISNULL(r.CharOfNumber, 0)       AS CharOfNumber,
                ISNULL(r.CharPosition, 0)       AS CharPosition,
                h.StartingDate,
                h.EndingDate,
                (SELECT COUNT(*) FROM dbo.CpnVchBOMCodeIssue c (NOLOCK) WHERE c.ItemNo = r.ItemNo) AS QtyCoupon,
                CAST(CASE WHEN h.Blocked = 1 THEN 0
                          WHEN CONVERT(date, h.EndingDate) < @today THEN 0
                          ELSE 1 END AS bit)    AS [Status]
        FROM    dbo.CpnVchBOMIssueRule r (NOLOCK)
        JOIN    dbo.CpnVchBOMHeader    h (NOLOCK) ON r.ItemNo = h.ItemNo
        WHERE   (@IssueType   = '' OR r.IssueType = @IssueType)
          AND   (@ItemNo      = '' OR r.ItemNo = @ItemNo)
          AND   (@Description = '' OR h.ItemName LIKE '%' + @Description + '%')
    )
    SELECT  ItemNo, Description, Prefix, LenCode, IssueType, CharOfNumber, CharPosition,
            CONVERT(varchar(10), StartingDate, 103) AS StartingDate,
            CONVERT(varchar(10), EndingDate,   103) AS EndingDate,
            QtyCoupon, [Status],
            COUNT(*) OVER() AS Total
    FROM    Data
    WHERE   (@Status = '-1' OR [Status] = CASE WHEN @Status = '1' THEN 1 ELSE 0 END)
    ORDER BY ItemNo DESC
    OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

/* ── 8.2 Danh sách mã coupon theo ItemNo (paging, Total/row) ─────────────── */
IF OBJECT_ID(N'dbo.usp_SetupCoupon_GetCodes', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupCoupon_GetCodes;
GO

CREATE PROCEDURE dbo.usp_SetupCoupon_GetCodes
(
    @ItemNo      nvarchar(20),
    @PageSize    int,
    @PageNumber  int                         -- 0-based
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  a.ItemNo,
            a.Code,
            CAST(ISNULL(a.Enabled, 0) AS bit) AS Enable,
            COUNT(*) OVER() AS Total
    FROM    dbo.CpnVchBOMCodeIssue a (NOLOCK)
    WHERE   a.ItemNo = @ItemNo
    ORDER BY a.ID
    OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

/* ── 8.2 Chi tiết 1 coupon (header + rule) + danh sách sản phẩm (edit) ───── */
IF OBJECT_ID(N'dbo.usp_SetupCoupon_GetDetail', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupCoupon_GetDetail;
GO

CREATE PROCEDURE dbo.usp_SetupCoupon_GetDetail
(
    @ItemNo  nvarchar(20)
)
AS
BEGIN
    SET NOCOUNT ON;

    -- RS1: header + rule
    SELECT  h.ItemNo,
            ISNULL(h.ItemName, '')        AS Description,
            ISNULL(r.IssueType, ISNULL(h.IssueType, '')) AS IssueType,
            ISNULL(h.SaleType, '')        AS SaleType,
            ISNULL(h.StoreGroupCode, '')  AS StoreGroupCode,
            ISNULL(h.ArticleType, 'ZCPN') AS ArticleType,
            CONVERT(varchar(10), h.StartingDate, 103) AS StartingDate,
            CONVERT(varchar(10), h.EndingDate,   103) AS EndingDate,
            ISNULL(h.IsCheckItem, 0)      AS IsCheckItem,
            ISNULL(r.Prefix, '')          AS Prefix,
            ISNULL(r.LenCode, 0)          AS LenCode,
            ISNULL(r.CharOfNumber, 0)     AS CharOfNumber,
            ISNULL(r.CharPosition, 0)     AS CharPosition,
            ISNULL(h.UnitOfMeasure, '')   AS UOM,
            h.DiscountType,
            CAST(ISNULL(h.DiscountValue, 0) AS decimal(18,2)) AS DiscountValue,
            CAST(ISNULL(h.MaxAmount, 0)     AS decimal(18,2)) AS MaxAmount,
            ISNULL(h.LimitQty, 0)         AS LimitQty,
            ISNULL(h.LimitQtyUsed, 0)     AS LimitQtyUsed,
            ISNULL(h.IsMultiUse, 0)       AS IsMultiUse,
            ISNULL(h.IsCheckAPI, 0)       AS IsCheckAPI,
            CASE WHEN h.Blocked = 1 THEN 1 ELSE 0 END AS Blocked,
            ISNULL(h.CpnVchType, '')      AS CpnVchType,
            (SELECT COUNT(*) FROM dbo.CpnVchBOMCodeIssue c (NOLOCK) WHERE c.ItemNo = h.ItemNo) AS QuantityCode
    FROM    dbo.CpnVchBOMHeader h (NOLOCK)
    LEFT JOIN dbo.CpnVchBOMIssueRule r (NOLOCK) ON r.ItemNo = h.ItemNo
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
