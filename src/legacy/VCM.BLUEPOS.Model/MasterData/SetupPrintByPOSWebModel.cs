using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MasterData
{
    public class SetupPrintByPOSWebResponseModel
    {
        public int ID { get; set; }
        public string IPAddress { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }
        public int Total { get; set; }

    }

    public class CreateSetupPrintByPOSWebModel
    {
        public string IPAddress { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }
    }

    public class UpdateSetupPrintByPOSWebModel
    {
        public string IPAddress { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }

    }

    public class DeleteSetupPrintByPOSWebModel
    {
        public string IPAddress { get; set; }
        public string StoreNo { get; set; }
        public bool? Status { get; set; }

    }

    public class ExportEExcelSetupPrintByPOSWebModel
    {
        public string IPAddress { get; set; }
        public string StoreNo { get; set; }
        public string Status { get; set; }

    }



}
