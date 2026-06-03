using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using TCX.API.Common.Constants;
using TCX.API.Common.Dtos.CentralMD;
using TCX.API.Common.Enums;
using TCX.API.Common.Models;
using TCX.WebApiCore;
using TCX.WebApiCore.AppServices.Offer;
using VCM.POSBLUE.API.Services;
using VCM.POSBLUE.Model.POS.Gift;

namespace VCM.POSBLUE.API.Controllers
{
    public class GiftController : BaseController
    {
        private readonly POSGiftService _posGiftService;
        private readonly KibanaService _kibanaService;
        private readonly MemoryCacheService _memoryCacheService;

        private static readonly KibanaService _kibanaServiceShare = new KibanaService();
        private static readonly MemoryCacheService _memoryCacheServiceShare = new MemoryCacheService();
        private static readonly MMLSchemeService _mmlSchemeServiceShare = new MMLSchemeService();
        private static readonly WinXService _winXServiceShare = new WinXService();
        public GiftController()
        {
            _memoryCacheService = _memoryCacheServiceShare;
            _posGiftService = new POSGiftService();
            _kibanaService = _kibanaServiceShare;
        }

        [HttpPost]
        [Route("api/v2/gifts/claim")]
        [ValidateModel]
        public async Task<HttpResponseMessage> ClaimMMLSchemaResponse([FromBody] MMLSchemeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return NewExceptionModels();
            }
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            {
                try
                {
                    string message = "";
                    string messageTechnical = "";
                    List<MMLSchemeResponse> mMLSchemeResponses = new List<MMLSchemeResponse>();

                    if (!string.Equals(request.Code, "THEWINX_QRCode", StringComparison.OrdinalIgnoreCase))
                    {
                        // Run WinX and MML in parallel — they are independent
                        var winXTask = _winXServiceShare.PosPostTransactions(_memoryCacheService, request);
                        var mmlTask = _mmlSchemeServiceShare.GetDataMMLSchemeResponse(_memoryCacheService, request);
                        await Task.WhenAll(winXTask, mmlTask);

                        var dataQR = winXTask.Result;
                        if (dataQR.Item1 != null)
                        {
                            mMLSchemeResponses.Add(new MMLSchemeResponse()
                            {
                                HeaderCode = "THEWINX_QRCode",
                                Code = "999",
                                Title = dataQR.Item1.Title,
                                Link = dataQR.Item1.Url,
                                IsGenQR = true,
                                Enabled = true,
                                Description = dataQR.Item1.Content,
                            });
                        }

                        var result = mmlTask.Result;
                        _kibanaService.LogResponse("api/v2/gifts/claim", request.PosNo, 0, "", $"{request.OrderNo} response: {JsonConvert.SerializeObject(result)}");
                        message = result.Item2;
                        if (result.Item1 != null)
                        {
                            mMLSchemeResponses.Add(result.Item1);
                        }
                        if (mMLSchemeResponses.Count == 0)
                        {
                            messageTechnical = JsonConvert.SerializeObject(result);
                        }
                    }
                    else
                    {
                        // Only WinX QRCode requested
                        var dataQR = await _winXServiceShare.PosPostTransactions(_memoryCacheService, request);
                        if (dataQR.Item1 != null)
                        {
                            mMLSchemeResponses.Add(new MMLSchemeResponse()
                            {
                                HeaderCode = "THEWINX_QRCode",
                                Code = "999",
                                Title = dataQR.Item1.Title,
                                Link = dataQR.Item1.Url,
                                IsGenQR = true,
                                Enabled = true,
                                Description = dataQR.Item1.Content,
                            });
                        }
                        messageTechnical = dataQR.Item2;
                    }

                    if (mMLSchemeResponses.Count > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Message = "Success",
                            Status = HttpStatusCode.OK,
                            Data = mMLSchemeResponses,
                            MessageTechnical = ""
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                        {
                            Message = message,
                            Status = HttpStatusCode.NotFound,
                            MessageTechnical = messageTechnical
                        });
                    }
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    return Request.CreateResponse((HttpStatusCode)408, new ResultResponse
                    {
                        Message = "Request timeout",
                        Status = (HttpStatusCode)408,
                        MessageTechnical = "Request exceeded the 25-second limit"
                    });
                }
                catch (Exception ex)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Message = ex.Message,
                        Status = HttpStatusCode.BadRequest,
                        MessageTechnical = JsonConvert.SerializeObject(ex)
                    });
                }
            }
        }

        [HttpPost]
        [Route("api/pos/gift")]
        public HttpResponseMessage PostGiftCheck([FromBody] GiftDataRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var isE = ModelState.Values.FirstOrDefault();
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                                                    ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, ModelState.Values.FirstOrDefault().Errors[0].ErrorMessage.ToString(), null));
                }
                else
                {
                    string giftStatus = request.Status.ToUpper();
                    //CHECK giftCode
                    if (GiftStatusConst.CheckStatusCreateGift.Contains(giftStatus))
                    {
                        var dataGiftCheck = _posGiftService.CreatePOSGiftInfo(request);
                        if (dataGiftCheck != null && dataGiftCheck.Count > 0)
                        {
                            var giftError = dataGiftCheck.Where(x => x.GiftStatus != GiftStatusEnum.AVAILABLE.ToString()).ToList();
                            if (giftError.Count > 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadGateway,
                                                   ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, "GiftCode đã sử dụng hoặc không tìm thấy", giftError));
                            }
                            else
                            {
                                return Request.CreateResponse(HttpStatusCode.OK,
                                               ResponseHelper.SusscessResponse(HttpStatusCode.OK, dataGiftCheck));
                            }
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.OK,
                                               ResponseHelper.ErrorMessageResponse(HttpStatusCode.OK, "GiftCode đã sử dụng hoặc không tìm thấy", request.GiftCode));
                        }
                    }
                    //SỬ DỤNG giftCode
                    else if (giftStatus == GiftStatusEnum.USED.ToString())
                    {
                        var listGift = _posGiftService.GetByGiftCode(request);
                        if (listGift != null && listGift.Count > 0)
                        {
                            var notAVAILABLE = listGift.Where(x => x.GiftStatus != GiftStatusEnum.AVAILABLE.ToString()).FirstOrDefault();
                            if (notAVAILABLE != null)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadGateway,
                                                  ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, "GiftCode: " + notAVAILABLE.GiftCode.ToString() + " không hợp lệ", null));
                            }
                            else //OK update giftStatus = USED
                            {
                                if (_posGiftService.UpdateStatusPOSGiftInfo(request))
                                {
                                    listGift.ForEach(x => { x.GiftStatus = GiftStatusEnum.USED.ToString(); x.PosUsed = request.PosNo; x.UsedTime = DateTime.Now; });
                                    return Request.CreateResponse(HttpStatusCode.OK, ResponseHelper.SusscessResponse(HttpStatusCode.OK, listGift));
                                }
                                else
                                {
                                    return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, "Có lỗi xảy ra, vui lòng thử lại.", null));
                                }
                            }
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.BadGateway, ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ", null));
                        }
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadGateway,
                                                   ResponseHelper.ErrorMessageResponse(HttpStatusCode.BadRequest, "Status " + giftStatus + " không hợp lệ", null));
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ResponseHelper.ExceptionMessageResponse(HttpStatusCode.BadRequest, ex));
            }
        }
    }
}