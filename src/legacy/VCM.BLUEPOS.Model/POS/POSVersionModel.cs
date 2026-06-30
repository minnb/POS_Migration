using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.POS
{
    public class UploadFilePOSDataSetupModel
    {
        public string Message { get; set; }
        public bool? Status { get; set; }
        public List<string> ListFileUploadError { get; set; }
        public int TotalFile { get; set; }
        public int ErrorFileNumber { get; set; }
    }

    public class POSVersionResponseModel
    {
        public string ID { get; set; }
        public string LastVersion { get; set; }
        public string CurVersion { get; set; }
        public string UpdateTimeStr { get; set; }
        public long? Counter { get; set; }
        public string Source { get; set; }
        public string Pkey { get; set; }
        public bool? IsUpdate { get; set; }
        public string Folder { get; set; }
    }

    public class POSVersionModel
    {
        public string LastVersion { get; set; }
        public string CurVersion { get; set; }
        public DateTime? UpdateTime { get; set; }
        public long? Counter { get; set; }
        public string Source { get; set; }
        public string Pkey { get; set; }
        public bool? IsUpdate { get; set; }
        public string Folder { get; set; }
    }

    public class UpdatePOSVersionModel
    {
        public string LastVersion { get; set; }
        public string CurVersion { get; set; }
        public string Source { get; set; }
        public string IsUpdate { get; set; }
        public string IsUpdateFile { get; set; }
        public Int64? Counter { get; set; }
        public string Folder { get; set; }
    }

    public class UpdatePOSVersionResponseModel
    {
        public string LastVersion { get; set; }
        public string CurVersion { get; set; }
        public string Source { get; set; }
        public bool? IsUpdate { get; set; }
        public Int64? Counter { get; set; }

    }

    public class ServerInfoPOSDataSetupModel
    {
        public string Code { get; set; }
        public string Value { get; set; }
    }


}
