using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.PLG;
using VCM.BLUEPOS.Model.Barcode;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.SetupPrice;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Model.Store;

namespace VCM.BLUEPOS.Data.SetupPrice
{
    public interface ISetupPriceData
    {
        ViewSetupPriceModel ViewSetupPrice();
        List<StorePriceGroupModel> ListStoreGroup();
        List<TableStoreModel> ListStoreByChannel(string channel);
        BarcodeResponseModel GetInfoBarcode(string barcode);
        List<ExcelExamplePriceModel> ExcelExamplePrice();
        List<ItemModel> ListItemBarcode();
        List<ImportResultSalePriceModel> GetListInfoItem(List<LoadExcelExamplePriceModel> listItemExcel);
        Tuple<bool, string> SaveSalesPrice(List<SalesPriceModel> model);
        List<ListPriceModel> LoadSalesPrice(string itemCode, string itemName, string barCode, string salesCode, string status, out int totalRecord, int pageNumber, int pageSize);
        Tuple<bool, string> DeletePrice(int id);
        Tuple<bool, string> UpdatePrice(int id, float unitPrice);
    }
    public class SetupPriceData : ISetupPriceData
    {

        public ViewSetupPriceModel ViewSetupPrice()
        {
            var result = new ViewSetupPriceModel();
            using (var db = new CentralMDPartnerContainer())
            {
                try 
                {
                    /*
                    var listStorePriceGroup = db.StorePriceGroups.Select(a => new StorePriceGroupModel
                    {
                        Store = a.Store,
                        PriceGroupCode = a.PriceGroupCode,
                        Type = a.Type,
                        Priority = a.Priority,
                        ReplicationCounter = a.ReplicationCounter

                    }).ToList();
                    */
                    var listSaleType = db.SalesOrderTypes.Where(d => d.IsActive == true).Select(a => new VCM.BLUEPOS.Model.SetupPromotion.SalesTypeModel
                    {
                        Code = a.Code,
                        Description = a.Description,
                        TenderType = a.TenderType

                    }).ToList();

                    var listPriceGroup = db.PriceGroups.Where(d=>d.Enabled == true).Select(a => new PriceGroupModel
                    {
                        PriceGroupCode = a.PriceGroupCode,
                        PriceGroupName = a.PriceGroupName,

                    }).ToList();

                    result = new ViewSetupPriceModel
                    {
                        //listStorePriceGroup = listStorePriceGroup,
                        ListSalesType = listSaleType,
                        listPriceGroup = listPriceGroup
                    };

                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }
        public List<StorePriceGroupModel> ListStoreGroup()
        {
            var result = new List<StorePriceGroupModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.StorePriceGroups.Select(a => new StorePriceGroupModel
                    {
                        Store = a.Store,
                        PriceGroupCode = a.PriceGroupCode,
                        Type = a.Type,
                        Priority = a.Priority,
                        ReplicationCounter = a.ReplicationCounter

                    }).ToList();
                }
                catch (Exception ex)
                {

                }
                return result;
            }

        }

        public List<TableStoreModel> ListStoreByChannel(string channel)
        {
            var result = new List<TableStoreModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.Stores.Where(d => d.StyleProfile == channel && d.ClosingMethod == 0).Select(a => new TableStoreModel
                    {
                        No = a.No,
                        Name = a.Name,
                        Address = a.Address,
                        StyleProfile = a.StyleProfile

                    }).ToList();
                }
                catch (Exception ex)
                {

                }
                return result;
            }

        }

        public BarcodeResponseModel GetInfoBarcode(string barcode)
        {
            var result = new BarcodeResponseModel();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.Barcodes.Where(d => d.BarcodeNo == barcode).Select(a => new BarcodeResponseModel
                    {
                        BarcodeNo = a.BarcodeNo,
                        ItemNo = a.ItemNo,
                        UnitBarcode = a.UnitOfMeasureCode

                    }).FirstOrDefault();
                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public List<ItemModel> ListItemBarcode()
        {
            var result = new List<ItemModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.Items.Select(a => new ItemModel
                    {
                        No = a.No,
                        No2 = a.No2,
                        Description = a.Description,
                        SearchDescription = a.SearchDescription,
                        LongDescription = a.LongDescription,
                        BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                        Barcode = db.Barcodes.FirstOrDefault(d => d.ItemNo == a.No) == null ? "" : db.Barcodes.FirstOrDefault(d => d.ItemNo == a.No).BarcodeNo

                    }).ToList();
                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }
        public PagingResult<ItemResultCombo> SearchItem(string keyword, int pageSize=20,int page = 1, string searchBy = "item")
        {
           // var result = new List<ItemModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    
                    var result = db.Database.SqlQuery<ItemResultCombo>("[dbo].[SetupPrice_SearchItem_Paging] @Keyword, @Page, @PageSize, @SearchBy",
                        new SqlParameter("@Keyword", keyword ?? string.Empty),
                        new SqlParameter("@Page", page ),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@SearchBy", searchBy)).ToList();

                    if (result == null || result.Count == 0)
                    {
                        return new PagingResult<ItemResultCombo>
                        {
                            Items = null,
                            TotalRecord = 0
                        };
                    }
                    // var totalRecord = result.FirstOrDefault().;
                   
                    return new PagingResult<ItemResultCombo>
                    {
                        Items = result,
                        TotalRecord = result.FirstOrDefault().TotalRecords
                    };
                }
                catch (Exception ex)
                {
                    return new PagingResult<ItemResultCombo>
                    {
                        Items = null,
                        TotalRecord = 0
                    };
                }
              //  return result;
            }
        }

        public List<ExcelExamplePriceModel> ExcelExamplePrice()
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.ExcelExamplePrices.Select(a => new ExcelExamplePriceModel
                    {
                        ItemNo = a.ItemNo,
                        Barcode = a.Barcode,
                        UnitPrice = a.UnitPrice,
                        StartingDate = a.StartingDate,
                        EndingDate = a.EndingDate

                    }).ToList();
                    return data;
                }
                catch (Exception ex)
                {
                }
                return new List<ExcelExamplePriceModel>();
            }
        }

        public Tuple<bool, string> SaveSalesPrice(List<SalesPriceModel> model)
        {
            /*
             1. Them moi voi list Pkey chua ton tai
             2. Call Procedure de xu ly neu Pkey da ton tai SalePrice
             */
            var result = new Tuple<bool, string>(false, "Fail");
            try
            {
                var v = new List<ParamCallStoreProcedureRequest>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var modelCall = new List<ParamCallStoreProcedureRequest>();
                    var listPkey = db.SalesPrices.Where(d => DbFunctions.TruncateTime(d.EndingDate).Value.Year != 7777).Select(a => new { Pkey = a.Pkey, Counter = a.Counter }).ToList();
                    if(listPkey == null || listPkey.Count == 0) // SalePrice ban dau empty
                    {
                        var listPkeyNotSalesPrice = model.Where(d => !listPkey.Any(a => a.Pkey == d.Pkey)).ToList();

                        var maxCounter =  1;
                        var insertSales = listPkeyNotSalesPrice.Select(a => new SalesPrice
                        {
                            ItemNo = a.ItemNo,
                            SalesCode = a.SalesCode,
                            StartingDate = a.StartingDate,
                            CurrencyCode = a.CurrencyCode,
                            UnitOfMeasureCode = a.UnitOfMeasureCode,
                            UnitPrice = a.UnitPrice,
                            PriceIncludesVAT = a.PriceIncludesVAT,
                            AllowInvoiceDisc = a.AllowInvoiceDisc,
                            SalesType = a.SalesType,
                            MinimumQuantity = a.MinimumQuantity,
                            EndingDate = a.EndingDate,
                            VariantCode = a.VariantCode,
                            AllowLineDisc = a.AllowLineDisc,
                            IsActive=true,
                            LastTimeUpdate = DateTime.Now,
                            Counter = maxCounter,
                            Pkey = a.Pkey
                        });
                        if (insertSales != null && insertSales.Count() > 0)
                        {
                            db.SalesPrices.AddRange(insertSales);
                            db.SaveChanges();
                        }
                        result = new Tuple<bool, string>(true, "OK");
                        return result;
                    }
                    
                    if (listPkey != null && listPkey.Count > 0)
                    {
                        var listPkeyNotSalesPrice = model.Where(d => !listPkey.Any(a => a.Pkey == d.Pkey)).ToList();
                        var listPkeyExistSalesPrice = model.Where(d => listPkey.Any(a => a.Pkey == d.Pkey)).ToList();

                        modelCall = (from a in model
                                     join b in listPkeyExistSalesPrice on a.ItemNo equals b.ItemNo
                                     select new ParamCallStoreProcedureRequest
                                     {
                                         Pkey = a.Pkey,
                                         FromDate = a.StartingDate.Date.ToString("yyyy-MM-dd"),
                                         ToDate = a.EndingDate.Date.ToString("yyyy-MM-dd"),
                                         UnitPrice = (float)a.UnitPrice

                                     }).ToList();

                        var maxCounter = listPkey.Max(d => d.Counter) + 1;
                        var insertSales = listPkeyNotSalesPrice.Select(a => new SalesPrice
                        {
                            ItemNo = a.ItemNo,
                            SalesCode = a.SalesCode,
                            StartingDate = a.StartingDate,
                            CurrencyCode = a.CurrencyCode,
                            UnitOfMeasureCode = a.UnitOfMeasureCode,
                            UnitPrice = a.UnitPrice,
                            PriceIncludesVAT = a.PriceIncludesVAT,
                            AllowInvoiceDisc = a.AllowInvoiceDisc,
                            SalesType = a.SalesType,
                            MinimumQuantity = a.MinimumQuantity,
                            EndingDate = a.EndingDate,
                            VariantCode = a.VariantCode,
                            AllowLineDisc = a.AllowLineDisc,
                            IsActive = true,
                            LastTimeUpdate = DateTime.Now,
                            Counter = maxCounter,
                            Pkey = a.Pkey
                        });
                        if (insertSales != null && insertSales.Count() > 0)
                        {
                            db.SalesPrices.AddRange(insertSales);
                            db.SaveChanges();
                        }
                    }
                    else // 
                    {
                        modelCall = model.Select(a => new ParamCallStoreProcedureRequest
                        {
                            Pkey = a.Pkey,
                            FromDate = a.StartingDate.Date.ToString("yyyy-MM-dd"),
                            ToDate = a.EndingDate.Date.ToString("yyyy-MM-dd"),
                            UnitPrice = (float)a.UnitPrice

                        }).ToList();
                    }

                    if (modelCall != null && modelCall.Count > 0)
                    {
                       // var exec2 = db.Database.ExecuteSqlCommand("[dbo].[Setup_SalePrice_Get] @Pkey,@FromDate,@ToDate,@UnitPrice,@IsInsert ",
                       //new SqlParameter("@Pkey", "ALL-65000165-GOI-ALL"),
                       //new SqlParameter("@FromDate", "2021-12-30"),
                       //new SqlParameter("@ToDate", "2022-11-30"),
                       //new SqlParameter("@UnitPrice", "20000"),
                       //new SqlParameter("@IsInsert", "1")
                       //);

                        var jsonPrice = JsonConvert.SerializeObject(modelCall);
                        var exec = db.Database.ExecuteSqlCommand("[dbo].[Setup_SalePrice_Get_ALL] @Json ",
                        new SqlParameter("@Json", jsonPrice));
                    }

                    result = new Tuple<bool, string>(true, "OK");
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public Tuple<bool, string> SaveSalesPrice_BK(List<SalesPriceModel> model)
        {
            var result = new Tuple<bool, string>(false, "Fail");
            try
            {
                var v = new List<ParamCallStoreProcedureRequest>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var modelCall = new List<ParamCallStoreProcedureRequest>();
                    var listPkey = db.SalesPrices.Where(d => DbFunctions.TruncateTime(d.EndingDate).Value.Year != 7777).Select(a => new { Pkey = a.Pkey, Counter = a.Counter }).ToList();
                    if (listPkey != null && listPkey.Count > 0)
                    {
                        var listPkeyNotSalesPrice = model.Where(d => !listPkey.Any(a => a.Pkey == d.Pkey)).ToList();
                        var listPkeyExistSalesPrice = model.Where(d => listPkey.Any(a => a.Pkey == d.Pkey)).ToList();

                        modelCall = (from a in model
                                     join b in listPkeyExistSalesPrice on a.ItemNo equals b.ItemNo
                                     select new ParamCallStoreProcedureRequest
                                     {
                                         Pkey = a.Pkey,
                                         FromDate = a.StartingDate.Date.ToString("yyyy-MM-dd"),
                                         ToDate = a.EndingDate.Date.ToString("yyyy-MM-dd"),
                                         UnitPrice = (float)a.UnitPrice

                                     }).ToList();

                        var maxCounter = listPkey.Max(d => d.Counter) + 1;
                        var insertSales = listPkeyNotSalesPrice.Select(a => new SalesPrice
                        {
                            ItemNo = a.ItemNo,
                            SalesCode = a.SalesCode,
                            StartingDate = a.StartingDate,
                            CurrencyCode = a.CurrencyCode,
                            UnitOfMeasureCode = a.UnitOfMeasureCode,
                            UnitPrice = a.UnitPrice,
                            PriceIncludesVAT = a.PriceIncludesVAT,
                            AllowInvoiceDisc = a.AllowInvoiceDisc,
                            SalesType = a.SalesType,
                            MinimumQuantity = a.MinimumQuantity,
                            EndingDate = a.EndingDate,
                            VariantCode = a.VariantCode,
                            AllowLineDisc = a.AllowLineDisc,
                            Counter = maxCounter,
                            Pkey = a.Pkey
                        });
                        if (insertSales != null && insertSales.Count() > 0)
                        {
                            db.SalesPrices.AddRange(insertSales);
                            db.SaveChanges();
                        }
                    }
                    else // 
                    {
                        modelCall = model.Select(a => new ParamCallStoreProcedureRequest
                        {
                            Pkey = a.Pkey,
                            FromDate = a.StartingDate.Date.ToString("yyyy-MM-dd"),
                            ToDate = a.EndingDate.Date.ToString("yyyy-MM-dd"),
                            UnitPrice = (float)a.UnitPrice

                        }).ToList();
                    }

                    if (modelCall != null && modelCall.Count > 0)
                    {
                        // var exec2 = db.Database.ExecuteSqlCommand("[dbo].[Setup_SalePrice_Get] @Pkey,@FromDate,@ToDate,@UnitPrice,@IsInsert ",
                        //new SqlParameter("@Pkey", "ALL-65000165-GOI-ALL"),
                        //new SqlParameter("@FromDate", "2021-12-30"),
                        //new SqlParameter("@ToDate", "2022-11-30"),
                        //new SqlParameter("@UnitPrice", "20000"),
                        //new SqlParameter("@IsInsert", "1")
                        //);

                        var jsonPrice = JsonConvert.SerializeObject(modelCall);
                        var exec = db.Database.ExecuteSqlCommand("[dbo].[Setup_SalePrice_Get_ALL] @Json ",
                        new SqlParameter("@Json", jsonPrice));
                    }

                    result = new Tuple<bool, string>(true, "OK");
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public DataTable ToDataTable(List<LoadExcelExamplePriceModel> list)
        {
            var dt = new DataTable();

            dt.Columns.Add("ItemNo", typeof(string));
            dt.Columns.Add("Uom", typeof(string));
            dt.Columns.Add("Barcode", typeof(string));
            dt.Columns.Add("UnitPrice", typeof(string));
            dt.Columns.Add("StartingDate", typeof(string));
            dt.Columns.Add("EndingDate", typeof(string));

            foreach (var item in list)
            {
                dt.Rows.Add(item.ItemNo, item.Uom, item.Barcode,item.UnitPrice,item.StartingDate,item.EndingDate);
            }

            return dt;
        }
        public List<ImportResultSalePriceModel> ValidateImport(List<LoadExcelExamplePriceModel> list)
        {
            var dataTable = ToDataTable(list);

            using (var db = new CentralMDPartnerContainer())
            {
                var conn = (SqlConnection)db.Database.Connection;

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Create temp table
                        new SqlCommand(@"
                    DROP TABLE IF EXISTS #ImportSalePriceData
                    CREATE TABLE #ImportSalePriceData
                    (
                        ItemNo NVARCHAR(50),
                        Uom NVARCHAR(20),
                        Barcode NVARCHAR(50),
                        UnitPrice varchar(20),
                        StartingDate varchar(20),
                        EndingDate varchar(20)
                    )
                ", conn, tran).ExecuteNonQuery();

                        // 2️⃣ Bulk insert
                        using (var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
                        {
                            bulk.DestinationTableName = "#ImportSalePriceData";

                            bulk.ColumnMappings.Add("ItemNo", "ItemNo");
                            bulk.ColumnMappings.Add("Uom", "Uom");
                            bulk.ColumnMappings.Add("Barcode", "Barcode");
                            bulk.ColumnMappings.Add("UnitPrice", "UnitPrice");
                            bulk.ColumnMappings.Add("StartingDate", "StartingDate");
                            bulk.ColumnMappings.Add("EndingDate", "EndingDate");

                            bulk.WriteToServer(dataTable);
                        }

                        // 🔥 QUAN TRỌNG
                        db.Database.UseTransaction(tran);

                        // 3️⃣ Validate
                        var result = db.Database.SqlQuery<ImportResultSalePriceModel>(@"
                   
                         SELECT 
                             ISNULL(IT.No,'') ItemNo,
                             ISNULL(U.Code,'') As uom,
                             '' AS barcode,
                             ISNULL(IT.No,'') +'-' + ISNULL(IT.Description,'') AS [text],
                             ISNULL(IT.No,'') As [id],
                             CONCAT(
                                 CASE WHEN IT.No IS NULL THEN I.ItemNo + N' Item không tồn tại; ' ELSE '' END,
                                 CASE WHEN U.Code IS NULL THEN I.ItemNo + N'- UOM: '+I.Uom+ N' không hợp lệ; ' ELSE '' END
                             ) AS ErrorMessage,
                            I.UnitPrice,I.StartingDate,I.EndingDate

                         FROM #ImportSalePriceData I
                         LEFT JOIN Item IT ON IT.No = I.ItemNo
                         LEFT JOIN ItemUnitOfMeasure U ON U.ItemNo = I.ItemNo AND U.Code = I.Uom
                         WHERE 1=1--(IT.No IS NULL OR U.Code IS NULL )
                         AND ISNULL(I.Barcode,'') = '' 

                         UNION

                         SELECT 
                             ISNULL(B.ItemNo,'') ItemNo,
                             ISNULL(B.UnitOfMeasureCode,'') As uom,
                             I.Barcode AS barcode,
                              ISNULL(IT.No,'') +'-' + ISNULL(IT.Description,'') AS [text],
                             ISNULL(IT.No,'')  As [id],
                             CASE WHEN B.BarcodeNo IS NULL THEN I.Barcode+ N' Barcode không tồn tại; ' ELSE '' END AS ErrorMessage,
                             I.UnitPrice,I.StartingDate,I.EndingDate
                         FROM #ImportSalePriceData I
                         LEFT JOIN Barcodes B ON B.BarcodeNo = I.Barcode
                         LEFT JOIN Item IT ON IT.No = B.ItemNo 
                         WHERE 1=1--(B.BarcodeNo IS NULL )
                         AND ISNULL(I.Barcode,'') <> ''
                ").ToList();

                        tran.Commit();

                        return result;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }
        public List<LoadSalesPriceModel> GetListInfoItemAndCheck(List<LoadExcelExamplePriceModel> listItemExcel)
        {
            var result = new List<LoadSalesPriceModel>();
            try
            {

            }
            catch (Exception ex)
            {

                throw;
            }
            return result;
        }
        public List<ImportResultSalePriceModel> GetListInfoItem(List<LoadExcelExamplePriceModel> listItemExcel)
        {
            var result = ValidateImport(listItemExcel);
             return result;
            
        }

        public List<ListPriceModel> LoadSalesPrice(string itemCode, string itemName, string barCode, string salesCode, string status, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ListPriceModel>();

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<ListPriceModel>("[dbo].[SP_LOAD_SALESPRICE] @ItemCode, @ItemName, @BarCode, @SalesCode, @HieuLuc, @PageSize, @PageNumber",
                            new SqlParameter("@ItemCode", itemCode ?? string.Empty),
                            new SqlParameter("@ItemName", itemName ?? string.Empty),
                            new SqlParameter("@BarCode", barCode ?? string.Empty),
                            new SqlParameter("@SalesCode", salesCode ?? string.Empty),
                            new SqlParameter("@HieuLuc", status),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ListPriceModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ListPriceModel>();
            }
        }

        public Tuple<bool, string> DeletePrice(int id)
        {
            var result = new Tuple<bool, string>(false, $"ID={id}");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkID = db.SalesPrices.FirstOrDefault(d => d.Id == id);
                    if (checkID != null)
                    {
                        // checkID.EndingDate = DateTime.Parse("7777-07-07");
                        checkID.LastTimeUpdate = DateTime.Now;
                        checkID.IsActive = false;
                        db.SaveChanges();

                        string pKey = checkID.Pkey;
                        var maxCounter = db.SalesPrices.Max(d => d.Counter) + 1;

                        var checkPkey = db.SalesPrices.Where(d => d.Pkey == pKey && d.Id != id).ToList();
                        if (checkPkey != null && checkPkey.Count > 0)
                        {
                            checkPkey.ForEach(x => { x.Counter = maxCounter; });
                        }
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Success");
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tìm  thấy dữ liệu cần xóa");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
            }
            return result;
        }

        public Tuple<bool, string> UpdatePrice(int id, float unitPrice)
        {
            var result = new Tuple<bool, string>(false, $"ID={id}");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkID = db.SalesPrices.FirstOrDefault(d => d.Id == id);
                    if (checkID != null)
                    {
                        if(checkID.UnitPrice == unitPrice)
                        {
                            result = new Tuple<bool, string>(false, $"Không tìm  thấy dữ liệu thay đổi");
                            return result;
                        }
                        checkID.UnitPrice = unitPrice;
                        checkID.LastTimeUpdate = DateTime.Now;
                        db.SaveChanges();

                        string pKey = checkID.Pkey;
                        var maxCounter = db.SalesPrices.Max(d => d.Counter) + 1;

                        var checkPkey = db.SalesPrices.Where(d => d.Pkey == pKey).ToList();
                        if (checkPkey != null && checkPkey.Count > 0)
                        {
                            checkPkey.ForEach(x => { x.Counter = maxCounter; });
                        }
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Success");
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tìm  thấy dữ liệu cần cập nhật");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
            }
            return result;
        }

    }
}
