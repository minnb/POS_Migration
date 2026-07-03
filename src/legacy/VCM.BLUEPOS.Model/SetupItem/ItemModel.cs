using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class ItemModel
    {
        public string No { get; set; }
        public string No2 { get; set; }
        public string Description { get; set; }
        public string SearchDescription { get; set; }
        public string LongDescription { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public int StatisticsGroup { get; set; }
        public int CommissionGroup { get; set; }
        public decimal UnitPrice { get; set; }
        public int CostingMethod { get; set; }
        public decimal UnitCost { get; set; }
        public decimal StandardCost { get; set; }
        public string VendorNo { get; set; }
        public string VendorItemNo { get; set; }
        public decimal MaximumInventory { get; set; }
        public decimal ReorderQuantity { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal NetWeight { get; set; }
        public decimal UnitsPerParcel { get; set; }
        public decimal UnitVolume { get; set; }
        public byte Blocked { get; set; }
        public System.DateTime LastDateModified { get; set; }
        public byte PriceIncludesVAT { get; set; }
        public string TaxGroupCode { get; set; }
        public int PreventNegativeInventory { get; set; }
        public decimal MinimumOrderQuantity { get; set; }
        public decimal MaximumOrderQuantity { get; set; }
        public decimal SafetyStockQuantity { get; set; }
        public decimal OrderMultiple { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public string ManufacturerCode { get; set; }
        public string ItemCategoryCode { get; set; }
        public string ProductGroupCode { get; set; }
        public string ServiceItemGroup { get; set; }
        public string ItemTrackingCode { get; set; }
        public string ProductionBOMNo { get; set; }
        public byte BlockedVINID { get; set; }
        public string CommonItemNo { get; set; }
        public string ItemFamilyCode { get; set; }
        public string DivisionCode { get; set; }
        public byte KeyinginPrice { get; set; }
        public byte ZeroPriceValid { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public string ParentCode { get; set; }
        public string Size { get; set; }
        public string ImageName { get; set; }
        public string ImageBase64 { get; set; }
        public Nullable<bool> IsVAT { get; set; }
        public string GroupCode { get; set; }
        public string GroupCodeWeb { get; set; }
        public string GroupCodeGrabFood { get; set; } // 14/04/2025, tungnt8
    }
}
