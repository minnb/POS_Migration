using Dapper;
using Elasticsearch.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.ROP;
using TCX.API.Common.Dtos.WinCare;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using TCX.WebApiCore.Shared;
using VTCX.API.Common.Enums;

namespace TCX.WebApiCore
{
    public class WinCareService
    {
        private readonly MemoryCacheService _memoryCacheService;
        private readonly SysWebApiDto _sysWebApiDto;
        private readonly KibanaService _kibanaService;
        private readonly string _connectStringCentralMD;
        private DapperContext _dapperDB;
        private readonly List<int> ResponseCodeSuccess = new List<int>() { 0, 1102 };
        public WinCareService()
        {
            _memoryCacheService = new MemoryCacheService();
            _connectStringCentralMD = _memoryCacheService.GetConnectStringCentralMD();
            _sysWebApiDto = _memoryCacheService.GetSysWebApi(_connectStringCentralMD)?.Where(x => x.AppCode.ToUpper() == PartnerEnum.WINCARE.ToString()).FirstOrDefault();
            _kibanaService = new KibanaService();
        }
        public async Task<string> RequestSentNotifyUserAsync(string qrCode, string storeNo, int typeId, string orderNo = "", string userDate = "")
        {
            try
            {               
                var note = $"Tích điểm staff point cho đơn hàng {orderNo}";
                string voucherCode = qrCode;
                string employeeCode = "";
                if (typeId == 1)
                {
                    if (string.IsNullOrEmpty(userDate))
                    {
                        userDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    }
                    note = $"Bạn đã sử dụng Voucher {qrCode} vào lúc {userDate} cho đơn hàng {orderNo}";
                }
                else if(typeId == 2)
                {
                    var arrPart = qrCode.Split('-');
                    if (arrPart.Length < 2)
                    {
                        StringHelper.Loggingz(qrCode + " error: " + JsonConvert.SerializeObject(arrPart));
                        return $"Lỗi qrCode không đúng định dạng A_B_C";
                    }

                    note = $"Tích điểm staff point cho đơn hàng {orderNo}";
                    voucherCode = $"WIN-{arrPart[1]}";
                    employeeCode = arrPart.Length > 2 ? arrPart[2] : "";
                }

                var body = new SentNotifyUserWincareRequest()
                {
                    SiteId = storeNo,
                    VoucherCode = voucherCode,
                    EmployeeCode = employeeCode,
                    RequestTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    TypeId = typeId,
                    Note = note
                };
                var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.WIN_APP.ToString()).FirstOrDefault();
                var callApiWincare = await WincareAppHelper.CallApiWincareApp(sysWebApiDto, MethodApiEnum.POST.ToString(), "Notify",
                                                                                JsonConvert.SerializeObject(body), "", ""
                                                                                );
                if (callApiWincare.Item1)
                {
                    return "Success";
                }
                StringHelper.Loggingz("Notify to Wincare: " + JsonConvert.SerializeObject(callApiWincare));
                return callApiWincare.Item3??"Failed";
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("Exception.WinCare.GetInfoCustomerStaff", ex);
                return ex.Message;
            }
        }
        public async Task<(bool, string, string, string)> GetInfoCustomerStaff(RedisManager redisCacheService, string phoneNumber, string posNo)
        {

            try
            {
                if (phoneNumber.Length > 11)
                {
                    var sysWebApiDto = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD())?.Where(x => x.AppCode.ToUpper() == PartnerEnum.WIN_APP.ToString()).FirstOrDefault();
                    var callApiWincare = await WincareAppHelper.CallApiWincareApp(sysWebApiDto, MethodApiEnum.POST.ToString(), "StaffPoints", 
                        JsonConvert.SerializeObject(new StaffPointsWincareRequest()
                        {
                            PosNo = posNo,
                            Barcode = $"WIN-{phoneNumber}",
                            RequestTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        }), "", phoneNumber
                        );

                    _kibanaService.LogResponse(sysWebApiDto.Host + "Public/StaffPointGetInfoBarcode", posNo, 0, "", $"{phoneNumber} " + JsonConvert.SerializeObject(callApiWincare.Item2));
                    if (callApiWincare.Item1 )
                    {
                        var dataStaff = StringHelper.StringToObject<StaffPointGetInfoBarcode>(JsonConvert.SerializeObject(callApiWincare.Item2.Data));
                        if(dataStaff != null && !string.IsNullOrEmpty(dataStaff.CapiId.ToString()))
                        {
                            phoneNumber = FormatHelper.PhoneNumberVietNam(phoneNumber);
                            redisCacheService.StringSet<string>($"{RedisConst.Redis_Key_MASANER_MemberStaff}:{phoneNumber}", phoneNumber, 3600);
                            StringHelper.Loggingz(JsonConvert.SerializeObject(dataStaff));
                            return (true, dataStaff.CapiId.ToString(), phoneNumber, dataStaff.EmployeeCode);
                        }
                    }
                    else if(ResponseCodeSuccess.Contains(callApiWincare.Item2.ResponseCode))
                    {
                        string mess = callApiWincare.Item2.Message;
                        if (string.IsNullOrEmpty(mess))
                        {
                            mess = $"barcode không tồn tại hoặc đã hết hạn sử dụng";
                        }
                        return (false, $"ResponseCode {callApiWincare.Item2.ResponseCode} - {mess}",  JsonConvert.SerializeObject(callApiWincare.Item2), null);
                    }
                    return (false, $"Barcode WIN-{phoneNumber} không tìm thấy thông tin trên app Wincare", JsonConvert.SerializeObject(callApiWincare), null);
                }
                else
                {
                    return (false, $"Giá trị scan từ app WinCare: {phoneNumber} không đúng định dạng", "", null);
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("Exception.WinCare.GetInfoCustomerStaff", ex);
                return (false, "", ex.Message, null);
            }
        }
        public Tuple<int, string, object> GetInfoBarcode(SaleOrderWinPlusBarcode saleOrderWinPlusBarcode)
        {
            try
            {
                if (_sysWebApiDto == null)
                {
                    return new Tuple<int, string, object>(400, MessConst.NotFounDataConfig, null);
                }
                var tokenBasic = EncryptionHelper.Base64Encode(_sysWebApiDto.UserName + ":" + _sysWebApiDto.Password);

                string url = _sysWebApiDto.Host + _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "GetInfoBarcode".ToUpper()).Route ?? "";
                _kibanaService.LogRequest(url, ComputerHelper.GetHostName(), "", JsonConvert.SerializeObject(saleOrderWinPlusBarcode));

                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();
                var api = new ApiHelper(
                                url, 
                                _sysWebApiDto.Authorization, 
                                tokenBasic, "POST", 
                                JsonConvert.SerializeObject(saleOrderWinPlusBarcode), 
                                true, _sysWebApiDto.HttpProxy, _sysWebApiDto.Bypasslist.Split(';'), saleOrderWinPlusBarcode.PosCode
                                );

                string result = "";
                var response = RspDataWinCare(api.InteractWithApi(), ref result);
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                _kibanaService.LogResponse(url, ComputerHelper.GetHostName(), milliseconds, "", JsonConvert.SerializeObject(response));

                if(response != null)
                {
                    if(response.StatusCode == 1)
                    {
                        return new Tuple<int, string, object>(response.StatusCode,
                                                              response.StatusName,
                                                              JsonConvert.DeserializeObject<SaleOrderWinPlusBarcodeData>(JsonConvert.SerializeObject(response.Data)));
                    }
                    else
                    {
                        return new Tuple<int, string, object>(response.StatusCode, response.StatusName, response);
                    }
                }
                else
                {
                    return new Tuple<int, string, object>(400, result, null);
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("Exception WinCare.GetInfoBarcode", ex);
                return new Tuple<int, string, object>(0, ex.Message.ToString(), null);
            }
        }
        public Tuple<int, string, object> ConfirmCollectedCash(ConfirmCollectedCashRequest confirmCollectedCash)
        {
            try
            {
                if (_sysWebApiDto == null)
                {
                    return new Tuple<int, string, object>(400, MessConst.NotFounDataConfig, null);
                }
                var tokenBasic = EncryptionHelper.Base64Encode(_sysWebApiDto.UserName + ":" + _sysWebApiDto.Password);

                string url = _sysWebApiDto.Host + _sysWebApiDto.SysWebApiRoute.FirstOrDefault(x => x.Name.ToUpper() == "ConfirmCollectedCash".ToUpper()).Route ?? "";
                _kibanaService.LogRequest(url, ComputerHelper.GetHostName(), "", JsonConvert.SerializeObject(confirmCollectedCash));

                var bodyJson = new SaleOrderWinPlusBarcode()
                {
                    WincareBarcode = confirmCollectedCash.WincareBarcode,
                    StoreCode = confirmCollectedCash.StoreCode,
                    PosCode = confirmCollectedCash.PosCode,
                    TotalCash = confirmCollectedCash.TotalCash,
                };

                long milliseconds = 0;
                var st1 = new Stopwatch();
                st1.Start();
                var api = new ApiHelper(
                                url,
                                _sysWebApiDto.Authorization,
                                tokenBasic, "POST",
                                JsonConvert.SerializeObject(bodyJson),
                                true, _sysWebApiDto.HttpProxy, _sysWebApiDto.Bypasslist.Split(';'), bodyJson.PosCode
                                );

                string result = "";
                var response = RspDataWinCare(api.InteractWithApi(), ref result);
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                _kibanaService.LogResponse(url, ComputerHelper.GetHostName(), milliseconds, "", JsonConvert.SerializeObject(response));
                if (response != null)
                {
                    if (response.StatusCode == 1)
                    {
                        Task.Run(() => SaveConfirmCollectedCash(confirmCollectedCash));

                        return new Tuple<int, string, object>(response.StatusCode,
                                                              StringHelper.IsNull(response.StatusName, @"Cập nhật thông tin lên WinCare bị lỗi"),
                                                              response);
                    }
                    else
                    {
                        return new Tuple<int, string, object>(response.StatusCode, StringHelper.IsNull(response.StatusName, @"Cập nhật thông tin lên WinCare bị lỗi"), response);
                    }
                }
                else
                {
                    return new Tuple<int, string, object>(400, result, null);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("Exception WinCare.ConfirmCollectedCash", ex);
                return new Tuple<int, string, object>(0, ex.Message.ToString(), null);
            }
        }
        private WinCareResponse RspDataWinCare(string response, ref string result)
        {
            if (!string.IsNullOrEmpty(response))
            {
                return JsonConvert.DeserializeObject<WinCareResponse>(response);
            }
            else
            {
                result = response;
                return null;
            }
        }
        private bool SaveConfirmCollectedCash(ConfirmCollectedCashRequest confirmCollectedCash)
        {
            _dapperDB = new DapperContext(_connectStringCentralMD);
            using (IDbConnection db = _dapperDB.CreateConnDB)
            {
                db.Open();
                try
                {
                    confirmCollectedCash.WincareBarcode = confirmCollectedCash.WincareBarcode.Trim();
                    confirmCollectedCash.BillCode = confirmCollectedCash.BillCode.Trim();

                    string queryIns = @"
                                        INSERT INTO [dbo].[ConfirmCollectedCash]
                                            ([StoreCode],[PosCode],[StaffID],[WincareBarcode],[EmployeeCode],[TotalCash],[CrtDate],BussinessDate, EmployeeName, ShiftNumber, StoreCodeWin, BillCode)
                                        VALUES(@StoreCode,@PosCode,@StaffID,@WincareBarcode,@EmployeeCode,@TotalCash,getdate(),@BussinessDate, @EmployeeName, @ShiftNumber, @StoreCodeWin, @BillCode)";
                    db.Execute(queryIns, confirmCollectedCash, commandType: CommandType.Text);
                    return true;
                }
                catch (Exception ex)
                {
                    FileHelper.WriteExpLogs("Exception SaveConfirmCollectedCash", ex);
                    return false;
                }
            }
        }
    }
}