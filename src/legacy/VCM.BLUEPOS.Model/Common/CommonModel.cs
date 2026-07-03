using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Common
{
    public class SQLServerModel
    {
        public int ID { get; set; }
        public string Code { get; set; }  // 25/11/2024, tungnt8
        public string IPServer { get; set; }
        public string IPServerRead { get; set; }
        public string NameSvr { get; set; }
        public string UserID { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public Nullable<bool> Status { get; set; }
    }

    public class StoreSetServerModel
    {
        public string StoreNo { get; set; }
        public string ServerIP { get; set; }
        public string ServerRead { get; set; }
        public string Description { get; set; }
        public bool? Status { get; set; }
    }

    public class ResultPingIPLocalModel
    {
        public bool? Status { get; set; }
        public string IPAddress { get; set; }
        public string Message { get; set; }
        public string HtmlData { get; set; }
        public bool? bIsConnected { get; set; }
    }

    public class StyleProfileModel
    {
        public string StyleProfile { get; set; } // VM,VMP
        public string ProfileName
        {
            get
            {
                var name = StyleProfile == "" ? "Tất cả" : StyleProfile == "VM" ? "Winmart" : StyleProfile == "VMP" ? "Winmart+" : "";
                return name;
            }
        }
    }

    public class VoucherReceiptTypeModel
    {
        public string Value { get; set; }
        public string Text { get; set; }

    }

    



}
