using System.Collections.Generic;
using VCM.POSBLUE.Model.VINID;

namespace TCX.API.Common.Models
{
    public class ErrorCapillaryResponse
    {
        public int CreatedId { get; set; }
        public object Data { get; set; }
        public IList<object> Errors { get; set; }
        public IList<object> Warnings { get; set; }
    }
    public class CheckErrorCapillaryResponse
    {
        public int CreatedId { get; set; }     
        public IList<object> Errors { get; set; }
        public IList<object> Warnings { get; set; }
    }
    public class ErrorData
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public class ServerErrorResponses
    {
        public int StatusCode { get; set; }
        public string DataRequest { get; set; }
        public string DataResponse { get; set; }
        public string RequestId { get; set; }
    }
}
