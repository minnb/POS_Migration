using System;
using System.Collections.Generic;
using TCX.API.Common.Models;
using VCM.POSBLUE.Data.PLG;
using VCM.POSBLUE.Model.Reward;

namespace VCM.POSBLUE.Business.PLG
{
    public interface IPLGBLO
    {
        ResultResponse InforVoucher(string seriNo);
        ResultResponse SaleVoucher(SaleVoucherRequestPOS model);
        ResultResponse RedeemVoucher(ReedemVoucherRequestPOS model);
        void LogVoucher(VoucherCardLogModel model);
        void VoucherTrans(List<VoucherCardTransModel> listmodel);
        Tuple<bool, int, string, RewardCodeSendModel> SendCodeReWardToPOS(RewardCodeRequest model);
    }
    public class PLGBLO : IPLGBLO
    {
        private PLGData data { get; set; }
        public PLGBLO()
        {
            data = new PLGData();
        }
        public ResultResponse InforVoucher(string seriNo)
        {
            return data.InforVoucher(seriNo);
        }

        public ResultResponse SaleVoucher(SaleVoucherRequestPOS model)
        {
            return data.SaleVoucher(model);
        }

        public void LogVoucher(VoucherCardLogModel model)
        {
            data.LogVoucher(model);
        }

        public ResultResponse RedeemVoucher(ReedemVoucherRequestPOS model)
        {
            return data.RedeemVoucher(model);
        }

        public void VoucherTrans(List<VoucherCardTransModel> listmodel)
        {
            data.VoucherTrans(listmodel);
        }

        public Tuple<bool, int, string, RewardCodeSendModel> SendCodeReWardToPOS(RewardCodeRequest model)
        {
            return data.SendCodeReWardToPOS(model);
        }
    }
}
