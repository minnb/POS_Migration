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
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.CrownX;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Business.CrownX;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class CrownXController : BaseController
    {
        private ICrownXBLO _crownXBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        public CrownXController (ICrownXBLO crownXBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _crownXBLO = crownXBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }

        [DisplayName("Báo cáo tích/tiêu CrownX/PLG")]
        public ActionResult TransactionDataCrownXPOS()
        {
            var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            var listSetDB = listStoreByUser
                .GroupBy(d => new {d.ServerIP, d.ServerIPRead, d.ServerStoreDesc})
                .Select(x =>x.First()).Select(a => new SQLServerModel
                {
                    IPServer = a.ServerIP,
                    IPServerRead = a.ServerIPRead,
                    Description = a.ServerStoreDesc
                }).ToList();

            ViewBag.SetDB = listSetDB; 
            return View();
        }

        [HttpPost]
        public JsonResult GetTransPointLine()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageIndex = skip/pageSize;
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

            var posTerminal = Request?.Form["PosTerminal"];
            if (!string.IsNullOrEmpty(posTerminal))
            {
                posTerminal = posTerminal.Trim();
            }

            var orderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var textSearch = Request?.Form["CardNumber"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var setServer = Request?.Form["SetServer"];
            var setDBRead = Request?.Form["ServerRead"];

            // 19/03/2024 :: phan quyen theo Store

            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["storeNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<TransPointLineResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<TransPointLineResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            var data = _crownXBLO.GetTransPointLine(fromDate, toDate, setDBRead, storeNo, posTerminal, orderType, textSearch, orderNo, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<TransPointLineResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        //16/10/2024: Show popup thông tin hoi vien
        [ParentAuthorize("OrderList")]
        public PartialViewResult GetPopupMemberCard(string StoreNo, string OrderNo)
        {
            try
            {
                var data = _crownXBLO.GetPopupMemberCard(StoreNo, OrderNo);
                if (data != null)
                {
                    return this.PartialView(data);
                }
                else
                {
                    return this.PartialView(new List<POPupMemberCardModel>());
                }
            }
            catch
            {
                return this.PartialView(new List<POPupMemberCardModel>());
            }
        }
        public ActionResult ExportExcelGetTransPointLine(DateTime FromDate, DateTime ToDate, string SetServer, string StoreNo, string PosTerminal, string OrderType, string CardNumber, string OrderNo)
        {
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

            var data = _crownXBLO.ExportExcelGetTransPointLine(FromDate, ToDate, SetServer, storeNo, PosTerminal, OrderType, CardNumber, OrderNo);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OrderDateStr,
                        x.OrderNo,
                        x.OrderType,
                        x.OrigOrder,
                        x.StoreNo,
                        x.PosTerminal,
                        x.MemberName,
                        x.MemberNumber,
                        x.CardLevel,
                        x.EarnPoints,
                        x.RedeemPoints
                    }).ToList();

                    worksheet.Cells[1, 1].Value  = "Ngày";
                    worksheet.Cells[1, 2].Value  = "Số đơn hàng";
                    worksheet.Cells[1, 3].Value  = "Loại giao dịch";
                    worksheet.Cells[1, 4].Value  = "Số đơn hàng gốc";
                    worksheet.Cells[1, 5].Value  = "Mã cửa hàng";
                    worksheet.Cells[1, 6].Value  = "Máy POS";
                    worksheet.Cells[1, 7].Value  = "Tên khách hàng";
                    worksheet.Cells[1, 8].Value  = "Số thẻ Phúc Long";
                    worksheet.Cells[1, 9].Value = "Hạng thẻ";
                    worksheet.Cells[1, 10].Value = "Điểm tích";
                    worksheet.Cells[1, 11].Value = "Điểm tiêu";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 11])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"BaocaotichtieuPLH_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo tổng hợp tích/tiêu tem")]
        public ActionResult TransactionByMemberStamp()
        {
            var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            var listSetDB = listStoreByUser
                .GroupBy(d => new {d.ServerIP, d.ServerIPRead, d.ServerStoreDesc})
                .Select(x =>x.First()).Select(a => new SQLServerModel
                {
                    IPServer = a.ServerIP,
                    IPServerRead = a.ServerIPRead,
                    Description = a.ServerStoreDesc
                }).ToList();

            var captionOrderType = "ORDERTYPE";
            ViewBag.SetDB = listSetDB;
            ViewBag.ListOrderType = _commonBLO.GetComboxOptionData(captionOrderType);
            return View();

        }
        public JsonResult GetTransactionByMemberStamp()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageIndex = skip/pageSize;
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

            var posTerminal = Request?.Form["POSTerminal"];
            if (!string.IsNullOrEmpty(posTerminal))
            {
                posTerminal = posTerminal.Trim();
            }

            var orderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var setServer = Request?.Form["SetServer"];
            var setDBRead = Request?.Form["ServerRead"];

            // 19/03/2024 :: phan quyen theo Store

            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["storeNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<TransPointLineResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<TransPointLineResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _crownXBLO.GetTransactionByMemberStamp(fromDate, toDate, setDBRead, storeNo, posTerminal, orderType, textSearch, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<TransactionByMemberStampModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcel_GetTransactionByMemberStamp(DateTime FromDate, DateTime ToDate, string SetServer, string StoreNo, string POSTerminal, string OrderType, string TextSearch)
        {
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

            var data = _crownXBLO.ExportExcel_GetTransactionByMemberStamp(FromDate, ToDate, SetServer, storeNo, POSTerminal, OrderType, TextSearch);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

                    var listExport = data.Select(x => new
                    {
                        x.BusinessDate,
                        x.OrderNo,
                        x.OrderType,
                        x.ReturnedOrderNo,
                        x.StoreNo,
                        x.POSTerminal,
                        x.MemberCard,
                        x.QuantityEarn,
                        x.QuantityRedeem
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "Số đơn hàng";
                    worksheet.Cells[1, 3].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 4].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 5].Value = "Cửa hàng";
                    worksheet.Cells[1, 6].Value = "Máy POS";
                    worksheet.Cells[1, 7].Value = "Số ĐT hội viên";
                    worksheet.Cells[1, 8].Value = "Tích tem";
                    worksheet.Cells[1, 9].Value = "Tiêu tem";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 9])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"ReportTransactionByStamp_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        [DisplayName("Báo cáo chi tiết tích tem")]
        public ActionResult DetailTransactionByMemberEarnStamp()
        {
            var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            var listSetDB = listStoreByUser
                .GroupBy(d => new {d.ServerIP, d.ServerIPRead, d.ServerStoreDesc})
                .Select(x =>x.First()).Select(a => new SQLServerModel
                {
                    IPServer = a.ServerIP,
                    IPServerRead = a.ServerIPRead,
                    Description = a.ServerStoreDesc
                }).ToList();

            var captionOrderType = "ORDERTYPE";
            ViewBag.SetDB = listSetDB;
            ViewBag.ListOrderType = _commonBLO.GetComboxOptionData(captionOrderType);
            return View();
        }

        public JsonResult GetDetailMemberEarnTransactionLine()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageIndex = skip/pageSize;
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
            var posTerminal = Request?.Form["POSTerminal"];
            if (!string.IsNullOrEmpty(posTerminal))
            {
                posTerminal = posTerminal.Trim();
            }
            var OrderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(OrderType))
            {
                OrderType = OrderType.Trim();
            }
            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }
            var setServer = Request?.Form["SetServer"];
            var setDBRead = Request?.Form["ServerRead"];

            // 19/03/2024 :: phan quyen theo Store
            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["storeNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<TransPointLineResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<TransPointLineResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
            var data = _crownXBLO.GetDetailMemberEarnTransactionLine(fromDate, toDate, setDBRead, storeNo, posTerminal, OrderType, textSearch, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<DetailMemberEarnTransactionLineModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }
        public ActionResult ExportExcel_DetailGetDetailMemberEarnTransactionLine(DateTime FromDate, DateTime ToDate, string SetServer, string StoreNo, string POSTerminal, string OrderType, string TextSearch)
        {
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

            var data = _crownXBLO.ExportExcel_DeatilGetDetailMemberEarnTransactionLine(FromDate, ToDate, SetServer, storeNo, POSTerminal, OrderType, TextSearch);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.BusinessDate,
                        x.OrderNo,
                        x.OrderType,
                        x.ReturnedOrderNo,
                        x.StoreNo,
                        x.POSTerminal,
                        x.MemberCard,
                        x.ItemNo,
                        x.ItemName,
                        x.Uom,
                        x.SaleQty,
                        x.PointEarn
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày";
                    worksheet.Cells[1, 2].Value = "Số đơn hàng";
                    worksheet.Cells[1, 3].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 4].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 5].Value = "Cửa hàng";
                    worksheet.Cells[1, 6].Value = "Máy POS";
                    worksheet.Cells[1, 7].Value = "Số ĐT hội viên";
                    worksheet.Cells[1, 8].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 9].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 10].Value = "ĐVT";
                    worksheet.Cells[1, 11].Value = "Số lượng";
                    worksheet.Cells[1, 12].Value = "Tích tem";
 
                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"DetailMemberEarnTransactionLine_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Báo cáo hạn mức sử dụng của Masaner")]
        public ActionResult CheckStaffMemberRemn()
        {
            return View();
        }
        public JsonResult GetStaffMemberRemnList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageIndex = skip / pageSize;
            var recordsTotal = 0;

            var monthyear = "";
            if (!string.IsNullOrEmpty(Request?.Form["MonthYear"]))
            {
                var month = Request?.Form["MonthYear"].Split('-')[0];
                var year = Request?.Form["MonthYear"].Split('-')[1];
                monthyear = $"{year}{month}";
            }
            var compCode = Request?.Form["CompCode"];
            if (!string.IsNullOrEmpty(compCode))
            {
                compCode = compCode.Trim();
            }
            var status = Request?.Form["StaffStatus"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }
            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }
            var data = _crownXBLO.GetStaffMemberRemnList(monthyear, compCode, textSearch, status, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<StaffMemberRemnResponeModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [DisplayName("Báo cáo đơn hàng khuyến mãi Masaner")]
        public ActionResult ReportStaffMemberRemnList()
        {
            var listStoreByUser = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            var listSetDB = listStoreByUser
                .GroupBy(d => new { d.ServerIP, d.ServerIPRead, d.ServerStoreDesc })
                .Select(x => x.First()).Select(a => new SQLServerModel
                {
                    IPServer = a.ServerIP,
                    IPServerRead = a.ServerIPRead,
                    Description = a.ServerStoreDesc
                }).ToList();

            var captionOrderType = "ORDERTYPE";
            ViewBag.SetDB = listSetDB;
            ViewBag.ListOrderType = _commonBLO.GetComboxOptionData(captionOrderType);
            return View();
        }

        public JsonResult GetReportStaffMemberRemnList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageIndex = skip / pageSize;
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
            var posTerminal = Request?.Form["POSTerminal"];
            if (!string.IsNullOrEmpty(posTerminal))
            {
                posTerminal = posTerminal.Trim();
            }
            var OrderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(OrderType))
            {
                OrderType = OrderType.Trim();
            }
            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }
            var setServer = Request?.Form["SetServer"];
            var setDBRead = Request?.Form["ServerRead"];

            // 19/03/2024 :: phan quyen theo Store
            var userName = base.LoginUser.UserName;
            var storeNo = Request?.Form["storeNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<TransPointLineResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<TransPointLineResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _crownXBLO.GetReportStaffMemberRemnList(fromDate, toDate, setDBRead, storeNo, posTerminal, OrderType, textSearch, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<ReportStaffMemberRemnModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public JsonResult GetDetailOfferStaffTransactionList()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageIndex = skip / pageSize;
            var recordsTotal = 0;
           
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var data = _crownXBLO.GetDetailOfferStaffTransactionList("", storeNo, orderNo, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<DetailReportStaffMemberRemnModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcel_ReportOfferStaffTransaction(DateTime FromDate, DateTime ToDate, string SetServer, string StoreNo, string POSTerminal, string OrderType, string TextSearch)
        {
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

            var data = _crownXBLO.ExportExcel_GetReportStaffMemberRemnList(FromDate, ToDate, SetServer, storeNo, POSTerminal, OrderType, TextSearch);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.CrtDateStr,
                        x.TimeDateStr,
                        x.StoreNo,
                        x.PosNo,
                        x.OrderNo,
                        x.OrderType,
                        x.ReturnedOrderNo,
                        x.AmountInclVAT,
                        x.StaffCode,
                        x.PhoneNumber,
                        x.ClubCode,
                        x.CashierID
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 2].Value = "Thời gian";
                    worksheet.Cells[1, 3].Value = "Cửa hàng";
                    worksheet.Cells[1, 4].Value = "Máy POS";
                    worksheet.Cells[1, 5].Value = "Số đơn hàng";
                    worksheet.Cells[1, 6].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 7].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 8].Value = "Tổng số tiền";
                    worksheet.Cells[1, 9].Value = "Mã nhân viên";
                    worksheet.Cells[1, 10].Value = "Số thẻ khách hàng";
                    worksheet.Cells[1, 11].Value = "Hội viên";
                    worksheet.Cells[1, 12].Value = "Thu ngân";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"BaocaodonhangKMHVMSN_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public JsonResult GetOrderListByStaffRemn()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var skip = start != null ? Convert.ToInt32(start) : 1;
            var pageIndex = skip / pageSize;
            var recordsTotal = 0;

            var monthyear = "";
            if (!string.IsNullOrEmpty(Request?.Form["MonthYear"]))
            {
                var month = Request?.Form["MonthYear"].Split('-')[0];
                var year = Request?.Form["MonthYear"].Split('-')[1];
                monthyear = $"{year}{month}";
            }

            var staffCode = Request?.Form["StaffCode"];
            if (!string.IsNullOrEmpty(staffCode))
            {
                staffCode = staffCode.Trim();
            }

            var phoneNumber = Request?.Form["PhoneNumber"];
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                phoneNumber = phoneNumber.Trim();
            }

            var data = _crownXBLO.GetOrderListByStaffRemn(monthyear, staffCode, phoneNumber, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<GetOrderListByStaffRemnModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }







    }
}