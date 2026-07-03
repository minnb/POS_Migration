using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ViewDetailEndShiftPLGModel
    {
        public List<TenderTypeModel> lstTenderType { get; set; }
        public List<AmountVATModel> lstAmountVAT { get; set; }
        public List<AmountNotVATModel> lstAmountNotVAT { get; set; }
        public List<RefundAmountVATModel> lstRefundAmountVAT { get; set; }
        public List<RefundAmountNotVATModel> lstRefundAmountNotVAT { get; set; }
        public List<MoneyCashModel> lstMoneyCash { get; set; }
        public InfoHeaderPLG infoHeaderPLG { get; set; }
        public InfoStaff infoStaff { get; set; }
        public SaleVINID saleVINID { get; set; }
        public ReceivablesCXPoint receivablesCXPoint { get; set; }
        public PayooCosmo payooAmount { get; set; }
        public string Host { get; set; }

    }
    
}
