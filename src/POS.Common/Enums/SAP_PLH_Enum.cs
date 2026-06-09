namespace POS.Common.Enums;

public static class SAP_PLH_StatusConst
{
    public static List<string> PLH_VoucherStatusConst = ["EXPI", "REDE", "CANC", "SOLD"];
}

public enum PLH_VoucherStatusEnum
{
    EXPI,
    REDE,
    CANC,
    SOLD
}
