namespace VCM.POSBLUE.Shared.DTOs;

/// <summary>Thông tin lỗi server khi gọi API đối tác (giữ nguyên từ ServerErrorResponses cũ).</summary>
public class ServerErrorResponses
{
    public int StatusCode { get; set; }
    public string? DataRequest { get; set; }
    public string? DataResponse { get; set; }
}
