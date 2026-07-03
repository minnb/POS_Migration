using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Barcode;

namespace VCM.BLUEPOS.Model.Product
{
    public class ProductListResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemNo_PLG { get; set; }
        public string ItemName1 { get; set; }
        public string ItemName2 { get; set; }  // ten san pham tieng viet
        public string SalesUnit { get; set; }
        public string BarcodeNo { get; set; }
        public string BarcodeUnit { get; set; }
        public string TaxGroupCode { get; set; }
        public string ParentCode { get; set; }
        public string Size { get; set; }
        //public string CommonItemNo { get; set; }
        //public string ItemFamilyCode { get; set; }
        //public long? Counter { get; set; }
        //public string Pkey { get; set; }
        public string LastDateModifiedStr { get; set; }
        public int Total { get; set; }

    }

    public class ExportProductListModel
    {
        public string ItemNo { get; set; }
        public string ItemNo_PLG { get; set; }
        public string ItemName1 { get; set; }
        public string ItemName2 { get; set; }
        public string SalesUnit { get; set; }
        public string BarcodeNo { get; set; }
        public string BarcodeUnit { get; set; }
        public string TaxGroupCode { get; set; }
        public string ParentCode { get; set; }
        public string Size { get; set; }
        //public string CommonItemNo { get; set; }
        //public string ItemFamilyCode { get; set; }
        public string LastDateModifiedStr { get; set; }
       
    }

    public class SearchProductModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public bool? Status { get; set; }
        public string Message { get; set; }
    }

    public class CreateProductModel
    {
        public string No2 { get; set; }
        public string ItemName { get; set; }
        public string SearchDescription { get; set; }
        public string ItemNameFull { get; set; }
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
        public int Blocked { get; set; }
        public DateTime? LastDateModified { get; set; }
        public int PriceIncludesVAT { get; set; }
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
        public Byte? BlockedVINID { get; set; }
        public string CommonItemNo { get; set; }
        public string ItemFamilyCode { get; set; }
        public string DivisionCode { get; set; }
        public int KeyinginPrice { get; set; }
        public int ZeroPriceValid { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CreateBarcodeListModel> CreateBarcodeList { get; set; }
        public string CreateBarcodeStr { get; set; }

    }

    public class UpdateProductListResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ItemNameFull { get; set; }
        public string SalesUnit { get; set; }
        public byte? Blocked { get; set; }
        public string BarcodeNo { get; set; }
        public string BarcodeUnit { get; set; }
        public byte? BlockedBarcode { get; set; }
        public string TaxGroupCode { get; set; }
        public string ItemCategoryCode { get; set; }        // nganh hang
        public string Brand { get; set; }                   // thuong hieu
        public string Model { get; set; }                   // model
        public string ItemFamilyCode { get; set; }
        public byte? BlockedVINID { get; set; }
        public string LastDateModifiedStr { get; set; }
        public int Total { get; set; }

    }

    public class UpdateProductListModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ItemNameFull { get; set; }
        public string SalesUnit { get; set; }
        public Byte  Blocked { get; set; }
        public string BarcodeNo { get; set; }
        public string BarcodeUnit { get; set; }
        public Byte BlockedBarcode { get; set; }
        public string TaxGroupCode { get; set; }
        public string ItemCategoryCode { get; set; }        // nganh hang
        public string Brand { get; set; }                   // thuong hieu
        public string Model { get; set; }                   // model
        public string ItemFamilyCode { get; set; }
        public Byte BlockedVINID { get; set; }
        public DateTime LastDateModifiedStr { get; set; }
    }


}
