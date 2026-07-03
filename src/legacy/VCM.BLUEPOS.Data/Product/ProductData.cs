using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Product;
using VCM.BLUEPOS.Model.Barcode;
using VCM.BLUEPOS.Model.OptionModel;
using Newtonsoft.Json;
using System.Data;

namespace VCM.BLUEPOS.Data.Product
{
    public interface IProductData
    {
        string GetIPAddessByPOSTerminal(string posTerminal);
        List<ProductListResponseModel> GetProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportProductListModel> ExportProductList(string itemNo, string itemName, string barcodeNo, string taxCode);
        ResultResponseModel SaveArticleList(CreateProductModel req);
        ResultResponseModel SaveBarcodeList(List<CreateBarcodeListModel> req);
        List<UpdateProductListResponseModel> GetUpdateProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel UpdateProductList(UpdateProductListModel req);
        List<StoreProductLockResponseModel> GetStoreProductLock(string setServer, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<ItemListResponseModel> GetItemListForBlock(string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        ResultResponseModel CreateProductLockOrUnlock(CreateProductLockModel req);
        List<ProductLockListResponseModel> GetProductLockList(string storeNo, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        ResultResponseModel CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass);

        // ----- Truy cap truc tiep xuong may POS ST/CH
        List<StoreProductLockResponseModel> POS_GetStoreProductLock(string IPAddress, string POSUser, string POSPass, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<ProductLockListResponseModel> POS_GetItemLockList(string IPAddress, string POSUser, string POSPass, string storeNo, string userName, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<ItemListResponseModel> POS_GetItemListForBlock(string IPAddress, string POSUser, string POSPass, string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        ResultResponseModel POS_CreateProductLockOrUnlock(CreateProductLockModel req, string POSUser, string POSPass);
        ResultResponseModel POS_CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass);

    }

    public class ProductData : IProductData
    {
        public string GetIPAddessByPOSTerminal(string posTerminal)
        {
            try
            {
                var result = string.Empty;
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.POSTerminals.Where(d => d.No == posTerminal).FirstOrDefault()?.IPAddress;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public List<ProductListResponseModel> GetProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ProductListResponseModel>("[dbo].[GetProductList] @ItemCode, @ItemName, @BarCode, @TaxCode, @PageSize, @PageNumber",
                        new SqlParameter("@ItemCode", itemNo ?? string.Empty),
                        new SqlParameter("@ItemName", itemName ?? string.Empty),
                        new SqlParameter("@BarCode", barcodeNo ?? string.Empty),
                        new SqlParameter("@TaxCode", taxCode ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ProductListResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ProductListResponseModel>();
            }
        }

        public List<ExportProductListModel> ExportProductList(string itemNo, string itemName, string barcodeNo, string taxCode)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportProductListModel>("[dbo].[GetProductList_Export] @ItemCode, @ItemName, @BarCode, @TaxCode",
                            new SqlParameter("@ItemCode", itemNo ?? string.Empty),
                            new SqlParameter("@ItemName", itemName ?? string.Empty),
                            new SqlParameter("@BarCode", barcodeNo ?? string.Empty),
                            new SqlParameter("@TaxCode", taxCode ?? string.Empty)
                            ).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportProductListModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportProductListModel>();
            }
        }

        public string CreateItemNo(string itemNo)
        {
            var result = "";
            if (string.IsNullOrEmpty(itemNo))
            {
                var _itemNo = "1000000001";
                result = _itemNo;
            }
            else
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var maxItemNo = db.Items.Max(x => x.No);
                    var _itemNo = int.Parse(maxItemNo) + 1;
                    result = Convert.ToString(_itemNo);
                }
            }
            return result;
        }
        public ResultResponseModel SaveArticleList(CreateProductModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    // OPTIMIZATION: Check Count instead of ToList() to avoid loading all items
                    var itemCount = db.Items.Count();
                    var _itemNo = "";
                    if (itemCount == 0)
                    {
                        _itemNo = "";
                    }
                    else
                    {
                        _itemNo = db.Items.Max(x => x.No);
                    }

                    var ItemNo = CreateItemNo(_itemNo);
                    if (itemCount == 0)
                    {
                        if (req.TaxGroupCode == "-1")
                        {
                            req.TaxGroupCode = string.Empty;
                        }
                        var insertArticle = new EF.Central.Item
                        {
                            No = ItemNo,
                            No2 = string.Empty,
                            Description = req.ItemName,
                            SearchDescription = req.ItemName,
                            LongDescription = req.ItemNameFull,
                            BaseUnitOfMeasure = req.BaseUnitOfMeasure,
                            StatisticsGroup = 0,
                            CommissionGroup = 0,
                            UnitPrice = 0,
                            CostingMethod = 0,
                            UnitCost = 0,
                            StandardCost = 0,
                            VendorNo = string.Empty,
                            VendorItemNo = string.Empty,
                            MaximumInventory = 0,
                            ReorderQuantity = 0,
                            GrossWeight = 0,
                            NetWeight = 0,
                            UnitsPerParcel = 0,
                            UnitVolume = 0,
                            Blocked = Convert.ToByte(req.Blocked),
                            LastDateModified = DateTime.Now,
                            PriceIncludesVAT = 1,
                            TaxGroupCode = req.TaxGroupCode,
                            PreventNegativeInventory = 0,
                            MinimumOrderQuantity = 0,
                            MaximumOrderQuantity = 0,
                            SafetyStockQuantity = 0,
                            OrderMultiple = 0,
                            SalesUnitOfMeasure = req.SalesUnitOfMeasure,
                            ManufacturerCode = string.Empty,
                            ItemCategoryCode = string.Empty,
                            ProductGroupCode = string.Empty,
                            ServiceItemGroup = string.Empty,
                            ItemTrackingCode = string.Empty,
                            ProductionBOMNo = string.Empty,
                            BlockedVINID = Convert.ToByte(req.BlockedVINID),
                            CommonItemNo = string.Empty,
                            ItemFamilyCode = req.ItemFamilyCode,
                            DivisionCode = string.Empty,
                            KeyinginPrice = 0,
                            ZeroPriceValid = 0,
                            Counter = 1,
                            Pkey = ItemNo
                        };

                        db.Items.Add(insertArticle);
                        db.SaveChanges();

                        // OPTIMIZATION: Get max counter ONCE before loop, not inside loop
                        var maxcounter = db.Barcodes.Any() ? db.Barcodes.Max(x => x.Counter) : 0;
                        
                        foreach (var item in req.CreateBarcodeList)
                        {
                            maxcounter++;
                            var insertBarcode = new EF.Central.Barcode
                            {
                                BarcodeNo = item.BarcodeNo,
                                ItemNo = ItemNo,
                                ShowForItem = 0,
                                Description = string.Empty,
                                Blocked = 1,                        // có hiệu lực
                                LastDateModified = DateTime.Now,
                                VariantCode = string.Empty,
                                UnitOfMeasureCode = item.UnitOfMeasureCode,
                                DiscountPercent = 0,
                                Counter = maxcounter,
                                Pkey = item.BarcodeNo
                            };
                            db.Barcodes.Add(insertBarcode);
                        }
                        // OPTIMIZATION: SaveChanges only ONCE for all barcodes, not in loop!
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        var checkItemNo = db.Items.FirstOrDefault(x => x.No == ItemNo);
                        if (checkItemNo != null)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"Mã sản phẩm : {ItemNo} đã có trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }

                        var maxCounter = db.Items.Max(x => x.Counter);
                        if (req.TaxGroupCode == "-1")
                        {
                            req.TaxGroupCode = string.Empty;
                        }
                        var insertArticle = new EF.Central.Item
                        {
                            No = ItemNo,
                            No2 = string.Empty,
                            Description = req.ItemName,
                            SearchDescription = req.ItemName,
                            LongDescription = req.ItemNameFull,
                            BaseUnitOfMeasure = req.BaseUnitOfMeasure,
                            StatisticsGroup = 0,
                            CommissionGroup = 0,
                            UnitPrice = 0,
                            CostingMethod = 0,
                            UnitCost = 0,
                            StandardCost = 0,
                            VendorNo = string.Empty,
                            VendorItemNo = string.Empty,
                            MaximumInventory = 0,
                            ReorderQuantity = 0,
                            GrossWeight = 0,
                            NetWeight = 0,
                            UnitsPerParcel = 0,
                            UnitVolume = 0,
                            Blocked = Convert.ToByte(req.Blocked),
                            LastDateModified = DateTime.Now,
                            PriceIncludesVAT = 1,
                            TaxGroupCode = req.TaxGroupCode,
                            PreventNegativeInventory = 0,
                            MinimumOrderQuantity = 0,
                            MaximumOrderQuantity = 0,
                            SafetyStockQuantity = 0,
                            OrderMultiple = 0,
                            SalesUnitOfMeasure = req.SalesUnitOfMeasure,
                            ManufacturerCode = string.Empty,
                            ItemCategoryCode = string.Empty,
                            ProductGroupCode = string.Empty,
                            ServiceItemGroup = string.Empty,
                            ItemTrackingCode = string.Empty,
                            ProductionBOMNo = string.Empty,
                            BlockedVINID = Convert.ToByte(req.BlockedVINID),
                            CommonItemNo = string.Empty,
                            ItemFamilyCode = req.ItemFamilyCode,
                            DivisionCode = string.Empty,
                            KeyinginPrice = 0,
                            ZeroPriceValid = 0,
                            Counter = maxCounter + 1,
                            Pkey = ItemNo
                        };

                        db.Items.Add(insertArticle);
                        db.SaveChanges();

                        // OPTIMIZATION: Get max counter ONCE before loop, not inside loop
                        var maxBarcodeCounter = db.Barcodes.Any() ? db.Barcodes.Max(x => x.Counter) : 0;
                        
                        foreach (var item in req.CreateBarcodeList)
                        {
                            maxBarcodeCounter++;
                            var insertBarcode = new EF.Central.Barcode
                            {
                                BarcodeNo = item.BarcodeNo,
                                ItemNo = ItemNo,
                                ShowForItem = 0,
                                Description = string.Empty,
                                Blocked = 1,                        // có hiệu lực
                                LastDateModified = DateTime.Now,
                                VariantCode = string.Empty,
                                UnitOfMeasureCode = item.UnitOfMeasureCode,
                                DiscountPercent = 0,
                                Counter = maxBarcodeCounter,
                                Pkey = item.BarcodeNo
                            };
                            db.Barcodes.Add(insertBarcode);
                        }
                        // OPTIMIZATION: SaveChanges only ONCE for all barcodes, not in loop!
                        db.SaveChanges();

                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }

        }

        public ResultResponseModel SaveBarcodeList(List<CreateBarcodeListModel> req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _itemNo = db.Items.Max(x => x.No);
                    foreach (var item in req)
                    {
                        var checkItemNo = db.Items.Select(x => x.No == _itemNo).ToList();
                        if (checkItemNo == null || checkItemNo.Count == 0)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"Mã sản phẩm : {_itemNo} không có trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }
                        else
                        {
                            var primarykey = db.Barcodes.Where(x => x.BarcodeNo == item.BarcodeNo && x.ItemNo == _itemNo && x.UnitOfMeasureCode == item.UnitOfMeasureCode).ToList();
                            if (primarykey == null || primarykey.Count == 0)
                            {
                                var maxcounter = db.Barcodes.Max(x => x.Counter);
                                var insert = new EF.Central.Barcode
                                {
                                    BarcodeNo = item.BarcodeNo,
                                    ItemNo = _itemNo,
                                    ShowForItem = 0,
                                    Description = string.Empty,
                                    Blocked = 1,                                // có hiệu lực
                                    LastDateModified = DateTime.Now,
                                    VariantCode = string.Empty,
                                    UnitOfMeasureCode = item.UnitOfMeasureCode,
                                    DiscountPercent = 0,
                                    Counter = maxcounter + 1,
                                    Pkey = item.BarcodeNo
                                };
                                db.Barcodes.Add(insert);
                                db.SaveChanges();
                            }
                            else
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Success,
                                    Message = $"Đã tồn tại : {item.BarcodeNo}-{_itemNo}-{item.UnitOfMeasureCode} trong hệ thống. Vui lòng kiểm tra lại"
                                };
                            }
                        }
                    }
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới barcode thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,  // 4
                    Message = "Có lỗi xảy ra"
                };
            }
        }

        public List<UpdateProductListResponseModel> GetUpdateProductList(string itemNo, string itemName, string barcodeNo, string taxCode, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<UpdateProductListResponseModel>("[dbo].[GetUpdateProductList] @ItemCode, @ItemName, @BarCode, @TaxCode, @PageSize, @PageNumber",
                        new SqlParameter("@ItemCode", itemNo ?? string.Empty),
                        new SqlParameter("@ItemName", itemName ?? string.Empty),
                        new SqlParameter("@BarCode", barcodeNo ?? string.Empty),
                        new SqlParameter("@TaxCode", taxCode ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<UpdateProductListResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<UpdateProductListResponseModel>();
            }
        }

        public ResultResponseModel UpdateProductList(UpdateProductListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var itemList = db.Items.FirstOrDefault(x => x.No == req.ItemNo);
                    var barcodeList = db.Barcodes.FirstOrDefault(y => y.ItemNo == req.ItemNo.Trim() && y.BarcodeNo == req.BarcodeNo.Trim() && y.UnitOfMeasureCode == req.BarcodeUnit.Trim());

                    if (itemList == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.warning,
                            Message = "Không có dữ liệu"
                        };
                    }
                    else
                    {
                        var maxcounter = db.Items.Max(x => x.Counter);
                        itemList.Description = req.ItemName;
                        itemList.SearchDescription = req.ItemName;
                        itemList.LongDescription = req.ItemNameFull;
                        itemList.TaxGroupCode = req.TaxGroupCode;
                        itemList.Blocked = req.Blocked;
                        itemList.BlockedVINID = req.BlockedVINID;
                        itemList.LastDateModified = req.LastDateModifiedStr;
                        itemList.Counter = maxcounter + 1;
                    }
                    db.SaveChanges();

                    if (barcodeList.BarcodeNo != null || barcodeList.BarcodeNo != "")
                    {
                        var maxcounter = db.Barcodes.Max(x => x.Counter);
                        barcodeList.Blocked = req.BlockedBarcode;
                        barcodeList.LastDateModified = req.LastDateModifiedStr;
                        barcodeList.Counter = maxcounter + 1;
                    }
                    else
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.warning,
                            Message = "Không có dữ liệu"
                        };
                    }
                    db.SaveChanges();
                }
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.Success, // 1
                    Message = "Cập nhật thành công"
                };

            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,  // 4
                    Message = "Có lỗi xảy ra"
                };
            }
        }

        public List<StoreProductLockResponseModel> GetStoreProductLock(string setServer, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var isAllStore = db.AdminUsers.Where(d => d.UserName == userName).FirstOrDefault()?.IsAllStore;
                    if (isAllStore == true)
                    {
                        if (storeNo == null || storeNo == "")
                        {
                            var listAllStoreAdminUser = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive,
                                IsAllStore = a.IsAllStore
                            }).FirstOrDefault();

                            var listStorePermision = db.Stores.Where(a => a.ClosingMethod == 0).ToList();

                            var data = (from s in listStorePermision
                                        join b in db.ItemBlocks.AsNoTracking() on s.No equals b.StoreNo into g
                                        from x in g.DefaultIfEmpty()
                                        where s.ClosingMethod == 0
                                        //where s.StyleProfile == salesChain && s.ClosingMethod == 0
                                        select new StoreProductLockResponseModel
                                        {
                                            StoreNo = s.No,
                                            StoreName = s.Name,
                                            StyleProfile = s.StyleProfile,
                                            ClosingMethod = string.Empty,
                                            IsLock = x == null ? "0" : x.Status == null ? "0" : x.Status.Value ? "1" : "0"
                                        });

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            if (!string.IsNullOrEmpty(searchText))
                            {
                                data = data.Where(x => x.StoreNo.ToLower().Contains(searchText.ToLower().Trim()) || x.StoreName.ToLower().Contains(searchText.ToLower().Trim()));
                            }

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            data = data.GroupBy(x => x.StoreNo).Select(x => x.Any(m => m.IsLock == "1")
                            ? x.FirstOrDefault(m => m.IsLock == "1") : x.FirstOrDefault()).OrderBy(x => x.StoreNo);

                            //Distinct
                            //data = data.GroupBy(x => x.StoreNo).Select(x => x.First());
                            //data = data.Distinct();

                            totalRecord = data.Count();
                            var result = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
                            return result;
                        }
                        else
                        {
                            var listStoreAdminUser = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive,
                                IsAllStore = a.IsAllStore
                            }).FirstOrDefault();

                            var listStorePermision = db.Stores.Where(a => a.ClosingMethod == 0).ToList();

                            var data = (from s in listStorePermision
                                        join b in db.ItemBlocks.AsNoTracking() on s.No equals b.StoreNo into g
                                        from x in g.DefaultIfEmpty()
                                        where s.ClosingMethod == 0 && s.No == storeNo
                                        //where s.StyleProfile == salesChain && s.ClosingMethod == 0 && s.No == storeNo
                                        select new StoreProductLockResponseModel
                                        {
                                            StoreNo = s.No,
                                            StoreName = s.Name,
                                            StyleProfile = s.StyleProfile,
                                            ClosingMethod = string.Empty,
                                            IsLock = x == null ? "0" : x.Status == null ? "0" : x.Status.Value ? "1" : "0"
                                        });

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            if (!string.IsNullOrEmpty(searchText))
                            {
                                data = data.Where(x => x.StoreNo.ToLower().Contains(searchText.ToLower().Trim()) || x.StoreName.ToLower().Contains(searchText.ToLower().Trim()));
                            }

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            data = data.GroupBy(x => x.StoreNo).Select(x => x.Any(m => m.IsLock == "1")
                            ? x.FirstOrDefault(m => m.IsLock == "1") : x.FirstOrDefault()).OrderBy(x => x.StoreNo);

                            //Distinct
                            //data = data.GroupBy(x => x.StoreNo).Select(x => x.First());
                            //data = data.Distinct();

                            totalRecord = data.Count();
                            var result = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
                            return result;
                        }
                    }
                    else     // IsAllStore = false
                    {
                        if (storeNo == null || storeNo == "")
                        {
                            var listAllStoreAdminUser = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive,
                                IsAllStore = a.IsAllStore
                            }).FirstOrDefault();

                            var listStore = JsonConvert.DeserializeObject<List<string>>(listAllStoreAdminUser.SiteNo);
                            var listStorePermision = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0).ToList();
                            var data = (from s in listStorePermision
                                        join b in db.ItemBlocks.AsNoTracking() on s.No equals b.StoreNo into g
                                        from x in g.DefaultIfEmpty()
                                        where s.ClosingMethod == 0
                                        //where s.StyleProfile == salesChain && s.ClosingMethod == 0
                                        select new StoreProductLockResponseModel
                                        {
                                            StoreNo = s.No,
                                            StoreName = s.Name,
                                            StyleProfile = s.StyleProfile,
                                            ClosingMethod = string.Empty,
                                            IsLock = x == null ? "0" : x.Status == null ? "0" : x.Status.Value ? "1" : "0"
                                        });

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            if (!string.IsNullOrEmpty(searchText))
                            {
                                data = data.Where(x => x.StoreNo.ToLower().Contains(searchText.ToLower().Trim()) || x.StoreName.ToLower().Contains(searchText.ToLower().Trim()));
                            }

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            data = data.GroupBy(x => x.StoreNo).Select(x => x.Any(m => m.IsLock == "1")
                            ? x.FirstOrDefault(m => m.IsLock == "1") : x.FirstOrDefault()).OrderBy(x => x.StoreNo);

                            //Distinct
                            //data = data.GroupBy(x => x.StoreNo).Select(x => x.First());
                            //data = data.Distinct();

                            totalRecord = data.Count();
                            var result = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
                            return result;
                        }
                        else 
                        {
                            var listStoreAdminUser = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive,
                                IsAllStore = a.IsAllStore
                            }).FirstOrDefault();

                            var listStore = JsonConvert.DeserializeObject<List<string>>(listStoreAdminUser.SiteNo);
                            var listStorePermision = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0).ToList();

                            var data = (from s in listStorePermision
                                        join b in db.ItemBlocks.AsNoTracking() on s.No equals b.StoreNo into g
                                        from x in g.DefaultIfEmpty()
                                        where s.ClosingMethod == 0 && s.No == storeNo
                                        //where s.StyleProfile == salesChain && s.ClosingMethod == 0 && s.No == storeNo
                                        select new StoreProductLockResponseModel
                                        {
                                            StoreNo = s.No,
                                            StoreName = s.Name,
                                            StyleProfile = s.StyleProfile,
                                            ClosingMethod = string.Empty,
                                            IsLock = x == null ? "0" : x.Status == null ? "0" : x.Status.Value ? "1" : "0"
                                        });

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            if (!string.IsNullOrEmpty(searchText))
                            {
                                data = data.Where(x => x.StoreNo.ToLower().Contains(searchText.ToLower().Trim()) || x.StoreName.ToLower().Contains(searchText.ToLower().Trim()));
                            }

                            if (data == null || data.Count() == 0)
                            {
                                return new List<StoreProductLockResponseModel>();
                            }

                            data = data.GroupBy(x => x.StoreNo).Select(x => x.Any(m => m.IsLock == "1")
                            ? x.FirstOrDefault(m => m.IsLock == "1") : x.FirstOrDefault()).OrderBy(x => x.StoreNo);

                            //Distinct
                            //data = data.GroupBy(x => x.StoreNo).Select(x => x.First());
                            //data = data.Distinct();

                            totalRecord = data.Count();
                            var result = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
                            return result;
                        }
                    }   
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<StoreProductLockResponseModel>();
            }
        }

        public List<ItemListResponseModel> GetItemListForBlock(string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = new List<ItemListResponseModel>();
                    if (status == "1")  // Đang tạm ngừng kinh doanh
                    {
                        data = (from a in db.Items
                                join b in db.ItemBlocks on a.No equals b.ItemNo
                                where b.Status == true && b.StoreNo == storeNo && a.Blocked == 0
                                select new ItemListResponseModel
                                {
                                    ItemNo = a.No,
                                    ItemName = a.Description,
                                    BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                    SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                    UnitPrice = a.UnitPrice,
                                    Blocked = a.Blocked,
                                    Status = "1",               // Khóa món, đang tạm ngừng kinh doanh
                                    VendorNo = a.VendorNo,
                                    ProductGroupCode = a.ProductGroupCode,
                                    ParentCode = a.ParentCode,
                                    Size = a.Size,
                                    LastDateModified = a.LastDateModified
                                }).Distinct().OrderBy(x => x.ItemNo).ToList();

                    }
                    else if (status == "0") // Đang kinh doanh, không khóa món
                    {
                        // Danh sách ItemNo chưa khóa món, có trong table Items và table ItemBlocks
                        var data1 = (from a in db.Items
                                     join b in db.ItemBlocks on a.No equals b.ItemNo
                                     where b.Status == false && b.StoreNo == storeNo && a.Blocked == 0
                                     select new ItemListResponseModel
                                     {
                                         ItemNo = a.No,
                                         ItemName = a.Description,
                                         BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                         SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                         UnitPrice = a.UnitPrice,
                                         Blocked = a.Blocked,
                                         Status = "0",              // Không khóa món, đang kinh doanh
                                         StoreNo = b.StoreNo,
                                         VendorNo = a.VendorNo,
                                         ProductGroupCode = a.ProductGroupCode,
                                         ParentCode = a.ParentCode,
                                         Size = a.Size,
                                         LastDateModified = a.LastDateModified
                                     }).Distinct().OrderBy(x => x.ItemNo).ToList();

                        // Danh sách ItemNo có trong table ItemBlocks
                        var listBlockedItemNo = db.ItemBlocks.Where(x => x.StoreNo == storeNo)
                            .Select(x => x.ItemNo).ToList();
                        if (listBlockedItemNo != null && listBlockedItemNo.Count > 0)
                        {
                            // Danh sách ItemNo có trong table Items, và không có table ItemBlocks
                            var data2 = db.Items.Where(x => !listBlockedItemNo.Contains(x.No) && x.Blocked == 0)
                                         .Select(a => new ItemListResponseModel
                                         {
                                             ItemNo = a.No,
                                             ItemName = a.Description,
                                             BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                             SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                             UnitPrice = a.UnitPrice,
                                             Blocked = a.Blocked,
                                             Status = "0",          // Không khóa món, đang kinh doanh
                                             StoreNo = string.Empty,
                                             VendorNo = a.VendorNo,
                                             ProductGroupCode = a.ProductGroupCode,
                                             ParentCode = a.ParentCode,
                                             Size = a.Size,
                                             LastDateModified = a.LastDateModified
                                         }).OrderBy(x => x.ItemNo).ToList();

                            var data3 = data2.Union(data1).OrderBy(x => x.ItemNo).ToList();
                            data = (from a in data3
                                    select new ItemListResponseModel
                                    {
                                        ItemNo = a.ItemNo,
                                        ItemName = a.ItemName,
                                        BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                        SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                        UnitPrice = a.UnitPrice,
                                        Blocked = a.Blocked,
                                        Status = a.Status,
                                        StoreNo = a.StoreNo,
                                        VendorNo = a.VendorNo,
                                        ProductGroupCode = a.ProductGroupCode,
                                        ParentCode = a.ParentCode,
                                        Size = a.Size,
                                        LastDateModified = a.LastDateModified
                                    }).Distinct().OrderBy(x => x.ItemNo).ToList();
                        }
                        else
                        {
                            data = (from a in db.Items
                                    where a.Blocked == 0 // SP có hieu luc
                                    select new ItemListResponseModel
                                    {
                                        ItemNo = a.No,
                                        ItemName = a.Description,
                                        BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                        SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                        UnitPrice = a.UnitPrice,
                                        Blocked = a.Blocked,
                                        Status = "0",           // Không khóa món, đang kinh doanh
                                        StoreNo = string.Empty,
                                        VendorNo = a.VendorNo,
                                        ProductGroupCode = a.ProductGroupCode,
                                        ParentCode = a.ParentCode,
                                        Size = a.Size,
                                        LastDateModified = a.LastDateModified
                                    }).Distinct().OrderBy(x => x.ItemNo).ToList();
                        }
                    }
                    else if (status == "-1") // Tất cả
                    {
                        var listBlockedItemNo = db.ItemBlocks.Where(x => x.StoreNo == storeNo)
                            .Select(x => x.ItemNo).ToList();
                        if (listBlockedItemNo != null && listBlockedItemNo.Count > 0)
                        {
                            // Danh sách ItemNo có trong table Items, và không có table ItemBlocks
                            var data4 = db.Items.Where(x => !listBlockedItemNo.Contains(x.No) && x.Blocked == 0)
                                         .Select(a => new ItemListResponseModel
                                         {
                                             ItemNo = a.No,
                                             ItemName = a.Description,
                                             BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                             SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                             UnitPrice = a.UnitPrice,
                                             Blocked = a.Blocked,
                                             Status = "0",          // Không khóa món, đang kinh doanh
                                             StoreNo = string.Empty,
                                             VendorNo = a.VendorNo,
                                             ProductGroupCode = a.ProductGroupCode,
                                             ParentCode = a.ParentCode,
                                             Size = a.Size,
                                             LastDateModified = a.LastDateModified
                                         }).Distinct().OrderBy(x => x.ItemNo).ToList();

                            var data5 = (from a in db.Items
                                         join b in db.ItemBlocks on a.No equals b.ItemNo
                                         where b.StoreNo == storeNo && a.Blocked == 0
                                         select new ItemListResponseModel
                                         {
                                             ItemNo = a.No,
                                             ItemName = a.Description,
                                             BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                             SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                             UnitPrice = a.UnitPrice,
                                             Blocked = a.Blocked,
                                             Status = (b.Status) == true ? "1" : "0",
                                             StoreNo = b.StoreNo,
                                             VendorNo = a.VendorNo,
                                             ProductGroupCode = a.ProductGroupCode,
                                             ParentCode = a.ParentCode,
                                             Size = a.Size,
                                             LastDateModified = a.LastDateModified
                                         }).Distinct().OrderBy(x => x.ItemNo).ToList();

                            var data6 = data4.Union(data5).OrderBy(x => x.ItemNo).ToList();
                            data = (from a in data6
                                    select new ItemListResponseModel
                                    {
                                        ItemNo = a.ItemNo,
                                        ItemName = a.ItemName,
                                        BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                        SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                        UnitPrice = a.UnitPrice,
                                        Blocked = a.Blocked,
                                        Status = a.Status,
                                        StoreNo = a.StoreNo,
                                        VendorNo = a.VendorNo,
                                        ProductGroupCode = a.ProductGroupCode,
                                        ParentCode = a.ParentCode,
                                        Size = a.Size,
                                        LastDateModified = a.LastDateModified
                                    }).Distinct().OrderBy(x => x.ItemNo).ToList();
                        }
                        else
                        {
                            data = (from a in db.Items
                                    where a.Blocked == 0
                                    select new ItemListResponseModel
                                    {
                                        ItemNo = a.No,
                                        ItemName = a.Description,
                                        BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                        SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                        UnitPrice = a.UnitPrice,
                                        Blocked = a.Blocked,
                                        Status = "0",           // Đang kinh doanh
                                        StoreNo = string.Empty,
                                        VendorNo = a.VendorNo,
                                        ProductGroupCode = a.ProductGroupCode,
                                        ParentCode = a.ParentCode,
                                        Size = a.Size,
                                        LastDateModified = a.LastDateModified
                                    }).Distinct().OrderBy(x => x.ItemNo).ToList();
                        }
                    }

                    if (data == null || data.Count == 0)
                    {
                        return new List<ItemListResponseModel>();
                    }
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        data = data.Where(x => x.ItemNo.ToLower().Contains(searchText.ToLower()) || x.ItemName.ToLower().Contains(searchText.ToLower())).ToList();
                    }

                    totalRecord = data.Count();
                    data = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
                    return data.ToList();

                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ItemListResponseModel>();
            }
        }

        // Khóa món trên Central
        public ResultResponseModel CreateProductLockOrUnlock(CreateProductLockModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _totalRow = db.ItemBlocks.Select(x => x.Id).ToList();
                    if (_totalRow == null || _totalRow.Count == 0)
                    {
                        foreach (var item in req.ItemList)
                        {
                            var maxCounter = db.ItemBlocks.Max(x => x.Counter) ?? 0;
                            var insert = new EF.Central.ItemBlock
                            {
                                ItemNo = item.ItemNo,
                                UnitOfMeasure = item.UnitOfMeasure,
                                StoreNo = req.StoreNo,
                                Status = true, // = 1
                                UpdatedDate = req.CreatedDate,
                                Counter = maxCounter + 1,
                                Pkey = $"{req.StoreNo}-{item.ItemNo}"
                            };
                            db.ItemBlocks.Add(insert);
                            db.SaveChanges();
                        }
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            //Message = "Lưu khóa món thành công"
                        };
                    }
                    else
                    {
                        foreach (var item in req.ItemList)
                        {
                            var checkPkey = $"{req.StoreNo}-{item.ItemNo}";
                            var data = db.ItemBlocks.Where(x => x.Pkey == checkPkey).ToList();
                            if (data == null || data.Count == 0) // insert
                            {
                                var maxCounter = db.ItemBlocks.Max(x => x.Counter) ?? 0;
                                var insert = new EF.Central.ItemBlock
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UnitOfMeasure,
                                    StoreNo = req.StoreNo,
                                    Status = true,
                                    UpdatedDate = req.CreatedDate,
                                    Counter = maxCounter + 1,
                                    Pkey = $"{req.StoreNo}-{item.ItemNo}"
                                };
                                db.ItemBlocks.Add(insert);
                                db.SaveChanges();
                            }
                            else  // update : Status va Counter + 1
                            {
                                var maxCounter = db.ItemBlocks.Max(x => x.Counter);
                                data.FirstOrDefault().Status = (item.Status) == "1" ? false : true;
                                data.FirstOrDefault().Counter = maxCounter + 1;
                                db.SaveChanges();
                            }
                        }
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            //Message = "Lưu khóa/mở món thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public List<ProductLockListResponseModel> GetProductLockList(string storeNo, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                var data = new List<ProductLockListResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    data = (from a in db.ItemBlocks
                            join b in db.Items on a.ItemNo equals b.No
                            where a.Status == true && a.StoreNo == storeNo && b.Blocked == 0
                            select new ProductLockListResponseModel
                            {
                                ItemNo = a.ItemNo,
                                ItemName = b.Description,
                                UnitOfMeasure = a.UnitOfMeasure,
                                UnitPrice = b.UnitPrice,
                                StoreNo = a.StoreNo,
                                Status = a.Status,
                                UpdatedDate = a.UpdatedDate,
                                Pkey = a.Pkey
                            }).OrderBy(x => x.ItemNo).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ProductLockListResponseModel>();
                    }

                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        data = data.Where(x => x.ItemNo.ToLower().Contains(itemNo.ToLower())).ToList();
                    }

                    if (!string.IsNullOrEmpty(itemName))
                    {
                        data = data.Where(x => x.ItemName.ToLower() == itemName.ToLower().Trim()).ToList();
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        var isActive = true;
                        if (status == "1")
                        {
                            isActive = true;
                        }
                        else if (status == "0")
                        {
                            isActive = false;
                        }
                        data = data.Where(x => x.Status == isActive).ToList();
                    }

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        data = data.Where(x => x.ItemNo.ToLower().Contains(searchText.ToLower()) || x.ItemName.ToLower().Contains(searchText.ToLower().Trim())).ToList();
                    }
                    totalRecord = data.Count();
                    data = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
                    return data.ToList();
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ProductLockListResponseModel>();
            }
        }
        public ResultResponseModel CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    foreach (var item in req.ProductUnlockList)
                    {
                        var checkPkey = $"{req.StoreNo}-{item.ItemNo}";
                        var data = db.ItemBlocks.Where(x => x.Pkey == checkPkey).ToList();
                        var maxCounter = db.ItemBlocks.Max(x => x.Counter);
                        data.FirstOrDefault().Status = false;               // false = 0
                        data.FirstOrDefault().UpdatedDate = req.UpdatedDate;
                        data.FirstOrDefault().Counter = maxCounter + 1;

                        db.SaveChanges();
                    }

                    var totalItemLock = db.ItemBlocks.Where(x => x.StoreNo == req.StoreNo && x.Status == true).ToList();
                    var isLock = 0;
                    if (totalItemLock.Count == 0)
                    {
                        isLock = 0;   // ko co item nao bi khoa mon
                    }
                    else
                    {
                        isLock = 1;
                    }
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        //Message = "Lưu mở khóa món thành công",
                        Flag = isLock
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    Flag = 3   // xay ra loi
                };
            }
        }

        // ******* Yêu cầu mới (14/11/2022, A.Hồng): Cập nhật trực tiếp khóa món xuống DB ST/CH Phúc Long  *******

        public List<StoreProductLockResponseModel> POS_GetStoreProductLock(string IPAddress, string POSUser, string POSPass, string storeNo, string userName, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            var data = new List<StoreProductLockResponseModel>();
            var result = new List<StoreProductLockResponseModel>();

            if (!string.IsNullOrEmpty(IPAddress))
            {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");

                    var sql = "SELECT S.[No] AS StoreNo, S.[Name] AS StoreName, S.[StyleProfile], S.[ClosingMethod], I.[ItemNo], I.[UnitOfMeasure], I.[Status], I.[Pkey] ";
                    sql += " FROM [dbo].[Store] AS S ";
                    sql += " LEFT JOIN [dbo].[ItemBlock] AS I ON S.[No] = I.[StoreNo] ";
                    sql += " WHERE S.[ClosingMethod] = 0 ";
                   
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "ItemBlockList");
                            DataTable dt = ds.Tables["ItemBlockList"];
                            data = (from DataRow dr in dt.Rows
                                    select new StoreProductLockResponseModel()
                                    {
                                        StoreNo = dr["StoreNo"].ToString(),
                                        StoreName = dr["StoreName"].ToString(),
                                        StyleProfile = dr["StyleProfile"].ToString(),
                                        ClosingMethod = dr["ClosingMethod"].ToString(),
                                        ItemNo = dr["ItemNo"].ToString(),
                                        UnitOfMeasure = dr["UnitOfMeasure"].ToString(),
                                        Status = (dr["Status"].ToString() == "False") ? "0" : "1",
                                        IsLock = (dr["Status"].ToString() == null || dr["Status"].ToString() == string.Empty || dr["Status"].ToString() == "" || dr["Status"].ToString() == "0" || dr["Status"].ToString() == "False") ? "0": "1",
                                        Pkey = dr["Pkey"].ToString()
                                    }).ToList();
                          
                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                totalRecord = 0;
                                return new List<StoreProductLockResponseModel>();
                            }

                            if (!string.IsNullOrEmpty(searchText))
                            {
                                data = data.Where(x =>x.StoreNo.Contains(searchText.Trim()) || x.StoreName.Contains(searchText.Trim())).ToList();
                            }
                            data = data.GroupBy(x => x.StoreNo).Select(x =>x.Any(m =>m.IsLock == "1")? x.FirstOrDefault(m => m.IsLock == "1") : x.FirstOrDefault()).OrderBy(x =>x.StoreNo).ToList();
                            connection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        totalRecord = 0;
                        return new List<StoreProductLockResponseModel>();
                    }
            }

            totalRecord = data.Count();
            result = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
            return result;
        }
        public List<ProductLockListResponseModel> POS_GetItemLockList(string IPAddress,  string POSUser, string POSPass, string storeNo, string userName, string itemNo, string itemName, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            var data = new List<ProductLockListResponseModel>();
            var result = new List<ProductLockListResponseModel>();

            if (!string.IsNullOrEmpty(IPAddress))
            {
                string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");

                var sql = "SELECT a.[StoreNo], a.[ItemNo], b.[Description], a.[UnitOfMeasure], a.[Status], a.[Pkey], a.[UpdatedDate]";
                sql += " FROM [dbo].[ItemBlock] AS a";
                sql += " INNER JOIN [dbo].[Item] AS b ON a.[ItemNo] = b.[No]";
                sql += " WHERE a.[Status] = 1 AND b.[Blocked] = 0 AND a.[StoreNo] = '" + storeNo +"'";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectStr))
                    {
                        connection.Open();
                        SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                        DataSet ds = new DataSet();
                        da.Fill(ds, "ItemBlockList");
                        DataTable dt = ds.Tables["ItemBlockList"];
                        data = (from DataRow dr in dt.Rows
                                select new ProductLockListResponseModel()
                                {
                                    StoreNo = dr["StoreNo"].ToString(),
                                    ItemNo = dr["ItemNo"].ToString(),
                                    ItemName = dr["Description"].ToString(),
                                    UnitOfMeasure = dr["UnitOfMeasure"].ToString(),
                                    Status = bool.Parse(dr["Status"].ToString()),                                   
                                    UpdatedDate = dr["UpdatedDate"] == null ? default(DateTime?) : Convert.ToDateTime(dr["UpdatedDate"].ToString()),
                                    Pkey = dr["Pkey"].ToString()
                                }).ToList();

                        connection.Close();

                        if (data.Count == 0 || data == null) // Không có dữ liệu
                        {
                            totalRecord = 0;
                            return new List<ProductLockListResponseModel>();
                        }

                        if (!string.IsNullOrEmpty(itemNo))
                        {
                            data = data.Where(x => x.ItemNo.ToLower().Contains(itemNo.ToLower())).ToList();
                        }

                        if (!string.IsNullOrEmpty(itemName))
                        {
                            data = data.Where(x => x.ItemName.ToLower() == itemName.ToLower().Trim()).ToList();
                        }

                        if (!string.IsNullOrEmpty(status))
                        {
                            var isActive = true;
                            if (status == "1")
                            {
                                isActive = true;
                            }
                            else if (status == "0")
                            {
                                isActive = false;
                            }
                            data = data.Where(x => x.Status == isActive).ToList();
                        }

                        if (!string.IsNullOrEmpty(searchText))
                        {
                            data = data.Where(x => x.ItemNo.ToLower().Contains(searchText.ToLower()) || x.ItemName.ToLower().Contains(searchText.ToLower().Trim())).ToList();
                        }
                    }
                }
                catch (Exception e)
                {
                    totalRecord = 0;
                    return new List<ProductLockListResponseModel>();
                }
            }

            totalRecord = data.Count();
            result = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
            return result;

        }
        public List<ItemListResponseModel> POS_GetItemListForBlock(string IPAddress, string POSUser, string POSPass, string storeNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            var data = new List<ItemListResponseModel>();

            try
            {
                if (!string.IsNullOrEmpty(IPAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");

                    if (status == "1") // Đang tạm ngừng kinh doanh
                    {
                        // Danh sach san pham đang tạm ngừng kinh doanh (khóa món)

                        var sql = "SELECT b.[StoreNo], a.[No] as ItemNo, a.[Description] as ItemName, a.[BaseUnitOfMeasure], a.[SalesUnitOfMeasure], a.[Blocked], a.[VendorNo], a.[ProductGroupCode], a.[ParentCode], a.[Size], a.[LastDateModified]";
                        sql += " FROM [dbo].[Item] AS a";
                        sql += " INNER JOIN [dbo].[ItemBlock] AS b ON a.[No] = b.[ItemNo]";
                        sql += " WHERE b.[Status] = 1 AND a.[Blocked] = 0 AND b.[StoreNo] = '" + storeNo + "'";

                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();

                            SqlDataAdapter da1 = new SqlDataAdapter(sql, connection);
                            DataSet ds1 = new DataSet();
                            da1.Fill(ds1, "data1");
                            DataTable dt1 = ds1.Tables["data1"];

                            var data1 = new List<ItemListResponseModel>();
                            data1 = (from DataRow dr in dt1.Rows
                                     select new ItemListResponseModel()
                                     {
                                         StoreNo = dr["StoreNo"].ToString(),
                                         ItemNo = dr["ItemNo"].ToString(),
                                         ItemName = dr["ItemName"].ToString(),
                                         BaseUnitOfMeasure = dr["BaseUnitOfMeasure"].ToString(),
                                         SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                         Blocked = byte.Parse(dr["Blocked"].ToString()),
                                         Status = "1",                                      // true = 1 : đang tam ngung kinh doanh
                                         VendorNo = dr["VendorNo"].ToString(),
                                         ProductGroupCode = dr["ProductGroupCode"].ToString(),
                                         ParentCode = dr["ParentCode"].ToString(),
                                         Size = dr["Size"].ToString(),
                                         LastDateModified = string.IsNullOrEmpty(dr["LastDateModified"].ToString()) ? default(DateTime?) : Convert.ToDateTime(dr["LastDateModified"].ToString())
                                     }).ToList();

                            data = (from a in data1
                                    select new ItemListResponseModel()
                                    {
                                        StoreNo = a.StoreNo,
                                        ItemNo = a.ItemNo,
                                        ItemName = a.ItemName,
                                        BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                        SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                        Blocked = a.Blocked,
                                        Status = a.Status,
                                        VendorNo = a.VendorNo,
                                        ProductGroupCode = a.ProductGroupCode,
                                        ParentCode = a.ParentCode,
                                        Size = a.Size,
                                        LastDateModified = a.LastDateModified
                                    }).ToList();

                            // Dong ket noi
                            connection.Close();
                        }
                    }
                    else if (status == "0")  // Đang kinh doanh
                    {
                        // Danh sách ItemNo chưa khóa món (có trong table Items và table ItemBlocks)

                        var sql1 = "SELECT b.[StoreNo], a.[No] as ItemNo, a.[Description] as ItemName, a.[BaseUnitOfMeasure], a.[SalesUnitOfMeasure], a.[Blocked], a.[VendorNo], a.[ProductGroupCode], a.[ParentCode], a.[Size], a.[LastDateModified]";
                        sql1 += " FROM [dbo].[Item] AS a";
                        sql1 += " INNER JOIN [dbo].[ItemBlock] AS b ON a.[No] = b.[ItemNo]";
                        sql1 += " WHERE b.[Status] = 0 AND a.[Blocked] = 0 AND b.[StoreNo] = '" + storeNo + "'";

                        // Danh sách ItemNo có trong table ItemBlocks

                        var sql2 = "SELECT a.[ItemNo], a.[UnitOfMeasure] as SalesUnitOfMeasure, a.[Status], a.[Pkey], a.[StoreNo], b.[UnitPrice]";
                        sql2 += " FROM [dbo].[ItemBlock] AS a ";
                        sql2 += " INNER JOIN [dbo].[SalesPrice] AS b ON a.[ItemNo] = b.[ItemNo]";
                        sql2 += " WHERE a.[StoreNo] = '" + storeNo + "'";

                        // Danh sách ItemNo có trong table Items nhưng không có trong ItemBlocks

                        var sql3 = "SELECT a.[No] as ItemNo, a.[Description] as ItemName, a.[BaseUnitOfMeasure], a.[SalesUnitOfMeasure], a.[Blocked], a.[VendorNo], a.[ProductGroupCode], a.[ParentCode], a.[Size], a.[LastDateModified]";
                        sql3 += " FROM [dbo].[Item] AS a";
                        sql3 += " WHERE a.[Blocked] = 0 AND a.[No] NOT IN (SELECT b.[ItemNo] FROM [dbo].[ItemBlock] AS b WHERE b.[StoreNo] = '" + storeNo + "')";

                        // Danh sách ItemNo đang có hiệu lực, có trong table Items

                        var sql4 = "SELECT a.[No] as ItemNo, a.[Description] as ItemName, a.[BaseUnitOfMeasure], a.[SalesUnitOfMeasure], a.[Blocked], a.[VendorNo], a.[ProductGroupCode], a.[ParentCode], a.[Size], a.[LastDateModified]";
                        sql4 += " FROM [dbo].[Item] AS a";
                        sql4 += " WHERE a.[Blocked] = 0";

                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();

                            SqlDataAdapter da1 = new SqlDataAdapter(sql1, connection);
                            DataSet ds1 = new DataSet();
                            da1.Fill(ds1, "data1");
                            DataTable dt1 = ds1.Tables["data1"];

                            var data1 = new List<ItemListResponseModel>();
                            data1 = (from DataRow dr in dt1.Rows
                                     select new ItemListResponseModel()
                                     {
                                         StoreNo = dr["StoreNo"].ToString(),
                                         ItemNo = dr["ItemNo"].ToString(),
                                         ItemName = dr["ItemName"].ToString(),
                                         BaseUnitOfMeasure = dr["BaseUnitOfMeasure"].ToString(),
                                         SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                         Blocked = byte.Parse(dr["Blocked"].ToString()),
                                         Status = "0", // false = 0 : đang kinh doanh
                                         VendorNo = dr["VendorNo"].ToString(),
                                         ProductGroupCode = dr["ProductGroupCode"].ToString(),
                                         ParentCode = dr["ParentCode"].ToString(),
                                         Size = dr["Size"].ToString(),
                                         LastDateModified = string.IsNullOrEmpty(dr["LastDateModified"].ToString()) ? default(DateTime?) : Convert.ToDateTime(dr["LastDateModified"].ToString())
                                     }).ToList();

                            SqlDataAdapter da2 = new SqlDataAdapter(sql2, connection);
                            DataSet ds2 = new DataSet();
                            da2.Fill(ds2, "data2");
                            DataTable dt2 = ds2.Tables["data2"];

                            var data2 = new List<ItemListResponseModel>();
                            data2 = (from DataRow dr in dt2.Rows
                                     select new ItemListResponseModel()
                                     {
                                         StoreNo = dr["StoreNo"].ToString(),
                                         ItemNo = dr["ItemNo"].ToString(),
                                         SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                         Status = dr["Status"].ToString(),
                                         Pkey = dr["Pkey"].ToString()
                                     }).ToList();

                            if (data2 != null || data2.Count > 0)
                            {
                                SqlDataAdapter da3 = new SqlDataAdapter(sql3, connection);
                                DataSet ds3 = new DataSet();
                                da3.Fill(ds3, "data3");
                                DataTable dt3 = ds3.Tables["data3"];

                                var data3 = new List<ItemListResponseModel>();
                                data3 = (from DataRow dr in dt3.Rows
                                         select new ItemListResponseModel()
                                         {
                                             StoreNo = string.Empty,
                                             ItemNo = dr["ItemNo"].ToString(),
                                             ItemName = dr["ItemName"].ToString(),
                                             BaseUnitOfMeasure = dr["BaseUnitOfMeasure"].ToString(),
                                             SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                             Blocked = byte.Parse(dr["Blocked"].ToString()),
                                             Status = "0",                                      // false = 0 : chưa khóa món, đang kinh doanh
                                             VendorNo = dr["VendorNo"].ToString(),
                                             ProductGroupCode = dr["ProductGroupCode"].ToString(),
                                             ParentCode = dr["ParentCode"].ToString(),
                                             Size = dr["Size"].ToString(),
                                             LastDateModified = string.IsNullOrEmpty(dr["LastDateModified"].ToString()) ? default(DateTime?) : Convert.ToDateTime(dr["LastDateModified"].ToString())
                                         }).ToList();

                                // Danh sach ItemNo chua khoa mon ( có trong Table Items va Table ItemBlocks )

                                var dataAll = new List<ItemListResponseModel>();

                                dataAll = data3.Union(data1).OrderBy(x => x.ItemNo).ToList();

                                data = (from a in dataAll
                                        select new ItemListResponseModel()
                                        {
                                            StoreNo = a.StoreNo,
                                            ItemNo = a.ItemNo,
                                            ItemName = a.ItemName,
                                            BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                            SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                            Blocked = a.Blocked,
                                            Status = a.Status,
                                            VendorNo = a.VendorNo,
                                            ProductGroupCode = a.ProductGroupCode,
                                            ParentCode = a.ParentCode,
                                            Size = a.Size,
                                            LastDateModified = a.LastDateModified
                                        }).ToList();
                            }
                            else
                            {
                                SqlDataAdapter da4 = new SqlDataAdapter(sql4, connection);
                                DataSet ds4 = new DataSet();
                                da4.Fill(ds4, "data4");
                                DataTable dt4 = ds4.Tables["data4"];

                                var data4 = new List<ItemListResponseModel>();
                                data4 = (from DataRow dr in dt4.Rows
                                         select new ItemListResponseModel()
                                         {
                                             StoreNo = dr["StoreNo"].ToString(),
                                             ItemNo = dr["ItemNo"].ToString(),
                                             ItemName = dr["ItemName"].ToString(),
                                             BaseUnitOfMeasure = dr["BaseUnitOfMeasure"].ToString(),
                                             SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                             Blocked = byte.Parse(dr["Blocked"].ToString()),
                                             Status = "0",  // false = 0 : đang kinh doanh, chưa khóa món
                                             VendorNo = dr["VendorNo"].ToString(),
                                             ProductGroupCode = dr["ProductGroupCode"].ToString(),
                                             ParentCode = dr["ParentCode"].ToString(),
                                             Size = dr["Size"].ToString(),
                                             LastDateModified = string.IsNullOrEmpty(dr["LastDateModified"].ToString()) ? default(DateTime?) : Convert.ToDateTime(dr["LastDateModified"].ToString())
                                         }).ToList();

                                data = (from a in data4
                                        where a.Blocked == 0
                                        select new ItemListResponseModel
                                        {
                                            ItemNo = a.ItemNo,
                                            ItemName = a.ItemName,
                                            BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                            SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                            Blocked = a.Blocked,
                                            Status = "0",  // false = 0 : đang kinh doanh, chưa khóa món
                                            StoreNo = string.Empty,
                                            VendorNo = a.VendorNo,
                                            ProductGroupCode = a.ProductGroupCode,
                                            ParentCode = a.ParentCode,
                                            Size = a.Size,
                                            LastDateModified = a.LastDateModified
                                        }).Distinct().OrderBy(x => x.ItemNo).ToList();
                            }
                            // Dong ket noi
                            connection.Close();
                        }
                    }
                    else if (status == "-1")  // Tat ca
                    {
                        // Danh sach ItemNo co trong table ItemBlock
                        var sql5 = "SELECT a.[StoreNo], a.[ItemNo], a.[UnitOfMeasure] as SalesUnitOfMeasure, a.[Status], a.[Pkey], a.[UpdatedDate]";
                        sql5 += " FROM [dbo].[ItemBlock] AS a ";
                        sql5 += " WHERE a.[StoreNo] = '" + storeNo + "'";

                        // Danh sách ItemNo có trong table Items nhưng không có trong ItemBlocks
                        var sql6 = "SELECT a.[No] as ItemNo, a.[Description] as ItemName, a.[BaseUnitOfMeasure], a.[SalesUnitOfMeasure], a.[Blocked], a.[VendorNo], a.[ProductGroupCode], a.[ParentCode], a.[Size], a.[LastDateModified]";
                        sql6 += " FROM [dbo].[Item] AS a ";
                        sql6 += " WHERE a.[Blocked] = 0 AND a.[No] NOT IN (SELECT b.[ItemNo] FROM [dbo].[ItemBlock] AS b WHERE b.[StoreNo] = '" + storeNo + "') ";

                        // Danh sách ItemNo có trong table Items và table ItemBlocks
                        var sql7 = "SELECT a.[No] as ItemNo, a.[Description] as ItemName, a.[BaseUnitOfMeasure], a.[SalesUnitOfMeasure], b.[Status], a.[Blocked], a.[VendorNo], a.[ProductGroupCode], a.[ParentCode], a.[Size], a.[LastDateModified]";
                        sql7 += " FROM [dbo].[Item] AS a";
                        sql7 += " INNER JOIN [dbo].[ItemBlock] AS b ON a.[No] = b.[ItemNo]";
                        sql7 += " WHERE a.[Blocked] = 0 AND b.[StoreNo] = '" + storeNo + "'";

                        // Danh sách ItemNo đang có hiệu lực
                        var sql8 = "SELECT a.[No] as ItemNo, a.[Description] as ItemName, a.[BaseUnitOfMeasure], a.[SalesUnitOfMeasure], a.[Blocked], a.[VendorNo], a.[ProductGroupCode], a.[ParentCode], a.[Size], a.[LastDateModified]";
                        sql8 += " FROM [dbo].[Item] AS a";
                        sql8 += " WHERE a.[Blocked] = 0";

                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();

                            SqlDataAdapter da5 = new SqlDataAdapter(sql5, connection);
                            DataSet ds5 = new DataSet();
                            da5.Fill(ds5, "data5");
                            DataTable dt5 = ds5.Tables["data5"];

                            var data5 = new List<ItemListResponseModel>();
                            data5 = (from DataRow dr in dt5.Rows
                                     select new ItemListResponseModel()
                                     {
                                         StoreNo = dr["StoreNo"].ToString(),
                                         ItemNo = dr["ItemNo"].ToString(),
                                         SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                         Status = dr["Status"].ToString(),
                                         Pkey = dr["Pkey"].ToString()
                                     }).ToList();
                            
                            if (data5 != null || data5.Count > 0) // Danh sach ItemNo co trong table ItemBlock
                            {
                                SqlDataAdapter da6 = new SqlDataAdapter(sql6, connection);
                                DataSet ds6 = new DataSet();
                                da6.Fill(ds6, "data6");
                                DataTable dt6 = ds6.Tables["data6"];
                                var data6 = new List<ItemListResponseModel>();

                                // Danh sách ItemNo có trong table Items nhưng không có trong ItemBlocks
                                data6 = (from DataRow dr in dt6.Rows
                                         select new ItemListResponseModel()
                                         {
                                             StoreNo = string.Empty,
                                             ItemNo = dr["ItemNo"].ToString(),
                                             ItemName = dr["ItemName"].ToString(),
                                             BaseUnitOfMeasure = dr["BaseUnitOfMeasure"].ToString(),
                                             SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                             Blocked = byte.Parse(dr["Blocked"].ToString()),
                                             Status = string.Empty,
                                             VendorNo = dr["VendorNo"].ToString(),
                                             ProductGroupCode = dr["ProductGroupCode"].ToString(),
                                             ParentCode = dr["ParentCode"].ToString(),
                                             Size = dr["Size"].ToString(),
                                             LastDateModified = string.IsNullOrEmpty(dr["LastDateModified"].ToString()) ? default(DateTime?) : Convert.ToDateTime(dr["LastDateModified"].ToString())
                                         }).Distinct().OrderBy(x => x.ItemNo).ToList();

                                SqlDataAdapter da7 = new SqlDataAdapter(sql7, connection);
                                DataSet ds7 = new DataSet();
                                da7.Fill(ds7, "data7");
                                DataTable dt7 = ds7.Tables["data7"];
                                var data7 = new List<ItemListResponseModel>();

                                // Danh sách ItemNo có trong table Items và table ItemBlocks

                                data7 = (from DataRow dr in dt7.Rows
                                         select new ItemListResponseModel()
                                         {
                                             StoreNo = string.Empty,
                                             ItemNo = dr["ItemNo"].ToString(),
                                             ItemName = dr["ItemName"].ToString(),
                                             BaseUnitOfMeasure = dr["BaseUnitOfMeasure"].ToString(),
                                             SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                             Blocked = byte.Parse(dr["Blocked"].ToString()),
                                             Status = dr["Status"].ToString(),
                                             VendorNo = dr["VendorNo"].ToString(),
                                             ProductGroupCode = dr["ProductGroupCode"].ToString(),
                                             ParentCode = dr["ParentCode"].ToString(),
                                             Size = dr["Size"].ToString(),
                                             LastDateModified = string.IsNullOrEmpty(dr["LastDateModified"].ToString()) ? default(DateTime?) : Convert.ToDateTime(dr["LastDateModified"].ToString())
                                         }).Distinct().OrderBy(x => x.ItemNo).ToList();

                                var dataAll = new List<ItemListResponseModel>();
                                dataAll = data6.Union(data7).OrderBy(x => x.ItemNo).ToList(); 
                                data = (from a in dataAll
                                        select new ItemListResponseModel
                                        {
                                            ItemNo = a.ItemNo,
                                            ItemName = a.ItemName,
                                            BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                            SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                            Blocked = a.Blocked,
                                            Status = (a.Status.ToString() == "False" || string.IsNullOrEmpty(a.Status.ToString())) ? "0":"1",
                                            StoreNo = a.StoreNo,
                                            VendorNo = a.VendorNo,
                                            ProductGroupCode = a.ProductGroupCode,
                                            ParentCode = a.ParentCode,
                                            Size = a.Size,
                                            LastDateModified = a.LastDateModified
                                        }).Distinct().OrderBy(x => x.ItemNo).ToList();
                            }
                            else
                            {
                                SqlDataAdapter da8 = new SqlDataAdapter(sql8, connection);
                                DataSet ds8 = new DataSet();
                                da8.Fill(ds8, "data8");
                                DataTable dt8 = ds8.Tables["data8"];
                                var data8 = new List<ItemListResponseModel>();
                                data8 = (from DataRow dr in dt8.Rows
                                         select new ItemListResponseModel()
                                         {
                                             StoreNo = string.Empty,
                                             ItemNo = dr["ItemNo"].ToString(),
                                             ItemName = dr["Description"].ToString(),
                                             BaseUnitOfMeasure = dr["BaseUnitOfMeasure"].ToString(),
                                             SalesUnitOfMeasure = dr["SalesUnitOfMeasure"].ToString(),
                                             Blocked = byte.Parse(dr["Blocked"].ToString()),
                                             Status = "0",  // false = 0 : đang kinh doanh, chua khoa mon
                                             VendorNo = dr["VendorNo"].ToString(),
                                             ProductGroupCode = dr["ProductGroupCode"].ToString(),
                                             ParentCode = dr["ParentCode"].ToString(),
                                             Size = dr["Size"].ToString(),
                                             LastDateModified = string.IsNullOrEmpty(dr["LastDateModified"].ToString()) ? default(DateTime?) : Convert.ToDateTime(dr["LastDateModified"].ToString())
                                         }).Distinct().OrderBy(x => x.ItemNo).ToList();

                                data = (from a in data8
                                        where a.Blocked == 0
                                        select new ItemListResponseModel
                                        {
                                            ItemNo = a.ItemNo,
                                            ItemName = a.ItemName,
                                            BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                                            SalesUnitOfMeasure = a.SalesUnitOfMeasure,
                                            Blocked = a.Blocked,
                                            Status = "0",  // false = 0, chưa khóa món, đang kinh doanh
                                            StoreNo = string.Empty,
                                            VendorNo = a.VendorNo,
                                            ProductGroupCode = a.ProductGroupCode,
                                            ParentCode = a.ParentCode,
                                            Size = a.Size,
                                            LastDateModified = a.LastDateModified
                                        }).Distinct().OrderBy(x => x.ItemNo).ToList();
                            }
                            // Dong ket noi
                            connection.Close();
                        }
                    }
                }

                if (data == null || data.Count == 0)
                {
                    totalRecord = 0;
                    return new List<ItemListResponseModel>();
                }

                if (!string.IsNullOrEmpty(searchText))
                {
                    data = data.Where(x => x.ItemNo.ToLower().Contains(searchText.ToLower()) || x.ItemName.ToLower().Contains(searchText.ToLower())).ToList();
                }

                totalRecord = data.Count();
                data = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();

                return data;

            }
            catch(Exception ex)
            {
                totalRecord = 0;
                return new List<ItemListResponseModel>();
            }
        }

        public ResultResponseModel POS_CreateProductLockOrUnlock(CreateProductLockModel req, string POSUser, string POSPass)
        {
            var data = new List<StoreProductLockResponseModel>();
            try
            {
                var IPAddress = req.POSTerminal;
                string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");

                var sql1 = "SELECT I.[ID], I.[StoreNo], I.[ItemNo], I.[UnitOfMeasure], I.[Status], I.[UpdatedDate], I.[Counter], I.[Pkey]";
                    sql1 += " FROM [dbo].[ItemBlock] AS I";

                using (SqlConnection connection = new SqlConnection(connectStr))
                {
                    connection.Open();

                    SqlDataAdapter da = new SqlDataAdapter(sql1, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds, "ItemBlock");
                    DataTable dt = ds.Tables["ItemBlock"];

                    data = (from DataRow dr in dt.Rows
                            select new StoreProductLockResponseModel()
                            {
                                ID = int.Parse(dr["ID"].ToString()),
                                StoreNo = dr["StoreNo"].ToString(),
                                ItemNo = dr["ItemNo"].ToString(),
                                UnitOfMeasure = dr["UnitOfMeasure"].ToString(),
                                Status = dr["Status"].ToString(),
                                UpdatedDate = string.IsNullOrEmpty(dr["UpdatedDate"].ToString()) ? default(DateTime): Convert.ToDateTime(dr["UpdatedDate"].ToString()),
                                Counter = long.Parse(dr["Counter"].ToString()),
                                Pkey = dr["Pkey"].ToString()
                            }).ToList();

                    if (data == null || data.Count == 0)
                    {
                        foreach (var item in req.ItemList)
                        {
                            var maxCounter = data.Max(x => x.Counter) ?? 0;
                           
                            var listItem = new EF.Central.ItemBlock
                            {
                                ItemNo = item.ItemNo,
                                UnitOfMeasure = item.UnitOfMeasure,
                                StoreNo = req.StoreNo,
                                Status = true, // 1
                                UpdatedDate = req.CreatedDate,
                                Counter = maxCounter + 1,
                                Pkey = $"{req.StoreNo}-{item.ItemNo}"
                            };

                            SqlCommand insert = new SqlCommand();
                            insert.Connection = connection;
                            insert.CommandText = "INSERT INTO ItemBlock (ItemNo, UnitOfMeasure, StoreNo, Status, UpdatedDate, Counter, Pkey) VALUES (@ItemNo, @UnitOfMeasure, @StoreNo, @Status, @UpdatedDate, @Counter, @Pkey);";
                            SqlParameterCollection pc = insert.Parameters;
                            pc.AddWithValue("@ItemNo", listItem.ItemNo);
                            pc.AddWithValue("@UnitOfMeasure", listItem.UnitOfMeasure);
                            pc.AddWithValue("@StoreNo", listItem.StoreNo);
                            pc.AddWithValue("@Status", true);  // 1 =true
                            pc.AddWithValue("@UpdatedDate", listItem.UpdatedDate);
                            pc.AddWithValue("@Counter", listItem.Counter);
                            pc.AddWithValue("@Pkey", listItem.Pkey);
                            insert.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        foreach (var item in req.ItemList)
                        {
                            var pkey = $"{req.StoreNo}-{item.ItemNo}";
                            var listData = data.Where(x => x.Pkey == pkey).ToList();
                            if (listData == null || listData.Count == 0) // Thêm mới
                            {
                                var maxCounter1 = data.Max(x => x.Counter);
                                var listItem = new EF.Central.ItemBlock
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UnitOfMeasure,
                                    StoreNo = req.StoreNo,
                                    Status = true,  // 1= true, Khóa món
                                    UpdatedDate = req.CreatedDate,
                                    Counter = maxCounter1 + 1,
                                    Pkey = $"{req.StoreNo}-{item.ItemNo}"
                                };

                                SqlCommand insert1 = new SqlCommand();
                                insert1.Connection = connection;
                                insert1.CommandText = "INSERT INTO ItemBlock (ItemNo, UnitOfMeasure, StoreNo, Status, UpdatedDate, Counter, Pkey) VALUES (@ItemNo, @UnitOfMeasure, @StoreNo, @Status, @UpdatedDate, @Counter, @Pkey);";
                                SqlParameterCollection pc = insert1.Parameters;
                                pc.AddWithValue("@ItemNo", listItem.ItemNo);
                                pc.AddWithValue("@UnitOfMeasure", listItem.UnitOfMeasure);
                                pc.AddWithValue("@StoreNo", listItem.StoreNo);
                                pc.AddWithValue("@Status", true);  // 1 = true, khóa món
                                pc.AddWithValue("@UpdatedDate", listItem.UpdatedDate);
                                pc.AddWithValue("@Counter", listItem.Counter);
                                pc.AddWithValue("@Pkey", listItem.Pkey);
                                insert1.ExecuteNonQuery();                               
                            }
                            else  // update : Status va Counter + 1
                            {
                                var maxCounter2 = data.Max(x => x.Counter);
                                listData.FirstOrDefault().Status = item.Status;
                                listData.FirstOrDefault().Counter = maxCounter2 + 1;
                                listData.FirstOrDefault().UpdatedDate = DateTime.Now;

                                SqlCommand update = new SqlCommand();
                                update.Connection = connection;
                                update.CommandText = "UPDATE ItemBlock SET Status = @Status, UpdatedDate = @UpdatedDate, Counter = @Counter WHERE Pkey='" + pkey + "'";
                                update.Parameters.Clear();
                                SqlParameterCollection pc = update.Parameters;
                               
                                pc.AddWithValue("@Status", true);  // 1 = true, khóa món
                                pc.AddWithValue("@UpdatedDate", listData.FirstOrDefault()?.UpdatedDate);
                                pc.AddWithValue("@Counter", listData.FirstOrDefault()?.Counter);

                                try
                                {
                                    update.ExecuteNonQuery();
                                }
                                catch(Exception ex)
                                {
                                    return new ResultResponseModel
                                    {
                                        Status = Model.Enums.ResultEnum.ErrorSystem,
                                        Message = ex.Message,
                                        Flag = 3   // xay ra loi
                                    };
                                }
                            }
                        }
                    }

                    connection.Close();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Lưu khóa/mở món thành công"
                    };

                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel POS_CreateProductUnlockByModal(CreateProductUnlockModel req, string POSUser, string POSPass)
        {
            var data1 = new List<StoreProductLockResponseModel>();
            var data2 = new List<StoreProductLockResponseModel>();
            try
            {
                var IPAddress = req.POSTerminal;
                //string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, "POS", "POS@1234", "StorePLH");

                string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, POSUser, POSPass, "StorePLH");

                var sql1 = "SELECT I.[ID], I.[StoreNo], I.[ItemNo], I.[UnitOfMeasure], I.[Status], I.[UpdatedDate], I.[Counter], I.[Pkey]";
                sql1 += " FROM [dbo].[ItemBlock] AS I ";

                var sql2 = "SELECT I.[ID], I.[StoreNo], I.[ItemNo], I.[UnitOfMeasure], I.[Status], I.[UpdatedDate], I.[Counter], I.[Pkey]";
                sql2 += " FROM [dbo].[ItemBlock] AS I ";
                sql2 += " WHERE I.[Status] = 1 ";

                using (SqlConnection connection = new SqlConnection(connectStr))
                {
                    connection.Open();
                    SqlDataAdapter da = new SqlDataAdapter(sql1, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds, "ItemBlock");
                    DataTable dt = ds.Tables["ItemBlock"];
                    data1 = (from DataRow dr in dt.Rows
                            select new StoreProductLockResponseModel()
                            {
                                ID = int.Parse(dr["ID"].ToString()),
                                StoreNo = dr["StoreNo"].ToString(),
                                ItemNo = dr["ItemNo"].ToString(),
                                UnitOfMeasure = dr["UnitOfMeasure"].ToString(),
                                Status = dr["Status"].ToString(),
                                UpdatedDate = string.IsNullOrEmpty(dr["UpdatedDate"].ToString()) ? default(DateTime) : Convert.ToDateTime(dr["UpdatedDate"].ToString()),
                                Counter = long.Parse(dr["Counter"].ToString()),
                                Pkey = dr["Pkey"].ToString()
                            }).ToList();

                    if (data1 == null || data1.Count == 0)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Không có thông tin. Vui lòng kiểm tra lại"
                        };
                    }

                    // Lưu mở món
                    foreach (var item in req.ProductUnlockList)
                    {
                        var pkey = $"{req.StoreNo}-{item.ItemNo}";
                        var listData = data1.Where(x => x.Pkey == pkey).ToList();
                        var maxCounter = data1.Max(x => x.Counter);

                        listData.FirstOrDefault().Status = "0";    // = 0 (false: không khóa món)
                        listData.FirstOrDefault().Counter = maxCounter + 1;
                        listData.FirstOrDefault().UpdatedDate = DateTime.Now;

                        SqlCommand update = new SqlCommand();
                        update.Connection = connection;
                        update.CommandText = "UPDATE ItemBlock SET Status = @Status, UpdatedDate = @UpdatedDate, Counter = @Counter WHERE Pkey= '"+ pkey + "'";
                        update.Parameters.Clear();
                        SqlParameterCollection pc = update.Parameters;
                        pc.AddWithValue("@Status", listData.FirstOrDefault()?.Status);    // = 0 (false: không khóa món)
                        pc.AddWithValue("@UpdatedDate", listData.FirstOrDefault()?.UpdatedDate);
                        pc.AddWithValue("@Counter", listData.FirstOrDefault()?.Counter);

                        try
                        {
                            update.ExecuteNonQuery();
                        }
                        catch(Exception ex)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.ErrorSystem,
                                Message = ex.Message,
                                Flag = 3   // xay ra loi
                            };
                        }
                        
                    }

                    SqlDataAdapter da2 = new SqlDataAdapter(sql2, connection);
                    DataSet ds2 = new DataSet();
                    da2.Fill(ds2, "ItemBlock");
                    DataTable dt1 = ds2.Tables["ItemBlock"];
                    data2 = (from DataRow dr in dt1.Rows
                            select new StoreProductLockResponseModel()
                            {
                                ID = int.Parse(dr["ID"].ToString()),
                                StoreNo = dr["StoreNo"].ToString(),
                                ItemNo = dr["ItemNo"].ToString(),
                                UnitOfMeasure = dr["UnitOfMeasure"].ToString(),
                                Status = dr["Status"].ToString(),
                                UpdatedDate = string.IsNullOrEmpty(dr["UpdatedDate"].ToString()) ? default(DateTime) : Convert.ToDateTime(dr["UpdatedDate"].ToString()),
                                Counter = long.Parse(dr["Counter"].ToString()),
                                Pkey = dr["Pkey"].ToString()
                            }).ToList();

                    var totalItemLock = data2.Where(x => x.StoreNo == req.StoreNo).ToList();
                    var isLock = 0;
                    if (totalItemLock.Count == 0)
                    {
                        isLock = 0;   // ko co item nao bi khoa mon
                    }
                    else
                    {
                        isLock = 1;
                    }

                    connection.Close();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Lưu mở khóa món thành công",
                        Flag = isLock
                    }; 
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    Flag = 3   // xay ra loi
                };
            }
        }





    }
}
