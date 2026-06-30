using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VCM.BLUEPOS.Authen;
//using VMC.BLUEPOS.Common;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Model.LogsData;
using VCM.BLUEPOS.Model.LogsData.LoyaltyCXLogAPIModel;
using VCM.BLUEPOS.Business.LogsData;
using System.Globalization;
using VCM.BLUEPOS.Model.Common;
using Newtonsoft.Json;
using VCM.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class LogsDataController : BaseController
    {
        private ILogsDataBLO _logsDataBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;

        public LogsDataController(ILogsDataBLO logsDataBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _logsDataBLO = logsDataBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }

        [DisplayName("Danh sách mã lỗi E-Invoice")]
        public ActionResult EInvoiceAPIMappingErrorLog()
        {
            return View();
        }

        public JsonResult LoadAPIErrorMapping()
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

            var eRRCode = Request?.Form["ERRCode"];
            var data = _logsDataBLO.GetAPIErrorMappingList(eRRCode, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<APIErrorMappingResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [DisplayName("Danh sách Log phát hành")]
        public ActionResult EInvoicePublishLog()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        public JsonResult LoadPublishLog()
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

            var storeNo = Request?.Form["storeNo"];

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var data = _logsDataBLO.GetPubLishLog(fromDate, toDate, storeNo, textSearch, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<PublishLogResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [DisplayName("Log lỗi")]
        public ActionResult EInvoiceErrorsLog()
        {
            return View();
        }

        public JsonResult LoadInvoiceErrorsLog()
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

            var exPort = 0;
            var data = _logsDataBLO.GetInvoiceErrorsLog(fromDate, toDate, exPort, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<InvoiceErrorsResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcelInvoiceErrorsLog(DateTime FromDate, DateTime ToDate, int exPort = 1)
        {
            var data = _logsDataBLO.ExportExcelInvoiceErrorsLog(FromDate, ToDate, exPort);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.ErrorID,
                        x.ErrorDateTimeStr,
                        x.UserName,
                        x.ErrorNumber,
                        x.ErrorState,
                        x.ErrorSeverity,
                        x.ErrorLine,
                        x.ErrorProcedure,
                        x.ErrorMessage
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ErrorID";
                    worksheet.Cells[1, 2].Value = "ErrorDateTime";
                    worksheet.Cells[1, 3].Value = "UserName";
                    worksheet.Cells[1, 4].Value = "ErrorNumber";
                    worksheet.Cells[1, 5].Value = "ErrorState";
                    worksheet.Cells[1, 6].Value = "ErrorSeverity";
                    worksheet.Cells[1, 7].Value = "ErrorLine";
                    worksheet.Cells[1, 8].Value = "ErrorProcedure";
                    worksheet.Cells[1, 9].Value = "ErrorMessage";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 9])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"Invoice_Errors_Log_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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


        [DisplayName("Log API CX")]
        public ActionResult Loyalty_CX_LogAPI()
        {
            return View();
        }

        public JsonResult LoadLoyaltyCXLogAPI()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];
            //var pageSize = length != null ? Convert.ToInt32(length) : 0;
            //var skip = start != null ? Convert.ToInt32(start) : 0;
            var recordsTotal = 0;
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

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
            var svProd = Request?.Form["Prod"];
            var storeNo = Request?.Form["StoreNo"];
            var actionType = Request?.Form["ActionType"];
            var keyword = Request?.Form["Keyword"];

            var data = _logsDataBLO.GetLoyaltyCXLogAPI(svProd, fromDate, toDate, actionType, storeNo, keyword, pageSize, pageNumber);
            recordsTotal = data.Count() == 0 ? 0 : (int)data.FirstOrDefault().Total;
            return Json(new DataTablesViewModel<LoyaltyCXLogAPIResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public PartialViewResult GetDetailCXLogAPIRequestPOS(int id)
        {
            try
            {
                var data = _logsDataBLO.GetDetailCXLogAPIRequestPOS(id);
                if (data != null)
                {
                    return this.PartialView(data);
                }
                else
                {
                    return this.PartialView(new List<DetailLoyaltyCXLogAPIRequestPOSModel>());
                }
            }
            catch (Exception ex)
            {
                return this.PartialView(new List<DetailLoyaltyCXLogAPIRequestPOSModel>());
            }
        }

        public PartialViewResult GetDetailCXLogAPIRequestXML(int id)
        {
            try
            {
                var data = _logsDataBLO.GetDetailCXLogAPIRequestXML(id);
                if (data != null)
                {
                    return this.PartialView(data);
                }
                else
                {
                    return this.PartialView(new List<DetailLoyaltyCXLogAPIRequestXMLModel>());
                }
            }
            catch
            {
                return this.PartialView(new List<DetailLoyaltyCXLogAPIRequestXMLModel>());
            }
        }

        public PartialViewResult GetDetailCXLogAPIResponseXML(int id)
        {
            try
            {
                var data = _logsDataBLO.GetDetailCXLogAPIResponseXML(id);
                if (data != null)
                    return this.PartialView(data);
                else
                    return this.PartialView(new List<DetailLoyaltyCXLogAPIResponseXMLModel>());
            }
            catch
            {
                return this.PartialView(new List<DetailLoyaltyCXLogAPIResponseXMLModel>());
            }
        }

        public ActionResult ExportExcelCXLogAPI(DateTime FromDate, DateTime ToDate, string StoreNo, string PosID, string CardNumber, string InvoiceNo)
        {
            var data = _logsDataBLO.ExportExcelCXLogAPI(FromDate, ToDate, StoreNo, PosID, CardNumber, InvoiceNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.ID,
                        x.CreateDateStr,
                        x.StoreNo,
                        x.POSTerminal,
                        x.CardNumber,
                        x.InvoiceNo,
                        x.ActionType,
                        x.RequestPOS,
                        x.RequestXML,
                        x.ResponseXML,
                        x.ResponseTotal,
                        x.Source
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Thời gian";
                    worksheet.Cells[1, 3].Value = "ST/CH";
                    worksheet.Cells[1, 4].Value = "Máy POS";
                    worksheet.Cells[1, 5].Value = "Số thẻ";
                    worksheet.Cells[1, 6].Value = "Số hóa đơn";
                    worksheet.Cells[1, 7].Value = "ActionType";
                    worksheet.Cells[1, 8].Value = "RequestPOS";
                    worksheet.Cells[1, 9].Value = "RequestXML";
                    worksheet.Cells[1, 10].Value = "ResponseXML";
                    worksheet.Cells[1, 11].Value = "ResponseTotal";
                    worksheet.Cells[1, 12].Value = "Source";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"CX_LogAPI_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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


        [DisplayName("Log API Voucher")]
        public ActionResult LogAPIVoucher()
        {
            return View();
        }

        public JsonResult LoadLogAPIVoucher()
        {
            var draw = Request?.Form["draw"];
            var start = Request?.Form["start"];
            var length = Request?.Form["length"];
            var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
            var sortColumnDirection = Request?.Form["order[0][dir]"];
            var searchValue = Request?.Form["search[value]"];

            var recordsTotal = 0;
            var pageSize = length != null ? Convert.ToInt32(length) : 1;
            var pageNumber = start != null ? Convert.ToInt32(start) > 0 ? Convert.ToInt32(start) / Convert.ToInt32(length) + 1 : 1 : 1;

            var fromDate = DateTime.Now;
            var toDate = DateTime.Now;
            var tuNgay = Request?.Form["fromDate"];
            var denNgay = Request?.Form["toDate"];
            if (!string.IsNullOrEmpty(tuNgay))
            {
                fromDate = DateTime.ParseExact(tuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }
            if (!string.IsNullOrEmpty(denNgay))
            {
                toDate = DateTime.ParseExact(denNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            }
            var svProd = Request?.Form["Prod"];
            var partner = Request?.Form["Partner"];
            var storeNo = Request?.Form["StoreNo"];
            var actionType = Request?.Form["ActionType"];
            var keyword = Request?.Form["Keyword"];

            var data = _logsDataBLO.LoadLogAPIVoucher(svProd, partner, fromDate, toDate, actionType, storeNo, keyword, pageSize, pageNumber);
            recordsTotal = data.Count() == 0 ? 0 : (int)data.FirstOrDefault().Total;
            return Json(new DataTablesViewModel<LogAPIVoucherModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }


        [DisplayName("Log thiếu đơn hàng bán")]
        public ActionResult LogMissOrderSale()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        public JsonResult LoadLogMissOrderSale()
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

                var storeNo = Request?.Form["StoreNo"];
                var fromDateStr = Request?.Form["FromDate"];
                var toDateStr = Request?.Form["ToDate"];
                DateTime fromDate = DateTime.ParseExact(fromDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime toDate = DateTime.ParseExact(toDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var result = _logsDataBLO.LogMissOrderSale(storeNo, fromDate, toDate, out recordsTotal, skip, pageSize);
                return Json(new DataTablesViewModel<LogMisSaleModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<LogMisSaleModel>());
            }
        }

        [DisplayName("Log kết thúc ngày tại POS")]
        public ActionResult LogCloseSalePOS()
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
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        public ActionResult LogCloseSalePOSList()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var take = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;

                var setDB = Request?.Form["SetDB"];
                var storeNo = Request?.Form["StoreNo"];
                var posTerminal = Request?.Form["PosTerminal"];
                var dateBusiness = Request?.Form["BussinessDate"];
                var status = Request?.Form["IsClosed"];
                var recordsTotal = 0;

                DateTime ngayKD = DateTime.ParseExact(dateBusiness, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var result = _logsDataBLO.LogCloseSalePOSByBussinessDate(setDB, storeNo, posTerminal, status, ngayKD, out recordsTotal, skip, take);
                return Json(new DataTablesViewModel<LogCloseSalePOSModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new List<LogCloseSalePOSModel>());
            }
        }

        [HttpPost]
        public JsonResult SyncBussinessDateToCentral(string jsonBussiness, string ipServer)
        {
            var result = new Tuple<bool, string>(false, "");
            try
            {
                var listPosID = JsonConvert.DeserializeObject<List<SyncBussinessDateToCentralModel>>(jsonBussiness);
                if (listPosID == null || listPosID.Count == 0)
                {
                    result = new Tuple<bool, string>(false, "Vui lòng chọn dữ liệu để đồng bộ lên Central");
                    return Json(result);
                }

                var model = listPosID.Select(a => new SyncBussinessDateModel
                {
                    StoreNo = a.StoreNo,
                    PosID = a.PosID,
                    NgayKD = Convert.ToDateTime(a.NgayKD),
                    IsClosed = string.IsNullOrEmpty(a.IsClosed) ? false : Convert.ToBoolean(a.IsClosed),
                    CloseDate = string.IsNullOrEmpty(a.CloseDate) ? DateTime.Now.Date : Convert.ToDateTime(a.CloseDate),
                    CloseUser = a.CloseUser
                }).ToList();

                var data = _logsDataBLO.SyncBussinessDateToCentral(ipServer, model);
                if (data.Item1)
                {
                    result = new Tuple<bool, string>(true, $"{data.Item2}");
                }
                else
                {
                    result = new Tuple<bool, string>(false, $"{data.Item2}");
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, $"{JsonConvert.SerializeObject(ex)}");
            }
            return Json(result);
        }

        [DisplayName("Log File Sale tại POS")]
        public ActionResult LogFileSalePOS()
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
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        public ActionResult LogFileSalePOSList()
        {
            try
            {
                var draw = Request?.Form["draw"];
                var start = Request?.Form["start"];
                var length = Request?.Form["length"];
                var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
                var sortColumnDirection = Request?.Form["order[0][dir]"];
                var searchValue = Request?.Form["search[value]"];
                var take = length != null ? Convert.ToInt32(length) : 0;
                var skip = start != null ? Convert.ToInt32(start) : 0;
                var recordsTotal = 0;

                var setDB = Request?.Form["SetDB"];
                var storeNo = Request?.Form["StoreNo"];
                var posTerminal = Request?.Form["PosTerminal"];
                var fromDate = Request?.Form["FromDate"];
                var toDate = Request?.Form["ToDate"];
                var file = Request?.Form["TableName"];

                DateTime fDate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                DateTime tDate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var result = _logsDataBLO.LogFileSalePOS(setDB, storeNo, posTerminal, file, fDate, tDate, out recordsTotal, skip, take);
                return Json(new DataTablesViewModel<LogFileSalePOSModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new List<LogFileSalePOSModel>());
            }
        }

        public ActionResult _ViewLogFileSalePOS(string IPServer, string FilePath, string FileName, string FolderFile)
        {
            var readFileModel = new ViewLogFileSalePOSModel { FileName = FileName };
            try
            {
                var strAfterProcess = FilePath.Substring(FilePath.IndexOf("Processing"), FilePath.Length - FilePath.IndexOf("Processing"));
                var storeNo = strAfterProcess.Split('\\')[1];
                var posID = strAfterProcess.Split('\\')[2];
                var pathFileServer = $"\\\\{IPServer}\\ftpbluepos\\SyncDataPos\\Sale\\Backup\\{FolderFile}\\{storeNo}\\{posID}\\{FileName}";
                //var pathFileLocal = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\LogFileSalePOSTest.txt";
                var readFile = System.IO.File.ReadAllText(pathFileServer);

                readFileModel.StoreNo = storeNo;
                readFileModel.PosID = posID;
                readFileModel.PathFileServer = pathFileServer;
                readFileModel.ReadFile = readFile;
                return Json(readFileModel);
            }
            catch (Exception ex)
            {
                readFileModel.ReadFile = JsonConvert.SerializeObject(ex);
                return Json(readFileModel);
            }
        }

        [DisplayName("Danh sách bảng đồng bộ")]
        public ActionResult LogSyncChangeTableByStore()
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
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        public JsonResult SyncChangeLogStoreList()
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
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }
            var tableName = Request?.Form["TableName"];
            if (!string.IsNullOrEmpty(tableName))
            {
                tableName = tableName.Trim();
            }
            var data = _logsDataBLO.SyncChangeLogStoreList(storeNo, tableName, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<SyncChangeLogStoreModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public JsonResult UpdateLastCounter(UpdateSyncChangeLogStoreModel req)
        {
            string _userName = LoginUser.UserName;
            req.UpdatedBy = _userName;
            req.UpdatedDate = DateTime.Now;
            var result_Infor = new Tuple<bool, string>(false, "");
            if (req.LastCounter == null)
            {
                result_Infor = new Tuple<bool, string>(false, "Vui lòng nhập số LastCounter");
                return Json(result_Infor);
            }
            var result = _logsDataBLO.UpdateLastCounter(req);
            return Json(result);
        }































    }
}