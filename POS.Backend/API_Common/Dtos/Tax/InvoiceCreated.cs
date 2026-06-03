using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Tax
{
    public class InvoiceCreatedRequest
    {
        [Required]
        public string SiteNo { get; set; }
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
        public string Email { get; set; }
        public string Address { get; set; }
        [Required]
        public string[] OrderNo { get; set; }
        public string Passport { get; set; }
        public string CCCD { get; set; }
        public string DVQHNS { get; set; }
    }
    public class InvoiceCreated
    {
        public string SiteNo { get; set; }       
        public string OrderNo { get; set; }
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public bool IsNotVAT { get; set; }
        public string IsNotVATPartner { get; set; }
        public string Id { get; set; }
        public string Passport { get; set; }
        public string CCCD { get; set; }
        public string DVQHNS { get; set; }
    }
}
