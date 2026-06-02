using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.Capillary.Customer;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore
{
    public static class LoyaltyHelper
    {
        public static string CheckSwithLoyaltyTheWINX(RedisManager redisManager, LoyaltyRepository loyaltyRepository, string storeNo)
        {
            string appCodeDefault = AppCodeEnum.Capillary.ToString();
            try
            {
                var getCacheConfigLoyalty = loyaltyRepository.GetMemoryCacheConfig(redisManager, LoyaltyProgramEnum.SwithLoyaltyTheWINX.ToString());
                if (getCacheConfigLoyalty == null || getCacheConfigLoyalty.Blocked)
                {
                    return appCodeDefault;
                }

                if (getCacheConfigLoyalty.ListStore == "ALL")
                {
                    return AppCodeEnum.THEWINX.ToString();
                }

                var storeList = StringHelper.ConverStringToArray(getCacheConfigLoyalty.ListStore, ';');
                return storeList.Contains(storeNo) ? AppCodeEnum.THEWINX.ToString() : appCodeDefault;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"CheckSwithLoyaltyTheWINX.Exception: {JsonConvert.SerializeObject(ex)}");
                return appCodeDefault;
            }
        }
        public static Tuple<bool, string> IsOffVinID(MemoryCacheService _memoryCacheService, string function)
        {
            var checkWebApiVinID = _memoryCacheService.GetSysWebApi(_memoryCacheService.GetConnectStringCentralMD()).FirstOrDefault(x => x.AppCode == "VINID");
            if (checkWebApiVinID != null)
            {
                var isCheck = checkWebApiVinID.SysWebApiRoute.FirstOrDefault(x => x.Name == function);
                if (isCheck != null && isCheck.Route.ToLower() == "NotUsed".ToLower())
                {
                    return new Tuple<bool, string>(true, isCheck.Notes);
                }
            }

            return new Tuple<bool, string>(false, "OK");
        }
        public static bool IsValidateMember(MemoryCacheService _memoryCacheService, string storeNo, string memberCard = "")
        {
            var checkStore = _memoryCacheService.GetStores().FirstOrDefault(x => x.No == storeNo);
            if (checkStore != null && checkStore.PostCode == "WINCARE")
            {
                return true;
            }
            return false;
        }
        public static string MessageNotValidPhone(string phoneNumber)
        {
            return string.Format("Số thẻ {0} không hợp lệ", phoneNumber);
        }
        public static bool IsVINID(string memberCard)
        {
            if ((StringHelper.Left(memberCard, 4) == "8888" || StringHelper.Left(memberCard, 5) == "66668") && memberCard.Length >= 16) return true;

            if (StringHelper.Left(memberCard, 1).ToUpper() == "P" && memberCard.Length != 14) return true;

            return false;
        }
        public static int DaysFromString(string dateString, DateTime orderTime)
        {
            try
            {
                var dt = DateTime.ParseExact(dateString, "ddMMyy", CultureInfo.InvariantCulture);
                if (dt.Year < 100)
                    dt = dt.AddYears(2000 - dt.Year);
                int days = (orderTime - dt).Days;
                return Math.Abs(days);
            }
            catch
            {
                return -1;
            }
        }
        public static int RoundToNearest(int value, int baseUnit, int level)
        {
            int remainder = value % baseUnit;
            if (remainder > level)
            {
                return value - remainder + baseUnit; 
            }
            else
            {
                return value - remainder;
            }
        }
        public static bool IsWincareTopUpPoints(string qRCode)
        {
            if (string.IsNullOrEmpty(qRCode) || qRCode.Length < 12)
            {
                return false;
            }
            string[] wincarePrefixes = new string[] { "WIN" };
            return wincarePrefixes.Contains(StringHelper.Left(qRCode, 3));
        }
        public static string Get_lineitem_brand_type(string divisionCode)
        {
            string[] lst = new string[] { "MBC", "PLG", "VMP", "WCM" };
            if (lst.Contains(divisionCode))
            {
                return divisionCode;
            }
            return "WCM";
        }
        public static string BluePOS_LoyaltyKeyRedis(string cardNumber)
        {
            return $"{RedisConst.Redis_Key_BLUEPOS}:Loaylty:{cardNumber.Substring(0,4)}";
        }
        public static async Task RefundTransactionLogging(RedisManager redisManager, KibanaService kibanaService, VinIDRefundRequest request, RefundTransationResponse response, List<EarnLineItems> transLine = null, long awardedPoints = 0)
        {
            var cusInfo = redisManager.HashGet<InfoMemberModel>(RedisConst.GetRedisKeyLoyaltyBalancePoints(FormatHelper.PhoneNumberVietNam(request.CardNumber)), FormatHelper.PhoneNumberVietNam(request.CardNumber));
            var dataLoggingLoyalty = new LoggingLoyaltyDto()
            {
                AppCode = AppCodeEnum.POS.ToString(),
                ActionType = ActionTypeCapiEnum.RefundTransaction.ToString(),
                OrderNo = request.OrderNo,
                OrigOrderNo = request.OrigOrderNo ?? "",
                MemberCardNo = FormatHelper.PhoneNumberVietNam(request.CardNumber),
                LoyaltyPoints = awardedPoints,
                Transaction = response.CreatedId.ToString(),
                TransactionType = request.TransactionType,
                Status = HttpStatusCodeEnum.Success.ToString(),
                Request = JsonConvert.SerializeObject(request),
                Response = JsonConvert.SerializeObject(response),
                CrtDate = DateTime.Now,
                Items = JsonConvert.SerializeObject(transLine),
                CustName = cusInfo != null ? cusInfo.MemberName : ""  
            };
            await Task.Run(() =>
            {
                RabbitMQProducer.ProducerRabbtMQCluster(kibanaService, RabitMQConst.Queue_Loyalty_LoggingLoyalty,
                                                                          request.OrderNo,
                                                                          JsonConvert.SerializeObject(dataLoggingLoyalty));
            });
        }
        public static async Task PointRedeemLogging(RedisManager redisManager, KibanaService kibanaService, VinIDSalesRequest request, PointsDataCap pointRedeemCapResponse)
        {
            var cusInfo = redisManager.HashGet<InfoMemberModel>(RedisConst.GetRedisKeyLoyaltyBalancePoints(FormatHelper.PhoneNumberVietNam(request.CardNumber)), FormatHelper.PhoneNumberVietNam(request.CardNumber));
            var dataLoggingLoyalty = new LoggingLoyaltyDto()
            {
                AppCode = AppCodeEnum.POS.ToString(),
                ActionType = ActionTypeCapiEnum.RedeemPoints.ToString(),
                OrderNo = request.OrderNo,
                OrigOrderNo = request.VirtualCard ?? "",
                MemberCardNo = FormatHelper.PhoneNumberVietNam(request.CardNumber),
                LoyaltyPoints = request.SpendPoints,
                Transaction = pointRedeemCapResponse.Redemption_id ??"",
                TransactionType = request.TransactionType,
                Status = HttpStatusCodeEnum.Success.ToString(),
                Request = JsonConvert.SerializeObject(request),
                Response = JsonConvert.SerializeObject(pointRedeemCapResponse),
                CrtDate = DateTime.Now,
                Items = null,
                CustName = cusInfo != null ? cusInfo.MemberName : ""
            };
            await Task.Run(() =>
            {
                RabbitMQProducer.ProducerRabbtMQCluster(kibanaService, RabitMQConst.Queue_Loyalty_LoggingLoyalty,
                                                                          request.OrderNo,
                                                                          JsonConvert.SerializeObject(dataLoggingLoyalty));
            });
        }
        public static async Task PointReverseLogging(RedisManager redisManager, KibanaService kibanaService, VinIDRefundRequest request, PointReverseCapResponse pointReverseCapResponse)
        {
            var cusInfo = redisManager.HashGet<InfoMemberModel>(RedisConst.GetRedisKeyLoyaltyBalancePoints(FormatHelper.PhoneNumberVietNam(request.CardNumber)), FormatHelper.PhoneNumberVietNam(request.CardNumber));
            var dataLoggingLoyalty = new LoggingLoyaltyDto()
            {
                AppCode = AppCodeEnum.POS.ToString(),
                ActionType = ActionTypeCapiEnum.RefundPoints.ToString(),
                OrderNo = request.OrderNo,
                OrigOrderNo = request.OrigOrderNo ?? "",
                MemberCardNo = FormatHelper.PhoneNumberVietNam(request.CardNumber),
                LoyaltyPoints = (long) pointReverseCapResponse.PointsToBeReversed,
                Transaction = pointReverseCapResponse.RedemptionId,
                TransactionType = request.TransactionType,
                Status = HttpStatusCodeEnum.Success.ToString(),
                Request = JsonConvert.SerializeObject(request),
                Response = JsonConvert.SerializeObject(pointReverseCapResponse),
                CrtDate = DateTime.Now,
                Items = null,
                CustName = cusInfo != null ? cusInfo.MemberName : ""
            };
            await Task.Run(() =>
            {
                RabbitMQProducer.ProducerRabbtMQCluster(kibanaService, RabitMQConst.Queue_Loyalty_LoggingLoyalty,
                                                                          request.OrderNo,
                                                                          JsonConvert.SerializeObject(dataLoggingLoyalty));
            });
        }
        public static async Task AddTransactionLogging(KibanaService kibanaService, RedisManager redisManager, Tuple<ResultResponse, PointModePOSResponse> resultEarnPoints, VinIDSalesRequest transaction, LoyaltyRepository _loyaltyRepository)
        {
            string phoneNumber = FormatHelper.PhoneNumberVietNam(transaction.CardNumber);
            string key = RedisConst.GetRedisKeyLoyaltyBalancePoints(phoneNumber);
            var cusInfo = redisManager.HashGet<InfoMemberModel>(key, phoneNumber);
            if (resultEarnPoints.Item1.Status == System.Net.HttpStatusCode.OK
                    && resultEarnPoints.Item2 != null
                    && NumberHelper.IsLong(resultEarnPoints.Item2.CreatedId) > 0)
            {
                var dataPoints = resultEarnPoints.Item2;
                long loyaltyPoints = 0;
                if (dataPoints.TransLine != null)
                {
                    var lineItems = dataPoints.TransLine.Where(x => x.IsPOS == false).ToList();
                    if (lineItems != null && lineItems.Count > 0)
                    {
                        loyaltyPoints = (long)lineItems.Where(x => x.EarnPoints > 0).Sum(x => x.EarnPoints);
                        var dataLoyalty = new LoggingLoyaltyDto()
                        {
                            AppCode = AppCodeEnum.POS.ToString(),
                            ActionType = ActionTypeCapiEnum.AddTransaction.ToString(),
                            OrderNo = transaction.OrderNo,
                            OrigOrderNo = transaction.OrigOrderNo,
                            MemberCardNo = FormatHelper.PhoneNumberVietNam(transaction.CardNumber),
                            LoyaltyPoints = loyaltyPoints,
                            Transaction = dataPoints.CreatedId ?? "",
                            Status = HttpStatusCodeEnum.Success.ToString(),
                            Request = StringHelper.ObjectToStringLowercase(transaction),
                            Response = JsonConvert.SerializeObject(resultEarnPoints.Item2),
                            CrtDate = DateTime.Now,
                            Items = JsonConvert.SerializeObject(lineItems),
                            TransactionType = transaction.TransactionType,
                            CustName = cusInfo != null ? cusInfo.MemberName : ""
                        };

                        redisManager.HashSet<string>(RedisConst.GetRedisKeyLoyaltyMemberPoints(dataLoyalty.OrderNo), dataLoyalty.OrderNo, dataLoyalty.MemberCardNo, DateTimeHelper.GetTotalSecondMidnight(1));
                        //save logging
                        await Task.Run(async () =>
                        {
                            if (await _loyaltyRepository.InsertLoggingLoyalty(dataLoyalty) == null)
                            {
                                RabbitMQProducer.ProducerRabbtMQCluster(kibanaService, RabitMQConst.Queue_Loyalty_LoggingLoyalty,
                                                                                  transaction.OrderNo,
                                                                                  JsonConvert.SerializeObject(dataLoyalty));
                            }
                        });
                    }
                }
            }
        }
        public static async Task RevertPointRedeemLogging(RedisManager redisManager, KibanaService kibanaService, RevertPointsRedeemRequest request, RevertPointsRedeemResponse response)
        {
            var cusInfo = redisManager.HashGet<InfoMemberModel>(RedisConst.GetRedisKeyLoyaltyBalancePoints(FormatHelper.PhoneNumberVietNam(request.MemberCard)), FormatHelper.PhoneNumberVietNam(request.MemberCard));
            var dataLoggingLoyalty = new LoggingLoyaltyDto()
            {
                AppCode = AppCodeEnum.POS.ToString(),
                ActionType = ActionTypeCapiEnum.RevertPointsRedeem.ToString(),
                OrderNo = request.OrderNo,
                OrigOrderNo = request.RedeemId ?? "",
                MemberCardNo = FormatHelper.PhoneNumberVietNam(request.MemberCard),
                LoyaltyPoints = (long) response.PointsReversed,
                Transaction = response.ReversalId,
                TransactionType = 1,
                Status = HttpStatusCodeEnum.Success.ToString(),
                Request = JsonConvert.SerializeObject(request),
                Response = JsonConvert.SerializeObject(response),
                CrtDate = DateTime.Now,
                Items = request.Reason??"",
                CustName = cusInfo != null ? cusInfo.MemberName : ""
            };
            await Task.Run(() =>
            {
                RabbitMQProducer.ProducerRabbtMQCluster(kibanaService, RabitMQConst.Queue_Loyalty_LoggingLoyalty,
                                                                          request.OrderNo,
                                                                          JsonConvert.SerializeObject(dataLoggingLoyalty));
            });
        }
    }
}