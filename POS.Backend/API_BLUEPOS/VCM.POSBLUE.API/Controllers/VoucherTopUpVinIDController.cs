using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using TCX.API.Common.Models;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.Business.VINID;
using VCM.POSBLUE.Model.TopupVoucherVinID;
using VCM.POSBLUE.Model.VINID;

namespace VCM.POSBLUE.API.Controllers
{
    [RoutePrefix("api/vinid")]
    public class VoucherTopUpVinIDController : ApiController
    {
        readonly string vc_topup_link = WebConfigurationManager.AppSettings["vc_topup_link"];
        readonly string X_key_code = WebConfigurationManager.AppSettings["vc_topup_keycode"];
        readonly string topup_get_user_by_phone = WebConfigurationManager.AppSettings["topup_get_user_by_phone"];
        readonly string topup_point_phone = WebConfigurationManager.AppSettings["topup_point_phone"];
        readonly string topup_status_order = WebConfigurationManager.AppSettings["topup_status_order"];
        readonly string evoucher_verify = WebConfigurationManager.AppSettings["evoucher_verify"];
        readonly string evoucher_mark_used = WebConfigurationManager.AppSettings["evoucher_mark_used"];
        readonly string evoucher_revoke = WebConfigurationManager.AppSettings["evoucher_revoke"];
        readonly string evoucher_refund = WebConfigurationManager.AppSettings["evoucher_refund"];
        readonly string topup_item_vinid = WebConfigurationManager.AppSettings["TOPUP_ITEM_VINID"];

        private LogAPIModel modelLog { get; set; }
        private VinIDBLO logVID { get; set; }
        public VoucherTopUpVinIDController()
        {
            modelLog = new LogAPIModel();
            logVID = new VinIDBLO();
        }

        [HttpGet]
        [Route("TopUpCheckMember")]//Thông tin khách hàng
        public HttpResponseMessage TopUpCheckMember(string phoneNumber, string posID, string storeNo, bool isLog = true)
        {
            if (string.IsNullOrEmpty(vc_topup_link))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Hệ thống đã ngừng dịch vụ này",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"phoneNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"storeNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"posID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                //storeNo = "1535";
                //posID = "1535001";

                var storeNoVINID = storeNo;
                var posVINID = posID;

                var storeMapping = logVID.StoreMapping(storeNo, posID);
                if (storeMapping != null)
                {
                    posVINID = storeMapping?.OldTerminalID;
                    storeNoVINID = storeMapping?.OldStoreID;
                }

                var endPoint = $"{topup_get_user_by_phone}?phone_number={phoneNumber}&store_code={storeNoVINID}&pos_code={posVINID}";
                var endPointPOS = $"api/vinid/CheckMember?phoneNumber={phoneNumber}&storeNo={storeNoVINID}&posID={posVINID}";
                modelLog = new LogAPIModel
                {
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    CardNumber = phoneNumber,
                    InvoiceNo = "",
                    ActionType = "TopUpCheckMember",
                    RequestPOS = $"{endPointPOS}",
                    RequestXML = $"{vc_topup_link}{endPoint}",
                    ResponseXML = "",
                    ResponseTotal = 0,
                    StatusVINID = false,
                    DateTime = DateTime.Now
                };
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                        var X_Nonce = Guid.NewGuid().ToString();

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(vc_topup_link);

                        //RawData = {url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};
                        var rawData = $"{topup_get_user_by_phone};GET;{X_Nonce};{X_timestamp};{X_key_code};";
                        var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                        client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                        client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                        client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                        client.DefaultRequestHeaders.Add("X-Signature", signature);
                        client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        ServicePointManager.DnsRefreshTimeout = 0;

                        var response = client.GetAsync(endPoint).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        modelLog.ResponseTotal = milliseconds;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            VinIDTopUpResponse modelResponse = JsonConvert.DeserializeObject<VinIDTopUpResponse>(readData.Result);
                            modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);
                            if (modelResponse.meta.code == 200)
                            {
                                modelLog.StatusVINID = true;
                                var data = modelResponse.data;

                                var responsePOS = new VinIDTopUpCheckPosResponse
                                {
                                    FullName = data.full_name,
                                    PhoneNumber = data.phone_number,
                                    DOB = data.dob,
                                    Gender = data.gender,
                                    Identify = data.identify,
                                    Status = data.status,// A: Active | I: Inactive | B: Block
                                    CardStatus = data.card_status,// A: Active | I: Inactive
                                    ArticleNo = topup_item_vinid
                                };

                                Task.Run(() => logVID.WriteLogAPI(modelLog));
                                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                                {
                                    Data = responsePOS,
                                    Message = $"Success",
                                    Status = HttpStatusCode.OK
                                });
                            }
                            else
                            {
                                Task.Run(() => logVID.WriteLogAPI(modelLog));
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = $"{modelResponse.meta.message}",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }
                        }
                        else
                        {
                            Task.Run(() => logVID.WriteLogAPI(modelLog));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Data = null,
                                Message = $"Gọi API VINID thất bại, vui lòng liên hệ IT hỗ trợ",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                        Task.Run(() => logVID.WriteLogAPI(modelLog));
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống VINID {JsonConvert.SerializeObject(ex)}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                modelLog.StatusVINID = false;

                Task.Run(() => logVID.WriteLogAPI(modelLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API BluePOS {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }

        }

        [HttpPost]
        [Route("TopUpPoinToPhone")] //API để nạp điểm cho số điện thoại người dùng
        public HttpResponseMessage TopUpPoinToPhone(VinIDTopUpPoinPosRequest model)
        {
            if (string.IsNullOrEmpty(vc_topup_link))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Hệ thống đã ngừng dịch vụ này",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                if (string.IsNullOrEmpty(model.PhoneNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"PhoneNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"StoreNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.PosID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"PosID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.OrderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"OrderNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.PointAmount < 50)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"PointAmount tối thiểu 50",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.IsVAT)
                {
                    var infoVAT = model.InfoVAT;
                    if (string.IsNullOrEmpty(infoVAT.FullName))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Họ tên xuất VAT đang bị trống",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (string.IsNullOrEmpty(infoVAT.Address))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Địa chỉ xuất VAT đang bị trống",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (string.IsNullOrEmpty(infoVAT.TaxCode))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"MST xuất VAT đang bị trống",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                    if (string.IsNullOrEmpty(infoVAT.Email))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Email xuất VAT đang bị trống",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }

                //model.StoreNo = "1535";
                //model.PosID = "1535001";

                var storeNoVINID = model.StoreNo;
                var posVINID = model.PosID;

                var storeMapping = logVID.StoreMapping(model.StoreNo, model.PosID);
                if (storeMapping != null)
                {
                    posVINID = storeMapping?.OldTerminalID;
                    storeNoVINID = storeMapping?.OldStoreID;
                }

                var requestPOS = JsonConvert.SerializeObject(model);
                var body = new VinIDTopUpPoinRequest
                {
                    phone_number = model.PhoneNumber,
                    store_code = storeNoVINID,
                    pos_code = posVINID,
                    point_amount = model.PointAmount,
                    order_id = model.OrderNo,
                    send_receipt = model.IsVAT,
                    txn_id = $"{model.OrderNo}",
                    receipt_info = (model.InfoVAT == null || model.IsVAT == false) ? null : new ReceiptInfo
                    {
                        address = model.InfoVAT.Address,
                        email = model.InfoVAT.Email,
                        name = model.InfoVAT.FullName,
                        tax_code = model.InfoVAT.TaxCode
                    }
                };

                var requestVIN = JsonConvert.SerializeObject(body);

                modelLog = new LogAPIModel
                {
                    StoreNo = model.StoreNo,
                    POSTerminal = model.PosID,
                    CardNumber = model.PhoneNumber,
                    InvoiceNo = model.OrderNo,
                    ActionType = "TopUpPoinToPhone",
                    RequestPOS = $"{requestPOS}",
                    RequestXML = $"{requestVIN}",
                    ResponseXML = "",
                    ResponseTotal = 0,
                    StatusVINID = false,
                    DateTime = DateTime.Now
                };
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                        var X_Nonce = Guid.NewGuid().ToString();

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(vc_topup_link);

                        //RawData = {url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};
                        var rawData = $"{topup_point_phone};POST;{X_Nonce};{X_timestamp};{X_key_code};{requestVIN}";
                        var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                        client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                        client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                        client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                        client.DefaultRequestHeaders.Add("X-Signature", signature);
                        client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        ServicePointManager.DnsRefreshTimeout = 0;

                        var response = client.PostAsync(topup_point_phone, new StringContent(requestVIN, Encoding.UTF8, "application/json")).Result;
                        // var response = client.GetAsync(endPoint).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        modelLog.ResponseTotal = milliseconds;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            var modelResponse = JsonConvert.DeserializeObject<VinIDTopUpPointResponse>(readData.Result);
                            modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);

                            var httpCode = HttpStatusCode.OK;
                            var message = "OK";
                            if (modelResponse.meta.code == 200)
                            {
                                modelLog.StatusVINID = true;
                            }
                            else
                            {
                                httpCode = HttpStatusCode.BadRequest;
                                message = $"{modelResponse.meta.message}";
                            }
                            Task.Run(() => logVID.WriteLogAPI(modelLog));


                            return Request.CreateResponse(httpCode, new ResultResponse
                            {
                                Message = $"{message}",
                                Status = httpCode
                            });
                        }
                        else
                        {
                            modelLog.StatusVINID = false;
                            Task.Run(() => logVID.WriteLogAPI(modelLog));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Data = null,
                                Message = $"Gọi API VINID thất bại, vui lòng liên hệ IT hỗ trợ",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                        modelLog.StatusVINID = false;
                        Task.Run(() => logVID.WriteLogAPI(modelLog));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống VINID {JsonConvert.SerializeObject(ex)}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                modelLog.StatusVINID = false;

                Task.Run(() => logVID.WriteLogAPI(modelLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API BluePOS {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }

        }

        [HttpGet]
        [Route("TopUpCheckStatusOrder")]//API để kiểm tra trạng thái đơn hàng
        public HttpResponseMessage TopUpCheckStatusOrder(string orderNo, string posID, string storeNo)
        {
            if (string.IsNullOrEmpty(vc_topup_link))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Hệ thống đã ngừng dịch vụ này",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                if (string.IsNullOrEmpty(orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"orderNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"storeNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"posID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                //storeNo = "1535";
                //posID = "1535001";

                var endPoint = $"{topup_status_order}?txn_id={orderNo}&store_code={storeNo}&pos_code={posID}";
                var endPointPOS = $"api/vinid/CheckStatusOrder?orderNo={orderNo}&storeNo={storeNo}&posID={posID}";
                modelLog = new LogAPIModel
                {
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    CardNumber = "",
                    InvoiceNo = $"{orderNo}",
                    ActionType = "TopUpCheckStatusOrder",
                    RequestPOS = $"{endPointPOS}",
                    RequestXML = $"{vc_topup_link}{endPoint}",
                    ResponseXML = "",
                    ResponseTotal = 0,
                    StatusVINID = false,
                    DateTime = DateTime.Now
                };
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                        var X_Nonce = Guid.NewGuid().ToString();

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(vc_topup_link);

                        //RawData = {url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};
                        var rawData = $"{topup_status_order};GET;{X_Nonce};{X_timestamp};{X_key_code};";
                        var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                        client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                        client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                        client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                        client.DefaultRequestHeaders.Add("X-Signature", signature);
                        client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        ServicePointManager.DnsRefreshTimeout = 0;

                        var response = client.GetAsync(endPoint).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        modelLog.ResponseTotal = milliseconds;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            var modelResponse = JsonConvert.DeserializeObject<VinIDTopUpStatusOrderResponse>(readData.Result);
                            modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);
                            modelLog.StatusVINID = true;

                            var data = modelResponse.data;
                            Task.Run(() => logVID.WriteLogAPI(modelLog));

                            if (data == null)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                                {
                                    Message = $"Không tìm thấy đơn hàng {orderNo}",
                                    Status = HttpStatusCode.BadRequest
                                });
                            }

                            var responsePOS = new VinIDTopUpStatusOrderPosResponse
                            {
                                StatusOrder = data.status,
                                TypeOrder = data.type
                            };

                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = responsePOS,
                                Message = $"",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            modelLog.StatusVINID = false;
                            Task.Run(() => logVID.WriteLogAPI(modelLog));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Data = null,
                                Message = $"Gọi API VINID thất bại, vui lòng liên hệ IT hỗ trợ",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                        modelLog.StatusVINID = false;
                        Task.Run(() => logVID.WriteLogAPI(modelLog));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống VINID {JsonConvert.SerializeObject(ex)}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                modelLog.StatusVINID = false;

                Task.Run(() => logVID.WriteLogAPI(modelLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API BluePOS {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }

        }

        [HttpGet]
        [Route("EVoucherVerify")] //API cho Merchant xác thực thông tin voucher mà khách hàng sử dụng
        public HttpResponseMessage EVoucherVerify(string storeNo, string posID, string serialNumber, string userPOS)
        {
            if (string.IsNullOrEmpty(vc_topup_link))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Hệ thống đã ngừng dịch vụ này",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                if (string.IsNullOrEmpty(serialNumber))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"SerialNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"StoreNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"PosID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var requestPOS = $"api/vinid/EVoucherVerify?storeNo={storeNo}&posID={posID}&serialNumber={serialNumber}&userPOS={userPOS}";// JsonConvert.SerializeObject(model);

                //storeNo = "1535";
                //posID = "1535001";


                var body = new VinIDEVoucherVerifyRequest
                {
                    store_code = "EVOUCHER",// storeNo,
                    pos_code = "EVOUCHER",// posID,
                    serial_number = serialNumber,
                    merchant_staff_id = userPOS
                };

                var requestVIN = JsonConvert.SerializeObject(body);

                modelLog = new LogAPIModel
                {
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    CardNumber = "",
                    InvoiceNo = serialNumber,
                    ActionType = "EVoucherVerify",
                    RequestPOS = $"{requestPOS}",
                    RequestXML = $"{requestVIN}",
                    ResponseXML = "",
                    ResponseTotal = 0,
                    StatusVINID = false,
                    DateTime = DateTime.Now
                };
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                        var X_Nonce = Guid.NewGuid().ToString();

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(vc_topup_link);

                        //RawData = {url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};
                        var rawData = $"{evoucher_verify};POST;{X_Nonce};{X_timestamp};{X_key_code};{requestVIN}";
                        var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                        client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                        client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                        client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                        client.DefaultRequestHeaders.Add("X-Signature", signature);
                        client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        ServicePointManager.DnsRefreshTimeout = 0;

                        var response = client.PostAsync(evoucher_verify, new StringContent(requestVIN, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        modelLog.ResponseTotal = milliseconds;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            var modelResponse = JsonConvert.DeserializeObject<VinIDEVoucherResponse>(readData.Result);
                            modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);

                            var httpCode = HttpStatusCode.OK;
                            var message = "OK";
                            var dataPOS = new EvoucherPosResponse();
                            if (modelResponse.meta.code == 200)
                            {
                                var data = modelResponse.data;

                                modelLog.StatusVINID = true;
                                dataPOS = new EvoucherPosResponse
                                {
                                    //PurchasedAt = data.purchased_at,
                                    //ExpiredAt = data.expired_at,
                                    MinOrderValue = data.min_order_value,
                                    DiscountType = data.discount_type,
                                    DiscountValue = data.discount_value
                                    //ApplyInHoliday = data.apply_in_holiday,
                                    //MerchantVoucherCode = data.merchant_voucher_code
                                };
                            }
                            else
                            {
                                httpCode = HttpStatusCode.BadRequest;
                                message = $"{modelResponse.meta.message}";
                            }

                            Task.Run(() => logVID.WriteLogAPI(modelLog));
                            var pos = modelResponse.meta.code != 200 ? null : dataPOS;
                            return Request.CreateResponse(httpCode, new ResultResponse
                            {
                                Message = $"{message}",
                                Status = httpCode,
                                Data = pos
                            });
                        }
                        else
                        {
                            modelLog.StatusVINID = false;
                            Task.Run(() => logVID.WriteLogAPI(modelLog));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Data = null,
                                Message = $"Gọi API VINID thất bại, vui lòng liên hệ IT hỗ trợ",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                        modelLog.StatusVINID = false;
                        Task.Run(() => logVID.WriteLogAPI(modelLog));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống VINID {JsonConvert.SerializeObject(ex)}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                modelLog.StatusVINID = false;

                Task.Run(() => logVID.WriteLogAPI(modelLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API BluePOS {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        //[HttpPost]
        //[Route("EVoucherMarkUsed")] //API cho Merchant đánh dấu voucher đã sử dụng
        //public HttpResponseMessage EVoucherMarkUsed(VinIDEVoucherUsedPosRequest model)
        //{
        //    try
        //    {
        //        var listSerial = model.ListSerialNumber;
        //        if (listSerial.Any(d => string.IsNullOrEmpty(d)))
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //            {
        //                Data = null,
        //                Message = $"SerialNumber đang bị trống",
        //                Status = HttpStatusCode.BadRequest
        //            });
        //        }
        //        if (string.IsNullOrEmpty(model.StoreNo))
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //            {
        //                Data = null,
        //                Message = $"StoreNo đang bị trống",
        //                Status = HttpStatusCode.BadRequest
        //            });
        //        }
        //        if (string.IsNullOrEmpty(model.PosID))
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //            {
        //                Data = null,
        //                Message = $"PosID đang bị trống",
        //                Status = HttpStatusCode.BadRequest
        //            });
        //        }
        //        if (string.IsNullOrEmpty(model.OrderNo))
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
        //            {
        //                Data = null,
        //                Message = $"OrderNo đang bị trống",
        //                Status = HttpStatusCode.BadRequest
        //            });
        //        }

        //        var requestPOS = JsonConvert.SerializeObject(model);

        //        model.StoreNo = "1535";
        //        model.PosID = "1535001";
        //        foreach (var item in listSerial)
        //        {

        //        }
        //        var body = new VinIDEVoucherUsedRequest
        //        {
        //            merchant_code = "VINMART",
        //            store_code = model.StoreNo,
        //            pos_code = model.PosID,
        //            serial_number = model.SerialNumber,
        //            merchant_staff_id = 35515,
        //            redeem_ref_id = model.OrderNo,
        //            merchant_voucher_code = model.SerialNumber,
        //            merchant_reference_id = "",
        //            extra_data = null
        //        };

        //        var requestVIN = JsonConvert.SerializeObject(body);

        //        modelLog = new LogAPIModel
        //        {
        //            StoreNo = model.StoreNo,
        //            POSTerminal = model.PosID,
        //            CardNumber = "",
        //            InvoiceNo = model.SerialNumber,
        //            ActionType = "EVoucherMarkUsed",
        //            RequestPOS = $"{requestPOS}",
        //            RequestXML = $"{requestVIN}",
        //            ResponseXML = "",
        //            ResponseTotal = 0,
        //            StatusVINID = false,
        //            DateTime = DateTime.Now
        //        };
        //        using (var client = new HttpClient())
        //        {
        //            try
        //            {
        //                var st1 = new Stopwatch();
        //                st1.Start();
        //                long milliseconds = 0;

        //                var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        //                var X_Nonce = Guid.NewGuid().ToString();

        //                client.Timeout = TimeSpan.FromMinutes(2);
        //                client.BaseAddress = new Uri(vc_topup_link);

        //                //RawData = {url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};
        //                var rawData = $"{evoucher_mark_used};POST;{X_Nonce};{X_timestamp};{X_key_code};{requestVIN}";
        //                var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

        //                client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
        //                client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
        //                client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
        //                client.DefaultRequestHeaders.Add("X-Signature", signature);
        //                client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

        //                ServicePointManager.Expect100Continue = true;
        //                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        //                ServicePointManager.DnsRefreshTimeout = 0;

        //                var response = client.PostAsync(evoucher_mark_used, new StringContent(requestVIN, Encoding.UTF8, "application/json")).Result;

        //                milliseconds = st1.ElapsedMilliseconds;
        //                st1.Stop();
        //                modelLog.ResponseTotal = milliseconds;

        //                if (response.StatusCode == HttpStatusCode.OK)
        //                {
        //                    var readData = response.Content.ReadAsStringAsync();
        //                    var modelResponse = JsonConvert.DeserializeObject<VinIDEVoucherUserResponse>(readData.Result);
        //                    modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);

        //                    var httpCode = HttpStatusCode.OK;
        //                    var message = "OK";

        //                    if (modelResponse.meta.code == 200)
        //                    {
        //                        modelLog.StatusVINID = true;
        //                    }
        //                    else
        //                    {
        //                        httpCode = HttpStatusCode.BadRequest;
        //                        message = $"{modelResponse.meta.message}";
        //                    }

        //                    Task.Run(() => logVID.WriteLogAPI(modelLog));

        //                    return Request.CreateResponse(httpCode, new ResultResponse
        //                    {
        //                        Message = $"{message}",
        //                        Status = httpCode
        //                    });
        //                }
        //                else
        //                {
        //                    modelLog.StatusVINID = false;
        //                    Task.Run(() => logVID.WriteLogAPI(modelLog));

        //                    return Request.CreateResponse(response.StatusCode, new ResultResponse
        //                    {
        //                        Data = null,
        //                        Message = $"Gọi API VINID thất bại, vui lòng liên hệ IT hỗ trợ",
        //                        Status = response.StatusCode
        //                    });
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
        //                modelLog.StatusVINID = false;
        //                Task.Run(() => logVID.WriteLogAPI(modelLog));

        //                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
        //                {
        //                    Data = null,
        //                    Message = $"Lỗi hệ thống API VINID {JsonConvert.SerializeObject(ex)}",
        //                    Status = HttpStatusCode.InternalServerError
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
        //        modelLog.StatusVINID = false;

        //        Task.Run(() => logVID.WriteLogAPI(modelLog));

        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
        //        {
        //            Data = null,
        //            Message = $"Lỗi hệ thống API BluePOS {JsonConvert.SerializeObject(ex)}",
        //            Status = HttpStatusCode.InternalServerError
        //        });
        //    }

        //}

        [HttpPost]
        [Route("EVoucherRefund")] //API Thu hồi e-voucher
        public HttpResponseMessage EVoucherRefund(VinIDEVoucherRefundPOSRequest model)
        {
            if (string.IsNullOrEmpty(vc_topup_link))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Hệ thống đã ngừng dịch vụ này",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                if (model.ListserialNumber == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"SerialNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"StoreNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.PosID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"PosID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var requestPOS = JsonConvert.SerializeObject(model);

                //model.StoreNo = "1535";
                //model.PosID = "1535001";

                var body = new VinIDEVoucherRefundRequest
                {
                    serials = model.ListserialNumber
                };

                var requestVIN = JsonConvert.SerializeObject(body);

                modelLog = new LogAPIModel
                {
                    StoreNo = model.StoreNo,
                    POSTerminal = model.PosID,
                    CardNumber = "",
                    InvoiceNo = JsonConvert.SerializeObject(model.ListserialNumber),
                    ActionType = "EVoucherRefund",
                    RequestPOS = $"{requestPOS}",
                    RequestXML = $"{requestVIN}",
                    ResponseXML = "",
                    ResponseTotal = 0,
                    StatusVINID = false,
                    DateTime = DateTime.Now
                };
                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                        var X_Nonce = Guid.NewGuid().ToString();

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(vc_topup_link);

                        //RawData = {url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};
                        var rawData = $"{evoucher_refund};POST;{X_Nonce};{X_timestamp};{X_key_code};{requestVIN}";
                        var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                        client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                        client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                        client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                        client.DefaultRequestHeaders.Add("X-Signature", signature);
                        client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        ServicePointManager.DnsRefreshTimeout = 0;

                        var response = client.PostAsync(evoucher_refund, new StringContent(requestVIN, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        modelLog.ResponseTotal = milliseconds;

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            var modelResponse = JsonConvert.DeserializeObject<VinIDEVoucherRevokeResponse>(readData.Result);
                            modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);

                            var httpCode = HttpStatusCode.OK;
                            var message = "OK";

                            if (modelResponse.meta.code == 200)
                            {
                                modelLog.StatusVINID = true;
                            }
                            else
                            {
                                httpCode = HttpStatusCode.BadRequest;
                                message = $"{modelResponse.meta.message}";
                            }

                            Task.Run(() => logVID.WriteLogAPI(modelLog));

                            return Request.CreateResponse(httpCode, new ResultResponse
                            {
                                Message = $"{message}",
                                Status = httpCode
                            });
                        }
                        else
                        {
                            modelLog.StatusVINID = false;
                            modelLog.ResponseXML = JsonConvert.SerializeObject(response);
                            Task.Run(() => logVID.WriteLogAPI(modelLog));

                            return Request.CreateResponse(response.StatusCode, new ResultResponse
                            {
                                Data = null,
                                Message = $"Gọi API VINID thất bại, vui lòng liên hệ IT hỗ trợ",
                                Status = response.StatusCode
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                        modelLog.StatusVINID = false;
                        Task.Run(() => logVID.WriteLogAPI(modelLog));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống VINID {JsonConvert.SerializeObject(ex)}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                modelLog.StatusVINID = false;

                Task.Run(() => logVID.WriteLogAPI(modelLog));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API BluePOS {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }

        }

        [HttpPost]
        [Route("EVoucherMarkUsed")] //API cho Merchant đánh dấu voucher đã sử dụng
        public HttpResponseMessage EVoucherMarkUsed(VinIDEVoucherUsedPosRequest model)
        {
            if (string.IsNullOrEmpty(vc_topup_link))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Hệ thống đã ngừng dịch vụ này",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                var listSerial = model.ListSerialNumber;
                if (listSerial.Any(d => string.IsNullOrEmpty(d)))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"SerialNumber đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.StoreNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"StoreNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.PosID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"PosID đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.OrderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"OrderNo đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var requestPOS = JsonConvert.SerializeObject(model);

                //model.StoreNo = "1535";
                //model.PosID = "1535001";

                var task_API = new List<Task>();
                var listSerialResponse = new List<ListSerialResponse>();
                foreach (var item in listSerial)
                {
                    //item:SerialNumber
                    var modelCall = new EVoucherUsedPosRequest
                    {
                        StoreNo = model.StoreNo,
                        PosID = model.PosID,
                        OrderNo = model.OrderNo,
                        UserPOS = model.UserPOS,
                        SerialNumber = item
                    };
                    //task_API.Add(Task.Run(() => listSerialResponse.Add(MarkUsedCallAPI(modelCall))));
                    listSerialResponse.Add(MarkUsedCallAPI(modelCall));
                }
                //Task taskAwait = Task.WhenAll(task_API.ToArray());
                //await taskAwait;


                var listFaild = listSerialResponse.Where(d => d.IsSuccess == false).ToList();
                if (listFaild.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Message = $"Sử dụng thành công",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {

                    var listOK_revoke = listFaild.Where(d => d.IsSuccess == true).ToList();
                    var listSerialRevokeResponse = new List<RevokeResponse>();
                    if (listOK_revoke != null && listOK_revoke.Count > 0)//Call hàm revoke
                    {
                        var modelCallRevoke = new VinIDEVoucherRefundPOSRequest
                        {
                            StoreNo = model.StoreNo,
                            PosID = model.PosID,
                            ListserialNumber = listOK_revoke.Select(a => a.SerialNumber).ToList()
                        };
                        var callRefund = EVoucherRefundCallAPI(modelCallRevoke);

                        var refundInvoice = new List<RevokeResponse>();
                        foreach (var item in listOK_revoke)
                        {
                            refundInvoice.Add(new RevokeResponse
                            {
                                SerialNumber = item.SerialNumber,
                                IsSuccess = callRefund.IsSuccess == true ? true : false,
                                Message = callRefund.IsSuccess == true ? "" : callRefund.Message
                            });
                        }
                        listSerialRevokeResponse.AddRange(refundInvoice);

                    }

                    var resultPOS = new SerialPOSResponse
                    {
                        usedList = listSerialResponse,
                        revokeList = listSerialRevokeResponse
                    };

                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Message = $"Sử dụng thất bại {listSerialResponse.Count} serial number",
                        Status = HttpStatusCode.BadRequest,
                        Data = resultPOS
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API BluePOS {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        ListSerialResponse MarkUsedCallAPI(EVoucherUsedPosRequest model)
        {
            var result = new ListSerialResponse
            {
                SerialNumber = model.SerialNumber,
                Message = "",                
                IsSuccess = false
            };
            var body = new VinIDEVoucherUsedRequest
            {
                merchant_code = "VinCommerce",//PROD:"VinCommerce",UAT:VINMART
                store_code = "EVOUCHER",// model.StoreNo,
                pos_code = "EVOUCHER",// model.PosID,
                serial_number = model.SerialNumber,
                merchant_staff_id = 35515,
                redeem_ref_id = model.OrderNo,
                merchant_voucher_code = model.SerialNumber,
                merchant_reference_id = "",
                extra_data = null
            };

            var requestVIN = JsonConvert.SerializeObject(body);
            var requestPOS = JsonConvert.SerializeObject(model);

            modelLog = new LogAPIModel
            {
                StoreNo = model.StoreNo,
                POSTerminal = model.PosID,
                CardNumber = "",
                InvoiceNo = model.SerialNumber,
                ActionType = "EVoucherMarkUsed",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{requestVIN}",
                ResponseXML = "",
                ResponseTotal = 0,
                StatusVINID = false,
                DateTime = DateTime.Now
            };

            using (var client = new HttpClient())
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    var X_Nonce = Guid.NewGuid().ToString();

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(vc_topup_link);

                    //RawData = {url};{method};{X-Nonce};{X-Timestamp};{X-Key-Code};
                    var rawData = $"{evoucher_mark_used};POST;{X_Nonce};{X_timestamp};{X_key_code};{requestVIN}";
                    var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                    client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                    client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                    client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                    client.DefaultRequestHeaders.Add("X-Signature", signature);
                    client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    ServicePointManager.DnsRefreshTimeout = 0;

                    var response = client.PostAsync(evoucher_mark_used, new StringContent(requestVIN, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                    modelLog.ResponseTotal = milliseconds;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var modelResponse = JsonConvert.DeserializeObject<VinIDEVoucherUserResponse>(readData.Result);
                        modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);

                        var message = "OK";

                        if (modelResponse.meta.code == 200)
                        {
                            modelLog.StatusVINID = true;
                            result.IsSuccess = true;
                        }
                        else
                        {
                            message = $"{modelResponse.meta.message}";
                        }
                        result.Message = message;
                    }
                    else
                    {
                        modelLog.StatusVINID = false;
                    }

                }
                catch (Exception ex)
                {
                    modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                    modelLog.StatusVINID = false;
                }
            }

            logVID.WriteLogAPI(modelLog);
            return result;
        }
        RevokeResponse EVoucherRevokeCallAPI(VinIDEVoucherRevokePosRequest model)
        {
            var result = new RevokeResponse
            {
                SerialNumber = model.SerialNumber,
                Message = "",
                IsSuccess = false
            };

            var requestPOS = JsonConvert.SerializeObject(model);

            var body = new VinIDEVoucherRevokeRequest
            {
                merchant_code = "VINMART",
                serial_number = model.SerialNumber
            };

            var requestVIN = JsonConvert.SerializeObject(body);

            modelLog = new LogAPIModel
            {
                StoreNo = model.StoreNo,
                POSTerminal = model.PosID,
                CardNumber = "",
                InvoiceNo = model.SerialNumber,
                ActionType = "EVoucherRevoke",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{requestVIN}",
                ResponseXML = "",
                ResponseTotal = 0,
                StatusVINID = false,
                DateTime = DateTime.Now
            };
            using (var client = new HttpClient())
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    var X_Nonce = Guid.NewGuid().ToString();

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(vc_topup_link);

                    var rawData = $"{evoucher_revoke};POST;{X_Nonce};{X_timestamp};{X_key_code};{requestVIN}";
                    var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                    client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                    client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                    client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                    client.DefaultRequestHeaders.Add("X-Signature", signature);
                    client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    ServicePointManager.DnsRefreshTimeout = 0;

                    var response = client.PostAsync(evoucher_revoke, new StringContent(requestVIN, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                    modelLog.ResponseTotal = milliseconds;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var modelResponse = JsonConvert.DeserializeObject<VinIDEVoucherRevokeResponse>(readData.Result);
                        modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);

                        var message = "OK";

                        if (modelResponse.meta.code == 200)
                        {
                            modelLog.StatusVINID = true;
                            result.IsSuccess = true;
                        }
                        else
                        {
                            message = $"{modelResponse.meta.message}";
                        }

                        result.Message = message;
                    }
                    else
                    {
                        modelLog.StatusVINID = false;
                        modelLog.ResponseXML = JsonConvert.SerializeObject(response);
                    }
                }
                catch (Exception ex)
                {
                    modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                    modelLog.StatusVINID = false;
                }

                logVID.WriteLogAPI(modelLog);
            }
            return result;
        }
        RevokeResponse EVoucherRefundCallAPI(VinIDEVoucherRefundPOSRequest model)
        {
            var result = new RevokeResponse
            {
                SerialNumber = JsonConvert.SerializeObject(model.ListserialNumber),
                Message = "",
                IsSuccess = false
            };

            var requestPOS = JsonConvert.SerializeObject(model);

            var body = new VinIDEVoucherRefundRequest
            {
                serials = model.ListserialNumber
            };

            var requestVIN = JsonConvert.SerializeObject(body);

            modelLog = new LogAPIModel
            {
                StoreNo = model.StoreNo,
                POSTerminal = model.PosID,
                CardNumber = "",
                InvoiceNo = JsonConvert.SerializeObject(model.ListserialNumber),
                ActionType = "EVoucherRefund_MarkUsed",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{requestVIN}",
                ResponseXML = "",
                ResponseTotal = 0,
                StatusVINID = false,
                DateTime = DateTime.Now
            };
            using (var client = new HttpClient())
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var X_timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    var X_Nonce = Guid.NewGuid().ToString();

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(vc_topup_link);

                    var rawData = $"{evoucher_refund};POST;{X_Nonce};{X_timestamp};{X_key_code};{requestVIN}";
                    var signature = VoucherTopUpSignatureVinID.SignatureVinIDVoucherTopUp(rawData);

                    client.DefaultRequestHeaders.Add("X-Key-Code", X_key_code);
                    client.DefaultRequestHeaders.Add("X-Timestamp", X_timestamp);
                    client.DefaultRequestHeaders.Add("X-Nonce", X_Nonce);
                    client.DefaultRequestHeaders.Add("X-Signature", signature);
                    client.DefaultRequestHeaders.Add("X-Request-ID", X_Nonce);

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    ServicePointManager.DnsRefreshTimeout = 0;

                    var response = client.PostAsync(evoucher_refund, new StringContent(requestVIN, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                    modelLog.ResponseTotal = milliseconds;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var modelResponse = JsonConvert.DeserializeObject<VinIDEVoucherRevokeResponse>(readData.Result);
                        modelLog.ResponseXML = JsonConvert.SerializeObject(modelResponse);

                        var message = "OK";

                        if (modelResponse.meta.code == 200)
                        {
                            modelLog.StatusVINID = true;
                            result.IsSuccess = true;
                        }
                        else
                        {
                            message = $"{modelResponse.meta.message}";
                        }

                        result.Message = message;
                    }
                    else
                    {
                        modelLog.StatusVINID = false;
                        modelLog.ResponseXML = JsonConvert.SerializeObject(response);
                    }
                }
                catch (Exception ex)
                {
                    modelLog.ResponseXML = JsonConvert.SerializeObject(ex);
                    modelLog.StatusVINID = false;
                }

                logVID.WriteLogAPI(modelLog);
            }
            return result;
        }
    }
}
