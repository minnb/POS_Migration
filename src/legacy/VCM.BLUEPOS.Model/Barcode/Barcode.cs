using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Barcode
{
    public class BarcodeResponseModel
    {
        public int ID { get; set; }
        public string BarcodeNo { get; set; }
        public string ItemNoSAP { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitBarcode { get; set; }
        public string VariantCode { get; set; }
        public Decimal? DiscountPercent { get; set; }
        public string LastDateModified { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }
    }

    public class ExportBarcodeResponseModel
    {
        public string BarcodeNo { get; set; }
        public string ItemNoSAP { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitBarcode { get; set; }
        public Decimal? DiscountPercent { get; set; }
        public string LastDateModified { get; set; }
    }

    public class CreateBarcodeListModel
    {
        public string BarcodeNo { get; set; }
        public string ItemNo { get; set; }
        public int ShowForItem { get; set; }
        public string Description { get; set; }
        public int Blocked { get; set; }
        public DateTime? LastDateModified { get; set; }
        public string VariantCode { get; set; }
        public string UnitOfMeasureCode { get; set; }
        public Nullable<decimal> DiscountPercent { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
    }





}
