using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Web.Mvc;
using VCM.BLUEPOS.Authen;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Globalization;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.Product;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Business.Product;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Common;


namespace PLG.Controllers
{
    public class CommonController : BaseController
    {
        private ICommonBLO _commonBLO;
        private IAuthenBLO _authenBLO;
        public CommonController(ICommonBLO commonBLO, IAuthenBLO authenBLO)
        {
            _commonBLO = commonBLO;
            _authenBLO = authenBLO;
        }
        public ActionResult GetIPSetServerByStore(string StoreNo)
        {
            var data = _commonBLO.GetIPSetServerByStore(StoreNo);
            return Json(data);
        }
        public ActionResult GetComboxUnitOfMeasure()
        {
            var data = _commonBLO.GetComboxUnitOfMeasure();
            return Json(data);
        }
        public ActionResult GetComboxVoucherCouponList()
        {
            var data = _commonBLO.GetComboxVoucherCouponList();
            return Json(data);
        }
        public JsonResult CheckStaffCode(string staffCode)
        {
            if (string.IsNullOrEmpty(staffCode))
            {
                return Json(new ResultResponseModel { Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning, Message = "Vui lòng nhập mã nhân viên" });
            }
            else
            {
                var l = staffCode.Length;
                int i = 0;
                int total = 0;
                while (i < l)
                {
                    if (staffCode[i] >= '0' && staffCode[i] <= '9')
                    {
                        total++;
                    }
                    i++;
                }
                if (total < 8)
                {
                    return Json(new ResultResponseModel { Status = VCM.BLUEPOS.Model.Enums.ResultEnum.warning, Message = "Mã nhân viên bắt buộc lớn hơn hoặc bằng 8 ký tự số. Vui lòng kiểm tra lại" });
                }
            }
            var data = _commonBLO.CheckStaffCode(staffCode);
            return Json(data.Item2);
        }
        public ActionResult GetPermisionPOSTerminal()
        {
            var data = _commonBLO.GetPermisionPOSTerminal();
            return Json(data);
        }
        public ActionResult GetComboxStoreList()
        {
            var data = _commonBLO.GetComboxStoreList();
            return Json(data);
        }
        public ActionResult GetComboxBankList()
        {
            var data = _commonBLO.LoadComboxBankList();
            return Json(data);
        }
        public ActionResult GetComboxPOSTerminalByStore(string storeNo)
        {
            var data = _commonBLO.LoadComboxPOSTerminalByStore(storeNo);
            return Json(data);
        }
        public ActionResult LoadComboxStoreNo()
        {
            var data = _commonBLO.LoadComboxStoreNo();
            return Json(data);
        }
        public ActionResult GetOrderType()
        {
            var data = _commonBLO.GetOrderType();
            return Json(data);
        }
        public ActionResult ListSQLServer()
        {
            var data = _commonBLO.ListSQLServer();
            return Json(data);
        }
        public ActionResult LoadIPAddressPOSTerminalByStore(string storeNo)
        {
            var _listPOS = new List<IPAddressPOSTerminalByStoreModel>();
            try
            {
                _listPOS = _commonBLO.LoadIPAddressPOSTerminalByStore(storeNo);
                if (_listPOS.Count == 0)
                {
                    _listPOS.Add(new IPAddressPOSTerminalByStoreModel { PosID = "-1", PosName = "Tất cả" });
                }
            }
            catch (Exception ex)
            { }
            return Json(_listPOS.OrderBy(d => d.PosID));
        }
        public ActionResult LoadIPAddressPOSByStoreNo(string StoreNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(StoreNo))
                {
                    var data = _commonBLO.LoadIPAddressPOSTerminalByStore(StoreNo);
                    return Json(data);
                }
            }
            catch
            { }
            return Json(new List<IPAddressModel>());
        }
        public ActionResult LoadPOSTerminalByStore(string storeNo)
        {
            var data = _commonBLO.LoadPOSTerminalByStore(storeNo);
            return Json(data);
        }
        public ActionResult LoadComboxStoreByUserName(string userLogin)
        {
            var data = _commonBLO.LoadComboxStoreByUserName(userLogin);
            return Json(data);
        }
        public ActionResult LoadStoreNoByUserName()
        {
            var data = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return Json(data);
        }
        public ActionResult LoadStoreNoForSyncDataTable()
        {
            var data = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return Json(data);
        }
        public ActionResult LoadStoreByUserNameSetDB(string userLogin, string ipServer)
        {
            var data = _commonBLO.LoadStoreByUserNameSetDB(userLogin, ipServer);
            return Json(data);
        }
        public ActionResult LoadStoreBySetServerIP(string IPServer)
        {
            var userName = base.LoginUser.UserName;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var addr = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            var ipWriteLog = addr.LastOrDefault()?.ToString();
            var listStoreNo = new List<ArraySiteNoModel>();
            try
            {
                listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(userName, IPServer, ipWriteLog).ToList();
                if (listStoreNo.Count > 1)
                {
                    listStoreNo.Add(new ArraySiteNoModel { SiteNo = "", SiteName = "Tất cả" });
                }
            }
            catch (Exception ex)
            {
            }
            return Json(listStoreNo.OrderBy(d => d.SiteNo));
        }
        public ActionResult LoadStoreByUserName()
        {
            var userName = base.LoginUser.UserName;
            var listStoreNo = new List<ArraySiteNoModel>();
            try
            {
                listStoreNo = _commonBLO.LoadComboxStoreByUserName(userName).ToList();
            }
            catch (Exception ex)
            {
            }
            return Json(listStoreNo.OrderBy(d => d.SiteNo));
        }

        public ActionResult LoadStoreBySetServerChanelNo(string ipServer, string chanelNo)
        {
            var listStoreNo = new List<ArraySiteNoModel>();
            try
            {
                listStoreNo = _commonBLO.LoadStoreBySetServerChanelNo(base.LoginUser.UserName, ipServer, chanelNo).ToList();
                if (listStoreNo.Count > 1)
                {
                    listStoreNo.Add(new ArraySiteNoModel { SiteNo = "", SiteName = "Tất cả" });
                }
                else if (listStoreNo.Count < 1)
                {
                    listStoreNo.Add(new ArraySiteNoModel { SiteNo = "-1", SiteName = "Không có ST/CH" });
                }
            }
            catch (Exception ex)
            { }
            return Json(listStoreNo.OrderBy(d => d.SiteNo));
        }
        public ActionResult LoadComboxStaffCode(string storeNo)
        {
            var listStaffCode = new List<StaffCodeResponseModel>();
            try
            {
                var username = base.LoginUser.UserName;
                var infoStaff = _commonBLO.InfoStaffByAD(username);
                var data = infoStaff.Result;

                if (data != null)
                {
                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        if (data.EmploymentType == 1)
                        {
                            listStaffCode = _commonBLO.LoadComboxStaffCode(storeNo).ToList();
                        }
                        else
                        {
                            listStaffCode.Add(new StaffCodeResponseModel { UserPOS = data.ID });
                        }
                    }
                    else
                    {
                        listStaffCode.Add(new StaffCodeResponseModel { UserPOS = data.ID });
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(listStaffCode);
        }

        public ActionResult LoadComboxStaffBySales(string storeNo)
        {
            var listStaffCode = new List<StaffCodeResponseModel>();
            try
            {
                var infoStaff = _commonBLO.InfoStaffByStore(storeNo);
                var data = infoStaff.ToList();
                if (data != null)
                {
                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        if (data.FirstOrDefault().EmploymentType == 1)
                        {
                            listStaffCode = _commonBLO.LoadComboxStaffCode(storeNo).ToList();
                        }
                        else
                        {
                            listStaffCode.Add(new StaffCodeResponseModel
                            {
                                UserPOS = data.FirstOrDefault().ID
                            });
                        }
                    }
                    else
                    {
                        listStaffCode.Add(new StaffCodeResponseModel
                        {
                            UserPOS = data.FirstOrDefault().ID
                        });
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(listStaffCode);
        }
        public ActionResult LoadTransType()
        {
            try
            {
                var result = _commonBLO.ListConfigXMLSAP();
                return Json(result);
            }
            catch (Exception ex)
            {
            }
            return Json(new List<SAPConfigXMLModel>());
        }
        public ActionResult LoadComboxProvinceList()
        {
            try
            {
                var result = _commonBLO.GetComboxProvinceList();
                return Json(result);
            }
            catch (Exception ex)
            {
            }
            return Json(new List<ProvinceModel>());
        }
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
            var data = _commonBLO.CheckStoreNo(storeNo);
            if (data.Item1 == false)
            {
                return Json(data.Item2.Status);
            }
            else
            {
                return Json(data.Item2);
            }
        }
        public ActionResult LoadComboxStoreByChanel(string ChanelNo)
        {
            var listData = new List<ArraySiteNoModel>();
            try
            {
                listData = _commonBLO.LoadComboxStoreByChanel(ChanelNo).ToList();
            }
            catch (Exception ex)
            {
            }
            return Json(listData.OrderBy(d => d.SiteNo));
        }

        public ActionResult LoadComboxSiteByChanelAndStore(string ChanelNo, string StoreNo)
        {
            var listData = new List<ArraySiteNoModel>();
            try
            {
                listData = _commonBLO.LoadComboxSiteByChanelAndStore(ChanelNo, StoreNo).ToList();
            }
            catch (Exception ex)
            {
            }
            return Json(listData.OrderBy(d => d.SiteNo));
        }

        public ActionResult LoadComboxPOSVersionSource()
        {
            try
            {
                var result = _commonBLO.LoadComboxPOSVersionSource();
                return Json(result);
            }
            catch (Exception ex)
            {
            }
            return Json(new List<OptionModel>());
        }
        public ActionResult LoadComboxPOSVersionList()
        {
            try
            {
                var result = _commonBLO.LoadComboxPOSVersionList();
                return Json(result);
            }
            catch (Exception ex)
            {
            }
            return Json(new List<OptionModel>());
        }
        public ActionResult LoadComboxPOSVersionIsUpdate()
        {
            try
            {
                var result = _commonBLO.LoadComboxPOSVersionIsUpdate();
                return Json(result);
            }
            catch (Exception ex)
            {
            }
            return Json(new List<OptionModel>());
        }
        public ActionResult LoadComboxSalesType()
        {
            var data = _commonBLO.LoadComboxSalesType();
            return Json(data);
        }
        public ActionResult GetVoucherTenderType()
        {
            var data = _commonBLO.GetVoucherTenderType();
            return Json(data);
        }
        public ActionResult LoadComboxSalesOrderType()
        {
            var data = _commonBLO.LoadComboxSalesOrderType();
            return Json(data);
        }
        public ActionResult LoadComboxCategoryList()
        {
            var data = _commonBLO.LoadComboxCategoryList();
            return Json(data);
        }
        public ActionResult LoadTenderTypeByWallet(string EWalletCode)
        {
            var data = _commonBLO.LoadTenderTypeByWallet(EWalletCode);
            return Json(data);
        }

        [DisplayName("Scan đơn hàng")]
        public ActionResult ScanOrder()
        {
            var listStore = _authenBLO.GetSiteNoByUser(base.LoginUser.UserName);
            return View(listStore);
        }

        [HttpPost]
        [ParentAuthorize("ScanOrder")]
        public ActionResult ScanOrderLoad()
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
                var ngayScan = Request?.Form["NgayScan"];
                var serverStore = Request?.Form["ServerStore"];
                var orderNo = Request?.Form["OrderNo"];
                var recordsTotal = 0;
                DateTime dateScan = DateTime.ParseExact(ngayScan, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var result = _commonBLO.ScanOrderLoad(serverStore, storeNo, dateScan, orderNo, pageSize, pageNumber);
                if (result.Count > 0)
                    recordsTotal = (int)result.FirstOrDefault()?.TotalRow;
                return Json(new DataTablesViewModel<ScanOrderLoadModal> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });
            }
            catch (Exception ex)
            {
                return Json(new List<ScanOrderLoadModal>());
            }
        }

        [ParentAuthorize("ScanOrder")]
        public ActionResult _ScanOrderView(string serverStore, string storeNo, string bussinessDate)
        {
            // var listStoreNo = _commonBLO.GetSiteNoByUserNameSetDB(base.LoginUser.UserName, "").ToList();
            var model = new ScanOrderViewModel
            {
                ServerStore = serverStore,
                StoreNo = storeNo,
                BussinessDate = bussinessDate,
            };
            return PartialView(model);
        }

        [HttpPost]
        [ParentAuthorize("ScanOrder")]
        public ActionResult InsertScanOrder(string StoreNo, string Barcode, string OrderNo, string ItemNo, string ServerStore)
        {
            try
            {
                var user = base.LoginUser.UserName;
                var data = _commonBLO.ScanOrderInsert(StoreNo, Barcode, OrderNo, ItemNo, user, ServerStore);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new Tuple<int, string, ScanOrderInsertResponseModel>(0, $"Error {ex.Message}", null));
            }
        }

        [HttpPost]
        [ParentAuthorize("ScanOrder")]
        public ActionResult DeleteScanOrder(string StoreNo, string OrderNo, long ID)
        {
            try
            {
                var user = base.LoginUser.UserName;
                var data = _commonBLO.ScanOrderDelete(StoreNo, OrderNo, user,ID);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new Tuple<int, string>(0, $"Xóa đơn hàng {OrderNo} error {ex.Message}"));
            }
        }
        public ActionResult LoadPOSTerminalByStoreSetupPrint(string storeNo)
        {
            var data = _commonBLO.LoadPOSTerminalByStoreSetupPrint(storeNo);
            return Json(data);
        }

        public ActionResult LoadStaffSalseForReport(string StoreNo)
        {
            var userName = base.LoginUser.UserName;
            var listStaffCode = new List<StaffCodeResponseModel>();
            try
            {
                var data = _commonBLO.LoadComboxStaffCode(StoreNo);
                if (data.Count > 0)
                {
                    if (!string.IsNullOrEmpty(StoreNo))
                    {
                        var staffCode = data.Where(x =>x.UserPOS == userName.Trim()).FirstOrDefault();

                        if (staffCode != null)
                        {
                            if (staffCode.EmploymentType == 1) // Quản lý : EmploymentType == 1, quản lý sẽ thấy all các nhân viên CH mình
                            {
                                listStaffCode = _commonBLO.LoadComboxStaffCode(StoreNo).ToList();
                            }
                            else // Nhân viên : EmploymentType == 0, nhân viên chỉ thấy chính mình, và không thấy các nhân viên khâc
                            {
                                listStaffCode.Add(new StaffCodeResponseModel
                                {
                                    UserPOS = staffCode.UserPOS,
                                    EmploymentType = staffCode.EmploymentType
                                });
                            }
                        }
                        else
                        {
                            listStaffCode = _commonBLO.LoadComboxStaffCode(StoreNo).ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(listStaffCode);
        }
        public ActionResult LoadComboxStoreNoByUser()
        {
            var data = _commonBLO.LoadComboxStoreNoByUser(base.LoginUser.UserName);
            return Json(data);
        }
        public ActionResult LoadSalesTypeByMultipleCheck()
        {
            var data = _commonBLO.LoadSalesTypeByMultipleCheck();
            return Json(data);
        }
        public ActionResult LoadComboxStoreByUserChannel(string channelSales)
        {
            var data = _commonBLO.LoadComboxStoreByUserChannel(base.LoginUser.UserName, channelSales);
            return Json(data);
        }
        public ActionResult LoadComboxStoreNoByWinLife()
        {
            var data = _commonBLO.LoadComboxStoreNoByWinLife();
            return Json(data);
        }
        public ActionResult LoadComboxCupType()
        {
            var data = _commonBLO.LoadComboxCupType();
            return Json(data);
        }
        public ActionResult CheckSecurityStoreByUserName(string userName, string listStore)
        {
            var data = _commonBLO.CheckSecurityStoreByUserName(userName,listStore);
            return Json(data);
        }
        public ActionResult LoadComboxStoreNoWinLife() // có phân quyền theo user
        {
            var userName = base.LoginUser.UserName;
            var data = _commonBLO.LoadComboxStoreNoWinLife(userName);
            return Json(data);
        }
        public ActionResult LoadComboxMemberCodeType() // Loại hang the
        {
            var data = _commonBLO.LoadComboxMemberCodeType();
            return Json(data);
        }
        public ActionResult LoadStoreByUpdateSpecialCombo(string channel, string code)
        {
            var data = _commonBLO.LoadStoreByUpdateSpecialCombo(channel, code);
            return Json(data);
        }


    }
}