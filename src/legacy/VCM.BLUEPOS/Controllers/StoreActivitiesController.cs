using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Globalization;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using TichHopSAP;
using Rotativa;
//using VMC.BLUEPOS.Common;
using VCM.BLUEPOS.Models;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Helpers;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Model.StoreActivities;
using VCM.BLUEPOS.Model.SalesStaging;
using VCM.BLUEPOS.Model.MonitorPOS;
using VCM.BLUEPOS.Business.StoreActivities;
using VCM.BLUEPOS.Business.Common;
using VCM.BLUEPOS.Business.SalesStaging;
using VCM.BLUEPOS.Business.MonitorPOS;
using System.Net.NetworkInformation;
using VCM.BLUEPOS.Common;
using PLG.Controllers;


namespace PLG.Controllers
{
    public class StoreActivitiesController : BaseController
    {
        private IStoreActivitiesBLO _activitiesBLO;
        private IAuthenBLO _authenBLO;
        private ICommonBLO _commonBLO;
        private ISalesStagingBLO _stagingBLO;
        private IMonitorPOSBLO _monitorPOSBLO;

        public StoreActivitiesController(IStoreActivitiesBLO activitiesBLO, IAuthenBLO authenBLO, ICommonBLO commonBLO, ISalesStagingBLO stagingBLO, IMonitorPOSBLO monitorPOSBLO)
        {
            _activitiesBLO = activitiesBLO;
            _authenBLO = authenBLO;
            _commonBLO = commonBLO;
            _stagingBLO = stagingBLO;
            _monitorPOSBLO = monitorPOSBLO;
        }

        public ActionResult ConfirmEndingShiftStores()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        public ActionResult GetComboxStaffList(string storeNo)
        {
            var listStaffCode = new List<StaffCodeModel>();
            try
            {
                var userName = base.LoginUser.UserName;
                var infoStaff = _activitiesBLO.StaffInforByAD(userName);
                var data = infoStaff.Result;
                var role = _authenBLO.GetRoleByUser(userName);
                if (role.RoleCode == "R0000" || role.RoleCode == "R0001")  // SuperAdmin : R0001
                {
                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        listStaffCode = _activitiesBLO.GetStaffCodeList(storeNo).ToList();
                    }
                }
                else
                {
                    if (data != null)
                    {
                        if (!string.IsNullOrEmpty(storeNo))
                        {
                            if (data.EmploymentType == 1)
                            {
                                listStaffCode = _activitiesBLO.GetStaffCodeList(storeNo).ToList();
                            }
                            else
                            {
                                listStaffCode.Add(new StaffCodeModel { UserPOS = data.ID });
                            }
                        }
                        else
                        {
                            listStaffCode.Add(new StaffCodeModel { UserPOS = data.ID });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(listStaffCode);
        }

        [HttpPost]
        public ActionResult GetShiftNumber(string StoreNo, string StaffCode, string BusinessDate, string Source)
        {
            DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var listShiftHeader = _activitiesBLO.ListShiftHeader(StaffCode, StoreNo, ngayKD, Source);
            var maxShiftNumber = (listShiftHeader != null && listShiftHeader.Count > 0) ? listShiftHeader.Max(d => int.Parse(d.ShiftNumber)) : 0;
            var ShiftCode = maxShiftNumber == 0 ? "" + 1 : "" + (maxShiftNumber + 1);
            if (maxShiftNumber > 0)
            {
                return Json(new
                {
                    ShiftNumber = ShiftCode,
                    ShiftName = $"(CA {ShiftCode})"
                });
            }
            return Json(new
            {
                ShiftNumber = "" + 1,
                ShiftName = $"(CA 1)"
            });
        }

        [HttpPost]
        public ActionResult LoadConfirmDate(string StoreNo, string BusinessDate)
        {
            try
            {
                if (!string.IsNullOrEmpty(BusinessDate))
                {
                    DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var result = _activitiesBLO.SaleBusinessStoreStaging(StoreNo, ngayKD);
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                return Json(new List<SaleBusinessStoreModel>());
            }

            return Json(new List<SaleBusinessStoreModel>());

        }

        [HttpPost]
        public ActionResult EndDateBusiness(string StoreNo)
        {
            try
            {
                if (!string.IsNullOrEmpty(StoreNo))
                {
                    var info = _activitiesBLO.GetEndDateBusiness(StoreNo);

                    if (info != null)
                    {
                        return Json(info);
                    }
                }
                else
                {
                    return Json(new BussinessDateOpenModel());
                }
            }
            catch (Exception ex)
            {
            }
            return Json(new POSBussinessDateModel());
        }

        [HttpPost]
        public async Task<ActionResult> LoadEndShift()
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

                DateTime businessDate = new DateTime();
                if (!string.IsNullOrEmpty(Request?.Form["BussinessDate"]))
                {
                    var date = Request?.Form["BussinessDate"];
                    businessDate = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                }

                var siteCode = Request?.Form["StoreNo"];
                var staffCode = Request?.Form["StaffCode"];
                var typeTrans = Request?.Form["Source"];
                var result = await _activitiesBLO.GetShiftHeaderList(siteCode, staffCode, typeTrans, businessDate, skip, pageSize);
                return Json(new DataTablesViewModel<ShiftHeaderModel> { draw = draw, recordsFiltered = result.Item2, recordsTotal = result.Item2, data = result.Item1 });
            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<ShiftHeaderModel>());
            }
        }


        [DisplayName("Xác nhận kết thúc ngày")]
        public ActionResult ConfirmEndingDateStores()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        [HttpPost]
        public List<Tuple<string, string>> UploadSaleFromPOSByOrder(string storeNo, string posTerminal, List<string> listOrderNo, string connectionPOS)
        {
            var resultList = new List<Tuple<string, string>>();
            if (listOrderNo.Count > 0)
            {
                foreach (var orderNo in listOrderNo)
                {
                    var Result = new Tuple<string, string>("@@-Not Found " + orderNo, posTerminal);
                    try
                    {
                        var dtTransHeaderCentral = _stagingBLO.StagingListOrder(storeNo, orderNo);
                        var dtListTable = _stagingBLO.StagingListTableFromPOS(storeNo);

                        // Get data from POS
                        StringBuilder SQLSalePOS = new StringBuilder();
                        foreach (var item in dtListTable)
                        {
                            string TableName = item.TableName;
                            string DocumentNoName = item.DocumentNoName;
                            string SQLTablePOS = string.Format("SELECT '{3}' AS TableName,* FROM [{0}] (Nolock) WHERE {1}='{2}'", TableName, DocumentNoName, orderNo, TableName);

                            // Kiem tra : neu so chung tu da ton tai thi khong add
                            if (dtTransHeaderCentral.Count == 0)
                            {
                                SQLSalePOS.AppendLine(SQLTablePOS);
                            }
                        }

                        bool IsHaveData = false;
                        if (SQLSalePOS.Length > 0)
                        {
                            using (SqlConnection connection = new SqlConnection(connectionPOS))
                            {
                                var result = new DataTable();
                                var SqlScript = SQLSalePOS.ToString();
                                SqlCommand command = new SqlCommand(SqlScript, connection);
                                using (var da = new SqlDataAdapter(command))
                                {
                                    try
                                    {
                                        DataSet dsSalePOS = new DataSet();
                                        da.Fill(dsSalePOS);

                                        foreach (DataTable tblData in dsSalePOS.Tables)
                                        {
                                            if (tblData.Rows.Count > 0)
                                            {
                                                // DataTable dtData = new DataTable();
                                                string TableNamePOS = tblData.Rows[0]["TableName"].ToString();
                                                tblData.TableName = TableNamePOS;

                                                // Insert vao Central
                                                DataTable tblDataFinal = VCM.BLUEPOS.Helpers.Utils.RemoveColumn(tblData, "TableName");
                                                string ResultInsert = _commonBLO.InsertBulk(tblDataFinal, TableNamePOS, storeNo, orderNo, posTerminal);

                                                // End Insert vao Central
                                                if (ResultInsert == "OK")
                                                {
                                                    IsHaveData = true;
                                                    Result = new Tuple<string, string>("OK", posTerminal);
                                                    resultList.Add(Result);
                                                }
                                            }
                                        }
                                        connection.Close();
                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                }
                            }
                            if (IsHaveData == true)
                            {
                                _commonBLO.InsertTrans_Invoice(storeNo, orderNo);
                            }
                            //Result = "OK";
                        }
                    }
                    catch (Exception ex)
                    {
                        Result = new Tuple<string, string>(ex.Message, posTerminal);
                        resultList.Add(Result);
                    }
                }
            }
            return resultList;
        }

        public async Task<List<Tuple<string, string>>> UploadSaleFromPOSToCentral(string storeNo, DateTime businessDate, List<PosTerminalMissOrderModel> model)
        {
            SyncDataToCentral sync = new SyncDataToCentral();
            var tasks = new List<Task>();
            var data = new List<Tuple<string, string>>();
            var listModelInsert = new List<POSEODModel>();
            foreach (var item in model)
            {
                string connectPOS = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", item.IPAddress, "POS", "POS@1234", "StorePLH");
                var listOrderCentral = item.ListOrderCentral;

                var listOrderPOS = _activitiesBLO.ListOrderPOS(connectPOS, item.StoreNo, item.PosTerminalID, businessDate);
                var listOrderMiss = listOrderPOS.Where(x => !listOrderCentral.Any(y => y.OrderNo == x.OrderNo && y.PosTerminal == x.PosTerminal && y.StoreNo == x.StoreNo)).ToList();

                if (listOrderMiss.Count > 0)
                {
                    var listOrderParam = listOrderMiss.Select(a => a.OrderNo).ToList();
                    var syncSale = UploadSaleFromPOSByOrder(storeNo, item.PosTerminalID, listOrderParam, connectPOS);//Đóng để test lộn set
                   // data.AddRange(syncSale);
                }

                listModelInsert.Add(new POSEODModel
                {
                    StoreNo = item.StoreNo,
                    POSTerminalNo = item.PosTerminalID,
                    BussinessDate = businessDate,
                    MaxBillNumber = listOrderPOS.Max(d => d.OrderNo),     //mới bổ sung 01/10/2021
                    TotalSale = listOrderPOS.Count(),
                    CreatedDate = DateTime.Now
                });
            }

            if (listModelInsert != null && listModelInsert.Count > 0)
            {
                _activitiesBLO.InsertUpdatePOSEOD(listModelInsert);
            }
            Task taskW = Task.WhenAll(tasks.ToArray());
            await taskW;
            return data;
        }

        // Xác nhận kết thúc ngày
        [HttpPost]
        public ActionResult CheckFinishDate(string SiteNo, string BusinessDate, int RetryConfirm)
        {
            try
            {
                var infoStaff = _commonBLO.InfoStaffByAD(base.LoginUser.UserName);
                if (infoStaff != null)
                {
                    var styleProfile = _commonBLO.GetStyleProfile(SiteNo);
                    if (styleProfile != null)
                    {
                        DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        var checkDate = ngayKD.Subtract(DateTime.Now.Date).TotalDays;
                        if (checkDate > 0)
                        {
                            return Json(new MessageCheckEndShiftResponse
                            {
                                Status = 0,
                                Message = $"Vui lòng không xác nhận kết thúc ngày lớn hơn ngày hiện tại",
                                Data = null
                            });
                        }

                        if (styleProfile.StyleProfile == "KS" || styleProfile.StyleProfile == "FS")     // FinishSale VM phải kêt thúc bán hàng ở POS
                        {
                            // Kiem tra may POS chua dong ca
                            var listShiftHeader = _activitiesBLO.ListTerminalNotEndShiftHeader(SiteNo, ngayKD);

                            if (listShiftHeader.Status == 2) // Chua dong ca
                            {
                                return Json(new MessageCheckEndShiftResponse
                                {
                                    Status = 5,
                                    Message = listShiftHeader.Message,
                                });
                            }

                            // Kiem tra may POS chua dong ngay
                            var data = _activitiesBLO.ListTerminalNotEndShift(SiteNo, ngayKD);
                            var checkFinish = data.Where(d => d.IsFinished == false).ToList();

                            if (checkFinish.Count > 0)
                            {
                                return Json(new MessageCheckEndShiftResponse
                                {
                                    Status = 2,
                                    Message = "Danh sách máy POS chưa đóng ngày",
                                    Data = checkFinish
                                });
                            }
                        }

                        var dataStore = _activitiesBLO.SaleBusinessStoreStaging(SiteNo, ngayKD);
                        var dataStoreResult = dataStore.Where(d => d.IsClosed != null).ToList();

                        if (dataStoreResult.Count > 0)
                        {
                            var checkStoreNotDate = dataStoreResult.Where(d => d.IsClosed == false).ToList();

                            if (checkStoreNotDate.Count > 0)
                            {
                                var array = string.Join(",", checkStoreNotDate.Select(a => a.TerminalID));
                                return Json(new MessageCheckEndShiftResponse
                                {
                                    Status = 3,
                                    Message = $"Danh sách máy POS chưa đóng ngày: {array} ",
                                    Data = array
                                });
                            }

                            //
                            var checkAgain = false;
                            var checkTotalSale = _activitiesBLO.CheckTotalSale(SiteNo, ngayKD); // Khong lay cac don hang ban tren PosWeb
                            
                            // Check POSTerminal miss OrderNo
                            if (!checkTotalSale.Item1)   // checkTotalSale.Item1 = false
                            {
                                checkAgain = true;
                                var listPosTerminalMissOrder = _activitiesBLO.ListPosTerminalMissOrderNo(SiteNo, ngayKD);

                                if (listPosTerminalMissOrder != null && listPosTerminalMissOrder.Count > 0)
                                {
                                    UploadSaleFromPOSToCentral(SiteNo, ngayKD, listPosTerminalMissOrder);
                                }
                            }

                            // Check lại sau khi đồng bộ từ POS
                            if (checkAgain)
                            {
                                var checkEODPOS = _activitiesBLO.CheckTotalSale(SiteNo, ngayKD);  // Khong lay cac don hang ban tren PosWeb
                                if (!checkEODPOS.Item1)    // checkEODPOS.Item1 = false  
                                {
                                    var totalSalePOS = checkEODPOS.Item2;
                                    var totalSaleCentral = checkEODPOS.Item3;
                                    return Json(new MessageCheckEndShiftResponse
                                    {
                                        Status = 0,
                                        Message = $"Hiện tại hệ thống đang đồng bộ dữ liệu sale: POS({totalSalePOS})-Central({totalSaleCentral}), Vui lòng liên hệ IT hỗ trợ",
                                        Data = null
                                    });
                                }
                            }

                            // Cảnh báo các ngày chưa xác nhận

                            var checkList = _activitiesBLO.ListCheckBusinessDate(SiteNo, ngayKD);

                            if (checkList != null && checkList.Count > 0 && RetryConfirm == 0)
                            {
                                var listDate = string.Join(", ", checkList.OrderBy(d => d.BussinessDate).Select(d => d.BussinessDate.Value.ToString("dd-MM-yyyy")));
                                return Json(new MessageCheckEndShiftResponse
                                {
                                    Status = 4,
                                    Message = $"Danh sách ngày [{listDate}] chưa được xác nhận kết thúc ngày. Bạn có chắc chắn xác nhận không?",
                                    Data = null
                                });
                            }

                            // Tự động insert ngày chưa xác nhận

                            if (checkList != null && checkList.Count > 0 && RetryConfirm == 1) 
                            {
                                var listRetry = new List<BussinessDateConfirmModel>();
                                var listErrorUser = string.Empty;
                                var listErrorPOS = string.Empty;
                                foreach (var item in checkList)
                                {
                                    var dataStoreRetry = _activitiesBLO.SaleBusinessStore(SiteNo, (DateTime)item.BussinessDate);
                                    var dataStoreResultRetry = dataStoreRetry.Where(d => d.IsClosed != null).ToList();

                                    if (dataStoreResultRetry.Count > 0)
                                    {
                                        var ngayRetry = (DateTime)item.BussinessDate;
                                        var dataCheckRetry = _activitiesBLO.CheckFinishSale(SiteNo, ngayRetry);
                                        var checkStoreNotDateRetry = dataStoreResultRetry.Where(d => d.IsClosed == false).ToList();

                                        if (checkStoreNotDateRetry.Count > 0)
                                        {
                                            var array = string.Join(",", checkStoreNotDateRetry.Select(a => a.TerminalID));
                                            listErrorPOS += $"Ngày kinh doanh {ngayRetry.ToString("dd/MM/yyyy")} có máy POS [{array}] chưa đóng ngày";
                                        }

                                        var totalAmountRetry = dataStoreResultRetry.Sum(d => d.AmountTotal);
                                        BussinessDateConfirmModel modelRety = new BussinessDateConfirmModel
                                        {
                                            Code = $"{SiteNo}{item.BussinessDate.Value.ToString("yyyyMMdd")}",
                                            StoreNo = SiteNo,
                                            TotalAmount = totalAmountRetry,
                                            BussinessDate = item.BussinessDate,
                                            IsConfirm = true,
                                            ConfirmDate = DateTime.Now,
                                            FileNameDetail = "",
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = base.LoginUser.UserName
                                        };

                                        listRetry.Add(modelRety);

                                    }
                                }

                                if (listRetry.Count > 0)
                                {
                                    if (!string.IsNullOrEmpty(listErrorUser))
                                    {
                                        return Json(new MessageCheckEndShiftResponse
                                        {
                                            Status = 0,
                                            Message = listErrorUser,
                                            Data = null
                                        });
                                    }

                                    if (!string.IsNullOrEmpty(listErrorPOS))
                                    {
                                        return Json(new MessageCheckEndShiftResponse
                                        {
                                            Status = 0,
                                            Message = listErrorPOS,
                                            Data = null
                                        });
                                    }

                                    _activitiesBLO.ConfirmRetryBusinessDate(listRetry, RetryConfirm); // Insert data vao table : BussinessDateConfirms

                                }
                            }

                            // Data ban tren cac POSWEB
                            //var dataStoreByPOSWEB = dataStoreResult.Where(d => d.Placement == "POSWEB").ToList();
                            //var totalAmount_POSWEB = dataStoreByPOSWEB.Sum(d => d.AmountTotal);
                            //var userName_POSWEB = base.LoginUser.UserName;

                            //BussinessDateConfirmModel modelConfirm_POSWEB = new BussinessDateConfirmModel
                            //{
                            //    Code = $"{SiteNo}{ngayKD.ToString("yyyyMMdd")}",
                            //    StoreNo = SiteNo,
                            //    TotalAmount = totalAmount_POSWEB,
                            //    BussinessDate = ngayKD,
                            //    IsConfirm = true,
                            //    ConfirmDate = DateTime.Now,
                            //    FileNameDetail = "",
                            //    CreatedDate = DateTime.Now,
                            //    CreatedUser = userName_POSWEB
                            //};

                            //var listModelConfirm_POSWEB = new List<BussinessDateConfirmModel>();
                            //listModelConfirm_POSWEB.Add(modelConfirm_POSWEB);
                            //var result_POSWEB = _activitiesBLO.ConfirmBusinessDate(listModelConfirm_POSWEB);


                            // Data ban tren cac POS không phải POSWEB
                            //var dataStoreByPOS = dataStoreResult.Where(d => d.Placement != "POSWEB").ToList();

                            var totalAmount = dataStoreResult.Sum(d => d.AmountTotal);
                            var userName = base.LoginUser.UserName;

                            BussinessDateConfirmModel modelConfirm = new BussinessDateConfirmModel
                            {
                                Code = $"{SiteNo}{ngayKD.ToString("yyyyMMdd")}",
                                StoreNo = SiteNo,
                                TotalAmount = totalAmount,
                                BussinessDate = ngayKD,
                                IsConfirm = true,
                                ConfirmDate = DateTime.Now,
                                FileNameDetail = "",
                                CreatedDate = DateTime.Now,
                                CreatedUser = userName
                            };

                            var listModelConfirm = new List<BussinessDateConfirmModel>();
                            listModelConfirm.Add(modelConfirm);
                            var result = _activitiesBLO.ConfirmBusinessDate(listModelConfirm);

                            if (result.Status == 1)
                            {
                                // Item2 = TotalSale in POSEOD, Item3 = CountTransHeader
                                if (checkTotalSale.Item2 != checkTotalSale.Item3)     // Update TotalSale vào POSEOD
                                {
                                    var totalSalePOSEOD = checkTotalSale.Item2;
                                    var totalSaleTransHeader = checkTotalSale.Item3;

                                    //_commonBLO.InsertLogErrorWeb(new LogErrorWebModel
                                    //{
                                    //    StoreNo = SiteNo,
                                    //    PosTerminal = "",
                                    //    OrderNo = "",
                                    //    Action = "CheckFinishDate",
                                    //    Description = $"Xác nhận kết thúc ngày",
                                    //    CreatedDate = DateTime.Now,
                                    //    CreatedUser = base.LoginUser.UserName,
                                    //    Msg = $"Ngày kinh doanh: {ngayKD} - TotalSalePOSEOD:{totalSalePOSEOD} - TotalSaleTransHeader:{totalSaleTransHeader}"
                                    //});

                                    Task.Run(() => _activitiesBLO.UpdateTotalSaleToPOSEOD(SiteNo, ngayKD));

                                }

                                return Json(new MessageCheckEndShiftResponse
                                {
                                    Status = 1,
                                    Message = result.Message,
                                    Data = null
                                });

                            }
                            else
                            {
                                return Json(new MessageCheckEndShiftResponse
                                {
                                    Status = 0,
                                    Message = result.Message,
                                    Data = null
                                });
                            } 
                        }
                        else
                        {
                            return Json(new MessageCheckEndShiftResponse
                            {
                                Status = 0,
                                Message = $"ST/CH {SiteNo} không có dữ liệu bán hàng.",
                                Data = null
                            });
                        }
                    }
                    else
                    {
                        return Json(new MessageCheckEndShiftResponse
                        {
                            Status = 0,
                            Message = "Không lấy được StyleProfile.",
                            Data = null
                        });
                    }
                }
                else
                {
                    return Json(new MessageCheckEndShiftResponse
                    {
                        Status = 0,
                        Message = "Bạn chưa được phân quyền vào trang kết thúc ngày",
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new MessageCheckEndShiftResponse
                {
                    Status = 0,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [DisplayName("Lịch sử kết thúc ca tại POS")]
        public ActionResult HistoryShiftEnd()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        // Lịch sử kết thúc ca tại POS
        [HttpPost]
        public ActionResult HistoryEndShift(string storeNo, string staffCode, string DateBusiness, string Status)
        {
            try
            {
                var userName = base.LoginUser.UserName;
                var role = _authenBLO.GetRoleByUser(userName);
                DateTime ngayKD = DateTime.ParseExact(DateBusiness, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var data = _activitiesBLO.ListHistoryEndShift(staffCode, storeNo, ngayKD, Status);
                if (data != null && data.Count > 0)
                {
                    return Json(new { Result = data, Role = role });
                }
            }
            catch (Exception ex)
            {
            }
            return Json(new { Result = new List<HistoryEndShiftModel>(), Role = new UserRoleModel() });
        }

        [HttpPost]
        public ActionResult DeleteShiftEnd(string ShiftCode, string ShiftNumber, string Remark)
        {
            try
            {
                var user = base.LoginUser.UserName;
                var data = _activitiesBLO.DeleteShiftEnd(ShiftCode, ShiftNumber, Remark, user);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new Tuple<int, string>(0, $"Xóa ca {ShiftNumber} error {ex.Message}"));
            }
        }

        [HttpPost]
        public ActionResult GetAllStaff(string siteCode)
        {
            var listStaffCode = new List<StaffCodeModel>();
            try
            {
                if (!string.IsNullOrEmpty(siteCode))
                {
                    listStaffCode = _activitiesBLO.ListTypeUser(siteCode).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return Json(listStaffCode);
        }

        [HttpGet]
        public ActionResult _ViewGiaiTrinh(string shiftCode, string storeNo, string bussinessDate, string viewTitle)
        {
            var data = _activitiesBLO.GetDescription(storeNo, shiftCode);
            DateTime ngayKD = DateTime.ParseExact(bussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var result = new ViewShiftHeaderDesciptionModel
            {
                ShiftCode = shiftCode,
                StoreNo = storeNo,
                BussinessDate = ngayKD,
                ViewTitle = viewTitle,
                DescriptionModel = data
            };
            return PartialView(result);
        }

        [HttpPost]
        public ActionResult UpdateGiaiTrinh(string shiftCode, string storeNo, string bussinessDate, string description)
        {
            try
            {
                var user = base.LoginUser.UserName;
                DateTime ngayKD = DateTime.ParseExact(bussinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var model = new ShiftHeaderDesciptionModel
                {
                    ShiftCode = shiftCode,
                    StoreNo = storeNo,
                    BussinessDate = ngayKD,
                    Descriptions = description,
                    CreatedDate = DateTime.Now,
                    CreatedUser = user
                };
                var data = _activitiesBLO.UpdateDescription(model);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new Tuple<bool, string>(false, $"Error {ex.Message}"));
            }
        }

        [HttpPost]
        public ActionResult DeleteGiaiTrinh(string shiftCode, string storeNo)
        {
            try
            {
                var user = base.LoginUser.UserName;
                var model = new ShiftHeaderDesciptionModel
                {
                    ShiftCode = shiftCode,
                    StoreNo = storeNo
                };
                var data = _activitiesBLO.DeleteDescription(model);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new Tuple<bool, string>(false, $"Error {ex.Message}"));
            }
        }

        [DisplayName("Lịch sử kết thúc ngày tại POS")]
        public ActionResult HistoryDateEndStore()
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
        public ActionResult LoadHistoryDateEndStore(string StoreNo, string BusinessDate, string POSID, string Status)
        {
            try
            {
                if (!string.IsNullOrEmpty(StoreNo) && !string.IsNullOrEmpty(BusinessDate))
                {
                    DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var result = _activitiesBLO.SaleBusinessStore(StoreNo, ngayKD);

                    if (!string.IsNullOrEmpty(POSID))
                    {
                        result = result.Where(d => d.TerminalID == POSID).ToList();
                    }
                    if (!string.IsNullOrEmpty(Status))
                    {
                        var isClosed = Status == "0" ? false : Status == "1" ? true : false;
                        result = result.Where(d => d.IsClosed == isClosed).ToList();
                    }
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                return Json(new List<SaleBusinessStoreModel>());
            }
            return Json(new List<SaleBusinessStoreModel>());
        }

        // Lich su ket thuc ngay tai POS
        public ActionResult HistoryDateEndStoreLoad()
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
                var setDB = Request?.Form["SetDB"];
                var setDB_Read = Request?.Form["ServerRead"];
                var storeNo = Request?.Form["StoreNo"];
                var PosID = Request?.Form["TerminalID"];
                var status = Request?.Form["Status"];

                DateTime ngayKD = DateTime.ParseExact(Request?.Form["BussinessDate"], "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(storeNo);
                var jsonStore = JsonConvert.SerializeObject(modelStore);

                if (string.IsNullOrEmpty(storeNo) || string.IsNullOrEmpty(modelStore.SiteNo))
                {
                    var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, setDB).ToList();
                    var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                    {
                        SiteNo = a.SiteNo
                    }).ToList();
                    jsonStore = JsonConvert.SerializeObject(listParam);
                }

                var result = _activitiesBLO.HistoryConfirmDatePOS(setDB_Read, jsonStore, PosID, ngayKD, status, pageSize, pageNumber);
                recordsTotal = result.Count() == 0 ? 0 : result.FirstOrDefault().TotalRow;
                return Json(new DataTablesViewModel<HistoryConfirmEndDatePOSModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return Json(new DataTablesViewModel<HistoryConfirmEndDatePOSModel>());
            }
        }

        public ActionResult HistoryDateEndStoreExcel(string SetDB, string SiteNo, string POSTerminal, string TuNgay, string Status)
        {
            if (string.IsNullOrEmpty(POSTerminal))
            {
                POSTerminal = "";
            }

            DateTime ngayKD = DateTime.ParseExact(TuNgay, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            var modelStore = JsonConvert.DeserializeObject<ParamJsonStoreList>(SiteNo);
            var jsonStore = JsonConvert.SerializeObject(modelStore);

            if (string.IsNullOrEmpty(SiteNo) || string.IsNullOrEmpty(modelStore.SiteNo))
            {
                var listStoreNo = _commonBLO.LoadStoreByUserNameSetDB(base.LoginUser.UserName, SetDB).ToList();
                var listParam = listStoreNo.Select(a => new ParamJsonStoreList
                {
                    SiteNo = a.SiteNo
                }).ToList();
                jsonStore = JsonConvert.SerializeObject(listParam);
            }

            var result = _activitiesBLO.HistoryConfirmDatePOS(SetDB, jsonStore, POSTerminal, ngayKD, Status, 1000000, 1);
            var data = new List<HistoryConfirmPOSExcel>();

            data = result.Select(a => new HistoryConfirmPOSExcel
            {
                StoreNo = a.StoreNo,
                TerminalID = a.TerminalID,
                DocumentType = a.DocumentType,  // Loai POS
                Status = a.IsClosed == true ? "Đã đóng ngày" : "Chưa đóng ngày",
                LastOrderTime = a.LastOrderTime,
                CreatedDate = a.CreatedDate,
                CloseDate = a.CloseDate,
                AmountTotal = a.AmountTotal,
                CashMoney = a.CashMoney,
                CountCustomer = a.CountCustomer,
                CountOrderNo = a.CountOrderNo
            }).ToList();

            if (data.Count > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells[1, 1].Value = "ST/CH";
                    worksheet.Cells[1, 2].Value = "Máy POS";
                    worksheet.Cells[1, 3].Value = "Loại POS";
                    worksheet.Cells[1, 4].Value = "Trạng thái";
                    worksheet.Cells[1, 5].Value = "Lần bán hàng cuối cùng";
                    worksheet.Cells[1, 6].Value = "Thời gian mở ngày";
                    worksheet.Cells[1, 7].Value = "Thời gian kết thúc ngày";
                    worksheet.Cells[1, 8].Value = "Tiền bán ra";
                    worksheet.Cells[1, 9].Value = "Tiền mặt";
                    worksheet.Cells[1, 10].Value = "Số lượt khách hàng";
                    worksheet.Cells[1, 11].Value = "Số lượng bán";

                    using (ExcelRange r = worksheet.Cells[1, 1, 1, 11])
                    {
                        r.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        r.Style.Font.Bold = true;
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#78bff5"));
                        r.AutoFilter = true;
                    }

                    var countR = data.Count;
                    for (var row = 1; row <= countR + 1; row++)
                    {
                        worksheet.Cells[row, 5].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                        worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 10].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 11].Style.Numberformat.Format = "#,##0";
                    }

                    worksheet.Cells["A2"].LoadFromCollection(data, false);
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    string fileName = $"HistoryConfirmPOS{ DateTime.Now.ToString("yyyyMMddhhmmssffff")}";
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

        [DisplayName("Lịch sử xác nhận kết thúc ngày")]
        public ActionResult HistoryDateEnd()
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

        public ActionResult HistoryEndDate()
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

                var setDB = Request?.Form["SetDB"];
                var setDB_Red = Request?.Form["ServerRead"];
                var storeNo = Request?.Form["StoreNo"];
                var dateBusiness = Request?.Form["BusinessDate"];
                var slStatus = Request?.Form["IsConfirm"];
                var recordsTotal = 0;

                DateTime ngayKD = DateTime.ParseExact(dateBusiness, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var result = _activitiesBLO.ListHistoryConfirmEndDate(setDB_Red, storeNo, ngayKD, slStatus, pageSize, pageNumber);

                if (result.Count > 0)
                {
                    recordsTotal = (int)result.FirstOrDefault()?.TotalRow;
                }

                return Json(new DataTablesViewModel<HistoryEndDateModel> { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = result });

            }
            catch (Exception ex)
            {
                return Json(new List<HistoryEndDateModel>());
            }
        }

        // Xem chi tiet tong hop ket thuc ngay ( View PDF )
        public ActionResult DetailEndDate(string storeNo, string businessDate)
        {
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName;
            var data = new ViewDetailEndDateModel();
            try
            {
                DateTime ngayKD = DateTime.ParseExact(businessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var lstDetail = _activitiesBLO.ViewDetailEndDate(storeNo, ngayKD);
                if (lstDetail.Count > 0)
                {
                    var lst1 = new List<EndDateTenderTypeModel>();
                    var lst2 = new List<EndDateAmountVATModel>();
                    var lst3 = new List<EndDateRefundAmountVATModel>();
                    var lst4 = new List<EndDateAmountNotVATModel>();
                    var lst5 = new List<EndDateRefundAmountNotVATModel>();
                    var lst6 = new List<CommissionTypeModel>();
                    var lst7 = new List<EndDateInfoStaff>();
                    var cxPoint = new List<ReceivablesCXPoint>();

                    if (lstDetail[0].Rows.Count > 0)
                    {
                        // Phuong thuc thanh toan
                        var lstOb1 = JsonConvert.SerializeObject(lstDetail[0]);
                        lst1 = JsonConvert.DeserializeObject<List<EndDateTenderTypeModel>>(lstOb1);
                    }
                    if (lstDetail[1].Rows.Count > 0)
                    {
                        // Doanh thu chiu thue
                        var lstOb2 = JsonConvert.SerializeObject(lstDetail[1]);
                        lst2 = JsonConvert.DeserializeObject<List<EndDateAmountVATModel>>(lstOb2);
                    }
                    if (lstDetail[2].Rows.Count > 0)
                    {
                        // Trả lại chiu thue
                        var lstOb3 = JsonConvert.SerializeObject(lstDetail[2]);
                        lst3 = JsonConvert.DeserializeObject<List<EndDateRefundAmountVATModel>>(lstOb3);
                    }
                    if (lstDetail[3].Rows.Count > 0)
                    {
                        // Doanh thu không chiu thuế
                        var lstOb4 = JsonConvert.SerializeObject(lstDetail[3]);
                        lst4 = JsonConvert.DeserializeObject<List<EndDateAmountNotVATModel>>(lstOb4);
                    }
                    if (lstDetail[4].Rows.Count > 0)
                    {
                        // Tra lai khong chiu thue
                        var lstOb5 = JsonConvert.SerializeObject(lstDetail[4]);
                        lst5 = JsonConvert.DeserializeObject<List<EndDateRefundAmountNotVATModel>>(lstOb5);
                    }
                    if (lstDetail[5].Rows.Count > 0)
                    {
                        // Chiet khau
                        var lstOb6 = JsonConvert.SerializeObject(lstDetail[5]);
                        lst6 = JsonConvert.DeserializeObject<List<CommissionTypeModel>>(lstOb6);
                    }
                    if (lstDetail[6].Rows.Count > 0)
                    {
                        // Thong tin cua hang
                        var lstOb7 = JsonConvert.SerializeObject(lstDetail[6]);
                        lst7 = JsonConvert.DeserializeObject<List<EndDateInfoStaff>>(lstOb7);
                    }
                    if (lstDetail[7].Rows.Count > 0)
                    {
                        // Phai thu CX
                        var lstOb8 = JsonConvert.SerializeObject(lstDetail[7]);
                        cxPoint = JsonConvert.DeserializeObject<List<ReceivablesCXPoint>>(lstOb8);
                    }

                    var host = $"{Request.Url.Scheme}://{Request.Url.Authority}";
                    data = new ViewDetailEndDateModel
                    {
                        lstTenderType = lst1,                           // Phuong thuc thanh toan
                        lstAmountVAT = lst2,                            // Doanh thu chiu thue
                        lstRefundAmountVAT = lst3,                      // Tra lai chiu thue 
                        lstAmountNotVAT = lst4,                         // Doanh thu khong chiu thue                 
                        lstRefundAmountNotVAT = lst5,                   // Tra lai khong chiu thue
                        lstCommissionType = lst6,                       // Chiet khau                       
                        infoStaff = new EndDateInfoStaff                // Thông tin cua hang
                        {
                            UserName = user,
                            FullName = hoten,
                            StoreNo = lst7.FirstOrDefault()?.StoreNo,
                            StoreName = lst7.FirstOrDefault()?.StoreName,
                            BusinessDate = businessDate,
                            Address = lst7.FirstOrDefault()?.Address,
                            PhoneNo = lst7.FirstOrDefault()?.PhoneNo
                        },
                        receivablesCXPoint = cxPoint.FirstOrDefault(),  // Phai thu CX
                        Host = host               
                    };
                }
            }
            catch (Exception ex)
            {
                data = new ViewDetailEndDateModel();
            }

            var report = new PartialViewAsPdf("~/Views/StoreActivities/DetailEndDate.cshtml", data)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };
            return report;
        }

        [DisplayName("Thay đổi ngày kinh doanh")]
        public ActionResult ChangeBusinessDate()
        {
            ViewBag.ListStore = _commonBLO.LoadComboxStoreByUserName(base.LoginUser.UserName);
            return View();
        }

        public ActionResult BusinessDateUpdate(string SiteNo, string BusinessDate)
        {
            try
            {
                DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var userName = base.LoginUser.UserName;
                BussinessDateOpenModel model = new BussinessDateOpenModel
                {
                    Code = SiteNo,
                    StoreNo = SiteNo,
                    BussinessDate = ngayKD,
                    CreatedDate = DateTime.Now,
                    CreatedUser = userName,
                    UpdatedDate = DateTime.Now,
                    UpdatedUser = userName
                };

                var result = new MessageCheckEndShiftResponse();
                var timeSpan = model.BussinessDate.Value.Subtract(DateTime.Now.Date);
                if (timeSpan.Days >= 2)
                {
                    return Json(new MessageCheckEndShiftResponse
                    {
                        Status = 0,
                        Message = "Không được chỉnh sửa ngày kinh doanh lớn hơn D+1",
                        Data = null
                    });
                }
                var data = _activitiesBLO.DateBusinessUpdate(model);
                if (data.Status == 1)
                {
                    return Json(new MessageCheckEndShiftResponse {
                        Status = 1,
                        Message = data.Message,
                        Data = model
                    });
                }
                else
                {
                    return Json(new MessageCheckEndShiftResponse {
                        Status = 0,
                        Message = data.Message,
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new MessageCheckEndShiftResponse {
                    Status = 0,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [DisplayName("IT xác nhận kết thúc ngày")]
        public ActionResult ITConfirmEndDateStores()
        {
            var siteNo = base.LoginUser.SiteCode;
            var setStoreUser = _commonBLO.GetInforStoreSet(siteNo);
            var listSetDB = new List<SQLServerModel>();
            if (setStoreUser != null && setStoreUser.Count > 0)
            {
                var first = setStoreUser.FirstOrDefault();
                listSetDB.Add(new SQLServerModel { IPServer = first.IPServer, IPServerRead = first.IPServerRead, Description = first.Description });
            }
            else
            {
                listSetDB = _commonBLO.ListSQLServer();
            }
            ViewBag.SetDB = listSetDB;
            return View();
        }

        public async Task<ActionResult> ITConfirmDateEndListStore(string ArrayStore, string BusinessDate)
        {
            var data = new List<Tuple<bool, string, LogMisSaleModel>>();
            try
            {
                var listStore = ArrayStore.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var tasks = new List<Task>();
                foreach (var siteCode in listStore)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        data.Add(MultiConfirmEndDate(siteCode, ngayKD));
                    }));
                }

                Task taskW = Task.WhenAll(tasks.ToArray());
                await taskW; 
            }
            catch (Exception ex)
            {
                data.Add(new Tuple<bool, string, LogMisSaleModel>(false, ex.Message, null));
            }

            var listStoreMissingSale = "";
            var dataModel = data.Where(a => a.Item3 != null).ToList();
            var listModel = dataModel.Select(a => a.Item3).ToList();
            if (listModel != null && listModel.Count > 0)
            {
                _activitiesBLO.UpdateMisSaleLog(listModel);
                listStoreMissingSale = "";
            }

            var resultFail = data.Where(d => d.Item1 == false).ToList();
            var resultSuccess = data.Where(d => d.Item1 == true).ToList();
            var listDate = string.Join(", ", resultFail.Select(d => d.Item2));
            var msgFail = "";
            if (!string.IsNullOrEmpty(listDate))   
                msgFail = resultFail.Count > 0 ? $" và thất bại {resultFail.Count} ST/CH ({listDate})" : "";
            return Json(new MessageCheckEndShiftResponse
            {
                Status = 1,
                Message = $"Xác nhận kết thúc ngày thành công {resultSuccess.Count} ST/CH {msgFail}",
                Data = null
            });
        }

        public async Task<ActionResult> ITCancelConfirmDateEndListStore(string ArrayStore, string BusinessDate)
        {
            var data = new List<Tuple<bool, string>>();
            try
            {
                var listStore = ArrayStore.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var tasks = new List<Task>();
                foreach (var siteCode in listStore)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        data.Add(MultiConfirmEndDateCancle(siteCode, ngayKD));
                    }));
                }

                Task taskW = Task.WhenAll(tasks.ToArray());
                await taskW;
            }
            catch (Exception ex)
            {
                data.Add(new Tuple<bool, string>(false, ex.Message));
            }

            var result = data.Where(d => d.Item1 == false).ToList();
            var listDate = string.Join(", ", result.Select(d => d.Item2));
            if (!string.IsNullOrEmpty(listDate))
                return Json(new MessageCheckEndShiftResponse
                {
                    Status = 0,
                    Message = $"{listDate}",
                    Data = null
                });

            return Json(new MessageCheckEndShiftResponse
            {
                Status = 1,
                Message = $"Hủy xác nhận kết thúc ngày thành công {data.Count} ST/CH",
                Data = null
            });
        }
        public Tuple<bool, string> MultiConfirmEndDateCancle(string SiteNo, DateTime ngayKD)
        {
            var result = new Tuple<bool, string>(true, "Success");
            try
            {
                var userName = base.LoginUser.UserName;
                BussinessDateConfirmModel modelConfirm = new BussinessDateConfirmModel
                {
                    Code = $"{SiteNo}{ngayKD.ToString("yyyyMMdd")}",
                    StoreNo = SiteNo,
                    BussinessDate = ngayKD
                };
                result = _activitiesBLO.ConfirmBusinessDateCancel(modelConfirm);
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public Tuple<bool, string, LogMisSaleModel> MultiConfirmEndDate(string SiteNo, DateTime ngayKD)
        {
            var result = new Tuple<bool, string, LogMisSaleModel>(true, "Success", null);
            var userName = base.LoginUser.UserName;
            var checkDate = ngayKD.Subtract(DateTime.Now.Date).TotalDays;
            if (checkDate > 0)
            {
                result = new Tuple<bool, string, LogMisSaleModel>(false, $"ST/CH {SiteNo} vui lòng không xác nhận kết thúc ngày kinh doanh {ngayKD} lớn hơn ngày hiện tại", null);
                return result;
            }

            // Xác nhận kết thúc ngày quá khứ
            var checkList = _activitiesBLO.ListCheckBusinessDate(SiteNo, ngayKD);
            if (checkList != null && checkList.Count > 0)
            {
                var listRetry = new List<BussinessDateConfirmModel>();
                foreach (var item in checkList)
                {
                    var dataStoreRetry = _activitiesBLO.SaleBusinessStore(SiteNo, (DateTime)item.BussinessDate);
                    var dataStoreResultRetry = dataStoreRetry.Where(d => d.IsClosed != null).ToList();
                    if (dataStoreResultRetry.Count > 0)
                    {
                        var totalAmountRetry = dataStoreResultRetry.Sum(d => d.AmountTotal);
                        BussinessDateConfirmModel modelRety = new BussinessDateConfirmModel
                        {
                            Code = $"{SiteNo}{item.BussinessDate.Value.ToString("yyyyMMdd")}",
                            StoreNo = SiteNo,
                            TotalAmount = totalAmountRetry,
                            BussinessDate = item.BussinessDate,
                            IsConfirm = true,
                            ConfirmDate = DateTime.Now,
                            FileNameDetail = "",
                            CreatedDate = DateTime.Now,
                            CreatedUser = base.LoginUser.UserName
                        };
                        listRetry.Add(modelRety);
                    }
                }
                if (listRetry.Count > 0)
                {
                    _activitiesBLO.ConfirmRetryBusinessDate(listRetry, 1);
                }
            }

            // Check Sale POS sync central
            if (!_activitiesBLO.CheckTotalSale(SiteNo, ngayKD).Item1)
            {
                // Check POSTerminal orderNo
                var listPosTerminalMissOrder = _activitiesBLO.ListPosTerminalMissOrderNo(SiteNo, ngayKD);
                if (listPosTerminalMissOrder != null && listPosTerminalMissOrder.Count > 0)
                {
                    UploadSaleFromPOSToCentral(SiteNo, ngayKD, listPosTerminalMissOrder);
                }
            }

            var dataStoreResult = _activitiesBLO.SaleBusinessStore(SiteNo, ngayKD);
            var totalAmount = dataStoreResult.Sum(d => d.AmountTotal);

            BussinessDateConfirmModel modelConfirm = new BussinessDateConfirmModel
            {
                Code = $"{SiteNo}{ngayKD.ToString("yyyyMMdd")}",
                StoreNo = SiteNo,
                TotalAmount = totalAmount,
                BussinessDate = ngayKD,
                IsConfirm = true,
                ConfirmDate = DateTime.Now,
                FileNameDetail = "",
                CreatedDate = DateTime.Now,
                CreatedUser = userName
            };

            var listModelConfirm = new List<BussinessDateConfirmModel>();
            listModelConfirm.Add(modelConfirm);
            var confirm = _activitiesBLO.ConfirmBusinessDate(listModelConfirm);

            if (confirm.Status != 1)
            {
                result = new Tuple<bool, string, LogMisSaleModel>(false, $"ST/CH {confirm.Message}", null);
            }

            if (result.Item1)
            {
                // Check sau khi đã đồng bộ Sale lên Central
                var checkTotalSale = _activitiesBLO.CheckTotalSale(SiteNo, ngayKD);   
                if (!checkTotalSale.Item1)
                {
                    var modelMis = new LogMisSaleModel
                    {
                        StoreNo = SiteNo,
                        BussinessDate = ngayKD,
                        TotalSalePOS = checkTotalSale.Item2,
                        TotalSaleServer = checkTotalSale.Item3,
                        CreatedDate = DateTime.Now,
                        CreatedUser = userName
                    };
                    result = new Tuple<bool, string, LogMisSaleModel>(true, $"", modelMis);
                }

                // Item2: TotalSale in POSEOD, Item3: Count TransHeader
                // Update TotalSale vào POSEOD

                if (checkTotalSale.Item2 != checkTotalSale.Item3)     
                {
                    var totalSalePOSEOD = checkTotalSale.Item2;
                    var totalSaleTransHeader = checkTotalSale.Item3;
                    //_commonBLO.InsertLogErrorWeb(new LogErrorWebModel
                    //{
                    //    StoreNo = SiteNo,
                    //    PosTerminal = "",
                    //    OrderNo = "",
                    //    Action = "ITConfirmEndDateStores",
                    //    Description = $"IT Xác nhận kết thúc ngày",
                    //    CreatedDate = DateTime.Now,
                    //    CreatedUser = base.LoginUser.UserName,
                    //    Msg = $"Ngày kinh doanh: {ngayKD} - TotalSalePOSEOD:{totalSalePOSEOD} - TotalSaleTransHeader:{totalSaleTransHeader}"
                    //});
                    Task.Run(() => _activitiesBLO.UpdateTotalSaleToPOSEOD(SiteNo, ngayKD));
                }
            }

            return result;

        }

        [HttpPost]
        public ActionResult CheckFinishSale(string SiteNo, string StaffCode, string BusinessDate, string ShiftCode)
        {
            try
            {
                var styleProfile = _commonBLO.GetStyleProfile(SiteNo);
                if (styleProfile != null)
                {
                    if (styleProfile.StyleProfile == "VMP" || styleProfile.StyleProfile == "KS" || styleProfile.StyleProfile == "FS")    // FinishSale
                    {
                        return Json(new MessageCheckEndShiftResponse
                        {
                            Status = 0,
                            Message = "Không được kết ca trên web",
                            Data = null
                        });
                    }

                    var syncInfoStaff = _commonBLO.InfoStaffByAD(StaffCode);
                    var infoStaff = syncInfoStaff.Result;
                    if (infoStaff != null)
                    {
                        DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                        return Json(new MessageCheckEndShiftResponse
                        {
                            Status = 1,
                            Message = "Hiển thị form nhập số tiền.",
                            Data = null
                        });
                    }
                }
                else
                {
                    return Json(new MessageCheckEndShiftResponse
                    {
                        Status = 0,
                        Message = "Không lấy được StyleProfile",
                        Data = null
                    });
                }
                return Json(new MessageCheckEndShiftResponse
                {
                    Status = 0,
                    Message = "Kiểm tra lại user đăng nhập POS",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return Json(new MessageCheckEndShiftResponse
                {
                    Status = 0,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        [HttpPost]
        public ActionResult GetStaffList(string siteCode)
        {
            var listStaffCode = new List<StaffCodeModel>();
            try
            {
                var username = base.LoginUser.UserName;
                var infoStaff = _commonBLO.InfoStaffByAD(username);
                var data = infoStaff.Result;
                var role = _authenBLO.GetRoleByUser(username);
                if (role.RoleName.ToUpper() == "SA")
                {
                    if (!string.IsNullOrEmpty(siteCode))
                        listStaffCode = _commonBLO.ListStaffCode(siteCode).ToList();
                }
                else
                {
                    if (data != null)
                    {
                        if (!string.IsNullOrEmpty(siteCode))
                        {
                            if (data.EmploymentType == 1)
                            {
                                listStaffCode = _commonBLO.ListStaffCode(siteCode).ToList();
                            }
                            else
                            {
                                listStaffCode.Add(new StaffCodeModel { UserPOS = data.ID });
                            }
                        }
                        else
                        {
                            listStaffCode.Add(new StaffCodeModel { UserPOS = data.ID });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return Json(listStaffCode);
        }

        [HttpPost]
        public ActionResult FinsishShift(string StaffCode, string SiteNo, string PosTerminal, string BusinessDate, float CloseAmount, float MoneyShift, int QuantityBox, int QuantityCoupon, int QuantityVoucher, string ArrayShiftLine)
        {
            try
            {
                var lstArray = ArrayShiftLine != "[]" ? JsonConvert.DeserializeObject<List<ShiftLineArray>>(ArrayShiftLine) : new List<ShiftLineArray>();
                DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var listShiftHeader = _activitiesBLO.ListShiftHeader(StaffCode, SiteNo, ngayKD, "WEB");
                var maxShiftNumber = listShiftHeader.Count > 0 ? listShiftHeader.Max(d => int.Parse(d.ShiftNumber)) : 0;
                var ShiftNumber = maxShiftNumber == 0 ? "" + 1 : "" + (maxShiftNumber + 1);
                var userName = base.LoginUser.UserName;
                var shiftHeader = new ShiftHeaderModel
                {
                    ShiftCode = SiteNo + StaffCode + ngayKD.ToString("yyyyMMdd") + ShiftNumber,
                    StaffCode = StaffCode,
                    StoreNo = SiteNo,
                    PosTerminal = PosTerminal,
                    BussinessDate = ngayKD,
                    ShiftNumber = ShiftNumber,
                    CloseAmount = CloseAmount,
                    BeginAmount = MoneyShift,
                    QuantityBox = QuantityBox,
                    QuantityCoupon = QuantityCoupon,
                    QuantityVoucher = QuantityVoucher,
                    IsShiftOpened = true,
                    IsShiftClosed = true,
                    OpenShiftDate = DateTime.Now,
                    CloseShiftDate = DateTime.Now,
                    CreatedUser = userName,
                    CreatedDate = DateTime.Now,
                    UpdatedUser = userName,
                    UpdatedDate = DateTime.Now,
                    Source = "WEB"
                };
                var lstShiftLine = lstArray.Select(a => new ShiftLineModel
                {
                    ShiftCode = shiftHeader.ShiftCode,
                    LineNo = a.LineNo,
                    CashCode = a.CashCode,
                    CashValue = a.CashValue,
                    CashQuantity = a.CashQuantity,
                    CashAmount = a.CashAmount,
                    CreatedDate = DateTime.Now,
                    CreatedUser = userName,
                    UpdatedDate = DateTime.Now,
                    UpdatedUser = userName
                }).ToList();
                var data = _activitiesBLO.FinishShift(shiftHeader, lstShiftLine);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new MessageCheckEndShiftResponse { Status = 0, Message = ex.Message, Data = null });
            }
        }

        [HttpPost]
        public ActionResult FinsishShiftTH(string StaffCode, string SiteNo, string PosTerminal, string BusinessDate, float CloseAmount, string ArrayShiftLine)
        {
            try
            {
                var source = PosTerminal;
                var lstArray = ArrayShiftLine != "[]" ? JsonConvert.DeserializeObject<List<ShiftLineArray>>(ArrayShiftLine) : new List<ShiftLineArray>();
                DateTime ngayKD = DateTime.ParseExact(BusinessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var listShiftHeader = _activitiesBLO.ListShiftHeader(StaffCode, SiteNo, ngayKD, source).ToList();
                var maxShiftNumber = listShiftHeader.Count > 0 ? listShiftHeader.Max(d => int.Parse(d.ShiftNumber)) : 0;
                var ShiftNumber = maxShiftNumber == 0 ? "" + 1 : "" + (maxShiftNumber + 1);
                var userName = base.LoginUser.UserName;
                var shiftHeader = new ShiftHeaderModel
                {
                    ShiftCode = SiteNo + StaffCode + ngayKD.ToString("yyyyMMdd") + PosTerminal + ShiftNumber,
                    StaffCode = StaffCode,
                    StoreNo = SiteNo,
                    PosTerminal = PosTerminal,
                    BussinessDate = ngayKD,
                    ShiftNumber = ShiftNumber,
                    CloseAmount = CloseAmount,
                    IsShiftOpened = true,
                    IsShiftClosed = true,
                    OpenShiftDate = DateTime.Now,
                    CloseShiftDate = DateTime.Now,
                    CreatedUser = userName,
                    CreatedDate = DateTime.Now,
                    UpdatedUser = userName,
                    UpdatedDate = DateTime.Now,
                    Source = source
                };

                var lstShiftLine = lstArray.Select(a => new ShiftLineModel
                {
                    ShiftCode = shiftHeader.ShiftCode,
                    LineNo = a.LineNo,
                    CashCode = a.CashCode,
                    CashValue = a.CashValue,
                    CashQuantity = a.CashQuantity,
                    CashAmount = a.CashAmount,
                    CreatedDate = DateTime.Now,
                    CreatedUser = userName,
                    UpdatedDate = DateTime.Now,
                    UpdatedUser = userName
                }).ToList();
                var data = _activitiesBLO.FinishShiftTH(shiftHeader, lstShiftLine);
                return Json(data);
            }
            catch (Exception ex)
            {
                return Json(new MessageCheckEndShiftResponse
                {
                    Status = 0,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        public ActionResult DetailEndShift(string storeNo, string staffCode, string businessDate, string shiftCode)
        {
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName;
            var data = new ViewDetailEndShiftModel();
            try
            {
                DateTime ngayKD = DateTime.ParseExact(businessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var lstDetail = _activitiesBLO.ViewDetailEndShift(storeNo, staffCode, ngayKD, shiftCode);
                if (lstDetail.Count > 0)
                {
                    var lst1 = new List<TenderTypeModel>();
                    var lst2 = new List<AmountVATModel>();
                    var lst3 = new List<AmountNotVATModel>();
                    var lst4 = new List<MoneyCashModel>();
                    var lst5 = new List<InfoShift>();

                    var lst6 = new List<RefundAmountVATModel>();
                    var lst7 = new List<RefundAmountNotVATModel>();
                    var saleVinID = new List<SaleVINID>();

                    if (lstDetail[0].Rows.Count > 0)
                    {
                        var lstOb1 = JsonConvert.SerializeObject(lstDetail[0]);
                        lst1 = JsonConvert.DeserializeObject<List<TenderTypeModel>>(lstOb1);
                    }

                    if (lstDetail[1].Rows.Count > 0)
                    {
                        var lstOb2 = JsonConvert.SerializeObject(lstDetail[1]);
                        lst2 = JsonConvert.DeserializeObject<List<AmountVATModel>>(lstOb2);
                    }

                    var lstOb3 = JsonConvert.SerializeObject(lstDetail[2]);
                    lst3 = JsonConvert.DeserializeObject<List<AmountNotVATModel>>(lstOb3);

                    if (lstDetail[3].Rows.Count > 0)
                    {
                        var lstOb4 = JsonConvert.SerializeObject(lstDetail[3]);
                        lst4 = JsonConvert.DeserializeObject<List<MoneyCashModel>>(lstOb4);
                    }

                    var lstOb5 = JsonConvert.SerializeObject(lstDetail[4]);
                    lst5 = JsonConvert.DeserializeObject<List<InfoShift>>(lstOb5);

                    if (lstDetail[5].Rows.Count > 0)
                    {
                        var lstOb6 = JsonConvert.SerializeObject(lstDetail[5]);
                        lst6 = JsonConvert.DeserializeObject<List<RefundAmountVATModel>>(lstOb6);
                    }
                    if (lstDetail[6].Rows.Count > 0)
                    {
                        var lstOb7 = JsonConvert.SerializeObject(lstDetail[6]);
                        lst7 = JsonConvert.DeserializeObject<List<RefundAmountNotVATModel>>(lstOb7);
                    }

                    if (lstDetail[7].Rows.Count > 0)
                    {
                        var lstOb8 = JsonConvert.SerializeObject(lstDetail[7]);
                        saleVinID = JsonConvert.DeserializeObject<List<SaleVINID>>(lstOb8);
                    }

                    data = new ViewDetailEndShiftModel
                    {
                        lstTenderType = lst1,
                        lstAmountVAT = lst2,
                        lstAmountNotVAT = lst3,
                        lstMoneyCash = lst4,
                        lstInfoShift = lst5,
                        infoStaff = new InfoStaff { UserName = user, FullName = hoten },
                        lstRefundAmountVAT = lst6,
                        lstRefundAmountNotVAT = lst7,
                        saleVINID = saleVinID.FirstOrDefault()
                    };
                }
            }
            catch (Exception ex)
            {
            }
            var report = new PartialViewAsPdf("~/Views/StoreActivities/DetailEndShift.cshtml", data)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };
            return report;
        }

        public ActionResult DetailEndShiftTH(string storeNo, string staffCode, string businessDate, string shiftCode)
        {
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName;
            var data = new ViewDetailEndShiftModel();
            try
            {
                DateTime ngayKD = DateTime.ParseExact(businessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var lstDetail = _activitiesBLO.ViewDetailEndShiftTH(storeNo, staffCode, ngayKD, shiftCode);
                if (lstDetail.Count > 0)
                {
                    var lst4 = new List<MoneyCashModel>();
                    var lst5 = new List<InfoShift>();

                    if (lstDetail[0].Rows.Count > 0)
                    {
                        var lstOb4 = JsonConvert.SerializeObject(lstDetail[0]);
                        lst4 = JsonConvert.DeserializeObject<List<MoneyCashModel>>(lstOb4);
                    }

                    var lstOb5 = JsonConvert.SerializeObject(lstDetail[1]);
                    lst5 = JsonConvert.DeserializeObject<List<InfoShift>>(lstOb5);

                    data = new ViewDetailEndShiftModel
                    {
                        lstMoneyCash = lst4,
                        lstInfoShift = lst5,
                        infoStaff = new InfoStaff { UserName = user, FullName = hoten }
                    };
                }
            }
            catch (Exception ex)
            {
            }
            var report = new PartialViewAsPdf("~/Views/StoreActivities/DetailEndShiftTH.cshtml", data)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };

            return report;
        }

        // Chi tiết : lịch sử xác nhận kết thúc ca tại POS (Phúc Long)
        public ActionResult DetailEndShiftPLG(string storeNo, string posTerminal, string businessDate, string shiftCode)
        {
            var user = base.LoginUser.UserName;
            var hoten = base.LoginUser.FullName;
            var data = new ViewDetailEndShiftPLGModel();

            try
            {

                DateTime ngayKD = DateTime.ParseExact(businessDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var lstDetail = _activitiesBLO.ViewDetailEndShiftPLG(storeNo, posTerminal, ngayKD, shiftCode);

                if (lstDetail.Count > 0)
                {
                    var lst1 = new List<TenderTypeModel>();
                    var lst2 = new List<AmountVATModel>();
                    var lst3 = new List<AmountNotVATModel>();
                    var lst4 = new List<RefundAmountVATModel>();
                    var lst5 = new List<RefundAmountNotVATModel>();
                    var lst6 = new List<MoneyCashModel>();
                    var infoHeader = new List<InfoHeaderPLG>();
                    var saleVinID = new List<SaleVINID>();
                    var receivablesCXPoint = new List<ReceivablesCXPoint>();

                    if (lstDetail[0].Rows.Count > 0)
                    {
                        var lstOb1 = JsonConvert.SerializeObject(lstDetail[0]);
                        lst1 = JsonConvert.DeserializeObject<List<TenderTypeModel>>(lstOb1);
                    }

                    if (lstDetail[1].Rows.Count > 0)
                    {
                        var lstOb2 = JsonConvert.SerializeObject(lstDetail[1]);
                        lst2 = JsonConvert.DeserializeObject<List<AmountVATModel>>(lstOb2);
                    }

                    if (lstDetail[2].Rows.Count > 0)
                    {
                        var lstOb3 = JsonConvert.SerializeObject(lstDetail[2]);
                        lst3 = JsonConvert.DeserializeObject<List<AmountNotVATModel>>(lstOb3);
                    }

                    if (lstDetail[3].Rows.Count > 0)
                    {
                        var lstOb4 = JsonConvert.SerializeObject(lstDetail[3]);
                        lst4 = JsonConvert.DeserializeObject<List<RefundAmountVATModel>>(lstOb4);
                    }

                    if (lstDetail[4].Rows.Count > 0)
                    {
                        var lstOb5 = JsonConvert.SerializeObject(lstDetail[4]);
                        lst5 = JsonConvert.DeserializeObject<List<RefundAmountNotVATModel>>(lstOb5);
                    }

                    if (lstDetail[5].Rows.Count > 0)
                    {
                        var lstOb6 = JsonConvert.SerializeObject(lstDetail[5]);
                        lst6 = JsonConvert.DeserializeObject<List<MoneyCashModel>>(lstOb6);
                    }

                    if (lstDetail[6].Rows.Count > 0)
                    {
                        var lstOb7 = JsonConvert.SerializeObject(lstDetail[6]);
                        infoHeader = JsonConvert.DeserializeObject<List<InfoHeaderPLG>>(lstOb7);
                    }

                    if (lstDetail[7].Rows.Count > 0)
                    {
                        var lstOb8 = JsonConvert.SerializeObject(lstDetail[7]);
                        receivablesCXPoint = JsonConvert.DeserializeObject<List<ReceivablesCXPoint>>(lstOb8);
                    }

                    //if (lstDetail[7].Rows.Count > 0)
                    //{
                    //    var lstOb8 = JsonConvert.SerializeObject(lstDetail[7]);
                    //    saleVinID = JsonConvert.DeserializeObject<List<SaleVINID>>(lstOb8);
                    //}

                    var host = $"{Request.Url.Scheme}://{Request.Url.Authority}";

                    data = new ViewDetailEndShiftPLGModel
                    {
                        lstTenderType = lst1,
                        lstAmountVAT = lst2,
                        lstAmountNotVAT = lst3,
                        lstRefundAmountVAT = lst4,
                        lstRefundAmountNotVAT = lst5,
                        lstMoneyCash = lst6,
                        infoHeaderPLG = infoHeader.FirstOrDefault(),
                        infoStaff = new InfoStaff { UserName = user, FullName = hoten },
                        receivablesCXPoint = receivablesCXPoint.FirstOrDefault(),
                        Host = host
                        //saleVINID = saleVinID.FirstOrDefault() 
                    };
                }
            }
            catch (Exception ex)
            {
            }

            var report = new PartialViewAsPdf("~/Views/StoreActivities/DetailEndShiftPLG.cshtml", data)
            {
                CustomSwitches = "--page-offset 0 --footer-center [page]/[toPage] --footer-font-size 10 --header-spacing 0",
                PageSize = Rotativa.Options.Size.A4
            };

            return report;

        }










    }
}