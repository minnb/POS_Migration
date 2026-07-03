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
using VCM.BLUEPOS.Model.VinID;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Business.VinID;
using VCM.BLUEPOS.Business.Common;

namespace PLG.Controllers
{
    public class VinIDController : BaseController
    {
        private IVinIDBLO _vinIDBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        public VinIDController(IVinIDBLO vinIDBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _vinIDBLO = vinIDBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }    
        public ActionResult VinIDReport()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }
        public JsonResult ReportVinIDLoad()
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

            var fromDate = DateTime.Now.ToString("yyyy-MM-dd h:mm tt");
            var toDate = DateTime.Now.ToString("yyyy-MM-dd h:mm tt");

            if (!string.IsNullOrEmpty(Request?.Form["FromDate"]))
            {
                fromDate = Convert.ToString(Request?.Form["FromDate"]);
            }
            else
            {
                fromDate = "2000-01-01";
            }

            if (!string.IsNullOrEmpty(Request?.Form["ToDate"]))
            {
                toDate = Convert.ToString(Request?.Form["ToDate"]);
            }
            else
            {
                toDate = "9999-12-31";
            }

            var storeNo = Request?.Form["StoreNo"];
            var terminalId = Request?.Form["TerminalId"];
            var typeTransaction = Request?.Form["TransactionType"];
            var textSearch = Request?.Form["CardNumber"];
            var orderNo = Request?.Form["OrderNo"];

            DateTime tungay = DateTime.ParseExact(fromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime denngay = DateTime.ParseExact(toDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            var data = _vinIDBLO.GetTransactionHistoryBluePOS(tungay, denngay, storeNo, terminalId, typeTransaction, textSearch, orderNo, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<TransactionHistoryBluePOSResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult ExportTransactionLoyaltyBluePOS(string fDate, string tDate, string StoreNo, string PosId, string TransactionType, string CardNumber, string OrderNo)
        {
            var FromDateStr = "";
            var ToDateStr = "";

            if (string.IsNullOrEmpty(fDate) || string.IsNullOrEmpty(tDate))
            {
                FromDateStr = "2010/01/01";
                ToDateStr = "9999/12/31";
            }
            else
            {
                FromDateStr = Convert.ToDateTime(fDate).ToString("yyyy/MM/dd");
                ToDateStr = Convert.ToDateTime(tDate).ToString("yyyy/MM/dd");
            }

            DateTime dFromDate = DateTime.ParseExact(FromDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime dToDate = DateTime.ParseExact(ToDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            var data = _vinIDBLO.ExportTransactionHistoryBluePOS(dFromDate, dToDate, StoreNo, PosId, TransactionType, CardNumber, OrderNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StoreID,
                        x.TerminalId,
                        x.DateTimeStr,
                        x.CardNumber,
                        x.QRCode,
                        x.OrderNo,
                        x.OrigOrderNo,
                        x.TransactionName,
                        x.AmountOnOrderPOS,
                        x.GrossTransactionAmountOE,
                        x.RefundAmount,
                        x.AwardPointOE,
                        x.SpendPoints,
                        x.AwardAmountOE,
                        x.RedeemAmountOE
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã ST/CH";
                    worksheet.Cells[1, 2].Value = "Máy POS";
                    worksheet.Cells[1, 3].Value = "Thời gian";
                    worksheet.Cells[1, 4].Value = "Số thẻ VinID";
                    worksheet.Cells[1, 5].Value = "Mã QR";
                    worksheet.Cells[1, 6].Value = "Số đơn hàng";
                    worksheet.Cells[1, 7].Value = "Số đơn hàng gốc";
                    worksheet.Cells[1, 8].Value = "Loại giao dịch";
                    worksheet.Cells[1, 9].Value = "Tổng số tiền thanh toán";
                    worksheet.Cells[1, 10].Value = "Tổng số tiền gia dịch OE";
                    worksheet.Cells[1, 11].Value = "Tổng số tiền trả";
                    worksheet.Cells[1, 12].Value = "Điểm tích";
                    worksheet.Cells[1, 13].Value = "Điểm tiêu";
                    worksheet.Cells[1, 14].Value = "Tiền tích điểm";
                    worksheet.Cells[1, 15].Value = "Tiền tiêu điểm";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 15])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"TransactionBluePOS_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public ActionResult TransactionBluePoints()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }
        public JsonResult GetTransactionBluePointHeader()
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

            var posCode = Request?.Form["PosCode"];
            if (!string.IsNullOrEmpty(posCode))
            {
                posCode = posCode.Trim();
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

            var data = _vinIDBLO.GetTransactionBluePointHeaderList(fromDate, toDate, storeNo, posCode, transactionType, textSearch, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<HeaderTransactionBluePointsModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult GetTransactionBluePointDetail()
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

            var receiptNumber = Request?.Form["ReceiptNumber"];
            if (!string.IsNullOrEmpty(receiptNumber))
            {
                receiptNumber = receiptNumber.Trim();
            }
            var data = _vinIDBLO.GetTransactionBluePointDetailList(receiptNumber, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<DetailTransactionBluePointsModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportTransactionBluePoint(string FromDate, string ToDate, string StoreNo, string PosCode, string TransactionType, string TextSearch)
        {
            DateTime dFromDate = DateTime.ParseExact(FromDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime dToDate = DateTime.ParseExact(ToDate, "yyyy/MM/dd", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(TextSearch))
            {
                TextSearch = TextSearch.Trim();
            }

            var data = _vinIDBLO.ExportToExcelTransactionBluePointList(dFromDate, dToDate, StoreNo, PosCode, TransactionType, TextSearch);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportTransactionBluePointsModel()
                    {
                        ReceiptNumber = x.ReceiptNumber,
                        OrderNo = x.OrderNo,
                        CreatedDate = x.CreatedDate,
                        StoreNo = x.StoreNo,
                        POSCode = x.POSCode,
                        TransactionType = x.TransactionType,
                        VinidCardNumber = x.VinidCardNumber,
                        CustomerName = x.CustomerName,
                        TotalBillAmount = x.TotalBillAmount,
                        ExtraEarnByItems = x.ExtraEarnByItems,
                        ExtraEarnByCampaign = x.ExtraEarnByCampaign,
                        Barcode = x.Barcode,
                        RecordNo = x.RecordNo,
                        Article = x.Article,
                        ArticleName = x.ArticleName,
                        UOM = x.UOM,
                        Quantity = x.Quantity,
                        SalePrice = x.SalePrice,
                        Amount = x.Amount,
                        DiscountAmount = x.DiscountAmount,
                        LineAmount = x.LineAmount,
                        ExtraQuantityEarn = x.ExtraQuantityEarn,
                        ExtraAmountEarn = x.ExtraAmountEarn,
                        CashierCode = x.CashierCode,
                        VinIDCSN = x.VinIDCSN,
                        ReferenceNumber = x.ReferenceNumber,
                        OriginalReceiptNumber = x.OriginalReceiptNumber,
                        OriginalReferenceNumber = x.OriginalReferenceNumber,

                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Số Receipt";
                    worksheet.Cells[1, 2].Value = "Số đơn hàng";
                    worksheet.Cells[1, 3].Value = "Thời gian";
                    worksheet.Cells[1, 4].Value = "Mã cửa hàng";
                    worksheet.Cells[1, 5].Value = "Mã máy POS";
                    worksheet.Cells[1, 6].Value = "Loại giao dịch";
                    worksheet.Cells[1, 7].Value = "Số thẻ VinID";
                    worksheet.Cells[1, 8].Value = "Tên khách hàng";
                    worksheet.Cells[1, 9].Value = "Tổng số tiền";
                    worksheet.Cells[1, 10].Value = "Điểm KM Masan";
                    worksheet.Cells[1, 11].Value = "Điểm nhân viên";
                    worksheet.Cells[1, 12].Value = "Mã BarCode";
                    worksheet.Cells[1, 13].Value = "RecordNo";
                    worksheet.Cells[1, 14].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 15].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 16].Value = "ĐVT";
                    worksheet.Cells[1, 17].Value = "Số lượng";
                    worksheet.Cells[1, 18].Value = "Đơn giá";
                    worksheet.Cells[1, 19].Value = "Thành tiền";
                    worksheet.Cells[1, 20].Value = "Khuyến mãi";
                    worksheet.Cells[1, 21].Value = "Thành tiền sau KM";
                    worksheet.Cells[1, 22].Value = "SL tích điểm";
                    worksheet.Cells[1, 23].Value = "Điểm";
                    worksheet.Cells[1, 24].Value = "Thu ngân";
                    worksheet.Cells[1, 25].Value = "Nhân viên (CSN)";
                    worksheet.Cells[1, 26].Value = "Số tham chiếu";
                    worksheet.Cells[1, 27].Value = "Số Receipt gốc";
                    worksheet.Cells[1, 28].Value = "Số tham chiếu gốc";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 28])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"TransactionBluePoint_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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