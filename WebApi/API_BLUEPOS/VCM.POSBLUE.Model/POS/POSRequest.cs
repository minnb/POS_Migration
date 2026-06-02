using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.POS
{
    public class POSRequest
    {
        [Required]
        [DefaultValue("201801")]
        [StringLength(6, MinimumLength = 6)]
        public string PosNo { get; set; } = "201801";
        public string StoreNo
        {
            get { return PosNo.Length >= 4 ? PosNo.Substring(0, 4) : PosNo; }
        }
    }
}
