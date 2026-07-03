using System;
using System.Collections.Generic;
using System.Net;

namespace VCM.BLUEPOS.Common
{
    public class ResultResponse
    {
        public HttpStatusCode Status { get; set; }
        public string Message { get; set; }
        public string ViewPartial { get; set; }
        public object Data { get; set; }
        public int isFlag { get; set; }
    }
    public class APIResponseStatus
    {
        public static int Ok = 200;
        public static int InternalServerError = 500;
        public static int BadRequest = 400;
        public static int ServiceUnavailable = 503;
        public static int Conflict = 409;
        public static int Unused = 306;
        public static int Unauthorized = 401;
    }
    public class ApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public string Msg { get; set; }
    }
    public class ApiResponseModel
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public List<string> Msg { get; set; }
    }


}
