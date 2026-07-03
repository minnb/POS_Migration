using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class EODCreatedInvoiceModel
    {
        public int ID { get; set; }
        public string BusinessDateCode { get; set; }
        public Nullable<System.DateTime> BusinessDate { get; set; }
        public string StoreNo { get; set; }
        public string Key { get; set; }
        public string DocumentNo { get; set; }
        public Nullable<bool> IsCompleted { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
    }


}
