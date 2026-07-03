using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class OfferBuyModel
    {
        public string OfferNo { get; set; }
        public int LineNo { get; set; }
        public int LineType { get; set; }//0 theo Item, 1 theo groupCode
        public string GroupCode { get; set; }
        public bool isTotalBill { get; set; }// true:insert/update OfferBenefit
        public string No { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public int DiscountType { get; set; }
        public Nullable<double> DiscountValue { get; set; }
        public Nullable<double> Quantity { get; set; }

        public Nullable<double> Step { get; set; }
        public string BonusBuyNo { get; set; }
        public string LineGroup { get; set; }
        public string ScaleType { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class OfferBuySetupModel
    {
        public string OfferNo { get; set; }
        public int LineNo { get; set; }
        public int LineType { get; set; }//0 theo Item, 1 theo groupCode
        public string GroupCode { get; set; }
        public bool isTotalBill { get; set; }// true:insert/update OfferBenefit
        public string No { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public int DiscountType { get; set; }
        public float DiscountValue { get; set; }
        public float Quantity { get; set; }     
        public string ScaleType { get; set; }     
    }

}
