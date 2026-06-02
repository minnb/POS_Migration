using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class GetOrderOfflineModel
    {
        public string MemberCardNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrderNo { get; set; }
        //public int PointRedeen { get; set; }
        //public int TransactionType { get; set; }
        //public string OrigOrderNo { get; set; }
        public double? AmountToEarn { get; set; }
        public double? OrderAmount { get; set; }

    }
}
