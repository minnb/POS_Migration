using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Voucher
{
    public class POSDocumentNosResponseModel
    {
        public int ID { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string TransDate { get; set; }
        public string LastNumber { get; set; }
        public string LastDateTime { get; set; }
        public string DocumentType { get; set; }
    }

    public class DocumentNoResponseModel
    {
        public string DocumentNumber { get; set; }
    }

    public class UpdatePOSDocumentNoModel
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string TransDate { get; set; }
        public string LastNumber { get; set; }
        public DateTime LastDateTime { get; set; }
        public string DocumentType { get; set; }
        public string IPAddress { get; set; }
    }

    public class InsertPOSDocumentNoModel
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string TransDate { get; set; }
        public string LastNumber { get; set; }
        public DateTime LastDateTime { get; set; }
        public string DocumentType { get; set; }
        public string IPAddress { get; set; }
    }



}
