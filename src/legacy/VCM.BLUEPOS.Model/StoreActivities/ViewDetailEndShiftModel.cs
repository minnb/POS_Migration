using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ViewDetailEndShiftModel
    {
        public List<TenderTypeModel> lstTenderType { get; set; }
        public List<AmountVATModel> lstAmountVAT { get; set; }
        public List<AmountNotVATModel> lstAmountNotVAT { get; set; }
        public List<MoneyCashModel> lstMoneyCash { get; set; }
        public List<InfoShift> lstInfoShift { get; set; }
        public InfoStaff infoStaff { get; set; }
        public List<RefundAmountVATModel> lstRefundAmountVAT { get; set; }
        public List<RefundAmountNotVATModel> lstRefundAmountNotVAT { get; set; }
        public SaleVINID saleVINID { get; set; }

    }
    public class ViewDetailEndShiftVMPModel
    {
        public List<TenderTypeModel> lstTenderType { get; set; }
        public List<AmountVATModel> lstAmountVAT { get; set; }
        public List<AmountNotVATModel> lstAmountNotVAT { get; set; }
        public List<RefundAmountVATModel> lstRefundAmountVAT { get; set; }
        public List<RefundAmountNotVATModel> lstRefundAmountNotVAT { get; set; }
        public List<MoneyCashModel> lstMoneyCash { get; set; }
        public InfoHeaderVMP infoHeaderVMP { get; set; }
        public InfoStaff infoStaff { get; set; }
        public SaleVINID saleVINID { get; set; }

    }
    public class InfoHeaderPLG
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string StoreName { get; set; }
        public DateTime? BusinessDate { get; set; }
        public DateTime? CloseShiftDate { get; set; }
        public string ShiftCode { get; set; }
        public double? BeginAmount { get; set; }
        public double? OutAmount { get; set; }
        public int? QuantityCoupon { get; set; }
        public int? QuantityVoucher { get; set; }

    }

    public class InfoHeaderVMP
    {
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public string StoreName { get; set; }
        public DateTime? BusinessDate { get; set; }
        public DateTime? CloseShiftDate { get; set; }
        public string ShiftCode { get; set; }
        public double? BeginAmount { get; set; }
        public double? OutAmount { get; set; }
        public int? QuantityCoupon { get; set; }
        public int? QuantityVoucher { get; set; }

    }

    public class InfoStaff
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
    }
    public class TenderTypeModel
    {
        public string StoreNo { get; set; }
        public string Code { get; set; }
        public string TenderTypeName { get; set; }
        public double? AmountTendered { get; set; }

    }
    public class AmountVATModel
    {
        public string StoreNo { get; set; }
        public double? AmountTenderedVAT { get; set; }
        public int? CountOrderNoVAT { get; set; }
        public int? CountCustomerVAT { get; set; }
    }
    public class AmountNotVATModel
    {
        public string StoreNo { get; set; }
        public double? AmountTenderedNotVAT { get; set; }
        public int? CountOrderNoNotVAT { get; set; }
        public int? CountCustomerNotVAT { get; set; }
    }
    public class MoneyCashModel
    {
        public string StoreNo { get; set; }
        public string ShiftNumber { get; set; }
        public string CashCode { get; set; }
        public int? CashQuantity { get; set; }
        public double? CashAmount { get; set; }
    }
    public class InfoShift
    {
        public string StoreNo { get; set; }
        public string ShiftCode { get; set; }
        public string StaffCode { get; set; }
        public string LastName { get; set; }
        public string StoreName { get; set; }
        public DateTime? BussinessDate { get; set; }
        public DateTime? CloseShiftDate { get; set; }
        public double? BeginAmount { get; set; }
        public double? OutAmount { get; set; }
        public int? QuantityCoupon { get; set; }
        public int? QuantityVoucher { get; set; }

    }
    public class RefundAmountVATModel
    {
        public string StoreNo { get; set; }
        public double? RefundAmountTenderedVAT { get; set; }
        public int? RefundCountOrderNoVAT { get; set; }
        public int? RefundCountCustomerVAT { get; set; }
    }
    public class RefundAmountNotVATModel
    {
        public string StoreNo { get; set; }
        public double? RefundAmountTenderedNotVAT { get; set; }
        public int? RefundCountOrderNoNotVAT { get; set; }
        public int? RefundCountCustomerNotVAT { get; set; }
    }
    public class SaleVINID
    {
        public string StoreNo { get; set; }
        public double? VINIDAmount { get; set; }
    }

    public class ReceivablesCXPoint  
    {
        public string StoreNo { get; set; }
        public double? CXPointAmount { get; set; }
    }

    public class PayooCosmo
    {
        public string StoreNo { get; set; }
        public double? PayooAmount { get; set; }
    }



}
