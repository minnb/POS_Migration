namespace POS.Common.Dtos.Ops;

public class Ops_Header
{
    public string? Table_name { get; set; }
    public string? Action { get; set; }
}

public class Ops_Monitoring : Ops_Header
{
    public Ops_Monitoring_Data? Data { get; set; }
}

public class Ops_Monitoring_Data
{
    public string? Server_code { get; set; }
    public string? Status { get; set; }
    public string? Group_name { get; set; }
    public string? Version { get; set; }
    public decimal Cpu_percent { get; set; }
    public decimal Memory_mb { get; set; }
    public decimal Requests_today { get; set; }
    public decimal Avg_response_ms { get; set; }
    public decimal Errors_today { get; set; }
}
