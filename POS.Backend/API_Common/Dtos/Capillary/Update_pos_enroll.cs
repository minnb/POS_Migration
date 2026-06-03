using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary
{
    public class Update_pos_enroll
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Mobile_enroll { get; set; }
    }
}
