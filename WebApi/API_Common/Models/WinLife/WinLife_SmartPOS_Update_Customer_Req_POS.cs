using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.WinLife
{

    public class WinLife_SmartPOS_Update_Customer_Req_CX
    {
        public string fullName { get; set; }//
        public string phoneNo { get; set; }
        public string gender { get; set; }//F,M
        public string title { get; set; }
        public string dob { get; set; }
    }
}
