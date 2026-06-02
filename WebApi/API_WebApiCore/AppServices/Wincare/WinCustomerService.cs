using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty.CX;
using TCX.API.Common.Dtos.TCB;
using TCX.API.Common.Dtos.WinCustomer;
using TCX.API.Common.Dtos.Winpay;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Model.Authen;
using VCM.POSBLUE.Model.Dtos.ROP;

namespace TCX.WebApiCore
{
    public class WinCustomerService
    {
        public readonly LoyaltyRepository _loyaltyRespository;
        private readonly RedisCacheService _redisService;
        readonly List<int> statusWinPayCheck = new List<int> { 11008, 2003, 1001 };
        public WinCustomerService() 
        {
            _loyaltyRespository = new LoyaltyRepository();
            _redisService = new RedisCacheService();
        }
        public string GetConnectStringLoyaltyDb()
        {
            return _loyaltyRespository.ConnectStringLoyaltyDb();
        }
        public Tuple<bool, string, List<SavingAccumulationDto>> SavingAccumulationOffline(IDbConnection db)
        {
            try 
            {
                var lstSavingAccumulation = new List<SavingAccumulationDto>();
                var posSetupData = _redisService.GetPOSDataSetupAll();

                string url = posSetupData.FirstOrDefault(x=>x.Code== "LINKWCAPI").Value;
                string userName = posSetupData.FirstOrDefault(x => x.Code == "WCAPIUSER").Value;
                string passWord = posSetupData.FirstOrDefault(x => x.Code == "WCAPIPASSWORD").Value;
                
                var token = GetTokenWinCustomer(url, userName, passWord);
                if (!token.Item1)
                {
                    return new Tuple<bool, string, List<SavingAccumulationDto>>(false, MessConst.ApiNotLogin, null);
                }

                string full_url = url + "accu/saving-accumulation";

                var request = db.Query<SavingAccumulationValue>("EXEC SP_API_GET_TRANSATION_ACCU_OFFLINE;").ToList();

                if(request == null || request.Count == 0) { return new Tuple<bool, string, List<SavingAccumulationDto>>(false, "NOT DATA", null); }

                foreach (var item in request)
                {
                    var bodyJson = JsonConvert.DeserializeObject<SavingAccumulationDto>(item.Value);
                    if (string.IsNullOrEmpty(bodyJson.TenderType) || string.IsNullOrEmpty(bodyJson.MemberCard))
                    {
                        return new Tuple<bool, string, List<SavingAccumulationDto>>(false, "OK", null);
                    }

                    FileHelper.WriteLogs("SavingAccumulation: " + JsonConvert.SerializeObject(bodyJson));
                    var api = new ApiHelper(full_url, "Bearer", token.Item2, "POST", JsonConvert.SerializeObject(bodyJson), true, "", null, item.OrderNo);
                    var response = WinCustomerHelper.GetResponseWincustomer(api.InteractWithApi());

                    if (response.Status == 200)
                    {
                        bodyJson.Response = JsonConvert.SerializeObject(response);
                        lstSavingAccumulation.Add(bodyJson);
                    }
                    else
                    {
                        FileHelper.WriteLogs("SavingAccumulation Error: " + JsonConvert.SerializeObject(response));
                    }
                }

                bool flag = false;
                if(lstSavingAccumulation.Count > 0)
                {
                    flag = true;
                }
                return new Tuple<bool, string, List<SavingAccumulationDto>>(flag, "OK", lstSavingAccumulation);
            }
            catch (Exception ex) 
            {
                return new Tuple<bool, string, List<SavingAccumulationDto>>(false, JsonConvert.SerializeObject(ex), null);
            }
        }
        public Tuple<bool, string> GetTokenWinCustomer(string url, string userName, string pasWord)
        {
            try
            {
                url += "Token";
                var bodydata = new ROPLogin()
                {
                    ConsumerKey = userName,
                    Password = pasWord
                };

                var api = new ApiHelper(url, "", "", "POST", JsonConvert.SerializeObject(bodydata), false, "", null);
                var response = api.InteractWithApi();

                if (!string.IsNullOrEmpty(response))
                {
                    var dataLogin = JsonConvert.DeserializeObject<ResponseROP>(response);
                    if (dataLogin != null)
                    {
                        var tokenData = JsonConvert.DeserializeObject<TokenData>(JsonConvert.SerializeObject(dataLogin.Data));
                        return new Tuple<bool, string>(true, tokenData.Token.ToString());
                    }
                    else
                    {
                        return new Tuple<bool, string>(false, response);
                    }
                }
                else
                {
                    return new Tuple<bool, string>(false, MessConst.ApiError + "Không lấy được token");
                }
            }
            catch
            {
                return new Tuple<bool, string>(false, MessConst.ApiError + "Không lấy được token");
            }
        }
        public Tuple<bool, string, object> WinPayAccumulation(WinPayAccumulationDto winPayAccumulationDto, List<SysWebApiDto> sysWebApi, List<ConditionAccumulation> conditionAll, string version)
        {
            try
            {
                var condition = CheckConditionStore(conditionAll, winPayAccumulationDto.StoreNo, winPayAccumulationDto.TenderType);
                if (condition == null || condition.Count == 0)
                {
                    return new Tuple<bool, string, object>(false, string.Format(@"Cửa hàng {0} chưa được khai báo tích lũy WinPay", winPayAccumulationDto.StoreNo), null);
                }

                var checkSysWebApi = sysWebApi.FirstOrDefault(x => x.AppCode == "WINCUST");
                if(checkSysWebApi == null) { return new Tuple<bool,string, object>(false, @"Chưa khai báo SysWebApi WINCUST", null); }
                var sysWebApiRoute = checkSysWebApi.SysWebApiRoute.FirstOrDefault(x => x.Name == "Accumulate");
                if (checkSysWebApi == null) { return new Tuple<bool, string, object>(false, @"Chưa khai báo SysWebApiRoute WINCUST", null); }

                var excludeWinpay = StringHelper.ConverStringToArray(sysWebApiRoute.Notes, ';');
                if(excludeWinpay != null && excludeWinpay.Length > 0)
                {
                    foreach( var item in excludeWinpay)
                    {
                        if(FormatHelper.PhoneNumberVietNam(winPayAccumulationDto.PhoneNumber) == FormatHelper.PhoneNumberVietNam(item))
                        {
                            return new Tuple<bool, string, object>(true, $"Số điện thoại {winPayAccumulationDto.PhoneNumber} không được tích Winpay", null);
                        }
                    }
                }

                var dapper = new DapperContext(_loyaltyRespository.ConnectStringLoyaltyDb());
                using (IDbConnection db = dapper.CreateConnDB)
                {
                    db.Open();
                    var dataAccumulation = CalcAccumulation(db, condition, winPayAccumulationDto, version);
                    return AccumulationToWincustomer(db, dataAccumulation.Item2, checkSysWebApi, sysWebApiRoute, dataAccumulation.Item1, version);
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("WinCustomerService.WinPayAccumulation.Exception", ex);
                return new Tuple<bool, string, object>(false,JsonConvert.SerializeObject(ex), null);
            }
        }
        private Tuple<string, WinPayAccumulationData> CalcAccumulation(IDbConnection db, List<ConditionAccumulation> conditionAccumulation, WinPayAccumulationDto winPayAccumulationDto, string version)
        {
            try
            {
                var orderMonth = winPayAccumulationDto.OrderDate.ToString("yyyyMM");
                ConditionAccumulation conditionResult = new ConditionAccumulation();
                ConditionAccumulation conditionResultOverQty = null;
                ConditionAccumulation conditionResultNotOKAmount = null;
               
                foreach (var condition in conditionAccumulation.OrderBy(x => x.Priority))
                {
                    //check conditionName
                    var conditionNameOfDB = StringHelper.ConverStringToArray(condition.Condition, '_');
                    if(conditionNameOfDB.Length > 0 && conditionNameOfDB[0].ToUpper() != winPayAccumulationDto.TenderType.ToUpper())
                    {
                        continue;
                    }
                    
                    //check payment vs totalbill
                    if (condition.IsCheckAmount && winPayAccumulationDto.PaymentAmount < winPayAccumulationDto.TotalAmount)
                    {
                        return new Tuple<string, WinPayAccumulationData>("OK", new WinPayAccumulationData()
                        {
                            PhoneNumber = winPayAccumulationDto.PhoneNumber,
                            OrderNo = winPayAccumulationDto.OrderNo,
                            OperationId = winPayAccumulationDto.OperationId,
                            OrderDate = winPayAccumulationDto.OrderDate,
                            StoreNo = winPayAccumulationDto.StoreNo,
                            PosNo = winPayAccumulationDto.PosNo,
                            RefCode = winPayAccumulationDto.RefCode,
                            TenderType = winPayAccumulationDto.TenderType,
                            PaymentAmount = winPayAccumulationDto.PaymentAmount,
                            TotalAmount = winPayAccumulationDto.TotalAmount,
                            Amount = 0,
                            Condition = "NotConditionPayment",
                            Day = winPayAccumulationDto.OrderDate.DayOfWeek.ToString(),
                            Month = orderMonth,
                            IsSync = false,
                            TraceId = "",
                            Version = version,
                            CrtDate = DateTime.Now,
                        });
                    }

                    //check Amount
                    string conditionName = condition.Condition + "_" + condition.Priority.ToString();
                    if (winPayAccumulationDto.PaymentAmount >= condition.Amount && condition.Amount > 0)
                    {
                        //isCheckItem
                        if (condition.IsMonth && condition.Qty > 0)
                        {
                            string queryDataAccumulation = @"SELECT [PhoneNumber], [Condition], COUNT(1) Qty " +
                                                        " FROM [WinPayAccumulate] (NOLOCK) " +
                                                        " WHERE [Amount] > 0 AND [Month] = '" + orderMonth + "' AND [PhoneNumber] = '" + winPayAccumulationDto.PhoneNumber + "'" +
                                                        " GROUP BY [PhoneNumber], [Condition];";
                            var checkDataAccumulation = db.Query<CountDataAccumulation>(queryDataAccumulation).ToList();
                            if (checkDataAccumulation != null)
                            {
                                var checkQtyAccumulation = checkDataAccumulation.FirstOrDefault(x => x.Condition.ToUpper() == conditionName.ToUpper()) != null ? checkDataAccumulation.FirstOrDefault(x => x.Condition.ToUpper() == conditionName.ToUpper()).Qty : 0;
                                if (checkQtyAccumulation < condition.Qty)
                                {
                                    //check day
                                    if (condition.IsDay && !string.IsNullOrEmpty(condition.DayOfWeek) && condition.AccumulationValue == 0)
                                    {
                                        var discountOfDay = ConvertHelper.ToObject<List<DayOfWeekData>>(condition.DayOfWeek);
                                        if (discountOfDay != null)
                                        {
                                            var valueOfDay = discountOfDay.FirstOrDefault(x => x.Name == winPayAccumulationDto.OrderDate.DayOfWeek.ToString());
                                            if (valueOfDay != null)
                                            {
                                                condition.AccumulationValue = valueOfDay.Value;
                                            }
                                            else
                                            {
                                                condition.AccumulationValue = 0;
                                            }
                                            conditionResult = condition;
                                            break;
                                        }
                                    }

                                    conditionResult = condition;
                                    break;
                                }
                                else
                                {
                                    condition.AccumulationValue = 0;
                                    conditionResultOverQty = condition;
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            if(condition.IsCheckItem && winPayAccumulationDto.IsCheckItem)
                            {
                                conditionResult = condition;
                                break;
                            }
                            else if (condition.IsCheckItem && !winPayAccumulationDto.IsCheckItem)
                            {
                                condition.AccumulationValue = 0;
                                conditionResult = condition;
                                continue;
                            }
                            else
                            {
                                conditionResult = condition;
                                break;
                            }
                        }
                        conditionResult = condition;
                        break ;
                    }
                    else
                    {
                        //check not condition amount
                        if (condition.Amount == 0 && condition.Qty > 0)
                        {
                            conditionResult = condition;
                            break;
                        }

                        //not AccumulationValue
                        condition.AccumulationValue = 0;
                        conditionResultNotOKAmount = condition;
                    }
                }
                
                //result
                if(conditionResult != null && !string.IsNullOrEmpty(conditionResult.Condition))
                {
                    int amountAccumulation = 0;
                    if(conditionResult.AccumulationType == "percentage")
                    {
                        amountAccumulation = (int)Math.Round(conditionResult.AccumulationValue * winPayAccumulationDto.PaymentAmount / 100, 0);
                    }
                    else
                    {
                        amountAccumulation = (int)conditionResult.AccumulationValue;
                    }

                    return new Tuple<string, WinPayAccumulationData>("OK", new WinPayAccumulationData()
                    {
                        PhoneNumber = winPayAccumulationDto.PhoneNumber,
                        OrderNo = winPayAccumulationDto.OrderNo,
                        OperationId = winPayAccumulationDto.OperationId,
                        OrderDate = winPayAccumulationDto.OrderDate,
                        StoreNo = winPayAccumulationDto.StoreNo,
                        PosNo = winPayAccumulationDto.PosNo,
                        RefCode = winPayAccumulationDto.RefCode,
                        TenderType = winPayAccumulationDto.TenderType,
                        PaymentAmount = winPayAccumulationDto.PaymentAmount,
                        TotalAmount = winPayAccumulationDto.TotalAmount,
                        Amount = amountAccumulation,
                        Condition = conditionResult.Condition + "_" + conditionResult.Priority.ToString(),
                        Day = winPayAccumulationDto.OrderDate.DayOfWeek.ToString(),
                        Month = orderMonth,
                        IsSync = false,
                        TraceId = "",
                        CrtDate = DateTime.Now,
                    });
                }
                else
                {
                    //over qty/condition
                    if(conditionResultOverQty != null || conditionResultNotOKAmount != null )
                    {
                        return new Tuple<string, WinPayAccumulationData>("OK", new WinPayAccumulationData()
                        {
                            PhoneNumber = winPayAccumulationDto.PhoneNumber,
                            OrderNo = winPayAccumulationDto.OrderNo,
                            OperationId = winPayAccumulationDto.OperationId,
                            OrderDate = winPayAccumulationDto.OrderDate,
                            StoreNo = winPayAccumulationDto.StoreNo,
                            PosNo = winPayAccumulationDto.PosNo,
                            RefCode = winPayAccumulationDto.RefCode,
                            TenderType = winPayAccumulationDto.TenderType,
                            PaymentAmount = winPayAccumulationDto.PaymentAmount,
                            TotalAmount = winPayAccumulationDto.TotalAmount,
                            Amount = 0,
                            Condition = "NotCondition",
                            Day = winPayAccumulationDto.OrderDate.DayOfWeek.ToString(),
                            Month = orderMonth,
                            IsSync = false,
                            TraceId = "",
                            CrtDate = DateTime.Now,
                        });
                    }

                    return new Tuple<string, WinPayAccumulationData>(winPayAccumulationDto.PhoneNumber + " không đạt điều kiện được tích lũy", null);
                }
            }
            catch( Exception ex ) 
            {
                FileHelper.WriteExpLogs("WinCustomerService.CalcAccumulation", ex);
                return new Tuple<string, WinPayAccumulationData>(JsonConvert.SerializeObject(ex), null); ;
            }
        }    
        public void RetryWinpayAccumulationError(IDbConnection db, List<SysWebApiDto> sysWebApi)
        {
            var checkSysWebApi = sysWebApi.FirstOrDefault(x => x.AppCode == "WINCUST");
            var sysWebApiRoute = checkSysWebApi.SysWebApiRoute.FirstOrDefault(x => x.Name == "Accumulate");
            var dataRetryDB = RetryWinpayAccumulation(db);
            if (dataRetryDB.Count > 0)
            {
                FileHelper.WriteLogs($"Thực hiện retry tích lũy WINPAY với {dataRetryDB.Count} giao dịch");
                foreach (var item in dataRetryDB)
                {
                    AccumulationToWincustomer(db, item, checkSysWebApi, sysWebApiRoute, @"Thực hiện retry tích lũy WINPAY", "V3", true);
                    Thread.Sleep(1000);
                }
            }
        }
        private List<WinPayAccumulationData> RetryWinpayAccumulation(IDbConnection db)
        {
            try
            {
                List<WinPayAccumulationData> result = new List<WinPayAccumulationData>();
                var _redisHelper = new RedisConnectionHelper();
                var lstKey = _redisHelper.GetKeys("WinPayAccumulation");
                List<string> lstRefCode = new List<string>();
                if (lstKey.Count > 0)
                {
                    foreach (var key in lstKey)
                    {
                        lstRefCode.Add(key);
                        result.Add(_redisHelper.Get<WinPayAccumulationData>(key));
                    }
                }

                string query = @"SELECT * FROM [WinPayAccumulate] (NOLOCK) WHERE CAST(CrtDate AS DATE) >= CAST(getdate() - 15 AS DATE) AND IsSync = 0;";
                var dataRetry = db.Query<WinPayAccumulationData>(query).ToList();
                if (dataRetry != null && dataRetry.Count > 0)
                {
                    foreach (var item in dataRetry)
                    {
                        if (lstKey.Contains(item.RefCode))
                        {
                            continue;
                        }
                        var checkRsp = WinCustomerHelper.GetResponseWincustomer(item.Response);
                        if (checkRsp != null && statusWinPayCheck.Contains(checkRsp.Status))
                        {
                            item.IsSync = true;
                            db.Execute(@"UPDATE [WinPayAccumulate] SET CrtDate = getdate(), [IsSync] = 1 WHERE PhoneNumber = @PhoneNumber AND OrderNo = @OrderNo;", item);
                        }
                        else
                        {
                            result.Add(item);
                        }
                    }
                }
                return result;
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("RetryWinpayAccumulation", ex);
                return null;
            }
        }
        private Tuple<bool, string, object> AccumulationToWincustomer(IDbConnection db, WinPayAccumulationData dataAccumulation, SysWebApiDto checkSysWebApi, SysWebApiRoute sysWebApiRoute, string messError,string version = "V2", bool IsRetry = false)
        {
            if (dataAccumulation != null)
            {
                var bodyJson = new WinPayAccumulationRequest()
                {
                    StoreNo = dataAccumulation.StoreNo,
                    PosNo = dataAccumulation.PosNo,
                    RefCode = dataAccumulation.RefCode,
                    PhoneNumber = dataAccumulation.PhoneNumber,
                    Amount = dataAccumulation.Amount,
                    OperationId = dataAccumulation.OperationId
                };

                if(version.ToUpper() == "V2")
                {
                    var token = GetTokenWinCustomer(checkSysWebApi.Host, checkSysWebApi.UserName, checkSysWebApi.Password);
                    if (!token.Item1)
                    {
                        return new Tuple<bool, string, object>(false, MessConst.ApiNotLogin, null);
                    }

                    string url = checkSysWebApi.Host + sysWebApiRoute.Route;

                    var api = new ApiHelper(url, "Bearer", token.Item2, "POST", JsonConvert.SerializeObject(bodyJson), true, "", null, bodyJson.RefCode);
                    var response = WinCustomerHelper.GetResponseWincustomer(api.InteractWithApi());
                    
                    if (response.Status == 200)
                    {
                        dataAccumulation.Version = "V2";
                        dataAccumulation.IsSync = true;
                        dataAccumulation.TraceId = response.Data.TraceId ?? "";
                        var _redisHelper = new RedisConnectionHelper();
                        if (_redisHelper.CheckExistsKey("WinPayAccumulation:" + dataAccumulation.RefCode))
                        {
                            FileHelper.WriteLogs("Delete key redis " + dataAccumulation.RefCode.ToString());
                            _redisHelper.DeleteWithParent(dataAccumulation.RefCode, "WinPayAccumulation");
                        }
                    }
                    else
                    {
                        FileHelper.WriteLogs(string.Format("ERROR WinPayAccumulation {0}:{1} ", dataAccumulation.OrderNo, JsonConvert.SerializeObject(response)));
                        if (statusWinPayCheck.Contains(response.Status))
                        {
                            _loyaltyRespository.InsertWinPayAccumulate(db, dataAccumulation, true);
                            return new Tuple<bool, string, object>(dataAccumulation.IsSync, response.Message, response);
                        }
                    }

                    dataAccumulation.Response = JsonConvert.SerializeObject(response);
                    _loyaltyRespository.InsertWinPayAccumulate(db, dataAccumulation, IsRetry);
                    return new Tuple<bool, string, object>(dataAccumulation.IsSync, response.Message, response);
                }
                else if (version.ToUpper() == "V3")
                {
                    WinpayService _winpayService = new WinpayService();
                    var dataBody = new CashbackWinpayPOS()
                    {
                        PosCode = dataAccumulation.PosNo,
                        StoreCode = dataAccumulation.StoreNo,
                        OrderNo = dataAccumulation.OrderNo,
                        Amount = dataAccumulation.Amount,
                        PhoneNumber = dataAccumulation.PhoneNumber,
                        TransactionId = dataAccumulation.RefCode,
                    };

                    var result = _winpayService.CashbackWinpay(dataBody);
                    dataAccumulation.Version = "V3";
                    if (result.StatusCode == 200)
                    {
                        var dataResponse = StringHelper.StringToObject<DataResponseWinpayPOS>(JsonConvert.SerializeObject(result.Data));
                        dataAccumulation.IsSync = true;
                        dataAccumulation.TraceId = dataResponse != null ? (dataResponse.Trace ?? dataResponse.RequestId) : "";
                        var _redisHelper = new RedisConnectionHelper();
                        if (_redisHelper.CheckExistsKey("WinPayAccumulation:" + dataAccumulation.OrderNo))
                        {
                            _redisHelper.DeleteWithParent(dataAccumulation.RefCode, "WinPayAccumulation");
                            FileHelper.WriteLogs("AccumulationToWincustomer Delete key redis " + dataAccumulation.RefCode.ToString());
                        }
                    }
                    else
                    {
                        FileHelper.WriteLogs(string.Format("ERROR WinPayAccumulation {0}:{1} ", dataAccumulation.OrderNo, JsonConvert.SerializeObject(result)));
                        if (statusWinPayCheck.Contains(result.StatusCode))
                        {
                            dataAccumulation.IsSync = true;
                            dataAccumulation.TraceId = "OK";
                            dataAccumulation.Response = JsonConvert.SerializeObject(result);
                            _loyaltyRespository.InsertWinPayAccumulate(db, dataAccumulation, true);
                            return new Tuple<bool, string, object>(dataAccumulation.IsSync, result.StatusName, result);
                        }
                    }
                    dataAccumulation.Response = JsonConvert.SerializeObject(result);
                    _loyaltyRespository.InsertWinPayAccumulate(db, dataAccumulation, IsRetry);
                    return new Tuple<bool, string, object>(dataAccumulation.IsSync, result.MessageTechnical, result);
                }
                else
                {
                    _loyaltyRespository.InsertWinPayAccumulate(db, dataAccumulation);
                    return new Tuple<bool, string, object>(false, @"Dữ liệu đã được tính toán nhưng chưa gửi sang tích lũy WINPAY", dataAccumulation);
                }
            }
            else
            {
                return new Tuple<bool, string, object>(false, string.Format(@"Số lần tích lũy Winpay trong tháng đã hết"), null);
            }
        }
        private List<ConditionAccumulation> CheckConditionStore(List<ConditionAccumulation> conditionAccumulations, string storeNo, string tenderType)
        {
            try
            {
                List<ConditionAccumulation> condition = new List<ConditionAccumulation>();
                foreach (var conditionAccumulation in conditionAccumulations)
                {
                    if (conditionAccumulation.ConditionStore.Select(x => x.StoreNo).ToArray().Contains(storeNo) && conditionAccumulation.Condition.Contains(tenderType))
                    {
                        condition.Add(conditionAccumulation);
                    }
                }

                return condition;
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("CheckConditionStore.Exception", ex);
                return null;
            }
        }

    }
}