using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Campaign;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Campaign;

namespace VCM.BLUEPOS.Business.Campaign
{
    public interface ICampaignBLO
    {
        Task<Tuple<List<ComboxOptionModel>, int>> GetStoreList(string searchText, int pageIndex = 0, int pageSize = 50);
        Task<List<ComboxOptionModel>> GetStoreByCodeList(List<string> listCode);
        Tuple<List<SelectionItem>, int> GetListOfferNo(CampaignOfferNoFilterModel filter);
        Tuple<List<SelectionItem>, int> GetListItem(CampaignItemFilterModel filter);
        Task<CampaignAPIResultModel<object>> Create(CampaignCreateModel req, CampaignAPIModel api);
        Task<CampaignAPIResultModel<CampaignDetailModel>> Detail(string objectId, CampaignAPIModel api);
        Task<CampaignAPIResultModel<object>> SyncCampaign(string objectId, CampaignAPIModel api);
        Task<CampaignAPIResultModel<object>> CopyCampaign(CampaignCopyModel req, CampaignAPIModel api);
        Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaign(CampaignFilterModel filter, CampaignAPIModel api);
        Task<Tuple<List<CampaignImportModel>, string>> Import(DataTable dataImport);
        Task<CampaignAPIResultModel<object>> SyncCampaignV2(string objectId, CampaignAPIModel api);
        Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaignV2(CampaignFilterModel filter, CampaignAPIModel api);
        List<GetOfferTypeModel> GetListOfferType();



    }

    public class CampaignBLO : ICampaignBLO
    {
        private readonly ICampaignData _data;
        public CampaignBLO()
        {
            _data = new CampaignData();
        }

        public Task<CampaignAPIResultModel<object>> Create(CampaignCreateModel req, CampaignAPIModel api)
        {
            return _data.Create(req, api);
        }

        public Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaign(CampaignFilterModel filter, CampaignAPIModel api)
        {
            return _data.GetListCampaign(filter, api);
        }

        public Task<CampaignAPIResultModel<List<CampaignDetailModel>>> GetListCampaignV2(CampaignFilterModel filter, CampaignAPIModel api)
        {
            return _data.GetListCampaignV2(filter, api);
        }

        public Task<CampaignAPIResultModel<CampaignDetailModel>> Detail(string objectId, CampaignAPIModel api)
        {
            return _data.Detail(objectId, api);
        }

        public Task<CampaignAPIResultModel<object>> CopyCampaign(CampaignCopyModel req, CampaignAPIModel api)
        {
            return _data.CopyCampaign(req, api);
        }

        public Task<CampaignAPIResultModel<object>> SyncCampaign(string objectId, CampaignAPIModel api)
        {
            return _data.SyncCampaign(objectId, api);
        }

        // 13/08/2025,tungnt8
        public Task<CampaignAPIResultModel<object>> SyncCampaignV2(string objectId, CampaignAPIModel api)
        {
            return _data.SyncCampaignV2(objectId, api);
        }

        public Tuple<List<SelectionItem>, int> GetListItem(CampaignItemFilterModel filter)
        {
            return _data.GetListItem(filter);
        }

        public Tuple<List<SelectionItem>, int> GetListOfferNo(CampaignOfferNoFilterModel filter)
        {
            return _data.GetListOfferNo(filter);
        }

        public Task<Tuple<List<ComboxOptionModel>, int>> GetStoreList(string searchText, int pageIndex = 0, int pageSize = 50)
        {
            return _data.GetStoreList(searchText, pageIndex, pageSize);
        }
        public Task<List<ComboxOptionModel>> GetStoreByCodeList(List<string> listCode)
        {
            return _data.GetStoreByCodeList(listCode);
        }
        public Task<Tuple<List<CampaignImportModel>,string>> Import(DataTable dataImport)
        {
            return _data.Import(dataImport);
        }
        public List<GetOfferTypeModel> GetListOfferType()
        {
            return _data.GetListOfferType();
        }

    }
}
