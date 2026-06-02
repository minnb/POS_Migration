using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Capillary.Customer;

namespace TCX.API.Common.Dtos.Capillary.Transaction
{
    public class AddTransactionErrorResponse : CustomerRegistrationResponse
    {

    }
    public class AddTransactionResponse : CustomerRegistrationResponse
    {
        public new long CreatedId { get; set; }
        public List<RawSideEffects> RawSideEffects { get; set; }
        public PointsCustomer Customer { get; set; }
    }
    public class PointsCustomer
    {
        public long ProgramId { get; set; }
        public long TotalPoints { get; set; }
        public long MainPoints { get; set; }
        public long PromisedPoints { get; set; }
    }
    public class RawSideEffects
    {
        public string AwardOn { get; set; }
        public string AwardedOn { get; set; }
        public string BillNumber { get; set; }
        public List<LineItemIdCAP> LineItemId { get; set; }
        public string Points { get; set; }
        public string Quantity { get; set; }
        public string SourceType { get; set; }
        public string SourceValue { get; set; }
        public string Type { get; set; }
        public string ProgramId { get; set; }
        public string PromoId { get; set; }
        public string PromoName { get; set; }
    }
    public class LineItemIdCAP
    {
        public string Id { get; set; }
        public string ItemCode { get; set; }
        public decimal Amount { get; set; }
        public decimal Qty { get; set; }
        public LineItemsExtendedFields ExtendedFields { get; set; }
    }
}
