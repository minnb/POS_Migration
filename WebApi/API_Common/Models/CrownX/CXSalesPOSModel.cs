using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class CXSalesPOSModel
    {
        public string QRCode { get; set; }
        public string CardNumber { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string InvoiceNo { get; set; }
        public int SpendPoints { get; set; }
        public decimal BillAmount { get; set; }
        public string OrderNo { get; set; }
        public bool IsOffline { get; set; }
        public decimal OrderAmount { get; set; }
    }
}
