using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Dtos.ROP
{
    public class ResponseAppWincare
    {
        public int ResponseCode { get; set; }
        public object Data { get; set; }
        public string[] TechnicalMessage { get; set; }
        public string Message { get; set; }
    }
    public class ResponseROP
    {
        public int ResponseCode { get; set; }
        public object Data { get; set; }
        public string[] TechnicalMessage { get; set; }
        public string Message { get; set; }
    }
    public class ResponseDataStringROP
    {
        public int ResponseCode { get; set; }
        public string Data { get; set; }
        public string[] TechnicalMessage { get; set; }
        public string Message { get; set; }
    }
    public class TokenData
    {
        public string Token { get; set; }
        public string TokenType { get; set; }
    }
    public class DataResponseROP
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
