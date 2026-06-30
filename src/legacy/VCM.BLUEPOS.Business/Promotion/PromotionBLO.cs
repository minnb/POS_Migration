using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Promotion;
using VCM.BLUEPOS.Model.Promotion;
using VCM.BLUEPOS.Model.OptionModel;


namespace VCM.BLUEPOS.Business.Promotion
{
    public interface IPromotionBLO
    {
        List<OptionModel> GetPromotionStatus();
        List<OptionModel> GetPromotionType();
        List<OfferHearderResponseModel> GetOfferHeaderList(string TextSearch, string Description, string Status, string OfferType, string ItemNo, string StoreNo, string SalesType, int Exp, out int totalRecord, int pageIndex = 0, int pageSize = 100);
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

    public class PromotionBLO : IPromotionBLO
    {
        private PromotionData _data { get; set; }
        public PromotionBLO()
        {
            _data = new PromotionData();
        }
        public List<OptionModel> GetPromotionStatus()
        {
            return _data.GetPromotionStatus();
        }
        public List<OptionModel> GetPromotionType()
        {
            return _data.GetPromotionType();
        }
        public List<OfferHearderResponseModel> GetOfferHeaderList(string TextSearch, string Description, string Status, string OfferType, string ItemNo, string StoreNo, string SalesType, int Exp, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetOfferHeaderList(TextSearch, Description, Status, OfferType, ItemNo, StoreNo, SalesType, Exp, out totalRecord, pageIndex, pageSize);
        }
        public List<DetailOfferHeaderResponseModel> GetDetailOfferHeaderList(string offerNo, out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.GetDetailOfferHeaderList(offerNo, out totalRecord, skip, take);
        }
        public List<DetailOfferBuyResponseModel> GetDetailOfferBuyList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.GetDetailOfferBuyList(offerNo, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public List<DetailOfferBenefitsResponseModel> GetDetailOfferBenefitsList(string offerNo, out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.GetDetailOfferBenefitsList(offerNo, out totalRecord, skip, take);
        }
        public List<DetailOfferGetResponseModel> GetDetailOfferGetList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.GetDetailOfferGetList(offerNo, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public List<DetailOfferSiteModel> GetDetailOfferSiteList(string offerNo, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.GetDetailOfferSiteList(offerNo, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public List<DetailOfferPriorityModel> GetDetailOfferPriorityList(string offerType, out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.GetDetailOfferPriorityList(offerType, out totalRecord, skip, take);
        }
        public List<ExportOfferHeaderModel> ExportExcelGetOfferHeaderList(string textSearch, string promotionName, string promotionStatus, string promotionTypes, string salesType, string itemNo, string storeNo)
        {
            return _data.ExportExcelGetOfferHeaderList(textSearch, promotionName, promotionStatus, promotionTypes, salesType, itemNo, storeNo);
        }

        public List<CheckPromotionModel> CheckPromotionList(string storeNo, string offerNo, string offerType, string salesType, string memberType, string status, string itemNo, string keyWord, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.CheckPromotionList(storeNo, offerNo, offerType, salesType, memberType, status, itemNo, keyWord, out totalRecord, pageIndex, pageSize);
        }

        public List<CheckPromotionModel> ExportExcelCheckPromotion(string storeNo, string offerNo, string offerType, string salesType, string memberType, string status, string itemNo, string keyWord, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.ExportExcelCheckPromotion(storeNo, offerNo, offerType, salesType, memberType, status, itemNo, keyWord, out totalRecord, pageIndex, pageSize);
        }

        public List<ExportExcel_PromotionOfferBuyModel> ExportToExcel_PromotionOfferBuy(string offerNo)
        {
            return _data.ExportToExcel_PromotionOfferBuy(offerNo);
        }

        public List<ExportExcel_PromotionOfferGetModel> ExportToExcel_PromotionOfferGet(string offerNo)
        {
            return _data.ExportToExcel_PromotionOfferGet(offerNo);
        }

        public List<ExportExcel_PromotionOfferSiteModel> ExportToExcel_PromotionOfferSite(string offerNo)
        {
            return _data.ExportToExcel_PromotionOfferSite(offerNo);
        }

        public List<ViewCheckPromotionModalModel> GetListViewPromotionCheck(string bonusbuyNo, string promotionNo, string barcodeNo, string itemNo)
        {
            return _data.GetListViewPromotionCheck(bonusbuyNo, promotionNo, barcodeNo, itemNo);
        }






















    }
}
