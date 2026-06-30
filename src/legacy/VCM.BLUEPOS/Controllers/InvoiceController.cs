using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Invoice;
using VCM.BLUEPOS.Store;
using VCM.BLUEPOS.Model.Invoice;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Common;
using DocSoThanhChu;
using TichHopSAP;
using VCM.BLUEPOS.Business.Common;
using System.Configuration;
using TichHopSAP.PortalService;
using System.Text.RegularExpressions;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using VCM.BLUEPOS.Business.SalesStaging;
using VCM.BLUEPOS.Model.Common;
using System.Threading.Tasks;
using PLG.Controllers;
using System.Net.Http;
using System.Web.Configuration;
using System.Net.Http.Headers;


namespace PLG.Controllers
{
    public class InvoiceController : BaseController
    {
        private IStoreBLO _storeBLO;
        private IInvoiceBLO _invoiceBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        private ISalesStagingBLO _stagingBLO;
        private readonly VCM.BLUEPOS.WSInvoicePortal.PortalService wsInvoice;
        private string eInvoiceUs = @"" + ConfigurationManager.AppSettings["EInvoiceUs"];
        private string eInvoicePw = @"" + ConfigurationManager.AppSettings["EInvoicePw"];
        private string issueVAT = @"" + ConfigurationManager.AppSettings["IssueVAT"];

        // --- 5/10/2023: tungnt8 ---
        private string linkAPI { get; set; }
        string apipartnertaxinfo  = WebConfigurationManager.AppSettings["api_partner_tax_info"];
        string pl_user_api_partner = WebConfigurationManager.AppSettings["api_partner_user_plg"];
        string pl_pass_api_partner = WebConfigurationManager.AppSettings["api_partner_pass_plg"];

        public InvoiceController(IStoreBLO storeBLO, IInvoiceBLO invoiceBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO, ISalesStagingBLO stagingBLO)
        {
            _storeBLO = storeBLO;
            _invoiceBLO = invoiceBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
            _stagingBLO = stagingBLO;
            wsInvoice = new VCM.BLUEPOS.WSInvoicePortal.PortalService();
            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKPARTNERAPI").Value;
        }

        [DisplayName("Quản lý hóa đơn điện tử")]
        public ActionResult MngInvoice()
        {
            var result = new ViewMngInvoiceModel();
            try
            {
                var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
                var listSetDB = listStoreByUser
                    .GroupBy(d => new { d.ServerIP, d.ServerIPRead, d.ServerStoreDesc })
                    .Select(a => new SQLServerModel
                    {
                        IPServer = a.FirstOrDefault().ServerIP,
                        IPServerRead = a.FirstOrDefault().ServerIPRead,
                        Description = a.FirstOrDefault().ServerStoreDesc
                    }).Distinct().ToList();

                var listInvoiceType = _invoiceBLO.ListInvoiceType();
                var siteNo = base.LoginUser.SiteCode; // lOCAL MỚI CÓ

                //var listSetDB = new List<SQLServerModel>();
                //var setStoreUser = _commonBLO.GetInforStoreSet(siteNo);

                //if (setStoreUser != null && setStoreUser.Count > 0)
                //{
                //    var first = setStoreUser.FirstOrDefault();
                //    listSetDB.Add(new SQLServerModel { IPServer = first.IPServer, IPServerRead = first.IPServerRead, Description = first.Description });
                //}
                //else
                //{
                //    listSetDB = _commonBLO.ListSQLServer();
                //}

                var ipSvrFirst = listSetDB.FirstOrDefault().IPServer;
                var listStore = listStoreByUser.Where(d => d.ServerIP == ipSvrFirst).ToList();  //_commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, ipSvrFirst).ToList();
                result = _invoiceBLO.ViewMngInvoi();
                result.ListStore = listStore;

                var storeNo = (listStore == null || listStore.Count == 0) ? "" : listStore.FirstOrDefault().SiteNo;
                var listMauSo = _invoiceBLO.ListInvoicePattern(storeNo, "");
                var listMauSoBySite = listMauSo.GroupBy(d => d.InvoicePattern).Select(a => new InvoicePatternByStore
                {
                    InvoicePattern = a.FirstOrDefault().InvoicePattern
                }).ToList();

                var listKyHieuBySite = listMauSo.Select(a => new InvoiceSerialByStore
                {
                    InvoiceSerial = a.SerialNo
                }).ToList();
                result.ListInvoicePattern = listMauSoBySite;
                result.ListInvoiceSerial = listKyHieuBySite;
                result.SetDB = listSetDB;
            }
            catch (Exception ex)
            {
            }
            return View(result);
        }

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult _ViewDieuChinhThayThe(string storeNo, int hinhthuc, string loaiDieuChinh, string key, string fKey, string branchNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };

            try
            {
                //var checkStatus = wsInvoice.checkFkey(branchNo, fKey, eInvoiceUs, eInvoicePw);
                //if (checkStatus == "1")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"Hóa đơn đã được lưu chưa được cấp số và ký số"
                //    };
                //    return Json(result);                   
                //}
                //if (checkStatus == "2")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"Hóa đơn chờ gửi sang CQT. Vui lòng đợi"
                //    };
                //    return Json(result);
                //}
                //if (checkStatus == "3")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"CQT chưa phản hồi kết quả"
                //    };
                //    return Json(result);
                //}
                //if (checkStatus == "4")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"Hóa đơn gửi không thành công sang CQT"
                //    };
                //    return Json(result);
                //}
                //if (checkStatus == "6")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"CQT phản hồi kiểm tra không hợp lệ"
                //    };
                //    return Json(result);
                //}
                //if (checkStatus == "7")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"Hóa đơn đã bị hủy"
                //    };
                //    return Json(result);
                //}
                //if (checkStatus == "ERR:6")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"Hóa đơn gốc chưa phát hành"
                //    };
                //    return Json(result);
                //}
                //if (checkStatus != "5")
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"{checkStatus}"
                //    };
                //    return Json(result);
                //}

                var checkFkey = _invoiceBLO.GetInfoByFKey(storeNo, key);

                if (!string.IsNullOrEmpty(checkFkey.InvoiceNo))
                {
                    if (loaiDieuChinh == "replaceNormal")  //Thay thế (bỏ)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Hóa đơn này đã được điều chỉnh thông tin bởi số hóa đơn (" + checkFkey.InvoiceNo + ") số seri (" + checkFkey.SerialNo + ") mẫu số (" + checkFkey.InvoicePattern + ")=>Không được thay thế"
                        };
                        return Json(result);
                    }

                    if (loaiDieuChinh == "adjustInfoNormal")  //Điều chỉnh
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Hóa đơn này đã được điều chỉnh thông tin bởi số hóa đơn (" + checkFkey.InvoiceNo + ") số seri (" + checkFkey.SerialNo + ") mẫu số (" + checkFkey.InvoicePattern + ")=>Không được điều chỉnh"
                        };
                        return Json(result);
                    }
                }

                var checkKey = _invoiceBLO.GetInfoByKey(storeNo, key);

                //if (!string.IsNullOrEmpty(checkKey.InvoiceNo))
                //{
                //    var dateNow = DateTime.Now.Date;
                //    var dateNgayPH = checkKey.ArisingDate;
                //    if (loaiDieuChinh == "replaceNormal")//Thay thế (bỏ)
                //    {
                //        if (dateNow.Year == dateNgayPH.Value.Year && dateNow.Month != dateNgayPH.Value.Month)
                //        {
                //            result = new ResultResponse
                //            {
                //                Status = HttpStatusCode.BadRequest,
                //                Message = $"Không được phép thay thế hóa đơn khác tháng => Chỉ được phép điều chỉnh thông tin"
                //            };
                //            return Json(result);
                //        }
                //    }
                //    if (loaiDieuChinh == "adjustInfoNormal")//Điều chỉnh
                //    {
                //        //Đóng để test ==> deloy bỏ comment

                //        if (dateNow.Year == dateNgayPH.Value.Year && dateNow.Month == dateNgayPH.Value.Month)
                //        {
                //            result = new ResultResponse
                //            {
                //                Status = HttpStatusCode.BadRequest,
                //                Message = $"Không được phép điều chỉnh thông tin hóa đơn cùng tháng => Chỉ được phép thay thế hóa đơn"
                //            };
                //            return Json(result);
                //        }
                //    }
                //}

                var listInvoice = _invoiceBLO.ListOrderReplace(storeNo, hinhthuc, key, fKey, loaiDieuChinh);

                var model = new ViewDieuChinhThayTheModel
                {
                    ListInvoiceReplace = listInvoice,
                    InfoInvoiceByKey = checkKey,
                    LoaiDieuChinh = loaiDieuChinh
                };

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/Invoice/_ViewDieuChinhThayThe.cshtml", model),
                    Data = model
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult ReplaceInvoice(string StoreNo, int isEOD, string Key, string FKey, string NgayPhatHanh, string TenKH, string TenCT, string DiaChi, string MST, string Phone, string Email, string LoaiDieuChinh)
        {
            try
            {
                MST = MST.Trim();
                TenCT = TenCT.Trim();
                DiaChi = DiaChi.Trim();
                TenKH = TenKH.Trim();
                Email = Email.Trim();
                Phone = Phone.Trim();

                if (!string.IsNullOrEmpty(MST))
                {
                    var checkTaxCode = VCM.BLUEPOS.Helpers.Utils.CheckTaxCode(MST);
                    if (checkTaxCode.Item1 == false)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"{checkTaxCode.Item2}" });
                    }
                    if (string.IsNullOrEmpty(TenCT))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập tên công ty" });
                    }
                }

                DateTime NgayPH = DateTime.ParseExact(NgayPhatHanh, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var dateNow = DateTime.Now.Date;
                if (NgayPH.Year == dateNow.Year && NgayPH.Month != dateNow.Month && LoaiDieuChinh == "replaceNormal")
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Không được thay thế hóa đơn của tháng trước" });
                }
                if (!string.IsNullOrEmpty(TenCT))
                {
                    if (string.IsNullOrEmpty(DiaChi))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập địa chỉ" });
                    }
                    if (TenCT.Length > 400)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Tên công ty không vượt quá 400 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(TenKH))
                {
                    if (TenKH.Length > 100)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Tên khách hàng không vượt quá 100 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(Email))
                {
                    if (Email.Length > 50)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Địa chỉ Email không vượt quá 50 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(Phone))
                {
                    if (Phone.Length > 20)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số điện thoại không vượt quá 20 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(DiaChi))
                {
                    if (DiaChi.Length > 400)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Địa chỉ không vượt quá 400 ký tự" });
                    }
                }

                MST = string.IsNullOrEmpty(MST) ? "" : MST.Trim();

                InvoicePOSPL invoice = new InvoicePOSPL();
                var date = DateTime.Now.ToString("yyyy-MM-dd");
                var infoCus = MST + "|" + TenKH + "|" + TenCT + "|" + DiaChi + "|" + Phone + "|" + Email;
                var result = "";
                var resultReplace = new List<string>();

                if (LoaiDieuChinh == "replaceNormal")//Thay thế
                {
                    var listInvoice = _invoiceBLO.ListOrderReplace(StoreNo, isEOD, Key, FKey, "replaceNormal").Where(d => d.IsRun == 1).ToList();
                    if (listInvoice.Count > 0)
                    {
                        foreach (var item in listInvoice)
                        {
                            if (invoice.checkFkey(StoreNo, item.Key).ToUpper() != "CANCEL")
                            {
                                if (isEOD == 1)
                                {
                                    result = invoice.ReplaceEOD(date, StoreNo, infoCus, item.Key, true);
                                    resultReplace.Add(result);
                                }
                                else
                                {
                                    result = invoice.ReplaceNormal(date, StoreNo, infoCus, item.Key, true);
                                    resultReplace.Add(result);
                                }
                            }
                        }
                    }

                    //result = invoice.ReplaceNormal(date, siteCode, infoCus, Key);
                    //resultReplace.Add(result);

                }
                else if (LoaiDieuChinh == "adjustInfoNormal") // Điều chỉnh
                {
                    var listInvoice = _invoiceBLO.ListOrderReplace(StoreNo, isEOD, Key, FKey, "adjustInfoNormal").Where(d => d.IsRun == 1).ToList();
                    if (listInvoice.Count > 0)
                    {
                        foreach (var item in listInvoice)
                        {
                            if (invoice.checkFkey(StoreNo, item.Key).ToUpper() != "CANCEL")
                            {
                                if (isEOD == 1)
                                {
                                    result = invoice.AdjustInfoEOD(date, StoreNo, infoCus, item.Key, true);
                                    resultReplace.Add(result);
                                }
                                else
                                {
                                    result = invoice.AdjustInfoNormal(date, StoreNo, infoCus, item.Key, true);
                                    resultReplace.Add(result);
                                }
                            }
                        }
                    }
                }
                else
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Kiểm tra lại loại điều chỉnh" });
                }

                if (LoaiDieuChinh == "adjustInfoNormal") // Điều chỉnh hóa đơn
                {
                    if (resultReplace.Count > 0)
                    {
                        int success = 0;
                        int error = 0;
                        var listError = new List<string>();
                        foreach (var rp in resultReplace)
                        {
                            var resulData = JsonConvert.DeserializeObject<List<InvoiceResultModel>>(rp);
                            if (resulData.Count() > 0)
                            {
                                foreach (var item in resulData)
                                {
                                    if (item.ERR == "OK")
                                    {
                                        success += 1;
                                    }
                                    else
                                    {
                                        error += 1;
                                        listError.Add(item.ERRMSG);
                                    }
                                }
                            }
                        }

                        if (success == 1 && error == 0)
                        {
                            return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = $"Điều chỉnh hóa đơn thành công" });
                        }
                        else if (success == 0 && error == 1)
                        {
                            return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Điều chỉnh hóa đơn thất bại, {string.Join(";", listError)}" });
                        }
                        else
                        {
                            var message = error == 0 ? $"Điều chỉnh thành công {success} hóa đơn" : $"Điều chỉnh thành công {success} và thất bại {error} hóa đơn";
                            return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = message });
                        }
                    }
                }
                else if (LoaiDieuChinh == "replaceNormal") // Thay thế hóa đơn
                {
                    var listDataSetup = _commonBLO.ListPOSSetup();
                    var linkSearchInvoice = (listDataSetup != null && listDataSetup.Count > 0 && listDataSetup.Any(d => d.Code == "LINK-SEARCH-INVOICE")) ? listDataSetup.FirstOrDefault(d => d.Code == "LINK-SEARCH-INVOICE").Value : "Not link";

                    if (resultReplace.Count > 0)
                    {
                        int success = 0;
                        int error = 0;
                        var listError = new List<string>();
                        var listPrint = new List<InvoicePrintModel>();
                        foreach (var rp in resultReplace)
                        {
                            var resulData = JsonConvert.DeserializeObject<List<InvoiceResultModel>>(rp);
                            if (resulData.Count() > 0)
                            {
                                foreach (var item in resulData)
                                {
                                    if (item.ERR == "OK")
                                    {
                                        success += 1;
                                        var infoPrint = _invoiceBLO.GetInfoByKey(StoreNo, item.Key);
                                        var infoPOS = _invoiceBLO.GetInfoPosTerminal(StoreNo, item.Key);
                                        InvoicePrintModel print = new InvoicePrintModel
                                        {
                                            SiteName = item.BranchName,
                                            InvoicePattern = item.InvoicePattern,
                                            InvoiceNo = item.InvoiceNo,
                                            SerialNo = item.SerialNo,
                                            SaltEncrypt = string.IsNullOrEmpty(item.Salt) ? "" : HDDTVGRBase64EncodeDecode.Base64EncodeDecode.Base64Encode(item.Salt),
                                            FullName = infoPrint.CusName,
                                            Company = infoPrint.CusCom,
                                            Address = infoPrint.CusAddress,
                                            MST = infoPrint.CusTaxCode,
                                            OrderDate = string.Format("{0:dd/MM/yyyy}", infoPrint.ArisingDate),
                                            StaffCode = infoPOS.Count == 0 ? "" : infoPOS.FirstOrDefault().CashierID,
                                            TeminalID = infoPOS.Count == 0 ? "" : infoPOS.FirstOrDefault().POSTerminalNo,
                                            OrderNo = infoPOS.Count == 0 ? "" : string.Join(", ", infoPOS.Select(d => d.OrderNo))
                                        };
                                        listPrint.Add(print);
                                    }
                                    else
                                    {
                                        error += 1;
                                        listError.Add(item.ERRMSG);
                                    }
                                }
                            }
                        }
                        var model = new PrintVATModel
                        {
                            ListPrint = listPrint,
                            LinkSearchInvoice = linkSearchInvoice
                        };

                        if (success == 1 && error == 0)
                        {
                            return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = $"Thay thế hóa đơn thành công", Data = model });
                        }
                        else if (success == 0 && error == 1)
                        {
                            return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Thay thế hóa đơn thất bại. {string.Join(";", listError)}", Data = model });
                        }
                        else
                        {
                            var message = error == 0 ? $"Thay thế thành công {success} hóa đơn" : $"Thay thế thành công {success} hóa đơn và thất bại {error}";
                            return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = message, Data = model });
                        }
                    }
                }
                return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Không tìm thấy hóa đơn" });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = ex.Message });
            }
        }

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult PrintAgainInvoice(string Key, string StoreNo, string ISEOD, string Type)
        {
            try
            {
                var isEOD = ISEOD == "true" ? 1 : 0;
                //var resulData = _invoiceBLO.GetInfoInvoice(StoreNo, isEOD, Key);

                var resulData = _invoiceBLO.GetInfoInvoiceV2(StoreNo, isEOD, Key, Type);  // 16/12/2024, tungnt8

                if (resulData != null)
                {
                    //var infoPrint = _invoiceBLO.GetInfoByKey(StoreNo, Key);

                    var infoPrint = _invoiceBLO.GetInfoByKeyV2(StoreNo, Key, Type);
                   
                    //var infoPOS = _invoiceBLO.GetInfoPosTerminal(StoreNo, Key);

                    var infoPOS = _invoiceBLO.GetInfoPosTerminalV2(StoreNo,Key,Type);   // 16/12/2024, tungnt8

                    var listDataSetup = _commonBLO.ListPOSSetup();
                    var LinkSearchInvoice = (listDataSetup != null && listDataSetup.Count > 0 && listDataSetup.Any(d => d.Code == "LINK-SEARCH-INVOICE")) ? listDataSetup.FirstOrDefault(d => d.Code == "LINK-SEARCH-INVOICE").Value : "Not link";

                    InvoicePrintModel print = new InvoicePrintModel
                    {
                        SiteName = resulData.BranchName,
                        InvoicePattern = resulData.InvoicePattern,
                        InvoiceNo = resulData.InvoiceNo,
                        SerialNo = resulData.SerialNo,
                        SaltEncrypt = HDDTVGRBase64EncodeDecode.Base64EncodeDecode.Base64Encode(resulData.Salt),
                        FullName = infoPrint.CusName,
                        Company = infoPrint.CusCom,
                        Address = infoPrint.CusAddress,
                        MST = infoPrint.CusTaxCode,
                        OrderDate = string.Format("{0:dd/MM/yyyy}", infoPrint.ArisingDate),
                        StaffCode = infoPOS.Count == 0 ? "" : infoPOS.FirstOrDefault().CashierID,
                        TeminalID = infoPOS.Count == 0 ? "" : infoPOS.FirstOrDefault().POSTerminalNo,
                        OrderNo = infoPOS.Count == 0 ? "" : string.Join(", ", infoPOS.Select(d => d.OrderNo)),
                        LinkSearchInvoice = (listDataSetup != null && listDataSetup.Count > 0 && listDataSetup.Any(d => d.Code == "LINK-SEARCH-INVOICE")) ? listDataSetup.FirstOrDefault(d => d.Code == "LINK-SEARCH-INVOICE").Value : "Not link"
                    };
                    return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = "In lại hóa đơn thành công", Data = print });
                }
                else
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Không tìm thấy thông tin hóa đơn VAT" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = ex.Message
                });
            }
        }

        //[HttpPost]
        //[ParentAuthorize("MngInvoice")]
        //public ActionResult ConvertForVerifyFkey(string StoreNo, string Key, string BranchNo)
        //{
        //    try
        //    {
        //        var check = _invoiceBLO.GetInfoByKey(StoreNo, Key);

        //        if (!(bool)check.IsSigned)
        //        {
        //            return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Hóa đơn này đang đợi ký số, nên không chuyển đổi được" });
        //        }

        //        if (check.IsCancel == true)
        //        {
        //            return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Hóa đơn này đã được hủy hoặc thay thế nên không được chuyển đổi" });
        //        }

        //        var result = wsInvoice.convertForVerifyFkey(BranchNo, Key, eInvoiceUs, eInvoicePw);

        //        if (VCM.BLUEPOS.Helpers.Utils.LeftStr(result, 3) != "ERR")
        //        {
        //            return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = result });
        //        }
        //        else
        //        {
        //            return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = result });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = ex.Message });
        //    }
        //}

        // --- 10/12/2022, tungnt8 : chuyển đổi hóa đơn

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult ConvertForVerifyFkeyV2(string StoreNo, string Key, string BranchNo,string Type)
        {
            try
            {
                //var check = _invoiceBLO.GetInfoByKey(StoreNo, Key);                
                var check = _invoiceBLO.GetInfoByKeyV2(StoreNo, Key, Type);

                if (!(bool)check.IsSigned)
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Hóa đơn này đang đợi ký số, nên không chuyển đổi được" });
                }

                if (check.IsCancel == true)
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Hóa đơn này đã được hủy hoặc thay thế nên không được chuyển đổi" });
                }

                var result = wsInvoice.convertForVerifyFkey(BranchNo, Key, eInvoiceUs, eInvoicePw);

                if (VCM.BLUEPOS.Helpers.Utils.LeftStr(result, 3) != "ERR")
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = result });
                }
                else
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = result });
                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = ex.Message });
            }
        }

        [HttpGet]
        [ParentAuthorize("MngInvoice")]
        public ActionResult PrintPDF(string BranchNo, string KeyPdf)
        {
            try
            {
                //EInvoice invoice = new EInvoice();
                //var pdfBase64 = invoice.downloadInvPDFFkeyByStaff(BranchNo, KeyPdf, UserName, Password);//BranchNo,Key

                var pdfBase64 = wsInvoice.downloadInvPDFFkeyByStaff(BranchNo, KeyPdf, eInvoiceUs, eInvoicePw);
                if (!string.IsNullOrEmpty(pdfBase64))
                {
                    byte[] sPDFDecoded = Convert.FromBase64String(pdfBase64);
                    return File(sPDFDecoded, "application/pdf");
                }
            }
            catch (Exception ex)
            {
            }
            return RedirectToAction("MngInvoice", "Invoice");
        }

        [HttpGet]
        [ParentAuthorize("MngInvoice")]
        public ActionResult PrintHTML(string BranchNo, string Key)
        {
            try
            {
                var html = wsInvoice.getInvViewFkeyByStaff(BranchNo, Key, eInvoiceUs, eInvoicePw);
                ViewBag.PrintHTML = html;
                if (!string.IsNullOrEmpty(html))
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("MngInvoice", "Invoice");
            }
            return RedirectToAction("MngInvoice", "Invoice");
        }

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult _ViewSendMail(string storeNo, string key)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };

            try
            {
                var checkKey = _invoiceBLO.GetInfoByKey(storeNo, key);
                if (string.IsNullOrEmpty(checkKey.InvoiceNo))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tìm thấy thông tin hóa đơn"
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"Success",
                        ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/Invoice/_ViewSendMail.cshtml", checkKey),
                        Data = checkKey
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.ServiceUnavailable,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult EditEmail(string StoreNo, string InvoiceType, string Key, string Email)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };

            try
            {
                var response = _invoiceBLO.EditEmail(StoreNo, InvoiceType, Key, Email);
                if (response.Item1 == true)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = "Chỉnh sửa email thành công"
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = response.Item2
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"{ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult SendMail(string Key, string Email, string StoreNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };
            try
            {
                var response = _invoiceBLO.SendEmail(Key, Email, StoreNo);
                if (response.Item1 == true)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = response.Item2
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = response.Item2
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"{ex.Message}"
                };
            }
            return Json(result);
        }

        [ParentAuthorize("MngInvoice")]
        public ActionResult _ViewDetailByDocumentNo(string storeNo, string documentNo, string branchInvoice, string typePublish)
        {
            var model = new ViewDetailByDocumentNoModel
            {
                storeNo = storeNo,
                documentNo = documentNo,
                branchInvoice = branchInvoice,
                typePublish = typePublish
            };
            return PartialView(model);
        }

        // 25/11/2024, tungnt8 : Xem chi tiết hóa đơn
        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult GetDetailInvoiceList()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];

                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;
                var recordsTotal = 0;

                var orderNo = Request?.Form["OrderNo"];
                var storeNo = Request?.Form["StoreNo"];
                var documentNo = Request?.Form["DocumentNo"];
                var branchInvoice = Request?.Form["BranchInvoice"];
                var typePublish = Request?.Form["TypePublish"];  // Phuong thuc phat hanh: GTGT = Hoa don gia trị gia tang ; MTT = Hoa don may tinh tien MTT

                var data = _invoiceBLO.GetDetailInvoiceList(storeNo, documentNo, typePublish, branchInvoice, orderNo, out recordsTotal, pageNumber, pageSize);
                return Json(new DataTablesViewModel<InvoiceLineModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<CustomerVATModel>());
            }
        }

        [ParentAuthorize("LoadViewDetailByDocumentNo")]
        public ActionResult LoadViewDetailByDocumentNoExcel(string storeNo, string documentNo, string branchInvoice, string orderNo)
        {
            var recordsTotal = 0;
            var data = _invoiceBLO.ListInvoiceLine(storeNo, documentNo, branchInvoice, orderNo, out recordsTotal, 1, 1000000);
            if (data != null)
            {
                var dataExport = new List<InvoiceLineExcelModel>();
                dataExport = data.Select(a => new InvoiceLineExcelModel
                {
                    OrderNo = a.OrderNo,
                    ProdName = a.ProdName,
                    ProdUnit = a.ProdUnit,
                    ProdQuantity = a.ProdQuantity,
                    ProdPrice = a.ProdPrice,
                    Total = a.Total,
                    VATRate = a.VATRate,
                    VATAmount = a.VATAmount,
                    Amount = a.Amount,
                    VatVinIDAmount = a.VatVinIDAmount,
                    NetVinIDAmount = a.NetVinIDAmount,
                    TotalVinIDAmount = a.TotalVinIDAmount
                }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataExport.Select(x => new
                    {
                        x.OrderNo,
                        x.ProdName,
                        x.ProdUnit,
                        x.ProdPrice,
                        x.Total,
                        x.VATRate,
                        x.VATAmount,
                        x.Amount,
                        x.VatVinIDAmount,
                        x.NetVinIDAmount,
                        x.TotalVinIDAmount
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số đơn hàng";
                    worksheet.Cells[1, 2].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 3].Value = "ĐVT";
                    worksheet.Cells[1, 4].Value = "Số lượng";
                    worksheet.Cells[1, 5].Value = "Đơn giá";
                    worksheet.Cells[1, 6].Value = "Thành tiền trước thuế (VAT)";
                    worksheet.Cells[1, 7].Value = "Thuế suất GTGT(%)";
                    worksheet.Cells[1, 8].Value = "Tiền thuế GTGT";
                    worksheet.Cells[1, 9].Value = "Thành tiền sau thuế (VAT)";
                    worksheet.Cells[1, 10].Value = "Tiền thuế CVL";
                    worksheet.Cells[1, 11].Value = "Tiền trước thuế CVL";
                    worksheet.Cells[1, 12].Value = "Tiền sau thuế CVL";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(dataExport, false).AutoFitColumns();
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ChiTietHoaDon_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        // 26/11/2024, tungnt8
        //[ParentAuthorize("LoadViewDetailByDocumentNo")]
        //public ActionResult ExportExcelDetailByDocumentNo(string storeNo, string documentNo, string branchInvoice, string orderNo, string typePublish)
        //{
        //    var recordsTotal = 0;
        //    var data = _invoiceBLO.GetDetailInvoiceList(storeNo, documentNo, typePublish, branchInvoice, orderNo, out recordsTotal, 1, 1000000);
        //    if (data != null)
        //    {
        //        var dataExport = new List<InvoiceLineExcelModel>();
        //        dataExport = data.Select(a => new InvoiceLineExcelModel
        //        {
        //            OrderNo = a.OrderNo,
        //            ProdName = a.ProdName,
        //            ProdUnit = a.ProdUnit,
        //            ProdQuantity = a.ProdQuantity,
        //            ProdPrice = a.ProdPrice,
        //            Total = a.Total,
        //            VATRate = a.VATRate,
        //            VATAmount = a.VATAmount,
        //            Amount = a.Amount,
        //            VatVinIDAmount = a.VatVinIDAmount,
        //            NetVinIDAmount = a.NetVinIDAmount,
        //            TotalVinIDAmount = a.TotalVinIDAmount
        //        }).ToList();

        //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //        using (var package = new ExcelPackage())
        //        {
        //            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
        //            var listExport = dataExport.Select(x => new
        //            {
        //                x.OrderNo,
        //                x.ProdName,
        //                x.ProdUnit,
        //                x.ProdPrice,
        //                x.Total,
        //                x.VATRate,
        //                x.VATAmount,
        //                x.Amount,
        //                x.VatVinIDAmount,
        //                x.NetVinIDAmount,
        //                x.TotalVinIDAmount
        //            }).ToList();

        //            worksheet.Cells[1, 1].Value = "Số đơn hàng";
        //            worksheet.Cells[1, 2].Value = "Tên sản phẩm";
        //            worksheet.Cells[1, 3].Value = "ĐVT";
        //            worksheet.Cells[1, 4].Value = "Số lượng";
        //            worksheet.Cells[1, 5].Value = "Đơn giá";
        //            worksheet.Cells[1, 6].Value = "Thành tiền trước thuế (VAT)";
        //            worksheet.Cells[1, 7].Value = "Thuế suất GTGT(%)";
        //            worksheet.Cells[1, 8].Value = "Tiền thuế GTGT";
        //            worksheet.Cells[1, 9].Value = "Thành tiền sau thuế (VAT)";
        //            worksheet.Cells[1, 10].Value = "Tiền thuế CVL";
        //            worksheet.Cells[1, 11].Value = "Tiền trước thuế CVL";
        //            worksheet.Cells[1, 12].Value = "Tiền sau thuế CVL";

        //            using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
        //            {
        //                r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
        //                r.Style.Font.Bold = true;
        //                r.Style.Fill.PatternType = ExcelFillStyle.Solid;
        //                r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
        //                r.AutoFilter = true;
        //            }

        //            worksheet.Cells["A2"].LoadFromCollection(dataExport, false);
        //            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //            string fileName = $"ChiTietHoaDon_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
        //            using (var memoryStream = new MemoryStream())
        //            {
        //                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //                Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
        //                package.SaveAs(memoryStream);
        //                memoryStream.WriteTo(Response.OutputStream);
        //                Response.Flush();
        //                Response.End();
        //            }
        //        }
        //    }
        //    return Json(string.Empty, JsonRequestBehavior.AllowGet);
        //}

        [HttpPost]
        [ParentAuthorize("MngInvoice")]
        public ActionResult MngInvoiceLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var skip = start != null ? Convert.ToInt32(start) : 1;
                var pageNumber = skip / pageSize;
                var recordsTotal = 0;

                var setDB = Request?.Form["SetDB"];
                var listStore = Request?.Form["StoreNo"];
                var TuNgay = Request?.Form["ArisingDate"];
                var DenNgay = Request?.Form["ArisingToDate"];
                var MauSo = Request?.Form["InvoicePattern"];
                var SoHD = Request?.Form["InvoiceNo"];
                var TenKH = Request?.Form["CusName"];
                var DienThoai = Request?.Form["CusPhone"];
                var TenCTy = Request?.Form["CusCom"];
                var KyHieu = Request?.Form["SerialNo"];
                var LoaiHoaDon = Request?.Form["InvoiceType"];
                var MST = Request?.Form["CusTaxCode"];
                var HinhThuc = Request?.Form["HinhThuc"];
                var isSigned = Request?.Form["IsSigned"];
                var fromDate = new DateTime();
                var toDate = new DateTime();

                var typePublish = Request?.Form["TypePublish"];

                if (!string.IsNullOrEmpty(Request?.Form["ArisingDate"]))
                {
                    var date = Request?.Form["ArisingDate"];
                    fromDate = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                }
                if (!string.IsNullOrEmpty(Request?.Form["ArisingToDate"]))
                {
                    var date = Request?.Form["ArisingToDate"];
                    toDate = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }

                // var listStoreArray = listStore.Split(new char[] { ',' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                var modelStore = string.IsNullOrEmpty(listStore) ? new ParamJsonStoreList() : JsonConvert.DeserializeObject<ParamJsonStoreList>(listStore);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(listStore) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName); 
                    var listStoreServerIP = listStoreByUser.Where(d => d.ServerIP == setDB).ToList();
                    var listParam = listStoreServerIP.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.SiteNo,
                        SiteName = a.SiteName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }
                else if (listStore == "Not")
                {
                    jsonStore = "";
                }
                else
                {
                    jsonStore = listStore;
                }

                InvoiceHearderRequest model = new InvoiceHearderRequest
                {
                    SetDB = setDB,
                    SiteNo = jsonStore,
                    TuNgay = fromDate,
                    DenNgay = toDate,
                    MauSo = MauSo,
                    SoHD = SoHD,
                    TenKH = TenKH,
                    DienThoai = DienThoai,
                    TenCTy = TenCTy,
                    KyHieu = KyHieu,
                    LoaiHoaDon = LoaiHoaDon,
                    MST = MST,
                    HinhThuc = HinhThuc,
                    IsSigned = isSigned,
                    StatusCQT = "",
                    Type = typePublish,
                    Export = "0",
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                
                var result = _invoiceBLO.GetInvoiceList(model);
                recordsTotal = (result == null || result.Count() == 0) ? 0 : (int)result.FirstOrDefault().TotalRow;
                return Json(new DataTablesViewModel<InvoiceHearderModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
                
                //var result = _invoiceBLO.LoadInvoice(model);
                //recordsTotal = (result == null || result.Count() == 0) ? 0 : (int)result.FirstOrDefault().TotalRow;
                //return Json(new DataTablesViewModel<InvoiceHearderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<InvoiceHearderModel> { draw = "", recordsFiltered = 0, recordsTotal = 0, data = new List<InvoiceHearderModel>() });
            }
        }

        //public ActionResult ExportExcelInvoice(string SetDB, string SiteNo, string tuNgay, string denNgay, string MauSo, string KyHieu, string SoHD, string TenKH, string TenCTy, string DienThoai, string MST, string LoaiHoaDon, string HinhThuc, string TinhTrang)
        //{
        //    DateTime fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //    DateTime toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

        //    //-----------------------

        //    var jsonStore = "";

        //    if (string.IsNullOrEmpty(SiteNo) || SiteNo == "-")
        //    {
        //        var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
        //        var listParam = listStoreNo.Select(a => new ParamJsonStoreList
        //        {
        //            SiteNo = a.StoreNo,
        //            SiteName = a.StoreName
        //        }).ToList();

        //        jsonStore = JsonConvert.SerializeObject(listParam);
        //    }
        //    else if (SiteNo == "Not")
        //    {
        //        jsonStore = "";
        //    }
        //    else
        //    {
        //        jsonStore = SiteNo;
        //    }

        //    //----------------------

        //    InvoiceHearderRequest model = new InvoiceHearderRequest
        //    {
        //        SetDB = SetDB,
        //        SiteNo = jsonStore,
        //        TuNgay = fromDate,
        //        DenNgay = toDate,
        //        MauSo = MauSo,
        //        SoHD = SoHD,
        //        TenKH = TenKH,
        //        DienThoai = DienThoai,
        //        TenCTy = TenCTy,
        //        KyHieu = KyHieu,
        //        LoaiHoaDon = LoaiHoaDon,
        //        MST = MST,
        //        HinhThuc = HinhThuc,
        //        IsSigned = TinhTrang,
        //        StatusCQT = "",
        //        PageNumber = 1,
        //        PageSize = 5000000
        //    };

        //    //var data = _invoiceBLO.LoadInvoice(model);
        //    var data = _invoiceBLO.GetInvoiceList(model);

        //    if (data != null)
        //    {
        //        var dataExport = new List<InvoiceExportModel>();
        //        dataExport = data.Select(a => new InvoiceExportModel
        //        {
        //            StoreNo = a.StoreNo,
        //            InvoicePattern = a.InvoicePattern,
        //            SerialNo = a.SerialNo,
        //            InvoiceNo = a.InvoiceNo,
        //            InvoiceTypeName = a.InvoiceTypeName,
        //            CreateDate = a.CreateDate.Value.ToString("dd/MM/yyyy HH:mm:ss"),
        //            BusinessDate = a.BusinessDate.Value.ToString("dd/MM/yyyy"),
        //            CusName = a.CusName,
        //            CusCom = a.CusCom,
        //            EmailDeliver = a.EmailDeliver,
        //            CusPhone = a.CusPhone,
        //            CusTaxCode = a.CusTaxCode,
        //            HinhThuc = a.HinhThuc,
        //            Remark = a.Remark
        //        }).ToList();

        //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        //        using (var package = new ExcelPackage())
        //        {
        //            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
        //            var listExport = dataExport.Select(x => new
        //            {
        //                x.StoreNo,
        //                x.InvoicePattern,
        //                x.SerialNo,
        //                x.InvoiceNo,
        //                x.InvoiceTypeName,
        //                x.CreateDate,
        //                x.BusinessDate,
        //                x.CusName,
        //                x.CusCom,
        //                x.EmailDeliver,
        //                x.CusPhone,
        //                x.CusTaxCode,
        //                x.HinhThuc,
        //                x.Remark
        //            }).ToList();

        //            worksheet.Cells[1, 1].Value = "CH/ST";
        //            worksheet.Cells[1, 2].Value = "Mẫu số";
        //            worksheet.Cells[1, 3].Value = "Ký hiệu";
        //            worksheet.Cells[1, 4].Value = "Số hóa đơn";
        //            worksheet.Cells[1, 5].Value = "Loại hóa đơn";
        //            worksheet.Cells[1, 6].Value = "Ngày phát hành";
        //            worksheet.Cells[1, 7].Value = "Ngày kinh doanh";
        //            worksheet.Cells[1, 8].Value = "Tên khách hàng";
        //            worksheet.Cells[1, 9].Value = "Tên công ty";
        //            worksheet.Cells[1, 10].Value = "Email";
        //            worksheet.Cells[1, 11].Value = "Điện thoại";
        //            worksheet.Cells[1, 12].Value = "Mã số thuế";
        //            worksheet.Cells[1, 13].Value = "Hình thức";
        //            worksheet.Cells[1, 14].Value = "Thay thế / điều chỉnh";

        //            using (ExcelRange r = worksheet.Cells[1, 1, 1, 14])
        //            {
        //                r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
        //                r.Style.Font.Bold = true;
        //                r.Style.Fill.PatternType = ExcelFillStyle.Solid;
        //                r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
        //                r.AutoFilter = true;
        //            }

        //            worksheet.Cells["A2"].LoadFromCollection(dataExport, false).AutoFitColumns();
        //            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //            string fileName = $"ExportInvoice_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
        //            using (var memoryStream = new MemoryStream())
        //            {
        //                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //                Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
        //                package.SaveAs(memoryStream);
        //                memoryStream.WriteTo(Response.OutputStream);
        //                Response.Flush();
        //                Response.End();
        //            }
        //        }
        //    }

        //    return Json(string.Empty, JsonRequestBehavior.AllowGet);
        //}

        // 21/11/2024, tungnt8
        public ActionResult ExportExcelInvoiceV2(string SetDB, string SiteNo, string tuNgay, string denNgay, string MauSo, string KyHieu, string SoHD, string TenKH, string TenCTy, string DienThoai, string MST, string LoaiHoaDon, string TypePublish, string HinhThuc, string TinhTrang)
        {
            DateTime fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var modelStore = string.IsNullOrEmpty(SiteNo) ? new ParamJsonStoreList() : JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
                var listStoreServerIP = listStoreByUser.Where(d => d.ServerIP == SetDB).ToList();
                var listParam = listStoreServerIP.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.SiteNo,
                    SiteName = a.SiteName
                }).ToList();
                jsonStore = JsonConvert.SerializeObject(listParam);
            }
            else if (SiteNo == "Not")
            {
                jsonStore = "";
            }
            else
            {
                jsonStore = SiteNo;
            }
            
            InvoiceHearderRequest model = new InvoiceHearderRequest
            {
                SetDB = SetDB,
                SiteNo = jsonStore,
                TuNgay = fromDate,
                DenNgay = toDate,
                MauSo = MauSo,
                SoHD = SoHD,
                TenKH = TenKH,
                DienThoai = DienThoai,
                TenCTy = TenCTy,
                KyHieu = KyHieu,
                LoaiHoaDon = LoaiHoaDon,
                MST = MST,
                Type = TypePublish,
                HinhThuc = HinhThuc,
                IsSigned = TinhTrang,
                StatusCQT = "",
                Export = "1",
                PageNumber = 1,
                PageSize = 5000000
            };

            //var data = _invoiceBLO.LoadInvoice(model);           
            var data = _invoiceBLO.GetInvoiceList(model);  // 06/12/2024

            if (data != null)
            {
                var dataExport = new List<InvoiceListExportModel>();
                dataExport = data.Select(a => new InvoiceListExportModel
                {
                    TinhTrangKySo = a.TinhTrangKySo,
                    StoreNo = a.StoreNo,
                    OrderNo = a.OrderNo,
                    InvoicePattern = a.InvoicePattern,
                    SerialNo = a.SerialNo,
                    InvoiceNo = a.InvoiceNo,
                    Amount = a.Amount,
                    InvoiceTypeName = a.InvoiceTypeName,
                    CreateDate = a.CreateDate.Value.ToString("dd/MM/yyyy HH:mm:ss"),
                    BusinessDate = a.BusinessDate.Value.ToString("dd/MM/yyyy"),
                    CusName = a.CusName,
                    CusCom = a.CusCom,
                    CusAddress = a.CusAddress,
                    EmailDeliver = a.EmailDeliver,
                    CusPhone = a.CusPhone,
                    CusTaxCode = a.CusTaxCode,
                    HinhThuc = a.HinhThuc,
                    Remark = a.Remark,
                    BillTaxCode = a.BillTaxCode,
                    Key = a.KeyPdf
                }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataExport.Select(x => new
                    {
                        x.TinhTrangKySo,
                        x.StoreNo,
                        x.OrderNo,
                        x.InvoicePattern,
                        x.SerialNo,
                        x.InvoiceNo,
                        x.Amount,
                        x.InvoiceTypeName,
                        x.CreateDate,
                        x.BusinessDate,
                        x.CusName,
                        x.CusCom,
                        x.CusAddress,
                        x.EmailDeliver,
                        x.CusPhone,
                        x.CusTaxCode,
                        x.HinhThuc,
                        x.Remark,
                        x.BillTaxCode,
                        x.Key
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Tình trạng";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Số đơn hàng";
                    worksheet.Cells[1, 4].Value = "Mẫu số";
                    worksheet.Cells[1, 5].Value = "Ký hiệu";
                    worksheet.Cells[1, 6].Value = "Số hóa đơn";
                    worksheet.Cells[1, 7].Value = "Tổng số tiền";
                    worksheet.Cells[1, 8].Value = "Loại hóa đơn";
                    worksheet.Cells[1, 9].Value = "Ngày phát hành";
                    worksheet.Cells[1, 10].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 11].Value = "Tên khách hàng";
                    worksheet.Cells[1, 12].Value = "Tên công ty";
                    worksheet.Cells[1, 13].Value = "Địa chỉ";
                    worksheet.Cells[1, 14].Value = "Email";
                    worksheet.Cells[1, 15].Value = "Điện thoại";
                    worksheet.Cells[1, 16].Value = "Mã số thuế";
                    worksheet.Cells[1, 17].Value = "Hình thức";
                    worksheet.Cells[1, 18].Value = "Thay thế / điều chỉnh";
                    worksheet.Cells[1, 19].Value = "Mã CQT";
                    worksheet.Cells[1, 20].Value = "Key";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 20])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(dataExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ListDetailInvoiceReport_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [DisplayName("Báo cáo hóa đơn điện tử")]
        public ActionResult ReportInvoice()
        {
            ViewBag.ListStore = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        public ActionResult ReportInvoiceLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;
                var recordsTotal = 0;

                var listStore = Request?.Form["StoreNo"];
                var TuNgay = Request?.Form["ArisingDate"];
                var DenNgay = Request?.Form["ArisingToDate"];
                var MauSo = Request?.Form["InvoicePattern"];
                var SoHD = Request?.Form["InvoiceNo"];
                var TenKH = Request?.Form["CusName"];
                var DienThoai = Request?.Form["CusPhone"];
                var TenCTy = Request?.Form["CusCom"];
                var KyHieu = Request?.Form["SerialNo"];
                var LoaiHoaDon = Request?.Form["InvoiceType"];
                var MST = Request?.Form["CusTaxCode"];
                var HinhThuc = Request?.Form["HinhThuc"];
                var isSigned = Request?.Form["IsSigned"];

                DateTime fromDate = new DateTime();
                DateTime toDate = new DateTime();
                if (!string.IsNullOrEmpty(Request?.Form["ArisingDate"]))
                {
                    var date = Request?.Form["ArisingDate"];
                    fromDate = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                if (!string.IsNullOrEmpty(Request?.Form["ArisingToDate"]))
                {
                    var date = Request?.Form["ArisingToDate"];
                    toDate = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                }
                var jsonStore = "";

                if (string.IsNullOrEmpty(listStore))
                {
                    var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.StoreNo,
                        SiteName = a.StoreName
                    }).ToList();

                    jsonStore = JsonConvert.SerializeObject(listParam);
                }
                else
                {
                    jsonStore = listStore;
                }

                InvoiceHearderRequest model = new InvoiceHearderRequest
                {
                    SiteNo = jsonStore,
                    TuNgay = fromDate,
                    DenNgay = toDate,
                    MauSo = MauSo,
                    SoHD = SoHD,
                    TenKH = TenKH,
                    DienThoai = DienThoai,
                    TenCTy = TenCTy,
                    KyHieu = KyHieu,
                    LoaiHoaDon = LoaiHoaDon,
                    MST = MST,
                    HinhThuc = HinhThuc,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var result = _invoiceBLO.ReportInvoiceLoad(model);
                if (result != null && result.Count > 0)
                {
                    recordsTotal = result.FirstOrDefault().TotalRow;
                }
                return Json(new DataTablesViewModel<ExportInvoiceReportModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<ExportInvoiceReportModel>());
            }
        }

        public ActionResult ExportInvoiceReport(string SiteNo, string tuNgay, string denNgay, string MauSo, string KyHieu, string SoHD, string TenKH, string TenCTy, string DienThoai, string MST, string LoaiHoaDon, string HinhThuc)
        {
            DateTime fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var jsonStore = "";
            if (string.IsNullOrEmpty(SiteNo))
            {
                var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.StoreNo,
                    SiteName = a.StoreName
                }).ToList();

                jsonStore = JsonConvert.SerializeObject(listParam);
            }
            else
            {
                jsonStore = SiteNo;
            }

            InvoiceHearderRequest model = new InvoiceHearderRequest
            {
                SiteNo = jsonStore,
                TuNgay = fromDate,
                DenNgay = toDate,
                MauSo = MauSo,
                SoHD = SoHD,
                TenKH = TenKH,
                DienThoai = DienThoai,
                TenCTy = TenCTy,
                KyHieu = KyHieu,
                LoaiHoaDon = LoaiHoaDon,
                MST = MST,
                HinhThuc = HinhThuc,
                PageNumber = 1,
                PageSize = 5000000
            };

            var result = _invoiceBLO.ReportInvoiceLoad(model);
            var data = new List<ExcelInvoiceReportModel>();
            data = result.Select(a => new ExcelInvoiceReportModel
            {
                SerialNo = a.SerialNo,
                InvoicePattern = a.InvoicePattern,
                VATNumber = a.VATNumber,
                IsEOD = a.IsEOD,
                CusTaxCode = a.CusTaxCode,
                CusName = a.CusName,
                CusCom = a.CusCom,
                EmailDeliver = a.EmailDeliver,
                CusAddress = a.CusAddress,
                SaltEncrypt = a.SaltEncrypt,
                BusinessDate = a.BusinessDate,
                PrintedDate = a.PrintedDate,
                StoreNo = a.StoreNo,
                DocumentNo = a.DocumentNo,
                TotalAmount = a.TotalAmount,
                MemberPointsEarn = a.MemberPointsEarn,
                MemberPointsRedeem = a.MemberPointsRedeem,
                Barcode = a.Barcode,
                ItemName = a.ItemName,
                UnitPrice = a.UnitPrice,
                Quantity = a.Quantity,
                UnitOfMeasure = a.UnitOfMeasure,
                Amount = a.Amount,
                DiscountAmount = a.DiscountAmount,
                MemberPointsRedeemVINID = a.MemberPointsRedeemVINID,
                VATPercent = a.VATPercent,
                VATAmount = a.VATAmount
            }).ToList();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells[1, 1].Value = "Ký hiệu";
                    worksheet.Cells[1, 2].Value = "Mẫu số";
                    worksheet.Cells[1, 3].Value = "Số hóa đơn";
                    worksheet.Cells[1, 4].Value = "IsEOD";
                    worksheet.Cells[1, 5].Value = "MST khách hàng";
                    worksheet.Cells[1, 6].Value = "Người mua hàng";
                    worksheet.Cells[1, 7].Value = "Tên đơn vị";
                    worksheet.Cells[1, 8].Value = "Email";
                    worksheet.Cells[1, 9].Value = "Địa chỉ";
                    worksheet.Cells[1, 10].Value = "Mã tra cứu";
                    worksheet.Cells[1, 11].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 12].Value = "Ngày xuất HĐ";
                    worksheet.Cells[1, 13].Value = "Mã Site";
                    worksheet.Cells[1, 14].Value = "Số bill";
                    worksheet.Cells[1, 15].Value = "Tổng tiền thanh toán";
                    worksheet.Cells[1, 16].Value = "Tổng điểm tích";
                    worksheet.Cells[1, 17].Value = "Tổng điểm tiêu";
                    worksheet.Cells[1, 18].Value = "Barcode";
                    worksheet.Cells[1, 19].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 20].Value = "Đơn giá";
                    worksheet.Cells[1, 21].Value = "Số lượng";
                    worksheet.Cells[1, 22].Value = "ĐVT";
                    worksheet.Cells[1, 23].Value = "Thành tiền";
                    worksheet.Cells[1, 24].Value = "Giảm giá";
                    worksheet.Cells[1, 25].Value = "Điểm tiêu VINID";
                    worksheet.Cells[1, 26].Value = "Thuế suất";
                    worksheet.Cells[1, 27].Value = "Tiền thuế";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 27])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }


                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"ExportReportInvoice_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [DisplayName("NoseriVAT")]
        public ActionResult NoseriVAT()
        {
            ViewBag.ListStore = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            ViewBag.ListBranch = _invoiceBLO.ListBranch();
            return View();
        }

        [HttpPost]
        public ActionResult NoseriVATLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var branchNo = Request?.Form["BranchNo"] == "-" ? "" : Request?.Form["BranchNo"];
                var storeNo = Request?.Form["StoreNo"] == "-" ? "" : Request?.Form["StoreNo"];
                var noseriCode = Request?.Form["NoseriCode"];
                var invoiceType = Request?.Form["InvoiceType"] == "-" ? "" : Request?.Form["InvoiceType"]; ;
                var status = Request?.Form["Enabled"] == "-" ? "" : Request?.Form["Enabled"];

                var recordsTotal = 0;
                var result = _invoiceBLO.LoadNoseriVAT(branchNo, storeNo, noseriCode, invoiceType, status, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<StoreNoseriVATModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<StoreNoseriVATModel>());
            }
        }

        [HttpPost]
        public ActionResult UpdateNoseri(string BranchNo, string StoreNo, string StoreName, string CompTaxCode, string NoseriCode, string InvoiceType, string Status)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "Cập nhật thành công"
            };

            try
            {
                var createUserName = base.LoginUser.UserName;
                var stt = Status == "1" ? true : false;
                StoreNoseriVATModel model = new StoreNoseriVATModel
                {
                    StoreNo = StoreNo,
                    StoreName = StoreName,
                    CompTaxCode = CompTaxCode,
                    BranchNo = BranchNo,
                    NoseriCode = NoseriCode,
                    InvoiceType = InvoiceType,
                    Enabled = stt,
                    CreateDate = DateTime.Now,
                    LastUpdate = DateTime.Now
                };

                if (!_invoiceBLO.UpdateNoseri(model))
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Cập nhật noseri thất bại"
                    };
                    return Json(result);
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"{ex.Message}"
                };
                return Json(result);
            }
        }

        [DisplayName("Dải hóa đơn")]
        public ActionResult VATNumber()
        {
            //ViewBag.ListBranch = _invoiceBLO.ListBranch();
            ViewBag.ListNoseri = _invoiceBLO.ListNoseriByType("");
            return View();
        }

        [HttpPost]
        public ActionResult LoadNoseriByType(string invoiceType)
        {
            var result = new List<StoreNoseriVATModel>();
            try
            {
                invoiceType = invoiceType == "-" ? "" : invoiceType;
                result = _invoiceBLO.ListNoseriByType(invoiceType);
            }
            catch
            {
            }
            return Json(result);
        }

        [HttpPost]
        public ActionResult VATNumberLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var invoiceType = Request?.Form["InvoiceType"] == "-" ? "" : Request?.Form["InvoiceType"]; ;
                var noseriCode = Request?.Form["NoseriCode"] == "-" ? "" : Request?.Form["NoseriCode"];
                var invoicePattern = Request?.Form["InvoicePattern"];
                var serialNo = Request?.Form["SerialNo"];
                var taxCode = Request?.Form["CompTaxCode"];
                var status = Request?.Form["Enabled"] == "-" ? "" : Request?.Form["Enabled"];

                var recordsTotal = 0;
                var result = _invoiceBLO.LoadVATNumber(invoiceType, noseriCode, invoicePattern, serialNo, taxCode, status, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<VatNumberModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<VatNumberModel>());
            }
        }

        [DisplayName("Danh sách chi nhánh")]
        public ActionResult BranchInvoice()
        {

            ViewBag.ListStore = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            ViewBag.ListBranch = _invoiceBLO.ListBranch();
            return View();
        }

        [HttpPost]
        public ActionResult BranchInvoiceLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;
                var recordsTotal = 0;

                var branchNo = Request?.Form["BranchNo"] == "-" ? "" : Request?.Form["BranchNo"]; ;
                var branchName = "";
                var comptaxCode = Request?.Form["CompTaxCode"];
                var type = Request?.Form["TypePublish"];

                //var result = _invoiceBLO.LoadBranch(branchNo, branchName, comptaxCode, out recordsTotal, skip, pageSize);
                var result = _invoiceBLO.LoadBranch(branchNo, branchName, comptaxCode, type, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<BranchModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<VatNumberModel>());
            }
        }

        [HttpPost]
        public ActionResult UpdateBranch(BranchModel model)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "Cập nhật thành công"
            };

            try
            {
                var createUserName = base.LoginUser.UserName;
                model.CreateDate = DateTime.Now;
                model.LastUpdate = DateTime.Now;

                var data = _invoiceBLO.UpdateBranch(model);
                if (data.Item1 == false)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}"
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{data.Item2}"
                    };
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"{ex.Message}"
                };
                return Json(result);
            }
        }

        [HttpPost]
        public ActionResult UpdateVATNumber(string CompTaxCode, string NoseriCode, string InvoicePattern, string SerialNo, string Status)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "Cập nhật thành công"
            };

            try
            {
                var createUserName = base.LoginUser.UserName;
                var stt = Status == "1" ? true : false;
                VatNumberModel model = new VatNumberModel
                {
                    CompTaxCode = CompTaxCode,
                    NoseriCode = NoseriCode,
                    SerialNo = SerialNo,
                    InvoicePattern = InvoicePattern,
                    Enabled = stt,
                    StartNumber = "0000001",
                    EndNumber = "9999999",
                    CreateDate = DateTime.Now,
                    LastUpdate = DateTime.Now
                };
                var data = _invoiceBLO.UpdateVatNumber(model);
                if (data.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = $"{data.Item2}"
                    };
                    return Json(result);
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"{data.Item2}"
                    };
                    return Json(result);
                }

            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"{ex.Message}"
                };
                return Json(result);
            }
        }

        [DisplayName("Xuất hóa đơn điện tử")]
        public ActionResult IssueInvoice()
        {
            var listStore = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            var infoInvoice = new InfoInvoiceModel();
            if (listStore != null && listStore.Count > 0)
            {
                infoInvoice = _invoiceBLO.InfoInvoice(listStore.FirstOrDefault().StoreNo);
            }
            var model = new ViewIssueInvoiceModel
            {
                ListStore = listStore,
                InfoInvoice = infoInvoice
            };
            return View(model);
        }

        [HttpPost]
        [ParentAuthorize("IssueInvoice")]
        public ActionResult InfoInvoice(string siteCode)
        {
            try
            {
                var info = _invoiceBLO.InfoInvoice(siteCode);
                if (info != null)
                {
                    return Json(info);
                }
            }
            catch
            {
            }
            return Json(new InfoInvoiceModel());
        }

        [ParentAuthorize("IssueInvoice")]
        public ActionResult _ViewListCustomer()
        {
            return PartialView();
        }

        [HttpPost]
        [ParentAuthorize("IssueInvoice")]
        public ActionResult LoadCustomer()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;
                var recordsTotal = 0;

                var tenKH = Request?.Form["CustomerName"];
                var tenCTy = Request?.Form["CompanyName"];
                var mst = Request?.Form["TaxCode"];
                var soDT = Request?.Form["PhoneNumber"];

                if (!string.IsNullOrEmpty(mst))
                {
                    mst = mst.TrimEnd();
                }

                if (!string.IsNullOrEmpty(soDT))
                {
                    mst = mst.TrimEnd();
                }

                var data = _invoiceBLO.LoadCustomer(tenKH, soDT, mst, tenCTy, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<CustomerVATModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<CustomerVATModel>());
            }
        }

        [ParentAuthorize("IssueInvoice")]
        public ActionResult _ViewListOrder()
        {
            return PartialView();
        }

        [HttpPost]
        [ParentAuthorize("IssueInvoice")]
        public ActionResult LoadOrder()
        {
            try
            {
                // var siteCode = base.LoginUser.SiteCode;
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

                var recordsTotal = 0;
                var orderNo = Request?.Form["OrderNo"];
                var listOrderSelected = Request?.Form["ListOrderSelect"];
                var siteCode = Request?.Form["StoreNo"];

                var result = _invoiceBLO.ListTranHeader(siteCode, orderNo, listOrderSelected, pageSize, pageNumber);
                if (result.Count == 0 && !string.IsNullOrEmpty(orderNo))
                {
                    if (!string.IsNullOrEmpty(orderNo) && orderNo.Length > 10)
                    {
                        //SyncData sync = new SyncData();
                        // var posID = orderNo.Substring(0, 6);
                        // var syncData = UploadSaleFromPOSByOrder(siteCode, orderNo, posID);
                        //result = _TransBLO.ListTranHeader(siteCode, orderNo, listOrderSelected, pageSize, pageNumber);
                    }
                }
                recordsTotal = result.Count() == 0 ? 0 : (int)result.FirstOrDefault().TotalRow;
                return Json(new DataTablesViewModel<TransHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<TransHeaderModel>());
            }
        }

        [HttpPost]
        [ParentAuthorize("IssueInvoice")]
        public ActionResult ViewListOrder(string StoreNo, string ListOrder)
        {
            try
            {
                //var result = _invoiceBLO.DetailListOrder(StoreNo, ListOrder);

                var viewNew = _invoiceBLO.ViewDetailListOrder(StoreNo, ListOrder);

                //sontp 19/06/2023
                //if (viewNew.ListDetailInvoiceLine.Count == 0)
                //{
                //    var itemOrder = ListOrder.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                //    if (itemOrder.Length > 0)
                //    {

                //        foreach (var item in itemOrder)
                //        {
                //            if (item.Length > 10)
                //            {
                //                var posID = item.Substring(0, 6);
                //                // UploadSaleFromPOSByOrder(StoreNo, item, posID); // đóng để test lộn set
                //            }
                //        }
                //    }
                //    viewNew = _invoiceBLO.ViewDetailListOrder(StoreNo, ListOrder);
                //}

                var header = viewNew.InvoiceHeader;// _invoiceBLO.ViewInvoiceHeader(StoreNo, ListOrder);

                if (header != null && header.VinIDRedeem > 0)
                {
                    // result.Add(new ListOrderDetailModel { Num = 0, ItemName = "Thanh toán bằng điểm CVL", UOM = "", ProdPrice = 0, Quantity = 0, VATAmount = 0, VATRate = 0, NetAmount = 0, Amount = header.VinIDRedeem });
                    viewNew.ListDetailInvoiceLine.Add(new ListOrderDetailModel { Num = 0, ItemName = "Thanh toán bằng điểm CVL", UOM = "", ProdPrice = 0, Quantity = 0, VATAmount = 0, VATRate = 0, NetAmount = 0, Amount = header.VinIDRedeem });
                }
                var totalAmount = header.TotalNon + header.Total0 + header.Total5 + header.Total8 + header.Total10;

                ReadMoney read = new ReadMoney();
                string readTotalAmount = totalAmount > 0 ? read.TienBangChu(totalAmount.ToString()) : "";

                // return Json(new { Detail = result, Header = header, ReadMoney = readTotalAmount });
                return Json(new { Detail = viewNew.ListDetailInvoiceLine, Header = header, ReadMoney = readTotalAmount });

            }
            catch
            {
            }
            return Json(new List<ListOrderDetailModel>());
        }

        [HttpPost]
        [ParentAuthorize("IssueInvoice")]
        public ActionResult IssueVATInvoice(string StoreNo, string ListOrder, string TenKH, string TenCT, string DiaChi, string MST, string Phone, string Email, string NotVAT)
        {
            try
            {
                var userName = base.LoginUser.UserName;
                MST = MST.Trim();
                TenCT = TenCT.Trim();
                DiaChi = DiaChi.Trim();
                TenKH = string.IsNullOrEmpty(TenKH) ? string.Empty : TenKH.Trim();
                Email = Email.Trim();
                Phone = Phone.Trim();

                if (!string.IsNullOrEmpty(MST))
                {
                    var checkTaxCode = VCM.BLUEPOS.Helpers.Utils.CheckTaxCode(MST);
                    if (checkTaxCode.Item1 == false)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"{checkTaxCode.Item2}" });
                    }
                    if (string.IsNullOrEmpty(TenCT))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập tên công ty" });
                    }
                }

                if (!string.IsNullOrEmpty(TenCT))
                {
                    if (string.IsNullOrEmpty(DiaChi))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập địa chỉ" });
                    }
                    if (TenCT.Length > 400)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Tên công ty không vượt quá 400 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(TenCT))
                {
                    if (string.IsNullOrEmpty(DiaChi))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập địa chỉ" });
                    }
                }
                if (!string.IsNullOrEmpty(TenKH))
                {
                    if (TenKH.Length > 80)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Tên khách hàng không vượt quá 80 ký tự" });
                    }
                }
                if (!string.IsNullOrEmpty(Email))
                {
                    if (Email.Length > 50)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Địa chỉ Email không vượt quá 50 ký tự" });
                    }
                }
                if (!string.IsNullOrEmpty(Phone))
                {
                    if (Phone.Length > 20)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số điện thoại không vượt quá 20 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(DiaChi))
                {
                    if (DiaChi.Length > 400)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Địa chỉ không vượt quá 400 ký tự" });
                    }
                }

                MST = string.IsNullOrEmpty(MST) ? "" : MST.Trim();
                InvoicePOSPL invoice = new InvoicePOSPL();
                var date = DateTime.Now.ToString("yyyy-MM-dd");
                var infoCus = MST + "|" + TenKH + "|" + TenCT + "|" + DiaChi + "|" + Phone + "|" + Email;

                var itemOrder = ListOrder.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToArray();
                if (itemOrder.Length > 0)
                {
                    foreach (var itemOrd in itemOrder)
                    {
                        if (itemOrd.Length > 10)
                        {
                            var storeItem = itemOrd.Substring(0, 4);
                            if (storeItem != StoreNo)
                            {
                                return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số hóa đơn này {itemOrd} không phải của CH/ST {StoreNo}" });
                            }
                        }
                    }
                }

                // ---- 25/11/2024, tungnt8 -------

                var checkInvoiceMTT = _invoiceBLO.CheckInvoiceByMTT(StoreNo);
                if (checkInvoiceMTT.Result == "Yes")
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"CH/ST {StoreNo} này không được phép xuất hóa đơn. Vui lòng thông báo khách hàng Scan mã QR dưới bill để nhập thông tin xuất hóa đơn." });
                }

                //--------------------------------


                //|| (d.OrderDate >= Convert.ToDateTime("2022-02-01") && d.OrderDate <= Convert.ToDateTime("2022-02-15"))

                var checkList = _invoiceBLO.CheckListOrder(ListOrder, StoreNo).Where(d => d.OrderDate == DateTime.Now.Date).ToList();
                var isSyncSale = false;

                if (itemOrder.Length != checkList.Count) // gọi Function đồng bộ lên POS
                {
                    isSyncSale = true;
                    if (itemOrder.Length > 0)
                    {
                        foreach (var itemOrd in itemOrder)
                        {
                            if (itemOrd.Length > 10)
                            {
                                var posID = itemOrd.Substring(0, 6);
                               // var syncData = UploadSaleFromPOSByOrder(StoreNo, itemOrd, posID); // đóng để test lộn set
                            }
                        }
                    }

                    var checkListSync = _invoiceBLO.CheckListOrder(ListOrder, StoreNo).Where(d => d.OrderDate == DateTime.Now.Date).ToList();//Gọi lại list Order để check sau khi đồng bộ
                    if (itemOrder.Length != checkListSync.Count)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Vui lòng kiểm tra lại số hóa đơn bán hàng" });
                    }
                }

                var checkInvoiceCreated = _invoiceBLO.CheckListOrderCreatedInvoice(ListOrder, StoreNo);
                if (checkInvoiceCreated.Count > 0)
                {
                    var lstStringOrder = string.Join(",", checkInvoiceCreated.Select(a => a.OrderNo).Distinct());
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số bill này ({lstStringOrder}) đã xuất hóa đơn GTGT rồi" });
                }

                var checkOrderPublish = _invoiceBLO.CheckPublishOrderInvoice(ListOrder, StoreNo);
                if (checkOrderPublish != null && checkOrderPublish.CheckResult == 0)
                {
                    if (itemOrder.Length > 0)
                    {
                        foreach (var itemOrd in itemOrder)
                        {
                            _invoiceBLO.ExcuteStagingInvcoie(itemOrd, StoreNo);
                        }
                    }
                }

                var dateOrder = checkList.FirstOrDefault().OrderDate;
                if (dateOrder != null)
                {
                    date = dateOrder.Value.ToString("yyyy-MM-dd");
                }

                bool isSuccess = false;
                var print = new InvoicePrintModel();
                var listInvoice = new List<InvoiceCreatedModel>();
                var listInvoiceVAT = new List<InvoiceInfoVATModel>();
                var listError = new List<string>();

                if (issueVAT == "true")
                {
                    var data = invoice.PublishNormal(date, StoreNo, ListOrder, infoCus, true);

                    var data2 = invoice.PublishNormalOther(date, StoreNo, ListOrder, infoCus, true);

                    var resulData = JsonConvert.DeserializeObject<List<InvoiceResultModel>>(data);

                    var resulData2 = JsonConvert.DeserializeObject<List<InvoiceResultModel>>(data2);
                    var item2 = resulData2.FirstOrDefault();

                    var item = resulData.FirstOrDefault();
                    var listDataSetup = _commonBLO.ListPOSSetup();

                    if (item.ERR == "OK")
                    {
                        isSuccess = true;

                        //List<InvoiceCreatedModel> listInvoice = new List<InvoiceCreatedModel>();

                        print = new InvoicePrintModel
                        {
                            SiteName = item.BranchName,
                            InvoicePattern = item.InvoicePattern,//Mẫu số
                            InvoiceNo = item.InvoiceNo,//Số HĐ GTGT
                            SerialNo = item.SerialNo,//Ký hiệu
                            SaltEncrypt = item.SaltEncrypt,//Mã bí mật
                            FullName = TenKH,
                            Company = TenCT,
                            Address = DiaChi,
                            MST = MST,
                            OrderDate = string.Format("{0:dd/MM/yyyy}", DateTime.Now),
                            StaffCode = checkList.FirstOrDefault()?.CashierID,
                            TeminalID = checkList.FirstOrDefault()?.POSTerminalNo.Substring(4, 2),
                            OrderNo = string.Join("-", itemOrder),
                            LinkSearchInvoice = (listDataSetup != null && listDataSetup.Count > 0 && listDataSetup.Any(d => d.Code == "LINK-SEARCH-INVOICE")) ? listDataSetup.FirstOrDefault(d => d.Code == "LINK-SEARCH-INVOICE").Value : "Not link"
                        };
                        listInvoiceVAT.Add(new InvoiceInfoVATModel
                        {
                            InvoicePattern = item.InvoicePattern,
                            InvoiceNo = item.InvoiceNo,
                            SerialNo = item.SerialNo,
                            SaltEncrypt = item.SaltEncrypt
                        });
                    }
                    else
                    {
                        listError.Add(item.ERRMSG);
                    }

                    if (item2.ERR == "OK")
                    {
                        isSuccess = true;
                        print = new InvoicePrintModel
                        {
                            SiteName = item2.BranchName,
                            InvoicePattern = item2.InvoicePattern,//Mẫu số
                            InvoiceNo = item2.InvoiceNo,//Số HĐ GTGT
                            SerialNo = item2.SerialNo,//Ký hiệu
                            SaltEncrypt = item2.SaltEncrypt,//Mã bí mật
                            FullName = TenKH,
                            Company = TenCT,
                            Address = DiaChi,
                            MST = MST,
                            OrderDate = string.Format("{0:dd/MM/yyyy}", DateTime.Now),
                            StaffCode = checkList.FirstOrDefault()?.CashierID,
                            TeminalID = checkList.FirstOrDefault()?.POSTerminalNo.Substring(4, 2),
                            OrderNo = string.Join("-", itemOrder),
                            LinkSearchInvoice = (listDataSetup != null && listDataSetup.Count > 0 && listDataSetup.Any(d => d.Code == "LINK-SEARCH-INVOICE")) ? listDataSetup.FirstOrDefault(d => d.Code == "LINK-SEARCH-INVOICE").Value : "Not link"
                        };

                        listInvoiceVAT.Add(new InvoiceInfoVATModel
                        {
                            InvoicePattern = item2.InvoicePattern,
                            InvoiceNo = item2.InvoiceNo,
                            SerialNo = item2.SerialNo,
                            SaltEncrypt = item2.SaltEncrypt
                        });
                    }
                    else
                    {
                        listError.Add(item2.ERRMSG);
                    }
                }

                if (issueVAT == "false" || isSuccess == true)
                {
                    var dateTime = DateTime.Now;
                    var isNotVAT = isSuccess == true ? false : true;
                    foreach (var orderNo in itemOrder)
                    {
                        InvoiceCreatedModel invoiceModel = new InvoiceCreatedModel();
                        invoiceModel.SiteNo = StoreNo;
                        invoiceModel.OrderNo = orderNo;
                        invoiceModel.CustomerName = TenKH;
                        invoiceModel.CompanyName = TenCT;
                        invoiceModel.TaxCode = MST;
                        invoiceModel.PhoneNumber = Phone;
                        invoiceModel.Email = Email;
                        invoiceModel.Address = DiaChi;
                        invoiceModel.IsNotVAT = isNotVAT;
                        invoiceModel.CreatedDate = dateTime;
                        invoiceModel.CreatedUser = userName;
                        listInvoice.Add(invoiceModel);
                    }

                    _invoiceBLO.CreatedInvoice(StoreNo, listInvoice);
                    var modelPrint = new VATPrintModel
                    {
                        dataPrint = print,
                        InvoiceInfoPrint = listInvoiceVAT
                    };
                    var message = "Xuất hóa đơn thành công";
                    //if (issueVAT == "false")
                    //{
                    //    message = "Do thông tư 78 chưa được cập nhật,Nên hệ thống đã ghi nhận thông tin xuất hóa đơn của KH, chúng tôi sẽ gửi EMAIL Hóa Đơn VAT đến KH trong thời gian sớm nhất.";
                    //}

                    return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = $"{message}", Data = modelPrint });
                }
                else
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = string.Join(",", listError) });
                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = JsonConvert.SerializeObject(ex) });
            }
        }

        [ParentAuthorize("IssueInvoice")]
        public string UploadSaleFromPOSByOrder(string StoreNo, string OrderNo, string Posterminal)
        {
            string Result = "@@-Not Found " + OrderNo;
            try
            {
                ////////////////////////////////////////
                var dtTransHeaderCentral = _stagingBLO.StagingListOrder(StoreNo, OrderNo);
                if (dtTransHeaderCentral.Count > 0)
                {
                    return Result = "OK";
                }

                //Get danh sach Table can upload
                //DataTable dtListTable = dbSale.ExecuteDataTable("SELECT DISTINCT TableName,DocumentNoName,OrdBy FROM SyncTableFromPOS (Nolock) WHERE [Status] = 1 AND GroupName='SALE' ORDER BY OrdBy");

                // var storeNo = OrderNo.Substring(0, 4);

                //Get data from POS

                bool IsHaveData = false;
                var callPOSID = _commonBLO.GetIPAddressByPOSTerminal(Posterminal);
                var IPAddress = callPOSID == null ? "" : _commonBLO.GetIPAddressByPOSTerminal(Posterminal)?.IpAddress;

                if (!string.IsNullOrEmpty(IPAddress))
                {
                    string ConnectPOS = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, "POS", "POS@1234", "StorePLH");
                    //if (IPAddress == "10.220.24.93")
                    //{
                    //    IPAddress = "10.233.9.123";
                    //    ConnectPOS = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, "POS", "POS@1234", "Store");
                    //}
                    //else
                    //    ConnectPOS = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, "POS", "POS@1234", "Store");

                    using (SqlConnection connection = new SqlConnection(ConnectPOS))
                    {
                        var orderDate = DateTime.Now.Date.ToString("yyyyMMdd");
                        var isSyncSale = false;

                        var checkOrderDateVAT = $"SELECT * FROM TransHeader(NOLOCK) WHERE OrderNo='{OrderNo}' AND CONVERT(DATE,OrderDate)=CONVERT(DATE,'{orderDate}')";
                        SqlCommand commandCheckOrder = new SqlCommand(checkOrderDateVAT, connection);

                        using (var daCheck = new SqlDataAdapter(commandCheckOrder))
                        {
                            try
                            {
                                DataSet dsCheck = new DataSet();
                                daCheck.Fill(dsCheck);
                                var dataTable = dsCheck.Tables[0];
                                if (dataTable.Rows.Count > 0)
                                {
                                    isSyncSale = true;
                                }
                                connection.Close();
                            }
                            catch (Exception ex)
                            {
                                connection.Close();
                            }
                        }
                        if (isSyncSale)
                        {
                            var dtListTable = _stagingBLO.StagingListTableFromPOS(StoreNo);
                            StringBuilder SQLSalePOS = new StringBuilder();

                            foreach (var item in dtListTable)
                            {
                                string TableName = item.TableName;
                                string DocumentNoName = item.DocumentNoName;
                                string SQLTablePOS = string.Format("SELECT '{3}' AS TableName,* FROM [{0}] (Nolock) WHERE {1}='{2}'", TableName, DocumentNoName, OrderNo, TableName);

                                //check neu so chung tu da ton tai thi k add
                                if (dtTransHeaderCentral.Count == 0)
                                {
                                    SQLSalePOS.AppendLine(SQLTablePOS);
                                }
                            }

                            var result = new DataTable();
                            var SqlScript = SQLSalePOS.ToString();
                            SqlCommand command = new SqlCommand(SqlScript, connection);
                            using (var da = new SqlDataAdapter(command))
                            {
                                try
                                {
                                    DataSet dsSalePOS = new DataSet();
                                    da.Fill(dsSalePOS);

                                    foreach (DataTable tblData in dsSalePOS.Tables)
                                    {
                                        if (tblData.Rows.Count > 0)
                                        {
                                            // DataTable dtData = new DataTable();
                                            string TableNamePOS = tblData.Rows[0]["TableName"].ToString();
                                            tblData.TableName = TableNamePOS;

                                            //Insert vao Central
                                            DataTable tblDataFinal = VCM.BLUEPOS.Helpers.Utils.RemoveColumn(tblData, "TableName");
                                            string ResultInsert = _commonBLO.InsertBulk(tblDataFinal, TableNamePOS, StoreNo, OrderNo, Posterminal);

                                            //End Insert vao Central
                                            if (ResultInsert == "OK")
                                            {
                                                IsHaveData = true;
                                            }
                                        }
                                    }
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                    }
                }

                if (IsHaveData == true)
                {
                    _commonBLO.InsertTrans_Invoice(StoreNo, OrderNo);
                }
                Result = "OK";
            }
            catch (Exception ex)
            {
                Result = ex.Message;
            }
            return Result;
        }

        [DisplayName("Hóa đơn dummy")]
        public ActionResult DummyOrder()
        {
            ViewBag.ListSite = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        [ParentAuthorize("DummyOrder")]
        public ActionResult GetListInvoicePattern(string SiteCode, string InvoiceType)
        {
            try
            {
                var info = _invoiceBLO.ListSerialByInvoiceType(SiteCode, InvoiceType);
                if (info != null)
                {
                    return Json(info);
                }
            }
            catch
            {
            }
            return Json(new List<InfoInvoiceModel>());
        }

        [HttpPost]
        [ParentAuthorize("DummyOrder")]
        public ActionResult GetListSerialNo(string SiteCode, string InvoiceType)
        {
            try
            {
                var info = _invoiceBLO.ListSerialByInvoiceType(SiteCode, InvoiceType);
                if (info != null)
                {
                    return Json(info);
                }
            }
            catch
            {
            }
            return Json(new List<InfoInvoiceModel>());
        }

        [HttpPost]
        [ParentAuthorize("DummyOrder")]
        public async Task<ActionResult> CreatedDummy(string StoreNo, string HinhThuc, string InvoicePattern, string SerialNo, string ListInvoiceNo, string NoseriCode)
        {
            //var request = "{\"StoreNo\":" + StoreNo + ",\"HinhThuc\":" + HinhThuc + ",\"InvoicePattern\":" + InvoicePattern + ",\"SerialNo\":" + SerialNo + ",\"InvoiceNo\":" + InvoiceNo + ",\"NoseriCode\":" + NoseriCode + "}";
            try
            {
                if (string.IsNullOrEmpty(ListInvoiceNo))
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số hóa đơn không được trống" });
                }
                var listInvoice = ListInvoiceNo.Split(new char[] { ';', ',', '\n', '\r', ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToList();

                //if (InvoiceNo.Trim().Length != 7)
                //{
                //    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số hóa đơn định dạng 7 ký tự" });
                //}

                Regex regex = new Regex(@"^[0-9]*$");
                var checkCharNumber = listInvoice.Where(b => !regex.IsMatch(b)).ToList();
                if (checkCharNumber != null && checkCharNumber.Count > 0)
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số hóa đơn là ký tự số ({string.Join(";", checkCharNumber)})" });
                }

                var checkListInvoiceDB = _invoiceBLO.CheckListInvoiceNo(StoreNo, listInvoice, InvoicePattern, SerialNo, NoseriCode, HinhThuc);
                if (checkListInvoiceDB.Item1 == true)
                {
                    var listExist = checkListInvoiceDB.Item3;
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Hóa đơn ({string.Join(";", listExist)}) đã tồn tại trong hệ thống" });
                }

                //if (_invoiceBLO.CheckInvoiceNo(StoreNo, InvoiceNo.Trim(), InvoicePattern, SerialNo, NoseriCode, HinhThuc))
                //{
                //    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Hóa đơn ({InvoiceNo}) đã tồn tại trong hệ thống" });
                //}


                //var isEOD = HinhThuc == "EOD" ? 1 : 0;

                //InvoicePOSPL invoice = new InvoicePOSPL();
                //var result = invoice.CreateDummyInvoice(isEOD, DateTime.Now.Date.ToString("yyyy-MM-dd"), StoreNo, InvoicePattern, SerialNo, InvoiceNo, true);
                //var resulData = JsonConvert.DeserializeObject<List<InvoiceResultModel>>(result);
                //if (resulData.FirstOrDefault().ERR == "OK")
                //{
                //    var data = resulData.FirstOrDefault();
                //    var messageOK = $"Tạo hóa đơn dummy thành công, Số hóa đơn {data.InvoiceNo}, ký hiệu{data.SerialNo}, mẫu số {data.InvoicePattern}";
                //    return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = messageOK, Data = resulData.FirstOrDefault() });
                //}
                //else
                //{
                //    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = resulData.FirstOrDefault().ERRMSG });
                //}

                var tasks = new List<Task>();
                var data = new List<Tuple<bool, string>>();
                foreach (var item in listInvoice)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        data.Add(TaskCreatedDummy(StoreNo, HinhThuc, InvoicePattern, SerialNo, item, NoseriCode));
                    }));
                }

                Task taskW = Task.WhenAll(tasks.ToArray());
                await taskW;

                var resultFaild = data.Where(d => d.Item1 == false).Select(a => a.Item2).ToList();
                var resultOK = data.Where(d => d.Item1 == true).Select(a => a.Item2).ToList();
                var message = resultFaild.Count == 0 ? $"Tạo thành công {resultOK.Count} hóa đơn Dummy" : $"Tạo thành công {resultOK.Count}, và thất bại {resultFaild.Count}({string.Join("", resultFaild)}) hóa đơn Dummy";
                return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = $"{message}" });


            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = JsonConvert.SerializeObject(ex) });
            }
        }

        [ParentAuthorize("DummyOrder")]
        public Tuple<bool, string> TaskCreatedDummy(string StoreNo, string HinhThuc, string InvoicePattern, string SerialNo, string InvoiceNo, string NoseriCode)
        {
            var result = new Tuple<bool, string>(false, "");
            var request = "{\"StoreNo\":" + StoreNo + ",\"HinhThuc\":" + HinhThuc + ",\"InvoicePattern\":" + InvoicePattern + ",\"SerialNo\":" + SerialNo + ",\"InvoiceNo\":" + InvoiceNo + ",\"NoseriCode\":" + NoseriCode + "}";
            try
            {
                var isEOD = HinhThuc == "EOD" ? 1 : 0;
                InvoicePOSPL invoice = new InvoicePOSPL();
                var resultCall = invoice.CreateDummyInvoice(isEOD, DateTime.Now.Date.ToString("yyyy-MM-dd"), StoreNo, InvoicePattern, SerialNo, InvoiceNo, true);
                var resulData = JsonConvert.DeserializeObject<List<InvoiceResultModel>>(resultCall);

                if (resulData.FirstOrDefault().ERR == "OK")
                {
                    result = new Tuple<bool, string>(true, $"Tạo hóa đơn dummy thành công");
                    return result;
                    // return Json(new ResultResponse { Status = "1", Message = "Tạo hóa đơn dummy thành công", response = resulData.FirstOrDefault() });
                }
                else
                {
                    result = new Tuple<bool, string>(false, $"{InvoiceNo}-{resulData.FirstOrDefault().ERRMSG}");
                    return result;
                    // return Json(new ResultResponse { Status = "0", Message = resulData.FirstOrDefault().ERRMSG });
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, $"{InvoiceNo}-{JsonConvert.SerializeObject(ex)}");
                return result;
            }
        }

        [DisplayName("Tạo XML hóa đơn")]
        public ActionResult XMLInvoice()
        {
            ViewBag.ListSite = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        [ParentAuthorize("XMLInvoice")]
        public ActionResult RunWriteXMLFromKey(string StoreNo, string Key)
        {
            try
            {
                InvoicePOSPL invoice = new InvoicePOSPL();
                var result = invoice.WriteXMLFromKey(StoreNo, Key, "", -1, "", true);
                var resulData = JsonConvert.DeserializeObject<InvoiceResultModel>(result);
                if (resulData.ERR == "OK")
                {
                    var checkEOD = _invoiceBLO.GetInfoByKey(StoreNo, Key);
                    if (checkEOD != null && checkEOD.IsEOD == true)
                    {
                        invoice.UploadXMLToSftp(true);
                    }
                    return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = "Tạo XML hóa đơn thành công" });
                }
                else
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = resulData.ERRMSG });
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = ex.Message });
            }
        }

        [DisplayName("Ký số normal")]
        public ActionResult SignNormal()
        {
            return View();
        }

        [HttpPost]
        [ParentAuthorize("SignNormal")]
        public ActionResult RunSignNormal()
        {
            try
            {
                EInvoicePLG invoice = new EInvoicePLG();
                var result = invoice.SignNormal();
                if (result)
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = "Chạy chữ ký số normal thành công" });
                }
                else
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = "Chạy chữ ký số normal thất bại" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = ex.Message });
            }
        }

        [DisplayName("Quản lý hóa đơn điện tử Temp")]
        public ActionResult MngInvoiceTemp()
        {
            var result = new ViewMngInvoiceModel();
            try
            {
                //var listStore = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
                //var listInvoiceType = _invoiceBLO.ListInvoiceType();
                //ViewBag.ListStore = listStore;
                //ViewBag.ListInvoiceType = listInvoiceType;

                var siteNo = base.LoginUser.SiteCode; //lOCAL MỚI CÓ
                var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
                var listSetDB = listStoreByUser
                    .GroupBy(d => new { d.ServerIP, d.ServerIPRead, d.ServerStoreDesc })
                    .Select(a => new SQLServerModel
                    {
                        IPServer = a.FirstOrDefault().ServerIP,
                        IPServerRead = a.FirstOrDefault().ServerIPRead,
                        Description = a.FirstOrDefault().ServerStoreDesc
                    }).Distinct().ToList();

                //var listSetDB = new List<SQLServerModel>();
                //var setStoreUser = _commonBLO.GetInforStoreSet(siteNo);

                //if (setStoreUser != null && setStoreUser.Count > 0)
                //{
                //    var first = setStoreUser.FirstOrDefault();
                //    listSetDB.Add(new SQLServerModel { IPServer = first.IPServer, IPServerRead = first.IPServerRead, Description = first.Description });
                //}
                //else
                //{
                //    listSetDB = _commonBLO.ListSQLServer();
                //}

                var ipSvrFirst = listSetDB.FirstOrDefault().IPServer;
                var listStore = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, ipSvrFirst).ToList();
                //result = _invoiceBLO.ViewMngInvoi();
                result.ListStore = listStore;
                result.SetDB = listSetDB;
            }
            catch (Exception ex)
            {
            }
            return View(result);
        }

        [HttpPost]
        [ParentAuthorize("MngInvoiceTemp")]
        public ActionResult MngInvoiceTempLoad()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

                var recordsTotal = 0;
                var setDB = Request?.Form["SetDB"];
                var listStore = Request?.Form["StoreNo"];
                var TuNgay = Request?.Form["TuNgay"];
                var DenNgay = Request?.Form["DenNgay"];
                var MST = Request?.Form["CusTaxCode"];
                var type = Request?.Form["Type"];
                var textSearch = Request?.Form["TextSearch"];

                var fromDate = new DateTime();
                var toDate = new DateTime();

                if (!string.IsNullOrEmpty(TuNgay))
                {
                    fromDate = DateTime.ParseExact(TuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                }
                if (!string.IsNullOrEmpty(DenNgay))
                {
                    toDate = DateTime.ParseExact(DenNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }

                var modelStore = string.IsNullOrEmpty(listStore) ? new ParamJsonStoreList() : JsonConvert.DeserializeObject<ParamJsonStoreList>(listStore);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(listStore) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.StoreNo,
                        SiteName = a.StoreName
                    }).ToList();

                    jsonStore = JsonConvert.SerializeObject(listParam);
                }
                else if (listStore == "Not")
                {
                    jsonStore = "";
                }
                else
                {
                    jsonStore = listStore;
                }

                var model = new InvoiceCusIssueVATRequest
                {
                    SetDB = setDB,
                    ListStoreJson = jsonStore,
                    TuNgay = fromDate,
                    DenNgay = toDate,
                    TaxCode = MST,
                    Address = "",
                    Type = type,
                    TextSearch = textSearch,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                //var result = _invoiceBLO.LoadCustIssueVAT(model);
                var result = _invoiceBLO.LoadCustIssueVATV2(model);  // 16/12/2024, tungnt8

                recordsTotal = (result == null || result.Count() == 0) ? 0 : (int)result.FirstOrDefault().TotalRow;
                return Json(new DataTablesViewModel<InvoiceCreatedModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<InvoiceCreatedModel> { draw = "", recordsFiltered = 0, recordsTotal = 0, data = new List<InvoiceCreatedModel>() });
            }
        }

        [ParentAuthorize("MngInvoiceTemp")]
        public ActionResult ExportExcelInvoiceTmp(string SetDB, string SiteNo, string tuNgay, string denNgay, string TypePublish, string MST, string TextSearch)
        {
            DateTime fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var modelStore = string.IsNullOrEmpty(SiteNo) ? new ParamJsonStoreList() : JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.StoreNo,
                    SiteName = a.StoreName
                }).ToList();

                jsonStore = JsonConvert.SerializeObject(listParam);
            }
            else if (SiteNo == "Not")
            {
                jsonStore = "";
            }
            else
            {
                jsonStore = SiteNo;
            }

            var model = new InvoiceCusIssueVATRequest
            {
                SetDB = SetDB,
                ListStoreJson = jsonStore,
                TuNgay = fromDate,
                DenNgay = toDate,
                Type = TypePublish,
                TaxCode = MST,
                Address = "",
                TextSearch = TextSearch,
                PageNumber = 1,
                PageSize = 5000000
            };

            //var data = _invoiceBLO.LoadCustIssueVAT(model);
            var data = _invoiceBLO.LoadCustIssueVATV2(model);

            if (data != null)
            {
                var dataExport = new List<InvoiceCusIssueVATExportModel>();
                dataExport = data.Select(a => new InvoiceCusIssueVATExportModel
                {
                    SiteNo = a.SiteNo,
                    CustomerName = a.CustomerName,
                    TaxCode = a.TaxCode,
                    PhoneNumber = a.PhoneNumber,
                    Email = a.Email,
                    CompanyName = a.CompanyName,
                    AddressCus = a.AddressCus,
                    CreatedDate = string.Format("{0:dd/MM/yyyy HH:mm:ss}", a.CreatedDate),
                    OrderNo = a.OrderNo,
                    PrepaymentAmount = a.PrepaymentAmount
                }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataExport.Select(x => new
                    {
                        x.SiteNo,
                        x.CustomerName,
                        x.TaxCode,
                        x.PhoneNumber,
                        x.Email,
                        x.CompanyName,
                        x.AddressCus,
                        x.CreatedDate,
                        x.OrderNo,
                        x.PrepaymentAmount
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Cửa hàng";
                    worksheet.Cells[1, 2].Value = "Tên khách hàng";
                    worksheet.Cells[1, 3].Value = "Mã số thuế";
                    worksheet.Cells[1, 4].Value = "Số điện thoại";
                    worksheet.Cells[1, 5].Value = "Email";
                    worksheet.Cells[1, 6].Value = "Tên công ty";
                    worksheet.Cells[1, 7].Value = "Địa chỉ";
                    worksheet.Cells[1, 8].Value = "Ngày xuất hóa đơn";
                    worksheet.Cells[1, 9].Value = "Số đơn hàng";
                    worksheet.Cells[1, 10].Value = "Tổng số tiền";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(dataExport, false).AutoFitColumns();
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"CustommerVAT_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [ParentAuthorize("MngInvoiceTemp")]
        public ActionResult _ViewUpdateInfoVAT(string ipServer, string storeNo, string orderNo)
        {
            var result = new InvoiceCreatedModel();
            try
            {
                result = _invoiceBLO.GetInfoVAT(ipServer, storeNo, orderNo);
            }
            catch (Exception ex)
            {
            }
            return PartialView(result);
        }

        [HttpPost]
        [ParentAuthorize("MngInvoiceTemp")]
        public ActionResult UpdateInfoVAT(InvoiceCreatedModel model)
        {
            try
            {
                var userName = base.LoginUser.UserName;
                var MST = !string.IsNullOrEmpty(model.TaxCode) ? model.TaxCode.Trim() : "";
                var TenCT = !string.IsNullOrEmpty(model.CompanyName) ? model.CompanyName.Trim() : "";
                var DiaChi = !string.IsNullOrEmpty(model.Address) ? model.Address.Trim() : "";
                var TenKH = !string.IsNullOrEmpty(model.CustomerName) ? model.CustomerName.Trim() : "";
                var Email = !string.IsNullOrEmpty(model.Email) ? model.Email.Trim() : "";
                var Phone = !string.IsNullOrEmpty(model.PhoneNumber) ? model.PhoneNumber.Trim() : "";

                if (!string.IsNullOrEmpty(MST))
                {
                    var checkTaxCode = VCM.BLUEPOS.Helpers.Utils.CheckTaxCode(MST);
                    if (checkTaxCode.Item1 == false)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"{checkTaxCode.Item2}" });
                    }
                    if (string.IsNullOrEmpty(TenCT))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập tên công ty" });
                    }
                }

                if (!string.IsNullOrEmpty(TenCT))
                {
                    if (string.IsNullOrEmpty(DiaChi))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập địa chỉ" });
                    }
                    if (TenCT.Length > 400)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Tên công ty không vượt quá 400 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(TenCT))
                {
                    if (string.IsNullOrEmpty(DiaChi))
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Vui lòng nhập địa chỉ" });
                    }
                }
                if (!string.IsNullOrEmpty(TenKH))
                {
                    if (TenKH.Length > 100)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Tên khách hàng không vượt quá 100 ký tự" });
                    }
                }
                if (!string.IsNullOrEmpty(Email))
                {
                    if (Email.Length > 50)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Địa chỉ Email không vượt quá 50 ký tự" });
                    }
                }
                if (!string.IsNullOrEmpty(Phone))
                {
                    if (Phone.Length > 20)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Số điện thoại không vượt quá 20 ký tự" });
                    }
                }

                if (!string.IsNullOrEmpty(DiaChi))
                {
                    if (DiaChi.Length > 400)
                    {
                        return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Địa chỉ không vượt quá 400 ký tự" });
                    }
                }

                var result = _invoiceBLO.UpdateInfoVAT(model);

                if (result.Item1)
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.OK, Message = $"{result.Item2}" });
                }
                else
                {
                    return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"{result.Item2}" });
                }

            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.InternalServerError, Message = JsonConvert.SerializeObject(ex) });
            }
        }

        // 26/11/2024, tungnt8 : Khách hàng định danh xuất hóa đơn MTT

        [DisplayName("KH định danh xuất hóa đơn MTT")] 
        public ActionResult CustomerInfoByInvoiceMTT()
        {
            var result = new ViewMngInvoiceModel();
            try
            {
                var siteNo = base.LoginUser.SiteCode; 
                var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
                var listSetDB = listStoreByUser
                    .GroupBy(d => new { d.ServerIP, d.ServerIPRead, d.ServerStoreDesc })
                    .Select(a => new SQLServerModel
                    {
                        IPServer = a.FirstOrDefault().ServerIP,
                        IPServerRead = a.FirstOrDefault().ServerIPRead,
                        Description = a.FirstOrDefault().ServerStoreDesc
                    }).Distinct().ToList();

                var ipSvrFirst = listSetDB.FirstOrDefault().IPServer;
                var listStore = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, ipSvrFirst).ToList();               
                result.ListStore = listStore;
                result.SetDB = listSetDB;
            }
            catch (Exception ex)
            {
            }
            return View(result);
        }

        [HttpPost]
        [ParentAuthorize("CustomerInfoByInvoiceMTT")]
        public ActionResult GetCustomerInfoByInvoiceMTT()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

                var recordsTotal = 0;
                var setDB = Request?.Form["SetDB"];
                var listStore = Request?.Form["StoreNo"];
                var TuNgay = Request?.Form["TuNgay"];
                var DenNgay = Request?.Form["DenNgay"];
                var TaxCode = Request?.Form["CusTaxCode"];
                var TextSearch = Request?.Form["TextSearch"];
                var fromDate = new DateTime();
                var toDate = new DateTime();

                if (!string.IsNullOrEmpty(TuNgay))
                {
                    fromDate = DateTime.ParseExact(TuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                if (!string.IsNullOrEmpty(DenNgay))
                {
                    toDate = DateTime.ParseExact(DenNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }

                var modelStore = string.IsNullOrEmpty(listStore) ? new ParamJsonStoreList() : JsonConvert.DeserializeObject<ParamJsonStoreList>(listStore);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(listStore) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.StoreNo,
                        SiteName = a.StoreName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }
                else if (listStore == "Not")
                {
                    jsonStore = "";
                }
                else
                {
                    jsonStore = listStore;
                }

                var model = new CustomerInfoInvoiceMTTRequest
                {
                    SetDB = setDB,
                    ListStoreJson = jsonStore,
                    TuNgay = fromDate,
                    DenNgay = toDate,
                    TaxCode = TaxCode,
                    TextSearch = TextSearch,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var result = _invoiceBLO.GetCustomerInfoInvoiceMTT(model);
                recordsTotal = (result == null || result.Count() == 0) ? 0 : (int)result.FirstOrDefault().Total;
                return Json(new DataTablesViewModel<CustomerInfoInvoiceMTTModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<CustomerInfoInvoiceMTTModel> { draw = "", recordsFiltered = 0, recordsTotal = 0, data = new List<CustomerInfoInvoiceMTTModel>() });
            }
        }

        [ParentAuthorize("CustomerInfoByInvoiceMTT")]
        public ActionResult ExportCustomerInfoInvoiceMTT(string SetDB, string listStore, string tuNgay, string denNgay, string MST, string TextSearch)
        {
            DateTime fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var modelStore = string.IsNullOrEmpty(listStore) ? new ParamJsonStoreList() : JsonConvert.DeserializeObject<ParamJsonStoreList>(listStore);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(listStore) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.StoreNo,
                    SiteName = a.StoreName
                }).ToList();

                jsonStore = JsonConvert.SerializeObject(listParam);
            }
            else if (listStore == "Not")
            {
                jsonStore = "";
            }
            else
            {
                jsonStore = listStore;
            }

            var model = new CustomerInfoInvoiceMTTRequest
            {
                SetDB = SetDB,
                ListStoreJson = jsonStore,
                TuNgay = fromDate,
                DenNgay = toDate,
                TaxCode = MST,
                TextSearch = TextSearch,
                PageNumber = 1,
                PageSize = 5000000
            };

            var data = _invoiceBLO.ExportCustomerInfoInvoiceMTT(model);

            if (data != null)
            {
                var dataExport = new List<ExportCustomerInfoInvoiceMTTModel>();
                dataExport = data.Select(a => new ExportCustomerInfoInvoiceMTTModel
                {
                    StoreNo = a.StoreNo,
                    CusName = a.CusName,
                    CusTaxCode = a.CusTaxCode,
                    CusPhone = a.CusPhone,
                    Email = a.Email,
                    CusCom = a.CusCom,
                    Address = a.Address,
                    InvoicePattern = a.InvoicePattern,
                    SerialNo = a.SerialNo,
                    InvoiceNo = a.InvoiceNo,
                    InvoiceTypeName = a.InvoiceTypeName,
                    ArisingDate = a.ArisingDate,
                    CreateDate = a.CreateDate,
                    HinhThuc = a.HinhThuc,
                    Remark = a.Remark,
                    OrderNo = a.OrderNo,
                    Amount = a.Amount,
                    BillTaxCode = a.BillTaxCode,
                    Key = a.Key
                }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataExport.Select(x => new
                    {
                        x.StoreNo,
                        x.CusName,
                        x.CusTaxCode,
                        x.CusPhone,
                        x.Email,
                        x.CusCom,
                        x.Address,
                        x.InvoicePattern,
                        x.SerialNo,
                        x.InvoiceNo,
                        x.InvoiceTypeName,
                        x.ArisingDate,
                        x.CreateDate,
                        x.HinhThuc,
                        x.Remark,
                        x.OrderNo,
                        x.Amount,
                        x.BillTaxCode,
                        x.Key
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã cửa hàng";
                    worksheet.Cells[1, 2].Value = "Tên khách hàng";
                    worksheet.Cells[1, 3].Value = "Mã số thuế";
                    worksheet.Cells[1, 4].Value = "Số ĐT";
                    worksheet.Cells[1, 5].Value = "Email";
                    worksheet.Cells[1, 6].Value = "Tên Công ty";
                    worksheet.Cells[1, 7].Value = "Địa chỉ";
                    worksheet.Cells[1, 8].Value = "Mẫu số";
                    worksheet.Cells[1, 9].Value = "Ký hiệu";
                    worksheet.Cells[1, 10].Value = "Số hóa đơn";
                    worksheet.Cells[1, 11].Value = "Loại hóa đơn";
                    worksheet.Cells[1, 12].Value = "Ngày xuất HĐ";
                    worksheet.Cells[1, 13].Value = "Ngày phát hành";
                    worksheet.Cells[1, 14].Value = "Hình thức";
                    worksheet.Cells[1, 15].Value = "Thay thế / điều chỉnh";
                    worksheet.Cells[1, 16].Value = "Số đơn hàng";
                    worksheet.Cells[1, 17].Value = "Tổng số tiền";
                    worksheet.Cells[1, 18].Value = "Mã CQT";
                    worksheet.Cells[1, 19].Value = "Key";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 19])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(dataExport, false).AutoFitColumns();
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"InfoCustomerByInvoiceCash_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        public ActionResult GetBranchByNo(string branchNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };

            try
            {
                var infoBranch = _invoiceBLO.GetBranchNo(branchNo);
                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = "OK",
                    Data = infoBranch
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = JsonConvert.SerializeObject(ex)
                };
            }
            return Json(result);
        }

        //---- 05/10/2023 : Hàm mới ----

        [HttpPost]
        public async Task<ActionResult> SearchTaxCodeByInvoice(string TaxCode)
        {
            try
            {
                var kq = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = "Có lỗi xảy ra",
                    Data = null
                };

                if (!string.IsNullOrEmpty(TaxCode))
                {
                    var checkTaxCode = VCM.BLUEPOS.Helpers.Utils.CheckTaxCode(TaxCode.TrimEnd());
                    if (checkTaxCode.Item1 == false)
                    {
                        kq = new ResultResponse
                        {
                            isFlag = 222, // loi khong dung dinh dang mst, ma loi nay tự quy định
                            Status = HttpStatusCode.NotModified,
                            Message = $"{checkTaxCode.Item2}",
                            Data = null
                        };
                        return Json(kq);
                    }
                }

                using (var client = new HttpClient())
                {
                    try
                    {
                        // ------ code new ------

                        client.Timeout = TimeSpan.FromMinutes(2);
                        client.BaseAddress = new Uri(linkAPI);

                        var byteArray = Encoding.ASCII.GetBytes($"{pl_user_api_partner}:{pl_pass_api_partner}");
                        var authenBasic = Convert.ToBase64String(byteArray);

                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenBasic);

                        var url = $"{apipartnertaxinfo}/{TaxCode}";
                        var response = await client.GetAsync(url);
                        var resultStr = response.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<ApiPartnerTaxCodeInfoResponseModel>(resultStr);

                        // ------  code old ------

                        //ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                        //httpClient.BaseAddress = new Uri(linkAPI);
                        //httpClient.Timeout = TimeSpan.FromMinutes(2);

                        //--- var path = $"{apipartnertaxinfo}?TaxCode={TaxCode}";

                        //var path = $"{apipartnertaxinfo}/{TaxCode}";
                        //var response = await httpClient.GetAsync(path);
                        //var resultStr = response.Content.ReadAsStringAsync().Result;
                        //var result = JsonConvert.DeserializeObject<ApiPartnerTaxCodeInfoResponseModel>(resultStr);

                        if (resultStr == null) {
                            kq = new ResultResponse
                            {
                                isFlag = 401,
                                Status = HttpStatusCode.BadRequest,
                                Message = $"Có lỗi xảy ra. Vui lòng kiểm tra lại",
                                Data = null
                            };
                            return Json(kq);
                        }

                        if (result.Meta.Code == 200)
                        {
                            var model = new SearchTaxCodeInfoModel();
                            model = new SearchTaxCodeInfoModel()
                            {
                                Id = result.Data.Id,
                                Name = result.Data.Name,
                                Address = result.Data.Address
                            };

                            kq = new ResultResponse
                            {
                                isFlag = 200,
                                Status = HttpStatusCode.OK,
                                Message = "",
                                Data = model
                            };
                            return Json(kq);
                        }
                        else if (result.Meta.Code == 400) // khong tim thay MST
                        {
                            kq = new ResultResponse
                            {
                                isFlag = 400,
                                Status = HttpStatusCode.BadRequest, // 400
                                Message = $"MST {TaxCode} này không tồn tại trong hệ thống. Vui lòng kiểm tra lại. Bạn có muốn sử dụng MST này không ? (Y/N)",
                                Data = null
                            };
                            return Json(kq);
                        }
                    }
                    catch (Exception ex)
                    {
                        kq = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = JsonConvert.SerializeObject(ex)
                        };
                        return Json(kq);
                    }
                }
                return Json(kq);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // 25/11/2024, tungnt8
        [DisplayName("Tạo hóa đơn dummy MTT")]
        public ActionResult DummyOrderMTT()
        {
            var result = new ViewMngInvoiceModel();
            try
            {
                var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
                var listSetDB = listStoreByUser
                    .GroupBy(d => new { d.Code, d.ServerIP, d.ServerIPRead, d.ServerStoreDesc})
                    .Select(a => new SQLServerModel
                    {
                        Code = a.FirstOrDefault().Code,
                        IPServer = a.FirstOrDefault().ServerIP,
                        IPServerRead = a.FirstOrDefault().ServerIPRead,
                        Description = a.FirstOrDefault().ServerStoreDesc
                    }).Distinct().ToList();

                result.SetDB = listSetDB;
            }
            catch (Exception ex)
            {
            }
            return View(result);
        }

        [HttpPost]
        [ParentAuthorize("DummyOrderMTT")]
        public JsonResult CreatedDummyMTT(string ipSetServer, string listInvoiceJson)
        {
            if (string.IsNullOrEmpty(listInvoiceJson))
            {
                return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Nhập nội dung JSON hóa đơn Dummy MTT" });
            }

            try
            {
                var result = _invoiceBLO.CreatedInvoiceDummyMTT(ipSetServer, listInvoiceJson);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new ResultResponse { Status = HttpStatusCode.BadRequest, Message = JsonConvert.SerializeObject(ex) });
            }
        }







    }
}