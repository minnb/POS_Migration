using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.SAP
{
    public class CpnVoucherCodeSendData : CpnVchCodeSendModel
    {
        public string PhoneNumber { get; set; }
    }
    public class CpnVchCodeSendModel
    {
        public string SerialNumber { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string OrderNo { get; set; }
        public string StatusCode { get; set; }
        public string Status { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string ActicleType { get; set; }
        public bool IsCheckItem { get; set; }
        public int MaxQtyUse { get; set; }
        public double MaxAmount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public double ValueCpnVch { get; set; }
    }
}
