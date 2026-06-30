using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Barcode;

namespace VCM.BLUEPOS.Model.Product
{
    public class StoreProductLockResponseModel
    {
        public int ID { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string ItemNo { get; set; }
        public string StyleProfile { get; set; }
        public string ClosingMethod { get; set; }
        public string Status { get; set; }
        public string IsLock { get; set; }
        public string UnitOfMeasure { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int Total { get; set; }

    }
    public class ItemListResponseModel
    {
        public int ID { get; set; }
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public string VendorNo { get; set; }
        public byte Blocked { get; set; }
        public string Status { get; set; }
        public string LastDateModifiedStr { get; set; }
        public DateTime? LastDateModified { get; set; }
        public string ProductGroupCode { get; set; }
        public string ParentCode { get; set; }
        public string Size { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }

    }

    public class CreateProductLockModel
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string Status { get; set; }
        public List<CreateProductLockListModel> ItemList { get; set; }
        public string ItemListStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateStoreLockListModel
    {   
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string StyleProfile { get; set; }
        public string ClosingMethod { get; set; }
    }
    public class CreateProductLockListModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class ProductLockListResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public string StoreNo { get; set; }
        public bool? Status { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Pkey { get; set; }
    }

    public class CreateProductUnlockModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string POSTerminal { get; set; }
        public string SourceType { get; set; }
        public string Status { get; set; }
        public List<CreateProductUnlockListModel> ProductUnlockList { get; set; }
        public string ProductUnlockListStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateProductUnlockListModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class CreateStoreUnlockListModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string StyleProfile { get; set; }
        public string ClosingMethod { get; set; }
    }

    public class CheckStatusStoreOfProductLockModel
    {
        public string StoreNo { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }
    }


    // POS Phúc Long
    public class POS_ProductLockResponseModel
    {
        public int ID { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string ItemNo { get; set; }
        public string UnitOfMeasure { get; set; }
        public string StyleProfile { get; set; }
        public string ClosingMethod { get; set; }
        public string Status { get; set; }
        public string Pkey { get; set; }
        public string IsLock { get; set; }
        public int Total { get; set; }
    }

    public class POS_StoreProductLockResponseModel
    {
        public int ID { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string ItemNo { get; set; }
        public string UnitOfMeasure { get; set; }
        public string StyleProfile { get; set; }
        public string ClosingMethod { get; set; }
        public string IsLock { get; set; }
        public string Status { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }
    }

    public class List_Active_Item_Async
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string AvailableStatus { get; set; }
        public List<GrabFoodActiveItemAsyncModel> ItemList { get; set; }
        public string ItemListStr { get; set; }
        public string User { get; set; }
    }
    public class GrabFoodActiveItemAsyncModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    // Khoa mon tren grabfood (call api)
    public class GrabFood_Active_Item_Async
    {
        public string ItemId { get; set; }
        public string Store { get; set; }
        public string AvailableStatus { get; set; }
        public string User { get; set; }
    }

    //---- 23/06/2025: Khoa mon tren grabfood (call api) New

    public class Item_Block_Model
    {
        public string Store { get; set; }
        public string ItemListStr { get; set; }
    }
    public class Item_Block
    {
        public string Store { get; set; }
        public List<Item_Block_Active> Item { get; set; }
    }
    public class Item_Block_Active
    {
        public string ItemId { get; set; }
        public string AvailableStatus { get; set; }
    }

    //-------------------------------------

}
