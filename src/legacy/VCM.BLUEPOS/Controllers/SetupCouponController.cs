using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PLG.Controllers;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Business.SetupCoupon;
using VCM.BLUEPOS.Business.SetupItem;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model.SetupCoupon;
using VCM.BLUEPOS.Models;
//using VMC.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class SetupCouponController : BaseController
    {
        private ISetupCouponBLO _setupCouponBLO { get; set; }
        private ISetupItemBLO _setupItemBLO { get; set; }
        public SetupCouponController(ISetupCouponBLO setupCouponBLO, ISetupItemBLO setupItemBLO)
        {
            _setupCouponBLO = setupCouponBLO;
            _setupItemBLO = setupItemBLO;
        }

        [DisplayName("Cài đặt coupon")]
        public ActionResult SetupCoupon()
        {
            var model = new ViewSetupCouponModel { };
            return View(model);
        }

        [HttpPost]
        public ActionResult SetupCouponLoad()
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

                var issueType = Request?.Form["IssueType"];
                var itemNo = Request?.Form["ItemNo"];
                var mota = Request?.Form["Description"];
                var blocked = Request?.Form["Blocked"];

                var recordsTotal = 0;
                var result = _setupCouponBLO.LoadCouponRule(issueType, itemNo, mota, blocked, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<CpnVchBOMIssueRuleModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<CpnVchBOMIssueRuleModel>());
            }
        }


        [HttpPost]
        public ActionResult PopupAdvanceCoupon(string itemNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK
            };
            try
            {
                var model = _setupCouponBLO.ViewInfoICoupon(itemNo);

                var listIssueType = new List<IssueTypeCouponModel>();
                listIssueType.Add(new IssueTypeCouponModel { IssueType = "Auto", IssueName = "Tự động tạo mã" });
                listIssueType.Add(new IssueTypeCouponModel { IssueType = "Import", IssueName = "Import coupon bằng excel" });

                model.ListIssueType = listIssueType;

                var listTypeCoupon = new List<TypeCouponModel>();
                listTypeCoupon.Add(new TypeCouponModel { TypeCode = "CUS", TypeName = "Khách hàng" });
                listTypeCoupon.Add(new TypeCouponModel { TypeCode = "EMP", TypeName = "Nhân viên" });
                model.ListType = listTypeCoupon;

                var listDiscountType = new List<DiscountTypeModel>();
                listDiscountType.Add(new DiscountTypeModel { DiscountType = 1, DiscountName = "Discount Percent(%)" });
                listDiscountType.Add(new DiscountTypeModel { DiscountType = 2, DiscountName = "Discount Amount(VNĐ)" });
                model.ListDiscountType = listDiscountType;

                result = new ResultResponse
                {
                    Status = HttpStatusCode.OK,
                    Message = $"Success",
                    ViewPartial = Ultils.RenderViewToString(ControllerContext, "~/Views/SetupCoupon/PopupAdvanceCoupon.cshtml", model),
                    Data = model
                };
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = $"Lỗi {ex.Message}"
                };
            }
            return Json(result);
        }

        [HttpGet]
        public ActionResult _SetupCoupon(string Id)
        {

            var model = new ViewSetupCouponModel();

            try
            {
                var listIssueType = new List<IssueTypeCouponModel>();
                listIssueType.Add(new IssueTypeCouponModel { IssueType = "Import", IssueName = "Import coupon bằng excel" });
                listIssueType.Add(new IssueTypeCouponModel { IssueType = "Auto", IssueName = "Tự động tạo mã" });

                model = _setupCouponBLO.ViewInfoICoupon(Id);
                model.ListIssueType = listIssueType;
            }
            catch (Exception ex)
            {

            }

            return PartialView(model);
        }


        [HttpPost]
        public JsonResult SaveIssueCoupon(SaveIssueCouponRequest model, HttpPostedFileBase ImportFile)
        {

            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                var isCheckExistCode = false;

                model.StartingDate = string.IsNullOrEmpty(model.StartingDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.StartingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.EndingDate = string.IsNullOrEmpty(model.EndingDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.EndingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.ArticleType = "ZCPN";

                if (string.IsNullOrEmpty(model.JsonItem))
                {
                    model.JsonItem = "";
                }

                var dateNow = DateTime.Now;
                //var checkDateStartNow = model.StartingDate.Date < dateNow.Date;
                //if (checkDateStartNow)
                //{
                //    result = new ResultResponse
                //    {
                //        Status = HttpStatusCode.BadRequest,
                //        Message = $"TỪ NGÀY không được nhỏ hơn ngày hiện tại"
                //    };
                //    return Json(result);
                //}

                var checkDate = model.StartingDate.Date > model.EndingDate.Date;
                if (checkDate)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"TỪ NGÀY không lớn hơn ĐẾN NGÀY"
                    };
                    return Json(result);
                }

                if (model.IssueType == "Auto")
                {

                    if (string.IsNullOrEmpty(model.ItemNo) || model.QuantityCodeInDB == 0)
                    {
                        isCheckExistCode = true;
                        Random random = new Random();
                        var listRandomCode = new List<string>();
                        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        string numbers = "0123456789";

                        // var lengthCode = model.LenCode;
                        var tienTo = "";

                        var listPrefixExists = new List<CpnVchBOMIssueRuleModel>();
                        if (!string.IsNullOrEmpty(model.Prefix))
                        {
                            // lengthCode = model.LenCode - model.Prefix.Trim().Length;
                            tienTo = model.Prefix.Trim().ToUpper();
                            //listPrefixExists = _setupCouponBLO.GetIssueRuleByPrefix(model.Prefix);
                        }
                        //if (listPrefixExists != null && listPrefixExists.Count > 0 && retry == false)
                        //{
                        //    //Thông báo đã tồn tại Prefix trong hệ thống
                        //    result = new ResultResponse
                        //    {
                        //        Status = HttpStatusCode.Conflict,
                        //        Message = $"Đã tồn tại TIỀN TỐ {model.Prefix.ToUpper()} trong hệ thống, Nếu sử dụng TIỀN TỐ này hệ thống có thể sẽ tạo không đúng với số lượng coupon"
                        //    };
                        //    return Json(result);
                        //}
                        var checkLenghtCode = model.LenCode + tienTo.Length + model.CharOfNumber;
                        if (checkLenghtCode > 20)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = "Tổng ký tự coupon đã vượt hơn 20",
                                Data = null
                            };
                            return Json(result);
                        }
                        var listCodeChar = new List<string>();
                        var listCodeNum = new List<string>();

                        for (var i = 0; i < model.Quantity; i++)
                        {
                            var codeChar = "";
                            if (model.CharOfNumber > 0)
                            {
                                codeChar = new string(Enumerable.Repeat(alphabet, model.CharOfNumber)
                                .Select(s => s[random.Next(s.Length)]).ToArray());

                                listCodeChar.Add(codeChar);
                            }

                            var lenNumber = model.LenCode;// - tienTo.Length;

                            var timeUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                            //var codeNumberFirst = new string(Enumerable.Repeat(numbers, lenNumber)
                            //  .Select(s => s[random.Next(s.Length)]).ToArray());

                            var lengthCodeUnix = lenNumber;//15
                            var codeBlance = "";

                            if (timeUnix.Length < lenNumber)//timeUnix.Length=13
                            {
                                lengthCodeUnix = timeUnix.Length;

                                codeBlance = new string(Enumerable.Repeat(numbers, lenNumber - timeUnix.Length)
                                  .Select(s => s[random.Next(s.Length)]).ToArray());
                            }

                            var codeNumberFirst = codeBlance + timeUnix.Substring(timeUnix.Length - lengthCodeUnix, lengthCodeUnix);

                            var code = codeNumberFirst;
                            if (model.CharOfNumber > 0)
                            {

                                //var removePosCode = codeNumberFirst.Remove(model.CharPosition - 1, model.CharOfNumber);
                                //code = removePosCode.Insert(model.CharPosition - 1, codeChar);

                                code = codeNumberFirst.Insert(model.CharPosition, codeChar);
                            }
                            var codeValue = $"{tienTo}{code}";
                            listRandomCode.Add(codeValue);

                            Thread.Sleep(1);
                        }

                        var checkCodeDuplicate = listRandomCode.GroupBy(x => x).Where(g => g.Count() > 1).ToList();
                        model.ListCode = listRandomCode.Select(a => new RandomCode { Code = a }).ToList();

                        if (checkCodeDuplicate.Count > 0)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = "Hiện tại hệ thống generate coupon trùng nhau, vui lòng chờ trong ít phút để tạo lại",
                                Data = null
                            };
                            return Json(result);
                        }
                    }
                }
                else // Import
                {
                    if (string.IsNullOrEmpty(model.ItemNo) || model.QuantityCodeInDB == 0)
                    {
                        isCheckExistCode = true;
                        if (ImportFile == null) return Json(new { Status = 0, Message = "No File Selected" });
                        string fileExtension = Path.GetExtension(ImportFile.FileName).ToLower();
                        if (fileExtension != ".xls" && fileExtension != ".xlsx")
                        {
                            return Json(new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = "Không đúng định dạng file excel",
                                Data = null
                            });
                        }
                        Workbook workbook = new Workbook();
                        workbook.LoadFromStream(ImportFile.InputStream);
                        Worksheet sheet = workbook.Worksheets[0];
                        var data = sheet.ExportDataTable();

                        var listCoupon = ConvertData.ConvertDataTable<ExcelExampleCouponModel>(data);
                        if (listCoupon.Count == 0)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = $"Vui lòng kiểm tra file Excel, không có mã coupon",
                                Data = null
                            };
                            return Json(result);
                        }
                        var checkCode = listCoupon.Any(a => string.IsNullOrEmpty(a.CodeCoupon));
                        if (checkCode)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = $"Vui lòng kiểm tra cột CodeCoupon, có giá trị trống",
                                Data = null
                            };
                            return Json(result);
                        }
                        Regex regexMST = new Regex(@"^[0-9-_A-Za-z]*$");
                        var checkRegex = listCoupon.Where(a => !regexMST.Match(a.CodeCoupon).Success).ToList();
                        if (checkRegex != null && checkRegex.Count() > 0)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = $"Có {checkRegex.Count} mã coupon trong file excel có ký tự đặc biệt ({string.Join(",", checkRegex.Select(a => a.CodeCoupon))})",
                                Data = null
                            };
                            return Json(result);
                        }
                        var checkLenght = listCoupon.Where(a => a.CodeCoupon.Length > 20).ToList();
                        if (checkLenght != null && checkLenght.Count() > 0)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.NotFound,
                                Message = $"Có {checkLenght.Count} mã coupon trong file excel vượt quá 20 ký tự ({string.Join(",", checkLenght.Select(a => a.CodeCoupon))})",
                                Data = null
                            };
                            return Json(result);
                        }

                        var checkCodeDuplicate = listCoupon.GroupBy(x => x.CodeCoupon).Where(g => g.Count() > 1).ToList();
                        model.ListCode = listCoupon.Select(a => new RandomCode { Code = a.CodeCoupon }).ToList();

                        if (checkCodeDuplicate.Count > 0)
                        {
                            result = new ResultResponse
                            {
                                Status = HttpStatusCode.BadRequest,
                                Message = $"File excel có giá trị trùng ({string.Join(",", checkCodeDuplicate)}), Vui lòng kiểm tra lại",
                                Data = null
                            };
                            return Json(result);
                        }
                    }
                }

                if (isCheckExistCode)
                {
                    var checkExistDB = _setupCouponBLO.CheckExistsCodeVoucher(model.ListCode).Distinct().ToList();
                    if (checkExistDB.Count > 0)
                    {
                        var message = model.IssueType == "Auto" ? $"Mã coupon trùng trong DB ({string.Join(",", checkExistDB)}), vui lòng chờ trong ít phút để tạo lại" : $"Mã coupon trùng trong DB ({string.Join(",", checkExistDB)}), vui lòng kiểm tra lại file Excel";
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = message,
                            Data = null
                        };
                        return Json(result);
                    }
                }

                var listItem = JsonConvert.DeserializeObject<List<CpnItemModel>>(model.JsonItem);
                if (model.IsCheckItem == true)
                {
                    if (listItem == null || listItem.Count == 0)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng thêm sản phẩm vào voucher/coupon"
                        };
                        return Json(result);
                    }
                }
                if (listItem != null && listItem.Count > 0 && model.IsCheckItem == false)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Voucher/Coupon đang áp dụng tổng hóa đơn, vui lòng xóa danh sách sản phẩm"
                    };
                    return Json(result);
                }
                var save = _setupCouponBLO.SaveCoupon(model);
                if (save.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = save.Item2,
                        Data = save.Item3
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = save.Item2,
                        Data = save.Item3
                    };
                }
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

        [HttpPost]
        public JsonResult SaveAdvancedCoupon(SetupAdvancedCouponRequest model)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {

                model.StartingDate = string.IsNullOrEmpty(model.StartingDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.StartingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.EndingDate = string.IsNullOrEmpty(model.EndingDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.EndingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.ArticleType = "ZCPN";
                var dateNow = DateTime.Now;
                var checkDateStartNow = model.StartingDate.Date < dateNow.Date;
                if (checkDateStartNow)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"TỪ NGÀY không được nhỏ hơn ngày hiện tại"
                    };
                    return Json(result);
                }

                var checkDate = model.StartingDate.Date > model.EndingDate.Date;
                if (checkDate)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"TỪ NGÀY không lớn hơn ĐẾN NGÀY"
                    };
                    return Json(result);
                }


                var excute = _setupCouponBLO.SaveAdvancedCoupon(model);
                if (excute.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = excute.Item2,
                        Data = excute.Item3
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = excute.Item2
                    };
                }

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

        [HttpPost]
        public ActionResult CouponCodeLoad()
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

                var itemNo = Request?.Form["ItemNo"];

                var recordsTotal = 0;
                var result = _setupCouponBLO.CouponCodeLoad(itemNo, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<LoadCouponCodeModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<LoadCouponCodeModel>());
            }
        }

        public ActionResult ExcelExampleCoupon()
        {

            var data = _setupCouponBLO.ExcelExampleCoupon();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("DataImport");

                    var countR = data.Count;

                    //add header
                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 1])
                    {
                        r.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        r.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        // r.AutoFilter = true;
                    }
                    //add value
                    using (ExcelRange r = worksheet.Cells[1, 1, 1 + countR, 1])
                    {
                        r.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                        r.Style.Border.Top.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Border.Left.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Border.Right.Color.SetColor(System.Drawing.Color.Black);
                        // r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = false;


                    }

                    worksheet.Cells["A1"].LoadFromCollection(data, true);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"FileMauCoupon_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public ActionResult DeleteItemCoupon(string ItemNo)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.OK,//OK
                Message = "",
                Data = null
            };
            try
            {
                var response = _setupCouponBLO.DeleteItemCoupon(ItemNo);
                if (response.Item1)
                {
                    result.Status = HttpStatusCode.OK;
                    result.Message = response.Item2;
                }
                else
                {
                    result.Status = HttpStatusCode.BadRequest;
                    result.Message = response.Item2;
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                result = new ResultResponse
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = $"Lỗi {ex.Message}",
                    Data = null
                };
                return Json(result);
            }
        }


        public ActionResult _ViewListItemCoupon()
        {
            var recordsTotal = 0;
            var model = _setupItemBLO.ListItemSetup("", "", "", out recordsTotal, 1, 1000000);
            return PartialView(model);
        }

        [HttpPost]
        public JsonResult CheckListItemExists(string jsonItem, string jsonItemArea)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                var listItem = JsonConvert.DeserializeObject<List<string>>(jsonItem);
                var listItemArea = JsonConvert.DeserializeObject<List<string>>(jsonItemArea);
                var listNotExists = listItemArea.Except(listItem).Where(d => !string.IsNullOrEmpty(d)).ToList();
                if (listNotExists.Count > 0)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Không tồn tại sản phẩm này trong hệ thống ({string.Join(",", listNotExists)})"
                    };
                    return Json(result);
                }

                var checkItem = _setupCouponBLO.CheckListItemExists(listItem);
                if (!checkItem.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = checkItem.Item2
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = checkItem.Item2,
                        Data = checkItem.Item3
                    };
                }
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

        [DisplayName("Tạo mã coupon/voucher")]
        public ActionResult CreatedCpnVch()
        {
            return View();
        }

        [HttpGet]
        public ActionResult _SetupCouponVoucher(string Id)
        {

            var model = new ViewSetupCpnVchModel();

            try
            {
                //var listIssueType = new List<IssueTypeCouponModel>();
                //listIssueType.Add(new IssueTypeCouponModel { IssueType = "Import", IssueName = "Import coupon bằng excel" });
                //listIssueType.Add(new IssueTypeCouponModel { IssueType = "Auto", IssueName = "Tự động tạo mã" });
                model = _setupCouponBLO.ViewInfoICpnVch(Id);

                var listTypeCoupon = new List<TypeCouponModel>();
                listTypeCoupon.Add(new TypeCouponModel { TypeCode = "CUS", TypeName = "Khách hàng" });
                listTypeCoupon.Add(new TypeCouponModel { TypeCode = "EMP", TypeName = "Nhân viên" });
                model.ListType = listTypeCoupon;

                var listDiscountType = new List<DiscountTypeModel>();
                listDiscountType.Add(new DiscountTypeModel { DiscountType = 1, DiscountName = "Discount Percent(%)" });
                listDiscountType.Add(new DiscountTypeModel { DiscountType = 2, DiscountName = "Discount Amount(VNĐ)" });
                model.ListDiscountType = listDiscountType;

                var listArticleType = new List<ArticleTypeModel>();
                listArticleType.Add(new ArticleTypeModel { ArticleNo = "ZVCN", ArticleName = "Voucher" });
                listArticleType.Add(new ArticleTypeModel { ArticleNo = "ZCPN", ArticleName = "Coupon" });
                model.ListArticleType = listArticleType;


                // model.ListIssueType = listIssueType;
            }
            catch (Exception ex)
            {

            }

            return PartialView(model);
        }

        [HttpPost]
        public ActionResult SetupCpnVchLoad()
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

                var itemNo = Request?.Form["ItemNo"];
                var mota = Request?.Form["Description"];
                var blocked = Request?.Form["Blocked"];

                var recordsTotal = 0;
                var result = _setupCouponBLO.LoadCpnVch(itemNo, mota, blocked, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<CpnVchBOMHeaderModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<CpnVchBOMHeaderModel>());
            }
        }

        [HttpPost]
        public JsonResult SaveCpnVch(CpnVchBOMHeaderRequestModel model)
        {
            var result = new ResultResponse
            {
                Status = HttpStatusCode.BadRequest
            };
            try
            {
                if (string.IsNullOrEmpty(model.CouponCode))
                {
                    model.CouponCode = "";
                }
                if (string.IsNullOrEmpty(model.JsonItem))
                {
                    model.JsonItem = "";
                }
                model.StartingDate = string.IsNullOrEmpty(model.StartingDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.StartingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                model.EndingDate = string.IsNullOrEmpty(model.EndingDateStr) ? Convert.ToDateTime("1900-01-01") : DateTime.ParseExact(model.EndingDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var dateNow = DateTime.Now;
                var checkDate = model.StartingDate.Date > model.EndingDate.Date;
                if (checkDate)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"TỪ NGÀY không lớn hơn ĐẾN NGÀY"
                    };
                    return Json(result);
                }
                var listItem = JsonConvert.DeserializeObject<List<CpnItemModel>>(model.JsonItem);
                if (model.IsCheckItem == true)
                {
                    if (listItem == null || listItem.Count == 0)
                    {
                        result = new ResultResponse
                        {
                            Status = HttpStatusCode.BadRequest,
                            Message = $"Vui lòng thêm sản phẩm vào voucher/coupon"
                        };
                        return Json(result);
                    }
                }
                if (listItem != null && listItem.Count > 0 && model.IsCheckItem == false)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = $"Voucher/Coupon đang áp dụng tổng hóa đơn, vui lòng xóa danh sách sản phẩm"
                    };
                    return Json(result);
                }
               
                if (model.LimitQtyUsed > 1)
                {
                    model.IsMultiUse = true;
                }

                var save = _setupCouponBLO.SaveCpnVch(model);
                if (save.Item1)
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.OK,
                        Message = save.Item2,
                        Data = save.Item3
                    };
                }
                else
                {
                    result = new ResultResponse
                    {
                        Status = HttpStatusCode.BadRequest,
                        Message = save.Item2,
                        Data = save.Item3
                    };
                }
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

        public ActionResult _ViewListItemByArticleType(string articleType)
        {
            var model = new ViewItemByArticleTypeRequest
            {
                articleType = articleType
            };
            return PartialView(model);
        }
        [HttpPost]
        public ActionResult ListItemCpnVch()
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

                var articleType = Request?.Form["ArticleType"];
                var itemNo = Request?.Form["No"];
                var itemName = Request?.Form["Description"];
                var recordsTotal = 0;
                var result = _setupCouponBLO.LoadItemByArticleType(articleType, itemNo, itemName, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<VCM.BLUEPOS.Model.SetupItem.ItemModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch
            {
                return View(new List<VCM.BLUEPOS.Model.SetupItem.ItemModel>());
            }
        }
    }
}