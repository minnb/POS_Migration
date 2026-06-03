using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Attribute;

namespace TCX.API.Common.Dtos.Loyalty.MemberBusiness
{
    public class ValidateMemberBusiness
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string MemberCard { get; set; }
        [Required]
        [Base64Validation]
        public string Key { get; set; }
    }
}
