using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Price;
using VCM.BLUEPOS.Model.Price;
using VCM.BLUEPOS.Model.PriceGroup;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Product;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Store;

namespace VCM.BLUEPOS.Business.Price
{
    public interface IPriceBLO
    {
        List<OptionModel> GetComboSite(string channelNo);
        List<OptionModel> GetComboRegion();
        List<OptionModel> GetComboxStyleProfile();
        List<PriceListResponseModel> GetPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportPriceListModel> ExportPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck);
        Tuple<bool, SearchProductModel> GetSearchProduct(string itemNo);
        List<OptionModel> GetUnitOfMeasureCode(string itemNo);
        ResultResponseModel CreatePriceList(List<CreatePriceModel> req);
        ResultResponseModel UpdatePriceList(UpdatePriceListModel req);
        List<UpdatePriceListResponseModel> GetUpdatePriceList(string salesCode, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel CreateStorePriceGroup(List<CreateStorePriceGroupModel> req);
        List<OptionModel> GetComboxPriceGroup();
        List<StoreModel> GetStoreList(string storeNo, string styleProfile, out int totalRecord, int skip, int take);
        ResultResponseModel CreatePriceGroup(CreatePriceGroupModel req);
        List<PriceGroupListResponseModel> GetPriceGroupList(string priceGroupCode, string priority, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel SaveStorePriceGroup(List<AddStorePriceGroupModel> model);
        List<AddStoreModel> GetStoreAllList(string storeNo, string textSearch, string styleProfile);


    }

    public class PriceBLO : IPriceBLO
    {
        private IPriceData _data { get; set; }
        public PriceBLO()
        {
            _data = new PriceData();
        }
        public List<OptionModel> GetComboSite(string channelNo)
        {
            return _data.GetComboSite(channelNo);
        }
        public List<OptionModel> GetComboRegion()
        {
            return _data.GetComboRegion();
        }
        public List<OptionModel> GetComboxStyleProfile()
        {
            return _data.GetComboxStyleProfile();
        }
        public List<PriceListResponseModel> GetPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetPriceList(itemCode, itemName, barCode, salesCode, isCheck, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportPriceListModel> ExportPriceList(string itemCode, string itemName, string barCode, string salesCode, int isCheck)
        {
            return _data.ExportPriceList(itemCode, itemName, barCode, salesCode, isCheck);
        }
        public Tuple<bool, SearchProductModel> GetSearchProduct(string itemNo)
        {
            return _data.GetSearchProduct(itemNo);
        }
        public List<OptionModel> GetUnitOfMeasureCode(string itemNo)
        {
            return _data.GetUnitOfMeasureCode(itemNo);
        }
        public ResultResponseModel CreatePriceList(List<CreatePriceModel> req)
        {
            return _data.CreatePriceList(req);
        }
        public ResultResponseModel UpdatePriceList(UpdatePriceListModel req)
        {
            return _data.UpdatePriceList(req);
        }
        public List<UpdatePriceListResponseModel> GetUpdatePriceList(string salesCode, string itemNo, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetUpdatePriceList(salesCode, itemNo, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel CreateStorePriceGroup(List<CreateStorePriceGroupModel> req)
        {
            return _data.CreateStorePriceGroup(req);
        }
        public List<OptionModel> GetComboxPriceGroup()
        {
            return _data.GetComboxPriceGroup();
        }
        public List<StoreModel> GetStoreList(string storeNo, string styleProfile, out int totalRecord, int skip, int take)
        {
            return _data.GetStoreList(storeNo, styleProfile, out totalRecord, skip, take);
        }
        public List<AddStoreModel> GetStoreAllList(string storeNo, string textSearch, string styleProfile)
        {
            return _data.GetStoreAllList(storeNo, textSearch, styleProfile);
        }
        public ResultResponseModel CreatePriceGroup(CreatePriceGroupModel req)
        {
            return _data.CreatePriceGroup(req);
        }
        public List<PriceGroupListResponseModel> GetPriceGroupList(string priceGroupCode, string priority, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetPriceGroupList(priceGroupCode, priority, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel SaveStorePriceGroup(List<AddStorePriceGroupModel> model)
        {
            return _data.SaveStorePriceGroup(model);
        }








    }
}
