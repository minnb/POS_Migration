using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Reward
{
    public class RewardHeaderRequest
    {
        public string RewardNo { get; set; }
        public string Title { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public string OfferNo { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public Nullable<bool> Enabled { get; set; }
        public Nullable<bool> IsReward { get; set; }
        public List<RewardCodeExcel> listCode { get; set; }
    }

    public class RewardCodeExcel
    {
        public string MaDuThuong { get; set; }
    }
}
