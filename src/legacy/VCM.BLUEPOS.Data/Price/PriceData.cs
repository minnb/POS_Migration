using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Price;
using VCM.BLUEPOS.Model.PriceGroup;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Product;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Store;

namespace VCM.BLUEPOS.Data.Price
{
    public interface IPriceData
    {
        List<OptionModel> GetComboSite(string channelNo);
        List<OptionModel> GetComboRegion();
        List<OptionModel> GetComboxStyleProfile();
        List<PriceListResponseModel> GetPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportPriceListModel> ExportPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck);
        Tuple<bool, SearchProductModel> GetSearchProduct(string itemNo);
        List<OptionModel> GetUnitOfMeasureCode(string itemNo);
        ResultResponseModel CreatePriceList(List<CreatePriceModel> req);
        ResultResponseModel UpdatePriceList(UpdatePriceListModel req);
        List<UpdatePriceListResponseModel> GetUpdatePriceList(string salesCode, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel CreateStorePriceGroup(List<CreateStorePriceGroupModel> req);
        List<OptionModel> GetComboxPriceGroup();
        List<StoreModel> GetStoreList(string storeNo, string styleProfile, out int totalRecord, int skip, int take);
        ResultResponseModel CreatePriceGroup(CreatePriceGroupModel req);
        List<PriceGroupListResponseModel> GetPriceGroupList(string priceGroupCode, string priority, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel SaveStorePriceGroup(List<AddStorePriceGroupModel> model);
        List<AddStoreModel> GetStoreAllList(string storeNo, string textSearch, string styleProfile);

    }

    public class PriceData : IPriceData
    {
        public List<OptionModel> GetComboSite(string channelNo)
        {
            try
            {
                var result = new List<OptionModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Stores.Where(d => d.StyleProfile == channelNo).ToList()
                            .Select(a => new OptionModel
                            {
                                Value = a.No,
                                Text = a.Name
                            }).OrderBy(x => x.Value).Distinct().ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<OptionModel>));
            }
        }

        public List<OptionModel> GetComboRegion()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.ComboRegions
                                where a.Status == true    // status = 1
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.Name
                                }).OrderBy(x => x.Value).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<PriceListResponseModel> GetPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var data = new List<PriceListResponseModel>();

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<PriceListResponseModel>("[dbo].[GetSalesPriceList] @ItemCode, @ItemName, @BarCode, @SalesCode, @isCheck, @PageSize, @PageNumber",
                            new SqlParameter("@ItemCode", itemCode ?? string.Empty),
                            new SqlParameter("@ItemName", itemName ?? string.Empty),
                            new SqlParameter("@BarCode", barCode ?? string.Empty),
                            new SqlParameter("@SalesCode", salesCode ?? string.Empty),
                            new SqlParameter("@isCheck", isCheck),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<PriceListResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<PriceListResponseModel>();
            }
        }
        public List<ExportPriceListModel> ExportPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportPriceListModel>("[dbo].[GetSalesPriceList_Export] @ItemCode, @ItemName, @BarCode, @SalesCode, @isCheck",
                        new SqlParameter("@ItemCode", itemCode ?? string.Empty),
                        new SqlParameter("@ItemName", itemName ?? string.Empty),
                        new SqlParameter("@BarCode", barCode ?? string.Empty),
                        new SqlParameter("@SalesCode", salesCode ?? string.Empty),
                        new SqlParameter("@isCheck", isCheck)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportPriceListModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportPriceListModel>();
            }
        }
        public Tuple<bool, SearchProductModel> GetSearchProduct(string itemNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Items.FirstOrDefault(x => x.No == itemNo.Trim());
                    if (data == null)
                    {
                        return new Tuple<bool, SearchProductModel>(false, new SearchProductModel
                        {
                            Message = $"Mã sản phẩm {itemNo} không tìm thấy",
                            Status = false
                        });
                    }

                    var model = new SearchProductModel
                    {
                        ItemNo = itemNo,
                        ItemName = data.Description,
                        Message = string.Empty,
                        Status = true
                    };
                    return new Tuple<bool, SearchProductModel>(true, model);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, SearchProductModel>(false, new SearchProductModel
                {
                    Message = $"Mã sản phẩm {itemNo} không tìm thấy",
                    Status = false
                });
            }
        }
        public List<OptionModel> GetUnitOfMeasureCode(string itemNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Barcodes
                                where a.ItemNo == itemNo
                                select new OptionModel
                                {
                                    Value = a.UnitOfMeasureCode
                                }).OrderBy(x => x.Value).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public ResultResponseModel CreatePriceList(List<CreatePriceModel> req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var primaryKey = "";
                    foreach (var item in req)
                    {
                        var checkItem = db.Items.FirstOrDefault(x => x.No == item.ItemNo)?.No;
                        if (string.IsNullOrEmpty(checkItem))
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail, // 3
                                Message = $"Mã sản phẩm  {item.ItemNo} này không tồn tại"
                            };
                        }

                        if (string.IsNullOrEmpty(item.RegionCode)) // theo kenh ban hang
                        {
                            primaryKey = $"{item.ItemNo}{"-"}{item.UOM}{"-"}{item.ChanelCode}{"-"}{item.StartDate.ToString("yyyyMMdd")}";
                        }
                        else // theo vung gia
                        {
                            primaryKey = $"{item.ItemNo}{"-"}{item.UOM}{"-"}{item.RegionCode}{"-"}{item.StartDate.ToString("yyyyMMdd")}";
                        }

                        var data = db.SalesPrices.FirstOrDefault(x => x.Pkey == primaryKey);
                        if (string.IsNullOrEmpty(item.RegionCode))   // theo kenh ban hang
                        {
                            if (data != null)     // Da ton tai Pkey, khong them dong moi
                            {
                                if (item.UnitPrice == data.UnitPrice)  // không thay doi gia
                                {
                                    data.UnitPrice = data.UnitPrice;
                                    db.SaveChanges();

                                    //return new ResultResponseModel
                                    //{
                                    //    Status = Model.Enums.ResultEnum.warning, // 2
                                    //    Message = "Giá không thay đổi. Vui lòng kiểm tra lại"
                                    //};
                                }
                                else  // có thay đổi giá
                                {
                                    //var maxCounter1 = db.SalesPrices.Max(x => x.Counter);
                                    //data.EndingDate = data.StartingDate.AddDays(-1);
                                    //data.Counter = maxCounter1 + 1;
                                    //db.SaveChanges();

                                    var maxCounter = db.SalesPrices.Max(x => x.Counter);
                                    data.UnitPrice = item.UnitPrice;
                                    data.Counter = maxCounter + 1;
                                    db.SaveChanges();
                                }
                            }
                            else  // thêm dòng mới
                            {
                                var maxCounter = db.SalesPrices.Max(x => x.Counter);
                                var insert = new EF.Central.SalesPrice
                                {
                                    ItemNo = item.ItemNo,
                                    SalesCode = item.ChanelCode,
                                    StartingDate = item.StartDate,
                                    CurrencyCode = "VND",
                                    UnitOfMeasureCode = item.UOM,
                                    UnitPrice = item.UnitPrice,
                                    PriceIncludesVAT = 1,
                                    AllowInvoiceDisc = 1,
                                    SalesType = "",
                                    MinimumQuantity = Convert.ToDouble(1),
                                    EndingDate = item.EndDate,
                                    VariantCode = "0",
                                    AllowLineDisc = 1,
                                    Counter = maxCounter + 1,
                                    Pkey = $"{item.ItemNo}{"-"}{item.UOM}{"-"}{item.ChanelCode}{"-"}{item.StartDate.ToString("yyyyMMdd")}"
                                };
                                db.SalesPrices.Add(insert);
                                db.SaveChanges();
                            }

                        }
                        else  // theo vung gia
                        {
                            if (data != null)     // Da ton tai Pkey, khong them dong moi
                            {
                                if (item.UnitPrice == data.UnitPrice)  // không thay doi gia
                                {
                                    //return new ResultResponseModel {
                                    //    Status = Model.Enums.ResultEnum.warning, // 2
                                    //    Message = "Giá không thay đổi. Vui lòng kiểm tra lại"
                                    //};

                                    data.UnitPrice = data.UnitPrice;
                                    db.SaveChanges();
                                }
                                else
                                {
                                    //var maxCounter1 = db.SalesPrices.Max(x => x.Counter);
                                    //data.EndingDate = data.StartingDate.AddDays(-1);
                                    //data.Counter = maxCounter1 + 1;
                                    //db.SaveChanges();

                                    var maxCounter = db.SalesPrices.Max(x => x.Counter);
                                    data.UnitPrice = item.UnitPrice;
                                    data.Counter = maxCounter + 1;
                                    db.SaveChanges();
                                }
                            }
                            else  // thêm dòng mới
                            {
                                var maxCounter = db.SalesPrices.Max(x => x.Counter);
                                var insert = new EF.Central.SalesPrice
                                {
                                    ItemNo = item.ItemNo,
                                    SalesCode = item.RegionCode,
                                    StartingDate = item.StartDate,
                                    CurrencyCode = "VND",
                                    UnitOfMeasureCode = item.UOM,
                                    UnitPrice = item.UnitPrice,
                                    PriceIncludesVAT = 1,
                                    AllowInvoiceDisc = 1,
                                    SalesType = "1",
                                    MinimumQuantity = Convert.ToDouble(1),
                                    EndingDate = item.EndDate,
                                    VariantCode = "0",
                                    AllowLineDisc = 1,
                                    Counter = maxCounter + 1,
                                    Pkey = $"{item.ItemNo}{"-"}{item.UOM}{"-"}{item.RegionCode}{"-"}{item.StartDate.ToString("yyyyMMdd")}"
                                };
                                db.SalesPrices.Add(insert);
                                db.SaveChanges();
                            }
                        }
                    }
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success, // 1
                        Message = "Lưu bảng giá thành công"
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

        public ResultResponseModel UpdatePriceList(UpdatePriceListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.SalesPrices.Where(x => x.Pkey == req.Pkey).ToList();
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.warning,
                            Message = "Không có dữ liệu"
                        };
                    }

                    if (req.UnitPrice == data.FirstOrDefault()?.UnitPrice)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.warning, // 2
                            Message = "Giá không thay đổi. Vui lòng kiểm tra lại"
                        };
                    }

                    if (req.StartDate == data.FirstOrDefault()?.StartingDate)
                    {
                        if (req.EndDate == data.FirstOrDefault()?.EndingDate)
                        {
                            var maxCounter = db.SalesPrices.Max(x => x.Counter);
                            data.FirstOrDefault().UnitPrice = req.UnitPrice;
                            data.FirstOrDefault().Counter = maxCounter + 1;
                            db.SaveChanges();
                        }
                        else
                        {
                            var maxCounter = db.SalesPrices.Max(x => x.Counter);
                            data.FirstOrDefault().UnitPrice = req.UnitPrice;
                            data.FirstOrDefault().EndingDate = req.EndDate;
                            data.FirstOrDefault().Counter = maxCounter + 1;
                            db.SaveChanges();
                        }
                    }
                    else // them moi
                    {
                        var maxCounter = db.SalesPrices.Max(x => x.Counter);
                        var insert = new EF.Central.SalesPrice
                        {
                            ItemNo = req.ItemNo,
                            SalesCode = req.SalesCode,
                            StartingDate = req.StartDate,
                            CurrencyCode = "VND",
                            UnitOfMeasureCode = req.UOM,
                            UnitPrice = req.UnitPrice,
                            PriceIncludesVAT = 1,
                            AllowInvoiceDisc = 1,
                            SalesType = "1",
                            MinimumQuantity = Convert.ToDouble(1),
                            EndingDate = req.EndDate,
                            VariantCode = "0",
                            AllowLineDisc = 1,
                            Counter = maxCounter + 1,
                            Pkey = $"{req.ItemNo}{"-"}{req.UOM}{"-"}{req.SalesCode}{"-"}{req.StartDate.ToString("yyyyMMdd")}"
                        };
                        db.SalesPrices.Add(insert);
                        db.SaveChanges();
                    }
                }
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.Success, // 1
                    Message = "Cập nhật bảng giá thành công"
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

        public List<UpdatePriceListResponseModel> GetUpdatePriceList(string salesCode, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var data = new List<UpdatePriceListResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<UpdatePriceListResponseModel>("[dbo].[GetUpdatePriceList] @SalesCode, @ItemNo, @PageSize, @PageNumber",
                            new SqlParameter("@SalesCode", salesCode ?? string.Empty),
                            new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<UpdatePriceListResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<UpdatePriceListResponseModel>();
            }
        }

        public ResultResponseModel CreateStorePriceGroup(List<CreateStorePriceGroupModel> req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var primaryKey = "";
                    // check storeNo truoc khi insert
                    foreach (var item in req)
                    {
                        var checkStoreNo = db.Stores.Where(y => y.No == item.Store.Trim()).ToList();
                        if (checkStoreNo.Count == 0)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail, // 3
                                Message = $"Mã ST/CH {item.Store} này không tồn tại trong hệ thống"
                            };
                        }
                    }
                    // duyet insert
                    foreach (var item in req)
                    {
                        primaryKey = $"{item.Store}{"-"}{item.PriceGroupCode}";
                        var data = db.StorePriceGroups.Where(x => x.Pkey == primaryKey.Trim()).ToList();

                        if (data.Count == 0)  // Thêm mới
                        {
                            var maxCounter = db.StorePriceGroups.Max(x => x.Counter);
                            var insert = new EF.Central.StorePriceGroup
                            {
                                Store = item.Store,
                                PriceGroupCode = item.PriceGroupCode,
                                Type = item.Type,
                                Priority = item.Priority,
                                ReplicationCounter = 1,
                                Counter = maxCounter + 1,
                                Pkey = $"{item.Store}{"-"}{item.PriceGroupCode}"
                            };
                            db.StorePriceGroups.Add(insert);
                            db.SaveChanges();
                        }
                        else  // Cập nhật lại
                        {
                            var maxCounter = db.StorePriceGroups.Max(x => x.Counter);
                            data.FirstOrDefault().Type = item.Type;
                            data.FirstOrDefault().Priority = item.Priority;
                            data.FirstOrDefault().ReplicationCounter = 1;
                            data.FirstOrDefault().Counter = maxCounter + 1;
                            data.FirstOrDefault().Pkey = $"{item.Store}{"-"}{item.PriceGroupCode}";
                            db.SaveChanges();
                        }
                    }
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success, // 1
                        Message = "Lưu thành công"
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

        public List<OptionModel> GetComboxStyleProfile()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.ComboStyleProfiles
                                where a.Status == true
                                select new OptionModel
                                {
                                    Value = a.StyleProfile,
                                    Text = a.StyleProfileName,
                                    Order = a.Ord
                                }).OrderBy(x => x.Order).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> GetComboxPriceGroup()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.PriceGroups
                                where a.Enabled == true
                                select new OptionModel
                                {
                                    Value = a.PriceGroupCode,
                                    Text = a.PriceGroupName,
                                    Order = a.Priority
                                }).OrderBy(x => x.Order).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<StoreModel> GetStoreList(string storeNo, string styleProfile, out int totalRecord, int skip, int take)
        {
            try
            {
                var result = new List<StoreModel>();
                List<string> filterSearch = new List<string>();
                if (!string.IsNullOrEmpty(storeNo) && storeNo !="-1" && storeNo.Any())
                {
                    var listSearch = storeNo.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    foreach (var item in listSearch)
                    {
                        filterSearch.Add(item.Trim().ToLower());
                    }
                }

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Stores.AsQueryable();
                    if (!string.IsNullOrEmpty(styleProfile) && styleProfile !="-1")
                    {
                        data = data.Where(d => d.StyleProfile == styleProfile);
                    }
                    if (filterSearch.Count > 0)
                    {
                        data = data.Where(d => filterSearch.Contains(d.No) && d.ClosingMethod == 0);
                    }
                    totalRecord = data.Count();
                    data = data.OrderByDescending(d => d.No).Skip(skip).Take(take);
                    result = data.ToList().Select(a => new StoreModel
                    {
                        StoreNo = a.No,
                        StoreName = a.Name
                    }).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                totalRecord = 0;
                return (default(List<StoreModel>));
            }
        }

        public List<AddStoreModel> GetStoreAllList(string storeNo, string textSearch, string styleProfile)
        {
            try
            {
                var result = new List<AddStoreModel>();
                List<string> filterSearch = new List<string>();
                if (!string.IsNullOrEmpty(storeNo) && storeNo != "-1" && storeNo.Any())
                {
                    var listSearch = storeNo.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    foreach (var item in listSearch)
                    {
                        filterSearch.Add(item.Trim().ToLower());
                    }
                }

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Stores.AsQueryable();
                    if (!string.IsNullOrEmpty(styleProfile) && styleProfile != "-1")
                    {
                        data = data.Where(d => d.StyleProfile == styleProfile);
                    }
                    if (filterSearch.Count > 0)
                    {
                        data = data.Where(d => filterSearch.Contains(d.No) && d.ClosingMethod == 0);
                    }
                    data = data.OrderByDescending(d => d.No);
                    result = data.ToList().Select(a => new AddStoreModel
                    {
                        StoreNo = a.No,
                        StoreName = a.Name
                    }).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                return (default(List<AddStoreModel>));
            }
        }
        public ResultResponseModel CreatePriceGroup(CreatePriceGroupModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var checkPriceGroupCode = db.PriceGroups.Any(x => x.PriceGroupCode == req.PriceGroupCode);
                    if (checkPriceGroupCode)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 2,
                            Message = $"Đã tồn tại mã: {req.PriceGroupCode} này trong hệ thống. Vui lòng kiểm tra lại"
                        };
                    }

                    var maxCounter = db.PriceGroups.Max(x => x.Counter) ?? 0;
                    var insertPriceGroup = new EF.Central.PriceGroup
                    {
                        PriceGroupCode = req.PriceGroupCode,
                        PriceGroupName = req.PriceGroupName,
                        Priority = req.Priority,
                        Counter = maxCounter + 1,
                        Pkey = req.PriceGroupCode,
                        Enabled = (req.EnabledStr == "1") ? true : false,
                        CreatedDate = DateTime.Now,
                        CreatedUser = req.CreatedUser
                    };

                    db.PriceGroups.Add(insertPriceGroup);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        IsStatus = 1,
                        Message = "Thêm mới thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message,
                        IsStatus = 3
                    };
                }
            }
        }
        public List<PriceGroupListResponseModel> GetPriceGroupList(string priceGroupCode, string priority, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var data = new List<PriceGroupListResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<PriceGroupListResponseModel>("[dbo].[GetPriceGroupList] @PriceGroupCode, @Priority, @PageSize, @PageNumber",
                            new SqlParameter("@PriceGroupCode", priceGroupCode ?? string.Empty),
                            new SqlParameter("@Priority", priority ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<PriceGroupListResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }

            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<PriceGroupListResponseModel>();
            }
        }
        public ResultResponseModel SaveStorePriceGroup(List<AddStorePriceGroupModel> model)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var listModel = new List<StorePriceGroup>();
                    var maxCounter = db.StorePriceGroups
                                     .Select(x => (int?)x.Counter)
                                     .Max() ?? 0;

                    var first = model.FirstOrDefault();
                    if (first == null || first.Store == null || !first.Store.Any())
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 0,
                            Message = "Model không hợp lệ"
                        };
                    }

                    var priceGroupCode = first.PriceGroupCode;
                    var priority = first.Priority ?? 0;

                    if (first?.Store == null || !first.Store.Any())
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 0,
                            Message = "Không có thông tin cửa hàng"
                        };
                    }

                    foreach (var item in first?.Store)
                    {
                        var pkey = $"{item.StoreNo}-{priceGroupCode}";
                        var itemAdd = new StorePriceGroup
                        {
                            Store = item.StoreNo,
                            PriceGroupCode = priceGroupCode,
                            Type = 1,
                            Priority = priority,
                            ReplicationCounter = (int)maxCounter + 1,
                            Counter = maxCounter + 1,
                            Pkey = pkey
                        };
                        listModel.Add(itemAdd);
                    }

                    // remove duplicate trong chính listModel
                    listModel = listModel
                        .GroupBy(x => x.Pkey)
                        .Select(g => g.First())
                        .ToList();

                    // check tồn tại DB
                    var listPkey = new HashSet<string>(
                                    db.StorePriceGroups
                                      .Where(x => x.PriceGroupCode == priceGroupCode)
                                      .Select(x => x.Pkey)
                                   );

                    var newItems = listModel
                                   .Where(x => !listPkey.Contains(x.Pkey))
                                   .ToList();

                    if (newItems.Any())
                    {
                        db.StorePriceGroups.AddRange(newItems);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            IsStatus = 1,
                            Item1 = Convert.ToString(newItems.Count), // tổng số mã cửa hàng
                            Message = $"Cập nhật thành công tổng số {Convert.ToString(newItems.Count)} cửa hàng vào bảng StorePriceGroup"
                        }; 
                    }
                    else
                    {
                        var newItemsExist = listModel
                                  .Where(x => listPkey.Contains(x.Pkey))
                                  .ToList();

                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 0,
                            Message = $"Danh sách Pkey: ({string.Join(",", newItemsExist.Select(a => a.Pkey))}) này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    IsStatus = 3
                };
            }
        }



    }
}
