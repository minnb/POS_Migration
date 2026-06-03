using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Capillary.Customer;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.Capillary.Transaction;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Dtos.WinMoney;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.API.Common.Shared;
using TCX.WebApiCore.AppServices;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using TCX.WebApiCore.Repository;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore
{
    public class LoyaltyExtentionService
    {
        private static readonly CentralMDRepository centralMDRepository = new CentralMDRepository();
        public LoyaltyExtentionService()
        {
        }
        public async Task<long> GetBlancePoints(RedisManager redisManager, string memberCardNo)
        {
            try
            {
                await Task.Delay(1);
                memberCardNo = FormatHelper.PhoneNumberVietNam(memberCardNo);
                var custInfo = redisManager.HashGet<InfoMemberModel>(RedisConst.GetRedisKeyLoyaltyBalancePoints(memberCardNo), memberCardNo, true);
                var totalPoints = (long)(custInfo != null ? custInfo.MemberPoint : 0);
                if (totalPoints < 0)
                {
                    totalPoints = 0;
                }
                return totalPoints;
            }
            catch
            {
                return 0;
            }
        }
        public static bool CheckStoreApplyStaffPoints(MemoryCacheService memoryCacheService, string storeNo)
        {
            try
            {
                var getCacheConfigLoyalty = memoryCacheService.GetCacheConfigLoyalty(memoryCacheService.GetStringDbLoyalty());
                var checkStoreTopUp = getCacheConfigLoyalty.Where(x => x.Code == LoyaltyProgramEnum.StoreTopUpPoints.ToString() && x.Blocked == false).FirstOrDefault();
                if (checkStoreTopUp == null)
                {
                    return false;
                }
                else
                {
                    if (checkStoreTopUp.ListStore != "ALL")
                    {
                        var lstStore = StringHelper.ConverStringToArray(checkStoreTopUp.ListStore, ';');
                        if (lstStore.Contains(storeNo))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        public static List<string> GetProgramIdCapillary(List<MemoryCacheConfig> memmoryCacheConfigLoyalty, string code = "", bool isStaff = false)
        {
            try
            {
                if(memmoryCacheConfigLoyalty == null)
                {
                    return new List<string>();
                }
                if (isStaff && string.IsNullOrEmpty(code))
                {
                    code = LoyaltyProgramEnum.CapillaryStaffMembershipPoints.ToString();
                }
                else if(!isStaff && string.IsNullOrEmpty(code))
                {
                    code = LoyaltyProgramEnum.CapillaryCustomerMembershipPoints.ToString();
                }
                var programId = memmoryCacheConfigLoyalty.FirstOrDefault(x => x.Blocked == false && x.Code.ToUpper() == code.ToUpper());
                if (programId != null && !string.IsNullOrEmpty(programId.ListStore))
                {
                    return StringHelper.ConverStringToArray(programId.ListStore, ';').ToList();
                }
                return new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        public static (long, long) GetPoints(List<MemoryCacheConfig> memmoryCacheConfigLoyalty, DataCustomerCapillaryResponse modelCustomer, List<PointsSummary> pointSummary, bool isWincare, string member_type = "")
        {
            long totalPoint = 0;
            long memberPoint = 0;
            try
            {
                if (modelCustomer != null
                        && modelCustomer.Points_summaries != null
                        && modelCustomer.Points_summaries.Points_summary != null
                        && pointSummary != null)
                {
                    if (isWincare && member_type == MemberTypeCapillaryEnum.STAFF.ToString())
                    {
                        var getProgramIdStaff = GetProgramIdCapillary(memmoryCacheConfigLoyalty, LoyaltyProgramEnum.CapillaryStaffMembershipPoints.ToString());
                        if (getProgramIdStaff != null && getProgramIdStaff.Count > 0)
                        {
                            var checkPointsStaff = pointSummary.FirstOrDefault(x => x.ProgramId == getProgramIdStaff.FirstOrDefault());
                            if (checkPointsStaff != null)
                            {
                                totalPoint = (long)NumberHelper.TryParseDecimal(checkPointsStaff.LoyaltyPoints ?? "0");
                                memberPoint = (long)NumberHelper.TryParseDecimal(checkPointsStaff.LoyaltyPoints ?? "0");
                            }
                        }
                    }
                    else
                    {
                        var getProgramIdCust = GetProgramIdCapillary(memmoryCacheConfigLoyalty, LoyaltyProgramEnum.CapillaryCustomerMembershipPoints.ToString());
                        if (getProgramIdCust != null && getProgramIdCust.Count > 0)
                        {
                            var checkPointsCust = pointSummary.Where(x => getProgramIdCust.Contains(x.ProgramId));
                            if (checkPointsCust != null)
                            {
                                foreach (var item in checkPointsCust)
                                {
                                    totalPoint += (long)NumberHelper.TryParseDecimal(item.LoyaltyPoints ?? "0");
                                    memberPoint += (long)NumberHelper.TryParseDecimal(item.LoyaltyPoints ?? "0");
                                }
                            }
                        }
                    }
                }
                return (totalPoint, memberPoint);
            }
            catch
            {
                return (0, 0);
            }
        }
        public static MemberBusinessData GetMemberBusinessData(MemberBusinessService _memberBusinessService, List<MemoryCacheConfig> getMemmoryCacheConfigLoyalty, string storeNo, string posID, string phoneNumber)
        {
            if (getMemmoryCacheConfigLoyalty != null && getMemmoryCacheConfigLoyalty.FirstOrDefault(x => x.Blocked == false && x.Code.ToLower() == OrtherStatusLoyal.MemberBusiness.ToString().ToLower()) != null)
            {
                var checkMemberBusiness = _memberBusinessService.GetMemberBusinessItem(storeNo, posID, FormatHelper.PhoneNumberVietNam(phoneNumber));
                if (checkMemberBusiness != null && checkMemberBusiness.Item1)
                {
                    return checkMemberBusiness.Item3;
                }
            }
            return null;
        }
        public static bool IsRevertTopUpPoints(MemoryCacheService memmoryCachesSevice)
        {
            try
            {
                var config = memmoryCachesSevice.GetCacheConfigLoyalty(memmoryCachesSevice.GetStringDbLoyalty());
                return config?.Any(x => x.Code == LoyaltyProgramEnum.RevertTopUpPoints.ToString() && x.Blocked == false) == true;
            }
            catch
            {
                return false;
            }
        }
        public static string GetInputTypeMember(string memberCapillary, string phoneNumber) 
        { 
            if(memberCapillary == MemberCapillaryEnum.WINCARE.ToString())
            {
                return phoneNumber;
            }
            else 
            {
                return memberCapillary;
            }
        }
        public int GetRate(LoyaltyCapillaryService loyaltyCapillaryService, DataCustomerCapillaryResponse modelCustomer)
        {
            try
            {
                if (!string.IsNullOrEmpty(loyaltyCapillaryService.GetValueFieldCapillary(modelCustomer.Custom_fields.Field, "staff_burn_rate")))
                {
                    return ParseHelper.IntOrZero(loyaltyCapillaryService.GetValueFieldCapillary(modelCustomer.Custom_fields.Field, "staff_burn_rate"));
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        public List<EarnLineItems> CalcEarnLineItemsV2(VinIDSalesRequest request, List<RawSideEffects> rawSideEffects)
        {
            try
            {
                var earnLineItems = new List<EarnLineItems>();
                foreach (var item in rawSideEffects)
                {
                    if (item.Points != null && decimal.Parse(item.Points) > 0)
                    {
                        //ghi nhận doanh thu tại POS
                        if (item.AwardOn == "BILL_LINEITEM_PROMOTION" && item.PromoName.ToUpper().Contains("BASE-POINTS-PROMOTION"))
                        {
                            var lineItemIdCAP = item.LineItemId.FirstOrDefault();
                            if(lineItemIdCAP == null)
                            {
                                continue;
                            }
                            var lineItem = request.TransLine.FirstOrDefault(x => x.LineNo == ParseHelper.IntOrZero(lineItemIdCAP.ExtendedFields.Pos_line_no));
                            if (lineItem != null)
                            {
                                earnLineItems.Add(new EarnLineItems()
                                {
                                    LineNo = ParseHelper.IntOrZero(lineItemIdCAP.ExtendedFields.Pos_line_no),
                                    ItemCode = lineItemIdCAP.ItemCode,
                                    UnitOfMeasure = lineItem.UnitOfMeasure,
                                    Quantity = lineItem.Quantity,
                                    LineAmountIncVAT = lineItem.LineAmountIncVAT,
                                    DiscountAmount = lineItem.DiscountAmount,
                                    Description = lineItem.Description,
                                    UnitPrice = lineItem.UnitPrice,
                                    DiscountType = lineItem.DiscountType,
                                    VatAmount = lineItem.VatAmount,
                                    Size = lineItem.Size,
                                    CardLevel = lineItem.CardLevel,
                                    EarnPoints = (long)decimal.Parse(item.Points),
                                    IsPOS = true,
                                    PromoName = item.PromoName
                                });
                            }
                        }
                        //không ghi nhận doanh thu tại POS
                        else if (item.AwardOn == "BILL_LINEITEM_PROMOTION" && item.PromoName.Contains("BONUS-POINTS"))
                        {
                            var lineItemIdCAP = item.LineItemId.FirstOrDefault();
                            if (lineItemIdCAP == null)
                            {
                                continue;
                            }
                            var lineItem = request.TransLine.FirstOrDefault(x => x.LineNo == ParseHelper.IntOrZero(lineItemIdCAP.ExtendedFields.Pos_line_no));
                            if (lineItem != null)
                            {
                                earnLineItems.Add(new EarnLineItems()
                                {
                                    LineNo = ParseHelper.IntOrZero(lineItemIdCAP.ExtendedFields.Pos_line_no),
                                    ItemCode = lineItemIdCAP.ItemCode,
                                    UnitOfMeasure = lineItem.UnitOfMeasure,
                                    Quantity = lineItem.Quantity,
                                    LineAmountIncVAT = lineItem.LineAmountIncVAT,
                                    DiscountAmount = lineItem.DiscountAmount,
                                    Description = lineItem.Description,
                                    UnitPrice = lineItem.UnitPrice,
                                    DiscountType = lineItem.DiscountType,
                                    VatAmount = lineItem.VatAmount,
                                    Size = lineItem.Size,
                                    CardLevel = lineItem.CardLevel,
                                    EarnPoints = (long)decimal.Parse(item.Points),
                                    IsPOS = false,
                                    PromoName = item.PromoName
                                });
                            }
                        }
                    }
                }
                return earnLineItems;
            }
            catch(Exception ex)
            {
                FileHelper.WriteLogs($"{request.OrderNo} CalcEarnLineItemsV2.Exception {JsonConvert.SerializeObject(ex)}");
                return null;
            }
        }
        public List<EarnLineItems> CalcEarnLineItems(VinIDSalesRequest request, List<RawSideEffects> rawSideEffects , TransactionDetailCapillaryResponse transactionDetail)
        {
            var earnLineItems = new List<EarnLineItems>();
            //var lineItemCap = transactionDetail.Data.FirstOrDefault().LineItems;
            //foreach (var item in lineItemCap)
            //{
            //    var dataPointByLine = rawSideEffects.FirstOrDefault(x => x.LineItemId == item.Id.ToString());
            //    if(dataPointByLine != null)
            //    {
            //        int decimalIndex = dataPointByLine.Points.IndexOf('.');
            //        string integerPart = decimalIndex == -1 ? dataPointByLine.Points : dataPointByLine.Points.Substring(0, decimalIndex);
            //        item.Details.Points = int.Parse(integerPart);
            //    }
            //}
            //foreach (var item in request.TransLine)
            //{
            //    var lineItem = lineItemCap.FirstOrDefault(x => x.Details.Serial == item.LineNo);
            //    earnLineItems.Add(new EarnLineItems()
            //    {
            //        LineNo = item.LineNo,
            //        ItemCode = item.ItemCode,
            //        UnitOfMeasure = item.UnitOfMeasure,
            //        Quantity = item.Quantity,
            //        LineAmountIncVAT = item.LineAmountIncVAT,
            //        DiscountAmount = item.DiscountAmount,
            //        Description = item.Description,
            //        UnitPrice = item.UnitPrice,
            //        DiscountType = item.DiscountType,
            //        VatAmount = item.VatAmount,
            //        Size = item.Size,
            //        CardLevel = item.CardLevel,
            //        EarnPoints = lineItem.Details.Points,
            //    });
            //}
            return earnLineItems;
        }
        public WinMoneyConversion WinMoneyConversionWinPay(MemoryCacheService _memoryCacheService, LoyaltyRepository _loyaltyRepository, string storeNo, string posNo, string phoneNumber, string cashierID, bool IsConversion)
        {
            try
            {
                var dataConversion = _memoryCacheService.GetWinMoneyConversions(_loyaltyRepository.ConnectStringLoyaltyDb());
                phoneNumber = FormatHelper.PhoneNumberVietNam(phoneNumber);
                WinMoneyConversion winMoneyConversion;
                if (dataConversion != null && dataConversion.Count > 0)
                {
                    winMoneyConversion = dataConversion.FirstOrDefault(x => x.PhoneNumber == phoneNumber);
                    if (winMoneyConversion != null && IsConversion)
                    {
                        string errMess = "";
                        winMoneyConversion.StoreNo = storeNo;
                        winMoneyConversion.PosNo = posNo;
                        winMoneyConversion.CashierID = cashierID;
                        winMoneyConversion.UpdateTime = DateTime.Now;
                        if (_loyaltyRepository.UpdateWinMoneyConversion(winMoneyConversion, ref errMess))
                        {
                            var messageKafka = new KafkaMessageCache() { Key = MemoryCacheConst.WinMoneyConversion, Type = TypeKafkaEnum.MEMCACHED.ToString(), Topic = TopicKafkaEnum.bluepos_loyalty.ToString(), Data = winMoneyConversion };
                            //_kafkaService.Produce(TopicKafkaEnum.bluepos_loyalty.ToString(), JsonConvert.SerializeObject(messageKafka));
                            _memoryCacheService.DeleteMemoryCache(MemoryCacheConst.WinMoneyConversion);
                            return winMoneyConversion;
                        }
                    }
                    else if (winMoneyConversion != null && IsConversion == false)
                    {
                        return winMoneyConversion;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("WinMoneyConversionWinPay.Exception", ex);
                return null;
            }
        }
        public string MessageNotValidPhone(string phoneNumber)
        {
            return string.Format("Số thẻ {0} không hợp lệ", phoneNumber);
        }
        public string CardLevelMapping(MemoryCacheService _memoryCacheService, string appCode, string cardLevel)
        {
            try
            {
                string newLevel = cardLevel;
                var level = _memoryCacheService.GetCardLevelMapping(_memoryCacheService.GetStringDbLoyalty());
                if (level != null && level.Count > 0)
                {
                    newLevel = level.Where(x => x.AppCode == appCode && x.Level.ToUpper() == cardLevel.ToUpper()).FirstOrDefault().ToLevel ?? cardLevel;
                }
                return newLevel;
            }
            catch
            {
                return cardLevel;
            }
        }
        public static async Task RetryTransactionOffline(MemoryCacheService _memoryCacheService, RedisManager _redisManager, LoyaltyService _loyaltyService, WinCustomerService _winCustomerService)
        {
            List<LoggingLoyaltyDto> logging = new List<LoggingLoyaltyDto>();
            string connectStringDapper = ConfigurationManager.ConnectionStrings["DAPPER_LOYALTY"].ConnectionString;
            var dapper = new DapperConnection(connectStringDapper);
            using (IDbConnection db = dapper.CreateConnDB)
            {
                try
                {
                    db.Open();
                    var data = db.Query<TransactionOfflineDto>("EXEC SP_API_GET_TRANSATION_LOYALTY_OFFLINE;", commandTimeout: 3600).ToList();
                    if (data.Count > 0)
                    {
                        var orderNo = data
                        .Select(x => new { x.OrderNo })
                        .GroupBy(x => new { x.OrderNo })
                        .Select(x =>
                        {
                            var temp = x.OrderByDescending(o => o.OrderNo).FirstOrDefault();
                            return new { x.Key.OrderNo };
                        }).ToList();

                        foreach (var item in orderNo)
                        {

                            var transOffline = data.Where(x => x.OrderNo == item.OrderNo).ToList();
                            if (transOffline.FirstOrDefault().TransactionType == 1)
                            {
                                var rspSales = await _loyaltyService.AddTransactionCapillary(_memoryCacheService, _redisManager, _loyaltyService.MappingAddTransactionCAP(transOffline), false);
                                if (rspSales.Item1.Status == HttpStatusCode.OK)
                                {
                                    logging.Add(new LoggingLoyaltyDto()
                                    {
                                        AppCode = "CAP",
                                        ActionType = "AddTransaction",
                                        OrderNo = transOffline.FirstOrDefault().OrderNo,
                                        MemberCardNo = transOffline.FirstOrDefault().CardNumber,
                                        LoyaltyPoints = NumberHelper.TryParseInt((rspSales.Item2.PointEarn ?? 0).ToString()),
                                        Transaction = "",
                                        Status = rspSales.Item1.Status.ToString(),
                                        Request = "",
                                        Response = JsonConvert.SerializeObject(rspSales.Item1.Data),
                                        CrtDate = DateTime.Now,
                                        OrigOrderNo = transOffline.FirstOrDefault().OrigOrderNo
                                    });
                                }
                                else
                                {
                                    logging.Add(new LoggingLoyaltyDto()
                                    {
                                        AppCode = "CAP",
                                        ActionType = "AddTransaction",
                                        OrderNo = transOffline.FirstOrDefault().OrderNo,
                                        MemberCardNo = transOffline.FirstOrDefault().CardNumber,
                                        LoyaltyPoints = NumberHelper.TryParseInt((rspSales.Item2.PointEarn ?? 0).ToString()),
                                        Transaction = "",
                                        Status = rspSales.Item1.Status.ToString(),
                                        Request = "",
                                        Response = JsonConvert.SerializeObject(rspSales.Item1.Data),
                                        CrtDate = DateTime.Now,
                                        OrigOrderNo = transOffline.FirstOrDefault().OrigOrderNo
                                    });
                                }
                            }
                            else
                            {
                                var rspSalesReturn = await _loyaltyService.RefundPointCapillary(_memoryCacheService, _redisManager, _loyaltyService.MappingReturnTransactionCAP(transOffline));
                                if (rspSalesReturn.Status == HttpStatusCode.OK)
                                {
                                    logging.Add(new LoggingLoyaltyDto()
                                    {
                                        AppCode = "CAP",
                                        ActionType = "PointReverse",
                                        OrderNo = transOffline.FirstOrDefault().OrderNo,
                                        MemberCardNo = transOffline.FirstOrDefault().CardNumber,
                                        LoyaltyPoints = 0,
                                        Transaction = "",
                                        Status = rspSalesReturn.Status.ToString(),
                                        Request = "",
                                        Response = JsonConvert.SerializeObject(rspSalesReturn.Data),
                                        CrtDate = DateTime.Now,
                                        OrigOrderNo = transOffline.FirstOrDefault().OrigOrderNo
                                    });
                                }
                                else
                                {
                                    logging.Add(new LoggingLoyaltyDto()
                                    {
                                        AppCode = "CAP",
                                        ActionType = "PointReverse",
                                        OrderNo = transOffline.FirstOrDefault().OrderNo,
                                        MemberCardNo = transOffline.FirstOrDefault().CardNumber,
                                        LoyaltyPoints = 0,
                                        Transaction = "",
                                        Status = rspSalesReturn.Status.ToString(),
                                        Request = "",
                                        Response = JsonConvert.SerializeObject(rspSalesReturn.Data),
                                        CrtDate = DateTime.Now,
                                        OrigOrderNo = transOffline.FirstOrDefault().OrigOrderNo
                                    });
                                }
                            }
                        }
                    }

                    //Retry Cập nhật tích lũy tiền cho KH
                    var resultSavingAccumulationOffline = _winCustomerService.SavingAccumulationOffline(db);
                    if (resultSavingAccumulationOffline.Item1)
                    {
                        foreach (var item in resultSavingAccumulationOffline.Item3)
                        {
                            logging.Add(new LoggingLoyaltyDto()
                            {
                                AppCode = "ACCU",
                                ActionType = "SavingAccumulation",
                                OrderNo = item.OrderNo,
                                MemberCardNo = item.MemberCard ?? "",
                                LoyaltyPoints = (long)item.TenderAmount,
                                Transaction = item.OperationId ?? "",
                                Status = "OK",
                                Request = JsonConvert.SerializeObject(item),
                                Response = item.Response ?? "",
                                CrtDate = DateTime.Now,
                                OrigOrderNo = item.OperationId ?? ""
                            });
                        }
                    }
                    if (logging.Count > 0)
                    {
                        //if (!_loyaltyService.InsertLoggingLoyaltyAsync(db, logging)) FileHelper.WriteLogs("InsertLoggingLoyaltyAsync ERROR");
                        logging.Clear();
                    }
                }
                catch (Exception ex)
                {
                    FileHelper.WriteExpLogs("GetTransationOffline IDbConnection Exception:", ex);
                }
            }
        }
    }
}