using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Model.VINID;

namespace TCX.API.Common.Dtos
{
    public class HttResponsePartner
    {
        public MetaPartner Meta { get; set; }
        public object Data { get; set; }
    }
    public class MetaPartner
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TechMssg { get; set; } 
    }
    public class HttpResponseBlueDto
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
    public class HttpResponseDto
    {
        public int ResponseCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string[] TechnicalMessage { get; set; }
    }
    public class NewHttpResponseDto
    {
        public int StatusCode { get; set; }
        public string StatusName { get; set; }
        public string MessageTechnical { get; set; }
        public object Data { get; set; }
    }
}
