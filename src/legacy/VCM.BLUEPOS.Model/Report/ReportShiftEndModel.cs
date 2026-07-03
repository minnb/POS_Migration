using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class ReportShiftEndModel
    {
        public string ShiftCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string SetDB { get; set; }
        public string StaffCode { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public double? CloseAmount { get; set; }
        public double? BeginAmount { get; set; }
        public double? AmountTendered { get; set; }
        public double? BlanceMoney { get; set; }
        public double? OutAmount { get; set; }
        public int? QuantityBox { get; set; }
        public int? QuantityCoupon { get; set; }
        public int? QuantityVoucher { get; set; }
        public string ShiftNumber { get; set; }
        public string IsShiftClosed { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<DateTime> CloseShiftDate { get; set; }
        public int? Total { get; set; }
        public string FormatBussinessDate { get { return string.Format("{0:dd/MM/yyyy}", BussinessDate); } }
        public string FormatCloseShiftDate
        {
            get
            {
                if (CloseShiftDate == null)
                    return "";
                return string.Format("{0:dd/MM/yyyy HH:mm:ss}", CloseShiftDate);
            }
        }

        public bool IsDeleteShiftCode
        {
            get
            {
                var checkDate = BussinessDate.Value.Date.Subtract(DateTime.Now.Date).TotalDays;//1
                if (checkDate >= 0)
                    return true;
                return false;
            }
        }
    }

    public class ReportShiftEndExcelModel
    {
        public string StoreNo { get; set; }
        public string StaffCode { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public string ShiftNumber { get; set; }
        public double? BeginAmount { get; set; }
        public double? CloseAmount { get; set; }
        public double? AmountTendered { get; set; }
        public double? BlanceMoney { get; set; } 
        public string IsShiftClosed { get; set; }
        public Nullable<DateTime> CloseShiftDate { get; set; }
    }
    public class ReportShiftEndExcelVMPModel
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public string ShiftNumber { get; set; }
        public double? BeginAmount { get; set; }
        public double? CloseAmount { get; set; }
        public double? AmountTendered { get; set; }
        public double? BlanceMoney { get; set; } 
        public string IsShiftClosed { get; set; }
        public Nullable<DateTime> CloseShiftDate { get; set; }
    }

    public class ShiftEndReportPLGResponseModel
    {
        public string ShiftCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string SetDB { get; set; }
        public string StaffCode { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public double? CloseAmount { get; set; }
        public double? BeginAmount { get; set; }
        public double? AmountTendered { get; set; }
        public double? BlanceMoney { get; set; }
        public double? OutAmount { get; set; }
        public int? QuantityBox { get; set; }
        public int? QuantityCoupon { get; set; }
        public int? QuantityVoucher { get; set; }
        public string ShiftNumber { get; set; }
        public string IsShiftClosed { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<DateTime> CloseShiftDate { get; set; }
        public int? Total { get; set; }
        public string FormatBussinessDate { get { return string.Format("{0:dd/MM/yyyy}", BussinessDate); } }
        public string FormatCloseShiftDate
        {
            get
            {
                if (CloseShiftDate == null)
                    return "";
                return string.Format("{0:dd/MM/yyyy HH:mm:ss}", CloseShiftDate);
            }
        }

        public bool IsDeleteShiftCode
        {
            get
            {
                var checkDate = BussinessDate.Value.Date.Subtract(DateTime.Now.Date).TotalDays;//1
                if (checkDate >= 0)
                    return true;
                return false;
            }
        }
    }

    public class ExportExcelShiftEndReportPLGModel
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public string ShiftNumber { get; set; }
        public double? BeginAmount { get; set; }
        public double? CloseAmount { get; set; }
        public double? AmountTendered { get; set; }
        public double? BlanceMoney { get; set; }
        public string IsShiftClosed { get; set; }
        public Nullable<DateTime> CloseShiftDate { get; set; }
    }







}
