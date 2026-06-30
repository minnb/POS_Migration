using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class RevenueOrderSalesByStoreModel
    {
        public int ID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string StyleProfile { get; set; }
        public string SetDB { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public DateTime? BussinessDate { get; set; }
        public int? CountOrderTotal { get; set; }
        public double? TruocVAT { get; set; }
        public double? SauVAT { get; set; }
        public double? ThueVAT { get; set; }
        public double? SumTruocVAT { get; set; }
        public double? SumSauVAT { get; set; }
        public double? SumVAT { get; set; }
        public int? Total { get; set; }
        public double? SumTotalAmount { get; set; }
        public double? SumTotalOrder { get; set; }
    }

    public class ExportPDFRevenueOrderSalesByStoreModel
    {
        public List<ExportPDFRevenueOrderSalesByStoreModel> lstSaleStore { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string TransDate { get; set; }
    }

    public class ExportExcelRevenueOrderSalesByStoreModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public DateTime? BussinessDate { get; set; }
        public int? CountOrderTotal { get; set; }
        public double? TotalTruocVAT { get; set; }
        public double? TotalVAT { get; set; }
        public double? AmountTotal { get; set; }
    }

    //--- WinLife ---

    public class RevenueOrderSalesByStoreWinLifeModel
    { 
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public DateTime? BussinessDate { get; set; }
        public int CountOrderTotal { get; set; }
        public Nullable<decimal> TruocVAT { get; set; }
        public Nullable<decimal> SauVAT { get; set; }
        public Nullable<decimal> ThueVAT { get; set; }
        public Nullable<double> SumTruocVAT { get; set; }
        public Nullable<double> SumSauVAT { get; set; }
        public Nullable<double> SumVAT { get; set; }        
        public Nullable<double> SumTotalAmount { get; set; }
        public Nullable<double> SumTotalOrder { get; set; }
        public int Total { get; set; }
    }

    public class ExportPDFRevenueOrderSalesByStoreWinLifeModel
    {
        public List<ExportPDFRevenueOrderSalesByStoreWinLifeModel> lstSaleStore { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string TransDate { get; set; }
    }

    public class ExportExcel_RevenueOrderSalesByStoreWinLifeModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public DateTime? BussinessDate { get; set; }
        public int? CountOrderTotal { get; set; }
        public Nullable<decimal> TotalTruocVAT { get; set; }
        public Nullable<decimal> TotalVAT { get; set; }
        public Nullable<decimal> AmountTotal { get; set; }
    }







}
