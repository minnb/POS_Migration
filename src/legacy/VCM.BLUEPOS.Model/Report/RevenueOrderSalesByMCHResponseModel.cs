using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class RevenueOrderSalesByMCHResponseModel
    {
        public List<RevenueOrderSalesByMCHModel> ListData { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string TransDate { get; set; }
        public string Message { get; set; }
        public bool? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }




}
