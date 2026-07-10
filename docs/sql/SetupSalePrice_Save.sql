/* ============================================================================
   9.3 Setup Giá (Bulk Import) — TVP + SP lưu bảng giá — RPOSMasterData (CentralMD)
   Port từ VCM.BLUEPOS SetupPriceData.SaveSalesPrice / ValidateImport (EF + Setup_SalePrice_Get_ALL).

   Gồm:
     1. dbo.SetupSalePriceImportTVP  — TVP dùng cho validate import (repository chạy query inline).
     2. dbo.SetupSalePriceLineTVP    — TVP dùng cho lưu (usp_SetupSalePrice_Save).
     3. dbo.usp_SetupSalePrice_Save  — INSERT Pkey mới + ủy quyền cập nhật Pkey đã tồn tại.

   HÀNH VI (giữ nguyên legacy):
     - Chỉ xét các Pkey có YEAR(EndingDate) <> 7777 (bỏ qua bản ghi đánh dấu xóa).
     - Pkey CHƯA tồn tại  → INSERT mới, Counter = MAX(Counter còn hiệu lực)+1 (rỗng → 1),
       defaults: CurrencyCode='VND', PriceIncludesVAT=1, AllowInvoiceDisc=1, AllowLineDisc=1,
       MinimumQuantity=1, VariantCode=''.
     - Pkey ĐÃ tồn tại   → gọi SP có sẵn [dbo].[Setup_SalePrice_Get_ALL] @Json (đã proven trên
       production) với JSON [{Pkey, FromDate 'yyyy-MM-dd', ToDate, UnitPrice}] — GIỮ NGUYÊN logic update legacy.

   Trả kết quả qua OUTPUT param @Ok bit + @Message nvarchar(4000) (KHÔNG dùng result set — vì nhánh
   update gọi [Setup_SalePrice_Get_ALL] có SELECT Interface_Errors + ROLLBACK bên trong, không thể
   hứng/nuốt result set bằng INSERT...EXEC). Lỗi → @Ok=0 + ERROR_MESSAGE(), KHÔNG throw ra ngoài.

   ⚠️ SCHEMA (đối chiếu DDL hiện hành dbo.SalesPrice): bảng CHỈ có 15 cột — KHÔNG có
      IsActive / LastTimeUpdate / Id (khác EF model legacy .NET 4.6). INSERT vì vậy chỉ ghi
      15 cột thực có; "đánh dấu xóa" dựa hoàn toàn vào EndingDate (YEAR = 7777) + Counter,
      KHÔNG dùng IsActive. SalesType là cột [int] → giá trị SalesType (chuỗi mã) được SQL
      Server convert ngầm khi INSERT; đảm bảo mã hình thức bán hàng là số hợp lệ.

   ⚠️ PHỤ THUỘC: SP [dbo].[Setup_SalePrice_Get_ALL] phải tồn tại sẵn trên RPOSMasterData
      (đang dùng bởi hệ thống legacy) và cũng phải khớp schema 15 cột nói trên. CHẠY 1 LẦN trên RPOSMasterData.
   ============================================================================ */
USE [RPOSMasterData];
GO

/* 1) TVP validate import ---------------------------------------------------- */
IF TYPE_ID(N'dbo.SetupSalePriceImportTVP') IS NULL
CREATE TYPE dbo.SetupSalePriceImportTVP AS TABLE
(
    ItemNo        nvarchar(50)  NULL,
    Uom           nvarchar(20)  NULL,
    Barcode       nvarchar(50)  NULL,
    UnitPrice     varchar(20)   NULL,
    StartingDate  varchar(20)   NULL,
    EndingDate    varchar(20)   NULL
);
GO

/* 2) TVP lưu bảng giá ------------------------------------------------------- */
IF TYPE_ID(N'dbo.SetupSalePriceLineTVP') IS NULL
CREATE TYPE dbo.SetupSalePriceLineTVP AS TABLE
(
    Pkey               nvarchar(200)  NOT NULL,
    ItemNo             nvarchar(50)   NOT NULL,
    SalesCode          nvarchar(50)   NOT NULL,
    SalesType          nvarchar(50)   NOT NULL,
    UnitOfMeasureCode  nvarchar(20)   NOT NULL,
    UnitPrice          float          NOT NULL,
    StartingDate       datetime       NOT NULL,
    EndingDate         datetime       NOT NULL
);
GO

/* 3) SP lưu ----------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.usp_SetupSalePrice_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SetupSalePrice_Save;
GO
USE [RPOSMasterData]
GO
/****** Object:  StoredProcedure [dbo].[usp_SetupSalePrice_Save]    Script Date: 7/6/2026 10:43:16 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[usp_SetupSalePrice_Save]
(
    @Lines   dbo.SetupSalePriceLineTVP READONLY,
    @Actor   nvarchar(200)  = NULL,
    @Ok      bit            = 0   OUTPUT,
    @Message nvarchar(4000) = N'' OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        /* JSON cho các Pkey ĐÃ tồn tại (chuẩn hóa sentinel về 9999-01-01) */
        DECLARE @Json nvarchar(max) =
        (
            SELECT
                L.Pkey AS Pkey,
                CONVERT(varchar(10), L.StartingDate, 120) AS FromDate,
                CONVERT(varchar(10),
                        CASE WHEN YEAR(L.EndingDate) = 9999 THEN CONVERT(datetime,'9999-01-01')
                             ELSE L.EndingDate END, 120)   AS ToDate,
                CONVERT(real, L.UnitPrice) AS UnitPrice
            FROM @Lines L
            WHERE EXISTS (SELECT 1 FROM dbo.SalesPrice SP
                          WHERE SP.Pkey = L.Pkey AND YEAR(SP.EndingDate) <> 7777
                            AND ISNULL(SP.IsActive,1) = 1)
            FOR JSON PATH
        );

        /* 4a) INSERT các Pkey CHƯA tồn tại (transaction riêng) */
        BEGIN TRAN;a
            DECLARE @maxCounter bigint =
                ISNULL((SELECT MAX(Counter) FROM dbo.SalesPrice WITH (UPDLOCK, HOLDLOCK)), 0) + 1;

            INSERT dbo.SalesPrice
                (ItemNo, SalesCode, StartingDate, CurrencyCode, UnitOfMeasureCode, UnitPrice,
                 PriceIncludesVAT, AllowInvoiceDisc, SalesType, MinimumQuantity, EndingDate,
                 VariantCode, AllowLineDisc, [Counter], Pkey, IsActive, LastTimeUpdate)
            SELECT
                L.ItemNo, L.SalesCode, L.StartingDate, N'VND', L.UnitOfMeasureCode, L.UnitPrice,
                1, 1, L.SalesType, 1,
                CASE WHEN YEAR(L.EndingDate) = 9999 THEN CONVERT(datetime,'9999-01-01') ELSE L.EndingDate END,
                N'', 1, @maxCounter, L.Pkey, 1, GETDATE()
            FROM @Lines L
            WHERE NOT EXISTS (SELECT 1 FROM dbo.SalesPrice SP
                              WHERE SP.Pkey = L.Pkey AND YEAR(SP.EndingDate) <> 7777
                                AND ISNULL(SP.IsActive,1) = 1);
        COMMIT;

        /* 4b) Pkey ĐÃ tồn tại -> engine set-based (soft-delete + interval split) */
        IF @Json IS NOT NULL AND LEN(@Json) > 0
            EXEC dbo.Setup_SalePrice_Get_ALL @Json = @Json, @IsInsert = 1;

        SET @Ok = 1;
        SET @Message = N'Cập nhật thành công bảng giá';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @Ok = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END

GO

/****** Object:  StoredProcedure [dbo].[Setup_SalePrice_Get_ALL]    Script Date: 7/6/2026 10:06:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROC [dbo].[Setup_SalePrice_Get_ALL]
(
    @Json     nvarchar(max) = '',
    @IsInsert bit = 1
)
AS
/*
  Setup_SalePrice_Get_ALL '[{"Pkey":"ALL-10000002-CAI-TQ","FromDate":"2026-04-15","ToDate":"9999-12-31","UnitPrice":11444.0}]', 1
*/
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ProcessID varchar(100) = LOWER(NEWID());

    /* --- VALIDATE (set-based) --- */
    DECLARE @errs nvarchar(max) =
    (
        SELECT STRING_AGG(msg, N'; ')
        FROM (
            SELECT N'Pkey=' + ISNULL(j.Pkey,N'(null)') +
                   CASE
                     WHEN j.Pkey IS NULL OR LEN(LTRIM(j.Pkey)) = 0 THEN N': thiếu Pkey'
                     WHEN j.UnitPrice IS NULL OR j.UnitPrice <= 0   THEN N': UnitPrice phải > 0'
                     WHEN j.FromDate IS NULL OR j.ToDate IS NULL     THEN N': thiếu ngày'
                     WHEN j.FromDate > j.ToDate                      THEN N': FromDate > ToDate'
                   END AS msg
            FROM OPENJSON(@Json)
                 WITH (Pkey nvarchar(50), FromDate date, ToDate date, UnitPrice float) AS j
            WHERE j.Pkey IS NULL OR LEN(LTRIM(j.Pkey)) = 0
               OR j.UnitPrice IS NULL OR j.UnitPrice <= 0
               OR j.FromDate IS NULL OR j.ToDate IS NULL
               OR j.FromDate > j.ToDate
        ) x
    );
    IF @errs IS NOT NULL
    BEGIN
        /* Ghi log Interface_Errors (điều chỉnh cột cho khớp bảng thật nếu cần) */
        BEGIN TRY
            INSERT INTO dbo.Interface_Errors (ProcessID, ErrorMessage )
            VALUES (@ProcessID, LEFT(@errs, 4000));
        END TRY BEGIN CATCH /* bỏ qua nếu schema Interface_Errors khác */ END CATCH;

        IF @IsInsert = 1 SELECT * FROM dbo.Interface_Errors WHERE ProcessID = @ProcessID;
        THROW 50001, N'Dữ liệu import không hợp lệ.', 1;
    END

    DECLARE @Counter bigint =
        ISNULL((SELECT MAX([Counter]) FROM dbo.SalesPrice WITH (NOLOCK))
                /*WHERE YEAR(EndingDate) <> 7777 AND ISNULL(IsActive,1) = 1)*/, 0) + 1;

    /* --- Dựng timeline 1 lần --- */
    DROP TABLE IF EXISTS #F;
    SELECT * INTO #F FROM dbo.tvf_SetupSalePrice_Timeline(@Json);

    IF @IsInsert = 0    -- preview
    BEGIN
        SELECT * FROM #F ORDER BY Pkey, StartingDate;
        DROP TABLE IF EXISTS #F;
        RETURN;
    END

    /* --- MERGE 1 lần: upsert + soft-delete, trong transaction --- */
    BEGIN TRY
        BEGIN TRAN;

            MERGE dbo.SalesPrice WITH (HOLDLOCK) AS T
            USING #F AS S
               ON  T.ItemNo            = S.ItemNo
               AND T.SalesCode         = S.SalesCode
			   AND T.SalesType         = S.SalesType
               AND T.StartingDate      = S.StartingDate
               AND T.UnitOfMeasureCode = S.UnitOfMeasureCode
            /* đổi giá/ngày hoặc revive dòng đã soft-delete -> cập nhật tại chỗ (không vỡ PK) */
            WHEN MATCHED AND (T.EndingDate <> S.EndingDate
                              OR T.UnitPrice <> S.UnitPrice
                              OR ISNULL(T.IsActive,1) <> 1
                              OR ISNULL(T.Pkey,N'') <> S.Pkey)
                THEN UPDATE SET
                    T.EndingDate     = S.EndingDate,
                    T.UnitPrice      = S.UnitPrice,
                    T.CurrencyCode   = S.CurrencyCode,
                    T.PriceIncludesVAT = S.PriceIncludesVAT,
                    T.AllowInvoiceDisc = S.AllowInvoiceDisc,
                    --T.SalesType      = S.SalesType,
                    T.MinimumQuantity= S.MinimumQuantity,
                    T.VariantCode    = S.VariantCode,
                    T.AllowLineDisc  = S.AllowLineDisc,
                    T.Pkey           = S.Pkey,
                    T.IsActive       = 1,
                    T.[Counter]      = @Counter,
                    T.LastTimeUpdate = GETDATE()
            /* StartingDate mới -> insert */
            WHEN NOT MATCHED BY TARGET
                THEN INSERT
                    (ItemNo, SalesCode, StartingDate, CurrencyCode, UnitOfMeasureCode, UnitPrice,
                     PriceIncludesVAT, AllowInvoiceDisc, SalesType, MinimumQuantity, EndingDate,
                     VariantCode, AllowLineDisc, [Counter], Pkey, IsActive, LastTimeUpdate)
                VALUES
                    (S.ItemNo, S.SalesCode, S.StartingDate, S.CurrencyCode, S.UnitOfMeasureCode, S.UnitPrice,
                     S.PriceIncludesVAT, S.AllowInvoiceDisc, S.SalesType, S.MinimumQuantity, S.EndingDate,
                     S.VariantCode, S.AllowLineDisc, @Counter, S.Pkey, 1, GETDATE())
            /* dòng active của Pkey mà timeline mới không còn -> SOFT DELETE (giữ dòng để sync) */
            WHEN NOT MATCHED BY SOURCE
                 AND T.Pkey IN (SELECT DISTINCT Pkey FROM #F)
                 AND YEAR(T.EndingDate) <> 7777
                 AND ISNULL(T.IsActive,1) = 1
                THEN UPDATE SET
                    T.IsActive       = 0,
                    T.[Counter]      = @Counter,
                    T.LastTimeUpdate = GETDATE();


				UPDATE dbo.SalesPrice
				SET [Counter] = @Counter,
					LastTimeUpdate = GETDATE()
				WHERE Pkey IN (SELECT DISTINCT Pkey FROM #F)
				  AND [Counter] <> @Counter;

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        BEGIN TRY
            INSERT INTO dbo.Interface_Errors (ProcessID, ErrorMessage )
            VALUES (@ProcessID, LEFT(ERROR_MESSAGE(), 4000));
        END TRY BEGIN CATCH END CATCH;
        DROP TABLE IF EXISTS #F;
        THROW;
    END CATCH

    DROP TABLE IF EXISTS #F;

    IF @IsInsert = 1
        SELECT * FROM dbo.Interface_Errors WHERE ProcessID = @ProcessID;  -- rỗng nếu không lỗi
END

GO
ALTER FUNCTION [dbo].[tvf_SetupSalePrice_Timeline] (@Json nvarchar(max))
RETURNS TABLE
AS
RETURN
(
    WITH
    /* JSON -> khoảng mới (đã chuẩn hóa sentinel & clamp <= 9999-01-01) */
    N AS (
        SELECT
            j.Pkey,
            ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS RowId,
            CONVERT(date, j.FromDate) AS FromDate,
            CASE WHEN CONVERT(date, j.ToDate) >= '9999-01-01' THEN CONVERT(date,'9999-01-01')
                 ELSE CONVERT(date, j.ToDate) END AS ToDate,
            j.UnitPrice
        FROM OPENJSON(@Json)
        WITH (Pkey nvarchar(50), FromDate date, ToDate date, UnitPrice float) AS j
    ),
    /* Template: 1 dòng thuộc tính đại diện mỗi Pkey (từ dòng active) */
    TPL AS (
        SELECT Pkey, ItemNo, SalesCode, UnitOfMeasureCode, CurrencyCode,
               PriceIncludesVAT, AllowInvoiceDisc, SalesType, MinimumQuantity,
               VariantCode, AllowLineDisc,
               ROW_NUMBER() OVER (PARTITION BY Pkey ORDER BY StartingDate) AS rn
        FROM dbo.SalesPrice
        WHERE YEAR(EndingDate) <> 7777 AND ISNULL(IsActive,1) = 1
          AND Pkey IN (SELECT Pkey FROM N)
    ),
    Tpl1 AS (SELECT * FROM TPL WHERE rn = 1),
    /* Segments: cũ (prio 0) + mới (prio = RowId, lớn hơn thắng); clamp End <= 9999-01-01 */
    Seg AS (
        SELECT sp.Pkey,
               CONVERT(date, sp.StartingDate) AS s,
               CASE WHEN CONVERT(date, sp.EndingDate) >= '9999-01-01' THEN CONVERT(date,'9999-01-01')
                    ELSE CONVERT(date, sp.EndingDate) END AS e,
               sp.UnitPrice AS price, CAST(0 AS bigint) AS prio
        FROM dbo.SalesPrice sp
        WHERE YEAR(sp.EndingDate) <> 7777 AND ISNULL(sp.IsActive,1) = 1
          AND sp.Pkey IN (SELECT Pkey FROM Tpl1)
        UNION ALL
        SELECT n.Pkey, n.FromDate, n.ToDate, n.UnitPrice, CONVERT(bigint, n.RowId)
        FROM N n
        WHERE n.Pkey IN (SELECT Pkey FROM Tpl1)
    ),
    /* Điểm biên = mọi Start và (End+1) */
    Bnd AS (
        SELECT Pkey, s AS b FROM Seg
        UNION
        SELECT Pkey, DATEADD(day, 1, e) FROM Seg
    ),
    Ord AS (
        SELECT Pkey, b, LEAD(b) OVER (PARTITION BY Pkey ORDER BY b) AS nb FROM Bnd
    ),
    Atomic AS (
        SELECT Pkey, b AS st, DATEADD(day, -1, nb) AS en
        FROM Ord WHERE nb IS NOT NULL
    ),
    /* Giá hiệu lực của từng khoảng nguyên tử = segment prio cao nhất phủ nó */
    Eff AS (
        SELECT a.Pkey, a.st, a.en, ca.price
        FROM Atomic a
        CROSS APPLY (
            SELECT TOP 1 sg.price
            FROM Seg sg
            WHERE sg.Pkey = a.Pkey AND sg.s <= a.st AND sg.e >= a.en
            ORDER BY sg.prio DESC
        ) ca
    ),
    /* Gộp các khoảng liền kề CÙNG GIÁ (gaps & islands) */
    Flag AS (
        SELECT Pkey, st, en, price,
               CASE WHEN LAG(en)    OVER (PARTITION BY Pkey ORDER BY st) = DATEADD(day,-1,st)
                     AND LAG(price) OVER (PARTITION BY Pkey ORDER BY st) = price
                    THEN 0 ELSE 1 END AS isStart
        FROM Eff
    ),
    Island AS (
        SELECT Pkey, st, en, price,
               SUM(isStart) OVER (PARTITION BY Pkey ORDER BY st ROWS UNBOUNDED PRECEDING) AS grp
        FROM Flag
    ),
    Final AS (
        SELECT Pkey, MIN(st) AS StartingDate, MAX(en) AS EndingDate, MIN(price) AS UnitPrice
        FROM Island GROUP BY Pkey, grp
    )
    SELECT
        f.Pkey,
        t.ItemNo, t.SalesCode, t.UnitOfMeasureCode, t.CurrencyCode,
        t.PriceIncludesVAT, t.AllowInvoiceDisc, t.SalesType, t.MinimumQuantity,
        t.VariantCode, t.AllowLineDisc,
        CONVERT(datetime, f.StartingDate) AS StartingDate,
        CONVERT(datetime, f.EndingDate)   AS EndingDate,
        f.UnitPrice
    FROM Final f
    JOIN Tpl1 t ON t.Pkey = f.Pkey
);
