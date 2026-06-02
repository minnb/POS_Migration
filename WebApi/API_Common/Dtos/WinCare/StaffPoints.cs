using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.WinCare
{
    public class SentNotifyPOSRequest: SentNotifyUserWincareRequest
    {
        public string OrderNo { get; set; }
    }
    public class SentNotifyUserWincareRequest
    {
        public string SiteId { get; set; }
        public string VoucherCode { get; set; }
        public string EmployeeCode { get; set; }
        public string RequestTime { get; set; }
        public string Note { get; set; }
        public int TypeId { get; set; }
    }
    public class StaffPointsWincareRequest
    {
        public string PosNo { get; set; }
        public string Barcode { get; set; }
        public string RequestTime { get; set; }
    }
    public class StaffPointsWincareData
    {
        public string StaffPointsId { get; set; }
        public string EmployeeCode { get; set; }
        public string PhoneNumber { get; set; }
    }
}
