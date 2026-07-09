namespace POS.Common.Dtos.LogService;

public class LogServiceModel
{
    public string? StoreNo { get; set; }
    public string? PosTerminal { get; set; }
    public string? RequestPOS { get; set; }
    public string? RequestPartner { get; set; }
    public string? ResponsPOS { get; set; }
    public string? ResponsePartner { get; set; }
    public string? EndPointPartner { get; set; }
    public string? MethodPartner { get; set; }
    public string? ActionController { get; set; }
    public int TimeResponse { get; set; }
    public DateTime? CreatedDate { get; set; }
}

