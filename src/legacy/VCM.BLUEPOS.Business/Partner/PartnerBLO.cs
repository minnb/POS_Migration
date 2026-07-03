using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.Partner;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Partner;
using VCM.BLUEPOS.Model.Partner.GrabFoodModel;


namespace VCM.BLUEPOS.Business.Partner
{
    public interface IPartnerBLO
    {
        List<Shopee_Restaurant_Model> LoadStorePLHByMappingNowFood();
        List<PartnerRestaurantModel> LoadComboxStoreByPartner(string partner);
        List<StoreByPartnerResponseModel> GetComboxStoreNowFood();
        List<StoreByPartnerResponseModel> GetComboxStoreByGrabFood();        
        List<GrabFoodStoreModel> LoadComboxStoreByGrabFood();
        List<Shopee_Restaurant_Model> LoadComboxStorePLH();
        List<Shopee_Restaurant_Model> GetStoreHeadByNowFood();
        List<OptionModel> LoadComboxCategory();
        string GetCategoryByGrabFood(string id);
        string GetImagesBase64ByGrabFood(string id);
        List<OptionModel> LoadComboxCategoryByPartner(string partner);
        List<PictureResponseModel> LoadComboxPictureByNowFood();
        string GetItemNameByGrabFood(string id);
        List<ItemListNowFoodResponseModel> GetItemListNowFood(string appcode, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize);
        ResultResponseModel UpdateItemListNowFood(UpdateItemListNowFoodModel req);
        List<CreateItemListPLHResponseModel> GetItemListByMappingPartner(string textSearch, out int totalRecord, int pageNumber, int pageSize);
        ResultResponseModel CheckCreateItemByMappingPartner(CreateItemPartnerModel req);
        string GetItemNameByNowFood(string partner_dish_id, string partner_restaurant_id);
        List<UpdateItemListPLHResponseModel> Get_Item_List_PLH_By_Mapping_By_Update(string textSearch, out int totalRecord, int pageNumber, int pageSize);
        string GetGroupNameByNowFood(string partner_dish_group_id);        
        List<OrderSalesListByNowFoodModel> GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder, out int totalRecord, int pageNumber, int pageSize);
        List<ExportExcel_OrderSalesListByNowFoodModel> ExportExcel_GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder);
        List<ShopeeRestaurantResponseModel> GetStoreHeadNowFoodByCreate(string storeNo, string isHeader, out int totalRecord, int pageNumber, int pageSize);
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

        // GrabFood
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
        ResultResponseModel UpdateStoreZaloFood(string store_id_old, string store_id_new, string merchant_store_id, string userName);
        ResultResponseModel UpdateStatusStoreZaloFood(string store_id, string merchant_store_id, string status, string userName);
        Tuple<int, string, string> ImportExcel_MappingStoreZaloFood(List<ImportExcelStoreZaloFoodModel> listData);


    }

    public class PartnerBLO : IPartnerBLO
    {
        private readonly IPartnerData _data;
        public PartnerBLO()
        {
            _data = new PartnerData();
        }
        public List<Shopee_Restaurant_Model> LoadStorePLHByMappingNowFood()
        {
            return _data.LoadStorePLHByMappingNowFood();
        }
        public List<PartnerRestaurantModel> LoadComboxStoreByPartner(string partner)
        {
            return _data.LoadComboxStoreByPartner(partner);
        }
        public List<GrabFoodStoreModel> LoadComboxStoreByGrabFood()
        {
            return _data.LoadComboxStoreByGrabFood();
        }
        public List<ComboxStoreBeFoodModel> LoadComboxStoreByBeFood()
        {
            return _data.LoadComboxStoreByBeFood();
        }
        public List<ComboxStoreZaloFoodModel> LoadComboxStoreByZaloFood()
        {
            return _data.LoadComboxStoreByZaloFood();
        }

        public List<Shopee_Restaurant_Model> LoadComboxStorePLH()
        {
            return _data.LoadComboxStorePLH();
        }
        public List<Shopee_Restaurant_Model> GetStoreHeadByNowFood()
        {
            return _data.GetStoreHeadByNowFood();
        }
        public List<OptionModel> LoadComboxCategory()
        {
            return _data.LoadComboxCategory();
        }
        public string GetCategoryByGrabFood(string id)
        {
            return _data.GetCategoryByGrabFood(id);
        }
        public List<OptionModel> LoadComboxCategoryByPartner(string partner)
        {
            return _data.LoadComboxCategoryByPartner(partner);
        }
        public List<PictureResponseModel> LoadComboxPictureByNowFood()
        {
            return _data.LoadComboxPictureByNowFood();
        }
        public List<ItemListNowFoodResponseModel> GetItemListNowFood(string appcode, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetItemListNowFood(appcode, storeno, mch2, keyWord, status, out totalRecord, pageNumber, pageSize);
        }
        public ResultResponseModel UpdateItemListNowFood(UpdateItemListNowFoodModel req)
        {
            return _data.UpdateItemListNowFood(req);
        }
        public List<CreateItemListPLHResponseModel> GetItemListByMappingPartner(string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetItemListByMappingPartner(textSearch, out totalRecord, pageNumber, pageSize);
        }
        public ResultResponseModel CheckCreateItemByMappingPartner(CreateItemPartnerModel req)
        {
            return _data.CheckCreateItemByMappingPartner(req);
        }
        public string GetItemNameByGrabFood(string id)
        {
            return _data.GetItemNameByGrabFood(id);
        }
        public string GetImagesBase64ByGrabFood(string id)
        {
            return _data.GetImagesBase64ByGrabFood(id);
        }
        public string GetItemNameByNowFood(string partner_dish_id, string partner_restaurant_id)
        {
            return _data.GetItemNameByNowFood(partner_dish_id, partner_restaurant_id);
        }
        public List<UpdateItemListPLHResponseModel> Get_Item_List_PLH_By_Mapping_By_Update(string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.Get_Item_List_PLH_By_Mapping_By_Update(textSearch, out totalRecord, pageNumber, pageSize);
        }
        public string GetGroupNameByNowFood(string partner_dish_group_id)
        {
            return _data.GetGroupNameByNowFood(partner_dish_group_id);
        }
        public List<OrderSalesListByNowFoodModel> GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetOrderSalesListByNowFood(fromDate, toDate, setServer, storeNo, transactionType, statusPayment, textSearchOrder, shopeeOrder, out totalRecord, pageNumber, pageSize);
        }
        public List<ExportExcel_OrderSalesListByNowFoodModel> ExportExcel_GetOrderSalesListByNowFood(DateTime fromDate, DateTime toDate, string setServer, string storeNo, int transactionType, string statusPayment, string textSearchOrder, string shopeeOrder)
        {
            return _data.ExportExcel_GetOrderSalesListByNowFood(fromDate, toDate, setServer, storeNo, transactionType, statusPayment, textSearchOrder, shopeeOrder);
        }
        public List<ShopeeRestaurantResponseModel> GetStoreHeadNowFoodByCreate(string storeNo, string isHeader, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetStoreHeadNowFoodByCreate(storeNo, isHeader, out totalRecord, pageNumber, pageSize);
        }
        public ResultResponseModel CreateStoreHeadByNowFood(CreateShopeeRestaurantModel req)
        {
            return _data.CreateStoreHeadByNowFood(req);
        }
        public ResultResponseModel UpdateStoreHeadByNowFood(UpdateShopeeRestaurantModel req)
        {
            return _data.UpdateStoreHeadByNowFood(req);
        }
        public List<ItemSalesOnAppModel> GetCupTypeByItemNowFood(string storeNo, string cupType, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetCupTypeByItemNowFood(storeNo, cupType, status, textSearch, out totalRecord, pageNumber, pageSize);
        }
        public List<ExportItemSalesOnAppModel> ExportExcelGetCupTypeByItem(string storeNo, string cupType, string status, string textSearch)
        {
            return _data.ExportExcelGetCupTypeByItem(storeNo, cupType, status, textSearch);
        }
        public ResultResponseModel UpdateStatusByOrder(UpdateStatusModel req)
        {
            return _data.UpdateStatusByOrder(req);
        }
        public List<StoreByPartnerResponseModel> GetStoreHeadByPartner(string partnerCode)
        {
            return _data.GetStoreHeadByPartner(partnerCode);
        }
        public List<StoreByPartnerResponseModel> GetComboxStoreNowFood()
        {
            return _data.GetComboxStoreNowFood();
        }
        public List<StoreByPartnerResponseModel> GetComboxStoreByGrabFood()
        {
            return _data.GetComboxStoreByGrabFood();
        }
        public List<CategoryByPartnerModel> LoadComboxCategoryGrabFood()
        {
            return _data.LoadComboxCategoryGrabFood();
        }
        public List<PictureResponseModel> LoadComboxPictureByPartner(string partnerCode)
        {
            return _data.LoadComboxPictureByPartner(partnerCode);
        }
        public List<UpdateItemListByPartnerResponseModel> GetItemMappingNowFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetItemMappingNowFoodByUpdate(partnerCode, textSearch, out totalRecord, pageNumber, pageSize);
        }
        public List<UpdateItemListByPartnerResponseModel> GetItemMappingGrabFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetItemMappingGrabFoodByUpdate(partnerCode, textSearch, out totalRecord, pageNumber, pageSize);
        }
        public List<UpdateItemListByPartnerResponseModel> GetItemMappingBeFoodByUpdate(string partnerCode, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetItemMappingBeFoodByUpdate(partnerCode, textSearch, out totalRecord, pageNumber, pageSize);
        }
        public List<GrabFood_Store_Model> GetStoreMappingByGrabFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetStoreMappingByGrabFood(storeNo, status, textSearch, out totalRecord, pageNumber, pageSize);
        }
        public Tuple<int, string, string> ImportExcel_MappingStoreGrabFood(List<ImportStoreGrabFoodModel> listData)
        {
            return _data.ImportExcel_MappingStoreGrabFood(listData);
        }
        public ResultResponseModel UpdateStatusStoreGrab(string GrabMerchantID, string StoreNo, string Status, string UserName)
        {
            return _data.UpdateStatusStoreGrab(GrabMerchantID, StoreNo, Status, UserName);
        }
        public ResultResponseModel UpdateStoreGrab(string GrabMerchantIDOld, string GrabMerchantIDNew, string StoreNo, string UserName)
        {
            return _data.UpdateStoreGrab(GrabMerchantIDOld, GrabMerchantIDNew, StoreNo, UserName);
        }
        public List<ToppingPartnerModel> GetToppingByGrabFood()
        {
            return _data.GetToppingByGrabFood();
        }
        public List<GrabFood_SetupItemModel> ListItemByGroup(string itemNo, string description, string groupCode, out int total, int skip, int take)
        {
            return _data.ListItemByGroup(itemNo, description, groupCode, out total, skip, take);
        }

        // BeFood
        public List<BeFood_Store_Model> GetStoreMappingByBeFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetStoreMappingByBeFood(storeNo, status, textSearch, out totalRecord, pageNumber, pageSize);
        }

        public ResultResponseModel UpdateStatusStoreBeFood(string BeFoodMerchantID, string StoreNo, string Status, string UserName)
        {
            return _data.UpdateStatusStoreBeFood(BeFoodMerchantID, StoreNo, Status, UserName);
        }

        public ResultResponseModel UpdateStoreBeFood(string BeFoodMerchantIDOld, string BeFoodMerchantIDNew, string StoreNo, string UserName)
        {
            return _data.UpdateStoreBeFood(BeFoodMerchantIDOld, BeFoodMerchantIDNew, StoreNo, UserName);
        }

        public Tuple<int, string, string> ImportExcel_MappingStoreBeFood(List<ImportExcelStoreBeFoodModel> listData)
        {
            return _data.ImportExcel_MappingStoreBeFood(listData);
        }

        // ZaloFood
        public List<ZaloFood_Store_Model> GetStoreMappingByZaloFood(string storeNo, string status, string textSearch, out int totalRecord, int pageNumber, int pageSize)
        {
            return _data.GetStoreMappingByZaloFood(storeNo, status, textSearch, out totalRecord, pageNumber, pageSize);
        }
        public ResultResponseModel UpdateStoreZaloFood(string store_id_old, string store_id_new, string merchant_store_id, string userName)
        {
            return _data.UpdateStoreZaloFood(store_id_old, store_id_new, merchant_store_id, userName);
        }
        public ResultResponseModel UpdateStatusStoreZaloFood(string store_id, string merchant_store_id, string status, string userName)
        {
            return _data.UpdateStatusStoreZaloFood(store_id, merchant_store_id, status, userName);
        }

        public Tuple<int, string, string> ImportExcel_MappingStoreZaloFood(List<ImportExcelStoreZaloFoodModel> listData)
        {
            return _data.ImportExcel_MappingStoreZaloFood(listData);
        }



    }
}
