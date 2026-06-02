using System.Net;
using Newtonsoft.Json;
using Serilog;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Constants;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Application.Services;

public class OfferService : IOfferService
{
    private readonly IMessageQueueService _messageQueueService;
    private readonly IRedisService _redisService;
    private readonly IOfferRepository _offerRepository;
    
    private const string ConfigValue = "BLUEPOS";
    private readonly string CACHE_KEY_OFFER_STAFF = ConfigValue + ":OfferStaffSetup";
    private readonly string CACHE_KEY_OFFER_STAFF_TRAN = ConfigValue + ":OfferStaffTransaction";
    private readonly string CACHE_KEY_OFFER_STAFF_CHECK = ConfigValue + ":OfferStaffRemn";

    public OfferService(
        IMessageQueueService messageQueueService,
        IRedisService redisService,
        IOfferRepository offerRepository)
    {
        _messageQueueService = messageQueueService;
        _redisService = redisService;
        _offerRepository = offerRepository;
    }

    public async Task ProcessRetryTopUpPointsStaffAsync(VinIDSalesRequest request)
    {
        var message = JsonConvert.SerializeObject(request);
        await _messageQueueService.PublishMessageAsync(
            RabbitMqConstants.Queue_Wincare_TopUpPoints, 
            request.VirtualCard ?? "", 
            message);
    }

    public async Task<string> ProcessTopUpPointsStaffAsync(string queueName, int number, int isRetry)
    {
        // TODO: Port MemberPointsService background jobs (RabbitMQLoyaltyMemberPoints, RetryTopUpPointsToCapillary, etc.)
        // Đây là các job query DB / Redis rồi push hàng loạt vào RabbitMQ, được kích hoạt thủ công qua API.
        // Tạm thời trả về tên queue để client nhận được response thành công như code cũ.
        await Task.CompletedTask;

        if (isRetry == 1)
        {
            return "RetryTopUpPointsToCapillary";
        }

        return queueName.ToLower();
    }

    private async Task<OfferStaffSetupDto?> GetOfferStaffSetupAsync()
    {
        try
        {
            var value = await _redisService.StringGetAsync<OfferStaffSetupDto>(CACHE_KEY_OFFER_STAFF);
            if (value == null)
            {
                var data = await _offerRepository.GetOfferStaffSetupAsync();
                if (data != null)
                {
                    var secondsUntilMidnight = (long)(DateTime.Today.AddDays(1) - DateTime.Now).TotalSeconds;
                    _redisService.StringSet(CACHE_KEY_OFFER_STAFF, data, secondsUntilMidnight);
                    return data;
                }
            }
            return value;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetOfferStaffSetupAsync");
            return null;
        }
    }

    public async Task<(HttpStatusCode Status, OfferStaffRemnDto? Data, string Message)> OfferStaffCheckAsync(
        string storeNo, string posNo, string phoneNumber)
    {
        try
        {
            // FormatHelper.PhoneNumberVietNam logic: nếu bắt đầu bằng 84 thì thay bằng 0
            if (phoneNumber.StartsWith("84") && phoneNumber.Length == 11)
                phoneNumber = "0" + phoneNumber.Substring(2);

            var getOfferSetup = await GetOfferStaffSetupAsync();
            if (getOfferSetup == null)
            {
                return (HttpStatusCode.NotFound, null, "Không tìm thấy chương trình ưu đãi cho CBNV MASAN");
            }

            var dataRemn = await _offerRepository.GetOfferStaffRemnAsync("", phoneNumber, getOfferSetup.ClubCode ?? "");
            if (dataRemn == null)
            {
                return (HttpStatusCode.NotFound, null, $"Số điện thoại {phoneNumber} không thuộc CBNV {getOfferSetup.ClubCode}");
            }

            var valueRedis = new CheckOfferStaffRemn
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

            if (posNo != "999999") // pos test không lưu cache
            {
                _redisService.HashSet(CACHE_KEY_OFFER_STAFF_CHECK, valueRedis.PhoneNumber ?? "", valueRedis);
            }

            return (HttpStatusCode.OK, dataRemn, "OK");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{Phone}_OfferStaffCheckAsync", phoneNumber);
            return (HttpStatusCode.ExpectationFailed, null, ex.Message);
        }
    }

    public async Task<(HttpStatusCode Status, string Message)> OfferStaffTransactionAsync(
        OfferStaffTransactionRequest request)
    {
        try
        {
            decimal initQuota = 0;
            var checkPhone = await _redisService.HashGetAsync<CheckOfferStaffRemn>(CACHE_KEY_OFFER_STAFF_CHECK, request.PhoneNumber ?? "");
            
            if (checkPhone == null)
            {
                if (request.TransactionType == 1)
                {
                    return (HttpStatusCode.NotFound, $"Số điện thoại {request.PhoneNumber} chưa kiểm tra số dư ưu đãi {request.ClubCode}");
                }
            }
            else if (request.TransactionType == 1)
            {
                initQuota = checkPhone.InitQuota;
                if (request.PosNo != checkPhone.PosNo)
                {
                    return (HttpStatusCode.BadRequest, $"Số điện thoại {request.PhoneNumber} đang xử lý ở POS {checkPhone.PosNo}, vui lòng thử lại sau");
                }
                
                if (checkPhone.ExpiryTime < DateTime.Now)
                {
                    return (HttpStatusCode.BadRequest, $"Số điện thoại {request.PhoneNumber} tại pos {checkPhone.PosNo} đã hết thời gian xử lý, vui lòng kiểm tra lại số dư");
                }
                
                if (checkPhone.RemainQuota < request.Amount)
                {
                    return (HttpStatusCode.BadRequest, $"Số điện thoại {request.PhoneNumber} số dư {checkPhone.RemainQuota:N0} không đủ sử dụng");
                }
            }

            // Check retry insert
            var checkStaffTransError = await _redisService.HashGetAsync<OfferStaffTransactionDto>(CACHE_KEY_OFFER_STAFF_TRAN, request.PhoneNumber ?? "");
            if (checkStaffTransError != null)
            {
                Log.Warning("OfferStaffTransaction retry: {OrderNo} - {PosNo}", checkStaffTransError.OrderNo, checkStaffTransError.PosNo);
                var processRetry = await _offerRepository.InsertOfferStaffTransactionAsync(checkStaffTransError);
                if (processRetry.Success)
                {
                    return (HttpStatusCode.OK, processRetry.Message);
                }
                return (HttpStatusCode.Conflict, $"Số điện thoại {request.PhoneNumber} đang lỗi xử lý số dư ưu đãi {checkStaffTransError.ClubCode}, vui lòng thử lại");
            }

            var dataInsert = new OfferStaffTransactionDto
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
                Amount = request.TransactionType == 1 ? request.Amount : request.Amount * -1,
                ReturnedOrderNo = request.ReturnedOrderNo,
                CrtDate = DateTime.Now
            };

            var checkIns = await _offerRepository.InsertOfferStaffTransactionAsync(dataInsert);
            if (!checkIns.Success && checkIns.Message.Contains("SqlException"))
            {
                return (HttpStatusCode.BadRequest, checkIns.Message);
            }
            else if (!checkIns.Success)
            {
                _redisService.HashSet(CACHE_KEY_OFFER_STAFF_TRAN, dataInsert.PhoneNumber ?? "", dataInsert);
            }

            _redisService.HashDelete(CACHE_KEY_OFFER_STAFF_CHECK, request.PhoneNumber ?? "");
            return (HttpStatusCode.OK, "OK");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OfferStaffTransactionAsync");
            return (HttpStatusCode.ExpectationFailed, ex.Message);
        }
    }
}
