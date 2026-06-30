using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class VoucherReceiptResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }
        public string ReferenceNo_Payment { get; set; }    // BNTT 
        public string ReferenceNo_Voucher { get; set; }    // Số voucher mua hàng
        public string TenderType { get; set; }
        public string StartingTimeStr { get; set; }
        public string OrderTimeStr { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public Nullable<double> TotalAmount { get; set; }
        public string OrderType { get; set; }
        public string CardVinID { get; set; }
        public int Total { get; set; }
    }

    public class ExportVoucherReceiptModel
    {
        public string ReferenceNo_Payment { get; set; }    // BNTT 
        public string ReferenceNo_Voucher { get; set; }    // Số voucher mua hàng
        public string OrderNo { get; set; }
        public string StartingTimeStr { get; set; }
        public string OrderTimeStr { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public Nullable<double> TotalAmount { get; set; }
        public string OrderType { get; set; }
        //public string CardVinID { get; set; }

    }



}
