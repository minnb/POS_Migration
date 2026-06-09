namespace POS.Common.Enums;

public enum OpsActionEnum
{
    upsert,
    insert
}

public enum OpsStatusEnum
{
    running,
    idle,
    error,
    unknown,
    warning,
    critical
}
