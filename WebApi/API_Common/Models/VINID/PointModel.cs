using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class PointModel
    {
        public int? PointEarn { get; set; }
        public int? PointRedeem { get; set; }
        public int? CurrentRate { get; set; }
        public bool? IsOfflineVinID { get; set; }      
        //Bổ sung ngày 23/03/2022
        public string EmpCode { get; set; }//Mã nhân viên đăng ký gói cước để được hưởng quyền lợi nhân viên
        public bool? MasanerPackageInd { get; set; }//Đánh dấu nhân viên có sử dụng gói Masaner (true) hay không (false)
        public int? StaffPercentage { get; set; }//Tỷ lệ phần trăm tích cho nhân viên còn làm việc được thiết lập trên CX
        public int? NormCustPercentage { get; set; }//Tỷ lệ phần trăm tích cho khách hàng thông thường hoặc nhân viên không còn làm việc được thiết lập trên CX
        public string RedemptionId { get; set; }
        public string ReversalId { get; set; }
        public string OrderNo { get; set; }
        public List<PointEarnCampaint> ExtraEarnByCampaign { get; set; }
    }

    public class PointEarnCampaint
    {
        public string LoyaltyMerchantId { get; set; }
        public double Amount { get; set; }
        public double EarnedPoints { get; set; }
    }
}
