using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.LogsData.LoyaltyCXLogAPIModel
{
    public class LoyaltyCXLogAPIResponseModel
    {
        public long? ID { get; set; }
        public string CreateDateStr { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string InvoiceNo { get; set; }
        public string ActionType { get; set; }
        public string PlainText { get; set; }
        public string Signature { get; set; }
        public string RequestPOS { get; set; }
        public string RequestXML { get; set; }
        public string ResponseXML { get; set; }
        public int ResponseTotal { get; set; }
        public string Source { get; set; }
        public int Total { get; set; }
    }

    public class DetailLoyaltyCXLogAPIRequestPOSModel
    {
        public long? ID { get; set; }
        public DateTime? CreateDate { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string InvoiceNo { get; set; }
        public string ActionType { get; set; }
        public string PlainText { get; set; }
        public string Signature { get; set; }
        public string RequestPOS { get; set; }
        public string RequestXML { get; set; }
        public string ResponseXML { get; set; }
        public int? ResponseTotal { get; set; }
        public string Source { get; set; }

    }

    public class DetailLoyaltyCXLogAPIRequestXMLModel
    {
        public long? ID { get; set; }
        public DateTime? CreateDate { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string InvoiceNo { get; set; }
        public string ActionType { get; set; }
        public string PlainText { get; set; }
        public string Signature { get; set; }
        public string RequestPOS { get; set; }
        public string RequestXML { get; set; }
        public string ResponseXML { get; set; }
        public int ResponseTotal { get; set; }
        public string Source { get; set; }
    }

    public class DetailLoyaltyCXLogAPIResponseXMLModel
    {
        public long? ID { get; set; }
        public DateTime? CreateDate { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string InvoiceNo { get; set; }
        public string ActionType { get; set; }
        public string PlainText { get; set; }
        public string Signature { get; set; }
        public string RequestPOS { get; set; }
        public string RequestXML { get; set; }
        public string ResponseXML { get; set; }
        public int ResponseTotal { get; set; }
        public string Source { get; set; }

    }

    public class ExportExcelLoyaltyCXLogAPIModel
    {
        public long? ID { get; set; }
        public string CreateDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string InvoiceNo { get; set; }
        public string ActionType { get; set; }
        public string RequestPOS { get; set; }
        public string RequestXML { get; set; }
        public string ResponseXML { get; set; }
        public int ResponseTotal { get; set; }
        public string Source { get; set; }
    }



}
