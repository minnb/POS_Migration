using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Store;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Promotion;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.SetupPromotion;
using VCM.BLUEPOS.Business.Promotion;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Business.SetupPromotion;
using VCM.BLUEPOS.Helpers;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using VCM.BLUEPOS.Common;

namespace PLG.Controllers
{
    public class PromotionController : BaseController
    {
        private IPromotionBLO _promotionBLO;
        private IAuthenBLO _authenBLO;
        private IStoreBLO _storeBLO;
        private ICommonBLO _commonBLO;
        private ISetupPromotionBLO _setupBBBLO { get; set; }
        public PromotionController(IPromotionBLO promotionBLO, IAuthenBLO authenBLO, IStoreBLO storeBLO, ICommonBLO commonBLO, ISetupPromotionBLO setupBBBLO)
        {
            _promotionBLO = promotionBLO;
            _authenBLO = authenBLO;
            _storeBLO = storeBLO;
            _commonBLO = commonBLO;
            _setupBBBLO = setupBBBLO;
        }

        [DisplayName("Danh mục khuyến mãi")]
        public ActionResult PromotionList()
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
            ViewBag.ListPromotionType = _promotionBLO.GetPromotionType();
            ViewBag.ListPromotionStatus = _promotionBLO.GetPromotionStatus();
            var model = _setupBBBLO.ViewSetupMain();
            return View(model);

        }

        [HttpPost]
        public JsonResult GetOfferHeaderList()
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

            var textSearch = Request?.Form["TextSearch"];
            var description = Request?.Form["PromotionName"];
            var status = Request?.Form["PromotionStatus"];
            var offerType = Request?.Form["PromotionTypes"];
            var itemNo = Request?.Form["ItemNo"];
            var storeNo = string.Empty;
            var salesType = Request?.Form["SalesType"];
            var styleProfile = Request?.Form["StyleProfile"];

            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            if (!string.IsNullOrEmpty(description))
            {
                description = description.Trim();
            }

            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }

            if (!string.IsNullOrEmpty(offerType))
            {
                offerType = offerType.Trim();
            }

            if (!string.IsNullOrEmpty(itemNo))
            {
                itemNo = itemNo.Trim();
            }

            if (!string.IsNullOrEmpty(salesType))
            {
                salesType = salesType.Trim();
            }
  
            int exp = 0;  // Khong export excel
            var data = _promotionBLO.GetOfferHeaderList(textSearch, description, status, offerType, itemNo, storeNo, salesType, exp, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<OfferHearderResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult GetDetailOfferHeaderList(string offerNo)
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

            var data = _promotionBLO.GetDetailOfferHeaderList(offerNo, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<DetailOfferHeaderResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult GetDetailOfferBuyList(string offerNo)
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

            var data = _promotionBLO.GetDetailOfferBuyList(offerNo, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            return Json(new DataTablesViewModel<DetailOfferBuyResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult GetDetailOfferBenefitsList(string offerNo)
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
            var data = _promotionBLO.GetDetailOfferBenefitsList(offerNo, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<DetailOfferBenefitsResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult GetDetailOfferGetList(string offerNo)
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

            var data = _promotionBLO.GetDetailOfferGetList(offerNo, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            return Json(new DataTablesViewModel<DetailOfferGetResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult GetDetailOfferSiteList(string offerNo)
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

            var data = _promotionBLO.GetDetailOfferSiteList(offerNo, out recordsTotal, skip, pageSize, sortColumn, sortColumnDirection, searchValue);
            return Json(new DataTablesViewModel<DetailOfferSiteModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public ActionResult GetDetailOfferPriorityList(string offerType)
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
            var data = _promotionBLO.GetDetailOfferPriorityList(offerType, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<DetailOfferPriorityModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcelGetOfferHeaderList(string TextSearch, string PromotionName, string PromotionStatus, string PromotionType, string SalesType, string ItemNo, string StoreNo)
        {
            var data = _promotionBLO.ExportExcelGetOfferHeaderList(TextSearch, PromotionName, PromotionStatus, PromotionType, SalesType, ItemNo, StoreNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.BonusBuyNo,
                        x.PromotionNo,
                        x.Description,
                        x.OfferType,
                        x.SalesTypeName,    // Hình thức bán hàng
                        x.Status,                      
                        x.StartingDate,
                        x.EndingDate,
                        x.VoucherFromDate,
                        x.VoucherToDate,
                        x.LocalSiteGroup,
                        x.LimitQty,
                        x.LastDateModified
                    }).ToList();

                    worksheet.Cells[1, 1].Value  = "Bonus Buy";
                    worksheet.Cells[1, 2].Value  = "Mã CTKM";
                    worksheet.Cells[1, 3].Value  = "Tên CTKM";
                    worksheet.Cells[1, 4].Value  = "Loại CTKM";
                    worksheet.Cells[1, 5].Value  = "Hình thức bán hàng";
                    worksheet.Cells[1, 6].Value  = "Trạng thái";                    
                    worksheet.Cells[1, 7].Value  = "Ngày bắt đầu hiệu lực";
                    worksheet.Cells[1, 8].Value  = "Ngày hết hiệu lực";
                    worksheet.Cells[1, 9].Value  = "Voucher có giá trị từ ngày";
                    worksheet.Cells[1, 10].Value = "Voucher có giá trị đến ngày";
                    worksheet.Cells[1, 11].Value = "LocalSiteGroup";
                    worksheet.Cells[1, 12].Value = "LimitQty";
                    worksheet.Cells[1, 13].Value = "Ngày cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 13])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"PromotionList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Tra cứu khuyến mãi")]
        public ActionResult CheckPromotionList()
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
            ViewBag.ListPromotionType = _promotionBLO.GetPromotionType();
            ViewBag.ListPromotionStatus = _promotionBLO.GetPromotionStatus();
            var model = _setupBBBLO.ViewSetupMain();
            return View(model);

        }

        [HttpPost]
        public JsonResult LoadCheckPromotionList()
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

            var sourceType = Request?.Form["SourceType"];
            var storeNo = Request?.Form["StoreNo"];
            var status = Request?.Form["PromotionStatus"];
            var offerType = Request?.Form["PromotionTypes"];
            var offerNo = Request?.Form["OfferNo"];
            var keyWord = Request?.Form["KeyWord"];
            var ipAddress = Request?.Form["IPAddress"];
            var salesType = Request?.Form["SalesType"];
            var memberType = Request?.Form["MemberType"];

            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            if (!string.IsNullOrEmpty(offerNo))
            {
                offerNo = offerNo.Trim();
            }

            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }

            if (!string.IsNullOrEmpty(offerType))
            {
                offerType = offerType.Trim();
            }

            var itemNo = Request?.Form["ItemNo"];
            if (!string.IsNullOrEmpty(itemNo))
            {
                itemNo = itemNo.Trim();
            }

            if (!string.IsNullOrEmpty(keyWord))
            {
                keyWord = keyWord.Trim();
            }

            if (!string.IsNullOrEmpty(salesType))
            {
                salesType = salesType.Trim();
            }

            if (!string.IsNullOrEmpty(memberType))
            {
                memberType = memberType.Trim();
            }

            var data = new List<CheckPromotionModel>();
            if (sourceType == "SERVER")
            {
                data = _promotionBLO.CheckPromotionList(storeNo, offerNo, offerType, salesType, memberType, status, itemNo, keyWord, out recordsTotal, pageNumber, pageSize);
            }
            else if (sourceType == "POS")
            {
                data = ExcuteScriptPOS_CheckPromotion(ipAddress, storeNo, offerNo, offerType, salesType, memberType, status, itemNo, keyWord, out recordsTotal, pageNumber, pageSize);
            }
            return Json(new DataTablesViewModel<CheckPromotionModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public List<CheckPromotionModel> ExcuteScriptPOS_CheckPromotion(string IpAddress, string storeNo, string offerNo, string offerType, string salesType, string memberType, string status, string itemNo, string keyWord, out int recordsTotal, int pageNumber, int pageSize)
        {
            var result = new List<CheckPromotionModel>();
            try
            {
                bool bIsConnected = false;
                var messagePort = "";
                if (!Utils.CheckPort(IpAddress, 1433, ref messagePort))
                {
                    bIsConnected = false;
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IpAddress, "POS", "POS@1234", "StorePLH");
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            if (memberType == "All") // All loại thẻ
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ScriptExcute_POS_PromotionCheck_All.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", storeNo);
                                sql = sql.Replace("[@No]", offerNo);
                                sql = sql.Replace("[@OfferType]", offerType);
                                sql = sql.Replace("[@SalesType]", salesType);
                                sql = sql.Replace("[@MemberCode]", memberType);
                                sql = sql.Replace("[@Status]", status);
                                sql = sql.Replace("[@ItemNo]", itemNo);
                                sql = sql.Replace("[@KeyWord]", keyWord);
                                sql = sql.Replace("@PageSize", "" + pageSize);
                                sql = sql.Replace("@PageNumber", "" + pageNumber);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();

                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);

                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    recordsTotal = (int)result.FirstOrDefault()?.Total;

                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                            else if(memberType == "Vip" || memberType == "Diamond" || memberType == "Member")
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ScriptExcute_POS_PromotionCheck_Member.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", storeNo);
                                sql = sql.Replace("[@No]", offerNo);
                                sql = sql.Replace("[@OfferType]", offerType);
                                sql = sql.Replace("[@SalesType]", salesType);
                                sql = sql.Replace("[@MemberCode]", memberType);
                                sql = sql.Replace("[@Status]", status);
                                sql = sql.Replace("[@ItemNo]", itemNo);
                                sql = sql.Replace("[@KeyWord]", keyWord);
                                sql = sql.Replace("@PageSize", "" + pageSize);
                                sql = sql.Replace("@PageNumber", "" + pageNumber);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();
                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);
                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    recordsTotal = (int)result.FirstOrDefault()?.Total;
                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                            else if (memberType == "0")  // Không cần thẻ thành viên
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ScriptExcute_POS_PromotionCheck_No.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", storeNo);
                                sql = sql.Replace("[@No]", offerNo);
                                sql = sql.Replace("[@OfferType]", offerType);
                                sql = sql.Replace("[@SalesType]", salesType);
                                sql = sql.Replace("[@MemberCode]", memberType);
                                sql = sql.Replace("[@Status]", status);
                                sql = sql.Replace("[@ItemNo]", itemNo);
                                sql = sql.Replace("[@KeyWord]", keyWord);
                                sql = sql.Replace("@PageSize", "" + pageSize);
                                sql = sql.Replace("@PageNumber", "" + pageNumber);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();
                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);
                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    recordsTotal = (int)result.FirstOrDefault()?.Total;
                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                            else if (memberType == "")  // tat ca
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ScriptExcute_POS_PromotionCheck.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", storeNo);
                                sql = sql.Replace("[@No]", offerNo);
                                sql = sql.Replace("[@OfferType]", offerType);
                                sql = sql.Replace("[@SalesType]", salesType);
                                sql = sql.Replace("[@MemberCode]", memberType);
                                sql = sql.Replace("[@Status]", status);
                                sql = sql.Replace("[@ItemNo]", itemNo);
                                sql = sql.Replace("[@KeyWord]", keyWord);
                                sql = sql.Replace("@PageSize", "" + pageSize);
                                sql = sql.Replace("@PageNumber", "" + pageNumber);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();

                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);
                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    recordsTotal = (int)result.FirstOrDefault()?.Total;
                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                recordsTotal = 0;
            }
            recordsTotal = (result != null && result.Count > 0) ? (int)result.FirstOrDefault()?.Total : 0;
            return result;
        }

        public ActionResult ExportExcel_CheckPromotion(string SourceType, string IPAddress, string StoreNo, string OfferNo, string OfferType, string SalesType, string MemberType, string Status, string ItemNo, string KeyWord)
        {
            int totalrow = 0;
            var data = new List<CheckPromotionModel>();
            if (SourceType == "SERVER")
            {
                data = _promotionBLO.ExportExcelCheckPromotion(StoreNo, OfferNo, OfferType, SalesType, MemberType, Status, ItemNo, KeyWord, out totalrow, 0, 1000000);
            }
            else if (SourceType == "POS")
            {
                data = ExportExcel_ExcuteScriptPOS(IPAddress, StoreNo, OfferNo, OfferType, SalesType, MemberType, Status, ItemNo, KeyWord);
            }
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportExcelCheckPromotionModel
                    {
                        StoreNo = x.StoreNo,
                        BonusBuyNo = x.BonusbuyNo,
                        PromotionNo = x.PromotionNo,
                        Description = x.Description,
                        OfferType = x.OfferType,
                        DiscountTypeStr = x.DiscountTypeStr,
                        SalesTypeName = x.SalesTypeName,
                        Status = x.Status,
                        Barcode = x.Barcode,
                        ItemNo = x.ItemNo,
                        ItemName = x.ItemName,
                        UOM = x.UOM,
                        SalesPrice = x.SalesPrice,
                        Discount = x.Discount,
                        Amount = x.Amount, 
                        StartingDate = x.StartingDate,
                        EndingDate = x.EndingDate,
                        MemberOnly = x.MemberOnly,
                        MemberCode = x.MemberCode,
                        LastDateModified = x.LastDateModified
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Bonus Buy";
                    worksheet.Cells[1, 3].Value = "Mã CTKM";
                    worksheet.Cells[1, 4].Value = "Tên CTKM";
                    worksheet.Cells[1, 5].Value = "Loại CTKM";
                    worksheet.Cells[1, 6].Value = "Loại giảm giá";
                    worksheet.Cells[1, 7].Value = "Hình thức bán hàng";
                    worksheet.Cells[1, 8].Value = "Trạng thái";
                    worksheet.Cells[1, 9].Value = "Barcode";
                    worksheet.Cells[1, 10].Value = "Mã sản phẩm";
                    worksheet.Cells[1, 11].Value = "Tên sản phẩm";
                    worksheet.Cells[1, 12].Value = "Đơn vị tính";
                    worksheet.Cells[1, 13].Value = "Đơn giá";
                    worksheet.Cells[1, 14].Value = "Giảm giá";
                    worksheet.Cells[1, 15].Value = "Thành tiền";
                    worksheet.Cells[1, 16].Value = "Ngày bắt đầu hiệu lực";
                    worksheet.Cells[1, 17].Value = "Ngày hết hiệu lực";
                    worksheet.Cells[1, 18].Value = "Thẻ thành viên";
                    worksheet.Cells[1, 19].Value = "Hạng thẻ";
                    worksheet.Cells[1, 20].Value = "Ngày cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 20])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"CheckPromotionList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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


        public List<CheckPromotionModel> ExportExcel_ExcuteScriptPOS(string IPAddress, string StoreNo, string OfferNo, string OfferType, string SalesType, string MemberType, string Status, string ItemNo, string KeyWord)
        {
            var result = new List<CheckPromotionModel>();
            try
            {
                bool bIsConnected = false;
                var messagePort = "";
                if (!Utils.CheckPort(IPAddress, 1433, ref messagePort))
                {
                    bIsConnected = false;
                }
                else
                {
                    bIsConnected = true;
                }

                if (bIsConnected)
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", IPAddress, "POS", "POS@1234", "StorePLH");
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            if (MemberType == "All")  // All loại thẻ
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ExportExcel_POS_PromotionCheck_All.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", StoreNo);
                                sql = sql.Replace("[@No]", OfferNo);
                                sql = sql.Replace("[@OfferType]", OfferType);
                                sql = sql.Replace("[@SalesType]", SalesType);
                                sql = sql.Replace("[@MemberCode]", MemberType);
                                sql = sql.Replace("[@Status]", Status);
                                sql = sql.Replace("[@ItemNo]", ItemNo);
                                sql = sql.Replace("[@KeyWord]", KeyWord);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();

                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);

                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                            else if (MemberType == "Vip" || MemberType == "Diamond" || MemberType == "Member")
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ExportExcel_POS_PromotionCheck_Member.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", StoreNo);
                                sql = sql.Replace("[@No]", OfferNo);
                                sql = sql.Replace("[@OfferType]", OfferType);
                                sql = sql.Replace("[@SalesType]", SalesType);
                                sql = sql.Replace("[@MemberCode]", MemberType);
                                sql = sql.Replace("[@Status]", Status);
                                sql = sql.Replace("[@ItemNo]", ItemNo);
                                sql = sql.Replace("[@KeyWord]", KeyWord);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();

                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);

                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                            else if (MemberType == "0")  // Không cần thẻ thành viên
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ExportExcel_POS_PromotionCheck_No.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", StoreNo);
                                sql = sql.Replace("[@No]", OfferNo);
                                sql = sql.Replace("[@OfferType]", OfferType);
                                sql = sql.Replace("[@SalesType]", SalesType);
                                sql = sql.Replace("[@MemberCode]", MemberType);
                                sql = sql.Replace("[@Status]", Status);
                                sql = sql.Replace("[@ItemNo]", ItemNo);
                                sql = sql.Replace("[@KeyWord]", KeyWord);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();

                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);

                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                            else if (MemberType == "")  // tất cả
                            {
                                var fileExute = AppDomain.CurrentDomain.BaseDirectory + "\\SqlScript\\ExportExcel_POS_PromotionCheck.txt";
                                var sql = System.IO.File.ReadAllText(fileExute);

                                sql = sql.Replace("[@StoreNo]", StoreNo);
                                sql = sql.Replace("[@No]", OfferNo);
                                sql = sql.Replace("[@OfferType]", OfferType);
                                sql = sql.Replace("[@SalesType]", SalesType);
                                sql = sql.Replace("[@MemberCode]", MemberType);
                                sql = sql.Replace("[@Status]", Status);
                                sql = sql.Replace("[@ItemNo]", ItemNo);
                                sql = sql.Replace("[@KeyWord]", KeyWord);

                                SqlCommand command = new SqlCommand(sql, connection);
                                try
                                {
                                    StringBuilder sb = new StringBuilder();
                                    connection.Open();

                                    var reader = command.ExecuteReader();
                                    var dataTable = new DataTable();
                                    dataTable.Load(reader);
                                    var countData = dataTable.Rows.Count;
                                    var data = JsonConvert.SerializeObject(dataTable);

                                    result = JsonConvert.DeserializeObject<List<CheckPromotionModel>>(data);
                                    reader.Close();
                                    connection.Close();
                                }
                                catch (Exception ex)
                                {
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public ActionResult ExportToExcel_PromotionOfferBuy(string OfferNo)
        {
            var data = _promotionBLO.ExportToExcel_PromotionOfferBuy(OfferNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OfferNo,
                        x.LineNo,
                        x.LineType,
                        x.No,
                        x.Description,
                        x.UnitOfMeasure,
                        x.DiscountTypeStr,
                        x.DiscountValue,
                        x.Quantity,
                        x.Step,
                        x.BonusBuyNo,
                        x.LineGroup,
                        x.ScaleTypeStr,
                        x.Counter,
                        x.Pkey
                    }).ToList();

                    worksheet.Cells[1, 1].Value     = "OfferNo";
                    worksheet.Cells[1, 2].Value     = "LineNo";
                    worksheet.Cells[1, 3].Value     = "LineType";
                    worksheet.Cells[1, 4].Value     = "No";
                    worksheet.Cells[1, 5].Value     = "Description";
                    worksheet.Cells[1, 6].Value     = "UnitOfMeasure";
                    worksheet.Cells[1, 7].Value     = "DiscountTypeStr";
                    worksheet.Cells[1, 8].Value     = "DiscountValue";
                    worksheet.Cells[1, 9].Value     = "Quantity";
                    worksheet.Cells[1, 10].Value    = "Step";
                    worksheet.Cells[1, 11].Value    = "BonusBuyNo";
                    worksheet.Cells[1, 12].Value    = "LineGroup";
                    worksheet.Cells[1, 13].Value    = "ScaleTypeStr";
                    worksheet.Cells[1, 14].Value    = "Counter";
                    worksheet.Cells[1, 15].Value    = "Pkey";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 15])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"OfferBuyList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public ActionResult ExportToExcel_PromotionOfferGet(string OfferNo)
        {
            var data = _promotionBLO.ExportToExcel_PromotionOfferGet(OfferNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OfferNo,
                        x.LineNo,
                        x.LineType,
                        x.No,
                        x.Description,
                        x.UnitOfMeasure,
                        x.DiscountTypeStr,
                        x.DiscountValue,
                        x.Quantity,
                        x.Step,
                        x.BonusBuyNo,
                        x.LineGroup,
                        x.ScaleTypeStr,
                        x.Counter,
                        x.Pkey
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "OfferNo";
                    worksheet.Cells[1, 2].Value = "LineNo";
                    worksheet.Cells[1, 3].Value = "LineType";
                    worksheet.Cells[1, 4].Value = "No";
                    worksheet.Cells[1, 5].Value = "Description";
                    worksheet.Cells[1, 6].Value = "UnitOfMeasure";
                    worksheet.Cells[1, 7].Value = "DiscountTypeStr";
                    worksheet.Cells[1, 8].Value = "DiscountValue";
                    worksheet.Cells[1, 9].Value = "Quantity";
                    worksheet.Cells[1, 10].Value = "Step";
                    worksheet.Cells[1, 11].Value = "BonusBuyNo";
                    worksheet.Cells[1, 12].Value = "LineGroup";
                    worksheet.Cells[1, 13].Value = "ScaleTypeStr";
                    worksheet.Cells[1, 14].Value = "Counter";
                    worksheet.Cells[1, 15].Value = "Pkey";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 15])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"OfferGetList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        public ActionResult ExportToExcel_PromotionOfferSite(string OfferNo)
        {
            var data = _promotionBLO.ExportToExcel_PromotionOfferSite(OfferNo);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.OfferNo,
                        x.PriceGroupCode,
                        x.StoreNo,
                        x.StyleProfile,
                        x.Counter,
                        x.Pkey
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "OfferNo";
                    worksheet.Cells[1, 2].Value = "PriceGroupCode";
                    worksheet.Cells[1, 3].Value = "StoreNo";
                    worksheet.Cells[1, 4].Value = "Kênh";
                    worksheet.Cells[1, 5].Value = "Counter";
                    worksheet.Cells[1, 6].Value = "Pkey";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 6])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"OfferSiteList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [HttpGet]
        public ActionResult _ViewPromotionCheckForItem(string bonusbuyNo, string promotionNo, string barcodeNo, string itemNo)
        {
            var result = new List<ViewCheckPromotionModalModel>();
            try
            {
                result = _promotionBLO.GetListViewPromotionCheck(bonusbuyNo, promotionNo, barcodeNo, itemNo);
            }
            catch (Exception ex)
            {
            }
            return PartialView(result);
        }










    }
}