using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class ItemProcedureModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ImageName { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string BarcodeNo { get; set; }
        public int Total { get; set; }
    }

    // San pham ban kenh NOWFOOD
    public class ItemPartnerResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public int ImageId { get; set; }
        public string BaseUnitOfMeasure { get; set; }
        public string MCH2 { get; set; }            // Ma nganh hang
        public string MCH2_Name { get; set; }       // Ten nganh hang    
        public string StoreNo { get; set; }
        public string SaleType { get; set; }
        public string Size { get; set; }
        public string CupType { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Blocked { get; set; }
        public DateTime? CrtDate { get; set; }
        public string IsTopping { get; set; }
        public int Total { get; set; }
    }




}
