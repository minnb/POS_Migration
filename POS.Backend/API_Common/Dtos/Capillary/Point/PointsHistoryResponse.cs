using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary.Point
{
    public class PointsHistoryResponse
    {
        public List<LedgerEntry> LedgerEntries { get; set; }
    }
    public class LedgerEntry     {
        public string Store { get; set; }
        public string StoreCode { get; set; }
        public int SourceProgramId { get; set; }
        public string NetPointsOnEvent { get; set; }
        public LedgerTransactionDetails TransactionDetails { get; set; }
        public string SourceProgramName { get; set; }
    }
    public class LedgerTransactionDetails
    {
        public long TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string TransactionNumber { get; set; }
        public string Source { get; set; }
        public double GrossBillAmount { get; set; }
    }
}
