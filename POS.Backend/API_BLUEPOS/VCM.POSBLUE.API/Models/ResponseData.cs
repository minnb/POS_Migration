using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using TCX.API.Common.Response;

namespace VCM.POSBLUE.API.Models
{
    public class ResponseData
    {
        public static HttpResponseMessage HttpResponseData(HttpStatusCode status, string message, object data)
        {
            var objectResponse = new ResponseModel<object, Enum>();
            objectResponse.Status = status;
            objectResponse.Message = message;
            objectResponse.Data = data;
            var httpRespone = new HttpResponseMessage();
            httpRespone.Content = new StringContent(JsonConvert.SerializeObject(objectResponse), Encoding.UTF8, "application/json");
            return httpRespone;
        }
    }
}