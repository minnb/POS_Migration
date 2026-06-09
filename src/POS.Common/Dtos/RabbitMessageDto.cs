using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos;

public class RabbitMessageDto
{
    [Required]
    public string DataType { get; set; } = string.Empty;
    [Required]
    public string Message { get; set; } = string.Empty;
}
