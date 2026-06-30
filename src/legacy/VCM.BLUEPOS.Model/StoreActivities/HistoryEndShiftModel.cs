using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ShiftHeaderResponseModel
    {
        public string ShiftCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string Placement { get; set; }
        public string StaffCode { get; set; }
        public string Source { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public double? CloseAmount { get; set; }
        public double? BeginAmount { get; set; }
        public double? OutAmount { get; set; }
        public int? QuantityBox { get; set; }
        public int? QuantityCoupon { get; set; }
        public int? QuantityVoucher { get; set; }
        public string ShiftNumber { get; set; }
        public bool? IsShiftClosed { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<DateTime> CloseShiftDate { get; set; }        
        public bool? IsShiftOpened { get; set; }
        public Nullable<DateTime> OpenShiftDate { get; set; }
        public string UpdatedUser { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public Nullable<DateTime> UpdatedDate { get; set; }
    }

    public class HistoryEndShiftModel
    {
        public string ShiftCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string Placement { get; set; }
        public string StaffCode { get; set; }
        public string Source { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public double? CloseAmount { get; set; }
        public double? OutAmount { get; set; }
        public double? AmountCA00 { get; set; }   // Phuc Long : tien mat = CA00
        public double? ChenhLech
        {
            get
            {
                return CloseAmount - AmountCA00;
            }
        }
        public int? QuantityBox { get; set; }
        public int? QuantityCoupon { get; set; }
        public int? QuantityVoucher { get; set; }
        public string ShiftNumber { get; set; }
        public bool? IsShiftClosed { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<DateTime> CloseShiftDate { get; set; }
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
        public string RoleUser { get; set; }

    }



}
