using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class SaleBusinessStoreModel
    {
        public string StoreNo { get; set; }
        public string TerminalID { get; set; }
        public string Status { get; set; }
        public string BussinessDate { get; set; }
        public bool? IsClosed { get; set; }
        public string Placement { get; set; }
        public DateTime? CloseDate { get; set; }
        public DateTime? LastOrderTime { get; set; }
        public double? AmountTotal { get; set; }
        public double? CashMoney { get; set; }
        public double? AmountBank { get; set; }
        public int? CountCustomer { get; set; }
        public int? CountOrderNo { get; set; }
        public int TotalRow { get; set; }
        public bool? IsConfirm { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string FormatIsClosed
        {
            get
            {
                if (IsClosed != null)
                {
                    if (IsClosed == true)
                        return "Đã đóng ngày";
                    else
                    {
                        return "Chưa đóng ngày";
                    }
                }
                return "-";
            }
        }
        public string FormatCreatedDate
        {
            get
            {
                if (CreatedDate != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", CreatedDate);
                return "-";
            }
        }
        public string FormatCloseDate
        {
            get
            {
                if (CloseDate != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", CloseDate);
                return "-";
            }
        }
        public string FormatUpdatedDate
        {
            get
            {
                if (UpdatedDate != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", UpdatedDate);
                return "-";
            }
        }
        public string FormatLastOrderTime
        {
            get
            {
                if (LastOrderTime != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", LastOrderTime);
                return "-";
            }
        }
        public string FormatAmountTotal
        {
            get
            {
                if (AmountTotal != null)
                    return string.Format("{0:###,###}", AmountTotal);
                return "-";
            }
        }
        public string FormatCashMoney
        {
            get
            {
                if (CashMoney != null)
                    return string.Format("{0:###,###}", CashMoney);
                return "-";
            }
        }
        public string FormatCountCustomer
        {
            get
            {
                if (CountCustomer != null)
                    return "" + CountCustomer;
                return "-";
            }
        }
        public string FormatCountOrderNo
        {
            get
            {
                if (CountOrderNo != null)
                    return "" + CountOrderNo;
                return "-";
            }
        }
    }

    public class HistoryConfirmEndDatePOSModel
    {
        public string StoreNo { get; set; }
        public string TerminalID { get; set; }
        public string DocumentType { get; set; }       // Loại POS
        public string Status { get; set; }
        public string BussinessDate { get; set; }
        public bool? IsClosed { get; set; }
        public DateTime? CloseDate { get; set; }
        public DateTime? LastOrderTime { get; set; }
        public double? AmountTotal { get; set; }
        public double? CashMoney { get; set; }
        public double? AmountBank { get; set; }
        public int? CountCustomer { get; set; }
        public int? CountOrderNo { get; set; }
        public int TotalRow { get; set; }
        public bool? IsConfirm { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string FormatIsClosed   // Trang thai
        {
            get
            {
                if (IsClosed != null)
                {
                    if (IsClosed == true)
                        return "Đã đóng ngày";
                    else
                    {
                        return "Chưa đóng ngày";
                    }
                }
                return "-";
            }
        }
        public string FormatCreatedDate      // Thoi gian mo ngay
        {
            get
            {
                if (CreatedDate != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", CreatedDate);
                return "-";
            }
        }
        public string FormatCloseDate        // Thoi gian ket thuc ngay
        {
            get
            {
                if (CloseDate != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", CloseDate);
                return "-";
            }
        }

        public string FormatUpdatedDate
        {
            get
            {
                if (UpdatedDate != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", UpdatedDate);
                return "-";
            }
        }
        public string FormatLastOrderTime
        {
            get
            {
                if (LastOrderTime != null)
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", LastOrderTime);
                return "-";
            }
        }      
        public string FormatAmountTotal  // Tien ban ra
        {
            get
            {
                if (AmountTotal != null)
                    return string.Format("{0:###,###}", AmountTotal);
                return "-";
            }
        }
        public string FormatCashMoney  // Tien mat
        {
            get
            {
                if (CashMoney != null)
                    return string.Format("{0:###,###}", CashMoney);
                return "-";
            }
        }
        public string FormatCountCustomer  // So luong KH
        {
            get
            {
                if (CountCustomer != null)
                    return "" + CountCustomer;
                return "-";
            }
        }
        public string FormatCountOrderNo
        {
            get
            {
                if (CountOrderNo != null)
                    return "" + CountOrderNo;
                return "-";
            }
        }
       
    }


}
