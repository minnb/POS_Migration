namespace TCX.API.Common.Models
{
    public class ResponseCrownX
    {
        public string code { get; set; }//E999: Mất kết nối với server, vui lòng thử lại sau ít phút,E001: {0} không thể để trống,E002: {0} không đúng định dạng,E003: Khách hàng không tồn tại trong hệ thống
        public string message { get; set; }
        public string developerMessage { get; set; }
        public object data { get; set; }
    }

    public class ResponseCXRevert
    {
        public RevertResponse data { get; set; }
    }
    public class RevertResponse
    {
        public string referenceNumber { get; set; }//Mã tham chiếu GD trong OLS
        public double extraEarnByCampaign { get; set; }//Điểm điều chỉnh
        public int currencyRate { get; set; }//Tỷ lệ quy đổi điểm
    }
    public class RevertPOSResponse
    { 
        public double extraEarnByCampaign { get; set; }//Điểm điều chỉnh
        public int currencyRate { get; set; }//Tỷ lệ quy đổi điểm
    }

    public class ResponseUpdStt
    {
        public ResponseUpdSttCode data { get; set; }
    }
    public class ResponseUpdSttCode
    {
        public int errorCode { get; set; }
        public string message { get; set; }
    }
}
