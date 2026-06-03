using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.WinCustomer
{
    public class WinPayAccumulationRequest
    {
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string RefCode { get; set; }
        public int Amount { get; set; }
        public string OperationId { get; set; }
    }
    public class WinPayAccumulationDto
    {
        [Required]
        public string OrderNo { get; set; }
        [Required]
        public string StoreNo { get; set; }
        [Required]
        public string PosNo { get; set; }
        [Required]
        public DateTime  OrderDate { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string RefCode { get; set; }
        [Required]
        public string TenderType { get; set; }
        public int TotalAmount { get; set; }
        public int PaymentAmount { get; set; }
        public string OperationId { get; set; }
        public bool IsCheckItem { get; set; }
    }

    public class WinPayAccumulationData: WinPayAccumulationDto
    {
        public int Amount { get; set; }
        public string Condition { get; set; }
        public bool IsSync { get; set; }
        public string Day { get; set; }
        public string Month { get; set; }
        public string TraceId { get; set; }
        public string Response { get; set; }
        public string Version { get; set; }
        public DateTime CrtDate { get; set; }
    }
    public class ConditionAccumulation
    {
        public string Condition { get; set; }
        public int Priority { get; set; }
        public bool IsCheckAmount { get; set; }
        public bool IsCheckItem { get; set; }
        public bool IsDay { get; set; }
        public bool IsMonth { get; set; }
        public int Amount { get; set; }
        public int Qty { get; set; }
        public string AccumulationType { get; set; }
        public decimal AccumulationValue { get; set; }
        public string Description { get; set; }
        public string DayOfWeek { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool Blocked { get; set; }
        public DateTime CrtDate { get; set; }
        public DateTime ChgDate { get; set; }
        public List<ConditionAccumulationStore> ConditionStore { get; set; }
    }

    public class ConditionAccumulationStore
    {
        public string Condition { get; set; }
        public string StoreNo { get; set; }
        public bool Blocked { get; set; }
        public DateTime CrtDate { get; set; }
    }
    public class DayOfWeekData
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
    }
    public class CountDataAccumulation
    {
        public string PhoneNumber { get; set; }
        public string Condition { get; set; }
        public int Qty { get; set; }
    }
}
