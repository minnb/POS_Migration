namespace POS.Common.Const;

public static class RedisConst
{
    public static readonly string Redis_Key_BLUEPOS = "BLUEPOS";
    public static readonly string Redis_Key_POS_PLH = "POS_PLH ";
    public static readonly string Redis_Key_SAPCODE_BNTT_ROP = "WWW_SAPCODE_BNTT_ROP";
    public static readonly string Redis_Key_WINCODE_W006 = "WWW_WINCODE_W006";
    public static readonly string Redis_Key_WWW_ROP_V_PREFIX = "WWW_ROP_V_PREFIX";
    public static readonly string Redis_Key_WWW_WIN_MERCHANT_ID = "WWW_WIN_MERCHANT_ID";
    public static readonly string Redis_Key_WWW_NONE_OTP = "WWW_NONE_OTP";

    public static readonly string Redis_Key_OtherStatusLoyalty = "ExtendedFieldsLoyalty";
    public static readonly string Redis_Key_ItemPointsMember = "MD:ItemPointsMember";
    public static readonly string Redis_Key_LoyaltyRate = "MD:LoyaltyRate";
    public static readonly string Redis_Key_MMLSchemeHeader = "MD:MMLSchemeHeader";
    public static readonly string Redis_Key_MMLSchemeItem = "MD:MMLSchemeItem";
    public static readonly string Redis_Key_MMLSchemeResponse = "MD:MMLSchemeResponse";

    public static readonly string Redis_Key_NotifyTelegram = "NotifyTelegram";
    public static readonly string Redis_Key_SysWebApi = "SysWebApi";
    public static readonly string Redis_Key_SysWebApiUser = "SysWebApiUser";
    public static readonly string Redis_Key_POSDataSetup = "POSDataSetup";
    public static readonly string Redis_Key_NotifyConfig = "NotifyConfig";
    public static readonly string Redis_Key_Stores = "Stores";
    public static readonly string Redis_Key_StoresMappingVinID = "StoresMappingVinID";
    public static readonly string Redis_Key_GetFileFromFTP = "GetFileFromFTP";
    public static readonly string Redis_Key_CreateMasterDataSlots = "MD:CreateMasterData:Slots";
    public static readonly string Redis_Key_CpnVchCodeSendQuota = "CpnVchCodeSendQuota";
    public static readonly string Redis_Key_CpnVchBOMHeader = "CpnVchBOMHeader";

    public static readonly string Redis_Key_MemoryCacheConfig = $"{Redis_Key_BLUEPOS}:Loyalty_MemoryCacheConfig";
    public static readonly string Redis_Key_MASANER_MemberStaff = $"{Redis_Key_BLUEPOS}:Loyalty_MemberStaff";
    public static readonly string Redis_Key_Loyalty_MemberPoints = $"{Redis_Key_BLUEPOS}:Loyalty_MemberPoints";
    public static readonly string Redis_Key_Loyalty_RedeemPoints = $"{Redis_Key_BLUEPOS}:Loyalty_RedeemPoints";
    public static readonly string Redis_Key_Loyalty_BalancePoints = $"{Redis_Key_BLUEPOS}:Loyalty_BalancePoints";

    public static string GetPOSSystem(string sys)
    {
        if (sys == "PLH")
            return Redis_Key_POS_PLH;
        else if (sys == "WCM")
            return Redis_Key_BLUEPOS;
        else
            return "POS";
    }

    public static string GetRedisKeyLoyaltyBalancePoints(string hashField)
    {
        return RedisKeyConst.GetShardKeyPrefix(Redis_Key_Loyalty_BalancePoints, hashField, 3);
    }

    public static string GetRedisKeyLoyaltyMemberPoints(string hashField)
    {
        return RedisKeyConst.GetShardKeyPrefix(Redis_Key_Loyalty_MemberPoints, hashField);
    }
}
