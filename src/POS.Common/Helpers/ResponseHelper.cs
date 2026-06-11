using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace POS.Common.Helpers
{
    public static class ResponseHelper
    {
        public static ResultResponse Response(
            HttpStatusCode status, string message, object? data, string technical = "")
            => new() { Status = status, Message = message, Data = data, MessageTechnical = technical };
    }
}
