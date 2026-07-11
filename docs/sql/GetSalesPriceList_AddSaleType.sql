/* ============================================================================
   9.1 Danh mục Bảng giá — GetSalesPriceList / GetSalesPriceList_Export
   RPOSMasterData (CentralMD). CHẠY THỦ CÔNG 1 LẦN.

   Nội dung: bản ALTER PROC do DBA cung cấp (đổi @BarCode → @SaleType,
   @SalesCode → @SalesGroup, thêm JOIN SalesOrderType trả SaleTypeName),
   CỘNG THÊM 2 điều chỉnh bắt buộc phát hiện khi review:

   1) SELECT thêm cột SalesGroupCode = S.[SalesCode] gốc (PriceGroupCode thật).
      SP mới trả SalesCode = PriceGroupName (tên hiển thị) — nếu không có thêm
      cột mã gốc thì usp_SalesPrice_UpdatePrice / usp_SalesPrice_SoftDelete
      (định vị dòng theo dbo.SalesPrice.SalesCode = mã, không phải tên) sẽ
      không tìm thấy dòng cần sửa/xóa.

   2) GetSalesPriceList_Export: sửa 2 chỗ tham chiếu sai tên temp table
      (#SalsePriceExportTemp không tồn tại — bảng tạo ra là #TempSalesPrice)
      → nếu không sửa, proc lỗi "Invalid object name '#SalsePriceExportTemp'"
      ngay khi gọi (nút Xuất Excel sẽ crash).
   ============================================================================ */
USE [RPOSMasterData]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* ----- Danh sach bang gia -------

 [dbo].[GetSalesPriceList] '10620893','','','1535','1',15,''
 [dbo].[GetSalesPriceList] '10620893','','','','1',15,''
 [dbo].[GetSalesPriceList] '10620893','','','1535','0',15,''
 [dbo].[GetSalesPriceList] '10620893','','','','0',15,''
*/

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
		ISNULL(S.[SalesCode],'') AS SalesGroupCode,           -- mã gốc (PK) — dùng cho Sửa/Xóa
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
		O.Description AS SaleTypeName
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

/* -- Export: Danh sach bang gia

 [dbo].[GetSalesPriceList_Export] '','','','2018','0'
 [dbo].[GetSalesPriceList_Export] '','','','2018','1'

*/

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
		ISNULL(S.[SalesCode],'') AS SalesGroupCode,           -- mã gốc (PK) — dùng cho Sửa/Xóa
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
		O.Description AS SaleTypeName
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
