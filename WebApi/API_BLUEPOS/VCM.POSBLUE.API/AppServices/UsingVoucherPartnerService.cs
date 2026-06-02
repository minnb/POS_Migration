using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TCX.API.Common.Dtos;
using TCX.API.Common.Enums;
using TCX.API.Common.Models;
using TCX.WebApiCore;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using VCM.POSBLUE.Business.Common;
using VCM.POSBLUE.Business.SAP;
using VCM.POSBLUE.Model.Dtos.DRW;
using VCM.POSBLUE.Model.Dtos.VoucherSAP;
using VCM.POSBLUE.Model.SAP;
namespace VCM.POSBLUE.API.Services
{
    public class UsingVoucherPartnerService
    {
        private readonly RedisCacheService _redisService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly List<SysWebApiDto> _sysWebApiDto;
        public UsingVoucherPartnerService()
        {
            _redisService = new RedisCacheService();
            _memoryCacheService = new MemoryCacheService();
            _sysWebApiDto = _memoryCacheService.GetSysWebApi(ConfigurationManager.ConnectionStrings["DAPPER_PARTNER"].ConnectionString);
        }
        public async Task<List<UpdateVoucherPARDto>> UpdateStatusVoucherPAR(VoucherUpdateModel model)
        {
            List<UpdateVoucherPARDto> voucherNumber = new List<UpdateVoucherPARDto>();
            try
            {
                foreach (var item in model.ListSeriNo)
                {
                    if(item.voucherNumber.Length > 3 && item.voucherNumber.ToUpper().Substring(0, 3) == "PAR")
                    {
                        voucherNumber.Add(new UpdateVoucherPARDto()
                        {
                            VoucherNumber = item.voucherNumber,
                            PosUsed = model.POSTerminal,
                            OrderNo = model.OrderNo??""
                        });
                    }
                }

                if(voucherNumber.Count > 0)
                {
                    string connectStringDapper = _memoryCacheService.GetPOSDataSetup("WWW_DAPPER_PARTNER").Value ?? "";
                    var dapper = new DapperConnection(connectStringDapper);
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        db.Open();
                        foreach(var item in voucherNumber)
                        {
                            var queryUpdate = @"UPDATE POS_VoucherIssueDetail SET UsedTime = getdate(), Status = 'RDM', PosUsed = @PosUsed, OrderNo = @OrderNo WHERE VoucherNumber = @VoucherNumber";
                            await db.ExecuteAsync(queryUpdate, item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("UpdateStatusVoucherPAR Exception:  " + ex.Message.ToString());
            }
            return voucherNumber;
        }
        public Tuple<bool, string, VoucherStatusResponse> CheckVoucherByPhoneNumber(string phoneNumber)
        {
            string connectStringDapper = _memoryCacheService.GetPOSDataSetup("WWW_DAPPER_PARTNER").Value ?? "";
            var dapper = new DapperConnection(connectStringDapper);
            using (IDbConnection db = dapper.CreateConnDB)
            {
                try
                {
                    db.Open();
                    var voucherDetail = db.Query<VoucherIssueDetail>("SELECT * FROM POS_VoucherIssueDetail (NOLOCK) WHERE Status = 'SOLD' AND PhoneNumber = '" + phoneNumber + "' ORDER BY CrtDate;").FirstOrDefault();
                    if (voucherDetail != null)
                    {
                        var result = new VoucherStatusResponse()
                        {
                            VoucherNumber = voucherDetail.VoucherNumber,
                            Value = voucherDetail.Value.ToString(),
                            Return = "0",
                            Status = voucherDetail.Status,
                            Expiry_Date = voucherDetail.ExpiryDate,
                            Validity_From_Date = voucherDetail.FromDate,
                            CompanyCode = "WCM",
                            Voucher_Currency = "VND",
                            ActicleNo = voucherDetail.ActicleNo,
                            ActicleType = voucherDetail.ArticleType
                        };
                        return new Tuple<bool, string, VoucherStatusResponse>(true, "", result);
                    }
                    else
                    {
                        return new Tuple<bool, string, VoucherStatusResponse>(false, string.Format("Số điện thoại {0} không có mã giảm giá", phoneNumber), null);
                    }
                }
                catch (Exception ex) 
                {
                    Serilog.Log.Logger.Information("CheckVoucherByPhoneNumber Exception:  " + ex.Message.ToString());
                    return new Tuple<bool, string, VoucherStatusResponse>(false, @"Lỗi Exception truy cập mã giảm giá O’lala", null);
                }
            }
        }
        public ResultResponse UpdateStatusSfaffPhone_DRW(UpdateStatusSfaffDiscountDto phoneNumber)
        {
            var checkWebConfig = _sysWebApiDto.Where(x => x.AppCode == PartnerEnum.DRW.ToString()).FirstOrDefault();
            if (checkWebConfig != null)
            {
                Serilog.Log.Logger.Information("UpdateStatusSfaffPhone request to WebApi DRW phoneNumber " + phoneNumber.StaffPhone);
                string url = checkWebConfig.Host + checkWebConfig.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "UpdateStatusStaffPhone".ToUpper()).Route ?? "";

                var api = new ApiHelper(
                                url, null, null, "POST", JsonConvert.SerializeObject(phoneNumber), false, checkWebConfig.HttpProxy, checkWebConfig.Bypasslist.Split(';')
                                );

                var response = api.InteractWithApi();
                Serilog.Log.Logger.Information("UpdateStatusSfaffPhone response from WebApi DRW: " + JsonConvert.SerializeObject(response));
                if (!string.IsNullOrEmpty(response))
                {
                    var rspBLUE = JsonConvert.DeserializeObject<HttpResponseBlueDto>(response);
                    return ResponseHelper.SusscessResponse(HttpStatusCode.OK, rspBLUE.Data, rspBLUE.Message);
                }
                else
                {
                    return ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, $"Lỗi gọi WebApi DRW");
                }

            }
            else
            {
                return ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, $"Chưa cấu hình WebApi StaffDiscount ");
            }
        }
    }

    public class UpdateVoucherPARDto
    {
        public string VoucherNumber { get; set; }
        public string PosUsed { get; set; }
        public string OrderNo { get; set; }
    }
}