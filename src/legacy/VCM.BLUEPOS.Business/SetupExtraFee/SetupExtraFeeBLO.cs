using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.SetupExtraFee;
using VCM.BLUEPOS.Model.SetupExtraFee;
using VCM.BLUEPOS.Model.SetupItem;

namespace VCM.BLUEPOS.Business.SetupExtraFee
{
    public interface ISetupExtraFeeBLO
    {
        List<ExtraFeeHeaderModel> LoadExtraFee(string code, string name, string storeGroup, out int totalRecord, int skip, int take);
        ViewSetupExtraFeeModel ViewInfoExtraFee(string code);
        List<ExtraFeeStroreGroupModel> ListGroupSiteExtraFee(string groupCode, string groupName, out int total, int skip, int take);
        List<StoreExtraFeeModel> ListStoreFeeByGroup(string groupCode, string storeNo, string storeName, out int total, int skip, int take);
        Tuple<bool, string> DeleteGroupSite(string groupSite);
        Tuple<bool, string, ExtraFeeStroreGroupModel> SetupGroupSite(ExtraFeeStroreGroupModel model);
        List<POSGroupModel> ListPOSGroup();
        Tuple<bool, string, SaveExtraFeeRequest> SaveExtraFee(SaveExtraFeeRequest model);
        Tuple<bool, string> DeleteExtraFee(string code);
    }
    public class SetupExtraFeeBLO : ISetupExtraFeeBLO
    {
        private SetupExtraFeeData data { get; set; }
        public SetupExtraFeeBLO()
        {
            data = new SetupExtraFeeData();
        }

        public List<ExtraFeeHeaderModel> LoadExtraFee(string code, string name, string storeGroup, out int totalRecord, int skip, int take)
        {
            return data.LoadExtraFee(code, name, storeGroup, out totalRecord, skip, take);
        }

        public ViewSetupExtraFeeModel ViewInfoExtraFee(string code)
        {
            return data.ViewInfoExtraFee(code);
        }

        public List<ExtraFeeStroreGroupModel> ListGroupSiteExtraFee(string groupCode, string groupName, out int total, int skip, int take)
        {
            return data.ListGroupSiteExtraFee(groupCode, groupName, out total, skip, take);
        }

        public List<StoreExtraFeeModel> ListStoreFeeByGroup(string groupCode, string storeNo, string storeName, out int total, int skip, int take)
        {
            return data.ListStoreFeeByGroup(groupCode, storeNo, storeName, out total, skip, take);
        }

        public Tuple<bool, string> DeleteGroupSite(string groupSite)
        {
            return data.DeleteGroupSite(groupSite);
        }
        public Tuple<bool, string, ExtraFeeStroreGroupModel> SetupGroupSite(ExtraFeeStroreGroupModel model)
        {
            return data.SetupGroupSite(model);
        }

        public List<POSGroupModel> ListPOSGroup()
        {
            return data.ListPOSGroup();
        }

        public Tuple<bool, string, SaveExtraFeeRequest> SaveExtraFee(SaveExtraFeeRequest model)
        {
            return data.SaveExtraFee(model);
        }

        public Tuple<bool, string> DeleteExtraFee(string code)
        {
            return data.DeleteExtraFee(code);
        }
    }
}
