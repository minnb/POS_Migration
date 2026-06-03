using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class CpnVchBOMHeaderDto
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public int DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public decimal ValueOfVoucher { get; set; }
        public System.DateTime StartingDate { get; set; }
        public System.DateTime EndingDate { get; set; }
        public byte Blocked { get; set; }
        public string CouponCode { get; set; }
        public int LimitQty { get; set; }
        public System.DateTime LastDateModified { get; set; }
        public Nullable<long> Counter { get; set; }
        public Nullable<bool> IsCheckItem { get; set; }
        public string Pkey { get; set; }
        public Nullable<bool> IsMultiUse { get; set; }
        public Nullable<int> LimitQtyUsed { get; set; }
        public string CpnVchType { get; set; }
        public Nullable<bool> IsCheckAPI { get; set; }
        public string SaleType { get; set; }
        public string StoreGroupCode { get; set; }
        public Nullable<double> MinAmount { get; set; }
        public Nullable<int> MinValueType { get; set; }
        public Nullable<int> IsTotalBill { get; set; }
        public Nullable<int> ActiveAfterDay { get; set; }
        public Nullable<int> ValidDay { get; set; }
    }
}
