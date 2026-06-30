using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class RevenueSalesByHourlyModel
    {
        public string StoreNo { get; set; }
        public string ForDate { get; set; }
        public string OrderNo { get; set; }
        public string SalesType { get; set; }
        public string OnHourStr { get; set; }
        string OnHour { get; set; }
        public double? AmountVAT { get; set; }          // Tien thue VAT
        public double? TotalAmountNET { get; set; }     // Doanh thu truoc thue
        public double? TotalAmountIncVAT { get; set; }  // Doanh thu sau thue
        public int? TotalBill { get; set; }             // Tổng đơn hàng theo salestype
        public double? AtStore { get; set; }
        public double? GrabFood { get; set; }
        public double? Gojeck { get; set; }
        public double? Beamin { get; set; }
        public double? BeFood { get; set; }
        public double? NowFood { get; set; }
        public double? WebSite { get; set; }
        public double? RDAmount { get; set; }
        public double? QAAmount { get; set; }
        public double? EduAmount { get; set; }
        public double? DeliveryAmount { get; set; }
        public double? CallCenterAmount { get; set; }
        public int? SL_AtStore { get; set; }
        public int? SL_GrabFood { get; set; }
        public int? SL_Gojeck { get; set; }
        public int? SL_Beamin { get; set; }
        public int? SL_BeFood { get; set; }
        public int? SL_NowFood { get; set; }
        public int? SL_WebSite { get; set; }
        public int? SL_RDAmount { get; set; }
        public int? SL_QAAmount { get; set; }
        public int? SL_EduAmount { get; set; }
        public int? SL_DeliveryAmount { get; set; }
        public int? SL_CallCenterAmount { get; set; }
        public int Total { get; set; }

    }

    public class ExportExcelRevenueSalesByHourlyModel
    {
        public string StoreNo { get; set; }
        public string ForDate { get; set; }
        public string OnHourStr { get; set; }
        public double? TotalAmountNET { get; set; }
        public double? AmountVAT { get; set; }
        public double? TotalAmountIncVAT { get; set; }  // Doanh thu sau thue
        public int? TotalBill { get; set; }             // Tổng đơn hàng
        public double? AtStore { get; set; }
        public int? SL_AtStore { get; set; }
        public double? DeliveryAmount { get; set; }
        public int? SL_DeliveryAmount { get; set; }
        public double? NowFood { get; set; }
        public int? SL_NowFood { get; set; }
        public double? GrabFood { get; set; }
        public int? SL_GrabFood { get; set; }
        public double? BeFood { get; set; }
        public int? SL_BeFood { get; set; }
        public double? Beamin { get; set; }
        public int? SL_Beamin { get; set; }
        public double? Gojeck { get; set; }
        public int? SL_Gojeck { get; set; }
        public double? WebSite { get; set; }
        public int? SL_WebSite { get; set; }
        public double? CallCenterAmount { get; set; }
        public int? SL_CallCenterAmount { get; set; }
        public double? RDAmount { get; set; }
        public int? SL_RDAmount { get; set; }
        public double? QAAmount { get; set; }
        public int? SL_QAAmount { get; set; }
        public double? EduAmount { get; set; }
        public int? SL_EduAmount { get; set; }     
       
    }


    public class ExcelRevenueSalesByHourlyModel
    {
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string ListStoreNo { get; set; }
        public string SalesType { get; set; }

    }





}
