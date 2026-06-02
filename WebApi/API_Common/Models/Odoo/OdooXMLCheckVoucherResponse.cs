namespace VCM.POSBLUE.Model.Odoo
{
    public class OdooXMLCheckVoucherResponse
    {
        public CheckResponse RESPONSE { get; set; }
    }
    public class CheckResponse
    {
        public string SERIALNUMBER { get; set; }
        public string FROMDATE { get; set; }
        public string TODATE { get; set; }
        public string VALUE { get; set; }
        public string REMAINAMOUNT { get; set; }
        public string STATUSCODE { get; set; }
        public string MATERIALNUMBER { get; set; }
        public string SALESDATE { get; set; }
        public string SALESTRANS { get; set; }
        public string REDEEMTRANS { get; set; }
        public string REDEEMDATE { get; set; }
        public string REDEEMPLANT { get; set; }
        public string ISEMPLOYEE { get; set; }
        public string ISVOUCHER { get; set; }
        public string STATUSMESSAGE { get; set; }
        public int STATUSMESSAGECODE { get; set; }
    }
}
