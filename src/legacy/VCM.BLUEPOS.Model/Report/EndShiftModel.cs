using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class HistoryEndDateModel
    {
        public int? TotalRow { get; set; }
        public string StoreNo { get; set; }
        public Nullable<DateTime> BusinessDate { get; set; }
        public string SetDB { get; set; }
        public string StoreName { get; set; }
        public double? AmountTotal { get; set; }
        public double? CashMoney { get; set; }
        public double? AmountBank { get; set; }
        public bool? IsConfirm { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? ConfirmDate { get; set; }
        public string FormatBusinessDate { get { return string.Format("{0:dd/MM/yyyy}", BusinessDate); } }
        public string FormatConfirmDate
        {
            get
            {
                if (ConfirmDate == null) return "";
                else
                    return string.Format("{0:dd/MM/yyyy HH:mm:ss}", ConfirmDate);
            }
        }
    }



}
