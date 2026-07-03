using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignAPIResultModel<T>
    {
        public Meta Meta { get; set; } = new Meta();
        public T Data { get; set; }
    }
    public class Meta
    {
        public int Code { get; set; } = 0;
        public string Message { get; set; }
        public string TechMssg { get; set; }
    }
}
