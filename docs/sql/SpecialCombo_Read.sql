/* ============================================================================
   11.2 Special Combo — SP đọc (list + detail) — RPOSMasterData
   CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* ── Danh sách header (filter + paging, Total/row) ──────────────────────── */
IF OBJECT_ID(N'dbo.usp_SpecialCombo_GetList', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SpecialCombo_GetList;
GO

CREATE PROCEDURE dbo.usp_SpecialCombo_GetList
(
    @StoreNo     nvarchar(50)  = '',
    @SalesType   nvarchar(50)  = '',
    @MemberType  nvarchar(50)  = '',
    @Status      nvarchar(10)  = '-1',   -- '-1' = tất cả; '1' = đang áp dụng; '0' = ngưng
    @TextSearch  nvarchar(250) = '',
    @PageSize    int,
    @PageNumber  int                       -- 0-based
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  H.Code            AS Code,
            ISNULL(H.SalesType,'')  AS SalesType,
            ISNULL(H.Name,'')       AS Name,
            ISNULL(H.ComboQuantity,0) AS ComboQuantity,
            ISNULL(H.Amount,0)        AS Amount,
            ISNULL(H.IsMember,0)      AS IsMember,
            ISNULL(H.MemberCode,'')   AS MemberCode,
            H.FromDate, H.ToDate,
            ISNULL(H.IsEnable,0)      AS IsEnable,
            ISNULL(H.CreatedBy,'')    AS CreatedBy,
            H.CreatedDate,
            ISNULL(H.UpdatedBy,'')    AS UpdatedBy,
            H.UpdatedDate,
            COUNT(*) OVER()           AS Total
    FROM    dbo.SpecialComboHeader H (NOLOCK)
    WHERE   (@SalesType = ''  OR H.SalesType = @SalesType)
      AND   (@MemberType = '' OR H.MemberCode = @MemberType)
      AND   (@Status = '-1'   OR ISNULL(H.IsEnable,0) = CASE WHEN @Status = '1' THEN 1 ELSE 0 END)
      AND   (@TextSearch = '' OR H.Code LIKE '%' + @TextSearch + '%' OR H.Name LIKE '%' + @TextSearch + '%')
      AND   (@StoreNo = '' OR EXISTS (
                 SELECT 1 FROM dbo.SpecialComboStore S (NOLOCK)
                 WHERE S.Code = H.Code AND (S.StoreNo = @StoreNo OR S.StoreNo = 'ALL')))
    ORDER BY H.Code DESC
    OFFSET @PageSize * @PageNumber ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

/* ── Chi tiết 1 combo: header + tất cả lines + tất cả stores ─────────────── */
IF OBJECT_ID(N'dbo.usp_SpecialCombo_GetDetail', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SpecialCombo_GetDetail;
GO

CREATE PROCEDURE dbo.usp_SpecialCombo_GetDetail
(
    @Code  nvarchar(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    -- RS1: header
    SELECT  Code, ISNULL(SalesType,'') AS SalesType, ISNULL(Name,'') AS Name,
            ISNULL(ComboQuantity,0) AS ComboQuantity, ISNULL(Amount,0) AS Amount,
            ISNULL(IsMember,0) AS IsMember, ISNULL(MemberCode,'') AS MemberCode,
            FromDate, ToDate, ISNULL(IsEnable,0) AS IsEnable
    FROM    dbo.SpecialComboHeader (NOLOCK)
    WHERE   Code = @Code;

    -- RS2: lines (PriceMode: 2 = giá động, 1 = mặc định, 0 = thường)
    SELECT  ISNULL(GroupCode,'') AS GroupCode, ISNULL(GroupName,'') AS GroupName,
            ISNULL(ItemNo,'') AS ItemNo, ISNULL(ItemName,'') AS ItemName, ISNULL(UOM,'') AS UOM,
            ISNULL(MinimumQuantity,0) AS MinimumQuantity, ISNULL(MaximumQuantity,0) AS MaximumQuantity,
            ISNULL([Order],0) AS [Order],
            CASE WHEN ISNULL(IsDynamicPrice,0) = 1 THEN 2
                 WHEN ISNULL(IsDefault,0) = 1 THEN 1 ELSE 0 END AS PriceMode,
            ISNULL(IsRequired,0) AS IsRequired, ISNULL(IsSendSAP,1) AS IsSendSAP
    FROM    dbo.SpecialComboLine (NOLOCK)
    WHERE   Code = @Code
    ORDER BY GroupCode, [Order], ItemNo;

    -- RS3: stores
    SELECT  ISNULL(StoreNo,'') AS StoreNo, ISNULL(IsEnable,0) AS IsEnable
    FROM    dbo.SpecialComboStore (NOLOCK)
    WHERE   Code = @Code
    ORDER BY StoreNo;
END
GO
