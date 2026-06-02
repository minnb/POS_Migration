using Confluent.Kafka;
using Dapper;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.Loyalty.ProgramPoints;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using VCM.POSBLUE.Model.VINID;

namespace TCX.WebApiCore.AppServices
{
    public class ProgramPointsService
    {
        private readonly KibanaService _kibana;
        private readonly RedisManager dbRedis;
        private readonly string ProgramPointsRemnItem = $"BLUEPOS:ProgramPointsRemnItem";
        private readonly string ProgramPointsRemn = $"BLUEPOS:ProgramPointsRemn";
        public ProgramPointsService()
        {
            _kibana = new KibanaService();
            dbRedis = new RedisManager();
        }
        private async Task<List<ProgramPointsRemnItem>> GetListRemnItem(string storeNo, string clubCode)
        {
            try
            {
                var programPointsRemnItem = await dbRedis.StringGetAsync<List<ProgramPointsRemnItem>>(ProgramPointsRemnItem);
                if (programPointsRemnItem != null && programPointsRemnItem.Count > 0) 
                { 
                    return programPointsRemnItem;
                }

                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    string query = @"SELECT *
			                      FROM ProgramPointsRemnItem (NOLOCK)
			                      WHERE StoreNo = @StoreNo AND ClubCode = @ClubCode;";
                    var data =  db.Query<ProgramPointsRemnItem>(query, new { StoreNo = storeNo, ClubCode = clubCode }).ToList();
                    dbRedis.StringSet<List<ProgramPointsRemnItem>>(ProgramPointsRemnItem, data);
                    return data;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetProgramPointsRemn.Exception", ex);
                return null;
            }
        }
        private List<ProgramPointsRemn> GetProgramPointsRemn(string phoneNumber)
        {
            try
            {
                var dataRedis = dbRedis.HashGet<List<ProgramPointsRemn>>(ProgramPointsRemn, phoneNumber, true);
                if (dataRedis != null) return dataRedis;

                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    string query = @" SELECT R.*
			                      FROM [ProgramPointsRemn] (NOLOCK) R
			                      INNER JOIN
				                       (SELECT ClubCode, MemberCard, Max(Id) AS Id
				                        FROM [ProgramPointsRemn] (NOLOCK)
				                        GROUP BY ClubCode, MemberCard) T ON R.ClubCode = T.ClubCode AND R.MemberCard = T.MemberCard AND R.Id = T.Id
			                      WHERE R.AvailablePoints >= 0; ";

                    var dataDB = db.Query<ProgramPointsRemn>(query).ToList();
                    var dataOfPhoneNumber = dataDB.Where(x => x.MemberCard == phoneNumber).ToList();
                    if (dataDB != null && dataDB.Count > 0 && !dbRedis.CheckKeyExists(ProgramPointsRemn))
                    {
                        var groupedData = from record in dataDB
                                          group record by record.MemberCard into grouped
                                          select new
                                          {
                                              MemberCard = grouped.Key
                                          };
                        foreach (var memberCard in groupedData) 
                        {
                            var data = dataDB.Where(x => x.MemberCard == memberCard.MemberCard).ToList();
                            if(data!= null && data.Count > 0) dbRedis.HashSet<List<ProgramPointsRemn>>(ProgramPointsRemn, memberCard.MemberCard, data, 31000000);
                        }
                    }
                    if (dataOfPhoneNumber != null && dataOfPhoneNumber.Count > 0) 
                    {
                        dbRedis.HashSet<List<ProgramPointsRemn>>(ProgramPointsRemn, phoneNumber, dataOfPhoneNumber, 31000000);
                    }
                    return dataOfPhoneNumber;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs($"{phoneNumber} GetProgramPointsRemn.Exception", ex);
                return null;
            }
        }
        private List<ProgramPointsDto> GetProgramPoints(MemoryCacheService _memoryCacheService)
        {
            try
            {
                var dataCache = _memoryCacheService.Get<List<ProgramPointsDto>>(MemoryCacheConst.MemoryCacheProgramPoints);
                if (dataCache == null)
                {
                    List<ProgramPointsDto> result = new List<ProgramPointsDto>();
                    using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                    {
                        db.Open();
                        var programHeader = db.Query<ProgramPointsDto>("SELECT * FROM ProgramPointsSetup (NOLOCK) WHERE Blocked = 0 AND CAST(FromDate AS DATE) <= CAST(getdate() AS DATE) AND CAST(ToDate AS DATE) >= CAST(getdate() AS DATE);").ToList();
                        if (programHeader != null && programHeader.Count > 0)
                        {
                            var programId = programHeader.Select(x => x.ProgramId).ToArray();

                            List<ProgramPointsItemDto> items = new List<ProgramPointsItemDto>();
                            List<ProgramPointsStoreDto> stores = new List<ProgramPointsStoreDto>();

                            //check store setup
                            var programStore = db.Query<ProgramPointsSetupStore>("SELECT * FROM ProgramPointsSetupStore (NOLOCK) WHERE Blocked = 0 AND ProgramId IN @programId", new { programId }).ToList();
                            foreach (var s in programStore)
                            {
                                stores.Add(new ProgramPointsStoreDto()
                                {
                                    ProgramId = s.ProgramId,
                                    StoreNo = s.StoreNo,
                                    LimitedQty = s.LimitedQty,
                                });
                            }
                            //check items setup
                            var programItem = db.Query<ProgramPointsSetupItem>("SELECT * FROM ProgramPointsSetupItem (NOLOCK) WHERE Blocked = 0 AND ProgramId IN @programId", new { programId }).ToList();
                            foreach (var i in programItem)
                            {
                                items.Add(new ProgramPointsItemDto()
                                {
                                    ProgramId = i.ProgramId,
                                    ItemNo = i.ItemNo,
                                    Uom = i.Uom,
                                    Qty = i.Qty,
                                    Rate = i.Rate
                                });
                            }

                            if (programStore.Count > 0 && programItem.Count > 0)
                            {
                                foreach (var item in programHeader)
                                {
                                    result.Add(new ProgramPointsDto()
                                    {
                                        ProgramId = item.ProgramId,
                                        ProgramName = item.ProgramName,
                                        ProgramType = item.ProgramType,
                                        ClubCode = item.ClubCode,
                                        CumulativeType = item.CumulativeType,
                                        ToDate = item.ToDate,
                                        FromDate = item.FromDate,
                                        Items = items.Where(x => x.ProgramId == item.ProgramId).ToList(),
                                        Stores = stores.Where(x => x.ProgramId == item.ProgramId).ToList()
                                    });
                                }
                            }
                        }
                    }

                    //return [] if not exists program
                    _memoryCacheService.Set(MemoryCacheConst.MemoryCacheProgramPoints, result);
                    return result;
                }
                return dataCache;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetProgramPoints.Exception", ex);
                _kibana.LogException("Exception", "", 0, "", "GetProgramPoints: " + JsonConvert.SerializeObject(ex));
                return null;
            }
        }
        public async Task<List<ProgramPointData>> GetProgramPointsSummaries(MemoryCacheService _memoryCacheService, string storeNo, string phoneNumber)
        {
            try
            {
                phoneNumber = FormatHelper.PhoneNumberVietNam(phoneNumber);
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();
                    var programPoints = GetProgramPoints(_memoryCacheService);
                    if (programPoints == null) return null;

                    //check store
                    List<ProgramPointsDto> programPointsRunning = new List<ProgramPointsDto>();
                    for (int i = programPoints.Count - 1; i >= 0; i--)
                    {
                        var item = programPoints[i];
                        var storeRunning = item.Stores.FirstOrDefault(x => x.StoreNo == storeNo || x.StoreNo.ToUpper() == "ALL");
                        if (storeRunning == null)
                        {
                            programPoints.RemoveAt(i);
                        }
                    }

                    //check member
                    if (programPoints != null && programPoints.Count > 0)
                    {
                        var result = await MappingProgramPoint(programPoints, storeNo);
                        var getPointRemn = GetProgramPointsRemn(phoneNumber);
                        if (getPointRemn != null && getPointRemn.Count > 0)
                        {
                            foreach (var item in result)
                            {
                                var pointClub = getPointRemn.FirstOrDefault(x=>x.ClubCode == item.ClubCode);
                                if (pointClub != null)
                                {
                                    item.AvailablePoints = pointClub.AvailablePoints;
                                    item.CommitPoints = pointClub.CommitPoints;
                                    item.PreviousPoint = pointClub.PreviousPoint;
                                }
                            }
                        }
                        return result;
                    }
                }
                return null;
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("GetProgramPointsSummaries.Exception", ex);
                return null;
            }
        }
        private async Task<List<ProgramPointData>> MappingProgramPoint(List<ProgramPointsDto> data, string storeNo)
        {
            List<ProgramPointData> result = new List<ProgramPointData>();
            foreach (var item in data)
            {
                if(item.ProgramType == UsePointEnum.REDEEM.ToString())
                {
                    var remnItem = await GetListRemnItem(storeNo, item.ClubCode);
                    foreach(var i in item.Items)
                    {
                        if (remnItem != null && remnItem.Count > 0)
                        {
                            var checkLitmitedQty = remnItem.FirstOrDefault(x=>x.ItemNo == i.ItemNo && x.Uom == i.Uom);
                            if(checkLitmitedQty != null)
                            {
                                i.LimitedQty = checkLitmitedQty.LimitedQty - checkLitmitedQty.UsedQty;
                            }
                            else 
                            { 
                                i.LimitedQty = 99999;
                            }
                            
                        }
                        else
                        {
                            i.LimitedQty = 99999;
                        }
                    }
                }

                result.Add(new ProgramPointData()
                {
                    ProgramType = item.ProgramType,
                    ProgramId = item.ProgramId,
                    ProgramName = item.ProgramName,
                    CumulativeType = item.CumulativeType,
                    PreviousPoint = 0,
                    AvailablePoints = 0,
                    CommitPoints = 0,
                    ClubCode = item.ClubCode,
                    ToDate = item.ToDate,
                    FromDate = item.FromDate,
                    Items = item.Items.ToList()
                });
            }
            return result;
        }
        public async Task<(bool, string, List<ProgramPointsTransactionSuccess>)> AddTransactionProgramPoints(MemoryCacheService _memoryCacheService, ProgramPointsTransactionDto request)
        {
            List<ProgramPointsTransactionSuccess> resultPoints = new List<ProgramPointsTransactionSuccess>();
            var phoneNumber = FormatHelper.PhoneNumberVietNam(request.MemberCard);
            try
            {
                string queryIns = @"INSERT INTO [dbo].[ProgramPointsTransaction] " +
                            " ([StoreNo],[PosNo],[Type],[ProgramId],[ClubCode],[MemberCard],[OrderNo],[TransactionType],[TotalPoint],[ReturnedOrderNo],[CrtDate]) " +
                            " VALUES(@StoreNo,@PosNo,@Type,@ProgramId,@ClubCode,@MemberCard,@OrderNo,@TransactionType,@TotalPoint,@ReturnedOrderNo,@CrtDate);";

                var getPointsSummaries = await GetProgramPointsSummaries(_memoryCacheService, request.StoreNo, request.MemberCard);
                if(getPointsSummaries == null)
                {
                    return (false, string.Format(@"Không có chương trình khuyến mãi Hội viện {0}", request.MemberCard), null);
                }
                var programIdRequest = request.Items.Select(x=>x.ProgramId).ToList();
                if (!request.Items.Select(x => x.ProgramId).ToList().All(item =>  getPointsSummaries.Select(x=>x.ProgramId).Contains(item)))
                {
                    return (false, string.Format(@"Chương trình khuyến mãi trong đơn hàng {0} không khớp", JsonConvert.SerializeObject(programIdRequest)), null);
                }

                //mapping result
                var transPointsResult = MappingTransactionPoints(request, getPointsSummaries);
                if(transPointsResult.Item1.Count == 0)
                {
                    return (false, "Không có sản phẩm nào được tích - tiêu điểm", null);
                }

                //process
                //var dapper = new DapperContext(_memoryCacheService.GetStringDbLoyalty());
                using (IDbConnection db = DapperLoyaltyFactory.CreateConnection())
                {
                    db.Open();

                    //check order return
                    if(request.TransactionType == 2)
                    {
                        string queryCheck = @"SELECT * FROM ProgramPointsTransaction (NOLOCK) WHERE OrderNo = '" + request.ReturnedOrderNo + "';";
                        var checkOrderNo = db.Query<ProgramPointsTransaction>(queryCheck).FirstOrDefault();
                        if(checkOrderNo == null)
                        {
                            return (false, string.Format(@"Không tìm thấy đơn hàng gốc {0}", request.ReturnedOrderNo), null);
                        }
                    }

                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            //redeem points                           
                            var transationRedeem = transPointsResult.Item1.Where(x => x.Type == UsePointEnum.REDEEM.ToString()).ToList();
                            if (transationRedeem != null && transationRedeem.Count > 0)
                            {
                                var pointsRemnInDbByPhone = db.Query<ProgramPointsRemn>(@"SELECT * FROM ProgramPointsRemn WITH (ROWLOCK, UPDLOCK) WHERE AvailablePoints > 0 AND MemberCard = @MemberCard;", new { request.MemberCard }, transaction).ToList();
                                if(pointsRemnInDbByPhone == null || pointsRemnInDbByPhone.Count == 0)
                                {
                                    return (false, string.Format(@"Số thẻ Hội viên {0} không có đủ điểm để đổi quà", request.MemberCard), null);
                                }
                                //check điểm đổi quà
                                foreach(var item in transationRedeem)
                                {
                                    var rempPointOfClub = pointsRemnInDbByPhone.FirstOrDefault(x=>x.ClubCode == item.ClubCode);
                                    if(rempPointOfClub == null) return (false, string.Format(@"Số thẻ Hội viên {0} không có đủ điểm để đổi quà theo chương trình {1}", request.MemberCard, item.ClubCode), null);
                                    if(rempPointOfClub.AvailablePoints < item.TotalPoint * (-1))
                                    {
                                        return (false, string.Format(@"Số thẻ Hội viên {0} có {1} điểm, không đủ đổi quà", request.MemberCard, ((int)rempPointOfClub.AvailablePoints)), null);
                                    }
                                }

                                var itemRemnInDbByStore = db.Query<ProgramPointsRemnItem>(@"SELECT * FROM ProgramPointsRemnItem WITH (ROWLOCK, UPDLOCK) WHERE StoreNo = @StoreNo;", new { request.StoreNo }, transaction).ToList();
                                if(itemRemnInDbByStore != null && itemRemnInDbByStore.Count > 0)
                                {
                                    var programPointsRemnItem = CheckItemGiftt(request, transPointsResult.Item1, itemRemnInDbByStore, pointsRemnInDbByPhone);
                                    if (programPointsRemnItem.Item1 && programPointsRemnItem.Item3.Count > 0)
                                    {
                                        string queryUpdate = "UPDATE ProgramPointsRemnItem SET UsedQty = UsedQty + @UsedQty " +
                                                            " WHERE ClubCode = @ClubCode AND StoreNo = @StoreNo AND ItemNo = @ItemNo AND Uom = @Uom; ";
                                        foreach (var item in programPointsRemnItem.Item3)
                                        {
                                            db.Execute(queryUpdate, item, transaction);
                                        }
                                        dbRedis.Delete(ProgramPointsRemnItem);
                                    }
                                    else if (!programPointsRemnItem.Item1)
                                    {
                                        return (false, programPointsRemnItem.Item2, null);
                                    }
                                }
                            }
                            //earn points
                            db.Execute(queryIns, transPointsResult.Item1, transaction: transaction);
                            transaction.Commit();
                            dbRedis.HashDelete(ProgramPointsRemn, phoneNumber);
                            resultPoints = GetTransactionCommit(transPointsResult.Item1, getPointsSummaries, GetProgramPointsRemn(phoneNumber), transPointsResult.Item2);
                        }
                        catch(Exception ex) 
                        {
                            transaction.Rollback();
                            FileHelper.WriteExpLogs("AddTransactionProgramPoints.Rollback.Exception", ex);
                            _kibana.LogException("Exception", "", 0, "", "AddTransactionProgramPoints: " + JsonConvert.SerializeObject(ex));
                            return (false, "Lỗi tích điểm, vui lòng thử lại", null);
                        }
                    }
                }

                return (true, "Thành công", resultPoints) ;
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("AddTransactionProgramPoints.Exception", ex);
                return (false, ex.Message.ToString(), null);
            }
        }
        private List<ProgramPointsTransactionSuccess> GetTransactionCommit(List<ProgramPointsTransaction> programPointsTransactions, List<ProgramPointData> pointSummaries, List<ProgramPointsRemn> remnPoints, List<ProgramPointsTransactionItemResponse> itemsResult)
        {
            List<ProgramPointsTransactionSuccess> result = new List<ProgramPointsTransactionSuccess>();
            foreach(var item in programPointsTransactions)
            {
                var programInfo = pointSummaries.FirstOrDefault(x => x.ProgramId == item.ProgramId);
                var remnPointItem = remnPoints.FirstOrDefault(x => x.ClubCode == item.ClubCode);
                result.Add(new ProgramPointsTransactionSuccess()
                {
                    ClubCode = item.ClubCode,
                    ProgramName = programInfo!= null ? programInfo.ProgramName : item.ClubCode,
                    EarnPoint = item.Type == "EARN" ? item.TotalPoint : 0,
                    RedeemPoint = item.Type == "REDEEM" ? item.TotalPoint * (-1) : 0,
                    AvailablePoints = remnPointItem!= null ? remnPointItem.AvailablePoints : 0,
                    Items = itemsResult.Where(x=>x.ProgramId == item.ProgramId).ToList(),
                });
            }
            
            return result;
        }
        private (List<ProgramPointsTransaction>, List<ProgramPointsTransactionItemResponse>) MappingTransactionPoints(ProgramPointsTransactionDto data, List<ProgramPointData> programPointDatas)
        {
            List<ProgramPointsTransaction> result = new List<ProgramPointsTransaction>();
            List<ProgramPointsTransactionItemResponse> itemResult = new List<ProgramPointsTransactionItemResponse>();
            //lọc bỏ Item không thuộc khuyến mãi
            for (int i = data.Items.Count - 1; i >= 0; i--)
            {
                var item = data.Items[i];
                var lstItem = programPointDatas.FirstOrDefault(x=>x.ProgramId == item.ProgramId);
                if (lstItem != null && lstItem.Items.FirstOrDefault(x => x.ItemNo == item.ItemNo && x.Uom == item.Uom) == null)
                {
                    data.Items.RemoveAt(i);
                }
            }

            foreach (var item in data.Items)
            {
                var itemData = programPointDatas.FirstOrDefault(x => x.ProgramId == item.ProgramId).Items.FirstOrDefault(x=>x.ItemNo == item.ItemNo && x.Uom == item.Uom);

                item.Rate = itemData.Rate;
                item.TotalPoint = item.Qty * itemData.Rate;
            }

            var items = data.Items
                    .GroupBy(item => new { item.ProgramId })
                    .Select(group => new
                    {
                        group.Key.ProgramId,
                        TotalPoint = group.Sum(item => item.TotalPoint)
                    })
                    .ToList();

            foreach (var item in items)
            {
                int point = 1;
                var programType = programPointDatas.FirstOrDefault(x => x.ProgramId == item.ProgramId);
                if (programType != null)
                {
                    if (programType.ProgramType.ToUpper() == UsePointEnum.REDEEM.ToString() && data.TransactionType == 1)
                    {
                        point = -1;
                    }
                    else if(data.TransactionType > 1)
                    {
                        point = -1;
                    }

                    result.Add(new ProgramPointsTransaction()
                    {
                        Type = programType.ProgramType,
                        StoreNo = data.StoreNo,
                        PosNo = data.PosNo,
                        MemberCard = data.MemberCard,
                        TransactionType = data.TransactionType,
                        OrderNo = data.OrderNo,
                        ClubCode = programType.ClubCode,
                        ProgramId = item.ProgramId,
                        TotalPoint = item.TotalPoint * point,
                        ReturnedOrderNo = data.TransactionType > 1 ? data.ReturnedOrderNo : "",
                        CrtDate = DateTime.Now,
                    });

                    foreach(var i in data.Items.Where(x=>x.ProgramId==item.ProgramId).ToList())
                    {
                        itemResult.Add(new ProgramPointsTransactionItemResponse()
                        {
                            LineNo = i.LineNo,
                            ProgramId = i.ProgramId,
                            ProgramType = programType.ProgramType.ToUpper(),
                            ItemNo = i.ItemNo,
                            UnitPrice = i.UnitPrice,
                            Uom =i.Uom,
                            Qty =i.Qty,
                            Rate = i.Rate,
                            TotalPoint = i.Qty * i.Rate * point,
                        });
                    }
                }
            }
            return (result, itemResult);
        }
        private (bool, string, List<ProgramPointsRemnItem>) CheckItemGiftt(ProgramPointsTransactionDto request, List<ProgramPointsTransaction> transPointsResult, List<ProgramPointsRemnItem> remnItemInDatabase, List<ProgramPointsRemn> pointsRemn)
        {
            List<ProgramPointsRemnItem> programPointsRemnItem = new List<ProgramPointsRemnItem>();
            var transationRedeem = transPointsResult.Where(x => x.Type == UsePointEnum.REDEEM.ToString()).ToList();
            if (transationRedeem != null && transationRedeem.Count > 0)
            {
                //check limitedQty
                List<ProgramPointsRemnItem> lstRemnItemInDatabase = new List<ProgramPointsRemnItem>();
                foreach (var trans in transationRedeem)
                {
                    var lstQtyInDb = remnItemInDatabase.Where(x=>x.ClubCode == trans.ClubCode).ToList();
                    if (lstQtyInDb == null || lstQtyInDb.Count == 0)
                    {
                        continue;
                    }

                    foreach (var item in lstQtyInDb)
                    {
                        lstRemnItemInDatabase.Add(item);
                    }

                    var lstItemRedeem = request.Items.Where(x => x.ProgramId == trans.ProgramId).ToList();
                    if (lstItemRedeem == null || lstItemRedeem.Count == 0)
                    {

                    }
                    foreach (var item in lstItemRedeem)
                    {
                        programPointsRemnItem.Add(new ProgramPointsRemnItem()
                        {
                            ItemNo = item.ItemNo,
                            Uom = item.Uom,
                            UsedQty = item.Qty,
                            ClubCode = trans.ClubCode,
                            StoreNo = trans.StoreNo
                        });
                    }
                }

                if (programPointsRemnItem.Count > 0 && lstRemnItemInDatabase.Count > 0)
                {
                    var lstRemnItemCommit = programPointsRemnItem
                           .GroupBy(item => new { item.ClubCode, item.StoreNo, item.ItemNo, item.Uom, item.Rate })
                           .Select(group => new
                           {
                               group.Key.ClubCode,
                               group.Key.StoreNo,
                               group.Key.ItemNo,
                               group.Key.Uom,
                               group.Key.Rate,
                               UsedQty = group.Sum(item => item.UsedQty)
                           }).ToList();

                    programPointsRemnItem.Clear();
                    foreach (var item in lstRemnItemCommit)
                    {
                        var checkPointRemn = pointsRemn.FirstOrDefault(x => x.ClubCode == item.ClubCode);
                        //check points for redeem
                        if (checkPointRemn == null || (checkPointRemn != null && checkPointRemn.AvailablePoints < item.UsedQty * item.Rate))
                        {
                            return (false, string.Format("Hội viên {0} không đủ điểm đổi qua cho chương trình {1}", request.MemberCard, item.ClubCode), null);
                        }

                        var itemInDB = lstRemnItemInDatabase.FirstOrDefault(x => x.ItemNo == item.ItemNo && x.Uom == item.Uom);
                        //check limitedQty
                        if (itemInDB != null)
                        {
                            if (itemInDB.LimitedQty > itemInDB.UsedQty + item.UsedQty)
                            {
                                programPointsRemnItem.Add(new ProgramPointsRemnItem()
                                {
                                    ItemNo = item.ItemNo,
                                    Uom = item.Uom,
                                    ClubCode = item.ClubCode,
                                    StoreNo = item.StoreNo,
                                    UsedQty = item.UsedQty,
                                });
                            }
                            else
                            {
                                return (false, string.Format("Mã hàng {0} đã hết số lượng quà tặng", item.ItemNo + '-' + item.Uom), null);
                            }
                        }
                    }
                }
            }

            return (true, "OK", programPointsRemnItem);
        }
    }
}
