using System;
using System.ComponentModel.DataAnnotations;

namespace VCM.POSBLUE.Model.SAP
{
    public class CpnVchCodeSendRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public DateTime BussinessDate { get; set; }
        [Required]
        public string ItemNo { get; set; }
        public string PhoneNumber { get; set; }
        public int QtyOfDay { get; set; }
        public int LimitQty { get; set; }
    }
}
