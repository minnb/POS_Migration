using System;
using System.Collections.Generic;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class ViewCapNhatSanPhamModel
    {
        public string Id { get; set; }
        public ItemModel InfoItem { get; set; }
        public List<UnitOfMeasureModel> ListUOM { get; set; }
        public List<ComboModel> ListCombo { get; set; }
        public List<BarcodeRequestModel> ListBarcode { get; set; }
        public List<ItemExtraModel> ListItemExtra { get; set; }
        public List<ItemOptionModel> ListItemOption { get; set; }
        public List<POSGroupModel> ListPOSGroup { get; set; }
        public List<POSGroupModel> ListPOSWEBGroup { get; set; }
        public List<OptionComboxModel> ListCategoryGrabFood { get; set; }   // tungnt8 : 14/04/2025
        public List<CheckItemGrabFoodModel> ListItemGrabFood { get; set; }  // tungnt8 : 15/04/2025
        public List<ToppingGrabFoodModel> ListToppingGrab { get; set; }     // tungnt8 : 21/04/2025

    }
    public class UnitOfMeasureModel
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }
    public class ComboModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
    }
    public class ItemExtraModel
    {
        public string ItemNo { get; set; }
        public string UnitOfMeasure { get; set; }
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UnitOfMeasureRef { get; set; }
        public string Description { get; set; }
        public string SaleTypeCode { get; set; }
        public string SaleTypeName { get; set; }
        public string Type { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public Nullable<short> Status { get; set; }
        public Nullable<System.DateTime> LastDateModified { get; set; }
        public Nullable<bool> IsDefault { get; set; }
        public Nullable<int> Seq { get; set; }
    }
    public class GroupItemExtraModel
    {
        public string ItemNo { get; set; }
        public string UnitOfMeasure { get; set; }
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UnitOfMeasureRef { get; set; }
        public string Description { get; set; }      
        public Nullable<bool> IsDefault { get; set; }
        
    }
    public class ItemOptionModel
    {
        public string ItemNo { get; set; }
        public string ItemNoOption { get; set; }
        public string OptionType { get; set; }
        public string OptionName { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public Nullable<short> Status { get; set; }
        public Nullable<System.DateTime> LastDateModified { get; set; }
        public Nullable<bool> IsDefault { get; set; }
        public Nullable<int> Seq { get; set; }
    }
    public class POSGroupModel
    {
        public int Id { get; set; }
        /// <summary>
        /// Mã nhóm sản phẩm
        /// </summary>
        public string GroupCode { get; set; }
        /// <summary>
        /// Tên nhóm sản phẩm
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Phân cấp nhóm sản phẩm
        /// </summary>
        public Nullable<int> Level { get; set; }
        /// <summary>
        /// Mã cha nhóm sản phẩm
        /// </summary>
        public string ParentCode { get; set; }
        /// <summary>
        /// Sắp xếp
        /// </summary>
        public Nullable<int> Seq { get; set; }
        /// <summary>
        /// Trạng thái (1-Active/0-Deactive)
        /// </summary>
        public int Status { get; set; }
    }

    public class PosGroupItemModel
    {
        public int Id { get; set; }
        public string GroupCode { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public int Seq { get; set; }
        public int Counter { get; set; }
        public string Pkey { get; set; }
        public int Status { get; set; }
        public DateTime LastDateModified { get; set; }
    }

    //public class ToppingGrabFoodModel
    //{
    //    public string Id { get; set; }
    //    public string Name { get; set; }
    //    public string UOM { get; set; }
    //    public string ModifierGroupID { get; set; }
    //    public string AvailableStatus { get; set; }
    //    public bool? isTopping { get; set; }
    //}


}
