namespace POS.Application.Common.DTOs;

// Giữ nguyên field names như API cũ (BussinessDate - typo cũ giữ nguyên)
public class BusinessDateResponse
{
    public DateTime? BussinessDate { get; set; }
    public DateTime? CurrentDate { get; set; }
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
}
