using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Web.UI.WebControls;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.ROP;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using VCM.POSBLUE.Model.Dtos.ROP;
using VCM.POSBLUE.Model.GiftBox;
using VCM.POSBLUE.Model.SAP;

namespace TCX.WebApiCore
{
    internal interface IROPVoucherService
    {
        (bool, string) GetToken();
        (bool, string, List<VoucherStatusResponse>) CreateVoucher(List<CreateVoucherModel> model, string voucherType, int statusId);
        (bool, string, List<VoucherStatusResponse>) UpdateVoucherStatus(VoucherUpdateModel model, RedisManager redisCacheService);
        (bool, string, VoucherStatusResponse) CheckVouchers(CheckVouchersROP request);
        (bool, string, VoucherStatusResponse) CheckVoucherBySerials(CheckVouchersROP request);
        (bool, string, object) GenOspQRLogin(GenOspQRLogin request);
        void UpdateRedeemCpnVchCodeSends();
    }
    public class ROPVoucherService : IROPVoucherService
    {
        private readonly MemoryCacheService _memoryCacheService;
        private readonly SysWebApiDto _sysWebApiDto;
        private readonly KibanaService _kibanaService;
        public ROPVoucherService()
        {
            _memoryCacheService = new MemoryCacheService();
            _kibanaService = new KibanaService();
            _sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.ROP.ToString()).FirstOrDefault();
        }
        public (bool, string, VoucherStatusResponse) CheckVoucherBySerials(CheckVouchersROP request)
        {
            try
            {
                var lstVoucher = new List<RspVoucherInfoROP>();
                var token = GetToken();
                if (!token.Item1)
                {
                    return (false, MessConst.ApiNotLogin, null);
                }
                if (_sysWebApiDto == null)
                {
                    return (false, MessConst.NotFounDataConfig, null);
                }

                string url = _sysWebApiDto.Host + _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CheckVoucherBySerials".ToUpper()).Route ?? "";
                _kibanaService.LogRequest(url, ComputerHelper.GetHostName(), "", JsonConvert.SerializeObject(request));

                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();               
                var api = new ApiHelper(
                                url, _sysWebApiDto.Authorization, token.Item2, "POST", JsonConvert.SerializeObject(request), true, _sysWebApiDto.HttpProxy, _sysWebApiDto.Bypasslist.Split(';'), request.PosTerminal
                                );

                var response = RspDataROP(api.InteractWithApi(), url, request.PosTerminal ?? "", voucherCode:JsonConvert.SerializeObject(request.VoucherNumbers));
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                
                _kibanaService.LogResponse(url, ComputerHelper.GetHostName(), milliseconds, "", JsonConvert.SerializeObject(response));
                //SaveLogToDB("CheckVoucherBySerials", request.PosTerminal, request.VoucherNumbers[0], JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(response));

                if (response != null)
                {
                    if (response.ResponseCode == 200)
                    {
                        List<VoucherStatusResponse> lstVouchers = new List<VoucherStatusResponse>();
                        var dataRspROP = JsonConvert.DeserializeObject<List<RspVoucherInfoROP>>(JsonConvert.SerializeObject(response.Data, Formatting.Indented,
                                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 }).FirstOrDefault();

                        if (dataRspROP != null && !VoucherROPConst.StatusVoucherSerialCheckROP.Contains(dataRspROP.StatusId))
                        {
                            var dataError = new VoucherStatusResponse
                            {
                                Status = VoucherROPHelper.GetNameStatusVoucher()[dataRspROP.StatusId] ?? "",
                                Return = dataRspROP.StatusId.ToString(),
                                VoucherNumber = dataRspROP.VoucherCode,
                                Voucher_Currency = "VND",
                            };
                            return (false, GetMessageVoucherROP("CheckVoucherBySerials", dataRspROP.StatusId.ToString(), request.VoucherNumbers.First()), dataError);
                        }
                        else if (dataRspROP != null && VoucherROPConst.StatusVoucherSerialCheckROP.Contains(dataRspROP.StatusId))
                        {
                            bool flag = false;
                            var susscessData = new VoucherStatusResponse
                            {
                                Status = VoucherROPHelper.GetCodeStatusByIdVoucher()[dataRspROP.StatusId].ToUpper() ?? "",
                                Return = "0",
                                ActicleNo = dataRspROP.SAPCode.Trim(),
                                ActicleType = dataRspROP.SAPArticleType ?? "",
                                Value = dataRspROP.VoucherValue.ToString(),
                                VoucherNumber = dataRspROP.VoucherCode,
                                Voucher_Currency = "VND",
                                Validity_From_Date = dataRspROP.ValidFrom.ToString("dd/MM/yyyy"),
                                Expiry_Date = dataRspROP.ValidTo.ToString("dd/MM/yyyy"),
                                CompanyCode = dataRspROP.CompanyCode.Trim(),
                                Partner = PartnerEnum.ROP.ToString(),
                                IsEmployee = false
                            };

                            if (VoucherROPConst.StatusVoucherSerialCheckROP.Contains(dataRspROP.StatusId) && dataRspROP.StorageSite == request.Site)
                            {
                                flag = true;
                            }
                            else
                            {
                                flag = false;
                                dataRspROP.StatusId = 99;
                            }
                            return (flag, GetMessageVoucherROP("CheckVoucherBySerials", dataRspROP.StatusId.ToString(), request.VoucherNumbers.First()), susscessData);
                        }
                        else
                        {
                            return (false, response.Message ?? "Lỗi gọi API ROP", null);
                        }
                    }
                    else
                    {
                        return (false, response.Message ?? "Lỗi gọi API ROP", null);
                    }
                }
                else
                {
                    return (false, MessConst.ApiError + PartnerEnum.ROP.ToString(), null);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message.ToString(), null);
            }
        }
        public (bool, string, VoucherStatusResponse) CheckVouchers(CheckVouchersROP request)
        {
            try
            {
                var lstVoucher = new List<RspVoucherInfoROP>();
                var token = GetToken();
                if (!token.Item1)
                {
                    return (false, MessConst.ApiNotLogin, null);
                }
                if (_sysWebApiDto == null)
                {
                    return (false, MessConst.NotFounDataConfig, null);
                }

                string url = _sysWebApiDto.Host + _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "CheckVouchers".ToUpper()).Route ?? "";
                _kibanaService.LogRequest(url, ComputerHelper.GetHostName(), "", JsonConvert.SerializeObject(request));

                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();
                var api = new ApiHelper(
                                url, 
                                _sysWebApiDto.Authorization, 
                                token.Item2, 
                                "POST", 
                                JsonConvert.SerializeObject(request), 
                                true, 
                                _sysWebApiDto.HttpProxy, 
                                _sysWebApiDto.Bypasslist.Split(';'), 
                                request.PosTerminal
                                );
                var response = RspDataROP(api.InteractWithApi(), url, request.PosTerminal??"",voucherCode: JsonConvert.SerializeObject(request.VoucherNumbers));

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                _kibanaService.LogResponse(url, ComputerHelper.GetIpServer(), milliseconds, "", JsonConvert.SerializeObject(response));
                if (response != null)
                {
                    if(response.ResponseCode == 200)
                    {
                        List<VoucherStatusResponse> lstVouchers = new List<VoucherStatusResponse>();
                        var dataRspROP = JsonConvert.DeserializeObject<List<RspVoucherInfoROP>>(JsonConvert.SerializeObject(response.Data,Formatting.Indented,
                                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                                                 new JsonSerializerSettings
                                                 {
                                                     NullValueHandling = NullValueHandling.Ignore
                                                 }).FirstOrDefault();
                        
                        if(dataRspROP != null && !VoucherROPConst.StatusVoucherROP.Contains(dataRspROP.StatusId))
                        {
                            var dataError = new VoucherStatusResponse
                            {
                                Status = VoucherROPHelper.GetNameStatusVoucher()[dataRspROP.StatusId] ?? "",
                                Return = "E4", //dataRspROP.StatusName,
                                VoucherNumber = dataRspROP.VoucherCode,
                                Voucher_Currency = "VND",
                            };
                            return (false, GetMessageVoucherROP("CheckVouchers", dataRspROP.StatusId.ToString(), request.VoucherNumbers.First()), dataError);
                        }
                        else if(dataRspROP != null && VoucherROPConst.StatusVoucherROP.Contains(dataRspROP.StatusId))
                        {
                            bool flag = false;
                            var susscessData = new VoucherStatusResponse
                            {
                                Status = VoucherROPHelper.GetCodeStatusByIdVoucher()[dataRspROP.StatusId].ToUpper() ?? "",
                                Return = "0",
                                ActicleNo = dataRspROP.SAPCode.Trim(),
                                ActicleType = dataRspROP.SAPArticleType??"",
                                Value = ((int)dataRspROP.VoucherValue).ToString(),
                                VoucherNumber = dataRspROP.VoucherCode,
                                Voucher_Currency = "VND",
                                Validity_From_Date = dataRspROP.ValidFrom.ToString("dd/MM/yyyy"),
                                Expiry_Date = dataRspROP.ValidTo.ToString("dd/MM/yyyy"),
                                CompanyCode = dataRspROP.CompanyCode.Trim(),
                                Partner = PartnerEnum.ROP.ToString(),
                                IsEmployee = false
                            };

                            if(VoucherROPConst.StatusVoucherInCheckROP.Contains(dataRspROP.StatusId))
                            {
                                flag = true;
                            }

                            return (flag, GetMessageVoucherROP("CheckVouchers", dataRspROP.StatusId.ToString(), request.VoucherNumbers.First()), susscessData);
                        }
                        else
                        {
                            return (false, response.Message ?? "Lỗi gọi API ROP", null);
                        }            
                    }
                    else
                    {
                        return (false, response.Message??"Lỗi gọi API ROP", null);
                    }
                }
                else
                {
                    return (false, MessConst.ApiError + PartnerEnum.ROP.ToString(), null);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message.ToString(), null);
            }
        }
        public (bool, string, List<VoucherStatusResponse>) CreateVoucher(List<CreateVoucherModel> model, string voucherType, int statusId)
        {
            try
            {
                var token = GetToken();
                if (!token.Item1)
                {
                    return (false, token.Item2, null);
                }
                if (_sysWebApiDto == null)
                {
                    return (false, MessConst.NotFounDataConfig, null);
                }

                var firstModel = model.FirstOrDefault();

                var request = new InsertVoucherROP()
                {
                    POSTerminal = firstModel.POSTerminal,
                    SiteCode = firstModel.SiteCode,
                    ArticleType = voucherType // 3 loại: CP/VC/BNT
                };

                List<InsertVoucherDataROP> insertVoucherROPs = new List<InsertVoucherDataROP>();
                //var OrderNumber = firstModel.POSTerminal + DateTime.Now.ToString("yyyyMMddHHmmssff");
                for (int i = 0; i < model.Count; i++)
                {
                    if (!string.IsNullOrEmpty(model[i].VoucherNumber))
                    {
                        insertVoucherROPs.Add(new InsertVoucherDataROP()
                        {
                            OrderNumber = model[i].OrderNo,
                            VoucherCode = model[i].VoucherNumber,
                            VoucherValue = model[i].Value,
                            BonusBuy = model[i].BonusBuy ?? "",
                            SAPCode = model[i].Article_No??"",
                            StatusId = statusId,
                            VoucherPercent = 0,
                            ValidFrom = StringHelper.FormartDate(model[i].From_Date,'/','-', "yyyy-MM-dd"),
                            ValidTo = StringHelper.FormartDate(model[i].Expiry_Date, '/', '-', "yyyy-MM-dd")
                        });
                    }
                }
                request.Vouchers = insertVoucherROPs;
                string url = _sysWebApiDto.Host + _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "InsertVoucher".ToUpper()).Route ?? "";

                _kibanaService.LogRequest(url, ComputerHelper.GetHostName(), "", JsonConvert.SerializeObject(request));
                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();
                var api = new ApiHelper(
                                url, _sysWebApiDto.Authorization, token.Item2, "POST", JsonConvert.SerializeObject(request), true, _sysWebApiDto.HttpProxy, _sysWebApiDto.Bypasslist.Split(';'), model.FirstOrDefault().POSTerminal
                                );

                var response = RspDataROP(api.InteractWithApi(), url, model.FirstOrDefault().POSTerminal ?? "", voucherCode: JsonConvert.SerializeObject(request.Vouchers));

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                
                _kibanaService.LogResponse(url, ComputerHelper.GetHostName(), milliseconds, "", JsonConvert.SerializeObject(response));
                //SaveLogToDB("CreateVoucher", model.FirstOrDefault().POSTerminal, model.FirstOrDefault().VoucherNumber, JsonConvert.SerializeObject(model), JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(response));
                
                if (response != null)
                {
                    if(response.ResponseCode == 200)
                    {
                        var result = request.Vouchers.Select(x => new VoucherStatusResponse 
                                { 
                                    Status = VoucherROPStatusEnum.Created.ToString(), 
                                    Return = "0",
                                    VoucherNumber = x.VoucherCode,
                                    Value = x.VoucherValue.ToString()
                                 }).ToList();
                        return (true, response.Message, result);
                    }
                    else
                    {
                        string mess = response.Message;
                        if(response.TechnicalMessage.Count() > 0)
                        {
                            mess = response.TechnicalMessage.First().ToString();
                        }
                        return (false, mess, null);
                    }
                }
                else
                {
                    return (false, MessConst.ApiError + "ROP", null);
                }
            }
            catch(Exception ex)
            {

                return (false, ex.Message.ToString(), null);
            }
        }
        public (bool, string, List<VoucherStatusResponse>) UpdateVoucherStatus(VoucherUpdateModel model, RedisManager redisCacheService)
        {
            try
            {
                var lstVoucher = new List<RspVoucherInfoROP>();
                var token = GetToken();
                if (!token.Item1)
                {
                    return (false, MessConst.ApiNotLogin, null);
                }
                if (_sysWebApiDto == null)
                {
                    return (false, MessConst.NotFounDataConfig, null);
                }

                string url = _sysWebApiDto.Host + _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "UpdateVoucherStatus".ToUpper()).Route ?? "";
                List<string> vouchers = new List<string>();
                foreach(var item in model.ListSeriNo)
                {
                    if(item.voucherNumber.Length > 15 && !ValidateHelper.IsVoucherCapillary(item.voucherNumber).Item1)
                    {
                        vouchers.Add(item.voucherNumber.ToString());
                    }
                }

                var request = new UpdateVoucherStatusROP()
                {
                    PosTerminal = model.POSTerminal,
                    Site = model.SiteCode,
                    Status = VoucherROPHelper.GetStatusIdByCodeVoucher()[model.ListSeriNo.FirstOrDefault().status],
                    OrderNumber = model.OrderNo??"",
                    VoucherNumbers = vouchers.ToArray()
                };

                _kibanaService.LogRequest(url, ComputerHelper.GetHostName(), "", JsonConvert.SerializeObject(request));
                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();

                var api = new ApiHelper(
                                url, _sysWebApiDto.Authorization, token.Item2, "POST", JsonConvert.SerializeObject(request), true, _sysWebApiDto.HttpProxy, _sysWebApiDto.Bypasslist.Split(';'), model.POSTerminal
                                );
                var response = RspDataROP(api.InteractWithApi(), url, request.PosTerminal ?? "", voucherCode: JsonConvert.SerializeObject(request.VoucherNumbers));
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                _kibanaService.LogResponse(url, ComputerHelper.GetHostName(), milliseconds, "",model.OrderNo + "|" + JsonConvert.SerializeObject(model.ListSeriNo) + "@@@" + JsonConvert.SerializeObject(response));
                if (response != null)
                {
                    if (response.ResponseCode == 200)
                    {                       
                        var result = new List<VoucherStatusResponse>();
                        foreach(var v in vouchers)
                        {
                            result.Add(new VoucherStatusResponse()
                            {
                                Return = "0",
                                Status = VoucherStatusEnum.RDM.ToString(),
                                VoucherNumber = v,
                                Expiry_Date = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                            });
                        }
                        
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                redisCacheService.HashSet<List<VoucherStatusResponse>>(SystemEnum.BLUEPOS.ToString() + ":UpdateVoucherStatus", model.OrderNo, result);
                                RabbitMQProducer.SendMessage(RabitMQConst.Queue_UpdateStatusVoucher, model.OrderNo, JsonConvert.SerializeObject(result));
                            }
                            catch (Exception ex)
                            {
                                _kibanaService.LogException("UpdateVoucherStatus.HashSet", model.POSTerminal, 0, "", JsonConvert.SerializeObject(ex));
                            }
                        });
                        return (true, response.Message ?? @"Thành công", result);
                    }
                    else if(response.ResponseCode == 1006)
                    {
                        List<VoucherStatusResponse> voucherStatusResponses = new List<VoucherStatusResponse>();
                        var dataRspROP = JsonConvert.DeserializeObject<List<RspVoucherInfoNullROP>>(JsonConvert.SerializeObject(response.Data));

                        if (dataRspROP != null && dataRspROP.Count > 0)
                        {
                            foreach (var item in dataRspROP)
                            {
                                voucherStatusResponses.Add(new VoucherStatusResponse()
                                {
                                    Status = GetMessageVoucherROP("UpdateVoucherStatus", item.StatusId.ToString(), item.VoucherCode??(item.SerialNumber??"")),
                                    Return = "E16",
                                    Partner = PartnerEnum.ROP.ToString(),
                                    CompanyCode = item.StorageSite, 
                                    VoucherNumber = item.VoucherCode??(item.SerialNumber ?? "")
                                });
                            }

                            string m = GetMessageVoucherROP("UpdateVoucherStatus", dataRspROP.FirstOrDefault().StatusId.ToString(), voucherStatusResponses.FirstOrDefault().VoucherNumber);
                            return (false, m, voucherStatusResponses);
                        }
                        else
                        {
                            var errorData = new List<VoucherStatusResponse>();
                            if(response.TechnicalMessage.Count() > 0)
                            {
                                for(var i = 0; i<response.TechnicalMessage.Count(); i++)
                                {
                                    errorData.Add(new VoucherStatusResponse()
                                    {
                                        Status = response.TechnicalMessage[i] ?? "",
                                        Return = "E16"
                                    });
                                }
                            }
                            return (false, response.Message ?? MessConst.ApiError, errorData);
                        }
                    }
                    else
                    {
                        return (false, response.Message?? MessConst.ApiError, null);
                    }
                }
                else
                {
                    return (false, MessConst.ApiError + PartnerEnum.ROP.ToString(), null);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message.ToString(), null);
            }
        }
        public (bool, string) GetToken()
        {
            try
            {
                if (_sysWebApiDto == null)
                {
                    return (false, MessConst.NotFounDataConfig);
                }

                return (true, API.Common.Shared.EncryptionHelper.Base64Encode(_sysWebApiDto.UserName +":" + _sysWebApiDto.Password));
            }
            catch
            {
                return (false, MessConst.ApiError + "ROP - Không lấy được token");
            }
        }
        private ResponseROP RspDataROP(string response, string url = "", string posNo = "", string voucherCode = "")
        {
            var result = StringHelper.StringToObject<ResponseROP>(response);
            if(result == null)
            {
                var respWarning = new ResponseROP()
                {
                    ResponseCode = (int)EStatusResponse.ServiceUnavailable,
                    TechnicalMessage = new string[] { response } ,
                    Message = $"Lỗi kết nối tới WebApi ROP, vui lòng liên hệ IT",
                    Data = new { TechnicalMessage  = response }
                };
                _kibanaService.LogException(url, posNo, 0, "", $"{voucherCode} response: {response} error:" + JsonConvert.SerializeObject(respWarning));
                Task.Run(() => _kibanaService.SendMessageSMS($"ERROR: {response} khi POS: {posNo} sử dụng voucher:{voucherCode} link webApi: {url}", "Error", "Lỗi sử dụng voucher ROP", appCode: AppCodeMessageEnum.BLUEPOS_ROP_APIVOUCHER.ToString()));
                return respWarning;
            }
            return result;
        }
        private string GetMessageVoucherROP(string type, string status, string voucherNo = "")
        {
            string mess = @"Tham số truyền vào không hợp lệ";
            try
            {
                var messConfig = _memoryCacheService.GetNotifyConfig(_memoryCacheService.GetConnectStringCentralMD()).Where(x=>x.AppCode == "ROP").ToList();
                if (messConfig != null && messConfig.Count > 0)
                {
                    mess = messConfig.FirstOrDefault(x => x.ActionType == type && x.Status == status) != null ? messConfig.FirstOrDefault(x => x.ActionType == type && x.Status == status).Message : mess;
                    if (mess.ToUpper().Contains("{VoucherNo}".ToUpper()))
                    {
                        try
                        {
                            StringBuilder builder = new StringBuilder(mess);
                            builder.Replace("{VoucherNo}", voucherNo);
                            mess = builder.ToString();
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("GetMessageVoucherROP", ex);
            }
            return mess;
        }
        public List<VoucherStatusResponse> ReTryUpdateVoucherStatus(string orderNo, RedisManager redisCacheService)
        {
            var getDataVoucher = redisCacheService.HashGet<List<VoucherStatusResponse>>(SystemEnum.BLUEPOS.ToString() + ":UpdateVoucherStatus", orderNo);
            if(getDataVoucher != null &&  getDataVoucher.Count > 0 )
            {
                return getDataVoucher;
            }
            return null;
        }
        public (bool, string, object) GenOspQRLogin(GenOspQRLogin request)
        {
            try
            {
                var token = GetToken();
                if (!token.Item1)
                {
                    return (false, MessConst.ApiNotLogin, null);
                }
                if (_sysWebApiDto == null)
                {
                    return (false, MessConst.NotFounDataConfig, null);
                }

                var requestROP = new GenOspQRLoginROP()
                {
                    Site = request.StoreNo,
                    PosTerminal = request.PosNo
                };

                string url = _sysWebApiDto.Host + _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GenOspQRLogin".ToUpper()).Route ?? "";
                _kibanaService.LogRequest(url, ComputerHelper.GetHostName(), "", JsonConvert.SerializeObject(request));

                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();
                var api = new ApiHelper(
                                url, _sysWebApiDto.Authorization, token.Item2, "POST", JsonConvert.SerializeObject(requestROP), true, _sysWebApiDto.HttpProxy, _sysWebApiDto.Bypasslist.Split(';'), request.PosNo
                                );

                var response = ConvertHelper.ToObject<ResponseROP>(api.InteractWithApi());
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                _kibanaService.LogResponse(url, ComputerHelper.GetHostName(), milliseconds, "", JsonConvert.SerializeObject(response));
                //SaveLogToDB("CheckVoucherBySerials", request.PosTerminal, request.VoucherNumbers[0], JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(response));

                if (response != null)
                {
                    if (response.ResponseCode == 200)
                    {
                        return (true, response.Message, response.Data);
                    }
                    else
                    {
                        return (false, response.Message ?? "Lỗi gọi API ROP", null);
                    }
                }
                else
                {
                    return (false, MessConst.ApiError + PartnerEnum.ROP.ToString(), null);
                }
            }
            catch (Exception ex)
            {

                return (false, ex.Message.ToString(), null);
            }
        }
        public void UpdateRedeemCpnVchCodeSends()
        {
            try
            {
                var _redisHelper = new RedisConnectionHelper();
                var keys = _redisHelper.GetKeys("UpdateVoucherStatus");
                if(keys.Count > 0)
                {
                    FileHelper.WriteLogs($"Total keys UpdateVoucherStatus: {keys.Count}");
                    var dapperCentral = new DapperConnection(_memoryCacheService.GetConnectStringCentralMD());
                    using (IDbConnection db = dapperCentral.CreateConnDB)
                    {
                        db.Open();
                        foreach (var key in keys)
                        {
                            var data = _redisHelper.Get<List<VoucherStatusResponse>>(key);
                            if (data != null && data.Count > 0)
                            {
                                foreach (var item in data)
                                {
                                    var sql = "UPDATE [CpnVchCodeSend] SET UserOrderNo = @OrderNo WHERE UserOrderNo IS NULL AND [Code] = @VoucherNumber;";
                                    var rowsAffected = db.Execute(sql, new { OrderNo = StringHelper.ConverStringToArray(key, ':')[1], VoucherNumber = item.VoucherNumber });
                                    if(rowsAffected > 0)
                                    {
                                        FileHelper.WriteLogs($"rowsAffected:{rowsAffected} - {item.VoucherNumber} - {key}");
                                    }
                                }
                            }
                            Task.Delay(100);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteLogs($"UpdateRedeemCpnVchCodeSends.Exception: {JsonConvert.SerializeObject(ex)}");
                throw;
            }
        }
    }
}
