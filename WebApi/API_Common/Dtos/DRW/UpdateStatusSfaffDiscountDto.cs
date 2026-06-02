using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Dtos.DRW
{
    public class UpdateStatusSfaffDiscountDto
    {
        public string PosNo { get; set; }
        public string StaffPhone { get; set; }
        public string OrderNo { get; set; }
        public long Id { get; set; }
    }
}
