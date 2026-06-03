using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.WinCode
{
    public class WinCodeMemoryDto
    {
        public string ProgramCode { get; set; }
        public string WinCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Quantity { get; set; }
        public bool Status { get; set; }
        public List<WinCodeStoreMemoryDto> ListStore { get; set; }
    }
    public class WinCodeStoreMemoryDto
    {
        public string ProgramCode { get; set; }
        public string StoreNo { get; set; }
        public bool Status { get; set; }
        public string WinCode { get; set; }
        public int Quantity { get; set; }
        public string DiscountType { get; set; }
        public string ApplyType { get; set; }
    }

    public class WincodeResult
    {
        public string WinCode { get; set; }//W001,W002,...
        public string ProgramCode { get; set; }
        public int? Quantity { get; set; }
        public string DiscountType { get; set; } //BILL or ITEM
        public string MerchantId { get; set; }//ALL or PLH
    }
}
