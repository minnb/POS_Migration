namespace VCM.POSBLUE.Shared.Constants;

/// <summary>Redis key constants (giữ nguyên từ RedisConst cũ).</summary>
public static class RedisConst
{
    public const string Redis_Key_BLUEPOS = "BLUEPOS";
    public const string Redis_Key_WWW_NONE_OTP = "WWW_NONE_OTP";
    public const string Redis_Key_LoyaltyRate = "MD:LoyaltyRate";
    public const string Redis_Key_MemoryCacheConfig = Redis_Key_BLUEPOS + ":Loyalty_MemoryCacheConfig";
    public const string Redis_Key_MASANER_MemberStaff = Redis_Key_BLUEPOS + ":Loyalty_MemberStaff";
    public const string Redis_Key_Loyalty_MemberPoints = Redis_Key_BLUEPOS + ":Loyalty_MemberPoints";
    public const string Redis_Key_Loyalty_RedeemPoints = Redis_Key_BLUEPOS + ":Loyalty_RedeemPoints";
    public const string Redis_Key_Loyalty_BalancePoints = Redis_Key_BLUEPOS + ":Loyalty_BalancePoints";
}
