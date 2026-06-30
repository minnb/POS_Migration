using System;
using System.Collections.Generic;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignDetailModel
    {
        public string MerchantID { get; set; }
        public string ObjectIDs { get; set; }
        public string OfferNo { get; set; }
        public string OfferType { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool Blocked { get; set; }  // 21/08/2025,tungnt8
        public string Name { get; set; }
        public string Description { get; set; }
        public string Photo { get; set; }
        public int Price { get; set; }
        public int Value { get; set; }
        public string CrtUser { get; set; }
        public string CrtDate { get; set; }
        public string IsAllStore { get; set; }
        public string IsAllStoreStr
        {
            get
            {
                return IsAllStore.ToLower() == "true" ? "Có" : "Không";
            }
        }
        public string IsSyncToGrab { get; set; }
        public bool EnableSyncToGrap { get; set; }
        public List<CampaignDetailItemModel> CampaignItems { get; set; }
        public List<CampaignStoreModel> CampaignStores { get; set; }
    }
}
