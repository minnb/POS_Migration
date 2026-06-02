using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.StagingDB
{
    public class DataRawJsonDto
    {
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string Type { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string BatchFile { get; set; }
        public string FileName { get; set; }
        public string DataJson { get; set; }
        public DateTime CrtDate { get; set; }
        public DateTime ChgDate { get; set; }
        public bool IsRead { get; set; }
        public string Srv { get; set; }
        public Guid Id { get; set; }
        public string MemberCardNo { get; set; }
        public int TransactionType { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VATAmount { get; set; }
        public decimal LineAmountIncVAT { get; set; }
    }
}
