using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.Authen;
using VCM.BLUEPOS.Data.Common;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.DBRead.CentralSales;
using VCM.BLUEPOS.Data.EF.PartnerPLH;
using VCM.BLUEPOS.Data.EF.PartnerMD;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Partner;
using VCM.BLUEPOS.Model.Partner.GrabFoodModel;
using VCM.BLUEPOS.Common.Helpers;


namespace VCM.BLUEPOS.Data.Partner
{
    public interface IPartnerData
    {
        List<Shopee_Restaurant_Model> LoadStorePLHByMappingNowFood();
        List<Shopee_Restaurant_Model> LoadComboxStorePLH();
        List<Shopee_Restaurant_Model> GetStoreHeadByNowFood();
        List<StoreByPartnerResponseModel> GetComboxStoreNowFood();
        List<StoreByPartnerResponseModel> GetComboxStoreByGrabFood();
        List<OptionModel> LoadComboxCategory();
        string GetCategoryByGrabFood(string id);
        List<OptionModel> LoadComboxCategoryByPartner(string partner);
        List<PictureResponseModel> LoadComboxPictureByNowFood();
        string GetItemNameByGrabFood(string id);
        string GetImagesBase64ByGrabFood(string id);
        List<ItemListNowFoodResponseModel> GetItemListNowFood(string appcode, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize);
        ResultResponseModel UpdateItemListNowFood(UpdateItemListNowFoodModel req);
        List<CreateItemListPLHResponseModel> GetItemListByMappingPartner(string textSearch, out int totalRecord, int pageNumber, int pageSize);
        ResultResponseModel CheckCreateItemByMappingPartner(CreateItemPartnerModel req);
        List<PartnerRestaurantModel> LoadComboxStoreByPartner(string partner);
        string GetItemNameByNowFood(string partner_dish_id, string partner_restaurant_id);
        List<UpdateItemListPLHResponseModel> Get_Item_List_PLH_By_Mapping_By_Update(string textSearch, out int totalRecord, int pageNumber, int pageSize);
        string GetGroupNameByNowFood(string partner_dish_group_id);
        List<OrderSalesListByNowFoodModel> GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder, out int totalRecord, int pageNumber, int pageSize);
        List<ExportExcel_OrderSalesListByNowFoodModel> ExportExcel_GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder);
        List<ShopeeRestaurantResponseModel> GetStoreHeadNowFoodByCreate(string storeNo, string isHeader, out int totalRecord, int pageNumber, int pageSize);
        List<StorePLHModel> StoreInfor(string storeNo);
        ResultResponseModel CreateStoreHeadByNowFood(CreateShopeeRestaurantModel req);
        ResultResponseModel UpdateStoreHeadByNowFood(UpdateShopeeRestaurantModel req);
        List<ItemSalesOnAppModel> GetCupTypeByItemNowFood(string storeNo, string cupType, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize);
        List<ExportItemSalesOnAppModel> ExportExcelGetCupTypeByItem(string storeNo, string cupType, string status, string textSearch);
        ResultResponseModel UpdateStatusByOrder(UpdateStatusModel req);
        List<StoreByPartnerResponseModel> GetStoreHeadByPartner(string partnerCode);
        List<PictureResponseModel> LoadComboxPictureByPartner(string partnerCode);
        List<UpdateItemListByPartnerResponseModel> GetItemMappingNowFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize);
        List<UpdateItemListByPartnerResponseModel> GetItemMappingGrabFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize);
        List<UpdateItemListByPartnerResponseModel> GetItemMappingBeFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize);
        
        // Grabfood
        List<GrabFoodStoreModel> LoadComboxStoreByGrabFood();
        List<CategoryByPartnerModel> LoadComboxCategoryGrabFood();
        List<GrabFood_Store_Model> GetStoreMappingByGrabFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize);
        Tuple<int, string, string> ImportExcel_MappingStoreGrabFood(List<ImportStoreGrabFoodModel> listData);
        ResultResponseModel UpdateStatusStoreGrab(string GrabMerchantID, string StoreNo, string Status, string UserName);
        ResultResponseModel UpdateStoreGrab(string GrabMerchantIDOld, string GrabMerchantIDNew, string StoreNo, string UserName);
        List<ToppingPartnerModel> GetToppingByGrabFood();
        List<GrabFood_SetupItemModel> ListItemByGroup(string itemNo, string description, string groupCode, out int total, int skip, int take);

        // BeFood
        List<BeFood_Store_Model> GetStoreMappingByBeFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize);
        List<ComboxStoreBeFoodModel> LoadComboxStoreByBeFood();
        ResultResponseModel UpdateStatusStoreBeFood(string BeFoodMerchantID, string StoreNo, string Status, string UserName);
        ResultResponseModel UpdateStoreBeFood(string BeFoodMerchantIDOld, string BeFoodMerchantIDNew, string StoreNo, string UserName);
        Tuple<int, string, string> ImportExcel_MappingStoreBeFood(List<ImportExcelStoreBeFoodModel> listData);

        // ZaloFood
        List<ComboxStoreZaloFoodModel> LoadComboxStoreByZaloFood();
        List<ZaloFood_Store_Model> GetStoreMappingByZaloFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize);
        ResultResponseModel UpdateStoreZaloFood(string store_id_old, string store_id_new, string merchant_store_id, string UserName);
        ResultResponseModel UpdateStatusStoreZaloFood(string store_id, string merchant_store_id, string status, string userName);
        Tuple<int, string, string> ImportExcel_MappingStoreZaloFood(List<ImportExcelStoreZaloFoodModel> listData);

    }

    public class PartnerData : IPartnerData
    {
        public List<Shopee_Restaurant_Model> LoadComboxStorePLH()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())  // Lấy danh sách CH PLH     
                {
                    db.Database.CommandTimeout = 3 * 60;
                    var data = (from a in db.Stores
                                where a.ClosingMethod == 0
                                select new Shopee_Restaurant_Model
                                {
                                    partner_restaurant_id = a.No,
                                    name = a.Name
                                }).OrderBy(x => x.partner_restaurant_id).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<Shopee_Restaurant_Model>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<Shopee_Restaurant_Model>();
            }
        }
        public List<Shopee_Restaurant_Model> LoadStorePLHByMappingNowFood()
        {
            try
            {
                using (var db = new PartnerMDEntities())        
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Shopee_restaurant // Lay all cac CH trong table db.Shopee_restaurant
                                select new Shopee_Restaurant_Model
                                {
                                    partner_restaurant_id = a.partner_restaurant_id,
                                    restaurant_id = a.restaurant_id,
                                    name = a.name
                                }).OrderBy(x => x.restaurant_id).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<Shopee_Restaurant_Model>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<Shopee_Restaurant_Model>();
            }
        }
        public List<PartnerRestaurantModel> LoadComboxStoreByPartner(string partner)
        {
            try
            {
                var data = new List<PartnerRestaurantModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    if (partner == "NOF")  // Nowfood
                    {
                        data = (from a in db.Shopee_restaurant
                                select new PartnerRestaurantModel
                                {
                                    partner_restaurant_id = a.partner_restaurant_id,
                                    restaurant_id = a.restaurant_id,
                                    name = a.name
                                }).OrderBy(x => x.restaurant_id).ToList();

                        if (data == null || data.Count == 0)
                        {
                            return new List<PartnerRestaurantModel>();
                        }                        
                    }

                    //else if (partner == "BEF")  // Befood
                    //{
                    //    data = (from a in db.BeeFood_restaurant
                    //            where a.blocked == false && (a.partner_restaurant_id != null || a.partner_restaurant_id != "")
                    //            select new PartnerRestaurantModel
                    //            {
                    //                partner_restaurant_id = a.partner_restaurant_id,
                    //                restaurant_id = a.restaurant_id,
                    //                restaurant_name = a.restaurant_name
                    //            }).OrderBy(x => x.restaurant_id).ToList();

                    //    if (data == null || data.Count == 0)
                    //    {
                    //        return new List<PartnerRestaurantModel>();
                    //    }
                    //}

                }
                return data;
            }
            catch (Exception ex)
            {
                return new List<PartnerRestaurantModel>();
            }
        }

        public List<GrabFoodStoreModel> LoadComboxStoreByGrabFood()
        {
            try
            {
                var data = new List<GrabFoodStoreModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.GrabFood_store
                            where a.IsOpen == true
                            select new GrabFoodStoreModel
                            {
                                PartnerMerchantID = a.PartnerMerchantID,
                                StoreNo = a.StoreNo,
                                StoreName = a.StoreName
                            }).OrderBy(x => x.StoreNo).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<GrabFoodStoreModel>();
                    }                    
                }
                return data;
            }
            catch (Exception ex)
            {
                return new List<GrabFoodStoreModel>();
            }
        }

        public List<ComboxStoreBeFoodModel> LoadComboxStoreByBeFood()
        {
            try
            {
                var data = new List<ComboxStoreBeFoodModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.BeeFood_restaurant
                            where a.blocked == false
                            select new ComboxStoreBeFoodModel
                            {
                                restaurant_id = a.restaurant_id,
                                partner_restaurant_id = a.partner_restaurant_id,
                                address = a.address
                            }).OrderBy(x => x.partner_restaurant_id).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ComboxStoreBeFoodModel>();
                    }
                }
                return data;
            }
            catch (Exception ex)
            {
                return new List<ComboxStoreBeFoodModel>();
            }
        }
        public List<ComboxStoreZaloFoodModel> LoadComboxStoreByZaloFood()
        {
            try
            {
                var data = new List<ComboxStoreZaloFoodModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.Zalo_store
                            where a.blocked == false
                            select new ComboxStoreZaloFoodModel
                            {
                                store_id = a.store_id,
                                merchant_store_id = a.merchant_store_id,
                                merchant_name = a.merchant_name
                            }).OrderBy(x => x.merchant_store_id).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ComboxStoreZaloFoodModel>();
                    }
                }
                return data;
            }
            catch (Exception ex)
            {
                return new List<ComboxStoreZaloFoodModel>();
            }
        }

        public List<Shopee_Restaurant_Model> GetStoreHeadByNowFood()
        {
            try
            {
                using (var db = new PartnerMDEntities())  // NowFood trên PRD vẫn dùng chuỗi kết nối này        
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Shopee_restaurant
                                where a.is_header == true   // = 1 : Đây là các CH Head
                                select new Shopee_Restaurant_Model
                                {
                                    partner_restaurant_id = a.partner_restaurant_id,
                                    restaurant_id = a.restaurant_id,
                                    name = a.name.Trim(), 
                                }).OrderBy(x => x.restaurant_id).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<Shopee_Restaurant_Model>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<Shopee_Restaurant_Model>();
            }
        }
        public List<OptionModel> LoadComboxCategory()
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
                                }).Distinct().ToList();
                    return data;
                }
                catch (Exception ex)
                {
                    return new List<OptionModel>();
                }
            }
        }

        public string GetCategoryByGrabFood(string id)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.GrabFood_categories.Where(x => x.Id == id).FirstOrDefault().Name;
                    if (data == null)
                    {
                        return string.Empty;
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public List<OptionModel> LoadComboxCategoryByPartner(string partner)
        {
                try
                {
                    var data = new List<OptionModel>();
                    using (var db = new PartnerMDEntities())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        if (partner == "NOF")
                        {
                            data = (from a in db.Shopee_dish_group
                                    where a.display_order == 0
                                    select new OptionModel
                                    {
                                        Value = a.partner_dish_group_id,
                                        Text = a.name
                                    }).OrderBy(a => a.Value).Distinct().ToList();
                        }
                        else if (partner == "GRF")
                        {
                            data = (from a in db.GrabFood_categories
                                    where a.AvailableStatus != "TEMPLATE"
                                    select new OptionModel
                                    {
                                        Value = a.Id,
                                        Text = a.Name
                                    }).OrderBy(a => a.Value).Distinct().ToList();
                        }
                    }
                    return data;
                }
                catch (Exception ex)
                {
                    return new List<OptionModel>();
                }            
        }
        public List<ItemListNowFoodResponseModel> GetItemListNowFood(string appcode, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ItemListNowFoodResponseModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<ItemListNowFoodResponseModel>("[dbo].[NOWFOOD_GET_ITEM_LIST] @AppCode, @StoreNo, @MCH2, @Keyword, @Status, @PageSize, @PageNumber",
                            new SqlParameter("@AppCode", appcode ?? string.Empty),
                            new SqlParameter("@StoreNo", storeno ?? string.Empty),
                            new SqlParameter("@MCH2", mch2 ?? string.Empty),       // Nganh hang
                            new SqlParameter("@Keyword", keyWord ?? string.Empty),
                            new SqlParameter("@Status", status ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ItemListNowFoodResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ItemListNowFoodResponseModel>();
            }
        }

        public ResultResponseModel UpdateItemListNowFood(UpdateItemListNowFoodModel req)
        {
            try
            {
                /*
                 *  Pkey : PartnerCode - AppCode - ItemNo - Uom - StoreNo
                 */
                 
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;                    
                    var data = db.ItemSalesOnApps.Where(x => x.ItemNo == req.ItemNo && x.Uom == req.Uom && x.StoreNo == req.StoreNo && x.AppCode == req.AppCode && x.PartnerCode == "PLH").FirstOrDefault();
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    data.Size = req.Size;
                    data.CupType = req.CupType;
                    data.UnitPrice = string.IsNullOrEmpty(req.UnitPrice.ToString()) ? 0 : Convert.ToDecimal(req.UnitPrice) ;
                    data.Blocked = req.IsActive;
                    data.ParentCode = req.ItemNo;
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
        public List<CreateItemListPLHResponseModel> GetItemListByMappingPartner(string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
                try
                {
                    var data = new List<CreateItemListPLHResponseModel>();
                    using (var db = new CentralMDPartnerContainer())
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        totalRecord = 0;
                        data = db.Database.SqlQuery<CreateItemListPLHResponseModel>("[dbo].[GetItemByMappingPartner] @TextSearch, @PageSize, @PageNumber",
                                new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                                new SqlParameter("@PageSize", pageSize),
                                new SqlParameter("@PageNumber", pageNumber)).ToList();

                        if (data == null || data.Count == 0)
                        {
                            return new List<CreateItemListPLHResponseModel>();
                        }
                        totalRecord = data.FirstOrDefault().Total;
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    totalRecord = 0;
                    return new List<CreateItemListPLHResponseModel>();
                }        
        }

        //public List<CreateItemListPLHResponseModel> Get_Item_List_PLH_By_Mapping(string textSearch, out int totalRecord, int pageNumber, int pageSize)
        //{
        //    try
        //    {
        //        var data = new List<CreateItemListPLHResponseModel>();
        //        using (var db = new CentralMDPartnerContainer())
        //        {
        //            db.Database.CommandTimeout = 2 * 60;
        //            totalRecord = 0;
        //            data = db.Database.SqlQuery<CreateItemListPLHResponseModel>("[dbo].[SP_GET_ITEM_FROM_PLH_TO_NOW_BY_CREATE] @TextSearch, @PageSize, @PageNumber",
        //                    new SqlParameter("@TextSearch", textSearch ?? string.Empty),
        //                    new SqlParameter("@PageSize", pageSize),
        //                    new SqlParameter("@PageNumber", pageNumber)
        //                    ).ToList();

        //            if (data == null || data.Count == 0)
        //            {
        //                return new List<CreateItemListPLHResponseModel>();
        //            }
        //            totalRecord = data.FirstOrDefault().Total;
        //            return data;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        totalRecord = 0;
        //        return new List<CreateItemListPLHResponseModel>();
        //    }
        //}

        public List<PictureResponseModel> LoadComboxPictureByNowFood()
        {
            var data = new List<PictureResponseModel>();
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Shopee_dish.GroupBy(n => new {n.picture_id, n.url})
                            .Select(a => new PictureResponseModel
                            {
                                picture_id = a.Key.picture_id,
                                url =  a.Key.url
                            }).OrderBy(x =>x.picture_id).ToList();

                    if (data.Count == 0)
                    {
                        return new List<PictureResponseModel>();
                    }
                }
            }
            catch (Exception e)
            {
                return new List<PictureResponseModel>();
            }
            return data;
        }
        public ResultResponseModel CheckCreateItemByMappingPartner(CreateItemPartnerModel req)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkData = string.Empty;
                    var result = new ResultResponseModel();

                    if (req.partner_id == "NOF")  // NowFood
                    {
                        checkData = db.Shopee_dish.Where(x => x.partner_dish_id == req.partner_dish_id && x.partner_restaurant_id == req.partner_restaurant_id).FirstOrDefault().partner_dish_id;
                        if (checkData != null)
                        {
                            result = new ResultResponseModel()
                            {
                                Status = Model.Enums.ResultEnum.Fail,  // 3
                                Message = $"Sản phẩm {req.partner_dish_id} này đã có bán trên {req.partner_id}. Vui lòng kiểm tra lại"
                            };
                        }
                        else
                        {
                            result = new ResultResponseModel()
                            {
                                Status = Model.Enums.ResultEnum.Success,  // 1
                                Message = $"Sản phẩm {req.partner_dish_id} này chưa có trên {req.partner_id}. Bạn có thể tạo sản phẩm này"
                            };
                        }
                    }
                    //else if(req.partner_id == "BEF") // BeFood
                    //{
                    //    var _merchantItemId = int.Parse(req.partner_dish_id);
                    //    Int32 checkmerchantItemId = db.BeeFood_dish.Where(x => x.MerchantItemId == _merchantItemId && x.PartnerCategoryId == req.partner_restaurant_id).FirstOrDefault().MerchantItemId;
                    //    checkData = Convert.ToString(checkmerchantItemId);
                    //    if (checkData != null)
                    //    {
                    //        result = new ResultResponseModel()
                    //        {
                    //            Status = Model.Enums.ResultEnum.Fail,  // 3
                    //            Message = $"Sản phẩm {checkData} này đã có bán trên {req.partner_id}. Vui lòng kiểm tra lại"
                    //        };
                    //    }
                    //    else
                    //    {
                    //        result = new ResultResponseModel()
                    //        {
                    //            Status = Model.Enums.ResultEnum.Success,  // 1
                    //            Message = $"Sản phẩm {checkData} này chưa có trên {req.partner_id}. Bạn có thể tạo sản phẩm này"
                    //        };
                    //    }
                    //}
                    return result;
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

        public string GetItemNameByGrabFood(string id)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.GrabFood_items.Where(x => x.Id == id).FirstOrDefault().Name;
                    if (data == null)
                    {
                        return string.Empty;
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public string GetImagesBase64ByGrabFood(string id)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.GrabFood_items.Where(x => x.Id == id).FirstOrDefault().ItemId.ToString();
                    if (data == null)
                    {
                        return string.Empty;
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public string GetItemNameByNowFood(string partner_dish_id, string partner_restaurant_id)
        {            
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Shopee_dish.Where(x => x.partner_dish_id == partner_dish_id && x.partner_restaurant_id == partner_restaurant_id).FirstOrDefault().name;
                    if (data == null)
                    {
                        return string.Empty;
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }           
        }

        // Lay tên nganh hang trên NowFood
        public string GetGroupNameByNowFood(string partner_dish_group_id)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Shopee_dish_group.Where(x => x.partner_dish_group_id == partner_dish_group_id && x.display_order == 0);
                    if (data == null)
                    {
                        return string.Empty;
                    }
                    return data.FirstOrDefault().name;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public List<UpdateItemListPLHResponseModel> Get_Item_List_PLH_By_Mapping_By_Update(string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<UpdateItemListPLHResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<UpdateItemListPLHResponseModel>("[dbo].[SP_GET_ITEM_FROM_PLH_TO_NOW_BY_CREATE] @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<UpdateItemListPLHResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<UpdateItemListPLHResponseModel>();
            }
        }
        public List<OrderSalesListByNowFoodModel> GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                using (var db = new PartnerPLHContainer()) // PRD: 10.235.25.89 , DB = PartnerPLH
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = new List<OrderSalesListByNowFoodModel>();

                    if (shopeeOrder == "0")
                    {
                        data = db.Database.SqlQuery<OrderSalesListByNowFoodModel>("[dbo].[Get_Order_Sales_By_Table_Shopee_Update_Order] @FromDate, @ToDate, @StoreNo, @TransactionType, @StatusPayment, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType),
                        new SqlParameter("@StatusPayment", statusPayment ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearchOrder ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                    }
                    else
                    {
                        data = db.Database.SqlQuery<OrderSalesListByNowFoodModel>("[dbo].[Get_Order_Sales_By_Table_Shopee_Update_Order_Backup] @FromDate, @ToDate, @StoreNo, @TransactionType, @StatusPayment, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType),
                        new SqlParameter("@StatusPayment", statusPayment ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearchOrder ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                    }
                    
                    if (data == null || data.Count == 0)
                    {
                        return new List<OrderSalesListByNowFoodModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<OrderSalesListByNowFoodModel>();
            }
        }

        public List<ExportExcel_OrderSalesListByNowFoodModel> ExportExcel_GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder)
        {
            try
            {
                using (var db = new PartnerPLHContainer()) // PRD: 10.235.25.89 , DB = PartnerPLH
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = new List<ExportExcel_OrderSalesListByNowFoodModel>();

                    if (shopeeOrder == "0")
                    {
                        data = db.Database.SqlQuery<ExportExcel_OrderSalesListByNowFoodModel>("[dbo].[Get_Order_Sales_By_Table_Shopee_Update_Order] @FromDate, @ToDate, @StoreNo, @TransactionType, @StatusPayment, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType),
                        new SqlParameter("@StatusPayment", statusPayment ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearchOrder ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();
                    }
                    else
                    {
                        data = db.Database.SqlQuery<ExportExcel_OrderSalesListByNowFoodModel>("[dbo].[Get_Order_Sales_By_Table_Shopee_Update_Order_Backup] @FromDate, @ToDate, @StoreNo, @TransactionType, @StatusPayment, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TransactionType", transactionType),
                        new SqlParameter("@StatusPayment", statusPayment ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearchOrder ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();
                    }

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcel_OrderSalesListByNowFoodModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_OrderSalesListByNowFoodModel>();
            }
        }
        public List<ShopeeRestaurantResponseModel> GetStoreHeadNowFoodByCreate(string storeNo, string isHeader, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ShopeeRestaurantResponseModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    data = db.Database.SqlQuery<ShopeeRestaurantResponseModel>("[dbo].[GET_STORE_HEAD_BY NOW_FOOD] @StoreNo, @IsHeader, @PageSize, @PageNumber",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@IsHeader", isHeader ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ShopeeRestaurantResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ShopeeRestaurantResponseModel>();
            }
        }

        public List<StorePLHModel> StoreInfor(string storeNo)
        {
            var data = new List<StorePLHModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;                    
                    data = db.Stores.Where(x => x.No == storeNo && x.ClosingMethod == 0)
                           .Select(a => new StorePLHModel
                            {
                                No = a.No,
                                Name = a.Name,
                                Address = a.Address,
                                City = a.LocationCode
                            }).ToList();

                    if (data.Count == 0)
                    {
                        return new List<StorePLHModel>();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return data;
        }

        public ResultResponseModel CreateStoreHeadByNowFood(CreateShopeeRestaurantModel req)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Shopee_restaurant.Where(x =>x.partner_restaurant_id == req.partner_restaurant_id || x.parent_restaurant == req.parent_restaurant).FirstOrDefault();

                    if (data == null)
                    {
                        var storeData = new List<StorePLHModel>();
                        storeData = StoreInfor(req.partner_restaurant_id);

                        var item = new Shopee_restaurant
                        {
                            restaurant_id = 0,  // A.Minh : khi them moi mac dinh = 0
                            partner_restaurant_id = req.partner_restaurant_id,
                            name = storeData.FirstOrDefault()?.Name,
                            city = storeData.FirstOrDefault()?.City,
                            address = storeData.FirstOrDefault()?.Address,
                            foody_service = "FOOD",
                            crt_date = DateTime.Now,
                            is_header = false, // A.Minh : default = false
                            parent_restaurant = req.partner_restaurant_id
                        };

                        db.Shopee_restaurant.Add(item);
                        db.SaveChanges();

                        return new ResultResponseModel
                        {
                            Item1 = req.partner_restaurant_id,
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Cửa hàng: {req.partner_restaurant_id} này đã có trong hệ thống. Vui lòng kiểm tra lại"
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

        public ResultResponseModel UpdateStoreHeadByNowFood(UpdateShopeeRestaurantModel req)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Shopee_restaurant.Where(x =>x.partner_restaurant_id == req.partner_restaurant_id && x.is_header == false).FirstOrDefault();

                    if (data == null) // Thêm mới
                    {
                        var storeData = new List<StorePLHModel>();
                        storeData = StoreInfor(req.partner_restaurant_id);

                        var item = new Shopee_restaurant
                        {
                            restaurant_id = 0,  // A.Minh : khi them moi mac dinh = 0
                            partner_restaurant_id = req.partner_restaurant_id,
                            name = storeData.FirstOrDefault()?.Name,
                            city = storeData.FirstOrDefault()?.City,
                            address = storeData.FirstOrDefault()?.Address,
                            foody_service = "FOOD",
                            crt_date = DateTime.Now,
                            is_header = false, // default = false
                            parent_restaurant = req.partner_restaurant_id
                        };

                        db.Shopee_restaurant.Add(item);
                        db.SaveChanges();

                        return new ResultResponseModel
                        {
                            Item1 = req.partner_restaurant_id,
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Cập nhật thành công"
                        };
                    }
                    else
                    {
                        return new ResultResponseModel
                        {
                            Item1 = req.partner_restaurant_id,
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Cửa hàng:'{req.partner_restaurant_id}' này đã có trong hệ thống, Vui lòng chọn Cửa hàng khác"
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
        public List<ItemSalesOnAppModel> GetCupTypeByItemNowFood(string storeNo, string cupType, string status,string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ItemSalesOnAppModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<ItemSalesOnAppModel>("[dbo].[PLH_NOW_CHECK_CUPTYPE] @StoreNo, @TextSearch, @CupType, @Status, @Export, @PageSize, @PageNumber",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@CupType", cupType ?? string.Empty),
                            new SqlParameter("@Status", status ?? string.Empty),
                            new SqlParameter("@Export",'1'),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ItemSalesOnAppModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ItemSalesOnAppModel>();
            }
        }

        public List<ExportItemSalesOnAppModel> ExportExcelGetCupTypeByItem(string storeNo, string cupType, string status, string textSearch)
        {
            try
            {
                var data = new List<ExportItemSalesOnAppModel>();
                using (var db = new PartnerMDEntities()) 
                {
                    db.Database.CommandTimeout = 5 * 60;
                    data = db.Database.SqlQuery<ExportItemSalesOnAppModel>("[dbo].[PLH_NOW_CHECK_CUPTYPE] @StoreNo, @TextSearch, @CupType, @Status, @Export, @PageSize, @PageNumber",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@CupType", cupType ?? string.Empty),
                            new SqlParameter("@Status", status ?? string.Empty),
                            new SqlParameter("@Export", '2'),
                            new SqlParameter("@PageSize", string.Empty),
                            new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportItemSalesOnAppModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportItemSalesOnAppModel>();
            }
        }
        public ResultResponseModel UpdateStatusByOrder(UpdateStatusModel req)
        {
            try
            {
                using (var db = new PartnerPLHContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Shopee_update_order.FirstOrDefault(a => a.order_code == req.OrderNo && a.update_flg == "N");
                    data.update_flg = req.Status;  // == Yes (Y)
                    db.SaveChanges();
                }
                return new ResultResponseModel
                {
                    Item1 = req.OrderNo,
                    Item2 = req.Status,
                    Status = Model.Enums.ResultEnum.Success,
                    Message = "Cập nhật Notify thành công"
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

        //26/02/2025,tungnt8
        public List<StoreByPartnerResponseModel> GetStoreHeadByPartner(string partnerCode)
        {
            try
            {
                var result = new List<StoreByPartnerResponseModel>();                
                using (var db = new PartnerMDEntities())        
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (partnerCode == "NOF")
                    {
                        result = (from a in db.Shopee_restaurant
                                  where a.is_header == true   // = 1 : Đây là các CH Head
                                  select new StoreByPartnerResponseModel
                                  {
                                      partner_restaurant_id = a.partner_restaurant_id,
                                      restaurant_id = a.restaurant_id,
                                      StoreNo = a.partner_restaurant_id.ToString(),
                                      StoreName = a.name.ToString()
                                  }).OrderBy(x => x.partner_restaurant_id).ToList();

                        if (result == null || result.Count == 0)
                        {
                            return new List<StoreByPartnerResponseModel>();
                        }
                    }
                    else if (partnerCode == "GRF")
                    {
                        result = (from a in db.GrabFood_store
                                    where a.IsOpen == true
                                    select new StoreByPartnerResponseModel
                                    {
                                        GrabMerchantID = a.GrabMerchantID,
                                        PartnerMerchantID =a.PartnerMerchantID,
                                        StoreNo = a.StoreNo,
                                        StoreName = a.StoreName,
                                    }).OrderBy(x => x.StoreNo).ToList();

                        if (result == null || result.Count == 0)
                        {
                            return new List<StoreByPartnerResponseModel>();
                        }
                    }

                    //else if (partnerCode == "BEF")
                    //{
                    //    result = (from a in db.BeeFood_restaurant
                    //              where a.is_header == true
                    //              select new StoreByPartnerResponseModel
                    //              {
                    //                  partner_restaurant_id = a.partner_restaurant_id,
                    //                  restaurant_id = a.restaurant_id,
                    //                  name = a.restaurant_name.Trim()
                    //              }).OrderBy(x => x.restaurant_id).ToList();

                    //    if (result == null || result.Count == 0)
                    //    {
                    //        return new List<StoreByPartnerResponseModel>();
                    //    }
                    //}

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<StoreByPartnerResponseModel>();
            }
        }
        public List<StoreByPartnerResponseModel> GetComboxStoreNowFood()
        {
            try
            {
                var result = new List<StoreByPartnerResponseModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                        result = (from a in db.Shopee_restaurant
                                  where a.is_header == true   // = 1 : Đây là các CH Head
                                  select new StoreByPartnerResponseModel
                                  {
                                      partner_restaurant_id = a.partner_restaurant_id,
                                      restaurant_id = a.restaurant_id,
                                      StoreNo = a.partner_restaurant_id.ToString(),
                                      StoreName = a.name.ToString()
                                  }).OrderBy(x => x.partner_restaurant_id).ToList();

                        if (result == null || result.Count == 0)
                        {
                            return new List<StoreByPartnerResponseModel>();
                        }
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<StoreByPartnerResponseModel>();
            }
        }

        //26/02/2025,tungnt8
        public List<StoreByPartnerResponseModel> GetComboxStoreByGrabFood()
        {
            try
            {
                var result = new List<StoreByPartnerResponseModel>();
                using (var db = new PartnerMDEntities())
                {
                        db.Database.CommandTimeout = 2 * 60;
                        result = (from a in db.GrabFood_store
                                  where a.IsOpen == true
                                  select new StoreByPartnerResponseModel
                                  {
                                      GrabMerchantID = a.GrabMerchantID,
                                      PartnerMerchantID = a.PartnerMerchantID,
                                      StoreNo = a.StoreNo,
                                      StoreName = a.StoreName
                                  }).OrderBy(x => x.StoreNo).ToList();

                        if (result == null || result.Count == 0)
                        {
                            return new List<StoreByPartnerResponseModel>();
                        }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<StoreByPartnerResponseModel>();
            }
        }
        public List<CategoryByPartnerModel> LoadComboxCategoryGrabFood()
        {
            var result = new List<CategoryByPartnerModel>();
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = (from a in db.GrabFood_categories
                                  where a.AvailableStatus == "AVAILABLE"
                                  select new CategoryByPartnerModel
                                  {
                                      Id = a.Id,
                                      Name = a.Name,
                                      AvailableStatus = a.AvailableStatus,
                                      Sequence = a.Sequence,
                                      CateId = a.CateId
                                  }).OrderBy(x => x.Id).ToList();

                        if (result == null || result.Count == 0)
                        {
                            return new List<CategoryByPartnerModel>();
                        }
                    }
            }
            catch (Exception ex)
            {
                return new List<CategoryByPartnerModel>();
            }
            return result;
        }
        public List<PictureResponseModel> LoadComboxPictureByPartner(string partnerCode)
        {
            var data = new List<PictureResponseModel>();
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (partnerCode == "NOF")
                    {
                        data = db.Shopee_dish.GroupBy(n => new { n.picture_id, n.url })
                            .Select(a => new PictureResponseModel
                            {
                                picture_id = a.Key.picture_id,
                                url = a.Key.url
                            }).OrderBy(x => x.picture_id).ToList();

                        if (data.Count == 0)
                        {
                            return new List<PictureResponseModel>();
                        }
                    }
                    else if (partnerCode == "GRF")
                    {
                        data = db.GrabFood_items.GroupBy(n => new { n.ItemId, n.Photo })
                            .Select(a => new PictureResponseModel
                            {
                                ItemId = a.Key.ItemId.ToString(),
                                Photo = a.Key.Photo
                            }).OrderBy(x => x.ItemId).ToList();

                        if (data.Count == 0)
                        {
                            return new List<PictureResponseModel>();
                        }
                    }
                    //else if (partnerCode == "BEF")
                    //{
                    //    data = db.BeeFood_dish.GroupBy(n => new { n.MerchantItemId, n.MerchantItemImage })
                    //        .Select(a => new PictureResponseModel
                    //        {
                    //            picture_id = a.Key.MerchantItemId,
                    //            url = a.Key.MerchantItemImage
                    //        }).OrderBy(x => x.picture_id).ToList();

                    //    if (data.Count == 0)
                    //    {
                    //        return new List<PictureResponseModel>();
                    //    }
                    //}                    
                }
            }
            catch (Exception e)
            {
                return new List<PictureResponseModel>();
            }
            return data;
        }

        public List<UpdateItemListByPartnerResponseModel> GetItemMappingNowFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<UpdateItemListByPartnerResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<UpdateItemListByPartnerResponseModel>("[dbo].[GetItemMappingByPartner] @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<UpdateItemListByPartnerResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<UpdateItemListByPartnerResponseModel>();
            }
        }

        public List<UpdateItemListByPartnerResponseModel> GetItemMappingGrabFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<UpdateItemListByPartnerResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<UpdateItemListByPartnerResponseModel>("[dbo].[GetItemMappingByPartner] @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<UpdateItemListByPartnerResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<UpdateItemListByPartnerResponseModel>();
            }
        }
        public List<UpdateItemListByPartnerResponseModel> GetItemMappingBeFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<UpdateItemListByPartnerResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<UpdateItemListByPartnerResponseModel>("[dbo].[GetItemMappingByPartner] @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<UpdateItemListByPartnerResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<UpdateItemListByPartnerResponseModel>();
            }
        }

        public List<GrabFood_Store_Model> GetStoreMappingByGrabFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<GrabFood_Store_Model>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<GrabFood_Store_Model>("[dbo].[GetStoreGrabFoodList] @StoreNo, @Status, @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@Status", status ?? string.Empty),
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<GrabFood_Store_Model>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<GrabFood_Store_Model>();
            }
        }

        public Tuple<int, string, string> ImportExcel_MappingStoreGrabFood(List<ImportStoreGrabFoodModel> listData)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    // Check existed StoreNo
                    var listStoreNo = listData.Select(x => x.StoreNo).ToList();

                    // Check CH đang kinh doanh hay không ?
                    using (var dbCentral = new CentralMDPartnerContainer())
                    {
                        var checkBlock = dbCentral.Stores.AsNoTracking()
                            .Where(x => listStoreNo.Contains(x.No) && x.ClosingMethod == 0) // Đang có hiệu lực
                            .Select(x => x.No)
                            .ToList();

                        if (checkBlock.Count == 0 || checkBlock == null)
                        {
                            return new Tuple<int, string, string>(0, $"Danh sách CH: {string.Join(", ", listStoreNo).TrimEnd()} này đang tạm khóa (hoặc không tồn tại trong hệ thống). Vui lòng kiểm tra lại", string.Empty);
                        }
                    }

                    var checkExistedStoreNo = db.GrabFood_store.AsNoTracking()
                        .Where(x => listStoreNo.Contains(x.StoreNo))
                        .Select(x => x.StoreNo)
                        .ToList();

                    if (checkExistedStoreNo.Count > 0)
                    {
                        var listNotExisted = listStoreNo.Except(checkExistedStoreNo);
                        if (listNotExisted != null && listNotExisted.Count() > 0)
                        {
                            var listCondition = listData.Where(a => listNotExisted.Contains(a.StoreNo))
                                .Select(x => x.StoreNo)
                                .ToList();
                            return new Tuple<int, string, string>(0, $"Danh sách CH: {string.Join(", ", listCondition).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);

                            //var listCondition = listData.Where(a => listNotExisted.Contains(a.StoreNo))
                            //   .Select(x => $"{x.StoreNo}-{x.StoreName}")
                            //   .ToList();
                            //return new Tuple<int, string, string>(3, string.Empty, string.Join("|", listCondition));

                        }
                    }

                    // Check trung trong DB

                    var listConditionDB = listData.Select(x => $"{x.GrabMerchantID}").ToList();
                    var checkConditionDB = db.GrabFood_store.AsNoTracking()
                        .Where(x => listConditionDB.Contains(x.GrabMerchantID))
                        .Select(x => x.GrabMerchantID)
                        .ToList();

                    if (checkConditionDB.Count > 0)
                    {
                        //return new Tuple<int, string, string>(2, $"Mã CH GrabFood đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Join("|", checkConditionDB));
                        return new Tuple<int, string, string>(0, $"Danh sách CH GrabFood: {string.Join(", ", checkConditionDB).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);
                    }

                    var listConditionStore = listData.Select(x => $"{x.StoreNo}").ToList();
                    var checkConditionStore = db.GrabFood_store.AsNoTracking()
                        .Where(x => listConditionStore.Contains(x.StoreNo))
                        .Select(x => x.StoreNo)
                        .ToList();

                    if (checkConditionStore.Count > 0)
                    {
                        //return new Tuple<int, string, string>(2, $"Mã CH GrabFood đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Join("|", checkConditionDB));
                        return new Tuple<int, string, string>(0, $"Danh sách CH PLH: {string.Join(", ", checkConditionStore).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);
                    }

                    // Xoa truoc khi cap nhat
                    foreach (var item in listData)
                    {
                        var checkStoreNo = db.GrabFood_store.AsNoTracking().FirstOrDefault(x => x.StoreNo == item.StoreNo && x.GrabMerchantID == item.GrabMerchantID);
                        if (checkStoreNo != null)
                        {
                            var storeNo = db.GrabFood_store.Where(x => x.StoreNo == item.StoreNo && x.GrabMerchantID == item.GrabMerchantID).FirstOrDefault();
                            db.GrabFood_store.Remove(storeNo);
                            db.SaveChanges();
                        }
                    }

                    // Insert DB                    
                    var listEntities = listData.Select(x => new GrabFood_store
                    {
                        GrabMerchantID = x.GrabMerchantID,
                        PartnerMerchantID = x.StoreNo,
                        StoreNo = x.StoreNo,
                        StoreName = x.StoreName,
                        IsOpen = true,  // 1 : open, 0 : close    
                        CloseReason = string.Empty,
                        DeliveryStartTime = string.Empty,
                        DeliveryEndTime = string.Empty,
                        SpecialStartTime = string.Empty,
                        SpecialEndTime = string.Empty,
                        DineInStartTime = string.Empty,
                        DineInEndTime = string.Empty,
                        CrtUser = x.UserName,
                        CrtDate = DateTime.Now,
                        ChgUser = x.UserName,
                        ChgDate = DateTime.Now
                    }).ToList();

                    db.GrabFood_store.AddRange(listEntities);
                    db.SaveChanges();
                    return new Tuple<int, string, string>(1, $"Import danh sách thành công {listEntities.Count} dòng", string.Empty);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string, string>(0, "Có lỗi xảy ra, Import danh sách thất bại", string.Empty);
            }
        }

        public ResultResponseModel UpdateStatusStoreGrab(string GrabMerchantID, string StoreNo, string Status, string UserName)
        {
            using (var db = new PartnerMDEntities())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var check = db.GrabFood_store.FirstOrDefault(x => x.GrabMerchantID == GrabMerchantID && x.StoreNo == StoreNo);
                    check.IsOpen = (Status == "1") ? true : false;
                    check.ChgUser = UserName;
                    check.ChgDate = DateTime.Now;
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
        public ResultResponseModel UpdateStoreGrab(string GrabMerchantIDOld, string GrabMerchantIDNew, string StoreNo, string UserName)
        {
            using (var db = new PartnerMDEntities())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkNew = db.GrabFood_store.Where(x => x.GrabMerchantID == GrabMerchantIDNew).FirstOrDefault();
                    if (checkNew != null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Mã CH GrabFood {GrabMerchantIDNew} này đã tồn tại trong hệ thống. Vui lòng chọn lại mã CH GrabFood khác"
                        };
                    }

                    var check = db.GrabFood_store.Where(x => x.GrabMerchantID == GrabMerchantIDOld && x.StoreNo == StoreNo).FirstOrDefault();
                    var storeNo = check.StoreNo;
                    var storeName = check.StoreName;

                    // Xóa
                    if (check != null)
                    {
                        db.GrabFood_store.Remove(check);
                        db.SaveChanges();
                    }

                    // Thêm mới
                    var insert = new GrabFood_store
                    {
                        GrabMerchantID = GrabMerchantIDNew,
                        PartnerMerchantID = storeNo,
                        StoreNo = storeNo,
                        StoreName = storeName,
                        IsOpen = true,
                        CrtUser = UserName,
                        CrtDate = DateTime.Now,
                        ChgUser = UserName,
                        ChgDate = DateTime.Now
                    };

                    db.GrabFood_store.Add(insert);
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

        public List<ToppingPartnerModel> GetToppingByGrabFood()
        {
            try
            {
                var result = new List<ToppingPartnerModel>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 120;
                    var data = (from a in db.GrabFood_modifier
                                where a.ModifierGroupID == "TOPPING" && a.AvailableStatus == "AVAILABLE"
                                select new ToppingPartnerModel
                                {
                                    Id = a.Id,
                                    Name = a.Name,
                                    AvailableStatus = a.AvailableStatus
                                });

                    var listItemNo = data.Select(a => a.Id).ToList();
                    using (var db1 = new CentralMDPartnerContainer())
                    {
                        var datalist = db1.Items.Where(a => listItemNo.Contains(a.No) && a.Blocked == 0 && a.ItemCategoryCode == "Topping").ToList();
                        var listItem = datalist.Select(a => new ToppingPartnerModel
                        {
                            Id = a.No,
                            Name = a.Description,
                            UOM = a.SalesUnitOfMeasure
                        }).OrderBy(x => x.Id).Distinct().ToList();
                        result = listItem;
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<ToppingPartnerModel>();
            }
        }
        public List<GrabFood_SetupItemModel> ListItemByGroup(string itemNo, string description, string groupCode, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.Items.Where(d => d.ItemCategoryCode == groupCode && d.Blocked == 0).Select(a => new GrabFood_SetupItemModel
                    {
                        ItemNo = a.No,
                        ItemNo2 = a.No2,
                        ItemName = a.Description,
                        UOM = a.SalesUnitOfMeasure
                    }).ToList();

                    var listItem = new List<GrabFood_SetupItemModel>();
                    using (var db1 = new PartnerMDEntities())
                    {
                        try
                        {
                            db.Database.CommandTimeout = 120;
                            var dataTopping = db1.GrabFood_modifier.AsNoTracking().Where(d => d.ModifierGroupID == "TOPPING" && d.AvailableStatus == "AVAILABLE")
                                              .Select(x =>x.Id).ToList();

                            var listItemNo = data.Select(x => x.ItemNo).ToList();
                            var model = new GrabFood_SetupItemModel();

                            if (dataTopping.Count > 0)
                            {
                                var list = listItemNo.Except(dataTopping);
                                foreach (var item in list)
                                {
                                    model = new GrabFood_SetupItemModel
                                    {
                                        ItemNo = item
                                    };
                                    listItem.Add(model);
                                }
                            }
                            else
                            {
                                listItem = data;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    var result = (from a in data
                                  join b in listItem on a.ItemNo equals b.ItemNo
                                  select new GrabFood_SetupItemModel
                                  {
                                        ItemNo = a.ItemNo,
                                        ItemNo2 = a.ItemNo2,
                                        ItemName = a.ItemName,
                                        UOM = a.UOM
                                  });

                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        result = result.Where(m => m.ItemNo == itemNo);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        result = result.Where(m => m.ItemName.ToLower().Contains(description.ToLower()));
                    }

                    total = result.Count();
                    var kq = result.OrderBy(d => d.ItemNo).Skip(skip).Take(take).ToList();
                    return kq;
                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return new List<GrabFood_SetupItemModel>();
            }
        }

        //----------- BeFood ----------
        public List<BeFood_Store_Model> GetStoreMappingByBeFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<BeFood_Store_Model>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<BeFood_Store_Model>("[dbo].[GetStoreBeFoodList] @StoreNo, @Status, @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@Status", status ?? string.Empty),
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<BeFood_Store_Model>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<BeFood_Store_Model>();
            }
        }

        public ResultResponseModel UpdateStatusStoreBeFood(string BeFoodMerchantID, string StoreNo, string Status, string UserName)
        {
            using (var db = new PartnerMDEntities())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var check = db.BeeFood_restaurant.FirstOrDefault(x => x.restaurant_id == BeFoodMerchantID && x.partner_restaurant_id == StoreNo);
                    check.blocked = (Status == "1") ? true : false;
                    check.ChgUser = UserName;
                    check.ChgDate = DateTime.Now;
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

        public ResultResponseModel UpdateStoreBeFood(string BeFoodMerchantIDOld, string BeFoodMerchantIDNew, string StoreNo, string UserName)
        {
            using (var db = new PartnerMDEntities())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkNew = db.BeeFood_restaurant.Where(x => x.restaurant_id == BeFoodMerchantIDNew).FirstOrDefault();
                    if (checkNew != null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Mã CH BeFood {BeFoodMerchantIDNew} này đã tồn tại trong hệ thống. Vui lòng chọn lại mã CH BeFood khác"
                        };
                    }

                    var check = db.BeeFood_restaurant.Where(x => x.restaurant_id == BeFoodMerchantIDOld && x.partner_restaurant_id == StoreNo).FirstOrDefault();
                    var storeNo = check.partner_restaurant_id;
                    var storeName = check.address;

                    // Xóa
                    if (check != null)
                    {
                        db.BeeFood_restaurant.Remove(check);
                        db.SaveChanges();
                    }

                    // Thêm mới
                    var insert = new BeeFood_restaurant
                    {
                        restaurant_id = BeFoodMerchantIDNew,
                        partner_restaurant_id = storeNo,
                        restaurant_name = storeName,
                        address = storeName,
                        foody_service = "1",
                        is_header = false,  // 0:false;1:true
                        blocked = false,    // false = active ; true = deactive
                        crt_date = DateTime.Now,
                        CrtUser = UserName,
                        CrtDate = DateTime.Now,
                        ChgUser = UserName,
                        ChgDate = DateTime.Now
                    };

                    db.BeeFood_restaurant.Add(insert);
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

        public Tuple<int, string, string> ImportExcel_MappingStoreBeFood(List<ImportExcelStoreBeFoodModel> listData)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    // Check existed StoreNo PL
                    var listStoreNo = listData.Select(x => x.partner_restaurant_id).ToList();

                    // Check CH đang kinh doanh hay không ?
                    using (var dbCentral = new CentralMDPartnerContainer())
                    {
                        var checkBlock = dbCentral.Stores.AsNoTracking()
                            .Where(x => listStoreNo.Contains(x.No) && x.ClosingMethod == 0) // Đang có hiệu lực
                            .Select(x => x.No)
                            .ToList();

                        if (checkBlock.Count == 0 || checkBlock == null)
                        {
                            return new Tuple<int, string, string>(0, $"Danh sách CH Phúc Long: {string.Join(", ", listStoreNo).TrimEnd()} này đang tạm khóa (hoặc không tồn tại trong hệ thống). Vui lòng kiểm tra lại", string.Empty);
                        }
                    }

                    var checkExistedStoreNo = db.BeeFood_restaurant.AsNoTracking()
                        .Where(x => listStoreNo.Contains(x.partner_restaurant_id))
                        .Select(x => x.partner_restaurant_id)
                        .ToList();

                    if (checkExistedStoreNo.Count > 0)
                    {
                        var listNotExisted = listStoreNo.Except(checkExistedStoreNo);
                        if (listNotExisted != null && listNotExisted.Count() > 0)
                        {
                            var listCondition = listData.Where(a => listNotExisted.Contains(a.partner_restaurant_id))
                                .Select(x => x.partner_restaurant_id)
                                .ToList();
                            return new Tuple<int, string, string>(0, $"Danh sách CH Phúc Long: {string.Join(", ", listCondition).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);

                            //var listCondition = listData.Where(a => listNotExisted.Contains(a.StoreNo))
                            //   .Select(x => $"{x.StoreNo}-{x.StoreName}")
                            //   .ToList();
                            //return new Tuple<int, string, string>(3, string.Empty, string.Join("|", listCondition));

                        }
                    }

                    // Check trung trong DB, mã CH BeFood

                    var listStoreBeFood = new List<CheckBeFoodStoreModel>();
                    var modelStoreBeFood = new CheckBeFoodStoreModel();
                    var listConditionDB = listData.Select(x => $"{x.restaurant_id}").ToList();

                    foreach (var id in listConditionDB)
                    {
                        modelStoreBeFood = new CheckBeFoodStoreModel
                        {
                            restaurant_id = id
                        };
                        listStoreBeFood.Add(modelStoreBeFood);
                    }

                    var checkStoreList = listStoreBeFood.Select(x => x.restaurant_id).ToList();
                    var checkConditionDB = db.BeeFood_restaurant.AsNoTracking()
                        .Where(x => checkStoreList.Contains(x.restaurant_id))
                        .Select(x => x.restaurant_id)
                        .ToList();

                    if (checkConditionDB.Count > 0)
                    {
                        //return new Tuple<int, string, string>(2, $"Mã CH GrabFood đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Join("|", checkConditionDB));
                        return new Tuple<int, string, string>(0, $"Danh sách CH BeFood: {string.Join(", ", checkConditionDB).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);
                    }

                    var listConditionStore = listData.Select(x => $"{x.partner_restaurant_id}").ToList();
                    var checkConditionStore = db.BeeFood_restaurant.AsNoTracking()
                        .Where(x => listConditionStore.Contains(x.partner_restaurant_id))
                        .Select(x => x.partner_restaurant_id)
                        .ToList();

                    if (checkConditionStore.Count > 0)
                    {
                        //return new Tuple<int, string, string>(2, $"Mã CH GrabFood đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Join("|", checkConditionDB));
                        return new Tuple<int, string, string>(0, $"Danh sách CH Phúc Long: {string.Join(", ", checkConditionStore).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);
                    }

                    // Xoa truoc khi cap nhat

                    foreach (var item in listData)
                    {
                        var checkStoreNo = db.BeeFood_restaurant.AsNoTracking().FirstOrDefault(x => x.partner_restaurant_id == item.partner_restaurant_id && x.restaurant_id == item.restaurant_id);
                        if (checkStoreNo != null)
                        {
                            var storeNo = db.BeeFood_restaurant.Where(x => x.partner_restaurant_id == item.partner_restaurant_id && x.restaurant_id == item.restaurant_id).FirstOrDefault();
                            db.BeeFood_restaurant.Remove(storeNo);
                            db.SaveChanges();
                        }
                    }

                    // Insert DB 
                    
                    var listEntities = listData.Select(x => new BeeFood_restaurant
                    {
                        restaurant_id = x.restaurant_id,
                        partner_restaurant_id = x.partner_restaurant_id,
                        restaurant_name = x.restaurant_name,
                        address = x.restaurant_name,
                        foody_service = "1",
                        is_header = false,  // 0:false;1:true
                        blocked = false,    // false = active ; true = deactive
                        crt_date = DateTime.Now,
                        CrtUser = x.UserName,
                        CrtDate = DateTime.Now,
                        ChgUser = x.UserName,
                        ChgDate = DateTime.Now
                    }).ToList();

                    db.BeeFood_restaurant.AddRange(listEntities);
                    db.SaveChanges();
                    return new Tuple<int, string, string>(1, $"Import danh sách thành công {listEntities.Count} dòng", string.Empty);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string, string>(0, "Có lỗi xảy ra, Import danh sách thất bại", string.Empty);
            }
        }

        // 11/08/2025,tungnt8 : ZaloFood
        public List<ZaloFood_Store_Model> GetStoreMappingByZaloFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            try
            {
                var data = new List<ZaloFood_Store_Model>();
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<ZaloFood_Store_Model>("[dbo].[GetStoreZaloFoodList] @StoreNo, @Status, @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@Status", status ?? string.Empty),
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageNumber)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ZaloFood_Store_Model>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ZaloFood_Store_Model>();
            }
        }

        public ResultResponseModel UpdateStoreZaloFood(string store_id_old, string store_id_new, string merchant_store_id, string UserName)
        {
            using (var db = new PartnerMDEntities())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkNew = db.Zalo_store.Where(x => x.store_id == store_id_new).FirstOrDefault();
                    if (checkNew != null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Mã CH ZaloFood {store_id_new} này đã tồn tại trong hệ thống. Vui lòng chọn lại mã CH ZaloFood khác"
                        };
                    }

                    var check = db.Zalo_store.Where(x => x.store_id == store_id_old && x.merchant_store_id == merchant_store_id).FirstOrDefault();
                    var storeNo = check.merchant_store_id;
                    var storeName = check.merchant_name;

                    // Xóa
                    if (check != null)
                    {
                        db.Zalo_store.Remove(check);
                        db.SaveChanges();
                    }

                    // Thêm mới
                    var insert = new Zalo_store
                    {
                        store_id = store_id_new,
                        merchant_store_id = storeNo,
                        merchant_name = storeName,
                        blocked = false, // mặc đinh là false = có hiệu lực
                        CrtUser = UserName,
                        crt_date = DateTime.Now,
                        ChgUser = UserName,
                        ChgDate = DateTime.Now
                    };
                    db.Zalo_store.Add(insert);
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

        public ResultResponseModel UpdateStatusStoreZaloFood(string store_id, string merchant_store_id, string status, string userName)
        {
            using (var db = new PartnerMDEntities())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Zalo_store.FirstOrDefault(x => x.store_id == store_id && x.merchant_store_id == merchant_store_id);
                    data.blocked = (status == "1") ? false : true;
                    data.ChgUser = userName;
                    data.ChgDate = DateTime.Now;
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

        public Tuple<int, string, string> ImportExcel_MappingStoreZaloFood(List<ImportExcelStoreZaloFoodModel> listData)
        {
            try
            {
                using (var db = new PartnerMDEntities())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    // Check existed StoreNo PLH
                    var listStoreNo = listData.Select(x => x.merchant_store_id).ToList();
                    // Check CH đang kinh doanh hay không ?
                    using (var dbCentral = new CentralMDPartnerContainer())
                    {
                        var checkBlock = dbCentral.Stores.AsNoTracking()
                            .Where(x => listStoreNo.Contains(x.No) && x.ClosingMethod == 0) // Đang có hiệu lực
                            .Select(x => x.No)
                            .ToList();

                        if (checkBlock.Count == 0 || checkBlock == null)
                        {
                            return new Tuple<int, string, string>(0, $"Danh sách CH Phúc Long: {string.Join(", ", listStoreNo).TrimEnd()} này đang tạm khóa (hoặc không tồn tại trong hệ thống). Vui lòng kiểm tra lại", string.Empty);
                        }
                    }

                    var checkExistedStoreNo = db.Zalo_store.AsNoTracking()
                        .Where(x => listStoreNo.Contains(x.merchant_store_id))
                        .Select(x => x.merchant_store_id)
                        .ToList();

                    if (checkExistedStoreNo.Count > 0)
                    {
                        var listNotExisted = listStoreNo.Except(checkExistedStoreNo);
                        if (listNotExisted != null && listNotExisted.Count() > 0)
                        {
                            var listCondition = listData.Where(a => !listNotExisted.Contains(a.merchant_store_id))
                                .Select(x => x.merchant_store_id)
                                .ToList();
                            return new Tuple<int, string, string>(0, $"Danh sách CH Phúc Long: {string.Join(", ", listCondition).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);

                            //var listCondition = listData.Where(a => listNotExisted.Contains(a.StoreNo))
                            //   .Select(x => $"{x.StoreNo}-{x.StoreName}")
                            //   .ToList();
                            //return new Tuple<int, string, string>(3, string.Empty, string.Join("|", listCondition));

                        }
                    }

                    // Check trung trong DB, mã CH ZaloFood
                    var listStoreZaloFood = new List<CheckZaloFoodStoreModel>();
                    var modelStoreZaloFood = new CheckZaloFoodStoreModel();
                    var listConditionDB = listData.Select(x => $"{x.store_id}").ToList();
                    foreach (var id in listConditionDB)
                    {
                        modelStoreZaloFood = new CheckZaloFoodStoreModel
                        {
                            store_id = id
                        };
                        listStoreZaloFood.Add(modelStoreZaloFood);
                    }

                    var checkStoreList = listStoreZaloFood.Select(x => x.store_id).ToList();
                    var checkConditionDB = db.Zalo_store.AsNoTracking()
                        .Where(x => checkStoreList.Contains(x.store_id))
                        .Select(x => x.store_id)
                        .ToList();

                    if (checkConditionDB.Count > 0)
                    {
                        //return new Tuple<int, string, string>(2, $"Mã CH GrabFood đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Join("|", checkConditionDB));
                        return new Tuple<int, string, string>(0, $"Danh sách CH ZaloFood: {string.Join(", ", checkConditionDB).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);
                    }

                    var listConditionStore = listData.Select(x => $"{x.merchant_store_id}").ToList();
                    var checkConditionStore = db.Zalo_store.AsNoTracking()
                        .Where(x => listConditionStore.Contains(x.merchant_store_id))
                        .Select(x => x.merchant_store_id)
                        .ToList();

                    if (checkConditionStore.Count > 0)
                    {
                        //return new Tuple<int, string, string>(2, $"Mã CH GrabFood đã tồn tại trong hệ thống. Vui lòng kiểm tra lại file import", string.Join("|", checkConditionDB));
                        return new Tuple<int, string, string>(0, $"Danh sách CH Phúc Long: {string.Join(", ", checkConditionStore).TrimEnd()} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại", string.Empty);
                    }

                    // Xoa truoc khi cap nhat
                    foreach (var item in listData)
                    {
                        var checkStoreNo = db.Zalo_store.AsNoTracking().FirstOrDefault(x => x.merchant_store_id == item.merchant_store_id && x.store_id == item.store_id);
                        if (checkStoreNo != null)
                        {
                            var storeNo = db.Zalo_store.Where(x => x.merchant_store_id == item.merchant_store_id && x.store_id == item.store_id).FirstOrDefault();
                            db.Zalo_store.Remove(storeNo);
                            db.SaveChanges();
                        }
                    }

                    // Insert DB 
                    var listEntities = listData.Select(x => new Zalo_store
                    {
                        store_id = x.store_id,
                        merchant_store_id = x.merchant_store_id,
                        merchant_name = x.merchant_name,
                        blocked = false,    // false = active ; true = deactive
                        crt_date = DateTime.Now,
                        CrtUser = x.UserName,
                        ChgUser = x.UserName,
                        ChgDate = DateTime.Now
                    }).ToList();

                    db.Zalo_store.AddRange(listEntities);
                    db.SaveChanges();
                    return new Tuple<int, string, string>(1, $"Import danh sách thành công {listEntities.Count} dòng", string.Empty);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string, string>(0, "Có lỗi xảy ra, Import danh sách thất bại", string.Empty);
            }
        }






    }
}
