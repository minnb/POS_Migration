using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CrownX
{
    public class ClubResponse
    {
        public ClubCustomerIdentifier customerIdentifier { get; set; }
        public List<ClubCodeDetails> clubCodeDetails { get; set; }
    }
    public class ClubCustomerIdentifier
    {
        public string id { get; set; }
        public string idType { get; set; }
    }
    public class ClubCodeDetails
    {
        public string csn { get; set; }
        public string merchantId { get; set; }
        public string clubCode { get; set; }
    }
}
