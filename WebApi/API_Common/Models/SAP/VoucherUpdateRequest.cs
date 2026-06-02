using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TCX.API.Common.Attribute;

namespace VCM.POSBLUE.Model.SAP
{
    public class VoucherUpdateRequest
    {
        public string CompanyCode { get; set; } = "";
        [Required]
        public string VoucherNumber { get; set; }
        public string ArticleNo { get; set; }
        public string ArticleType { get; set; }
        [Required]
        [StringRange(AllowableValues = new[] { "SOLD", "RDM", "EXP" }, ErrorMessage = "Trạng thái không hợp lệ (ACT/RDM/EXP)")]
        [DefaultValue("RDM")]
        public string Status { get; set; }
        [Required]
        [StringLength(6, MinimumLength = 4)]
        public string SiteCode { get; set; } = string.Empty;
        [Required]
        public string POSTerminal { get; set; } = string.Empty;
        public string OrderNo { get; set; }
    }


    public class SeriNoAPIPL
    {
        public string seriNo { get; set; }
        public bool isVoucher { get; set; }
        public double value { get; set; }
    }

    public class VoucherUpdateModel
    {       
        public string OrderNo { get; set; } = "";
        public double TotalBill { get; set; } = 0;
        public string SiteCode { get; set; } = string.Empty;
        public string POSTerminal { get; set; } = string.Empty;
        public bool IsVoucher { get; set; } = true; //true:Voucher, false:Coupon
        public bool IsRetry { get; set; } = false;
        public string PhoneNumber { get; set; } = "";
        public List<VoucherUpdateSerial> ListSeriNo { get; set; }
    }

    public class VoucherUpdateSerial
    {
        public string CompanyCode { get; set; } = "";
        public string Partner { get; set; } = "";
        public string voucherNumber { get; set; }
        public bool isVoucher { get; set; }//true:VOUCER, false:Prepaid Card
        public double value { get; set; }
        public string articleNo { get; set; }
        public string articleType { get; set; }
        public string status { get; set; }//RED,EXP
    }
}
