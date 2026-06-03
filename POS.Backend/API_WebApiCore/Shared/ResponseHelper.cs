using Newtonsoft.Json;
using System;
using System.Net;
using TCX.API.Common.Dtos;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;

namespace TCX.WebApiCore
{
    public static class ResponseHelper
    {
        public static NewHttpResponseDto NewRspOK(object data)
        {
            return new NewHttpResponseDto()
            {
                StatusCode = 200,
                StatusName = "Thành công",
                MessageTechnical = null,
                Data = data
            };
        }
        public static ResultResponse ResponseData(HttpStatusCode status, string message, object data, string messageTechnical = "")
        {
            return new ResultResponse
            {
                Data = data,
                Message = message,
                Status = status,
                MessageTechnical = messageTechnical
            };
        }

        public static ResultResponse ExceptionMessageResponse(HttpStatusCode status, Exception ex)
        {
            return new ResultResponse
            {
                Data = null,
                Message = ex.Message,
                Status = status,
                MessageTechnical = JsonConvert.SerializeObject(ex)
            };
        }
        public static ResultResponse ErrorMessageResponse(HttpStatusCode status, string mess, object data = null, string messTech = "")
        {
            return new ResultResponse
            {
                Data = data,
                Message = mess,
                Status = status,
                MessageTechnical= messTech,
            };
        }
        public static ResultResponse SusscessResponse(HttpStatusCode status, object data = null, string mess = "Success", string messTech = "")
        {
            return new ResultResponse
            {
                Data = data,
                Message = status == HttpStatusCode.OK ? "Thành công" : mess,
                Status = status,
                MessageTechnical = messTech
            };
        }

        public static ResultResponse ExceptionResponse(Exception ex)
        {

            if (ex.InnerException != null && ex.InnerException.InnerException != null)
            {
                FileHelper.WriteLogs("InnerException: " + JsonConvert.SerializeObject(ex.InnerException));
                return new ResultResponse
                {
                    Data = ex.InnerException.InnerException,
                    Message = ex.InnerException.InnerException.Message,
                    Status = HttpStatusCode.InternalServerError
                };
            }

            return new ResultResponse
            {
                Data = null,
                Message = "Lỗi ngoại lệ trong việc xử lý WebApi",
                Status = HttpStatusCode.InternalServerError,
                MessageTechnical = JsonConvert.SerializeObject(ex)
            };

        }
    }
}