using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.WinLife
{
    public class WinLife_Register_POS_Response
    {
        public string fullName { get; set; }//
        public string phoneNo { get; set; }
        public string gender { get; set; }//F,M
        public string csn { get; set; }
    }

    public class WinLife_Register_CX_Response
    {
        public string fullName { get; set; }//
        public string phoneNo { get; set; }
        public string gender { get; set; }//F,M
        public string csn { get; set; }
    }

    public class WinLife_Register_Response
    {
        public string developerMessage { get; set; }
        public string message { get; set; }
        public string code { get; set; }
        public WinLife_Register_CX_Response data { get; set; }
        public List<ErrorResponse> errors { get; set; }
    }

    public class ErrorResponse
    {
        public string field { get; set; }
        public string message { get; set; }
    }
}
