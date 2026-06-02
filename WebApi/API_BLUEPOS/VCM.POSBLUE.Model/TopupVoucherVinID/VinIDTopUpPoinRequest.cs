using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDTopUpPoinRequest
    {
        public string phone_number { get; set; }
        public string store_code { get; set; }
        public string pos_code { get; set; }
        public int point_amount { get; set; }
        public string order_id { get; set; }
        public string txn_id { get; set; }
        public bool send_receipt { get; set; }//Yêu cầu xuất hóa đơn  true: có. false : không
        public ReceiptInfo receipt_info { get; set; }//Thông tin để xuất hóa đơn
    }
    public class ReceiptInfo
    {
        public string name { get; set; }//Tên ghi hóa đơn, Tối đa 50 ký tự
        public string address { get; set; }//Địa chỉ ghi hóa đơn,   Tối đa 100 ký tự
        public string email { get; set; }//Email ghi hóa đơn (Yêu cầu truyền đúng định dạng)   Tối đa 50 ký tự.
        public string tax_code { get; set; }//Mã số thuế ghi hóa đơn.Tối đa 32 ký tự
    }

    public class VinIDTopUpPoinPosRequest
    {
        public string PhoneNumber { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public int PointAmount { get; set; }
        public string OrderNo { get; set; }
        public bool IsVAT { get; set; }
        public ReceiptPOSInfo InfoVAT { get; set; }
    }

    public class ReceiptPOSInfo
    {
        public string FullName { get; set; }//Tên ghi hóa đơn, Tối đa 50 ký tự
        public string Address { get; set; }//Địa chỉ ghi hóa đơn,Tối đa 100 ký tự
        public string Email { get; set; }//Email ghi hóa đơn (Yêu cầu truyền đúng định dạng), Tối đa 50 ký tự.
        public string TaxCode { get; set; }//Mã số thuế ghi hóa đơn.Tối đa 32 ký tự
    }
}
