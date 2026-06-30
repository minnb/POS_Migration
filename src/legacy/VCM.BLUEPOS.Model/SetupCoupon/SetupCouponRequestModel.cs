using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupCoupon
{
    public class SetupCouponRequestModel
    {
        public CpnVchBOMIssueRuleModel issueRuleCoupon { get; set; }
        public List<CodeGeneralCouponRequest> listCoupon { get; set; }
    }
    public class CodeGeneralCouponRequest
    {
        public string Code { get; set; }
    }
}
