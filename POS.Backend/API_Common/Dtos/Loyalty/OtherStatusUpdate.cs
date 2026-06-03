using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class ExtendedFieldLoyalty
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string StoreCode { get; set; }
        public string StoreMapping { get; set; }
        public string PhoneNumber { get; set; }
        public string Desc { get; set; }
        public bool IsSuccess { get; set; }
        public string CashierID { get; set; }
        public DateTime UpdateTime { get; set; }
        public bool Blocked { get; set; }
    }
    public class OtherStatusUpdate
    {
        [Required]
        public string OtherStatus { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        public string CashierID { get; set; }
    }
}
