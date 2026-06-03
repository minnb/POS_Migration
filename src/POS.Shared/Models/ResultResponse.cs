using Newtonsoft.Json;

namespace POS.Shared.Models;

// Giữ nguyên 100% cấu trúc JSON như API cũ:
// { "Status": 200, "Message": "...", "Data": {...}, "MessageTechnical": "..." }
public class ResultResponse
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? MessageTechnical { get; set; }
}
