using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Reward;
using VCM.BLUEPOS.Model.Reward;
using VCM.BLUEPOS.Model.SetupPromotion;

namespace VCM.BLUEPOS.Business.Reward
{
    public interface IRewardBLO
    {
        List<RewardHeaderModel> LoadReward(string code, string title, string offerNo, string status, out int totalRecord, int skip, int take);
        ViewRewardModel InfoReward(string code);
        List<RewardCodeModel> RewardCodeLoad(string rewardNo, out int totalRecord, int skip, int take);
        Tuple<bool, string, RewardHeaderRequest> SaveReward(RewardHeaderRequest model);
        List<OfferHeaderModel> LoadOfferReward(string offerType, string offerNo, string offerName, out int total, int skip, int take);
    }
    public class RewardBLO : IRewardBLO
    {
        private RewardData data { get; set; }
        public RewardBLO()
        {
            data = new RewardData();
        }
        public List<RewardHeaderModel> LoadReward(string code, string title, string offerNo, string status, out int totalRecord, int skip, int take)
        {
            return data.LoadReward(code, title, offerNo, status, out totalRecord, skip, take);
        }

        public ViewRewardModel InfoReward(string code)
        {
            return data.InfoReward(code);
        }

        public List<RewardCodeModel> RewardCodeLoad(string rewardNo, out int totalRecord, int skip, int take)
        {
            return data.RewardCodeLoad(rewardNo, out totalRecord, skip, take);
        }

        public Tuple<bool, string, RewardHeaderRequest> SaveReward(RewardHeaderRequest model)
        {
            return data.SaveReward(model);
        }

        public List<OfferHeaderModel> LoadOfferReward(string offerType, string offerNo, string offerName, out int total, int skip, int take)
        {
            return data.LoadOfferReward(offerType, offerNo, offerName,out total, skip, take);
        }
    }
}
