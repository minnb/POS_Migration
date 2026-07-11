/* ============================================================================
   9.1 Danh mục Bảng giá — GetSalesPriceList / GetSalesPriceList_Export
   RPOSMasterData (CentralMD). CHẠY THỦ CÔNG 1 LẦN (nối tiếp docs/sql/GetSalesPriceList_AddSaleType.sql
   đã áp dụng trước đó — bản này chỉ thêm 1 cột, giữ nguyên toàn bộ phần còn lại).

   Lý do: 1 item/uom/nhóm giá/ngày hiệu lực có thể có NHIỀU dòng khác nhau theo SalesType (hình thức
   bán hàng). SP trước đó chỉ trả SaleTypeName (text mô tả) — không đủ để định vị chính xác 1 dòng khi
   Sửa/Xóa giá (PriceRowKey cần thêm SalesType). Bổ sung cột SalesTypeCode = SalesPrice.SalesType (mã
   gốc) để PricesPage.razor build đúng khoá, dùng cùng lúc với SalesGroupCode đã thêm trước đó.
   ============================================================================ */
USE [RPOSMasterData]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[GetSalesPriceList]
(
	@ItemCode		nvarchar(20),
	@ItemName		nvarchar(500),
	@SaleType		nvarchar(50),
	@SalesGroup		nvarchar(20),
	@isCheck		int,
	@PageSize		int,
	@PageNumber		int
)

AS
/*
	@isCheck : 1 có hiệu lực; 0 : tất cả
*/
BEGIN
----------------------------------------------------------
	DECLARE @Total int = 0
----------------------------------------------------------
	SELECT distinct PriceGroupCode ,PriceGroupName
	INTO #TmpStorePriceGroup
	FROM StorePriceGroup (NOLOCK)
	WHERE (PriceGroupCode = @SalesGroup OR @SalesGroup = '')
----------------------------------------------------------

SELECT  ISNULL(S.[ItemNo],'') AS ItemNo,
		ISNULL(G.PriceGroupName,'') AS SalesCode,
		ISNULL(S.[SalesCode],'') AS SalesGroupCode,           -- mã gốc nhóm giá (PK) — dùng cho Sửa/Xóa
		'' AS StoreNo,
		ISNULL(I.[Description],'') AS ItemName,
		--ISNULL(B.[BarcodeNo],'') AS BarcodeNo,
		'' BarcodeNo,
		ISNULL(S.[UnitOfMeasureCode],'') AS UnitOfMeasureCode,
		FORMAT(S.[UnitPrice],'###,###') AS UnitPrice,
		CONVERT(VARCHAR, S.[StartingDate], 103) AS StartingDateStr,
		CONVERT(VARCHAR, S.[EndingDate], 103) AS EndingDateStr,
		--CASE WHEN RIGHT (CONVERT(VARCHAR, S.[EndingDate], 103),4) <> '7777' THEN RIGHT(CONVERT(VARCHAR, S.[EndingDate], 103),4) END EndingYearStr,
		CASE WHEN RIGHT (CONVERT(VARCHAR, S.[EndingDate], 103),4) <> '9999' THEN RIGHT(CONVERT(VARCHAR, S.[EndingDate], 103),4)
		     WHEN RIGHT (CONVERT(VARCHAR, S.[EndingDate], 103),4) <> '7777' THEN N''
		END EndingYearStr,
		ISNULL(S.[Counter],'') AS [Counter],
		ISNULL(S.[Pkey],'') AS Pkey,
		S.[StartingDate], -- để order by
		O.Description AS SaleTypeName,
		ISNULL(CONVERT(nvarchar(50), S.[SalesType]), '') AS SalesTypeCode   -- mã gốc hình thức bán hàng — dùng cho Sửa/Xóa
		INTO #TempSalesPrice

FROM [dbo].[SalesPrice] S (NOLOCK)
INNER JOIN [dbo].[Item] I (NOLOCK) ON I.[No] = S.[ItemNo]
--LEFT JOIN [dbo].[Barcodes] B (NOLOCK) ON B.[ItemNo] = S.ItemNo AND S.UnitOfMeasureCode = B.[UnitOfMeasureCode]
LEFT JOIN #TmpStorePriceGroup G ON S.[SalesCode] = G.[PriceGroupCode]
LEFT JOIN SalesOrderType O (Nolock) ON S.SalesType = O.Code AND O.IsActive = 1
WHERE (@ItemCode = '' OR CHARINDEX(@ItemCode, S.[ItemNo]) > 0)
AND (@ItemName = '' OR CHARINDEX(@ItemName, I.[Description]) > 0)
AND (@SaleType = '' OR S.SalesType = @SaleType)
AND (@SalesGroup = '' OR ISNULL(G.PriceGroupCode,'XXX') = @SalesGroup)
AND (@isCheck = 0 OR (@isCheck = 1 AND YEAR(EndingDate) NOT IN(7777) AND (CONVERT(DATE,getdate()) BETWEEN CONVERT(DATE,S.[StartingDate]) AND CONVERT(DATE,S.[EndingDate]))))
AND S.IsActive = 1
--ORDER BY  S.[StartingDate] DESC

--------------------------------------------------------------------
	SET @Total = (SELECT COUNT (T.[ItemNo]) FROM #TempSalesPrice T)
--------------------------------------------------------------------
	SELECT T.*,@Total AS Total
	FROM #TempSalesPrice T (NOLOCK)
	ORDER BY t.StartingDate DESC
--------------------------------------------------------------------
	OFFSET @PageSize * @PageNumber ROWS
	FETCH NEXT @PageSize ROWS ONLY;
--------------------------------------------------------------------
	DROP TABLE #TmpStorePriceGroup
	DROP TABLE #TempSalesPrice
--------------------------------------------------------------------

END
GO

/****** Object:  StoredProcedure [dbo].[GetSalesPriceList_Export] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[GetSalesPriceList_Export]
(
	@ItemCode		nvarchar(20),
	@ItemName		nvarchar(500),
	@SaleType		nvarchar(50),
	@SalesGroup		nvarchar(20),
	@isCheck		int
)

AS
BEGIN

--------------------------------------------------------------------
SELECT distinct PriceGroupCode ,PriceGroupName
	INTO #TmpStorePriceGroup
	FROM StorePriceGroup (NOLOCK)
	WHERE (PriceGroupCode = @SalesGroup OR @SalesGroup = '')
--------------------------------------------------------------------

SELECT  ISNULL(S.[ItemNo],'') AS ItemNo,
		ISNULL(G.PriceGroupName,'') AS SalesCode,
		ISNULL(S.[SalesCode],'') AS SalesGroupCode,           -- mã gốc nhóm giá (PK) — dùng cho Sửa/Xóa
		'' AS StoreNo,
		ISNULL(I.[Description],'') AS ItemName,
		--ISNULL(B.[BarcodeNo],'') AS BarcodeNo,
		'' BarcodeNo,
		ISNULL(S.[UnitOfMeasureCode],'') AS UnitOfMeasureCode,
		FORMAT(S.[UnitPrice],'###,###') AS UnitPrice,
		CONVERT(VARCHAR, S.[StartingDate], 103) AS StartingDateStr,
		CONVERT(VARCHAR, S.[EndingDate], 103) AS EndingDateStr,
		--CASE WHEN RIGHT (CONVERT(VARCHAR, S.[EndingDate], 103),4) <> '7777' THEN RIGHT(CONVERT(VARCHAR, S.[EndingDate], 103),4) END EndingYearStr,
		CASE WHEN RIGHT (CONVERT(VARCHAR, S.[EndingDate], 103),4) <> '9999' THEN RIGHT(CONVERT(VARCHAR, S.[EndingDate], 103),4)
		     WHEN RIGHT (CONVERT(VARCHAR, S.[EndingDate], 103),4) <> '7777' THEN N''
		END EndingYearStr,
		ISNULL(S.[Counter],'') AS [Counter],
		ISNULL(S.[Pkey],'') AS Pkey,
		S.[StartingDate], -- để order by
		O.Description AS SaleTypeName,
		ISNULL(CONVERT(nvarchar(50), S.[SalesType]), '') AS SalesTypeCode   -- mã gốc hình thức bán hàng — dùng cho Sửa/Xóa
		INTO #TempSalesPrice

FROM [dbo].[SalesPrice] S (NOLOCK)
INNER JOIN [dbo].[Item] I (NOLOCK) ON I.[No] = S.[ItemNo]
--LEFT JOIN [dbo].[Barcodes] B (NOLOCK) ON B.[ItemNo] = S.ItemNo AND S.UnitOfMeasureCode = B.[UnitOfMeasureCode]
LEFT JOIN #TmpStorePriceGroup G ON S.[SalesCode] = G.[PriceGroupCode]
LEFT JOIN SalesOrderType O (Nolock) ON S.SalesType = O.Code AND O.IsActive = 1
WHERE (@ItemCode = '' OR CHARINDEX(@ItemCode, S.[ItemNo]) > 0)
AND (@ItemName = '' OR CHARINDEX(@ItemName, I.[Description]) > 0)
AND (@SaleType = '' OR S.SalesType = @SaleType)
AND (@SalesGroup = '' OR ISNULL(G.PriceGroupCode,'XXX') = @SalesGroup)
AND (@isCheck = 0 OR (@isCheck = 1 AND YEAR(EndingDate) NOT IN(7777) AND (CONVERT(DATE,getdate()) BETWEEN CONVERT(DATE,S.[StartingDate]) AND CONVERT(DATE,S.[EndingDate]))))
AND S.IsActive = 1

-----------------------------------------------------
SELECT * FROM #TempSalesPrice A (NOLOCK)
ORDER BY A.[StartingDate] DESC
-----------------------------------------------------
DROP TABLE #TmpStorePriceGroup
DROP TABLE #TempSalesPrice
-----------------------------------------------------

END
GO
