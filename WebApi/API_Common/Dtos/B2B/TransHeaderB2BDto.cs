using System;

namespace TCX.API.Common.Dtos
{
    public class TransHeaderB2BDto
    {
        public Guid ID { get; set; }
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string ProcessedStatus { get; set; }
        public string ProcessedStatusName { get; set; }
        public DateTime ProcessedDate { get; set; }
        public string Note { get; set; }
        public string DeliveryFrom { get; set; }
        public string DeliveryAddress { get; set; }
        public string DeliveryNote { get; set; }
        public DateTime DeliveryEstimatedDate { get; set; }
        public string MemberPhoneNumber { get; set; }
        public string MemberName { get; set; }
        public decimal PaymentAmount { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsVATInvoice { get; set; }
        public string VATCustomerName { get; set; }
        public string VATCompanyName { get; set; }
        public string VATTaxCode { get; set; }
        public string VATPhoneNumber { get; set; }
        public string VATEmail { get; set; }
        public string VATAddress { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
    }
}
