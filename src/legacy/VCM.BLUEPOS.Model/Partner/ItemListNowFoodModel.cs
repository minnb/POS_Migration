using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{
    public class OptionModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
    public class Shopee_Restaurant_Model
    {
        public int restaurant_id { get; set; }
        public string partner_restaurant_id { get; set; }
        public string name { get; set; }
    }
    public class ViewItemListNowFoodModel
    {
        public int TotalRow { get; set; }
        public int TotalPageNumber { get; set; }
        public int PageSize { get; set; }
        public string AppCode { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }
        public string Keyword { get; set; }
        public string MCH2 { get; set; }            // Mã nganh hang
        public string MCH2_Name { get; set; }       // Ten nganh hang
        public List<ItemListNowFoodResponseModel> ListItem { get; set; }
        public string CupType { get; set; }
    }
    public class ItemListNowFoodResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ImageName { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string MCH2 { get; set; }            // Ma nganh hang
        public string MCH2_Name { get; set; }       // Ten nganh hang    
        public string StoreNo { get; set; }
        public string AppCode { get; set; }
        public string Size { get; set; }
        public string CupType { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Blocked { get; set; }
        public DateTime? CrtDate { get; set; }
        public string IsTopping { get; set; }
        public int Total { get; set; }
    }

    public class CreateItemListPLHResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Blocked { get; set; }
        public string ImageName { get; set; }
        public int Total { get; set; }
    }

    public class CreateViewItemListNowFoodModel
    {
        public int TotalRow { get; set; }
        public int TotalPageNumber { get; set; }
        public int PageSize { get; set; }
        public string AppCode { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }
        public string Keyword { get; set; }
        public string MCH2 { get; set; }            // Mã nganh hang
        public string MCH2_Name { get; set; }       // Ten nganh hang
        public List<CreateItemListPLHResponseModel> ListItem { get; set; }
        public string CupType { get; set; }
    }

    // ----- update -----
    public class UpdateViewItemListNowFoodModel
    {
        public int TotalRow { get; set; }
        public int TotalPageNumber { get; set; }
        public int PageSize { get; set; }
        public string AppCode { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }
        public string Keyword { get; set; }
        public string MCH2 { get; set; }            // Mã nganh hang
        public string MCH2_Name { get; set; }       // Ten nganh hang
        public List<UpdateItemListPLHResponseModel> ListItem { get; set; }
        public string CupType { get; set; }
    }
    public class UpdateItemListPLHResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Blocked { get; set; }
        public string ImageName { get; set; }
        public int Total { get; set; }
    }
    public class PartnerRestaurantModel
    {
        public int restaurant_id { get; set; }
        public string restaurant_name { get; set; }
        public string partner_restaurant_id { get; set; }
        public string name { get; set; }
    }

    public class StoreByPartnerResponseModel
    {
        // Grab food
        public string GrabMerchantID { get; set; }
        public string PartnerMerchantID { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public Boolean IsOpen {get; set;}

        // Shoppe food
        public int restaurant_id { get; set; }
        public string restaurant_name { get; set; }
        public string partner_restaurant_id { get; set; }
        public string name { get; set; }

    }

    // ----- update -----
    public class UpdateViewItemListByPartnerModel
    {
        public int TotalRow { get; set; }
        public int TotalPageNumber { get; set; }
        public int PageSize { get; set; }
        public string AppCode { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }
        public string Keyword { get; set; }
        public string MCH2 { get; set; }            // Mã nganh hang
        public string MCH2_Name { get; set; }       // Ten nganh hang
        public List<UpdateItemListByPartnerResponseModel> ListItem { get; set; }
        public string CupType { get; set; }
    }

    public class UpdateItemListByPartnerResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Blocked { get; set; }
        public string ImageName { get; set; }
        public int Total { get; set; }
    }
    public class CategoryByPartnerModel
    {
        // Grab food
        public string Id { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public Guid? CateId { get; set; }
    }

}
