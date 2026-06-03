namespace TCX.API.Common.Models
{
    public class LoggingElastic
    {
        public string HttpContext { get; set; }
        public string PosNo { get; set; }
        public string WebApi { get; set; }
        public string DeveloperMessage { get; set; }
        public long ResponseTime { get; set; }
        public int TotalBill { get; set; }
    }
}
