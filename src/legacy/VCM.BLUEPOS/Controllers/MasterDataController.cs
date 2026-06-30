using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Data;
using System.Data.OleDb;
using Spire.Xls;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using System.Drawing;
using System.Web.Configuration;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Helpers;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Enums;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.POS;
using VCM.BLUEPOS.Model.Bank;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Voucher;
using VCM.BLUEPOS.Model.MasterData;
using VCM.BLUEPOS.Business.MasterData;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class MasterDataController : BaseController
    {
        string urlImgServer = WebConfigurationManager.AppSettings["urlImg"];
        string cdn_Image = WebConfigurationManager.AppSettings["CDN_Image"];
        private IMasterDataBLO _masterDataBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;

        public MasterDataController(IMasterDataBLO masterDataBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO)
        {
            _masterDataBLO = masterDataBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
        }
        public void WriteLog(string functionName, string message)
        {
            var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
            var content = $"{now} {functionName}         {message}";
            var path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
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

        [DisplayName("Danh mục nhân viên")]
        public ActionResult EmployeeList()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(uerName);
            return View();
        }

        [HttpPost]
        public JsonResult GetEmployeeList()
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

            var staffCode = Request?.Form["StaffCode"];
            if (!string.IsNullOrEmpty(staffCode))
            {
                staffCode = staffCode.Trim();
            }
            var staffName = Request?.Form["StaffName"];
            if (!string.IsNullOrEmpty(staffName))
            {
                staffName = staffName.Trim();
            }
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }
            var typeGroup = Request?.Form["TypeGroup"];
            if (!string.IsNullOrEmpty(typeGroup))
            {
                typeGroup = typeGroup.Trim();
            }
            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }
            var data = _masterDataBLO.GetEmployeeList(staffCode, staffName, storeNo, typeGroup, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<EmployeeListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }        
        public ActionResult ExportEmployeeList(string StaffCode, string StaffName, string StoreNo, string TypeGroup, string StatusActive)
        {
            var data = _masterDataBLO.ExportEmployeeList(StaffCode, StaffName, StoreNo, TypeGroup, StatusActive);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportEmployeeListModel
                    {
                        StaffCode = x.StaffCode,
                        FirstName = x.FirstName,
                        Status = x.Status,
                        Password = x.Password,
                        VoidTransaction = x.VoidTransaction,
                        TypeGroup = x.TypeGroup,
                        PermissionGroup = x.PermissionGroup,
                        HomePhoneNo = x.HomePhoneNo,
                        StoreNo = x.StoreNo,
                        StoreName = x.StoreName,
                        LastDateModifiedStr = x.LastDateModifiedStr
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã nhân viên";
                    worksheet.Cells[1, 2].Value = "Tên nhân viên";
                    worksheet.Cells[1, 3].Value = "Trạng thái hoạt động";
                    worksheet.Cells[1, 4].Value = "Mật khẩu";
                    worksheet.Cells[1, 5].Value = "Quyền hủy bill";
                    worksheet.Cells[1, 6].Value = "Nhóm loại";
                    worksheet.Cells[1, 7].Value = "Nhóm quyền";
                    worksheet.Cells[1, 8].Value = "Số điện thoại";
                    worksheet.Cells[1, 9].Value = "Mã cửa hàng";
                    worksheet.Cells[1, 10].Value = "Tên cửa hàng";
                    worksheet.Cells[1, 11].Value = "Ngày cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 11])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"EmployeeList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public JsonResult CheckStaffCode(string staffCode)
        {
            if (string.IsNullOrEmpty(staffCode))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.ErrorSystem,
                    Message = "Nhập mã nhân viên"
                });
            }
            var data = _commonBLO.CheckStaffCode(staffCode);
            return Json(data.Item2);
        }
        [HttpPost]
        public JsonResult CreateEmployeeList(CreateEmployeeListModel req)
        {
            var data = _masterDataBLO.CreateEmployeeList(req);
            return Json(data);
        }
        [HttpPost]
        public JsonResult UpdateEmployeeList(UpdateEmployeeListModel req)
        {
            var data = _masterDataBLO.UpdateEmployeeList(req);
            return Json(data);
        }
        [HttpPost]
        public JsonResult DeleteEmployeeList(DeleteEmployeeListModel req)
        {
            var data = _masterDataBLO.DeleteEmployeeList(req);
            return Json(data);
        }

        [DisplayName("Khai báo máy POS bán hàng")]
        public ActionResult SetupPOSList()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(uerName);
            ViewBag.PermisionPOS = _commonBLO.GetPermisionPOSTerminal();
            return View();
        }

        [HttpPost]
        public JsonResult GetSetupPOSList()
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

            var _storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(_storeNo))
            {
                _storeNo = _storeNo.Trim();
            }

            var _ipAddress = Request?.Form["IPAddress"];
            if (!string.IsNullOrEmpty(_ipAddress))
            {
                _ipAddress = _ipAddress.Trim();
            }

            var _posID = Request?.Form["POSID"];
            if (!string.IsNullOrEmpty(_posID))
            {
                _posID = _posID.Trim();
            }

            var _styleProfile = Request?.Form["StyleProfile"];
            if (!string.IsNullOrEmpty(_styleProfile))
            {
                _styleProfile = _styleProfile.Trim();
            }

            var _status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(_status))
            {
                _status = _status.Trim();
            }

            var _posType = Request?.Form["POSType"];
            if (!string.IsNullOrEmpty(_posType))
            {
                _posType = _posType.Trim();
            }

            var data = _masterDataBLO.GetSetupPOSList(_ipAddress, _posID, _storeNo, _styleProfile, _posType, _status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<SetupPOSListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        public ActionResult ExportExcel_SetupPOSList(string IpAddress, string PosID, string StoreNo, string ChanelNo, string POSType, string Status)
        {
            var data = _masterDataBLO.ExportExcel_SetupPOSList(IpAddress, PosID, StoreNo, ChanelNo, POSType, Status);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportSetupPOSListModel
                    {
                        POSID = x.POSID,
                        IPAddress = x.IPAddress,
                        StoreNo = x.StoreNo,
                        StyleProfile = x.StyleProfile,          // Kênh
                        DefaultSalesType = x.DefaultSalesType,  // Quyền
                        Status = x.Status,
                        TerminalNetworkID = x.TerminalNetworkID,
                        Placement = x.Placement,
                        CustomerDisplayText1 = x.CustomerDisplayText1,
                        CreatedBy = x.CreatedBy,
                        CreatedDateStr = x.CreatedDateStr,
                        UpdatedBy = x.UpdatedBy,
                        UpdatedDateStr = x.UpdatedDateStr
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Máy POS";
                    worksheet.Cells[1, 2].Value = "Địa chỉ IP";
                    worksheet.Cells[1, 3].Value = "ST/CH";
                    worksheet.Cells[1, 4].Value = "Kênh";
                    worksheet.Cells[1, 5].Value = "Quyền";
                    worksheet.Cells[1, 6].Value = "Trạng thái";
                    worksheet.Cells[1, 7].Value = "Folder";
                    worksheet.Cells[1, 8].Value = "Placement";
                    worksheet.Cells[1, 9].Value = "Lời chào";
                    worksheet.Cells[1, 10].Value = "Người tạo";
                    worksheet.Cells[1, 11].Value = "Ngày tạo";
                    worksheet.Cells[1, 12].Value = "Người cập nhật";
                    worksheet.Cells[1, 13].Value = "Ngày cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 13])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"POSTerminalList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public JsonResult GetMaxPosIDPOSTerminal(string storeNo)
        {
            var data = _masterDataBLO.GetMaxPosIDPOSTerminal(storeNo);
            return Json(data);
        }

        [HttpPost]
        public JsonResult CreatePOSTerminalList(CreatePOSTerminalListModel req)
        {
            /*
            if (string.IsNullOrEmpty(req.SalesChanelType))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Chọn loại máy POS bán hàng"
                });
            }
            */
            if (string.IsNullOrEmpty(req.StoreNo))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Chọn cửa hàng"
                });
            }

            if (req.SalesChanelType == "POS" && string.IsNullOrEmpty(req.IPAddress))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Nhập địa chỉ IP máy POS bán hàng"
                });
            }

            
            //if (string.IsNullOrEmpty(req.POSID))
            //{
            //    return Json(new ResultResponseModel
            //    {
            //        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
            //        Message = "Nhập số máy POS (số nguyên dương > 0, 6 ký tự số)"
            //    });
            //}

            //if (HelpersExtension.IsNumber(req.POSID) == false)
            //{
            //    return Json(new ResultResponseModel
            //    {
            //        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
            //        Message = "Số máy POS phải là số nguyên dương"
            //    });
            //}

            //if (!string.IsNullOrEmpty(req.POSID))
            //{
            //    if ((req.POSID.Trim().Length < 6) || (req.POSID.Trim().Length > 6))
            //    {
            //        return Json(new ResultResponseModel {
            //            Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
            //            Message = "Số máy POS gồm 6 ký tự số nguyên dương"
            //        });
            //    }
            //}

            /* Tam thoi khong check cai nay
                ViewBag.MaxPOSID = _masterDataBLO.GetMaxPosIDPOSTerminal(req.StoreNo);
           */

            req.CreatedBy = LoginUser.UserName;
            var data = _masterDataBLO.CreatePOSTerminalList(req);
            return Json(data);

        }

        [HttpPost]
        public JsonResult UpdatePOSTerminalList(UpdatePOSTerminalListModel req)
        {
            if (req.SalesChanelType == "POS" && string.IsNullOrEmpty(req.IPAddress))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Nhập địa chỉ IP máy POS bán hàng"
                });
            }

            req.UpdatedBy = LoginUser.UserName;
            var data = _masterDataBLO.UpdatePOSTerminalList(req);
            return Json(data);

        }

        [HttpPost]
        public JsonResult DeletePOSTerminalList(DeletePOSTerminalListModel req)
        {
            if (string.IsNullOrEmpty(req.POSID))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Chọn máy POS để xóa"
                });
            }

            var data = _masterDataBLO.DeletePOSterminalList(req);
            return Json(data);
        }

        [HttpPost]
        public ActionResult ImportExcelPOSTerminal(HttpPostedFileBase importFile)
        {
            if (importFile == null)
            {
                return Json(new
                {
                    Status = 0,
                    Message = "Không có file nào được chọn"
                });
            }

            string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                return Json(new {
                    Status = 0,
                    Message = "Định dạng file excel không đúng"
                });
            }

            try
            {
                var createUserName = base.LoginUser.UserName;
                var streamData = importFile.InputStream;

                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);
                    Worksheet sheet = workbook.Worksheets[0];

                    var data = sheet.ExportDataTable();
                    var insert = _masterDataBLO.ImportExcelPOSTerminal(createUserName, data);

                    if (insert.Item1 == 0 || insert.Item1 < 0)
                    {
                        return Json(new {
                            Status = 0,
                            Message = $"{insert.Item2}"
                        });
                    }

                    return Json(new {
                        Status = 1,
                        Message = $"Import danh sách thành công {insert.Item1} dòng"
                    });

                }
                catch (Exception e)
                {
                    return Json(new {
                        Status = 0,
                        Message = e.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new {
                    Status = 0,
                    Message = ex.Message
                });
            }
        }

        [DisplayName("Danh mục siêu thị, cửa hàng")]
        public ActionResult StoreList()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListProvince = _commonBLO.GetComboxProvince();
            return View();
        }
        [HttpPost]
        public JsonResult GetStoreList()
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

            var provinceNo = Request?.Form["ProvinceNo"];
            if (!string.IsNullOrEmpty(provinceNo))
            {
                provinceNo = provinceNo.Trim();
            }

            var chanelNo = Request?.Form["ChanelNo"];
            if (!string.IsNullOrEmpty(chanelNo))
            {
                chanelNo = chanelNo.Trim();
            }

            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }
            
            var taxCode = "";
            var data = _masterDataBLO.GetStoreList(provinceNo, chanelNo, storeNo, taxCode, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<StoreListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcelStoreList(string ProvinceNo, string ChanelNo, string StoreNo)
        {
            var data = _masterDataBLO.ExportExcel_StoreList(ProvinceNo, ChanelNo, StoreNo, "");
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.StyleProfile,
                        x.StoreNo,
                        x.StoreName,
                        x.Address,
                        x.StoreOpenTime,
                        x.StoreCloseTime,
                        x.PhoneNo,
                        x.PasswordWifi,
                        x.ProvinceCode,
                        x.ProvinceName,
                        x.IssueVoucherOnline,
                        x.LastDateModifiedStr
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Kênh bán hàng";
                    worksheet.Cells[1, 2].Value = "ST/CH";
                    worksheet.Cells[1, 3].Value = "Tên ST/CH";                  
                    worksheet.Cells[1, 4].Value = "Địa chỉ";
                    worksheet.Cells[1, 5].Value = "Giờ mở cửa";
                    worksheet.Cells[1, 6].Value = "Giờ đóng cửa";
                    worksheet.Cells[1, 7].Value = "Điện thoại";
                    worksheet.Cells[1, 8].Value = "Password Wifi";
                    worksheet.Cells[1, 9].Value = "Mã tỉnh thành";
                    worksheet.Cells[1, 10].Value = "Tên tỉnh thành";
                    worksheet.Cells[1, 11].Value = "Phát hành voucher online";
                    worksheet.Cells[1, 12].Value = "Ngày cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 12])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }
                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    string fileName = $"StoreList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public JsonResult CreateStoreList(CreateStoreListModel req)
        {
            if (req.StoreName == null || req.StoreName == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng nhập tên ST/CH"
                });
            }
            if (req.Address == null || req.Address == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng nhập địa chỉ ST/CH"
                });
            }

            if (req.OpenTime == null || req.OpenTime == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng chọn giờ mở cửa ST/CH"
                });
            }

            if (req.CloseTime == null || req.CloseTime == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng chọn giờ đóng cửa ST/CH"
                });
            }

            // Kiểm tra mã số thuế

            if (!string.IsNullOrEmpty(req.TaxCode))
            {
                if (req.TaxCode.Trim().Length < 10)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "MST bắt buộc lớn hơn hoặc bằng 10 ký tự số. Vui lòng kiểm tra lại"
                    });
                }

                if (req.TaxCode.Trim().Length == 10)
                {
                    Regex regex10 = new Regex(@"^[0-9]*$");
                    var checkTaxCode10 = regex10.Match(req.TaxCode.Trim());
                    if (checkTaxCode10.Length == 0)
                    {
                        return Json(new ResultResponseModel
                        {
                            Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                            Message = "MST có 10 ký tự số. Vui lòng kiểm tra lại"
                        });
                    }
                }

                if (req.TaxCode.Trim().Length > 10 && req.TaxCode.Trim().Length != 14)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "MST có 14 ký tự bao gồm: số và dấu ngạch ngang (-). Vui lòng kiểm tra lại"
                    });
                }

                if (req.TaxCode.Trim().Length == 14)
                {
                    Regex regex14 = new Regex(@"^[0-9-]*$");
                    var checkTaxCode14 = regex14.Match(req.TaxCode.Trim());
                    if (checkTaxCode14.Length == 0)
                    {
                        return Json(new ResultResponseModel
                        {
                            Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                            Message = "MST có 14 ký tự bao gồm: số và dấu ngạch ngang (-). Vui lòng kiểm tra lại"
                        });
                    }
                }
            }

            var data = _masterDataBLO.CreateStoreList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateStoreList(UpdateStoreListModel req)
        {
            if (req.StoreName == null || req.StoreName == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng nhập tên ST/CH"
                });
            }
            if (req.Address == null || req.Address == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng nhập địa chỉ ST/CH"
                });
            }
            if (req.OpenTime == null || req.OpenTime == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng chọn giờ mở cửa ST/CH"
                });
            }
            if (req.CloseTime == null || req.CloseTime == "")
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                    Message = "Vui lòng chọn giờ đóng cửa ST/CH"
                });
            }

            // Kiểm tra mã số thuế

            if (!string.IsNullOrEmpty(req.TaxCode))
            {
                if (req.TaxCode.Trim().Length < 10)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "MST bắt buộc lớn hơn hoặc bằng 10 ký tự số. Vui lòng kiểm tra lại"
                    });
                }

                if (req.TaxCode.Trim().Length == 10)
                {
                    Regex regex10 = new Regex(@"^[0-9]*$");
                    var checkTaxCode10 = regex10.Match(req.TaxCode.Trim());
                    if (checkTaxCode10.Length == 0)
                    {
                        return Json(new ResultResponseModel
                        {
                            Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                            Message = "MST có 10 ký tự số. Vui lòng kiểm tra lại"
                        });
                    }
                }

                if (req.TaxCode.Trim().Length > 10 && req.TaxCode.Trim().Length != 14)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                        Message = "MST có 14 ký tự bao gồm: số và dấu ngạch ngang (-). Vui lòng kiểm tra lại"
                    });
                }

                if (req.TaxCode.Trim().Length == 14)
                {
                    Regex regex14 = new Regex(@"^[0-9-]*$");
                    var checkTaxCode14 = regex14.Match(req.TaxCode.Trim());
                    if (checkTaxCode14.Length == 0)
                    {
                        return Json(new ResultResponseModel
                        {
                            Status = VCM.BLUEPOS.Model.Enums.ResultEnum.Fail,
                            Message = "MST có 14 ký tự bao gồm: số và dấu ngạch ngang (-). Vui lòng kiểm tra lại"
                        });
                    }
                }
            }

            var data = _masterDataBLO.UpdateStoreList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteStoreList(DeleteStoreListModel req)
        {
            var data = _masterDataBLO.DeleteStoreList(req);
            return Json(data);
        }


        [HttpPost]
        [AllowAnonymous]
        public JsonResult CheckStoreNo(string storeNo)
        {
            if (string.IsNullOrEmpty(storeNo))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng nhập mã ST/CH"
                });
            }
            else
            {
                if (storeNo.Trim().Length < 4)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                        Message = "Mã ST/CH bắt buộc lớn hơn hoặc bằng 4 ký tự số. Vui lòng kiểm tra lại"
                    });
                }
                if (storeNo.Trim().Length == 4)
                {
                    Regex regex4 = new Regex(@"^[0-9]*$");
                    var checkStoreNo4 = regex4.Match(storeNo.Trim());
                    if (checkStoreNo4.Length == 0)
                    {
                        return Json(new ResultResponseModel
                        {
                            Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                            Message = "Mã ST/CH là 4 ký tự số. Vui lòng kiểm tra lại"
                        });
                    }
                }
            }
            var data = _masterDataBLO.CheckStoreNo(storeNo);
            return Json(data.Item2);
        }

        [DisplayName("Danh mục ngân hàng")]
        public ActionResult BankList()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetBankList()
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

            var bankCode = Request?.Form["BankCode"];
            if (!string.IsNullOrEmpty(bankCode))
            {
                bankCode = bankCode.Trim();
            }
            var bankName = Request?.Form["BankName"];
            if (!string.IsNullOrEmpty(bankName))
            {
                bankName = bankName.Trim();
            }
            var data = _masterDataBLO.GetBankList(bankCode, bankName, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<BankListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }
        public ActionResult ExportBankList(string BankCode, string BankName)
        {
            var data = _masterDataBLO.ExportGetBankList(BankCode, BankName);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.BankCode,
                        x.BankName,
                        x.CreatedDateStr,
                        x.UpdatedDateStr
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã Ngân hàng";
                    worksheet.Cells[1, 2].Value = "Tên Ngân hàng";
                    worksheet.Cells[1, 3].Value = "Ngày tạo";
                    worksheet.Cells[1, 4].Value = "Ngày cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 4])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"BankList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public JsonResult CreateBankList(CreateBankModel req)
        {
            if (string.IsNullOrEmpty(req.BankCode))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng nhập mã ngân hàng"
                });
            }
            if (string.IsNullOrEmpty(req.BankName))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng nhập tên ngân hàng"
                });
            }
            var data = _masterDataBLO.CreateBankList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateBankList(UpdateBankModel req)
        {
            if (string.IsNullOrEmpty(req.BankName))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng nhập tên ngân hàng"
                });
            }
            var data = _masterDataBLO.UpdateBankList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteBankList(DeleteBankModel req)
        {
            var data = _masterDataBLO.DeleteBankList(req);
            return Json(data);
        }

        [DisplayName("Khai báo máy POS ngân hàng")]
        public ActionResult BankPOSList()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(uerName);
            ViewBag.ListBank = _commonBLO.LoadComboxBankList();
            return View();
        }

        [HttpPost]
        public JsonResult GetBankPOSList()
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

            var status = Request?.Form["Status"];
            if (status == "true")
            {
                status = "1";
            }
            else if (status == "false")
            {
                status = "0";
            }
            else
            {
                status = "-1";
            }
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }
            var bankCode = Request?.Form["BankCode"];
            if (!string.IsNullOrEmpty(bankCode))
            {
                bankCode = bankCode.Trim();
            }
            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }
            var data = _masterDataBLO.GetBankPOSList(storeNo, textSearch, bankCode, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<BankPOSListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }
        public ActionResult ExportBankPOSList(string storeNo, string bankCode, string status, string textSearch)
        {
            var data = _masterDataBLO.ExportBankPOSList(storeNo, bankCode, status, textSearch);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportBankPOSListModel
                    {
                        BankPOSCode = x.BankPOSCode,
                        POSTerminal = x.POSTerminal,
                        POSNo = x.POSNo,
                        StoreNo = x.StoreNo,
                        StoreName = x.StoreName,
                        BankCode = x.BankCode,
                        BankPOSName = x.BankPOSName,
                        IsOnline = x.IsOnline,
                        Status = x.Status,
                        CreatedDateStr = x.CreatedDateStr,
                        CreatedUser = x.CreatedUser,
                        UpdatedDateStr = x.UpdatedDateStr,
                        UpdatedUser = x.UpdatedUser
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã máy cà thẻ";             
                    worksheet.Cells[1, 2].Value = "Máy POS";
                    worksheet.Cells[1, 3].Value = "POSNo";
                    worksheet.Cells[1, 4].Value = "ST/CH";
                    worksheet.Cells[1, 5].Value = "Tên ST/CH";
                    worksheet.Cells[1, 6].Value = "Mã ngân hàng";              
                    worksheet.Cells[1, 7].Value = "Tên ngân hàng";             
                    worksheet.Cells[1, 8].Value = "Thanh toán Online";         
                    worksheet.Cells[1, 9].Value = "Trạng thái hoạt động";      
                    worksheet.Cells[1, 10].Value = "Ngày tạo";
                    worksheet.Cells[1, 11].Value = "Người tạo";
                    worksheet.Cells[1, 12].Value = "Ngày cập nhật";
                    worksheet.Cells[1, 13].Value = "Người cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 13])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"BankPOSList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public JsonResult CreateBankPOSList(CreateBankPOSListModel req)
        {
            req.CreatedUser = LoginUser.UserName;
            var data = _masterDataBLO.CreateBankPOSList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateBankPOSList(UpdateBankPOSListModel req)
        {
            req.UpdatedUser = LoginUser.UserName;
            var data = _masterDataBLO.UpdateBankPOSList(req);
            return Json(data);
        }
        public JsonResult DeleteBankPOSList(DeleteBankPOSListModel req)
        {
            var data = _masterDataBLO.DeleteBankPOSList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetBankPOSCode(string storeNo, string bankCode, string posNo)
        {
            var bankposcode = storeNo + "_" + bankCode + "_" + posNo;
            var data = _masterDataBLO.GetBankPOSCode(storeNo, bankCode, posNo);
            if (string.IsNullOrEmpty(data))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Đã tồn tại mã " + bankposcode + " này trong hệ thống. Vui lòng kiểm tra lại"
                });
            }
            else
            {
                return Json(data);
            }
        }

        [HttpPost]
        public JsonResult GetBankPOSName(string bankCode)
        {
            var data = _masterDataBLO.GetBankPOSName(bankCode);
            return Json(data);
        }

        [HttpPost]
        public ActionResult ImportExcelBankPOS(HttpPostedFileBase importFile)
        {
            if (importFile == null) return Json(new { Status = 0, Message = "Không có file nào được chọn" });
            string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                return Json(new { Status = 0, Message = "Định dạng file excel không đúng." });
            }
            try
            {
                var createUserName = base.LoginUser.UserName;
                var streamData = importFile.InputStream;
                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);
                    Worksheet sheet = workbook.Worksheets[0];
                    var data = sheet.ExportDataTable();
                    var insert = _masterDataBLO.ImportExcelBankPOS(createUserName, data);
                    if (insert.Item1 == 0 || insert.Item1 < 0)
                    {
                        return Json(new { Status = 0, Message = $"{insert.Item2} " });
                    }
                    return Json(new { Status = 1, Message = $"Import thành công {insert.Item1} dòng" });
                }
                catch (Exception e)
                {
                    return Json(new { Status = 0, Message = e.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = 0, Message = ex.Message });
            }
        }

        [DisplayName("Cập nhật POS Version")]
        public ActionResult POSVersionlist(UploadFilePOSDataSetupModel req)
        {
            ViewBag.ListSource = _commonBLO.LoadComboxPOSVersionSource();
            ViewBag.ListVersion = _commonBLO.LoadComboxPOSVersionList();
            ViewBag.ListIsUpdate = _commonBLO.LoadComboxPOSVersionIsUpdate();
            return View(req);
        }

        [HttpPost]
        public JsonResult GetPOSVersionList()
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
            var data = _masterDataBLO.GetPOSVersionList(out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<POSVersionResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult UpdatePOSVersionList(UpdatePOSVersionModel req)
        {
            var data = _masterDataBLO.UpdatePOSVersionList(req);
            return Json(data);
        }

        [DisplayName("Khai báo tỷ lệ đổi quà")]
        public ActionResult UpdateVoucherNumberPOS()
        {
            string _userName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(_userName);
            return View();
        }

        [HttpPost]
        public JsonResult GetVoucherNumberToPOSList()
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

            var voucherType = Request?.Form["VoucherType"];
            if (!string.IsNullOrEmpty(voucherType))
            {
                voucherType = voucherType.Trim();
            }

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

            var ipAddress = Request?.Form["IPAddress"];
            if (!string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = ipAddress.Trim();
            }
            var data = _masterDataBLO.GetVoucherNumberToPOSList(voucherType, storeNo, posID, ipAddress, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<POSDocumentNosResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult GetDocumentNoList()
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

            var posID = Request?.Form["PosID"];
            if (!string.IsNullOrEmpty(posID))
            {
                posID = posID.Trim();
            }

            var voucherType = Request?.Form["DocumnentType"];
            if (!string.IsNullOrEmpty(voucherType))
            {
                voucherType = voucherType.Trim();
            }

            var data = _masterDataBLO.GetDocumentNoList(storeNo, posID, voucherType, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<DocumentNoResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult UpdatePOSDocumentNo(UpdatePOSDocumentNoModel req)
        {
            if (string.IsNullOrEmpty(req.IPAddress))
            {
                return Json(new
                {
                    Status = 2,
                    Message = $"Không ping được IP : {req.IPAddress} này. Vui lòng kiểm tra lại"
                });
            }
            var data = _masterDataBLO.UpdatePOSDocumentNo(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult InsertPOSDocumentNo(InsertPOSDocumentNoModel req)
        {
            var data = _masterDataBLO.InsertPOSDocumentNo(req);
            return Json(data);
        }

        [DisplayName("Danh mục tỉnh thành")]
        public ActionResult ProvinceList()
        {
            return View();
        }
        [HttpPost]
        public JsonResult GetProvinceList()
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

            var provinceCode = Request?.Form["ProvinceCode"];
            if (!string.IsNullOrEmpty(provinceCode))
            {
                provinceCode = provinceCode.Trim();
            }
            var provinceName = Request?.Form["ProvinceName"];
            if (!string.IsNullOrEmpty(provinceName))
            {
                provinceName = provinceName.Trim();
            }
            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }
            var data = _masterDataBLO.GetProvinceList(provinceCode, provinceName, status, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<ProvinceListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult CreateProvinceList(CreateProvinceResponseModel req)
        {
            req.CreatedUser = LoginUser.UserName;
            var data = _masterDataBLO.CreateProvinceList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateProvinceList(UpdateProvinceResponseModel req)
        {
            req.LastUpdateUser = LoginUser.UserName;
            var data = _masterDataBLO.UpdateProvinceList(req);
            return Json(data);
        }
        [HttpPost]
        public JsonResult DeleteProvinceList(DeleteProvinceResponseModel req)
        {
            var data = _masterDataBLO.DeleteProvinceList(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult CheckProvinceCode(string provinceCode)
        {
            var data = _masterDataBLO.CheckProvinceCode(provinceCode);
            return Json(data.Item2);
        }

        [DisplayName("Khai báo Sales Order Type")]
        public ActionResult SetupSalesOrderType()
        {
            string _userName = LoginUser.UserName;
            var model = _commonBLO.LoadSalesOrderType();
            ViewBag.ListTenderType = _commonBLO.LoadTenderTypeForSalesType();
            ViewBag.ListPaymentTenderType = _commonBLO.LoadPaymentTenderTypeForSalesType();
            return View(model);
        }

        [HttpPost]
        public JsonResult GetSalesOrderTypeList()
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

            var salesType = Request?.Form["SalesType"];
            if (!string.IsNullOrEmpty(salesType))
            {
                salesType = salesType.Trim();
            }

            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }

            var data = _masterDataBLO.GetSalesOrderTypeList(salesType, status, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<SalesOrderTypeListModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public JsonResult CreateSalesOrderType(CreateSalesOrderTypeModel req)
        {
            req.CreatedBy = LoginUser.UserName;
            var data = _masterDataBLO.CreateSalesOrderType(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateSalesOrderType(UpdateSalesOrderTypeModel req)
        {
            req.UpdatedBy = LoginUser.UserName;
            var data = _masterDataBLO.UpdateSalesOrderType(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteSalesOrderType(DeleteSalesOrderTypeModel req)
        {
            var data = _masterDataBLO.DeleteSalesOrderType(req);
            return Json(data);
        }

        [DisplayName("Khai báo thẻ ngân hàng")]
        public ActionResult BankCardTypeList()
        {
            return View();
        }

        [HttpPost]
        public JsonResult LoadBankCardTypeList()
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

            var bankCardCode = Request?.Form["BankCardCode"];
            if (!string.IsNullOrEmpty(bankCardCode))
            {
                bankCardCode = bankCardCode.Trim();
            }

            var bankCardName = Request?.Form["BankCardName"];
            if (!string.IsNullOrEmpty(bankCardName))
            {
                bankCardName = bankCardName.Trim();
            }

            var _status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(_status))
            {
                _status = _status.Trim();
            }

            var data = _masterDataBLO.LoadBankCardType(bankCardCode, bankCardName, _status, out recordsTotal, skip, pageSize);
            return Json(new DataTablesViewModel<BankCardTypeResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public JsonResult CreateBankCardType(CreateBankCardTypeModel req)
        {
            var data = _masterDataBLO.CreateBankCardType(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateBankCardType(UpdateBankCardTypeModel req)
        {
            var data = _masterDataBLO.UpdateBankCardType(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteBankCardType(DeleteBankCardTypeModel req)
        {
            var data = _masterDataBLO.DeleteBankCardType(req);
            return Json(data);
        }

        public ActionResult SetupPartner()
        {
            string _userName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(_userName);
            return View();
        }

        //[HttpPost]
        //public JsonResult LoadPartnerSetup()
        //{
        //    var draw = Request?.Form["draw"];
        //    var start = Request?.Form["start"];
        //    var length = Request?.Form["length"];
        //    var sortColumn = Request?.Form["columns[" + Request.Form["order[0][column]"] + "][name]"];
        //    var sortColumnDirection = Request?.Form["order[0][dir]"];
        //    var searchValue = Request?.Form["search[value]"];
        //    var pageSize = length != null ? Convert.ToInt32(length) : 0;
        //    var skip = start != null ? Convert.ToInt32(start) : 0;
        //    var pageIndex = skip / pageSize;
        //    var recordsTotal = 0;

        //    var chanelNo = Request?.Form["ChanelNo"];
        //    var storeNo = Request?.Form["StoreNo"];
        //    var partnerCode = Request?.Form["PartnerCode"];
        //    var status = Request?.Form["Status"];

        //    if (!string.IsNullOrEmpty(chanelNo))
        //    {
        //        chanelNo = chanelNo.Trim();
        //    }

        //    if (!string.IsNullOrEmpty(storeNo))
        //    {
        //        storeNo = storeNo.Trim();
        //    }

        //    if (!string.IsNullOrEmpty(partnerCode))
        //    {
        //        partnerCode = partnerCode.Trim();
        //    }

        //    int exPort = 0;  // Không xuất excel
        //    var data = _storeposBLO.GetPartnerSetupList(chanelNo, storeNo, partnerCode, status, exPort, out recordsTotal, pageIndex, pageSize);
        //    return Json(new DataTablesViewModel<PartnerSetupResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        //}

        [DisplayName("Khai báo thanh toán ví điện tử")]
        public ActionResult SetupEWalletList()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(uerName);
            ViewBag.ListEWallet = _commonBLO.LoadComboxEWalletList();
            ViewBag.ListPaymentType = _commonBLO.LoadPaymentTypeForEWalletList();
            return View();
        }

        [HttpPost]
        public JsonResult LoadSetupEWalletList()
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

            var status = Request?.Form["Status"];
            if (status == "true")
            {
                status = "1";
            }
            else if (status == "false")
            {
                status = "0";
            }
            else
            {
                status = "-1";
            }
            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }
            var eWalletType = Request?.Form["EWalletType"];
            if (!string.IsNullOrEmpty(eWalletType))
            {
                eWalletType = eWalletType.Trim();
            }
            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }
            var data = _masterDataBLO.LoadSetupEWalletList(storeNo, eWalletType, textSearch, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<EWalletListResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        public ActionResult ExportExcelSetupEWalletList(string StoreNo, string EWalletType, string TextSearch, string Status)
        {
            var data = _masterDataBLO.ExportExcelSetupEWalletList(StoreNo, EWalletType, TextSearch, Status);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportExcelEWalletListModel
                    {
                        TerminalCode = x.TerminalCode,
                        POSNo = x.POSNo,
                        POSTerminal = x.POSTerminal,
                        StoreNo = x.StoreNo,
                        StoreName = x.StoreName,
                        Status = x.Status,
                        EWalletCode = x.EWalletCode,
                        EWalletName = x.EWalletName,
                        PaymentType = x.PaymentType,
                        TenderType = x.TenderType,
                        Mode = x.Mode,
                        IsTpay = x.IsTpay,
                        CreatedDateStr = x.CreatedDateStr,
                        CreatedUser = x.CreatedUser,
                        UpdatedDateStr = x.UpdatedDateStr,
                        UpdatedUser = x.UpdatedUser
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Mã máy thanh toán ví";              
                    worksheet.Cells[1, 2].Value = "POSNo";
                    worksheet.Cells[1, 3].Value = "Máy POS";                  
                    worksheet.Cells[1, 4].Value = "ST/CH";
                    worksheet.Cells[1, 5].Value = "Tên ST/CH";
                    worksheet.Cells[1, 6].Value = "Trạng thái";               
                    worksheet.Cells[1, 7].Value = "Mã loại ví điện tử";              
                    worksheet.Cells[1, 8].Value = "Tên loại ví điện tử";          
                    worksheet.Cells[1, 9].Value = "PaymentType";
                    worksheet.Cells[1, 10].Value = "TenderType";
                    worksheet.Cells[1, 11].Value = "Model";
                    worksheet.Cells[1, 12].Value = "Thanh toán Tpay";
                    worksheet.Cells[1, 13].Value = "Ngày tạo";
                    worksheet.Cells[1, 14].Value = "Người tạo";
                    worksheet.Cells[1, 15].Value = "Ngày cập nhật";
                    worksheet.Cells[1, 16].Value = "Người cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 16])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"EWalletList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public JsonResult GetEWalletTerminalCode(string storeNo, string eWalletCode, string posNo)
        {
            var terminalCode = storeNo + "_" + eWalletCode + "_" + posNo;
            var data = _masterDataBLO.GetEWalletTerminalCode(storeNo, eWalletCode, posNo);
            if (string.IsNullOrEmpty(data))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Đã tồn tại mã " + terminalCode + " này trong hệ thống. Vui lòng kiểm tra lại"
                });
            }
            else
            {
                return Json(data);
            }
        }

        [HttpPost]
        public JsonResult CreateSetupEWallet(CreateEWalletListModel req)
        {
            req.CreatedUser = LoginUser.UserName;
            var data = _masterDataBLO.CreateSetupEWallet(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateSetupEWallet(UpdateEWalletListModel req)
        {
            req.UpdatedUser = LoginUser.UserName;
            var data = _masterDataBLO.UpdateSetupEWallet(req);
            return Json(data);
        }
        public JsonResult DeleteSetupEWalletList(DeleteEWalletListModel req)
        {
            var data = _masterDataBLO.DeleteSetupEWalletList(req);
            return Json(data);
        }

        [HttpPost]
        public ActionResult ImportExcelEWallet(HttpPostedFileBase importFile)
        {
            if (importFile == null)
            {
                return Json(new
                {
                    Status = 0,
                    Message = "Không có file excel nào được chọn"
                });
            }

            string fileExtension = Path.GetExtension(importFile.FileName).ToLower();
            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                return Json(new {
                    Status = 0,
                    Message = "Định dạng file excel không đúng."
                });
            }

            try
            {
                var createUserName = base.LoginUser.UserName;
                var streamData = importFile.InputStream;
                try
                {
                    Workbook workbook = new Workbook();
                    workbook.LoadFromStream(streamData);
                    Worksheet sheet = workbook.Worksheets[0];

                    var data = sheet.ExportDataTable();
                    var insert = _masterDataBLO.ImportExcelEWallet(createUserName, data);

                    if (insert.Item1 == 0 || insert.Item1 < 0)
                    {
                        return Json(new { Status = 0, Message = $"{insert.Item2} " });
                    }

                    return Json(new { Status = 1, Message = $"Import file excel thành công {insert.Item1} dòng" });

                }
                catch (Exception e)
                {
                    return Json(new { Status = 0, Message = e.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { Status = 0, Message = ex.Message });
            }
        }

        [DisplayName("Đổi mật khẩu")]
        public ActionResult ChangePassWord()
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

        public JsonResult ChangePassWordList()
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

            var setServer = Request?.Form["SetDB"];
            var storeNo = Request?.Form["StoreNo"]; 
            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            // 19/03/2024 : phan quyen theo store
            var userName = base.LoginUser.UserName;
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
                var listValidSite = _commonBLO.CheckSecurityStoreByUserName(userName, storeNo);
                if (listValidSite == null | listValidSite.Count == 0)
                {
                    return Json(new DataTablesViewModel<ChangePassWordModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = new List<ChangePassWordModel>() });
                }
                storeNo = string.Join(";", listValidSite);
            }
            else
            {
                var listStoreNo = _commonBLO.LoadStoreByUserName(setServer, storeNo, userName);
                storeNo = string.Join(";", listStoreNo.Select(a => a.SiteNo));
            }

            var data = _masterDataBLO.ChangePassWordList(storeNo, userName, textSearch, out recordsTotal, pageIndex, pageSize);
            return Json(new DataTablesViewModel<ChangePassWordModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public JsonResult ChangePassWord(ChangePassWordModel req)
        {
            var _userName = LoginUser.UserName;
            req.UpdatedBy = _userName;
            req.UpdatedDate = DateTime.Now;
            var data = _masterDataBLO.ChangePassWord(req);
            return Json(data);
        }

        [DisplayName("Khai báo Images Slider")]
        public ActionResult SetupImageSlider()
        {
            ViewBag.StoreList = _commonBLO.GetComboxStoreList();
            return View();
        }
        public JsonResult GetSetupImageSliderList(string StoreNo)
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

            var listStore = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                string[] listStoreArray = JsonConvert.DeserializeObject<string[]>(StoreNo);
                
                foreach (var i in listStoreArray.ToList())
                {
                    listStore += i + ";";
                }
            }
            else
            {
                listStore = "";
            }

            var styleProfile = Request?.Form["StyleProfile"];

            var description = Request?.Form["Description"];
            if (!string.IsNullOrEmpty(description))
            {
                description = description.Trim();
            }

            var fileName = Request?.Form["FileName"];
            if (!string.IsNullOrEmpty(fileName))
            {
                fileName = fileName.Trim();
            }

            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }
            var data = _masterDataBLO.GetSetupImageSliderList(styleProfile, listStore, description, fileName, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<SetupImageSliderResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public async Task<JsonResult> CreateImageSlider(CreateSetupImageSliderModel req)
        {
            var userName = base.LoginUser.UserName;
            req.CreatedUser = userName;

            try
            {
                if (req.FileName == "" || req.FileName == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.Fail,
                        Message = "Vui lòng chọn file hình ảnh",
                        IsStatus = 2
                    });
                }

                if (req.Description == "" || req.Description == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.Fail,
                        Message = "Vui lòng nhập tên chương trình",
                        IsStatus = 2
                    });
                }

                var arrListStr = req.FileName.Split('\\');
                var fileName = arrListStr.LastOrDefault();
              
                var friendlyNameImages = $"{DateTime.Now.ToString("yyyyMMddHHmmssff")}-{fileName.GenerateFriendlyName()}";
                //var friendlyNameImages = fileName.GenerateFriendlyName();

                var pathFileCDN_Img = $"{cdn_Image}\\slider\\{friendlyNameImages}";
                var urlFile = $"{urlImgServer}slider/{friendlyNameImages}";

                //var pathFileCDN_Img = $"{cdn_Image}\\slider\\{fileName}";
                //var urlFile = $"{urlImgServer}slider/{fileName}";

                if (System.IO.File.Exists(pathFileCDN_Img))
                {
                    System.IO.File.Delete(pathFileCDN_Img);
                }

                if (!string.IsNullOrEmpty(req.ImageBase64))
                {
                    string imgCDNPath = Path.Combine($"{cdn_Image}\\slider", friendlyNameImages);
                    var getImageSize = HelpersExtension.GetImageSize(req.ImageBase64);
                    if (getImageSize > 2000)
                    {
                        var imgSize = HelpersExtension.ResizeImage(HelpersExtension.ConvertBase64ToImage(req.ImageBase64), new Size(1024,768));
                        byte[] imageBytes = HelpersExtension.ImageToByteArray(imgSize);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }
                    else
                    {
                        var imageBytes = Convert.FromBase64String(req.ImageBase64);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }
                    req.UrlFile = urlFile;
                }

                /* ----- Code Old -----
                  
                    var pathFileCDN_Img = $"{cdn_Image}\\slider\\{req.FileName}";
                    var urlFile = $"{urlImgServer}slider/{req.FileName}";

                    if (System.IO.File.Exists(pathFileCDN_Img))
                    {
                        System.IO.File.Delete(pathFileCDN_Img);
                    }

                    if (!string.IsNullOrEmpty(req.ImageBase64))
                    {
                        byte[] imageBytes = Convert.FromBase64String(req.ImageBase64);
                        string imgCDNPath = Path.Combine($"{cdn_Image}\\slider", req.FileName);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                        req.UrlFile = urlFile;  
                    }

                */

                // ------- 17/05/2023 ---------
                var model = new CreateSetupImageSliderModel();
                var listStoreNo = new List<StoreListModel>();
                var listStoreNo2 = new List<StoreListModel>();
                var listStoredNotApply = JsonConvert.DeserializeObject<List<string>>(req.StoreListStr2).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList() ;

                if (req.IsSelectAll == true && listStoredNotApply.Count == 0) // Tất cả Store, Khong duoc chon ST/CH khong ap dung
                {
                    model = new CreateSetupImageSliderModel
                    {
                        ID = req.ID,
                        StoreNo = "ALL",
                        StoreList = listStoreNo,
                        POSNo = req.POSNo,
                        FromDate = req.FromDate,
                        ToDate = req.ToDate,
                        FileName = friendlyNameImages,
                        UrlFile = req.UrlFile,
                        Description = req.Description,
                        Status = req.Status,
                        Counter = req.Counter,
                        Pkey = req.Pkey,
                        CreatedDate = req.CreatedDate,
                        UpdatedDate = req.UpdatedDate,
                        CreatedUser = LoginUser.UserName,
                        UpdatedUser = LoginUser.UserName,
                        ImageName = req.ImageName,
                        ImageBase64 = req.ImageBase64,
                        IsSelectAll = req.IsSelectAll,
                        CheckStore = req.CheckStore
                    };
                }
                else
                {
                    var StoreModel = JsonConvert.DeserializeObject<List<string>>(req.StoreListStr).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                    if (StoreModel != null)
                    {
                        foreach (var item in StoreModel)
                        {
                            listStoreNo.Add(new StoreListModel
                            {
                                StoreNo = item.ToString()
                            });
                        }
                    }
                    //var StoreModel2 = JsonConvert.DeserializeObject<List<string>>(req.StoreListStr2);
                    foreach (var item in listStoredNotApply)
                    {
                        listStoreNo2.Add(new StoreListModel
                        {
                            StoreNo = item.ToString()
                        });
                    }
                    model = new CreateSetupImageSliderModel
                    {
                        ID = req.ID,
                        StoreNo = req.StoreNo,
                        StoreList = listStoreNo,
                        POSNo = req.POSNo,
                        FromDate = req.FromDate,
                        ToDate = req.ToDate,
                        FileName = friendlyNameImages,
                        UrlFile = req.UrlFile,
                        Description = req.Description,
                        Status = req.Status,
                        Counter = req.Counter,
                        Pkey = req.Pkey,
                        CreatedDate = req.CreatedDate,
                        UpdatedDate = req.UpdatedDate,
                        CreatedUser = LoginUser.UserName,
                        UpdatedUser = LoginUser.UserName,
                        ImageName = req.ImageName,
                        ImageBase64 = req.ImageBase64,
                        IsSelectAll = req.IsSelectAll,
                        StoreList2 = listStoreNo2,       // ST/CH không áp dụng chương trình này
                        IsSelectAll2 = req.IsSelectAll2,
                        CheckStore = req.CheckStore
                    };
                }
                var data = await _masterDataBLO.CreateImageSliderAsync(model).ConfigureAwait(false);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Xảy ra lỗi khi lưu hình ảnh lên CDN",
                    IsStatus = 2
                });
            }           
        }

        // ------ New: 25/10/2023 ------
        [HttpPost]
        public async Task<JsonResult> CreateImageSlider_V2(CreateSetupImageSliderModel req)
        {
            var userName = base.LoginUser.UserName;
            req.CreatedUser = userName;

            try
            {
                if (req.FileName == "" || req.FileName == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.Fail,
                        Message = "Vui lòng chọn file hình ảnh",
                        IsStatus = 2
                    });
                }

                if (req.Description == "" || req.Description == null)
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.Fail,
                        Message = "Vui lòng nhập tên chương trình",
                        IsStatus = 2
                    });
                }

                //if (req.FromDate.Date < DateTime.Now.Date || req.ToDate.Date < DateTime.Now.Date)
                //{
                //    return Json(new ResultResponseModel
                //    {
                //        Status = ResultEnum.Fail,
                //        Flag = 0,
                //        Message = "Hiệu lực từ ngày (hiệu lực đến ngày) >= ngày hiện tại. Vui lòng kiểm tra lại"
                //    });
                //}

                //if (req.FromDate > req.ToDate)
                //{
                //    return Json(new ResultResponseModel
                //    {
                //        Status = ResultEnum.Fail,
                //        Flag = 0,
                //        Message = "Hiệu lực từ ngày <= hiệu lực đến ngày. Vui lòng kiểm tra lại"
                //    });
                //}

                var arrListStr = req.FileName.Split('\\');
                var fileName = arrListStr.LastOrDefault();
                var friendlyNameImages = $"{DateTime.Now.ToString("yyyyMMddHHmmssff")}-{fileName.GenerateFriendlyName()}";
                var pathFileCDN_Img = $"{cdn_Image}\\slider\\{friendlyNameImages}";
                var urlFile = $"{urlImgServer}slider/{friendlyNameImages}";

                if (System.IO.File.Exists(pathFileCDN_Img))
                {
                    System.IO.File.Delete(pathFileCDN_Img);
                }

                if (!string.IsNullOrEmpty(req.ImageBase64))
                {
                    string imgCDNPath = Path.Combine($"{cdn_Image}\\slider", friendlyNameImages);
                    var getImageSize = HelpersExtension.GetImageSize(req.ImageBase64);
                    if (getImageSize > 600)  // old : getImageSize > 2000
                    {
                        var imgSize = HelpersExtension.ResizeImage(HelpersExtension.ConvertBase64ToImage(req.ImageBase64), new Size(1024,768));
                        byte[] imageBytes = HelpersExtension.ImageToByteArray(imgSize);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }
                    else
                    {
                        var imageBytes = Convert.FromBase64String(req.ImageBase64);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }
                    req.UrlFile = urlFile;
                }
                // ------- 17/05/2023 -------
                var model = new CreateSetupImageSliderModel();
                var listStoreNo = new List<StoreListModel>();
                var listStoreNo2 = new List<StoreListModel>();
                var listStoredNotApply = JsonConvert.DeserializeObject<List<string>>(req.StoreListStr2).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList() ;

                if (req.IsSelectAll == true && listStoredNotApply.Count == 0) // Tất cả CH
                {
                    model = new CreateSetupImageSliderModel
                    {
                        ID = req.ID,
                        StoreNo = "ALL",
                        StoreList = listStoreNo,
                        POSNo = req.POSNo,
                        FromDate = req.FromDate.Date,
                        ToDate = req.ToDate.Date,
                        //FromDate = DateTime.ParseExact(req.FromDateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        //ToDate = DateTime.ParseExact(req.ToDateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                        FileName = friendlyNameImages,
                        UrlFile = req.UrlFile,
                        Description = req.Description,
                        Status = req.Status,
                        Counter = req.Counter,
                        Pkey = req.Pkey,
                        CreatedDate = req.CreatedDate,
                        UpdatedDate = req.UpdatedDate,
                        CreatedUser = LoginUser.UserName,
                        UpdatedUser = LoginUser.UserName,
                        ImageName = req.ImageName,
                        ImageBase64 = req.ImageBase64,
                        IsSelectAll = req.IsSelectAll,
                        CheckStore = req.CheckStore
                    };
                }
                else
                {
                    var StoreModel = JsonConvert.DeserializeObject<List<string>>(req.StoreListStr).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
                    if (StoreModel != null)
                    {
                        foreach (var item in StoreModel)
                        {
                            listStoreNo.Add(new StoreListModel
                            {
                                StoreNo = item.ToString()
                            });
                        }
                    }
                    //var StoreModel2 = JsonConvert.DeserializeObject<List<string>>(req.StoreListStr2);
                    foreach (var item in listStoredNotApply)
                    {
                        listStoreNo2.Add(new StoreListModel
                        {
                            StoreNo = item.ToString()
                        });
                    }

                    model = new CreateSetupImageSliderModel
                    {
                        ID = req.ID,
                        StoreNo = req.StoreNo,
                        StoreList = listStoreNo,
                        POSNo = req.POSNo,
                        FromDate = req.FromDate.Date,
                        ToDate = req.ToDate.Date,
                        //FromDateStr = DateTime.ParseExact(req.FromDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd hh:mm:ss tt"),
                        //ToDateStr = DateTime.ParseExact(req.ToDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd hh:mm:ss tt"),
                        FileName = friendlyNameImages,
                        UrlFile = req.UrlFile,
                        Description = req.Description,
                        Status = req.Status,
                        Counter = req.Counter,
                        Pkey = req.Pkey,
                        CreatedDate = req.CreatedDate,
                        UpdatedDate = req.UpdatedDate,
                        CreatedUser = LoginUser.UserName,
                        UpdatedUser = LoginUser.UserName,
                        ImageName = req.ImageName,
                        ImageBase64 = req.ImageBase64,
                        IsSelectAll = req.IsSelectAll,
                        StoreList2 = listStoreNo2,       // CH không áp dụng chương trình này
                        IsSelectAll2 = req.IsSelectAll2,
                        CheckStore = req.CheckStore
                    };
                }
                var data = await _masterDataBLO.CreateImageSlider_V2(model).ConfigureAwait(false);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Có lỗi xảy ra khi lưu hình ảnh lên CDN",
                    IsStatus = 2
                });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateImageSlider(UpdateSetupImageSliderModel req)
        {
            var userName = base.LoginUser.UserName;
            req.UpdatedUser = userName;

            // Get old image
            var urlImgOld = $"{cdn_Image}\\slider\\{req.FileNameOld}";
            
            // CDN
            try
            {
                // Delete old image
                if (!string.IsNullOrEmpty(urlImgOld))
                {
                    if (System.IO.File.Exists(urlImgOld))
                    {
                        System.IO.File.Delete(urlImgOld);
                    }
                }

                // Add new image
                var pathFileCDN_Img = $"{cdn_Image}\\slider\\{req.FileName}";
                if (System.IO.File.Exists(pathFileCDN_Img))
                {
                    System.IO.File.Delete(pathFileCDN_Img);
                }

                var fileName = "";
                if (!string.IsNullOrEmpty(req.FileNameNew))
                {
                    fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmssff")}-{req.FileNameNew.GenerateFriendlyName()}";
                }
                else
                {
                    fileName = req.FileName.GenerateFriendlyName();
                }

                var friendlyNameImages = fileName;

                //var friendlyNameImages = $"{DateTime.Now.ToString("yyyyMMddHHmmssff")}-{fileName.GenerateFriendlyName()}";
                //var friendlyNameImages = req.FileName.GenerateFriendlyName();

                if (!string.IsNullOrEmpty(req.ImageBase64))
                {
                    string imgCDNPath = Path.Combine($"{cdn_Image}\\slider", friendlyNameImages);
                    var getImageSize = HelpersExtension.GetImageSize(req.ImageBase64);

                    if (getImageSize > 600) // old : getImageSize > 2000
                    {
                        var imgSize = HelpersExtension.ResizeImage(HelpersExtension.ConvertBase64ToImage(req.ImageBase64), new Size(1024,768));
                        byte[] imageBytes = HelpersExtension.ImageToByteArray(imgSize);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }
                    else
                    {
                        var imageBytes = Convert.FromBase64String(req.ImageBase64);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }

                    req.UrlFile = $"{urlImgServer}slider/{friendlyNameImages}";
                    req.FileName = friendlyNameImages;

                    //byte[] imageBytes = Convert.FromBase64String(req.ImageBase64);
                    //string imgCDNPath = Path.Combine($"{cdn_Image}\\slider", req.FileName);
                    //System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    //req.UrlFile = urlFile;

                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Xảy ra lỗi khi lưu hình ảnh lên CDN",
                });
            }
            var data = await _masterDataBLO.UpdateImageSliderAsync(req).ConfigureAwait(false);
            return Json(data);
        }

        // ---- New: 25/10/2023 ----
        [HttpPost]
        public async Task<JsonResult> UpdateImageSlider_V2(UpdateSetupImageSliderModel req)
        {
            var userName = base.LoginUser.UserName;
            req.UpdatedUser = userName;
            // Get old image              
            var urlImgOld = $"{cdn_Image}\\slider\\{req.FileNameOld}";
            // CDN
            try
            {
                // Delete old images
                if (!string.IsNullOrEmpty(urlImgOld))
                {
                    if (System.IO.File.Exists(urlImgOld))
                    {
                        System.IO.File.Delete(urlImgOld);
                    }
                }
                // Add new images
                var pathFileCDN_Img = $"{cdn_Image}\\slider\\{req.FileName}";
                if (System.IO.File.Exists(pathFileCDN_Img))
                {
                    System.IO.File.Delete(pathFileCDN_Img);
                }
                var fileName = "";
                if (!string.IsNullOrEmpty(req.FileNameNew))
                {
                    fileName = $"{DateTime.Now.ToString("yyyyMMddHHmmssff")}-{req.FileNameNew.GenerateFriendlyName()}";
                }
                else
                {
                    fileName = req.FileName.GenerateFriendlyName();
                }
                var friendlyNameImages = fileName;
                if (!string.IsNullOrEmpty(req.ImageBase64))
                {
                    string imgCDNPath = Path.Combine($"{cdn_Image}\\slider", friendlyNameImages);
                    var getImageSize = HelpersExtension.GetImageSize(req.ImageBase64);
                    if (getImageSize > 600)
                    {
                        var imgSize = HelpersExtension.ResizeImage(HelpersExtension.ConvertBase64ToImage(req.ImageBase64), new Size(1024,768));
                        byte[] imageBytes = HelpersExtension.ImageToByteArray(imgSize);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }
                    else
                    {
                        var imageBytes = Convert.FromBase64String(req.ImageBase64);
                        System.IO.File.WriteAllBytes(imgCDNPath, imageBytes);
                    }
                    req.UrlFile = $"{urlImgServer}slider/{friendlyNameImages}";
                    req.FileName = friendlyNameImages;
                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Có lỗi xảy ra khi cập nhật ảnh lên CDN",
                });
            }
            var data = await _masterDataBLO.UpdateImageSlider_V2(req).ConfigureAwait(false);
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteImageSlider(DeleteSetupImageSliderModel req)
        {
            var data = _masterDataBLO.DeleteImageSlider(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult GetImagesSilder(string Id, string Pkey)
        {
            long _id = 0;
            if (!string.IsNullOrEmpty(Id))
            {
                 _id = long.Parse(Id, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign);
            }            
            var model = new UpdateSetupImageSliderModel
            {
                ID = _id,
                Pkey = Pkey
            };

            try
            {
                model = _masterDataBLO.ViewInfoImagesSlider(Id, Pkey);
                if (!string.IsNullOrEmpty(model.ImageName))
                {
                    var pathImg = $"{cdn_Image}\\slider\\{model.ImageName}";
                    byte[] imageArray = System.IO.File.ReadAllBytes(pathImg);

                    if(imageArray != null)
                    {
                        model.ImageBase64 = Convert.ToBase64String(imageArray);
                        model.Status = "1";
                    } 
                }
                else
                {
                    model.ImageBase64 = string.Empty;
                    model.Status = "0";
                }
            }
            catch (Exception ex)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Message = "Không tìm thấy file hình này trên CDN",
                });
            }
            var jsonResult = Json(model, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        // 07/08/2025,tungt8
        [HttpPost]
        public string GetDescriptionImageSilder(string description)
        {
            return  _masterDataBLO.GetDescriptionImageSlider(description);
        }

        [HttpPost]
        public JsonResult SaveImageSilderByBlock(string description,string status)
        {
            var model = new ImageSliderBlockModel()
            {
                Description = description.Trim(),
                Status = status,
                UpdatedUser = base.LoginUser.UserName
            };
            var result = _masterDataBLO.SaveImageSliderByBlock(model);
            var jsonResult = Json(result, JsonRequestBehavior.AllowGet);
            return jsonResult;
        }

        //------- 20/10/2023 -------
        public ActionResult ExportExcel_ImagesSliderList( string StoreNo, string StyleProfile, string Description, string FileName, string StatusFile)
        {
            var listStore = "";
            if (!string.IsNullOrEmpty(StoreNo))
            {
                string[] listStoreArray = JsonConvert.DeserializeObject<string[]>(StoreNo);

                foreach (var i in listStoreArray.ToList())
                {
                    listStore += i + ";";
                }
            }
            else
            {
                listStore = "";
            }

            var data = _masterDataBLO.ExportExcel_ImagesSliderList(listStore, StyleProfile, Description, FileName, StatusFile);

            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new
                    {
                        x.ID,
                        x.StoreNo,
                        x.Description,
                        x.Status,
                        x.FromDate,
                        x.ToDate,
                        x.FileName,
                        x.UrlFile,
                        x.Pkey,
                        x.CreatedDate,
                        x.CreatedUser,
                        x.UpdatedDate,
                        x.UpdatedUser
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Cửa hàng";
                    worksheet.Cells[1, 3].Value = "Tên chương trình";
                    worksheet.Cells[1, 4].Value = "Trạng thái";
                    worksheet.Cells[1, 5].Value = "Từ ngày";
                    worksheet.Cells[1, 6].Value = "Đến ngày";
                    worksheet.Cells[1, 7].Value = "Tên File";
                    worksheet.Cells[1, 8].Value = "URL";
                    worksheet.Cells[1, 9].Value = "Pkey";
                    worksheet.Cells[1, 10].Value = "Ngày tạo";
                    worksheet.Cells[1, 11].Value = "Người tạo";
                    worksheet.Cells[1, 12].Value = "Ngày cập nhật";
                    worksheet.Cells[1, 13].Value = "Người cập nhật";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 13])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"Images_Slider_List_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";

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

        [DisplayName("Khai báo quy đổi tiền tệ")]
        public ActionResult SetupCurrencyRate()
        {
            return View();
        }
        public JsonResult GetSetupCurrencyRateList()
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

            var UnitCurrency = Request?.Form["UnitCurrency"];
            if (!string.IsNullOrEmpty(UnitCurrency))
            {
                UnitCurrency = UnitCurrency.Trim();
            }

            var Status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(Status))
            {
                Status = Status.Trim();
            }

            var ExchangeRate = Request?.Form["ExchangeRate"];
            if (!string.IsNullOrEmpty(ExchangeRate))
            {
                ExchangeRate = ExchangeRate.Trim();
            }

            var data = _masterDataBLO.GetSetupCurrencyRateList(UnitCurrency, Status, ExchangeRate, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<SetupCurrencyRateListModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public JsonResult CreateCurrencyRate(CreateCurrencyRateModel req)
        {
            req.CreatedUser = LoginUser.UserName;
            if (req.ExchangeRate == null || req.ExchangeRate == 0)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Flag = 0,
                    Message = "Nhập tỷ giá quy đổi sang VNĐ. Vui lòng kiểm tra lại"
                });
            }

            if (req.StartingDate.Date < DateTime.Now.Date)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Flag = 0,
                    Message = "Hiệu lực từ ngày phải lớn hơn hoặc bằng ngày hiện tại. Vui lòng kiểm tra lại"
                });
            }

            if (req.StartingDate > req.EndingDate) {

                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Flag = 0,
                    Message = "Hiệu lực từ ngày phải nhỏ hơn hoặc bằng hiệu lực đến ngày. Vui lòng kiểm tra lại"
                });
            }

            var model = new CreateCurrencyRateModel
            {
                CurrencyCode = req.CurrencyCode,
                StartingDate = req.StartingDate,
                EndingDate = req.EndingDate,
                ExchangeRate = req.ExchangeRate,
                Status = req.Status,
                CreatedDate = DateTime.Now,
                CreatedUser = LoginUser.UserName,
                UpdatedDate = DateTime.Now,
                UpdatedUser = LoginUser.UserName,
                Pkey = req.Pkey
            };

            var data = _masterDataBLO.CreateCurrencyRate(req);

            if (data == null)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Flag = 0,
                    Message = "Không có dữ liệu khai báo tỷ giá. Vui lòng kiểm tra lại"
                });
            }
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateCurrencyRate(UpdateCurrencyRateModel req)
        {
            req.CreatedUser = LoginUser.UserName;
            req.UpdatedUser = LoginUser.UserName;

            if (req.ExchangeRate == null || req.ExchangeRate == 0)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Flag = 0,
                    Message = "Nhập tỷ giá quy đổi sang VNĐ. Vui lòng kiểm tra lại"
                });
            }

            if (req.StartingDate > req.EndingDate)
            {

                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.Fail,
                    Flag = 0,
                    Message = "Hiệu lực từ ngày phải nhỏ hơn hoặc bằng hiệu lực đến ngày. Vui lòng kiểm tra lại"
                });
            }

            var model = new UpdateCurrencyRateModel
            {
                CurrencyCode = req.CurrencyCode,
                StartingDate = req.StartingDate,
                EndingDate = req.EndingDate,
                ExchangeRate = req.ExchangeRate,
                Status = req.Status,
                CheckDateDefault = req.CheckDateDefault,
                CreatedDate = DateTime.Now,
                CreatedUser = LoginUser.UserName,
                UpdatedDate = DateTime.Now,
                UpdatedUser = LoginUser.UserName,
                Pkey = req.Pkey
            };
            var data = _masterDataBLO.UpdateCurrencyRate(req);
            return Json(data);
        }

        [DisplayName("Khai báo QR trên hóa dơn")]
        public ActionResult SetupQRInformation()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(uerName);
            return View();
        }

        [HttpPost]
        [ParentAuthorize("SetupQRInformation")]
        public JsonResult GetQRInformationList()
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

            var POSID = Request?.Form["POSID"];
            if (!string.IsNullOrEmpty(POSID))
            {
                POSID = POSID.Trim();
            }

            var textSearch = Request?.Form["TextSearch"];
            if (!string.IsNullOrEmpty(textSearch))
            {
                textSearch = textSearch.Trim();
            }

            var IsActive = Request?.Form["IsActive"];
            if (!string.IsNullOrEmpty(IsActive))
            {
                IsActive = IsActive.Trim();
            }

            var ContentType = Request?.Form["ContentType"];
            if (!string.IsNullOrEmpty(ContentType))
            {
                ContentType = ContentType.Trim();
            }

            var data = _masterDataBLO.GetInformationQRList(fromDate, toDate, storeNo, POSID, textSearch, IsActive, ContentType, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<QRInformationModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        [ParentAuthorize("SetupQRInformation")]
        public JsonResult CreateInformationQRList(CreateInformationQRModel req)
        {
            if (string.IsNullOrEmpty(req.StoreNo))
            {
                return Json(new ResultResponseModel {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng chọn ST/CH"
                });
            }

            if (string.IsNullOrEmpty(req.POSID))
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng chọn máy POS"
                });
            }

            if (req.FromDate == null)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng từ ngày"
                });
            }

            if (req.ToDate == null)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng đến ngày"
                });
            }

            if (req.FromDate > req.ToDate)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Từ ngày không được lớn hơn đến ngày. Vui lòng kiểm tra lại"
                });
            }

            if (string.IsNullOrEmpty(req.ContentType))
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng chọn loại"
                });
            }

            if (req.IsActive == null)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng chọn trạng thái"
                });
            }

            if (req.Order == 0)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng nhập số thứ tự ưu tiên hiển thị"
                });
            }

            if (req.ContentType == "Text")
            {
                if (req.Content == null || req.Content == "")
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning,
                        Message = "Vui lòng nhập nội dung"
                    });
                }               
            }
            else if(req.ContentType == "Link")
            {
                if (req.QRLink == null || req.QRLink == "")
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning,
                        Message = "Vui lòng nhập Link QR"
                    });
                }
            }

            req.CreatedBy = LoginUser.UserName;
            var data = _masterDataBLO.CreateInformationQRList(req);
            return Json(data);

        }

        [HttpPost]
        [ParentAuthorize("SetupQRInformation")]
        public JsonResult UpdateQRInformationList(UpdateInformationQRModel req)
        {
            if (string.IsNullOrEmpty(req.FromDateStr))
            {
                return Json(new ResultResponseModel {
                    Status = ResultEnum.warning,
                    Message = "Từ ngày không hợp lệ. Vui lòng kiểm tra lại"
                });

            }

            if (string.IsNullOrEmpty(req.ToDateStr))
            {
                return Json(new ResultResponseModel {
                    Status = ResultEnum.warning,
                    Message = "Đến ngày không hợp lệ. Vui lòng kiểm tra lại"
                });
            }

            req.FromDate = Convert.ToDateTime(req.FromDateStr);
            req.ToDate = Convert.ToDateTime(req.ToDateStr);

            if (req.FromDate > req.ToDate)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Từ ngày không được lớn hơn đến ngày. Vui lòng kiểm tra lại"
                });
            }

            if (req.ContentType == "Text")
            {
                if (req.Content == null || req.Content == "")
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning,
                        Message = "Vui lòng nhập nội dung"
                    });
                }
            }
            else if (req.ContentType == "Link")
            {
                if (req.QRLink == null || req.QRLink == "")
                {
                    return Json(new ResultResponseModel
                    {
                        Status = ResultEnum.warning,
                        Message = "Vui lòng nhập Link QR"
                    });
                }
            }

            if (req.Order == 0)
            {
                return Json(new ResultResponseModel
                {
                    Status = ResultEnum.warning,
                    Message = "Vui lòng nhập số thứ tự ưu tiên hiển thị"
                });
            }

            req.UpdatedBy = LoginUser.UserName;
            var data = _masterDataBLO.UpdateQRInformationList(req);
            return Json(data);

        }

        [HttpPost]
        [ParentAuthorize("SetupQRInformation")]
        public JsonResult DeleteQRInformationList(DeleteInformationQRModel req)
        {
            var data = _masterDataBLO.DeleteQRInformationList(req);
            return Json(data);
        }

        [DisplayName("Danh sách User login POS Web")]
        public ActionResult UserLoginPOSWeb()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(uerName);
            return View();
        }

        public JsonResult UserLoginPOSWebList()
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

            var typePOS = Request?.Form["TypePOS"];
            if (!string.IsNullOrEmpty(typePOS))
            {
                typePOS = typePOS.Trim();
            }

            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var posTerminal = Request?.Form["PosTerminal"];
            if (!string.IsNullOrEmpty(posTerminal))
            {
                posTerminal = posTerminal.Trim();
            }

            var data = _masterDataBLO.GetUserLoginPOSWebList(storeNo, posTerminal, typePOS, textSearch, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<AdminUserLoginPOSResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }

        [HttpPost]
        public JsonResult DeleteUserLoginPOSWeb(DeleteAdminUserLoginPOSWebModel req)
        {
            var data = _masterDataBLO.DeleteUserLoginPOSWeb(req);
            return Json(data);
        }

        public ActionResult ExportExcel_UserLoginPOSWebList(string StoreNo, string PosTerminal, string TypePOS, string TextSearch)
        {
            var data = _masterDataBLO.ExportExcel_UserLoginPOSWebList(StoreNo, PosTerminal, TypePOS, TextSearch);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportAdminUserLoginPOSResponseModel
                    {
                        UserName = x.UserName,
                        StaffCode = x.StaffCode,
                        FullName = x.FullName,
                        StoreNo = x.StoreNo,
                        PosNo = x.PosNo,
                        TypePOS = x.TypePOS,
                        Token = x.Token,
                        CreatedDateStr = x.CreatedDateStr  
                    }).ToList();

                    worksheet.Cells[1, 1].Value = "Tài khoản";
                    worksheet.Cells[1, 2].Value = "Mã nhân viên";
                    worksheet.Cells[1, 3].Value = "Tên nhân viên";
                    worksheet.Cells[1, 4].Value = "Mã CH/ST";
                    worksheet.Cells[1, 5].Value = "Máy POS";
                    worksheet.Cells[1, 6].Value = "Loại";
                    worksheet.Cells[1, 7].Value = "Token";
                    worksheet.Cells[1, 8].Value = "Ngày";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 8])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"AdminUserLoginPOSWEBList_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Cài đặt máy in POSWeb")]
        public ActionResult SetupPrintByPOSWeb()
        {
            var uerName = LoginUser.UserName;
            ViewBag.ListStore = _commonBLO.GetComboxStoreByUser(uerName);
            return View();
        }

        [HttpPost]
        public JsonResult SetupPrintByPOSWebList()
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

            var storeNo = Request?.Form["StoreNo"];
            if (!string.IsNullOrEmpty(storeNo))
            {
                storeNo = storeNo.Trim();
            }

            var ipAddress = Request?.Form["IPAddress"];
            if (!string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = ipAddress.Trim();
            }

            var status = Request?.Form["Status"];
            if (!string.IsNullOrEmpty(status))
            {
                status = status.Trim();
            }

            var data = _masterDataBLO.SetupPrintByPOSWebList(ipAddress, storeNo, status, out recordsTotal, pageNumber, pageSize);
            return Json(new DataTablesViewModel<SetupPrintByPOSWebResponseModel> { draw = draw.ToString(), recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });

        }
        public ActionResult ExportExcel_SetupPrintByPOSWeb(string IPAddress, string StoreNo, string Status)
        {
            var data = _masterDataBLO.ExportExcel_SetupPrintByPOSWeb(IPAddress, StoreNo, Status);
            if (data != null)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    var listExport = data.Select(x => new ExportEExcelSetupPrintByPOSWebModel
                    {    
                        IPAddress = x.IPAddress,
                        StoreNo = x.StoreNo,
                        Status = x.Status
                    }).ToList();
   
                    worksheet.Cells[1, 1].Value = "Địa chỉ IP";
                    worksheet.Cells[1, 2].Value = "Mã ST/CH";
                    worksheet.Cells[1, 3].Value = "Trạng thái";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 3])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#b1afaf"));
                    }

                    worksheet.Cells["A2"].LoadFromCollection(listExport, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"StoreSetupPrintByPOSWeb_{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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
        public JsonResult CreateSetupPrintByPOSWeb(CreateSetupPrintByPOSWebModel req)
        {
            if (string.IsNullOrEmpty(req.StoreNo))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng chọn ST/CH"
                });
            }

            if (string.IsNullOrEmpty(req.IPAddress))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng chọn máy POS bán hàng"
                });
            }

            if (string.IsNullOrEmpty(req.Status))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng chọn trạng thái máy in"
                });
            }
            var data = _masterDataBLO.CreateSetupPrintByPOSWeb(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult UpdateSetupPrintByPOSWeb(UpdateSetupPrintByPOSWebModel req)
        {
            if (string.IsNullOrEmpty(req.Status))
            {
                return Json(new ResultResponseModel
                {
                    Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning,
                    Message = "Vui lòng chọn trạng thái máy in"
                });
            }
            var data = _masterDataBLO.UpdateSetupPrintByPOSWeb(req);
            return Json(data);
        }

        [HttpPost]
        public JsonResult DeleteSetupPrintByPOSWeb(DeleteSetupPrintByPOSWebModel req)
        {
            var data = _masterDataBLO.DeleteSetupPrintByPOSWeb(req);
            return Json(data);
        }




    }
}
