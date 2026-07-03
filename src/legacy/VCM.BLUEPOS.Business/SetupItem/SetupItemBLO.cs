using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.SetupItem;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.SetupItem;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Business.SetupItem
{
    public interface ISetupItemBLO
    {
        List<ItemProcedureModel> LoadItem(string keyWord, out int totalRecord, int pageNumber, int pageSize);
        ViewCapNhatSanPhamModel ViewInfoItem(string Id);
        List<SetupItemModel> ListItemSetup(string itemHeader,string itemNo, string description, out int total, int skip, int take);
        List<SetupItemModel> ListItemByGroup(string itemHeader, string itemNo, string description, string groupCode, out int total, int skip, int take);
        Tuple<bool, string, SetupItemRequestModel> SaveItem(SetupItemRequestModel model);
        List<SalesTypeModel> ListSalesType();
        List<POSGroupModel> GetPOSGroup(string searchText);
        List<POSGroupModel> GetParentPOSGroup();
        POSGroupModel GetDetailPosGroup(int id);
        List<PosGroupItemModel> GetPosGroupItemByGroupCode(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize);
        List<PosGroupItemModel> GetPosGroupItemIsValid(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize);

        // Kenh ban hang doi tac
        List<OptionModel> LoadCategoryByPartner();
        List<ItemPartnerResponseModel> LoadItemPartnerSales(string keyWord, string mch2, string mch5, out int totalRecord, int pageNumber, int pageSize);
        Tuple<bool, string, SetupItemPartnerModel> SaveItemListForPartner(SetupItemPartnerModel model);
        List<ItemPartnerResponseModel> GetItemPartnerList(string salestype, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize);
        ViewSetupItemPartnerDetailModel ViewInforItemDetailByPartner(string itemno);
        ResultResponseModel UpdateItemDetailByPartner(UpdateItemPartnerModel req);
        // Pos group
        ResultResponseModel CreatePosGroup(POSGroupModel req);
        ResultResponseModel UpdatePosGroup(POSGroupModel req, bool isParent);
        ResultResponseModel UpdateStatusPosGroup(int id, int status);
        ResultResponseModel CreatePosGroupItem(PosGroupItemModel req);
        ResultResponseModel DeletePosGroupItem(int id);

        // GrabFood
        List<OptionComboxModel> LoadComboxCategorybyGrabFood();
        List<CheckItemGrabFoodModel> CheckItemGrabFood(string itemNo);
        List<ToppingGrabFoodModel> GetToppingByGrabFood();
        Tuple<bool, string, SetupItemRequestModel> SaveItemV2(SetupItemRequestModel model);



    }

    public class SetupItemBLO : ISetupItemBLO
    {
        private SetupItemData data { get; set; }
        public SetupItemBLO()
        {
            data = new SetupItemData();
        }
        public List<ItemProcedureModel> LoadItem(string keyWord, out int totalRecord, int pageNumber, int pageSize)
        {
            return data.LoadItem(keyWord, out totalRecord, pageNumber, pageSize);
        }
        public ViewCapNhatSanPhamModel ViewInfoItem(string Id)
        {
            return data.ViewInfoItem(Id);
        }
        public List<SetupItemModel> ListItemSetup(string itemHeader, string itemNo, string description, out int total, int skip, int take)
        {
            return data.ListItemSetup(itemHeader, itemNo, description, out total, skip, take);
        }
        public Tuple<bool, string, SetupItemRequestModel> SaveItem(SetupItemRequestModel model)
        {
            return data.SaveItem(model);
        }

        // tungnt8: 21/04/2025
        public Tuple<bool, string, SetupItemRequestModel> SaveItemV2(SetupItemRequestModel model)
        {
            return data.SaveItemV2(model);
        }

        public List<SalesTypeModel> ListSalesType()
        {
            return data.ListSalesType();
        }
        public List<SetupItemModel> ListItemByGroup(string itemHeader, string itemNo, string description, string groupCode, out int total, int skip, int take)
        {
            return data.ListItemByGroup(itemHeader, itemNo, description, groupCode, out total, skip, take);
        }

        // ---------- San pham ban kenh doi tac -----------

        public List<OptionModel> LoadCategoryByPartner()
        {
            return data.LoadCategoryByPartner();
        }
        public List<ItemPartnerResponseModel> LoadItemPartnerSales(string keyWord, string mch2, string mch5, out int totalRecord, int pageNumber, int pageSize)
        {
            return data.LoadItemPartnerSales(keyWord, mch2, mch5, out totalRecord, pageNumber, pageSize);
        }

        public Tuple<bool, string, SetupItemPartnerModel> SaveItemListForPartner(SetupItemPartnerModel model)
        {
            return data.SaveItemListForPartner(model);
        }

        public List<ItemPartnerResponseModel> GetItemPartnerList(string salestype, string storeno, string mch2, string keyWord, string status, out int totalRecord, int pageNumber, int pageSize)
        {
            return data.GetItemPartnerList(salestype, storeno, mch2, keyWord, status, out totalRecord, pageNumber, pageSize);
        }

        public ViewSetupItemPartnerDetailModel ViewInforItemDetailByPartner(string itemno)
        {
            return data.ViewInforItemDetailByPartner(itemno);
        }

        public ResultResponseModel UpdateItemDetailByPartner(UpdateItemPartnerModel req)
        {
            return data.UpdateItemDetailByPartner(req);
        }

        public List<POSGroupModel> GetPOSGroup(string searchText)
        {
            return data.GetPOSGroup(searchText);
        }
        public List<POSGroupModel> GetParentPOSGroup()
        {
            return data.GetParentPOSGroup();
        }
        public POSGroupModel GetDetailPosGroup(int id)
        {
            return data.GetDetailPosGroup(id);
        }
        public List<PosGroupItemModel> GetPosGroupItemByGroupCode(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize)
        {
            return data.GetPosGroupItemByGroupCode(groupCode, searchText, out totalRecord, pageNumber, pageSize);
        }
        public List<PosGroupItemModel> GetPosGroupItemIsValid(string groupCode, string searchText, out int totalRecord, int pageNumber, int pageSize)
        {
            return data.GetPosGroupItemIsValid(groupCode, searchText, out totalRecord, pageNumber, pageSize);
        }
        public ResultResponseModel CreatePosGroup(POSGroupModel req)
        {
            return data.CreatePosGroup(req);
        }
        public ResultResponseModel UpdatePosGroup(POSGroupModel req, bool isParent)
        {
            return data.UpdatePosGroup(req, isParent);
        }
        public ResultResponseModel UpdateStatusPosGroup(int id, int status)
        {
            return data.UpdateStatusPosGroup(id, status);
        }
        public ResultResponseModel CreatePosGroupItem(PosGroupItemModel req)
        {
            return data.CreatePosGroupItem(req);
        }
        public ResultResponseModel DeletePosGroupItem(int id)
        {
            return data.DeletePosGroupItem(id);
        }
        
        // GrabFood
        public List<OptionComboxModel> LoadComboxCategorybyGrabFood()
        {
            return data.LoadComboxCategorybyGrabFood();
        }
        public List<CheckItemGrabFoodModel> CheckItemGrabFood(string itemNo)
        {
            return data.CheckItemGrabFood(itemNo);
        }
        public List<ToppingGrabFoodModel> GetToppingByGrabFood()
        {
            return data.GetToppingByGrabFood();
        }




    }
}
