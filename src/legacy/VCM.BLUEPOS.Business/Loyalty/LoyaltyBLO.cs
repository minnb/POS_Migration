using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Loyalty;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Loyalty;


namespace VCM.BLUEPOS.Business.Loyalty
{
    public interface ILoyaltyBLO
    {
        List<GiftRedeemResponseModel> GetSetupLoyaltyList(DateTime fromDate, DateTime toDate, string itemNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null);
        List<SetupLoyaltyResponseModel> GetProductList(string textSearch, out int totalRecord, int pageNumber = 0, int pageSize = 100);
        ResultResponseModel CreateSetupLoyalty(CreateSetupLoyaltyModel req);
        ResultResponseModel UpdateSetupLoyalty(UpdateSetupLoyaltyModel req);
        ResultResponseModel DeleteSetupLoyalty(DeleteLoyaltyModel req);
        List<ListGiftRedeemModalModel> GetItemGiftRedeemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100);
        ResultResponseModel CreateSetupLoyaltyForTabGiftRedeem(CreateSetupLoyaltyTabGiftRedeemModel req);
        Tuple<int, string> ImportExcelGiftRedeem(string userName, DataTable dt);
        List<ExportExcelGiftRedeemResponseModel> ExportExcelGiftRedeemList(DateTime fromDate, DateTime toDate, string itemNo, string status);

        List<MemberEarnItemResponseModel> SetupMemberEarnItemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100);
        List<ExportMemberEarnItemResponseModel> ExportExcel_MemberEarnItemList(string textSearch, string status);
        ResultResponseModel UpdateSetupMemberEarnItem(UpdateSetupMemberEarnItemModel req);
        ResultResponseModel DeleteSetupMemberEarnItem(DeleteSetupMemberEarnItemModel req);
        Tuple<int, string, string> ImportExcelMemberEarnItem(string userName, List<ImportExcelSetupMemberEarnItemModel> listData);
        List<GiftCouponResponseModel> GetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcelGiftCouponResponseModel> ExportExcelGetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX);
        

    }

    public class LoyaltyBLO : ILoyaltyBLO
    {
        private LoyaltyData _data { get; set; }
        public LoyaltyBLO()
        {
            _data = new LoyaltyData();
        }
        public List<GiftRedeemResponseModel> GetSetupLoyaltyList(DateTime fromDate, DateTime toDate, string itemNo, string status, out int totalRecord, int skip = 0, int take = 100, string SortColumn = null, string SortColumnDirection = null, string searchText = null)
        {
            return _data.GetSetupLoyaltyList(fromDate, toDate, itemNo, status, out totalRecord, skip, take, SortColumn, SortColumnDirection, searchText);
        }
        public List<SetupLoyaltyResponseModel> GetProductList(string textSearch, out int totalRecord, int pageNumber = 0, int pageSize = 100)
        {
            return _data.GetProductList(textSearch, out totalRecord, pageNumber, pageSize);
        }
        public List<ListGiftRedeemModalModel> GetItemGiftRedeemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100)
        {
            return _data.GetItemGiftRedeemList(textSearch, status, out totalRecord, pageNumber, pageSize);
        }
        public ResultResponseModel CreateSetupLoyalty(CreateSetupLoyaltyModel req)
        {
            return _data.CreateSetupLoyalty(req);
        }
        public ResultResponseModel UpdateSetupLoyalty(UpdateSetupLoyaltyModel req)
        {
            return _data.UpdateSetupLoyalty(req);
        }
        public ResultResponseModel DeleteSetupLoyalty(DeleteLoyaltyModel req)
        {
            return _data.DeleteSetupLoyalty(req);
        }
        public ResultResponseModel CreateSetupLoyaltyForTabGiftRedeem(CreateSetupLoyaltyTabGiftRedeemModel req)
        {
            return _data.CreateSetupLoyaltyForTabGiftRedeem(req);
        }

        public Tuple<int, string> ImportExcelGiftRedeem(string userName, DataTable dt)
        {
            return _data.ImportExcelGiftRedeem(userName, dt);
        }

        //public Tuple<int, string> ImportExcelGiftRedeem_V2(string userName, List<ImportExcelGiftRedeemModel> listData)
        //{
        //    return _data.ImportExcelGiftRedeem_V2(userName, listData);
        //}

        public List<ExportExcelGiftRedeemResponseModel> ExportExcelGiftRedeemList(DateTime fromDate, DateTime toDate, string itemNo, string status)
        {
            return _data.ExportExcelGiftRedeemList(fromDate, toDate, itemNo, status);
        }

        //--- 22/05/2024 ---
        public List<MemberEarnItemResponseModel> SetupMemberEarnItemList(string textSearch, string status, out int totalRecord, int pageNumber = 0, int pageSize = 100)
        {
            return _data.SetupMemberEarnItemList(textSearch, status, out totalRecord, pageNumber, pageSize);
        }
        public List<ExportMemberEarnItemResponseModel> ExportExcel_MemberEarnItemList(string TextSearch, string status)
        {
            return _data.ExportExcel_MemberEarnItemList(TextSearch, status);
        }
        public ResultResponseModel UpdateSetupMemberEarnItem(UpdateSetupMemberEarnItemModel req)
        {
            return _data.UpdateSetupMemberEarnItem(req);
        }
        public ResultResponseModel DeleteSetupMemberEarnItem(DeleteSetupMemberEarnItemModel req)
        {
            return _data.DeleteSetupMemberEarnItem(req);
        }
        public Tuple<int, string, string> ImportExcelMemberEarnItem(string userName, List<ImportExcelSetupMemberEarnItemModel> listData)
        {
            return _data.ImportExcelMemberEarnItem(userName, listData);
        }
        public List<GiftCouponResponseModel> GetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetGiftCouponList(fromDate, toDate, orderNo, coupon, textSearch, isUsed, sendCX, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelGiftCouponResponseModel> ExportExcelGetGiftCouponList(DateTime fromDate, DateTime toDate, string orderNo, string coupon, string textSearch, string isUsed, string sendCX)
        {
            return _data.ExportExcelGetGiftCouponList(fromDate, toDate, orderNo, coupon, textSearch, isUsed, sendCX);
        }

        





    }
}
