namespace POS.Infrastructure.Repositories;

public sealed class SpAuditOptions
{
    public const string SectionName = "SpAudit";

    public int CleanupRetentionDays { get; set; } = 90;
    public int CommandTimeoutSeconds { get; set; } = 60;
    public int MaxProceduresPerDatabase { get; set; } = 5000;
    public string[] TargetDatabases { get; set; } = ["CentralMD", "CentralSale", "Loyalty"];
}
