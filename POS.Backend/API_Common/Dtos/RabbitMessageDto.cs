using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos
{
    public class RabbitMessageDto
    {
        [Required]
        public string DataType { get; set; }
        [Required]
        public string Message { get; set; }
    }
}
