using System.Collections.Generic;

namespace TCX.API.Common.Dtos.Capillary.Coupons
{
    public class RedeemCouponResponseCap
    {
        public List<ResultRedeemCouponResponseCap> Redemption { get; set; }
        //public int TotalCount { get; set; }
        //public int FailureCount { get; set; }
    }
    public class DataRedeemCouponResponseCap
    {
        public long EntityId { get; set; }
        public ResultRedeemCouponResponseCap Result { get; set; }
        public List<object> Errors { get; set; }
    }
    public class ResultRedeemCouponResponseCap
    {
        public string RedemptionId { get; set; }
        public long Id { get; set; }
        public bool CurrencyInput { get; set; }
        public int LocalToBaseCurrencyExchangeRate { get; set; }
        public List<object> Warnings { get; set; }
        public string AppendedErrorMessage { get; set; }
        public string Code { get; set; }
        public string DiscountCode { get; set; }
        public int SeriesCode { get; set; }
        public bool IsAbsolute { get; set; }
        public decimal CouponValue { get; set; }
        public RedemptionStatusCouponCapillary RedemptionStatus { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public int DiscountUpto { get; set; }
    }
    public class StatusCodeCouponCapillary
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
    }
    public class RedemptionStatusCouponCapillary
    {
        public StatusCodeCouponCapillary StatusCode {  get; set; }
        public List<object> Warnings { get; set; }
        public List<object> WarningsAsStatusCode { get; set; }
        public string Message { get; set; }
        public int Code { get; set; }
        public bool Success { get; set; }
    }
}
