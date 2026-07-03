using System.Collections.Generic;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignImportModel
    {
        public int Sequence { get; set; }                 // Thứ tự
        public List<SelectionItem> Items { get; set; }
    }
}
