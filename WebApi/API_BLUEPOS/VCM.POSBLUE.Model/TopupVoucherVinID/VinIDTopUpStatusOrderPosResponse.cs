using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDTopUpStatusOrderPosResponse
    {
        public string StatusOrder { get; set; }//SUCCESS | ERROR
        public string TypeOrder { get; set; }//TXN_SALE | TXN_EARN | TXN_BURN | TXN_REFUND
    }
}
