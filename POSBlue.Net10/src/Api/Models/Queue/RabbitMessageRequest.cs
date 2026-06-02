using System.ComponentModel.DataAnnotations;

namespace VCM.POSBLUE.Api.Models.Queue;

/// <summary>Request đẩy message vào RabbitMQ (giữ nguyên từ RabbitMessageDto cũ).</summary>
public class RabbitMessageRequest
{
    [Required]
    public string DataType { get; set; } = default!;

    [Required]
    public string Message { get; set; } = default!;
}
