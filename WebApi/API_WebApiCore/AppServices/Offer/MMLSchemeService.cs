using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Dtos.WinX;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.WebApiCore.DbContext;
using TCX.WebApiCore.Repository;

namespace TCX.WebApiCore.AppServices.Offer
{
    public class MMLSchemeService
    {
        private readonly string KeyRedisOrderWinX = "BLUEPOS:WINX_Orders";
        private readonly KibanaService kibanaService;
        private readonly CentralMDRepository centralMDRepository;
        private readonly RedisManager redisManager;
        public MMLSchemeService()
        {
            kibanaService = new KibanaService();
            centralMDRepository = new CentralMDRepository();
            redisManager = new RedisManager();
        }
        public async Task GetOrderDataWinX(MemoryCacheService memoryCacheService, MMLSchemeRequest mMLSchemeRequest)
        {
            try
            {
                var billWinX = new BillWinX()
                {
                    Id = mMLSchemeRequest.OrderNo,
                    Number = mMLSchemeRequest.OrderNo,
                    Date = mMLSchemeRequest.OrderTime != null ? mMLSchemeRequest.OrderTime : DateTime.Now,
                    User = new UserWinX()
                    {
                        UserId = !string.IsNullOrEmpty(mMLSchemeRequest.UserId) ? mMLSchemeRequest.UserId : mMLSchemeRequest.MemberCardNo,
                        Mobile = mMLSchemeRequest.MemberCardNo
                    },
                    Amounts = new AmountWinX()
                    {
                        Gross = mMLSchemeRequest.Items.Sum(x => x.LineAmountIncVAT),
                        Discount = mMLSchemeRequest.Items.Sum(x => x.DiscountAmount),
                        Net = mMLSchemeRequest.Items.Sum(x => x.LineAmountIncVAT)
                    },
                };
                List<LineItemWinX> lineItems = new List<LineItemWinX>();
                foreach (var item in mMLSchemeRequest.Items)
                {
                    if (item.UnitPrice > 0)
                    {
                        lineItems.Add(new LineItemWinX()
                        {
                            LineId = item.LineNo,
                            ItemCode = item.ItemNo,
                            ItemName = item.ItemName ?? "",
                            Quantity = item.Quantity,
                            UOM = item.UOM,
                            Pricing = new PricingWinX()
                            {
                                Amount = item.LineAmountIncVAT,
                                Discount = item.DiscountAmount,
                                UnitPrice = item.UnitPrice,
                                Tax = item.VatAmount
                            },
                        });
                    }
                }
                billWinX.LineItems = lineItems;
                var orderDataWinX = new OrderDataWinXResponse()
                {
                    Bill = billWinX
                };

                var bodyRequest = new SetRedisApiPartnerDto()
                {
                    HashField = $"{mMLSchemeRequest.OrderNo}",
                    Key = KeyRedisOrderWinX,
                    Data = orderDataWinX
                };
                var sysWebApiDto = memoryCacheService.GetSysWebApi(memoryCacheService.GetConnectStringCentralMD()).FirstOrDefault(x => x.AppCode == "PARTNER");
                if (sysWebApiDto == null)
                {
                    FileHelper.WriteExpLogs("MMLSchemeService.GetOrderDataWinX.SysWebApiDto", new Exception("SysWebApiDto PARTNER not found"));
                    return;
                }
                var sendOrderDataWinX = await HttpClientService.CallWebApiPartnerAsync(sysWebApiDto, "SetKeyRedis", "POST", null, StringHelper.ObjectToStringLowercase(bodyRequest));
                StringHelper.Loggingz($"GetOrderDataWinX: {JsonConvert.SerializeObject(sendOrderDataWinX)}");
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("MMLSchemeService.GetOrderDataWinX.Exception", ex);
            }
        }
        public async Task<(MMLSchemeResponse, string)> GetDataMMLSchemeResponse(MemoryCacheService memoryCacheService, MMLSchemeRequest mMLSchemeRequest)
        {
            try
            {
                var mMLSchemeHeader = await centralMDRepository.GetMMLSchemeHeader(redisManager, mMLSchemeRequest.Code);
                if (mMLSchemeHeader == null)
                {
                    return (null, $"MMLSchemeHeader not found");
                }
                var mMLSchemeItems = await centralMDRepository.GetMMLSchemeItem(redisManager);
                if (mMLSchemeItems == null || mMLSchemeItems.Count == 0)
                {
                    return (null, $"MMLSchemeItem not found");
                }

                var filteredItem = mMLSchemeItems.Where(x => x.HeaderCode == mMLSchemeHeader.HeaderCode).FirstOrDefault();
                if (filteredItem == null)
                {
                    return (null, $"Chưa khai báo sản phẩm cho {mMLSchemeRequest.Code}"); ;
                }

                var listItemsInBill = mMLSchemeRequest.Items.Select(x => x.ItemNo + x.UOM);
                var listItemsInScheme = mMLSchemeItems.Where(x => x.HeaderCode == mMLSchemeHeader.HeaderCode)
                                                  .Select(x => x.ItemNo + x.UOM).ToList();
                var setB = new HashSet<string>(listItemsInScheme);

                if (listItemsInBill.Any(item => setB.Contains(item)))
                {
                    string journeyCode = string.Empty;
                    List<string> result = listItemsInBill.Intersect(listItemsInScheme).ToList();
                    var checkWECO = mMLSchemeItems.Where(x => result.Contains(x.ItemNo + x.UOM) && x.CategoryCode == MMLSchemeItemEnum.WECO.ToString()).ToList();
                    var checkMML = mMLSchemeItems.Where(x => result.Contains(x.ItemNo + x.UOM) && x.CategoryCode == MMLSchemeItemEnum.MML.ToString()).ToList();

                    //1. Bill chỉ có WECO, không có MDL
                    if ((checkWECO != null && checkWECO.Count > 0) && (checkMML == null || checkMML.Count == 0))
                    {
                        journeyCode = MMLSchemeResponseEnum.Journey_1.ToString();
                    }
                    //2. Bill có cả WECO và MDL
                    else if ((checkWECO != null && checkWECO.Count > 0) && (checkMML != null && checkMML.Count > 0))
                    {
                        journeyCode = MMLSchemeResponseEnum.Journey_2.ToString();
                    }
                    //3. Bill chỉ có MDL, không có WECO
                    else if ((checkWECO == null || checkWECO.Count == 0) && (checkMML != null && checkMML.Count > 0))
                    {
                        journeyCode = MMLSchemeResponseEnum.Journey_3.ToString();
                    }

                    var mMLSchemeResponse = await centralMDRepository.GetMMLSchemeResponse(redisManager, mMLSchemeRequest.Code, journeyCode);
                    if (mMLSchemeResponse != null)
                    {
                        //await GetOrderDataWinX(memoryCacheService, mMLSchemeRequest);
                        _ = Task.Run(() => GetOrderDataWinX(memoryCacheService, mMLSchemeRequest));
                        mMLSchemeResponse.Link += $"{mMLSchemeRequest.OrderNo}";
                        return (mMLSchemeResponse, "Success");
                    }
                    else
                    {
                        return (null, $"Không tìm thấy Hành trình đủ điều kiện");
                    }
                }
                else
                {
                    return (null, $"Hóa đơn không có sản phẩm MML, WECO");
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("MMLSchemeService.GetDataMMLSchemeResponse.Exception", ex);
                return (null, "Exception: " + ex.Message);
            }
        }
    }
}
