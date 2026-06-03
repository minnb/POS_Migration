namespace TCX.API.Common.Enums
{
    public enum AppCodeMessageEnum
    {
        BLUEPOS_SQL_EXP = 100,
        BLUEPOS_CAP_TIMEOUT = 0,
        BLUEPOS_CAP_ERROR = 1,
        BLUEPOS_ROP_APIVOUCHER = 2,
        BLUEPOS_WINPAY_TIMEOUT,
        BLUEPOS_WINX_RETURN,
        BLUEPOS_WINX_VOUCHERS,
        BLUEPOS_REDIS_CONNECT,
        BLUEPOS_GOTIT,
        BLUEPOS_URBOX,
        BLUEPOS_ONEU,
        BLUEPOS_VAT_VALIDATE,
        PLH_POS_SAP,
        PLH_POS_PARTNER,
        WINCARE_APP_ERROR,
    }
    public enum TelegramEnum
    {
        Information = 0,
        Critical = 1,
        WebApi = 2
    }

    public enum MessageTypeEnum
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}
