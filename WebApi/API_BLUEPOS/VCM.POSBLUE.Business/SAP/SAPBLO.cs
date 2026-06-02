using System;
using System.Net;
using VCM.POSBLUE.Data.POS;
using VCM.POSBLUE.Model.SAP;

namespace VCM.POSBLUE.Business.SAP
{
    public interface ISAPBLO
    {
        //VoucherModel CheckVoucher(VoucherRequest model);
        //VoucherModel CheckReturnVoucher(VoucherRequest model);
        //UpdateDataResponse<List<VoucherStatusResponseModel>> UpdateVourcher(VoucherUpdateRequest model);
        //UpdateDataResponse<List<VoucherStatusResponseModel>> UpdateReturVourcher(VoucherUpdateRequest model);
        //CreateDataResponse<List<VoucherStatusResponseModel>> CreateVoucher(List<CreateVoucherModel> model);
        //CreateDataResponse<List<VoucherStatusResponseModel>> CreateReturnVoucher(List<CreateVoucherModel> model);
        void WriteLogSAP(LogSAPModel model);
        Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel> SendCodeToPOS(CpnVchCodeSendRequest model);
    }
    public class SAPBLO : ISAPBLO
    {
        private ISAPData _sapData { get; set; }
        public SAPBLO()
        {
            _sapData = new SAPData();
        }
        public void WriteLogSAP(LogSAPModel model)
        {
            _sapData.WriteLogSAP(model);
        }

        public Tuple<bool, HttpStatusCode, string, CpnVchCodeSendModel> SendCodeToPOS(CpnVchCodeSendRequest model)
        {
            return _sapData.SendCodeToPOS(model);
        }


        //public VoucherModel CheckReturnVoucher(VoucherRequest model)
        //{
        //    return _sapData.CheckReturnVoucher(model);
        //}

        //public VoucherModel CheckVoucher(VoucherRequest model)
        //{
        //    return _sapData.CheckVoucher(model);
        //}

        //public CreateDataResponse<List<VoucherStatusResponseModel>> CreateReturnVoucher(List<CreateVoucherModel> model)
        //{
        //    return _sapData.CreateReturnVoucher(model);
        //}

        //public CreateDataResponse<List<VoucherStatusResponseModel>> CreateVoucher(List<CreateVoucherModel> model)
        //{
        //    return _sapData.CreateVoucher(model);
        //}

        //public UpdateDataResponse<List<VoucherStatusResponseModel>> UpdateReturVourcher(VoucherUpdateRequest model)
        //{
        //    return _sapData.UpdateReturVourcher(model);
        //}

        //public UpdateDataResponse<List<VoucherStatusResponseModel>> UpdateVourcher(VoucherUpdateRequest model)
        //{
        //    return _sapData.UpdateVourcher(model);
        //}
    }
}
