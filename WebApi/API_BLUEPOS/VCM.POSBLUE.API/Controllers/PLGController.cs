using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using TCX.API.Common.Common;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Models;
using VCM.POSBLUE.API.GiftBox;
using VCM.POSBLUE.API.Giftee;
using VCM.POSBLUE.API.GotIt;
using VCM.POSBLUE.API.Models;
using VCM.POSBLUE.API.SAPOdoo;
using VCM.POSBLUE.API.Urbox;
using VCM.POSBLUE.Business.PLG;
using VCM.POSBLUE.Business.VINID;
using VCM.POSBLUE.Data.PLG;
using VCM.POSBLUE.Model.GiftBox;
using VCM.POSBLUE.Model.Giftee;
using VCM.POSBLUE.Model.GotIt;
using VCM.POSBLUE.Model.Odoo;


namespace VCM.POSBLUE.API.Controllers
{

    [RoutePrefix("api/plg")]
    public class PLGController : ApiController
    {
        readonly string UrBoxUrl = WebConfigurationManager.AppSettings["UrBoxUrl"];
        readonly string UrBoxSecret = WebConfigurationManager.AppSettings["UrBox_Secret"];
        readonly string UrBoxId = WebConfigurationManager.AppSettings["UrBox_Id"];


        readonly string GiftBoxUrl = WebConfigurationManager.AppSettings["GiftBoxUrl"];
        readonly string GiftBoxAuthKey = WebConfigurationManager.AppSettings["GiftBox_authKey"];
        readonly string GiftBoxBrand = WebConfigurationManager.AppSettings["GiftBox_BrandCode"];

        readonly string GotItUrl = WebConfigurationManager.AppSettings["GotItUrl"];

        readonly string GifteeUrl = WebConfigurationManager.AppSettings["GifteeUrl"];
        readonly string GifteeToken = WebConfigurationManager.AppSettings["GifteeToken"];

        readonly string CXUrl = WebConfigurationManager.AppSettings["CXUrl"];
        readonly string CXUser = WebConfigurationManager.AppSettings["CXUser"];
        readonly string CXPassword = WebConfigurationManager.AppSettings["CXPassword"];

        readonly string OdooUser = WebConfigurationManager.AppSettings["OdooUser"];
        readonly string OdooPassword = WebConfigurationManager.AppSettings["OdooPassword"];
        readonly string OdooEndPoint = WebConfigurationManager.AppSettings["OdooEndPoint"];

        private CX_LogAPIModel modelLogCX { get; set; }
        private VinIDBLO logVID { get; set; }
        SI_VC_OUTService SAPOdoo { get; set; }

        // UrlBox có 2 function(check, used): done
        // GiftBox có 3 function(check, used, cancle): Chưa làm cancle
        // Gotit có 2 function (check, used): done
        // Giftee: pending

        private VoucherCardLogModel logVC { get; set; }
        private VoucherCardTransModel voucherTrans { get; set; }
        private List<VoucherCardTransModel> listVoucherTrans { get; set; }

        private IPLGBLO pLGBLO { get; set; }
        public PLGController()
        {
            pLGBLO = new PLGBLO();
            logVC = new VoucherCardLogModel();
            voucherTrans = new VoucherCardTransModel();
            listVoucherTrans = new List<VoucherCardTransModel>();
            logVID = new VinIDBLO();
            modelLogCX = new CX_LogAPIModel();
            SAPOdoo = new SI_VC_OUTService
            {
                Credentials = new NetworkCredential(OdooUser, OdooPassword)
            };
        }

        [HttpPost]
        [Route("GetInfoCard")]
        public HttpResponseMessage GetInfoCard(PLCheckVoucherRequest model)
        {
            #region Check Authorization

            var checkAuthen = AuthenAPI.AuthorizationBasic(ActionContext);
            if (checkAuthen.StatusCode != HttpStatusCode.OK)
            {
                return Request.CreateResponse(checkAuthen.StatusCode, new ResultResponse
                {
                    Data = null,
                    Message = $"Authorization has been denied for this request",
                    Status = checkAuthen.StatusCode
                });
            }
            #endregion


            if (string.IsNullOrEmpty(model.partner))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Vui lòng nhập partner",
                    Status = HttpStatusCode.BadRequest
                });
            }
            if (model.listSeriNo.Any(d => string.IsNullOrEmpty(d.seriNo)))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"Vui lòng nhập serialNo",
                    Status = HttpStatusCode.BadRequest
                });
            }

            try
            {
                if (model.partner == "PLG")
                {
                    return PL_GetInfoCard(model.listSeriNo.FirstOrDefault().seriNo);
                }
                else if (model.partner == "UrBox")
                {
                    return UrBoxCheck(model);
                }
                else if (model.partner == "GiftBox")
                {
                    var result = GiftBox_mutilVoucherStatus(model);
                    return result;
                    // return GiftBox_voucherStatus(model);
                }
                else if (model.partner == "GotIt")
                {
                    var result = GotIt_MultilCheckVoucher(model);
                    return result;
                    //var modelGI = new GotItCheckPOSRequest
                    //{
                    //    partner = model.partner,
                    //    serialNo = serialNo,
                    //    storeNo = model.storeNo,
                    //    posID = model.posID
                    //};

                    //return GotItCheck(modelGI);
                }
                else if (model.partner == "Giftee")
                {
                    var modelGT = new GTStatusVoucherRequest
                    {
                        StoreNo = model.storeNo,
                        PosTerminal = model.posID,
                        //SeriNo = serialNo
                    };

                    return Giftee_statusTickets(modelGT);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Không tìm thấy nhà cung cấp",
                        Status = HttpStatusCode.BadRequest
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpPut]
        [Route("SaleVoucher")]
        public HttpResponseMessage SaleVoucher(SaleVoucherRequestPOS model)
        {
            try
            {
                #region Check Authorization

                var checkAuthen = AuthenAPI.AuthorizationBasic(ActionContext);
                if (checkAuthen.StatusCode != HttpStatusCode.OK)
                {
                    return Request.CreateResponse(checkAuthen.StatusCode, new ResultResponse
                    {
                        Data = null,
                        Message = $"Authorization has been denied for this request",
                        Status = checkAuthen.StatusCode
                    });
                }

                #endregion
                if (model == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Dữ liệu đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"CH/ST đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (string.IsNullOrEmpty(model.posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Mã POS đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.salePrice <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Vui lòng nhập giá bán voucher",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.partner))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Partner đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.partner.ToUpper() != "PLG")
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Chỉ partner PLG mới bán được voucher ở POS",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;

                var data = pLGBLO.SaleVoucher(model);
                var responsePOS = new POSResponse
                {
                    Status = (int)data.Status,
                    Message = data.Message
                };

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                var logCard = new VoucherCardLogModel
                {
                    Partner = "PLG",
                    StoreNo = model.storeNo,
                    PosID = model.posID,
                    SeriNo = model.seriNo,
                    // Value = model.value,
                    FunctionName = "SaleVoucher",
                    Status = (int)data.Status == 200,
                    PosRequest = JsonConvert.SerializeObject(model),
                    PosResponse = JsonConvert.SerializeObject(responsePOS),
                    MsgError = data.Message,
                    StatusCode = (int)data.Status,
                    UserName = model.staffCode,
                    TimeResponse = (int)milliseconds,

                };


                if (data.Status == HttpStatusCode.OK)
                {
                    var dataResponse = JsonConvert.DeserializeObject<VoucherCardTransResponse>(JsonConvert.SerializeObject(data.Data));
                    logCard.Value = dataResponse.Value;
                    Task.Run(() => pLGBLO.LogVoucher(logCard));

                    //voucherTrans = new VoucherCardTransModel
                    //{
                    //    Partner = model.partner,
                    //    SeriNo = model.seriNo,
                    //    StoreNo = model.storeNo,
                    //    PosID = model.posID,
                    //    Status = true,
                    //    Value = model.value,
                    //    FunctionName = "SaleVoucher",
                    //    UserName = model.staffCode
                    //};
                    //Task.Run(() => pLGBLO.VoucherTrans(voucherTrans));

                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = dataResponse,
                        Message = data.Message,
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    Task.Run(() => pLGBLO.LogVoucher(logCard));
                    return Request.CreateResponse(data.Status, new ResultResponse
                    {
                        Data = null,
                        Message = data.Message,
                        Status = data.Status
                    });
                }
            }
            catch (Exception ex)
            {
                var logCard = new VoucherCardLogModel
                {
                    Partner = "PLG",
                    StoreNo = model.storeNo,
                    PosID = model.posID,
                    SeriNo = model.seriNo,
                    Value = 0,
                    FunctionName = "SaleVoucher",
                    Status = false,
                    PosRequest = JsonConvert.SerializeObject(model),
                    PosResponse = JsonConvert.SerializeObject(ex),
                    MsgError = ex.Message,
                    StatusCode = 4000,
                    UserName = model.staffCode,
                    TimeResponse = 0
                };
                Task.Run(() => pLGBLO.LogVoucher(logCard));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }

        }

        [HttpPost]
        [Route("RedeemVoucher")]
        public HttpResponseMessage RedeemVoucher(ReedemVoucherRequestPOS model)
        {
            try
            {
                #region Check Authorization

                var checkAuthen = AuthenAPI.AuthorizationBasic(ActionContext);
                if (checkAuthen.StatusCode != HttpStatusCode.OK)
                {
                    return Request.CreateResponse(checkAuthen.StatusCode, new ResultResponse
                    {
                        Data = null,
                        Message = $"Authorization has been denied for this request",
                        Status = checkAuthen.StatusCode
                    });
                }
                #endregion

                if (string.IsNullOrEmpty(model.partner))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Vui lòng nhập partner",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.listSeriNo == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Vui lòng nhập SeriNo",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (string.IsNullOrEmpty(model.storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Vui lòng nhập StoreNo",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                if (model.listSeriNo != null && model.listSeriNo.Count > 0)
                {
                    if (model.listSeriNo.Any(d => d.value < 0))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = $"Vui lòng nhập giá trị voucher > 0",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
                if (model.partner == "PLG")
                {
                    return PL_RedeemVoucher(model);
                }
                else if (model.partner == "UrBox")
                {
                    return UrBox_Payment(model);
                }
                else if (model.partner == "GiftBox")
                {
                    return GiftBox_voucherUsed(model);
                }
                else if (model.partner == "GotIt")
                {
                    return GotItUsed(model);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = $"Không tìm thấy partner",
                        Status = HttpStatusCode.BadRequest
                    });
                }

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        #region Phúc Long
        public HttpResponseMessage PL_GetInfoCard(string serialNo)
        {
            try
            {
                var data = pLGBLO.InforVoucher(serialNo);
                if (data.Status == HttpStatusCode.OK)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = data.Data,
                        Message = "Voucher hợp lệ",
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(data.Status, new ResultResponse
                    {
                        Message = data.Message,
                        Status = data.Status,
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        public HttpResponseMessage PL_RedeemVoucher(ReedemVoucherRequestPOS model)
        {
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;
                var data = pLGBLO.RedeemVoucher(model);

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                var responsePOS = new POSResponse
                {
                    Status = (int)data.Status,
                    Message = data.Message
                };

                Task.Run(() => pLGBLO.LogVoucher(new VoucherCardLogModel
                {
                    Partner = model.partner,
                    StoreNo = model.storeNo,
                    PosID = model.posID,
                    SeriNo = string.Join(",", model.listSeriNo.Select(a => a.seriNo)),
                    Value = model.listSeriNo.Count > 1 ? 0 : model.listSeriNo.FirstOrDefault().value,
                    FunctionName = "RedeemVoucher",
                    Status = (int)data.Status == 200,
                    PosRequest = JsonConvert.SerializeObject(model),
                    PosResponse = JsonConvert.SerializeObject(responsePOS),
                    MsgError = data.Message,
                    StatusCode = (int)data.Status,
                    UserName = model.staffCode,
                    TimeResponse = (int)milliseconds,

                }));

                if (data.Status == HttpStatusCode.OK)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                    {
                        Data = null,
                        Message = data.Message,
                        Status = HttpStatusCode.OK
                    });
                }
                else
                {
                    return Request.CreateResponse(data.Status, new ResultResponse
                    {
                        Data = null,
                        Message = data.Message,
                        Status = data.Status
                    });
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        #endregion

        #region SAP Odoo

        [HttpPost]
        [Route("Check_VC_Odoo")]
        public HttpResponseMessage Check_VC_Odoo(PLCheckVoucherRequest posModel)
        {
            try
            {
                var listResponse = OdooSAP_Task_Check_Voucher(posModel);

                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = listResponse,
                    Message = "OK",
                    Status = HttpStatusCode.OK
                });

                //HttpWebRequest request = CreateRequestSOAP.CreateWebRequestSOAP(OdooEndPoint, OdooUser, OdooPassword);

                //XmlDocument SOAPReqBody = new XmlDocument();

                //foreach (var item in posModel.listSeriNo)
                //{
                //    var model = new OdooCheckVCRequest
                //    {
                //        isVoucher = "X",
                //        serialNumber = item.seriNo
                //    };

                //    SOAPReqBody.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                //    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:phuclong:s4:voucher:to:odoo:ws"">  
                //     <soap:Body>  
                //            <urn:MT_ODOO_VC_CHECK_REQUEST>
                //                <REQUEST>
                //                    <ISVOUCHER>" + model.isVoucher + @"</ISVOUCHER>
                //                    <SERIALNUMBER>" + model.serialNumber + @"</SERIALNUMBER>
                //                </REQUEST>
                //            </urn:MT_ODOO_VC_CHECK_REQUEST>                
                //      </soap:Body>  
                //    </soap:Envelope>");

                //    using (Stream stream = request.GetRequestStream())
                //    {
                //        SOAPReqBody.Save(stream);
                //    }
                //    using (WebResponse response = request.GetResponse())
                //    {
                //        using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                //        {
                //            string soapResult = rd.ReadToEnd();
                //            var doc = XDocument.Parse(soapResult);
                //            var nodes = doc.Root.Descendants("RESPONSE").FirstOrDefault();
                //            var jsonNode = JsonConvert.SerializeObject(nodes);

                //            var convertObject = JsonConvert.DeserializeObject<OdooXMLCheckVoucherResponse>(jsonNode);
                //            var data = convertObject.RESPONSE;

                //            if (!string.IsNullOrEmpty(data.STATUSCODE))
                //            {
                //                var result = new InforVoucherResponse
                //                {
                //                    SeriNo = data.SERIALNUMBER,
                //                    TypeVoucher = model.isVoucher.ToUpper() == "X" ? "Voucher" : "Prepaid",
                //                    StatusVoucher = data.STATUSCODE == "AVAI" ? ConstStatus.N : data.STATUSCODE == "SOLD" ? ConstStatus.A : data.STATUSCODE == "REDE" ? ConstStatus.R : data.STATUSCODE == "EXPI" ? ConstStatus.E : ConstStatus.I,
                //                    DescVoucher = data.STATUSCODE == "AVAI" ? "Chưa kích hoạt" : data.STATUSCODE == "SOLD" ? "Đã kích hoạt" : data.STATUSCODE == "REDE" ? "Đã sử dụng" : data.STATUSCODE == "EXPI" ? "Đã hết hạn" : ConstStatus.I,
                //                    Value = double.Parse(data.VALUE)
                //                };

                //                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                //                {
                //                    Data = result,
                //                    Message = "OK",
                //                    Status = HttpStatusCode.OK
                //                });
                //            }
                //            else
                //            {
                //                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                //                {
                //                    Data = null,
                //                    Message = data.STATUSMESSAGE,
                //                    Status = HttpStatusCode.BadRequest
                //                });
                //            }
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        public List<InforVoucherResponse> OdooSAP_Task_Check_Voucher(PLCheckVoucherRequest posModel)
        {
            var result = new List<InforVoucherResponse>();
            try
            {
                var tasks = new List<Task>();
                foreach (var item in posModel.listSeriNo)
                {
                    var model = new OdooCheckPOSRequest
                    {
                        partner = posModel.partner,
                        storeNo = posModel.storeNo,
                        posID = posModel.posID,
                        seriNo = item.seriNo
                    };

                    var call = OdooSAP_Check_Call_WS(model);
                    result.Add(call);

                    //tasks.Add(Task.Run(() =>
                    //{
                    //    var call = OdooSAP_Call_WS(model);
                    //    result.Add(call);

                    //}));
                }

                //Task taskW = Task.WhenAll(tasks.ToArray());
                //await taskW;
            }
            catch
            {
                return null;
            }

            return result;
        }
        public InforVoucherResponse OdooSAP_Check_Call_WS_BK(OdooCheckPOSRequest posModel)
        {
            var result = new InforVoucherResponse
            {
                SeriNo = posModel.listVoucher.FirstOrDefault().seriNo
            };

            var model = new OdooCheckVCRequest
            {
                isVoucher = "X",
                serialNumber = posModel.listVoucher.FirstOrDefault().seriNo
            };
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;

                HttpWebRequest request = CreateRequestSOAP.CreateWebRequestSOAP(OdooEndPoint, OdooUser, OdooPassword);
                XmlDocument SOAPReqBody = new XmlDocument();

                SOAPReqBody.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>  
                    <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:phuclong:s4:voucher:to:odoo:ws"">  
                     <soap:Body>  
                            <urn:MT_ODOO_VC_CHECK_REQUEST>
                                <REQUEST>
                                    <ISVOUCHER>" + model.isVoucher + @"</ISVOUCHER>
                                    <SERIALNUMBER>" + model.serialNumber + @"</SERIALNUMBER>
                                </REQUEST>
                            </urn:MT_ODOO_VC_CHECK_REQUEST>
                      </soap:Body>  
                    </soap:Envelope>");

                using (Stream stream = request.GetRequestStream())
                {
                    SOAPReqBody.Save(stream);
                }

                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        string soapResult = rd.ReadToEnd();
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        logVC = new VoucherCardLogModel
                        {
                            Partner = "PLG",
                            StoreNo = posModel.storeNo,
                            PosID = posModel.posID,
                            SeriNo = model.serialNumber,
                            Value = 0,
                            FunctionName = "GetInfoCard",
                            Status = false,
                            PosRequest = JsonConvert.SerializeObject(posModel),
                            PosResponse = "",
                            PartnerRequest = JsonConvert.SerializeObject(model),
                            PartnerResponse = soapResult,
                            MsgError = "",
                            StatusCode = 0,
                            UserName = "",
                            TimeResponse = (int)milliseconds,
                        };

                        var doc = XDocument.Parse(soapResult);
                        var nodes = doc.Root.Descendants("RESPONSE").FirstOrDefault();
                        var jsonNode = JsonConvert.SerializeObject(nodes);

                        var convertObject = JsonConvert.DeserializeObject<OdooXMLCheckVoucherResponse>(jsonNode);
                        var data = convertObject.RESPONSE;

                        logVC.PosResponse = JsonConvert.SerializeObject(data);

                        if (!string.IsNullOrEmpty(data.STATUSCODE))
                        {
                            logVC.StatusCode = 200;
                            logVC.Value = double.Parse(string.IsNullOrEmpty(data.VALUE) ? "0" : data.VALUE);
                            result = new InforVoucherResponse
                            {
                                SeriNo = data.SERIALNUMBER,
                                TypeVoucher = model.isVoucher.ToUpper() == "X" ? "Voucher" : "Prepaid",
                                StatusVoucher = data.STATUSCODE == "AVAI" ? ConstStatus.N : data.STATUSCODE == "SOLD" ? ConstStatus.A : data.STATUSCODE == "REDE" ? ConstStatus.R : data.STATUSCODE == "EXPI" ? ConstStatus.E : ConstStatus.I,
                                DescVoucher = data.STATUSCODE == "AVAI" ? "Voucher chưa kích hoạt" : data.STATUSCODE == "SOLD" ? "Voucher hợp lệ" : data.STATUSCODE == "REDE" ? "Voucher đã sử dụng" : data.STATUSCODE == "EXPI" ? "Voucher đã hết hạn" : ConstStatus.I,
                                Value = double.Parse(string.IsNullOrEmpty(data.VALUE) ? "0" : data.VALUE)
                            };
                        }
                        else
                        {
                            logVC.MsgError = data.STATUSMESSAGE;
                            result = new InforVoucherResponse
                            {
                                SeriNo = model.serialNumber,
                                Value = 0,
                                DescVoucher = data.STATUSMESSAGE
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.Status = false;
                logVC.MsgError = JsonConvert.SerializeObject(ex);

                result = new InforVoucherResponse
                {
                    SeriNo = model.serialNumber,
                    Value = 0,
                    DescVoucher = ex.Message
                };
                logVC.PosResponse = JsonConvert.SerializeObject(result);

            }
            Task.Run(() => pLGBLO.LogVoucher(logVC));
            return result;
        }
        public InforVoucherResponse OdooSAP_Check_Call_WS(OdooCheckPOSRequest posModel)
        {
            var result = new InforVoucherResponse
            {
                SeriNo = posModel.seriNo
            };

            var model = new OdooCheckVCRequest
            {
                isVoucher = "X",
                serialNumber = posModel.seriNo
            };
            try
            {
                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;

                DT_ODOO_VC_CHECK_REQUEST requestCheck = new DT_ODOO_VC_CHECK_REQUEST
                {
                    REQUEST = new DT_ODOO_VC_CHECK_REQUESTREQUEST
                    {
                        ISVOUCHER = "X",
                        SERIALNUMBER = posModel.seriNo
                    }
                };

                var response = SAPOdoo.SI_VC_CHECK_OUT(requestCheck)[0];

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                var jsonResponse = JsonConvert.SerializeObject(response);
                var data = JsonConvert.DeserializeObject<CheckResponse>(jsonResponse);

                logVC = new VoucherCardLogModel
                {
                    Partner = "PLG",
                    StoreNo = posModel.storeNo,
                    PosID = posModel.posID,
                    SeriNo = model.serialNumber,
                    Value = 0,
                    FunctionName = "GetInfoCard",
                    Status = false,
                    PosRequest = JsonConvert.SerializeObject(posModel),
                    PosResponse = "",
                    PartnerRequest = JsonConvert.SerializeObject(model),
                    PartnerResponse = jsonResponse,
                    MsgError = "",
                    StatusCode = 0,
                    UserName = "",
                    TimeResponse = (int)milliseconds,
                };

                logVC.PosResponse = JsonConvert.SerializeObject(data);

                if (!string.IsNullOrEmpty(data.STATUSCODE))
                {
                    logVC.StatusCode = 200;
                    logVC.Value = double.Parse(string.IsNullOrEmpty(data.VALUE) ? "0" : data.VALUE);
                    result = new InforVoucherResponse
                    {
                        SeriNo = model.serialNumber,
                        TypeVoucher = model.isVoucher.ToUpper() == "X" ? "Voucher" : "Prepaid",
                        StatusVoucher = data.STATUSCODE == "AVAI" ? ConstStatus.N : data.STATUSCODE == "SOLD" ? ConstStatus.A : data.STATUSCODE == "REDE" ? ConstStatus.R : data.STATUSCODE == "EXPI" ? ConstStatus.E : ConstStatus.I,
                        DescVoucher = data.STATUSCODE == "AVAI" ? "Voucher chưa kích hoạt" : data.STATUSCODE == "SOLD" ? "Voucher hợp lệ" : data.STATUSCODE == "REDE" ? "Voucher đã sử dụng" : data.STATUSCODE == "EXPI" ? "Voucher đã hết hạn" : ConstStatus.I,
                        Value = double.Parse(string.IsNullOrEmpty(data.VALUE) ? "0" : data.VALUE)
                    };
                }
                else
                {
                    logVC.MsgError = data.STATUSMESSAGE;
                    result = new InforVoucherResponse
                    {
                        SeriNo = model.serialNumber,
                        Value = 0,
                        DescVoucher = data.STATUSMESSAGE
                    };
                }
            }
            catch (Exception ex)
            {
                logVC.Status = false;
                logVC.MsgError = JsonConvert.SerializeObject(ex);

                result = new InforVoucherResponse
                {
                    SeriNo = model.serialNumber,
                    Value = 0,
                    DescVoucher = ex.Message
                };
                logVC.PosResponse = JsonConvert.SerializeObject(result);

            }
            Task.Run(() => pLGBLO.LogVoucher(logVC));
            return result;
        }

        [HttpPost]
        [Route("OdooSAP_Update_Voucher")]
        public HttpResponseMessage OdooSAP_Update_Voucher(OdooUpdateVoucherRequest model)
        {
            try
            {

                var st1 = new Stopwatch();
                st1.Start();
                long milliseconds = 0;

                DT_ODOO_VC_UPDATE_REQUEST requestUpdate = new DT_ODOO_VC_UPDATE_REQUEST
                {
                    REQUEST = new DT_ODOO_VC_UPDATE_REQUESTREQUEST
                    {
                        SERIALNUMBER = model.seriNo,
                        ISVOUCHER = model.isVoucher,
                        STATUSCODE = model.statusCode,
                        SALESPRICE = model.salePrice,
                        SALESDATE = string.Format("{0:yyyyMMdd}", DateTime.Now),
                        SALESPLANT = model.salePlant,
                        SALESTRANS = model.saleTrans,
                        DISCOUNT = model.discount,
                        REDEEMAMOUNT = model.redeemAmount,
                        REDEEMDATE = model.redeemDate,
                        REDEEMPLANT = model.redeemPlant,
                        REDEEMTRANS = model.redeemTrans
                    }
                };

                var responseWS = SAPOdoo.SI_VC_UPDATE_OUT(requestUpdate).RESPONSE;
                var responseJSon = JsonConvert.SerializeObject(responseWS);
                var data = JsonConvert.DeserializeObject<OdooUpdateVCResponse>(responseJSon);

                milliseconds = st1.ElapsedMilliseconds;
                st1.Stop();

                logVC = new VoucherCardLogModel
                {
                    Partner = "SAPOdoo",
                    StoreNo = "",
                    PosID = "",
                    SeriNo = "",
                    Value = 0,
                    FunctionName = "SaleVoucher",
                    Status = false,
                    PosRequest = JsonConvert.SerializeObject(model),
                    PosResponse = "",
                    PartnerRequest = JsonConvert.SerializeObject(model),
                    PartnerResponse = responseJSon,
                    MsgError = "",
                    StatusCode = 0,
                    UserName = "",
                    TimeResponse = (int)milliseconds,
                };

                return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                {
                    Data = data,
                    Message = "OK",
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception ex)
            {
                logVC.Status = false;
                logVC.MsgError = JsonConvert.SerializeObject(ex);

                //result = new InforVoucherResponse
                //{
                //    SeriNo = model.serialNumber,
                //    Value = 0,
                //    DescVoucher = ex.Message
                //};
                //logVC.PosResponse = JsonConvert.SerializeObject(result);

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = JsonConvert.SerializeObject(ex),
                    Status = HttpStatusCode.InternalServerError
                });
            }
            //Task.Run(() => pLGBLO.LogVoucher(logVC));
            //return result;

        }

        #endregion

        #region UrBox

        //[HttpPost]
        //[Route("UrBoxCheck")]
        public HttpResponseMessage UrBoxCheck(PLCheckVoucherRequest modelPOS)
        {
            try
            {
                var model = new RequestCheckUrboxModel
                {
                    amount = 0,
                    code = string.Join(",", modelPOS.listSeriNo.Select(a => a.seriNo)),
                    //staff_id = "",
                    store_id = modelPOS.storeNo,
                    terminal_id = modelPOS.posID

                };

                using (var client = new HttpClient())
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;
                    var endPoint = UrlUrBox.UrBoxCheck;

                    var jsonEncode = JsonConvert.SerializeObject(model);
                    var signature = UrBox.SignData(jsonEncode);

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(UrBoxUrl);

                    client.DefaultRequestHeaders.Add("app-id", UrBoxId);
                    client.DefaultRequestHeaders.Add("app-secret", UrBoxSecret);
                    client.DefaultRequestHeaders.Add("Signature", signature);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

                    var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    logVC = new VoucherCardLogModel
                    {
                        Partner = "UrBox",
                        StoreNo = model.store_id,
                        PosID = model.terminal_id,
                        SeriNo = model.code,
                        Value = model.amount,
                        FunctionName = "GetInfoCard",
                        Status = false,
                        PosRequest = JsonConvert.SerializeObject(modelPOS),
                        PosResponse = "",
                        PartnerRequest = JsonConvert.SerializeObject(model),
                        PartnerResponse = "",
                        MsgError = "",
                        StatusCode = 0,
                        UserName = "",
                        TimeResponse = (int)milliseconds,
                    };


                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var resultResponse = JsonConvert.DeserializeObject<List<ResponseCheckUrboxModel>>(readData.Result);

                        var resultDataFirst = resultResponse.FirstOrDefault();

                        logVC.Status = resultDataFirst.done == 1;

                        logVC.PartnerResponse = JsonConvert.SerializeObject(resultResponse);
                        logVC.MsgError = resultDataFirst.data.msg;
                        logVC.StatusCode = (int)resultDataFirst.status;


                        var data = resultResponse.Select(a => new InforVoucherResponse
                        {
                            SeriNo = a.data.code,
                            TypeVoucher = "" + a.type,//// resultData.type == 1 ? "Voucher item" : resultData.type == 2 ? "Voucher fixed amount" : resultData.type == 3 ? "Voucher unfixed amount" : "",
                            StatusVoucher = a.status == 101 ? ConstStatus.A : a.status == 205 ? ConstStatus.R : a.status == 206 ? ConstStatus.E : a.status == 207 ? ConstStatus.D : ConstStatus.I,
                            DescVoucher = a.data.msg,
                            Value = double.Parse(a.data.amount.ToString())
                        }).ToList();
                        var result = new ResultResponse
                        {
                            Data = data,
                            Message = "",
                            Status = HttpStatusCode.OK
                        };
                        logVC.PosResponse = JsonConvert.SerializeObject(result);

                        Task.Run(() => pLGBLO.LogVoucher(logVC));

                        return Request.CreateResponse(HttpStatusCode.OK, result);

                    }
                    else
                    {
                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = response.ReasonPhrase,
                            Status = response.StatusCode
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.Partner = "UrBox";
                logVC.StoreNo = modelPOS.storeNo;
                logVC.PosID = modelPOS.posID;
                logVC.SeriNo = string.Join(",", modelPOS.listSeriNo.Select(a => a.seriNo));
                logVC.Value = 0;
                logVC.FunctionName = "GetInfoCard";
                logVC.Status = false;
                logVC.PosRequest = JsonConvert.SerializeObject(modelPOS);

                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.MsgError = "Error";
                logVC.StatusCode = 0;

                Task.Run(() => pLGBLO.LogVoucher(logVC));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        //[HttpPost]
        //[Route("UrBoxPayment")]
        public HttpResponseMessage UrBox_Payment(ReedemVoucherRequestPOS posModel)
        {
            var listSeriNo = posModel.listSeriNo;
            var model = new RequestVoucherPaymentModel
            {
                amount = (int)posModel.totalBill,
                bill_id = posModel.orderNo,
                code = string.Join(",", listSeriNo.Select(a => a.seriNo)),// posModel.seriNo,//"UBxxxxxxxxxxxxxxxx,UBxxxxxxxxxxxxxxxx,UBxxxxxxxxxxxxxxxx"
                staff_id = posModel.staffCode,
                store_id = posModel.storeNo,
                terminal_id = posModel.posID,
                time = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
            };
            try
            {
                using (var client = new HttpClient())
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var endPoint = UrlUrBox.UrBoxPayment;

                    var jsonEncode = JsonConvert.SerializeObject(model);
                    var signature = UrBox.SignData(jsonEncode);

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(UrBoxUrl);

                    client.DefaultRequestHeaders.Add("app-id", UrBoxId);
                    client.DefaultRequestHeaders.Add("app-secret", UrBoxSecret);
                    client.DefaultRequestHeaders.Add("Signature", signature);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

                    var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    logVC = new VoucherCardLogModel
                    {
                        Partner = "UrBox",
                        StoreNo = model.store_id,
                        PosID = model.terminal_id,
                        SeriNo = string.Join(",", listSeriNo.Select(a => a.seriNo)),
                        // Value = listSeriNo.Count > 1 ? 0 : model.amount,
                        FunctionName = "RedeemVoucher",
                        Status = false,
                        PosRequest = JsonConvert.SerializeObject(posModel),
                        PosResponse = "",
                        PartnerRequest = JsonConvert.SerializeObject(model),
                        PartnerResponse = "",
                        MsgError = "",
                        StatusCode = 0,
                        UserName = model.staff_id,
                        TimeResponse = (int)milliseconds,
                    };

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();

                        var responseUrBox = readData.Result;
                        var listVC = new List<UBResponseVoucherModel>();
                        var resultData = new ResponseVoucherPaymentModel();
                        //if (listSeriNo.Count != 1)
                        //{
                        //    resultData = JsonConvert.DeserializeObject<ResponseVoucherPaymentModel>(readData.Result);
                        //    listVC.Add(new UBResponseVoucherModel
                        //    {
                        //        code = resultData.data.code,
                        //        amount = resultData.data.amount
                        //    });
                        //}
                        //else
                        //{
                        var checkListOrFirst = responseUrBox.Contains("[{");
                        if (checkListOrFirst)
                        {
                            var listObject = JsonConvert.DeserializeObject<List<ResponseVoucherPaymentModel>>(readData.Result);

                            listVC.AddRange(listObject.Select(a => new UBResponseVoucherModel
                            {
                                code = a.data.code,
                                amount = a.data.amount
                            }));

                            resultData = listObject.FirstOrDefault();
                        }
                        else
                        {
                            resultData = JsonConvert.DeserializeObject<ResponseVoucherPaymentModel>(readData.Result);
                            listVC.Add(new UBResponseVoucherModel
                            {
                                code = resultData.data.code,
                                amount = resultData.data.amount
                            });
                        }
                        //}

                        logVC.Status = resultData.done == 1;
                        logVC.PartnerResponse = JsonConvert.SerializeObject(resultData);
                        logVC.MsgError = resultData.data.msg;
                        logVC.StatusCode = (int)resultData.status;

                        if (resultData.done == 1)
                        {
                            var result = new ResultResponse
                            {
                                Data = null,
                                Message = $"Thanh toán thành công {listSeriNo.Count} voucher",
                                Status = HttpStatusCode.OK
                            };
                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            listVoucherTrans.AddRange(listVC.Select(a => new VoucherCardTransModel
                            {
                                Partner = posModel.partner,
                                SeriNo = a.code,
                                StoreNo = posModel.storeNo,
                                PosID = posModel.posID,
                                Status = true,
                                Value = a.amount,
                                FunctionName = "RedeemVoucher",
                                UserName = posModel.staffCode
                            }));

                            Task.Run(() => pLGBLO.VoucherTrans(listVoucherTrans));

                            return Request.CreateResponse(HttpStatusCode.OK, result);
                        }
                        else
                        {
                            var result = new ResultResponse
                            {
                                Data = null,
                                Message = resultData.data.msg,
                                Status = HttpStatusCode.BadRequest
                            };

                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.BadRequest, result);
                        }
                    }
                    else
                    {
                        logVC.PosResponse = JsonConvert.SerializeObject(response);
                        logVC.PartnerResponse = JsonConvert.SerializeObject(response);
                        logVC.MsgError = response.ReasonPhrase;
                        logVC.StatusCode = 0;
                        Task.Run(() => pLGBLO.LogVoucher(logVC));

                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = response.ReasonPhrase,
                            Status = response.StatusCode
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.MsgError = "Error";
                logVC.StatusCode = 0;

                Task.Run(() => pLGBLO.LogVoucher(logVC));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion

        #region GiftBox

        //[HttpPost]
        //[Route("GiftBoxVoucherStatus")]

        public HttpResponseMessage GiftBox_mutilVoucherStatus(PLCheckVoucherRequest posModel)
        {
            var dataTask = new HttpResponseMessage();
            try
            {
                var data = GiftBox_CallAPIVoucher(posModel);

                var result = new ResultResponse
                {
                    Data = data,
                    Message = "",
                    Status = HttpStatusCode.OK
                };
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                var result = new ResultResponse
                {
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                };
                return Request.CreateResponse(HttpStatusCode.InternalServerError, result);
            }

        }

        public List<InforVoucherResponse> GiftBox_CallAPIVoucher(PLCheckVoucherRequest posModel)
        {
            var result = new List<InforVoucherResponse>();

            try
            {
                var endPoint = UrlGiftBox.GiftBoxStatus;
                var listSeriNo = posModel.listSeriNo;
                foreach (var item in listSeriNo)
                {
                    using (var client = new HttpClient())
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;
                        var model = new GBVoucherStatusRequest
                        {
                            authKey = GiftBoxAuthKey,
                            brandCode = GiftBoxBrand,
                            pinNo = item.seriNo,
                            storeCode = posModel.storeNo
                        };

                        var jsonEncode = JsonConvert.SerializeObject(model);
                        var signature = UrBox.SignData(jsonEncode);

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(GiftBoxUrl);
                        client.DefaultRequestHeaders.Add("Signature", signature);
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        logVC = new VoucherCardLogModel
                        {
                            Partner = "GiftBox",
                            StoreNo = model.storeCode,
                            PosID = model.storeCode,
                            SeriNo = model.pinNo,
                            Value = 0,
                            FunctionName = "GetInfoCard",
                            Status = false,
                            PosRequest = JsonConvert.SerializeObject(model),
                            PosResponse = "",
                            PartnerRequest = JsonConvert.SerializeObject(model),
                            PartnerResponse = "",
                            MsgError = "",
                            StatusCode = 0,
                            UserName = "",
                            TimeResponse = (int)milliseconds,
                        };

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            var resultResponse = JsonConvert.DeserializeObject<GBVoucherStatusResponse>(readData.Result);

                            logVC.Value = double.Parse(resultResponse.supplyPrice);
                            logVC.Status = resultResponse.resCode == "0000";

                            logVC.PartnerResponse = JsonConvert.SerializeObject(resultResponse);
                            logVC.MsgError = resultResponse.message;
                            logVC.StatusCode = resultResponse.resCode == "0000" ? 200 : int.Parse(resultResponse.resCode);

                            var data = new InforVoucherResponse
                            {
                                SeriNo = resultResponse.pinNo,
                                TypeVoucher = resultResponse.goodsType,//Voucher product type (discount, cash discount, product voucher, voucher book)
                                StatusVoucher = resultResponse.pinStatus == "R" ? ConstStatus.A : resultResponse.pinStatus == "U" ? ConstStatus.R : resultResponse.pinStatus == "F" ? ConstStatus.R : resultResponse.pinStatus == "C" ? ConstStatus.D : resultResponse.pinStatus == "L" ? ConstStatus.L : ConstStatus.I,
                                //R: un used U: used part of voucher F: already used C:cancel
                                DescVoucher = resultResponse.message,
                                Value = double.Parse(resultResponse.supplyPrice)
                            };
                            result.Add(data);
                            logVC.PosResponse = JsonConvert.SerializeObject(data);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                        }
                        else
                        {
                            var data = new InforVoucherResponse
                            {
                                SeriNo = item.seriNo,
                                TypeVoucher = "",
                                StatusVoucher = "",
                                DescVoucher = response.ReasonPhrase,
                                Value = 0
                            };
                            result.Add(data);
                            logVC.PosResponse = JsonConvert.SerializeObject(response);
                            logVC.PartnerResponse = JsonConvert.SerializeObject(response);
                            logVC.StatusCode = 0;
                            logVC.MsgError = response.ReasonPhrase;
                            Task.Run(() => pLGBLO.LogVoucher(logVC));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.StatusCode = 0;
                logVC.MsgError = ex.Message;

                var data = new InforVoucherResponse
                {
                    SeriNo = "",
                    TypeVoucher = "",
                    StatusVoucher = "",
                    DescVoucher = "",
                    Value = 0
                };
                result.Add(data);
                Task.Run(() => pLGBLO.LogVoucher(logVC));
            }

            return result;
        }

        public HttpResponseMessage GiftBox_voucherStatus(GBVoucherStatusRequest model)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var endPoint = UrlGiftBox.GiftBoxStatus;

                    var jsonEncode = JsonConvert.SerializeObject(model);
                    var signature = UrBox.SignData(jsonEncode);

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(GiftBoxUrl);
                    client.DefaultRequestHeaders.Add("Signature", signature);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    logVC = new VoucherCardLogModel
                    {
                        Partner = "GiftBox",
                        StoreNo = model.storeCode,
                        PosID = model.storeCode,
                        SeriNo = model.pinNo,
                        Value = 0,
                        FunctionName = "GetInfoCard",
                        Status = false,
                        PosRequest = JsonConvert.SerializeObject(model),
                        PosResponse = "",
                        PartnerRequest = JsonConvert.SerializeObject(model),
                        PartnerResponse = "",
                        MsgError = "",
                        StatusCode = 0,
                        UserName = "",
                        TimeResponse = (int)milliseconds,
                    };

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var resultResponse = JsonConvert.DeserializeObject<GBVoucherStatusResponse>(readData.Result);

                        logVC.Value = double.Parse(resultResponse.supplyPrice);
                        logVC.Status = resultResponse.resCode == "0000";

                        logVC.PartnerResponse = JsonConvert.SerializeObject(resultResponse);
                        logVC.MsgError = resultResponse.message;
                        logVC.StatusCode = resultResponse.resCode == "0000" ? 200 : int.Parse(resultResponse.resCode);

                        if (resultResponse.resCode == "0000")
                        {
                            var data = new InforVoucherResponse
                            {
                                SeriNo = resultResponse.pinNo,
                                TypeVoucher = resultResponse.goodsType,//Voucher product type (discount, cash discount, product voucher, voucher book)
                                StatusVoucher = resultResponse.pinStatus == "R" ? ConstStatus.A : resultResponse.pinStatus == "U" ? ConstStatus.R : resultResponse.pinStatus == "F" ? ConstStatus.R : resultResponse.pinStatus == "C" ? ConstStatus.D : resultResponse.pinStatus == "L" ? ConstStatus.L : ConstStatus.I,
                                //R: un used U: used part of voucher F: already used C:cancel
                                DescVoucher = resultResponse.message,
                                Value = double.Parse(resultResponse.supplyPrice)
                            };

                            var result = new ResultResponse
                            {
                                Data = data,
                                Message = "Voucher hợp lệ",
                                Status = HttpStatusCode.OK
                            };
                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.OK, result);
                        }
                        else
                        {
                            var result = new ResultResponse
                            {
                                Data = null,
                                Message = resultResponse.message,
                                Status = HttpStatusCode.BadRequest
                            };
                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.BadRequest, result);
                        }

                    }
                    else
                    {
                        logVC.PosResponse = JsonConvert.SerializeObject(response);
                        logVC.PartnerResponse = JsonConvert.SerializeObject(response);
                        logVC.StatusCode = 0;
                        logVC.MsgError = response.ReasonPhrase;
                        Task.Run(() => pLGBLO.LogVoucher(logVC));

                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = response.ReasonPhrase,
                            Status = response.StatusCode
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.StatusCode = 0;
                logVC.MsgError = ex.Message;
                Task.Run(() => pLGBLO.LogVoucher(logVC));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        //[HttpPost]
        //[Route("GiftBoxVoucherUsed")]
        public HttpResponseMessage GiftBox_voucherUsed(ReedemVoucherRequestPOS posModel)
        {
            try
            {
                var model = new GBVoucherUsedListRequest
                {
                    authKey = GiftBoxAuthKey,
                    brandCode = GiftBoxBrand,
                    pinNoList = posModel.listSeriNo.Select(a => new PinNo
                    {
                        pinNo = a.seriNo,
                        usePrice = (int)a.value
                    }).ToList(),
                    referenceNumber = posModel.orderNo,
                    storeCode = posModel.storeNo

                };
                using (var client = new HttpClient())
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var endPoint = UrlGiftBox.GiftBoxvoucherUsedList;
                    var jsonEncode = JsonConvert.SerializeObject(model);
                    var signature = UrBox.SignData(jsonEncode);

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(GiftBoxUrl);
                    client.DefaultRequestHeaders.Add("Signature", signature);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    logVC = new VoucherCardLogModel
                    {
                        Partner = "GiftBox",
                        StoreNo = model.storeCode,
                        PosID = model.storeCode,
                        SeriNo = string.Join(",", posModel.listSeriNo.Select(a => a.seriNo)),
                        Value = posModel.listSeriNo.Count > 1 ? 0 : posModel.listSeriNo.FirstOrDefault().value,
                        FunctionName = "RedeemVoucher",
                        Status = false,
                        PosRequest = JsonConvert.SerializeObject(posModel),
                        PosResponse = "",
                        PartnerRequest = JsonConvert.SerializeObject(model),
                        PartnerResponse = "",
                        MsgError = "",
                        StatusCode = 0,
                        UserName = posModel.staffCode,
                        TimeResponse = (int)milliseconds,
                    };

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var resultResponse = JsonConvert.DeserializeObject<GBVoucherUsedListResponse>(readData.Result);

                        //var result = new InforVoucherResponse
                        //{
                        //    CardType = "" + resultResponse.goodsType,
                        //    Value = double.Parse(resultResponse.supplyPrice)
                        //};

                        //logVC.Value = model.usePrice;
                        logVC.Status = resultResponse.resCodeTrans == "0000";
                        // logVC.PosResponse = JsonConvert.SerializeObject(result);
                        logVC.PartnerResponse = JsonConvert.SerializeObject(resultResponse);
                        logVC.MsgError = resultResponse.messageTrans;
                        logVC.StatusCode = resultResponse.resCodeTrans == "0000" ? 200 : int.Parse(resultResponse.resCodeTrans);


                        if (resultResponse.resCodeTrans == "0000")
                        {
                            var result = new ResultResponse
                            {
                                Data = null,
                                Message = $"Sử dụng thành công {resultResponse.pinInfo.Count} voucher",
                                Status = HttpStatusCode.OK
                            };

                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            var listResponseVC = resultResponse.pinInfo;
                            listVoucherTrans.AddRange(listResponseVC.Select(a => new VoucherCardTransModel
                            {
                                Partner = posModel.partner,
                                SeriNo = a.pinNo,
                                StoreNo = posModel.storeNo,
                                PosID = posModel.posID,
                                Status = true,
                                Value = a.listPrice,
                                FunctionName = "RedeemVoucher",
                                UserName = posModel.staffCode
                            }));
                            Task.Run(() => pLGBLO.VoucherTrans(listVoucherTrans));
                            return Request.CreateResponse(HttpStatusCode.OK, result);
                        }
                        else
                        {
                            var result = new ResultResponse
                            {
                                Data = null,
                                Message = resultResponse.messageTrans,
                                Status = HttpStatusCode.BadRequest
                            };
                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.BadRequest, result);
                        }
                    }
                    else
                    {
                        logVC.PosResponse = JsonConvert.SerializeObject(response);
                        logVC.PartnerResponse = JsonConvert.SerializeObject(response);
                        logVC.MsgError = response.ReasonPhrase;
                        logVC.StatusCode = 0;
                        Task.Run(() => pLGBLO.LogVoucher(logVC));

                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = response.ReasonPhrase,
                            Status = response.StatusCode
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.MsgError = "Error";
                logVC.StatusCode = 0;
                Task.Run(() => pLGBLO.LogVoucher(logVC));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion

        #region Giftee

        //[HttpGet]
        //[Route("statusTickets")]
        public HttpResponseMessage Giftee_statusTickets(GTStatusVoucherRequest model)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // var endPoint = UrlGiftee.GifteeCheck;
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var endPoint = $"{UrlGiftee.GifteeCheck}/{model.SeriNo}";

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(GifteeUrl);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GifteeToken);
                    client.DefaultRequestHeaders.Add("X-Giftee", "1");
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    var response = client.GetAsync(endPoint).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    logVC = new VoucherCardLogModel
                    {
                        Partner = "Giftee",
                        StoreNo = model.StoreNo,
                        PosID = model.PosTerminal,
                        SeriNo = model.SeriNo,
                        Value = 0,
                        FunctionName = "GetInfoCard",
                        Status = false,
                        PosRequest = JsonConvert.SerializeObject(model),
                        PosResponse = "",
                        PartnerRequest = $"{GifteeUrl}{endPoint}",
                        PartnerResponse = "",
                        MsgError = "",
                        StatusCode = 0,
                        UserName = "",
                        TimeResponse = (int)milliseconds,
                    };

                    var readData = response.Content.ReadAsStringAsync();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var resultData = JsonConvert.DeserializeObject<StatusReferenceResponse>(readData.Result);

                        logVC.Status = true;
                        logVC.PosResponse = "";
                        logVC.PartnerResponse = JsonConvert.SerializeObject(resultData);
                        logVC.MsgError = "";
                        logVC.StatusCode = 200;

                        Task.Run(() => pLGBLO.LogVoucher(logVC));

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = resultData,
                            Message = "Thành công",
                            Status = HttpStatusCode.OK
                        });

                    }
                    else
                    {
                        logVC.Status = false;
                        logVC.PosResponse = "";
                        logVC.PartnerResponse = JsonConvert.SerializeObject(readData.Result);
                        logVC.MsgError = "";
                        logVC.StatusCode = 400;

                        Task.Run(() => pLGBLO.LogVoucher(logVC));
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                        {
                            Data = null,
                            Message = "",
                            Status = HttpStatusCode.BadRequest
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.Status = false;
                logVC.PosResponse = "";
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.MsgError = ex.Message;
                logVC.StatusCode = (int)HttpStatusCode.InternalServerError;
                Task.Run(() => pLGBLO.LogVoucher(logVC));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        #endregion

        #region Got it
        //[HttpPost]
        //[Route("GotItCheck")]
        public HttpResponseMessage GotItCheck(GotItCheckPOSRequest posModel)
        {
            try
            {
                var model = new GotItVoucherCheckRequest
                {
                    pin = posModel.storeNo,
                    code = posModel.serialNo
                };

                using (var client = new HttpClient())
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var endPoint = UrlGotIt.GotItCheck;
                    var jsonEncode = JsonConvert.SerializeObject(model);

                    client.Timeout = TimeSpan.FromMinutes(2);

                    client.BaseAddress = new Uri(GotItUrl);
                    var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    logVC = new VoucherCardLogModel
                    {
                        Partner = "GotIt",
                        StoreNo = model.pin,
                        PosID = model.pin,
                        SeriNo = model.code,
                        Value = 0,
                        FunctionName = "GetInfoCard",
                        Status = false,
                        PosRequest = JsonConvert.SerializeObject(posModel),
                        PosResponse = "",
                        PartnerRequest = JsonConvert.SerializeObject(model),
                        PartnerResponse = "",
                        MsgError = "",
                        StatusCode = 0,
                        UserName = "",
                        TimeResponse = (int)milliseconds,
                    };

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var resultData = JsonConvert.DeserializeObject<GotItVoucherCheckResponse>(readData.Result);

                        logVC.Status = resultData.valid;
                        logVC.Value = resultData.products == null ? 0 : double.Parse(resultData.products.value);
                        logVC.PartnerResponse = JsonConvert.SerializeObject(resultData);
                        logVC.MsgError = resultData.message_vi;
                        logVC.StatusCode = resultData.valid == true ? 200 : 0;

                        if (resultData.valid == true)
                        {
                            var data = new InforVoucherResponse
                            {
                                TypeVoucher = resultData.products == null ? "" : resultData.products.type,//Loại voucher (c: voucher mệnh giá như tiền mặt, i: voucher quy đổi sản phẩm)
                                StatusVoucher = (resultData.state == 5 || resultData.state == 3) ? ConstStatus.A : ConstStatus.I,
                                Value = resultData.products == null ? 0 : double.Parse(resultData.products.value)
                            };
                            var result = new ResultResponse
                            {
                                Data = data,
                                Message = "Voucher hợp lệ",
                                Status = HttpStatusCode.OK
                            };

                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.OK, result);
                        }
                        else
                        {
                            var result = new ResultResponse
                            {
                                Data = null,
                                Message = resultData.message_vi,
                                Status = HttpStatusCode.BadRequest
                            };

                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.BadRequest, result);
                        }
                    }
                    else
                    {
                        logVC.PosResponse = JsonConvert.SerializeObject(response);
                        logVC.PartnerResponse = JsonConvert.SerializeObject(response);
                        logVC.MsgError = "Error";
                        logVC.StatusCode = 0;

                        Task.Run(() => pLGBLO.LogVoucher(logVC));

                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = response.ReasonPhrase,
                            Status = response.StatusCode
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.MsgError = "Error";
                logVC.StatusCode = 0;

                Task.Run(() => pLGBLO.LogVoucher(logVC));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        public HttpResponseMessage GotIt_MultilCheckVoucher(PLCheckVoucherRequest posModel)
        {
            var dataTask = new HttpResponseMessage();
            try
            {
                var data = GotIt_CallAPIVoucher(posModel);

                var result = new ResultResponse
                {
                    Data = data,
                    Message = "",
                    Status = HttpStatusCode.OK
                };
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                var result = new ResultResponse
                {
                    Message = ex.Message,
                    Status = HttpStatusCode.InternalServerError
                };
                return Request.CreateResponse(HttpStatusCode.InternalServerError, result);
            }

        }
        public List<InforVoucherResponse> GotIt_CallAPIVoucher(PLCheckVoucherRequest posModel)
        {
            var result = new List<InforVoucherResponse>();
            try
            {
                var endPoint = UrlGotIt.GotItCheck;
                var listSeriNo = posModel.listSeriNo;
                foreach (var item in listSeriNo)
                {
                    using (var client = new HttpClient())
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;
                        var model = new GotItVoucherCheckRequest
                        {
                            pin = posModel.storeNo,
                            code = item.seriNo
                        };

                        var jsonEncode = JsonConvert.SerializeObject(model);
                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(GotItUrl);
                        var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;


                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        logVC = new VoucherCardLogModel
                        {
                            Partner = "GotIt",
                            StoreNo = model.pin,
                            PosID = model.pin,
                            SeriNo = model.code,
                            Value = 0,
                            FunctionName = "GetInfoCard",
                            Status = false,
                            PosRequest = JsonConvert.SerializeObject(posModel),
                            PosResponse = "",
                            PartnerRequest = JsonConvert.SerializeObject(model),
                            PartnerResponse = "",
                            MsgError = "",
                            StatusCode = 0,
                            UserName = "",
                            TimeResponse = (int)milliseconds,
                        };

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var readData = response.Content.ReadAsStringAsync();
                            var resultData = JsonConvert.DeserializeObject<GotItVoucherCheckResponse>(readData.Result);

                            logVC.Status = resultData.valid;
                            logVC.PartnerResponse = JsonConvert.SerializeObject(resultData);
                            logVC.MsgError = resultData.message_vi;
                            logVC.StatusCode = resultData.valid == true ? 200 : 0;

                            if (resultData.valid == true)
                            {
                                var data = new InforVoucherResponse
                                {
                                    SeriNo = item.seriNo,
                                    DescVoucher = resultData.message_vi,
                                    TypeVoucher = resultData.products == null ? "" : resultData.products.type,//Loại voucher (c: voucher mệnh giá như tiền mặt, i: voucher quy đổi sản phẩm)
                                    StatusVoucher = (resultData.state == 5 || resultData.state == 3) ? ConstStatus.A : ConstStatus.I,
                                    Value = resultData.products == null ? 0 : double.Parse(resultData.products.value)
                                };

                                result.Add(data);
                                logVC.Value = resultData.products == null ? 0 : double.Parse(resultData.products.value);

                                logVC.PosResponse = JsonConvert.SerializeObject(data);
                                Task.Run(() => pLGBLO.LogVoucher(logVC));
                            }
                            else
                            {
                                var data = new InforVoucherResponse
                                {
                                    SeriNo = item.seriNo,
                                    DescVoucher = resultData.message_vi,
                                    TypeVoucher = resultData.products == null ? "" : resultData.products.type,//Loại voucher (c: voucher mệnh giá như tiền mặt, i: voucher quy đổi sản phẩm)
                                    StatusVoucher = resultData.state == 4 ? ConstStatus.R : resultData.state == 8 ? ConstStatus.E : resultData.state == 9 ? ConstStatus.D : ConstStatus.I,
                                    Value = resultData.products == null ? 0 : double.Parse(resultData.products.value)
                                };
                                result.Add(data);

                                logVC.PosResponse = JsonConvert.SerializeObject(data);
                                Task.Run(() => pLGBLO.LogVoucher(logVC));

                            }
                        }
                        else
                        {
                            var data = new InforVoucherResponse
                            {
                                SeriNo = item.seriNo,
                                DescVoucher = "Response api partner thất bại",
                                TypeVoucher = "",
                                StatusVoucher = ConstStatus.I,
                                Value = 0
                            };
                            logVC.PosResponse = JsonConvert.SerializeObject(data);
                            logVC.PartnerResponse = JsonConvert.SerializeObject(response);
                            logVC.MsgError = "Error";
                            logVC.StatusCode = 0;

                            result.Add(data);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var data = new InforVoucherResponse
                {
                    SeriNo = "",
                    TypeVoucher = "",
                    StatusVoucher = "",
                    DescVoucher = ex.Message,
                    Value = 0
                };
                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.StatusCode = 0;
                logVC.MsgError = ex.Message;

                result.Add(data);
                Task.Run(() => pLGBLO.LogVoucher(logVC));
            }

            return result;
        }

        //[HttpPost]
        //[Route("GotItUsed")]
        public HttpResponseMessage GotItUsed(ReedemVoucherRequestPOS posModel)
        {

            try
            {
                var model = new GotItVoucherUsedListRequest
                {
                    pin = posModel.storeNo,
                    code = posModel.listSeriNo.Select(a => a.seriNo).ToList(),
                    bill_number = posModel.orderNo,
                    total_bill = posModel.totalBill
                };

                using (var client = new HttpClient())
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    var endPoint = UrlGotIt.GotItUseList;
                    var jsonEncode = JsonConvert.SerializeObject(model);

                    client.Timeout = TimeSpan.FromMinutes(2);

                    client.BaseAddress = new Uri(GotItUrl);
                    var response = client.PostAsync(endPoint, new StringContent(jsonEncode, Encoding.UTF8, "application/json")).Result;

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    logVC = new VoucherCardLogModel
                    {
                        Partner = "GotIt",
                        StoreNo = model.pin,
                        PosID = model.pin,
                        SeriNo = string.Join(",", posModel.listSeriNo.Select(a => a.seriNo)),
                        Value = posModel.listSeriNo.Count > 0 ? 0 : posModel.listSeriNo.FirstOrDefault().value,
                        FunctionName = "RedeemVoucher",
                        Status = false,
                        PosRequest = JsonConvert.SerializeObject(posModel),
                        PosResponse = "",
                        PartnerRequest = JsonConvert.SerializeObject(model),
                        PartnerResponse = "",
                        MsgError = "",
                        StatusCode = 0,
                        UserName = posModel.staffCode,
                        TimeResponse = (int)milliseconds,
                    };

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var readData = response.Content.ReadAsStringAsync();
                        var checkResponse = JsonConvert.DeserializeObject<GotItVoucherUsedCheckResponse>(readData.Result);

                        if (string.IsNullOrEmpty(checkResponse.state))
                        {
                            logVC.Status = checkResponse.success;
                            logVC.Value = posModel.listSeriNo.Count > 1 ? 0 : posModel.listSeriNo.FirstOrDefault().value;
                            logVC.PartnerResponse = JsonConvert.SerializeObject(checkResponse);
                            logVC.MsgError = checkResponse.message_vi;
                            logVC.StatusCode = checkResponse.success == true ? 200 : 0;
                            var result = new ResultResponse
                            {
                                Message = checkResponse.message_vi,
                                Status = HttpStatusCode.BadRequest
                            };

                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.BadRequest, result);

                        }

                        var resultData = JsonConvert.DeserializeObject<GotItVoucherUsedListResponse>(readData.Result);

                        logVC.Status = resultData.success;
                        logVC.Value = posModel.listSeriNo.Count > 1 ? 0 : posModel.listSeriNo.FirstOrDefault().value;
                        logVC.PartnerResponse = JsonConvert.SerializeObject(resultData);
                        logVC.MsgError = resultData.message_vi;
                        logVC.StatusCode = resultData.success == true ? 200 : 0;

                        if (resultData.success == true)
                        {
                            var result = new ResultResponse
                            {
                                Message = $"Sử dụng thành công {posModel.listSeriNo.Count} voucher",
                                Status = HttpStatusCode.OK
                            };

                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            listVoucherTrans.AddRange(posModel.listSeriNo.Select(a => new VoucherCardTransModel
                            {
                                Partner = posModel.partner,
                                SeriNo = a.seriNo,
                                StoreNo = posModel.storeNo,
                                PosID = posModel.posID,
                                Status = true,
                                Value = a.value,
                                FunctionName = "RedeemVoucher",
                                UserName = posModel.staffCode
                            }));

                            Task.Run(() => pLGBLO.VoucherTrans(listVoucherTrans));

                            return Request.CreateResponse(HttpStatusCode.OK, result);
                        }
                        else
                        {
                            var result = new ResultResponse
                            {
                                Message = resultData.message_vi,
                                Status = HttpStatusCode.BadRequest
                            };

                            logVC.PosResponse = JsonConvert.SerializeObject(result);
                            Task.Run(() => pLGBLO.LogVoucher(logVC));

                            return Request.CreateResponse(HttpStatusCode.BadRequest, result);
                        }
                    }
                    else
                    {
                        logVC.PosResponse = JsonConvert.SerializeObject(response);
                        logVC.PartnerResponse = JsonConvert.SerializeObject(response);
                        logVC.MsgError = "Error";
                        logVC.StatusCode = 0;

                        Task.Run(() => pLGBLO.LogVoucher(logVC));

                        return Request.CreateResponse(response.StatusCode, new ResultResponse
                        {
                            Data = null,
                            Message = response.ReasonPhrase,
                            Status = response.StatusCode
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logVC.PosResponse = JsonConvert.SerializeObject(ex);
                logVC.PartnerResponse = JsonConvert.SerializeObject(ex);
                logVC.MsgError = "Error";
                logVC.StatusCode = 0;

                Task.Run(() => pLGBLO.LogVoucher(logVC));

                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API {JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }
        #endregion

        #region Tích điểm
        [HttpPost]
        [Route("Sale")]
        public HttpResponseMessage Sale(PLSaleCXRequestModel model)
        {
            try
            {
                #region Check Authorization

                var checkAuthen = AuthenAPI.AuthorizationBasic(ActionContext);
                if (checkAuthen.StatusCode != HttpStatusCode.OK)
                {
                    return Request.CreateResponse(checkAuthen.StatusCode, new ResultResponse
                    {
                        Data = null,
                        Message = $"Authorization has been denied for this request",
                        Status = checkAuthen.StatusCode
                    });
                }
                #endregion

                if (string.IsNullOrEmpty(model.storeNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Mã CH/ST đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (string.IsNullOrEmpty(model.posID))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Mã POS đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (string.IsNullOrEmpty(model.orderNo))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Đơn hàng đang bị trống",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                if (model.orderAmount <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Tổng giá trị đơn hàng phải lớn hơn 0",
                        Status = HttpStatusCode.BadRequest
                    });
                }

                return CXSales(model.cardNumber, model.storeNo, model.posID, model.spendPoints, model.awardAmount, model.orderNo, model.orderAmount);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi dữ liệu server {ex.Message}",
                    Status = HttpStatusCode.InternalServerError
                });
            }
        }

        [HttpGet]
        [Route("GetInfoMember")]
        public HttpResponseMessage GetInfoMember(string numberCard, string storeNo, string posID)
        {
            #region Check Authorization

            var checkAuthen = AuthenAPI.AuthorizationBasic(ActionContext);
            if (checkAuthen.StatusCode != HttpStatusCode.OK)
            {
                return Request.CreateResponse(checkAuthen.StatusCode, new ResultResponse
                {
                    Data = null,
                    Message = $"Authorization has been denied for this request",
                    Status = checkAuthen.StatusCode
                });
            }
            #endregion

            //var endPoint = $"{UrlCrownX.CXUrlProfile}?codeValue={codeValue}";
            //var endPoint = $"{UrlCrownX.CXUrlInfo}?phoneNo={numberCard}&clubCode=CVL";
            var endPoint = $"{UrlCrownX.CXUrlInfoV2}?codeValue={numberCard}&clubCode=CVL";
            if (string.IsNullOrEmpty(numberCard))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                {
                    Data = null,
                    Message = $"numberCard đang bị trống",
                    Status = HttpStatusCode.BadRequest
                });
            }
            var qrCode = "";
            if (numberCard.Substring(0, 1) == "C")//QRCode
            {
                qrCode = numberCard;
            }

            try
            {
                var regexItem = new Regex("^[a-zA-Z0-9 ]*$");

                if (!regexItem.IsMatch(numberCard))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new ResultResponse
                    {
                        Data = null,
                        Message = "Mã thẻ/QRCode/số điện thoại không hợp lệ, vui lòng scan lại thông tin",
                        Status = HttpStatusCode.BadRequest
                    });
                }
                modelLogCX = new CX_LogAPIModel
                {
                    CardNumber = numberCard,
                    StoreNo = storeNo,
                    POSTerminal = posID,
                    ActionType = "GetInfoMember",
                    RequestPOS = $"{endPoint}",
                    RequestXML = $"{CXUrl}{endPoint}",
                    Source = "PL",
                    DateTime = DateTime.Now
                };

                using (var client = new HttpClient())
                {
                    try
                    {
                        var st1 = new Stopwatch();
                        st1.Start();
                        long milliseconds = 0;

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(CXUrl);

                        var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var response = client.GetAsync(endPoint).Result;
                        var readData = response.Content.ReadAsStringAsync();
                        var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);

                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();

                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);
                        modelLogCX.ResponseTotal = milliseconds;
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var dataResult = JsonConvert.SerializeObject(resultData.data);
                            var modelCustomer = JsonConvert.DeserializeObject<CustomerEnquiryModel>(dataResult);

                            var firstMemberBalance = modelCustomer.memberBalance.FirstOrDefault(d => d.subPoolId == "CVL");
                            var memberPoint = 0;
                            var totalPoint = 0;
                            var currentRate = 0;
                            if (firstMemberBalance != null)
                            {
                                memberPoint = Convert.ToInt32(firstMemberBalance.availableBalance ?? 0);
                                totalPoint = Convert.ToInt32(firstMemberBalance.totalBalance ?? 0);
                                currentRate = (int)firstMemberBalance.currencyRate;
                            }
                            PLInfoMemberModel result = new PLInfoMemberModel
                            {
                                CardNumber = !string.IsNullOrEmpty(modelCustomer.cardNo) ? modelCustomer.cardNo : modelCustomer.phoneNo,
                                CMND = "",
                                MemberName = modelCustomer.fullName,
                                CardLevel = modelCustomer.customerTier,
                                MemberCSN = modelCustomer.csn,
                                PhoneNumber = modelCustomer.phoneNo,
                                QRCode = qrCode,
                                MemberPoint = memberPoint,
                                TotalPoint = totalPoint,
                                CurrentRate = currentRate,
                                IsRedeem = (bool)modelCustomer.isRedeem,
                                Status = modelCustomer.cardStatus == "ACTIVE" ? "Hoạt động" : modelCustomer.cardStatus == "BLOCK" ? "Khóa thẻ" : modelCustomer.cardStatus == "INACTIVE" ? "Không hoạt động" : string.IsNullOrEmpty(modelCustomer.cardStatus) ? "" : modelCustomer.cardStatus,
                                //Status = modelCustomer.cardStatus == "ACTIVE" ? "Hoạt động" : modelCustomer.cardStatus == "BLOCK" ? "Khóa thẻ" : string.IsNullOrEmpty(modelCustomer.cardStatus) ? "Chưa hoạt động" : modelCustomer.cardStatus == "INACTIVE" ? "Không hoạt động" : modelCustomer.cardStatus,
                            };
                            return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                            {
                                Data = result,
                                Message = "Thành công",
                                Status = HttpStatusCode.OK
                            });
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.NotFound, new ResultResponse
                            {
                                Data = null,
                                Message = resultData.message,
                                Status = HttpStatusCode.NotFound
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                        Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = null,
                            Message = $"Lỗi hệ thống API CrownX {JsonConvert.SerializeObject(ex)}",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new ResultResponse
                {
                    Data = null,
                    Message = $"Lỗi hệ thống API Server{JsonConvert.SerializeObject(ex)}",
                    Status = HttpStatusCode.ServiceUnavailable
                });
            }
        }

        HttpResponseMessage CXSales(string card, string storeNo, string posTerminal, int spendPoints, double billAmount, string orderNo, double orderAmount)
        {

            //cardNumber: cardNo or phoneNo

            var endPoint = $"{UrlCrownX.CXUrlSale}";
            long timeUni = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var phoneNumber = "";
            var cardNo = "";

            if (card.Substring(0, 4) == "6868")
            {
                cardNo = card;
            }
            else//PhoneNo
            {
                phoneNumber = card;
            }

            var modelPos = new CXSalesPOSRequest
            {
                CardNumber = card,
                StoreNo = storeNo,
                PosTerminal = posTerminal,
                SpendPoints = spendPoints,
                BillAmount = billAmount,
                OrderNo = orderNo,
                OrderAmount = orderAmount
            };
            string requestPOS = JsonConvert.SerializeObject(modelPos);

            var model = new CXSalesModelRequest
            {
                phoneNo = phoneNumber,
                cardNo = cardNo,
                invoiceNo = orderNo,
                posCode = posTerminal,
                storeCode = storeNo,
                spendPoints = spendPoints,
                totalBillAmount = billAmount,
                transactionTime = timeUni,
                merchantId = "VCM",//RED-- CHAT DOI THANH VCM
                clubCode = "CVL"
            };
            string jsonCX = JsonConvert.SerializeObject(model);

            modelLogCX = new CX_LogAPIModel
            {
                CardNumber = card,
                StoreNo = storeNo,
                POSTerminal = posTerminal,
                InvoiceNo = orderNo,
                ActionType = "CXSales",
                RequestPOS = $"{requestPOS}",
                RequestXML = $"{jsonCX}",
                Source = "WIN",
                DateTime = DateTime.Now
            };

            using (var client = new HttpClient())
            {
                try
                {
                    var st1 = new Stopwatch();
                    st1.Start();
                    long milliseconds = 0;

                    client.Timeout = TimeSpan.FromMinutes(2);
                    client.BaseAddress = new Uri(CXUrl);

                    var byteArray = Encoding.ASCII.GetBytes($"{CXUser}:{CXPassword}");
                    var authenBasic = Convert.ToBase64String(byteArray);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                    var response = client.PostAsync(endPoint, new StringContent(jsonCX, Encoding.UTF8, "application/json")).Result;
                    var readData = response.Content.ReadAsStringAsync();
                    var resultData = JsonConvert.DeserializeObject<ResponseCrownX>(readData.Result);

                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();

                    modelLogCX.ResponseXML = JsonConvert.SerializeObject(resultData);
                    modelLogCX.ResponseTotal = milliseconds;
                    Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var dataResult = JsonConvert.SerializeObject(resultData.data);
                        var modelSale = JsonConvert.DeserializeObject<CXSalesModelResponse>(dataResult);

                        var tichdiem = modelSale.currencyRate == 0 ? 0 : Math.Abs((int)Math.Round(((double)(modelSale.extraEarnByCampaign) / modelSale.currencyRate), 0));
                        var tieudiem = modelSale.spendPoints;

                        var modelTrans = new CX_TransactionLoyaltyModel
                        {
                            CardNumber = card,
                            PhoneNumber = modelSale.phoneNo,
                            InvoiceNo = modelSale.invoiceNo,
                            ReferenceNumberCX = modelSale.referenceNumber ?? 0,
                            StoreID = storeNo,//son truyen
                            TerminalId = posTerminal,//son truyen
                            ChannelCode = "VCM",
                            DateTime = DateTime.Now,
                            TransactionType = "SALE",
                            TransactionName = "Bán",
                            AmountOnOrderPOS = (double)orderAmount,//Son truyen
                            BillAmount = (double)billAmount,
                            SpendPoints = (double)spendPoints,
                            RefundAmount = 0,
                            Operator = "+",
                            OrderNo = modelSale.invoiceNo,
                            OrigOrderNo = modelSale.invoiceNo,
                            OrigInvoiceNo = modelSale.invoiceNo,
                            OrigBillAmount = (double)billAmount,
                            Source = "WIN",
                            Rate = modelSale.currencyRate,
                            // NettAmtOE = IsNumber(nett_amt),
                            //GrossTransactionAmountOE = IsNumber(gross_transaction_amount),
                            AwardPointCX = tichdiem,// modelSale.extraEarnByCampaign,
                            AwardAmountCX = modelSale.extraEarnByCampaign,
                            // RedeemPointOE = redeem_units,
                            // RedeemAmountOE = redeem_units * 10,
                            IsOffline = false

                        };

                        Task.Run(() => logVID.CXTransaction(modelTrans));

                        PLPointModel result = new PLPointModel
                        {
                            PointEarn = tichdiem,//Tích điểm
                            PointRedeem = spendPoints,//(int)modelSale.spendPoints,//Tiêu điểm
                            CurrentRate = (int)modelSale.currencyRate,

                        };

                        return Request.CreateResponse(HttpStatusCode.OK, new ResultResponse
                        {
                            Data = result,
                            Message = "Thành công",
                            Status = HttpStatusCode.OK
                        });

                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError)//OFFLINE
                    {
                        PLPointModel result = new PLPointModel
                        {
                            PointEarn = 1,//Tích điểm
                            PointRedeem = 0,//Tiêu điểm
                            CurrentRate = 10
                        };
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                        {
                            Data = result,
                            Message = $"Lỗi hệ thống API CX",
                            Status = HttpStatusCode.InternalServerError
                        });
                    }
                    else
                    {
                        var result = new ResultResponse
                        {
                            Data = null,
                            Message = resultData.message,
                            Status = response.StatusCode
                        };
                        return Request.CreateResponse(response.StatusCode, result);
                    }
                }

                catch (Exception ex)
                {
                    modelLogCX.ResponseXML = JsonConvert.SerializeObject(ex);
                    Task.Run(() => logVID.WriteLogAPI_CX(modelLogCX));

                    return Request.CreateResponse(HttpStatusCode.InternalServerError, new ResultResponse
                    {
                        Data = null,
                        Message = $"Lỗi hệ thống API CrownX {ex.Message}",
                        Status = HttpStatusCode.InternalServerError
                    });
                }
            }
        }
        #endregion
       
    }
}
