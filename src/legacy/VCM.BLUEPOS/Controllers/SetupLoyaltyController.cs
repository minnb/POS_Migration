using Newtonsoft.Json;
using PLG.Controllers;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Data;
using System.Web.Hosting;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Text;
using VCM.BLUEPOS.Business.Loyalty;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Loyalty;
using VCM.BLUEPOS.Models;
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Helpers;



namespace PLG.Controllers
{
    public class SetupLoyaltyController : BaseController
    {
        private ILoyaltyBLO _setupLoyaltyBLO { get; set; }
        private ICommonBLO _commonBLO;
        public SetupLoyaltyController(ILoyaltyBLO setupLoyaltyBLO, ICommonBLO commonBLO)
        {
            _setupLoyaltyBLO = setupLoyaltyBLO;
            _commonBLO = commonBLO;
        }

        [DisplayName("Khai báo tỷ lệ đổi quà")]
        public ActionResult SetupLoyalty()
        {
            ViewBag.ListMemberCardType = _commonBLO.GetComboxMemberCardType();
            return View();
        }

        public JsonResult LoadSetupLoyaltyList()
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

            var fromDate = Request?.Form["SearchFromDate"];
            var toDate = Request?.Form["SearchToDate"];           
            var status = Request?.Form["Status"];

            var itemNo = Request?.Form["ItemNo"];
            if (!string.IsNullOrEmpty(fromDate))
            {
                fromDate = fromDate.Trim();
            }
            if (!string.IsNullOrEmpty(toDate))
            {
                toDate = toDate.Trim();
            }
            
            if (!string.IsNullOrEmpty(itemNo))
            {
                itemNo = itemNo.Trim();
            }

            var _fromDate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var _toDate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var data = _setupLoyaltyBLO.GetSetupLoyaltyList(_fromDate, _toDate, itemNo, status, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            return Json(new DataTablesViewModel<GiftRedeemResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public ActionResult _ProductList(string fromDate, string toDate, string pointRedeem, string clubCode)
        {               
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.ClubCode = clubCode;
            ViewBag.PointRedeem = pointRedeem;
            return PartialView();
        }

        [HttpPost]
        public JsonResult LoadProductList()
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

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }            
            var data = _setupLoyaltyBLO.GetProductList(textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<SetupLoyaltyResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult GetItemGiftRedeemList()
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

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var status = ""; // Tat ca
            var data = _setupLoyaltyBLO.GetItemGiftRedeemList(textSearch, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<ListGiftRedeemModalModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult CreateSetupLoyalty(CreateSetupLoyaltyModel req)
        {
            var _listItem = JsonConvert.DeserializeObject<List<ProductListForLoyaltyResponseModel>>(req.ItemNoStr);
            var model = new CreateSetupLoyaltyModel
            {
                Point = string.IsNullOrEmpty(req.PointStr) ? 0 : Int32.Parse(req.PointStr),
                FromDate = DateTime.ParseExact(req.FromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                ToDate = DateTime.ParseExact(req.ToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                Status = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = LoginUser.UserName,
                ListItem = _listItem,
                ClubCode = req.ClubCode // hoi vien
            };

            var data = _setupLoyaltyBLO.CreateSetupLoyalty(model);
            return Json(data);

        }

        [HttpPost]
        public ActionResult CreateSetupLoyaltyForTabGiftRedeem(CreateSetupLoyaltyTabGiftRedeemModel req)
        {
            var _listItem = JsonConvert.DeserializeObject<List<ItemGiftRedeemForTabGiftRedeemModel>>(req.ListItemStr);
            var model = new CreateSetupLoyaltyTabGiftRedeemModel
            {
                Point = string.IsNullOrEmpty(req.PointStr) ? 0 : Int32.Parse(req.PointStr),
                FromDateStr = req.FromDateStr,
                ToDateStr = req.ToDateStr,
                Status = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = LoginUser.UserName,
                ListItem = _listItem,
                ClubCode = req.ClubCode
            };

            var data = _setupLoyaltyBLO.CreateSetupLoyaltyForTabGiftRedeem(model);
            return Json(data);
        }

        [HttpPost]
        public ActionResult UpdateSetupLoyalty(UpdateSetupLoyaltyModel req)
        {
            var model = new UpdateSetupLoyaltyModel
            {
                ID = req.ID,
                ItemNo = req.ItemNo,
                ItemName = req.ItemName,
                Point = string.IsNullOrEmpty(req.PointStr) ? 0 : Int32.Parse(req.PointStr),
                FromDate = DateTime.ParseExact(req.FromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                ToDate = DateTime.ParseExact(req.ToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                Status = Convert.ToInt16(req.Status),
                CreatedDate = DateTime.Now,
                CreatedUser = LoginUser.UserName,
                UpdatedDate = DateTime.Now,
                UpdatedUser = LoginUser.UserName,
                Pkey = req.Pkey
                //ClubCode = req.ClubCode
            };

            var data = _setupLoyaltyBLO.UpdateSetupLoyalty(model);
            return Json(data);

        }

        [HttpPost]
        public JsonResult DeleteSetupLoyalty(DeleteLoyaltyModel req)
        {
            var data = _setupLoyaltyBLO.DeleteSetupLoyalty(req);
            return Json(data);
        }

        [HttpPost]
        public ActionResult ImportExcelGiftRedeem(HttpPostedFileBase ImportFile)
        {
            if (ImportFile == null) return Json(new
            {
                Status = 0,
                Message = "Không có file nào được chọn"
            });

            string fileExtension = Path.GetExtension(ImportFile.FileName).ToLower();

            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                return Json(new
                {
                    Status = 0,
                    Message = "Định dạng file excel không đúng."
                });
            }

            try
            {
                var createUserName = base.LoginUser.UserName;
                var streamData = ImportFile.InputStream;

                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);
                    Worksheet sheet = workbook.Worksheets[0];
                    var data = sheet.ExportDataTable();
                    if (data.Rows.Count == 0 || data == null)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = "File excel không có dữ liệu. Vui lòng kiểm tra lại."
                        });
                    }

                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        if (data.Rows[i]["ItemNo"].ToString() == null || data.Rows[i]["ItemNo"].ToString() == "")
                        {
                            return Json(new
                            {
                                Status = 0,
                                Message = $"Dữ liệu cột [ItemNo] đang bị rỗng, Vui lòng kiểm tra lại"
                            });
                        }

                        if (data.Rows[i]["PointRedeem"].ToString() == null || data.Rows[i]["PointRedeem"].ToString() == "")
                        {
                            return Json(new
                            {
                                Status = 0,
                                Message = $"Dữ liệu cột [PointRedeem] đang bị rỗng, Vui lòng kiểm tra lại"
                            });
                        }

                        if (data.Rows[i]["FromDate"].ToString() == null || data.Rows[i]["FromDate"].ToString() == "")
                        {
                            return Json(new
                            {
                                Status = 0,
                                Message = $"Dữ liệu cột [FromDate] đang bị rỗng, Vui lòng kiểm tra lại"
                            });
                        }

                        if (data.Rows[i]["ToDate"].ToString() == null || data.Rows[i]["ToDate"].ToString() == "")
                        {
                            return Json(new
                            {
                                Status = 0,
                                Message = $"Dữ liệu cột [ToDate] đang bị rỗng, Vui lòng kiểm tra lại"
                            });
                        }

                        var exFromDate = DateTime.ParseExact(data.Rows[i]["FromDate"].ToString().Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        var exToDate = DateTime.ParseExact(data.Rows[i]["ToDate"].ToString().Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }

                    var insert = _setupLoyaltyBLO.ImportExcelGiftRedeem(createUserName, data);

                    if (insert.Item1 == 0 || insert.Item1 < 0)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"{insert.Item2}"
                        });
                    }

                    return Json(new
                    {
                        Status = 1,
                        Message = $"Import file thành công {insert.Item1} dòng"
                    });
                }
                catch (Exception e)
                {
                    return Json(new
                    {
                        Status = 0,
                        Message = e.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Status = 0,
                    Message = ex.Message
                });
            }
        }

        //[HttpPost]
        //public ActionResult ImportExcelGiftRedeem_V2(HttpPostedFileBase ImportFile)
        //{
        //    if (ImportFile == null) return Json(new {
        //        Status = 0,
        //        Message = "Không có file nào được chọn"
        //    });

        //    string fileExtension = Path.GetExtension(ImportFile.FileName).ToLower();

        //    if (fileExtension != ".xls" && fileExtension != ".xlsx")
        //    {
        //        return Json(new {
        //            Status = 0,
        //            Message = "Định dạng file excel không đúng"
        //        });
        //    }

        //    try
        //    {
        //        var createUserName = base.LoginUser.UserName;
        //        string filename = ImportFile.FileName;
        //        string targetpath = Server.MapPath("~/Uploads/");
        //        string pathToExcelFile = targetpath + filename;

        //        if (System.IO.File.Exists(pathToExcelFile))
        //        {
        //            System.IO.File.Delete(pathToExcelFile);
        //        }

        //        var t = ImportFile.InputStream;
        //        ImportFile.SaveAs(targetpath + filename);

        //        try
        //        {
        //            Workbook workbook = new Workbook();
        //            workbook.LoadFromFile(pathToExcelFile);
        //            Worksheet sheet = workbook.Worksheets[0];
        //            var data = sheet.ExportDataTable();

        //            if (System.IO.File.Exists(pathToExcelFile))
        //            {
        //                System.IO.File.Delete(pathToExcelFile);
        //            }

        //            if (data.Rows.Count == 0 || data == null)
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = "File excel không có dữ liệu. Vui lòng kiểm tra lại."
        //                });
        //            }

        //            // Convert data

        //            var listDataImport = new List<ImportExcelGiftRedeemModel>();

        //            for (int i = 0; i < data.Rows.Count; i++)
        //            {
        //                var excel_ItemNo = data.Rows[i]["ItemNo"] == null ? string.Empty : data.Rows[i]["ItemNo"].ToString().TrimEnd();
        //                var excel_PointRedeem = data.Rows[i]["PointRedeem"] == null ? string.Empty : data.Rows[i]["PointRedeem"].ToString().TrimEnd();
        //                var excel_FromDate = data.Rows[i]["FromDate"] == null ? string.Empty : data.Rows[i]["FromDate"].ToString().TrimEnd();
        //                var excel_ToDate = data.Rows[i]["ToDate"] == null ? string.Empty : data.Rows[i]["ToDate"].ToString().TrimEnd();
        //                var excel_ClubCode = data.Rows[i]["ClubCode"] == null ? string.Empty : data.Rows[i]["ClubCode"].ToString().TrimEnd().ToUpper();

        //                var FromDate = DateTime.ParseExact(excel_FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //                var ToDate = DateTime.ParseExact(excel_ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

        //                var temp = new ImportExcelGiftRedeemModel
        //                {
        //                    ItemNo = excel_ItemNo,
        //                    PointRedeemStr = excel_PointRedeem,
        //                    FromDate = FromDate,
        //                    ToDate = ToDate,
        //                    FromDateStr = FromDate.ToString("yyyyMMdd"),
        //                    ToDateStr = ToDate.ToString("yyyyMMdd"),
        //                    ClubCode = excel_ClubCode
        //                };

        //                listDataImport.Add(temp);
        //            }

        //            // validate

        //            if (listDataImport.Count == 0)
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Không có dữ liệu import. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            if (listDataImport.Any(x => string.IsNullOrEmpty(x.ItemNo)))
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Dữ liệu cột [ItemNo] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            if (listDataImport.Any(x => string.IsNullOrEmpty(x.PointRedeemStr)))
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Dữ liệu cột [PointRedeem] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            if (listDataImport.Any(x => string.IsNullOrEmpty(x.FromDateStr)))
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Dữ liệu cột [FromDate] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            if (listDataImport.Any(x => string.IsNullOrEmpty(x.ToDateStr)))
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Dữ liệu cột [ToDate] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            if (listDataImport.Any(x => string.IsNullOrEmpty(x.ClubCode)))
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Dữ liệu cột [ClubCode] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            // Validate Fromdate / ToDate

        //            var listFromDate = listDataImport.Select(y => y.FromDateStr).ToList();
        //            var listToDate = listDataImport.Select(y => y.ToDateStr).ToList();
        //            var listCheckDate = listDataImport.Where(x => x.FromDate > x.ToDate).ToList();
        //            if (listCheckDate.Count > 0)
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Hiệu lực từ ngày phải nhỏ hơn hoặc bằng hiệu lực đến ngày. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            var listCheckFromDate_DateNow = listDataImport.Where(x => x.FromDate < DateTime.Now.Date).ToList();
        //            if (listCheckFromDate_DateNow.Count > 0)
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Hiệu lực từ ngày phải lớn hơn hoặc bằng ngày hiện tại. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            var listCheckToDate_DateNow = listDataImport.Where(x => x.ToDate < DateTime.Now.Date).ToList();
        //            if (listCheckToDate_DateNow.Count > 0)
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Hiệu lực đến ngày phải lớn hơn hoặc bằng ngày hiện tại. Vui lòng kiểm tra lại file import"
        //                });
        //            }

        //            // Validate ClubCode : hoi vien

        //            var listClubCode = listDataImport.Where(x => x.ClubCode.ToUpper() != "WIN" && x.ClubCode.ToUpper() != "PLH").Select(y => y.ClubCode.ToString()).ToList();
        //            if (listClubCode.Count > 0)
        //            {
        //                return Json(new
        //                {
        //                    Status = 0,
        //                    Message = $"Mã hội viên {listClubCode.ToString()} này không phải là WIN/PLH. Vui lòng kiểm tra lại file import"
        //                });
        //            }
                   
        //            //var dataImport = HelpersExtension.ListtoDataTableConverter.ConvertToDataTable(listImport);

        //            var insert = _setupLoyaltyBLO.ImportExcelGiftRedeem_V2(createUserName, listDataImport);
                    
        //            if (insert.Item1 == 0 || insert.Item1 < 0)
        //            {
        //                return Json(new {
        //                    Status = 0,
        //                    Message = $"{insert.Item2}"
        //                });
        //            }
                    
        //            return Json(new {
        //                Status = 1,
        //                Message = $"Import file thành công {insert.Item1} dòng"
        //            });

        //        }
        //        catch (Exception e)
        //        {
        //            return Json(new {
        //                Status = 0,
        //                Message = e.Message
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new {
        //            Status = 0,
        //            Message = ex.Message
        //        });
        //    }
        //}

        public ActionResult ExcelExampleGiftRedeem()
        {
            var sDocument = Server.MapPath("~/Files/") + "ImportGiftRedeem.xlsx";
            byte[] fileBytes = System.IO.File.ReadAllBytes(sDocument);
            string fileName = "ImportGiftRedeem.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public ActionResult ExportExcelGiftRedeemList(DateTime FromDate, DateTime ToDate, string ItemNo, string Status)
        {          
            var data = _setupLoyaltyBLO.ExportExcelGiftRedeemList(FromDate, ToDate, ItemNo, Status);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.ItemNo,
                        x.ItemName,
                        x.PointRedeem,
                        x.StatusStr,
                        x.FromDateStr,
                        x.ToDateStr,
                        x.CreatedDate,
                        x.CreatedUser,
                        x.UpdatedDate,
                        x.UpdatedUser
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 2].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 3].Value = "Điểm quy đổi";
                    worksheet.Cells[1, 4].Value = "Trạng thái";
                    worksheet.Cells[1, 5].Value = "Hiệu lực từ ngày";
                    worksheet.Cells[1, 6].Value = "Hiệu lực đến ngày";
                    worksheet.Cells[1, 7].Value = "Ngày tạo";
                    worksheet.Cells[1, 8].Value = "Người tạo";
                    worksheet.Cells[1, 9].Value = "Ngày cập nhật";
                    worksheet.Cells[1, 10].Value = "Người cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"GiftRedeemList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        //--- 22/05/2024 ---
        
        [DisplayName("Khai báo tỷ lệ tích tem đổi quà")]
        public ActionResult SetupMemberEarnItem()
        {
            return View();
        }
        public JsonResult SetupMemberEarnItemList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 0;
            var skip = start != null ? Convert.ToInt32(start) : 0;
            var pageNumber = skip / pageSize;
            var recordsTotal = 0;

            var status = Request?.Form["Status"];
            var textSearch = Request?.Form["TextSearch"];

            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var data = _setupLoyaltyBLO.SetupMemberEarnItemList(textSearch, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<MemberEarnItemResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcel_MemberEarnItemList( string TextSearch, string Status)
        {
            var data = _setupLoyaltyBLO.ExportExcel_MemberEarnItemList(TextSearch, Status);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.ItemNo,
                        x.ItemName,
                        x.Uom,
                        x.Size,
                        x.SaleQty,
                        x.StampsQty,
                        x.Blocked,
                        x.FromDateStr,
                        x.ToDateStr,
                        x.CreateDateStr
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 2].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 3].Value = "ĐVT";
                    worksheet.Cells[1, 4].Value = "Size";
                    worksheet.Cells[1, 5].Value = "Số lượng";
                    worksheet.Cells[1, 6].Value = "Tỷ lệ tích tem";
                    worksheet.Cells[1, 7].Value = "Trạng thái";
                    worksheet.Cells[1, 8].Value = "Từ ngày";
                    worksheet.Cells[1, 9].Value = "Đến ngày";
                    worksheet.Cells[1, 10].Value = "Ngày tạo";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"MemberEarnItemList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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
        public ActionResult UpdateSetupMemberEarnItem(UpdateSetupMemberEarnItemModel req)
        {
            var model = new UpdateSetupMemberEarnItemModel
            {
                ItemNo = req.ItemNo,
                Uom = req.Uom,
                SaleQty = req.SaleQty,
                StampsQty = req.StampsQty,
                FromDateStr = req.FromDateStr,
                ToDateStr = req.ToDateStr,
                Blocked = req.Blocked,
                CrtUser = base.LoginUser.UserName,
                UpdDate = DateTime.Now

                //FromDate = DateTime.ParseExact(req.FromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                //ToDate = DateTime.ParseExact(req.ToDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                //Status = Convert.ToInt16(req.Status)
            };

            var data = _setupLoyaltyBLO.UpdateSetupMemberEarnItem(model);
            return Json(data);

        }

        [HttpPost]
        public JsonResult DeleteSetupMemberEarnItem(DeleteSetupMemberEarnItemModel req)
        {
            var data = _setupLoyaltyBLO.DeleteSetupMemberEarnItem(req);
            return Json(data);
        }

        public ActionResult DownloadTemplate_MemberEarnItem()
        {
            var sDocument = Server.MapPath("~/Files/") + "ImportMemberEarnItemTemplate.xlsx";
            byte[] fileBytes = System.IO.File.ReadAllBytes(sDocument);
            string fileName = "ImportMemberEarnItemTemplate.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost]
        public ActionResult ImportExcelMemberEarnItem(HttpPostedFileBase ImportFile)
        {
            if (ImportFile == null) return Json(new
            {
                Status = 0,
                Message = "Không có file nào được chọn"
            }); 

            string fileExtension = Path.GetExtension(ImportFile.FileName).ToLower();

            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                return Json(new
                {
                    Status = 0,
                    Message = "Định dạng file excel không đúng"
                });
            }

            try
            {
                var UserName = base.LoginUser.UserName;
                string filename = ImportFile.FileName;
                string targetpath = Server.MapPath("~/Uploads/");
                string pathToExcelFile = targetpath + filename;

                if (System.IO.File.Exists(pathToExcelFile))
                {
                    System.IO.File.Delete(pathToExcelFile);
                }

                var t = ImportFile.InputStream;
                ImportFile.SaveAs(targetpath + filename);

                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromFile(pathToExcelFile);
                    Worksheet sheet = workbook.Worksheets[0];
                    var data = sheet.ExportDataTable();

                    if (System.IO.File.Exists(pathToExcelFile))
                    {
                        System.IO.File.Delete(pathToExcelFile);
                    }

                    if (data.Rows.Count == 0 || data == null)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = "File excel không có dữ liệu. Vui lòng kiểm tra lại."
                        });
                    }

                    // Convert data
                    var listDataImport = new List<ImportExcelSetupMemberEarnItemModel>();

                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        var excel_ItemNo = data.Rows[i]["ItemNo"] == null ? string.Empty : data.Rows[i]["ItemNo"].ToString().TrimEnd();
                        var excel_UOM = data.Rows[i]["Uom"] == null ? string.Empty : data.Rows[i]["Uom"].ToString().TrimEnd().ToUpper();
                        var excel_SaleQty = data.Rows[i]["SaleQty"] == null ? string.Empty : data.Rows[i]["SaleQty"].ToString().TrimEnd();
                        var excel_StampsQty = data.Rows[i]["StampsQty"] == null ? string.Empty : data.Rows[i]["StampsQty"].ToString().TrimEnd();
                        var excel_FromDate = data.Rows[i]["FromDate"] == null ? string.Empty : data.Rows[i]["FromDate"].ToString().TrimEnd();
                        var excel_ToDate = data.Rows[i]["ToDate"] == null ? string.Empty : data.Rows[i]["ToDate"].ToString().TrimEnd();
                        //var excel_Blocked = data.Rows[i]["Blocked"] == null ? string.Empty : data.Rows[i]["Blocked"].ToString().TrimEnd();
                        var FromDate = DateTime.ParseExact(excel_FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        var ToDate = DateTime.ParseExact(excel_ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                        var temp = new ImportExcelSetupMemberEarnItemModel
                        {
                            ItemNo = excel_ItemNo,
                            Uom = excel_UOM,
                            Size = string.Empty,
                            SaleQty = int.Parse(excel_SaleQty),
                            StampsQty = int.Parse(excel_StampsQty),

                            //FromDate = FromDate.Date,
                            //ToDate = ToDate.Date,

                            FromDate = FromDate,
                            ToDate = ToDate,

                            FromDateStr = FromDate.Date.ToString("yyyy-MM-dd"),
                            ToDateStr = ToDate.Date.ToString("yyyy-MM-dd"),
                            //Blocked = int.Parse((excel_Blocked == "1" ? "1" : "0")),
                            Blocked = 0,  // default luon active
                            CrtUser = UserName,
                            CrtdDate = DateTime.Now,
                            UpdDate = DateTime.Now
                        };

                        listDataImport.Add(temp);
                    }

                    // Validate

                    if (listDataImport.Count == 0)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Không có dữ liệu import. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listDataImport.Any(x => string.IsNullOrEmpty(x.ItemNo)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [ItemNo] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listDataImport.Any(x => string.IsNullOrEmpty(x.Uom)))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [Uom] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listDataImport.Any(x => string.IsNullOrEmpty(x.SaleQty.ToString())))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [SaleQty] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listDataImport.Any(x => string.IsNullOrEmpty(x.StampsQty.ToString())))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [StampsQty] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listDataImport.Any(x => string.IsNullOrEmpty(x.FromDate.ToString())))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [FromDate] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    if (listDataImport.Any(x => string.IsNullOrEmpty(x.ToDate.ToString())))
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Dữ liệu cột [ToDate] đang bị null / rỗng. Vui lòng kiểm tra lại file import"
                        });
                    }

                    // Validate Fromdate / ToDate

                    //var listFromDate = listDataImport.Where(x =>x.Blocked != 1).Select(y => y.FromDate).ToList();
                    //var listToDate = listDataImport.Where(x =>x.Blocked != 1).Select(y => y.ToDate).ToList();

                    var listCheckDate = listDataImport.Where(x => (x.FromDate > x.ToDate) && x.Blocked != 1).ToList();

                    if (listCheckDate.Count > 0)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Từ ngày phải nhỏ hơn hoặc bằng đến ngày. Vui lòng kiểm tra lại file import"
                        });
                    }

                    var listCheckFromDate_DateNow = listDataImport.Where(x => (x.FromDate < DateTime.Now.Date) && x.Blocked != 1).ToList();

                    if (listCheckFromDate_DateNow.Count > 0)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Từ ngày phải lớn hơn hoặc bằng ngày hiện tại. Vui lòng kiểm tra lại file import"
                        });
                    }

                    var listCheckToDate_DateNow = listDataImport.Where(x => (x.ToDate < DateTime.Now.Date) && x.Blocked != 1).ToList();
                    if (listCheckToDate_DateNow.Count > 0)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"Đến ngày phải lớn hơn hoặc bằng ngày hiện tại. Vui lòng kiểm tra lại file import"
                        });
                    }

                    var insert = _setupLoyaltyBLO.ImportExcelMemberEarnItem(UserName, listDataImport);

                    if (insert.Item1 == 0 || insert.Item1 < 0)
                    {
                        return Json(new
                        {
                            Status = 0,
                            Message = $"{insert.Item2}"
                        });
                    }

                    return Json(new
                    {
                        Status = 1,
                        Message = $"Import danh sách thành công"
                        //Message = $"Import danh sách thành công {insert.Item1} dòng"
                    });

                }
                catch (Exception e)
                {
                    return Json(new
                    {
                        Status = 0,
                        Message = e.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Status = 0,
                    Message = ex.Message
                });
            }
        }

        [DisplayName("Báo cáo coupon đã phát hành")]
        public ActionResult GiftCouponList()
        {
            return View();
        }
        public JsonResult GetGiftCodeList()
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

            var fromDate = Request?.Form["FromDate"];
            
            if (!string.IsNullOrEmpty(fromDate))
            {
                fromDate = fromDate.Trim();
            }

            var toDate = Request?.Form["ToDate"];
            if (!string.IsNullOrEmpty(toDate))
            {
                toDate = toDate.Trim();
            }

            var isUsed = Request?.Form["IsUsed"];
            var sendCX = Request?.Form["SendCX"];

            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var coupon = Request?.Form["Coupon"];
            if (!string.IsNullOrEmpty(coupon))
            {
                coupon = coupon.Trim();
            }

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var _fromDate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var _toDate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var data = _setupLoyaltyBLO.GetGiftCouponList(_fromDate, _toDate, orderNo, coupon, textSearch, isUsed, sendCX, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<GiftCouponResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcelGiftCouponList(DateTime FromDate, DateTime ToDate, string OrderNo, string Coupon, string TextSearch, string IsUsed, string SendCX)
        {
            var data = _setupLoyaltyBLO.ExportExcelGetGiftCouponList(FromDate, ToDate, OrderNo, Coupon, TextSearch, IsUsed, SendCX);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    var listExport = data.Select(x => new ExportExcelGiftCouponResponseModel
                    {
                        CrtDateStr = x.CrtDateStr,
                        OrderNo = x.OrderNo,
                        MemberCard = x.MemberCard,
                        CouponCode = x.CouponCode, // so serial
                        MaterialNo = x.MaterialNo, // ma coupon
                        FromDateStr = x.FromDateStr,
                        ToDateStr = x.ToDateStr,
                        IsUsed = x.IsUsed,
                        IsSync = x.IsSync, // gui CX
                        Message = x.Message,
                        OrderNoUsed = x.OrderNoUsed,  // don hang da su dung
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Thời gian";
                    worksheet.Cells[1, 2].Value = "Số đơn hàng bán"; 
                    worksheet.Cells[1, 3].Value = "Số điện thoại";
                    worksheet.Cells[1, 4].Value = "Số serial";
                    worksheet.Cells[1, 5].Value = "Mã coupon";
                    worksheet.Cells[1, 6].Value = "Từ ngày";
                    worksheet.Cells[1, 7].Value = "Đến ngày";
                    worksheet.Cells[1, 8].Value = "Tình trạng sử dụng";
                    worksheet.Cells[1, 9].Value = "Trạng thái gửi CX";
                    worksheet.Cells[1, 10].Value = "Nội dung";
                    worksheet.Cells[1, 11].Value = "Số đơn hàng đã sử dụng";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 11])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ReportGiftCouponList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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