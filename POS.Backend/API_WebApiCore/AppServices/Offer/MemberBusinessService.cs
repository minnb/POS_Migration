using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Dtos.Loyalty.MemberBusiness;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.EntityFrameworkCore.Dapper;
using TCX.WebApiCore.Repository;

namespace TCX.WebApiCore.AppServices
{
    public class MemberBusinessService
    {
        private readonly LoyaltyRepository _loyaltyRepository;
        private readonly MemoryCacheService _memoryCacheService;
        private readonly string _parentKeyMemberBusiness = "MemberBusiness";
        private readonly string _parentKeyMemberRemnItem = "MemberRemnItem";
        private readonly RedisConnectionHelper _redisHelper;
        public MemberBusinessService()
        {
            _loyaltyRepository = new LoyaltyRepository();
            _redisHelper = new RedisConnectionHelper();
            _memoryCacheService = new MemoryCacheService();
        }
        private string CreateKeyMemberBusiness(string storeNo, string posNo, string memberCard, bool isWithParent= false)
        {
            string subKey = string.Join("_", new string[] { storeNo, posNo, memberCard, DateTimeHelper.UnixTimestampString() });
            if (isWithParent)
            {
                return _parentKeyMemberBusiness + ":" + subKey;
                //return _parentKeyMemberBusiness + ":" + storeNo + "_" + posNo + "_" + memberCard + "_" + DateTimeHelper.UnixTimestampString();
            }
            else
            {
                return subKey;
            }
            
        }
        public CheckMemberBusinessData ValidateMemberBusinessDataPayment(string storeNo, string posNo, string memberCard, string key)
        {
            try
            {
                var lstKey = _redisHelper.GetKeys(_parentKeyMemberBusiness);
                bool contains = lstKey.Contains(EncryptionHelper.Base64Decode(key));
                if(contains)
                {
                    var dataOld = _redisHelper.Get<MemberBusinessData>(EncryptionHelper.Base64Decode(key));
                    var checkMemberCard = CheckKeyMemberBusiness(storeNo, posNo, memberCard, true);
                    if (checkMemberCard.Item1 && checkMemberCard.Item2 != null)
                    {
                        if (checkMemberCard.Item2.KeyExists != dataOld.Key)
                        {
                            dataOld.ExistsProcessing = checkMemberCard.Item2;
                        }
                    }
                    return new CheckMemberBusinessData()
                    {
                        IsChecked = true,
                        Description = "Key hợp lệ cho việc thanh toán đơn hàng",
                        MemberBusinessData = dataOld
                    };
                }
                else
                {
                    var newData = GetMemberBusinessItem(storeNo, posNo, memberCard);
                    if (newData.Item1)
                    {
                        return new CheckMemberBusinessData()
                        {
                            IsChecked = false,
                            Description = "Key không hợp lệ, vui lòng sử dụng key mới",
                            MemberBusinessData = newData.Item3
                        };
                    }
                    else
                    {
                        return new CheckMemberBusinessData()
                        {
                            IsChecked = false,
                            Description = "Key không hợp lệ, lấy thông tin HVDN thất bại, vui lòng thử lại",
                            MemberBusinessData = null
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("CheckMemberBusinessData.Exception", ex);
                return null;
            }
        }
        public Tuple<bool,string, MemberBusinessData> GetMemberBusinessItem(string storeNo, string posNo, string memberCard)
        {
            try
            {
                RetryMemberBusinessTransaction();
                Thread.Sleep(10);
                var memberBusinesses = _memoryCacheService.GetMemberBusinesses(_loyaltyRepository.ConnectStringLoyaltyDb());
                if(memberBusinesses == null) { return new Tuple<bool, string, MemberBusinessData>(false, null, null);  }
                var memberBusinessesData = memberBusinesses.FirstOrDefault(x => x.MemberCard == memberCard && x.CardLevel == MemberBusinessesEnum.HVDN.ToString());
                if(memberBusinessesData != null)
                {
                    if(memberBusinessesData.StoreNo != storeNo)
                    {
                        return new Tuple<bool, string, MemberBusinessData>(true, memberBusinessesData.CardLevel, new MemberBusinessData()
                        {
                            CardLevel = memberBusinessesData.CardLevel,
                            Month = DateTime.Now.ToString("yyyyMM"),
                            IsDiscount = false,
                            MemberCard = memberCard,
                            MemberStore = memberBusinessesData.MemberStore,
                            Key = null,
                            ExistsProcessing = null,
                            Items = null
                        });
                    }

                    string monthCurrent = DateTime.Now.ToString("yyyyMM");
                    string key = CreateKeyMemberBusiness(storeNo, posNo, memberCard, true);
                    string queryItem = @"SELECT MemberCard = '" + memberCard + "', A.[Month], A.CardLevel, A.ItemNo, A.Uom, A.MaxValue, IIF(ISNULL(B.RemnValue, 0) > 0, (A.MaxValue - ISNULL(B.RemnValue, 0)), 0) UsedValue, IIF(ISNULL(B.RemnValue, 0) = 0, A.MaxValue, ISNULL(B.RemnValue, 0)) RemnValue " +
                                         " FROM MemberItemDiscount (NOLOCK) A " +
                                         " LEFT JOIN (SELECT T0.* FROM MemberRemnItem (NOLOCK) T0 INNER JOIN (SELECT T2.MemberCard, T2.[Month], T2.CardLevel, T2.ItemNo, T2.Uom, MAX(T2.Id) Id FROM MemberRemnItem (NOLOCK) T2 GROUP BY T2.MemberCard, T2.[Month], T2.CardLevel, T2.ItemNo, T2.Uom) T1 ON T0.Id = T1.Id AND T0.MemberCard = '" + memberCard + "') B ON A.ItemNo = B.ItemNo AND A.Uom = B.Uom " +
                                                                    " AND A.CardLevel = B.CardLevel AND B.MemberCard = '" + memberCard + "' AND B.[Month] = '" + monthCurrent + "'" +
                                         "WHERE A.MaxValue > 0 AND A.Blocked = 0 AND A.[Month] = '" + monthCurrent + "';";
                         
                    var dapper = new DapperContext(_loyaltyRepository.ConnectStringLoyaltyDb());
                    using (IDbConnection db = dapper.CreateConnDB)
                    {
                        var dataItem = db.Query<MemberBusinessItemDto>(queryItem).ToList();

                        var memberBusinessData = MappingMemberBusinessData(dataItem, memberBusinessesData.MemberStore, "HVDN", memberCard, monthCurrent);                       
                        var checkMemberCard = CheckKeyMemberBusiness(storeNo, posNo, memberCard);
                        memberBusinessData.Key = EncryptionHelper.Base64Encode(key);
                        memberBusinessData.IsDiscount = true;

                        _redisHelper.Set<MemberBusinessData>(CreateKeyMemberBusiness(storeNo, posNo, memberCard), memberBusinessData, _parentKeyMemberBusiness, DateTimeHelper.GetTimeUntilMidnight());
                        if (checkMemberCard.Item1 && checkMemberCard.Item2 != null)
                        {
                            if(checkMemberCard.Item2.KeyExists != memberBusinessData.Key){
                                memberBusinessData.ExistsProcessing = checkMemberCard.Item2;
                            }
                        }
                        return new Tuple<bool, string, MemberBusinessData>(true, memberBusinessesData.CardLevel, memberBusinessData);
                    }
                }
                else
                {
                    return new Tuple<bool, string, MemberBusinessData>(false, null, null);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("GetMemberBusinessItem.Exception", ex);
                return new Tuple<bool, string, MemberBusinessData>(false, null, null);
            }
        }   
        public Tuple<bool, string> MemberBusinessTransaction(MemberBusinessRequest salesRequest)
        {
            try
            {
                salesRequest.CardNumber = FormatHelper.PhoneNumberVietNam(salesRequest.CardNumber);
                //check key
                var lstKey = _redisHelper.GetKeys(_parentKeyMemberBusiness);
                bool contains = lstKey.Contains(EncryptionHelper.Base64Decode(salesRequest.Key));
                if (!contains)
                {
                    return new Tuple<bool, string>(false, string.Format(@"POS {0} không tìm thấy phiên làm việc với HVDN", salesRequest.PosNo));
                }
                //check line HVDN
                var checkBill = salesRequest.TransLine.FirstOrDefault(x => x.CardLevel == MemberBusinessesEnum.HVDN.ToString());
                if (checkBill == null)
                {
                    return new Tuple<bool, string>(false, string.Format(@"Hóa đơn {0} mua hàng không thỏa mã điều kiện HVDN", salesRequest.OrderNo));
                }
                var salesMemberBusinessItem = salesRequest.TransLine.Where(x => x.CardLevel == MemberBusinessesEnum.HVDN.ToString() && x.DiscountAmount > 0).ToList();

                var dataMemberBusinessItem = _redisHelper.GetLastKey<MemberBusinessData>(_parentKeyMemberBusiness, string.Join("_", salesRequest.StoreNo, salesRequest.PosNo, salesRequest.CardNumber));
                FileHelper.WriteLogs(JsonConvert.SerializeObject(dataMemberBusinessItem));
                if(dataMemberBusinessItem == null)
                {
                    return new Tuple<bool, string>(false, string.Format(@"Không tìm thấy Key của máy POS: {0} với HVDN: {1}", salesRequest.PosNo, salesRequest.CardNumber));
                }

                List<MemberRemnItemGroupBy> groupItems = new List<MemberRemnItemGroupBy>();
                if (salesMemberBusinessItem!= null && salesMemberBusinessItem.Count > 0)
                {
                    foreach(var item in salesMemberBusinessItem)
                    {
                        groupItems.Add(new MemberRemnItemGroupBy()
                        {
                            Month = DateTime.Now.ToString("yyyyMM"),
                            MemberCard = salesRequest.CardNumber,
                            CardLevel = item.CardLevel,
                            ItemNo = item.ItemCode,
                            Uom = item.UnitOfMeasure,
                            UsedValue = item.LineAmountIncVAT,
                        });
                    }
                }

                var groupedItems = groupItems
                                    .GroupBy(item => new {item.Month, item.CardLevel, item.ItemNo, item.Uom })
                                    .Select(group => new
                                    {
                                        Month = group.Key.Month,
                                        CardLevel = group.Key.CardLevel,
                                        ItemNo = group.Key.ItemNo,
                                        Uom = group.Key.Uom,
                                        UsedValue = group.Sum(item => item.UsedValue)
                                    })
                                    .ToList();
                
                List<MemberRemnItem> memberRemnItems = new List<MemberRemnItem>();
                int checkItemNotQuota = 0;
                foreach (var item in groupedItems)
                {
                    var itemQuota = dataMemberBusinessItem.Items.FirstOrDefault(x => x.ItemNo == item.ItemNo && x.Uom.ToUpper() == item.Uom.ToUpper());
                    if(itemQuota == null)
                    {
                        FileHelper.WriteLogs(string.Format("{0} Mã hàng {1} không có hạn mức cho thẻ {2}", salesRequest.OrderNo, item.ItemNo, salesRequest.CardNumber));
                        checkItemNotQuota++;
                        continue;
                    }
                    else
                    {
                        if(itemQuota.RemnValue < item.UsedValue)
                        {
                            return new Tuple<bool, string>(false, string.Format("Mã hàng {0} sử dụng {1} vượt hạn mức đang có là {2}", 
                                                                    item.ItemNo, StringHelper.FormatNumberString((int)item.UsedValue), StringHelper.FormatNumberString((int)itemQuota.RemnValue)));
                        }
                        var rem = itemQuota.RemnValue - item.UsedValue;
                        memberRemnItems.Add(new MemberRemnItem()
                        {
                            Month = item.Month,
                            CardLevel = item.CardLevel,
                            ItemNo = item.ItemNo,
                            Uom = item.Uom.ToUpper(),
                            UsedValue = item.UsedValue,
                            MaxValue = itemQuota.MaxValue,
                            RemnValue = rem,
                            CrtDate = DateTime.Now,
                            MemberCard = salesRequest.CardNumber,
                            OrderNo = salesRequest.OrderNo,
                        });
                    }
                }
                string errMess = "";
                if (memberRemnItems != null && memberRemnItems.Count > 0 && _loyaltyRepository.InsertMemberRemnItem(memberRemnItems, _parentKeyMemberRemnItem, ref errMess))
                {
                    _redisHelper.DelAllKeyWithParent(salesRequest.CardNumber, _parentKeyMemberBusiness);
                    return new Tuple<bool, string>(true, @"Trừ hạn mức HVDN thành công");
                }
                else if(checkItemNotQuota > 0)
                {
                    _redisHelper.DelAllKeyWithParent(salesRequest.CardNumber, _parentKeyMemberBusiness);
                    return new Tuple<bool, string>(true, @"Không có sản phẩm đặt hạn mức");
                }

                return new Tuple<bool, string>(false, @"Lỗi trừ hạn mức thẻ HVDN - " + errMess);
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("MemberBusinessTransaction.Exception", ex);
                return new Tuple<bool, string>(false, @"Lỗi ngoại lệ trừ hạn mức thẻ HVDN");
            }
        }
        private MemberBusinessData MappingMemberBusinessData(List<MemberBusinessItemDto> memberBusinessItems, string memberStore, string cardLevel, string memberCard, string month)
        {
            List<MemberBusinessItemQuota> itemQuota = new List<MemberBusinessItemQuota>();
            if(memberBusinessItems != null && memberBusinessItems.Count > 0)
            {
                foreach (var item in memberBusinessItems)
                {
                    itemQuota.Add(new MemberBusinessItemQuota()
                    {
                        ItemNo = item.ItemNo,
                        Uom = item.Uom,
                        MaxValue = item.MaxValue,
                        UsedValue = item.UsedValue,
                        RemnValue = item.RemnValue
                    });
                }
                return new MemberBusinessData()
                {
                    MemberStore = memberStore,
                    CardLevel = memberBusinessItems.FirstOrDefault().CardLevel,
                    MemberCard = memberBusinessItems.FirstOrDefault().MemberCard,
                    Month = memberBusinessItems.FirstOrDefault().Month,
                    Items = itemQuota.ToList()
                };
            }
            else
            {
                return new MemberBusinessData()
                {
                    MemberStore = memberStore,
                    CardLevel = cardLevel,
                    MemberCard = memberCard,
                    Month = month,
                    Items = null
                };
            }
        }
        private void RetryMemberBusinessTransaction()
        {
            try
            {
                string errMess = "";
                var checkDataRedis = _redisHelper.GetKeys(_parentKeyMemberRemnItem);
                if(checkDataRedis != null && checkDataRedis.Count > 0) 
                { 
                    foreach(var key in checkDataRedis)
                    {
                        var dataRemnItem = _redisHelper.Get<List<MemberRemnItem>>(key);
                        if(dataRemnItem != null && dataRemnItem.Count > 0)
                        {
                            _loyaltyRepository.InsertMemberRemnItem(dataRemnItem, _parentKeyMemberRemnItem, ref errMess);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("RetryMemberBusinessTransaction.Exception", ex);
            }
        }
        private Tuple<bool, ExistsProcessing> CheckKeyMemberBusiness(string storeNo, string posNo, string memberCard, bool isValidate = false)
        {
            string keyCheck = string.Join("_", new string[] { storeNo, posNo, memberCard });
            try
            {
                var lstKey = _redisHelper.GetKeys(_parentKeyMemberBusiness);
                if(lstKey.Count > 0)
                {
                    foreach (var key in lstKey)
                    {
                        if (key.Contains(keyCheck) && isValidate == false)
                        {
                            _redisHelper.Delete(key);
                        }
                        else
                        {
                            if (key.Contains(memberCard))
                            {
                                string PosNo = "";
                                var arrKey = StringHelper.ConverStringToArray(key, ':');
                                if (arrKey.Length >= 1)
                                {
                                    if (StringHelper.ConverStringToArray(arrKey[1], '_').Length >= 1)
                                    {
                                        PosNo = StringHelper.ConverStringToArray(arrKey[1], '_')[1];
                                    }
                                }
                                return Tuple.Create(true, new ExistsProcessing { KeyExists = EncryptionHelper.Base64Encode(key), PosNo = PosNo });
                            }
                        }
                    }
                }

                return new Tuple<bool, ExistsProcessing>(false, null);
            }
            catch(Exception ex)
            {
                FileHelper.WriteExpLogs("CheckKeyMemberBusiness.Exception: " + keyCheck, ex);
                return new Tuple<bool, ExistsProcessing>(false, null);
            }
        }
        public Tuple<bool, string> DeleteMemberRemnItem(string orderNo, string memberCard)
        {
            var checkRefund = _loyaltyRepository.RefundMemberRemnItem(orderNo, memberCard);
            
            return new Tuple<bool, string>(checkRefund.Item1, checkRefund.Item2);
        }
    }
}
