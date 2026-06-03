using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Net;
using System.Text;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Winpay;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.API.Common.Utils;
using TCX.API.Common.Enums;
using System.Threading.Tasks;

namespace TCX.WebApiCore
{
    public class WinpayService
    {
        private readonly string _connectStringDb;
        private readonly KibanaService _kibanaService;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly string subjectSMS = "Dịch vụ kết nối WebApi WINPAY";
        public WinpayService()
        {
            _kibanaService = new KibanaService();
            _memoryCacheService = new MemoryCacheService();
            _connectStringDb = ConfigurationManager.ConnectionStrings["DAPPER_CENTRAL_MD"].ConnectionString;
        }
        private (bool, string) CallApiWinpay(string phoneNumber, object rawDataWinpay, string action, string posNo, ref string privateKey, string version = "v1", string request_id = "", int timeOut = 0)
        {
            long milliseconds = 0;
            var st1 = new Stopwatch();
            st1.Start();
            var sysWebApi = _memoryCacheService.GetSysWebApi(_connectStringDb).FirstOrDefault(x => x.AppCode == PartnerEnum.WINPAY.ToString());
            if (sysWebApi == null)
            {
                return (false, @"Chưa cấu hình WebApi cho Winpay");
            }
            string url = sysWebApi.Host + version.ToString();
            try
            {
                privateKey = sysWebApi.PrivateKey;
                if (string.IsNullOrEmpty(request_id))
                {
                    request_id = posNo + DateTimeHelper.UnixTimestampString() + DateTime.Now.ToString("mmssfff");
                }
                string merchant_id = sysWebApi.UserName;
                string tokenBasic = string.Empty;

                object requestWinpay = null;
                if (version.ToString().ToUpper() == "V1")
                {
                    EncryptWinpayUtil encryptWinpayUtil = new EncryptWinpayUtil();
                    var iv = RSAHelper.GenerateIV();
                    var base64_iv = Convert.ToBase64String(iv);
                    var cinpher_data = AESUtils.EncryptWithIV(StringHelper.ObjectToStringLowercase(rawDataWinpay), sysWebApi.PrivateKey, iv);
                    //hmac_sha256("merchant_id="+merchant_id+"&request_id="request_id+"&cipher_data="+cinpher_data+"&base64_iv="+base64(iv) , secretkey
                    var queryData = "merchant_id=" + merchant_id + "&request_id=" + request_id + "&cipher_data=" + cinpher_data + "&base64_iv=" + base64_iv;
                    var signData = EncryptionHelper.ComputeHMACSHA256(queryData, privateKey);
                    requestWinpay = new RequestWinpay()
                    {
                        Merchant_id = merchant_id,
                        Request_id = request_id,
                        Base64_iv = base64_iv,
                        Cipher_data = cinpher_data,
                        Signature = signData
                    };
                } 
                else if (version.ToString().ToUpper() == "V2")
                {
                    tokenBasic = "Basic " + EncryptionHelper.Base64Encode(sysWebApi.UserName + ":" + sysWebApi.PrivateKey);
                    requestWinpay = new UncodeRequestWinpay()
                    {
                        Merchant_id = merchant_id,
                        Request_id = request_id,
                        Data = rawDataWinpay
                    };
                }

                if(requestWinpay == null)
                {
                    return (false, $"Lỗi tạo dữ liệu gửi Winpay");
                }

                _kibanaService.LogRequest(url, action, "",$"{phoneNumber}" + StringHelper.ObjectToStringLowercase(requestWinpay));
                var client = HttpClientProvider.GetClientV2(sysWebApi.HttpProxy, int.Parse(sysWebApi.Authorization), url);
                if (!string.IsNullOrEmpty(tokenBasic))
                {
                    client.DefaultRequestHeaders.Add("Authorization", tokenBasic);
                }
                var response = client.PostAsync(url, new StringContent(StringHelper.ObjectToStringLowercase(requestWinpay), Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                _kibanaService.LogResponse(url, action, milliseconds, "", $"{phoneNumber} with request_id:" + request_id +"|action: " + action + "|" + response.StatusCode.ToString() + "|body: " + response.Content.ReadAsStringAsync().Result);
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    var readData = response.Content.ReadAsStringAsync();
                    return (true, readData.Result);
                }
                else
                {
                    return (false, response.StatusCode.ToString());
                }
            }
            catch (TaskCanceledException ex)
            {
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                var timeoutResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
                {
                    Content = new StringContent("Request timed out.")
                };
                _kibanaService.LogException(sysWebApi.Host, posNo, milliseconds, "", $"{phoneNumber} " + JsonConvert.SerializeObject(ex));
                HttpClientService.SendMessageSMS($"WebApi.Winpay.TaskCanceledException: {url} - action: **{action}** vs {phoneNumber} 408_RequestTimeout: {milliseconds} milliseconds. Message: {ex.Message}","Warning", subjectSMS, appCode: AppCodeMessageEnum.BLUEPOS_WINPAY_TIMEOUT.ToString());
                return (false, $"408_RequestTimeout {milliseconds} milliseconds. Message: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                _kibanaService.LogException(sysWebApi.Host, posNo, milliseconds, "", $"{phoneNumber} " + JsonConvert.SerializeObject(ex));
                HttpClientService.SendMessageSMS($"WebApi.Winpay.HttpRequestException: **{action}** vs {phoneNumber}  Message: {ex.Message} url: {url}", "Warning", subjectSMS, appCode: AppCodeMessageEnum.BLUEPOS_WINPAY_TIMEOUT.ToString());
                return (false, ex.Message);
            }
            catch (AggregateException ex)
            {
                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                _kibanaService.LogException(sysWebApi.Host, posNo, milliseconds, "", $"{phoneNumber} " + JsonConvert.SerializeObject(ex));
                HttpClientService.SendMessageSMS($"WebApi.Winpay.AggregateException: **{action}** vs {phoneNumber}_Message {ex.Message} url: {url}", "Warning", subjectSMS, appCode: AppCodeMessageEnum.BLUEPOS_WINPAY_TIMEOUT.ToString());
                return (false, ex.Message);
            }
        }
        private NewHttpResponseDto ConvertResponseWinpay(string response, string posNo, PaymentWinpayResponse rspPayment = null)
        {
            var result = StringHelper.StringToObject<ResponseWinpay>(response);
            if (result == null)
            {
                var respWarning = new NewHttpResponseDto()
                {
                    StatusCode = (int)EStatusResponse.ServiceUnavailable,
                    MessageTechnical = response,
                    StatusName = $"Lỗi kết nối tới WebApi Winpay, vui lòng liên hệ IT",
                    Data = new { TechnicalMessage = response }
                };
                _kibanaService.LogException("ResponseWinpay", posNo, 0, "", JsonConvert.SerializeObject(respWarning));
                return respWarning;
            }
            var statusData = GetStatusWPay(result.Code, result.Message);
            return new NewHttpResponseDto()
            {
                StatusCode = result.Code == 0 ? 200 : statusData.Item1,
                StatusName = statusData.Item2,
                MessageTechnical = result.Message,
                Data = result.Code == 0 ? new { Trace = rspPayment?.Id.ToString(), RequestId = result.Request_id } : null,
            };
        }
        public NewHttpResponseDto RegisterWinpay(RegisterWinpayPOS request)
        {
            DataRegisterWinpay registerData = new DataRegisterWinpay()
            {
                Action_type = ActionWinpayEnum.create_account.ToString(),
                Phone_number = FormatHelper.PhoneNumberVietNam(request.PhoneNumber),
                Code = request.Otp, //OTP
                IdCard = request.IDCard,
                Name = request.Name,
                FPEnrollments = request.FPEnrollments,
                Store_id =request.StoreCode,
                Pos_id = request.PosCode,
                Order_id = request.PosCode + DateTimeHelper.UnixTimestampString()
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(registerData.Phone_number, registerData, registerData.Action_type, registerData.Pos_id, ref privateKey, "v2", request.RequestId);
                if (callWinpay.Item1)
                {
                    var result = StringHelper.StringToObject<ResponseWinpay>(callWinpay.Item2);
                    if(result != null)
                    {
                        if(result.Code == 0)
                        {
                            return ConvertResponseWinpay(callWinpay.Item2, request.OperationId);
                        }
                        else if(result.Code == 1001 || result.Code == 1005)
                        {
                            return new NewHttpResponseDto()
                            {
                                StatusCode = 409,
                                StatusName = result.Message,
                                MessageTechnical = callWinpay.Item2,
                                Data = null,
                            };
                        }
                        else
                        {
                            return new NewHttpResponseDto()
                            {
                                StatusCode = 400,
                                StatusName = $"Mã lỗi {result.Code} - {result.Message}",
                                MessageTechnical = callWinpay.Item2,
                                Data = null,
                            };
                        }
                    }

                    return ConvertResponseWinpay(callWinpay.Item2, request.PosCode);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = null,
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("RegisterWinpay", request.OperationId, 0, "", JsonConvert.SerializeObject(registerData));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = "Lỗi ngoại lệ xử lý với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        public NewHttpResponseDto GetRegisterInfo(string posNo, string phoneNumber)
        {
            GetBalanceWinpay rawData = new GetBalanceWinpay()
            {
                Action_type = ActionWinpayEnum.balance.ToString(),
                Phone_number = FormatHelper.PhoneNumberVietNam(phoneNumber),
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(phoneNumber, rawData, rawData.Action_type, posNo, ref privateKey, timeOut:15);
                if (callWinpay.Item1)
                {
                    var result = StringHelper.StringToObject<ResponseWinpay>(callWinpay.Item2);
                    if(result != null && result.Code == 0)
                    {                       
                        string decryptData = AESUtils.DecryptWithIV(result.Cipher_data, privateKey, Convert.FromBase64String(result.Base64_iv));
                        var custInfo = StringHelper.StringToObject<CustomerInfoWinpay>(decryptData);
                        if(custInfo != null && custInfo.Account != null)
                        {
                            if(custInfo.Account.Status == 2)
                            {
                                return new NewHttpResponseDto()
                                {
                                    StatusCode = 404,
                                    StatusName = "Tài khoản đã bị xóa",
                                    MessageTechnical = $"Status = {custInfo.Account.Status}; {result.Message}",
                                    Data = new { custInfo.Account },
                                };
                            }
                            else if (custInfo.Account.Status == 1)
                            {
                                return new NewHttpResponseDto()
                                {
                                    StatusCode = 400,
                                    StatusName = "Tài khoản đã bị khóa",
                                    MessageTechnical = $"Status = {custInfo.Account.Status}; {result.Message}",
                                    Data = new { custInfo.Account },
                                };
                            }

                            WalletRule walletRule = new WalletRule();
                            if(custInfo.Balance != null && custInfo.Balance.WalletRule != null)
                            {
                                walletRule.DailyCashout = custInfo.Balance.WalletRule.DailyCashout;
                                walletRule.WeeklyCashout = custInfo.Balance.WalletRule.WeeklyCashout;
                                walletRule.MonthlyCashout = custInfo.Balance.WalletRule.MonthlyCashout;
                            }
                            var resultData = new GetInfoWinpayPOS()
                            {
                                PhoneNumber = custInfo.Account != null ?  custInfo.Account.MobilePhone : phoneNumber,
                                Name = custInfo.Account.RealName,
                                IDCard ="",
                                Balance = custInfo.Balance != null ? custInfo.Balance.Balance : 0,
                                DateOfRegistration = DateTime.Now,
                                FingerprintEnrolled = custInfo.FingerprintEnrolled,
                                TotalCashBack = 0,
                                WithdrawQuotaInfo = new WithdrawQuotaInfo()
                                {
                                    QuotaPerDay = walletRule.DailyCashout,
                                    TotalPerDay = custInfo.Balance != null ? (decimal)custInfo.Balance.DailyRemainingCashout : 0,
                                    QuotaPerWeek = walletRule.WeeklyCashout,
                                    TotalPerWeek = custInfo.Balance != null ? (decimal) custInfo.Balance.WeeklyRemainingCashout : 0,
                                    QuotaPerMonth = walletRule.MonthlyCashout,
                                    TotalPerMonth = custInfo.Balance != null ? (decimal)custInfo.Balance.MonthlyRemainingCashout : 0,
                                },
                            };
                            return new NewHttpResponseDto()
                            {
                                StatusCode = 200,
                                StatusName = result.Message,
                                MessageTechnical = "Success",
                                Data = resultData,
                            };
                        }
                    }
                    else if (result != null && (result.Code == 1001 || result.Code == 1011)) 
                    {
                        return new NewHttpResponseDto()
                        {
                            StatusCode = 404,
                            StatusName = result.Message,
                            MessageTechnical = callWinpay.Item2,
                            Data = new { callWinpay.Item2 },
                        };
                    }
                    return ConvertResponseWinpay(callWinpay.Item2, posNo);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = new { callWinpay.Item2 },
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("GetRegisterInfo", posNo, 0, "", JsonConvert.SerializeObject(rawData));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = "Lỗi ngoại lệ xử lý với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        //payment, deposit, withdraw
        public NewHttpResponseDto PaymentWinpay(PaymentWinpayPOS paymentWinpay, string actionType, string fund_source = "wcm", string walletType = "winpay")
        {
            DataRequestWinpay cashInWinpay = new DataRequestWinpay()
            {
                Fund_source = fund_source,
                Wallet_type = walletType,
                Action_type = actionType,
                Phone_number = paymentWinpay.PhoneNumber,
                Store_id = paymentWinpay.StoreCode,
                Pos_id = paymentWinpay.PosCode,
                Amount = paymentWinpay.Amount.ToString(),
                Order_id = paymentWinpay.InvoiceNum,
                Fingerprint = paymentWinpay.Fingerprint,
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(cashInWinpay.Phone_number, cashInWinpay, cashInWinpay.Action_type, paymentWinpay.PosCode, ref privateKey, "v1", paymentWinpay.RequestId);               
                if (callWinpay.Item1)
                {
                    var result = StringHelper.StringToObject<ResponseWinpay>(callWinpay.Item2);
                    Task.Run(() => _kibanaService.LogResponse(actionType, paymentWinpay.PosCode, 0, "", $"{actionType}: {paymentWinpay.InvoiceNum} payment success {JsonConvert.SerializeObject(result)}"));
                    if (result != null && result.Code == 0)
                    {
                        string decryptData = AESUtils.DecryptWithIV(result.Cipher_data, privateKey, Convert.FromBase64String(result.Base64_iv));
                        if(!string.IsNullOrEmpty(decryptData))
                        {
                            var paymentData = StringHelper.StringToObject<PaymentWinpayResponse>(decryptData);
                            return ConvertResponseWinpay(callWinpay.Item2, paymentWinpay.PosCode, paymentData);
                        }
                    }
                    return ConvertResponseWinpay(callWinpay.Item2, paymentWinpay.PosCode);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = new { callWinpay.Item2 },
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("Winpay." + cashInWinpay.Action_type, paymentWinpay.PosCode, 0, "", JsonConvert.SerializeObject(cashInWinpay));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = $"Lỗi ngoại lệ xử lý giao dịch {paymentWinpay.InvoiceNum} với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        public NewHttpResponseDto FingerUpdate(UpdateFingerWinpayPOS request)
        {
            UpdateFingerWinpay requestWinpay = new UpdateFingerWinpay()
            {
                Action_type = ActionWinpayEnum.change_fingerprint.ToString(),
                Phone_number = request.PhoneNumber,
                Code = request.Otp,
                FPEnrollments = request.FPEnrollments,
                Store_id = request.StoreCode,
                Pos_id = request.PosCode,
                Cashier_id = request.OperationId,
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(requestWinpay.Phone_number, requestWinpay, requestWinpay.Action_type, request.PosCode, ref privateKey,"v1", request.RequestId);
                if (callWinpay.Item1)
                {
                    return ConvertResponseWinpay(callWinpay.Item2, request.PosCode);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = new { callWinpay.Item2 },
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("Winpay." + requestWinpay.Action_type, request.PosCode, 0, "", JsonConvert.SerializeObject(requestWinpay));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = "Lỗi ngoại lệ xử lý giao dịch với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        public NewHttpResponseDto RefundWinpay(RefundWinpayPOS request)
        {
            RefundWinpay requestWinpay = new RefundWinpay()
            {
                Action_type = ActionWinpayEnum.refund.ToString(),
                Phone_number = request.PhoneNumber,
                Store_id = request.StoreCode,
                Pos_id = request.PosCode,
                Amount = request.Amount.ToString(),
                Order_id = request.InvoiceNum,
                Trans_id = request.TransactionId,
                Cashier_id=request.OperationId,
                Payment_order_id = request.Description
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(requestWinpay.Phone_number, requestWinpay, requestWinpay.Action_type, request.PosCode, ref privateKey, "v1", request.RequestId);
                if (callWinpay.Item1)
                {
                    var result = StringHelper.StringToObject<ResponseWinpay>(callWinpay.Item2);
                    if (result != null && result.Code == 0)
                    {
                        string decryptData = AESUtils.DecryptWithIV(result.Cipher_data, privateKey, Convert.FromBase64String(result.Base64_iv));
                        if (!string.IsNullOrEmpty(decryptData))
                        {
                            var paymentData = StringHelper.StringToObject<PaymentWinpayResponse>(decryptData);
                            //FileHelper.WriteLogs($"{requestWinpay.Action_type} response: {JsonConvert.SerializeObject(paymentData)}");

                            return ConvertResponseWinpay(callWinpay.Item2, requestWinpay.Pos_id, paymentData);
                        }

                    }

                    return ConvertResponseWinpay(callWinpay.Item2, request.PosCode);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = new { callWinpay.Item2 },
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("Winpay." + requestWinpay.Action_type, request.PosCode, 0, "", JsonConvert.SerializeObject(requestWinpay));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = "Lỗi ngoại lệ xử lý giao dịch với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        public NewHttpResponseDto VerifyFingerWinpay(VerifyFingerWinpayPOS request)
        {
            VerifyFingerWinpay requestWinpay = new VerifyFingerWinpay()
            {
                Action_type = ActionWinpayEnum.auth_fingerprint.ToString(),
                Phone_number = request.PhoneNumber,
                Store_id = request.StoreCode,
                Pos_id = request.PosCode,
                Cashier_id = request.OperationId,
                Fingerprint = request.Fingerprint,
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(requestWinpay.Phone_number, requestWinpay, requestWinpay.Action_type, request.PosCode, ref privateKey);
                if (callWinpay.Item1)
                {
                    var result = StringHelper.StringToObject<ResponseWinpay>(callWinpay.Item2);
                    if(result.Code == 0)
                    {
                        string decryptData = AESUtils.DecryptWithIV(result.Cipher_data, privateKey, Convert.FromBase64String(result.Base64_iv));
                    }

                    return ConvertResponseWinpay(callWinpay.Item2, request.PosCode);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = new { callWinpay.Item2 },
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("Winpay." + requestWinpay.Action_type, request.PosCode, 0, "", JsonConvert.SerializeObject(requestWinpay));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = "Lỗi ngoại lệ xử lý giao dịch với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        public NewHttpResponseDto UnRegisterWinpay(UnregisterWinpayPOS request)
        {
            CloseAccountWinpay requestWinpay = new CloseAccountWinpay()
            {
                Action_type = ActionWinpayEnum.close_account.ToString(),
                Phone_number = request.PhoneNumber,
                Store_id = request.StoreCode,
                Pos_id = request.PosCode,
                Fingerprint = request.Fingerprint,
                Cashier_id = request.OperationId,
                Code = request.Otp
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(requestWinpay.Phone_number, requestWinpay, requestWinpay.Action_type, request.PosCode, ref privateKey, "v1", request.RequestId);
                if (callWinpay.Item1)
                {
                    return ConvertResponseWinpay(callWinpay.Item2, request.PosCode);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = new { callWinpay.Item2 },
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("Winpay." + requestWinpay.Action_type, request.PosCode, 0, "", JsonConvert.SerializeObject(requestWinpay));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = "Lỗi ngoại lệ xử lý giao dịch với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        public NewHttpResponseDto CashbackWinpay(CashbackWinpayPOS request)
        {
            CashbackWinpay requestWinpay = new CashbackWinpay()
            {
                Action_type = ActionWinpayEnum.cashback.ToString(),
                Phone_number = request.PhoneNumber,
                Store_id = request.StoreCode,
                Pos_id = request.PosCode,
                Amount = request.Amount.ToString(),
                Trans_id = request.TransactionId,
                Order_id = request.OrderNo
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(requestWinpay.Phone_number, requestWinpay, requestWinpay.Action_type, request.PosCode, ref privateKey);
                if (callWinpay.Item1)
                {
                    return ConvertResponseWinpay(callWinpay.Item2, request.PosCode);
                }
                else
                {
                    return new NewHttpResponseDto()
                    {
                        StatusCode = 400,
                        StatusName = callWinpay.Item2,
                        MessageTechnical = callWinpay.Item2,
                        Data = new { callWinpay.Item2 },
                    };
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("Winpay." + requestWinpay.Action_type, request.PosCode, 0, "", JsonConvert.SerializeObject(requestWinpay));
                return new NewHttpResponseDto()
                {
                    StatusCode = 400,
                    StatusName = "Lỗi ngoại lệ xử lý giao dịch với Winpay",
                    MessageTechnical = JsonConvert.SerializeObject(ex),
                    Data = null,
                };
            }
        }
        public (bool, string) SendOtpWinpay(string posNo, string phoneNumber, string action_send_otp)
        {

            CloseAccountWinpay requestWinpay = new CloseAccountWinpay()
            {
                Action_type = action_send_otp,
                Phone_number = phoneNumber,
                Store_id = StringHelper.Left(posNo, 4),
                Pos_id = posNo
            };

            try
            {
                string privateKey = "";
                var callWinpay = CallApiWinpay(phoneNumber, requestWinpay, requestWinpay.Action_type, posNo, ref privateKey);
                if (callWinpay.Item1)
                {
                    var result = StringHelper.StringToObject<ResponseWinpay>(callWinpay.Item2);
                    if (result != null && result.Code == 0)
                    {
                        string decryptData = AESUtils.DecryptWithIV(result.Cipher_data, privateKey, Convert.FromBase64String(result.Base64_iv));
                        var dataOtp = StringHelper.StringToObject<DataOtpWinpay>(decryptData);
                        return (true, decryptData);
                        //if (dataOtp != null && !string.IsNullOrEmpty(dataOtp.Otp))
                        //{
                        //    return (true, dataOtp.Otp);
                        //}
                        //return (false, $"Mã code {result.Code}: {result.Message}");
                    }
                    else if(result != null)
                    {
                        return (false, $"Mã code {result.Code}: {result.Message}");
                    }
                    else
                    {
                        return (false, $"Lỗi convert dữ liệu winpay");
                    }
                }
                else
                {
                    return (false, $"Lỗi kết nối Winpay");
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("Winpay." + requestWinpay.Action_type, posNo, 0, "", JsonConvert.SerializeObject(requestWinpay));
                return (false, $"Lỗi Exception: {JsonConvert.SerializeObject(ex)}");
            }
        }
        private (int, string) GetStatusWPay(int status, string mess)
        {
            try
            {
                var messConfig = _memoryCacheService.GetNotifyConfig(_connectStringDb).Where(x => x.AppCode == "WPAY").ToList();
                if (messConfig != null && messConfig.Count > 0)
                {
                   var messData = messConfig.FirstOrDefault(x => x.Status == status.ToString());
                    if(messData!= null)
                    {
                        return (int.Parse(messData.ActionType), messData.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetStatusWPay", ex);
            }
            return (status, mess);
        }
    }
}