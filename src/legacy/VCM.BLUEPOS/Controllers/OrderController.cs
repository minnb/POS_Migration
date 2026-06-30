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
using System.Web.Configuration;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Account;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.Order.OrderWinLifeModel;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel;
using VCM.BLUEPOS.Business.Order;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class OrderController : BaseController
    {
        private IOrderBLO _orderBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        private IAccountBLO _accountBLO;

        string ipServer = WebConfigurationManager.AppSettings["setDB1"];

        public OrderController(IOrderBLO orderBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO, IAccountBLO accountBLO)
        {
            _orderBLO = orderBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
            _accountBLO = accountBLO;
        }
        public void WriteLog(string functionName, string message)
        {
            var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
            var content = $"{now} {functionName}         {message}";
            var path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
            var filepath = path + functionName + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt";

            if (!System.IO.File.Exists(filepath))
            {
                using (StreamWriter sw = System.IO.File.CreateText(filepath))
                {
                    sw.WriteLine(content);
                }
            }
            else
            {
                using (StreamWriter sw = System.IO.File.AppendText(filepath))
                {
                    sw.WriteLine(content);
                }
            }

            if (Environment.UserInteractive)
            {
                Console.WriteLine(content);
            }
        }

        [DisplayName("Danh sách đơn hàng")]
        public ActionResult OrderList()
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

            ViewBag.ListOrderType = _commonBLO.GetComboxOptionData(captionOrderType);
            ViewBag.ListSalesType = _commonBLO.GetComboxOptionData(captionSaleType);
            ViewBag.ListSourceBill = _commonBLO.GetComboxOptionData(captionSourceBill);
            ViewBag.SetDB = listSetDB;
            
            ViewBag.ToSalesType = _commonBLO.GetToChangeSalesType();
            var model = _commonBLO.LoadComboxSalesType();
            var dataSalesType = _commonBLO.GetFromChangeSalesType();
            
            ViewBag.FromSalesType = string.Join(",", dataSalesType);
            ViewBag.PermissionRole = _accountBLO.GetRoleByUser(base.LoginUser.UserName);

            //ViewBag.PermissionRole = _authenBLO.GetRoleByUser(base.LoginUser.UserName);
            return View(model);

        }

        public JsonResult GetOrderList()
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

            var orderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var salesType = Request?.Form["SalesType"];
            if (!string.IsNullOrEmpty(salesType))
            {
                salesType = salesType.Trim();
            }

            var textSearchOrder = Request?.Form["textSearchOrder"];
            if (!string.IsNullOrEmpty(textSearchOrder))
            {
                textSearchOrder = textSearchOrder.Trim();
            }
            var textSearchItem = Request?.Form["textSearchItem"];
            if (!string.IsNullOrEmpty(textSearchItem))
            {
                textSearchItem = textSearchItem.Trim();
            }
            var posID = Request?.Form["PosID"];
            if (!string.IsNullOrEmpty(posID))
            {
                posID = posID.Trim();
            }
            
            var fromAmount = Request?.Form["FromAmount"];
            float dFAmount = 0;
            if (!string.IsNullOrEmpty(fromAmount))
            {
                dFAmount = float.Parse(fromAmount);
            }

            var toAmount = Request?.Form["ToAmount"];
            float dTAmount = 0;
            if (!string.IsNullOrEmpty(toAmount))
            {
                dTAmount = float.Parse(toAmount);
            }

            var userID = Request?.Form["UserID"];
            if (!string.IsNullOrEmpty(userID))
            {
                userID = userID.Trim();
            }

            var setServer = Request?.Form["SetServer"];
            var setDBRead = Request?.Form["ServerRead"];

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

            var data = _orderBLO.GetOrderList(fromDate, toDate, setDBRead, storeNo, posID, orderType, salesType, userID, textSearchOrder, textSearchItem, dFAmount, dTAmount, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<OrderListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcelOrderList(string FromDate, string ToDate, string SetServer, string StoreNo, string PosID, string OrderType, string SalesType, string UserCreateBill, string TextSearchOrder, string TextSearchItem, string FromAmount, string ToAmount)
        {
            DateTime fromDate = DateTime.ParseExact(FromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(ToDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var userName = base.LoginUser.UserName;             
            if (!string.IsNullOrEmpty(StoreNo))
            {
                StoreNo = StoreNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, StoreNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
                }
                StoreNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(SetServer, StoreNo, userName);
                StoreNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            float dFAmount = 0;
            float dTAmount = 0;

            if (!string.IsNullOrEmpty(FromAmount))
            {
                dFAmount = float.Parse(FromAmount);
            }
            if (!string.IsNullOrEmpty(ToAmount))
            {
                dTAmount = float.Parse(ToAmount);
            }

            var data = _orderBLO.ExportExcelOrderList(fromDate, toDate, SetServer, StoreNo, PosID, OrderType, SalesType, UserCreateBill, TextSearchOrder, TextSearchItem, dFAmount, dTAmount);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OrderNo,
                        x.StartingTimeStr,
                        x.OrderTimeStr,
                        x.OrderDateStr,
                        x.StoreNo,
                        x.POSTerminalNo,
                        x.TotalAmount,
                        x.DiscountTotalAmount,
                        x.PaymentAtPOSAmount,
                        x.SalesType,                // Hình thức bán hàng
                        x.OrderType,                // Loại đơn hàng
                        x.UserID,                   // Noi tao bill
                        x.OrigOrder,                // Số đơn hàng gốc
                        x.ReferenceNo,              // So dh tham chieu
                        x.CustomerName,             // Ten khach hang
                        x.CashierID,                // Thu ngan
                        x.LabelStr,                 // Label                 
                        x.Note                      // Ghi chu
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số đơn hàng";
                    worksheet.Cells[1, 2].Value = "Ngày tạo đơn hàng";
                    worksheet.Cells[1, 3].Value = "Thời gian";
                    worksheet.Cells[1, 4].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 5].Value = "ST/CH";
                    worksheet.Cells[1, 6].Value = "Máy POS";                   
                    worksheet.Cells[1, 7].Value = "Tổng số tiền";
                    worksheet.Cells[1, 8].Value = "Tổng số tiền giảm giá";
                    worksheet.Cells[1, 9].Value = "Thành tiền";
                    worksheet.Cells[1, 10].Value = "Hình thức bán hàng";
                    worksheet.Cells[1, 11].Value = "Loại đơn hàng";                                
                    worksheet.Cells[1, 12].Value = "Nơi tạo bill";
                    worksheet.Cells[1, 13].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 14].Value = "Số đơn hàng tham chiếu";
                    worksheet.Cells[1, 15].Value = "Tên khách hàng";
                    worksheet.Cells[1, 16].Value = "Thu ngân";
                    worksheet.Cells[1, 17].Value = "Label";       
                    worksheet.Cells[1, 18].Value = "Ghi chú";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 18])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"SalesOrderList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public ActionResult GetDetailOrderList()
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

            var data = _orderBLO.GetDetailOrderList(storeNo, orderNo, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<DetailOrderListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult GetDetailOrderListByPromotion()
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

            var check = Request?.Form["CheckSearch"];
            var data = new List<ViewDetailPromotionVoucherCouponModel>();
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

            var fromDateStr = fromDate.ToString();
            var toDateStr = toDate.ToString();

            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var storeNo = Request?.Form["StoreNo"];
            var setServer = Request?.Form["SetServer"];
            var setDBRead = Request?.Form["ServerRead"];
            var PosTerminal = Request?.Form["PosTerminal"];
            var orderNo2 = Request?.Form["OrderNo2"];

            var userName = base.LoginUser.UserName;
            var storeNo2 = Request?.Form["StoreNo2"];
            if (!string.IsNullOrEmpty(storeNo2))
            {
                storeNo2 = storeNo2.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo2);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<OrderListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<OrderListResponseModel>() });
                }
                storeNo2 = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo2, userName);
                //var listStoreNo = _commonBLO.LoadStoreByUserName(setDBRead, storeNo, userName);
                storeNo2 = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }
           
            if (check == "true")
            {
                //data = _orderBLO.Get_Detail_Promotion_Voucher_Coupon_List_By_Posterminal(fromDateStr, toDateStr, setDB_Read, storeNo2, PosTerminal, orderNo2, out recordsTotal, pageNumber, pageSize);
                data = _orderBLO.GetOrderDetailPromotionByPosterminal(fromDateStr, toDateStr, setDBRead, storeNo2, PosTerminal, orderNo2, out recordsTotal, pageNumber, pageSize);
            }
            else
            {
                //data = _orderBLO.Get_Detail_Promotion_Voucher_Coupon_List(storeNo, orderNo, out recordsTotal, pageNumber, pageSize);
                data = _orderBLO.GetOrderDetailPromotionList(storeNo, orderNo, out recordsTotal, pageNumber, pageSize);
            }
            return Json(new DataTablesViewModel<ViewDetailPromotionVoucherCouponModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcel_ModalDetailPromotionVoucherByOderSales(string ExportFromDate, string ExportToDate, string SetServerIP, string StoreNoStr, string PosTerminalStr, string TextSearchOrder)
        {
            var data = _orderBLO.Export_Get_Detail_Promotion_List_By_Posterminal(ExportFromDate, ExportToDate, SetServerIP, StoreNoStr, PosTerminalStr, TextSearchOrder);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OrderNo,
                        x.OrderDateStr,
                        x.StoreNo,
                        x.POSTerminalNo,
                        x.LineNo,
                        x.LineType,
                        x.OfferType,
                        x.OfferNo,
                        x.Barcode,
                        x.ItemNo,
                        x.Description,
                        x.UOM,
                        x.UnitPrice,    
                        x.Quantity,       
                        x.DiscountAmount  
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số đơn hàng";
                    worksheet.Cells[1, 2].Value = "Ngày";
                    worksheet.Cells[1, 3].Value = "Cửa hàng";
                    worksheet.Cells[1, 4].Value = "Máy POS";
                    worksheet.Cells[1, 5].Value = "LineNo";
                    worksheet.Cells[1, 6].Value = "LineType";
                    worksheet.Cells[1, 7].Value = "OfferType";
                    worksheet.Cells[1, 8].Value = "OfferNo";
                    worksheet.Cells[1, 9].Value = "Barcode/Coupon";
                    worksheet.Cells[1, 10].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 11].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 12].Value = "ĐVT";
                    worksheet.Cells[1, 13].Value = "Số lượng";
                    worksheet.Cells[1, 14].Value = "Đơn Giá";
                    worksheet.Cells[1, 15].Value = "Giảm giá";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 15])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"OrderSalesListByPromotion_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        public ActionResult GetDetailPaymentOrderList()
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

            var storeNo = Request?.Form["StoreNo"];
            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var data = _orderBLO.GetPaymentDetailOrderList(storeNo, orderNo, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<PaymentDetailOrderResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult UpdateSalesType(UpdateSalesTypeForOrderModel req)
        {
            var data = _orderBLO.UpdateSalesType(req);
            return Json(data);
        }

        [DisplayName("Danh sách đơn hàng WinLife")]
        public ActionResult OrderListWinLife()
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
            var model = _commonBLO.LoadComboxSalesType();
            return View(model);
        }

        public JsonResult GetOrderListWinLife()
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
            
            var orderType = Request?.Form["OrderType"];
            if (!string.IsNullOrEmpty(orderType))
            {
                orderType = orderType.Trim();
            }

            var transactionType = Request?.Form["transactionType"];
            if (!string.IsNullOrEmpty(transactionType))
            {
                transactionType = transactionType.Trim();
            }

            var textSearchOrder = Request?.Form["textSearchOrder"];
            if (!string.IsNullOrEmpty(textSearchOrder))
            {
                textSearchOrder = textSearchOrder.Trim();
            }

            var posID = Request?.Form["PosID"];
            if (!string.IsNullOrEmpty(posID))
            {
                posID = posID.Trim();
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
                    return Json(new DataTablesViewModel<OrderListWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<OrderListWinLifeResponseModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(ipServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _orderBLO.GetOrderListWinLife(fromDate, toDate, storeNo, posID, orderType, transactionType, textSearchOrder, chanelSales, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<OrderListWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcelOrderListWinLife(DateTime FromDate, DateTime ToDate, string StoreNo, string PosID, string OrderType, string TransactionType, string TextSearchOrder, string ChanelSales)
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

            var data = _orderBLO.ExportExcelOrderListWinLife(FromDate, ToDate, storeNo, PosID, OrderType, TransactionType, TextSearchOrder, ChanelSales);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OrderNo,
                        x.EndingTimeStr,
                        x.TimeStr,
                        x.OrderDate,
                        x.StoreNoPLG,
                        x.StoreNoWCM,
                        x.POSTerminalNo,
                        x.BeforeTotalAmount,
                        x.DiscountAmount,
                        x.TotalAmount,
                        x.OrderType,
                        x.TransactionType,
                        x.RefKey,
                        x.ChanelSales
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số đơn hàng";
                    worksheet.Cells[1, 2].Value = "Ngày tạo đơn hàng";
                    worksheet.Cells[1, 3].Value = "Thời gian";
                    worksheet.Cells[1, 4].Value = "Ngày kinh doanh";
                    worksheet.Cells[1, 5].Value = "ST/CH PLG";
                    worksheet.Cells[1, 6].Value = "ST/CH WCM";
                    worksheet.Cells[1, 7].Value = "Máy POS";
                    worksheet.Cells[1, 8].Value = "Tổng số tiền";
                    worksheet.Cells[1, 9].Value = "Tổng số tiền giảm giá";
                    worksheet.Cells[1, 10].Value = "Thành tiền";
                    worksheet.Cells[1, 11].Value = "Hình thức bán hàng";
                    worksheet.Cells[1, 12].Value = "Loại đơn hàng";
                    worksheet.Cells[1, 13].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 14].Value = "Kênh bán hàng";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 14])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"SalesOrderList_WinLife_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public ActionResult GetDetailOrderListWinLife()
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
            var storeNo = Request?.Form["StoreNo"];
            var orderNo = Request?.Form["OrderNo"];
            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }
            var data = _orderBLO.GetDetailOrderListWinLife(storeNo, orderNo, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<DetailOrderListWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult GetDetailPaymentOrderListWinLife()
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

            var storeNo = Request?.Form["StoreNo"];
            var orderNo = Request?.Form["OrderNo"];

            if (!string.IsNullOrEmpty(orderNo))
            {
                orderNo = orderNo.Trim();
            }

            var data = _orderBLO.GetPaymentDetailOrderListWinLife(storeNo, orderNo, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<PaymentDetailOrderWinLifeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }












    }
}