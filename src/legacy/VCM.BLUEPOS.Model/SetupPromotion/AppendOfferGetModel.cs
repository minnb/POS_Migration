using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
   public class AppendOfferGetModel
    {
        public string OfferNo { get; set; }
        public string OfferType { get; set; }
        public string ConditonGet { get; set; }
        public bool IsTottalBill { get; set; }
        public bool IsSetupGet { get; set; }
        public Nullable<double> TotalBill { get; set; }
        public OfferGetModel GetOfferGet { get; set; }
    }
}
