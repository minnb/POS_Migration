using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.WinLife
{
    public class WinLife_UpdatePromotions_CX_Response
    {
        public string developerMessage { get; set; }
        public string message { get; set; }
        public object errors { get; set; }
        public object data { get; set; }
        public object paging { get; set; }
    }
    public class WinLife_UpdatePromotion_Response
    {
        public WinLife_UpdatePromotions_CX_Response data { get; set; }
    }
}
