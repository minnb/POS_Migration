using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CXVoucher
{
    public class GenerateOTPResponse
    {
        public string errorCode { get; set; }
        public string errorMessage { get; set; }
        public object data { get; set; }
    }   

}
