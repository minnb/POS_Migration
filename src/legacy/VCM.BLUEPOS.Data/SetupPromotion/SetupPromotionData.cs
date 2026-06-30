using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Model.SetupPromotion.SetupSpecialComboModel;


namespace VCM.BLUEPOS.Data.SetupPromotion
{
    public interface ISetupPromotionData
    {
        string GenerateRandomCode(string dto);
        SetupMainModel ViewSetupMain();
        List<ItemModel> ListItem();
        List<OfferHeaderModel> LoadSetupPromotionHeader(string offerNo, string offerName, string approve, out int total, int skip, int take);
        List<OfferTypeModel> LoadOfferType();
        OfferHeaderModel GetOfferHeader(string bonusBuy);
        List<OfferBuyModel> GetOfferBuy(string bonusBuy);
        List<OfferSiteModel> GetOfferSite(string bonusBuy);
        List<OfferGetModel> GetOfferGet(string offerNo, bool isTotalBill, bool isSetupGet);
        Tuple<bool, string, OfferHeaderModel> SaveSetupPromotion(OfferHeaderModel model);
        Tuple<bool, string, OfferHeaderModel> SaveSetupCTKMAll(AdvenceSetupRequest req);
        Tuple<bool, string, OfferHeaderModel> UpdateOfferHeader(OfferHeaderModel model);
        Tuple<bool, string, OfferHeaderModel> InsertUpdateAdvance(OfferHeaderModel model);
        Tuple<bool, string> DeleteOfferBuy(string offerNo, int lineNo, int lineType, string groupCode);
        Tuple<bool, string> DeleteOfferGet(string offerNo, int lineNo, bool isTotalBill, bool isSetupGet, int lineType, string groupCode);
        Tuple<bool, string, OfferBuyModel> InsertUpdateOfferBuy(OfferBuyModel model, bool isTotalBill, bool isSetupGet, string conditionBuy, string offerType, string itemHidden);
        List<ItemSetupMode> ListItemSetup(string itemNo, string description, out int total, int skip, int take);

        Tuple<bool, string, OfferGetModel> InsertUpdateOfferGetOrBenefit(OfferGetModel model, bool isTotalBill, bool isSetupGet, string conditionGet, string offerType, string itemHidden);
        Tuple<bool, string> SetupGroupSite(GroupSiteModel model);
        List<GroupSiteModel> ListGroupSiteSetup(string groupCode, string groupName, out int total, int skip, int take);
        List<StoreSetupModel> ListStoreSetup(string groupCode, string storeNo, string storeName, out int total, int skip, int take);
        Tuple<bool, string, List<GroupSiteModel>> InsertUpdateOfferSite(string offerNo, List<string> listGroupCode);
        Tuple<bool, string> SetupGroupBuyItem(BuyGroupItemModel model);
        List<BuyGroupItemModel> LoadBuyGroup(string groupCode, string groupName, out int total, int skip, int take);
        List<ItemSetupMode> LoadListItemBuySetup(string groupCode, string itemNo, string itemName, out int total, int skip, int take);
        Tuple<bool, string> DeleteOfferSite(string offerNo, string groupSite);

        Tuple<bool, string, OfferHeaderModel> SetupPromotionHeader(OfferHeaderModel model);
        OfferHeaderModel SetupGetOfferHeader(string bonusBuy);
        Tuple<bool, string, OfferHeaderModel> SetupInsertUpdateAdvance(OfferHeaderModel model);
        Tuple<bool, string, OfferBuyModel> SetupInsertUpdateOfferBuy(OfferBuyModel model, bool isTotalBill, bool isSetupGet, string conditionBuy, string offerType, string itemHidden);
        GetBBModel SetupGetOfferBuy(string bonusBuy);
        Tuple<bool, string> SetupDeleteOfferBuy(string offerNo, int lineNo, int lineType, string groupCode);
        Tuple<bool, string> SetupInsertUpdateHeaderBuy(string offerNo, string buyLinkCat, string checkMinValue, string minValue);
        GetBBModel SetupGetOfferGet(string offerNo);
        Tuple<bool, string, OfferGetModel> SetupInsertUpdateOfferGet(OfferGetModel model, string conditionGet, string itemHidden);
        Tuple<bool, string> SetupDeleteOfferGet(string offerNo, int lineNo, int lineType, string groupCode);
        Tuple<bool, string> SetupInsertUpdateHeaderGet(string offerNo, string getLinkCat, string checkDiscount, string typeDiscount, string discountValue);
        Tuple<bool, string> SetupDeleteOfferGetByOfferNo(string offerNo);
        Tuple<bool, string> SetupInsertUpdateOfferSite(string offerNo, string siteGroupCode);
        List<OfferSiteModel> SetupGetOfferSite(string bonusBuy);
        Tuple<bool, string> SetupDeleteOfferSite(string offerNo, string groupSite);
        Tuple<bool, string> SetupDeleteGroupSite(string groupSite);
        Tuple<bool, string> SetupDeleteGroupItem(string groupItem);
        Tuple<bool, string> FinishSetupPromotion(string offerNo, string status, string salesType, string offerName, string tuNgay, string denNgay, bool isVoucher, bool isApprove);
        Tuple<bool, string> UpdateStatusPromotion(string offerNo, string status);
        List<OfferHeaderModel> LoadExtraFeeCombo(List<string> listOfferSelected, string offerNo, string offerName, out int total, int skip, int take);

        Tuple<bool, string> CreateSpecialCombo(CreateSetupSpecialComboModel model);
        List<GetSpecialComboHeaderModel> GetSpecialComboHeaderList(string FromDate, string ToDate, string storeNo, string textSearch, string SalesType,string MemberType, string Status, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<DetailSpecialComboModel> GetDetailSpecialComboList(string code, string storeNo, string status, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<CodeBySpecialComboHeaderModel> LoadCodeBySpecialComboHeader();
        List<ItemByComboLineModel> LoadComboxItemByComboLine();
        ResultResponseModel UpdateStatusComboHeader(string code, string status);
        List<ItemByComboLineModel> LoadComboxUOM(string itemNo);
        List<ItemByComboLineModel> GetListComboxUOM();
        List<ItemByComboLineModel> LoadComboxItem();
        Tuple<bool, string> CreateSpecialComboV2(CreateSetupSpecialComboModel model);
        Tuple<bool, string> UpdateSpecialComboV2(UpdateSetupSpecialComboModel model);
        List<DetailSpecialComboModel> GetDetailItemNoByCombo(string comboCode, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        
        Tuple<bool, string> UpdateItemNoByGroup(UpdateItemNoByGroupModel model);
        Tuple<bool, string> AddItemNoByGroupCode(UpdateItemNoByGroupModel model);
        List<ItemByComboLineModel> LoadComboxItemV2(string searchText, int page, int pageSize, out int countTotal, string itemNo);
        List<PriceSalesModel> LoadComboxPrice(string ItemNo, string UOM);
        Tuple<bool, string> DeleteItemNoByGroupCode(DeleteItemNoByGroupModel model);
        string LoadComboxPriceV2(string ItemNo);
        Tuple<bool, string> UpdateSpecialComboV3(UpdateSetupSpecialComboModel model);
        List<SpecialComboStoreModel> GetStoreSpecialCombo(string code, string status, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel UpdateStatusStore(string code, string storeno, string status, string userName);

    }


    public class SetupPromotionData : ISetupPromotionData
    {
        private readonly object lockRequest = new object();

        public List<OfferTypeModel> LoadOfferType()
        {
            var result = new List<OfferTypeModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.OfferTypes.Where(d => d.Enabled == true).Select(a => new OfferTypeModel
                    {
                        ID = a.ID,
                        OfferType1 = a.OfferType1,
                        OfferName = a.OfferName,
                        IsTotalBill = a.IsTotalBill,
                        IsSetupBuy = a.IsSetupBuy,
                        IsSetupGet = a.IsSetupGet,
                        IsVoucher = a.IsVoucher
                    }).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }

        public List<ItemModel> ListItem()
        {
            var result = new List<ItemModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.Items.Select(a => new ItemModel
                    {
                        No = a.No,
                        No2 = a.No2,
                        Description = a.Description,
                        SearchDescription = a.SearchDescription,
                        LongDescription = a.LongDescription,
                        BaseUnitOfMeasure = a.BaseUnitOfMeasure

                    }).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<OfferHeaderModel> LoadSetupPromotionHeader(string offerNo, string offerName, string approve, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.SetupPromotionHEADERs.Select(a => new OfferHeaderModel
                    {
                        No = a.BBYNR,
                        Description = a.BBYTEXT,
                        OfferType = a.BBYTYPE,
                        IsApprove = a.IsApprove ?? false
                    });

                    if (!string.IsNullOrEmpty(offerNo))
                    {
                        data = data.Where(m => m.No == offerNo);
                    }

                    if (!string.IsNullOrEmpty(offerName))
                    {
                        data = data.Where(m => m.Description.ToLower().Contains(offerName.ToLower()));
                    }

                    if (!string.IsNullOrEmpty(approve))
                    {
                        var isApprove = approve == "1" ? true : false;
                        data = data.Where(m => m.IsApprove == isApprove);
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.No).Skip(skip).Take(take).ToList();
                    return result;

                }
                catch (Exception ex)
                {
                    total = 0;
                }

                return new List<OfferHeaderModel>();
            }
        }

        public SetupMainModel ViewSetupMain()
        {
            var result = new SetupMainModel();
            var offerType = new List<OfferTypeModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    offerType = db.OfferTypes.Where(d => d.Enabled == true).Select(a => new OfferTypeModel
                    {
                        ID = a.ID,
                        OfferType1 = a.OfferType1,
                        OfferName = a.OfferName,
                        IsTotalBill = a.IsTotalBill,
                        IsSetupBuy = a.IsSetupBuy,
                        IsSetupGet = a.IsSetupGet,
                        IsVoucher = a.IsVoucher,
                        IsGift = a.IsGift,
                        Description = a.Description,
                        UserGuide = a.UserGuide
                    }).OrderBy(d => d.OfferType1).ToList();

                    var salesType = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypeModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();

                    result = new SetupMainModel
                    {
                        ListOfferType = offerType,
                        ListSalesType = salesType
                    };
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }

        public OfferHeaderModel GetOfferHeader(string bonusBuy)
        {
            var result = new OfferHeaderModel();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.OfferHeaders.Where(d => d.No == bonusBuy).Select(a => new OfferHeaderModel
                    {
                        No = a.No,
                        Type = a.SalesType,
                        Description = a.Description,
                        Status = a.Status,
                        OfferType = a.OfferType,
                        ConditionBuy = a.ConditionBuy,
                        ConditionGet = a.ConditionGet,
                        MemberOnly = a.MemberOnly,
                        StartingDate = a.StartingDate,
                        EndingDate = a.EndingDate,
                        FromTime = a.FromTime,
                        ToTime = a.ToTime,
                        Mon = a.Mon,
                        Tue = a.Tue,
                        Wed = a.Wed,
                        Thu = a.Thu,
                        Fri = a.Fri,
                        Sat = a.Sat,
                        Sun = a.Sun,
                        LimitQty = a.LimitQty,
                        LocalSiteGroup = a.LocalSiteGroup,
                        VoucherFromDate = a.VoucherFromDate,
                        VoucherToDate = a.VoucherToDate,
                        VoucherValidDay = a.VoucherValidDay,
                        VoucherLimitNumber = a.VoucherLimitNumber,
                        PriorityBBY = a.PriorityBBY,
                        NumOfDays = a.NumOfDays,
                        AmountTotalBill = db.OfferBenefits.FirstOrDefault(d => d.OfferNo == a.No) == null ? 0.0 : (double)db.OfferBenefits.FirstOrDefault(d => d.OfferNo == a.No).StepAmount
                    }).FirstOrDefault();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<OfferBuyModel> GetOfferBuy(string bonusBuy)
        {
            var result = new List<OfferBuyModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.OfferBuys.Where(d => d.OfferNo == bonusBuy).Select(a => new OfferBuyModel
                    {
                        OfferNo = a.OfferNo,
                        LineNo = a.LineType == 1 ? (string.IsNullOrEmpty(a.BonusBuyNo) ? a.LineNo : db.OfferBuys.Where(d => d.LineType == a.LineType && d.OfferNo == a.OfferNo && d.BonusBuyNo == a.BonusBuyNo).Max(d => d.LineNo)) : a.LineNo,
                        LineType = a.LineType,
                        No = a.LineType == 1 ? db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.BonusBuyNo).GroupCode : a.No,
                        Description = a.LineType == 1 ? db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.BonusBuyNo).GroupName : a.Description,
                        UnitOfMeasure = a.LineType == 1 ? "" : a.UnitOfMeasure,
                        DiscountType = a.DiscountType,
                        DiscountValue = a.DiscountValue,
                        Quantity = a.Quantity,
                        Step = a.Step,
                        BonusBuyNo = a.BonusBuyNo,
                        LineGroup = a.LineGroup,
                        ScaleType = a.ScaleType

                    }).Distinct().OrderByDescending(a => a.No).ThenByDescending(d => d.OfferNo).ToList();
                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public List<OfferGetModel> GetOfferGet(string offerNo, bool isTotalBill, bool isSetupGet)
        {
            var result = new List<OfferGetModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    if (isTotalBill && isSetupGet)
                    {
                        result = db.OfferBenefits.Where(d => d.OfferNo == offerNo).Select(a => new OfferGetModel
                        {
                            OfferNo = a.OfferNo,
                            LineNo = a.Type == 1 ? (string.IsNullOrEmpty(a.VariantCode) ? a.LineNo : db.OfferBenefits.Where(d => d.Type == a.Type && d.OfferNo == a.OfferNo && d.VariantCode == a.VariantCode).Max(d => d.LineNo)) : a.LineNo,
                            LineType = a.Type,
                            No = a.Type == 1 ? db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.VariantCode).GroupCode : a.No,
                            Description = a.Type == 1 ? db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.VariantCode).GroupName : a.Description,
                            UnitOfMeasure = a.Type == 1 ? "" : a.UnitOfMeasure,
                            DiscountType = a.ValueType,
                            DiscountValue = a.Value,
                            Quantity = a.Quantity,
                            Step = a.StepAmount,
                            BonusBuyNo = a.VariantCode,
                            LineGroup = a.LineGroup,
                            ScaleType = ""

                        }).Distinct().OrderByDescending(a => a.No).ThenByDescending(d => d.OfferNo).ToList();
                    }
                    else
                    {
                        var dbOfferGet = db.OfferGets.Where(d => d.OfferNo == offerNo);
                        result = dbOfferGet.Select(a => new OfferGetModel
                        {
                            OfferNo = a.OfferNo,
                            LineNo = a.LineType == 1 ? (string.IsNullOrEmpty(a.BonusBuyNo) ? a.LineNo : dbOfferGet.Where(d => d.LineType == a.LineType && d.BonusBuyNo == a.BonusBuyNo).Max(d => d.LineNo)) : a.LineNo,
                            LineType = a.LineType,
                            No = a.LineType == 1 ? db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.BonusBuyNo).GroupCode : a.No,
                            Description = a.LineType == 1 ? db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.BonusBuyNo).GroupName : a.Description,
                            UnitOfMeasure = a.LineType == 1 ? "" : a.UnitOfMeasure,
                            DiscountType = a.DiscountType,
                            DiscountValue = a.DiscountValue,
                            Quantity = a.Quantity,
                            Step = a.Step,
                            BonusBuyNo = a.BonusBuyNo,
                            LineGroup = a.LineGroup,
                            ScaleType = a.ScaleType

                        }).Distinct().OrderByDescending(a => a.No).ThenByDescending(d => d.OfferNo).ToList();
                    }
                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public List<OfferSiteModel> GetOfferSite(string bonusBuy)
        {
            var result = new List<OfferSiteModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.OfferSites.Where(d => d.OfferNo == bonusBuy).Select(a => new OfferSiteModel
                    {
                        OfferNo = a.OfferNo,
                        //StoreNo = a.StoreNo,
                        PriceGroupCode = a.PriceGroupCode,
                        GroupName = db.SetupGroupSites.FirstOrDefault(d => d.GroupCode == a.PriceGroupCode).GroupName
                    }).Distinct().ToList();
                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public Tuple<bool, string, OfferHeaderModel> UpdateOfferHeader(OfferHeaderModel model)
        {
            var dataModel = new OfferHeaderModel();
            var result = new Tuple<bool, string, OfferHeaderModel>(true, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    lock (lockRequest)
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var noNew = "6000000001";
                        var noAuto = db.OfferHeaders.Max(d => d.No);
                        var noSave = string.IsNullOrEmpty(noAuto) ? Convert.ToInt64(noNew) : Convert.ToInt64(noAuto) + 1;
                        var insert = new OfferHeader
                        {
                            No = "" + noSave,
                            Status = 0,
                            SalesType = "",
                            Description = model.Description,
                            OfferType = model.OfferType,
                            LastDateModified = model.LastDateModified,
                            ConditionBuy = model.ConditionBuy,
                            ConditionGet = model.ConditionGet,
                            MemberOnly = model.MemberOnly,
                            StartingDate = model.StartingDate,
                            EndingDate = model.EndingDate,
                            FromTime = "",// model.FromTime,
                            ToTime = "",// model.ToTime,
                            Mon = model.Mon,
                            Tue = model.Tue,
                            Wed = model.Wed,
                            Thu = model.Thu,
                            Fri = model.Fri,
                            Sat = model.Sat,
                            Sun = model.Sun,
                            NumOfDays = 0,// model.NumOfDays,
                            DayOfWeek = "",// model.DayOfWeek,
                            LimitQty = model.LimitQty,
                            LocalSiteGroup = "",// model.LocalSiteGroup,
                            VoucherFromDate = model.VoucherFromDate == null ? Convert.ToDateTime("1900-01-01") : model.VoucherFromDate,
                            VoucherToDate = model.VoucherToDate == null ? Convert.ToDateTime("9999-12-31") : model.VoucherToDate,
                            VoucherValidDay = model.VoucherValidDay,
                            VoucherLimitNumber = model.VoucherLimitNumber,
                            PriorityBBY = model.PriorityBBY,

                            PriceGroup = "",
                            RoundingMethod = "",
                            CurrencyCode = "",
                            ValidationPeriodID = "",
                            ValidationDescription = "",
                            BlockPeriodicDiscount = 0,
                            DealPrice = 0,
                            ShowDealLines = 0,
                            SalesTypeFilter = "",
                            SelectionType = 0,
                            CustomerDiscGroup = "",
                            MemberValue = "",
                            DiscountTrackingNo = "",
                            CouponCode = "",
                            CouponQtyNeeded = 0,
                            MemberType = 0,
                            MemberAttribute = "",
                            MemberAttributeValue = "",
                            BlockSalesCommission = 0,
                            BlockManualPriceChange = 0,
                            BlockInfoCodeDiscount = 0,
                            BlockTotalDiscountOffer = 0,
                            BlockTenderTypeDiscount = 0,
                            BlockMemberPoints = 0,
                            NoSeries = "",
                            TenderTypeCode = "",
                            TenderTypeValue = 0,
                            TenderTypeOfferPercent = 0,
                            TenderTypeOfferAmount = 0,
                            BankCode = "",
                            PromotionNo = "",
                            Counter = 0,
                            Pkey = "" + noSave
                        };
                        db.OfferHeaders.Add(insert);
                        model.No = "" + noSave;

                        db.SaveChanges();
                        dataModel = model;

                        result = new Tuple<bool, string, OfferHeaderModel>(true, "Success", dataModel);
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferHeaderModel>(false, ex.Message, dataModel);
                }
            }

            return result;
        }
        public Tuple<bool, string, OfferHeaderModel> InsertUpdateAdvance(OfferHeaderModel model)
        {
            var dataModel = new OfferHeaderModel();
            var result = new Tuple<bool, string, OfferHeaderModel>(true, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkNo = db.OfferHeaders.FirstOrDefault(d => d.No == model.No);
                    if (checkNo != null)
                    {
                        checkNo.LastDateModified = model.LastDateModified;
                        checkNo.MemberOnly = model.MemberOnly;
                        checkNo.FromTime = model.FromTime;
                        checkNo.ToTime = model.ToTime;
                        checkNo.Mon = model.Mon;
                        checkNo.Tue = model.Tue;
                        checkNo.Wed = model.Wed;
                        checkNo.Thu = model.Thu;
                        checkNo.Fri = model.Fri;
                        checkNo.Sat = model.Sat;
                        checkNo.Sun = model.Sun;
                        checkNo.NumOfDays = model.NumOfDays;
                        checkNo.DayOfWeek = model.DayOfWeek;
                        checkNo.LimitQty = model.LimitQty;
                        checkNo.VoucherFromDate = model.VoucherFromDate;
                        checkNo.VoucherToDate = model.VoucherToDate;
                        checkNo.VoucherValidDay = model.VoucherValidDay;
                        checkNo.VoucherLimitNumber = model.VoucherLimitNumber;
                        checkNo.PriorityBBY = model.PriorityBBY;

                        if (model.isTotalBill && model.AmountTotalBill > 0)
                        {
                            var checkOfferBenefit = db.OfferBenefits.Where(d => d.OfferNo == model.No).ToList();
                            if (checkOfferBenefit != null && checkOfferBenefit.Count > 0)
                            {
                                checkOfferBenefit.ForEach(x => { x.StepAmount = (float)model.AmountTotalBill; });
                            }
                            else
                            {
                                var maxLineNo = db.OfferBenefits.Where(d => d.OfferNo == model.No).Count() == 0 ? 1 : db.OfferBenefits.Where(d => d.OfferNo == model.No).Max(d => d.LineNo) + 1;
                                var insertOfferBenefit = new OfferBenefit
                                {
                                    OfferNo = model.No,
                                    LineNo = maxLineNo,
                                    No = "",
                                    VariantCode = "",
                                    Description = "",
                                    UnitOfMeasure = "",
                                    ValueType = model.OfferType == "ZB13" ? 0 : 0,
                                    Value = model.OfferType == "ZB13" ? 100 : 0,
                                    StepAmount = (float)model.AmountTotalBill,
                                    Quantity = 0,
                                    LineGroup = "",
                                    Type = 0,
                                    Counter = 0,
                                    Pkey = ""
                                };
                                db.OfferBenefits.Add(insertOfferBenefit);
                            }
                            //else
                            //{
                            //    var maxLineNo = db.OfferBenefits.Count() == 0 ? 1 : db.OfferBenefits.Max(d => d.LineNo) + 1;

                            //    var listOfferBuy = db.OfferBuys.Where(d => d.OfferNo == model.No && d.No != "").ToList();
                            //    if (listOfferBuy != null && listOfferBuy.Count > 0)
                            //    {
                            //        foreach (var a in listOfferBuy)
                            //        {
                            //            var insertOffBenefit = new OfferBenefit
                            //            {
                            //                OfferNo = a.OfferNo,
                            //                LineNo = maxLineNo,
                            //                No = a.No,
                            //                VariantCode = "",
                            //                Description = a.Description,
                            //                UnitOfMeasure = a.UnitOfMeasure,
                            //                ValueType = model.OfferType == "ZB13" ? 0 : a.DiscountType,
                            //                Value = model.OfferType == "ZB13" ? 100 : a.DiscountValue,
                            //                StepAmount = (decimal)model.AmountTotalBill,
                            //                Quantity = (int)a.Quantity,
                            //                LineGroup = a.LineGroup,
                            //                Type = 0,
                            //                Counter = 0,
                            //                Pkey = a.No
                            //            };
                            //            maxLineNo++;

                            //            db.OfferBenefits.Add(insertOffBenefit);
                            //        }
                            //    }
                            //    else
                            //    {
                            //        var insertOfferBenefit = new OfferBenefit
                            //        {
                            //            OfferNo = model.No,
                            //            LineNo = maxLineNo,
                            //            No = "",
                            //            VariantCode = "",
                            //            Description = "",
                            //            UnitOfMeasure = "",
                            //            ValueType = model.OfferType == "ZB13" ? 0 : 0,
                            //            Value = model.OfferType == "ZB13" ? 100 : 0,
                            //            StepAmount = (decimal)model.AmountTotalBill,
                            //            Quantity = 0,
                            //            LineGroup = "",
                            //            Type = 0,
                            //            Counter = 0,
                            //            Pkey = ""
                            //        };
                            //        db.OfferBenefits.Add(insertOfferBenefit);
                            //    }
                            //}
                        }

                        db.SaveChanges();
                        dataModel = model;

                        result = new Tuple<bool, string, OfferHeaderModel>(true, "Success", dataModel);
                    }
                    else
                    {
                        result = new Tuple<bool, string, OfferHeaderModel>(false, $"Không tìm thấy dữ liệu CTKM {model.No}", dataModel);
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferHeaderModel>(false, ex.Message, dataModel);
                }

            }

            return result;
        }

        public Tuple<bool, string> DeleteOfferBuy(string offerNo, int lineNo, int lineType, string groupCode)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (lineType == 0)
                    {
                        var checkID = db.OfferBuys.FirstOrDefault(d => d.OfferNo == offerNo && d.LineNo == lineNo);
                        if (checkID != null)
                        {
                            db.OfferBuys.Remove(checkID);
                            db.SaveChanges();
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và lineNo {lineNo} trong hệ thống");
                        }
                    }
                    else
                    {
                        var checkID = db.OfferBuys.Where(d => d.OfferNo == offerNo && d.BonusBuyNo == groupCode);
                        if (checkID != null)
                        {
                            db.OfferBuys.RemoveRange(checkID);
                            db.SaveChanges();
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và groupCode {groupCode} trong OfferBuys");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public Tuple<bool, string> DeleteOfferSite(string offerNo, string groupSite)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkID = db.OfferSites.Where(d => d.OfferNo == offerNo && d.PriceGroupCode == groupSite).ToList();
                    if (checkID != null && checkID.Count > 0)
                    {
                        db.OfferSites.RemoveRange(checkID);
                        db.SaveChanges();
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và groupSite {groupSite} trong OfferSite");
                    }

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public Tuple<bool, string> DeleteOfferGet(string offerNo, int lineNo, bool isTotalBill, bool isSetupGet, int lineType, string groupCode)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    if (isTotalBill && isSetupGet) // Table OfferBenefit
                    {
                        if (lineType == 0)
                        {
                            var checkID = db.OfferBenefits.FirstOrDefault(d => d.OfferNo == offerNo && d.LineNo == lineNo);
                            if (checkID != null)
                            {
                                db.OfferBenefits.Remove(checkID);
                                db.SaveChanges();
                            }
                            else
                            {
                                result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và lineNo {lineNo} trong hệ thống");
                            }
                        }
                        else
                        {
                            var checkID = db.OfferBenefits.Where(d => d.OfferNo == offerNo && d.VariantCode == groupCode);
                            if (checkID != null)
                            {
                                db.OfferBenefits.RemoveRange(checkID);
                                db.SaveChanges();
                            }
                            else
                            {
                                result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và groupCode {groupCode} trong OfferBenefit");
                            }
                        }
                    }
                    else // Table OfferGet
                    {
                        if (lineType == 0)
                        {
                            var checkID = db.OfferGets.FirstOrDefault(d => d.OfferNo == offerNo && d.LineNo == lineNo);
                            if (checkID != null)
                            {
                                db.OfferGets.Remove(checkID);
                                db.SaveChanges();
                            }
                            else
                            {
                                result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và lineNo {lineNo} trong hệ thống");
                            }
                        }
                        else
                        {
                            var checkID = db.OfferGets.Where(d => d.OfferNo == offerNo && d.BonusBuyNo == groupCode);
                            if (checkID != null)
                            {
                                db.OfferGets.RemoveRange(checkID);
                                db.SaveChanges();
                            }
                            else
                            {
                                result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và groupCode {groupCode} trong OfferGet");
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public Tuple<bool, string, OfferBuyModel> InsertUpdateOfferBuy(OfferBuyModel model, bool isTotalBill, bool isSetupGet, string conditionBuy, string offerType, string itemHidden)
        {
            var dataModel = new OfferBuyModel();
            var result = new Tuple<bool, string, OfferBuyModel>(true, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var offerBuy = db.OfferBuys.Where(d => d.OfferNo == model.OfferNo).ToList();
                    if (model.LineType == 0) // theo Item
                    {
                        if (isTotalBill && isSetupGet)
                        {
                            var checkGet = db.OfferBenefits.Any(d => d.OfferNo == model.OfferNo && d.No == model.No && model.No != "");
                            if (checkGet)
                            {
                                var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferBuyModel
                                {
                                    No = a.No,
                                    Description = a.Description,
                                    UnitOfMeasure = a.SalesUnitOfMeasure

                                }).FirstOrDefault();
                                return new Tuple<bool, string, OfferBuyModel>(false, $"CTKM {model.OfferNo} đã tồn tại item {model.No} trong sản phẩm khuyến mãi", infoItemHidden);
                            }
                        }
                        else
                        {
                            var checkGet = db.OfferGets.Any(d => d.OfferNo == model.OfferNo && d.No == model.No && model.No != "");
                            if (checkGet)
                            {
                                var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferBuyModel
                                {
                                    No = a.No,
                                    Description = a.Description,
                                    UnitOfMeasure = a.SalesUnitOfMeasure

                                }).FirstOrDefault();
                                return new Tuple<bool, string, OfferBuyModel>(false, $"CTKM {model.OfferNo} đã tồn tại item {model.No} trong sản phẩm khuyến mãi", infoItemHidden);
                            }
                        }

                        if (itemHidden.Trim() != model.No.Trim())
                        {
                            var checkNoBuy = offerBuy.Where(d => d.No == model.No).ToList();
                            if (checkNoBuy != null && checkNoBuy.Count > 0)
                            {
                                var groupCode = checkNoBuy.FirstOrDefault().BonusBuyNo;
                                if (!string.IsNullOrEmpty(groupCode))
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferBuyModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại item {model.No} trong group {groupCode}", infoItemHidden);
                                }
                                else
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferBuyModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại item {model.No} trong CTKM", infoItemHidden);
                                }
                            }
                        }


                        var checkNo = offerBuy.FirstOrDefault(d => d.LineNo == model.LineNo);

                        if (checkNo != null)
                        {
                            checkNo.LineType = model.LineType;
                            checkNo.No = model.No;
                            checkNo.Description = model.Description == null ? "" : model.Description;
                            checkNo.UnitOfMeasure = model.UnitOfMeasure;
                            checkNo.DiscountType = model.DiscountType;
                            checkNo.DiscountValue = model.DiscountValue;
                            checkNo.Quantity = model.Quantity;
                            checkNo.Step = model.Quantity;
                            checkNo.BonusBuyNo = model.BonusBuyNo == null ? "" : model.BonusBuyNo;
                            checkNo.ScaleType = model.ScaleType;
                            //checkNo.LineGroup = lineGroup;
                            checkNo.Pkey = model.No;
                        }
                        else
                        {
                            var maxLineNo = offerBuy.Count() == 0 ? 1 : offerBuy.Max(d => d.LineNo) + 1;
                            var lineGroup = conditionBuy == "OR" ? "B1" : "B" + maxLineNo;

                            var insert = new OfferBuy
                            {
                                OfferNo = model.OfferNo,
                                LineNo = maxLineNo,
                                LineType = maxLineNo > 1 ? offerBuy.FirstOrDefault().LineType : model.LineType,
                                No = model.No,
                                Description = model.Description,
                                UnitOfMeasure = model.UnitOfMeasure,
                                DiscountType = model.DiscountType,
                                DiscountValue = model.DiscountValue,
                                Quantity = model.Quantity,
                                Step = model.Step,
                                BonusBuyNo = model.BonusBuyNo,
                                LineGroup = lineGroup,
                                ScaleType = model.ScaleType,
                                Counter = model.Counter,
                                Pkey = model.No
                            };
                            db.OfferBuys.Add(insert);

                            //var checkOfferHeader = db.OfferHeaders.FirstOrDefault(d => d.No == model.OfferNo);
                            //if (checkOfferHeader != null)
                            //{
                            //    var conditionBuySave = conditionBuy == "OR" ? 1 : 2;
                            //    checkOfferHeader.ConditionBuy = conditionBuySave;
                            //}

                            model = new OfferBuyModel
                            {
                                OfferNo = insert.OfferNo,
                                LineNo = insert.LineNo,
                                LineType = insert.LineType,
                                No = insert.No,
                                Description = insert.Description,
                                UnitOfMeasure = insert.UnitOfMeasure,
                                DiscountType = insert.DiscountType,
                                DiscountValue = insert.DiscountValue,
                                Quantity = insert.Quantity,
                                LineGroup = insert.LineGroup,
                                ScaleType = insert.ScaleType
                            };
                        }
                        db.SaveChanges();
                        dataModel = model;

                        result = new Tuple<bool, string, OfferBuyModel>(true, "Success", dataModel);
                    }
                    else // Theo group Item
                    {
                        if (string.IsNullOrEmpty(model.GroupCode))
                        {
                            var checkNo = offerBuy.FirstOrDefault(d => d.LineNo == model.LineNo && d.No == "");
                            if (checkNo != null)
                            {
                                checkNo.LineType = model.LineType;

                                db.SaveChanges();
                                result = new Tuple<bool, string, OfferBuyModel>(true, "Success", null);
                            }

                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(model.GroupCode))//List GroupCode (trước cho chọn nhiều groupCode)
                            {
                                var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                var checkGroup = offerBuy.Any(d => d.BonusBuyNo == listGroup.FirstOrDefault());
                                if (checkGroup)
                                {
                                    return result = new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm mua", null);
                                }
                                if (isTotalBill && isSetupGet)
                                {
                                    var checkGroupOfferBenefit = db.OfferBenefits.Any(d => d.VariantCode == listGroup.FirstOrDefault());
                                    if (checkGroupOfferBenefit)
                                    {
                                        return result = new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm khuyến mãi", null);
                                    }
                                }
                                else
                                {
                                    var checkGroupOfferGet = db.OfferGets.Any(d => d.BonusBuyNo == listGroup.FirstOrDefault());
                                    if (checkGroupOfferGet)
                                    {
                                        return result = new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm khuyến mãi", null);
                                    }
                                }
                            }

                            if (model.GroupCode == model.BonusBuyNo)//Update các thông số
                            {
                                var checkGroupCode = offerBuy.Where(d => d.BonusBuyNo == model.BonusBuyNo).ToList();
                                if (checkGroupCode != null && checkGroupCode.Count > 0)
                                {

                                    checkGroupCode.ForEach(x =>
                                    {
                                        x.DiscountType = model.DiscountType;
                                        x.DiscountValue = model.DiscountValue;
                                        x.Quantity = model.Quantity;
                                        x.Step = model.Quantity;
                                        x.ScaleType = model.ScaleType;

                                    });

                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferBuyModel>(true, "Success", null);
                                }
                            }
                            else // insert list item vào OfferBuy
                            {
                                var offerBuyOld = offerBuy.Where(d => d.BonusBuyNo == model.BonusBuyNo);
                                if (offerBuyOld != null && offerBuyOld.ToList().Count > 0)
                                {
                                    db.OfferBuys.RemoveRange(offerBuyOld);
                                }
                                var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                if (listGroup.Count > 0)
                                {
                                    var maxLineNo = offerBuy.Count() == 0 ? 1 : offerBuy.Max(d => d.LineNo) + 1;
                                    var maxLine = maxLineNo;

                                    var lineGroup = offerBuy.FirstOrDefault(d => d.LineNo == model.LineNo).LineGroup;
                                    var listItemBuyGroup = new List<string>();

                                    foreach (var item in listGroup)
                                    {
                                        //var lineGroup = //conditionBuy == "OR" ? "B1" : "B" + maxLineGroup;
                                        var checkGroupBuy = db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == item);
                                        if (checkGroupBuy == null)
                                        {
                                            //return new Tuple<bool, string, OfferBuyModel>(false, $"Không tồn tại group này {model.GroupCode} trong hệ thống", null);
                                        }
                                        else
                                        {
                                            var listItemGroup = JsonConvert.DeserializeObject<List<string>>(checkGroupBuy.ListItemNo);

                                            var listItemInsert = db.Items.Where(d => listItemGroup.Contains(d.No)).ToList();
                                            foreach (var itemInsert in listItemInsert)
                                            {
                                                var checkFirst = offerBuy.FirstOrDefault(d => d.LineNo == model.LineNo);
                                                if (string.IsNullOrEmpty(checkFirst.No))
                                                {
                                                    checkFirst.LineType = model.LineType;
                                                    checkFirst.No = itemInsert.No;
                                                    checkFirst.Description = itemInsert.Description;
                                                    checkFirst.UnitOfMeasure = itemInsert.BaseUnitOfMeasure;
                                                    checkFirst.DiscountType = model.DiscountType;
                                                    checkFirst.DiscountValue = model.DiscountValue;
                                                    checkFirst.Quantity = model.Quantity;
                                                    checkFirst.Step = model.Quantity;
                                                    checkFirst.BonusBuyNo = item;
                                                    checkFirst.ScaleType = model.ScaleType;
                                                    checkFirst.Pkey = itemInsert.No;
                                                }
                                                else
                                                {

                                                    var checkItem = offerBuy.FirstOrDefault(d => d.No == itemInsert.No && d.BonusBuyNo == item);
                                                    if (checkItem != null)
                                                    {
                                                        checkItem.Description = itemInsert.Description;
                                                        checkItem.UnitOfMeasure = itemInsert.BaseUnitOfMeasure;
                                                        checkItem.DiscountType = model.DiscountType;
                                                        checkItem.DiscountValue = model.DiscountValue;
                                                        checkItem.Quantity = model.Quantity;
                                                        checkItem.Step = model.Quantity;
                                                        checkItem.ScaleType = model.ScaleType;
                                                        checkItem.BonusBuyNo = item;
                                                        checkItem.Pkey = itemInsert.No;
                                                    }
                                                    else
                                                    {
                                                        var insert = new OfferBuy
                                                        {
                                                            OfferNo = model.OfferNo,
                                                            LineNo = maxLine,
                                                            LineType = model.LineType,
                                                            No = itemInsert.No,
                                                            Description = itemInsert.Description,
                                                            UnitOfMeasure = itemInsert.BaseUnitOfMeasure,
                                                            DiscountType = model.DiscountType,
                                                            DiscountValue = model.DiscountValue,
                                                            Quantity = model.Quantity,
                                                            Step = model.Step,
                                                            BonusBuyNo = item,
                                                            LineGroup = lineGroup,
                                                            ScaleType = model.ScaleType,
                                                            Counter = 0,
                                                            Pkey = itemInsert.No
                                                        };
                                                        db.OfferBuys.Add(insert);
                                                        maxLine++;
                                                    }

                                                }
                                            }
                                        }
                                        //maxLineGroup++;
                                    }

                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferBuyModel>(true, "Success", null);
                                }
                                else
                                {
                                    return new Tuple<bool, string, OfferBuyModel>(false, $"Vui lòng chọn nhóm item", null);
                                }
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferBuyModel>(false, ex.Message, dataModel);
                }
            }

            return result;
        }

        public Tuple<bool, string, OfferGetModel> InsertUpdateOfferGetOrBenefit(OfferGetModel model, bool isTotalBill, bool isSetupGet, string conditionGet, string offerType, string itemHidden)
        {
            var dataModel = new OfferGetModel();
            var result = new Tuple<bool, string, OfferGetModel>(true, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (model.LineType == 0) // theo Item
                    {
                        if (isTotalBill && isSetupGet) // lưu vào OfferBenefit
                        {
                            var checkGet = db.OfferBuys.Any(d => d.OfferNo == model.OfferNo && d.No == model.No && model.No != "");
                            if (checkGet)
                            {
                                var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferGetModel
                                {
                                    No = a.No,
                                    Description = a.Description,
                                    UnitOfMeasure = a.SalesUnitOfMeasure

                                }).FirstOrDefault();
                                return new Tuple<bool, string, OfferGetModel>(false, $"CTKM {model.OfferNo} đã tồn tại item {model.No} trong sản phẩm mua", infoItemHidden);
                            }

                            if (itemHidden.Trim() != model.No.Trim())
                            {
                                var checkNoBuy = db.OfferBenefits.Any(d => d.OfferNo == model.OfferNo && d.No == model.No);
                                if (checkNoBuy)
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferGetModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại item {model.No} trong CTKM", infoItemHidden);
                                }
                            }

                            var checkNo = db.OfferBenefits.FirstOrDefault(d => d.OfferNo == model.OfferNo && d.LineNo == model.LineNo);
                            if (checkNo != null)
                            {
                                checkNo.Type = model.LineType;
                                checkNo.No = model.No;
                                checkNo.Description = model.Description == null ? "" : model.Description;

                                checkNo.UnitOfMeasure = model.UnitOfMeasure;
                                checkNo.ValueType = model.DiscountType;
                                checkNo.Value = model.DiscountValue;
                                checkNo.Quantity = (int)model.Quantity;
                                checkNo.StepAmount = model.Step;
                                checkNo.Pkey = model.No;

                            }
                            else
                            {
                                var maxLineNo = db.OfferBenefits.Where(d => d.OfferNo == model.OfferNo).Count() == 0 ? 1 : db.OfferBenefits.Where(d => d.OfferNo == model.OfferNo).Max(d => d.LineNo) + 1;
                                var lineGroup = conditionGet == "OR" ? "B1" : "B" + maxLineNo;

                                var insert = new OfferBenefit
                                {
                                    OfferNo = model.OfferNo,
                                    LineNo = maxLineNo,
                                    Type = model.LineType,
                                    No = model.No,
                                    Description = model.Description,
                                    VariantCode = "",
                                    UnitOfMeasure = model.UnitOfMeasure,
                                    ValueType = model.DiscountType,
                                    Value = model.DiscountValue,
                                    Quantity = (int)model.Quantity,
                                    StepAmount = model.Step,
                                    LineGroup = lineGroup,
                                    Counter = model.Counter,
                                    Pkey = model.No
                                };
                                db.OfferBenefits.Add(insert);

                                model = new OfferGetModel
                                {
                                    OfferNo = insert.OfferNo,
                                    LineNo = insert.LineNo,
                                    LineType = insert.Type,
                                    No = insert.No,
                                    Description = insert.Description,
                                    UnitOfMeasure = insert.UnitOfMeasure,
                                    DiscountType = insert.ValueType,
                                    DiscountValue = insert.Value,
                                    Quantity = insert.Quantity,
                                    LineGroup = insert.LineGroup,
                                    ScaleType = "",
                                    Step = insert.StepAmount
                                };
                            }
                        }
                        else // lưu vào OfferGet
                        {
                            var checkGet = db.OfferBuys.Any(d => d.OfferNo == model.OfferNo && d.No == model.No && model.No != "");
                            if (checkGet)
                            {
                                var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferGetModel
                                {
                                    No = a.No,
                                    Description = a.Description,
                                    UnitOfMeasure = a.SalesUnitOfMeasure

                                }).FirstOrDefault();
                                return new Tuple<bool, string, OfferGetModel>(false, $"CTKM {model.OfferNo} đã tồn tại item {model.No} trong sản phẩm mua", infoItemHidden);
                            }

                            if (itemHidden.Trim() != model.No.Trim())
                            {
                                var checkNoBuy = db.OfferGets.Any(d => d.OfferNo == model.OfferNo && d.No == model.No);
                                if (checkNoBuy)
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferGetModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại item {model.No} trong CTKM", infoItemHidden);
                                }
                            }

                            var checkNo = db.OfferGets.FirstOrDefault(d => d.OfferNo == model.OfferNo && d.LineNo == model.LineNo);
                            if (checkNo != null)
                            {
                                checkNo.LineType = model.LineType;
                                checkNo.No = model.No;
                                checkNo.Description = model.Description == null ? "" : model.Description;
                                checkNo.UnitOfMeasure = model.UnitOfMeasure;
                                checkNo.DiscountType = model.DiscountType;
                                checkNo.DiscountValue = model.DiscountValue;
                                checkNo.Quantity = model.Quantity;
                                checkNo.Step = model.Quantity;
                                checkNo.BonusBuyNo = model.BonusBuyNo == null ? "" : model.BonusBuyNo;
                                checkNo.ScaleType = model.ScaleType;
                                checkNo.Pkey = model.No;


                            }
                            else
                            {
                                var maxLineNo = db.OfferGets.Where(d => d.OfferNo == model.OfferNo).Count() == 0 ? 1 : db.OfferGets.Where(d => d.OfferNo == model.OfferNo).Max(d => d.LineNo) + 1;
                                var lineGroup = conditionGet == "OR" ? "B1" : "B" + maxLineNo;

                                var insert = new OfferGet
                                {
                                    OfferNo = model.OfferNo,
                                    LineNo = maxLineNo,
                                    LineType = model.LineType,
                                    No = model.No,
                                    Description = model.Description,
                                    UnitOfMeasure = model.UnitOfMeasure,
                                    DiscountType = model.DiscountType,
                                    DiscountValue = model.DiscountValue,
                                    Quantity = model.Quantity,
                                    Step = model.Step,
                                    BonusBuyNo = model.BonusBuyNo,
                                    LineGroup = lineGroup,
                                    ScaleType = model.ScaleType,
                                    Counter = model.Counter,
                                    Pkey = model.No
                                };
                                db.OfferGets.Add(insert);


                                //var checkOfferHeader = db.OfferHeaders.FirstOrDefault(d => d.No == model.OfferNo);
                                //if (checkOfferHeader != null)
                                //{
                                //    var conditionBuy = model.ConditionBuy == "OR" ? 1 : 2;
                                //    checkOfferHeader.ConditionBuy = conditionBuy;
                                //}

                                model = new OfferGetModel
                                {
                                    OfferNo = insert.OfferNo,
                                    LineNo = insert.LineNo,
                                    LineType = insert.LineType,
                                    No = insert.No,
                                    Description = insert.Description,
                                    UnitOfMeasure = insert.UnitOfMeasure,
                                    DiscountType = insert.DiscountType,
                                    DiscountValue = insert.DiscountValue,
                                    Quantity = insert.Quantity,
                                    LineGroup = insert.LineGroup,
                                    ScaleType = insert.ScaleType
                                };
                            }
                        }
                    }
                    else//Theo group =1
                    {
                        if (isTotalBill && isSetupGet)
                        {
                            var offerBenefit = db.OfferBenefits.Where(d => d.OfferNo == model.OfferNo);

                            if (string.IsNullOrEmpty(model.GroupCode))
                            {
                                var checkNo = offerBenefit.FirstOrDefault(d => d.LineNo == model.LineNo && d.No == "");
                                if (checkNo != null)
                                {
                                    checkNo.Type = model.LineType;

                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(model.GroupCode))//List GroupCode (trước cho chọn nhiều groupCode)
                                {
                                    var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                    var checkGroup = offerBenefit.Any(d => d.VariantCode == listGroup.FirstOrDefault());
                                    if (checkGroup)
                                    {
                                        return result = new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm khuyến mãi", null);
                                    }
                                    else
                                    {
                                        var checkGroupOfferBuy = db.OfferBuys.Any(d => d.BonusBuyNo == listGroup.FirstOrDefault());
                                        if (checkGroupOfferBuy)
                                        {
                                            return result = new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm mua", null);
                                        }
                                    }
                                }

                                if (model.GroupCode == model.BonusBuyNo)//Update các thông số
                                {
                                    var checkGroupCode = offerBenefit.Where(d => d.VariantCode == model.BonusBuyNo).ToList();
                                    if (checkGroupCode != null && checkGroupCode.Count > 0)
                                    {
                                        checkGroupCode.ForEach(x =>
                                        {
                                            x.ValueType = model.DiscountType;
                                            x.Value = model.DiscountValue;
                                            x.Quantity = (int)model.Quantity;
                                            x.StepAmount = model.Step;
                                        });

                                        db.SaveChanges();
                                        result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                                    }

                                }
                                else
                                {
                                    var offerBenefitOld = offerBenefit.Where(d => d.VariantCode == model.BonusBuyNo);
                                    if (offerBenefitOld != null && offerBenefitOld.ToList().Count > 0)
                                    {
                                        db.OfferBenefits.RemoveRange(offerBenefitOld);
                                    }
                                    var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                    if (listGroup.Count > 0)
                                    {
                                        var maxLineNo = offerBenefit.Count() == 0 ? 1 : offerBenefit.Max(d => d.LineNo) + 1;

                                        var maxLine = maxLineNo;
                                        var listItemBuyGroup = new List<string>();
                                        var lineGroup = offerBenefit.FirstOrDefault(d => d.LineNo == model.LineNo).LineGroup;
                                        foreach (var item in listGroup)
                                        {
                                            //var lineGroup = //conditionBuy == "OR" ? "B1" : "B" + maxLineGroup;
                                            var checkGroupBuy = db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == item);
                                            if (checkGroupBuy == null)
                                            {
                                            }
                                            else
                                            {
                                                var listItemGroup = JsonConvert.DeserializeObject<List<string>>(checkGroupBuy.ListItemNo);
                                                var listItemInsert = db.Items.Where(d => listItemGroup.Contains(d.No)).ToList();

                                                foreach (var itemInsert in listItemInsert)
                                                {
                                                    var checkFirst = offerBenefit.FirstOrDefault(d => d.LineNo == model.LineNo);
                                                    if (string.IsNullOrEmpty(checkFirst.No))
                                                    {
                                                        checkFirst.Type = model.LineType;
                                                        checkFirst.No = itemInsert.No;
                                                        checkFirst.Description = itemInsert.Description;
                                                        checkFirst.UnitOfMeasure = itemInsert.BaseUnitOfMeasure;
                                                        checkFirst.ValueType = model.DiscountType;
                                                        checkFirst.Value = model.DiscountValue;
                                                        checkFirst.Quantity = (int)model.Quantity;
                                                        checkFirst.StepAmount = model.Step;
                                                        checkFirst.VariantCode = item;
                                                        checkFirst.Pkey = itemInsert.No;
                                                    }
                                                    else
                                                    {
                                                        var checkItem = offerBenefit.FirstOrDefault(d => d.No == itemInsert.No && d.VariantCode == item);
                                                        if (checkItem != null)
                                                        {
                                                            checkItem.Description = itemInsert.Description;
                                                            checkItem.UnitOfMeasure = itemInsert.BaseUnitOfMeasure;
                                                            checkItem.ValueType = model.DiscountType;
                                                            checkItem.Value = model.DiscountValue;
                                                            checkItem.Quantity = (int)model.Quantity;
                                                            checkItem.StepAmount = model.Step;
                                                            checkItem.VariantCode = item;
                                                            checkItem.Pkey = itemInsert.No;
                                                        }
                                                        else
                                                        {
                                                            var insert = new OfferBenefit
                                                            {
                                                                OfferNo = model.OfferNo,
                                                                LineNo = maxLine,
                                                                Type = model.LineType,
                                                                No = itemInsert.No,
                                                                Description = itemInsert.Description,
                                                                UnitOfMeasure = itemInsert.BaseUnitOfMeasure,
                                                                ValueType = model.DiscountType,
                                                                Value = model.DiscountValue,
                                                                Quantity = (int)model.Quantity,
                                                                StepAmount = model.Step,
                                                                VariantCode = item,
                                                                LineGroup = lineGroup,
                                                                Counter = 0,
                                                                Pkey = itemInsert.No
                                                            };
                                                            db.OfferBenefits.Add(insert);
                                                            maxLine++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else // OfferGet
                        {
                            var offerGet = db.OfferGets.Where(d => d.OfferNo == model.OfferNo);
                            if (string.IsNullOrEmpty(model.GroupCode))
                            {
                                var checkNo = offerGet.FirstOrDefault(d => d.LineNo == model.LineNo && d.No == "");
                                if (checkNo != null)
                                {
                                    checkNo.LineType = model.LineType;
                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(model.GroupCode))//List GroupCode (trước cho chọn nhiều groupCode)
                                {
                                    var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                    var checkGroup = offerGet.Any(d => d.BonusBuyNo == listGroup.FirstOrDefault());
                                    if (checkGroup)
                                    {
                                        return result = new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm khuyến mãi", null);
                                    }
                                    else
                                    {
                                        var checkGroupOfferBuy = db.OfferBuys.Any(d => d.BonusBuyNo == listGroup.FirstOrDefault());
                                        if (checkGroupOfferBuy)
                                        {
                                            return result = new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm mua", null);
                                        }
                                    }
                                }
                                if (model.GroupCode == model.BonusBuyNo)//Update các thông số
                                {
                                    var checkGroupCode = offerGet.Where(d => d.BonusBuyNo == model.BonusBuyNo).ToList();
                                    if (checkGroupCode != null && checkGroupCode.Count > 0)
                                    {
                                        checkGroupCode.ForEach(x =>
                                        {
                                            x.DiscountType = model.DiscountType;
                                            x.DiscountValue = model.DiscountValue;
                                            x.Quantity = model.Quantity;
                                            x.Step = model.Quantity;
                                            x.ScaleType = model.ScaleType;
                                        });

                                        db.SaveChanges();
                                        result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                                    }

                                }
                                else // insert list item vào OfferBuy
                                {
                                    var offerBuyOld = offerGet.Where(d => d.BonusBuyNo == model.BonusBuyNo);
                                    if (offerBuyOld != null && offerBuyOld.ToList().Count > 0)
                                    {
                                        db.OfferGets.RemoveRange(offerBuyOld);
                                    }
                                    var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                    if (listGroup.Count > 0)
                                    {
                                        var maxLineNo = offerGet.Count() == 0 ? 1 : offerGet.Max(d => d.LineNo) + 1;
                                        var maxLine = maxLineNo;

                                        var lineGroup = offerGet.FirstOrDefault(d => d.LineNo == model.LineNo).LineGroup;
                                        var listItemBuyGroup = new List<string>();

                                        foreach (var item in listGroup)
                                        {
                                            //var lineGroup = //conditionBuy == "OR" ? "B1" : "B" + maxLineGroup;
                                            var checkGroupBuy = db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == item);
                                            if (checkGroupBuy == null)
                                            {
                                                //return new Tuple<bool, string, OfferBuyModel>(false, $"Không tồn tại group này {model.GroupCode} trong hệ thống", null);
                                            }
                                            else
                                            {
                                                var listItemGroup = JsonConvert.DeserializeObject<List<string>>(checkGroupBuy.ListItemNo);

                                                var listItemInsert = db.Items.Where(d => listItemGroup.Contains(d.No)).ToList();
                                                foreach (var itemInsert in listItemInsert)
                                                {
                                                    var checkFirst = offerGet.FirstOrDefault(d => d.LineNo == model.LineNo);
                                                    if (string.IsNullOrEmpty(checkFirst.No))
                                                    {
                                                        checkFirst.LineType = model.LineType;
                                                        checkFirst.No = itemInsert.No;
                                                        checkFirst.Description = itemInsert.Description;
                                                        checkFirst.UnitOfMeasure = itemInsert.BaseUnitOfMeasure;
                                                        checkFirst.DiscountType = model.DiscountType;
                                                        checkFirst.DiscountValue = model.DiscountValue;
                                                        checkFirst.Quantity = model.Quantity;
                                                        checkFirst.Step = model.Quantity;
                                                        checkFirst.BonusBuyNo = item;
                                                        checkFirst.ScaleType = model.ScaleType;
                                                        checkFirst.Pkey = itemInsert.No;
                                                    }
                                                    else
                                                    {

                                                        var checkItem = offerGet.FirstOrDefault(d => d.No == itemInsert.No && d.BonusBuyNo == item);
                                                        if (checkItem != null)
                                                        {
                                                            checkItem.Description = itemInsert.Description;
                                                            checkItem.UnitOfMeasure = itemInsert.BaseUnitOfMeasure;
                                                            checkItem.DiscountType = model.DiscountType;
                                                            checkItem.DiscountValue = model.DiscountValue;
                                                            checkItem.Quantity = model.Quantity;
                                                            checkItem.Step = model.Quantity;
                                                            checkItem.ScaleType = model.ScaleType;
                                                            checkItem.BonusBuyNo = item;
                                                            checkItem.Pkey = itemInsert.No;
                                                        }
                                                        else
                                                        {
                                                            var insert = new OfferBuy
                                                            {
                                                                OfferNo = model.OfferNo,
                                                                LineNo = maxLine,
                                                                LineType = model.LineType,
                                                                No = itemInsert.No,
                                                                Description = itemInsert.Description,
                                                                UnitOfMeasure = itemInsert.BaseUnitOfMeasure,
                                                                DiscountType = model.DiscountType,
                                                                DiscountValue = model.DiscountValue,
                                                                Quantity = model.Quantity,
                                                                Step = model.Step,
                                                                BonusBuyNo = item,
                                                                LineGroup = lineGroup,
                                                                ScaleType = model.ScaleType,
                                                                Counter = 0,
                                                                Pkey = itemInsert.No
                                                            };
                                                            db.OfferBuys.Add(insert);
                                                            maxLine++;
                                                        }

                                                    }
                                                }
                                            }
                                            //maxLineGroup++;
                                        }

                                        db.SaveChanges();
                                        result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                                    }
                                    else
                                    {
                                        return new Tuple<bool, string, OfferGetModel>(false, $"Vui lòng chọn nhóm item", null);
                                    }
                                }
                            }
                        }
                    }

                    db.SaveChanges();
                    dataModel = model;

                    result = new Tuple<bool, string, OfferGetModel>(true, "Success", dataModel);
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferGetModel>(false, ex.Message, dataModel);
                }
            }

            return result;
        }

        public List<ItemSetupMode> ListItemSetup(string itemNo, string description, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.Items.Select(a => new ItemSetupMode
                    {
                        No = a.No,
                        Description = a.Description,
                        UOM = a.SalesUnitOfMeasure

                    });
                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        data = data.Where(m => m.No == itemNo);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        data = data.Where(m => m.Description.ToLower().Contains(description.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderBy(d => d.No).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }

                return new List<ItemSetupMode>();
            }
        }
        public Tuple<bool, string> SetupGroupSite(GroupSiteModel model)
        {
            var result = new Tuple<bool, string>(true, "");
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkGroup = db.SetupGroupSites.FirstOrDefault(d => d.GroupCode == model.GroupCode);
                    if (checkGroup != null)
                    {
                        checkGroup.GroupName = model.GroupName;
                        // checkGroup.ListStore = model.ListStore;
                        checkGroup.LastUpdateDate = model.LastUpdateDate;
                        checkGroup.LastUpdateUser = model.LastUpdateUser;
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Cập nhật thành công group {model.GroupCode}");
                    }
                    else
                    {
                        var storeList = db.Stores.ToList();
                        if (model.ListStore == "ALL")
                        {
                            var insert = new SetupGroupSite
                            {
                                GroupCode = model.GroupCode,
                                GroupName = model.GroupName,
                                CreatedUser = model.CreatedUser,
                                CreatedDate = model.CreatedDate,
                                ListStore = model.ListStore,
                                Status = true
                            };
                            db.SetupGroupSites.Add(insert);
                            db.SaveChanges();
                            result = new Tuple<bool, string>(true, $"Setup thành công group {model.GroupCode} với {storeList.Count} cửa hàng");
                        }
                        else
                        {
                            var listStoreSetup = JsonConvert.DeserializeObject<List<string>>(model.ListStore);

                            var listStoreExists = listStoreSetup.Where(d => storeList.Any(a => a.No == d.ToString())).ToList();
                            var listStoretExists = listStoreSetup.Where(d => !storeList.Any(a => a.No == d.ToString())).ToList();

                            // var messageStoreError = listStoretExists.Count > 0 ? $" và thất bại với cửa hàng ({string.Join(",", listStoretExists) }) do không tồn tại trong hệ thống" : "";

                            if (listStoretExists.Count > 0)
                            {
                                return new Tuple<bool, string>(false, $"Cửa hàng ({string.Join(",", listStoretExists) }) không tồn tại trong hệ thống, vui lòng kiểm tra lại");
                            }

                            var insert = new SetupGroupSite
                            {
                                GroupCode = model.GroupCode,
                                GroupName = model.GroupName,
                                CreatedUser = model.CreatedUser,
                                CreatedDate = model.CreatedDate,
                                ListStore = JsonConvert.SerializeObject(listStoreExists),// model.ListStore,
                                Status = true
                            };
                            db.SetupGroupSites.Add(insert);
                            db.SaveChanges();
                            result = new Tuple<bool, string>(true, $"Setup thành công group {model.GroupCode} với {listStoreExists.Count} cửa hàng");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }

            return result;
        }

        public Tuple<bool, string> SetupGroupBuyItem(BuyGroupItemModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkGroup = db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == model.GroupCode);
                    if (checkGroup != null)
                    {
                        checkGroup.GroupName = model.GroupName;
                        // checkGroup.ListItemNo = model.ListItemNo;
                        checkGroup.LastUpdateDate = model.LastUpdateDate;
                        checkGroup.LastUpdateUser = model.LastUpdateUser;
                        result = new Tuple<bool, string>(true, $"Chỉnh sửa thành công group {model.GroupCode}");

                        db.SaveChanges();
                    }
                    else
                    {
                        var test = JsonConvert.SerializeObject(model.ListItemNo);
                        var itemList = db.Items.ToList();
                        var listItemNoSetup = JsonConvert.DeserializeObject<List<string>>(model.ListItemNo).Where(d => !string.IsNullOrEmpty(d)).ToList();

                        var listItemExists = listItemNoSetup.Where(d => itemList.Any(a => a.No == d.ToString())).ToList();
                        var listItemNotExists = listItemNoSetup.Where(d => !itemList.Any(a => a.No == d.ToString())).ToList();

                        // var messageItemError = listItemNotExists.Count > 0 ? $" và thất bại với sản phẩm ({string.Join(",", listItemNotExists) }) do không tồn tại trong hệ thống" : "";

                        if (listItemNotExists.Count > 0)
                        {
                            return new Tuple<bool, string>(false, $"Sản phẩm ({string.Join(",", listItemNotExists) }) không tồn tại trong hệ thống, vui lòng kiểm tra lại");
                        }

                        var insert = new SetupGroupItem
                        {
                            GroupCode = model.GroupCode,
                            GroupName = model.GroupName,
                            CreatedUser = model.CreatedUser,
                            CreatedDate = model.CreatedDate,
                            ListItemNo = JsonConvert.SerializeObject(listItemExists),// model.ListItemNo,
                            Status = true
                        };
                        db.SetupGroupItems.Add(insert);
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Setup thành công group {model.GroupCode} với {listItemExists.Count} sản phẩm");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }

            return result;
        }

        public List<GroupSiteModel> ListGroupSiteSetup(string groupCode, string groupName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.SetupGroupSites.Where(d => d.Status == true).Select(a => new GroupSiteModel
                    {
                        GroupCode = a.GroupCode,
                        GroupName = a.GroupName,
                        ListStore = a.ListStore,
                        Status = a.Status,
                        CreatedDate = a.CreatedDate,
                        CreatedUser = a.CreatedUser,
                        LastUpdateDate = a.LastUpdateDate,
                        LastUpdateUser = a.LastUpdateUser

                    });
                    if (!string.IsNullOrEmpty(groupCode))
                    {
                        data = data.Where(m => m.GroupCode == groupCode);
                    }
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        data = data.Where(m => m.GroupName.ToLower().Contains(groupName.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }

                return new List<GroupSiteModel>();
            }
        }

        public List<StoreSetupModel> ListStoreSetup(string groupCode, string storeNo, string storeName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                var result = new List<StoreSetupModel>();
                try
                {
                    db.Database.CommandTimeout = 120;
                    var checkData = db.SetupGroupSites.Where(d => d.GroupCode == groupCode).Select(a => new GroupSiteModel
                    {
                        GroupCode = a.GroupCode,
                        GroupName = a.GroupName,
                        ListStore = a.ListStore

                    }).FirstOrDefault();

                    if (checkData != null)
                    {

                        if (checkData.ListStore == "ALL")
                        {
                            var data = db.Stores.Where(d => d.ClosingMethod == 0).Select(a => new StoreSetupModel
                            {
                                SiteNo = a.No,
                                SiteName = a.Name
                            });
                            if (!string.IsNullOrEmpty(storeNo))
                            {
                                data = data.Where(d => d.SiteNo == storeNo);
                            }
                            if (!string.IsNullOrEmpty(storeName))
                            {
                                data = data.Where(d => d.SiteName.Contains(storeName));
                            }
                            total = data.Count();
                            result = data.OrderBy(d => d.SiteNo).Skip(skip).Take(take).ToList();
                            return result;

                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(checkData.ListStore);
                            var data = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0)
                                       .Select(a => new StoreSetupModel
                                       {
                                           SiteNo = a.No,
                                           SiteName = a.Name

                                       });
                            if (!string.IsNullOrEmpty(storeNo))
                            {
                                data = data.Where(d => d.SiteNo == storeNo);
                            }
                            if (!string.IsNullOrEmpty(storeName))
                            {
                                data = data.Where(d => d.SiteName.Contains(storeName));
                            }
                            total = data.Count();
                            result = data.OrderBy(d => d.SiteNo).Skip(skip).Take(take).ToList();
                            return result;
                        }

                    }
                    else
                    {
                        total = 0;
                        return result;
                    }

                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return result;
            }
        }

        public Tuple<bool, string, List<GroupSiteModel>> InsertUpdateOfferSite(string offerNo, List<string> listGroupCode)
        {
            var result = new Tuple<bool, string, List<GroupSiteModel>>(true, "", null);
            var listGroup = new List<GroupSiteModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var listError = new List<string>();
                    db.Database.CommandTimeout = 2 * 60;
                    foreach (var groupCode in listGroupCode)
                    {
                        var checkData = db.SetupGroupSites.Where(d => d.GroupCode == groupCode).Select(a => new GroupSiteModel
                        {
                            GroupCode = a.GroupCode,
                            GroupName = a.GroupName,
                            ListStore = a.ListStore

                        }).FirstOrDefault();

                        if (checkData == null)
                        {
                            listError.Add(groupCode);
                            //result = new Tuple<bool, string, GroupSiteModel>(false, "Không tìm thấy dữ liệu group site", null);
                        }
                        else
                        {
                            var checkOfferSite = db.OfferSites.Where(d => d.OfferNo == offerNo && d.PriceGroupCode == groupCode);
                            if (checkOfferSite != null)
                            {
                                db.OfferSites.RemoveRange(checkOfferSite);
                                db.SaveChanges();
                            }

                            if (checkData.ListStore == "ALL")
                            {
                                var listStore = db.Stores.Where(d => d.ClosingMethod == 0).ToList().Select(a => new OfferSite
                                {
                                    OfferNo = offerNo,
                                    PriceGroupCode = groupCode,
                                    StoreNo = a.No,
                                    Pkey = offerNo,
                                    Counter = 0
                                });
                                db.OfferSites.AddRange(listStore);
                            }
                            else
                            {
                                var listStore = JsonConvert.DeserializeObject<List<string>>(checkData.ListStore);

                                var listOfferSite = listStore.Select(a => new OfferSite
                                {
                                    OfferNo = offerNo,
                                    PriceGroupCode = groupCode,
                                    StoreNo = a.ToString(),
                                    Pkey = offerNo,
                                    Counter = 0

                                });
                                db.OfferSites.AddRange(listOfferSite);

                            }
                            db.SaveChanges();
                            listGroup.Add(checkData);
                            // result = new Tuple<bool, string, GroupSiteModel>(true, "Success", checkData);
                        }
                    }
                    result = new Tuple<bool, string, List<GroupSiteModel>>(true, "Success", listGroup);

                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, List<GroupSiteModel>>(false, JsonConvert.SerializeObject(ex), listGroup);
                }
            }

            return result;
        }

        public List<BuyGroupItemModel> LoadBuyGroup(string groupCode, string groupName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.SetupGroupItems.Where(d => d.Status == true).Select(a => new BuyGroupItemModel
                    {
                        GroupCode = a.GroupCode,
                        GroupName = a.GroupName,
                        ListItemNo = a.ListItemNo,
                        Status = a.Status,
                        CreatedDate = a.CreatedDate,
                        CreatedUser = a.CreatedUser,
                        LastUpdateDate = a.LastUpdateDate,
                        LastUpdateUser = a.LastUpdateUser

                    });
                    if (!string.IsNullOrEmpty(groupCode))
                    {
                        data = data.Where(m => m.GroupCode == groupCode);
                    }
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        data = data.Where(m => m.GroupName.ToLower().Contains(groupName.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.CreatedDate).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }

                return new List<BuyGroupItemModel>();
            }
        }

        public List<ItemSetupMode> LoadListItemBuySetup(string groupCode, string itemNo, string itemName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                var result = new List<ItemSetupMode>();
                try
                {
                    db.Database.CommandTimeout = 120;
                    var checkData = db.SetupGroupItems.Where(d => d.GroupCode == groupCode).Select(a => new BuyGroupItemModel
                    {
                        GroupCode = a.GroupCode,
                        GroupName = a.GroupName,
                        ListItemNo = a.ListItemNo

                    }).FirstOrDefault();

                    if (checkData != null)
                    {


                        var listStore = JsonConvert.DeserializeObject<List<string>>(checkData.ListItemNo);
                        var data = db.Items.Where(a => listStore.Contains(a.No))
                                   .Select(a => new ItemSetupMode
                                   {
                                       No = a.No,
                                       Description = a.Description,
                                       UOM = a.SalesUnitOfMeasure

                                   });
                        if (!string.IsNullOrEmpty(itemNo))
                        {
                            data = data.Where(d => d.No == itemNo);
                        }
                        if (!string.IsNullOrEmpty(itemName))
                        {
                            data = data.Where(d => d.Description.Contains(itemName));
                        }
                        total = data.Count();
                        result = data.OrderBy(d => d.No).Skip(skip).Take(take).ToList();
                        return result;
                    }
                    else
                    {
                        total = 0;
                        return result;
                    }

                }
                catch (Exception ex)
                {
                    total = 0;
                }
                return result;
            }
        }

        #region SetupPromotion
        public Tuple<bool, string, OfferHeaderModel> SetupPromotionHeader(OfferHeaderModel model)
        {
            var result = new Tuple<bool, string, OfferHeaderModel>(false, "", model);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    lock (lockRequest)
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var noNew = "6000000001";
                        var noAuto = db.SetupPromotionHEADERs.Max(d => d.BBYNR);
                        var noSave = string.IsNullOrEmpty(noAuto) ? Convert.ToInt64(noNew) : Convert.ToInt64(noAuto) + 1;

                        var insert = new SetupPromotionHEADER
                        {
                            BBYNR = "" + noSave,
                            SalesType = model.SalesType,
                            BBYTEXT = model.Description,
                            STATUS = "" + model.Status,
                            BBYTYPE = model.OfferType,
                            VALIDFROM = model.StartingDate.ToString("yyyyMMdd"),
                            VALIDTO = model.EndingDate.ToString("yyyyMMdd"),
                            ZVCDATE_ST = model.StartingDate.ToString("yyyyMMdd"),
                            ZVCDATE_EN = model.EndingDate.ToString("yyyyMMdd"),
                            TIMEFROM = "",
                            TIMETO = "",
                            MON = "",
                            TUE = "",
                            WED = "",
                            THUR = "",
                            FRI = "",
                            SAT = "",
                            SUN = "",
                            PROMOTION = "" + noSave,
                            PROMOTIONTEXT = model.Description,
                            IsVoucher = model.IsVoucherHeader
                            //LIMIT = model.LIMIT,
                            //TOTALMINVALUE = model.TOTALMINVALUE,
                            //MINVALUE = model.MINVALUE

                        };

                        db.SetupPromotionHEADERs.Add(insert);
                        db.SaveChanges();

                        model.No = insert.BBYNR;
                        model.VoucherFromDate = (string.IsNullOrEmpty(insert.ZVCDATE_ST) || insert.ZVCDATE_ST == "0" || insert.ZVCDATE_ST.Length != 8) ? DateTime.ParseExact("19000101", "yyyyMMdd", CultureInfo.InvariantCulture) : DateTime.ParseExact(insert.ZVCDATE_ST, "yyyyMMdd", CultureInfo.InvariantCulture);
                        model.VoucherToDate = (string.IsNullOrEmpty(insert.ZVCDATE_EN) || insert.ZVCDATE_EN == "0" || insert.ZVCDATE_EN.Length != 8) ? DateTime.ParseExact("99991231", "yyyyMMdd", CultureInfo.InvariantCulture) : DateTime.ParseExact(insert.ZVCDATE_EN, "yyyyMMdd", CultureInfo.InvariantCulture);


                        result = new Tuple<bool, string, OfferHeaderModel>(true, "Success", model);
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferHeaderModel>(false, ex.Message, model);
                }
            }

            return result;
        }
        public Tuple<bool, string, OfferHeaderModel> SaveSetupPromotion(OfferHeaderModel model)
        {
            var result = new Tuple<bool, string, OfferHeaderModel>(false, "", model);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    lock (lockRequest)
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var checkNo = db.SetupPromotionHEADERs.FirstOrDefault(d => d.BBYNR == model.No);
                        if (checkNo != null)
                        {
                            if (checkNo.IsApprove == true)
                            {
                                return new Tuple<bool, string, OfferHeaderModel>(false, $"CTKM {model.No} đã được DUYỆT, không được phép lưu tạm", model);
                            }
                            checkNo.SalesType = model.SalesType;
                            checkNo.BBYTEXT = model.Description;
                            checkNo.STATUS = "" + model.Status;
                            //checkNo.BBYTYPE = model.OfferType;
                            checkNo.VALIDFROM = model.StartingDate.ToString("yyyyMMdd");
                            checkNo.VALIDTO = model.EndingDate.ToString("yyyyMMdd");
                            checkNo.PROMOTIONTEXT = model.Description;
                            checkNo.IsVoucher = model.IsVoucherHeader;
                            checkNo.IsApprove = model.IsApprove;

                            db.SaveChanges();
                        }
                        else
                        {
                            var noNew = "6000000001";
                            var noAuto = db.SetupPromotionHEADERs.Max(d => d.BBYNR);
                            var noSave = string.IsNullOrEmpty(noAuto) ? Convert.ToInt64(noNew) : Convert.ToInt64(noAuto) + 1;

                            var insert = new SetupPromotionHEADER
                            {
                                BBYNR = "" + noSave,
                                SalesType = model.SalesType,
                                BBYTEXT = model.Description,
                                STATUS = "" + model.Status,
                                BBYTYPE = model.OfferType,
                                VALIDFROM = model.StartingDate.ToString("yyyyMMdd"),
                                VALIDTO = model.EndingDate.ToString("yyyyMMdd"),
                                ZVCDATE_ST = model.StartingDate.ToString("yyyyMMdd"),
                                ZVCDATE_EN = model.EndingDate.ToString("yyyyMMdd"),
                                TIMEFROM = "",
                                TIMETO = "",
                                MON = "",
                                TUE = "",
                                WED = "",
                                THUR = "",
                                FRI = "",
                                SAT = "",
                                SUN = "",
                                PROMOTION = "" + noSave,
                                PROMOTIONTEXT = model.Description,
                                IsVoucher = model.IsVoucherHeader

                                //LIMIT = model.LIMIT,
                                //TOTALMINVALUE = model.TOTALMINVALUE,
                                //MINVALUE = model.MINVALUE

                            };

                            db.SetupPromotionHEADERs.Add(insert);
                            db.SaveChanges();

                            model.No = insert.BBYNR;
                            model.VoucherFromDate = (string.IsNullOrEmpty(insert.ZVCDATE_ST) || insert.ZVCDATE_ST == "0" || insert.ZVCDATE_ST.Length != 8) ? DateTime.ParseExact("19000101", "yyyyMMdd", CultureInfo.InvariantCulture) : DateTime.ParseExact(insert.ZVCDATE_ST, "yyyyMMdd", CultureInfo.InvariantCulture);
                            model.VoucherToDate = (string.IsNullOrEmpty(insert.ZVCDATE_EN) || insert.ZVCDATE_EN == "0" || insert.ZVCDATE_EN.Length != 8) ? DateTime.ParseExact("99991231", "yyyyMMdd", CultureInfo.InvariantCulture) : DateTime.ParseExact(insert.ZVCDATE_EN, "yyyyMMdd", CultureInfo.InvariantCulture);
                        }
                        result = new Tuple<bool, string, OfferHeaderModel>(true, "Success", model);
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferHeaderModel>(false, ex.Message, model);
                }
            }

            return result;
        }

        public Tuple<bool, string, OfferHeaderModel> SaveSetupCTKMAll(AdvenceSetupRequest req)
        {
            var result = new Tuple<bool, string, OfferHeaderModel>(false, "", null);

            // ── Validate input ──
            if (string.IsNullOrWhiteSpace(req.Description))
                return new Tuple<bool, string, OfferHeaderModel>(false, "Vui lòng nhập tên chương trình khuyến mãi", null);
            if (string.IsNullOrWhiteSpace(req.SalesType))
                return new Tuple<bool, string, OfferHeaderModel>(false, "Vui lòng chọn hình thức bán hàng", null);
            if (string.IsNullOrWhiteSpace(req.OfferType) && string.IsNullOrWhiteSpace(req.BonusBuy))
                return new Tuple<bool, string, OfferHeaderModel>(false, "Vui lòng chọn loại CTKM", null);
            if (string.IsNullOrWhiteSpace(req.StartingDate) || string.IsNullOrWhiteSpace(req.EndingDate))
                return new Tuple<bool, string, OfferHeaderModel>(false, "Vui lòng nhập ngày bắt đầu và kết thúc CTKM", null);
            if (string.IsNullOrWhiteSpace(req.Status) || !new[] { "0", "1", "2" }.Contains(req.Status))
                return new Tuple<bool, string, OfferHeaderModel>(false, "Trạng thái không hợp lệ", null);

            // Kiểm tra ngày bắt đầu <= ngày kết thúc
            DateTime _startDate, _endDate;
            if (!DateTime.TryParseExact(req.StartingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _startDate))
                return new Tuple<bool, string, OfferHeaderModel>(false, "Ngày bắt đầu không đúng định dạng (dd/MM/yyyy)", null);
            if (!DateTime.TryParseExact(req.EndingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _endDate))
                return new Tuple<bool, string, OfferHeaderModel>(false, "Ngày kết thúc không đúng định dạng (dd/MM/yyyy)", null);
            if (_endDate < _startDate)
                return new Tuple<bool, string, OfferHeaderModel>(false, "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu", null);

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    lock (lockRequest)
                    {
                        db.Database.CommandTimeout = 2 * 60;
                        var dateNow = DateTime.Now;
                        var startDate = DateTime.ParseExact(req.StartingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        var endDate = DateTime.ParseExact(req.EndingDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                        // ── 1. HEADER ──
                        string offerNo;
                        SetupPromotionHEADER headerEntity;
                        var checkNo = !string.IsNullOrEmpty(req.BonusBuy)
                            ? db.SetupPromotionHEADERs.FirstOrDefault(d => d.BBYNR == req.BonusBuy)
                            : null;
                        if (checkNo != null)
                        {
                            if (checkNo.IsApprove == true)
                                return new Tuple<bool, string, OfferHeaderModel>(false, $"CTKM {req.BonusBuy} đã được DUYỆT, không được phép lưu tạm", null);

                            checkNo.SalesType = req.SalesType;
                            checkNo.BBYTEXT = req.Description;
                            checkNo.BBYTYPE = req.OfferType;
                            checkNo.STATUS = req.Status;
                            checkNo.VALIDFROM = startDate.ToString("yyyyMMdd");
                            checkNo.VALIDTO = endDate.ToString("yyyyMMdd");
                            checkNo.PROMOTIONTEXT = req.Description;
                            checkNo.IsVoucher = req.isVoucherHeader;
                            offerNo = checkNo.BBYNR;
                            headerEntity = checkNo;
                        }
                        else
                        {
                            var noNew = "6000000001";
                            var noAuto = db.SetupPromotionHEADERs.Max(d => d.BBYNR);
                            var noSave = string.IsNullOrEmpty(noAuto) ? Convert.ToInt64(noNew) : Convert.ToInt64(noAuto) + 1;
                            offerNo = "" + noSave;

                            var insert = new SetupPromotionHEADER
                            {
                                BBYNR = offerNo,
                                SalesType = req.SalesType,
                                BBYTEXT = req.Description,
                                STATUS = req.Status,
                                BBYTYPE = req.OfferType,
                                VALIDFROM = startDate.ToString("yyyyMMdd"),
                                VALIDTO = endDate.ToString("yyyyMMdd"),
                                ZVCDATE_ST = startDate.ToString("yyyyMMdd"),
                                ZVCDATE_EN = endDate.ToString("yyyyMMdd"),
                                TIMEFROM = "",
                                TIMETO = "",
                                MON = "",
                                TUE = "",
                                WED = "",
                                THUR = "",
                                FRI = "",
                                SAT = "",
                                SUN = "",
                                PROMOTION = offerNo,
                                PROMOTIONTEXT = req.Description,
                                IsVoucher = req.isVoucherHeader
                            };
                            db.SetupPromotionHEADERs.Add(insert);
                            headerEntity = insert;
                        }

                        // ── 2. ADVANCE ──
                        if (req.HasAdvanceData)
                        {
                            DateTime? vcFromDate = null, vcToDate = null;
                            if (!string.IsNullOrEmpty(req.VoucherFromDateStr))
                                vcFromDate = DateTime.ParseExact(req.VoucherFromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            if (!string.IsNullOrEmpty(req.VoucherToDateStr))
                                vcToDate = DateTime.ParseExact(req.VoucherToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                            headerEntity.VINID = req.MemberOnly == 1 ? "X" : "";
                            headerEntity.TIMEFROM = req.FromTime ?? "";
                            headerEntity.TIMETO = req.ToTime ?? "";
                            headerEntity.MON = req.Mon == 1 ? "1" : "";
                            headerEntity.TUE = req.Tue == 1 ? "1" : "";
                            headerEntity.WED = req.Wed == 1 ? "1" : "";
                            headerEntity.THUR = req.Thu == 1 ? "1" : "";
                            headerEntity.FRI = req.Fri == 1 ? "1" : "";
                            headerEntity.SAT = req.Sat == 1 ? "1" : "";
                            headerEntity.SUN = req.Sun == 1 ? "1" : "";
                            headerEntity.NUMOFDAYS = "" + req.NumOfDays;
                            headerEntity.LIMIT = "" + req.LimitQty;
                            headerEntity.ZVCDATE_ST = vcFromDate != null ? vcFromDate.Value.ToString("yyyyMMdd") : "19000101";
                            headerEntity.ZVCDATE_EN = vcToDate != null ? vcToDate.Value.ToString("yyyyMMdd") : "19000101";
                            headerEntity.ZVCDATE_VA = "" + req.VoucherValidDay;
                            headerEntity.LIMITNR = "" + req.VoucherLimitNumber;
                            headerEntity.ZPRIOR = "" + (req.PriorityBBY == 0 ? 1 : req.PriorityBBY);
                            headerEntity.MemberCode = req.MemberCode;
                            // TODO: Chưa có cột trong DB - bỏ comment sau khi chạy ALTER TABLE
                            //headerEntity.AllowUseAfterDay = "" + req.AllowUseAfterDay;
                            //headerEntity.AllowUseAfterTime = req.AllowUseAfterTime ?? "";
                        }

                        // ── 3. BUY ROWS ──
                        if (req.BuyRows != null && req.BuyRows.Any())
                        {
                            var offerBuys = db.SetupPromotionBUYs.Where(d => d.BBYNR == offerNo).ToList();
                            headerEntity.BUYLINKCAT = req.ConditionBuy == "OR" ? "O" : "A";
                            foreach (var row in req.BuyRows)
                            {
                                var existing = offerBuys.FirstOrDefault(d => d.ID == row.LineNo);
                                if (existing != null)
                                {
                                    existing.BUYTYPE = row.LineType == 1 ? "MGP" : "MAT";
                                    existing.MAT_NR = row.LineType == 1 ? "" : (row.No ?? "");
                                    existing.MATGROUP = row.LineType == 1 ? (row.GroupCode ?? "") : "";
                                    existing.MEINH = row.UnitOfMeasure ?? "";
                                    existing.MAT_QUAN = "" + row.Quantity;
                                    existing.ScaleType = row.ScaleType ?? "C";
                                }
                                else
                                {
                                    db.SetupPromotionBUYs.Add(new SetupPromotionBUY
                                    {
                                        BBYNR = offerNo,
                                        CREATEDDATE = dateNow,
                                        LINEINDICATOR = "2",
                                        BUYTYPE = row.LineType == 1 ? "MGP" : "MAT",
                                        MAT_NR = row.LineType == 1 ? "" : (row.No ?? ""),
                                        MATGROUP = row.LineType == 1 ? (row.GroupCode ?? "") : "",
                                        MAT_QUAN = "" + row.Quantity,
                                        MEINH = row.UnitOfMeasure ?? "",
                                        ScaleType = row.ScaleType ?? "C"
                                    });
                                }
                            }
                        }

                        // ── 4. GET ROWS ──
                        if (req.GetRows != null && req.GetRows.Any())
                        {
                            var offerGets = db.SetupPromotionGETs.Where(d => d.BBYNR == offerNo).ToList();
                            headerEntity.GETLINKCAT = req.ConditionGet == "OR" ? "O" : "A";
                            foreach (var row in req.GetRows)
                            {
                                var disType = row.DiscountType == 0 ? "%" : row.DiscountType == 1 ? "R" : "P";
                                var bbyval = row.DiscountType == 0 ? "0" : "" + row.DiscountValue;
                                var bbyper = row.DiscountType == 0 ? "" + row.DiscountValue : "0";
                                var existing = offerGets.FirstOrDefault(d => d.ID == row.LineNo);
                                if (existing != null)
                                {
                                    existing.GETTYPE = row.LineType == 1 ? "MGP" : "MAT";
                                    existing.MATERIALCODE = row.LineType == 1 ? "" : (row.No ?? "");
                                    existing.MATGROUP = row.LineType == 1 ? (row.GroupCode ?? "") : "";
                                    existing.MEINH = row.UnitOfMeasure ?? "";
                                    existing.QTY = "" + row.Quantity;
                                    existing.DISTYPE = disType;
                                    existing.BBYVAL = bbyval;
                                    existing.BBYPER = bbyper;
                                    existing.SCALETYPE = row.ScaleType ?? "C";
                                }
                                else
                                {
                                    db.SetupPromotionGETs.Add(new SetupPromotionGET
                                    {
                                        BBYNR = offerNo,
                                        CREATEDDATE = dateNow,
                                        LINEINDICATOR = "3",
                                        GETTYPE = row.LineType == 1 ? "MGP" : "MAT",
                                        MATERIALCODE = row.LineType == 1 ? "" : (row.No ?? ""),
                                        MATGROUP = row.LineType == 1 ? (row.GroupCode ?? "") : "",
                                        QTY = "" + row.Quantity,
                                        MEINH = row.UnitOfMeasure ?? "",
                                        DISTYPE = disType,
                                        BBYVAL = bbyval,
                                        BBYPER = bbyper,
                                        SCALETYPE = row.ScaleType ?? "C"
                                    });
                                }
                            }
                        }

                        // ── 5. SITE ROWS ──
                        if (req.SiteRows != null && req.SiteRows.Any())
                        {
                            var oldSites = db.SetupPromotionSITEs.Where(d => d.BBYNR == offerNo).ToList();
                            db.SetupPromotionSITEs.RemoveRange(oldSites);
                            foreach (var groupCode in req.SiteRows.Where(g => !string.IsNullOrEmpty(g)))
                            {
                                var groupData = db.SetupGroupSites.FirstOrDefault(d => d.GroupCode.ToUpper() == groupCode.ToUpper());
                                if (groupData == null) continue;

                                if (groupData.ListStore == "ALL")
                                {
                                    db.SetupPromotionSITEs.Add(new SetupPromotionSITE
                                    {
                                        BBYNR = offerNo,
                                        SITEGROUPCODE = groupCode,
                                        SITECODE = "ALL",
                                        CREATEDDATE = dateNow
                                    });
                                }
                                else
                                {
                                    var listStore = JsonConvert.DeserializeObject<List<string>>(groupData.ListStore);
                                    foreach (var siteCode in listStore)
                                    {
                                        db.SetupPromotionSITEs.Add(new SetupPromotionSITE
                                        {
                                            BBYNR = offerNo,
                                            SITEGROUPCODE = groupCode,
                                            SITECODE = siteCode,
                                            CREATEDDATE = dateNow
                                        });
                                    }
                                }
                            }
                        }

                        // ── 6. SINGLE SaveChanges ──
                        db.SaveChanges();

                        var savedModel = new OfferHeaderModel
                        {
                            No = offerNo,
                            Description = req.Description,
                            SalesType = req.SalesType,
                            OfferType = req.OfferType,
                            StartingDate = startDate,
                            EndingDate = endDate,
                            IsVoucherHeader = req.isVoucherHeader,
                            Status = int.Parse(req.Status)
                        };
                        result = new Tuple<bool, string, OfferHeaderModel>(true, $"Lưu tạm thành công CTKM {offerNo}", savedModel);
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferHeaderModel>(false, ex.Message, null);
                }
            }
            return result;
        }

        public OfferHeaderModel SetupGetOfferHeader(string bonusBuy)
        {
            var result = new OfferHeaderModel();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {

                    var data = db.SetupPromotionHEADERs.Where(d => d.BBYNR == bonusBuy).FirstOrDefault();
                    result = new OfferHeaderModel
                    {
                        No = data.BBYNR,
                        Description = data.BBYTEXT,
                        SalesType = data.SalesType,
                        Status = int.Parse(data.STATUS),
                        OfferType = data.BBYTYPE,
                        ConditionBuy = data.BUYLINKCAT == "A" ? 2 : 1,
                        ConditionGet = data.GETLINKCAT == "A" ? 2 : 1,
                        MemberOnly = data.VINID == "X" ? byte.Parse("1") : byte.Parse("0"),
                        StartingDate = DateTime.ParseExact(data.VALIDFROM, "yyyyMMdd", CultureInfo.InvariantCulture),
                        EndingDate = DateTime.ParseExact(data.VALIDTO, "yyyyMMdd", CultureInfo.InvariantCulture),

                        FromTime = data.TIMEFROM,
                        ToTime = data.TIMETO,
                        BuyLinkCat = data.BUYLINKCAT,
                        GetLinkCat = data.GETLINKCAT,
                        CheckMinValue = data.TOTALMINVALUE,
                        TotalMinValue = data.MINVALUE,

                        Mon = !string.IsNullOrEmpty(data.MON) ? byte.Parse("1") : byte.Parse("0"),
                        Tue = !string.IsNullOrEmpty(data.TUE) ? byte.Parse("1") : byte.Parse("0"),
                        Wed = !string.IsNullOrEmpty(data.WED) ? byte.Parse("1") : byte.Parse("0"),
                        Thu = !string.IsNullOrEmpty(data.THUR) ? byte.Parse("1") : byte.Parse("0"),
                        Fri = !string.IsNullOrEmpty(data.FRI) ? byte.Parse("1") : byte.Parse("0"),
                        Sat = !string.IsNullOrEmpty(data.SAT) ? byte.Parse("1") : byte.Parse("0"),
                        Sun = !string.IsNullOrEmpty(data.SUN) ? byte.Parse("1") : byte.Parse("0"),
                        LimitQty = !string.IsNullOrEmpty(data.LIMIT) ? float.Parse(data.LIMIT) : 0,
                        //LocalSiteGroup = a.LocalSiteGroup,                      
                        VoucherFromDate = (string.IsNullOrEmpty(data.ZVCDATE_ST) || data.ZVCDATE_ST == "0" || data.ZVCDATE_ST.Length != 8) ? DateTime.ParseExact("19000101", "yyyyMMdd", CultureInfo.InvariantCulture) : DateTime.ParseExact(data.ZVCDATE_ST, "yyyyMMdd", CultureInfo.InvariantCulture),
                        VoucherToDate = (string.IsNullOrEmpty(data.ZVCDATE_EN) || data.ZVCDATE_EN == "0" || data.ZVCDATE_EN.Length != 8) ? DateTime.ParseExact("19000101", "yyyyMMdd", CultureInfo.InvariantCulture) : DateTime.ParseExact(data.ZVCDATE_EN, "yyyyMMdd", CultureInfo.InvariantCulture),

                        VoucherValidDay = string.IsNullOrEmpty(data.ZVCDATE_VA) ? 0 : int.Parse(data.ZVCDATE_VA),
                        VoucherLimitNumber = string.IsNullOrEmpty(data.LIMITNR) ? 0 : int.Parse(data.LIMITNR),
                        PriorityBBY = !string.IsNullOrEmpty(data.ZPRIOR) ? int.Parse(data.ZPRIOR) : 1,

                        NumOfDays = string.IsNullOrEmpty(data.NUMOFDAYS) ? 0 : int.Parse(data.NUMOFDAYS),
                        AmountTotalBill = string.IsNullOrEmpty(data.MINVALUE) ? 0 : double.Parse(data.MINVALUE),
                        IsVoucherHeader = (bool)data.IsVoucher,
                        IsApprove = data.IsApprove ?? false,
                        MemberCode = data.MemberCode,
                        // TODO: Chưa có cột trong DB - bỏ comment sau khi chạy ALTER TABLE
                        //AllowUseAfterDay = string.IsNullOrEmpty(data.AllowUseAfterDay) ? 0 : int.Parse(data.AllowUseAfterDay),
                        //AllowUseAfterTime = data.AllowUseAfterTime ?? ""
                    };
                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public Tuple<bool, string, OfferHeaderModel> SetupInsertUpdateAdvance(OfferHeaderModel model)
        {
            var dataModel = new OfferHeaderModel();
            var result = new Tuple<bool, string, OfferHeaderModel>(false, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkNo = db.SetupPromotionHEADERs.FirstOrDefault(d => d.BBYNR == model.No);
                    if (checkNo != null)
                    {
                        if (checkNo.IsApprove == true)
                        {
                            result = new Tuple<bool, string, OfferHeaderModel>(false, $"CTKM {model.No} đã được DUYỆT, không được phép sửa", dataModel);
                        }
                        else
                        {
                            checkNo.VINID = model.MemberOnly == 1 ? "X" : "";
                            checkNo.TIMEFROM = model.FromTime;
                            checkNo.TIMETO = model.ToTime;
                            checkNo.MON = model.Mon == 1 ? "1" : "";
                            checkNo.TUE = model.Tue == 1 ? "1" : "";
                            checkNo.WED = model.Wed == 1 ? "1" : "";
                            checkNo.THUR = model.Thu == 1 ? "1" : "";
                            checkNo.FRI = model.Fri == 1 ? "1" : "";
                            checkNo.SAT = model.Sat == 1 ? "1" : "";
                            checkNo.SUN = model.Sun == 1 ? "1" : "";
                            checkNo.NUMOFDAYS = "" + model.NumOfDays;
                            checkNo.LIMIT = "" + model.LimitQty;
                            checkNo.ZVCDATE_ST = model.VoucherFromDate != null ? model.VoucherFromDate.Value.ToString("yyyyMMdd") : "19000101";
                            checkNo.ZVCDATE_EN = model.VoucherToDate != null ? model.VoucherToDate.Value.ToString("yyyyMMdd") : "19000101";
                            checkNo.ZVCDATE_VA = "" + model.VoucherValidDay;
                            checkNo.LIMITNR = "" + model.VoucherLimitNumber;
                            checkNo.ZPRIOR = "" + model.PriorityBBY;
                            checkNo.IsVoucher = model.IsVoucherHeader;
                            checkNo.MemberCode = model.MemberCode;

                            if (model.isTotalBill && model.AmountTotalBill > 0)
                            {
                                checkNo.MINVALUE = "" + model.AmountTotalBill;
                            }

                            db.SaveChanges();
                            dataModel = model;
                            result = new Tuple<bool, string, OfferHeaderModel>(true, "Success", dataModel);
                        }
                    }
                    else
                    {
                        result = new Tuple<bool, string, OfferHeaderModel>(false, $"Không tìm thấy dữ liệu CTKM {model.No}", dataModel);
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferHeaderModel>(false, ex.Message, dataModel);
                }

            }

            return result;
        }


        public Tuple<bool, string, OfferBuyModel> SetupInsertUpdateOfferBuy(OfferBuyModel model, bool isTotalBill, bool isSetupGet, string conditionBuy, string offerType, string itemHidden)
        {
            var dataModel = new OfferBuyModel();
            var result = new Tuple<bool, string, OfferBuyModel>(true, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var dateNow = DateTime.Now;
                    var checkHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == model.OfferNo);
                    if (checkHeader.Any(d => d.IsApprove == true))
                    {
                        return new Tuple<bool, string, OfferBuyModel>(false, $"CTKM {model.OfferNo} đã được DUYỆT,nên không thể thêm mới hoặc chỉnh sửa", dataModel);
                    }

                    var offerBuy = db.SetupPromotionBUYs.Where(d => d.BBYNR == model.OfferNo).ToList();
                    if (model.LineType == 0) // theo Item
                    {
                       
                        if (itemHidden.Trim() != model.No.Trim())
                        {
                            var checkNoBuy = offerBuy.Where(d => d.MAT_NR == model.No).ToList();
                            if (checkNoBuy != null && checkNoBuy.Count > 0)
                            {
                                var groupCode = checkNoBuy.FirstOrDefault().MATGROUP;
                                if (!string.IsNullOrEmpty(groupCode))
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferBuyModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại item {model.No} trong group {groupCode}", infoItemHidden);
                                }
                                else
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferBuyModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại item {model.No} trong CTKM", infoItemHidden);
                                }
                            }
                        }

                        var checkNo = offerBuy.FirstOrDefault(d => d.ID == model.LineNo);
                        if (checkNo != null)
                        {
                            checkNo.BUYTYPE = model.LineType == 1 ? "MGP" : "MAT";
                            checkNo.MAT_NR = model.No;//ItemNo
                            checkNo.MEINH = model.UnitOfMeasure;
                            checkNo.MAT_QUAN = "" + model.Quantity;
                            //checkNo.DiscountType = "" + model.DiscountType;
                            //checkNo.DiscountValue = "" + model.DiscountValue;
                            checkNo.ScaleType = model.ScaleType;
                        }
                        else
                        {
                            var insert = new SetupPromotionBUY
                            {
                                BBYNR = model.OfferNo,
                                CREATEDDATE = dateNow,
                                LINEINDICATOR = "2",
                                BUYTYPE = model.LineType == 1 ? "MGP" : "MAT",
                                MAT_NR = model.No,
                                MAT_QUAN = "" + model.Quantity,
                                MEINH = model.UnitOfMeasure,
                                //DiscountType = "" + model.DiscountType,
                                //DiscountValue = "" + model.DiscountValue,
                                ScaleType = model.ScaleType
                            };
                            db.SetupPromotionBUYs.Add(insert);
                            db.SaveChanges();

                            //var checkOfferHeader = db.OfferHeaders.FirstOrDefault(d => d.No == model.OfferNo);
                            //if (checkOfferHeader != null)
                            //{
                            //    var conditionBuySave = conditionBuy == "OR" ? 1 : 2;
                            //    checkOfferHeader.ConditionBuy = conditionBuySave;
                            //}

                            model = new OfferBuyModel
                            {
                                OfferNo = insert.BBYNR,
                                LineNo = insert.ID,
                                //ID = insert.ID,
                                LineType = insert.BUYTYPE == "MGP" ? 1 : 0,
                                No = insert.MAT_NR,
                                Description = db.Items.FirstOrDefault(d => d.No == insert.MAT_NR) == null ? "" : db.Items.FirstOrDefault(d => d.No == insert.MAT_NR).Description,
                                UnitOfMeasure = insert.MEINH,
                                //DiscountType = string.IsNullOrEmpty(insert.DiscountType) ? 0 : int.Parse(insert.DiscountType),
                                //DiscountValue = string.IsNullOrEmpty(insert.DiscountValue) ? 0 : decimal.Parse(insert.DiscountValue),
                                Quantity = string.IsNullOrEmpty(insert.MAT_NR) ? 0 : int.Parse(insert.MAT_QUAN),
                                ScaleType = insert.ScaleType
                            };
                        }
                        db.SaveChanges();
                        dataModel = model;

                        result = new Tuple<bool, string, OfferBuyModel>(true, "Success", dataModel);
                    }
                    else // Theo group Item
                    {
                        if (string.IsNullOrEmpty(model.GroupCode))
                        {
                            var checkNo = offerBuy.FirstOrDefault(d => d.ID == model.LineNo && string.IsNullOrEmpty(d.MAT_NR));
                            if (checkNo != null)
                            {
                                checkNo.BUYTYPE = "MGP";
                                db.SaveChanges();
                                result = new Tuple<bool, string, OfferBuyModel>(true, "Success", null);
                            }
                        }
                        else
                        {
                            //"["NuocUong"]"
                            if (!string.IsNullOrEmpty(model.GroupCode) && model.GroupCode.Contains("["))//List GroupCode (trước cho chọn nhiều groupCode)
                            {
                                var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                var checkGroup = offerBuy.Any(d => d.MATGROUP == listGroup.FirstOrDefault());
                                if (checkGroup)
                                {
                                    return result = new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm mua", null);
                                }
                                else
                                {
                                    var checkGroupOfferGet = db.SetupPromotionGETs.Any(d => d.BBYNR == model.OfferNo && d.MATGROUP == listGroup.FirstOrDefault());
                                    if (checkGroupOfferGet)
                                    {
                                        return result = new Tuple<bool, string, OfferBuyModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm khuyến mãi", null);
                                    }
                                }
                            }

                            if (model.GroupCode == model.BonusBuyNo)//Update các thông số
                            {
                                var checkGroupCode = offerBuy.Where(d => d.MATGROUP == model.BonusBuyNo).ToList();
                                if (checkGroupCode != null && checkGroupCode.Count > 0)
                                {
                                    checkGroupCode.ForEach(x =>
                                    {
                                        x.DiscountType = "" + model.DiscountType;
                                        x.DiscountValue = "" + model.DiscountValue;
                                        x.MAT_QUAN = "" + model.Quantity;
                                        x.ScaleType = model.ScaleType;
                                    });

                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferBuyModel>(true, "Success", null);
                                }
                            }
                            else // insert list item vào OfferBuy
                            {
                                var offerBuyOld = offerBuy.Where(d => d.MATGROUP == model.BonusBuyNo && d.BUYTYPE == "MGP");
                                if (offerBuyOld != null && offerBuyOld.ToList().Count > 0)
                                {
                                    db.SetupPromotionBUYs.RemoveRange(offerBuyOld);
                                }
                                var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                if (listGroup.Count > 0)
                                {
                                    var listItemBuyGroup = new List<string>();

                                    foreach (var item in listGroup)
                                    {
                                        var checkGroupBuy = db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == item);

                                        var listItemGroup = JsonConvert.DeserializeObject<List<string>>(checkGroupBuy.ListItemNo);

                                        var listItemInsert = db.Items.Where(d => listItemGroup.Contains(d.No)).ToList();

                                        foreach (var itemInsert in listItemInsert)
                                        {
                                            var insert = new SetupPromotionBUY
                                            {
                                                BBYNR = model.OfferNo,
                                                BUYTYPE = "MGP",
                                                LINEINDICATOR = "2",
                                                MAT_NR = itemInsert.No,
                                                MEINH = itemInsert.BaseUnitOfMeasure,
                                                //DiscountType = "" + model.DiscountType,
                                                //DiscountValue = "" + model.DiscountValue,
                                                MAT_QUAN = "" + model.Quantity,
                                                MATGROUP = item,
                                                ScaleType = model.ScaleType,
                                                CREATEDDATE = dateNow
                                            };
                                            db.SetupPromotionBUYs.Add(insert);
                                        }
                                    }

                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferBuyModel>(true, "Success", null);
                                }
                                else
                                {
                                    return new Tuple<bool, string, OfferBuyModel>(false, $"Vui lòng chọn nhóm item", null);
                                }
                            }
                        }

                    }
                    var updateLinkBuy = checkHeader.FirstOrDefault();
                    if (updateLinkBuy != null)
                    {
                        updateLinkBuy.BUYLINKCAT = conditionBuy;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferBuyModel>(false, ex.Message, dataModel);
                }
            }

            return result;
        }

       
        public Tuple<bool, string> SetupDeleteOfferBuy(string offerNo, int lineNo, int lineType, string groupCode)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == offerNo);
                    if (checkHeader.Any(d => d.IsApprove == true))
                    {
                        return new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT,nên không xóa được");
                    }
                    if (lineType == 0)
                    {
                        var checkID = db.SetupPromotionBUYs.FirstOrDefault(d => d.BBYNR == offerNo && d.ID == lineNo);
                        if (checkID != null)
                        {
                            db.SetupPromotionBUYs.Remove(checkID);
                            db.SaveChanges();
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} hoặc lineNo {lineNo} trong hệ thống");
                        }
                    }
                    else
                    {
                        var checkID = db.SetupPromotionBUYs.Where(d => d.BBYNR == offerNo && d.BUYTYPE == "MGP" && (d.MATGROUP == groupCode || string.IsNullOrEmpty(d.MATGROUP)));
                        if (checkID != null)
                        {
                            db.SetupPromotionBUYs.RemoveRange(checkID);
                            db.SaveChanges();
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} hoặc groupCode {groupCode} trong OfferBuys");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public Tuple<bool, string> SetupInsertUpdateHeaderBuy(string offerNo, string buyLinkCat, string checkMinValue, string minValue)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                var result = new Tuple<bool, string>(false, "");
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var check = db.SetupPromotionHEADERs.FirstOrDefault(d => d.BBYNR == offerNo);
                    if (check != null)
                    {
                        if (check.IsApprove == true)
                        {
                            result = new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT, nên không thể thêm mới hoặc update");
                            return result;
                        }

                        check.BUYLINKCAT = buyLinkCat;
                        check.TOTALMINVALUE = checkMinValue;
                        check.MINVALUE = minValue;

                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, "Success");
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tìm thấy dữ liệu CTKM {offerNo}");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, ex.Message);
                }
                return result;
            }
        }

        public GetBBModel SetupGetOfferBuy(string bonusBuy)
        {
            var result = new GetBBModel();
            var offerBuy = new List<OfferBuyModel>();
            if (string.IsNullOrEmpty(bonusBuy))
            {
                result = new GetBBModel
                {
                    OfferNo = "",
                    GetOfferBuy = offerBuy,
                    GetOfferHearder = new OfferHeaderModel()
                };
                return result;
            }
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.SetupPromotionBUYs.Where(d => d.BBYNR == bonusBuy).Select(a => new SetupPromotionBUYModel
                    {
                        ID = a.ID,
                        BBYNR = a.BBYNR,
                        BUYTYPE = a.BUYTYPE,
                        MATGROUP = a.MATGROUP,
                        MAT_NR = a.MAT_NR,
                        MAT_QUAN = a.MAT_QUAN,
                        MEINH = a.MEINH,
                        ScaleType = a.ScaleType,
                        //DiscountType = a.DiscountType,
                        //DiscountValue = a.DiscountValue,
                        LINEINDICATOR = a.LINEINDICATOR
                    }).ToList();

                    var dvtGroup = data.Where(d => d.BUYTYPE == "MGP").Select(a => a.MEINH).Distinct();

                    offerBuy = data.Where(d => d.BBYNR == bonusBuy).Select(a => new OfferBuyModel
                    {
                        OfferNo = a.BBYNR,
                        LineNo = a.BUYTYPE == "MGP" ? string.IsNullOrEmpty(a.MATGROUP) ? a.ID : data.Where(d => d.BBYNR == a.BBYNR && d.MATGROUP == a.MATGROUP).Max(d => d.ID) : a.ID,

                        LineType = a.BUYTYPE == "MGP" ? 1 : 0,
                        No = a.BUYTYPE == "MGP" ? (string.IsNullOrEmpty(a.MATGROUP) || db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP) == null) ? "" : db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP).GroupName : a.MAT_NR,
                        Description = a.BUYTYPE == "MGP" ? ((string.IsNullOrEmpty(a.MATGROUP) || db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP) == null) ? "" : db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP).GroupName) : (db.Items.FirstOrDefault(d => d.No == a.MAT_NR) != null ? db.Items.FirstOrDefault(d => d.No == a.MAT_NR).Description : ""),
                        UnitOfMeasure = a.BUYTYPE == "MGP" ? (dvtGroup.Count() > 1 ? "" : dvtGroup.FirstOrDefault().ToString()) : a.MEINH,
                        //DiscountType = int.Parse(a.DiscountType),
                        //DiscountValue = decimal.Parse(a.DiscountValue),
                        Quantity = float.Parse(a.MAT_QUAN),
                        ScaleType = a.ScaleType,
                        BonusBuyNo = a.MATGROUP

                    }).ToList();

                    offerBuy = offerBuy.GroupBy(g => new { g.OfferNo, g.LineNo, g.LineType, g.No, g.Description, g.UnitOfMeasure, g.DiscountType, g.DiscountValue, g.Quantity, g.ScaleType, g.BonusBuyNo })
                        .Select(g => new OfferBuyModel
                        {
                            OfferNo = g.Key.OfferNo,
                            LineNo = g.Key.LineNo,
                            LineType = g.Key.LineType,
                            No = g.Key.No,
                            Description = g.Key.Description,
                            UnitOfMeasure = g.Key.UnitOfMeasure,
                            //DiscountType = g.Key.DiscountType,
                            //DiscountValue = g.Key.DiscountValue,
                            Quantity = g.Key.Quantity,
                            ScaleType = g.Key.ScaleType,
                            BonusBuyNo = g.Key.BonusBuyNo

                        }).ToList();

                    var offerHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == bonusBuy)
                        .Select(a => new OfferHeaderModel
                        {
                            No = a.BBYNR,
                            OfferType = a.BBYTYPE,
                            BuyLinkCat = a.BUYLINKCAT,
                            CheckMinValue = a.TOTALMINVALUE,
                            TotalMinValue = a.MINVALUE,

                        }).FirstOrDefault();
                    if (offerHeader != null)
                    {
                        result = new GetBBModel
                        {
                            OfferNo = offerHeader.No,
                            GetOfferBuy = offerBuy,
                            GetOfferHearder = offerHeader
                        };
                    }

                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public GetBBModel SetupGetOfferGet(string offerNo)
        {
            var result = new GetBBModel();
            var listOfferGet = new List<OfferGetModel>();
            if (string.IsNullOrEmpty(offerNo))
            {
                result = new GetBBModel
                {
                    OfferNo = "",
                    OfferType="",
                    GetOfferGet = listOfferGet,
                    GetOfferHearder = new OfferHeaderModel()
                };
                return result;
            }
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var data = db.SetupPromotionGETs.Where(d => d.BBYNR == offerNo).Select(a => new SetupPromotionGETModel
                    {
                        ID = a.ID,
                        BBYNR = a.BBYNR,
                        GETTYPE = a.GETTYPE,
                        MATGROUP = a.MATGROUP,
                        MATERIALCODE = a.MATERIALCODE,
                        QTY = a.QTY,
                        MEINH = a.MEINH,
                        DISTYPE = a.DISTYPE,
                        SCALETYPE = a.SCALETYPE,
                        BBYVAL = a.BBYVAL,
                        BBYPER = a.BBYPER,
                        PRICEUNIT = a.PRICEUNIT,
                        LINEINDICATOR = a.LINEINDICATOR
                    }).ToList();

                    var dvtGroup = data.Where(d => d.GETTYPE == "MGP").Select(a => a.MEINH).Distinct();

                    listOfferGet = data.Where(d => d.BBYNR == offerNo).Select(a => new OfferGetModel
                    {
                        OfferNo = a.BBYNR,
                        LineNo = a.GETTYPE == "MGP" ? string.IsNullOrEmpty(a.MATGROUP) ? a.ID : data.Where(d => d.BBYNR == a.BBYNR && d.MATGROUP == a.MATGROUP).Max(d => d.ID) : a.ID,
                        LineType = a.GETTYPE == "MGP" ? 1 : 0,
                        No = a.GETTYPE == "MGP" ? (string.IsNullOrEmpty(a.MATGROUP) || db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP) == null) ? "" : db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP).GroupName : a.MATERIALCODE,
                        Description = a.GETTYPE == "MGP" ? ((string.IsNullOrEmpty(a.MATGROUP) || db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP) == null) ? "" : db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == a.MATGROUP).GroupName) : (db.Items.FirstOrDefault(d => d.No == a.MATERIALCODE) != null ? db.Items.FirstOrDefault(d => d.No == a.MATERIALCODE).Description : ""),
                        UnitOfMeasure = a.GETTYPE == "MGP" ? (dvtGroup.Count() > 1 ? "" : dvtGroup.FirstOrDefault().ToString()) : a.MEINH,
                        DiscountType = int.Parse(a.DISTYPE == "%" ? "0" : a.DISTYPE == "R" ? "1" : "2"),
                        DiscountValue = float.Parse(a.DISTYPE == "%" ? a.BBYPER : a.BBYVAL),
                        Quantity = float.Parse(a.QTY),
                        ScaleType = a.SCALETYPE,
                        BonusBuyNo = a.MATGROUP

                    }).ToList();

                    listOfferGet = listOfferGet.GroupBy(g => new { g.OfferNo, g.LineNo, g.LineType, g.No, g.Description, g.UnitOfMeasure, g.DiscountType, g.DiscountValue, g.Quantity, g.ScaleType, g.BonusBuyNo })
                        .Select(g => new OfferGetModel
                        {
                            OfferNo = g.Key.OfferNo,
                            LineNo = g.Key.LineNo,
                            LineType = g.Key.LineType,
                            No = g.Key.No,
                            Description = g.Key.Description,
                            UnitOfMeasure = g.Key.UnitOfMeasure,
                            DiscountType = g.Key.DiscountType,
                            DiscountValue = g.Key.DiscountValue,
                            Quantity = g.Key.Quantity,
                            ScaleType = g.Key.ScaleType,
                            BonusBuyNo = g.Key.BonusBuyNo

                        }).ToList();

                    var offerHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == offerNo)
                        .Select(a => new OfferHeaderModel
                        {
                            No = a.BBYNR,
                            OfferType = a.BBYTYPE,
                            GetLinkCat = a.GETLINKCAT,
                            CheckDiscount = a.TOTALDISCOUNT,
                            DiscountType = a.TOTALDISCOUNTTYPE,
                            DiscountValue = a.TOTALDISCOUNTVALUE

                        }).FirstOrDefault();
                    result = new GetBBModel
                    {
                        OfferNo = offerNo,
                        OfferType = offerHeader.OfferType,
                        GetOfferHearder = offerHeader,
                        GetOfferGet = listOfferGet
                    };

                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public Tuple<bool, string, OfferGetModel> SetupInsertUpdateOfferGet(OfferGetModel model, string conditionGet, string itemHidden)
        {
            var dataModel = new OfferGetModel();
            var result = new Tuple<bool, string, OfferGetModel>(true, "", dataModel);
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var dateNow = DateTime.Now;
                    var checkHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == model.OfferNo);
                    if (checkHeader.Any(d => d.IsApprove == true))
                    {
                        return new Tuple<bool, string, OfferGetModel>(false, $"CTKM {model.OfferNo} đã được DUYỆT,nên không thể thêm mới hoặc chỉnh sửa", dataModel);
                    }
                    var offerGet = db.SetupPromotionGETs.Where(d => d.BBYNR == model.OfferNo).ToList();
                    if (model.LineType == 0) // theo Item
                    {
                        //var checkGet = db.SetupPromotionBUYs.Any(d => d.BBYNR == model.OfferNo && d.MAT_NR == model.No && model.No != "");
                        //if (checkGet)
                        //{
                        //    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferGetModel
                        //    {
                        //        No = a.No,
                        //        Description = a.Description,
                        //        UnitOfMeasure = a.SalesUnitOfMeasure

                        //    }).FirstOrDefault();
                        //    return new Tuple<bool, string, OfferGetModel>(false, $"CTKM {model.OfferNo} đã tồn tại item {model.No} trong sản phẩm mua", infoItemHidden);
                        //}

                        if (itemHidden.Trim() != model.No.Trim())
                        {
                            var checkNoGet = offerGet.Where(d => d.MATERIALCODE == model.No).ToList();
                            if (checkNoGet != null && checkNoGet.Count > 0)
                            {
                                var groupCode = checkNoGet.FirstOrDefault().MATGROUP;
                                if (!string.IsNullOrEmpty(groupCode))
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferGetModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại item {model.No} trong group {groupCode}", infoItemHidden);
                                }
                                else
                                {
                                    var infoItemHidden = db.Items.Where(d => d.No == itemHidden).Select(a => new OfferGetModel
                                    {
                                        No = a.No,
                                        Description = a.Description,
                                        UnitOfMeasure = a.SalesUnitOfMeasure

                                    }).FirstOrDefault();
                                    return new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại item {model.No} trong sản phẩm khuyến mãi", infoItemHidden);
                                }
                            }
                        }

                        var checkNo = offerGet.FirstOrDefault(d => d.ID == model.LineNo);
                        if (checkNo != null)
                        {
                            checkNo.GETTYPE = model.LineType == 1 ? "MGP" : "MAT";
                            checkNo.MATERIALCODE = model.No;//ItemNo
                            checkNo.MEINH = model.UnitOfMeasure;
                            checkNo.QTY = "" + model.Quantity;
                            checkNo.DISTYPE = model.DiscountType == 0 ? "%" : model.DiscountType == 1 ? "R" : "P";
                            checkNo.BBYVAL = model.DiscountType == 0 ? "0" : "" + model.DiscountValue;
                            checkNo.BBYPER = model.DiscountType == 0 ? "" + model.DiscountValue : "0";
                            checkNo.SCALETYPE = model.ScaleType;
                        }
                        else
                        {
                            var insert = new SetupPromotionGET
                            {
                                BBYNR = model.OfferNo,
                                CREATEDDATE = dateNow,
                                LINEINDICATOR = "3",
                                GETTYPE = model.LineType == 1 ? "MGP" : "MAT",
                                MATERIALCODE = model.No,
                                QTY = "" + model.Quantity,
                                MEINH = model.UnitOfMeasure,
                                DISTYPE = model.DiscountType == 0 ? "%" : model.DiscountType == 1 ? "R" : "P",
                                BBYVAL = model.DiscountType == 0 ? "0" : "" + model.DiscountValue,
                                BBYPER = model.DiscountType == 0 ? "" + model.DiscountValue : "0",
                                SCALETYPE = model.ScaleType
                            };
                            db.SetupPromotionGETs.Add(insert);
                            db.SaveChanges();

                            //var checkOfferHeader = db.OfferHeaders.FirstOrDefault(d => d.No == model.OfferNo);
                            //if (checkOfferHeader != null)
                            //{
                            //    var conditionBuySave = conditionBuy == "OR" ? 1 : 2;
                            //    checkOfferHeader.ConditionBuy = conditionBuySave;
                            //}

                            model = new OfferGetModel
                            {
                                OfferNo = insert.BBYNR,
                                LineNo = insert.ID,
                                LineType = insert.GETTYPE == "MGP" ? 1 : 0,
                                No = insert.MATERIALCODE,
                                Description = db.Items.FirstOrDefault(d => d.No == insert.MATERIALCODE) == null ? "" : db.Items.FirstOrDefault(d => d.No == insert.MATERIALCODE).Description,
                                UnitOfMeasure = insert.MEINH,
                                DiscountType = insert.DISTYPE == "%" ? 0 : insert.DISTYPE == "R" ? 1 : 2,
                                DiscountValue = insert.DISTYPE == "%" ? (string.IsNullOrEmpty(insert.BBYPER) ? 0 : float.Parse(insert.BBYPER)) : (string.IsNullOrEmpty(insert.BBYVAL) ? 0 : float.Parse(insert.BBYVAL)),
                                Quantity = string.IsNullOrEmpty(insert.MATERIALCODE) ? 0 : int.Parse(insert.MATERIALCODE),
                                ScaleType = insert.SCALETYPE
                            };
                        }
                        db.SaveChanges();
                        dataModel = model;

                        result = new Tuple<bool, string, OfferGetModel>(true, "Success", dataModel);
                    }
                    else // Theo group Item
                    {
                        if (string.IsNullOrEmpty(model.GroupCode))
                        {
                            var checkNo = offerGet.FirstOrDefault(d => d.ID == model.LineNo && string.IsNullOrEmpty(d.MATERIALCODE));
                            if (checkNo != null)
                            {
                                checkNo.GETTYPE = "MGP";
                                db.SaveChanges();
                                result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                            }
                        }
                        else
                        {
                            //"["NuocUong"]"
                            if (!string.IsNullOrEmpty(model.GroupCode) && model.GroupCode.Contains("["))//List GroupCode (trước cho chọn nhiều groupCode)
                            {
                                var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                var checkGroup = offerGet.Any(d => d.MATGROUP == listGroup.FirstOrDefault());
                                if (checkGroup)
                                {
                                    return result = new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm khuyến mãi", null);
                                }
                                else
                                {
                                    var checkGroupOfferBuy = db.SetupPromotionBUYs.Any(d => d.BBYNR == model.OfferNo && d.MATGROUP == listGroup.FirstOrDefault());
                                    if (checkGroupOfferBuy)
                                    {
                                        return result = new Tuple<bool, string, OfferGetModel>(false, $"Đã tồn tại group {model.GroupCode} trong sản phẩm mua", null);
                                    }
                                }
                            }

                            if (model.GroupCode == model.BonusBuyNo)//Update các thông số
                            {
                                var checkGroupCode = offerGet.Where(d => d.MATGROUP == model.BonusBuyNo).ToList();
                                if (checkGroupCode != null && checkGroupCode.Count > 0)
                                {
                                    checkGroupCode.ForEach(x =>
                                    {
                                        x.DISTYPE = model.DiscountType == 0 ? "%" : model.DiscountType == 1 ? "R" : "P";
                                        x.BBYVAL = model.DiscountType == 0 ? "0" : "" + model.DiscountValue;
                                        x.BBYPER = model.DiscountType == 0 ? "" + model.DiscountValue : "0";
                                        x.QTY = "" + model.Quantity;
                                        x.SCALETYPE = model.ScaleType;
                                    });

                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                                }
                            }
                            else // insert list item vào OfferGet
                            {
                                //var offerGetOld = offerGet.Where(d => d.MATGROUP == model.BonusBuyNo && !string.IsNullOrEmpty(model.BonusBuyNo));
                                var offerGetOld = offerGet.Where(d => d.MATGROUP == model.BonusBuyNo && d.GETTYPE == "MGP");
                                if (offerGetOld != null && offerGetOld.ToList().Count > 0)
                                {
                                    db.SetupPromotionGETs.RemoveRange(offerGetOld);
                                }
                                var listGroup = JsonConvert.DeserializeObject<List<string>>(model.GroupCode);
                                if (listGroup.Count > 0)
                                {
                                    var listItemBuyGroup = new List<string>();

                                    foreach (var item in listGroup)
                                    {
                                        var checkGroupBuy = db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == item);

                                        var listItemGroup = JsonConvert.DeserializeObject<List<string>>(checkGroupBuy.ListItemNo);

                                        var listItemInsert = db.Items.Where(d => listItemGroup.Contains(d.No)).ToList();

                                        foreach (var itemInsert in listItemInsert)
                                        {
                                            var insert = new SetupPromotionGET
                                            {
                                                BBYNR = model.OfferNo,
                                                GETTYPE = "MGP",
                                                LINEINDICATOR = "3",
                                                MATERIALCODE = itemInsert.No,
                                                MEINH = itemInsert.BaseUnitOfMeasure,
                                                DISTYPE = model.DiscountType == 0 ? "%" : model.DiscountType == 1 ? "R" : "P",
                                                BBYVAL = model.DiscountType == 0 ? "0" : "" + model.DiscountValue,
                                                BBYPER = model.DiscountType == 0 ? "" + model.DiscountValue : "0",
                                                QTY = "" + model.Quantity,
                                                MATGROUP = item,
                                                SCALETYPE = model.ScaleType,
                                                CREATEDDATE = dateNow
                                            };
                                            db.SetupPromotionGETs.Add(insert);
                                        }
                                    }

                                    db.SaveChanges();
                                    result = new Tuple<bool, string, OfferGetModel>(true, "Success", null);
                                }
                                else
                                {
                                    return new Tuple<bool, string, OfferGetModel>(false, $"Vui lòng chọn nhóm item", null);
                                }
                            }
                        }
                    }
                    var updateLinkBuy = checkHeader.FirstOrDefault();
                    if (updateLinkBuy != null)
                    {
                        updateLinkBuy.GETLINKCAT = conditionGet;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string, OfferGetModel>(false, ex.Message, dataModel);
                }
            }

            return result;
        }

        public Tuple<bool, string> SetupDeleteOfferGet(string offerNo, int lineNo, int lineType, string groupCode)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == offerNo);
                    if (checkHeader.Any(d => d.IsApprove == true))
                    {
                        return new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT,nên không xóa được");
                    }
                    if (lineType == 0)
                    {
                        var checkID = db.SetupPromotionGETs.FirstOrDefault(d => d.BBYNR == offerNo && d.ID == lineNo);
                        if (checkID != null)
                        {
                            db.SetupPromotionGETs.Remove(checkID);
                            db.SaveChanges();
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và lineNo {lineNo} trong hệ thống");
                        }
                    }
                    else
                    {
                        var checkID = db.SetupPromotionGETs.Where(d => d.BBYNR == offerNo && d.GETTYPE == "MGP" && (d.MATGROUP == groupCode || string.IsNullOrEmpty(d.MATGROUP)));
                        if (checkID != null)
                        {
                            db.SetupPromotionGETs.RemoveRange(checkID);
                            db.SaveChanges();
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và groupCode {groupCode} trong OfferGet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public Tuple<bool, string> SetupDeleteOfferGetByOfferNo(string offerNo)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkHead = db.SetupPromotionHEADERs.Any(d => d.BBYNR == offerNo && d.IsApprove == true);
                    if (checkHead)
                    {
                        result = new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT nên không xóa được");
                        return result;
                    }

                    var listDel = db.SetupPromotionGETs.Where(d => d.BBYNR == offerNo);
                    if (listDel != null)
                    {
                        db.SetupPromotionGETs.RemoveRange(listDel);
                        db.SaveChanges();
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} trong hệ thống");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }
        public Tuple<bool, string> SetupInsertUpdateHeaderGet(string offerNo, string getLinkCat, string checkDiscount, string typeDiscount, string discountValue)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                var result = new Tuple<bool, string>(false, "");
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var check = db.SetupPromotionHEADERs.FirstOrDefault(d => d.BBYNR == offerNo);
                    if (check != null)
                    {
                        if (check.IsApprove == true)
                        {
                            result = new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT, nên không thể thêm mới hoặc update");
                            return result;
                        }

                        check.GETLINKCAT = getLinkCat;
                        check.TOTALDISCOUNT = checkDiscount;
                        check.TOTALDISCOUNTTYPE = typeDiscount;
                        check.TOTALDISCOUNTVALUE = discountValue;

                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, "Success");
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tìm thấy dữ liệu CTKM {offerNo}");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, ex.Message);
                }
                return result;
            }
        }

        public Tuple<bool, string> SetupInsertUpdateOfferSite(string offerNo, string siteGroupCode)
        {
            var result = new Tuple<bool, string>(false, "Fail");
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var listError = new List<string>();
                    db.Database.CommandTimeout = 2 * 60;
                    var dateTime = DateTime.Now;
                    var checkHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == offerNo);
                    if (checkHeader.Any(d => d.IsApprove == true))
                    {
                        return new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT,nên không thể thêm mới hoặc chỉnh sửa");
                    }

                    var checkData = db.SetupGroupSites.Where(d => d.GroupCode.ToUpper() == siteGroupCode.ToUpper()).Select(a => new GroupSiteModel
                    {
                        GroupCode = a.GroupCode,
                        GroupName = a.GroupName,
                        ListStore = a.ListStore

                    }).FirstOrDefault();

                    if (checkData == null)
                    {
                        result = new Tuple<bool, string>(false, "Không tìm thấy dữ liệu group cửa hàng");
                    }
                    else
                    {
                        var checkOfferSite = db.SetupPromotionSITEs.Where(d => d.BBYNR == offerNo);
                        if (checkOfferSite != null)
                        {
                            db.SetupPromotionSITEs.RemoveRange(checkOfferSite);
                            db.SaveChanges();
                        }

                        if (checkData.ListStore == "ALL")
                        {
                            db.SetupPromotionSITEs.Add(new SetupPromotionSITE
                            {
                                BBYNR = offerNo,
                                SITEGROUPCODE = siteGroupCode,
                                SITECODE = "ALL",
                                CREATEDDATE = dateTime
                            });
                            db.SaveChanges();
                            result = new Tuple<bool, string>(true, $"Setup tất cả cửa hàng cho CTKM {offerNo} thành công");
                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(checkData.ListStore);
                            var listOfferSite = listStore.Select(a => new SetupPromotionSITE
                            {
                                BBYNR = offerNo,
                                SITEGROUPCODE = siteGroupCode,
                                SITECODE = a.ToString(),
                                CREATEDDATE = dateTime
                            });
                            db.SetupPromotionSITEs.AddRange(listOfferSite);
                            db.SaveChanges();
                            result = new Tuple<bool, string>(true, $"Setup {listOfferSite.Count()} cửa hàng cho CTKM {offerNo} thành công");
                        }
                    }

                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }

            return result;
        }

        public List<OfferSiteModel> SetupGetOfferSite(string bonusBuy)
        {
            var result = new List<OfferSiteModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.SetupPromotionSITEs.Where(d => d.BBYNR == bonusBuy).Select(a => new OfferSiteModel
                    {
                        OfferNo = a.BBYNR,
                        //StoreNo = a.StoreNo,
                        PriceGroupCode = a.SITEGROUPCODE,
                        GroupName = db.SetupGroupSites.FirstOrDefault(d => d.GroupCode == a.SITEGROUPCODE).GroupName
                    }).Distinct().ToList();
                }
                catch (Exception ex)
                {

                }
                return result;
            }
        }

        public Tuple<bool, string> SetupDeleteOfferSite(string offerNo, string groupSite)
        {
            var result = new Tuple<bool, string>(true, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkHeader = db.SetupPromotionHEADERs.Where(d => d.BBYNR == offerNo);
                    if (checkHeader.Any(d => d.IsApprove == true))
                    {
                        return new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT,nên không xóa được");
                    }
                    var checkID = db.SetupPromotionSITEs.Where(d => d.BBYNR == offerNo && d.SITEGROUPCODE == groupSite).ToList();
                    if (checkID != null && checkID.Count > 0)
                    {
                        db.SetupPromotionSITEs.RemoveRange(checkID);
                        db.SaveChanges();
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Không tồn tại offerNo {offerNo} và groupSite {groupSite} trong OfferSite");
                    }

                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }


        public Tuple<bool, string> SetupDeleteGroupSite(string groupSite)
        {
            var result = new Tuple<bool, string>(false, $"Group site {groupSite} fail");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkGroupSite = db.SetupPromotionSITEs.Any(d => d.SITEGROUPCODE == groupSite);
                    if (!checkGroupSite)
                    {
                        var checkDelete = db.SetupGroupSites.Where(d => d.GroupCode.ToLower() == groupSite.ToLower());
                        if (checkDelete != null)
                        {
                            db.SetupGroupSites.RemoveRange(checkDelete);
                            db.SaveChanges();
                            result = new Tuple<bool, string>(true, $"Xóa thành công group site {groupSite}");
                        }
                        else
                        {
                            result = new Tuple<bool, string>(false, $"Không tồn tại group site {groupSite} trong hệ thống");
                        }
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, $"Đã setup group site {groupSite} này trong CTKM");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
            }
            return result;
        }

        public Tuple<bool, string> SetupDeleteGroupItem(string groupItem)
        {
            var result = new Tuple<bool, string>(false, $"Nhóm sản phẩm {groupItem} fail");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkgroupBUY = db.SetupPromotionBUYs.Any(d => d.MATGROUP.ToLower() == groupItem.ToLower());
                    if (checkgroupBUY)
                    {
                        result = new Tuple<bool, string>(false, $"Nhóm sản phẩm {groupItem} này đã được cài đặt, không xóa được");
                        return result;
                    }

                    var checkgroupGET = db.SetupPromotionGETs.Any(d => d.MATGROUP.ToLower() == groupItem.ToLower());

                    if (checkgroupGET)
                    {
                        result = new Tuple<bool, string>(false, $"Nhóm sản phẩm {groupItem} này đã được cài đặt, không xóa được");
                        return result;
                    }

                    var checkGroupDel = db.SetupGroupItems.FirstOrDefault(d => d.GroupCode == groupItem);
                    if (checkGroupDel != null)
                    {
                        db.SetupGroupItems.Remove(checkGroupDel);
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Xóa thành công nhóm sản phẩm {groupItem}");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
            }
            return result;
        }
        public Tuple<bool, string> FinishSetupPromotion(string offerNo, string status, string salesType, string offerName, string tuNgay, string denNgay, bool isVoucher, bool isApprove)
        {
            var result = new Tuple<bool, string>(false, "");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkHeader = db.SetupPromotionHEADERs.FirstOrDefault(d => d.BBYNR == offerNo);
                    var checkBuy = db.SetupPromotionBUYs.Where(d => d.BBYNR == offerNo).ToList();
                    var checkGet = db.SetupPromotionGETs.Where(d => d.BBYNR == offerNo).ToList();
                    var checkSite = db.SetupPromotionSITEs.Where(d => d.BBYNR == offerNo).ToList();

                    var checkValidGet = false;
                    var checkValidBuy = false;

                    var checkTotalValue = true;
                    var checkTotalDiscount = true;

                    if (checkHeader.TOTALMINVALUE == "1")
                    {
                        if (checkHeader.MINVALUE == "0" || string.IsNullOrEmpty(checkHeader.MINVALUE))
                        {
                            return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm mua,do chưa nhập GIÁ TRỊ TỔNG TIỀN TỐI THIỂU");
                        }
                        if (checkBuy.Count() > 0)
                        {
                            if (checkBuy.Any(d => string.IsNullOrEmpty(d.MAT_NR)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm mua,do chưa setup item");
                            }
                            if (checkBuy.Any(d => d.MAT_QUAN == "0" || string.IsNullOrEmpty(d.MAT_QUAN)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm mua,do chưa setup số lượng");
                            }
                            if (checkBuy.Any(d => string.IsNullOrEmpty(d.MEINH)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm mua,do chưa setup ĐVT");
                            }
                        }
                        checkTotalValue = false;
                    }
                    else
                    {
                        if (checkBuy.Count() > 0)
                        {
                            if (checkBuy.Any(d => string.IsNullOrEmpty(d.MAT_NR)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm mua,do chưa setup item");
                            }
                            if (checkBuy.Any(d => d.MAT_QUAN == "0"))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm mua,do chưa setup số lượng");
                            }
                            if (checkBuy.Any(d => string.IsNullOrEmpty(d.MEINH)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm mua,do chưa setup ĐVT");
                            }
                        }
                        else if (checkTotalValue)
                        {
                            checkValidBuy = true;
                        }
                    }
                    ////////////////////////CHECK GET////////////////////
                    if (checkHeader.TOTALDISCOUNT == "1")
                    {
                        if (checkHeader.TOTALDISCOUNTVALUE == "0" || string.IsNullOrEmpty(checkHeader.TOTALDISCOUNTVALUE))
                        {
                            return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm khuyến mãi,do chưa nhập GIÁ TRỊ KHUYẾN MÃI");
                        }
                        if (checkGet.Count() > 0)
                        {
                            if (checkGet.Any(d => string.IsNullOrEmpty(d.MATERIALCODE)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm khuyến mãi,do chưa setup item cho khuyến mãi");
                            }

                            if (checkGet.Any(d => d.QTY == "0" || string.IsNullOrEmpty(d.QTY)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm khuyến mãi,do chưa setup số lượng");
                            }
                            if (checkGet.Any(d => string.IsNullOrEmpty(d.MEINH)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm khuyến mãi, do chưa setup ĐVT");
                            }
                        }
                        checkTotalDiscount = false;
                    }
                    else
                    {
                        if (checkGet.Count() > 0)
                        {
                            if (checkGet.Any(d => string.IsNullOrEmpty(d.MATERIALCODE)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm khuyến mãi,do chưa setup item cho khuyến mãi");
                            }

                            if (checkGet.Any(d => d.QTY == "0"))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm khuyến mãi,do chưa setup số lượng");
                            }
                            if (checkGet.Any(d => string.IsNullOrEmpty(d.MEINH)))
                            {
                                return new Tuple<bool, string>(false, $"Vui lòng kiểm tra TAB sản phẩm khuyến mãi, do chưa setup ĐVT");
                            }
                        }
                        else if (checkTotalDiscount)
                        {
                            checkValidGet = true;
                        }
                    }

                    //////////CHECK GET VA BUY/////////////
                    if (checkValidGet && checkValidBuy)
                    {
                        return new Tuple<bool, string>(false, $"Chưa setup xong chương trình khuyến mãi");
                    }

                    if (checkSite.Count() == 0)
                    {
                        return new Tuple<bool, string>(false, $"Chưa setup danh sách cửa hàng cho chương trình khuyến mãi {offerNo}");
                    }

                    var siteGroupCode = string.Join(",", checkSite.Select(a => a.SITEGROUPCODE).Distinct());

                    if (checkHeader != null)
                    {
                        //if (checkHeader.STATUS == "0")
                        //{
                        //    return new Tuple<bool, string>(false, $"CTKM {offerNo} đã được DUYỆT, nên không xác nhận hoàn thành được");
                        //}

                        checkHeader.STATUS = status;
                        if (isApprove)
                        {
                            checkHeader.IsVoucher = isVoucher;
                            checkHeader.BBYTEXT = offerName;
                            checkHeader.PROMOTIONTEXT = offerName;
                            checkHeader.SalesType = salesType;
                            checkHeader.VALIDFROM = tuNgay;
                            checkHeader.VALIDTO = denNgay;
                            checkHeader.SITEGROUPCODE = siteGroupCode;
                        }
                        checkHeader.IsApprove = isApprove;
                        db.SaveChanges();
                    }

                    var exec = db.Database.ExecuteSqlCommand("[dbo].[Setup_Promotion_Insert] @BBY ",
                    new SqlParameter("@BBY", offerNo));

                    var approve = isApprove == true ? " DUYỆT " : " BỎ DUYỆT ";
                    var message = status == "2" ? "Deactivated" : status == "1" ? "Planned" : "Activated";
                    result = new Tuple<bool, string>(true, $"Xác nhận {message} {approve} CTKM {offerNo} thành công");
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, $"{JsonConvert.SerializeObject(ex)}");
            }
            return result;

        }

        public Tuple<bool, string> UpdateStatusPromotion(string offerNo, string status)
        {
            var result = new Tuple<bool, string>(false, "Fail");
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkHeader = db.SetupPromotionHEADERs.FirstOrDefault(d => d.BBYNR == offerNo);

                    if (checkHeader != null)
                    {
                        checkHeader.STATUS = status;
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Setup thành công trạng thái CTKM {offerNo}");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, $"{JsonConvert.SerializeObject(ex)}");
            }
            return result;

        }

        public List<OfferHeaderModel> LoadExtraFeeCombo(List<string> listOfferSelected, string offerNo, string offerName, out int total, int skip, int take)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 120;
                    var data = db.OfferHeaders.Where(d => d.Status == 0 && d.OfferType == "ZB08").Select(a => new OfferHeaderModel
                    {
                        No = a.No,
                        Description = a.Description,
                        OfferType = a.OfferType,
                        IsOfferSelected = listOfferSelected.Any(e => e == a.No)
                    });
                    if (!string.IsNullOrEmpty(offerNo))
                    {
                        data = data.Where(m => m.No == offerNo);
                    }
                    if (!string.IsNullOrEmpty(offerName))
                    {
                        data = data.Where(m => m.Description.ToLower().Contains(offerName.ToLower()));
                    }

                    total = data.Count();
                    var result = data.OrderByDescending(d => d.No).Skip(skip).Take(take).ToList();
                    return result;
                }
                catch (Exception ex)
                {
                    total = 0;
                }

                return new List<OfferHeaderModel>();
            }
        }
        #endregion


        //--------------------------------------

        public Tuple<bool, string> CreateSpecialCombo(CreateSetupSpecialComboModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkCodeHeader = db.SpecialComboHeaders.FirstOrDefault(d => d.Code == model.Code);
                    var checkCodeLine = db.SpecialComboLines.FirstOrDefault(d => d.Code == model.Code);

                    //var listStore = string.Empty;
                    //if(model.StoreNo.ToString() != "ALL")
                    //{
                    //    listStore = JsonConvert.SerializeObject(model.StoreNo);
                    //}

                    if (checkCodeHeader != null)
                    {
                        var pkey = $"{model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo}";
                        var checkPkey = db.SpecialComboLines.Where(a =>a.Pkey == pkey);

                        var maxCounterLine = db.SpecialComboLines.Max(x =>x.Counter) ?? 0;
                        if (checkPkey != null)
                        {
                            var insertLine = new SpecialComboLine
                            {
                                Code = model.Code,
                                GroupCode = model.GroupCode,
                                GroupName = model.GroupName,
                                ItemNo = model.ItemNo,
                                ItemName = db.Items.Where(x => x.No == model.ItemNo.ToString()).Select(a =>a.Description).FirstOrDefault(),
                                UOM = model.UOM,
                                MinimumQuantity = double.Parse(model.MinQuantity),
                                MaximumQuantity = double.Parse(model.MaxQuantity),
                                IsDefault = (model.IsMember == "1") ? true : false,
                                IsDynamicPrice = (model.IsEnable == "1") ? true: false,
                                IsEnable = (model.IsEnable == "1") ? true: false,
                                IsSendSAP = (model.IsMember == "1") ? true : false,
                                Order = int.Parse(model.Order),
                                CreatedDate = model.CreatedDate,
                                CreatedBy = model.CreatedBy,
                                UpdatedDate = model.UpdatedDate,
                                UpdatedBy = model.UpdatedBy,
                                Pkey = $"{model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo}",
                                Counter = maxCounterLine + 1
                            };

                            var maxCounterStore = db.SpecialComboStores.Max(x =>x.Counter) ?? 0;
                            var insertStore1= new List<SpecialComboStore>();
                            foreach (var item in model.StoreNo)
                            {
                                insertStore1.Add(new SpecialComboStore
                                {
                                    Code = model.Code,
                                    StoreNo = item.ToString(),
                                    IsEnable = (model.StatusStore == "1") ? true : false,
                                    CreatedDate = model.CreatedDate,
                                    CreatedBy = model.CreatedBy,
                                    UpdatedDate = model.UpdatedDate,
                                    UpdatedBy = model.UpdatedBy,
                                    Counter = maxCounterStore + 1
                                });
                            }

                            db.SpecialComboStores.AddRange(insertStore1);
                            db.SaveChanges();

                            result = new Tuple<bool, string>(true, $"Thêm mới GroupCode {model.GroupCode} thành công");                           
                        }
                        else
                        {
                            result = new Tuple<bool, string>(true, $"GroupCode {model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại");
                        }
                    }
                    else
                    {
                        var maxCounter = db.SpecialComboHeaders.Max(x =>x.Counter) ?? 0;
                        var inSert = new SpecialComboHeader
                        {
                            Code = model.Code,
                            SalesType = model.SalesType,
                            Name = model.Name,
                            FromDate = model.FromDate,
                            ToDate = model.ToDate,
                            ComboQuantity = double.Parse(model.ComboQuantity),
                            Amount = double.Parse(model.Amount),
                            IsMember = (model.IsMember == "1") ? true : false,
                            MemberCode = model.MemberCode,
                            IsEnable = (model.IsEnable == "1") ? true: false,
                            CreatedDate = model.CreatedDate,
                            CreatedBy = model.CreatedBy,
                            UpdatedDate = model.UpdatedDate,
                            UpdatedBy = model.UpdatedBy,
                            Pkey = model.Code,
                            Counter = maxCounter + 1
                        };

                        db.SpecialComboHeaders.Add(inSert);
                        db.SaveChanges();

                        var maxCounterLine = db.SpecialComboLines.Max(x =>x.Counter) ?? 0;
                        var insertLine = new SpecialComboLine
                        {
                            Code = model.Code,
                            GroupCode = model.GroupCode,
                            GroupName = model.GroupName,
                            ItemNo = model.ItemNo,
                            ItemName = db.Items.Where(x => x.No == model.ItemNo.ToString()).Select(a =>a.Description).FirstOrDefault(),
                            UOM = model.UOM,
                            MinimumQuantity = double.Parse(model.MinQuantity),
                            MaximumQuantity = double.Parse(model.MaxQuantity),
                            IsDefault = (model.IsMember == "1") ? true : false,
                            IsDynamicPrice = (model.IsEnable == "1") ? true: false,
                            IsEnable = (model.IsEnable == "1") ? true: false,
                            IsSendSAP = (model.IsMember == "1") ? true : false,
                            Order = int.Parse(model.Order),
                            CreatedDate = model.CreatedDate,
                            CreatedBy = model.CreatedBy,
                            UpdatedDate = model.UpdatedDate,
                            UpdatedBy = model.UpdatedBy,
                            Pkey = $"{model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo}",
                            Counter = maxCounterLine + 1
                        };

                        db.SpecialComboLines.Add(insertLine);
                        db.SaveChanges();

                        var maxCounterStore = db.SpecialComboStores.Max(x =>x.Counter) ?? 0;
                        var insertStore = new List<SpecialComboStore>();
                        foreach (var item in model.StoreNo)
                        {
                            insertStore.Add(new SpecialComboStore
                            {
                                Code = model.Code,
                                StoreNo = item.ToString(),
                                IsEnable = (model.StatusStore == "1") ? true : false,
                                CreatedDate = model.CreatedDate,
                                CreatedBy = model.CreatedBy,
                                UpdatedDate = model.UpdatedDate,
                                UpdatedBy = model.UpdatedBy,
                                Counter = maxCounterStore + 1
                            });
                        }

                        db.SpecialComboStores.AddRange(insertStore);
                        db.SaveChanges();

                        result = new Tuple<bool, string>(true, $"Thêm mới {model.Code} thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }
            return result;
        }

        public Tuple<bool, string> CreateSpecialComboV2(CreateSetupSpecialComboModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkCodeHeader = db.SpecialComboHeaders.FirstOrDefault(d => d.Code == model.Code);
                    var checkCodeLine = db.SpecialComboLines.FirstOrDefault(d => d.Code == model.Code);

                    // Check gia khi isDynamicPrice = 2

                    //var totalPriceSales = model.ListComboLine.GroupBy(x => new { x.Code, x.IsDynamicPrice }, (key, g) => new
                    //{
                    //    GroupCode = key.Code,
                    //    IsDynamicPrice = key.IsDynamicPrice,
                    //    ListData = g.Sum(x =>x.UnitPrice)
                    //}).Where(x => x.IsDynamicPrice != "2").Select(x =>x.ListData).ToList();

                    //var totalAmountSales = ((double)totalPriceSales.Sum(x =>x.Value));

                    //foreach (var item in model.ListComboLine)
                    //{
                    //    if (item.IsDynamicPrice == "2")
                    //    {
                    //        if (totalAmountSales > double.Parse(model.Amount))
                    //        {
                    //            result = new Tuple<bool, string>(
                    //                false,
                    //                $"Tổng giá trị các sản phẩm không có giá bán động {totalAmountSales} lớn hơn giá trị của Combo {double.Parse(model.Amount)}. Vui lòng kiểm tra lại giá các sản phẩm khi cài đặt Combo có giá bán động");
                    //            return result;
                    //        }
                    //    }
                    //}

                    if (checkCodeHeader != null)
                    {
                        var pkey = $"{model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo}";
                        var checkPkey = db.SpecialComboLines.Where(a =>a.Pkey == pkey);

                        var maxCounterLine = db.SpecialComboLines.Max(x =>x.Counter) ?? 0;
                        if (checkPkey != null)
                        {
                            var insertLine1 = model.ListComboLine.Select(x => new SpecialComboLine
                            {
                                Code = model.Code,
                                GroupCode = x.GroupCode,
                                GroupName = x.GroupName,
                                ItemNo = x.ItemNo,
                                ItemName = db.Items.Where(a => a.No == x.ItemNo.ToString()).Select(a =>a.Description).FirstOrDefault(),
                                UOM = model.UOM,
                                MinimumQuantity = double.Parse(x.MinimumQuantity),
                                MaximumQuantity = double.Parse(x.MaximumQuantity),
      
                                IsDynamicPrice = (x.IsDynamicPrice == "2") ? true: false,
                                IsDefault = (x.IsDynamicPrice == "0") ? false : true,

                                //IsEnable = (x.IsEnable == "1") ? true: false,

                                IsEnable = true,    // mac dinh la true
                                IsSendSAP = true,   // mac dinh la true

                                Order = int.Parse(model.Order),
                                CreatedDate = model.CreatedDate,
                                CreatedBy = model.CreatedBy,
                                UpdatedDate = model.UpdatedDate,
                                UpdatedBy = model.UpdatedBy,
                                Pkey = $"{model.Code}{"-"}{x.GroupCode}{"-"}{x.ItemNo}",
                                Counter = maxCounterLine + 1
                            }).ToList();

                            var maxCounterStore = db.SpecialComboStores.Max(x =>x.Counter) ?? 0;
                            var insertStore1= new List<SpecialComboStore>();
                            foreach (var item in model.StoreNo)
                            {
                                insertStore1.Add(new SpecialComboStore
                                {
                                    Code = model.Code,
                                    StoreNo = item.ToString(),
                                    IsEnable = (model.StatusStore == "1") ? true : false,
                                    CreatedDate = model.CreatedDate,
                                    CreatedBy = model.CreatedBy,
                                    UpdatedDate = model.UpdatedDate,
                                    UpdatedBy = model.UpdatedBy,
                                    Counter = maxCounterStore + 1,
                                    Pkey = $"{model.Code}{"-"}{item}"
                                });
                            }

                            db.SpecialComboLines.AddRange(insertLine1);
                            db.SpecialComboStores.AddRange(insertStore1);
                            db.SaveChanges();

                            result = new Tuple<bool, string>(true, $"Thêm mới nhóm {model.GroupCode} thành công");
                        }
                        else
                        {
                            result = new Tuple<bool, string>(true, $"Mã nhóm {model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại");
                        }

                    }
                    else
                    {
                        var maxCounter = db.SpecialComboHeaders.Max(x =>x.Counter) ?? 0;
                        var inSert = new SpecialComboHeader
                        {
                            Code = model.Code,
                            SalesType = model.SalesType,
                            Name = model.Name,
                            FromDate = model.FromDate,
                            ToDate = model.ToDate,
                            ComboQuantity = double.Parse(model.ComboQuantity),
                            Amount = double.Parse(model.Amount),
                            IsMember = (model.IsMember == "1") ? true : false,
                            MemberCode = model.MemberCode,
                            IsEnable = (model.IsEnable == "1") ? true: false,
                            CreatedDate = model.CreatedDate,
                            CreatedBy = model.CreatedBy,
                            UpdatedDate = model.UpdatedDate,
                            UpdatedBy = model.UpdatedBy,
                            Pkey = model.Code,
                            Counter = maxCounter + 1
                        };
                      
                        var maxCounterLine = db.SpecialComboLines.Max(x =>x.Counter) ?? 0;
                        var insertLine2 = model.ListComboLine.Select(x => new SpecialComboLine
                        {
                            Code = model.Code,
                            GroupCode = x.GroupCode,
                            GroupName = x.GroupName,
                            ItemNo = x.ItemNo,
                            ItemName = db.Items.Where(a => a.No == x.ItemNo.ToString()).Select(a =>a.Description).FirstOrDefault(),
                            UOM = x.UOM,
                            MinimumQuantity = double.Parse(x.MinimumQuantity),
                            MaximumQuantity = double.Parse(x.MaximumQuantity),

                            //IsDefault = (x.IsDynamicPrice == "0" || x.IsDynamicPrice == "2") ? true : false, 
                            //IsDynamicPrice = (x.IsDynamicPrice == "2") ? true: false, // IsDynamicPrice == "2" ---> san pham co gia ban dong

                            IsDynamicPrice = (x.IsDynamicPrice == "2") ? true: false,
                            IsDefault = (x.IsDynamicPrice == "0") ? false : true,

                            //IsEnable = (x.IsEnable == "1") ? true: false,

                            IsEnable = true,    // mac dinh la true
                            IsSendSAP = true,  // mac dinh la true

                            Order = int.Parse(x.Order),
                            CreatedDate = model.CreatedDate,
                            CreatedBy = model.CreatedBy,
                            UpdatedDate = model.UpdatedDate,
                            UpdatedBy = model.UpdatedBy,
                            Pkey = $"{model.Code}{"-"}{x.GroupCode}{"-"}{x.ItemNo}",
                            Counter = maxCounterLine + 1
                        }).ToList();

                        var maxCounterStore = db.SpecialComboStores.Max(x =>x.Counter) ?? 0;
                        var insertStore = new List<SpecialComboStore>();

                        foreach (var item in model.StoreNo)
                        {
                            insertStore.Add(new SpecialComboStore
                            {
                                Code = model.Code,
                                StoreNo = item.ToString(),
                                //IsEnable = (model.StatusStore == "1") ? true : false,
                                IsEnable = true, // mac dinh
                                CreatedDate = model.CreatedDate,
                                CreatedBy = model.CreatedBy,
                                UpdatedDate = model.UpdatedDate,
                                UpdatedBy = model.UpdatedBy,
                                Counter = maxCounterStore + 1,
                                Pkey = $"{model.Code}{"-"}{item}"
                            });
                        }

                        db.SpecialComboHeaders.Add(inSert);
                        db.SpecialComboLines.AddRange(insertLine2);
                        db.SpecialComboStores.AddRange(insertStore);

                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Thêm mới Combo {model.Code} thành công");

                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }
            return result;
        }

        public Tuple<bool, string> UpdateSpecialComboV2(UpdateSetupSpecialComboModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkCodeComboHeader = db.SpecialComboHeaders.FirstOrDefault(d => d.Code == model.Code);
                    var checkCodeComboStore = db.SpecialComboStores.FirstOrDefault(a => a.Code == model.Code);

                    if (checkCodeComboHeader != null)
                    {
                        // Update: table SpecialComboHeader
                        var maxcounter = db.SpecialComboHeaders.Max(a =>a.Counter) ?? 0;

                        checkCodeComboHeader.Name = (model.Name == null) ? string.Empty: model.Name;
                        checkCodeComboHeader.SalesType = model.SalesType;
                        checkCodeComboHeader.ComboQuantity = double.Parse(model.ComboQuantity);
                        checkCodeComboHeader.Amount = double.Parse(model.Amount);
                        checkCodeComboHeader.IsMember = (model.IsMember == "1") ? true : false;
                        checkCodeComboHeader.MemberCode = (model.MemberCode == null) ? string.Empty: model.MemberCode;
                        checkCodeComboHeader.IsEnable = (model.IsEnable == "1") ? true : false;
                        checkCodeComboHeader.FromDate = model.FromDate;
                        checkCodeComboHeader.ToDate = model.ToDate;
                        checkCodeComboHeader.CreatedDate = model.CreatedDate;
                        checkCodeComboHeader.CreatedBy = model.CreatedBy;
                        checkCodeComboHeader.UpdatedDate = model.UpdatedDate;
                        checkCodeComboHeader.UpdatedBy = model.UpdatedBy;
                        checkCodeComboHeader.Counter = maxcounter + 1;

                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Cập nhật thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }
            return result;
        }

        // New : 17/07/2024
        public Tuple<bool, string> UpdateSpecialComboV3(UpdateSetupSpecialComboModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkCodeComboHeader = db.SpecialComboHeaders.FirstOrDefault(d => d.Code == model.Code);
                    var checkCodeComboStore = db.SpecialComboStores.FirstOrDefault(a => a.Code == model.Code);

                    if (checkCodeComboHeader != null)
                    {
                        // Update: table SpecialComboHeader

                        var maxcounter = db.SpecialComboHeaders.Max(a =>a.Counter) ?? 0;

                        checkCodeComboHeader.Name = (model.Name == null) ? string.Empty : model.Name;
                        checkCodeComboHeader.SalesType = model.SalesType;
                        checkCodeComboHeader.ComboQuantity = double.Parse(model.ComboQuantity);
                        checkCodeComboHeader.Amount = double.Parse(model.Amount);
                        checkCodeComboHeader.IsMember = (model.IsMember == "1") ? true : false;
                        checkCodeComboHeader.MemberCode = (model.MemberCode == null) ? string.Empty : model.MemberCode;
                        checkCodeComboHeader.IsEnable = (model.IsEnable == "1") ? true : false;
                        checkCodeComboHeader.FromDate = model.FromDate;
                        checkCodeComboHeader.ToDate = model.ToDate;
                        checkCodeComboHeader.CreatedDate = model.CreatedDate;
                        checkCodeComboHeader.CreatedBy = model.CreatedBy;
                        checkCodeComboHeader.UpdatedDate = model.UpdatedDate;
                        checkCodeComboHeader.UpdatedBy = model.UpdatedBy;
                        checkCodeComboHeader.Counter = maxcounter + 1;


                        // Update: table SpecialComboStore

                        if (model.StoreNo != null || model.StoreNo.Count > 0)
                        {
                            var checkListStore = db.SpecialComboStores.AsNoTracking().Where(a =>model.StoreNo.Contains(a.StoreNo) && a.Code == model.Code).ToList();
                            if (checkListStore.Count == 0) // Cua hang muon them vao khong ton tai trong he thong
                            {
                                if (model.CheckOptionStore == "1")
                                {
                                    // Xoa StoreNo = ALL truoc khi them
                                    var codeComboALL = db.SpecialComboStores.Where(d =>d.Code == model.Code && d.StoreNo == "ALL").ToList();
                                    if (codeComboALL.Count > 0)
                                    {
                                        db.SpecialComboStores.RemoveRange(codeComboALL);
                                        db.SaveChanges();
                                    }

                                    // Them moi
                                    var maxCounter1 = db.SpecialComboStores.Max(a =>a.Counter) ?? 0;
                                    var _maxCounter1 = maxCounter1 + 1;
                                    var insertStore1 = new List<SpecialComboStore>();

                                    foreach (var item in model.StoreNo)
                                    {
                                        insertStore1.Add(new SpecialComboStore
                                        {
                                            Code = model.Code,
                                            StoreNo = item.ToString(),
                                            //IsEnable = true, // mac dinh la true
                                            IsEnable = model.StatusStore == "1" ? true : false,
                                            CreatedDate = model.CreatedDate,
                                            CreatedBy = model.CreatedBy,
                                            UpdatedDate = model.UpdatedDate,
                                            UpdatedBy = model.UpdatedBy,
                                            Counter = _maxCounter1,
                                            Pkey = $"{model.Code}{"-"}{item}"
                                        });
                                    }
                                    db.SpecialComboStores.AddRange(insertStore1);
                                }
                                else
                                {
                                    // remove
                                    var codeCombo = db.SpecialComboStores.Where(d =>d.Code == model.Code).ToList();
                                    db.SpecialComboStores.RemoveRange(codeCombo);
                                    db.SaveChanges();

                                    // insert
                                    var maxCounter = db.SpecialComboStores.Max(a =>a.Counter) ?? 0;
                                    var _maxCounter = maxCounter + 1;
                                    var insertStore = new List<SpecialComboStore>();

                                    foreach (var item in model.StoreNo)
                                    {
                                        insertStore.Add(new SpecialComboStore
                                        {
                                            Code = model.Code,
                                            StoreNo = item.ToString(),
                                            //IsEnable = true, // mac dinh la true
                                            IsEnable = model.StatusStore == "1" ? true : false,
                                            CreatedDate = model.CreatedDate,
                                            CreatedBy = model.CreatedBy,
                                            UpdatedDate = model.UpdatedDate,
                                            UpdatedBy = model.UpdatedBy,
                                            Counter = _maxCounter,
                                            Pkey = $"{model.Code}{"-"}{item}"
                                        });
                                    }
                                    db.SpecialComboStores.AddRange(insertStore);
                                }
                            }
                            else  
                            {
                                var listStoreNot = model.StoreNo.Except(checkListStore.Select(b =>b.StoreNo));
                                if (model.CheckOptionStore == "1")
                                {
                                    // Xoa StoreNo = ALL truoc khi them
                                    var codeComboALL = db.SpecialComboStores.Where(d =>d.Code == model.Code && d.StoreNo == "ALL").ToList();
                                    if (codeComboALL.Count > 0)
                                    {
                                        db.SpecialComboStores.RemoveRange(codeComboALL);
                                        db.SaveChanges();
                                    }

                                    // Them moi
                                    var maxCounter2 = db.SpecialComboStores.Max(a =>a.Counter) ?? 0;
                                    var _maxCounter2 = maxCounter2 + 1;
                                    var insertStore2 = new List<SpecialComboStore>();

                                    foreach (var item in listStoreNot)
                                    {
                                        insertStore2.Add(new SpecialComboStore
                                        {
                                            Code = model.Code,
                                            StoreNo = item.ToString(),
                                            //IsEnable = true, // mac dinh la true
                                            IsEnable = model.StatusStore == "1" ? true : false,
                                            CreatedDate = model.CreatedDate,
                                            CreatedBy = model.CreatedBy,
                                            UpdatedDate = model.UpdatedDate,
                                            UpdatedBy = model.UpdatedBy,
                                            Counter = _maxCounter2,
                                            Pkey = $"{model.Code}{"-"}{item}"
                                        });
                                    }
                                    db.SpecialComboStores.AddRange(insertStore2);
                                }
                                else
                                {
                                    // remove
                                    var codeCombo = db.SpecialComboStores.Where(d =>d.Code == model.Code).ToList();
                                    db.SpecialComboStores.RemoveRange(codeCombo);
                                    db.SaveChanges();

                                    // insert
                                    var maxCounter = db.SpecialComboStores.Max(a =>a.Counter) ?? 0;
                                    var _maxCounter = maxCounter + 1;
                                    var insertStore = new List<SpecialComboStore>();

                                    foreach (var item in model.StoreNo)
                                    {
                                        insertStore.Add(new SpecialComboStore
                                        {
                                            Code = model.Code,
                                            StoreNo = item.ToString(),
                                            //IsEnable = true, // mac dinh la true
                                            IsEnable = model.StatusStore == "1" ? true : false,
                                            CreatedDate = model.CreatedDate,
                                            CreatedBy = model.CreatedBy,
                                            UpdatedDate = model.UpdatedDate,
                                            UpdatedBy = model.UpdatedBy,
                                            Counter = _maxCounter,
                                            Pkey = $"{model.Code}{"-"}{item}"
                                        });
                                    }
                                    db.SpecialComboStores.AddRange(insertStore);
                                }
                            } 
                        }
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Cập nhật thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }
            return result;
        }

        public Tuple<bool, string> UpdateItemNoByGroup(UpdateItemNoByGroupModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var pKey = $"{model.Code}{'-'}{model.GroupCode}{'-'}{model.ItemNo}";
                    var checkPkey = db.SpecialComboLines.FirstOrDefault(d => d.Pkey == pKey);
                    var maxCounter = db.SpecialComboLines.Max(x =>x.Counter) ?? 0;

                    // Check gia khi chọn IsDynamicPrice = 2 : san pham mac dinh co gia ban dong

                    //var listItemNoIsDynamicPrice = db.SpecialComboLines.AsNoTracking().Where(a =>a.Code == model.Code && a.IsDynamicPrice != true).ToList(); // <> 2
                    //var totalAmountCombo = db.SpecialComboHeaders.AsNoTracking().Where(b =>b.Code == model.Code).FirstOrDefault()?.Amount;
                    //var listItemNoComboLine = listItemNoIsDynamicPrice.Select(b =>b.ItemNo).ToList();
                    //var totalPriceSales = db.SalesPrices.Where(a =>listItemNoComboLine.Contains(a.ItemNo) && a.EndingDate.Year.ToString() != "7777").Sum(a =>a.UnitPrice);

                    //if (model.IsDynamicPrice == "2")
                    //{
                    //    if (totalPriceSales > totalAmountCombo)
                    //    {
                    //        result = new Tuple<bool, string>(
                    //            false,
                    //            $"Tổng giá trị các sản phẩm không có giá bán động {totalPriceSales} lớn hơn giá trị của Combo {totalAmountCombo}. Vui lòng kiểm tra lại giá các sản phẩm khi cài đặt Combo có giá bán động");
                    //        return result;
                    //    }
                    //}

                    //------------------------------

                    if (checkPkey != null)
                    {
                        if (model.IsDynamicPrice == "2")
                        {
                            var checkIsDynamicPrice = db.SpecialComboLines.Where(x =>x.Code == model.Code && x.IsDynamicPrice == true).ToList();                            
                            if (checkIsDynamicPrice.Count > 0)
                            {
                                result = new Tuple<bool, string>(false, $"Combo {model.Code} này đang tồn tại trong hệ thống sản phẩm có loại giá bán động, nên không thể thêm loại giá bán động cho sản phẩm này. Vui lòng kiểm tra lại");
                                return result;
                            }
                        }

                        checkPkey.GroupName = model.GroupName;
                        checkPkey.UOM = model.UOM;

                        //checkPkey.IsDynamicPrice = (model.IsDynamicPrice == "1") ? true: false;

                        checkPkey.IsDynamicPrice = (model.IsDynamicPrice == "2") ? true : false;
                        checkPkey.IsDefault = (model.IsDynamicPrice == "0") ? false : true;

                        checkPkey.MinimumQuantity = double.Parse(model.MinQuantity);
                        checkPkey.MaximumQuantity = double.Parse(model.MaxQuantity);

                        //checkPkey.IsEnable = (model.IsEnable == "1") ? true : false;

                        checkPkey.Order = int.Parse(model.Order);
                        checkPkey.CreatedDate = DateTime.Now;
                        checkPkey.CreatedBy = model.CreatedBy;
                        checkPkey.UpdatedDate = DateTime.Now;
                        checkPkey.UpdatedBy = model.UpdatedBy;
                        checkPkey.Counter = Convert.ToInt32(maxCounter) + 1;

                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Cập nhật thành công");
                    }
                    else
                    {                        
                        if (model.IsDynamicPrice == "2")
                        {
                            var checkIsDynamicPrice = db.SpecialComboLines.Where(x =>x.Code == model.Code && x.IsDynamicPrice == true).ToList();
                            // xóa itemno có IsDynamicPrice = true
                            if (checkIsDynamicPrice.Count > 0)
                            {
                                db.SpecialComboLines.RemoveRange(checkIsDynamicPrice);
                                db.SaveChanges();
                            }                            
                        }

                        var checkItem = db.SpecialComboLines.FirstOrDefault(x =>x.Code == model.Code && x.ItemNo == model.ItemNo);
                        if (checkItem != null)
                        {
                            db.SpecialComboLines.Remove(checkItem);
                            db.SaveChanges();
                        }

                        // Thêm mới
                        var insert = new SpecialComboLine
                        {
                            Code = model.Code,
                            GroupCode = model.GroupCode,
                            GroupName = model.GroupName,
                            ItemNo = model.ItemNo,
                            ItemName = db.Items.Where(a => a.No == model.ItemNo.ToString()).Select(a =>a.Description).FirstOrDefault(),
                            UOM = model.UOM,
                            MinimumQuantity = double.Parse(model.MinQuantity),
                            MaximumQuantity = double.Parse(model.MaxQuantity),

                            //IsDefault = (model.IsDynamicPrice == "2") ? true : false,
                            //IsDynamicPrice = (model.IsDynamicPrice == "2") ? true : false, 

                            IsDynamicPrice = (model.IsDynamicPrice == "2") ? true : false,
                            IsDefault = (model.IsDynamicPrice == "0") ? false : true,

                            //IsEnable = (model.IsEnable == "1") ? true: false,

                            IsEnable = true,    // mac dinh la true
                            IsSendSAP = true,   // mac dinh luon
                            Order = int.Parse(model.Order),

                            CreatedDate = model.CreatedDate,
                            CreatedBy = model.CreatedBy,
                            UpdatedDate = model.UpdatedDate,
                            UpdatedBy = model.UpdatedBy,
                            Pkey = $"{model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo}",
                            Counter = Convert.ToInt32(maxCounter) + 1
                        };

                        db.SpecialComboLines.Add(insert);
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Cập nhật thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }
            return result;
        }

        public Tuple<bool, string> AddItemNoByGroupCode(UpdateItemNoByGroupModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var pKey = $"{model.Code}{'-'}{model.GroupCode}{'-'}{model.ItemNo}";
                    var checkPkey = db.SpecialComboLines.FirstOrDefault(d => d.Pkey == pKey);
                    var maxCounter = db.SpecialComboLines.Max(x =>x.Counter) ?? 0;

                    //------------------------------

                    if (checkPkey != null)
                    {
                        if (model.IsDynamicPrice == "2")
                        {
                            if (model.IsDynamicPrice == "2")
                            {
                                var checkIsDynamicPrice = db.SpecialComboLines.Where(x =>x.Code == model.Code && x.IsDynamicPrice == true).ToList();
                                if (checkIsDynamicPrice.Count > 0)
                                {
                                    result = new Tuple<bool, string>(false, $"Combo {model.Code} này đang có sản phẩm có loại giá bán động, nên không thể thêm loại giá bán động cho sản phẩm này. Vui lòng kiểm tra lại");
                                    return result;
                                }
                            }
                        }

                        // Cap nhat

                        checkPkey.UOM = model.UOM;
                        checkPkey.IsDynamicPrice = (model.IsDynamicPrice == "2") ? true : false;
                        checkPkey.IsDefault = (model.IsDynamicPrice == "0") ? false : true;
                        checkPkey.MinimumQuantity = double.Parse(model.MinQuantity);
                        checkPkey.MaximumQuantity = double.Parse(model.MaxQuantity);
                        checkPkey.Order = int.Parse(model.Order);
                        checkPkey.CreatedDate = DateTime.Now;
                        checkPkey.CreatedBy = model.CreatedBy;
                        checkPkey.UpdatedDate = DateTime.Now;
                        checkPkey.UpdatedBy = model.UpdatedBy;
                        checkPkey.Counter = Convert.ToInt32(maxCounter) + 1;

                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Cập nhật thành công");

                    }
                    else  // Them moi
                    {
                        if (model.IsDynamicPrice == "2")
                        {
                            var checkIsDynamicPrice = db.SpecialComboLines.Where(x =>x.Code == model.Code && x.IsDynamicPrice == true).ToList();
                            
                            // xóa san pham có IsDynamicPrice = true trong combo, truoc khi them san pham moi

                            if (checkIsDynamicPrice.Count > 0)
                            {
                                db.SpecialComboLines.RemoveRange(checkIsDynamicPrice);
                                db.SaveChanges();
                            }
                        }

                        // Thêm mới
                        var insert = new SpecialComboLine
                        {
                            Code = model.Code,
                            GroupCode = model.GroupCode,
                            GroupName = model.GroupName,
                            ItemNo = model.ItemNo,
                            ItemName = db.Items.Where(a => a.No == model.ItemNo.ToString()).Select(a =>a.Description).FirstOrDefault(),
                            UOM = model.UOM,
                            MinimumQuantity = double.Parse(model.MinQuantity),
                            MaximumQuantity = double.Parse(model.MaxQuantity),

                            IsDynamicPrice = (model.IsDynamicPrice == "2") ? true : false,
                            IsDefault = (model.IsDynamicPrice == "0") ? false : true,

                            IsEnable = true,    // mac dinh la true
                            IsSendSAP = true,   // mac dinh la true
                            Order = int.Parse(model.Order),

                            CreatedDate = model.CreatedDate,
                            CreatedBy = model.CreatedBy,
                            UpdatedDate = model.UpdatedDate,
                            UpdatedBy = model.UpdatedBy,
                            Pkey = $"{model.Code}{"-"}{model.GroupCode}{"-"}{model.ItemNo}",
                            Counter = Convert.ToInt32(maxCounter) + 1

                        };

                        db.SpecialComboLines.Add(insert);
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Thêm sản phẩm trong nhóm {model.GroupName} thành công");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }
            return result;
        }

        public Tuple<bool, string> DeleteItemNoByGroupCode(DeleteItemNoByGroupModel model)
        {
            var result = new Tuple<bool, string>(false, "Fail");

            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var pkey = $"{model.Code}{'-'}{model.GroupCode}{'-'}{model.ItemNo}";
                    var checkPkey = db.SpecialComboLines.FirstOrDefault(d => d.Pkey == pkey);
                    
                    //------------------------------

                    if (checkPkey != null)
                    {
                        db.SpecialComboLines.Remove(checkPkey);
                        db.SaveChanges();
                        result = new Tuple<bool, string>(true, $"Xóa sản phẩm {model.ItemNo} thành công");
                    }
                    else  
                    {
                        result = new Tuple<bool, string>(true, $"Không tìm thấy thông tin");
                    }
                }
                catch (Exception ex)
                {
                    result = new Tuple<bool, string>(false, JsonConvert.SerializeObject(ex));
                }
            }
            return result;
        }

        public List<GetSpecialComboHeaderModel> GetSpecialComboHeaderList(string FromDate, string ToDate, string storeNo, string textSearch, string SalesType, string MemberType, string Status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    //var data = db.Database.SqlQuery<GetSpecialComboHeaderModel>("[dbo].[GetSpecialComboHeaderList] @FromDate, @ToDate, @StoreNo, @TextSearch, @SalesType, @Status, @PageSize, @PageNumber",                    
                    var data = db.Database.SqlQuery<GetSpecialComboHeaderModel>("[dbo].[GetSpecialComboHeaderListV2] @FromDate, @ToDate, @StoreNo, @TextSearch, @SalesType, @MemberType, @Status, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", FromDate ?? string.Empty),
                        new SqlParameter("@ToDate", ToDate ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@SalesType", SalesType ?? string.Empty),
                        new SqlParameter("@MemberType", MemberType ?? string.Empty),
                        new SqlParameter("@Status", Status ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<GetSpecialComboHeaderModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<GetSpecialComboHeaderModel>();
            }
        }

        // Danh sách chi tiết Special Combo
        public List<DetailSpecialComboModel> GetDetailSpecialComboList(string code, string storeNo, string status, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailSpecialComboModel>("[dbo].[GetDetailSpecialCombo] @Code, @StoreNo, @Status, @TextSearch, @PageSize, @PageNumber",
                        new SqlParameter("@Code", code ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailSpecialComboModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailSpecialComboModel>();
            }
        }

        public List<DetailSpecialComboModel> GetDetailItemNoByCombo(string comboCode, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DetailSpecialComboModel>("[dbo].[GetDetailItemNoByCombo] @ComboCode, @PageSize, @PageNumber",
                        new SqlParameter("@ComboCode", comboCode ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DetailSpecialComboModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DetailSpecialComboModel>();
            }
        }

        public List<CodeBySpecialComboHeaderModel> LoadCodeBySpecialComboHeader()
        {
            var result = new List<CodeBySpecialComboHeaderModel>();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    result = db.SpecialComboHeaders.Where(d => d.IsEnable == true).Select(a => new CodeBySpecialComboHeaderModel
                    {
                        Code = a.Code,
                        Name = a.Name
                    }).OrderByDescending(x =>x.Code).Distinct().ToList();
                }
                catch (Exception ex)
                {
                    return new List<CodeBySpecialComboHeaderModel>();
                }
                return result;
            }
        }

        public List<ItemByComboLineModel> LoadComboxItemByComboLine()
        {
            var result = new List<ItemByComboLineModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Items.AsNoTracking().Where(x =>x.Blocked == 0).Select(a => new ItemByComboLineModel
                    {
                        No = a.No,
                        Description = a.Description
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public List<ItemByComboLineModel> LoadComboxUOM(string ItemNo)
        {
            var result = new List<ItemByComboLineModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    if (!string.IsNullOrEmpty(ItemNo))
                    {
                        result = db.Items.AsNoTracking().Where(x => x.Blocked == 0 && x.No == ItemNo).Select(a => new ItemByComboLineModel
                        {
                            UOM = a.SalesUnitOfMeasure
                        }).OrderByDescending(x => x.UOM).Distinct().ToList();
                    }
                    else
                    {
                        return new List<ItemByComboLineModel>();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<ItemByComboLineModel> GetListComboxUOM()
        {
            var result = new List<ItemByComboLineModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.Items.AsNoTracking().Where(x => x.Blocked == 0).Select(a => new ItemByComboLineModel
                    {
                        UOM = a.SalesUnitOfMeasure
                    }).OrderByDescending(x => x.UOM).Distinct().ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<PriceSalesModel> LoadComboxPrice(string ItemNo, string UOM)
        {
            var result = new List<PriceSalesModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    if (!string.IsNullOrEmpty(ItemNo))
                    {
                        result = db.SalesPrices.AsNoTracking().Where(x => x.ItemNo == ItemNo && x.UnitOfMeasureCode == UOM && x.EndingDate.Year.ToString() != "7777").Select(a => new PriceSalesModel
                        {
                            ItemNo = a.ItemNo,
                            UnitOfMeasure = a.UnitOfMeasureCode,
                            UnitPrice = a.UnitPrice
                        }).OrderByDescending(x => x.ItemNo).ToList();
                    }
                    else
                    {
                        return new List<PriceSalesModel>();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public string LoadComboxPriceV2(string ItemNo)
        {
            var result = string.Empty;
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    if (!string.IsNullOrEmpty(ItemNo))
                    {
                        var UOM = db.Items.AsNoTracking().FirstOrDefault(a =>a.No == ItemNo && a.Blocked == 0).SalesUnitOfMeasure;
                        var priceSales = db.SalesPrices.AsNoTracking().Where(x => x.ItemNo == ItemNo && x.UnitOfMeasureCode == UOM && x.EndingDate.Year.ToString() != "7777").FirstOrDefault()?.UnitPrice;
                        result = Convert.ToString(priceSales);                      
                    }
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
            return result;
        }
        public string GenerateRandomCode(string dto)
        {
            // Get the current timestamp
            // var timestamp = DateTimeOffset.UtcNow.ToUniversalTime();

            // Random random = new Random();
            // var sequenceNumber = random.Next(1000, 9999);

            // Generate the code with the format "S{timestamp}{sequenceNumber}"
            // string generatedCode = $"{'S'}{timestamp}{sequenceNumber}";
            // return generatedCode;

            // Generate a random sequence number

            Random random = new Random();
            var sequenceNumber = random.Next(1000, 9999);

            // Generate the code with the format "P{timestamp}{sequenceNumber}"

            string generatedCode = $"{'S'}{dto}{sequenceNumber}";
            return generatedCode;
        }

        public List<ItemByComboLineModel> LoadComboxItem()
        {
            var result = new List<ItemByComboLineModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = (from a in db.Items.AsNoTracking()
                              join b in db.PosGroupItems on a.No equals b.ItemNo
                              where a.Blocked == 0 
                                select new ItemByComboLineModel
                                {
                                    No = a.No,
                                    Description = a.Description,
                                    UOM = a.SalesUnitOfMeasure
                                }).OrderBy(a => a.No).Distinct().ToList();

                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        //public List<ItemByComboLineModel> LoadComboxItemV2(int pageIndex, string itemNo, string searchText, out int total)
        //{
        //    var pageSize = 20;
        //    var skip = pageSize * pageIndex;
        //    total = 0;

        //    using (var db = new CentralMDPartnerContainer())
        //    {
        //        db.Database.CommandTimeout = 2 * 60;

        //        var data = db.Database.SqlQuery<ItemByComboLineModel>("[dbo].[GetItemByComboxList] @ItemNo, @SearchText",
        //            new SqlParameter("@ItemNo", itemNo ?? string.Empty),
        //            new SqlParameter("@SearchText", searchText ?? string.Empty)).ToList();

        //        total = data.Count();
        //        data = data.OrderBy(x => x.No).Skip(skip).Take(pageSize).ToList();
        //        return data;
        //    }
        //}

        public List<ItemByComboLineModel> LoadComboxItemV2(string searchText, int page, int pageSize, out int countTotal, string itemNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    //var resultData = db.Items.AsNoTracking().AsQueryable().Where(x =>x.Blocked == 0);
                    var resultData = (from a in db.Items.AsNoTracking()
                              join b in db.PosGroupItems on a.No equals b.ItemNo
                              join c in db.SalesPrices on a.No equals c.ItemNo
                              where a.Blocked == 0 && a.SalesUnitOfMeasure == c.UnitOfMeasureCode && (c.EndingDate.Year.ToString() != "7777" && c.EndingDate >= DateTime.Now)
                              select new ItemByComboLineModel
                              {
                                  No = a.No,
                                  Description = a.Description,
                                  UOM = a.SalesUnitOfMeasure
                              }).OrderBy(a => a.No).Distinct().ToList();

                    if (!string.IsNullOrEmpty(itemNo))
                    {
                        resultData = resultData.Where(x => x.No == itemNo).ToList();
                    }

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        searchText = searchText.Trim();
                        resultData = resultData.Where(d => (d.No.Contains(searchText) || d.Description.Contains(searchText))).ToList();
                    }

                    countTotal = resultData.Count();
                    int skip = (page-1) * pageSize > countTotal ? countTotal : (page-1) * pageSize;
                    var result = resultData
                        .OrderBy(x => x.No)
                        .Skip(skip)
                        .Take(pageSize)
                        .Select(a => new ItemByComboLineModel
                        {
                            No = a.No,
                            Description = a.Description
                        }).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                countTotal = 0;
                return (default(List<ItemByComboLineModel>));
            }
        }
        public ResultResponseModel UpdateStatusComboHeader(string code, string status)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkCode = db.SpecialComboHeaders.FirstOrDefault(x => x.Code == code);
                    checkCode.IsEnable = (status == "1") ? true : false;
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật trạng thái thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message
                    };
                }
            }
        }

        // Danh sách CH Special Combo
        public List<SpecialComboStoreModel> GetStoreSpecialCombo(string code, string status, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<SpecialComboStoreModel>("[dbo].[GetStoreSpecialCombo] @Code, @Status, @TextSearch, @PageSize, @PageNumber",
                        new SqlParameter("@Code", code ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SpecialComboStoreModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<SpecialComboStoreModel>();
            }
        }

        public ResultResponseModel UpdateStatusStore(string code, string storeno, string status, string userName)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var check = db.SpecialComboStores.FirstOrDefault(x => x.Code == code && x.StoreNo == storeno);
                    check.IsEnable = (status == "1") ? true : false;
                    check.UpdatedBy = userName;
                    check.UpdatedDate = DateTime.Now;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật trạng thái thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message
                    };
                }
            }
        }

    }
}
