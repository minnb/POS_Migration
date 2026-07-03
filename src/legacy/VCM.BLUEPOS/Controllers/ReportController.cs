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
using Rotativa;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Report;
using VCM.BLUEPOS.Model.Report.PromotionOfferTypeComboModel;
using VCM.BLUEPOS.Model.Report.WinLifeModel;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Business.Order;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Business.Report;
using VCM.BLUEPOS.Common;
using VCM.BLUEPOS.Business.SetupPromotion;
using PLG.Controllers;
using System.Web.Configuration;
using System.Drawing;
using System.Data;


namespace PLG.Controllers
{
    public class ReportController : BaseController
    {
        private IReportBLO _reportBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        private ISetupPromotionBLO _setupProBLO;
        string ipServer = WebConfigurationManager.AppSettings["setDB1"]; 
        public ReportController(IReportBLO reportBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO, ISetupPromotionBLO setupProBLO)
        {
            _reportBLO = reportBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
            _setupProBLO = setupProBLO;
        }

        [HttpPost]
        public JsonResult GetIPPOSTerminalByStore(string storeNo)
        {
            var data = _commonBLO.LoadIPAddressPOSTerminalByStore(storeNo);
            return Json(data);
        }

        [DisplayName("Báo cáo hủy hàng")]
        public ActionResult ReportDeleteOrder()
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

            ViewBag.SetDB = listSetDB;
            return View();
        }

        public JsonResult GetDeleteRowTotalOrderList()
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
                       
            var setDBRead = Request?.Form["ServerRead"];

            //----- 19/03/2024 : phân quyền theo store -----
            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<ReportDeleteTotalRowOrderModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<ReportDeleteTotalRowOrderModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            //---------------------------------------------

            var data = _reportBLO.GetDeleteRowTotalOrderList(fromDate, toDate, setDBRead, storeNo, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<ReportDeleteTotalRowOrderModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcelGetDeleteRowTotalOrderList(String Tab1FromDate, String Tab1ToDate, string Tab1SetServer, string Tab1StoreNo)
        {
            DateTime fromDate = DateTime.ParseExact(Tab1FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(Tab1ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            //----- 19/03/2024 : phân quyền theo store -----
            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(Tab1StoreNo))
            {
                storeNo = Tab1StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(Tab1SetServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcelGetDeleteRowTotalOrderList(fromDate, toDate, Tab1SetServer, Tab1StoreNo);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StaffID,
                        x.StoreNo,
                        x.TotalRow
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã nhân viên";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Tổng số dòng hủy";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 3])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"TotalRowDeleteList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public JsonResult GetDetailDeleteRowOrderList()
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

            var fromDate = DateTime.Now;
            var toDate = DateTime.Now;

            if (!string.IsNullOrEmpty(Request?.Form["FromDate"]))
            {
                fromDate = Convert.ToDateTime(Request?.Form["FromDate"]);
            }

            if (!string.IsNullOrEmpty(Request?.Form["ToDate"]))
            {
                toDate = Convert.ToDateTime(Request?.Form["ToDate"]);
            }

            var staffID = Request?.Form["StaffCode"];
            if (!string.IsNullOrEmpty(staffID))
            {
                staffID = staffID.Trim();
            }

            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var setDBRead = Request?.Form["ServerRead"];

            // 19/03/2024: phân quyền theo Store

            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<ReportDetailDeleteRowOrderModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<ReportDetailDeleteRowOrderModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.GetDetailDeleteRowOrderList(fromDate, toDate, setDBRead, storeNo, staffID, orderNo, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<ReportDetailDeleteRowOrderModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcelGetDetailDeleteRowOrderList(String FromDate, String ToDate, string Tab2SetServer, string Tab2StoreNo, string Tab2StaffCode, string Tab2OrderNo)
        {
            DateTime fromDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            //----- 19/03/2024 : phân quyền theo store -----

            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(Tab2StoreNo))
            {
                storeNo = Tab2StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(Tab2SetServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcelGetDetailDeleteRowOrderList(fromDate, toDate, Tab2SetServer, Tab2StoreNo, Tab2StaffCode, Tab2OrderNo);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StaffID,
                        x.StoreNo,
                        x.OrderNo,
                        x.DateTimeStr,
                        x.TimeStr,
                        x.DeleteTimeStr,
                        x.ItemNo,
                        x.Description,
                        x.Quantity,
                        x.UnitPrice,
                        x.DiscountAmount,
                        x.LineAmountIncVAT
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã nhân viên";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Số đơn hàng";
                    worksheet.Cells[1, 4].Value = "Ngày";
                    worksheet.Cells[1, 5].Value = "Thời gian";
                    worksheet.Cells[1, 6].Value = "Ngày hủy dòng";
                    worksheet.Cells[1, 7].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 8].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 9].Value = "Số lượng";
                    worksheet.Cells[1, 10].Value = "Đơn giá";
                    worksheet.Cells[1, 11].Value = "Chiết khấu";
                    worksheet.Cells[1, 12].Value = "Thành tiền";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));

                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ReportDetailDeleteRowOrderList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public JsonResult GetDeleteOrderSalesList()
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

            var fromDate = DateTime.Now;
            var toDate = DateTime.Now;

            if (!string.IsNullOrEmpty(Request?.Form["FromDate"]))
            {
                fromDate = Convert.ToDateTime(Request?.Form["FromDate"]);
            }

            if (!string.IsNullOrEmpty(Request?.Form["ToDate"]))
            {
                toDate = Convert.ToDateTime(Request?.Form["ToDate"]);
            }

            var posID = Request?.Form["POSID"];
            if (!string.IsNullOrEmpty(posID))
            {
                posID = posID.Trim();
            }

            var orderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var staffCode = Request?.Form["StaffCode"];
            if (!string.IsNullOrEmpty(staffCode))
            {
                staffCode = staffCode.Trim();
            }

            var setDBRead = Request?.Form["ServerRead"];

            // 19/03/2024: phân quyền theo Store
            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<ReportDeleteOrderListModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<ReportDeleteOrderListModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.GetDeleteOrderSalesList(fromDate, toDate, setDBRead, storeNo, posID, orderType, orderNo, staffCode, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<ReportDeleteOrderListModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcelGetDeleteOrderSalesList(string FromDate, string ToDate, string SetServer3, string Tab3StoreNo, string Tab3PosID, string Tab3OrderType, string Tab3OrderNo, string Tab3StaffCode)
        {
            DateTime fromDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            //----- 19/03/2024 : phân quyền theo store -----
            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(Tab3StoreNo))
            {
                storeNo = Tab3StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer3, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcelGetDeleteOrderSalesList(fromDate, toDate, SetServer3, Tab3StoreNo, Tab3PosID, Tab3OrderType, Tab3OrderNo, Tab3StaffCode);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StoreNo,
                        x.POSTerminalNo,
                        x.StaffID,
                        x.OrderNo,
                        x.OrderType,
                        x.OrderTimeStr,
                        x.DeleteTimeStr,
                        x.TotalAmount
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Máy POS";
                    worksheet.Cells[1, 3].Value = "Mã nhân viên";
                    worksheet.Cells[1, 4].Value = "Số đơn hàng";
                    worksheet.Cells[1, 5].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 6].Value = "Ngày giao dịch";
                    worksheet.Cells[1, 7].Value = "Ngày hủy đơn hàng";
                    worksheet.Cells[1, 8].Value = "Tổng số tiền";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 8])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ReportDeleteOrderSalesList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo doanh thu chi tiết")]
        public ActionResult DetailedRevenueReport()
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
         
            var captionOrderType = "ORDERTYPE";
            var captionSaleType = "SALESTYPEREPORT";
            var captionSourceBill = "SOURCEBILL";

            ViewBag.SetDB = listSetDB;
            ViewBag.ListOrderType = _commonBLO.GetComboxOptionData(captionOrderType);
            ViewBag.ListSalesType = _commonBLO.GetComboxOptionData(captionSaleType);
            ViewBag.ListSourceBill = _commonBLO.GetComboxOptionData(captionSourceBill);
            return View();
        }
        public JsonResult GetDetailRevenueReport()
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

            var orderType = Request?.Form["orderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var salesType = Request?.Form["salesType"];
            if (!string.IsNullOrEmpty(salesType))
            {
                salesType = salesType.Trim();
            }

            var textSearch = Request?.Form["textSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var returnOrder = Request?.Form["returnOrder"];
            if (!string.IsNullOrEmpty(returnOrder))
            {
                returnOrder = returnOrder.Trim();
            }

            var userID = Request?.Form["userID"];
            if (!string.IsNullOrEmpty(userID))
            {
                userID = userID.Trim();
            }

            var vatCode = Request?.Form["vatCode"];
            var setServer = Request?.Form["setServer"];
            var setDBRead = Request?.Form["serverRead"];
            
            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["storeNo"];

            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<OrderListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<OrderListResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.GetDetailRevenueReport(fromDate, toDate, setDBRead, storeNo, orderType, salesType, vatCode, returnOrder, userID, textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<DetailRevenueReportResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcelDetailRevenueReport(string FromDate, string ToDate, string SetServer, string StoreNo, string OrderType, string SalesType, string VatCode, string ReturnOrder, string UserCreateBill, string TextSearch)
        {
            DateTime fromDate = DateTime.ParseExact(FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                StoreNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, StoreNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, StoreNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcelDetailRevenueReport(fromDate, toDate, SetServer, storeNo, OrderType, SalesType, VatCode, ReturnOrder, UserCreateBill, TextSearch);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StartingTimeStr,
                        x.OrderTimeStr,
                        x.OrderDateStr,
                        x.StoreNo,
                        x.POSTerminalNo,
                        x.OrderNo,
                        x.SalesTypeName,    // Hinh thuc ban hang
                        x.OrderType,        // Loai don hang                        
                        x.OrigOrder,        // Don hang goc 
                        x.UserID,           // Noi tao bill
                        x.PromotionID,      // Mã CTKM, Mã Voucher
                        x.CouponType,       // Loại Coupon
                        x.CouponCode,       // Mã Coupon
                        x.MCHName,
                        x.Barcode,
                        x.ItemNo,
                        x.Description,
                        x.Size,
                        x.CupType,
                        x.UnitOfMeasure,
                        x.Quantity,
                        x.UnitPrice,
                        x.DiscountAmount,
                        x.PromotionName,
                        x.VoucherCouponName,
                        x.LineAmountBeforeVAT,
                        x.VATCodeStr,
                        x.VATAmount,
                        x.LineAmountIncVAT,
                        x.ReferenceNo,
                        x.CashierID,                        
                        x.Note
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày tạo đơn hàng";
                    worksheet.Cells[1, 2].Value = "Thời gian";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "ST/CH";
                    worksheet.Cells[1, 5].Value = "Máy POS";
                    worksheet.Cells[1, 6].Value = "Số đơn hàng";
                    worksheet.Cells[1, 7].Value = "Hình thức bán hàng";
                    worksheet.Cells[1, 8].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 9].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 10].Value = "Nơi tạo bill";
                    worksheet.Cells[1, 11].Value = "Mã CTKM/Voucher";
                    worksheet.Cells[1, 12].Value = "Loại Coupon";
                    worksheet.Cells[1, 13].Value = "Mã Coupon";
                    worksheet.Cells[1, 14].Value = "Tên ngành hàng";
                    worksheet.Cells[1, 15].Value = "Barcode";
                    worksheet.Cells[1, 16].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 17].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 18].Value = "Size";
                    worksheet.Cells[1, 19].Value = "Loại ly";
                    worksheet.Cells[1, 20].Value = "ĐVT";
                    worksheet.Cells[1, 21].Value = "Số lượng";
                    worksheet.Cells[1, 22].Value = "Giá bán";
                    worksheet.Cells[1, 23].Value = "Giảm giá";
                    worksheet.Cells[1, 24].Value = "Tên CTKM/Voucher";
                    worksheet.Cells[1, 25].Value = "Tên Coupon";
                    worksheet.Cells[1, 26].Value = "Thành tiền trước thuế";
                    worksheet.Cells[1, 27].Value = "Thuế suất GTGT (%)";
                    worksheet.Cells[1, 28].Value = "Tiền thuế GTGT";
                    worksheet.Cells[1, 29].Value = "Thành tiền sau thuế";
                    worksheet.Cells[1, 30].Value = "Số đơn hàng tham chiếu";
                    worksheet.Cells[1, 31].Value = "Thu ngân";
                    worksheet.Cells[1, 32].Value = "Ghi chú";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 32])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"DetailRenvenueSalesList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo hình thức thanh toán")]
        public ActionResult PaymentOrderSalesReport()
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

            var captionOrderType = "ORDERTYPE";
            var captionSaleType = "SALESTYPEREPORT";
            var captionSourceBill = "SOURCEBILL";
            var captionPayment = "PAYMENTREPORT";
            
            ViewBag.SetDB = listSetDB;
            ViewBag.ListOrderType = _commonBLO.GetComboxOptionData(captionOrderType);
            ViewBag.ListSalesType = _commonBLO.GetComboxOptionData(captionSaleType);
            ViewBag.ListSourceBill = _commonBLO.GetComboxOptionData(captionSourceBill);
            ViewBag.ListPaymentType = _commonBLO.GetComboxOptionData(captionPayment);
            return View();
        }

        public JsonResult GetPaymentOrderSalesList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageNumber = skip/pageSize;
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
            
            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var orderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var tenderType = Request?.Form["TenderType"];
            if (!string.IsNullOrEmpty(tenderType))
            {
                tenderType = tenderType.Trim();
            }

            var staffCode = Request?.Form["StaffCode"];
            if (!string.IsNullOrEmpty(staffCode))
            {
                staffCode = staffCode.Trim();
            }

            var userID = Request?.Form["UserID"];
            if (!string.IsNullOrEmpty(userID))
            {
                userID = userID.Trim();
            }

            var searchBy = int.Parse(Request?.Form["SearchDate"]);
            var setServer = Request?.Form["setServer"];
            var setDBRead = Request?.Form["serverRead"];

            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<OrderListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<OrderListResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.GetPaymentOrderSalesList(fromDate, toDate, setDBRead, storeNo, orderNo, orderType, tenderType, userID, staffCode, searchBy, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<PaymentOrderSalesResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcel_PaymentOrderSalesList(DateTime FromDate, DateTime ToDate, string SetServer, string StoreNo, string OrderNo, string OrderType, string TenderType, string UserCreateBill, string StaffCode, string SearchDate)
        {
            var SerachBy = int.Parse(SearchDate.ToString());
            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcelPaymentOrderSalesList(FromDate, ToDate, SetServer, storeNo, OrderNo, OrderType, TenderType, UserCreateBill, StaffCode, SerachBy);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OrderNo,
                        x.PaymentDay,           // Ngay thanh toan
                        x.BusinessDay,          // Ngay kinh doanh
                        x.StoreNo,
                        x.PosNo,
                        x.TenderTypeName,       // Hinh thuc ban hang
                        x.SalesType,            // Hinh thuc thanh toan
                        x.OrderType,            // Loai don hang
                        x.UserID,               // Noi tao bill
                        x.ExchangeRate,         // Ty gia
                        x.AmountInCurrency,     // Tien nguyen te
                        x.CurrencyCode,         // Don vi ngoai te
                        x.AmountTendered,       // So tien thanh toan
                        x.CurrencyUnit,         // Đon vi tien te VNĐ
                        x.ReferenceNo,
                        x.ApprovalCode,
                        x.BankPOSCode,
                        x.BankCardType,
                        x.ReceiptNo,
                        x.IsOnline,
                        x.RefKey1,              // DH tham chieu
                        x.PartnerCode,          // Đối tác
                        x.StaffID               // Thu ngan
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số đơn hàng";
                    worksheet.Cells[1, 2].Value = "Ngày thanh toán";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "Cửa hàng";
                    worksheet.Cells[1, 5].Value = "Máy POS";
                    worksheet.Cells[1, 6].Value = "Hình thức thanh toán";
                    worksheet.Cells[1, 7].Value = "Hình thức bán hàng";                   
                    worksheet.Cells[1, 8].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 9].Value = "Nơi tạo bill";
                    worksheet.Cells[1, 10].Value = "Tỷ giá";
                    worksheet.Cells[1, 11].Value = "Tiền nguyên tệ";
                    worksheet.Cells[1, 12].Value = "Đơn vị ngoại tệ";
                    worksheet.Cells[1, 13].Value = "Số tiền thanh toán";
                    worksheet.Cells[1, 14].Value = "Đơn vị tiền tệ";
                    worksheet.Cells[1, 15].Value = "Số tham chiếu";
                    worksheet.Cells[1, 16].Value = "Mã chuẩn chi";
                    worksheet.Cells[1, 17].Value = "Máy thanh toán ngân hàng";
                    worksheet.Cells[1, 18].Value = "Loại thẻ thanh toán";
                    worksheet.Cells[1, 19].Value = "Số hóa đơn";
                    worksheet.Cells[1, 20].Value = "Trạng thái Online";
                    worksheet.Cells[1, 21].Value = "Số ĐH tham chiếu";
                    worksheet.Cells[1, 22].Value = "Đối tác";
                    worksheet.Cells[1, 23].Value = "Thu ngân";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 23])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"PaymentOrderSalesList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo doanh thu theo nhân viên")]
        public ActionResult RevenueOrderSalesByStaff()
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

            ViewBag.SetDB = listSetDB;
            return View();
        }

        public ActionResult GetRevenueOrderSalesByStaff(string FromDate, string ToDate, string SetServer, string StoreNo, string StaffCode)
        {
            try
            {
                var userName = base.LoginUser.UserName;
                var storeNo = "";
                if (!string.IsNullOrEmpty(StoreNo))
                {
                    storeNo = StoreNo.Trim();
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(new List<RevenueOrderSalesByStaffModel>());
                    }
                    storeNo = string.Join(";", listValidSite);
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, StoreNo, userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
                }

                if (!string.IsNullOrEmpty(FromDate) && !string.IsNullOrEmpty(ToDate))
                {
                    DateTime _fromDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                    DateTime _toDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                    var result = _reportBLO.GetRevenueOrderSalesByStaff(_fromDate, _toDate, SetServer, storeNo, StaffCode);
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                return Json(new List<RevenueOrderSalesByStaffModel>());
            }

            return Json(new List<RevenueOrderSalesByStaffModel>());

        }

        public ActionResult ExportPDFRevenueOrderSalesByStaff(string fromDate, string toDate, string SetServer, string storeNo, string storeName, string staffCode)
        {
            var result = new ExportPDFRevenueOrderSalesModel();
            var data = new List<RevenueOrderSalesByStaffModel>();

            try
            {
                var user = base.LoginUser.UserName;
                var hoten = base.LoginUser.FullName;
                DateTime _fromDate = DateTime.ParseExact(fromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime _toDate = DateTime.ParseExact(toDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                if (!string.IsNullOrEmpty(storeNo) && !string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                {
                    data = _reportBLO.GetRevenueOrderSalesByStaff(_fromDate, _toDate, SetServer, storeNo, staffCode);
                }

                result = new ExportPDFRevenueOrderSalesModel
                {
                    lstSaleEmp = data,
                    StaffCode = staffCode,
                    UserName = user,
                    FullName = hoten,
                    StoreNo = storeNo,
                    StoreName = storeName,
                    TransDate = _fromDate == _toDate ? $"{fromDate}" : $"{fromDate} - {toDate}"
                };
            }
            catch (Exception ex)
            {
            }

            var report = new PartialViewAsPdf("~/Views/Report/RevenueOrderSalesByStaffPDF.cshtml", result)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };

            return report;
        }

        public ActionResult ExportExcel_RevenueOrderSalesByStaff(string FromDate, string ToDate, string SetServer, string StoreNo, string StaffCode)
        {
            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new List<ExportExcelRevenueOrderSalesByStaffModel>());
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, StoreNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            DateTime fDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            var data = new List<ExportExcelRevenueOrderSalesByStaffModel>();

            if (!string.IsNullOrEmpty(FromDate) && !string.IsNullOrEmpty(ToDate))
            {
                data = _reportBLO.ExportExcel_RevenueOrderSalesByStaff(fDate, tDate, SetServer, storeNo, StaffCode);
            }

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StaffCode,
                        x.FullName,
                        x.AmountTotal
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã nhân viên";
                    worksheet.Cells[1, 2].Value = "Tên nhân viên";
                    worksheet.Cells[1, 3].Value = "Doanh số";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 3])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    var countR = data.Count;
                    var rowTotal = countR + 2;

                    worksheet.Cells[rowTotal, 1].Value = "Total";
                    worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A" + rowTotal + ":B" + rowTotal + ""].Merge = true;

                    worksheet.Cells[rowTotal, 3].Value = data.Sum(d => d.AmountTotal);
                    worksheet.Cells[rowTotal, 3].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 3].Style.Font.Bold = true;

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0";
                    }

                    string fileName = $"SalesByStaff_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("BC doanh thu theo cửa hàng")]
        public ActionResult RevenueOrderSalesByStore()
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

            ViewBag.SetDB = listSetDB;
            return View();
        }
        public ActionResult GetRevenueOrderSalesByStore()
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

                var userName = base.LoginUser.UserName;
                var storeNo = Request?.Form["StoreNo"];

                var fromDate = Request?.Form["FromDate"];
                var toDate = Request?.Form["ToDate"];
                var SetDB = Request?.Form["SetServer"];
                var setDBRead = Request?.Form["ServerRead"];

                DateTime tuNgay = DateTime.ParseExact(fromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime denNgay = DateTime.ParseExact(toDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                var jsonStore = JsonConvert.SerializeObject(modelStore);
                var listData = new List<RevenueOrderSalesByStoreModel>();

                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.SiteNo,
                        SiteName = a.SiteName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }

                // 19/03/2024 : check store xem có đúng là store đã được phân quyền không ?

                if (!string.IsNullOrEmpty(storeNo))
                {
                    storeNo = modelStore.SiteNo;
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<RevenueOrderSalesByStoreModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<RevenueOrderSalesByStoreModel>() });
                    }
                }

                // -------------------------------

                if (!string.IsNullOrEmpty(SetDB) || !string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var result = _reportBLO.GetRevenueOrderSalesByStore(setDBRead, jsonStore, storeNo, tuNgay, denNgay, pageSize, pageNumber);
                    if (result.Count > 0)
                    {
                        recordsTotal = (int)result.FirstOrDefault()?.Total;
                    }
                    listData = result;
                }

                return Json(new DataTablesViewModel<RevenueOrderSalesByStoreModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = listData });

            }
            catch (Exception ex)
            {
                return Json(new List<HistoryEndDateModel>());
            }
        }

        public ActionResult ExportExcelRevenueOrderSalesByStore(string SetDB, string ServerRead, string SiteNo, string TuNgay, string DenNgay)
        {
            DateTime fDate = DateTime.ParseExact(TuNgay, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(DenNgay, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            var dataResult = new List<RevenueOrderSalesByStoreModel>();
            var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.SiteNo,
                    SiteName = a.SiteName
                }).ToList();
                jsonStore = JsonConvert.SerializeObject(listParam);
            }

            // 19/03/2024 : check store xem có đúng là store đã được phân quyền không ?
            
            if (!string.IsNullOrEmpty(SiteNo))
            {
                SiteNo = modelStore.SiteNo;
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(base.LoginUser.UserName, SiteNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
            }

            //-----------------------------------------

            if (!string.IsNullOrEmpty(SetDB) || !string.IsNullOrEmpty(modelStore.SiteNo))
            {
                dataResult = _reportBLO.GetRevenueOrderSalesByStore(ServerRead, jsonStore, SiteNo, fDate, tDate, 1000000, 1);
            }

            var data = new List<ExportExcelRevenueOrderSalesByStoreModel>();

            data = dataResult.Select(a => new ExportExcelRevenueOrderSalesByStoreModel
            {
                StoreNo = a.StoreNo,
                StoreName = a.StoreName,
                BussinessDate = a.BussinessDate,
                CountOrderTotal = a.CountOrderTotal,
                AmountTotal = a.SauVAT,
                TotalTruocVAT = a.TruocVAT,
                TotalVAT = a.ThueVAT
            }).ToList();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Tên ST/CH";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "Tổng số hóa đơn";
                    worksheet.Cells[1, 5].Value = "Doanh thu trước thuế";
                    worksheet.Cells[1, 6].Value = "Tiền thuế VAT";
                    worksheet.Cells[1, 7].Value = "Doanh thu sau thuế";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 7])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    var countR = data.Count;
                    var rowTotal = countR + 2;

                    worksheet.Cells[rowTotal, 1].Value = "Total";
                    worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A" + rowTotal + ":C" + rowTotal + ""].Merge = true;

                    worksheet.Cells[rowTotal, 4].Value = data.Sum(d => d.CountOrderTotal);
                    worksheet.Cells[rowTotal, 4].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 4].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 5].Value = data.Sum(d => d.TotalTruocVAT);
                    worksheet.Cells[rowTotal, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 5].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 6].Value = data.Sum(d => d.TotalVAT);
                    worksheet.Cells[rowTotal, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 6].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 7].Value = data.Sum(d => d.AmountTotal);
                    worksheet.Cells[rowTotal, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 7].Style.Font.Bold = true;

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                    }

                    string fileName = $"RevenueOrderSalesByStore_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        [DisplayName("BC doanh thu theo ngành hàng")]
        public ActionResult RevenueOrderSalesByMCH()
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

            ViewBag.SetDB = listSetDB;
            ViewBag.ListStore = listStoreByUser;
            return View();
        }

        [HttpPost]
        public ActionResult SalesByMCHLoad(string SetServer, string StoreNo, string FromDate, string ToDate)
        {
            var data = new List<RevenueOrderSalesByMCHModel>();
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName; 
            var _tungayTitle = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("dd/MM/yyyy");
            var _denngayTitle = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("dd/MM/yyyy");

            // 19/03/2024 : phan quyen theo Store

            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(user, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return PartialView(new SalesByMCHRepsone
                    {
                        Message = $"Không có dữ liệu báo cáo"
                    });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, storeNo, user);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            try
            {
                if (!string.IsNullOrEmpty(FromDate) && !string.IsNullOrEmpty(ToDate))
                {
                    DateTime TuNgay = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                    DateTime DenNgay = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                    var daySpan = DenNgay.Subtract(TuNgay).TotalDays;

                    if (daySpan >= 7)
                    {
                        return PartialView(new SalesByMCHRepsone {
                            Status = false,
                            TransDate = $"{_tungayTitle} - {_denngayTitle}",
                            FromDate = TuNgay,
                            ToDate = DenNgay,
                            UserName = user,
                            FullName = hoten,
                            StoreNo = StoreNo,
                            ListData = data,
                            Message = $"Chỉ xem được khoảng thời gian tối đa 7 ngày"
                        });
                    }
                    else
                    {
                        data = _reportBLO.GetRevenueOrderSalesByMCH(SetServer, storeNo, TuNgay, DenNgay);
                        var storeName = "";
                        if (!string.IsNullOrEmpty(StoreNo) || StoreNo != "")
                        {
                            if (data.Count > 0)
                            {
                                storeName = data.FirstOrDefault()?.StoreName;
                                return PartialView(new SalesByMCHRepsone
                                {
                                    Status = true,
                                    TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                    FromDate = TuNgay,
                                    ToDate = DenNgay,
                                    UserName = user,
                                    FullName = hoten,
                                    StoreNo = StoreNo,
                                    StoreName = storeName,
                                    ListData = data,
                                    Message = $"Success"
                                });
                            }
                            else
                            {
                                storeName = data.FirstOrDefault()?.StoreName;
                                return PartialView(new SalesByMCHRepsone
                                {
                                    Status = false,
                                    TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                    FromDate = TuNgay,
                                    ToDate = DenNgay,
                                    UserName = user,
                                    FullName = hoten,
                                    StoreNo = StoreNo,
                                    StoreName = "",
                                    ListData = data,
                                    Message = $"Không có dữ liệu báo cáo"
                                });
                            }                            
                        }
                        else
                        {
                            if (data.Count > 0)
                            {
                                return PartialView(new SalesByMCHRepsone
                                {
                                    Status = true,
                                    TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                    FromDate = TuNgay,
                                    ToDate = DenNgay,
                                    UserName = user,
                                    FullName = hoten,
                                    ListData = data,
                                    Message = $"Success"
                                });
                            }
                            else
                            {
                                return PartialView(new SalesByMCHRepsone
                                {
                                    Status = true,
                                    TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                    FromDate = TuNgay,
                                    ToDate = DenNgay,
                                    UserName = user,
                                    FullName = hoten,
                                    ListData = data,
                                    Message = $"Không có dữ liệu báo cáo"
                                });
                            }                           
                        }  
                    }
                }
            }
            catch (Exception ex)
            {
                return PartialView(new SalesByMCHRepsone {
                    Status = false,
                    TransDate = $"{FromDate} - {ToDate}",
                    UserName = user,
                    FullName = hoten,
                    StoreNo = StoreNo,
                    ListData = data,
                    Message = $"Error {JsonConvert.SerializeObject(ex)}"
                });
            }

            return PartialView(new SalesByMCHRepsone {
                Status = false,
                UserName = user,
                TransDate = $"{FromDate} - {ToDate}",
                FullName = hoten,
                StoreNo = StoreNo,
                ListData = data, Message = $"Fail"
            });
        }

        public ActionResult SalesByMCHPDF(string SetServer, string StoreNo, string FromDate, string ToDate)
        {
            var user = base.LoginUser.UserName;
            var result = new SalesByMCHRepsone();
            var data = new List<RevenueOrderSalesByMCHModel>();
            var _tungayTitle = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("dd/MM/yyyy");
            var _denngayTitle = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("dd/MM/yyyy");

            // 19/03/2024 : phan quyen theo Store
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(user, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return PartialView(new SalesByMCHRepsone
                    {
                        Message = $"Không có dữ liệu báo cáo"
                    });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, storeNo, user);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            try
            {               
                var hoten = base.LoginUser.FullName;
                DateTime tuNgay = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime denNgay = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                var daySpan = denNgay.Subtract(tuNgay).TotalDays;

                if (daySpan >= 7)
                {
                    result = new SalesByMCHRepsone {
                        Status = false,
                        TransDate = $"{_tungayTitle} - {_denngayTitle}",
                        FromDate = tuNgay,
                        ToDate = denNgay,
                        UserName = user,
                        FullName = hoten,
                        StoreNo = StoreNo,
                        ListData = data,
                        Message = $"Chỉ xem được khoảng thời gian tối đa 7 ngày"
                    };
                }
                else
                {
                    data = _reportBLO.GetRevenueOrderSalesByMCH(SetServer, storeNo, tuNgay, denNgay); 

                     var storeName = "";
                    if (!string.IsNullOrEmpty(StoreNo) || StoreNo != "")
                    {
                        if (data.Count > 0)
                        {
                            storeName = data.FirstOrDefault()?.StoreName;
                            result = new SalesByMCHRepsone
                            {
                                Status = true,
                                TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                FromDate = tuNgay,
                                ToDate = denNgay,
                                UserName = user,
                                FullName = hoten,
                                StoreNo = StoreNo,
                                StoreName = storeName,
                                ListData = data,
                                Message = $"Success"
                            };
                        }
                        else
                        {
                            result = new SalesByMCHRepsone
                            {
                                Status = false,
                                TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                FromDate = tuNgay,
                                ToDate = denNgay,
                                UserName = user,
                                FullName = hoten,
                                StoreNo = StoreNo,
                                StoreName = "",
                                ListData = data,
                                Message = $"Không có dữ liệu báo cáo"
                            };
                        }
                    }
                    else
                    {
                        if (data.Count > 0)
                        {
                            result = new SalesByMCHRepsone
                            {
                                Status = true,
                                TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                FromDate = tuNgay,
                                ToDate = denNgay,
                                UserName = user,
                                FullName = hoten,
                                ListData = data,
                                Message = $"Success"
                            };
                        }
                        else
                        {
                            result = new SalesByMCHRepsone
                            {
                                Status = true,
                                TransDate = $"{_tungayTitle} - {_denngayTitle}",
                                FromDate = tuNgay,
                                ToDate = denNgay,
                                UserName = user,
                                FullName = hoten,
                                ListData = data,
                                Message = $"Không có dữ liệu báo cáo"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            var report = new PartialViewAsPdf("~/Views/Report/SalesByMCHLoad.cshtml", result)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };

            return report;
        }

        public ActionResult ExportExcel_RevenueOrderSalesByMCH(string SetServer, string StoreNo, DateTime FromDate, DateTime ToDate)
        {
            // 19/03/2024: phan quyen theo Store
            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcel_RevenueOrderSalesByMCH(SetServer, storeNo, FromDate, ToDate);

            if (data != null && data.Count > 0)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.BussinessDateStr,
                        x.StoreNo,
                        x.StoreName,
                        x.MCHCode,
                        x.MCHName,
                        x.OrderTotal,
                        x.AmountTotal                       
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "BÁO CÁO TỔNG HỢP DOANH THU THEO NGÀNH HÀNG";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells["A1:G1"].Merge = true;

                    worksheet.Cells[2, 1].Value = $"TỪ NGÀY : {Convert.ToDateTime(FromDate).ToString("dd-MM-yyyy")} - ĐẾN NGÀY:{Convert.ToDateTime(ToDate).ToString("dd-MM-yyyy")}";
                    worksheet.Cells[2, 1].Style.Font.Bold = true;
                    worksheet.Cells["A2:G2"].Merge = true;

                    worksheet.Cells[5, 1].Value = "Ngày kinh doanh";
                    worksheet.Cells[5, 2].Value = "Mã cửa hàng";
                    worksheet.Cells[5, 3].Value = "Tên cửa hàng";
                    worksheet.Cells[5, 4].Value = "Mã ngành hàng";
                    worksheet.Cells[5, 5].Value = "Tên ngành hàng";
                    worksheet.Cells[5, 6].Value = "Số lượng";
                    worksheet.Cells[5, 7].Value = "Thành tiền";

                    using (ExcelRange r = worksheet.Cells[5, 1, 5, 7])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                    }

                    //for (var row = 6; row <= data.Count + 6; row++)
                    //{
                    //    worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                    //    worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                    //}

                    //worksheet.Cells["A2"].LoadFromCollection(listExport, false);

                    worksheet.Cells["A6"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"RevenueOrderSalesByMCH_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("BC doanh thu theo quầy hàng")]
        public ActionResult RevenueOrderSalesDetailByMCH()
        {
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(base.LoginUser.UserName);
            ViewBag.ComboxNganhHang = _commonBLO.LoadComboxNganhHang();
            return View();
        }

        public ActionResult _SalesByMCHDetailLoad(string siteCode, string mch2, string mch5, string fromDate, string toDate)
        {
            var data = new List<SalesDetailListByMCHModel>();
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName;
            try
            {
                if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                {
                    DateTime tuNgay = Convert.ToDateTime(fromDate);
                    DateTime denNgay = Convert.ToDateTime(toDate);
                    var daySpan = denNgay.Subtract(tuNgay).TotalDays;
                    if (daySpan >= 7)
                    {
                        return PartialView(new SalesByMCHDetailRepsone
                        {
                            Status = false,
                            TransDate = $"{fromDate} - {toDate}",
                            FromDate = tuNgay,
                            ToDate = denNgay,
                            UserName = user,
                            FullName = hoten,
                            StoreNo = siteCode,
                            ListData = data,
                            Message = $"Chỉ xem được khoảng thời gian tối đa 7 ngày"
                        });
                    }
                    else
                    {
                        // 19/03/2024 :; check store theo phân quyền

                        if (!string.IsNullOrEmpty(siteCode))
                        {
                            siteCode = siteCode.Trim();
                            var listValidSite = _commonBLO.CheckSecurityStoreByUserName(base.LoginUser.UserName, siteCode);
                            if (listValidSite == null | listValidSite.Count == 0)
                            {
                                return Json(string.Empty, JsonRequestBehavior.AllowGet);
                            }
                        }
                        //-----------------------------------

                        data = _reportBLO.GetDetailRevenueSalesByStoreMCH(siteCode, mch2, mch5, tuNgay, denNgay);

                        var storeName = "";
                        var mch2Name = "";

                        if (!string.IsNullOrEmpty(siteCode))
                        {
                            storeName = data.FirstOrDefault()?.StoreName;
                        }

                        if (!string.IsNullOrEmpty(mch2))
                        {
                            mch2Name = data.FirstOrDefault()?.MCHName;
                        }

                        return PartialView(new SalesByMCHDetailRepsone
                        {
                            Status = true,
                            TransDate = $"{fromDate} - {toDate}",
                            FromDate = tuNgay,
                            ToDate = denNgay,
                            UserName = user,
                            FullName = hoten,
                            StoreNo = siteCode,
                            StoreName = storeName,
                            MCH2Name = mch2Name,
                            ListData = data,
                            Message = $"Success"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return PartialView(new SalesByMCHDetailRepsone
                {
                    Status = false,
                    TransDate = $"{fromDate} - {toDate}",
                    UserName = user,
                    FullName = hoten,
                    StoreNo = siteCode,
                    ListData = data,
                    Message = $"Error {JsonConvert.SerializeObject(ex)}"
                });
            }

            return PartialView(new SalesByMCHDetailRepsone
            {
                Status = false,
                UserName = user,
                TransDate = $"{fromDate} - {toDate}",
                FullName = hoten,
                StoreNo = siteCode,
                ListData = data,
                Message = $"Fail"
            });

        }

        public ActionResult ExportExcel_GetDetailRevenueSalesByStoreMCH(string FromDate, string ToDate, string StoreNo, string MCH2, string MCH5)
        {
            // 19/03/2024 : check store theo phân quyền
            if (!string.IsNullOrEmpty(StoreNo))
            {
                StoreNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(base.LoginUser.UserName, StoreNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json("Không có dữ liệu", JsonRequestBehavior.AllowGet);
                }
            }

            //-----------------------------------------

            var data = _reportBLO.ExportExcel_GetDetailRevenueSalesByStoreMCH(FromDate, ToDate, StoreNo, MCH2, MCH5);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    var listExport = data.Select(x => new
                    {
                        x.BussinessDate,
                        x.StoreNo,
                        x.POSTerminalNo,
                        x.OrderNo,
                        x.SalesTypeName,
                        x.SalesIsReturn,
                        x.MCH2Code,
                        x.MCH2Name,
                        x.MCH3Code,
                        x.MCH3Name,
                        x.UnitOfMeasure,
                        x.Quantity,
                        x.AmountVAT                       
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Máy POS";
                    worksheet.Cells[1, 4].Value = "Số đơn hàng";
                    worksheet.Cells[1, 5].Value = "Hình thức bán hàng";
                    worksheet.Cells[1, 6].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 7].Value = "Mã ngành hàng";
                    worksheet.Cells[1, 8].Value = "Tên ngành hàng";
                    worksheet.Cells[1, 9].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 10].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 11].Value = "ĐVT";
                    worksheet.Cells[1, 12].Value = "Số lượng";
                    worksheet.Cells[1, 13].Value = "Số tiền";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 13])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"Revenue_Detail_By_Store_MCH_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        public ActionResult SalesByMCHDetailPDF(string siteCode, string mch2, string mch5, string fromDate, string toDate)
        {
            var result = new SalesByMCHDetailRepsone();
            var data = new List<SalesDetailListByMCHModel>();
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName;
            try
            {
                DateTime tuNgay = Convert.ToDateTime(fromDate);
                DateTime denNgay = Convert.ToDateTime(toDate);
                if (!string.IsNullOrEmpty(siteCode) && !string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                {
                    data = _reportBLO.GetDetailRevenueSalesByStoreMCH(siteCode, mch2, mch5, tuNgay, denNgay);
                }
                var daySpan = denNgay.Subtract(tuNgay).TotalDays;
                if (daySpan >= 7)
                {
                    result = new SalesByMCHDetailRepsone
                    {
                        Status = false,
                        TransDate = $"{fromDate} - {toDate}",
                        FromDate = tuNgay,
                        ToDate = denNgay,
                        UserName = user,
                        FullName = hoten,
                        StoreNo = siteCode,
                        ListData = data,
                        Message = $"Chỉ xem được khoảng thời gian tối đa 7 ngày"
                    };
                }
                else
                {
                    var storeName = "";
                    var mch2Name = "";
                    if (!string.IsNullOrEmpty(siteCode))
                    {
                        storeName = data.FirstOrDefault()?.StoreName;
                    }

                    if (!string.IsNullOrEmpty(mch2))
                    {
                        mch2Name = data.FirstOrDefault()?.MCHName;
                    }

                    result = new SalesByMCHDetailRepsone
                    {
                        Status = true,
                        TransDate = $"{fromDate} - {toDate}",
                        FromDate = tuNgay,
                        ToDate = denNgay,
                        UserName = user,
                        FullName = hoten,
                        StoreNo = siteCode,
                        StoreName = storeName,
                        MCH2Name = mch2Name,
                        ListData = data,
                        Message = $"Success"
                    };
                }
            }
            catch (Exception ex)
            {
                return PartialView(new SalesByMCHDetailRepsone
                {
                    Status = false,
                    TransDate = $"{fromDate} - {toDate}",
                    UserName = user,
                    FullName = hoten,
                    StoreNo = siteCode,
                    ListData = data,
                    Message = $"Error {JsonConvert.SerializeObject(ex)}"
                });
            }

            var report = new PartialViewAsPdf("~/Views/Report/_SalesByMCHDetailLoad.cshtml", result)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };
            return report;
        }

        [HttpPost]
        public ActionResult GetMCH5(string MCH2)
        {
            var mch5 = new List<MCHComboboxModel>();
            try
            {
                if (!string.IsNullOrEmpty(MCH2))
                {
                    mch5 = _commonBLO.LoadComboxMCH5(MCH2);
                }
            }
            catch (Exception ex)
            {
            }
            return Json(mch5);
        }

        [DisplayName("BC sử dụng BNMH - Voucher")]
        public ActionResult VoucherReceiptSalesReport()
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

            ViewBag.SetDB = listSetDB;
            ViewBag.ListVoucherType = _commonBLO.GetVoucherTenderType();
            return View();
        }

        public JsonResult GetVoucherReceiptList()
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

            var setServer = Request?.Form["SetServer"];
            var setReadDB = Request?.Form["ServerRead"];
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var posID = Request?.Form["PosID"];
            if (!string.IsNullOrEmpty(posID))
            {
                posID = posID.Trim();
            }

            var voucherType = Request?.Form["VoucherReceiptType"];
            if (!string.IsNullOrEmpty(voucherType))
            {
                voucherType = voucherType.Trim();
            }

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var fromDateStr = Request?.Form["fromDate"];
            var toDateStr = Request?.Form["toDate"];

            DateTime tungay = DateTime.ParseExact(fromDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime denngay = DateTime.ParseExact(toDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            var data = _reportBLO.GetVoucherReceiptList(tungay, denngay, setReadDB, storeNo, posID, voucherType, textSearch, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<VoucherReceiptResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcelGetVoucherReceiptList(string FromDate, string ToDate, string SetServer, string StoreNo, string PosID, string TenderType, string TextSearch)
        {
            DateTime Tungay = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime Denngay = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            var data = _reportBLO.ExportGetVoucherReceiptList(Tungay, Denngay, SetServer, StoreNo, PosID, TenderType, TextSearch);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.ReferenceNo_Payment,
                        x.ReferenceNo_Voucher,
                        x.OrderNo,
                        x.StartingTimeStr,
                        x.OrderTimeStr,
                        x.OrderDateStr,
                        x.StoreNo,
                        x.POSTerminalNo,
                        x.TotalAmount,
                        x.OrderType
                        //x.CardVinID
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số BNMH";
                    worksheet.Cells[1, 2].Value = "Serial Voucher";
                    worksheet.Cells[1, 3].Value = "Số đơn hàng";
                    worksheet.Cells[1, 4].Value = "Ngày tạo đơn hàng";
                    worksheet.Cells[1, 5].Value = "Thời gian";
                    worksheet.Cells[1, 6].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 7].Value = "ST/CH";
                    worksheet.Cells[1, 8].Value = "Máy POS";
                    worksheet.Cells[1, 9].Value = "Số tiền";
                    worksheet.Cells[1, 10].Value = "Loại đơn hàng";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"VoucherReceiptReport_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("BC kết ca VinMart")]
        public ActionResult ReportShiftEndVM()
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

            ViewBag.SetDB = listSetDB;
            return View();
        }
        public ActionResult ReportShiftEndVMLoad()
        {
            try
            {
                var userName = base.LoginUser.UserName;
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

                var storeNo = Request?.Form["StoreNo"];
                var dateBusiness = Request?.Form["BussinessDate"];
                var slStatus = Request?.Form["IsShiftClosed"];
                var staffCode = Request?.Form["StaffCode"];
                var SetDB = Request?.Form["SetDB"];
                var recordsTotal = 0;
                DateTime ngayKD = DateTime.ParseExact(dateBusiness, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.SiteNo,
                        SiteName = a.SiteName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }

                var result = _reportBLO.Report_ShiftEnd_VM(SetDB, jsonStore, ngayKD, staffCode, slStatus, pageSize, pageNumber);
                if (result.Count > 0)
                    recordsTotal = (int)result.FirstOrDefault()?.Total;
                return Json(new DataTablesViewModel<ReportShiftEndModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new List<ReportShiftEndModel>());
            }
        }
        public ActionResult ReportShiftEndVMExcel(string SetServer, string SiteNo, string BussinessDate, string StaffCode, string Status)
        {
            var userName = base.LoginUser.UserName;
            DateTime bussinessDate = DateTime.ParseExact(BussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetServer).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.SiteNo,
                    SiteName = a.SiteName
                }).ToList();
                jsonStore = JsonConvert.SerializeObject(listParam);
            }

            var result = _reportBLO.Report_ShiftEnd_VM(SetServer, jsonStore, bussinessDate, StaffCode, Status, 1000000, 1);
            var data = result.Select(a => new ReportShiftEndExcelModel
            {
                StoreNo = a.StoreNo,
                StaffCode = a.StaffCode,
                BussinessDate = a.BussinessDate,
                ShiftNumber = a.ShiftNumber,
                BeginAmount = a.BeginAmount,
                CloseAmount = a.CloseAmount,
                AmountTendered = a.AmountTendered,
                BlanceMoney = a.BlanceMoney,
                IsShiftClosed = a.IsShiftClosed == "1" ? "Đã đóng ca" : "Chưa đóng ca",
                CloseShiftDate = a.CloseShiftDate
            }).ToList();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Mã nhân viên";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "Ca";
                    worksheet.Cells[1, 5].Value = "Tiền đầu ca";
                    worksheet.Cells[1, 6].Value = "Tiền mặt";
                    worksheet.Cells[1, 7].Value = "Tiền hệ thống";
                    worksheet.Cells[1, 8].Value = "Chênh lệch";
                    worksheet.Cells[1, 9].Value = "Trạng thái";
                    worksheet.Cells[1, 10].Value = "Ngày kết ca";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }
                    var countR = data.Count;
                    var rowTotal = countR + 2;

                    worksheet.Cells[rowTotal, 1].Value = "Total";
                    worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A" + rowTotal + ":D" + rowTotal + ""].Merge = true;

                    worksheet.Cells[rowTotal, 5].Value = data.Sum(d => d.BeginAmount);
                    worksheet.Cells[rowTotal, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 5].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 6].Value = data.Sum(d => d.CloseAmount);
                    worksheet.Cells[rowTotal, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 6].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 7].Value = data.Sum(d => d.AmountTendered);
                    worksheet.Cells[rowTotal, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 7].Style.Font.Bold = true;

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 10].Style.Numberformat.Format = "dd/MM/yyyy hh:MM:ss";
                    }

                    string fileName = $"ShiftVM_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public ActionResult ReportShiftEndVMP()
        {
            var listSetDB = _commonBLO.ListSQLServer().Where(d => d.IPServer != "10.235.25.71").ToList();
            ViewBag.SetDB = listSetDB;
            return View();
        }

        [HttpPost]
        public ActionResult ReportShiftEndVMPLoad()
        {
            try
            {
                var userName = base.LoginUser.UserName;
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

                var storeNo = Request?.Form["StoreNo"];
                var dateBusiness = Request?.Form["BussinessDate"];
                var slStatus = Request?.Form["IsShiftClosed"];
                var posID = Request?.Form["PosTerminal"];
                var SetDB = Request?.Form["SetDB"];

                var recordsTotal = 0;
                DateTime ngayKD = DateTime.ParseExact(dateBusiness, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);

                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.SiteNo,
                        SiteName = a.SiteName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }

                var result = _reportBLO.Report_ShiftEnd_VMP(SetDB, jsonStore, ngayKD, posID, slStatus, pageSize, pageNumber);
                if (result.Count > 0)
                    recordsTotal = (int)result.FirstOrDefault()?.Total;
                return Json(new DataTablesViewModel<ReportShiftEndModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new List<ReportShiftEndModel>());
            }
        }

        public ActionResult ReportShiftEndVMPExcel(string SetServer, string SiteNo, string BussinessDate, string Keyword, string Status)
        {
            var userName = base.LoginUser.UserName;
            DateTime bussinessDate = DateTime.ParseExact(BussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);
            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetServer).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.SiteNo,
                    SiteName = a.SiteName
                }).ToList();
                jsonStore = JsonConvert.SerializeObject(listParam);
            }

            var result = _reportBLO.Report_ShiftEnd_VMP(SetServer, jsonStore, bussinessDate, Keyword, Status, 1000000, 1);
            var data = result.Select(a => new ReportShiftEndExcelVMPModel
            {
                StoreNo = a.StoreNo,
                PosTerminal = a.PosTerminal,
                BussinessDate = a.BussinessDate,
                ShiftNumber = a.ShiftNumber,
                BeginAmount = a.BeginAmount,
                CloseAmount = a.CloseAmount,
                AmountTendered = a.AmountTendered,
                BlanceMoney = a.BlanceMoney,
                IsShiftClosed = a.IsShiftClosed == "1" ? "Đã đóng ca" : "Chưa đóng ca",
                CloseShiftDate = a.CloseShiftDate
            }).ToList();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Mã POS";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "Ca";
                    worksheet.Cells[1, 5].Value = "Tiền đầu ca";
                    worksheet.Cells[1, 6].Value = "Tiền mặt";
                    worksheet.Cells[1, 7].Value = "Tiền hệ thống";
                    worksheet.Cells[1, 8].Value = "Chênh lệch";
                    worksheet.Cells[1, 9].Value = "Trạng thái";
                    worksheet.Cells[1, 10].Value = "Ngày đóng ca";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    var countR = data.Count;
                    var rowTotal = countR + 2;

                    worksheet.Cells[rowTotal, 1].Value = "Total";
                    worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A" + rowTotal + ":D" + rowTotal + ""].Merge = true;

                    worksheet.Cells[rowTotal, 5].Value = data.Sum(d => d.BeginAmount);
                    worksheet.Cells[rowTotal, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 5].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 6].Value = data.Sum(d => d.CloseAmount);
                    worksheet.Cells[rowTotal, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 6].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 7].Value = data.Sum(d => d.AmountTendered);
                    worksheet.Cells[rowTotal, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 7].Style.Font.Bold = true;

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 10].Style.Numberformat.Format = "dd/MM/yyyy hh:MM:ss";
                    }

                    string fileName = $"ShiftVMP_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public ActionResult SalesFailBusDateReport()
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

            ViewBag.SetDB = listSetDB;
            return View();
        }

        [HttpPost]
        public ActionResult SalesFailBusDateLoad()
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

                var storeNo = Request?.Form["StoreNo"];
                var fromDate = Request?.Form["FromDate"];
                var toDate = Request?.Form["ToDate"];
                var SetDB = Request?.Form["SetDB"];
                var SetDB_Read = Request?.Form["ServerRead"];
                var recordsTotal = 0;

                DateTime tuNgay = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime denNgay = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.SiteNo,
                        SiteName = a.SiteName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }

                if (!string.IsNullOrEmpty(SetDB) || !string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var result = _reportBLO.Sales_By_Store_Fail_BussinessDate(SetDB_Read, jsonStore, modelStore.SiteNo, tuNgay, denNgay, pageSize, pageNumber);
                    if (result.Count > 0)
                        recordsTotal = (int)result.FirstOrDefault()?.Total;
                    return Json(new DataTablesViewModel<SalesFailDateBusByStoreModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
                }
            }
            catch (Exception ex)
            {
            }
            return Json(new List<SalesFailDateBusByStoreModel>());
        }

        public ActionResult ExportExcelSalesFailBusinessDate(string SetServer, string SiteNo, string TuNgay, string DenNgay)
        {
            DateTime fDate = DateTime.ParseExact(TuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(DenNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var dataResult = new List<SalesFailDateBusByStoreModel>();
            var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetServer).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.SiteNo,
                    SiteName = a.SiteName
                }).ToList();
                jsonStore = JsonConvert.SerializeObject(listParam);
            }

            if (!string.IsNullOrEmpty(SetServer) || !string.IsNullOrEmpty(modelStore.SiteNo))
            {
                dataResult = _reportBLO.Sales_By_Store_Fail_BussinessDate(SetServer, jsonStore, modelStore.SiteNo, fDate, tDate, 1000000, 1);
            }

            var data = new List<SalesFailDateBusByStoreExcelModel>();
            data = dataResult.Select(a => new SalesFailDateBusByStoreExcelModel
            {
                StoreNo = a.StoreNo,
                StoreName = a.StoreName,
                BussinessDate = a.BussinessDate,
                OrderTime = a.OrderTime,
                CountOrderTotal = a.CountOrderTotal,
                AmountTotal = a.SauVAT,
                TotalTruocVAT = a.TruocVAT,
                TotalVAT = a.ThueVAT
            }).ToList();

            if (data != null)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Tên ST/CH";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "Ngày đơn hàng";
                    worksheet.Cells[1, 5].Value = "Tổng hóa đơn";
                    worksheet.Cells[1, 6].Value = "Trước thuế";
                    worksheet.Cells[1, 7].Value = "Thuế VAT";
                    worksheet.Cells[1, 8].Value = "Sau thuế";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 8])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }
                    var countR = data.Count;
                    var rowTotal = countR + 2;

                    worksheet.Cells[rowTotal, 1].Value = "Total";
                    worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A" + rowTotal + ":C" + rowTotal + ""].Merge = true;

                    worksheet.Cells[rowTotal, 5].Value = data.Sum(d => d.CountOrderTotal);
                    worksheet.Cells[rowTotal, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 5].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 6].Value = data.Sum(d => d.TotalTruocVAT);
                    worksheet.Cells[rowTotal, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 6].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 7].Value = data.Sum(d => d.TotalVAT);
                    worksheet.Cells[rowTotal, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 7].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 8].Value = data.Sum(d => d.AmountTotal);
                    worksheet.Cells[rowTotal, 8].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 8].Style.Font.Bold = true;

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                    }

                    string fileName = $"SaleFailDateByStore_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        //[DisplayName("BC tốc độ tính KPI nhân viên")]
        //public ActionResult KPIStaffReport()
        //{
        //    ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName).Where(d => d.StyleProfile == "VM").ToList();
        //    return View();
        //}
        //public ActionResult KPIStaffLoadAll()
        //{
        //    try
        //    {
        //        var draw = Request?.Form["draw"];
        //        var start = Request?.Form["start"];
        //        var length = Request?.Form["length"];
        //        var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
        //        var sortColumnDirection = Request?.Form["order[0][dir]"];
        //        var searchValue = Request?.Form["search[value]"];
        //        var pageSize = length != null ? Convert.ToInt32(length) : 1;
        //        var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

        //        var storeNo = Request?.Form["StoreNo"];
        //        var fromDate = Request?.Form["FromDate"];
        //        var toDate = Request?.Form["ToDate"];
        //        var staffCode = Request?.Form["StaffCode"];
        //        var recordsTotal = 0;

        //        DateTime tuNgay = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //        DateTime denNgay = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //        var result = _reportBLO.ReportKPIStaff(storeNo, staffCode, tuNgay, denNgay);
        //        if (result.Count > 0)
        //            recordsTotal = (int)result.Count();
        //        return Json(new DataTablesViewModel<KPIStaffModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new List<KPIStaffModel>());
        //    }
        //}

        //public ActionResult KPIStaffExcel(string SiteNo, string StaffCode, string TuNgay, string DenNgay)
        //{
        //    DateTime tuNgay = DateTime.ParseExact(TuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //    DateTime denNgay = DateTime.ParseExact(DenNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        //    var result = _reportBLO.ReportKPIStaff(SiteNo, StaffCode, tuNgay, denNgay);

        //    if (result.Count > 0)
        //    {
        //        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //        using (var package = new ExcelPackage())
        //        {
        //            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
        //            worksheet.Cells[1, 1].Value = "ST/CH";
        //            worksheet.Cells[1, 2].Value = "Mã nhân viên";
        //            worksheet.Cells[1, 3].Value = "Mã SAP";
        //            worksheet.Cells[1, 4].Value = "Tên nhân viên";
        //            worksheet.Cells[1, 5].Value = "Tổng số lượng";
        //            worksheet.Cells[1, 6].Value = "Tổng thời gian(giây)";
        //            worksheet.Cells[1, 7].Value = "Thời gian trung bình(giây)";

        //            using (ExcelRange r = worksheet.Cells[1, 1, 1, 7])
        //            {
        //                r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
        //                r.Style.Font.Bold = true;
        //                r.Style.Fill.PatternType = ExcelFillStyle.Solid;
        //                r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
        //                r.AutoFilter = true;
        //            }
        //            var countR = result.Count;
        //            var rowTotal = countR + 2;

        //            worksheet.Cells[rowTotal, 1].Value = "Total";
        //            worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
        //            worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        //            worksheet.Cells["A" + rowTotal + ":D" + rowTotal + ""].Merge = true;

        //            worksheet.Cells[rowTotal, 5].Value = result.Sum(d => d.Quantity);
        //            worksheet.Cells[rowTotal, 5].Style.Numberformat.Format = "#,000";
        //            worksheet.Cells[rowTotal, 5].Style.Font.Bold = true;

        //            worksheet.Cells[rowTotal, 6].Value = result.Sum(d => d.SeccondTotal);
        //            worksheet.Cells[rowTotal, 6].Style.Numberformat.Format = "#,000";
        //            worksheet.Cells[rowTotal, 6].Style.Font.Bold = true;

        //            worksheet.Cells[rowTotal, 7].Value = result.Sum(d => d.SeccondTotal) / result.Sum(d => d.Quantity);
        //            worksheet.Cells[rowTotal, 7].Style.Numberformat.Format = "#.000";
        //            worksheet.Cells[rowTotal, 7].Style.Font.Bold = true;

        //            worksheet.Cells["A2"].LoadFromCollection(result, false);
        //            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //            for (var row = 1; row <= countR + 1; row++)
        //            {
        //                worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
        //                worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
        //                worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
        //            }

        //            string fileName = $"KPIStaff_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo kết ca")]
        public ActionResult ReportShiftEndPLG()
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

            ViewBag.SetDB = listSetDB;
            return View();
        }

        public ActionResult LoadShiftEndReportPLG()
        {
            try
            {
                var userName = base.LoginUser.UserName;
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var pageSize = length != null ? Convert.ToInt32(length) : 1;
                var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;
                var recordsTotal = 0;

                var storeNo = Request?.Form["StoreNo"];
                var dateBusiness = Request?.Form["BussinessDate"];
                var slStatus = Request?.Form["IsShiftClosed"];
                var posID = Request?.Form["PosTerminal"];
                var SetDB = Request?.Form["SetDB"];

                DateTime ngayKD = DateTime.ParseExact(dateBusiness, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.SiteNo,
                        SiteName = a.SiteName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }

                // 19/03/2024 : check store theo phân quyền

                if (!string.IsNullOrEmpty(storeNo))
                {
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(base.LoginUser.UserName, modelStore.SiteNo);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<ShiftEndReportPLGResponseModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<ShiftEndReportPLGResponseModel>() });
                    }
                }
                //------------------------------------------------

                var setDBRead = Request?.Form["ServerRead"];
                var result = _reportBLO.GetShiftEndReportPLG(setDBRead, jsonStore, ngayKD, posID, slStatus, pageSize, pageNumber);

                if (result.Count > 0)
                {
                    recordsTotal = (int)result.FirstOrDefault()?.Total;
                }

                return Json(new DataTablesViewModel<ShiftEndReportPLGResponseModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return Json(new List<ReportShiftEndModel>());
            }
        }

        public ActionResult ExportExcelShiftEndReportPLG(string SetServer, string SiteNo, string BussinessDate, string Keyword, string Status)
        {
            var userName = base.LoginUser.UserName;
            DateTime bussinessDate = DateTime.ParseExact(BussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetServer).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.SiteNo,
                    SiteName = a.SiteName
                }).ToList();
                jsonStore = JsonConvert.SerializeObject(listParam);
            }
            else
            {
                // 19/03/2024 : check store theo phân quyền
                if (!string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(base.LoginUser.UserName, modelStore.SiteNo);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            var result = _reportBLO.GetShiftEndReportPLG(SetServer, jsonStore, bussinessDate, Keyword, Status, 1000000, 1);
            var data = result.Select(a => new ExportExcelShiftEndReportPLGModel
            {
                StoreNo = a.StoreNo,
                PosTerminal = a.PosTerminal,
                BussinessDate = a.BussinessDate,
                ShiftNumber = a.ShiftNumber,
                BeginAmount = a.BeginAmount,
                CloseAmount = a.CloseAmount,
                AmountTendered = a.AmountTendered,
                BlanceMoney = a.BlanceMoney,
                IsShiftClosed = a.IsShiftClosed == "1" ? "Đã đóng ca" : "Chưa đóng ca",
                CloseShiftDate = a.CloseShiftDate
            }).ToList();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Máy POS";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "Ca";
                    worksheet.Cells[1, 5].Value = "Tiền đầu ca";
                    worksheet.Cells[1, 6].Value = "Tiền mặt";
                    worksheet.Cells[1, 7].Value = "Tiền hệ thống";
                    worksheet.Cells[1, 8].Value = "Chênh lệch";
                    worksheet.Cells[1, 9].Value = "Trạng thái";
                    worksheet.Cells[1, 10].Value = "Ngày đóng ca";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    var countR = data.Count;
                    var rowTotal = countR + 2;

                    worksheet.Cells[rowTotal, 1].Value = "Total";
                    worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A" + rowTotal + ":D" + rowTotal + ""].Merge = true;

                    worksheet.Cells[rowTotal, 5].Value = data.Sum(d => d.BeginAmount);
                    worksheet.Cells[rowTotal, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 5].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 6].Value = data.Sum(d => d.CloseAmount);
                    worksheet.Cells[rowTotal, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 6].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 7].Value = data.Sum(d => d.AmountTendered);
                    worksheet.Cells[rowTotal, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 7].Style.Font.Bold = true;

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 10].Style.Numberformat.Format = "dd/MM/yyyy hh:MM:ss";
                    }

                    string fileName = $"Baocaoketca_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        [DisplayName("Báo cáo tổng hợp KM giảm giá")]
        public ActionResult PromotionDiscountValueReport()
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

            var model = _reportBLO.LoadOfferTypeDiscountValue();
            ViewBag.SetDB = listSetDB;
            return View(model);
        }

        public ActionResult GetPromotionDiscountValue()
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

                var fromDate = Request?.Form["FromDate"];
                var toDate = Request?.Form["ToDate"];
                var SetDB = Request?.Form["SetServer"];
                var setDBRead = Request?.Form["ServerRead"];
                var offerType = Request?.Form["OfferType"];
                var storeNo = Request?.Form["StoreNo"];

                DateTime tuNgay = DateTime.ParseExact(fromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime denNgay = DateTime.ParseExact(toDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                //var jsonStore = JsonConvert.SerializeObject(modelStore);
                //var listData = new List<OrderSalesDiscountValueResponseModel>();

                //if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                //{
                //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                //    {
                //        SiteNo = a.SiteNo,
                //        SiteName = a.SiteName
                //    }).ToList();
                //    jsonStore = JsonConvert.SerializeObject(listParam);
                //}

                // 19/03/2024 : phan quyen theo store
                var userName = base.LoginUser.UserName;
                var listData = new List<OrderSalesDiscountValueResponseModel>();

                if (!string.IsNullOrEmpty(storeNo))
                {
                    storeNo = storeNo.Trim();
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null || listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<OrderSalesDiscountValueResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<OrderSalesDiscountValueResponseModel>() });
                    }
                    storeNo = string.Join(";", listValidSite);
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
                }

                if (!string.IsNullOrEmpty(setDBRead))
                {
                    //var result = _reportBLO.GetPromotionDiscountValue(setDBRead, jsonStore, storeNo, tuNgay, denNgay, offerType, pageSize, pageNumber);

                    var result = _reportBLO.GetPromotionDiscountValue(setDBRead, storeNo, "", tuNgay, denNgay, offerType, pageSize, pageNumber);

                    if (result.Count > 0)
                    {
                        recordsTotal = (int)result.FirstOrDefault()?.Total;
                    }

                    listData = result;
                }

                return Json(new DataTablesViewModel<OrderSalesDiscountValueResponseModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = listData });
            }
            catch (Exception ex)
            {
                return Json(new List<OrderSalesDiscountValueResponseModel>());
            }
        }

        public ActionResult ExportExcel_PromotionDiscountValue(string SetDB, string ServerRead, string StoreNo, string FromDate, string ToDate, string OfferType)
        {
            DateTime fDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            //var dataResult = new List<OrderSalesDiscountValueResponseModel>();
            //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(StoreNo);
            //var jsonStore = JsonConvert.SerializeObject(modelStore);

            //if (string.IsNullOrEmpty(StoreNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            //{
            //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
            //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
            //    {
            //        SiteNo = a.SiteNo,
            //        SiteName = a.SiteName
            //    }).ToList();
            //    jsonStore = JsonConvert.SerializeObject(listParam);
            //}

            //----- 19/03/2024 : phân quyền theo store -----

            var userName = base.LoginUser.UserName;
            var dataResult = new List<OrderSalesDiscountValueResponseModel>();
            var storeNo = "";

            if (!string.IsNullOrEmpty(StoreNo))
            {
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, StoreNo);
                if (listValidSite == null || listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else // th chon tat ca
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ServerRead, "", userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            if (!string.IsNullOrEmpty(ServerRead))
            {
                //dataResult = _reportBLO.GetPromotionDiscountValue(ServerRead, jsonStore, storeNo, fDate, tDate, OfferType, 1000000, 1);
                dataResult = _reportBLO.GetPromotionDiscountValue(ServerRead, storeNo, "", fDate, tDate, OfferType, 1000000, 1);
            }

            if (dataResult.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataResult.Select(a => new
                    {
                        a.BussinessDate,
                        a.StoreNo,
                        a.StoreName,
                        a.OfferType,
                        a.CountOrderTotal,
                        a.TotalDiscountAmount
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Tên ST/CH";
                    worksheet.Cells[1, 4].Value = "Loại CTKM giảm giá";
                    worksheet.Cells[1, 5].Value = "Tổng số đơn hàng";
                    worksheet.Cells[1, 6].Value = "Tổng tiền giảm giá";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 6])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ReportPromotionDiscountValue_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo chi tiết KM giảm giá")]
        public ActionResult DetailPromotionDiscountValueReport()
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

            var model = _reportBLO.LoadOfferTypeDiscountValue();
            ViewBag.SetDB = listSetDB;
            return View(model);
        }

        public ActionResult GetDetailPromotionDiscountValue()
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

                var storeNo = Request?.Form["StoreNo"];
                var fromDate = Request?.Form["FromDate"];
                var toDate = Request?.Form["ToDate"];
                var SetDB = Request?.Form["SetServer"];
                var setDBRead = Request?.Form["ServerRead"];
                var offerType = Request?.Form["OfferType"];

                DateTime tuNgay = DateTime.ParseExact(fromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime denNgay = DateTime.ParseExact(toDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                //var jsonStore = JsonConvert.SerializeObject(modelStore);
                //var listData = new List<DetailOrderSalesDiscountValueResponseModel>();

                //if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                //{
                //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                //    {
                //        SiteNo = a.SiteNo,
                //        SiteName = a.SiteName
                //    }).ToList();
                //    jsonStore = JsonConvert.SerializeObject(listParam);
                //}

                // 19/03/2024 : phan quyen theo store

                var userName = base.LoginUser.UserName;
                var listData = new List<DetailOrderSalesDiscountValueResponseModel>();

                if (!string.IsNullOrEmpty(storeNo))
                {
                    storeNo = storeNo.Trim();
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null || listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<DetailOrderSalesDiscountValueResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<DetailOrderSalesDiscountValueResponseModel>() });
                    }
                    storeNo = string.Join(";", listValidSite);
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
                }

                if (!string.IsNullOrEmpty(setDBRead))
                {
                    //var result = _reportBLO.GetDetailPromotionDiscountValue(setDBRead, jsonStore, storeNo, tuNgay, denNgay, offerType, pageSize, pageNumber);
                    var result = _reportBLO.GetDetailPromotionDiscountValue(setDBRead, storeNo, "", tuNgay, denNgay, offerType, pageSize, pageNumber);

                    if (result.Count > 0)
                    {
                        recordsTotal = (int)result.FirstOrDefault()?.Total;
                    }

                    listData = result;
                }

                return Json(new DataTablesViewModel<DetailOrderSalesDiscountValueResponseModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = listData });
            }
            catch (Exception ex)
            {
                return Json(new List<DetailOrderSalesDiscountValueResponseModel>());
            }
        }

        public ActionResult ExportExcel_DetailPromotionDiscountValue(string SetDB, string ServerRead, string StoreNo, string FromDate, string ToDate, string OfferType)
        {
            DateTime fDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            //var dataResult = new List<DetailOrderSalesDiscountValueResponseModel>();
            //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(StoreNo);
            //var jsonStore = JsonConvert.SerializeObject(modelStore);

            //if (string.IsNullOrEmpty(StoreNo))
            //{
            //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
            //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
            //    {
            //        SiteNo = a.SiteNo,
            //        SiteName = a.SiteName
            //    }).ToList();
            //    jsonStore = JsonConvert.SerializeObject(listParam);
            //}

            //----- 19/03/2024 : phân quyền theo store -----

            var userName = base.LoginUser.UserName;
            var dataResult = new List<DetailOrderSalesDiscountValueResponseModel>();
            var storeNo = "";

            if (!string.IsNullOrEmpty(StoreNo))
            {
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, StoreNo);
                if (listValidSite == null || listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else // th chon tat ca
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ServerRead, "", userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            if (!string.IsNullOrEmpty(ServerRead))
            {
                //dataResult = _reportBLO.GetDetailPromotionDiscountValue(ServerRead, jsonStore, storeNo, fDate, tDate, OfferType, 1000000, 1);
                dataResult = _reportBLO.GetDetailPromotionDiscountValue(ServerRead, storeNo, "", fDate, tDate, OfferType, 1000000, 1);

            }

            if (dataResult.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataResult.Select(a => new
                    {
                        a.BussinessDate,
                        a.StoreNo,
                        a.StoreName,
                        a.OrderNo,
                        a.OfferType,
                        a.OfferNo,
                        a.ProName,
                        a.DiscountLoyalty,
                        a.TotalDiscountAmount,
                        a.CashierID
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Tên ST/CH";
                    worksheet.Cells[1, 4].Value = "Số đơn hàng";
                    worksheet.Cells[1, 5].Value = "Loại CTKM giảm giá";
                    worksheet.Cells[1, 6].Value = "Mã CTKM giảm giá";
                    worksheet.Cells[1, 7].Value = "Tên CTKM giảm giá";
                    worksheet.Cells[1, 8].Value = "Giảm giá Loyalty";
                    worksheet.Cells[1, 9].Value = "Số tiền";
                    worksheet.Cells[1, 10].Value = "Thu ngân";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 10])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"BC_Chitiet_KM_Giamgia_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public ActionResult SaleOdoo()
        {
            return View();
        }

        [HttpPost]
        public JsonResult SaleOdooLoad()
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
                var orderNo = Request?.Form["OrderNo"];
                var storeNo = Request?.Form["StoreNo"];
                var data = _reportBLO.ListSaleOdoo(storeNo, orderNo, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<SaleOdooModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
            }
            catch
            {
                return Json(new DataTablesViewModel<SaleOdooModel> { recordsFiltered = 0, recordsTotal = 0, data = new List<SaleOdooModel>() });
            }
        }

        public ActionResult DetailOrder(string orderNo)
        {
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName;
            var data = new List<SaleOdooModel>();
            try
            {
                data = _reportBLO.ListDetailOrder(orderNo);
            }
            catch (Exception ex)
            {
            }

            var report = new PartialViewAsPdf("~/Views/Report/DetailOrder.cshtml", data)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };
            return report;
        }

        [DisplayName("Báo cáo tổng hợp KM ComBo")]
        public ActionResult PromotionOfferTypeByComboReport()
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

            var model = _reportBLO.LoadOfferTypeCombo();
            ViewBag.SetDB = listSetDB;
            return View(model);

        }
        public ActionResult GetPromotionOfferTypeByComboList()
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

                var storeNo = Request?.Form["StoreNo"];
                var fromDate = Request?.Form["FromDate"];
                var toDate = Request?.Form["ToDate"];
                var SetDB = Request?.Form["SetServer"];
                var setDBRead = Request?.Form["ServerRead"];
                var offerType = Request?.Form["OfferType"];

                DateTime tuNgay = DateTime.ParseExact(fromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime denNgay = DateTime.ParseExact(toDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                //var jsonStore = JsonConvert.SerializeObject(modelStore);
                //var listData = new List<PromotionOfferTypeComboResponseModel>();

                //if (string.IsNullOrEmpty(storeNo))
                //{
                //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                //    {
                //        SiteNo = a.SiteNo,
                //        SiteName = a.SiteName
                //    }).ToList();
                //    jsonStore = JsonConvert.SerializeObject(listParam);
                //}

                // 19/03/2024 : phan quyen theo store

                var userName = base.LoginUser.UserName;
                var listData = new List<PromotionOfferTypeComboResponseModel>();

                if (!string.IsNullOrEmpty(storeNo))
                {
                    storeNo = storeNo.Trim();
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null || listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<PromotionOfferTypeComboResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<PromotionOfferTypeComboResponseModel>() });
                    }
                    storeNo = string.Join(";", listValidSite);
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
                }
                //-------------------------------------

                if (!string.IsNullOrEmpty(setDBRead))
                {
                    //var result = _reportBLO.GetPromotionOfferTypeByComboList(setDBRead, jsonStore, storeNo, tuNgay, denNgay, offerType, pageSize, pageNumber);
                    var result = _reportBLO.GetPromotionOfferTypeByComboList(setDBRead, storeNo, "", tuNgay, denNgay, offerType, pageSize, pageNumber);

                    if (result.Count > 0)
                    {
                        recordsTotal = (int)result.FirstOrDefault()?.Total;
                    }

                    listData = result;
                }

                return Json(new DataTablesViewModel<PromotionOfferTypeComboResponseModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = listData });

            }
            catch (Exception ex)
            {
                return Json(new List<PromotionOfferTypeComboResponseModel>());
            }
        }

        public ActionResult ExportExcel_PromotionOfferTypeByCombo(string SetDB, string ServerRead, string StoreNo, string FromDate, string ToDate, string OfferType)
        {
            DateTime fDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            //var dataResult = new List<PromotionOfferTypeComboResponseModel>();
            //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(StoreNo);
            //var jsonStore = JsonConvert.SerializeObject(modelStore);

            //if (string.IsNullOrEmpty(StoreNo))
            //{
            //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
            //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
            //    {
            //        SiteNo = a.SiteNo,
            //        SiteName = a.SiteName
            //    }).ToList();
            //    jsonStore = JsonConvert.SerializeObject(listParam);
            //}

            // 19/03/2024 : phan quyen theo store

            var userName = base.LoginUser.UserName;
            var dataResult = new List<PromotionOfferTypeComboResponseModel>();
            var storeNo = "";

            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null || listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ServerRead, StoreNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            //--------------------------------------------

            if (!string.IsNullOrEmpty(ServerRead))
            {
                //dataResult = _reportBLO.GetPromotionOfferTypeByComboList(ServerRead, jsonStore, storeNo, fDate, tDate, OfferType, 1000000, 1);
                dataResult = _reportBLO.GetPromotionOfferTypeByComboList(ServerRead, storeNo, "", fDate, tDate, OfferType, 1000000, 1);

            }

            if (dataResult != null)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataResult.Select(a => new
                    {
                        a.BussinessDate,
                        a.StoreNo,
                        a.StoreName,
                        a.OfferType,
                        a.OfferNo,
                        a.OfferName,
                        a.SL_Combo,
                        a.TotalAmount
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Tên ST/CH";
                    worksheet.Cells[1, 4].Value = "Loại CTKM Combo";
                    worksheet.Cells[1, 5].Value = "Mã CTKM Combo";
                    worksheet.Cells[1, 6].Value = "Tên CTKM Combo";
                    worksheet.Cells[1, 7].Value = "Số lượng Combo";
                    worksheet.Cells[1, 8].Value = "Tổng số tiền giảm giá";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 8])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ListPromotionOfferTypeCombo_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo chi tiết KM ComBo")]
        public ActionResult DetailPromotionOfferTypeByComboReport()
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

            var model = _reportBLO.LoadOfferTypeCombo();
            ViewBag.SetDB = listSetDB;
            return View(model);
        }

        public ActionResult GetDetailPromotionOfferTypeByComboList()
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

                var storeNo = Request?.Form["StoreNo"];
                var fromDate = Request?.Form["FromDate"];
                var toDate = Request?.Form["ToDate"];
                var SetDB = Request?.Form["SetServer"];
                var setDBRead = Request?.Form["ServerRead"];
                var offerType = Request?.Form["OfferType"];

                DateTime tuNgay = DateTime.ParseExact(fromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime denNgay = DateTime.ParseExact(toDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

                //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                //var jsonStore = JsonConvert.SerializeObject(modelStore);
                //var listData = new List<DetailPromotionOfferTypeComboResponseModel>();

                //if (string.IsNullOrEmpty(storeNo))
                //{
                //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                //    {
                //        SiteNo = a.SiteNo,
                //        SiteName = a.SiteName
                //    }).ToList();
                //    jsonStore = JsonConvert.SerializeObject(listParam);
                //}

                // 19/03/2024 : phan quyen theo store

                var userName = base.LoginUser.UserName;
                var listData = new List<DetailPromotionOfferTypeComboResponseModel>();

                if (!string.IsNullOrEmpty(storeNo))
                {
                    storeNo = storeNo.Trim();
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null || listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<DetailPromotionOfferTypeComboResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<DetailPromotionOfferTypeComboResponseModel>() });
                    }
                    storeNo = string.Join(";", listValidSite);
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
                }
                //-------------------------------------

                if (!string.IsNullOrEmpty(setDBRead))
                {
                    //var result = _reportBLO.GetDetailPromotionOfferTypeByComboList(setDBRead, jsonStore, storeNo, tuNgay, denNgay, offerType, pageSize, pageNumber);
                    var result = _reportBLO.GetDetailPromotionOfferTypeByComboList(setDBRead, storeNo, "", tuNgay, denNgay, offerType, pageSize, pageNumber);

                    if (result.Count > 0)
                    {
                        recordsTotal = (int)result.FirstOrDefault()?.Total;
                    }

                    listData = result;

                }

                return Json(new DataTablesViewModel<DetailPromotionOfferTypeComboResponseModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = listData });
            }
            catch (Exception ex)
            {
                return Json(new List<DetailPromotionOfferTypeComboResponseModel>());
            }
        }

        public ActionResult ExportExcel_DetailPromotionOfferTypeByCombo(string SetDB, string ServerRead, string StoreNo, string FromDate, string ToDate, string OfferType)
        {
            DateTime fDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            //var dataResult = new List<DetailPromotionOfferTypeComboResponseModel>();
            //var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(StoreNo);
            //var jsonStore = JsonConvert.SerializeObject(modelStore);

            //if (string.IsNullOrEmpty(StoreNo))
            //{
            //    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
            //    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
            //    {
            //        SiteNo = a.SiteNo,
            //        SiteName = a.SiteName
            //    }).ToList();
            //    jsonStore = JsonConvert.SerializeObject(listParam);
            //}

            // 19/03/2024 : phan quyen theo store

            var userName = base.LoginUser.UserName;
            var dataResult = new List<DetailPromotionOfferTypeComboResponseModel>();
            var storeNo = "";

            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null || listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ServerRead, StoreNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            //--------------------------------------------

            if (!string.IsNullOrEmpty(ServerRead))
            {
                //dataResult = _reportBLO.GetDetailPromotionOfferTypeByComboList(ServerRead, jsonStore, storeNo, fDate, tDate, OfferType, 1000000, 1);
                dataResult = _reportBLO.GetDetailPromotionOfferTypeByComboList(ServerRead, storeNo, "", fDate, tDate, OfferType, 1000000, 1);

            }

            if (dataResult != null)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = dataResult.Select(a => new
                    {
                        a.BussinessDate,
                        a.StoreNo,
                        a.StoreName,
                        a.OfferType,
                        a.OfferNo,
                        a.OfferName,
                        a.OrderNo,
                        a.ItemNo,
                        a.Description,
                        a.Quantity,
                        a.UnitPrice,
                        a.DiscountAmount,
                        a.PrepaymentAmount
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Tên ST/CH";
                    worksheet.Cells[1, 4].Value = "Loại CTKM Combo";
                    worksheet.Cells[1, 5].Value = "Mã CTKM Combo";
                    worksheet.Cells[1, 6].Value = "Tên CTKM Combo";
                    worksheet.Cells[1, 7].Value = "Số đơn hàng";
                    worksheet.Cells[1, 8].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 9].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 10].Value = "Số lượng";
                    worksheet.Cells[1, 11].Value = "Giá bán";
                    worksheet.Cells[1, 12].Value = "Giảm giá";
                    worksheet.Cells[1, 13].Value = "Thành tiền";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 13])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                        r.AutoFilter = true;
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ListDetailPromotionOfferTypeCombo_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo sử dụng ly")]
        public ActionResult ReportUsedCup()
        {
            ViewBag.ListSite = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            return View();
        }

        [HttpGet]
        [ParentAuthorize("ReportUsedCup")]
        public ActionResult _ViewReportCup(string storeNo, string ngayKD, string shiftCode)
        {
            var result = new List<SaleUsedCupModel>();
            try
            {
                // 19/03/2024 : check store theo phan quyen

                var userName = base.LoginUser.UserName;
                if (!string.IsNullOrEmpty(storeNo))
                {
                    storeNo = storeNo.Trim();
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        result = new List<SaleUsedCupModel>(); 
                    }
                }
                else // khong chon CH
                {
                    result = new List<SaleUsedCupModel>(); 
                }

                //-------------------------------------------

                var businessDate = DateTime.ParseExact(ngayKD, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                result = _reportBLO.SaleReportUsedCup(storeNo, businessDate, shiftCode);

            }
            catch (Exception ex)
            {
            }
            return PartialView(result);
        }

        [ParentAuthorize("ReportUsedCup")]
        public ActionResult ExportExcelReportCup(string storeNo, string ngayKD, string shiftCode)
        {
            // 19/03/2024: check store theo phan quyền

            var userName = base.LoginUser.UserName;
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null || listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
            }
            else // không chon CH
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            //----------------------------

            DateTime businessDate = DateTime.ParseExact(ngayKD, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var data = _reportBLO.SaleReportUsedCup(storeNo, businessDate, shiftCode);

            if (data != null && data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    var listSize = data.GroupBy(d => new { Size = d.Size }).Select(a => new { Size = a.Key.Size }).ToList();
                    var listShift = data.GroupBy(d => new { ShiftCode = d.ShiftNo }).Select(a => new { ShiftCode = a.Key.ShiftCode }).OrderBy(d => d.ShiftCode).ToList();

                    worksheet.Cells[1, 1].Value = "BÁO CÁO SỬ DỤNG LY CỬA HÀNG: " + storeNo;
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells["A1:E1"].Merge = true;

                    worksheet.Cells[2, 1].Value = $"NGÀY KINH DOANH: {ngayKD}";
                    worksheet.Cells[2, 1].Style.Font.Bold = true;
                    worksheet.Cells["A2:E2"].Merge = true;

                    worksheet.Cells[5, 1].Value = "Ca";
                    worksheet.Cells[5, 2].Value = "Mã sản phẩm";
                    worksheet.Cells[5, 3].Value = "Tên sản phẩm";

                    var s = 4;
                    if (listSize.Count > 0)
                    {
                        foreach (var size in listSize)
                        {
                            worksheet.Cells[5, s].Value = "Size " + size.Size;
                            s++;
                        }
                    }
                    worksheet.Cells[5, s].Value = "Tổng";

                    using (ExcelRange r = worksheet.Cells[5, 1, 5, s])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    var rowItem = 6;
                    var rowExcel = 0;

                    foreach (var shift in listShift)
                    {
                        var listItem = data.Where(d => d.ShiftNo == shift.ShiftCode).GroupBy(d => new { d.StoreNo, d.ItemNo, d.Description })
                        .Select(a => new
                        {
                            StoreNo = a.Key.StoreNo,
                            ItemNo = a.Key.ItemNo,
                            ItemName = a.Key.Description
                        }).ToList();

                        var listCountItemShift = listItem.Count;
                        rowExcel += listCountItemShift;

                        var merFirst = rowItem + listCountItemShift;
                        var rowMerge = $"A{rowItem}:A{merFirst - 1}";

                        worksheet.Cells[rowItem, 1].Value = shift.ShiftCode;
                        worksheet.Cells[rowMerge].Merge = true;
                        worksheet.Cells[rowMerge].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells[rowMerge].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                        foreach (var item in listItem)
                        {
                            worksheet.Cells[rowItem, 2].Value = item.ItemNo;
                            worksheet.Cells[rowItem, 3].Value = item.ItemName;
                            var colSize = 4;
                            foreach (var size in listSize)
                            {
                                var checkQtt = data.FirstOrDefault(d => d.ItemNo == item.ItemNo && d.Size == size.Size && d.ShiftNo == shift.ShiftCode);
                                var quantity = checkQtt == null ? 0 : checkQtt.Quantity;
                                worksheet.Cells[rowItem, colSize].Value = quantity;
                                colSize++;
                            }

                            var checkTotalQty = data.Where(d => d.ItemNo == item.ItemNo && d.ShiftNo == shift.ShiftCode);
                            var totalQty = checkTotalQty == null ? 0 : checkTotalQty.Sum(d => d.Quantity);

                            worksheet.Cells[rowItem, colSize].Value = totalQty;
                            worksheet.Cells[rowItem, colSize].Style.Font.Bold = true;
                            worksheet.Cells[rowItem, colSize].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            rowItem++;
                        }
                    }
                    using (ExcelRange r = worksheet.Cells[5, 1, 5 + rowExcel, s])
                    {
                        r.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        r.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    }

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"ListCup_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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
        [ParentAuthorize("ReportUsedCup")]
        public ActionResult _ViewReportCupDetail(string storeNo, string ngayKD, string shiftCode, string itemCup, string size)
        {
            var salesOrderType = _commonBLO.LoadComboxSalesOrderType();
            var model = new DetailCupRequest
            {
                StoreNo = storeNo,
                BussinessDate = Convert.ToDateTime(ngayKD),
                ShiftNo = shiftCode,
                ItemCup = itemCup,
                Size = size,
                SalesOrderType = salesOrderType
            };

            return PartialView(model);
        }

        [HttpPost]
        [ParentAuthorize("ReportUsedCup")]
        public ActionResult _ViewReportCupDetailLoad()
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
                var BussinessDate = Convert.ToDateTime(Request?.Form["BussinessDate"]).ToString("yyyyMMdd");
                var ShiftCode = Request?.Form["ShiftCode"];
                var Size = Request?.Form["Size"];

                var ItemCup = Request?.Form["ItemCup"];
                var SaleType = Request?.Form["SaleType"];
                var SaleIsReturn = Request?.Form["SaleIsReturn"];
                var ItemNo = Request?.Form["ItemNo"];

                var data = _reportBLO.ListDetailCup(storeNo, BussinessDate, ShiftCode, ItemCup, Size, orderNo, SaleType, ItemNo, SaleIsReturn, out recordsTotal, pageNumber, pageSize);
                return Json(new DataTablesViewModel<Sale_Report_Detail_Cup_Model> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<Sale_Report_Detail_Cup_Model>());
            }
        }

        [ParentAuthorize("ReportUsedCup")]
        public ActionResult ReportExportDetailCup(string storeNo, string ngayKD, string shiftCode, string itemCup, string size, string orderNo, string salesOrderType, string saleIsReturn, string itemNo)
        {
            var businessDate = Convert.ToDateTime(ngayKD).ToString("yyyyMMdd");
            //  var data = _reportBLO.SaleReportUsedCup(storeNo, businessDate, shiftCode);
            var recordsTotal = 0;
            var data = _reportBLO.ListDetailCup(storeNo, businessDate, shiftCode, itemCup, size, orderNo, salesOrderType, itemNo, saleIsReturn, out recordsTotal, 1000000, 1);

            if (data != null && data.Count > 0)
            {
                var listExport = data.Select(x => new
                {
                    x.SaleTypeName,
                    x.OrderNo,
                    x.SaleIsReturnName,
                    x.ItemNo,
                    x.Barcode,
                    x.Description,
                    x.UnitOfMeasure,
                    x.QuantityCup,
                    x.UnitPrice,
                    x.DiscountAmount,
                    x.LineAmount,
                    x.VATCode,
                    x.VATAmount,
                    x.Amount
                }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    worksheet.Cells[1, 1].Value = "BÁO CÁO CHI TIẾT SỬ DỤNG LY CỬA HÀNG: " + storeNo;
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells["A1:E1"].Merge = true;

                    worksheet.Cells[2, 1].Value = $"NGÀY KINH DOANH: {Convert.ToDateTime(ngayKD).ToString("dd-MM-yyyy")} - CA:{shiftCode} - LY:{itemCup}";
                    worksheet.Cells[2, 1].Style.Font.Bold = true;
                    worksheet.Cells["A2:E2"].Merge = true;

                    worksheet.Cells[5, 1].Value = "Hình thức";
                    worksheet.Cells[5, 2].Value = "Mã đơn hàng";
                    worksheet.Cells[5, 3].Value = "Loại đơn hàng";
                    worksheet.Cells[5, 4].Value = "Mã sản phẩm";
                    worksheet.Cells[5, 5].Value = "Barcode";
                    worksheet.Cells[5, 6].Value = "Tên sản phẩm";
                    worksheet.Cells[5, 7].Value = "ĐVT";
                    worksheet.Cells[5, 8].Value = "Số lượng";
                    worksheet.Cells[5, 9].Value = "Đơn giá";
                    worksheet.Cells[5, 10].Value = "Giảm giá";
                    worksheet.Cells[5, 11].Value = "Thành tiền trước thuế";
                    worksheet.Cells[5, 12].Value = "Thuế suất";
                    worksheet.Cells[5, 13].Value = "Tiền thuế";
                    worksheet.Cells[5, 14].Value = "Thành tiền sau thuế";

                    using (ExcelRange r = worksheet.Cells[5, 1, 5, 14])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    for (var row = 6; row <= data.Count + 6; row++)
                    {
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 11].Style.Numberformat.Format = "#,##0";
                        //worksheet.Cells[row, 12].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 13].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 14].Style.Numberformat.Format = "#,##0";
                    }

                    worksheet.Cells["A6"].LoadFromCollection(listExport, false).AutoFitColumns();
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"DetailCup_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("BC doanh thu theo sản phẩm")]
        public ActionResult RevenueOrderSalesByProduct()
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

            ViewBag.SetDB = listSetDB;
            ViewBag.ListCategory = _commonBLO.LoadComboxCategoryList();
            return View();
        }

        public JsonResult GetListRevenueOrderSalesByProduct()
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

            var category = Request?.Form["Category"];
            if (!string.IsNullOrEmpty(category))
            {
                category = category.Trim();
            }

            var setServer = Request?.Form["SetServer"];
            var setDBRead = Request?.Form["ServerRead"];

            // 19/03/2024: phân quyền theo Store

            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<RevenueOrderSalesByProductModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<RevenueOrderSalesByProductModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.GetList_Revenue_OrderSales_ByProduct(fromDate, toDate, setDBRead, storeNo, category, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<RevenueOrderSalesByProductModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcel_RevenueOrderSalesByProduct(string FromDate, string ToDate, string SetServer, string StoreNo, string Category)
        {
            DateTime fromDate = DateTime.ParseExact(FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            
            //----- 19/03/2024 : phân quyền theo store -----

            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcel_Revenue_OrderSales_ByProduct(fromDate, toDate, SetServer, StoreNo, Category);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StoreNo,
                        x.StoreName,
                        x.OrderDateStr,
                        x.MCHName1,
                        x.MCHName2,
                        x.OrderNo,
                        x.ItemNo,
                        x.Description,
                        x.ItemNoPLG,
                        x.Quantity,
                        x.UnitPrice,
                        x.AmountBeforeDiscount,
                        x.DiscountAmount,
                        x.PrepaymentAmountIncVAT,
                        x.VATAmount
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Tên ST/CH";
                    worksheet.Cells[1, 3].Value = "Ngày/Tháng";
                    worksheet.Cells[1, 4].Value = "Ngành hàng cha";
                    worksheet.Cells[1, 5].Value = "Ngành hàng con";
                    worksheet.Cells[1, 6].Value = "Số đơn hàng";
                    worksheet.Cells[1, 7].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 8].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 9].Value = "Mã tham chiếu";
                    worksheet.Cells[1, 10].Value = "Số lượng";
                    worksheet.Cells[1, 11].Value = "Đơn giá";
                    worksheet.Cells[1, 12].Value = "Tổng doanh thu";
                    worksheet.Cells[1, 13].Value = "Giảm giá";
                    worksheet.Cells[1, 14].Value = "Doanh thu sau giảm giá";
                    worksheet.Cells[1, 15].Value = "Tiền thuế GTGT";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 15])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"RevenueOrderSaleByProduct_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("BC TH phương thức thanh toán")]
        public ActionResult GeneralPaymentOrderSalesReport()
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

            var captionPayment = "PAYMENTREPORT";
            ViewBag.SetDB = listSetDB;
            ViewBag.ListPaymentType = _commonBLO.GetComboxOptionData(captionPayment);
            return View();
        }

        public JsonResult PaymentOrderSalesGeneralList()
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

            var tenderType = Request?.Form["TenderType"];
            if (!string.IsNullOrEmpty(tenderType))
            {
                tenderType = tenderType.Trim();
            }

            var setServer = Request?.Form["setServer"];
            var setDBRead = Request?.Form["serverRead"];

            // 19/03/2024 : phân quyền theo Store

            var userName = base.LoginUser.UserName;
            //var listStoreNo = new List<ArraySiteNoModel>();
            var storeNo = Request?.Form["StoreNo"];

            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<GeneralPaymentOrderSalesModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<GeneralPaymentOrderSalesModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            //if (!string.IsNullOrEmpty(storeNo))
            //{
            //    storeNo = storeNo.Trim();
            //}
            //else
            //{
            //    listStoreNo = _commonBLO.LoadStoreByUserName(setServer, storeNo, userName);
            //    string[] listStoreArray = listStoreNo.Select(a => a.SiteNo).ToArray();
            //    var listStore = "";
            //    foreach (var i in listStoreArray.ToList())
            //    {
            //        listStore += i + ";";
            //    }
            //    storeNo = listStore;
            //}

            var data = _reportBLO.PaymentOrderSalesGeneralList(fromDate, toDate, setDBRead, storeNo, tenderType, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<GeneralPaymentOrderSalesModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcel_PaymentOrderSalesGeneralList(DateTime FromDate, DateTime ToDate, string SetServer, string StoreNo, string TenderType)
        {
            // 19/03/2024 : phan quyen theo store

            var userName = base.LoginUser.UserName;
            var storeNo = "";      
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, StoreNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _reportBLO.ExportExcel_PaymentOrderSalesGeneralList(FromDate, ToDate, SetServer, storeNo, TenderType);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    var listExport = data.Select(x => new
                    {
                        x.StoreNo,
                        x.StoreName,
                        x.PaymentDateStr,  
                        x.TenderTypeName,
                        x.AmountTendered,       
                        x.CountOrder           
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Tên ST/CH";
                    worksheet.Cells[1, 3].Value = "Ngày thanh toán";
                    worksheet.Cells[1, 4].Value = "Phương thức thanh toán";
                    worksheet.Cells[1, 5].Value = "Doanh thu";
                    worksheet.Cells[1, 6].Value = "Số lượng bill";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 6])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"BC_TH_PTTT_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        [DisplayName("Báo cáo salesType")]
        public ActionResult ReportSalesType()
        {
            var result = new ViewSalesTypeModel();

            try
            {
                var siteNo = base.LoginUser.SiteCode; // Local mới có
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

                var salesOrderType = _commonBLO.LoadComboxSalesOrderType();
                result.ListStore = listStore;
                result.SetDB = listSetDB;
                result.SalesType = salesOrderType;

                var captionSaleType = "SALESTYPEREPORT";
                ViewBag.ListSalesType = _commonBLO.GetComboxOptionData(captionSaleType);

            }
            catch (Exception ex)
            {
            }
            return View(result);
        }

        [ParentAuthorize("ReportSalesType")]
        public ActionResult ReportSalesTypeLoad()
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

                var TuNgay = Request?.Form["TuNgay"];
                var DenNgay = Request?.Form["DenNgay"];
                var TenKH = Request?.Form["CusName"];
                var salesType = Request?.Form["SalesType"];
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

                var setDB = Request?.Form["SetDB"];
                var storeNo = Request?.Form["StoreNo"];

                var modelStore = string.IsNullOrEmpty(storeNo) ? new ParamJsonStoreList() : JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.StoreNo,
                        SiteName = a.StoreName
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }
                else if (storeNo == "Not")
                {
                    jsonStore = "";
                }
                else
                {
                    jsonStore = storeNo;
                }

                // 19/03/2024: phan quyen theo Store
                var userName = base.LoginUser.UserName;                
                if (!string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, modelStore.SiteNo);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<SalesTypeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<SalesTypeResponseModel>() });
                    }
                }
                //------------------------------------

                var model = new SalesTypeRequestModel
                {
                    SetDB = setDB,
                    ListStoreJson = jsonStore,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SalesType = salesType,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var result = _reportBLO.ReportSalesType(model);

                recordsTotal = (result == null || result.Count() == 0) ? 0 : (int)result.FirstOrDefault().TotalRow;
                return Json(new DataTablesViewModel<SalesTypeResponseModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<SalesTypeResponseModel> { draw = "", recordsFiltered = 0, recordsTotal = 0, data = new List<SalesTypeResponseModel>() });
            }
        }

        [ParentAuthorize("ReportSalesType")]
        public ActionResult ReportSalesTypeExcel(string setDB, string listStore, string TuNgay, string DenNgay, string salesType)
        {
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

            // 19/03/2024: phan quyen theo Store
            var userName = base.LoginUser.UserName;
            if (!string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, modelStore.SiteNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
            }

            var model = new SalesTypeRequestModel
            {
                SetDB = setDB,
                ListStoreJson = jsonStore,
                FromDate = fromDate,
                ToDate = toDate,
                SalesType = salesType,
                PageNumber = 1,
                PageSize = 1000000
            };

            var data = _reportBLO.ReportSalesType(model);

            if (data != null && data.Count > 0)
            {
                var listExport = data.Select(x => new
                {
                    x.StoreNo,
                    x.StoreName,
                    x.SalesTypeName,
                    x.SalesAmount,
                    x.TotalOrder
                }).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    worksheet.Cells[1, 1].Value = "BÁO CÁO DOANH THU THEO HÌNH THỨC BÁN HÀNG";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells["A1:E1"].Merge = true;

                    worksheet.Cells[2, 1].Value = $"TỪ NGÀY : {Convert.ToDateTime(fromDate).ToString("dd-MM-yyyy")} - ĐẾN NGÀY:{Convert.ToDateTime(toDate).ToString("dd-MM-yyyy")}";
                    worksheet.Cells[2, 1].Style.Font.Bold = true;
                    worksheet.Cells["A2:E2"].Merge = true;

                    worksheet.Cells[5, 1].Value = "Mã CH/ST";
                    worksheet.Cells[5, 2].Value = "Tên CH/ST";
                    worksheet.Cells[5, 3].Value = "Hình thức bán hàng";
                    worksheet.Cells[5, 4].Value = "Tổng doanh thu";
                    worksheet.Cells[5, 5].Value = "Tổng hóa đơn";                   

                    using (ExcelRange r = worksheet.Cells[5, 1, 5, 5])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    for (var row = 6; row <= data.Count + 6; row++)
                    {
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";                       
                    }

                    worksheet.Cells["A6"].LoadFromCollection(listExport, false).AutoFitColumns();
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"SalesType_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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


        [DisplayName("Báo cáo doanh thu chi tiết WinLife")]
        public ActionResult DetailRevenueOrderSalesReportWinLife()
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

            ViewBag.SetDB = listSetDB;
            ViewBag.ListStore = _commonBLO.LoadComboxStoreNoWinLife(base.LoginUser.UserName);
            ViewBag.ListOrderType = _commonBLO.GetOrderType();
            return View();

        }

        public JsonResult GetDetailRevenueOrderSalesReportWinLife()
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

            var transactionType = Request?.Form["TransactionType"];
            if (!string.IsNullOrEmpty(transactionType))
            {
                transactionType = transactionType.Trim();
            }

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var returnOrder = Request?.Form["ReturnOrder"];
            if (!string.IsNullOrEmpty(returnOrder))
            {
                returnOrder = returnOrder.Trim();
            }

            var chanelSales = Request?.Form["ChanelSales"];
            if (!string.IsNullOrEmpty(chanelSales))
            {
                chanelSales = chanelSales.Trim();
            }

            var vatCode = Request?.Form["VatCode"];

            // 19/03/2024: phân quyền theo Store

            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<DetailRevenueReportWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<DetailRevenueReportWinLifeResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            
            var data = _reportBLO.GetDetailRevenueReportWinLife(fromDate, toDate, storeNo, transactionType, returnOrder, textSearch, chanelSales, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<DetailRevenueReportWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcelDetailRevenueReportWinLife(string FromDate, string ToDate, string StoreNo, string TransactionType, string ReturnOrder, string TextSearch, string ChanelSales)
        {
            // 19/03/2024 : phan quyen theo store

            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, StoreNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            DateTime fromDate = DateTime.ParseExact(FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var data = _reportBLO.ExportExcelDetailRevenueReportWinLife(fromDate, toDate, storeNo, TransactionType, ReturnOrder, TextSearch, ChanelSales);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.EndingTimeStr,
                        x.TimeStr,
                        x.OrderDate,
                        x.StoreNoPLG,
                        x.StoreNoWCM,
                        x.POSTerminalNo,
                        x.OrderNo,
                        x.OrderType,        // Hinh thuc ban hang
                        x.TransactionType,  // Loai don hang
                        x.OrigOrder,        // DH goc
                        x.ItemNo,
                        x.ItemNoSAP,
                        x.Description,
                        x.Size,
                        x.CupName,
                        x.UnitOfMeasure,
                        x.Quantity,
                        x.UnitPrice,
                        x.DiscountAmount,
                        x.LineAmountBeforeVAT,
                        x.VATCode,
                        x.VATAmount,
                        x.LineAmountIncVAT,
                        x.ChanelSales
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày tạo đơn hàng";
                    worksheet.Cells[1, 2].Value = "Thời gian";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "ST/CH PLG";
                    worksheet.Cells[1, 5].Value = "ST/CH WCM";
                    worksheet.Cells[1, 6].Value = "Máy POS";
                    worksheet.Cells[1, 7].Value = "Số đơn hàng";
                    worksheet.Cells[1, 8].Value = "Hình thức bán hàng";
                    worksheet.Cells[1, 9].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 10].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 11].Value = "Mã sản phẩm PLG";
                    worksheet.Cells[1, 12].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 13].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 14].Value = "Size";
                    worksheet.Cells[1, 15].Value = "Loại ly";
                    worksheet.Cells[1, 16].Value = "ĐVT";
                    worksheet.Cells[1, 17].Value = "Số lượng";
                    worksheet.Cells[1, 18].Value = "Giá bán";
                    worksheet.Cells[1, 19].Value = "Giảm giá";
                    worksheet.Cells[1, 20].Value = "Thành tiền trước thuế";
                    worksheet.Cells[1, 21].Value = "Thuế suất GTGT (%)";
                    worksheet.Cells[1, 22].Value = "Tiền thuế GTGT";
                    worksheet.Cells[1, 23].Value = "Thành tiền sau thuế";
                    worksheet.Cells[1, 24].Value = "Kênh bán hàng";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 24])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"DetailRenvenueListWinLife_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo hình thức thanh toán WinLife")]
        public ActionResult PaymentOrderSalesReportWinLife()
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

            ViewBag.SetDB = listSetDB;
            ViewBag.ListOrderType = _commonBLO.GetOrderType();
            ViewBag.ListPaymentType = _commonBLO.GetPaymentType();
            ViewBag.ListStore = _commonBLO.LoadComboxStoreNoWinLife(base.LoginUser.UserName);
            //ViewBag.ListStore = _commonBLO.GetComboxStoreListByWinLife();
            return View();

        }

        public JsonResult GetPaymentOrderSalesListWinLife()
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

            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var transactionType = Request?.Form["TransactionType"];
            if (!string.IsNullOrEmpty(transactionType))
            {
                transactionType = transactionType.Trim();
            }

            var tenderType = Request?.Form["TenderType"];
            if (!string.IsNullOrEmpty(tenderType))
            {
                tenderType = tenderType.Trim();
            }

            var chanelSales = Request?.Form["ChanelSales"];
            if (!string.IsNullOrEmpty(chanelSales))
            {
                chanelSales = chanelSales.Trim();
            }

            // 19/03/2024: phân quyền theo Store
            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<PaymentOrderSalesWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<PaymentOrderSalesWinLifeResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
        
            var data = _reportBLO.GetPaymentOrderSalesListWinLife(fromDate, toDate, storeNo, orderNo, transactionType, tenderType, chanelSales, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<PaymentOrderSalesWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcelPaymentOrderSalesListWinLife(DateTime FromDate, DateTime ToDate, string StoreNo, string OrderNo, string TransactionType, string TenderType, string ChanelSales)
        {
            // 19/03/2024 : phan quyen theo store
            var userName = base.LoginUser.UserName;
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                storeNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, StoreNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            //------------------------------------

            var data = _reportBLO.ExportExcelPaymentOrderSalesListWinLife(FromDate, ToDate, storeNo, OrderNo, TransactionType, TenderType, ChanelSales);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OrderNo,
                        x.PaymentDay,
                        x.StoreNoPLG,
                        x.StoreNoWCM,
                        x.PosNo,
                        x.TenderType,       // Hinh thuc thanh toan
                        x.OrderType,        // Hinh thuc ban hang
                        x.TransactionType,  // Loai don hang                       
                        x.AmountTendered,   // Tong so tien
                        x.CurrencyUnit,
                        x.ReferenceNo,
                        x.ChanelSales       // Kenh ban hang
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số đơn hàng";
                    worksheet.Cells[1, 2].Value = "Ngày thanh toán";
                    worksheet.Cells[1, 3].Value = "ST/CH PLG";
                    worksheet.Cells[1, 4].Value = "ST/CH WCM";
                    worksheet.Cells[1, 5].Value = "Máy POS";
                    worksheet.Cells[1, 6].Value = "Hình thức thanh toán";
                    worksheet.Cells[1, 7].Value = "Hình thức bán hàng";
                    worksheet.Cells[1, 8].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 9].Value = "Số tiền";
                    worksheet.Cells[1, 10].Value = "Đơn vị tiền tệ";
                    worksheet.Cells[1, 11].Value = "Số tham chiếu";
                    worksheet.Cells[1, 12].Value = "Kênh bán hàng";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"PaymentOrderSalesListWinLife_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo doanh thu theo giờ")]
        public ActionResult RevenueSalesReportByHourly()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreNoByUser(base.LoginUser.UserName);
            var model = _commonBLO.LoadComboxSalesType();
            return View(model);
        }

        public JsonResult GetRevenueSalesByHourly(string StoreNo, string SalesType)
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
            var toDate = Request?.Form["ToDate"];
            var orderType = Request?.Form["OrderType"];

            //string[] listStoreArray = JsonConvert.DeserializeObject<string[]>(StoreNo);
            //var listStore = "";
            //foreach (var i in listStoreArray.ToList())
            //{
            //    listStore += i + ";";
            //}

            // 19/03/2024 : phân quyền theo Store

            var userName = base.LoginUser.UserName;
            var storeNo = "";
            var storeList = string.Join(";", JsonConvert.DeserializeObject<string[]>(StoreNo));

            if (!string.IsNullOrEmpty(storeList))
            {
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeList);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<RevenueSalesByHourlyModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<RevenueSalesByHourlyModel>() });
                }
                storeNo = storeList;
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, storeList, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            //-------------------------------

            string[] listSalesTypeArray = JsonConvert.DeserializeObject<string[]>(SalesType);
            var listSalesType = "";
            foreach (var i in listSalesTypeArray.ToList())
            {
                listSalesType += i + ";";
            }

            var data = _reportBLO.GetRevenueSalesByHourly(fromDate, toDate, ipServer, storeNo, orderType, listSalesType, out recordsTotal, pageNumber, pageSize);

            //var data = _reportBLO.GetRevenueSalesByHourly(fromDate, toDate, ipServer, listStore, orderType, listSalesType, out recordsTotal, pageNumber, pageSize);

            return Json(new DataTablesViewModel<RevenueSalesByHourlyModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcel_GetRevenueSalesByHourly(string FromDateStr, string ToDateStr, List<string> StoreNo, string OrderType, List<string> SalesType)
        {
            try
            {
                // 19/03/2024 : phan quyen theo store
                var userName = base.LoginUser.UserName;
                var storeNo = "";
                var listSite = new List<string>();
                var storeList = string.Join(";", StoreNo);

                if (!string.IsNullOrEmpty(storeList))
                {
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeList);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    }
                    listSite = listValidSite;
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, storeList, userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
                    listSite = JsonConvert.DeserializeObject<List<string>>(storeNo);                      
                }

                var data = _reportBLO.ExportExcel_GetRevenueSalesByHourly(FromDateStr, ToDateStr, ipServer, listSite, OrderType, SalesType);

                //var data = _reportBLO.ExportExcel_GetRevenueSalesByHourly(FromDateStr, ToDateStr, ipServer, storeNo, OrderType, SalesType);

                if (data != null)
                {
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                        var listExport = data.Select(x => new
                        {
                            x.StoreNo,
                            x.ForDate,
                            x.OnHourStr,
                            x.TotalAmountNET,
                            x.AmountVAT,
                            x.TotalAmountIncVAT,
                            x.TotalBill,
                            x.AtStore,
                            x.SL_AtStore,
                            x.DeliveryAmount,
                            x.SL_DeliveryAmount,
                            x.NowFood,
                            x.SL_NowFood,
                            x.GrabFood,
                            x.SL_GrabFood,
                            x.BeFood,
                            x.SL_BeFood,
                            x.Beamin,
                            x.SL_Beamin,
                            x.Gojeck,
                            x.SL_Gojeck,
                            x.WebSite,
                            x.SL_WebSite,
                            x.CallCenterAmount,
                            x.SL_CallCenterAmount,
                            x.RDAmount,
                            x.SL_RDAmount,
                            x.QAAmount,
                            x.SL_QAAmount,
                            x.EduAmount,
                            x.SL_EduAmount
                        }).ToList();

                        worksheet.Cells["A1:AD1"].Merge = true;
                        worksheet.Cells["A1:AD1"].Value = "BÁO CÁO DOANH THU THEO GIỜ";
                        worksheet.Cells["A1:AD1"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["A1:AD1"].Style.Font.Bold = true;
                        worksheet.Cells["A1:AD1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A1:AD1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["A1:AD1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["A3:A3"].Merge = true;
                        worksheet.Cells["A3:A3"].Value = "Cửa hàng (Store)";
                        worksheet.Cells["A3:A3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["A3:A3"].Style.Font.Bold = true;
                        worksheet.Cells["A3:A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A3:A3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["A3:A3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(1).Width = 17.50;

                        worksheet.Cells["B3:B3"].Merge = true;
                        worksheet.Cells["B3:B3"].Value = "Ngày (Business Day)";
                        worksheet.Cells["B3:B3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["B3:B3"].Style.Font.Bold = true;
                        worksheet.Cells["B3:B3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["B3:B3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["B3:B3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(2).Width = 18.50;

                        worksheet.Cells["C3:C3"].Merge = true;
                        worksheet.Cells["C3:C3"].Value = "Thời gian (Time)";
                        worksheet.Cells["C3:C3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["C3:C3"].Style.Font.Bold = true;
                        worksheet.Cells["C3:C3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["C3:C3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["C3:C3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(3).Width = 18.50;

                        worksheet.Cells["D3:D3"].Merge = true;
                        worksheet.Cells["D3:D3"].Value = "DT trước thuế (-VAT)";
                        worksheet.Cells["D3:D3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["D3:D3"].Style.Font.Bold = true;
                        worksheet.Cells["D3:D3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["D3:D3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["D3:D3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(4).Width = 19;

                        worksheet.Cells["E3:E3"].Merge = true;
                        worksheet.Cells["E3:E3"].Value = "Tiền thuế (VAT)";
                        worksheet.Cells["E3:E3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["E3:E3"].Style.Font.Bold = true;
                        worksheet.Cells["E3:E3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["E3:E3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["E3:E3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(5).Width = 17.50;

                        worksheet.Cells["F3:F3"].Merge = true;
                        worksheet.Cells["F3:F3"].Value = "DT sau thuế (+VAT)";
                        worksheet.Cells["F3:F3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["F3:F3"].Style.Font.Bold = true;
                        worksheet.Cells["F3:F3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["F3:F3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["F3:F3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(6).Width = 19;

                        worksheet.Cells["G3:G3"].Merge = true;
                        worksheet.Cells["G3:G3"].Value = "Tổng đơn hàng (Total Bill)";
                        worksheet.Cells["G3:G3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["G3:G3"].Style.Font.Bold = true;
                        worksheet.Cells["G3:G3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["G3:G3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["G3:G3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(7).Width = 23;

                        worksheet.Cells["H3:I3"].Merge = true;
                        worksheet.Cells["H3:I3"].Value = "Tại chỗ (Dine In)";
                        worksheet.Cells["H3:I3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["H3:I3"].Style.Font.Bold = true;
                        worksheet.Cells["H3:I3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["H3:I3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["H3:I3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["J3:K3"].Merge = true;
                        worksheet.Cells["J3:K3"].Value = "Delivery";
                        worksheet.Cells["J3:K3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["J3:K3"].Style.Font.Bold = true;
                        worksheet.Cells["J3:K3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["J3:K3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["J3:K3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["L3:M3"].Merge = true;
                        worksheet.Cells["L3:M3"].Value = "NowFood";
                        worksheet.Cells["L3:M3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["L3:M3"].Style.Font.Bold = true;
                        worksheet.Cells["L3:M3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["L3:M3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["L3:M3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["N3:O3"].Merge = true;
                        worksheet.Cells["N3:O3"].Value = "GrabFood";
                        worksheet.Cells["N3:O3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["N3:O3"].Style.Font.Bold = true;
                        worksheet.Cells["N3:O3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["N3:O3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["N3:O3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["P3:Q3"].Merge = true;
                        worksheet.Cells["P3:Q3"].Value = "BeFood";
                        worksheet.Cells["P3:Q3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["P3:Q3"].Style.Font.Bold = true;
                        worksheet.Cells["P3:Q3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["P3:Q3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["P3:Q3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["R3:S3"].Merge = true;
                        worksheet.Cells["R3:S3"].Value = "Beamin";
                        worksheet.Cells["R3:S3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["R3:S3"].Style.Font.Bold = true;
                        worksheet.Cells["R3:S3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["R3:S3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["R3:S3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["T3:U3"].Merge = true;
                        worksheet.Cells["T3:U3"].Value = "Gojeck";
                        worksheet.Cells["T3:U3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["T3:U3"].Style.Font.Bold = true;
                        worksheet.Cells["T3:U3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["T3:U3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["T3:U3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["V3:W3"].Merge = true;
                        worksheet.Cells["V3:W3"].Value = "WebSite";
                        worksheet.Cells["V3:W3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["V3:W3"].Style.Font.Bold = true;
                        worksheet.Cells["V3:W3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["V3:W3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["V3:W3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["X3:Y3"].Merge = true;
                        worksheet.Cells["X3:Y3"].Value = "CallCenter";
                        worksheet.Cells["X3:Y3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["X3:Y3"].Style.Font.Bold = true;
                        worksheet.Cells["X3:Y3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["X3:Y3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["X3:Y3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["Z3:AA3"].Merge = true;
                        worksheet.Cells["Z3:AA3"].Value = "R&D";
                        worksheet.Cells["Z3:AA3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["Z3:AA3"].Style.Font.Bold = true;
                        worksheet.Cells["Z3:AA3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["Z3:AA3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["Z3:AA3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["AB3:AC3"].Merge = true;
                        worksheet.Cells["AB3:AC3"].Value = "Q&A";
                        worksheet.Cells["AB3:AC3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["AB3:AC3"].Style.Font.Bold = true;
                        worksheet.Cells["AB3:AC3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["AB3:AC3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["AB3:AC3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["AD3:AE3"].Merge = true;
                        worksheet.Cells["AD3:AE3"].Value = "Đào tạo (Training)";
                        worksheet.Cells["AD3:AE3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["AD3:AE3"].Style.Font.Bold = true;
                        worksheet.Cells["AD3:AE3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["AD3:AE3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["AD3:AE3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));


                        // -------------------------------------------------------

                        worksheet.Cells["H4:H4"].Value = "Doanh thu";
                        worksheet.Cells["H4:H4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["H4:H4"].Style.Font.Bold = true;
                        worksheet.Cells["H4:H4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["H4:H4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["H4:H4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["I4:I4"].Value = "Đơn hàng";
                        worksheet.Cells["I4:I4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["I4:I4"].Style.Font.Bold = true;
                        worksheet.Cells["I4:I4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["I4:I4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["I4:I4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["J4:J4"].Value = "Doanh thu";
                        worksheet.Cells["J4:J4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["J4:J4"].Style.Font.Bold = true;
                        worksheet.Cells["J4:J4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["J4:J4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["J4:J4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["K4:K4"].Value = "Đơn hàng";
                        worksheet.Cells["K4:K4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["K4:K4"].Style.Font.Bold = true;
                        worksheet.Cells["K4:K4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["K4:K4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["K4:K4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["L4:L4"].Value = "Doanh thu";
                        worksheet.Cells["L4:L4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["L4:L4"].Style.Font.Bold = true;
                        worksheet.Cells["L4:L4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["L4:L4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["L4:L4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["M4:M4"].Value = "Đơn hàng";
                        worksheet.Cells["M4:M4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["M4:M4"].Style.Font.Bold = true;
                        worksheet.Cells["M4:M4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["M4:M4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["M4:M4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["N4:N4"].Value = "Doanh thu";
                        worksheet.Cells["N4:N4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["N4:N4"].Style.Font.Bold = true;
                        worksheet.Cells["N4:N4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["N4:N4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["N4:N4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["O4:O4"].Value = "Đơn hàng";
                        worksheet.Cells["O4:O4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["O4:O4"].Style.Font.Bold = true;
                        worksheet.Cells["O4:O4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["O4:O4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["O4:O4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["P4:P4"].Value = "Doanh thu";
                        worksheet.Cells["P4:P4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["P4:P4"].Style.Font.Bold = true;
                        worksheet.Cells["P4:P4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["P4:P4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["P4:P4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["Q4:Q4"].Value = "Đơn hàng";
                        worksheet.Cells["Q4:Q4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["Q4:Q4"].Style.Font.Bold = true;
                        worksheet.Cells["Q4:Q4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["Q4:Q4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["Q4:Q4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["R4:R4"].Value = "Doanh thu";
                        worksheet.Cells["R4:R4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["R4:R4"].Style.Font.Bold = true;
                        worksheet.Cells["R4:R4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["R4:R4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["R4:R4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["S4:S4"].Value = "Đơn hàng";
                        worksheet.Cells["S4:S4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["S4:S4"].Style.Font.Bold = true;
                        worksheet.Cells["S4:S4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["S4:S4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["S4:S4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["T4:T4"].Value = "Doanh thu";
                        worksheet.Cells["T4:T4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["T4:T4"].Style.Font.Bold = true;
                        worksheet.Cells["T4:T4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["T4:T4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["T4:T4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["U4:U4"].Value = "Đơn hàng";
                        worksheet.Cells["U4:U4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["U4:U4"].Style.Font.Bold = true;
                        worksheet.Cells["U4:U4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["U4:U4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["U4:U4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["V4:V4"].Value = "Doanh thu";
                        worksheet.Cells["V4:V4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["V4:V4"].Style.Font.Bold = true;
                        worksheet.Cells["V4:V4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["V4:V4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["V4:V4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["W4:W4"].Value = "Đơn hàng";
                        worksheet.Cells["W4:W4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["W4:W4"].Style.Font.Bold = true;
                        worksheet.Cells["W4:W4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["W4:W4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["W4:W4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["X4:X4"].Value = "Doanh thu";
                        worksheet.Cells["X4:X4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["X4:X4"].Style.Font.Bold = true;
                        worksheet.Cells["X4:X4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["X4:X4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["X4:X4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["Y4:Y4"].Value = "Đơn hàng";
                        worksheet.Cells["Y4:Y4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["Y4:Y4"].Style.Font.Bold = true;
                        worksheet.Cells["Y4:Y4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["Y4:Y4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["Y4:Y4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["Z4:Z4"].Value = "Doanh thu";
                        worksheet.Cells["Z4:Z4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["Z4:Z4"].Style.Font.Bold = true;
                        worksheet.Cells["Z4:Z4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["Z4:Z4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["Z4:Z4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["AA4:AA4"].Value = "Đơn hàng";
                        worksheet.Cells["AA4:AA4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["AA4:AA4"].Style.Font.Bold = true;
                        worksheet.Cells["AA4:AA4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["AA4:AA4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["AA4:AA4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["AB4:AB4"].Value = "Doanh thu";
                        worksheet.Cells["AB4:AB4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["AB4:AB4"].Style.Font.Bold = true;
                        worksheet.Cells["AB4:AB4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["AB4:AB4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["AB4:AB4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["AC4:AC4"].Value = "Đơn hàng";
                        worksheet.Cells["AC4:AC4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["AC4:AC4"].Style.Font.Bold = true;
                        worksheet.Cells["AC4:AC4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["AC4:AC4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["AC4:AC4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["AD4:AD4"].Value = "Doanh thu";
                        worksheet.Cells["AD4:AD4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["AD4:AD4"].Style.Font.Bold = true;
                        worksheet.Cells["AD4:AD4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["AD4:AD4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["AD4:AD4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["AE4:AE4"].Value = "Đơn hàng";
                        worksheet.Cells["AE4:AE4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["AE4:AE4"].Style.Font.Bold = true;
                        worksheet.Cells["AE4:AE4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["AE4:AE4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["AE4:AE4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        DataSet ds = VCM.BLUEPOS.Helpers.HelpersExtension.ToDataSet(listExport);
                        var totalRow = ds.Tables[0].Select("StoreNo is not null").Length;
                        var total = totalRow + 4;

                        // --- kẻ bảng

                        for (var i = 3; i <= total; i++) // duyệt row
                        {
                            for (var y = 1; y <= 31; y++) // duyệt cột
                            {
                                // var cell = worksheet.Cells[rowIndex, colIndex];

                                var cell = worksheet.Cells[i, y];
                                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Left.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Right.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Top.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Bottom.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            }
                        }

                        // --- tinh tong theo sales type

                        var countR = total;
                        var rowTotal = countR + 1;

                        // -----------------------------------------------

                        for (var col = 1; col < 32; col++)
                        {
                            var cell = worksheet.Cells[rowTotal, col];
                            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Top.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                        }

                        // -----------------------------------------------

                        worksheet.Cells[rowTotal, 1].Value = "Total";
                        worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                        worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A" + rowTotal + ":G" + rowTotal + ""].Merge = true;

                        worksheet.Cells[rowTotal, 8].Value = data.Sum(d => d.AtStore);
                        worksheet.Cells[rowTotal, 8].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 8].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 9].Value = data.Sum(d => d.SL_AtStore);
                        worksheet.Cells[rowTotal, 9].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 9].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 10].Value = data.Sum(d => d.DeliveryAmount);
                        worksheet.Cells[rowTotal, 10].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 10].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 11].Value = data.Sum(d => d.SL_DeliveryAmount);
                        worksheet.Cells[rowTotal, 11].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 11].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 12].Value = data.Sum(d => d.NowFood);
                        worksheet.Cells[rowTotal, 12].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 12].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 13].Value = data.Sum(d => d.SL_NowFood);
                        worksheet.Cells[rowTotal, 13].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 13].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 14].Value = data.Sum(d => d.GrabFood);
                        worksheet.Cells[rowTotal, 14].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 14].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 15].Value = data.Sum(d => d.SL_GrabFood);
                        worksheet.Cells[rowTotal, 15].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 15].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 16].Value = data.Sum(d => d.BeFood);
                        worksheet.Cells[rowTotal, 16].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 16].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 17].Value = data.Sum(d => d.SL_BeFood);
                        worksheet.Cells[rowTotal, 17].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 17].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 18].Value = data.Sum(d => d.Beamin);
                        worksheet.Cells[rowTotal, 18].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 18].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 19].Value = data.Sum(d => d.SL_Beamin);
                        worksheet.Cells[rowTotal, 19].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 19].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 20].Value = data.Sum(d => d.Gojeck);
                        worksheet.Cells[rowTotal, 20].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 20].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 21].Value = data.Sum(d => d.SL_Gojeck);
                        worksheet.Cells[rowTotal, 21].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 21].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 22].Value = data.Sum(d => d.WebSite);
                        worksheet.Cells[rowTotal, 22].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 22].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 23].Value = data.Sum(d => d.SL_WebSite);
                        worksheet.Cells[rowTotal, 23].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 23].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 24].Value = data.Sum(d => d.CallCenterAmount);
                        worksheet.Cells[rowTotal, 24].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 24].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 25].Value = data.Sum(d => d.SL_CallCenterAmount);
                        worksheet.Cells[rowTotal, 25].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 25].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 26].Value = data.Sum(d => d.RDAmount);
                        worksheet.Cells[rowTotal, 26].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 26].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 27].Value = data.Sum(d => d.SL_RDAmount);
                        worksheet.Cells[rowTotal, 27].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 27].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 28].Value = data.Sum(d => d.QAAmount);
                        worksheet.Cells[rowTotal, 28].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 28].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 29].Value = data.Sum(d => d.SL_QAAmount);
                        worksheet.Cells[rowTotal, 29].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 29].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 30].Value = data.Sum(d => d.EduAmount);
                        worksheet.Cells[rowTotal, 30].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 30].Style.Font.Bold = true;

                        worksheet.Cells[rowTotal, 31].Value = data.Sum(d => d.SL_EduAmount);
                        worksheet.Cells[rowTotal, 31].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[rowTotal, 31].Style.Font.Bold = true;


                        for (var row = 1; row <= countR + 1; row++)
                        {
                            worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 11].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 12].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 13].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 14].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 15].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 16].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 17].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 18].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 19].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 20].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 21].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 22].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 23].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 24].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 25].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 26].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 27].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 28].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 29].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 30].Style.Numberformat.Format = "#,##0";
                            worksheet.Cells[row, 31].Style.Numberformat.Format = "#,##0";
                        }

                        // ----- Fill data ------

                        worksheet.Cells["A5"].LoadFromCollection(listExport, false);

                        string fileName = $"RevenueSalesByHourly_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
            catch (Exception ex)
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [DisplayName("Cumulative Sales Reports")]
        public ActionResult CumulativeSalesReport(string channelSales)
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserChannel(base.LoginUser.UserName, channelSales);
            return View();
        }

        public JsonResult GetCumulativeSalesList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];

            var fromDateSelected = Request?.Form["FromDateSelected"];
            var toDateSelected = Request?.Form["ToDateSelected"];

            var fromDateComparison = Request?.Form["FromDateComparison"];
            var toDateComparison = Request?.Form["ToDateComparison"];
            var channel = Request?.Form["Channels"];

            //var storeNo = Request?.Form["StoreNo"];
            //var listStore = "";
            //if (storeNo != null)
            //{
            //    string[] listStoreArray = JsonConvert.DeserializeObject<string[]>(storeNo);                
            //    foreach (var i in listStoreArray.ToList())
            //    {
            //        listStore += i + ";";
            //    }
            //}
            //else
            //{
            //    listStore = "";
            //}

            // 19/03/2024 : phân quyền theo Store

            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["StoreNo"];
            var listStore = "";
            var storeList = string.Join(";", JsonConvert.DeserializeObject<string[]>(storeNo));

            if (!string.IsNullOrEmpty(storeList))
            {
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeList);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<CumulativeSalesResponseModel> { draw = draw.ToString(), data = new List<CumulativeSalesResponseModel>() });
                }
                listStore = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, storeList, userName);
                listStore = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            //---------------------------------------

            var data = _reportBLO.GetCumulativeSalesList(fromDateSelected, toDateSelected, fromDateComparison, toDateComparison, ipServer, channel, listStore);
            return Json(new DataTablesViewModel<CumulativeSalesResponseModel> { draw = draw.ToString(), data = data });

        }

        public ActionResult ExportExcel_CumulativeSalesList(string FromDateBySelected, string ToDateBySelected, string FromDateByComparison, string ToDateByComparison, string Channels, List<string> StoreNo)
        {
            try
            {
                // 19/03/2024 : phan quyen theo store
                var userName = base.LoginUser.UserName;
                var storeNo = "";
                var listStore = new List<string>();
                var storeList = "";

                if (StoreNo != null)
                {
                    storeList = string.Join(";", StoreNo);
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeList);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    }
                    listStore = listValidSite; 
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, "", userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
                    listStore = listStoreNo.Select(a => a.SiteNo).ToList();                    
                }

                var data = _reportBLO.ExportExcel_CumulativeSalesList(FromDateBySelected, ToDateBySelected, FromDateByComparison, ToDateByComparison, ipServer, Channels, listStore);

                //var data = _reportBLO.ExportExcel_CumulativeSalesList(FromDateBySelected, ToDateBySelected, FromDateByComparison, ToDateByComparison, ipServer, Channels, StoreNo);

                if (data != null)
                {
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                        var listExport = data.Select(x => new
                        {
                            x.SelectedDate,
                            x.ComparisonDate,
                            x.OnHourForSelected,
                            x.OnHourForComparison,

                            x.TotalSalesBySelectedDate,
                            x.TotalAmountNETSelectedDate, // doanh thu truoc thue (-VAT)
                            x.CumulativeSalesBySelectedDate,
                            x.PercentBySelectedDate,

                            x.TotalSalesDineInSelectedDate,
                            x.PercentTotalSalesDineInSelectedDate,
                            x.TotalSalesDeliverySelectedDate,
                            x.PercentTotalSalesDeliverySelectedDate,

                            x.TotalSalesByComparisonDate,
                            x.TotalAmountNETComparisonDate, // doanh thu truoc thue (-VAT)
                            x.CumulativeSalesByComparisonDate,
                            x.PercentByComparisonDate,

                            x.TotalSalesDineInComparisonDate,
                            x.PercentTotalSalsesDineInComparisonDate,
                            x.TotalSalesDeliveryComparisonDate,
                            x.PercentTotalSalesDeliveryComparisonDate

                        }).ToList();

                        var listData = listExport.Select(a => new
                        {
                            a.OnHourForSelected,
                            a.OnHourForComparison,

                            //a.TotalAmountIncVATSelectedDate,
                            a.TotalAmountNETSelectedDate,
                            a.CumulativeSalesBySelectedDate,
                            a.PercentBySelectedDate,

                            a.TotalSalesDineInSelectedDate,
                            a.PercentTotalSalesDineInSelectedDate,
                            a.TotalSalesDeliverySelectedDate,
                            a.PercentTotalSalesDeliverySelectedDate,

                            //a.TotalAmountIncVATComparisonDate,
                            a.TotalAmountNETComparisonDate,
                            a.CumulativeSalesByComparisonDate,
                            a.PercentByComparisonDate,

                            a.TotalSalesDineInComparisonDate,
                            a.PercentTotalSalsesDineInComparisonDate,
                            a.TotalSalesDeliveryComparisonDate,
                            a.PercentTotalSalesDeliveryComparisonDate

                        }).ToList();

                        //var sumSalesBySelectedDate = listExport.Sum(d =>d.TotalAmountIncVATSelectedDate);
                        //var sumSalesByComparisonDate = listExport.Sum(d =>d.TotalAmountIncVATComparisonDate);

                        var sumSalesBySelectedDate = listExport.Sum(d =>d.TotalAmountNETSelectedDate);
                        var sumSalesByComparisonDate = listExport.Sum(d =>d.TotalAmountNETComparisonDate);

                        var listDataGroupHeader = listExport.Select(a => new ExportExcel_CumulativeSalesGroupHeaderModel()
                        {
                            //SelectedDateStr = a.SelectedDate + " - " + a.SelectedDate,
                            //ComparisonDateStr = a.ComparisonDate + " - "+ a.ComparisonDate,

                            SelectedDateStr = a.SelectedDate,
                            ComparisonDateStr = a.ComparisonDate,

                            TotalSalesBySelectedDate = sumSalesBySelectedDate,
                            CumulativeSalesBySelectedDateStr = 0,
                            PercentBySelectedDateStr = 0,
                            TotalSalesDineInSelectedDateStr = 0,
                            PercentTotalSalesDineInSelectedDateStr = 0,
                            TotalSalesDeliverySelectedDateStr = 0,
                            PercentTotalSalesDeliverySelectedDateStr = 0,

                            TotalSalesByComparisonDate = sumSalesByComparisonDate,
                            CumulativeSalesByComparisonDateStr = 0,
                            PercentByComparisonDateStr = 0,
                            TotalSalesDineInComparisonDateStr = 0,
                            PercentTotalSalsesDineInComparisonDateStr = 0,
                            TotalSalesDeliveryComparisonDateStr = 0,
                            PercentTotalSalesDeliveryComparisonDateStr = 0

                        }).ToList();

                        worksheet.Cells["A1:Q1"].Merge = true;
                        worksheet.Cells["A1:Q1"].Value = "CUMULATIVE SALES REPORTS";
                        worksheet.Cells["A1:Q1"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["A1:Q1"].Style.Font.Bold = true;
                        worksheet.Cells["A1:Q1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A1:Q1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["A1:Q1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["B3:C3"].Merge = true;
                        worksheet.Cells["B3:C3"].Value = "Date";
                        worksheet.Cells["B3:C3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["B3:C3"].Style.Font.Bold = true;
                        worksheet.Cells["B3:C3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["B3:C3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["B3:C3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        //worksheet.Column(2).Width = 17.50;

                        worksheet.Cells["D3:J3"].Merge = true;
                        worksheet.Cells["D3:J3"].Value = "Selected date";
                        worksheet.Cells["D3:J3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["D3:J3"].Style.Font.Bold = true;
                        worksheet.Cells["D3:J3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["D3:J3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["D3:J3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        //worksheet.Column(2).Width = 18.50;

                        worksheet.Cells["K3:Q3"].Merge = true;
                        worksheet.Cells["K3:Q3"].Value = "Comparison date";
                        worksheet.Cells["K3:Q3"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["K3:Q3"].Style.Font.Bold = true;
                        worksheet.Cells["K3:Q3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["K3:Q3"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["K3:Q3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        //worksheet.Column(3).Width = 18.50;

                        worksheet.Cells["B4:B5"].Merge = true;
                        worksheet.Cells["B4:B5"].Value = "Selected date";
                        worksheet.Cells["B4:B5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["B4:B5"].Style.Font.Bold = true;
                        worksheet.Cells["B4:B5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["B4:B5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["B4:B5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(2).Width = 25;

                        worksheet.Cells["C4:C5"].Merge = true;
                        worksheet.Cells["C4:C5"].Value = "Comparison date";
                        worksheet.Cells["C4:C5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["C4:C5"].Style.Font.Bold = true;
                        worksheet.Cells["C4:C5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["C4:C5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["C4:C5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(3).Width = 25;

                        worksheet.Cells["D4:D5"].Merge = true;
                        worksheet.Cells["D4:D5"].Value = "Total sales (-VAT)";
                        worksheet.Cells["D4:D5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["D4:D5"].Style.Font.Bold = true;
                        worksheet.Cells["D4:D5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["D4:D5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["D4:D5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(4).Width = 15;

                        worksheet.Cells["E4:E5"].Merge = true;
                        worksheet.Cells["E4:E5"].Value = "Cumulative sales";
                        worksheet.Cells["E4:E5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["E4:E5"].Style.Font.Bold = true;
                        worksheet.Cells["E4:E5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["E4:E5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["E4:E5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(5).Width = 15;

                        worksheet.Cells["F4:F5"].Merge = true;
                        worksheet.Cells["F4:F5"].Value = "%";
                        worksheet.Cells["F4:F5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["F4:F5"].Style.Font.Bold = true;
                        worksheet.Cells["F4:F5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["F4:F5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["F4:F5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(6).Width = 15;

                        worksheet.Cells["G4:H4"].Merge = true;
                        worksheet.Cells["G4:H4"].Value = "Dine-in total sales";
                        worksheet.Cells["G4:H4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["G4:H4"].Style.Font.Bold = true;
                        worksheet.Cells["G4:H4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["G4:H4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["G4:H4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["I4:J4"].Merge = true;
                        worksheet.Cells["I4:J4"].Value = "Delivery total sales";
                        worksheet.Cells["I4:J4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["I4:J4"].Style.Font.Bold = true;
                        worksheet.Cells["I4:J4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["I4:J4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["I4:J4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["K4:K5"].Merge = true;
                        worksheet.Cells["K4:K5"].Value = "Total sales (-VAT)";
                        worksheet.Cells["K4:K5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["K4:K5"].Style.Font.Bold = true;
                        worksheet.Cells["K4:K5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["K4:K5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["K4:K5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(11).Width = 15;

                        worksheet.Cells["L4:L5"].Merge = true;
                        worksheet.Cells["L4:L5"].Value = "Cumulative sales";
                        worksheet.Cells["L4:L5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["L4:L5"].Style.Font.Bold = true;
                        worksheet.Cells["L4:L5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["L4:L5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["L4:L5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(12).Width = 15;

                        worksheet.Cells["M4:M5"].Merge = true;
                        worksheet.Cells["M4:M5"].Value = "%";
                        worksheet.Cells["M4:M5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["M4:M5"].Style.Font.Bold = true;
                        worksheet.Cells["M4:M5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["M4:M5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["M4:M5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(13).Width = 15;

                        worksheet.Cells["N4:O4"].Merge = true;
                        worksheet.Cells["N4:O4"].Value = "Dine-in total sales";
                        worksheet.Cells["N4:O4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["N4:O4"].Style.Font.Bold = true;
                        worksheet.Cells["N4:O4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["N4:O4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["N4:O4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        worksheet.Cells["P4:Q4"].Merge = true;
                        worksheet.Cells["P4:Q4"].Value = "Delivery total sales";
                        worksheet.Cells["P4:Q4"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["P4:Q4"].Style.Font.Bold = true;
                        worksheet.Cells["P4:Q4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["P4:Q4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["P4:Q4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));

                        // -------------------------------------------------------

                        worksheet.Cells["G5:G5"].Value = "Sales";
                        worksheet.Cells["G5:G5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["G5:G5"].Style.Font.Bold = true;
                        worksheet.Cells["G5:G5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["G5:G5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["G5:G5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(7).Width = 15;

                        worksheet.Cells["H5:H5"].Value = "%";
                        worksheet.Cells["H5:H5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["H5:H5"].Style.Font.Bold = true;
                        worksheet.Cells["H5:H5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["H5:H5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["H5:H5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(8).Width = 15;

                        worksheet.Cells["I5:I5"].Value = "Sales";
                        worksheet.Cells["I5:I5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["I5:I5"].Style.Font.Bold = true;
                        worksheet.Cells["I5:I5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["I5:I5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["I5:I5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(9).Width = 15;

                        worksheet.Cells["J5:J5"].Value = "%";
                        worksheet.Cells["J5:J5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["J5:J5"].Style.Font.Bold = true;
                        worksheet.Cells["J5:J5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["J5:J5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["J5:J5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(10).Width = 15;

                        worksheet.Cells["N5:N5"].Value = "Sales";
                        worksheet.Cells["N5:N5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["N5:N5"].Style.Font.Bold = true;
                        worksheet.Cells["N5:N5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["N5:N5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["N5:N5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(14).Width = 15;

                        worksheet.Cells["O5:O5"].Value = "%";
                        worksheet.Cells["O5:O5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["O5:O5"].Style.Font.Bold = true;
                        worksheet.Cells["O5:O5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["O5:O5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["O5:O5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(15).Width = 15;

                        worksheet.Cells["P5:P5"].Value = "Sales";
                        worksheet.Cells["P5:P5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["P5:P5"].Style.Font.Bold = true;
                        worksheet.Cells["P5:P5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["P5:P5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["P5:P5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(16).Width = 15;

                        worksheet.Cells["Q5:Q5"].Value = "%";
                        worksheet.Cells["Q5:Q5"].Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        worksheet.Cells["Q5:Q5"].Style.Font.Bold = true;
                        worksheet.Cells["Q5:Q5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["Q5:Q5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells["Q5:Q5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#f0f0f0"));
                        worksheet.Column(17).Width = 15;

                        //DataSet ds = VCM.BLUEPOS.Helpers.HelpersExtension.ToDataSet(listExport);
                        //var totalRow = ds.Tables[0].Select("StoreNo is not null").Length;
                        //var totalRow = ds.Tables[0].Select("SelectedDate is not null").Length;

                        DataSet ds = VCM.BLUEPOS.Helpers.HelpersExtension.ToDataSet(listData);
                        var totalRow = ds.Tables[0].Select("OnHourForSelected is not null").Length;
                        var total = totalRow + 6;

                        // --- Kẻ bảng

                        for (var i = 3; i <= total; i++)  // duyệt row
                        {
                            for (var y = 2; y <= 17; y++) // duyệt column
                            {
                                // var cell = worksheet.Cells[rowIndex, colIndex];

                                var cell = worksheet.Cells[i, y];
                                cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Left.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                                cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Right.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                                cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Top.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                                cell.Style.Border.Bottom.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            }
                        }

                        // var countR = total;
                        // var rowTotal = countR + 1;

                        // -----------------------------------------------

                        for (var col = 2; col < 18; col++)
                        {
                            var cell = worksheet.Cells[6, col];
                            cell.Style.Font.Bold = true;

                            cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Left.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Right.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Top.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            cell.Style.Border.Bottom.Color.SetColor(ColorTranslator.FromHtml("#000000"));

                        }

                        // ----- Fill data ------

                        worksheet.Cells["B6"].LoadFromCollection(listDataGroupHeader, false);
                        worksheet.Cells["B7"].LoadFromCollection(listData, false);

                        string fileName = $"CumulativeSalesReport_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
            catch (Exception ex)
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [DisplayName("Báo cáo doanh thu theo cửa hàng WinLife")]
        public ActionResult RevenueOrderSalesByStoreWinLife()
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

            ViewBag.SetDB = listSetDB;
            ViewBag.ListStore = _commonBLO.LoadComboxStoreNoWinLife(base.LoginUser.UserName);
            return View();
        }
        public ActionResult GetRevenueOrderSalesByStoreWinLife()
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
                var pageNumber = skip / pageSize;
                var recordsTotal = 0;

                var fromDate = DateTime.Now;
                var toDate = DateTime.Now;

                if (!string.IsNullOrEmpty(Request?.Form["FromDate"]))
                {
                    fromDate = Convert.ToDateTime(Request?.Form["FromDate"]);
                }

                if (!string.IsNullOrEmpty(Request?.Form["ToDate"]))
                {
                    toDate = Convert.ToDateTime(Request?.Form["ToDate"]);
                }

                // 19/03/2024: phân quyền theo Store
                var userName = base.LoginUser.UserName;
                var storeNo = Request?.Form["StoreNo"];

                if (!string.IsNullOrEmpty(storeNo))
                {
                    storeNo = storeNo.Trim();
                    var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                    if (listValidSite == null | listValidSite.Count == 0)
                    {
                        return Json(new DataTablesViewModel<RevenueOrderSalesByStoreWinLifeModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<RevenueOrderSalesByStoreWinLifeModel>() });
                    }
                    storeNo = string.Join(";", listValidSite);
                }
                else
                {
                    var listStoreNo = _commonBLO.LoadComboxStoreNoWinLife(userName);
                    storeNo = string.Join(";", listStoreNo.Select(a => a.StoreNoPLH));
                }

                // -------------------------------

                var data = _reportBLO.GetRevenueOrderSalesByStoreWinLife(storeNo, fromDate, toDate, out recordsTotal, pageNumber, pageSize);
                return Json(new DataTablesViewModel<RevenueOrderSalesByStoreWinLifeModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

            }
            catch (Exception ex)
            {
                return Json(new List<RevenueOrderSalesByStoreWinLifeModel>());
            }
        }

        public ActionResult ExportExcel_RevenueOrderSalesByStoreWinLife(string StoreNo, string TuNgay, string DenNgay)
        {
            DateTime fDate = DateTime.ParseExact(TuNgay, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime tDate = DateTime.ParseExact(DenNgay, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            // 19/03/2024 : check store xem có đúng là store đã được phân quyền không ?
            var storeNo = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                StoreNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(base.LoginUser.UserName, StoreNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadComboxStoreNoWinLife(base.LoginUser.UserName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.StoreNoPLH));
            }

            //-----------------------------------------

            var data = new List<ExportExcel_RevenueOrderSalesByStoreWinLifeModel>();

            var dataResult = _reportBLO.ExportExcel_GetRevenueOrderSalesByStoreWinLife(storeNo, fDate, tDate);

            data = dataResult.Select(a => new ExportExcel_RevenueOrderSalesByStoreWinLifeModel
            {
                StoreNo = a.StoreNo,
                StoreName = a.StoreName,
                BussinessDate = a.BussinessDate,
                CountOrderTotal = a.CountOrderTotal,
                AmountTotal = a.SauVAT,
                TotalTruocVAT = a.TruocVAT,
                TotalVAT = a.ThueVAT
            }).ToList();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells[1, 1].Value = "Mã Cửa hàng";
                    worksheet.Cells[1, 2].Value = "Tên Cửa hàng";
                    worksheet.Cells[1, 3].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 4].Value = "Tổng số hóa đơn";
                    worksheet.Cells[1, 5].Value = "Doanh thu trước thuế";
                    worksheet.Cells[1, 6].Value = "Tiền thuế VAT";
                    worksheet.Cells[1, 7].Value = "Doanh thu sau thuế";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 7])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    var countR = data.Count;
                    var rowTotal = countR + 2;

                    worksheet.Cells[rowTotal, 1].Value = "Total";
                    worksheet.Cells[rowTotal, 1].Style.Font.Bold = true;
                    worksheet.Cells[rowTotal, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells["A" + rowTotal + ":C" + rowTotal + ""].Merge = true;

                    worksheet.Cells[rowTotal, 4].Value = data.Sum(d => d.CountOrderTotal);
                    worksheet.Cells[rowTotal, 4].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 4].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 5].Value = data.Sum(d => d.TotalTruocVAT);
                    worksheet.Cells[rowTotal, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 5].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 6].Value = data.Sum(d => d.TotalVAT);
                    worksheet.Cells[rowTotal, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 6].Style.Font.Bold = true;

                    worksheet.Cells[rowTotal, 7].Value = data.Sum(d => d.AmountTotal);
                    worksheet.Cells[rowTotal, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[rowTotal, 7].Style.Font.Bold = true;

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 3].Style.Numberformat.Format = "dd/MM/yyyy";
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                    }

                    string fileName = $"RevenueOrderSalesByStoreWinLife_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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


        //tungnt8: 11/02/2025, báo cáo chi tiết khuyến mãi (Kế toán đề xuất)

        [DisplayName("Báo cáo chi tiết khuyến mãi")]
        public ActionResult ReportSalesDetailPromotion()
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

            var captionOrderType = "ORDERTYPE";
            var captionSaleType = "SALESTYPEREPORT";
            //var captionSourceBill = "SOURCEBILL";

            ViewBag.SetDB = listSetDB;
            ViewBag.ListOrderType = _commonBLO.GetComboxOptionData(captionOrderType);
            ViewBag.ListSalesType = _commonBLO.GetComboxOptionData(captionSaleType);
            //ViewBag.ListSourceBill = _commonBLO.GetComboxOptionData(captionSourceBill);
            return View();
        }
        public JsonResult GetSalesDetailPromotion()
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

            var orderType = Request?.Form["orderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var salesType = Request?.Form["salesType"];
            if (!string.IsNullOrEmpty(salesType))
            {
                salesType = salesType.Trim();
            }

            var textSearch = Request?.Form["textSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var setServer = Request?.Form["setServer"];
            var setDBRead = Request?.Form["serverRead"];
            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["storeNo"];

            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<OrderListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<OrderListResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            var data = _reportBLO.GetSalesDetailPromotion(fromDate, toDate, setDBRead, storeNo, orderType, salesType, textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<SalesDetailPromotionModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }




    }
}