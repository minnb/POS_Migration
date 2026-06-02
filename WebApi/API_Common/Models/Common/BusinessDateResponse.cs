using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class BusinessDateResponse
    {
        public DateTime? BussinessDate { get; set; }
        public DateTime? CurrentDate { get; set; }
        public int Status { get; set; }
        public string Message { get; set; }

    }
}
