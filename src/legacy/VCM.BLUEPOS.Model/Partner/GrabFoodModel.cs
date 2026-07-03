using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner.GrabFoodModel
{
    public class GrabFood_Menu
    {
        public string MerchantID { get; set; }
        public string PartnerMerchantID { get; set; }
    }

    public class GrabFood_Category
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public List<GrabFood_Item> Items { get; set; }
    }

    public class GrabFood_Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public int Price { get; set; }
        public bool Taxable { get; set; }
        public List<string> Photos { get; set; }
        public string Description { get; set; }
        public List<GrabFood_ModifierGroup> ModifierGroups { get; set; }
    }

    public class GrabFood_ModifierGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public int SelectionRangeMin { get; set; }
        public int SelectionRangeMax { get; set; }
        public List<GrabFood_Modifier> Modifiers { get; set; }
    }
    public class GrabFood_Modifier
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public int Price { get; set; }
        public bool Taxable { get; set; }
    }
    public class GrabFood_Modifier_Update
    {
        public string Id { get; set; }
        public string ModifierGroupId { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public string User { get; set; }
    }

    public class GrabFood_ModifierGroup_Update
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
    }

    public class GrabFood_ModifierGroup_Name_Update
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AvailableStatus { get; set; }
        public int SelectionRangeMin { get; set; }
        public int SelectionRangeMax { get; set; }
    }

    public class GrabFood_Item_Update
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public int Price { get; set; }
        public bool Taxable { get; set; }
        public string Description { get; set; }
        public string User { get; set; }
    }

    public class GrabFood_Modifier_Group_Name
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AvailableStatus { get; set; }
    }
    public class GrabFood_Categories
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AvailableStatus { get; set; }
    }
    public class GrabFood_Item_Create
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public bool Taxable { get; set; } = false;
        public string Photo { get; set; }
        public int Price { get; set; } = 0;
        public int MaxStock { get; set; } = 0;
        public string CategoryID { get; set; }
        public string CategoryName { get; set; }
        public List<GrabFood_Modifier_Group> Modifier_group { get; set; }
        public bool IsModifierGroups { get; set; } = false;
    }
    public class GrabFood_Modifier_Group
    {
        public string Modifier_groupID { get; set; }
        public string Modifier_groupname { get; set; }
        public int SelectionRangeMin { get; set; } = 0;
        public int SelectionRangeMax { get; set; } = 1;
    }

    // 14/03
    public class GrabFoodGroupOptionModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
        public int Price { get; set; }
        public bool Taxable { get; set; }
        public string Photos { get; set; }
        public string Description { get; set; }
        public List<GrabFood_ModifierGroup> ModifierGroups { get; set; }
    }

    // call api : partner/v1/Grab/Get_all_item
    public class Get_All_Item_Grab_Food_Model
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AvailableStatus { get; set; }
        public int Price { get; set; }
        public string CategoryID { get; set; }
        public string CategoryName { get; set; }
    }
    public class Modifier_Create
    {
        public string ItemID { get; set; }
        public string Name { get; set; }
        public string ModifierGroupId { get; set; }
        public string AvailableStatus { get; set; }
        public string User { get; set; }       
    }
    public class Add_Options_Item
    {
        public string ModifierGroupID { get; set; }
        public string AvailableStatus { get; set; }
        public string ItemID { get; set; }
        public int ParentId { get; set; }
        public string User { get; set; }
    }

    // 08/04/2025, New
    public class Item_Update_Model
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public string AvailableStatus { get; set; }
        public string Description { get; set; }
        public string User { get; set; }
        public string ModifierGroupStr { get; set; }

        //public List<ModifierOptionModel> ListOptionGroup { get; set; }  // 24/04/2025
        public ModifierOptionModel ListOptionGroup { get; set; } 

    }

    public class ModifierOptionModel
    {
        public bool ChonSIze { get; set; }
        public bool Size5k { get; set; }
        public bool Size10k { get; set; }
        public bool DoNgot { get; set; }
        public bool Vitra { get; set; }
        public bool ThemTopping { get; set; }        
    }

    public class Item_Update
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public string AvailableStatus { get; set; }
        public string Description { get; set; }
        public string User { get; set; }
        public List<ModifierGroup_Update> ModifierGroup { get; set; }
    }
    public class ModifierGroup_Update
    {
        public string ModifierGroupId { get; set; }
        public string AvailableStatus { get; set; }
    }
    public class GrabFoodStoreModel
    {
        // Grab food
        //public string GrabMerchantID { get; set; }
        public string PartnerMerchantID { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        //public Boolean IsOpen { get; set; }
    }

    public class ItemListSetupRequest
    {
        public string ItemNo { get; set; }
        public List<SalesTypeListModel> ListSalesType { get; set; }
    }
    public class SalesTypeListModel
    {
        public string Code { get; set; }
        public string TenderType { get; set; }
        public string Description { get; set; }
        public Nullable<double> Percent { get; set; }
        public string ImageName { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<int> Order { get; set; }
        public string HotKey { get; set; }
        public Nullable<int> Counter { get; set; }
        public string Pkey { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class SetupItemRequestModel
    {
        public List<ToppingGrabRequestModel> listToppingGrab { get; set; }
    }

    public class ToppingGrabRequestModel
    {
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UOMRef { get; set; }
        public int? Seq { get; set; }
    }

    public class ToppingGrabPartnerRequestModel
    {
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UOMRef { get; set; }
        public int? Seq { get; set; }
    }

    public class ToppingPartnerModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UOM { get; set; }
        public string ModifierGroupID { get; set; }
        public string AvailableStatus { get; set; }
        public bool? isTopping { get; set; }
    }

    public class CreateToppingPartnerModel
    {
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UOMRef { get; set; }
        //public string ModifierGroupId { get; set; }
        public int? Seq { get; set; }
    }

    // 23/04/2025: New
    public class Get_Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AvailableStatus { get; set; }
        public int Price { get; set; }
        public string Photo { get; set; }
        public string CategoryID { get; set; }
        public bool IsTopping { get; set; }
        public bool IsCampaign { get; set; }
        public string CategoryName { get; set; }
        public List<Get_Group_Item> ModifierGroup { get; set; }

    }
    public class Get_Group_Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AvailableStatus { get; set; }
        public string ItemID { get; set; }
    }

    public class GrabFood_SyncMenu
    {
        public string StoreNo { get; set; }

    }

    public class GrabFood_SetupItemModel
    {
        public string ItemNo { get; set; }
        public string ItemNo2 { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
    }





}
