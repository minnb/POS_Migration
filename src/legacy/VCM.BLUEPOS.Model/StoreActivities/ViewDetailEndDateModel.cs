using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ViewDetailEndDateModel
    {
        public List<EndDateTenderTypeModel> lstTenderType { get; set; }
        public List<EndDateAmountVATModel> lstAmountVAT { get; set; }
        public List<EndDateRefundAmountVATModel> lstRefundAmountVAT { get; set; }
        public List<EndDateAmountNotVATModel> lstAmountNotVAT { get; set; }
        public List<EndDateRefundAmountNotVATModel> lstRefundAmountNotVAT { get; set; }
        public List<CommissionTypeModel> lstCommissionType { get; set; }
        public EndDateInfoStaff infoStaff { get; set; }
        public SaleVINID saleVINID { get; set; }
        public ReceivablesCXPoint receivablesCXPoint { get; set; }
        public PayooCosmo payooAmount { get; set; }
        public string Host { get; set; }

    }

    public class EndDateInfoStaff
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string BusinessDate { get; set; }
        public string Address { get; set; }
        public string PhoneNo { get; set; }

    }
    public class EndDateTenderTypeModel
    {
        public string StoreNo { get; set; }
        public string Code { get; set; }
        public string TenderTypeName { get; set; }
        public double? AmountTendered { get; set; }
        public int? SL_TenderType { get; set; }

    }
    public class EndDateAmountVATModel
    {
        public string StoreNo { get; set; }
        public double? AmountTenderedVAT { get; set; }
        public int? CountOrderNoVAT { get; set; }
        public int? CountCustomerVAT { get; set; }
    }
    public class EndDateRefundAmountVATModel
    {
        public string StoreNo { get; set; }
        public double? RefundAmountTenderedVAT { get; set; }
        public int? RefundCountOrderNoVAT { get; set; }
        public int? RefundCountCustomerVAT { get; set; }
    }
    public class EndDateAmountNotVATModel
    {
        public string StoreNo { get; set; }
        public double? AmountTenderedNotVAT { get; set; }
        public int? CountOrderNoNotVAT { get; set; }
        public int? CountCustomerNotVAT { get; set; }
    }
    public class EndDateRefundAmountNotVATModel
    {
        public string StoreNo { get; set; }
        public double? RefundAmountTenderedNotVAT { get; set; }
        public int? RefundCountOrderNoNotVAT { get; set; }
        public int? RefundCountCustomerNotVAT { get; set; }
    }

    public class CommissionTypeModel
    {
        //public string StoreNo { get; set; }
        //public string Code { get; set; }
        public string TenderTypeName { get; set; }
        public double? CommissionAmount { get; set; }
    }

}
