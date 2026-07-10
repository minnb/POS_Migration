/*
 GetPromotionOfferHeaderList_AddDateRangeFilter.sql
 Mục đích: thêm filter "Từ ngày" cho trang promotion/offers (OffersPage.razor).
 Ngữ nghĩa: CHỈ lọc theo @FromDate — CTKM còn hiệu lực từ @FromDate trở đi:
   H.[EndingDate] >= @FromDate
 KHÔNG lọc theo "Đến ngày" (đã bỏ, chốt 2026-07-10) — lý do: nhiều CTKM có EndingDate đặt xa
 (vd tới năm 2030), nếu thêm điều kiện `StartingDate <= @ToDate` cùng @FromDate mặc định
 (hôm nay − 90 ngày) thì vẫn đúng về logic, nhưng người dùng phản hồi không cần lọc theo mốc
 cuối — chỉ cần "CTKM còn hiệu lực kể từ ngày X" là đủ, tránh rối UI với field không thực sự
 cần thiết cho nghiệp vụ này.
 @FromDate NULL (không truyền) = bỏ qua điều kiện (tương thích ngược).

 Áp dụng thủ công trên CentralMD (RPOSMasterData) — DBA chạy 1 lần trên SSMS.
 Đã inject điều kiện vào ĐỦ 12 branch (4 tổ hợp @ItemNo có/không × @Status ''/'0'/khác, nhân 2
 cho nhánh @Exp=0 (phân trang UI) và @Exp<>0 (export)) — script sinh bằng find/replace tự động
 trên đúng chuỗi `AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])` (verify đếm đủ 12 occurrence
 trước khi generate, tránh sót branch).
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROC [dbo].[GetPromotionOfferHeaderList]
(
	@No				nvarchar(20)='',
	@Description	nvarchar(250)='',
	@Status			nvarchar(10)='',
	@OfferType		nvarchar(10)='',
	@ItemNo			varchar(20)='',
	@StoreNo		varchar(10)='',
	@FromDate		date=NULL,
	@Exp			int,
	@PageSize		int,
	@PageNumber		int
)
AS
BEGIN
---------------------------------------------------
	DECLARE @Total int = 0
---------------------------------------------------
	CREATE TABLE #TempPro   (OfferNo nvarchar(20))
	INSERT INTO #TempPro
	SELECT OfferNo
	FROM
	(
		SELECT OfferNo
		FROM OfferBuy (Nolock)
		WHERE [No] = @ItemNo
		UNION
		SELECT OfferNo
		FROM OfferGet (Nolock)
		WHERE [No] = @ItemNo
		UNION
		SELECT OfferNo
		FROM OfferBenefits (Nolock)
		WHERE [No] = @ItemNo
	) M
---------------------------------------------------------
    CREATE TABLE #TempProEx (OfferNo nvarchar(20))
	INSERT INTO #TempProEx
	SELECT OfferNo
	FROM
	(
		SELECT OfferNo
		FROM OfferBuy (Nolock)
		WHERE [No] = @ItemNo
		UNION
		SELECT OfferNo
		FROM OfferGet (Nolock)
		WHERE [No] = @ItemNo
		UNION
		SELECT OfferNo
		FROM OfferBenefits (Nolock)
		WHERE [No] = @ItemNo
	) N
---------------------------------------------------

IF (@Exp = 0)
BEGIN

IF (@ItemNo <> '')  -- Search theo ma san pham
BEGIN
--------------------------------------------

IF (@Status = '')  -- Tat ca
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion1

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion1 T)
--------------------------------------------------------------
    SELECT T.*, @Total AS Total
	FROM   #TempPromotion1 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	OFFSET @PageSize * @PageNumber ROWS
	FETCH NEXT @PageSize ROWS ONLY;
--------------------------------------------------------------
	DROP TABLE #TempPromotion1
--------------------------------------------------------------

END
ELSE IF (@Status = '0')  -- Co hieu luc
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion2

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] = 0 AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate])))
		--ORDER BY H.[No] DESC

--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion2 T)
--------------------------------------------------------------
    SELECT T.*, @Total AS Total
	FROM   #TempPromotion2 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	OFFSET @PageSize * @PageNumber ROWS
	FETCH NEXT @PageSize ROWS ONLY;
--------------------------------------------------------------
	DROP TABLE #TempPromotion2
--------------------------------------------------------------
END

ELSE IF (@Status <> '0')  -- Het hieu luc
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion3

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] <> 0 OR (H.[Status] = 0 AND CONVERT(date,H.[EndingDate]) < CONVERT(date,GETDATE())))
		ORDER BY H.[No] DESC

--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion3 T)
--------------------------------------------------------------
    SELECT T.*, @Total AS Total
	FROM   #TempPromotion3 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	OFFSET @PageSize * @PageNumber ROWS
	FETCH NEXT @PageSize ROWS ONLY;
--------------------------------------------------------------
	DROP TABLE #TempPromotion3
--------------------------------------------------------------
END

END
ELSE IF (@ItemNo = '')  -- Không search theo mã sản phẩm
BEGIN
IF (@Status = '')  -- Tat ca
BEGIN
SELECT	[No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion4

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		ORDER BY H.[No] DESC
--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion4 T)
--------------------------------------------------------------
    SELECT T.*, @Total AS Total
	FROM   #TempPromotion4 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	OFFSET @PageSize * @PageNumber ROWS
	FETCH NEXT @PageSize ROWS ONLY;
--------------------------------------------------------------
	DROP TABLE #TempPromotion4
--------------------------------------------------------------
END
ELSE IF (@Status = '0')  -- Có hieu luc
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion5

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] = 0 AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate])))
		--ORDER BY H.[No] DESC
--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion5 T)
--------------------------------------------------------------
    SELECT T.*, @Total AS Total
	FROM  #TempPromotion5 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	OFFSET @PageSize * @PageNumber ROWS
	FETCH NEXT @PageSize ROWS ONLY;
--------------------------------------------------------------
	DROP TABLE #TempPromotion5
--------------------------------------------------------------
END
ELSE IF (@Status <> '0')  -- Het hieu luc
BEGIN
SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion6

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] <> 0 OR (H.[Status] = 0 AND CONVERT(date,H.[EndingDate]) < CONVERT(date,GETDATE())))
		--ORDER BY H.[No] DESC
--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion6 T)
--------------------------------------------------------------
    SELECT T.*, @Total AS Total
	FROM  #TempPromotion6 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	OFFSET @PageSize * @PageNumber ROWS
	FETCH NEXT @PageSize ROWS ONLY;
--------------------------------------------------------------
	DROP TABLE #TempPromotion6
--------------------------------------------------------------
END
END

END

---------- export excel ----------

IF (@Exp <> 0)
BEGIN

IF (@ItemNo <> '')  -- Search theo ma san pham
BEGIN
--------------------------------------------

IF (@Status = '')  -- Tat ca
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion10

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)

--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion10 T)
--------------------------------------------------------------

    SELECT T.*, @Total AS Total
	FROM #TempPromotion10 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	DROP TABLE #TempPromotion10
--------------------------------------------------------------

END
ELSE IF (@Status = '0')  -- Co hieu luc
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion11

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] = 0 AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate])))

--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion11 T)
--------------------------------------------------------------

    SELECT T.*, @Total AS Total
	FROM #TempPromotion11 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	DROP TABLE #TempPromotion11
--------------------------------------------------------------
END

ELSE IF (@Status <> '0')  -- Het hieu luc
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion12

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] <> 0 OR (H.[Status] = 0 AND CONVERT(date,H.[EndingDate]) < CONVERT(date,GETDATE())))
--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion12 T)
--------------------------------------------------------------

    SELECT T.*, @Total AS Total
	FROM #TempPromotion12 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC

--------------------------------------------------------------
	DROP TABLE #TempPromotion12
--------------------------------------------------------------
END

END
ELSE IF (@ItemNo = '')  -- Không search theo mã sản phẩm
BEGIN
IF (@Status = '')  -- Tat ca
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion13

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		ORDER BY H.[No] DESC
--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion13 T)
--------------------------------------------------------------
    SELECT T.*, @Total AS Total
	FROM  #TempPromotion13 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC

--------------------------------------------------------------
	DROP TABLE #TempPromotion13
--------------------------------------------------------------
END
ELSE IF (@Status = '0')  -- Có hieu luc
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion14

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] = 0 AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate])))
--------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion14 T)
--------------------------------------------------------------

    SELECT T.*, @Total AS Total
	FROM  #TempPromotion14 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	DROP TABLE #TempPromotion14
--------------------------------------------------------------
END

ELSE IF (@Status <> '0')  -- Het hieu luc
BEGIN

SELECT	        [No] AS BonusbuyNo,
				K.[StoreNo] AS StoreNo,
				[PromotionNo],
				[Description],
				[OfferType],
				CASE WHEN H.[Status] <> 0  THEN  N'Hết hiệu lực'
					 WHEN H.[Status] = 0  AND (CONVERT(date,getdate()) BETWEEN CONVERT(date,H.[StartingDate]) AND CONVERT(date,H.[EndingDate]))  THEN N'Có hiệu lực'
				     ELSE N'Hết hiệu lực'
				END AS [Status],
				CONVERT(varchar(30),H.[StartingDate],103) AS StartingDate,
				CONVERT(varchar(30),H.[EndingDate],103) AS EndingDate,
				ISNULL(H.[LocalSiteGroup],'') AS LocalSiteGroup,
				H.[LimitQty],
				CONVERT(varchar(30),H.[VoucherFromDate],103) AS VoucherFromDate,
				CONVERT(varchar(30),H.[VoucherToDate],103) AS VoucherToDate,
				CONVERT(varchar(30),H.[LastDateModified],103) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion15

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR @StoreNo = K.[StoreNo])
		AND (@FromDate IS NULL OR H.[EndingDate] >= @FromDate)
		AND (H.[Status] <> 0 OR (H.[Status] = 0 AND CONVERT(date,H.[EndingDate]) < CONVERT(date,GETDATE())))
-----------------------------------------------------------------------
    SET @Total = (SELECT COUNT(T.BonusbuyNo) FROM #TempPromotion15 T)
-----------------------------------------------------------------------

    SELECT T.*, @Total AS Total
	FROM  #TempPromotion15 T
	ORDER BY T.BonusbuyNo,T.StoreNo,T.[OfferType] DESC
--------------------------------------------------------------
	DROP TABLE #TempPromotion15
--------------------------------------------------------------
END
END
END
--------------------------------------------------------------
	DROP TABLE #TempProEx
	DROP TABLE #TempPro
--------------------------------------------------------------
END
GO
