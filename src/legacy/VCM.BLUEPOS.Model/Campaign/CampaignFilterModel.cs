using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignFilterModel
    {
        public string MerchantID { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string OfferType { get; set; }
        public string Status { get; set; }
        public string SearchText { get; set; }
    }

    public class GetOfferTypeModel
    {
        public int ID { get; set; }
        public string OfferType { get; set; }
        public string OfferName { get; set; }
        public string Enabled { get; set; }
    }
}
