/* ============================================================================
   Danh sách master Coupon/Voucher (CpnVchBOMHeader) — RPOSMasterData
   Port trung thực từ SP legacy [dbo].[GetCpnVchBOMHeaderList].
   List THẲNG CpnVchBOMHeader (KHÔNG join CpnVchBOMIssueRule) — khác với
   usp_SetupCoupon_GetList (màn "Phát hành Coupon" = join IssueRule).
   Dùng cho trang POS.Web /promotion/coupons (read-only, mọi ArticleType).
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

IF OBJECT_ID(N'dbo.usp_CpnVchBOMHeader_GetList', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CpnVchBOMHeader_GetList;
GO

CREATE PROCEDURE dbo.usp_CpnVchBOMHeader_GetList
(
    @KeyWord    nvarchar(150) = '',      -- tìm trong ItemNo | CouponCode | ItemName
    @Type       varchar(20)   = '',      -- ArticleType ('' = tất cả)
    @Status     varchar(10)   = '-1',    -- '-1' tất cả | '0' còn hiệu lực | '1' hết hiệu lực
    @PageSize   int,
    @PageNumber int                        -- 0-based
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @today date = CONVERT(date, GETDATE());

    SELECT  H.[ItemNo],
            ISNULL(H.[ItemName], '')                              AS ItemName,
            ISNULL(H.[UnitOfMeasure], '')                         AS UnitOfMeasure,
            ISNULL(H.[ArticleType], '')                           AS ArticleType,
            ISNULL(H.[DiscountType], 0)                           AS DiscountType,
            CAST(ISNULL(H.[DiscountValue], 0)  AS decimal(38,20)) AS DiscountValue,
            CAST(ISNULL(H.[MaxAmount], 0)      AS decimal(38,20)) AS MaxAmount,
            CAST(ISNULL(H.[ValueOfVoucher], 0) AS decimal(38,20)) AS ValueOfVoucher,
            H.[StartingDate],
            H.[EndingDate],
            CAST(CASE WHEN H.[Blocked] = 1 THEN 1 ELSE 0 END AS bit) AS Blocked,
            ISNULL(H.[CouponCode], '')                            AS CouponCode,
            ISNULL(H.[LimitQty], 0)                               AS LimitQty,
            CAST(ISNULL(H.[IsCheckItem], 0) AS bit)               AS IsCheckItem,
            ISNULL(H.[Counter], 0)                                AS [Counter],
            ISNULL(H.[Pkey], '')                                  AS Pkey,
            COUNT(*) OVER()                                       AS Total
    FROM    dbo.CpnVchBOMHeader H (NOLOCK)
    WHERE   (@KeyWord = ''
             OR CHARINDEX(@KeyWord, H.[ItemNo])     > 0
             OR CHARINDEX(@KeyWord, H.[CouponCode]) > 0
             OR CHARINDEX(@KeyWord, H.[ItemName])   > 0)
      AND   (@Type = '' OR H.[ArticleType] = @Type)
      AND   (@Status = '-1'
             OR (@Status = '0' AND CONVERT(date, H.[EndingDate]) >= @today)
             OR (@Status = '1' AND CONVERT(date, H.[EndingDate]) <  @today))
    ORDER BY H.[LastDateModified] DESC
    OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
