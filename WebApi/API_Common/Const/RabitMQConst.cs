using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Const
{
    public static class RabitMQConst
    {
        public static readonly string Queue_UpdateStatusVoucher = @"queue_redeem_voucher";
        public static readonly string Queue_UpdatePosEnroll = @"queue_capillary";
        public static readonly string Queue_SMS = @"queue_sms";
        public static readonly string Queue_Ops_Logging = @"ops_interface_monitoring";

        public static readonly string Queue_Wincare_TopUpPoints = @"queue_loyalty_topup_points";
        public static readonly string Queue_Wincare_Revert_TopUpPoints = @"queue_loyalty_revert_topup_points";
        public static readonly string Queue_Loyalty_Member_Points = @"queue_loyalty_member_points";
        public static readonly string Queue_Loyalty_Refund_Points = @"queue_loyalty_refund_points";
        public static readonly string Queue_Loyalty_LoggingLoyalty = @"queue_loyalty_logging";
    }
}
