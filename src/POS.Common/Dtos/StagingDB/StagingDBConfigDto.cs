namespace POS.Common.Dtos.StagingDB;

public class StagingDBConfigDto
{
    public string? Action { get; set; }
    public string? Execution { get; set; }
    public string? Description { get; set; }
    public bool Blocked { get; set; }
}
