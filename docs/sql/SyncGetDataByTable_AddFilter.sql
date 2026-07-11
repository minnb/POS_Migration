USE [RPOSMasterData]
GO

/*
 ============================================================================
  [dbo].[SyncGetDataByTable] — THÊM filter per-store (backward-compatible)
 ============================================================================
  Mục đích: cho bảng IsByStore=1 (Store, Staff…), chỉ trả về dòng của store
  đang request (WHERE [ColumnFilter] = siteCode) thay vì toàn bộ store
  → file .txt nhỏ đi rất nhiều, POS import nhanh hơn, DB chỉ seek theo index.

  Thay đổi so với bản cũ:
   1. Thêm 2 tham số mới (default '' → caller cũ KHÔNG bị ảnh hưởng):
        @FilterColumn varchar(128) = ''   -- tên cột lọc (vd 'No', 'StoreNo')
        @FilterValue  nvarchar(128) = ''  -- giá trị lọc (= siteCode)
   2. BẮT BUỘC bọc ngoặc điều kiện Counter:
        WHERE ([Counter] > N OR 0=N) AND [FilterColumn] = @pFilterValue
      (không có ngoặc → AND bind chặt hơn OR → khi 0=N đúng vẫn lọt mọi dòng
       Counter>0 KHÔNG filter → sai dữ liệu).
   3. Giá trị filter PARAMETERIZED qua sp_executesql (@pFilterValue) → chống
      SQL injection ở phần giá trị. Tên cột @FilterColumn được bracket-quote
      (lấy từ cấu hình SyncTableList tin cậy).

  LƯU Ý go-live: đảm bảo các cột filter (Store.No, Staff.StoreNo…) có index
  để seek nhanh.
 ============================================================================
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[SyncGetDataByTable]
(
    @TableName     varchar(50)  = '',
    @ColumnOrderBy varchar(50)  = '',
    @POSLastCounter bigint      = 0,
    @FilterColumn  varchar(128) = '',   -- MỚI
    @FilterValue   nvarchar(128) = ''   -- MỚI
)
AS
/*
  SyncGetDataByTable 'OfferSite','StoreNo,Counter',0          -- không filter (global)
  SyncGetDataByTable 'Store','',0,'No','2018'                 -- filter per-store
*/
BEGIN
    DECLARE @Query nvarchar(max) = ''
    DECLARE @QueryColumn nvarchar(4000) = ''

    SET @QueryColumn =
    (
        SELECT STUFF((SELECT ',' +
            CASE WHEN DATA_TYPE = 'decimal'
                 THEN 'CONVERT(float,[' + COLUMN_NAME + ']) AS ' + COLUMN_NAME
                 ELSE '[' + COLUMN_NAME + ']' END
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName
            FOR XML PATH('')), 1, 1, '')
    )

    -- Điều kiện Counter (giữ nguyên) — BẮT BUỘC bọc ngoặc.
    DECLARE @Where nvarchar(1000) =
        ' WHERE ([Counter] > ' + LTRIM(@POSLastCounter) + ' OR 0=' + LTRIM(@POSLastCounter) + ')'

    -- Filter per-store (mới) — tên cột bracket-quote, giá trị parameterized.
    IF @FilterColumn <> '' AND @FilterValue <> ''
        SET @Where = @Where + ' AND [' + @FilterColumn + '] = @pFilterValue'

    SET @Query = 'SELECT ' + @QueryColumn + ' FROM [' + @TableName + '] (Nolock)' + @Where

    IF @ColumnOrderBy <> ''
        SET @Query = @Query + ' ORDER BY ' + @ColumnOrderBy

    --PRINT @Query
    EXEC sp_executesql @Query, N'@pFilterValue nvarchar(128)', @pFilterValue = @FilterValue
END
GO
