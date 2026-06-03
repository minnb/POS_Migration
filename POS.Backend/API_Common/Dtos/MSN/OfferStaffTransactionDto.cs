using System;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos
{
    public class OfferStaffTransactionRequest
    {
        [Required]
        public string ClubCode { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string StaffCode { get; set; }
        public decimal Amount { get; set; }
        [Required]
        public int TransactionType { get; set; }
        public string ReturnedOrderNo { get; set; }
    }
    public class OfferStaffTransactionDto
    {
        public string ClubCode { get; set; }
        public string Month { get; set; }
        public decimal InitQuota { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string OrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string PhoneNumber { get; set; }
        public string StaffCode { get; set; }
        public decimal Amount { get; set; }
        [ValidateValueRange(1, 2, 3, ErrorMessage = "Type must be 1, 2, or 3")]
        public int TransactionType { get; set; }
        public string ReturnedOrderNo { get; set; }
        public DateTime CrtDate { get; set; }
    }
}
