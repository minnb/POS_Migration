using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.OptionModel;

namespace VCM.BLUEPOS.Model.Report
{
    public class CumulativeSalesResponseModel
    {
        public string StoreNo { get; set; }
        public string SelectedDate { get; set; }
        public string OnHourForSelected { get; set; }
        public string ComparisonDate { get; set; }
        public string OnHourForComparison { get; set; }
        public string OnHour { get; set; }

        //------------------------------------------------------------
        public Nullable<double> TotalSalesBySelectedDate { get; set; }
        //public Nullable<double> TotalAmountIncVATSelectedDate { get; set; }        
        public Nullable<double> TotalAmountNETSelectedDate { get; set; }
        public Nullable<double> CumulativeSalesBySelectedDate { get; set; }
        public Nullable<double> PercentBySelectedDate { get; set; }
        
        //------------------------------------------------------------
        public Nullable<double> TotalSalesDineInSelectedDate { get; set; }
        public Nullable<double> PercentTotalSalesDineInSelectedDate { get; set; }
        public Nullable<double> TotalSalesDeliverySelectedDate { get; set; } 
        public double? PercentTotalSalesDeliverySelectedDate { get; set; }
        //------------------------------------------------------------
        public Nullable<double> TotalSalesByComparisonDate { get; set; }
        //public Nullable<double> TotalAmountIncVATComparisonDate { get; set; }
        public Nullable<double> TotalAmountNETComparisonDate { get; set; }
        public Nullable<double> CumulativeSalesByComparisonDate { get; set; }
        public Nullable<double> PercentByComparisonDate { get; set; }
        //------------------------------------------------------------
        public Nullable<double> TotalSalesDineInComparisonDate { get; set; }
        public Nullable<double> PercentTotalSalsesDineInComparisonDate { get; set; }

        public Nullable<double> TotalSalesDeliveryComparisonDate { get; set; }
        public Nullable<double> PercentTotalSalesDeliveryComparisonDate { get; set; }
        //------------------------------------------------------------
        public int Total { get; set; }

    }

    public class ExportExcel_CumulativeSalesResponseModel
    {
        public string SelectedDate { get; set; }
        public string OnHourForSelected { get; set; }
        public string ComparisonDate { get; set; }
        public string OnHourForComparison { get; set; }
        public string EndingTime { get; set; }

        //------------------------------------------------------------
        public Nullable<double> TotalSalesBySelectedDate { get; set; }
        //public Nullable<double> TotalAmountIncVATSelectedDate { get; set; }
        
        public Nullable<double> TotalAmountNETSelectedDate { get; set; }
        public Nullable<double> CumulativeSalesBySelectedDate { get; set; }
        public Nullable<double> PercentBySelectedDate { get; set; }

        //------------------------------------------------------------
        public Nullable<double> TotalSalesDineInSelectedDate { get; set; }
        public Nullable<double> PercentTotalSalesDineInSelectedDate { get; set; }
        public Nullable<double> TotalSalesDeliverySelectedDate { get; set; }
        public Nullable<double> PercentTotalSalesDeliverySelectedDate { get; set; }
        //------------------------------------------------------------
        public Nullable<double> TotalSalesByComparisonDate { get; set; }
        //public Nullable<double> TotalAmountIncVATComparisonDate { get; set; }
        public Nullable<double> TotalAmountNETComparisonDate { get; set; }
        public Nullable<double> CumulativeSalesByComparisonDate { get; set; }
        public Nullable<double> PercentByComparisonDate { get; set; }
        //------------------------------------------------------------
        public Nullable<double> TotalSalesDineInComparisonDate { get; set; }
        public Nullable<double> PercentTotalSalsesDineInComparisonDate { get; set; }
        public Nullable<double> TotalSalesDeliveryComparisonDate { get; set; }
        public Nullable<double> PercentTotalSalesDeliveryComparisonDate { get; set; }
        
    }

    public class ExportExcel_CumulativeSalesGroupHeaderModel
    {
        public string SelectedDateStr { get; set; }
        public string ComparisonDateStr { get; set; }
        //------------------------------------------------------------
        public Nullable<double> TotalSalesBySelectedDate { get; set; }
        public Nullable<double> CumulativeSalesBySelectedDateStr { get; set; }
        public Nullable<double> PercentBySelectedDateStr { get; set; }

        public Nullable<double> TotalSalesDineInSelectedDateStr { get; set; }
        public Nullable<double> PercentTotalSalesDineInSelectedDateStr { get; set; }
        public Nullable<double> TotalSalesDeliverySelectedDateStr { get; set; }
        public Nullable<double> PercentTotalSalesDeliverySelectedDateStr { get; set; }

        //------------------------------------------------------------
        public Nullable<double> TotalSalesByComparisonDate { get; set; }
        public Nullable<double> CumulativeSalesByComparisonDateStr { get; set; }
        public Nullable<double> PercentByComparisonDateStr { get; set; }
       
        public Nullable<double> TotalSalesDineInComparisonDateStr { get; set; }
        public Nullable<double> PercentTotalSalsesDineInComparisonDateStr { get; set; }
        public Nullable<double> TotalSalesDeliveryComparisonDateStr { get; set; }
        public Nullable<double> PercentTotalSalesDeliveryComparisonDateStr { get; set; }

    }




}
