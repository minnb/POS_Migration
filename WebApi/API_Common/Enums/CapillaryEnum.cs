namespace TCX.API.Common.Enums
{
    public enum ObjectErrorEnum
    {
        Item_status = 1001,
        Errors = 1002,
        Warnings = 1003,
        Unauthorized = 401
    }
    public enum LoyaltyProgramEnum
    {
        CapillaryStaffMembershipPoints = 2,
        CapillaryCustomerMembershipPoints = 1,
        StoreTopUpPoints = 3,
        RevertTopUpPoints = 4,
        SkipCapillaryMembershipPoints = 5,
        SwithLoyaltyTheWINX = 6,
        IsOfflineCapillary = 7
    }
    public enum LoyaltyRateEnum
    {
        CAPBLOCK = 10,
        CAPROUND = 100000,
        CAPRATE = 100,
        WINCARE = 70
    }
    public enum MemberCapillaryEnum
    {
        ID = 0,
        PHONE = 1,
        NONE = 2,
        WINCARE = 3,
        WINX = 4
    }
    public enum GenderEnum
    {
        Female = 1,
        Male = 2,
        Other = 3,
        Transgender = 4
    }
    public enum ChannelCapEnum
    {
        POS = 0,
        WINCARE = 1,
        PHUCLONG = 2,
        PhucLongPOS = 998,
        BluePOS = 999
    }
    public enum IdentifiersCapEnum
    {
        mobile = 1,
        email = 2,
        externalId = 3
    }
    public enum TransactionTypeCapEnum
    {
        REGULAR = 1,
        RETURN = 2,
        NOT_INTERESTED = 3,
        NOT_INTERESTED_RETURN = 4,
        RETURNED = 5
    }
    public enum ReturnTypeCapEnum
    {
        FULL = 1,
        LINE_ITEM = 2
    }
    public enum OtherStatusEnum
    {
        WINMONEY_TO_WINPAY = 1,
        WINSCORE_CONFIRM = 2,
        WINSCORE = 3
    }
    public enum MemberTypeCapillaryEnum
    {
        WIN,
        WINX,
        STAFF,
        CUSTOMER
    }
}
