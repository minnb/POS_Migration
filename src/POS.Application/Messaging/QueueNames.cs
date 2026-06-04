namespace POS.Application.Messaging;

// Tập trung tên queue — lấy từ RabitMQConst trong dự án cũ.
public static class QueueNames
{
    // Loyalty / Capillary
    public const string LoyaltyMemberPoints = "BLUEPOS:Loyalty:MemberPoints";
    public const string LoyaltyReturnTransaction = "BLUEPOS:Loyalty:ReturnTransaction";

    // WinCare Staff Points
    public const string WincareTopUpPoints = "BLUEPOS:Wincare:TopUpPoints";
    public const string WincareRevertTopUpPoints = "BLUEPOS:Wincare:RevertTopUpPoints";
}
