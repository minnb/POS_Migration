using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.GotIT
{
    public class MarkUseMultipleDto
    {
        [Required]
        public string Pin {  get; set; }
        [Required]
        public List<string> Codes { get; set; }
        [Required]
        public string Bill_number { get; set; }
        public decimal Total_bill { get; set; }
        public bool Skip_reserved_when_mark_used { get; set; }
        public List<Skus_info_gotit> Skus_info { get; set; }
    }
}
