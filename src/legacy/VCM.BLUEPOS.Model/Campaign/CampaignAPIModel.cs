using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignAPIModel
    {
        public string endPoint { get; set; }
        public int timeOut { get; set; }
        public string nameAPI { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string StartTime { get; set; } // 13/08/2025,tungnt8
    }
    // 13/08/2025,tungnt8
    public class CampaignAPICreateV2Model
    {        
        public string StartTime { get; set; }
        public string CampainID { get; set; }
    }


}
