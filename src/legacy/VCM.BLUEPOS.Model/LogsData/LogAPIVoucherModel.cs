using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.LogsData
{
    public class LogAPIVoucherModel
    {
        public long? ID { get; set; }
        public string Parner { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string SerialNumber { get; set; }        
        public string ActionType { get; set; }      
        public string PosRequest { get; set; }
        public string PosResponse { get; set; }
        public string PartnerRequest { get; set; }
        public string PartnerResponse { get; set; }
        public int StatusCode { get; set; }
        public string ISNULL { get; set; }
        public int ResponseTotal { get; set; }      
        public int Total { get; set; }
    }
}
