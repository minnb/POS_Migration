using System;

namespace TCX.API.Common.Models
{
    public class VoucherCardTransModel
    {
        public long ID { get; set; }
        public string Partner { get; set; }
        public string SeriNo { get; set; }
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public Nullable<double> Value { get; set; }
        public Nullable<double> SalePrice { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public Nullable<bool> Status { get; set; }
        public string FunctionName { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string UserName { get; set; }
    }
    public class VoucherCardTransResponse
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string SeriNo { get; set; }       
        public Nullable<double> Value { get; set; }
        public string StatusVoucher { get; set; }        
    }
}
