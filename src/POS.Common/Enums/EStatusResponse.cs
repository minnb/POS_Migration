using System.ComponentModel;

namespace POS.Common.Enums;

public enum EStatusResponse
{
    [Description("Success")]
    Success = 1,
    [Description("Fail")]
    Fail = 0,
    [Description("Error")]
    Error = -1,
    [Description("503 Service Unavailable")]
    ServiceUnavailable = 6688503,
}
