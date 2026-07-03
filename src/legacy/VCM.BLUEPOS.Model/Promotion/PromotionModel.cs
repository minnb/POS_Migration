using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Promotion
{
    public class OfferHearderResponseModel   
    {
        public int ID { get; set; }
        public string BonusbuyNo { get; set; }
        public string PromotionNo { get; set; }
        public string Description { get; set; }
        public string OfferType { get; set; }
        public string SalesType { get; set; }
        public string SalesTypeName { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Status { get; set; }
        public string StyleProfile { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
        public string LocalSiteGroup { get; set; }
        public decimal LimitQty { get; set; }
        public string VoucherFromDate { get; set; }
        public string VoucherToDate { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string LastDateModified { get; set; }
        public int Total { get; set; }

    }

    public class ExportOfferHearderResponseModel
    {
        public string BonusBuyNo { get; set; }
        public string PromotionNo { get; set; }
        public string Description { get; set; }
        public string OfferType { get; set; }
        public string StyleProfile { get; set; }
        public string SalesType { get; set; }
        public string SalesTypeName { get; set; }
        public string Status { get; set; }        
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
        public string VoucherFromDate { get; set; }
        public string VoucherToDate { get; set; }
        public string LocalSiteGroup { get; set; }
        public decimal LimitQty { get; set; }
        public string LastDateModified { get; set; }
    }

    public class DetailOfferHeaderModel
    {
        public string OfferNo { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int? Status { get; set; }
        public string OfferType { get; set; }
        public string SalesType { get; set; }
        public string PriceGroup { get; set; }
        public string RoundingMethod { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime? LastDateModified { get; set; }
        public string ValidationPeriodID { get; set; }
        public string ValidationDescription { get; set; }
        public DateTime? StartingDate { get; set; }
        public DateTime? EndingDate { get; set; }
        public Int64? BlockPeriodicDiscount { get; set; }
        public Nullable<double> DealPrice { get; set; }
        public int? ShowDealLines { get; set; }
        public string SalesTypeFilter { get; set; }
        public int? SelectionType { get; set; }
        public string CustomerDiscGroup { get; set; }
        public string MemberValue { get; set; }
        public string DiscountTrackingNo { get; set; }
        public string CouponCode { get; set; }
        public Nullable<double> CouponQtyNeeded { get; set; }
        public int? MemberType { get; set; }
        public string MemberAttribute { get; set; }
        public string MemberAttributeValue { get; set; }
        public int? BlockSalesCommission { get; set; }
        public int? BlockManualPriceChange { get; set; }
        public int? BlockInfoCodeDiscount { get; set; }
        public int? BlockLineDiscountOffer { get; set; }
        public int? BlockTotalDiscountOffer { get; set; }
        public int? BlockTenderTypeDiscount { get; set; }
        public int? BlockMemberPoints { get; set; }
        public int? ConditionBuy { get; set; }
        public int? MemberOnly { get; set; }
        public int? ConditionGet { get; set; }
        public string NoSeries { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }

        public byte? MonDay { get; set; }
        public byte? TueDay { get; set; }
        public byte? WedDay { get; set; }
        public byte? ThuDay { get; set; }
        public byte? FriDay { get; set; }
        public byte? SatDay { get; set; }
        public byte? SunDay { get; set; }

        public int? NumOfDays { get; set; }
        public string DayOfWeek { get; set; }
        public string TenderTypeCode { get; set; }
        public Nullable<double> TenderTypeValue { get; set; }
        public Nullable<double> TenderTypeOfferPercent { get; set; }
        public Nullable<double> TenderTypeOfferAmount { get; set; }
        public string BankCode { get; set; }
        public string LocalSiteGroup { get; set; }
        public Nullable<double> LimitQty { get; set; }
        public DateTime? VoucherFromDate { get; set; }
        public DateTime? VoucherToDate { get; set; }
        public int? VoucherValidDay { get; set; }
        public int? VoucherLimitNumber { get; set; }
        public string PromotionNo { get; set; }
        public int? PriorityBBY { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }

        public double? MinValue { get; set; }
        public int? TotalDiscountType { get; set; }
        public double? TotalDiscountValue { get; set; }
        public bool? IsVoucher { get; set; }
        public bool? IsTotalBill { get; set; }
        public bool? IsGift { get; set; }
        public string MemberCode { get; set; }
        public double? DiscountAmountMax { get; set; }
        public bool? IsFullPrice { get; set; }

    }

    public class DetailOfferHeaderResponseModel
    {
        public string OfferNo { get; set; }
        public int? Type { get; set; }
        public string Description { get; set; }
        public int? Status { get; set; }
        public string OfferType { get; set; }
        public string SalesType { get; set; }
        public string PriceGroup { get; set; }
        public string RoundingMethod { get; set; }
        public string CurrencyCode { get; set; }
        public string LastDateModified { get; set; }
        public string ValidationPeriodID { get; set; }
        public string ValidationDescription { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
        public Int64? BlockPeriodicDiscount { get; set; }
        public string DealPrice { get; set; }
        public int? ShowDealLines { get; set; }
        public string SalesTypeFilter { get; set; }
        public int? SelectionType { get; set; }
        public string CustomerDiscGroup { get; set; }
        public string MemberValue { get; set; }
        public string DiscountTrackingNo { get; set; }
        public string CouponCode { get; set; }
        public string CouponQtyNeeded { get; set; }
        public int? MemberType { get; set; }
        public string MemberAttribute { get; set; }
        public string MemberAttributeValue { get; set; }
        public int? BlockSalesCommission { get; set; }
        public int? BlockManualPriceChange { get; set; }
        public int? BlockInfoCodeDiscount { get; set; }
        public int? BlockLineDiscountOffer { get; set; }
        public int? BlockTotalDiscountOffer { get; set; }
        public int? BlockTenderTypeDiscount { get; set; }
        public int? BlockMemberPoints { get; set; }
        public string ConditionBuyStr { get; set; }
        public int? MemberOnly { get; set; }
        public string ConditionGetStr { get; set; }
        public string NoSeries { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }

        public byte? MonDay { get; set; }
        public byte? TueDay { get; set; }
        public byte? WedDay { get; set; }
        public byte? ThuDay { get; set; }
        public byte? FriDay { get; set; }
        public byte? SatDay { get; set; }
        public byte? SunDay { get; set; }

        public int? NumOfDays { get; set; }
        public string DayOfWeek { get; set; }
        public string TenderTypeCode { get; set; }
        public string TenderTypeValue { get; set; }
        public string TenderTypeOfferPercent { get; set; }
        public string TenderTypeOfferAmount { get; set; }
        public string BankCode { get; set; }
        public string LocalSiteGroup { get; set; }
        public string LimitQty { get; set; }
        public string VoucherFromDate { get; set; }
        public string VoucherToDate { get; set; }
        public int? VoucherValidDay { get; set; }
        public int? VoucherLimitNumber { get; set; }
        public string PromotionNo { get; set; }
        public int? PriorityBBY { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }

        public double? MinValue { get; set; }
        public int? TotalDiscountType { get; set; }
        public double? TotalDiscountValue { get; set; }
        public string IsVoucher { get; set; }
        public string IsTotalBill { get; set; }
        public string IsGift { get; set; }
        public string MemberCode { get; set; }
        public double? DiscountAmountMax { get; set; }
        public string IsFullPrice { get; set; }

    }

    public class DetailOfferBuyModel
    {
        public string OfferNo { get; set; }
        public int? LineNo { get; set; }
        public int? LineType { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public int? DiscountType { get; set; }
        public Nullable<double> DiscountValue { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> Step { get; set; }
        public string BonusBuyNo { get; set; }
        public string LineGroup { get; set; }
        public string ScaleType { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DetailOfferBuyResponseModel
    {
        public string OfferNo { get; set; }
        public int? LineNo { get; set; }
        public int? LineType { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string DiscountTypeStr { get; set; }
        public string DiscountValue { get; set; }
        public string Quantity { get; set; }
        public string Step { get; set; }
        public string BonusBuyNo { get; set; }
        public string LineGroup { get; set; }
        public string ScaleTypeStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DetailOfferBenefitsModel
    {
        public string OfferNo { get; set; }
        public int? LineNo { get; set; }
        public int? Type { get; set; }
        public string No { get; set; }
        public string VariantCode { get; set; }
        public string Description { get; set; }
        public int? ValueType { get; set; }
        public Nullable<double>  Value { get; set; }
        public Nullable<double> StepAmount { get; set; }
        public string LineGroup { get; set; }
        public int? Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DetailOfferBenefitsResponseModel
    {
        public string OfferNo { get; set; }
        public int? LineNo { get; set; }
        public int? Type { get; set; }
        public string No { get; set; }
        public string VariantCode { get; set; }
        public string Description { get; set; }
        public string ValueTypeStr { get; set; }
        public string Value { get; set; }
        public string StepAmount { get; set; }
        public string LineGroup { get; set; }
        public int? Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DetailOfferGetModel
    {
        public string OfferNo { get; set; }
        public int? LineNo { get; set; }
        public int? LineType { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public int? DiscountType { get; set; }
        public Nullable<double> DiscountValue { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> Step { get; set; }
        public string BonusBuyNo { get; set; }
        public string LineGroup { get; set; }
        public string ScaleType { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DetailOfferGetResponseModel
    {
        public string OfferNo { get; set; }
        public int? LineNo { get; set; }
        public int? LineType { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string DiscountTypeStr { get; set; }
        public string DiscountValue { get; set; }
        public string Quantity { get; set; }
        public string Step { get; set; }
        public string BonusBuyNo { get; set; }
        public string LineGroup { get; set; }
        public string ScaleType { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DetailOfferSiteModel
    {
        public string OfferNo { get; set; }
        public string PriceGroupCode { get; set; }
        public string StoreNo { get; set; }
        public string StyleProfile { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DetailOfferPriorityModel
    {
        public string OfferType { get; set; }
        public int? Priority { get; set; }
        public bool? IsMember { get; set; }
        public bool? IsDuplicate { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class ExportOfferHeaderModel
    {
        public string BonusBuyNo { get; set; }
        public string PromotionNo { get; set; }
        public string Description { get; set; }
        public string OfferType { get; set; }
        public string SalesTypeName { get; set; }
        public string Status { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
        public string VoucherFromDate { get; set; }
        public string VoucherToDate { get; set; }
        public string LocalSiteGroup { get; set; }
        public decimal LimitQty { get; set; }
        public string LastDateModified { get; set; }
    }

    public class CheckPromotionModel
    {
        public string BonusbuyNo { get; set; }
        public string PromotionNo { get; set; }
        public string Description { get; set; }
        public string OfferType { get; set; }
        public string SalesType { get; set; }     // Hinh thuc ban hang
        public string SalesTypeName { get; set; } // Hinh thuc ban hang
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Status { get; set; }
        public string StoreNo { get; set; }
        public string IPAddress { get; set; }
        public string TypeQuery { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
        public string LocalSiteGroup { get; set; }
        public string Barcode { get; set; }
        public string UOM { get; set; }
        public int DiscountType { get; set; }
        public string DiscountTypeStr { get; set; }
        public double? DiscountValue { get; set; }
        public double? SalesPrice { get; set; }
        public double? Discount { get; set; }
        public double? Amount { get; set; } 
        public string MemberOnly { get; set; }  // Ap dung cho the thanh vien
        public string MemberCode { get; set; }  // Hang the = Loai the
        public double? LimitQty { get; set; }
        public string VoucherFromDate { get; set; }
        public string VoucherToDate { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string LastDateModified { get; set; }
        public int Total { get; set; }
    }

    public class ExportExcelCheckPromotionModel
    {
        public string StoreNo { get; set; }
        public string BonusBuyNo { get; set; }
        public string PromotionNo { get; set; }
        public string Description { get; set; }
        public string OfferType { get; set; }
        public string DiscountTypeStr { get; set; }
        public string SalesTypeName { get; set; }
        public string Status { get; set; }
        public string Barcode { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }        
        public double? SalesPrice { get; set; }
        public double? Discount { get; set; }
        public double? Amount { get; set; }       
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }     
        public string MemberOnly { get; set; }
        public string MemberCode { get; set; }
        public string LastDateModified { get; set; }
    }

    public class ViewCheckPromotionModalModel
    {
        public string Status { get; set; }
        public string OfferType { get; set; }
        public int DiscountType { get; set; }
        public string BonusBuy { get; set; }
        public string PromotionNo { get; set; }
        public string Barcode { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Size { get; set; }
        public string UOM { get; set; }
        public double? DiscountValue { get; set; }
        public double? SalesPrice { get; set; }
        public double? Amount { get; set; }
        
        //public string DiscountTypeStr { get; set; }
        //public double? Discount { get; set; }
        //public string Description { get; set; }        
        //public string SalesType { get; set; }     // Hinh thuc ban hang
        //public string SalesTypeName { get; set; } // Hinh thuc ban hang
        //public string StoreNo { get; set; }
        //public string IPAddress { get; set; }
        //public string TypeQuery { get; set; }
        //public string StartingDate { get; set; }
        //public string EndingDate { get; set; }
        //public string LocalSiteGroup { get; set; }
        //public string MemberOnly { get; set; }  // Ap dung cho the thanh vien
        //public string MemberCode { get; set; }  // Hang the = Loai the
        //public double? LimitQty { get; set; }
        //public string VoucherFromDate { get; set; }
        //public string VoucherToDate { get; set; }
        //public long? Counter { get; set; }
        //public string Pkey { get; set; }
        //public string LastDateModified { get; set; }
        //public int Total { get; set; }

    }






}
