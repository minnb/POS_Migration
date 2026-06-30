using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.PartnerMD;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.SetupItem;
using VCM.BLUEPOS.Model.SetupPromotion;


namespace VCM.BLUEPOS.Data.SetupItem
{
    public interface ISetupItemData
    {
        List<ItemProcedureModel> LoadItem(string keyWord, out int totalRecord, int pageNumber, int pageSize);
        ViewCapNhatSanPhamModel ViewInfoItem(string Id);
        List<SetupItemModel> ListItemSetup(string itemHeader, string itemNo, string description, out int total, int skip, int take);
        List<SetupItemModel> ListItemByGroup(string itemHeader, string itemNo, string description, string groupCode, out int total, int skip, int take);
        Tuple<bool, string, SetupItemRequestModel> SaveItem(SetupItemRequestModel model);
        List<SalesTypeModel> ListSalesType();
        List<POSGroupModel> GetPOSGroup(string searchText);
        List<POSGroupModel> GetParentPOSGroup();
        POSGroupModel GetDetailPosGroup(int id);
        List<PosGroupItemModel> GetPosGroupItemByGroupCode(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize);
        List<PosGroupItemModel> GetPosGroupItemIsValid(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize);

        // Kenh ban hang doi tac
        List<OptionModel> LoadCategoryByPartner();
        List<ItemPartnerResponseModel> LoadItemPartnerSales(string keyWord, string mch2, string mch5, out int totalRecord, int pageNumber, int pageSize);
        Tuple<bool, string, SetupItemPartnerModel> SaveItemListForPartner(SetupItemPartnerModel model);
        List<ItemPartnerResponseModel> GetItemPartnerList(string salestype, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize);
        ViewSetupItemPartnerDetailModel ViewInforItemDetailByPartner(string itemno);
        ResultResponseModel UpdateItemDetailByPartner(UpdateItemPartnerModel req);

        // Pos group
        ResultResponseModel CreatePosGroup(POSGroupModel req);
        ResultResponseModel UpdatePosGroup(POSGroupModel req, bool isParent);
        ResultResponseModel UpdateStatusPosGroup(int id, int status);
        ResultResponseModel CreatePosGroupItem(PosGroupItemModel req);
        ResultResponseModel DeletePosGroupItem(int id);

        // GrabFood
        List<OptionComboxModel> LoadComboxCategorybyGrabFood();
        List<CheckItemGrabFoodModel> CheckItemGrabFood(string itemNo);
        List<ToppingGrabFoodModel> GetToppingByGrabFood();
        Tuple<bool, string, SetupItemRequestModel> SaveItemV2(SetupItemRequestModel model);


    }

    public class SetupItemData : ISetupItemData
    {
        private readonly object lockRequest = new object();
        public List<ItemProcedureModel> LoadItem(string keyWord, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ItemProcedureModel>();

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<ItemProcedureModel>("[dbo].[SP_LOAD_ITEM] @Keyword, @PageSize, @PageNumber",
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ItemProcedureModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ItemProcedureModel>();
            }
        }

        public ViewCapNhatSanPhamModel ViewInfoItem(string Id)
        {
            var result = new ViewCapNhatSanPhamModel();
            try
            {
                // [PERF] Chạy 3 lần gọi PartnerMD song song thay vì tuần tự
                var taskGrabCategory = Task.Run(() => string.IsNullOrEmpty(Id) ? new List<OptionComboxModel>() : LoadComboxCategorybyGrabFood());
                var taskGrabCheck    = Task.Run(() => string.IsNullOrEmpty(Id) ? new List<CheckItemGrabFoodModel>() : CheckItemGrabFood(Id));
                var taskGrabTopping  = Task.Run(() => string.IsNullOrEmpty(Id) ? new List<ToppingGrabFoodModel>() : GetToppingByGrabFood());

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var listUOM = VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Get<List<UnitOfMeasureModel>>(VCM.BLUEPOS.Data.Caching.CacheKeys.UOMs);
                    if (listUOM == null)
                    {
                        listUOM = db.UnitOfMeasures.Select(a => new UnitOfMeasureModel
                        {
                            Code = a.Code,
                            Description = a.Description
                        }).ToList();
                        VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.UOMs, listUOM, TimeSpan.FromHours(1));
                    }

                    var listCombo = VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Get<List<ComboModel>>(VCM.BLUEPOS.Data.Caching.CacheKeys.Combos);
                    if (listCombo == null)
                    {
                        listCombo = db.Comboxes.Select(a => new ComboModel
                        {
                            Name = a.Name,
                            Value = a.Value,
                            Type = a.Type
                        }).ToList();
                        var vatCode = db.POSVATCodes.Where(d => d.Status == true)
                            .Select(a => new ComboModel
                            {
                                Name = a.Description,
                                Value = a.VATCode,
                                Type = "POSVAT"
                            }).ToList();
                        listCombo.AddRange(vatCode);
                        VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.Combos, listCombo, TimeSpan.FromHours(1));
                    }
                    else
                    {
                        listCombo = new List<ComboModel>(listCombo);
                    }

                    var listBarcode = db.Barcodes.Where(d => d.ItemNo == Id).Select(a => new BarcodeRequestModel
                    {
                        Barcode = a.BarcodeNo,
                        UOM = a.UnitOfMeasureCode
                    }).ToList();

                    // [PERF] Fix correlated subquery: tách truy vấn GroupCode ra ngoài infoItem
                    var posGroupCode    = db.PosGroupItems.Where(d => d.ItemNo == Id).Select(d => d.GroupCode).FirstOrDefault() ?? "";
                    var posWebGroupCode = db.PosWebGroupItems.Where(d => d.ItemNo == Id).Select(d => d.GroupCode).FirstOrDefault() ?? "";

                    var infoItem = db.Items.Where(d => d.No == Id).Select(a => new Model.SetupItem.ItemModel
                    {
                        No = a.No,
                        No2 = a.No2,
                        Description = a.Description,
                        SearchDescription = a.SearchDescription,
                        LongDescription = a.LongDescription,
                        BaseUnitOfMeasure = a.BaseUnitOfMeasure,
                        VendorNo = a.VendorNo,
                        Blocked = a.Blocked,
                        PriceIncludesVAT = a.PriceIncludesVAT,
                        TaxGroupCode = a.TaxGroupCode,
                        ProductGroupCode = a.ProductGroupCode,
                        BlockedVINID = a.BlockedVINID,
                        ItemFamilyCode = a.ItemFamilyCode,
                        ParentCode = a.ParentCode,
                        Size = a.Size,
                        ImageName = a.ImageName,
                        IsVAT = a.IsVAT
                    }).FirstOrDefault();

                    if (infoItem != null)
                    {
                        infoItem.GroupCode    = posGroupCode;
                        infoItem.GroupCodeWeb = posWebGroupCode;
                    }

                    // [PERF] Fix N+1: thay vì subquery mỗi row, pre-fetch lookup 1 lần duy nhất
                    var extraRows = db.ItemExtras.Where(a => a.ItemNo == Id).ToList();

                    var itemNoRefs = extraRows.Select(a => a.ItemNoRef).Distinct().ToList();
                    var saleTypeCodes = extraRows.Select(a => a.SaleTypeCode)
                                                 .Where(c => !string.IsNullOrEmpty(c))
                                                 .Distinct().ToList();

                    // Batch query Items theo ItemNoRef (tránh OR-condition trên index)
                    var itemRefByNo  = db.Items.Where(d => itemNoRefs.Contains(d.No))
                                              .Select(d => new { d.No, d.Description }).ToList();
                    var itemRefByNo2 = db.Items.Where(d => itemNoRefs.Contains(d.No2))
                                              .Select(d => new { No = d.No2, d.Description }).ToList();
                    var itemRefDict = itemRefByNo.Concat(itemRefByNo2)
                                                 .GroupBy(d => d.No)
                                                 .ToDictionary(g => g.Key, g => g.First().Description);

                    // Batch query SalesOrderTypes (tránh gọi 2 lần mỗi row)
                    var saleTypeDict = saleTypeCodes.Count > 0
                        ? db.SalesOrderTypes.Where(d => saleTypeCodes.Contains(d.Code))
                                            .Select(d => new { d.Code, d.Description })
                                            .ToDictionary(d => d.Code, d => d.Description)
                        : new Dictionary<string, string>();

                    // Join trong memory – không phát sinh thêm bất kỳ SQL nào
                    var listExtra = extraRows.Select(a => new ItemExtraModel
                    {
                        ItemNo = a.ItemNo,
                        UnitOfMeasure = a.UnitOfMeasure,
                        ItemNoRef = a.ItemNoRef,
                        ItemNameRef = itemRefDict.ContainsKey(a.ItemNoRef ?? "") ? itemRefDict[a.ItemNoRef] : "",
                        UnitOfMeasureRef = a.UnitOfMeasureRef,
                        Description = a.Description,
                        SaleTypeCode = a.SaleTypeCode,
                        SaleTypeName = !string.IsNullOrEmpty(a.SaleTypeCode) && saleTypeDict.ContainsKey(a.SaleTypeCode)
                                            ? saleTypeDict[a.SaleTypeCode] : "",
                        Type = a.Type,
                        Status = a.Status,
                        IsDefault = a.IsDefault,
                        Seq = a.Seq
                    }).ToList();

                    var listItemOption = db.ItemOptions.Where(d => d.ItemNo == Id).Select(a => new ItemOptionModel
                    {
                        ItemNo = a.ItemNo,
                        ItemNoOption = a.ItemNoOption,
                        OptionType = a.OptionType,
                        OptionName = a.OptionName,
                        Status = a.Status,
                        IsDefault = a.IsDefault,
                        Seq = a.Seq
                    }).ToList();

                    var listPosGroup = VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Get<List<POSGroupModel>>(VCM.BLUEPOS.Data.Caching.CacheKeys.PosGroups);
                    if (listPosGroup == null)
                    {
                        listPosGroup = db.PosGroups.Where(d => d.Status == 1).Select(a => new POSGroupModel
                        {
                            GroupCode = a.GroupCode,
                            Description = a.Description,
                            Level = a.Level,
                            ParentCode = a.ParentCode,
                            Seq = a.Seq
                        }).ToList();
                        VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.PosGroups, listPosGroup, TimeSpan.FromHours(1));
                    }

                    // Chờ 3 task GrabFood hoàn thành
                    Task.WaitAll(taskGrabCategory, taskGrabCheck, taskGrabTopping);

                    result = new ViewCapNhatSanPhamModel
                    {
                        Id = Id,
                        InfoItem = infoItem,
                        ListUOM = listUOM,
                        ListCombo = listCombo,
                        ListBarcode = listBarcode,
                        ListItemExtra = listExtra,
                        ListItemOption = listItemOption,
                        ListPOSGroup = listPosGroup,
                        ListCategoryGrabFood = taskGrabCategory.Result,
                        ListItemGrabFood = taskGrabCheck.Result,
                        ListToppingGrab = taskGrabTopping.Result
                    };
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<CheckItemGrabFoodModel> CheckItemGrabFood(string itemNo)
        {
            try
            {
                var cached = VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Get<List<CheckItemGrabFoodModel>>(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodCheck(itemNo));
                if (cached != null) return cached;

                var result = new List<CheckItemGrabFoodModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 5;
                    var data = db.GrabFood_items.Where(d => d.Id == itemNo).Select(a => new CheckItemGrabFoodModel
                    {
                        ItemNo = a.Id,
                        ItemName = a.Name,
                        CategoryID = a.CategoryID,
                        CategoryName = a.CategoryName
                    });
                    result = data.ToList();
                    VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodCheck(itemNo), result, TimeSpan.FromMinutes(10));
                    return result;
                }
            }
            catch (Exception ex)
            {
                var empty = new List<CheckItemGrabFoodModel>();
                VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodCheck(itemNo), empty, TimeSpan.FromMinutes(5));
                return empty;
            }
        }

        public List<ToppingGrabFoodModel> GetToppingByGrabFood()
        {
            try
            {
                var cached = VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Get<List<ToppingGrabFoodModel>>(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodToppings);
                if (cached != null) return cached;

                var result = new List<ToppingGrabFoodModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 5;
                    var data = (from a in db.GrabFood_modifier
                                where a.ModifierGroupID == "TOPPING" && a.AvailableStatus == "AVAILABLE"
                                select new ToppingGrabFoodModel
                                {
                                    Id = a.Id,
                                    Name = a.Name,
                                    AvailableStatus = a.AvailableStatus
                                });

                    var listItemNo = data.Select(a =>a.Id).ToList();
                    using (var db1 = new CentralMDPartnerContainer())
                    {
                        var datalist = db1.Items.Where(a => listItemNo.Contains(a.No) && a.Blocked == 0).ToList();
                        var listItem = datalist.Select(a => new ToppingGrabFoodModel
                        {
                            Id = a.No,
                            Name = a.Description,
                            UOM = a.SalesUnitOfMeasure
                        }).OrderBy(x => x.Id).Distinct().ToList();
                        result = listItem;
                    }
                    VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodToppings, result, TimeSpan.FromMinutes(30));
                    return result;
                }
            }
            catch (Exception ex)
            {
                var emptyResult = new List<ToppingGrabFoodModel>();
                VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodToppings, emptyResult, TimeSpan.FromMinutes(5));
                return emptyResult;
            }
        }

        public List<SetupItemModel> ListItemSetup(string itemHeader, string itemNo, string description, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.Items.Where(d => d.No != itemHeader).Select(a => new SetupItemModel
                    {
                        ItemNo = a.No,
                        ItemNo2 = a.No2,
                        ItemName = a.Description,
                        UOM = a.SalesUnitOfMeasure
                    });
                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        data = data.Where(m => m.ItemNo == itemNo);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        data = data.Where(m => m.ItemName.ToLower().Contains(description.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderBy(d => d.ItemNo).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return new List<SetupItemModel>();
            }
        }

        public List<SetupItemModel> ListItemByGroup(string itemHeader, string itemNo, string description, string groupCode, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.Items.Where(d => d.No != itemHeader && d.ItemCategoryCode == groupCode && d.Blocked == 0).Select(a => new SetupItemModel
                    {
                        ItemNo = a.No,
                        ItemNo2 = a.No2,
                        ItemName = a.Description,
                        UOM = a.SalesUnitOfMeasure
                    });

                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        data = data.Where(m => m.ItemNo == itemNo);
                    }

                    if (!string.IsNullOrEmpty(description))
                    {
                        data = data.Where(m => m.ItemName.ToLower().Contains(description.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderBy(d => d.ItemNo).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return new List<SetupItemModel>();
            }
        }
        public Tuple<bool, string, SetupItemRequestModel> SaveItem(SetupItemRequestModel model)
        {
            var result = new Tuple<bool, string, SetupItemRequestModel>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var item = model.itemHeader;
                            var checkItem = db.Items.FirstOrDefault(d => d.No == item.ItemNo);
                            //byte blocked = item.IsBlocked == true ? byte.Parse("1") : byte.Parse("0");
                            var dateTime = DateTime.Now;
                            var counterItem = db.Items.Max(a => a.Counter) ?? 0;
                            if (checkItem != null)
                            {
                                checkItem.Counter = counterItem + 1;
                                // null = không thông báo thay ảnh → giữ nguyên; rỗng = xóa ảnh; có giá trị = đặt tên mới
                                if (item.ImgName != null)
                                    checkItem.ImageName = !string.IsNullOrEmpty(item.ImgName) ? $"{item.ItemNo}.jpg" : "";
                                checkItem.No2 = item.ItemNo2;
                                checkItem.Description = item.ItemName;
                                checkItem.SearchDescription = item.ItemDecs;
                                checkItem.LongDescription = item.ItemName;
                                checkItem.BaseUnitOfMeasure = item.UOM;
                                checkItem.TaxGroupCode = item.TAX;
                                checkItem.SalesUnitOfMeasure = item.UOM;
                                checkItem.ItemCategoryCode = item.ItemType;
                                checkItem.ItemFamilyCode = item.ItemType;
                                checkItem.ParentCode = item.ParentCode;
                                checkItem.Size = item.Size;
                                checkItem.IsVAT = item.IsVAT;
                                checkItem.Blocked = item.IsBlocked ? (byte)1 : (byte)0;
                                checkItem.LastDateModified = dateTime;
                            }
                            else
                            {
                                var noNew = 65000000L;
                                // Load toàn bộ mã số về client để lấy Max chính xác (tránh so sánh string)
                                var maxNoNumeric = db.Items.Where(d => SqlFunctions.IsNumeric(d.No) == 1)
                                                           .Select(d => d.No)
                                                           .ToList()
                                                           .Select(n => Convert.ToInt64(n))
                                                           .DefaultIfEmpty(noNew - 1)
                                                           .Max();
                                var noSave = !string.IsNullOrEmpty(item.ItemNo) ? item.ItemNo : "" + (maxNoNumeric + 1);

                                var blocked = item.IsBlocked == true ? (byte)1 : (byte)0;
                                var insertItem = new Item
                                {
                                    No = noSave,
                                    No2 = item.ItemNo2,
                                    Description = item.ItemName,
                                    SearchDescription = item.ItemDecs,
                                    LongDescription = item.ItemName,
                                    BaseUnitOfMeasure = item.UOM,
                                    StatisticsGroup = 0,
                                    CommissionGroup = 0,
                                    UnitPrice = 0,
                                    CostingMethod = 0,
                                    UnitCost = 0,
                                    StandardCost = 0,
                                    VendorNo = "PLH",
                                    VendorItemNo = "",
                                    MaximumInventory = 0,
                                    ReorderQuantity = 0,
                                    GrossWeight = 0,
                                    NetWeight = 0,
                                    UnitsPerParcel = 0,
                                    UnitVolume = 0,
                                    Blocked = blocked,
                                    LastDateModified = dateTime,
                                    PriceIncludesVAT = 1,
                                    TaxGroupCode = item.TAX,
                                    PreventNegativeInventory = 0,
                                    MinimumOrderQuantity = 0,
                                    MaximumOrderQuantity = 0,
                                    SafetyStockQuantity = 0,
                                    OrderMultiple = 0,
                                    SalesUnitOfMeasure = item.UOM,
                                    ManufacturerCode = "",
                                    ItemCategoryCode = item.ItemType,
                                    ProductGroupCode = "",
                                    ServiceItemGroup = "",
                                    ItemTrackingCode = "",
                                    ProductionBOMNo = "",
                                    BlockedVINID = 0,
                                    CommonItemNo = "",
                                    ItemFamilyCode = item.ItemType,
                                    DivisionCode = "",
                                    KeyinginPrice = 0,
                                    ZeroPriceValid = 0,
                                    Counter = counterItem + 1,
                                    Pkey = noSave,
                                    ParentCode = item.ParentCode,
                                    Size = item.Size,
                                    ImageName = !string.IsNullOrEmpty(item.ImgName) ? $"{noSave}.jpg" : "",
                                    IsVAT = item.IsVAT,
                                    WeightscaleDesc = ""
                                };
                                db.Items.Add(insertItem);
                                model.itemHeader.ItemNo = string.IsNullOrEmpty(model.itemHeader.ItemNo) ? noSave : model.itemHeader.ItemNo;
                                item = model.itemHeader;
                            }

                            var listBarCode = model.listBarcode;
                            var checkBarcode = db.Barcodes.Where(d => d.ItemNo == item.ItemNo).ToList();
                            if (checkBarcode != null && checkBarcode.Count > 0)
                            {
                                db.Barcodes.RemoveRange(checkBarcode);
                            }
                            if (listBarCode != null && listBarCode.Count > 0)
                            {
                                var listStringBarcode = listBarCode.Select(a => a.Barcode).ToList();
                                if (string.IsNullOrEmpty(item.ItemNo))
                                {
                                    var checkBarcodeInDB = db.Barcodes.Where(d => listStringBarcode.Contains(d.BarcodeNo)).ToList();

                                    if (checkBarcodeInDB != null && checkBarcodeInDB.Count > 0)
                                    {
                                        result = new Tuple<bool, string, SetupItemRequestModel>(false, $"Đã tồn tại barcode ({string.Join(",", checkBarcodeInDB.Select(a => a.BarcodeNo))}) trong hệ thống", model);
                                        return result;
                                    }
                                }
                                else
                                {
                                    var listBarcodeOld = db.Barcodes.Where(d => d.ItemNo == item.ItemNo).Select(a => a.BarcodeNo).ToList();
                                    var checkListBarcodeNew = listStringBarcode.Except(listBarcodeOld).ToList();
                                    if (checkListBarcodeNew.Count > 0)
                                    {
                                        var checkBarcodeInDB = db.Barcodes.Where(d => checkListBarcodeNew.Contains(d.BarcodeNo)).ToList();

                                        if (checkBarcodeInDB != null && checkBarcodeInDB.Count > 0)
                                        {
                                            result = new Tuple<bool, string, SetupItemRequestModel>(false, $"Đã tồn tại barcode ({string.Join(",", checkBarcodeInDB.Select(a => a.BarcodeNo))}) trong hệ thống", model);
                                            return result;
                                        }
                                    }
                                }

                                var counterBarcode = db.Barcodes.Max(a => a.Counter) ?? 0;
                                var insertBarcode = listBarCode.Select(a => new EF.Central.Barcode
                                {
                                    BarcodeNo = a.Barcode,
                                    ItemNo = model.itemHeader.ItemNo,
                                    UnitOfMeasureCode = a.UOM,
                                    ShowForItem = 0,
                                    Description = "",
                                    Blocked = 0,
                                    LastDateModified = dateTime,
                                    VariantCode = "",
                                    DiscountPercent = 0,
                                    Counter = counterBarcode + 1,
                                    Pkey = a.Barcode

                                });
                                db.Barcodes.AddRange(insertBarcode);
                            }

                            var listTopping = model.listTopping;

                            //var checkTopping = db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Topping").ToList();
                            //if (checkTopping != null && checkTopping.Count > 0)
                            //{
                            //    db.ItemExtras.RemoveRange(checkTopping);
                            //}

                            //16/05/2023
                            db.ItemExtras.RemoveRange(db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Topping"));
                            //
                            var itemExtraCounter = db.ItemExtras.Max(a => a.Counter) ?? 0;
                            if (listTopping != null && listTopping.Count > 0)
                            {
                                //var counterTopping = db.ItemExtras.Max(a => a.Counter) ?? 0;
                                var sttTopping = 1;
                                if (listTopping.Count > 0)
                                {
                                    listTopping.ForEach(c => c.Seq = sttTopping++);
                                }

                                var insertTopping = listTopping.Select(a => new ItemExtra
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UOM,
                                    ItemNoRef = a.ItemNoRef,
                                    UnitOfMeasureRef = a.UOMRef,
                                    Description = a.ItemNameRef,
                                    SaleTypeCode = "",
                                    Type = "Topping",
                                    Pkey = $"{item.ItemNo}-Topping",
                                    Counter = itemExtraCounter + 1,
                                    Status = 1,
                                    Seq = a.Seq,
                                    LastDateModified = dateTime
                                });
                                db.ItemExtras.AddRange(insertTopping);
                            }

                            var listCup = model.listCup;
                            //var checkCup = db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Cup").ToList();
                            //if (checkCup != null && checkCup.Count > 0)
                            //{
                            //    db.ItemExtras.RemoveRange(checkCup);
                            //}

                            db.ItemExtras.RemoveRange(db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Cup"));

                            if (listCup != null && listCup.Count > 0)
                            {
                                //var counterCup = db.ItemExtras.Max(a => a.Counter) ?? 0;

                                var sttCup = 1;
                                if (listCup.Count > 0)
                                {
                                    listCup.ForEach(c => c.Seq = sttCup++);
                                }
                                var insertCup = listCup.Select(a => new ItemExtra
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UOM,
                                    ItemNoRef = a.ItemNoRef,
                                    UnitOfMeasureRef = a.UOMRef,
                                    Description = a.ItemNameRef,
                                    SaleTypeCode = a.SalesTypeCode,
                                    Type = "Cup",
                                    Pkey = $"{item.ItemNo}-Cup",
                                    Counter = itemExtraCounter + 1,
                                    Status = 1,
                                    IsDefault = a.IsDefault,
                                    Seq = a.Seq,
                                    LastDateModified = dateTime
                                });

                                db.ItemExtras.AddRange(insertCup);
                            }

                            var listOption = model.listOption;

                            //var checkOption = db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Option").ToList();
                            //if (checkOption != null && checkOption.Count > 0)
                            //{
                            //    db.ItemExtras.RemoveRange(checkOption);
                            //}

                            db.ItemExtras.RemoveRange(db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Option"));

                            if (listOption != null && listOption.Count > 0)
                            {
                                //var counterOption = db.ItemExtras.Max(a => a.Counter) ?? 0;

                                var sttOption = 1;
                                if (listOption.Count > 0)
                                {
                                    listOption.ForEach(c => c.Seq = sttOption++);
                                }

                                var insertOption = listOption.Select(a => new ItemExtra
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UOM,

                                    ItemNoRef = a.ItemNoRef,
                                    UnitOfMeasureRef = a.UOMRef,
                                    Description = a.ItemNameRef,
                                    SaleTypeCode = "",
                                    Type = "Option",
                                    Pkey = $"{item.ItemNo}-Option",
                                    Counter = itemExtraCounter + 1,
                                    Status = 1,
                                    Seq = a.Seq,
                                    LastDateModified = dateTime
                                });
                                db.ItemExtras.AddRange(insertOption);
                            }

                            var listDinhLuong = model.listDinhLuong;

                            //var checkDinhLuong = db.ItemOptions.Where(d => d.ItemNo == item.ItemNo).ToList();
                            //if (checkDinhLuong != null && checkDinhLuong.Count > 0)
                            //{
                            //    db.ItemOptions.RemoveRange(checkDinhLuong);
                            //}

                            db.ItemOptions.RemoveRange(db.ItemOptions.Where(d => d.ItemNo == item.ItemNo));

                            if (listDinhLuong != null && listDinhLuong.Count > 0)
                            {
                                var counterDinhLuong = db.ItemOptions.Max(a => a.Counter) ?? 0;

                                var sttDinhLuong = 1;
                                if (listDinhLuong.Count > 0)
                                {
                                    listDinhLuong.ForEach(c => c.Seq = sttDinhLuong++);
                                }

                                var insertDL = listDinhLuong.Select(a => new ItemOption
                                {
                                    ItemNo = item.ItemNo,
                                    ItemNoOption = a.ItemNoOption,
                                    OptionType = a.OptionType,
                                    OptionName = a.OptionName,
                                    Quantity = 0,
                                    Counter = counterDinhLuong + 1,
                                    Pkey = $"{a.ItemNoOption}-{item.ItemNo}",
                                    Status = 1,
                                    LastDateModified = dateTime,
                                    IsDefault = a.IsDefault,
                                    Seq = a.Seq
                                });
                                db.ItemOptions.AddRange(insertDL);
                            }

                            var listPosGroup = model.listPosGroup;
                            if (listPosGroup != null && listPosGroup.Count > 0)
                            {
                                var firstGroup = listPosGroup.FirstOrDefault();
                                var checkPosGroup = db.PosGroupItems.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                                var counterPosGroup = db.PosGroupItems.Max(a => a.Counter) ?? 0;
                                if (checkPosGroup != null)
                                {
                                    checkPosGroup.GroupCode = firstGroup.GroupCode;
                                    checkPosGroup.UnitOfMeasure = item.UOM;
                                    checkPosGroup.LastDateModified = dateTime;
                                    checkPosGroup.Counter = counterPosGroup + 1;
                                }
                                else
                                {
                                    var insertPosGroup = new PosGroupItem
                                    {
                                        ItemNo = item.ItemNo,
                                        GroupCode = firstGroup.GroupCode,
                                        UnitOfMeasure = item.UOM,
                                        Seq = 1,
                                        Counter = counterPosGroup + 1,
                                        Pkey = item.ItemNo,
                                        Status = 1,
                                        LastDateModified = dateTime
                                    };
                                    db.PosGroupItems.Add(insertPosGroup);
                                }
                            }

                            var listPosGroupWeb = model.listPosGroupWeb;
                            if (listPosGroupWeb != null && listPosGroupWeb.Count > 0)
                            {
                                var firstGroup = listPosGroupWeb.FirstOrDefault();
                                var checkPosGroup = db.PosWebGroupItems.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                                var counterPosGroup = db.PosWebGroupItems.Max(a => a.Counter) ?? 0;
                                if (checkPosGroup != null)
                                {
                                    checkPosGroup.GroupCode = firstGroup.GroupCode;
                                    checkPosGroup.UnitOfMeasure = item.UOM;
                                    checkPosGroup.LastDateModified = dateTime;
                                    checkPosGroup.Counter = counterPosGroup + 1;
                                }
                                else
                                {
                                    var insertPosGroup = new PosWebGroupItem
                                    {
                                        ItemNo = item.ItemNo,
                                        GroupCode = firstGroup.GroupCode,
                                        UnitOfMeasure = item.UOM,
                                        Seq = 1,
                                        Counter = counterPosGroup + 1,
                                        Pkey = item.ItemNo,
                                        Status = 1,
                                        LastDateModified = dateTime
                                    };
                                    db.PosWebGroupItems.Add(insertPosGroup);
                                }
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, SetupItemRequestModel>(true, $"Cập nhật thành công sản phẩm {item.ItemNo}", model);

                        }
                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, SetupItemRequestModel>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }

        public Tuple<bool, string, SetupItemRequestModel> SaveItemV2(SetupItemRequestModel model)
        {
            var result = new Tuple<bool, string, SetupItemRequestModel>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var item = model.itemHeader;
                            var checkItem = db.Items.FirstOrDefault(d => d.No == item.ItemNo);
                            
                            var dateTime = DateTime.Now;
                            var counterItem = db.Items.Max(a => a.Counter) ?? 0;
                            if (checkItem != null)
                            {
                                checkItem.Counter = counterItem + 1;
                                // null = không thay ảnh → giữ nguyên; rỗng = xóa ảnh; có giá trị = đặt tên mới
                                if (item.ImgName != null)
                                    checkItem.ImageName = !string.IsNullOrEmpty(item.ImgName) ? $"{item.ItemNo}.jpg" : "";
                                checkItem.No2 = item.ItemNo2;
                                checkItem.Description = item.ItemName;
                                checkItem.SearchDescription = item.ItemDecs;
                                checkItem.LongDescription = item.ItemName;
                                checkItem.BaseUnitOfMeasure = item.UOM;
                                checkItem.TaxGroupCode = item.TAX;
                                checkItem.SalesUnitOfMeasure = item.UOM;
                                checkItem.ItemCategoryCode = item.ItemType;
                                checkItem.ItemFamilyCode = item.ItemType;
                                checkItem.ParentCode = item.ParentCode;
                                checkItem.Size = item.Size;
                                checkItem.IsVAT = item.IsVAT;
                                checkItem.Blocked = item.IsBlocked ? (byte)1 : (byte)0;
                                checkItem.LastDateModified = dateTime;
                            }
                            else
                            {
                                var noNew = 65000000L;
                                var maxNoNumeric = db.Items.Where(d => SqlFunctions.IsNumeric(d.No) == 1)
                                                           .Select(d => d.No)
                                                           .ToList()
                                                           .Select(n => Convert.ToInt64(n))
                                                           .DefaultIfEmpty(noNew - 1)
                                                           .Max();
                                var noSave = !string.IsNullOrEmpty(item.ItemNo) ? item.ItemNo : "" + (maxNoNumeric + 1);

                                var blocked = item.IsBlocked == true ? (byte)1 : (byte)0;
                                var insertItem = new Item
                                {
                                    No = noSave,
                                    No2 = item.ItemNo2,
                                    Description = item.ItemName,
                                    SearchDescription = item.ItemDecs,
                                    LongDescription = item.ItemName,
                                    BaseUnitOfMeasure = item.UOM,
                                    StatisticsGroup = 0,
                                    CommissionGroup = 0,
                                    UnitPrice = 0,
                                    CostingMethod = 0,
                                    UnitCost = 0,
                                    StandardCost = 0,
                                    VendorNo = "PLH",
                                    VendorItemNo = "",
                                    MaximumInventory = 0,
                                    ReorderQuantity = 0,
                                    GrossWeight = 0,
                                    NetWeight = 0,
                                    UnitsPerParcel = 0,
                                    UnitVolume = 0,
                                    Blocked = blocked,
                                    LastDateModified = dateTime,
                                    PriceIncludesVAT = 1,
                                    TaxGroupCode = item.TAX,
                                    PreventNegativeInventory = 0,
                                    MinimumOrderQuantity = 0,
                                    MaximumOrderQuantity = 0,
                                    SafetyStockQuantity = 0,
                                    OrderMultiple = 0,
                                    SalesUnitOfMeasure = item.UOM,
                                    ManufacturerCode = "",
                                    ItemCategoryCode = item.ItemType,
                                    ProductGroupCode = "",
                                    ServiceItemGroup = "",
                                    ItemTrackingCode = "",
                                    ProductionBOMNo = "",
                                    BlockedVINID = 0,
                                    CommonItemNo = "",
                                    ItemFamilyCode = item.ItemType,
                                    DivisionCode = "",
                                    KeyinginPrice = 0,
                                    ZeroPriceValid = 0,
                                    Counter = counterItem + 1,
                                    Pkey = noSave,
                                    ParentCode = item.ParentCode,
                                    Size = item.Size,
                                    ImageName = !string.IsNullOrEmpty(item.ImgName) ? $"{noSave}.jpg" : "",
                                    IsVAT = item.IsVAT,
                                    WeightscaleDesc = ""
                                };
                                db.Items.Add(insertItem);
                                model.itemHeader.ItemNo = string.IsNullOrEmpty(model.itemHeader.ItemNo) ? noSave : model.itemHeader.ItemNo;
                                item = model.itemHeader;
                            }

                            var listBarCode = model.listBarcode;
                            var checkBarcode = db.Barcodes.Where(d => d.ItemNo == item.ItemNo).ToList();
                            if (checkBarcode != null && checkBarcode.Count > 0)
                            {
                                db.Barcodes.RemoveRange(checkBarcode);
                            }
                            if (listBarCode != null && listBarCode.Count > 0)
                            {
                                var listStringBarcode = listBarCode.Select(a => a.Barcode).ToList();
                                if (string.IsNullOrEmpty(item.ItemNo))
                                {
                                    var checkBarcodeInDB = db.Barcodes.Where(d => listStringBarcode.Contains(d.BarcodeNo)).ToList();

                                    if (checkBarcodeInDB != null && checkBarcodeInDB.Count > 0)
                                    {
                                        result = new Tuple<bool, string, SetupItemRequestModel>(false, $"Đã tồn tại barcode ({string.Join(",", checkBarcodeInDB.Select(a => a.BarcodeNo))}) trong hệ thống", model);
                                        return result;
                                    }
                                }
                                else
                                {
                                    var listBarcodeOld = db.Barcodes.Where(d => d.ItemNo == item.ItemNo).Select(a => a.BarcodeNo).ToList();
                                    var checkListBarcodeNew = listStringBarcode.Except(listBarcodeOld).ToList();
                                    if (checkListBarcodeNew.Count > 0)
                                    {
                                        var checkBarcodeInDB = db.Barcodes.Where(d => checkListBarcodeNew.Contains(d.BarcodeNo)).ToList();

                                        if (checkBarcodeInDB != null && checkBarcodeInDB.Count > 0)
                                        {
                                            result = new Tuple<bool, string, SetupItemRequestModel>(false, $"Đã tồn tại barcode ({string.Join(",", checkBarcodeInDB.Select(a => a.BarcodeNo))}) trong hệ thống", model);
                                            return result;
                                        }
                                    }
                                }

                                var counterBarcode = db.Barcodes.Max(a => a.Counter) ?? 0;
                                var insertBarcode = listBarCode.Select(a => new EF.Central.Barcode
                                {
                                    BarcodeNo = a.Barcode,
                                    ItemNo = model.itemHeader.ItemNo,
                                    UnitOfMeasureCode = a.UOM,
                                    ShowForItem = 0,
                                    Description = "",
                                    Blocked = 0,
                                    LastDateModified = dateTime,
                                    VariantCode = "",
                                    DiscountPercent = 0,
                                    Counter = counterBarcode + 1,
                                    Pkey = a.Barcode

                                });
                                db.Barcodes.AddRange(insertBarcode);
                            }

                            var listTopping = model.listTopping;
                            db.ItemExtras.RemoveRange(db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Topping"));
                            //
                            var itemExtraCounter = db.ItemExtras.Max(a => a.Counter) ?? 0;
                            if (listTopping != null && listTopping.Count > 0)
                            {
                                var sttTopping = 1;
                                if (listTopping.Count > 0)
                                {
                                    listTopping.ForEach(c => c.Seq = sttTopping++);
                                }

                                var insertTopping = listTopping.Select(a => new ItemExtra
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UOM,
                                    ItemNoRef = a.ItemNoRef,
                                    UnitOfMeasureRef = a.UOMRef,
                                    Description = a.ItemNameRef,
                                    SaleTypeCode = "",
                                    Type = "Topping",
                                    Pkey = $"{item.ItemNo}-Topping",
                                    Counter = itemExtraCounter + 1,
                                    Status = 1,
                                    Seq = a.Seq,
                                    LastDateModified = dateTime
                                });
                                db.ItemExtras.AddRange(insertTopping);
                            }

                            var listCup = model.listCup;
                            db.ItemExtras.RemoveRange(db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Cup"));
                            if (listCup != null && listCup.Count > 0)
                            {
                                //var counterCup = db.ItemExtras.Max(a => a.Counter) ?? 0;

                                var sttCup = 1;
                                if (listCup.Count > 0)
                                {
                                    listCup.ForEach(c => c.Seq = sttCup++);
                                }
                                var insertCup = listCup.Select(a => new ItemExtra
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UOM,
                                    ItemNoRef = a.ItemNoRef,
                                    UnitOfMeasureRef = a.UOMRef,
                                    Description = a.ItemNameRef,
                                    SaleTypeCode = a.SalesTypeCode,
                                    Type = "Cup",
                                    Pkey = $"{item.ItemNo}-Cup",
                                    Counter = itemExtraCounter + 1,
                                    Status = 1,
                                    IsDefault = a.IsDefault,
                                    Seq = a.Seq,
                                    LastDateModified = dateTime
                                });

                                db.ItemExtras.AddRange(insertCup);
                            }

                            var listOption = model.listOption;
                            db.ItemExtras.RemoveRange(db.ItemExtras.Where(d => d.ItemNo == item.ItemNo && d.Type == "Option"));

                            if (listOption != null && listOption.Count > 0)
                            {
                                var sttOption = 1;
                                if (listOption.Count > 0)
                                {
                                    listOption.ForEach(c => c.Seq = sttOption++);
                                }

                                var insertOption = listOption.Select(a => new ItemExtra
                                {
                                    ItemNo = item.ItemNo,
                                    UnitOfMeasure = item.UOM,

                                    ItemNoRef = a.ItemNoRef,
                                    UnitOfMeasureRef = a.UOMRef,
                                    Description = a.ItemNameRef,
                                    SaleTypeCode = "",
                                    Type = "Option",
                                    Pkey = $"{item.ItemNo}-Option",
                                    Counter = itemExtraCounter + 1,
                                    Status = 1,
                                    Seq = a.Seq,
                                    LastDateModified = dateTime
                                });
                                db.ItemExtras.AddRange(insertOption);
                            }

                            var listDinhLuong = model.listDinhLuong;
                            db.ItemOptions.RemoveRange(db.ItemOptions.Where(d => d.ItemNo == item.ItemNo));

                            if (listDinhLuong != null && listDinhLuong.Count > 0)
                            {
                                var counterDinhLuong = db.ItemOptions.Max(a => a.Counter) ?? 0;

                                var sttDinhLuong = 1;
                                if (listDinhLuong.Count > 0)
                                {
                                    listDinhLuong.ForEach(c => c.Seq = sttDinhLuong++);
                                }

                                var insertDL = listDinhLuong.Select(a => new ItemOption
                                {
                                    ItemNo = item.ItemNo,
                                    ItemNoOption = a.ItemNoOption,
                                    OptionType = a.OptionType,
                                    OptionName = a.OptionName,
                                    Quantity = 0,
                                    Counter = counterDinhLuong + 1,
                                    Pkey = $"{a.ItemNoOption}-{item.ItemNo}",
                                    Status = 1,
                                    LastDateModified = dateTime,
                                    IsDefault = a.IsDefault,
                                    Seq = a.Seq
                                });
                                db.ItemOptions.AddRange(insertDL);
                            }

                            var listPosGroup = model.listPosGroup;
                            if (listPosGroup != null && listPosGroup.Count > 0)
                            {
                                var firstGroup = listPosGroup.FirstOrDefault();
                                var checkPosGroup = db.PosGroupItems.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                                var counterPosGroup = db.PosGroupItems.Max(a => a.Counter) ?? 0;
                                if (checkPosGroup != null)
                                {
                                    checkPosGroup.GroupCode = firstGroup.GroupCode;
                                    checkPosGroup.UnitOfMeasure = item.UOM;
                                    checkPosGroup.LastDateModified = dateTime;
                                    checkPosGroup.Counter = counterPosGroup + 1;
                                }
                                else
                                {
                                    var insertPosGroup = new PosGroupItem
                                    {
                                        ItemNo = item.ItemNo,
                                        GroupCode = firstGroup.GroupCode,
                                        UnitOfMeasure = item.UOM,
                                        Seq = 1,
                                        Counter = counterPosGroup + 1,
                                        Pkey = item.ItemNo,
                                        Status = 1,
                                        LastDateModified = dateTime
                                    };
                                    db.PosGroupItems.Add(insertPosGroup);
                                }
                            }

                            var listPosGroupWeb = model.listPosGroupWeb;
                            if (listPosGroupWeb != null && listPosGroupWeb.Count > 0)
                            {
                                var firstGroup = listPosGroupWeb.FirstOrDefault();
                                var checkPosGroup = db.PosWebGroupItems.FirstOrDefault(d => d.ItemNo == item.ItemNo);
                                var counterPosGroup = db.PosWebGroupItems.Max(a => a.Counter) ?? 0;
                                if (checkPosGroup != null)
                                {
                                    checkPosGroup.GroupCode = firstGroup.GroupCode;
                                    checkPosGroup.UnitOfMeasure = item.UOM;
                                    checkPosGroup.LastDateModified = dateTime;
                                    checkPosGroup.Counter = counterPosGroup + 1;
                                }
                                else
                                {
                                    var insertPosGroup = new PosWebGroupItem
                                    {
                                        ItemNo = item.ItemNo,
                                        GroupCode = firstGroup.GroupCode,
                                        UnitOfMeasure = item.UOM,
                                        Seq = 1,
                                        Counter = counterPosGroup + 1,
                                        Pkey = item.ItemNo,
                                        Status = 1,
                                        LastDateModified = dateTime
                                    };
                                    db.PosWebGroupItems.Add(insertPosGroup);
                                }
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, SetupItemRequestModel>(true, $"Cập nhật thành công sản phẩm {item.ItemNo}", model);

                        }
                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, SetupItemRequestModel>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }

        public List<SalesTypeModel> ListSalesType()
        {
            var result = new List<SalesTypeModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypeModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }

        // ----- 12/12/2022: San pham ben kenh doi tac ------
        public List<OptionModel> LoadCategoryByPartner()
        {
            using (var db = new PartnerMDEntities())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var data = (from a in db.Shopee_dish_group
                                where a.display_order == 0
                                select new OptionModel
                                {
                                    Value = a.partner_dish_group_id,
                                    Text = a.name
                                }).ToList();
                    return data;
                }
                catch (Exception ex)
                {
                    return (default(List<OptionModel>));
                }
            }
        }

        public List<ItemPartnerResponseModel> LoadItemPartnerSales(string keyWord, string mch2, string mch5, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ItemPartnerResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    data = db.Database.SqlQuery<ItemPartnerResponseModel>("[dbo].[GET_ITEM_LIST_FOR_PARTNER_SALES] @Keyword, @MCH2, @MCH5, @PageSize, @PageNumber",
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@MCH2", mch2 ?? string.Empty),  // Nganh hang
                            new SqlParameter("@MCH5", mch5 ?? string.Empty),  // Ten san pham / Mat hang
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ItemPartnerResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ItemPartnerResponseModel>();
            }
        }
        public Tuple<bool, string, SetupItemPartnerModel> SaveItemListForPartner(SetupItemPartnerModel model)
        {
            var result = new Tuple<bool, string, SetupItemPartnerModel>(false, "", model);
            lock (lockRequest)
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var listData = db.ItemSalesOnApps.Select(x => x.PartnerCode).FirstOrDefault().ToString();
                            if (listData == null || listData.Count() == 0)
                            {
                                foreach (var item in model.ListItemPartner)
                                {
                                    if (item.CheckItem == true)  // San pham duoc chọn
                                    {
                                        var insert = new EF.PartnerMD.ItemSalesOnApp
                                        {
                                            PartnerCode = "PLH",
                                            AppCode = item.SalesType,
                                            ItemNo = item.ItemNo,
                                            ItemName = item.ItemName,
                                            Uom = item.UOM,
                                            Blocked = true,
                                            ItemGroup = item.CategoryCode,
                                            ItemGroupName = item.CategoryName,
                                            //PictureId = Convert.FromBase64String(item.Images),
                                            CrtDate = DateTime.Now,
                                            ChgeDate = DateTime.Now
                                        };
                                        db.ItemSalesOnApps.Add(insert);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var item in model.ListItemPartner)
                                {
                                    if (item.CheckItem == true)
                                    {
                                        var insert = new EF.PartnerMD.ItemSalesOnApp
                                        {
                                            PartnerCode = "PLH",
                                            AppCode = item.SalesType,
                                            ItemNo = item.ItemNo,
                                            ItemName = item.ItemName,
                                            Uom = item.UOM,
                                            Blocked = true,
                                            ItemGroup = item.CategoryCode,
                                            ItemGroupName = item.CategoryName,
                                            //PictureId = Convert.FromBase64String(item.Images),
                                            CrtDate = DateTime.Now,
                                            ChgeDate = DateTime.Now
                                        };
                                        db.ItemSalesOnApps.Add(insert);
                                    }
                                }
                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, SetupItemPartnerModel>(true, $"Cập nhật thành công", model);
                        }
                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, SetupItemPartnerModel>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }

        public List<ItemPartnerResponseModel> GetItemPartnerList(string salestype, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ItemPartnerResponseModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    data = db.Database.SqlQuery<ItemPartnerResponseModel>("[dbo].[NOWFOOD_GET_ITEM_LIST] @SaleType, @StoreNo, @MCH2, @Keyword, @Status, @PageSize, @PageNumber",
                            new SqlParameter("@SaleType", salestype ?? string.Empty),
                            new SqlParameter("@StoreNo", storeno ?? string.Empty),
                            new SqlParameter("@MCH2", mch2 ?? string.Empty),       // Nganh hang
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@Status", status ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)
                            ).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ItemPartnerResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ItemPartnerResponseModel>();
            }
        }
        public ViewSetupItemPartnerDetailModel ViewInforItemDetailByPartner(string itemno)
        {
            var result = new ViewSetupItemPartnerDetailModel();
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var itemInfor = (from a in db.ItemSalesOnApps
                                     join b in db.Shopee_dish on a.DishId equals b.dish_id
                                     where a.ItemNo == itemno
                                     select new Model.SetupItem.ItemPartnerModel
                                     {
                                         StoreNo = a.StoreNo,
                                         SalesType = a.AppCode,
                                         ItemGroup = a.ItemGroup,
                                         ItemGroupName = a.ItemGroupName,
                                         ItemNo = a.ItemNo,
                                         ItemName = a.ItemName,
                                         UnitPrice = a.UnitPrice,
                                         Uom = a.Uom,
                                         Size = a.Size,
                                         CupType = a.CupType,
                                         IsTopping = a.IsTopping,
                                         ImgName = b.name,
                                         dish_id = b.dish_id,
                                         //PictureId = Convert.ToBase64String(url),
                                         Url = b.url

                                     }).FirstOrDefault();

                    result = new ViewSetupItemPartnerDetailModel
                    {
                        ItemNo = itemno,
                        InforItem = itemInfor
                    };
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public ResultResponseModel UpdateItemDetailByPartner(UpdateItemPartnerModel req)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.ItemSalesOnApps.Where(x => x.ItemNo == req.ItemNo).FirstOrDefault();
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    data.StoreNo = req.StoreNo;
                    data.ItemGroup = req.ItemGroup;
                    data.CupType = req.CupType;
                    data.UnitPrice = req.UnitPrice;
                    data.Blocked = req.IsActive;
                    data.CrtDate = DateTime.Now;
                    data.ChgeDate = DateTime.Now;

                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
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

        // PosGroup
        public List<POSGroupModel> GetPOSGroup(string searchText)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var searchString = searchText.Trim().ToLower();
                    var query = db.PosGroups.AsNoTracking();

                    var parentData = query.Where(x => x.ParentCode == String.Empty);
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        var searchData = query.Where(x => x.Description.ToLower().Contains(searchString));
                        parentData = parentData.Where(x => searchData.Any(c => c.ParentCode == x.GroupCode) || searchData.Any(c => c.Id == x.Id));
                    }
                    var result = parentData.ToList();

                    var childrenData = db.PosGroups.AsNoTracking().Where(x => parentData.Any(d => d.GroupCode == x.ParentCode)).ToList();
                    result.AddRange(childrenData);

                    return result.Select(a => new POSGroupModel
                    {
                        Id = a.Id,
                        GroupCode = a.GroupCode,
                        Description = a.Description,
                        Level = a.Level,
                        ParentCode = a.ParentCode,
                        Seq = a.Seq,
                        Status = (int)a.Status
                    }).ToList();
                }
                catch (Exception ex)
                {
                    return new List<POSGroupModel>();
                }
            }
        }

        public List<POSGroupModel> GetParentPOSGroup()
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var parentData = db.PosGroups.AsNoTracking().Where(x => x.ParentCode == String.Empty).ToList();
                    return parentData.Select(a => new POSGroupModel
                    {
                        Id = a.Id,
                        GroupCode = a.GroupCode,
                        Description = a.Description,
                        Level = a.Level,
                        ParentCode = a.ParentCode,
                        Seq = a.Seq,
                        Status = (int)a.Status
                    }).ToList();
                }
                catch (Exception ex)
                {
                    return new List<POSGroupModel>();
                }
            }
        }

        public POSGroupModel GetDetailPosGroup(int id)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    return db.PosGroups.AsNoTracking().Where(x => x.Id == id).Select(a => new POSGroupModel
                    {
                        Id = a.Id,
                        GroupCode = a.GroupCode,
                        Description = a.Description,
                        Level = a.Level,
                        ParentCode = a.ParentCode,
                        Seq = a.Seq,
                        Status = (int)a.Status
                    }).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    return new POSGroupModel();
                }
            }
        }

        public ResultResponseModel CreatePosGroup(POSGroupModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkExist = db.PosGroups.AsNoTracking().FirstOrDefault(x => x.GroupCode == req.GroupCode);
                    var counterPosGroup = db.PosGroups.Max(a => a.Counter) ?? 0;
                    if (checkExist != null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Mã danh mục đã tồn tại"
                        };
                    }

                    var newPosGroup = new PosGroup();
                    newPosGroup.Description = req.Description;
                    newPosGroup.GroupCode = req.GroupCode;
                    newPosGroup.Level = 1;
                    newPosGroup.ParentCode = req.ParentCode;
                    newPosGroup.Pkey = newPosGroup.GroupCode;
                    newPosGroup.Seq = req.Seq;
                    newPosGroup.Status = (short)req.Status;
                    newPosGroup.Counter = counterPosGroup + 1;
                    newPosGroup.LastDateModified = DateTime.Now;
                    db.PosGroups.Add(newPosGroup);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới thành công"
                    };
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
        }

        public ResultResponseModel UpdatePosGroup(POSGroupModel req, bool isParent)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var posGroup = db.PosGroups.FirstOrDefault(x => x.Id == req.Id);
                    posGroup.Description = req.Description;
                    posGroup.Seq = req.Seq;
                    posGroup.LastDateModified = DateTime.Now;
                    if (!isParent)
                    {
                        posGroup.ParentCode = req.ParentCode;
                        posGroup.Status = (short)req.Status;
                    }
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
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
        }

        public ResultResponseModel UpdateStatusPosGroup(int id, int status)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var posGroup = db.PosGroups.FirstOrDefault(x => x.Id == id);
                    posGroup.Status = (short)status;
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật trạng thái thành công"
                    };
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
        }
        
        //PosGroupItem
        public List<PosGroupItemModel> GetPosGroupItemByGroupCode(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize)
        {
            var data = new List<PosGroupItemModel>();
            totalRecord = 0;
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var searchString = searchText.Trim().ToLower();

                    var posGroupItem = (from pos in db.PosGroupItems.AsNoTracking()
                                        join item in db.Items on pos.ItemNo equals item.No
                                        where pos.GroupCode == groupCode
                                        select new PosGroupItemModel
                                        {
                                            Id = pos.Id,
                                            ItemName = item.Description,
                                            GroupCode = pos.GroupCode,
                                            Pkey = pos.Pkey,
                                            Seq = (int)pos.Seq,
                                            Status = (int)pos.Status,
                                            UnitOfMeasure = pos.UnitOfMeasure,
                                            ItemNo = pos.ItemNo
                                        });

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        posGroupItem = posGroupItem.Where(x => x.ItemName.ToLower().Contains(searchString)
                                       || x.ItemNo.ToLower().Contains(searchString));
                    }

                    totalRecord = posGroupItem.Count();

                    var result = posGroupItem.OrderByDescending(x => x.Id).Skip(pageNumber).Take(pageSize).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
            }
            return data;
        }

        public List<PosGroupItemModel> GetPosGroupItemIsValid(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize)
        {
            var data = new List<PosGroupItemModel>();
            totalRecord = 0;
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var searchString = searchText.Trim().ToLower();
                    var itemIsValid = db.Items.AsNoTracking().Where(x => !db.PosGroupItems.Any(c => c.ItemNo == x.No));

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        itemIsValid = itemIsValid.Where(x => x.Description.ToLower().Contains(searchString) 
                                      || x.No.ToLower().Contains(searchString));
                    }

                    totalRecord = itemIsValid.Count();

                    itemIsValid = itemIsValid.OrderByDescending(x => x.No).Skip(pageNumber).Take(pageSize);

                    var result = itemIsValid.Select(item => new PosGroupItemModel
                    {
                        Id = 0,
                        GroupCode = groupCode,
                        ItemNo = item.No,
                        ItemName = item.Description,
                        UnitOfMeasure = item.BaseUnitOfMeasure,
                        Status = 1,
                        Pkey = item.No,
                        Seq = 0
                    }).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
            }
            return data;
        }

        public ResultResponseModel CreatePosGroupItem(PosGroupItemModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var item = db.Items.AsNoTracking().FirstOrDefault(x => x.No == req.ItemNo);
                    var counterPosGroupItem = db.PosGroupItems.Max(a => a.Counter) ?? 0;

                    var insertPosGroup = new PosGroupItem
                    {
                        ItemNo = item.No,
                        GroupCode = req.GroupCode,
                        UnitOfMeasure = item.BaseUnitOfMeasure,
                        Seq = 1,
                        Counter = counterPosGroupItem + 1,
                        Pkey = item.No,
                        Status = 1,
                        LastDateModified = DateTime.Now
                    };
                    db.PosGroupItems.Add(insertPosGroup);

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới thành công"
                    };
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
        }

        public ResultResponseModel DeletePosGroupItem(int id)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var posGroupItem = db.PosGroupItems.FirstOrDefault(x => x.Id == id);
                    db.PosGroupItems.Remove(posGroupItem);

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
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
        }

        //****** GrabFood ******
        public List<OptionComboxModel> LoadComboxCategorybyGrabFood()
        {
            var cached = VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Get<List<OptionComboxModel>>(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodCategories);
            if (cached != null) return cached;

            using (var db = new PartnerMDEntities())
            {
                db.Database.CommandTimeout = 5;
                try
                {
                    var data = (from a in db.GrabFood_categories
                                where a.AvailableStatus == "AVAILABLE"
                                orderby a.Id ascending
                                select new OptionComboxModel
                                {
                                    Value = a.Id,
                                    Text = a.Name,
                                    Order = a.Sequence
                                }).Distinct().ToList();
                    VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodCategories, data, TimeSpan.FromMinutes(30));
                    return data;
                }
                catch (Exception ex)
                {
                    var emptyResult = new List<OptionComboxModel>();
                    VCM.BLUEPOS.Data.Caching.RedisCacheHelper.Set(VCM.BLUEPOS.Data.Caching.CacheKeys.GrabFoodCategories, emptyResult, TimeSpan.FromMinutes(5));
                    return emptyResult;
                }
            }
        }

        public Tuple<bool, string, SetupItemRequestModel> SaveToppingGrab(SetupItemRequestModel model)
        {
            var result = new Tuple<bool, string, SetupItemRequestModel>(false, "", model);

            lock (lockRequest)
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var item = model.itemHeader;
                            var listTopping = model.listToppingGrab;
                            if (listTopping != null && listTopping.Count > 0)
                            {
                                var sttTopping = 1;
                                if (listTopping.Count > 0)
                                {
                                    listTopping.ForEach(c => c.Seq = sttTopping++);
                                }

                                var insertTopping = listTopping.Select(a => new GrabFood_modifier
                                {
                                    Id = a.ItemNoRef,
                                    Name = a.ItemNameRef,
                                    Sequence = 1,
                                    AvailableStatus = "AVAILABLE",
                                    Price = 25000,
                                    ModifierGroupID = "TOPPING",
                                    CrtUser = "tungnt8",
                                    CrtDate = DateTime.Now,
                                    IsCampaign = false
                                });

                                db.GrabFood_modifier.AddRange(insertTopping);
                                // insert item

                            }

                            db.SaveChanges();
                            transaction.Commit();
                            result = new Tuple<bool, string, SetupItemRequestModel>(true, $"Cập nhật thành công sản phẩm {item.ItemNo}", model);

                        }
                        catch (Exception ex)
                        {
                            result = new Tuple<bool, string, SetupItemRequestModel>(false, JsonConvert.SerializeObject(ex), model);
                            transaction.Rollback();
                        }
                    }
                }
            }
            return result;
        }



    }
}
