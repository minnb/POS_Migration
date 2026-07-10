/*
 GetPromotionOfferHeaderList_FixDuplicateRows.sql
 Bug: trang promotion/offers (OffersPage.razor) load ra rất nhiều row trùng lặp (cùng
 BonusbuyNo/PromotionNo/Description lặp lại nhiều lần).

 Nguyên nhân: cả 12 branch của SP đều có
   LEFT JOIN [dbo].[OfferSite] K (NOLOCK) ON H.[No] = K.[OfferNo]
 nhưng @StoreNo luôn là '' (C# PromotionRepository.BuildParams: "StoreNo = string.Empty, //
 không lọc theo store (parity legacy)") — LEFT JOIN này KHÔNG BAO GIỜ dùng để lọc, chỉ để
 "phòng khi cần lọc theo store" nhưng lại fan-out 1 dòng OfferHeader thành N dòng theo đúng
 N dòng OfferSite của CTKM đó (1 CTKM áp dụng nhiều site/store => N dòng OfferSite). Do
 K.[StoreNo] không hiển thị trên UI (OffersPage.razor không có cột StoreNo) nên user thấy
 y hệt 1 dòng CTKM lặp lại N lần.

 Fix: bỏ LEFT JOIN, thay điều kiện lọc theo StoreNo (nếu sau này có truyền @StoreNo) bằng
 EXISTS — không fan-out hàng vì EXISTS chỉ trả về true/false, không nhân dòng theo số OfferSite
 khớp. Cột output StoreNo giữ nguyên tên/kiểu (để không đổi DTO OfferHeaderListItemDto.StoreNo)
 nhưng luôn trả rỗng — field này hiện không hiển thị ở OffersPage.razor lẫn Excel export, giữ
 lại cho tương thích DTO, không phải regression.

 Fix #2 (cùng đợt, yêu cầu đổi định dạng cột "NGÀY CẬP NHẬT" trên OffersPage.razor sang
 "dd-mm-yyyy HH:mm:ss"): CONVERT(...,H.[LastDateModified],103) trả dd/mm/yyyy — KHÔNG có phần
 giờ:phút:giây (style 103 = date-only), nên trước đây cột "Ngày cập nhật" luôn hiển thị 00:00
 dù DB có lưu giờ thật. Đổi sang style 120 (yyyy-mm-dd hh:mi:ss, giữ đủ time) để
 DateTime.TryParse ở Razor parse được đầy đủ giờ thật trước khi format lại theo ý người dùng.

 Áp dụng thủ công trên CentralMD (RPOSMasterData) — DBA chạy 1 lần trên SSMS, SAU
 GetPromotionOfferHeaderList_AddDateRangeFilter.sql (bản ALTER PROC gần nhất).
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion1

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion2

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion3

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion4

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion5

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion6

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion10

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion11

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion12

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		INNER JOIN #TempPro M (NOLOCK) ON H.[No] = M.[OfferNo]
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion13

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion14

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
				CONVERT(varchar(10),'') AS StoreNo,
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
				CONVERT(varchar(30),H.[LastDateModified],120) AS LastDateModified,
				H.[Counter],
				H.[Pkey]
				INTO #TempPromotion15

		FROM [dbo].[OfferHeader]  H (NOLOCK)
		WHERE (@No = '' OR @No = H.[No] OR @No = H.[PromotionNo])
	    AND (@Description = '' OR H.[Description] LIKE N'%' + @Description + '%')
		AND (@OfferType = '' OR @OfferType = H.[OfferType])
		AND (@StoreNo = '' OR EXISTS (SELECT 1 FROM [dbo].[OfferSite] K WHERE K.[OfferNo] = H.[No] AND K.[StoreNo] = @StoreNo))
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
