using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Routing;
using TCX.API.Common.Helpers;
using TCX.API.Common.Models;
using TCX.WebApiCore;
using TichHopSAP;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.Business.Common;
using VCM.POSBLUE.Business.PLG;
using VCM.POSBLUE.Business.VINID;
using VCM.POSBLUE.Data.DataBaseContext.CentralSale;
using VCM.POSBLUE.Model.Common;
using VCM.POSBLUE.Model.Reward;
using VCM.POSBLUE.Model.VINID;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/common")]
    public class CommonController : BaseController
    {
        private readonly CommonBLO _CommonBLO;
        private LogAPIModel ModelLog { get; set; }
        private IPLGBLO IpLGBLO { get; set; }
        private VinIDBLO LogVID { get; set; }
        private readonly KibanaService _kibanaService;
        public CommonController()
        {
            _CommonBLO = new CommonBLO();
            IpLGBLO = new PLGBLO();
            ModelLog = new LogAPIModel();
            LogVID = new VinIDBLO();
            _kibanaService = new KibanaService();
        }

        [HttpGet]
        [Route("TransactionIssue")]
        public HttpResponseMessage TransactionIssue([Required] string articleNo, [Required] string siteCode)
        {
            try
            {
                if (string.IsNullOrEmpty(articleNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "ArticleNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                var data = _CommonBLO.TransactionQtyUse(articleNo, siteCode);
                if (data == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Không có dữ liệu",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = data,
                    Message = "Success",
                    Status = HttpStatusCode.OK
                });

            }
            catch (Exception ex)
            {
                _kibanaService.LogException("TransactionIssue", siteCode, 0, "", JsonConvert.SerializeObject(ex.InnerException));
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống:" + ex.Message,
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("GetCurrentTime")]
        public HttpResponseMessage GetCurrentTime()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = DateTime.Now,
                    Message = "Success",
                    Status = HttpStatusCode.OK
                });

            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("Request GetCurrentTime Exception: " + ex.Message.ToString());
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống:" + ex.Message,
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("GetBusinessDate")]
        public HttpResponseMessage GetBusinessDate(string siteCode, string posTerminal = "")
        {
            BusinessDateResponse result;
            try
            {
                var data = _CommonBLO.GetBusinessDate(siteCode);
                if (data == null)
                {
                    result = new BusinessDateResponse { BussinessDate = DateTime.Now, CurrentDate = DateTime.Now, Status = 1, Message = "Success" };

                    if (!string.IsNullOrEmpty(siteCode))
                    {
                        var modelInsertDateOpen = new BussinessDateOpenModel { Code = siteCode, StoreNo = siteCode, BussinessDate = DateTime.Now, CreatedUser = "api", CreatedDate = DateTime.Now };
                        _CommonBLO.InsertBussinessDateOpen(modelInsertDateOpen);
                    }

                    if (!string.IsNullOrEmpty(posTerminal)) // Insert vao table SignalStore
                    {
                        var model = new SignalStoreModel { StoreNO = siteCode, POSTerminalID = posTerminal, BusinessDate = DateTime.Now.Date, CreatedDate = DateTime.Now };
                        _CommonBLO.InsertSignalStore(model);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = result,
                        Message = "Success",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    if (data.BussinessDate.Value.Date == data.CurrentDate.Value.Date)
                    {
                        result = new BusinessDateResponse { BussinessDate = data.BussinessDate, CurrentDate = data.CurrentDate, Status = 1, Message = "Đúng ngày" };

                        if (!string.IsNullOrEmpty(posTerminal)) // Insert vao table SignalStore
                        {
                            var model = new SignalStoreModel { StoreNO = siteCode, POSTerminalID = posTerminal, BusinessDate = data.BussinessDate, CreatedDate = DateTime.Now };
                            _CommonBLO.InsertSignalStore(model);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                    else
                    {
                        result = new BusinessDateResponse { BussinessDate = data.BussinessDate, CurrentDate = data.CurrentDate, Status = 2, Message = $"Ngày kinh doanh:{data.BussinessDate.Value.ToString("dd/MM/yyyy")} đang nhở hơn ngày hiện tại:{data.CurrentDate.Value.ToString("dd/MM/yyyy")}" };
                        if (!string.IsNullOrEmpty(posTerminal)) // Insert vao table SignalStore
                        {
                            var model = new SignalStoreModel { StoreNO = siteCode, POSTerminalID = posTerminal, BusinessDate = data.CurrentDate, CreatedDate = DateTime.Now };
                            _CommonBLO.InsertSignalStore(model);
                        }
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Success",
                            Status = HttpStatusCode.OK
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                result = new BusinessDateResponse { BussinessDate = null, CurrentDate = null, Status = 3, Message = "Lỗi hệ thống: " + ex.Message };
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = result,
                    Message = $"Lỗi hệ thống: " + ex.Message,
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("CheckEndShift")]
        public HttpResponseMessage CheckEndShift(string siteCode, string posTerminal, DateTime businessDate)
        {
            try
            {
                if (string.IsNullOrEmpty(siteCode))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "siteCode đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posTerminal))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "posTerminal đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(businessDate.ToString()))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Ngày kinh doanh đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                var data = _CommonBLO.GET_SHIFT_HEADER(siteCode, posTerminal, businessDate);
                if (data == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Không có dữ liệu",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = data.IsShiftClosed,
                    Message = "Success",
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = "Lỗi hệ thống: " + ex.Message,
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("POSMonitor")]
        public HttpResponseMessage POSMonitor(POSMonitorInsertRequest model)
        {
            var ipServer = GetIpServer();
            try
            {
                _ = Task.Run(() => _CommonBLO.POSMonitorInsert(model));
                return ResponseData.HttpResponseData(HttpStatusCode.OK,
                                                    $"Response from IPServer {ipServer}=> Monitor Ins thành công",
                                                    new POSMonitorInsertResponse() { ReturnAction = "Ins" });
            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, $"Response from IPServer {ipServer} ({JsonConvert.SerializeObject(ex)})", null);
            }
        }

        [HttpGet]
        [Route("CheckIPaddressPos")]
        public HttpResponseMessage CheckIPaddressPos(string IPAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(IPAddress))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "IPAddress đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var data = _CommonBLO.CheckIPaddressPos(IPAddress);
                if (data == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Hiện tại chưa thiết lập POSTerminal cho IP {IPAddress}, Vui lòng liên hệ IT để được hỗ trợ",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);
            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("POSDataSetup")]
        public HttpResponseMessage POSDataSetup()
        {
            try
            {
                var data = _CommonBLO.GetDataSetup();
                if (data == null && data.Count <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Không có dữ liệu",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);
            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("GetPOSVersion")]
        public HttpResponseMessage GetPOSVersion()
        {
            try
            {
                var data = _CommonBLO.GetPOSVersion();
                if (data == null || data.Count <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Không có dữ liệu",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                Serilog.Log.Information("GetPOSVersion: " + JsonConvert.SerializeObject(data));

                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);
            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("GetOrderInfo")]
        public HttpResponseMessage GetOrderInfo(string orderNo, string storeNo = "", string posNo = "")
        {
            try
            {
                if (string.IsNullOrEmpty(orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Đơn hàng đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (_CommonBLO.CheckSaleReturn(orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Đơn hàng: {orderNo} là đơn hàng trả, không thể thực hiện trả hàng với đơn hàng này",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var data = _CommonBLO.GetOrderInfo(orderNo);

                if (data.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Đơn hàng: {orderNo} chưa được đồng bộ lên Central, vui lòng đợi dữ liệu đồng bộ thành công trước khi trả hàng",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                _kibanaService.LogResponse("GetOrderInfo", posNo, 0, orderNo, JsonConvert.SerializeObject(data));
                var transHeaderStr = data.FirstOrDefault(x => x.TableName == "TransHeader");
                if (transHeaderStr != null && !string.IsNullOrEmpty(storeNo))
                {
                    var transHeaderData = JsonConvert.DeserializeObject<List<TransHeader>>(transHeaderStr.TableData);
                    if (transHeaderData != null && transHeaderData.FirstOrDefault() != null)
                    {
                        if (transHeaderData.FirstOrDefault().StoreNo != storeNo)
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                            {
                                Data = null,
                                Message = $"Đơn hàng {orderNo} không thuộc siêu thị {storeNo}",
                                Status = HttpStatusCode.BadRequest
                            });
                        }
                    }
                }

                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);
            }
            catch (Exception ex)
            {

                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("WriteFileByManual")]
        public HttpResponseMessage WriteFileByManual(string storeNo, string posTerminal)
        {
            try
            {
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "CH/ST đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posTerminal))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "POSTerminal đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                SyncDataToPos sync = new SyncDataToPos();
                var writeFile = sync.writeFileByManual(storeNo, posTerminal, true, true);

                if (writeFile == "OK")
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = writeFile,
                        Message = writeFile,
                        Status = HttpStatusCode.OK
                    });
                }

                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = writeFile,
                    Status = HttpStatusCode.BadRequest
                });

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API WriteFileByManual: {ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("GetListPOSDocumentNo")]
        public HttpResponseMessage GetListPOSDocumentNo(string siteCode, string posTerminal)
        {
            try
            {
                var data = _CommonBLO.ListPOSDocumentNo(siteCode, posTerminal);
                if (data != null && data.Count > 0)
                {
                    return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);
                }
                else
                {
                    return ResponseData.HttpResponseData(HttpStatusCode.OK, "Không có dữ liệu", data);
                }

            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("Exception: " + ex.Message.ToString());
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, ex.Message, null);
            }
        }

        [HttpGet]
        [Route("CheckCouponLine")]
        public HttpResponseMessage CheckCouponLine(string itemNo, string barCode)
        {
            try
            {

                var data = _CommonBLO.CheckCouponLine(itemNo, barCode);
                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);

            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("Exception: " + ex.Message.ToString());
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, ex.Message, false);
            }
        }

        [HttpPost]
        [Route("UpdateOrderTrans")]
        public HttpResponseMessage UpdateOrderTrans(UpdateOrderInfoModel model)
        {
            ResponseUpdateTransModel result;
            try
            {
                if (model.Header == null)
                {
                    result = new ResponseUpdateTransModel { Status = false, Message = $"Dữ liệu trả hàng(Header) không được rỗng" };
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, "Fail", result);
                }

                if (model.ListLine.Count() == 0)
                {
                    result = new ResponseUpdateTransModel { Status = false, Message = $"Dữ liệu trả hàng(Line) không được rỗng" };
                    return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, "Fail", result);
                }
                result = _CommonBLO.InsertLineOrig_Update_OrderInfo(model);
            }

            catch (Exception ex)
            {
                result = new ResponseUpdateTransModel { Status = false, Message = ex.Message };
                return ResponseData.HttpResponseData(HttpStatusCode.BadRequest, "Fail", result);
            }
            Serilog.Log.Logger.Information("UpdateOrderTrans result: " + JsonConvert.SerializeObject(result));
            return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", result);
        }

        [HttpGet]
        [Route("GetInsurance")]
        public HttpResponseMessage GetInsurance(string receiptNo, string posNo, string staffCode)
        {
            try
            {
                if (string.IsNullOrEmpty(receiptNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "receiptNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "posNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(staffCode))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "staffCode đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var data = _CommonBLO.GetInsurance(receiptNo, posNo, staffCode);
                Serilog.Log.Logger.Information("GetInsurance result: " + JsonConvert.SerializeObject(data));
                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);

            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Information("GetInsurance Exception: " + ex.Message.ToString());
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, false);
            }
        }

        [HttpPut]
        [Route("UpdateEOD")]
        public HttpResponseMessage UpdateEOD(POSEOD_APIModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "StoreNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.POSTerminal))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "POSTerminal đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var data = _CommonBLO.UpdatePOSEOD(model);
                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);

            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, false);
            }
        }

        [HttpGet]
        [Route("CheckTotalBill")]
        public HttpResponseMessage CheckTotalBill(string storeNo, string posTerminal, DateTime bussinessDate, int posTotal)
        {
            try
            {
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "storeNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posTerminal))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "posNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var data = _CommonBLO.CheckTotalBill(storeNo, posTerminal, bussinessDate, posTotal);
                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", data);

            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }

        [HttpPost]
        [Route("kios/insert-sale")]
        public HttpResponseMessage KiosInsertSale(KiosInsertSalePOSRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"CH/ST đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var jsonSale = JsonConvert.DeserializeObject<SyncSaleObject>(req.SaleJson);

                string type = jsonSale.Type;
                string json = JsonConvert.SerializeObject(jsonSale.Data);

                json = json.Replace("'", "");
                json = json.Replace("'", "");
                json = json.Replace("'", "");

                var model = new KiosInsertSaleRequest
                {
                    StoreNo = req.StoreNo,
                    Type = type,
                    Json = json
                };

                var logSale = new LogSaleKiosModel
                {
                    StoreNo = req.StoreNo,
                    PosID = req.PosID,
                    RequestPOS = req.SaleJson,
                    CreatedDate = DateTime.Now,
                    RequestAPI = JsonConvert.SerializeObject(model),
                    ResponsePOS = "",
                    ResponseAPI = ""
                };

                var result = _CommonBLO.KiosInsertSale(model);

                logSale.ResponseAPI = JsonConvert.SerializeObject(result);

                if (result.Item1 == true)
                {
                    var res = new ResultResponse
                    {
                        Data = null,
                        Message = $"{result.Item2}",
                        Status = HttpStatusCode.OK
                    };

                    logSale.ResponsePOS = JsonConvert.SerializeObject(res);

                    Task.Run(() => _CommonBLO.LogSaleKios(logSale));

                    return Request.CreateResponse(HttpStatusCode.OK, res);
                }
                else
                {

                    var res = new ResultResponse
                    {
                        Data = null,
                        Message = $"{result.Item2}",
                        Status = HttpStatusCode.BadRequest
                    };

                    logSale.ResponsePOS = JsonConvert.SerializeObject(res);
                    Task.Run(() => _CommonBLO.LogSaleKios(logSale));

                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"{result.Item2}",
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }

            catch (Exception ex)
            {
                var jsonSale = JsonConvert.DeserializeObject<SyncSaleObject>(req.SaleJson);

                string type = jsonSale.Type;
                string json = JsonConvert.SerializeObject(jsonSale.Data);

                json = json.Replace("'", "");
                json = json.Replace("'", "");
                json = json.Replace("'", "");

                var model = new KiosInsertSaleRequest
                {
                    StoreNo = req.StoreNo,
                    Type = type,
                    Json = json
                };

                var logSale = new LogSaleKiosModel
                {
                    StoreNo = req.StoreNo,
                    PosID = req.PosID,
                    RequestPOS = req.SaleJson,
                    CreatedDate = DateTime.Now,
                    RequestAPI = JsonConvert.SerializeObject(model),
                    ResponsePOS = "",
                    ResponseAPI = ""
                };

                var res = new ResultResponse
                {
                    Data = null,
                    Message = $"{JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                };

                logSale.ResponsePOS = JsonConvert.SerializeObject(res);
                Task.Run(() => _CommonBLO.LogSaleKios(logSale));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, res);
            }


        }

        [HttpGet]
        [Route("kios/check-order")]
        public HttpResponseMessage KiosCheckOrder(string storeNo, string posNo, string orderNo)
        {
            try
            {
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"CH/ST đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã POS đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã đơn hàng đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var result = _CommonBLO.KiosCheckOrder(storeNo, posNo, orderNo);

                if (result.Item1 == true)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = result.Item3,
                        Message = $"{result.Item2}",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"{result.Item2}",
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }

            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Error ${JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }


        }

        [HttpGet]
        [Route("SendCodeReward")]
        public HttpResponseMessage SendCodeReward(string storeNo, string posID, string bussinessDate, string orderNo, string offerNo)
        {
            //bussinessDate: yyyy-MM-dd
            try
            {
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "storeNo không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "posID không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "orderNo không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(bussinessDate))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "bussinessDate không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(offerNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "offerNo không được rỗng",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //bussinessDate="yyyy/MM/dd";
                //var ngayKD = DateTime.ParseExact(bussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var model = new RewardCodeRequest
                {
                    StoreNo = storeNo,
                    PosID = posID,
                    BussinessDate = Convert.ToDateTime(bussinessDate),
                    OrderNo = orderNo,
                    OfferNo = offerNo,
                    IPServer = GetIpServer(),
                };

                var data = IpLGBLO.SendCodeReWardToPOS(model);
                if (data.Item1 == true)
                {
                    if (data.Item2 == 204)// Không tìm thấy code, POS cho qua
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = null,
                            Message = data.Item3,
                            Status = HttpStatusCode.NoContent
                        });
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = data.Item4,
                        Message = "Success",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    var requestPOS = JsonConvert.SerializeObject(model);
                    ModelLog = new LogAPIModel
                    {
                        CardNumber = offerNo,
                        StoreNo = storeNo,
                        POSTerminal = posID,
                        InvoiceNo = orderNo,
                        ActionType = "SendCodeReward",
                        RequestPOS = $"{requestPOS}",
                        RequestXML = $"{requestPOS}",
                        ResponseXML = data.Item3,
                        DateTime = DateTime.Now
                    };

                    Task.Run(() => LogVID.WriteLogAPI(ModelLog));

                    if (data.Item2 == 500)
                    {
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = data.Item3,
                            Status = HttpStatusCode.InternalServerError
                        });
                    }

                    return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                    {
                        Data = null,
                        Message = data.Item3,
                        Status = HttpStatusCode.NotFound
                    });
                }
            }
            catch (Exception ex)
            {
                var model = new RewardCodeRequest
                {
                    StoreNo = storeNo,
                    PosID = posID,
                    BussinessDate = Convert.ToDateTime(bussinessDate),
                    OrderNo = orderNo,
                    OfferNo = offerNo
                };

                var requestPOS = JsonConvert.SerializeObject(model);
                ModelLog = new LogAPIModel
                {
                    CardNumber = offerNo,
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    InvoiceNo = orderNo,
                    ActionType = "SendCodeReward",
                    RequestPOS = $"{requestPOS}",
                    RequestXML = $"{requestPOS}",
                    ResponseXML = JsonConvert.SerializeObject(ex),
                    DateTime = DateTime.Now
                };

                Task.Run(() => LogVID.WriteLogAPI(ModelLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống:{JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPost]
        [Route("logging")]
        public HttpResponseMessage PutLoggingElastic(LoggingElastic loggingElastic)
        {
            try
            {
                _kibanaService.LoggingResponseWebApi(loggingElastic);
                return ResponseData.HttpResponseData(HttpStatusCode.OK, "Success", loggingElastic);
            }
            catch (Exception ex)
            {
                return ResponseData.HttpResponseData(HttpStatusCode.InternalServerError, ex.Message, null);
            }
        }
    }
}
