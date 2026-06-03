using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.WinScore
{
    public class CheckStatusWinscore
    {
        public int Is_qualified { get; set; }
        public string Display_message { get; set; }
    }
    public class UpdateStatusWinscore
    {
        public string win_membership { get; set; }
        public string pos_id { get; set; }
        public string store_id { get; set; }
        public string updated_status { get; set; }
    }

    public class UpdateStatusWinscorePOS
    {
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public int Status { get; set; }
    }
}
