using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model.Promotion;
using VCM.BLUEPOS.Model.OptionModel;

namespace VCM.BLUEPOS.Data.Promotion
{
    public interface IPromotionData
    {
        List<OptionModel> GetPromotionStatus();
        List<OptionModel> GetPromotionType();
        List<OfferHearderResponseModel> GetOfferHeaderList(string textSearch, string description, string status, string offerType, string itemNo, string storeNo, string salesType, int exp, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<DetailOfferHeaderResponseModel> GetDetailOfferHeaderList(string offerNo, out int totalRecord, int skip = 0, int take = 100);
        List<DetailOfferBuyResponseModel> GetDetailOfferBuyList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<DetailOfferBenefitsResponseModel> GetDetailOfferBenefitsList(string offerNo, out int totalRecord, int skip = 0, int take = 100);
        List<DetailOfferGetResponseModel> GetDetailOfferGetList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<DetailOfferSiteModel> GetDetailOfferSiteList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<DetailOfferPriorityModel> GetDetailOfferPriorityList(string offerType, out int totalRecord, int skip = 0, int take = 100);
        List<ExportOfferHeaderModel> ExportExcelGetOfferHeaderList(string textSearch, string promotionName, string promotionStatus, string promotionTypes, string salesType, string itemNo, string storeNo);
        List<CheckPromotionModel> CheckPromotionList(string storeNo, string offerNo, string offerType, string salesType, string memberType, string status, string itemNo, string keyWord, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<CheckPromotionModel> ExportExcelCheckPromotion(string storeNo, string offerNo, string offerType, string salesType, string memberType, string status, string itemNo, string keyWord, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcel_PromotionOfferBuyModel> ExportToExcel_PromotionOfferBuy(string offerNo);
        List<ExportExcel_PromotionOfferGetModel> ExportToExcel_PromotionOfferGet(string offerNo);
        List<ExportExcel_PromotionOfferSiteModel> ExportToExcel_PromotionOfferSite(string offerNo);
        List<ViewCheckPromotionModalModel> GetListViewPromotionCheck(string bonusbuyNo, string promotionNo, string barcodeNo, string itemNo);



    }

    public class PromotionData : IPromotionData
    {
        public List<OptionModel> GetPromotionStatus()
        {
            return new List<OptionModel>
            {
                new OptionModel
                {
                     Value = "0",               // 0 : Active
                     Text = "Có hiệu lực"       // Active
                },
                new OptionModel
                {
                     Value ="2",                // <> 0 (1 hoac 2): Disable
                     Text = "Hết hiệu lực"      // Disable
                }
            };
        }
        public List<OptionModel> GetPromotionType()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.OfferTypes
                                select new OptionModel
                                {
                                    Value = a.OfferType1,
                                    Text = a.OfferName
                                }).OrderBy(x => x.Value).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception)
            {
                return new List<OptionModel>();
            }
        }
        public List<OfferHearderResponseModel> GetOfferHeaderList(string textSearch, string description, string status, string offerType, string itemNo, string storeNo, string salesType, int exp, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<OfferHearderResponseModel>("[dbo].[GetPromotionOfferHeaderList] @No, @Description, @Status, @OfferType, @ItemNo, @StyleProfile, @StoreNo, @SalesType, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@No", textSearch ?? string.Empty),
                        new SqlParameter("@Description", description ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@OfferType", offerType ?? string.Empty),
                        new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                        new SqlParameter("@StyleProfile", "-1"),                         //Kênh/Chuỗi áp dụng : -1 = tất cả
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty), 
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@Exp", exp),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();
                    if (data == null || data.Count == 0)
                    {
                        return new List<OfferHearderResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<OfferHearderResponseModel>();
            }
        }
        public List<DetailOfferHeaderResponseModel> GetDetailOfferHeaderList(string offerNo, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = new List<DetailOfferHeaderResponseModel>();
                    totalRecord = 0;
                    var data = (from a in db.OfferHeaders
                                where a.No == offerNo
                                select new DetailOfferHeaderModel
                                {
                                    OfferNo = a.No,
                                    Type = a.SalesType,
                                    Description = a.Description,
                                    Status = a.Status,
                                    OfferType = a.OfferType,
                                    SalesType = a.SalesType,
                                    PriceGroup = a.PriceGroup,
                                    RoundingMethod = a.RoundingMethod,
                                    CurrencyCode = a.CurrencyCode,
                                    LastDateModified = a.LastDateModified,
                                    ValidationPeriodID = a.ValidationPeriodID,
                                    ValidationDescription = a.ValidationDescription,
                                    StartingDate = a.StartingDate,
                                    EndingDate = a.EndingDate,
                                    BlockPeriodicDiscount = a.BlockPeriodicDiscount,
                                    DealPrice = a.DealPrice,
                                    ShowDealLines = a.ShowDealLines,
                                    SalesTypeFilter = a.SalesTypeFilter,
                                    SelectionType = a.SelectionType,
                                    CustomerDiscGroup = a.CustomerDiscGroup,
                                    MemberValue = a.MemberValue,
                                    DiscountTrackingNo = a.DiscountTrackingNo,
                                    CouponCode = a.CouponCode,
                                    CouponQtyNeeded = a.CouponQtyNeeded,
                                    MemberType = a.MemberType,
                                    MemberAttribute = a.MemberAttribute,
                                    MemberAttributeValue = a.MemberAttributeValue,
                                    BlockSalesCommission = a.BlockSalesCommission,
                                    BlockManualPriceChange = a.BlockManualPriceChange,
                                    BlockInfoCodeDiscount = a.BlockInfoCodeDiscount,
                                    BlockLineDiscountOffer = a.BlockLineDiscountOffer,
                                    BlockTotalDiscountOffer = a.BlockTotalDiscountOffer,
                                    BlockTenderTypeDiscount = a.BlockTenderTypeDiscount,
                                    BlockMemberPoints = a.BlockMemberPoints,
                                    ConditionBuy = a.ConditionBuy,
                                    MemberOnly = a.MemberOnly,
                                    ConditionGet = a.ConditionGet,
                                    NoSeries = a.NoSeries,
                                    FromTime = a.FromTime,
                                    ToTime = a.ToTime,
                                    MonDay = a.Mon,
                                    TueDay = a.Tue,
                                    WedDay = a.Wed,
                                    ThuDay = a.Thu,
                                    FriDay = a.Fri,
                                    SatDay = a.Sat,
                                    SunDay = a.Sun,
                                    NumOfDays = a.NumOfDays,
                                    DayOfWeek = a.DayOfWeek,
                                    TenderTypeCode = a.TenderTypeCode,
                                    TenderTypeValue = a.TenderTypeValue,
                                    TenderTypeOfferPercent = a.TenderTypeOfferPercent,
                                    TenderTypeOfferAmount = a.TenderTypeOfferAmount,
                                    BankCode = a.BankCode,
                                    LocalSiteGroup = a.LocalSiteGroup,
                                    LimitQty = a.LimitQty,
                                    VoucherFromDate = a.VoucherFromDate,
                                    VoucherToDate = a.VoucherToDate,
                                    VoucherValidDay = a.VoucherValidDay,
                                    VoucherLimitNumber = a.VoucherLimitNumber,
                                    PromotionNo = a.PromotionNo,
                                    PriorityBBY = a.PriorityBBY,
                                    MinValue = a.MinValue,
                                    TotalDiscountType = a.TotalDiscountType,
                                    TotalDiscountValue = a.TotalDiscountValue,
                                    IsVoucher = a.IsVoucher,
                                    IsTotalBill = a.IsTotalBill,
                                    IsGift = a.IsGift,
                                    MemberCode = a.MemberCode,
                                    DiscountAmountMax = a.DiscountAmountMax,
                                    IsFullPrice = a.IsFullPrice,
                                    Counter = a.Counter,
                                    Pkey = a.Pkey
                                }).OrderBy(x => x.OfferNo).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailOfferHeaderResponseModel>();
                    }

                    var ConditionBuyStr = "";
                    var getConditionBuy = new Func<int?, string>(ConditionBuy =>
                    {
                        if (ConditionBuy == 1)
                        {
                            ConditionBuyStr = "OR";
                        }
                        else if (ConditionBuy == 2)
                        {
                            ConditionBuyStr = "AND";
                        }
                        return ConditionBuyStr;
                    });

                    var ConditionGetStr = "";
                    var getConditionGet = new Func<int?, string>(ConditionGet =>
                    {
                        if (ConditionGet == 1)
                        {
                            ConditionGetStr = "OR";
                        }
                        else if (ConditionGet == 2)
                        {
                            ConditionGetStr = "AND";
                        }
                        return ConditionGetStr;
                    });

                    result = (from b in data
                              select new DetailOfferHeaderResponseModel
                              {
                                  OfferNo = b.OfferNo,                                 
                                  Description = b.Description,
                                  Status = b.Status,
                                  OfferType = b.OfferType,
                                  SalesType = b.SalesType,
                                  PriceGroup = b.PriceGroup,
                                  RoundingMethod = b.RoundingMethod,
                                  CurrencyCode = b.CurrencyCode,
                                  LastDateModified = String.Format("{0:dd/MM/yyyy}", b.LastDateModified),
                                  ValidationPeriodID = b.ValidationPeriodID,
                                  ValidationDescription = b.ValidationDescription,
                                  StartingDate = String.Format("{0:dd/MM/yyyy}", b.StartingDate),
                                  EndingDate = String.Format("{0:dd/MM/yyyy}", b.EndingDate),
                                  BlockPeriodicDiscount = b.BlockPeriodicDiscount,
                                  DealPrice = Math.Round((double)b.DealPrice, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  ShowDealLines = b.ShowDealLines,
                                  SalesTypeFilter = b.SalesTypeFilter,
                                  SelectionType = b.SelectionType,
                                  CustomerDiscGroup = b.CustomerDiscGroup,
                                  MemberValue = b.MemberValue,
                                  DiscountTrackingNo = b.DiscountTrackingNo,
                                  CouponCode = b.CouponCode,
                                  CouponQtyNeeded = Math.Round((double)b.CouponQtyNeeded, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  MemberType = b.MemberType,
                                  MemberAttribute = b.MemberAttribute,
                                  MemberAttributeValue = b.MemberAttributeValue,
                                  BlockSalesCommission = b.BlockSalesCommission,
                                  BlockManualPriceChange = b.BlockManualPriceChange,
                                  BlockInfoCodeDiscount = b.BlockInfoCodeDiscount,
                                  BlockLineDiscountOffer = b.BlockLineDiscountOffer,
                                  BlockTotalDiscountOffer = b.BlockTotalDiscountOffer,
                                  BlockTenderTypeDiscount = b.BlockTenderTypeDiscount,
                                  BlockMemberPoints = b.BlockMemberPoints,
                                  ConditionBuyStr = getConditionBuy(b.ConditionBuy),
                                  MemberOnly = b.MemberOnly,
                                  ConditionGetStr = getConditionGet(b.ConditionGet),
                                  NoSeries = b.NoSeries,
                                  FromTime = b.FromTime,
                                  ToTime = b.ToTime,
                                  MonDay = b.MonDay,
                                  TueDay = b.TueDay,
                                  WedDay = b.WedDay,
                                  ThuDay = b.ThuDay,
                                  FriDay = b.FriDay,
                                  SatDay = b.SatDay,
                                  SunDay = b.SunDay,
                                  NumOfDays = b.NumOfDays,
                                  DayOfWeek = b.DayOfWeek,
                                  TenderTypeCode = b.TenderTypeCode,
                                  TenderTypeValue = Math.Round((double)b.TenderTypeValue, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  TenderTypeOfferPercent = Math.Round((double)b.TenderTypeOfferPercent, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  TenderTypeOfferAmount = Math.Round((double)b.TenderTypeOfferAmount, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  BankCode = b.BankCode,
                                  LocalSiteGroup = b.LocalSiteGroup,
                                  LimitQty = Math.Round((double)b.LimitQty, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  VoucherFromDate = String.Format("{0:dd/MM/yyyy}", b.VoucherFromDate),
                                  VoucherToDate = String.Format("{0:dd/MM/yyyy}", b.VoucherToDate),
                                  VoucherValidDay = b.VoucherValidDay,
                                  VoucherLimitNumber = b.VoucherLimitNumber,
                                  PromotionNo = b.PromotionNo,
                                  PriorityBBY = b.PriorityBBY,
                                  MinValue = b.MinValue,
                                  TotalDiscountType = b.TotalDiscountType,
                                  TotalDiscountValue = b.TotalDiscountValue,
                                  IsVoucher = Convert.ToBoolean(b.IsVoucher) ? "1" : "0",
                                  IsTotalBill = Convert.ToBoolean(b.IsTotalBill) ? "1" : "0",
                                  IsGift = Convert.ToBoolean(b.IsGift) ? "1" : "0",
                                  MemberCode = b.MemberCode,
                                  DiscountAmountMax = b.DiscountAmountMax,
                                  IsFullPrice = Convert.ToBoolean(b.IsFullPrice) ? "1" : "0",
                                  Counter = b.Counter,
                                  Pkey = b.Pkey
                              }).OrderBy(x => x.OfferNo).ToList();

                    totalRecord = data.Count;
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOfferHeaderResponseModel>();
            }
        }
        public List<DetailOfferBuyResponseModel> GetDetailOfferBuyList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = new List<DetailOfferBuyResponseModel>();
                    totalRecord = 0;

                    var data = (from a in db.OfferBuys
                                join b in db.OfferHeaders on a.OfferNo equals b.No
                                where a.OfferNo == offerNo
                                select new DetailOfferBuyModel
                                {
                                    OfferNo = a.OfferNo,
                                    LineNo = a.LineNo,
                                    LineType = a.LineType,
                                    No = a.No,
                                    Description = a.Description,
                                    UnitOfMeasure = a.UnitOfMeasure,
                                    DiscountType = a.DiscountType,
                                    DiscountValue = a.DiscountValue,
                                    Quantity = a.Quantity,
                                    Step = a.Step,
                                    BonusBuyNo = a.BonusBuyNo,
                                    LineGroup = a.LineGroup,
                                    ScaleType = a.ScaleType,
                                    Counter = a.Counter,
                                    Pkey = a.Pkey
                                }).OrderBy(x => x.No).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailOfferBuyResponseModel>();
                    }

                    var DiscountTypeStr = "";
                    var getDiscountType = new Func<int?, string>(DiscountType =>
                    {
                        if (DiscountType == 0)
                        {
                            DiscountTypeStr = "%";
                        }
                        else
                        {
                            if (DiscountType == 1)
                            {
                                DiscountTypeStr = "Amount";
                            }
                            else if (DiscountType == 2)
                            {
                                DiscountTypeStr = "Price";
                            }
                        }
                        return DiscountTypeStr;
                    });

                    var ScaleTypeStr = "";
                    var getScaleType = new Func<string, string>(ScaleType =>
                    {
                        if (ScaleType == "A")
                        {
                            ScaleTypeStr = "From";
                        }
                        else
                        {
                            if (ScaleType == "B")
                            {
                                ScaleTypeStr = "UpTo";
                            }
                            else if (ScaleType == "C")
                            {
                                ScaleTypeStr = "Equal";
                            }
                        }
                        return ScaleTypeStr;
                    });

                    result = (from b in data
                              select new DetailOfferBuyResponseModel
                              {
                                  OfferNo = string.IsNullOrEmpty(b.OfferNo) ? string.Empty : b.OfferNo,
                                  LineNo = b.LineNo,
                                  LineType = b.LineType,
                                  No = string.IsNullOrEmpty(b.No) ? string.Empty : b.No,
                                  Description = string.IsNullOrEmpty(b.Description) ? string.Empty : b.Description,
                                  UnitOfMeasure = string.IsNullOrEmpty(b.UnitOfMeasure) ? string.Empty : b.UnitOfMeasure,
                                  DiscountTypeStr = getDiscountType(b.DiscountType),
                                  DiscountValue = Math.Round((double)b.DiscountValue, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  Quantity = Math.Round((double)b.Quantity, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  Step = Math.Round((double)b.Step, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  BonusBuyNo = string.IsNullOrEmpty(b.BonusBuyNo) ? string.Empty : b.BonusBuyNo,
                                  LineGroup = string.IsNullOrEmpty(b.LineGroup) ? string.Empty : b.LineGroup,
                                  ScaleTypeStr = getScaleType(b.ScaleType),
                                  Counter = b.Counter,
                                  Pkey = string.IsNullOrEmpty(b.Pkey) ? string.Empty : b.Pkey
                              }).OrderBy(x => x.No).ToList();

                    if (!string.IsNullOrEmpty(searchText))  // tim theo ma san pham
                    {
                        result = result.Where(x => x.No.Contains(searchText)).ToList();
                    }
                    totalRecord = result.Count();
                    result = skip == 0 ? result.Take(take).ToList() : result.Skip(skip).Take(take).ToList();
                    return result.ToList();

                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOfferBuyResponseModel>();
            }
        }
        public List<DetailOfferBenefitsResponseModel> GetDetailOfferBenefitsList(string offerNo, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var result = new List<DetailOfferBenefitsResponseModel>();
                    var data = (from a in db.OfferBenefits
                                join b in db.OfferHeaders on a.OfferNo equals b.No
                                where a.OfferNo == offerNo
                                select new DetailOfferBenefitsModel
                                {
                                    OfferNo = a.OfferNo,
                                    LineNo = a.LineNo,
                                    Type = a.Type,
                                    No = a.No,
                                    VariantCode = a.VariantCode,
                                    Description = a.Description,
                                    ValueType = a.ValueType,
                                    Value = a.Value,
                                    StepAmount = a.StepAmount,
                                    LineGroup = a.LineGroup,
                                    Quantity = a.Quantity,
                                    UnitOfMeasure = a.UnitOfMeasure,
                                    Counter = a.Counter,
                                    Pkey = a.Pkey
                                }).OrderBy(x => x.No).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailOfferBenefitsResponseModel>();
                    }

                    var ValueTypeStr = "";
                    var getValueType = new Func<int?, string>(ValueType =>
                    {
                        if (ValueType == 0)
                        {
                            ValueTypeStr = "%";
                        }
                        else
                        {
                            if (ValueType == 1)
                            {
                                ValueTypeStr = "Amount";
                            }
                            else if (ValueType == 2)
                            {
                                ValueTypeStr = "Price";
                            }
                        }
                        return ValueTypeStr;
                    });

                    result = (from b in data
                              select new DetailOfferBenefitsResponseModel
                              {
                                  OfferNo = b.OfferNo,
                                  LineNo = b.LineNo,
                                  Type = b.Type,
                                  No = b.No,
                                  VariantCode = b.VariantCode,
                                  Description = b.Description,
                                  ValueTypeStr = getValueType(b.ValueType),
                                  Value = Math.Round((double)b.Value, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  StepAmount = Math.Round((double)b.StepAmount, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  LineGroup = b.LineGroup,
                                  Quantity = b.Quantity,
                                  UnitOfMeasure = b.UnitOfMeasure,
                                  Counter = b.Counter,
                                  Pkey = b.Pkey
                              }).OrderBy(x => x.No).ToList();

                    totalRecord = data.Count();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOfferBenefitsResponseModel>();
            }
        }
        public List<DetailOfferGetResponseModel> GetDetailOfferGetList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var result = new List<DetailOfferGetResponseModel>();
                    var data = (from a in db.OfferGets
                                join b in db.OfferHeaders on a.OfferNo equals b.No
                                where a.OfferNo == offerNo
                                select new DetailOfferGetModel
                                {
                                    OfferNo = a.OfferNo,
                                    LineNo = a.LineNo,
                                    LineType = a.LineType,
                                    No = a.No,
                                    Description = a.Description,
                                    UnitOfMeasure = a.UnitOfMeasure,
                                    DiscountType = a.DiscountType,
                                    DiscountValue = a.DiscountValue,
                                    Quantity = a.Quantity,
                                    Step = a.Step,
                                    BonusBuyNo = a.BonusBuyNo,
                                    LineGroup = a.LineGroup,
                                    ScaleType = a.ScaleType,
                                    Counter = a.Counter,
                                    Pkey = a.Pkey
                                }).OrderBy(x => x.No).ToList();

                    if (data == null)
                    {
                        return new List<DetailOfferGetResponseModel>();
                    }

                    var DiscountTypeStr = "";
                    var getDiscountType = new Func<int?, string>(DiscountType =>
                    {
                        if (DiscountType == 0)
                        {
                            DiscountTypeStr = "%";
                        }
                        else
                        {
                            if (DiscountType == 1)
                            {
                                DiscountTypeStr = "Amount";
                            }
                            else if (DiscountType == 2)
                            {
                                DiscountTypeStr = "Price";
                            }
                        }
                        return DiscountTypeStr;
                    });

                    var ScaleTypeStr = "";
                    var getScaleType = new Func<string, string>(ScaleType =>
                    {
                        if (ScaleType == "A")
                        {
                            ScaleTypeStr = "From";
                        }
                        else
                        {
                            if (ScaleType == "B")
                            {
                                ScaleTypeStr = "UpTo";
                            }
                            else if (ScaleType == "C")
                            {
                                ScaleTypeStr = "Equal";
                            }
                        }

                        return ScaleTypeStr;

                    });

                    result = (from a in data
                              select new DetailOfferGetResponseModel
                              {
                                  OfferNo = string.IsNullOrEmpty(a.OfferNo) ? string.Empty : a.OfferNo,
                                  LineNo = a.LineNo,
                                  LineType = a.LineType,
                                  No = string.IsNullOrEmpty(a.No) ? string.Empty : a.No,
                                  Description = string.IsNullOrEmpty(a.Description) ? string.Empty : a.Description,
                                  UnitOfMeasure = string.IsNullOrEmpty(a.UnitOfMeasure) ? string.Empty : a.UnitOfMeasure,
                                  DiscountTypeStr = getDiscountType(a.DiscountType),
                                  DiscountValue = Math.Round((double)a.DiscountValue, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  Quantity = Math.Round((double)a.Quantity, 0, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  Step = Math.Round((double)a.Step, 2, MidpointRounding.AwayFromZero).ToString("#,0.00"),
                                  BonusBuyNo = string.IsNullOrEmpty(a.BonusBuyNo) ? string.Empty : a.BonusBuyNo,
                                  LineGroup = string.IsNullOrEmpty(a.LineGroup) ? string.Empty : a.LineGroup,
                                  ScaleType = getScaleType(a.ScaleType),
                                  Counter = a.Counter,
                                  Pkey = string.IsNullOrEmpty(a.Pkey) ? string.Empty : a.Pkey
                              }).OrderBy(x => x.No).ToList();

                    if (!string.IsNullOrEmpty(searchText))  // tim theo ma san pham
                    {
                        result = result.Where(x => x.No.Contains(searchText)).ToList();
                    }
                    totalRecord = result.Count();
                    result = skip == 0 ? result.Take(take).ToList() : result.Skip(skip).Take(take).ToList();
                    return result.ToList();

                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOfferGetResponseModel>();
            }
        }
        public List<DetailOfferSiteModel> GetDetailOfferSiteList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = (from a in db.OfferSites
                                join b in db.OfferHeaders on a.OfferNo equals b.No
                                join c in db.Stores on a.StoreNo equals c.No
                                where a.OfferNo == offerNo
                                select new DetailOfferSiteModel
                                {
                                    OfferNo = a.OfferNo,
                                    PriceGroupCode = a.PriceGroupCode,
                                    StoreNo = a.StoreNo,
                                    StyleProfile = c.StyleProfile,          // Chuỗi áp dụng
                                    Counter = a.Counter,
                                    Pkey = a.Pkey
                                }).OrderBy(x => x.StoreNo).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailOfferSiteModel>();
                    }

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        data = data.Where(x => x.StoreNo.Contains(searchText)).ToList();
                    }
                    totalRecord = data.Count();
                    data = skip == 0 ? data.Take(take).ToList() : data.Skip(skip).Take(take).ToList();
                    return data.ToList();
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOfferSiteModel>();
            }
        }
        public List<DetailOfferPriorityModel> GetDetailOfferPriorityList(string offerType, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var result = new List<DetailOfferPriorityModel>();

                    var data = (from a in db.OfferPriorities
                                join b in db.OfferHeaders on a.OfferType equals b.OfferType
                                where a.OfferType == offerType
                                select new DetailOfferPriorityModel
                                {
                                    OfferType = a.OfferType,
                                    Priority = a.Priority,
                                    IsMember = a.IsMember,
                                    IsDuplicate = a.IsDuplicate,
                                    Counter = a.Counter,
                                    Pkey = a.Pkey
                                }).OrderBy(x => x.OfferType).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailOfferPriorityModel>();
                    }

                    result = (from b in data
                              select new DetailOfferPriorityModel
                              {
                                  OfferType = b.OfferType,
                                  Priority = b.Priority,
                                  IsMember = b.IsMember,
                                  IsDuplicate = b.IsDuplicate,
                                  Counter = b.Counter,
                                  Pkey = b.Pkey
                              }).OrderBy(x => x.OfferType).ToList();

                    totalRecord = data.Count();
                    result = data.OrderBy(x => x.OfferType).Skip(skip).Take(take).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailOfferPriorityModel>();
            }
        }
        public List<ExportOfferHeaderModel> ExportExcelGetOfferHeaderList(string textSearch, string promotionName, string promotionStatus, string promotionTypes, string salesType, string itemNo, string storeNo)
        {
            try
            {               
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportOfferHeaderModel>("[dbo].[GetPromotionOfferHeaderList] @No, @Description, @Status, @OfferType, @ItemNo, @StyleProfile, @StoreNo, @SalesType, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@No", textSearch ?? string.Empty),
                        new SqlParameter("@Description", promotionName ?? string.Empty),
                        new SqlParameter("@Status", promotionStatus ?? string.Empty),
                        new SqlParameter("@OfferType", promotionTypes ?? string.Empty),
                        new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                        new SqlParameter("@StyleProfile", "-1"), 
                        new SqlParameter("@StoreNo", "-1"),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@Exp", 1),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportOfferHeaderModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportOfferHeaderModel>();
            }
        }

        public List<CheckPromotionModel> CheckPromotionList(string storeNo, string offerNo, string offerType, string salesType, string memberType, string status, string itemNo, string keyWord, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<CheckPromotionModel>("[dbo].[GetPromotionCheckList] @StoreNo, @No, @OfferType, @SalesType, @MemberCode, @Status, @ItemNo, @KeyWord, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@No", offerNo ?? string.Empty),
                        new SqlParameter("@OfferType", offerType ?? string.Empty),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@MemberCode", memberType ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                        new SqlParameter("@KeyWord", keyWord ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<CheckPromotionModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<CheckPromotionModel>();
            }
        }

        public List<CheckPromotionModel> ExportExcelCheckPromotion(string storeNo, string offerNo, string offerType, string salesType, string memberType, string status, string itemNo, string keyWord, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            var data = new List<CheckPromotionModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    data = db.Database.SqlQuery<CheckPromotionModel>("[dbo].[GetPromotionCheckList] @StoreNo, @No, @OfferType, @SalesType, @MemberCode, @Status, @ItemNo, @KeyWord, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@No", offerNo ?? string.Empty),
                        new SqlParameter("@OfferType", offerType ?? string.Empty),
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@MemberCode", memberType ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@ItemNo", itemNo ?? string.Empty),
                        new SqlParameter("@KeyWord", keyWord ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<CheckPromotionModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<CheckPromotionModel>();
            }
        }

        public List<ExportExcel_PromotionOfferBuyModel> ExportToExcel_PromotionOfferBuy(string offerNo)
        {
            try
            {
                var listData = new List<ExportExcel_PromotionOfferBuyModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    listData = db.Database.SqlQuery<ExportExcel_PromotionOfferBuyModel>("[dbo].[PLG_Promotion_OfferBuy_List] @OfferNo",
                             new SqlParameter("@OfferNo", offerNo ?? string.Empty)).ToList();

                    if (listData == null || listData.Count == 0)
                    {
                        return new List<ExportExcel_PromotionOfferBuyModel>();
                    }
                    return listData;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_PromotionOfferBuyModel>();
            }
        }

        public List<ExportExcel_PromotionOfferGetModel> ExportToExcel_PromotionOfferGet(string offerNo)
        {
            try
            {
                var listData = new List<ExportExcel_PromotionOfferGetModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    listData = db.Database.SqlQuery<ExportExcel_PromotionOfferGetModel>("[dbo].[PLG_Promotion_OfferGet_List] @OfferNo",
                             new SqlParameter("@OfferNo", offerNo ?? string.Empty)).ToList();

                    if (listData == null || listData.Count == 0)
                    {
                        return new List<ExportExcel_PromotionOfferGetModel>();
                    }

                    return listData;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_PromotionOfferGetModel>();
            }
        }

        public List<ExportExcel_PromotionOfferSiteModel> ExportToExcel_PromotionOfferSite(string offerNo)
        {
            try
            {
                var listData = new List<ExportExcel_PromotionOfferSiteModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    listData = db.Database.SqlQuery<ExportExcel_PromotionOfferSiteModel>("[dbo].[PLG_Promotion_OfferSite_List] @OfferNo",
                             new SqlParameter("@OfferNo", offerNo ?? string.Empty)).ToList();

                    if (listData == null || listData.Count == 0)
                    {
                        return new List<ExportExcel_PromotionOfferSiteModel>();
                    }

                    return listData;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_PromotionOfferSiteModel>();
            }
        }

        public List<ViewCheckPromotionModalModel> GetListViewPromotionCheck(string bonusbuyNo, string promotionNo, string barcodeNo, string itemNo)
        {
            var result = new List<ViewCheckPromotionModalModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<ViewCheckPromotionModalModel>("[dbo].[GET_VIEW_DETAIL_PROMOTION_CHECK] @BonusBuyNo, @PromotionNo, @BarcodeNo, @ItemNo, @PageSize, @PageNumber",
                        new SqlParameter("@BonusBuyNo", bonusbuyNo),
                        new SqlParameter("@PromotionNo", promotionNo),
                        new SqlParameter("@BarcodeNo", barcodeNo),
                        new SqlParameter("@ItemNo", itemNo),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }


















    }
}
