using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.Capillary.Point;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;

namespace TCX.WebApiCore.AppServices
{
    public class MemberPointsService
    {
        private readonly KibanaService kibanaService;
        private readonly CentralMDRepository centralMDRepository;
        private readonly LoyaltyRepository loyaltyRepository;
        private readonly LoyaltyCapillaryService loyaltyCapillaryService;
        private readonly WinCareService winCareService;
        private readonly LoyaltyService loyaltyService;
        public MemberPointsService()
        {
            kibanaService = new KibanaService();
            centralMDRepository = new CentralMDRepository();
            loyaltyRepository = new LoyaltyRepository();
            loyaltyCapillaryService = new LoyaltyCapillaryService();
            winCareService = new WinCareService();
            loyaltyService = new LoyaltyService();
        }
        public async Task<(long, long)> GetPointsHistory(MemoryCacheService _memoryCacheService, string phoneNumber, string storeNo, string orderNo)
        {
            try
            {
                var result = await loyaltyCapillaryService.GetPointsLedger(_memoryCacheService, storeNo, storeNo + "00", phoneNumber);
                if (result.Item1)
                {
                    var dataPoints = StringHelper.StringToObject<PointsHistoryResponse>(JsonConvert.SerializeObject(result.Item3));
                    if(dataPoints != null)
                    {
                        var pointsHistory = dataPoints.LedgerEntries.Where(x => x.TransactionDetails.TransactionNumber == orderNo).FirstOrDefault();
                        if(pointsHistory != null)
                        {
                            return (NumberHelper.ConvertScientificToNumber<long>(pointsHistory.NetPointsOnEvent), pointsHistory.TransactionDetails.TransactionId);
                        }
                    }
                }
                return (0,0);
            }
            catch (Exception ex)
            {
                kibanaService.LogException("GetPointsHistory", "", 0, "", JsonConvert.SerializeObject(ex));
                return (0, 0);
            }
        }
        public async Task<bool> IsProcessingRefundPointsCapillary(MemoryCacheService _memoryCacheService, RedisManager redisManager, VinIDRefundRequest transaction)
        {
            if (transaction == null)
            {
                return false;
            }
            try
            {
                transaction.SpendPoints = 0;
                var result = await loyaltyService.RefundPointCapillary(_memoryCacheService, redisManager, transaction);
                if (result.Status == System.Net.HttpStatusCode.OK && result.Data != null)
                {
                    var dataPoints = StringHelper.StringToObject<PointModePOSResponse>(JsonConvert.SerializeObject(result.Data));
                    var dataLoyalty = new LoggingLoyaltyDto()
                    {
                        AppCode = AppCodeEnum.POS.ToString(),
                        ActionType = ActionTypeCapiEnum.RefundPoints.ToString(),
                        OrderNo = transaction.OrderNo,
                        OrigOrderNo = transaction.OrigOrderNo,
                        MemberCardNo = transaction.CardNumber,
                        LoyaltyPoints = (int)dataPoints.PointEarn,
                        Transaction = dataPoints.RedemptionId ?? "",
                        Status = result.Status.ToString(),
                        Request = StringHelper.ObjectToStringLowercase(transaction),
                        Response = JsonConvert.SerializeObject(result.Data),
                        CrtDate = DateTime.Now,
                        Items = JsonConvert.SerializeObject(dataPoints.TransLine),
                        TransactionType = transaction.TransactionType,
                    };
                    await loyaltyRepository.InsertLoggingLoyalty(dataLoyalty);
                    return true;
                }
                kibanaService.LogResponse("IsProcessingRefundPointsCapillary", transaction.TerminalId, 0, "", $"{transaction.CardNumber} - {transaction.OrderNo} failed member points: " + JsonConvert.SerializeObject(result.Data));
                return false;
            }
            catch (Exception ex)
            {
                kibanaService.LogException("IsProcessingRefundPointsCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        public async Task<bool> IsProcessingMemberPointsCapillary(MemoryCacheService _memoryCacheService, RedisManager redisManager, VinIDSalesRequest transaction)
        {
            if (transaction == null)
            {
                return false;
            }
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                transaction.SpendPoints = 0;
                var resultEarnPoints = await loyaltyService.AddTransactionCapillary(_memoryCacheService, redisManager, transaction, false);
                if (resultEarnPoints.Item1.Status == System.Net.HttpStatusCode.OK
                    && resultEarnPoints.Item2 != null
                    && NumberHelper.IsLong(resultEarnPoints.Item2.CreatedId) > 0)
                {
                    await LoyaltyHelper.AddTransactionLogging(kibanaService, redisManager,resultEarnPoints, transaction, loyaltyRepository);
                }
                else
                {
                    long loyaltyPoints = 0;
                    List<int> statusOK = new List<int>() { 604, 3045 };
                    var getCacheConfigLoyalty = _memoryCacheService.GetCacheConfigLoyalty(_memoryCacheService.GetStringDbLoyalty());
                    var checkStatus = getCacheConfigLoyalty.Where(x => x.Code == LoyaltyProgramEnum.SkipCapillaryMembershipPoints.ToString() && x.Blocked == false).FirstOrDefault();
                    if(checkStatus != null)
                    {
                        var arr = StringHelper.ConverStringToArray(checkStatus.ListStore, ';');
                        if(arr != null && arr.Length > 0)
                        {
                            foreach(var store in arr)
                            {
                                statusOK.Add(NumberHelper.IsInteger(store));
                            }
                        }
                    }
                    
                    string status = "Failed";
                    string transactionId = "";
                    string response = resultEarnPoints.Item1.Message;
                    var dataPoints = StringHelper.StringToObject<PointModePOSResponse>(JsonConvert.SerializeObject(resultEarnPoints.Item1.Data));
                    if (dataPoints != null && statusOK.Contains(dataPoints.StatusCode))
                    {
                        status = System.Net.HttpStatusCode.OK.ToString();
                        var dataPointsResponse = await GetPointsHistory(_memoryCacheService, transaction.CardNumber, transaction.MerchantId, transaction.OrderNo);
                        if(dataPointsResponse.Item2 > 0)
                        {
                            loyaltyPoints = dataPointsResponse.Item1;
                            transactionId = dataPointsResponse.Item2.ToString();
                        }
                    }

                    var dataLoyalty = new LoggingLoyaltyDto()
                    {
                        AppCode = "POS",
                        ActionType = ActionTypeCapiEnum.AddTransaction.ToString(),
                        OrderNo = transaction.OrderNo,
                        OrigOrderNo = transaction.OrigOrderNo,
                        MemberCardNo = transaction.CardNumber,
                        LoyaltyPoints = loyaltyPoints,
                        Transaction = transactionId,
                        Status = status,
                        Request = StringHelper.ObjectToStringLowercase(transaction),
                        Response = response,
                        CrtDate = DateTime.Now,
                        Items = null,
                        TransactionType = transaction.TransactionType,
                    };
                    redisManager.HashSet<string>(RedisConst.GetRedisKeyLoyaltyMemberPoints(dataLoyalty.OrderNo), dataLoyalty.OrderNo, dataLoyalty.MemberCardNo, DateTimeHelper.GetTotalSecondMidnight(1));
                    await Task.Run(() => loyaltyRepository.InsertLoggingLoyalty(dataLoyalty));
                }
                st1.Stop();
                kibanaService.LogResponse("IsProcessingMemberPointsCapillary", transaction.PosID, st1.ElapsedMilliseconds, "", $"{transaction.CardNumber} - {transaction.OrderNo} failed member points: " + JsonConvert.SerializeObject(resultEarnPoints));
                return true;
            }
            catch (Exception ex)
            {
                kibanaService.LogException("IsProcessingTopUpPointsCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        public async Task<bool> IsProcessingTopUpPointsCapillary(MemoryCacheService _memoryCacheService, RedisManager redisManager, VinIDSalesRequest transaction)
        {
            if (transaction == null)
            {
                return false;
            }
            try
            {
                if (!LoyaltyExtentionService.CheckStoreApplyStaffPoints(_memoryCacheService, transaction.MerchantId))
                {
                    return true;
                }

                string pointsCode = LoyaltyRateEnum.WINCARE.ToString();
                long totalPoints = 0;
                if (!LoyaltyHelper.IsWincareTopUpPoints(transaction.QRCode) || !transaction.QRCode.Contains("WIN"))
                {
                    return true;
                }
                var loyaltyRateData = centralMDRepository.GetLoyaltyRateData(redisManager, pointsCode);
                if (loyaltyRateData == null)
                {
                    FileHelper.WriteLogs($"LoyaltyRateData is null for Code: {pointsCode}");
                    return true;
                }

                if (transaction.TransLine != null && transaction.TransLine.Count > 0 && loyaltyRateData.Rate > 0)
                {
                    var pointsCalculation = CalculatePoints(redisManager, loyaltyRateData, transaction, pointsCode);
                    totalPoints = pointsCalculation.Item1;
                    if (totalPoints > 0)
                    {
                        if (await PointToUpCapillary(_memoryCacheService,redisManager, transaction, pointsCalculation.Item2, totalPoints, pointsCode))
                        {
                            StringHelper.Loggingz($"PointToUpCapillary: {JsonConvert.SerializeObject(transaction)}");
                            //RequestSentNotifyUserAsync
                            //await winCareService.RequestSentNotifyUserAsync(transaction.QRCode, transaction.MerchantId, 2, transaction.OrderNo);
                            var (time, value) = ExecutionTimer.Measure(async () => await winCareService.RequestSentNotifyUserAsync(transaction.QRCode, transaction.MerchantId, 2, transaction.OrderNo));
                            FileHelper.WriteLogs($"RequestSentNotifyUserAsync execution time: {time.TotalMilliseconds} ms");
                            return true;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                kibanaService.LogException("IsProcessingTopUpPointsCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        public async Task RabbitMQLoyaltyMemberPoints(MemoryCacheService memoryCacheService, RedisManager redisManager, string queueName, int maxMessagesToProcess)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" }
                };
                using (var connection = RabbitMQClusterConnection.GetConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: args);
                    channel.BasicQos(0, 2, false);
                    int messageCount = 0;
                    while (messageCount < maxMessagesToProcess)
                    {
                        var result = channel.BasicGet(queue: queueName, autoAck: false);
                        if (result == null)
                        {
                            break;
                        }
                        var success = false;
                        var message = Encoding.UTF8.GetString(result.Body.ToArray());
                        if (RabitMQConst.Queue_Loyalty_Member_Points == queueName)
                        {
                            success = await IsProcessingMemberPointsCapillary(memoryCacheService, redisManager, StringHelper.StringToObject<VinIDSalesRequest>(message));
                        }
                        else if (RabitMQConst.Queue_Loyalty_Refund_Points == queueName)
                        {
                            success = await SaveLoggingLoyalty(StringHelper.StringToObject<LoggingLoyaltyData>(message));
                        }
                        if (success)
                        {
                            channel.BasicAck(result.DeliveryTag, false);
                            messageCount++;
                        }
                        else
                        {
                            channel.BasicNack(result.DeliveryTag, false, true);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                kibanaService.LogException("ProccessTopUpPointsCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
            }
        }
        public async Task RabbitMQTopUpStaffPoints(MemoryCacheService memoryCacheService, RedisManager redisManager, int maxMessagesToProcess, string queueName)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" }
                };
                using (var connection = RabbitMQClusterConnection.GetConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: args);
                    int messageCount = 0;
                    channel.BasicQos(0, 10, false);
                    while (messageCount < maxMessagesToProcess)
                    {
                        var result = channel.BasicGet(queue: queueName, autoAck: false);
                        if (result == null)
                        {
                            break;
                        }
                        var success = false;
                        var message = Encoding.UTF8.GetString(result.Body.ToArray());
                        if (RabitMQConst.Queue_Wincare_TopUpPoints == queueName)
                        {
                            success = await IsProcessingTopUpPointsCapillary(memoryCacheService, redisManager, StringHelper.StringToObject<VinIDSalesRequest>(message));
                        }
                        else if (RabitMQConst.Queue_Loyalty_LoggingLoyalty == queueName)
                        {
                            var dataLoggingLoyalty = StringHelper.StringToObject<LoggingLoyaltyDto>(message);
                            if (dataLoggingLoyalty != null)
                            {
                                if (await loyaltyRepository.InsertLoggingLoyalty(dataLoggingLoyalty, null) != null)
                                {
                                    success = true;
                                }
                            }
                            success = true;
                        }

                        if (success)
                        {
                            channel.BasicAck(result.DeliveryTag, false);
                            messageCount++;
                        }
                        else
                        {
                            channel.BasicNack(result.DeliveryTag, false, true);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                kibanaService.LogException("ProccessTopUpPointsCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
            }
        }
        public async Task RetryTopUpPointsToCapillary(MemoryCacheService memoryCacheService, RedisManager redisManager)
        {
            try
            {
                var dataRetry = loyaltyRepository.GetLoggingLoyalty("TopUp", "Failed");
                FileHelper.WriteLogs($"RetryTopUpPointsToCapillary: {dataRetry.Count} rows");
                if (dataRetry != null && dataRetry.Count > 0)
                {
                    foreach (var item in dataRetry)
                    {
                        var pointsTopUpData = StringHelper.StringToObject<PointTopupPOSRequest>(item.Request);
                        if (pointsTopUpData != null)
                        {
                            FileHelper.WriteLogs($"RetryTopUpPointsToCapillary: {item.Request}");
                            var topUpResult = await loyaltyCapillaryService.PointToUp(memoryCacheService, redisManager, loyaltyRepository, pointsTopUpData);
                            string statusTopUp = topUpResult != null && topUpResult.Item1 ? "Success" : "Failed";
                            item.Response = topUpResult != null ? StringHelper.ObjectToStringLowercase(topUpResult.Item3) : topUpResult.Item2;
                            if (statusTopUp == "Success")
                            {
                                item.Status = statusTopUp;
                            }
                            loyaltyRepository.UpdateStatusLoggingLoyalty(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("RetryTopUpPointsToCapillary", ex);
            }
        }
        public async Task RevertTopUpPointsCapillary(MemoryCacheService memoryCacheService, RedisManager redisManager, int maxMessagesToProcess)
        {
            try
            {
                string queueName = RabitMQConst.Queue_Wincare_Revert_TopUpPoints;
                var args = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" }
                };
                using (var connection = RabbitMQClusterConnection.GetConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: args);
                    int messageCount = 0;
                    while (messageCount < maxMessagesToProcess)
                    {
                        var result = channel.BasicGet(queue: queueName, autoAck: true);
                        if (result != null)
                        {
                            var message = Encoding.UTF8.GetString(result.Body.ToArray());
                            await RevertTopUpPoints(memoryCacheService, redisManager, StringHelper.StringToObject<VinIDRefundRequest>(message));
                        }
                        else
                        {
                            break;
                        }
                        messageCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                kibanaService.LogException("ProccessTopUpPointsCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
            }
        }
        public async Task<bool> SaveLoggingLoyalty(LoggingLoyaltyData data)
        {
            try
            {
                if (data == null)
                {
                    return true;
                }
                int totalPoints = 0;
                if (data.DataPoints != null)
                {
                    totalPoints = (int)data.DataPoints.PointEarn;
                }
                if (totalPoints == 0)
                {
                    return true;
                }

                var dataLoyalty = new LoggingLoyaltyDto()
                {
                    AppCode = data.AppCode,
                    ActionType = data.ActionType,
                    OrderNo = data.OrderNo,
                    OrigOrderNo = data.RefOrderNo,
                    MemberCardNo = data.MemberCardNo,
                    LoyaltyPoints = totalPoints,
                    Transaction = data.TransactionId ?? "",
                    TransactionType = data.TransactionType,
                    Status = data.HttpStatus,
                    Request = data.DataRequest,
                    Response = data.DataResponse,
                    CrtDate = DateTime.Now,
                    Items = data.DataPoints.TransLine != null ? JsonConvert.SerializeObject(data.DataPoints.TransLine) : null
                };
                var result = await loyaltyRepository.InsertLoggingLoyalty(dataLoyalty);
                kibanaService.LogResponse("SaveLoggingLoyalty", data.OrderNo, 0, "", $"{data.MemberCardNo}-{dataLoyalty.Request}-{dataLoyalty.Response}");
                StringHelper.Loggingz(JsonConvert.SerializeObject(dataLoyalty));
                return result != null;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("LoggingLoyalty", ex);
                return false;
            }
        }
        public async Task<bool> CheckLoggingLoyalty(string orderNo, string actionType)
        {
            try
            {
                List<string> status = new List<string>() { "OK", "Success" };
                var checkData = await loyaltyRepository.GetListLoggingLoyalty(orderNo, actionType);
                if(checkData != null && checkData.Count > 0 && checkData.FirstOrDefault(x=> status.Contains(x.Status)) != null)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CheckLoggingLoyalty.Exception", ex);
                return false;
            }
        }
        private async Task<bool> RevertTopUpPoints(MemoryCacheService memoryCacheService, RedisManager redisManager, VinIDRefundRequest transaction)
        {
            try
            {
                var dataTopUp = loyaltyRepository.InsertLoggingLoyalty(null, transaction.OrigOrderNo);
                if (dataTopUp == null)
                {
                    FileHelper.WriteLogs($"No TopUp points found for OrigOrderNo: {transaction.OrigOrderNo}");
                    return true;
                }

                string pointsCode = LoyaltyRateEnum.WINCARE.ToString();
                long totalRevertPoints = 0;
                var loyaltyRateData = centralMDRepository.GetLoyaltyRateData(redisManager, pointsCode);
                if (loyaltyRateData == null)
                {
                    FileHelper.WriteLogs($"LoyaltyRateData is null for Code: {pointsCode}");
                    return true;
                }
                var pointsCalculation = CalculateRevertPoints(redisManager, loyaltyRateData, transaction, pointsCode);
                totalRevertPoints = pointsCalculation.Item1;
                if (totalRevertPoints > 0)
                {
                    StringHelper.Loggingz($"RevertTopUpPoints: {JsonConvert.SerializeObject(transaction)}");
                    if (await RevertPointToUpCapillary(memoryCacheService, transaction, totalRevertPoints, pointsCode))
                    {
                        //await winCareService.RequestSentNotifyUserAsync(transaction.QRCode, transaction.MerchantId);
                        return true;
                    }
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                kibanaService.LogException("RevertTopUpPoints", "", 0, "", JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        private Tuple<long, List<ItemsTopUpCapillary>> CalculateRevertPoints(RedisManager redisManager, LoyaltyRateDto loyaltyRateData, VinIDRefundRequest transaction, string pointsCode)
        {
            long totalPoints = 0;
            List<ItemsTopUpCapillary> itemsTopUp = new List<ItemsTopUpCapillary>();
            foreach (var line in transaction.TransLine)
            {
                var hashField = $"{pointsCode}-{line.ItemCode}-{line.UnitOfMeasure}";
                var itemPointsMember = centralMDRepository.GetItemPointsMember(redisManager, pointsCode, line.ItemCode, line.UnitOfMeasure);
                if (itemPointsMember != null)
                {
                    var pointsTopUp = LoyaltyHelper.RoundToNearest((int)(line.LineAmountIncVAT * loyaltyRateData.Rate), 100000, 50000);
                    itemsTopUp.Add(new ItemsTopUpCapillary()
                    {
                        ItemNo = line.ItemCode,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        DiscountAmount = line.DiscountAmount,
                        LineAmountIncVAT = line.LineAmountIncVAT,
                        Points = pointsTopUp
                    });
                    totalPoints += pointsTopUp;
                }
            }
            return new Tuple<long, List<ItemsTopUpCapillary>>(totalPoints, itemsTopUp);
        }
        private Tuple<long, List<ItemsTopUpCapillary>> CalculatePoints(RedisManager redisManager, LoyaltyRateDto loyaltyRateData, VinIDSalesRequest transaction, string pointsCode)
        {
            long totalPoints = 0;
            List<ItemsTopUpCapillary> itemsTopUp = new List<ItemsTopUpCapillary>();
            foreach (var line in transaction.TransLine)
            {
                var hashField = $"{pointsCode}-{line.ItemCode}-{line.UnitOfMeasure}";
                var itemPointsMember = centralMDRepository.GetItemPointsMember(redisManager, pointsCode, line.ItemCode, line.UnitOfMeasure);
                if (itemPointsMember != null)
                {
                    var daysExpired = LoyaltyHelper.DaysFromString(StringHelper.Right(line.PackId, 6), transaction.OrderTime);
                    StringHelper.Loggingz($"CalculatePoints {itemPointsMember.ItemNo}: {daysExpired} {itemPointsMember.ShelfLife - daysExpired}");
                    if (itemPointsMember.ShelfLife - daysExpired < 0 || itemPointsMember.ShelfLife - daysExpired > itemPointsMember.DaysOfUsed)
                    {
                        FileHelper.WriteLogs($"{line.PackId} expired for {transaction.OrderTime:yyyy-MM-dd}");
                        continue;
                    }
                    else
                    {
                        var pointsTopUp = LoyaltyHelper.RoundToNearest((int)(line.LineAmountIncVAT * loyaltyRateData.Rate), 100000, 50000);
                        itemsTopUp.Add(new ItemsTopUpCapillary()
                        {
                            ItemNo = line.ItemCode,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            DiscountAmount = line.DiscountAmount,
                            LineAmountIncVAT = line.LineAmountIncVAT,
                            Points = pointsTopUp
                        });
                        totalPoints += pointsTopUp;
                    }
                }
            }
            return new Tuple<long, List<ItemsTopUpCapillary>>(totalPoints, itemsTopUp);
        }
        private async Task<bool> PointToUpCapillary(MemoryCacheService memoryCacheService,RedisManager redisManager, VinIDSalesRequest transaction, List<ItemsTopUpCapillary> itemsTopUp, long totalPoints, string pointsCode)
        {
            try
            {
                var pointsTopUpData = new PointTopupPOSRequest()
                {
                    StoreNo = transaction.MerchantId,
                    PosNo = transaction.TerminalId,
                    OrderNo = transaction.OrderNo,
                    PhoneNumber = transaction.CardNumber,
                    Points = totalPoints,
                    Comments = StringHelper.ObjectToStringLowercase(itemsTopUp)
                };
                var topUpResult = await loyaltyCapillaryService.PointToUp(memoryCacheService, redisManager, loyaltyRepository, pointsTopUpData);
                string statusTopUp = topUpResult != null && topUpResult.Item1 ? "Success" : "Failed";
                string responseTopUp = topUpResult != null ? StringHelper.ObjectToStringLowercase(topUpResult.Item3) : topUpResult.Item2;
                var dataLoyalty = new LoggingLoyaltyDto()
                {
                    AppCode = pointsCode,
                    ActionType = ActionTypeCapiEnum.TopUp.ToString(),
                    OrderNo = transaction.OrderNo,
                    OrigOrderNo = transaction.OrigOrderNo,
                    MemberCardNo = transaction.CardNumber,
                    LoyaltyPoints = (int)totalPoints,
                    Transaction = transaction.VirtualCard ?? "",
                    Status = statusTopUp,
                    Request = StringHelper.ObjectToStringLowercase(pointsTopUpData),
                    Response = responseTopUp,
                    CrtDate = DateTime.Now,
                    TransactionType = transaction.TransactionType,
                    Items = JsonConvert.SerializeObject(itemsTopUp)
                };
                await loyaltyRepository.InsertLoggingLoyalty(dataLoyalty);
                kibanaService.LogResponse("PointToUpCapillary", transaction.TerminalId, 0, "", $"{transaction.CardNumber}-{dataLoyalty.Request}-{dataLoyalty.Response}");
                StringHelper.Loggingz(JsonConvert.SerializeObject(topUpResult));
                return topUpResult != null && topUpResult.Item1;
            }
            catch (Exception ex)
            {
                kibanaService.LogException("PointToUpCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
                return false;
            }
        }
        private async Task<bool> RevertPointToUpCapillary(MemoryCacheService memoryCacheService, VinIDRefundRequest transaction, long totalPoints, string pointsCode)
        {
            try
            {
                var pointsTopUpData = new RevertTopUpPointsCapRequest()
                {
                    PointsToBeAdjusted = totalPoints.ToString(),
                    PointCategoryTypes = "REGULAR",
                    ProgramId = 0,
                    ReasonOfReturn = $"Revert TopUp Points OrderNo:{transaction.OrigOrderNo}"
                };

                var topUpResult = await loyaltyCapillaryService.RevertTopUpPoints(memoryCacheService, pointsTopUpData, transaction.CardNumber, transaction.MerchantId);
                string statusTopUp = topUpResult != null && topUpResult.Item1 ? "Success" : "Failed";
                string responseTopUp = topUpResult != null ? StringHelper.ObjectToStringLowercase(topUpResult.Item3) : topUpResult.Item2;
                var dataLoyalty = new LoggingLoyaltyDto()
                {
                    AppCode = pointsCode,
                    ActionType = ActionTypeCapiEnum.RevertPointsTopUp.ToString(),
                    OrderNo = transaction.OrderNo,
                    OrigOrderNo = transaction.OrigOrderNo,
                    MemberCardNo = transaction.CardNumber,
                    LoyaltyPoints = (int)totalPoints,
                    Transaction = transaction.OrigOrderNo ?? "",
                    Status = statusTopUp,
                    Request = StringHelper.ObjectToStringLowercase(pointsTopUpData),
                    Response = responseTopUp,
                    CrtDate = DateTime.Now,
                };
                await loyaltyRepository.InsertLoggingLoyalty(dataLoyalty);
                kibanaService.LogResponse("RevertPointToUpCapillary", transaction.TerminalId, 0, "", $"{transaction.CardNumber}-{dataLoyalty.Request}-{dataLoyalty.Response}");
                StringHelper.Loggingz(JsonConvert.SerializeObject(topUpResult));
                return topUpResult != null && topUpResult.Item1;
            }
            catch (Exception ex)
            {
                kibanaService.LogException("RevertPointToUpCapillary", "", 0, "", JsonConvert.SerializeObject(ex));
                return false;
            }
        }
    }
}