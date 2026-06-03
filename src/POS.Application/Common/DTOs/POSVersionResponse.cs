namespace POS.Application.Common.DTOs;

public class POSVersionResponse
{
    public string? LastVersion { get; set; }
    public string? CurVersion { get; set; }
    public DateTime? UpdateTime { get; set; }
    public long? Counter { get; set; }
    public string? Source { get; set; }
    public string? Pkey { get; set; }
    public bool? IsUpdate { get; set; }
    public string? Folder { get; set; }
}
