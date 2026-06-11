using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace POS.Common.Dtos.POS
{
    public class KafkaMessageDto
    {
        public string TransactionId { get; set; } = string.Empty;
        [Required]
        public string? DataType { get; set; }
        [Required]
        public string? Message { get; set; }
    }
}
