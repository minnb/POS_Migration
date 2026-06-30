using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.SetupPromotion;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Model.SetupPromotion.SetupSpecialComboModel;


namespace VCM.BLUEPOS.Business.SetupPromotion
{
    public interface ISetupPromotionBLO
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
        Tuple<bool, string, OfferHeaderModel> SaveSetupPromotion(OfferHeaderModel model);
        Tuple<bool, string, OfferHeaderModel> SaveSetupCTKMAll(AdvenceSetupRequest req);
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
        List<GetSpecialComboHeaderModel> GetSpecialComboHeaderList(string FromDate, string ToDate, string storeNo, string textSearch, string SalesType, string MemberType, string Status, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<DetailSpecialComboModel> GetDetailSpecialComboList(string code, string storeNo, string status, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<CodeBySpecialComboHeaderModel> LoadCodeBySpecialComboHeader();
        List<ItemByComboLineModel> LoadComboxItemByComboLine();
        ResultResponseModel UpdateStatusComboHeader(string code, string status);
        List<ItemByComboLineModel> LoadComboxItem();
        List<ItemByComboLineModel> LoadComboxUOM(string itemNo);
        List<ItemByComboLineModel> GetListComboxUOM();
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


    public class SetupPromotionBLO : ISetupPromotionBLO
    {
        private SetupPromotionData data { get; set; }
        public SetupPromotionBLO()
        {
            data = new SetupPromotionData();
        }

        public SetupMainModel ViewSetupMain()
        {
            return data.ViewSetupMain();
        }
        public List<ItemModel> ListItem()
        {
            return data.ListItem();
        }
        public List<OfferTypeModel> LoadOfferType()
        {
            return data.LoadOfferType();
        }

        public Tuple<bool, string, OfferHeaderModel> UpdateOfferHeader(OfferHeaderModel model)
        {
            return data.UpdateOfferHeader(model);
        }

        public OfferHeaderModel GetOfferHeader(string bonusBuy)
        {
            return data.GetOfferHeader(bonusBuy);
        }

        public Tuple<bool, string, OfferHeaderModel> InsertUpdateAdvance(OfferHeaderModel model)
        {
            return data.InsertUpdateAdvance(model);
        }

        public List<OfferBuyModel> GetOfferBuy(string bonusBuy)
        {
            return data.GetOfferBuy(bonusBuy);
        }

        public List<OfferGetModel> GetOfferGet(string offerNo, bool isTotalBill, bool isSetupGet)
        {
            return data.GetOfferGet(offerNo, isTotalBill, isSetupGet);
        }

        public Tuple<bool, string> DeleteOfferBuy(string offerNo, int lineNo, int lineType, string groupCode)
        {
            return data.DeleteOfferBuy(offerNo, lineNo, lineType, groupCode);
        }
        public Tuple<bool, string, OfferBuyModel> InsertUpdateOfferBuy(OfferBuyModel model, bool isTotalBill, bool isSetupGet, string conditionBuy, string offerType, string itemHidden)
        {
            return data.InsertUpdateOfferBuy(model, isTotalBill, isSetupGet, conditionBuy, offerType, itemHidden);
        }
        public List<ItemSetupMode> ListItemSetup(string itemNo, string description, out int total, int skip, int take)
        {
            return data.ListItemSetup(itemNo, description, out total, skip, take);
        }

        public Tuple<bool, string, OfferGetModel> InsertUpdateOfferGetOrBenefit(OfferGetModel model, bool isTotalBill, bool isSetupGet, string conditionGet, string offerType, string itemHidden)
        {
            return data.InsertUpdateOfferGetOrBenefit(model, isTotalBill, isSetupGet, conditionGet, offerType, itemHidden);
        }

        public Tuple<bool, string> DeleteOfferGet(string offerNo, int lineNo, bool isTotalBill, bool isSetupGet, int lineType, string groupCode)
        {
            return data.DeleteOfferGet(offerNo, lineNo, isTotalBill, isSetupGet, lineType, groupCode);
        }

        public List<OfferSiteModel> GetOfferSite(string bonusBuy)
        {
            return data.GetOfferSite(bonusBuy);
        }

        public Tuple<bool, string> SetupGroupSite(GroupSiteModel model)
        {
            return data.SetupGroupSite(model);
        }

        public List<GroupSiteModel> ListGroupSiteSetup(string groupCode, string groupName, out int total, int skip, int take)
        {
            return data.ListGroupSiteSetup(groupCode, groupName, out total, skip, take);
        }

        public List<StoreSetupModel> ListStoreSetup(string groupCode, string storeNo, string storeName, out int total, int skip, int take)
        {
            return data.ListStoreSetup(groupCode, storeNo, storeName, out total, skip, take);
        }

        public Tuple<bool, string, List<GroupSiteModel>> InsertUpdateOfferSite(string offerNo, List<string> listGroupCode)
        {
            return data.InsertUpdateOfferSite(offerNo, listGroupCode);
        }

        public Tuple<bool, string> SetupGroupBuyItem(BuyGroupItemModel model)
        {
            return data.SetupGroupBuyItem(model);
        }

        public List<BuyGroupItemModel> LoadBuyGroup(string groupCode, string groupName, out int total, int skip, int take)
        {
            return data.LoadBuyGroup(groupCode, groupName, out total, skip, take);
        }

        public List<ItemSetupMode> LoadListItemBuySetup(string groupCode, string itemNo, string itemName, out int total, int skip, int take)
        {
            return data.LoadListItemBuySetup(groupCode, itemNo, itemName, out total, skip, take);
        }

        public Tuple<bool, string> DeleteOfferSite(string offerNo, string groupSite)
        {
            return data.DeleteOfferSite(offerNo, groupSite);
        }

        public Tuple<bool, string, OfferHeaderModel> SetupPromotionHeader(OfferHeaderModel model)
        {
            return data.SetupPromotionHeader(model);
        }

        public OfferHeaderModel SetupGetOfferHeader(string bonusBuy)
        {
            return data.SetupGetOfferHeader(bonusBuy);
        }

        public Tuple<bool, string, OfferHeaderModel> SetupInsertUpdateAdvance(OfferHeaderModel model)
        {
            return data.SetupInsertUpdateAdvance(model);
        }

        public Tuple<bool, string, OfferBuyModel> SetupInsertUpdateOfferBuy(OfferBuyModel model, bool isTotalBill, bool isSetupGet, string conditionBuy, string offerType, string itemHidden)
        {
            return data.SetupInsertUpdateOfferBuy(model, isTotalBill, isSetupGet, conditionBuy, offerType, itemHidden);
        }

        public GetBBModel SetupGetOfferBuy(string bonusBuy)
        {
            return data.SetupGetOfferBuy(bonusBuy);
        }

        public Tuple<bool, string> SetupDeleteOfferBuy(string offerNo, int lineNo, int lineType, string groupCode)
        {
            return data.SetupDeleteOfferBuy(offerNo, lineNo, lineType, groupCode);
        }

        public Tuple<bool, string> SetupInsertUpdateHeaderBuy(string offerNo, string buyLinkCat, string checkMinValue, string minValue)
        {
            return data.SetupInsertUpdateHeaderBuy(offerNo, buyLinkCat, checkMinValue, minValue);
        }

        public GetBBModel SetupGetOfferGet(string offerNo)
        {
            return data.SetupGetOfferGet(offerNo);
        }

        public Tuple<bool, string, OfferGetModel> SetupInsertUpdateOfferGet(OfferGetModel model, string conditionGet, string itemHidden)
        {
            return data.SetupInsertUpdateOfferGet(model, conditionGet, itemHidden);
        }

        public Tuple<bool, string> SetupDeleteOfferGet(string offerNo, int lineNo, int lineType, string groupCode)
        {
            return data.SetupDeleteOfferGet(offerNo, lineNo, lineType, groupCode);
        }

        public Tuple<bool, string> SetupInsertUpdateHeaderGet(string offerNo, string getLinkCat, string checkDiscount, string typeDiscount, string discountValue)
        {
            return data.SetupInsertUpdateHeaderGet(offerNo, getLinkCat, checkDiscount, typeDiscount, discountValue);
        }

        public Tuple<bool, string> SetupDeleteOfferGetByOfferNo(string offerNo)
        {
            return data.SetupDeleteOfferGetByOfferNo(offerNo);
        }

        public Tuple<bool, string> SetupInsertUpdateOfferSite(string offerNo, string siteGroupCode)
        {
            return data.SetupInsertUpdateOfferSite(offerNo, siteGroupCode);
        }

        public List<OfferSiteModel> SetupGetOfferSite(string bonusBuy)
        {
            return data.SetupGetOfferSite(bonusBuy);
        }

        public Tuple<bool, string> SetupDeleteOfferSite(string offerNo, string groupSite)
        {
            return data.SetupDeleteOfferSite(offerNo, groupSite);
        }
        public Tuple<bool, string> SetupDeleteGroupItem(string groupItem)
        {
            return data.SetupDeleteGroupItem(groupItem);
        }
        public Tuple<bool, string> FinishSetupPromotion(string offerNo, string status, string salesType, string offerName, string tuNgay, string denNgay, bool isVoucher, bool isApprove)
        {
            return data.FinishSetupPromotion(offerNo, status, salesType, offerName, tuNgay, denNgay, isVoucher, isApprove);
        }

        public Tuple<bool, string> SetupDeleteGroupSite(string groupSite)
        {
            return data.SetupDeleteGroupSite(groupSite);
        }

        public Tuple<bool, string> UpdateStatusPromotion(string offerNo, string status)
        {
            return data.UpdateStatusPromotion(offerNo, status);
        }

        public Tuple<bool, string, OfferHeaderModel> SaveSetupPromotion(OfferHeaderModel model)
        {
            return data.SaveSetupPromotion(model);
        }

        public Tuple<bool, string, OfferHeaderModel> SaveSetupCTKMAll(AdvenceSetupRequest req)
        {
            return data.SaveSetupCTKMAll(req);
        }

        public List<OfferHeaderModel> LoadSetupPromotionHeader(string offerNo, string offerName, string approve, out int total, int skip, int take)
        {
            return data.LoadSetupPromotionHeader(offerNo, offerName, approve, out total, skip, take);
        }

        public List<OfferHeaderModel> LoadExtraFeeCombo(List<string> listOfferSelected, string offerNo, string offerName, out int total, int skip, int take)
        {
            return data.LoadExtraFeeCombo(listOfferSelected,offerNo, offerName, out total, skip, take);
        }

        public Tuple<bool, string> CreateSpecialCombo(CreateSetupSpecialComboModel model)
        {
            return data.CreateSpecialCombo(model);
        }

        public Tuple<bool, string> CreateSpecialComboV2(CreateSetupSpecialComboModel model)
        {
            return data.CreateSpecialComboV2(model);
        }

        public Tuple<bool, string> UpdateSpecialComboV2(UpdateSetupSpecialComboModel model)
        {
            return data.UpdateSpecialComboV2(model);
        }
        public Tuple<bool, string> UpdateItemNoByGroup(UpdateItemNoByGroupModel model)
        {
            return data.UpdateItemNoByGroup(model);
        }
        public Tuple<bool, string> AddItemNoByGroupCode(UpdateItemNoByGroupModel model)
        {
            return data.AddItemNoByGroupCode(model);
        }
        public Tuple<bool, string> DeleteItemNoByGroupCode(DeleteItemNoByGroupModel model)
        {
            return data.DeleteItemNoByGroupCode(model);
        }
        public List<GetSpecialComboHeaderModel> GetSpecialComboHeaderList(string FromDate, string ToDate,  string storeNo, string textSearch, string SalesType, string MemberType, string Status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return data.GetSpecialComboHeaderList(FromDate, ToDate, storeNo, textSearch, SalesType, MemberType, Status, out totalRecord, pageIndex, pageSize);
        }
        public List<DetailSpecialComboModel> GetDetailSpecialComboList(string code, string storeNo, string status, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return data.GetDetailSpecialComboList(code, storeNo, status, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<DetailSpecialComboModel> GetDetailItemNoByCombo(string comboCode, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return data.GetDetailItemNoByCombo(comboCode, out totalRecord, pageIndex, pageSize);
        }
        public List<CodeBySpecialComboHeaderModel> LoadCodeBySpecialComboHeader()
        {
            return data.LoadCodeBySpecialComboHeader();
        }
        public List<ItemByComboLineModel> LoadComboxItemByComboLine()
        {
            return data.LoadComboxItemByComboLine();
        }
        public List<ItemByComboLineModel> LoadComboxUOM(string itemNo)
        {
            return data.LoadComboxUOM(itemNo);
        }
        public List<ItemByComboLineModel> GetListComboxUOM()
        {
            return data.GetListComboxUOM();
        }
        public List<PriceSalesModel> LoadComboxPrice(string ItemNo, string UOM)
        {
            return data.LoadComboxPrice(ItemNo, UOM);
        }
        public string LoadComboxPriceV2(string ItemNo)
        {
            return data.LoadComboxPriceV2(ItemNo);
        }
        public string GenerateRandomCode(string dto)
        {
            return data.GenerateRandomCode(dto);
        }
        public List<ItemByComboLineModel> LoadComboxItem()
        {
            return data.LoadComboxItem();
        }
        public List<ItemByComboLineModel> LoadComboxItemV2(string searchText, int page, int pageSize, out int countTotal, string itemNo)
        {
            return data.LoadComboxItemV2(searchText, page, pageSize, out countTotal, itemNo);
        }
        public ResultResponseModel UpdateStatusComboHeader(string code, string status)
        {
            return data.UpdateStatusComboHeader(code, status);
        }
        public Tuple<bool, string> UpdateSpecialComboV3(UpdateSetupSpecialComboModel model)
        {
            return data.UpdateSpecialComboV3(model);
        }
        public List<SpecialComboStoreModel> GetStoreSpecialCombo(string code, string status, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return data.GetStoreSpecialCombo(code, status, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel UpdateStatusStore(string code, string storeno,string status, string userName)
        {
            return data.UpdateStatusStore(code, storeno, status, userName);
        }


    }
}
