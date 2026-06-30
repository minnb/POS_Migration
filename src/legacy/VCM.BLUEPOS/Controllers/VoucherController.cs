using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;
using System.Configuration;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Voucher;
using VCM.BLUEPOS.Business.Voucher;
using VCM.BLUEPOS.Business.Common;
using System.Threading.Tasks;
using System.Text;
using VCM.BLUEPOS.Common;
using System.Web.Configuration;
using System.Net;

namespace PLG.Controllers
{
    public class VoucherController : BaseController
    {
        private IVoucherBLO _voucherBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        private string linkAPI { get; set; }

        string PLH_SAP_VCHCPN_CREATE = WebConfigurationManager.AppSettings["PLH_SAP_VCHCPN_CREATE"];
        string PLH_SAP_VCHCPN_PASS = WebConfigurationManager.AppSettings["PLH_SAP_VCHCPN_PASS"];

        public VoucherController(IVoucherBLO voucherBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _voucherBLO = voucherBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
            linkAPI = _commonBLO.ListPOSSetup().FirstOrDefault(d => d.Code == "LINKAPI").Value;
        }

        public ActionResult VoucherList()
        {
            return View();
        }

        public ActionResult CreateVoucher()
        {
            ViewBag.ListArticleType = _commonBLO.GetComboxArticleType();
            ViewBag.ListCouponType = _commonBLO.GetComboxVoucherCouponList();
            ViewBag.ListUnitOfMeasure = _commonBLO.GetComboxUnitOfMeasure();
            return View();
        }

        public JsonResult CreateVoucherCoupon(CreateVoucherCouponResponseModel req)
        {
            var itemApplyModel = new List<CreateVoucherLineModel>();
            if (req.ListItemApplyStr != null)
            {
                itemApplyModel = JsonConvert.DeserializeObject<List<CreateVoucherLineModel>>(req.ListItemApplyStr);
            }
            var model = new CreateVoucherCouponModel
            {
                ItemName = req.ItemName,
                CouponCode = req.CouponCode,
                ArticleType = req.ArticleType,
                UnitOfMeasure = req.UnitOfMeasure,
                DiscountType = int.Parse(req.DiscountType),
                DiscountValue = (float)(string.IsNullOrEmpty(req.DiscountValue.ToString()) ? 0 : Convert.ToDecimal(req.DiscountValue.ToString())),
                ValueOfVoucher = (float)(string.IsNullOrEmpty(req.ValueOfVoucher.ToString()) ? 0 : Convert.ToDecimal(req.ValueOfVoucher.ToString())),
                MaxAmount = (float)(string.IsNullOrEmpty(req.MaxAmount.ToString()) ? 0 : Convert.ToDecimal(req.MaxAmount.ToString())),
                LimitQty = string.IsNullOrEmpty(req.LimitQty.ToString()) ? 99999999 : int.Parse(req.LimitQty.ToString()),
                Blocked = Convert.ToByte(req.BlockedStr),
                IsCheckItem = req.IsCheckItem,
                StartingDate = DateTime.ParseExact(req.StartingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                EndingDate = DateTime.ParseExact(req.EndingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                ListItemApply = itemApplyModel,
                LastDateModified = DateTime.Now,
                CreatedUser = LoginUser.UserName,
                CreatedDate = DateTime.Now
            };
            var data = _voucherBLO.CreateVoucherCoupon(model);
            if (data == null)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Không có dữ liệu"
                });
            }
            return Json(data);
        }

        public ActionResult UpdateVoucher()
        {
            ViewBag.ListCouponType = _commonBLO.GetComboxVoucherCouponList();
            ViewBag.ListUnitOfMeasure = _commonBLO.GetComboxUnitOfMeasure();
            return View();
        }

        public JsonResult GetUpdateVoucherHeaderList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var pageIndex = skip/pageSize;
            var recordsTotal = 0;

            var _codeVoucher = Request?.Form["codeVoucher"];
            if (!string.IsNullOrEmpty(_codeVoucher))
            {
                _codeVoucher = _codeVoucher.Trim();
            }

            var _nameVoucher = Request?.Form["nameVoucher"];
            if (!string.IsNullOrEmpty(_nameVoucher))
            {
                _nameVoucher = _nameVoucher.Trim();
            }

            var _serialVoucher = Request?.Form["serialVoucher"];
            if (!string.IsNullOrEmpty(_serialVoucher))
            {
                _serialVoucher = _serialVoucher.Trim();
            }

            var _typeVoucher = Request?.Form["typeVoucher"];
            if (!string.IsNullOrEmpty(_typeVoucher))
            {
                _typeVoucher = _typeVoucher.Trim();
            }

            var _statusVoucher = int.Parse(Request?.Form["statusVoucher"]);
            
            var data = _voucherBLO.GetUpdateVoucherHeaderList(_codeVoucher, _nameVoucher, _serialVoucher, _typeVoucher, _statusVoucher, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<VoucherCouponHeaderListModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult UpdateVoucherHeader(UpdateVoucherHeaderResponseModel req)
        {
            req.UpdatedUser = LoginUser.UserName;
            req.UpdatedDate = DateTime.Now;

            var model = new UpdateVoucherHeaderModel
            {
                ItemNo = req.ItemNo,
                ItemName = req.ItemName,
                UnitOfMeasure = req.UnitOfMeasure,
                DiscountType = int.Parse(req.DiscountType),
                DiscountValue = (float?)(string.IsNullOrEmpty(req.DiscountValue.ToString()) ? 0 : Convert.ToDecimal(req.DiscountValue.ToString())),
                MaxAmount = (float?)(string.IsNullOrEmpty(req.MaxAmount.ToString()) ? 0 : Convert.ToDecimal(req.MaxAmount.ToString())),
                ArticleType = req.ArticleType,
                ValueOfVoucher = (float?)(string.IsNullOrEmpty(req.ValueOfVoucher.ToString()) ? 0 : Convert.ToDecimal(req.ValueOfVoucher.ToString())),
                CouponCode = req.CouponCode,
                LimitQty = int.Parse(req.LimitQty),
                Blocked = Convert.ToByte(req.BlockedStr),
                IsCheckItem = req.IsCheckItem,
                StartingDate = DateTime.ParseExact(req.StartingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                EndingDate = DateTime.ParseExact(req.EndingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                LastDateModified = req.UpdatedDate,
                UpdatedUser = req.UpdatedUser,
                UpdatedDate = req.UpdatedDate
            };
            var data = _voucherBLO.UpdateVoucherHeader(model);
            if (data == null)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Không có dữ liệu"
                });
            }
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteVoucherHeaderList(DeleteVoucherHeaderModel req)
        {
            var data = _voucherBLO.DeleteVoucherHeaderList(req);
            return Json(data);
        }

        public ActionResult ExportVoucherHeaderList(string CodeVoucher, string NameVoucher, string SerialVoucher, string TypeVoucher, int StatusVoucher)
        {
            var data = _voucherBLO.ExportGetVoucherHeaderList(CodeVoucher, NameVoucher, SerialVoucher, TypeVoucher, StatusVoucher);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportVoucherHeaderListModel
                    {
                        CouponCode = x.CouponCode,
                        ItemNo = x.ItemNo,
                        ItemName = x.ItemName,
                        ArticleType = x.ArticleType,
                        UnitOfMeasure = x.UnitOfMeasure,
                        Blocked = x.Blocked,
                        ValueOfVoucher = x.ValueOfVoucher,
                        DiscountType = x.DiscountType,
                        DiscountValue = x.DiscountValue,
                        MaxAmount = x.MaxAmount,
                        LimitQty = x.LimitQty,
                        IsCheckItem = x.IsCheckItem,
                        StartingDateStr = x.StartingDateStr,
                        EndingDateStr = x.EndingDateStr,
                        LastDateModifiedStr = x.LastDateModifiedStr

                    }).ToList();

                    worksheet.Cells[1, 1].Value  = "Số serial voucher/coupon";
                    worksheet.Cells[1, 2].Value  = "Mã voucher/coupon";
                    worksheet.Cells[1, 3].Value  = "Tên voucher/coupon";
                    worksheet.Cells[1, 4].Value  = "Loại voucher/coupon";
                    worksheet.Cells[1, 5].Value  = "Đơn vị tính";
                    worksheet.Cells[1, 6].Value  = "Trạng thái";
                    worksheet.Cells[1, 7].Value  = "Giá trị voucher/coupon";
                    worksheet.Cells[1, 8].Value  = "Loại giảm giá";
                    worksheet.Cells[1, 9].Value  = "Giá trị giảm";
                    worksheet.Cells[1, 10].Value = "Giá trị lớn nhất"; 
                    worksheet.Cells[1, 11].Value = "Voucher/Coupon limit";
                    worksheet.Cells[1, 12].Value = "Tổng bill";
                    worksheet.Cells[1, 13].Value = "Ngày bắt đầu hiệu lực";
                    worksheet.Cells[1, 14].Value = "Ngày hết hiệu lực";
                    worksheet.Cells[1, 15].Value = "Ngày cập nhật";
                    
                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 15])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"VoucherHeaderList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.Close();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetUpdateVoucherLineList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var pageIndex = skip/pageSize;
            var recordsTotal = 0;

            var _noVoucher = Request?.Form["noVoucher"];
            if (!string.IsNullOrEmpty(_noVoucher))
            {
                _noVoucher = _noVoucher.Trim();
            }

            var _nameVoucher = Request?.Form["nameVoucher"];
            if (!string.IsNullOrEmpty(_nameVoucher))
            {
                _nameVoucher = _nameVoucher.Trim();
            }

            var _lineVoucher = Request?.Form["lineItemNo"];
            if (!string.IsNullOrEmpty(_lineVoucher))
            {
                _lineVoucher = _lineVoucher.Trim();
            }

            var _serialVoucher = Request?.Form["serialVoucher"];
            if (!string.IsNullOrEmpty(_serialVoucher))
            {
                _serialVoucher = _serialVoucher.Trim();
            }

            var _barCode = Request?.Form["barCode"];
            if (!string.IsNullOrEmpty(_barCode))
            {
                _barCode = _barCode.Trim();
            }

            var _typeVoucher = Request?.Form["typeVoucher"];
            if (!string.IsNullOrEmpty(_typeVoucher))
            {
                _typeVoucher = _barCode.Trim();
            }

            var _statusVoucher = int.Parse(Request?.Form["statusVoucher"]);
            var data = _voucherBLO.GetUpdateVoucherLineList(_noVoucher, _nameVoucher, _lineVoucher, _serialVoucher,  _barCode, _typeVoucher, _statusVoucher, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<VoucherLineResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult DeleteVoucherLineList(DeleteVoucherLineModel req)
        {
            var data = _voucherBLO.DeleteVoucherLineList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateVoucherLine(UpdateVoucherLineResponseModel req)
        {
            req.UpdatedUser = LoginUser.UserName;
            req.UpdatedDate = DateTime.Now;
            var model = new UpdateVoucherLineModel
            {
                ItemNo = req.ItemNo,
                LineItemNo = req.LineItemNo,
                Description = req.Description,
                UnitOfMeasure = string.Empty,
                Barcode = req.Barcode,
                Pkey = req.Pkey,
                UpdatedUser = req.UpdatedUser,
                UpdatedDate = req.UpdatedDate
            };
            var data = _voucherBLO.UpdateVoucherLine(model);
            if (data == null)
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Không có dữ liệu"
                });
            }
            return Json(data);
        }

        public ActionResult ExportVoucherLineList(string NoVoucherLine, string NameVoucherLine, string LineItemNo, string SerialVoucherLine, string BarcodeVoucherLine, string TypeVoucherLine, int StatusVoucherLine)
        {
            var data = _voucherBLO.ExportGetVoucherLineList(NoVoucherLine, NameVoucherLine, LineItemNo, SerialVoucherLine, BarcodeVoucherLine, TypeVoucherLine, StatusVoucherLine);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportVoucherLineResponseModel
                    {
                        CouponCode = x.CouponCode,
                        ItemNo = x.ItemNo,
                        ItemName = x.ItemName,
                        ArticleType = x.ArticleType,
                        Blocked = x.Blocked,
                        Barcode = x.Barcode,
                        LineItemNo = x.LineItemNo,
                        Description = x.Description,
                        UnitOfMeasure = x.UnitOfMeasure
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số serial voucher/coupon";
                    worksheet.Cells[1, 2].Value = "Mã voucher/coupon";
                    worksheet.Cells[1, 3].Value = "Tên voucher/coupon";
                    worksheet.Cells[1, 4].Value = "Loại voucher/coupon";
                    worksheet.Cells[1, 5].Value = "Trạng thái hoạt động";
                    worksheet.Cells[1, 6].Value = "Mã Barcode";
                    worksheet.Cells[1, 7].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 8].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 9].Value = "Đơn vị tính";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 9])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"VoucherLineList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.Close();
                        Response.End();

                    }
                }
            }

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [DisplayName("Tra cứu V/C đã phát hành")]
        public ActionResult VoucherPublished()
        {
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(base.LoginUser.UserName);
            ViewBag.ListCouponType = _commonBLO.GetComboxVoucherCouponList();
            return View();
        }
        public JsonResult GetVoucherPublishedList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var pageIndex = skip / pageSize;
            var recordsTotal = 0;

            var fromDate = DateTime.Now;
            var toDate = DateTime.Now;

            if (!string.IsNullOrEmpty(Request?.Form["fromDate"]))
            {
                fromDate = Convert.ToDateTime(Request?.Form["fromDate"]);
            }
            if (!string.IsNullOrEmpty(Request?.Form["toDate"]))
            {
                toDate = Convert.ToDateTime(Request?.Form["toDate"]);
            }

            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var voucherType = Request?.Form["VoucherType"];
            if (!string.IsNullOrEmpty(voucherType))
            {
                voucherType = voucherType.Trim();
            }

            var statusVoucher = Request?.Form["StatusVoucher"];
            if (!string.IsNullOrEmpty(statusVoucher))
            {
                statusVoucher = statusVoucher.Trim();
            }

            var sendSAP = Request?.Form["SendSAP"];

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var data = _voucherBLO.GetVoucherPublishedList(fromDate, toDate, storeNo, voucherType, textSearch, statusVoucher, sendSAP, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<VoucherPublishedResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult CheckToSAP(string VoucherNumber)
        {
            try
            {
                var sendSuccess = new List<ListVoucherResendModel>();
                var item = new ListVoucherResendModel();
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromMinutes(1);
                        client.BaseAddress = new Uri(linkAPI);
                        var response = client.GetAsync("api/sap/CheckVoucher?voucherNumber=" + VoucherNumber + "").Result;                
                        var data = response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ResultResponse>(data.Result);
                        var jsonData = JsonConvert.SerializeObject(result.Data);
                        var responseResult = JsonConvert.DeserializeObject<CheckVoucherResponse>(jsonData);
                        return Json(new { ResutlData = responseResult, ResultStatus = result.Status, ResultMessage = result.Message });
                    }
                    catch (Exception ex)
                    {
                    }
                }
                return Json(new { ResutlData = new CheckVoucherResponse(), ResultStatus = "400", ResultMessage = $"Check lại mã voucher {VoucherNumber}" });
            }
            catch (Exception ex)
            {
                return Json(new { ResutlData = new CheckVoucherResponse(), ResultStatus = "400", ResultMessage = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult> ResendVoucherToSAP(string modelVoucher)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,
                Message = "",
                Data = null
            };
            var sendSuccess = new List<VoucherResendSAPModel>();
            try
            {
                if (modelVoucher == null)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.PreconditionFailed,
                        Message = "Không có dữ liệu để resend SAP. Vui lòng kiểm tra lại",
                        Data = null
                    };
                    return Json(result);
                }

                
                var model = new VoucherResendSAPModel();
                var sendAPI = new List<VoucherResendSAPModel>();       
                var lstVoucher = JsonConvert.DeserializeObject<List<VoucherResendSAPModel>>(modelVoucher);
                if (lstVoucher.Count > 0)
                {
                    var tasks = new List<Task>();
                    foreach (var item in lstVoucher)
                    {
                        model = new VoucherResendSAPModel()
                        {
                            StoreNo = item.StoreNo,
                            POSTerminal = item.POSTerminal,
                            UserPOS = item.UserPOS,
                            ListSerial = item.ListSerial.ToList()
                        };

                        sendAPI.Add(model);

                        tasks.Add(Task.Run(() =>
                        {
                                var endPoint = $"{PLH_SAP_VCHCPN_CREATE}";
                                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                                using (var client = new HttpClient())
                                {
                                    try
                                    {
                                        client.BaseAddress = new Uri(linkAPI);
                                        client.Timeout = TimeSpan.FromMinutes(2);
                                        client.DefaultRequestHeaders.Add("Authorization", PLH_SAP_VCHCPN_PASS);

                                        string modelJSON = JsonConvert.SerializeObject(sendAPI);
                                        var response = client.PostAsync(endPoint, new StringContent(modelJSON, Encoding.UTF8, "application/json")).Result;
                                        var resultStr = response.Content.ReadAsStringAsync().Result;
                                        var data = JsonConvert.DeserializeObject<ApiResponse>(resultStr);

                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {                                            
                                            result = new ResultResponse
                                            {
                                                Status = data.StatusCode,
                                                Message = data.Message,
                                                Data = data.Msg
                                            };
                                        }
                                        else
                                        {
                                            result = new ResultResponse
                                            {
                                                Status = data.StatusCode,
                                                Message = data.Message,
                                                Data = data.Msg
                                            };
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        result = new ResultResponse
                                        {
                                            Status = HttpStatusCode.InternalServerError,
                                            Message = $"Lỗi {ex.Message}",
                                            Data = JsonConvert.SerializeObject(ex)
                                        };
                                    }
                                }
                                sendSuccess.Add(model);
                        }));
                    }

                    Task taskFinish = Task.WhenAll(tasks.ToArray());
                    await taskFinish;
                    if (taskFinish.Status == TaskStatus.RanToCompletion)
                    {
                        if (sendSuccess.Count > 0)
                        {
                            var kq =  _voucherBLO.UpdateStatusVoucherV2(sendSuccess);
                            result = new ResultResponse
                            {
                                Status = kq.Status,
                                Message = kq.Message,
                                Data = kq.Data
                            };
                        }
                    }
                }
                //return Json(sendSuccess);
            }
            catch(Exception ex)
            {
            }
            return Json(result);
        }

        public ActionResult ExportExcelVoucherPublishedList(DateTime FromDate, DateTime ToDate, string StoreNo, string VoucherType, string TextSearch,  string StatusVoucher, string SendSAP)
        {
            var data = _voucherBLO.ExportExcelVoucherPublishedList(FromDate, ToDate, StoreNo, VoucherType, TextSearch, StatusVoucher, SendSAP);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportExcelVoucherPublishedResponseModel
                    {
                        StoreNo = x.StoreNo,
                        PosNo = x.PosNo,
                        BonusBuy = x.BonusBuy,
                        SerialNo = x.SerialNo,
                        OrderNo = x.OrderNo,
                        ArticleNo = x.ArticleNo,
                        ItemName = x.ItemName,
                        ApplyType = x.ApplyType,
                        Status = x.Status,
                        VoucherValue = x.VoucherValue,          
                        MaxQtyUse = x.MaxQtyUse,
                        MaxAmount = x.MaxAmount,
                        MaxQuantityIssue = x.MaxQuantityIssue,   
                        TranDateStr = x.TranDateStr,                 
                        IsOffline = x.IsOffline,                    // IsFileIssue
                        IsCheckItem = x.IsCheckItem,
                        VoucherType = x.VoucherType,
                        CreatedDateStr = x.CreatedDateStr           // Ngay hieu luc

                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Máy POS";
                    worksheet.Cells[1, 3].Value = "Bonus Buy";
                    worksheet.Cells[1, 4].Value = "Serial No";
                    worksheet.Cells[1, 5].Value = "Số đơn hàng";
                    worksheet.Cells[1, 6].Value = "Article No";
                    worksheet.Cells[1, 7].Value = "Tên chương trình";
                    worksheet.Cells[1, 8].Value = "Loại Voucher/Coupon áp dụng";
                    worksheet.Cells[1, 9].Value = "Tình trạng";
                    worksheet.Cells[1, 10].Value = "Giá trị";
                    worksheet.Cells[1, 11].Value = "Giá trị tối đa";
                    worksheet.Cells[1, 12].Value = "Số lượng sử dụng tối đa";
                    worksheet.Cells[1, 13].Value = "Số lượng phát hành tối đa";
                    worksheet.Cells[1, 14].Value = "Thời gian tạo";
                    worksheet.Cells[1, 15].Value = "IsFileIssue";       // Batch File
                    worksheet.Cells[1, 16].Value = "Áp dụng cho Item";
                    worksheet.Cells[1, 17].Value = "Loại";
                    worksheet.Cells[1, 18].Value = "Ngày bắt đầu có hiệu lực";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 18])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"VoucherPublished_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.Close();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [DisplayName("Danh mục Voucher/Coupon")]
        public ActionResult VoucherCouponList()
        {
            ViewBag.ListVoucherType = _commonBLO.LoadComboxVoucherType();
            return View();
        }

        public JsonResult GetCouponVoucherList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var pageIndex = skip / pageSize;
            var recordsTotal = 0;

            var voucherType = Request?.Form["VoucherType"];
            if (!string.IsNullOrEmpty(voucherType))
            {
                voucherType = voucherType.Trim();
            }

            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var data = _voucherBLO.GetCouponVoucherList(voucherType, status, textSearch, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<CouponVoucherModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcel_VoucherCouponList(string VoucherCouponType, string TextSearch, string Status)
        {
            var data = _voucherBLO.ExportExcel_VoucherCouponList(VoucherCouponType,TextSearch,Status);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    var listExport = data.Select(x => new
                    {
                        x.ItemNo,           // Ma voucher/coupon
                        x.ItemName,         // Ten voucher/coupon
                        x.CouponCode,       // Serial Number
                        x.ArticleType,      // Loại voucher/coupon
                        x.DiscountType,     // Loai giam gia
                        x.Blocked,          // Trang thai
                        x.CpnVchType,       // Loai the ap dung
                        x.SaleType,         // Hình thức bán hàng       
                        x.TotalBill,        // Ap dung tong bill
                        x.UnitOfMeasure,    // ĐVT
                        x.DiscountValue,    // Giá trị giảm
                        x.ValueOfVoucher,   // Giá trị voucher/coupon
                        x.StartingDateStr,  // Ngay bat dau co hieu luc
                        x.EndingDateStr,    // Ngay bat dau ket thuc hieu luc
                        x.LastDateModified  // Ngay cap nhat
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã Voucher/Coupon";
                    worksheet.Cells[1, 2].Value = "Tên Voucher/Coupon";                   
                    worksheet.Cells[1, 3].Value = "Serial Number";
                    worksheet.Cells[1, 4].Value = "Loại Voucher/Coupon";
                    worksheet.Cells[1, 5].Value = "Loại giảm giá";
                    worksheet.Cells[1, 6].Value = "Trạng thái";
                    worksheet.Cells[1, 7].Value = "Loại thẻ áp dụng";
                    worksheet.Cells[1, 8].Value = "Hình thức bán hàng";                    
                    worksheet.Cells[1, 9].Value = "Áp dụng tổng bill";
                    worksheet.Cells[1, 10].Value = "ĐVT";
                    worksheet.Cells[1, 11].Value = "Giá trị giảm";
                    worksheet.Cells[1, 12].Value = "Giá trị Voucher/Coupon";
                    worksheet.Cells[1, 13].Value = "Ngày bắt đầu có hiệu lực";
                    worksheet.Cells[1, 14].Value = "Ngày bắt đầu kết thúc hiệu lực";
                    worksheet.Cells[1, 15].Value = "Ngày cập nhật";
                   
                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 15])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"VoucherCouponList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.Close();
                        Response.End();
                    }
                }
            }

            return Json(string.Empty, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public ActionResult DetailCouponVoucherHeaderList(string itemNo)
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

            var data = _voucherBLO.DetailCouponVoucherHeaderList(itemNo);
            return Json(new DataTablesViewModel<DetailCouponVoucherHeaderResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public ActionResult DetailCouponVoucherLineList(string itemNo)
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            //var pageIndex = skip / pageSize;
            var recordsTotal = 0;

            var data = _voucherBLO.DetailCouponVoucherLineList(itemNo, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            return Json(new DataTablesViewModel<DetailCouponVoucherLineResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcel_DetailCouponVoucherLineList(string OfferNo)
        {
            var data = _voucherBLO.ExportToExcel_DetailCouponVoucherLineList(OfferNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.ItemNo,
                        x.LineNo,
                        x.Barcode,
                        x.ApplyItemNoSAP,
                        x.ApplyItemNoPLG,
                        x.Description,
                        x.UnitOfMeasure,
                        x.Size,
                        x.TaxCode
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "MÃ COUPON/VOUCHER";
                    worksheet.Cells[1, 2].Value = "LineNo";
                    worksheet.Cells[1, 3].Value = "BARCODE";
                    worksheet.Cells[1, 4].Value = "MÃ SẢN PHẨM SAP";
                    worksheet.Cells[1, 5].Value = "MÃ SẢN PHẨM PLG";
                    worksheet.Cells[1, 6].Value = "TÊN SẢN PHẨM";
                    worksheet.Cells[1, 7].Value = "ĐVT";
                    worksheet.Cells[1, 8].Value = "SIZE";
                    worksheet.Cells[1, 9].Value = "TAXCODE";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 9])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"CouponVoucherLineList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
                    using (var memoryStream = new MemoryStream())
                    {
                        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                        package.SaveAs(memoryStream);
                        memoryStream.WriteTo(Response.OutputStream);
                        Response.Flush();
                        Response.Close();
                        Response.End();
                    }
                }
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }



    }
}