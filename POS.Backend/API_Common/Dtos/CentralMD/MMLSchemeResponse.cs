using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Loyalty;

namespace TCX.API.Common.Dtos.CentralMD
{
    public enum MMLSchemeResponseEnum
    {
        Journey_1 = 1,
        Journey_2 = 2,
        Journey_3 = 3,
        Journey_4 = 4,
    }
    public class MMLSchemeResponse
    {
        public string  HeaderCode { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public bool IsGenQR { get; set; }
        public bool Enabled { get; set; }
        public string Description { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public string Ref5 { get; set; }

    }

    public class MMLSchemeRequest
    {
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string OrderNo { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string Code { get; set; }
        public string MemberCardNo { get; set; }
        public string UserId { get { return MemberCardNo; } }
        public DateTime OrderTime { get; set; } = DateTime.Now;
        public bool IsMember { get; set; }
        public List<MMLSchemeItemsRequest> Items { get; set; }
        public List<PaymentEntryLoyalty> Payments { get; set; }

    }
    public class MMLSchemeItemsRequest
    {
        public int LineNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public string Barcode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VatAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineAmountIncVAT { get; set; }
        public string PackId { get; set; }
    }
}