using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty
{
    public class WinLife_SmartPOS_Update_Customer_Req_POS
    {
        public string fullName { get; set; }//
        public string phoneNo { get; set; }
        public string gender { get; set; }//F,M
        public string title { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string dob { get; set; }
        public string email { get; set; }
        public string address { get; set; }
        public string province { get; set; }
        public string district { get; set; }
        public string city { get; set; }
        public string ward { get; set; }
    }
}
