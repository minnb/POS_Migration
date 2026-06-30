using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class CustomerVATModel
    {       
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string CreatedUser { get; set; }
        
    }
}
