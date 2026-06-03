namespace VCM.POSBLUE.Model.Odoo
{
    public class OdooUpdateVCResponse
    {
        public string SERIALNUMBER { get; set; }
        public string STATUSCODE { get; set; }
        public string MATERIALNUMBER { get; set; }
        public double? REMAINAMOUNT { get; set; }
        public string STATUSMESSAGE { get; set; }
        public int STATUSMESSAGECODE { get; set; }
    }
}
