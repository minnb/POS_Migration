namespace POS.Common.Enums;

public enum MemberTypeLoyaltyEnum
{
    WIN,
    WINX,
    STAFF
}

public enum HttpStatusCodeEnum
{
    OK = 200,
    Success,
    Failed,
    BadRequest = 400,
    NotFound = 404,
    InternalServerError = 500
}

public enum ActionTypeCapiEnum
{
    RedeemPoints = 1,
    RefundPoints,
    AddTransaction,
    RefundTransaction,
    TopUp,
    ReActiveCoupon,
    RevertPointsTopUp,
    RevertPointsRedeem,
}

public enum CumulativeTypeEnum
{
    POINT = 1,
    AMOUNT = 2
}

public enum UsePointEnum
{
    EARN = 1,
    REDEEM = 2,
    REFUND = 3
}

public enum LoyaltyEnum
{
    PLH = 0,
    WIN = 1,
    CVL = 2
}

public enum OrtherStatusLoyal
{
    DRW_CrossConsulting = 0,
    WinMoneyConversion = 1,
    MemberBusiness = 2,
    Mobile_enroll = 3
}


public enum AkaChainStateLoyaltyEnum
{
    Closed = 1,
    Open = 2
}
