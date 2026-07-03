using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Promotion
{
    public class ExportExcel_PromotionOfferGetModel
    {
        public string OfferNo { get; set; }
        public int? LineNo { get; set; }
        public int? LineType { get; set; }
        public string No { get; set; }      // Ma san pham
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string DiscountTypeStr { get; set; }
        public string DiscountValue { get; set; }
        public string Quantity { get; set; }
        public string Step { get; set; }
        public string BonusBuyNo { get; set; }
        public string LineGroup { get; set; }
        public string ScaleTypeStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }




}
