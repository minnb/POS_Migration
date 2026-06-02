using System;
using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class PLH_CheckAPIVoucherRequest
    {
        public string partner { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public bool isVoucher { get; set; }
        public bool isWeb { get; set; }
        public List<PLH_SeriNoRequest> listSeriNo { get; set; }
    }
    public class PLH_SeriNoRequest
    {
        public string seriNo { get; set; }
        public string articleSAP { get; set; }
        public bool isEmployee { get; set; }
    }

    public class PLH_InforVoucherResponse
    {
        public string SeriNo { get; set; }
        public string TypeVoucher { get; set; }
        public string StatusVoucher { get; set; }
        public string DescVoucher { get; set; }
        public Nullable<double> Value { get; set; }
        public bool? IsEmployee { get; set; }
    }
}
