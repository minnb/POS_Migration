using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPrice
{
    public class SaveItemPriceRequest
    {
        public string ID { get; set; }
        public string SalesType { get; set; }
        public string SalesCode { get; set; }
        public string ChannelCode { get; set; }
        public string RegionCode { get; set; }
        public string StoreNo { get; set; }
        public string Barcode { get; set; }
        public string ItemNo { get; set; }
        public string UOM { get; set; }
        public string UnitPrice { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
    public class SaveItemPriceModel
    {
        public int ID { get; set; }
        public string SalesType { get; set; }
        public string SalesCode { get; set; }
        
        public string ChannelCode { get; set; }
        public string RegionCode { get; set; }
        public string StoreNo { get; set; }
        public string Barcode { get; set; }
        public string ItemNo { get; set; }
        public string UOM { get; set; }
        public double? UnitPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ParamCallStoreProcedureRequest
    {
        public string Pkey { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public float UnitPrice { get; set; }
    }
    public class ResponseStoreProcedureRequest
    {
        public string Pkey { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public double UnitPrice { get; set; }
    }
}
