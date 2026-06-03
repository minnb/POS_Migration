using Newtonsoft.Json;
using Serilog;
using System;
using System.Net;
using TCX.API.Common.Dtos;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;

namespace TCX.WebApiCore.AppServices
{
    public class OfferEmployeeService
    {
        public static string ConfigValue;
        private readonly OfferStaffRepository _offerStaffRepository;
        public OfferEmployeeService()
        {
            _offerStaffRepository = new OfferStaffRepository();
        }
        static OfferEmployeeService()
        {
            ConfigValue = "BLUEPOS";
        }
        private readonly string CACHE_KEY_OFFER_STAFF = ConfigValue + ":OfferStaffSetup";
        private readonly string CACHE_KEY_OFFER_STAFF_TRAN = ConfigValue + ":OfferStaffTransaction";
        private readonly string CACHE_KEY_OFFER_STAFF_CHECK = ConfigValue + ":OfferStaffRemn";
        private OfferStaffSetupDto GetOfferStaffSetup(RedisClusterManager redis)
        {
            try
            {
                var value = redis.StringGet<OfferStaffSetupDto>(CACHE_KEY_OFFER_STAFF);
                if (value == null)
                {
                    var data = _offerStaffRepository.GetOfferStaffSetup();
                    if (data != null)
                    {
                        redis.StringSet<OfferStaffSetupDto>(CACHE_KEY_OFFER_STAFF, data, (long)DateTimeHelper.GetTimeUntilMidnight().TotalSeconds);
                        return data;
                    }
                }
                return value;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetOfferStaffSetup.Exception");
                FileHelper.WriteExpLogs($"GetOfferStaffSetup.Exception:", ex);
                return null;
            }
        }
        public (HttpStatusCode, OfferStaffRemnDto, string) GetOfferStaffRemnDB(RedisClusterManager redis, string posNo, string phone, string staffCode = "")
        {
            try
            {
                phone = FormatHelper.PhoneNumberVietNam(phone);
                var getOfferSetup = GetOfferStaffSetup(redis);
                if(getOfferSetup == null)
                {
                    return (HttpStatusCode.NotFound, null, "Không tìm thấy chương trình ưu đãi cho CBNV MASAN");
                }

                var dataRemn = _offerStaffRepository.GetOfferStaffRemn(staffCode, phone, getOfferSetup.ClubCode);
                if (dataRemn == null)
                {
                    return (HttpStatusCode.NotFound, null, $"Số điện thoại {phone} không thuộc CBNV {getOfferSetup.ClubCode}");
                }

                var valueRedis = new CheckOfferStaffRemn()
                {
                    ClubCode = dataRemn.ClubCode,
                    Month = dataRemn.Month,
                    PhoneNumber = dataRemn.PhoneNumber,
                    StaffCode = dataRemn.StaffCode,
                    RemainQuota = dataRemn.RemainQuota,
                    InitQuota = dataRemn.InitQuota,
                    ChgDate = dataRemn.ChgDate,
                    CrtDate = dataRemn.CrtDate,
                    PosNo = posNo,
                    ExpiryTime = DateTime.Now.AddMinutes(30)
                };
                if (posNo != "999999") //pos test ko lưu cache
                {
                    redis.HashSet(CACHE_KEY_OFFER_STAFF_CHECK, valueRedis.PhoneNumber, valueRedis);
                }
                return (HttpStatusCode.OK, dataRemn, "OK");
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs($"{phone}_GetOfferStaffRemnDB.Exception:",ex);
                return (HttpStatusCode.ExpectationFailed, null, ex.Message);
            }
        }
        public (HttpStatusCode, string) OfferStaffTransaction(RedisClusterManager redis, OfferStaffTransactionRequest request, KibanaService kibanaService)
        {
            try
            {
                decimal initQuota = 0;
                var checkPhone = redis.HashGet<CheckOfferStaffRemn>(CACHE_KEY_OFFER_STAFF_CHECK, request.PhoneNumber);
                if (checkPhone == null)
                {
                    if(request.TransactionType == 1)
                    {
                        return (HttpStatusCode.NotFound, $"Số điện thoại {request.PhoneNumber} chưa kiểm tra số dư ưu đãi {request.ClubCode}");
                    }
                }
                else if(checkPhone != null && request.TransactionType == 1)
                {
                    initQuota = checkPhone.InitQuota;
                    if (request.PosNo != checkPhone.PosNo) 
                    {
                        return (HttpStatusCode.BadRequest, $"Số điện thoại {request.PhoneNumber} đang xử lý ở POS {checkPhone.PosNo}, vui lòng thử lại sau");
                    }
                    else if(checkPhone.ExpiryTime < DateTime.Now)//check thời gian
                    {
                        return (HttpStatusCode.BadRequest, $"Số điện thoại {request.PhoneNumber} tại pos {checkPhone.PosNo} đã hết thời gian xử lý, vui lòng kiểm tra lại số dư");
                    }
                    else if (checkPhone.RemainQuota < request.Amount)
                    {
                        return (HttpStatusCode.BadRequest, $"Số điện thoại {request.PhoneNumber} số dư {checkPhone.RemainQuota:N0} không đủ sử dụng");
                    }
                }

                //check retry insert
                var checkStaffTransError = redis.HashGet<OfferStaffTransactionDto>(CACHE_KEY_OFFER_STAFF_TRAN, request.PhoneNumber);
                if (checkStaffTransError != null) 
                {
                    kibanaService.LogException("OfferStaffTransaction",checkStaffTransError.PosNo, 0, "", $"{checkStaffTransError.OrderNo} lỗi insert OfferStaffTransaction | {JsonConvert.SerializeObject(checkStaffTransError)}");
                    var processRetry = _offerStaffRepository.InsertOfferStaffTransaction(checkStaffTransError);
                    if (processRetry.Item1)
                    {
                        return (HttpStatusCode.OK, processRetry.Item2);
                    }
                    return (HttpStatusCode.Conflict, $"Số điện thoại {request.PhoneNumber} đang lỗi xử lý số dư ưu đãi {checkStaffTransError.ClubCode}, vui lòng thử lại");
                }

                var dataInsert = new OfferStaffTransactionDto()
                {
                    ClubCode = request.ClubCode,
                    Month = request.OrderDate.ToString("yyyyMM"),
                    InitQuota = initQuota,
                    StoreNo = request.StoreNo,
                    PosNo = request.PosNo,
                    PhoneNumber = request.PhoneNumber,
                    StaffCode = request.StaffCode,
                    OrderNo = request.OrderNo,
                    OrderDate = request.OrderDate,
                    TransactionType = request.TransactionType,
                    Amount = request.TransactionType == 1 ? request.Amount : request.Amount * (-1),
                    ReturnedOrderNo = request.ReturnedOrderNo,
                    CrtDate = DateTime.Now
                };
                var checkIns = _offerStaffRepository.InsertOfferStaffTransaction(dataInsert);
                if (!checkIns.Item1 && checkIns.Item2.Contains("SqlException"))
                {
                    return (HttpStatusCode.BadRequest, checkIns.Item2);
                }
                else if (!checkIns.Item1)
                {
                    redis.HashSet<OfferStaffTransactionDto>(CACHE_KEY_OFFER_STAFF_TRAN, dataInsert.PhoneNumber, dataInsert);
                }
                
                redis.HashDelete(CACHE_KEY_OFFER_STAFF_CHECK, request.PhoneNumber);
                return (HttpStatusCode.OK, "OK");
            }
            catch (Exception ex)
            {
                kibanaService.LogException("OfferStaffTransaction.Exception", request.PosNo, 0, "", $"{request.OrderNo} lỗi insert OfferStaffTransaction | {ex.Message}");
                return (HttpStatusCode.ExpectationFailed, ex.Message);
            }
        }
    }
}