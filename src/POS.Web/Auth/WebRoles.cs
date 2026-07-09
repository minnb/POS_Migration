namespace POS.Web.Auth;

public static class WebRoles
{
    public const string StoreOperator = "StoreOperator";
    public const string BackOffice    = "BackOffice";
    public const string ITOps         = "ITOps";
    public const string SystemAdmin   = "SystemAdmin";
}

public static class WebPolicies
{
    public const string StoreAndAbove      = "StoreAndAbove";      // cả 4 role
    public const string BackOfficeAndAbove = "BackOfficeAndAbove"; // BackOffice + ITOps + SystemAdmin
    public const string OpsAndAbove        = "OpsAndAbove";        // ITOps + SystemAdmin
    public const string AdminOnly          = "AdminOnly";          // SystemAdmin only
}
