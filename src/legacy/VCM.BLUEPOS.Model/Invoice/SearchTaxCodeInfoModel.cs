using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class SearchTaxCodeInfoModel
    {
        public string Id { get; set; }         // TaxCode
        public string Name { get; set; }       // CompanyName
        public string Address { get; set; }       
    }

    public class ResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    public class ApiPartnerTaxCodeInfoResponseModel
    {
        public ResponseModel Meta { get; set; }
        public SearchTaxCodeInfoModel Data { get; set; }
    }


}
