using System;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class CpnVchCodeQuotaRemn 
    {
        public string PhoneNumber { get; set; }
        public string ItemNo { get; set; }
        public DateTime OrderDate { get; set; }
        public int InitQuota { get; set; }
        public int Qty { get; set; }
    }
    public class CpnVchCodeSendQuota
    {
        public string ItemNo { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public bool IsCheckMember { get; set; }
        public int QtyOfDay { get; set; }
        public int LimitQty { get; set; }
        public int QtyIssue { get; set; }
        public bool Blocked { get; set; }
    }
}
