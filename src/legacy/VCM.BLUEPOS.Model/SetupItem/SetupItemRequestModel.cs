using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class SetupItemRequestModel
    {
        public ItemHeaderRequestModel itemHeader { get; set; }
        public List<BarcodeRequestModel> listBarcode { get; set; }
        public List<ToppingRequestModel> listTopping { get; set; }
        public List<CupRequestModel> listCup { get; set; }
        public List<OptionRequestModel> listOption { get; set; }
        public List<DinhLuongRequestModel> listDinhLuong { get; set; }
        public List<PosGroupRequestModel> listPosGroup { get; set; }
        public List<PosGroupRequestModel> listPosGroupWeb { get; set; }
        public List<ToppingGrabRequestModel> listToppingGrab { get; set; }
    }
    public class ItemHeaderRequestModel
    {
        public string ItemNo { get; set; }
        public string ItemNo2 { get; set; }
        public string ItemName { get; set; }
        public string ImgName { get; set; }
        public string ImgBase64 { get; set; }
        public bool ImgChanged { get; set; }
        public string ItemDecs { get; set; }
        public string ItemType { get; set; }
        public string UOM { get; set; }
        public string TAX { get; set; }
        public string Size { get; set; }
        public string ParentCode { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsVAT { get; set; }
        public bool CheckManual { get; set; }
    }
    public class BarcodeRequestModel
    {
        public string Barcode { get; set; }
        public string UOM { get; set; }
    }
    public class ToppingRequestModel
    {
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UOMRef { get; set; }
        public int? Seq { get; set; }
    }

    public class ToppingGrabRequestModel
    {
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UOMRef { get; set; }
        //public string ModifierGroupId { get; set; }
        public int? Seq { get; set; }
    }
    public class CupRequestModel
    {
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UOMRef { get; set; }
        public bool IsDefault { get; set; }
        public string SalesTypeCode { get; set; }
        public int? Seq { get; set; }
    }
    public class OptionRequestModel
    {
        public string ItemNoRef { get; set; }
        public string ItemNameRef { get; set; }
        public string UOMRef { get; set; }
        public int? Seq { get; set; }
    }
    public class DinhLuongRequestModel
    {
        public string ItemNoOption { get; set; }
        public string OptionType { get; set; }
        public string OptionName { get; set; }
        public bool IsDefault { get; set; }
        public int? Seq { get; set; }
    }
    public class PosGroupRequestModel
    {
        public string GroupCode { get; set; }
    }

    // GrabFood
    public class GrabFood_Item_Create
    {
        public string Id { get; set; }
        public string Name { get; set; }        
        public string CategoryID { get; set; }
        public bool IsDefault { get; set; }
        public string UOM { get; set; }
        public string Merchant { get; set; }
        public string User { get; set; }
    }

    public class GrabFood_Modifier_Create
    {
        public string ItemID { get; set; }
        public string Name { get; set; }
        public string UOM { get; set; }
        public string ModifierGroupId { get; set; }
        public string AvailableStatus { get; set; }
        public string Merchant { get; set; }  // Grab / Befood
        public string User { get; set; }
    }




}
