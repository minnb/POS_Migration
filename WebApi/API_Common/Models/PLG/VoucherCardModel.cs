using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
   public class VoucherCardModel
    {
        public long ID { get; set; }
        public string CardType { get; set; }
        public string SeriNo { get; set; }
        public string CardNo { get; set; }
        public Nullable<System.DateTime> ValidFrom { get; set; }
        public Nullable<System.DateTime> ValidTo { get; set; }
        public Nullable<int> ValidDays { get; set; }
        public Nullable<double> Value { get; set; }
        public Nullable<double> RemainValue { get; set; }
        public string Status { get; set; }
        public Nullable<bool> IsReset { get; set; }
        public string ResetTime { get; set; }
        public Nullable<System.DateTime> ActivateDate { get; set; }
        public string LastUsedStore { get; set; }
        public string LastUsedPosID { get; set; }
        public Nullable<System.DateTime> LastUsedDate { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string UserName { get; set; }
    }
}
