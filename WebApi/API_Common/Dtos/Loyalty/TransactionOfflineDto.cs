using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class TransactionOfflineDto
    {
        public string TableName { get; set; }
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string OrigOrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string CardNumber { get; set; }
        public DateTime OrderTime { get; set; }
        public int TransactionType { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal VatAmmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineAmountIncVAT { get; set; }
        public string TenderType { get; set; }
        public decimal AmountTendered { get; set; }
        public string CardType { get; set; }
    }
}
