using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class VatNumberModel
    {
        public int ID { get; set; }
        public string NoseriCode { get; set; }
        public string InvoicePattern { get; set; }
        public string InvoiceType { get; set; }
        public string SerialNo { get; set; }
        public string CompTaxCode { get; set; }
        public string BranchNo { get; set; }
        public string BranchName { get; set; }
        public string StartNumber { get; set; }
        public string EndNumber { get; set; }
        public Nullable<bool> Enabled { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> LastUpdate { get; set; }
    }
}
