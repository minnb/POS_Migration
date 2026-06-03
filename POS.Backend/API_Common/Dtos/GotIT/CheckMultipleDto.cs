using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.GotIT
{
    public class CheckMultipleDto
    {
        [Required]
        public string Pin {  get; set; }
        [Required]
        public List<string> Codes { get; set; }
        public string Bill_number { get; set; }
        public bool Skip_reserved_when_mark_used { get; set; }
        public List<Skus_info_gotit> Skus_info { get; set; }
        public decimal Total_bill { get; set; }
    }
    public class Skus_info_gotit
    {
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
    }
}
