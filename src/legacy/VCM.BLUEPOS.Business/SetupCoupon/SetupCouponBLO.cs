using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.SetupCoupon;
using VCM.BLUEPOS.Model.SetupCoupon;
using VCM.BLUEPOS.Model.SetupItem;

namespace VCM.BLUEPOS.Business.SetupCoupon
{
    public interface ISetupCouponBLO
    {
        ViewSetupCouponModel ViewInfoICoupon(string itemNo);
        ViewSetupCpnVchModel ViewInfoICpnVch(string itemNo);
        List<CpnVchBOMIssueRuleModel> LoadCouponRule(string issueType, string itemNo, string description, string blocked, out int totalRecord, int skip, int take);
        List<CpnVchBOMIssueRuleModel> GetIssueRuleByPrefix(string prefix);
        Tuple<bool, string, SaveIssueCouponRequest> SaveCoupon(SaveIssueCouponRequest model);
        Tuple<bool, string, SetupAdvancedCouponRequest> SaveAdvancedCoupon(SetupAdvancedCouponRequest model);
        List<LoadCouponCodeModel> CouponCodeLoad(string itemNo, out int totalRecord, int skip, int take);
        List<string> CheckExistsCodeVoucher(List<RandomCode> listCode);
        List<ExcelExampleCouponModel> ExcelExampleCoupon();
        Tuple<bool, string> DeleteItemCoupon(string itemNo);
        Tuple<bool, string, List<CpnItemModel>> CheckListItemExists(List<string> listItem);
        Tuple<bool, string, CpnVchBOMHeaderRequestModel> SaveCpnVch(CpnVchBOMHeaderRequestModel model);
        List<CpnVchBOMHeaderModel> LoadCpnVch(string itemNo, string description, string blocked, out int totalRecord, int skip, int take);
        List<Model.SetupItem.ItemModel> LoadItemByArticleType(string articleType, string itemNo, string itemName, out int totalRecord, int skip, int take);
    }
    public class SetupCouponBLO : ISetupCouponBLO
    {
        private SetupCouponData data { get; set; }
        public SetupCouponBLO()
        {
            data = new SetupCouponData();
        }
        public List<CpnVchBOMIssueRuleModel> LoadCouponRule(string issueType, string itemNo, string description, string blocked, out int totalRecord, int skip, int take)
        {
            return data.LoadCouponRule(issueType, itemNo, description, blocked, out totalRecord, skip, take);
        }

        public List<CpnVchBOMIssueRuleModel> GetIssueRuleByPrefix(string prefix)
        {
            return data.GetIssueRuleByPrefix(prefix);
        }

        public ViewSetupCouponModel ViewInfoICoupon(string itemNo)
        {
            return data.ViewInfoICoupon(itemNo);
        }

        public Tuple<bool, string, SaveIssueCouponRequest> SaveCoupon(SaveIssueCouponRequest model)
        {
            return data.SaveCoupon(model);
        }

        public Tuple<bool, string, SetupAdvancedCouponRequest> SaveAdvancedCoupon(SetupAdvancedCouponRequest model)
        {
            return data.SaveAdvancedCoupon(model);
        }

        public List<LoadCouponCodeModel> CouponCodeLoad(string itemNo, out int totalRecord, int skip, int take)
        {
            return data.CouponCodeLoad(itemNo, out totalRecord, skip, take);
        }
        public List<string> CheckExistsCodeVoucher(List<RandomCode> listCode)
        {
            return data.CheckExistsCodeVoucher(listCode);
        }
        public List<ExcelExampleCouponModel> ExcelExampleCoupon()
        {
            return data.ExcelExampleCoupon();
        }

        public Tuple<bool, string> DeleteItemCoupon(string itemNo)
        {
            return data.DeleteItemCoupon(itemNo);
        }

        public Tuple<bool, string, List<CpnItemModel>> CheckListItemExists(List<string> listItem)
        {
            return data.CheckListItemExists(listItem);
        }

        public ViewSetupCpnVchModel ViewInfoICpnVch(string itemNo)
        {
            return data.ViewInfoICpnVch(itemNo);
        }

        public Tuple<bool, string, CpnVchBOMHeaderRequestModel> SaveCpnVch(CpnVchBOMHeaderRequestModel model)
        {
            return data.SaveCpnVch(model);
        }

        public List<CpnVchBOMHeaderModel> LoadCpnVch(string itemNo, string description, string blocked, out int totalRecord, int skip, int take)
        {
            return data.LoadCpnVch(itemNo, description, blocked, out totalRecord, skip, take);
        }

        public List<ItemModel> LoadItemByArticleType(string articleType, string itemNo, string itemName, out int totalRecord, int skip, int take)
        {
            return data.LoadItemByArticleType(articleType, itemNo,itemName, out totalRecord, skip, take);
        }
    }
}
