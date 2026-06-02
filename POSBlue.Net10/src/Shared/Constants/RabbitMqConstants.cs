namespace VCM.POSBLUE.Shared.Constants;

/// <summary>Tên queue RabbitMQ (giữ nguyên từ RabitMQConst cũ).</summary>
public static class RabbitMqConstants
{
    public const string Queue_UpdateStatusVoucher = "queue_redeem_voucher";
    public const string Queue_UpdatePosEnroll = "queue_capillary";
    public const string Queue_SMS = "queue_sms";
    public const string Queue_Ops_Logging = "ops_interface_monitoring";

    public const string Queue_Wincare_TopUpPoints = "queue_loyalty_topup_points";
    public const string Queue_Wincare_Revert_TopUpPoints = "queue_loyalty_revert_topup_points";
    public const string Queue_Loyalty_Member_Points = "queue_loyalty_member_points";
    public const string Queue_Loyalty_Refund_Points = "queue_loyalty_refund_points";
    public const string Queue_Loyalty_LoggingLoyalty = "queue_loyalty_logging";
}
