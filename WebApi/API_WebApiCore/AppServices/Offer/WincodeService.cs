using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TCX.API.Common.Dtos.Loyalty.WinCode;
using TCX.API.Common.Shared;
using VCM.POSBLUE.Model.WinLife;

namespace TCX.WebApiCore
{
    public enum WinCodeApplyType
    {
        BY_STORE = 1,
        BY_QTY = 0
    }
    public class WincodeService
    {
        private readonly MemoryCacheService _memoryCacheService;
        private readonly KibanaService _kibanaService;
        private readonly WincodeRepository _wincodeRepository;
        public WincodeService()
        {
            _memoryCacheService = new MemoryCacheService();
            _kibanaService = new KibanaService();
            _wincodeRepository = new WincodeRepository();
        }
        public (bool, string, List<WincodeResult>) GetWinCodeByStore(string merchantId, string storeNo, string phoneNumber)
        {
            try
            {
                phoneNumber = FormatHelper.PhoneNumberVietNam(phoneNumber);
                var winCodeData = _memoryCacheService.GetWinCodeMemory(_memoryCacheService.GetConnectStringCentralMD());
                if (winCodeData == null)
                {
                    return (false, "Không có chương trình Wincode", null);
                }

                // Fix Bug 1: Không mutate object trong MemoryCache (shared state) – dùng Where filter thay vì gán StoreNo
                // Trước đây: foreach mutate wincode.StoreNo = storeNo → race condition khi nhiều POS gọi đồng thời
                var dataWincodeByStore = winCodeData
                    .Where(x => x.StoreNo == storeNo || x.StoreNo == "ALL")
                    .ToList();

                // Fix Bug 2: .ToList() không bao giờ trả về null – kiểm tra Count thay vì null
                if (dataWincodeByStore.Count == 0)
                {
                    return (false, "Không có chương trình Wincode cho mã cửa hàng " + storeNo, null);
                }
                
                //check apply
                var checkExistsBill = _wincodeRepository.GetWinCodeCustomer(phoneNumber);
                List<WincodeResult> dataResult = new List<WincodeResult>();
                foreach(var item in dataWincodeByStore)
                {
                    int qty = 0;
                    WinCodeCustomerDto checkQty = null;
                    if(item.ApplyType == WinCodeApplyType.BY_QTY.ToString() && checkExistsBill != null)
                    {
                        checkQty = checkExistsBill.Where(x => x.WinCode == item.WinCode).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                    }
                    else if (item.ApplyType == WinCodeApplyType.BY_STORE.ToString() && checkExistsBill != null)
                    {
                        checkQty = checkExistsBill.Where(x => x.WinCode == item.WinCode && x.StoreNo == item.StoreNo).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                    }

                    if(checkQty != null)
                    {
                        qty = checkQty.QuantityRecieptedSum;
                    }
                    if(qty == item.Quantity) { continue; }

                    if(item.Quantity - qty > 0)
                    {
                        dataResult.Add(new WincodeResult()
                        {
                            WinCode = item.WinCode,
                            ProgramCode = item.ProgramCode,
                            Quantity = item.Quantity - qty,
                            DiscountType = item.DiscountType,
                            MerchantId = merchantId
                        });
                    }
                }
                return (true, "OK", dataResult);
            }
            catch (Exception ex)
            {
                return (false, ex.Message.ToString(), null);
            }
        }
        public (bool, string, object) InsertWinCodeCustomer(WinLife_UpdatePromotions_POS_Request request)
        {
            try
            {
                Tuple<bool, string> result = new Tuple<bool, string>(false, null);
                long milliseconds;
                var st1 = new Stopwatch();
                st1.Start();

                request.phoneNumber = FormatHelper.PhoneNumberVietNam(request.phoneNumber);
                var winCodeData = _memoryCacheService.GetWinCodeMemory(_memoryCacheService.GetConnectStringCentralMD());
                if (winCodeData == null)
                {
                    return (false, string.Format("Không tìm thấy chương trình WinCode"), null);
                }

                if (request != null && request.action == "A") 
                {
                    var checkExistsBill = _wincodeRepository.GetWinCodeCustomer(request.phoneNumber);
                    
                    foreach (var item in request.appliedCodes)
                    {
                        var checkWincode = winCodeData.Where(x => x.WinCode == item.winCode).FirstOrDefault();
                        if (checkWincode == null)
                        {
                            return (false, string.Format("Không tồn tại chương chình {0}", item.winCode), null);
                        }

                        //var checkQty = checkExistsBill.Where(x=>x.WinCode == item.winCode).OrderByDescending(x=>x.CreatedDate).FirstOrDefault();
                        WinCodeCustomerDto checkQty = null;
                        if (checkWincode.ApplyType == WinCodeApplyType.BY_QTY.ToString())
                        {
                            checkQty = checkExistsBill.Where(x => x.WinCode == item.winCode).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                        }
                        else if (checkWincode.ApplyType == WinCodeApplyType.BY_STORE.ToString())
                        {
                            checkQty = checkExistsBill.Where(x => x.WinCode == item.winCode && x.StoreNo == request.storeCode).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                        }

                        if (checkQty != null && checkWincode != null && checkQty.QuantityRecieptedSum >= checkWincode.Quantity)
                        {
                            return (false, $"Số thẻ hội viên {request.phoneNumber} đã nhận đủ số lượng {checkWincode.Quantity} quà tặng tại cửa hàng {request.storeCode}", null);
                        }
                    }
                    result = Task.Run(()=> _wincodeRepository.InsertWincodeCustomer(request)).Result;
                }
                else if(request.action == "I") 
                {
                    result = Task.Run(() => _wincodeRepository.UpdateWincodeCustomer(request)).Result;
                }

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();
                _kibanaService.LogResponse("WinCodeCustomer", request.posCode, milliseconds, "", JsonConvert.SerializeObject(result));
                return (result.Item1, result.Item2, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message.ToString(), null);
            }
        }
    }
}