using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Data.SqlClient;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.DBRead.CentralSales;
using VCM.BLUEPOS.Data.EF.EInvoices;
using VCM.BLUEPOS.Model.StoreActivities;
using VCM.BLUEPOS.Model.Invoice;


namespace VCM.BLUEPOS.Data.StoreActivities
{
    public interface IStoreActivitiesData
    {
        Task<StaffCodeModel> StaffInforByAD(string userName);
        List<StaffCodeModel> GetStaffCodeList(string storeNo);
        List<ShiftHeaderModel> ListShiftHeader(string staffCode, string storeNo, DateTime bussinessDate, string source);
        BussinessDateOpenModel GetEndDateBusiness(string storeNo);
        Task<Tuple<List<ShiftHeaderModel>, int>> GetShiftHeaderList(string storeNo, string staffCode, string typeTrans, DateTime dateTime, int Skip, int Take);
        List<SaleBusinessStoreModel> SaleBusinessStoreStaging(string storeNo, DateTime businessDate);
        ViewPopupNotEnd ListTerminalNotEndShiftHeader(string storeNo, DateTime businessDate);
        List<ViewPopupNotEnd> ListTerminalNotEndShift(string storeNo, DateTime businessDate);
        List<EODCreatedInvoiceModel> ListNotCreatedInvoice(string StoreNo);
        List<TransHeaderPOSModel> ListOrderPOS(string connectStringPOS, string storeNo, string posTerminal, DateTime businessDate);
        Tuple<bool, int, int> CheckTotalSale(string storeNo, DateTime businessDate);
        List<PosTerminalModel> GetTerminalStore(string storeNo);
        List<PosTerminalMissOrderModel> ListPosTerminalMissOrderNo(string storeNo, DateTime businessDate);
        bool InsertUpdatePOSEOD(List<POSEODModel> model);
        List<BusinessDateModel> ListCheckBusinessDate(string storeNo, DateTime businessDate);
        List<SaleBusinessStoreModel> SaleBusinessStore(string storeNo, DateTime businessDate);
        List<ViewPopupNotEnd> CheckFinishSale(string storeNo, DateTime businessDate);
        void ConfirmRetryBusinessDate(List<BussinessDateConfirmModel> listmodel, int RetryConfirm);
        MessageCheckEndShiftResponse ConfirmBusinessDate(List<BussinessDateConfirmModel> listmodel);
        bool UpdateTotalSaleToPOSEOD(string storeNo, DateTime businessDate);
        List<HistoryEndShiftModel> ListHistoryEndShift(string staffCode, string storeNo, DateTime bussinessDate, string status);
        Tuple<int, string> DeleteShiftEnd(string shiftCode, string shiftNumber, string remark, string userName);
        List<StaffCodeModel> ListTypeUser(string storeNo);
        List<HistoryConfirmEndDatePOSModel> HistoryConfirmDatePOS(string setDB, string listStore, string PosID, DateTime fromDate, string status, int pageSize, int pageNumber);
        List<HistoryEndDateModel> ListHistoryConfirmEndDate(string setDB, string siteNo, DateTime businessDate, string isConfirm, int pageSize, int pageNumber);
        List<DataTable> ViewDetailEndDate(string storeNo, DateTime businessDate);
        MessageCheckEndShiftResponse DateBusinessUpdate(BussinessDateOpenModel model);
        Tuple<bool, string> ConfirmBusinessDateCancel(BussinessDateConfirmModel model);
        bool UpdateMisSaleLog(List<LogMisSaleModel> model);
        List<TransHeader> ListTransPOS(DateTime businessDate, string storeNo, string staffCode);
        MessageCheckEndShiftResponse FinishShift(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel);
        MessageCheckEndShiftResponse FinishShiftTH(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel);
        List<DataTable> ViewDetailEndShift(string storeNo, string staffCode, DateTime businessDate, string shifCode);
        List<DataTable> ViewDetailEndShiftTH(string storeNo, string staffCode, DateTime businessDate, string shifCode);
        List<DataTable> ViewDetailEndShiftPLG(string storeNo, string staffCode, DateTime businessDate, string shiftCode);
        ShiftHeaderDesciptionModel GetDescription(string storeNo, string shiftCode);

    }

    public class StoreActivitiesData : IStoreActivitiesData
    {
        public async Task<StaffCodeModel> StaffInforByAD(string userName)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Staffs.Where(d => d.ID == userName).Select(x => new StaffCodeModel
                    {
                        ID = x.ID,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        StoreNo = x.StoreNo,
                        EmploymentType = x.EmploymentType
                    }).FirstOrDefault();

                    if (data == null)
                    {
                        return default(StaffCodeModel);
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return default(StaffCodeModel);
            }
        }
        public List<StaffCodeModel> GetStaffCodeList(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Staffs
                                where a.StoreNo == storeNo   // && a.EmploymentType == 0
                                select new StaffCodeModel
                                {
                                    UserPOS = a.ID,
                                    EmploymentType = a.EmploymentType
                                }).ToList();
                    return data;
                }
            }
            catch
            {
                return (default(List<StaffCodeModel>));
            }
        }
        public List<ShiftHeaderModel> ListShiftHeader(string staffCode, string storeNo, DateTime bussinessDate, string source)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var result = new List<ShiftHeaderModel>();
                    result = db.ShiftHeaders.Where(d => d.BussinessDate == DbFunctions.TruncateTime(bussinessDate)
                    && d.StoreNo == storeNo && d.Source == source
                    && d.StaffCode == staffCode)
                    .Select(a => new ShiftHeaderModel
                    {
                        ShiftCode = a.ShiftCode,
                        StaffCode = a.StaffCode,
                        ShiftNumber = a.ShiftNumber
                    }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ShiftHeaderModel>));
            }
        }
        public BussinessDateOpenModel GetEndDateBusiness(string storeNo)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.BussinessDateOpens
                                where a.StoreNo == storeNo
                                select new BussinessDateOpenModel
                                {
                                    Code = a.Code,
                                    StoreNo = a.StoreNo,
                                    BussinessDate = a.BussinessDate,
                                    CreatedDate = a.CreatedDate,
                                    UpdatedUser = a.UpdatedUser,
                                    UpdatedDate = a.UpdatedDate,
                                    StatusConfirm = (db.BussinessDateConfirms.Any(x => x.StoreNo == storeNo && x.IsConfirm == true 
                                    && DbFunctions.TruncateTime(x.BussinessDate) == DbFunctions.TruncateTime(a.BussinessDate)))
                                }).OrderByDescending(d => d.CreatedDate).FirstOrDefault();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(BussinessDateOpenModel));
            }
        }
        public async Task<Tuple<List<ShiftHeaderModel>, int>> GetShiftHeaderList(string storeNo, string staffCode, string typeTrans, DateTime dateTime, int Skip, int Take)
        {
            try
            {
                var total = 0;
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);

                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var resultData = db.ShiftHeaders.Where(a => a.StoreNo == storeNo && (a.Source == "WEB")
                                     && DbFunctions.TruncateTime(a.BussinessDate) == DbFunctions.TruncateTime(dateTime));

                    if (!string.IsNullOrEmpty(staffCode))
                    {
                        resultData = resultData.Where(d => d.StaffCode == staffCode);
                    }

                    if (!string.IsNullOrEmpty(typeTrans))
                    {
                        resultData = resultData.Where(d => d.Source == typeTrans);
                    }

                    var dataEnd = resultData.Select(a => new ShiftHeaderModel
                    {
                        ShiftCode = a.ShiftCode,
                        StaffCode = a.StaffCode,
                        StoreNo = a.StoreNo,
                        PosTerminal = a.PosTerminal,
                        BussinessDate = a.BussinessDate,
                        ShiftNumber = a.ShiftNumber,
                        BeginAmount = a.BeginAmount,
                        CloseAmount = a.CloseAmount,
                        OutAmount = a.OutAmount,
                        QuantityBox = a.QuantityBox,
                        QuantityCoupon = a.QuantityCoupon,
                        QuantityVoucher = a.QuantityVoucher,
                        IsShiftClosed = a.IsShiftClosed,
                        IsShiftOpened = a.IsShiftOpened,
                        Source = a.Source,
                        CreatedUser = a.CreatedUser
                    });

                    total = dataEnd.Count();
                    var result = dataEnd.OrderBy(d => d.StoreNo).Skip(Skip).Take(Take).ToList();
                    return new Tuple<List<ShiftHeaderModel>, int>(result, total);

                }
            }
            catch (Exception ex)
            {
                return new Tuple<List<ShiftHeaderModel>, int>(new List<ShiftHeaderModel>(), 0);
            }
        }
        public List<SaleBusinessStoreModel> SaleBusinessStoreStaging(string storeNo, DateTime businessDate)
        {
            var result = new List<SaleBusinessStoreModel>();
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo); 
            using (var db = new CentralSalesContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<SaleBusinessStoreModel>("[dbo].[SP_END_DATE_CONFIRM_STAGING] @StoreNo, @BusinessDate",
                            new SqlParameter("@StoreNo", storeNo),
                            new SqlParameter("@BusinessDate", businessDate)).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<CheckSaleStaffCodeVMModel> ListShiftHeaderByStore(string storeNo, DateTime bussinessDate)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var result = new List<CheckSaleStaffCodeVMModel>();
                    var listStaffOrder = db.TransHeaders.Where(d => d.StoreNo == storeNo && DbFunctions.TruncateTime(d.OrderDate) == DbFunctions.TruncateTime(bussinessDate))
                        .GroupBy(a => new { a.StoreNo, a.CashierID })
                        .Select(b => new CheckSaleStaffCodeVMModel { StaffCode = b.Key.CashierID, StoreNo = b.Key.StoreNo, TotalOrder = b.Count() }).ToList();

                    var listStaffFinishShift = db.ShiftOrders.Where(d => d.StoreNo == storeNo && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(bussinessDate))
                        .GroupBy(a => new { a.StoreNo, a.StaffCode })
                        .Select(b => new CheckSaleStaffCodeVMModel { StaffCode = b.Key.StaffCode, StoreNo = b.Key.StoreNo, TotalOrder = b.Count() }).ToList();

                    var listResult = listStaffOrder.Where(a => !listStaffFinishShift.Any(d => d.StaffCode == a.StaffCode && d.StoreNo == a.StoreNo && d.TotalOrder == a.TotalOrder)).ToList();
                    return listResult;
                }
            }
            catch (Exception ex)
            {
                return (default(List<CheckSaleStaffCodeVMModel>));
            }
        }

        // May POS chua dong ca tai POS
        public ViewPopupNotEnd ListTerminalNotEndShiftHeader(string storeNo, DateTime businessDate)
        {
            var result = new ViewPopupNotEnd();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var endShift_Ca1 = db.ShiftHeaders.Where(x => x.StoreNo == storeNo && x.BussinessDate == businessDate && x.ShiftNumber == "1").ToList();
                    var endShift_Ca2 = db.ShiftHeaders.Where(x => x.StoreNo == storeNo && x.BussinessDate == businessDate && x.ShiftNumber == "2").ToList();

                    if (endShift_Ca1.FirstOrDefault().IsShiftClosed == false || endShift_Ca2.FirstOrDefault().IsShiftClosed == false)
                    {
                        // Chưa đóng ca máy POS sẽ không được xác nhận kết thúc ngày trên Web
                        result = new ViewPopupNotEnd
                        {
                            Status = 2,
                            Message = $"ST/CH {storeNo} này có máy POS chưa đóng ca. Vui lòng kiểm tra lại"
                        };
                        return result;
                    }
                    else
                    {
                        // ST/CH đã đóng ca
                        result = new ViewPopupNotEnd
                        {
                            Status = 1,
                            Message = $"ST/CH {storeNo} này đã đóng ca"
                        };
                        return result;
                   }                    
                }              
            }
            catch (Exception ex)
            {
                return result;
            }
        }
        public List<ViewPopupNotEnd> ListTerminalNotEndShift(string storeNo, DateTime businessDate)
        {
            var result = new ViewPopupNotEnd();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.BussinessDates
                                where a.StoreNo == storeNo && a.BussinessDate1 == businessDate && a.IsClosed == false
                                select new ViewPopupNotEnd
                                {
                                    StoreNo = a.StoreNo,
                                    PosTerminal = a.PosTerminal,
                                    StaffCode = a.CreatedUser,
                                    IsFinished = a.IsClosed,
                                    BussinessDate = a.BussinessDate1
                                }).OrderBy(d => d.PosTerminal).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ViewPopupNotEnd>));
            }
        }
        public List<EODCreatedInvoiceModel> ListNotCreatedInvoice(string StoreNo)
        {
            /*
                 - Server Test : Phuc Long
                 - EInvoiceContainer : 10.235.9.102 ; database = [EInvoice] ;  table = [dbo].[EODCreatedInvoice]
                 - var ipServer = ServerIPConnection.GetIPServerByStore(StoreNo);
                 - var db = new EInvoiceContainer(ipServer)

                 - Server PRD : Phuc Long
                 - EInvoicePLHContainer : 10.235.25.101 ; database = [EInvoice] ; table = [dbo].[EODCreatedInvoice]
                 - var db = new EInvoicePLHContainer()
                 - ActivitiesData/EF/EInvoices/EInvoicePLH.edmx

                var ipServer = ServerIPConnection.GetIPServerByStore(StoreNo);
                var db = new EInvoiceContainer(ipServer)
                var db = new EInvoicePLHContainer()
           */

            try
            {
                using (var db = new EInvoicePLHContainer()) 
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.EODCreatedInvoices
                                where a.StoreNo == StoreNo && a.IsCompleted == false
                                select new EODCreatedInvoiceModel
                                {
                                    BusinessDate = a.BusinessDate,
                                    StoreNo = a.StoreNo
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex) 
            {
                return new List<EODCreatedInvoiceModel>();
            }
        }
        public List<TransHeaderPOSModel> ListOrderPOS(string connectStringPOS, string storeNo, string posTerminal, DateTime businessDate)
        {
            var result = new List<TransHeaderPOSModel>();
            var dt = new DataTable();
            var date = businessDate.Date.ToString("MM/dd/yyyy");
            using (SqlConnection connection = new SqlConnection(connectStringPOS))
            {
                string queryDB = string.Format("SELECT a.StoreNo,a.POSTerminalNo as POSTerminal, a.OrderNo FROM TransHeader(NOLOCK) a WHERE convert(date, a.OrderDate)='{0}' and a.StoreNo='{1}' and a.POSTerminalNo='{2}' ", date, storeNo, posTerminal);
                SqlCommand command = new SqlCommand(queryDB, connection);
                try
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    try
                    {
                        dt.Load(reader);
                        var convertObj = JsonConvert.SerializeObject(dt);
                        result = JsonConvert.DeserializeObject<List<TransHeaderPOSModel>>(convertObj);
                    }
                    finally
                    {
                        reader.Close();
                    }
                    connection.Close();
                }
                catch (Exception ex)
                {
                }
            }
            return result;
        }
        public Tuple<bool, int, int> CheckTotalSale(string storeNo, DateTime businessDate)
        {
            var result = new Tuple<bool, int, int>(true, 0, 0);         
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var listPOS = new List<string>();
                    using (var db1 = new CentralMDPartnerContainer())
                    {
                        var listData = db1.POSTerminals.Where(a =>a.StoreNo == storeNo && a.Placement !="POSWEB").ToList();
                        listPOS = listData.Select(a =>a.No).ToList();
                    }

                    // Lay data ban hang cua các POS khong phai la POSWEB
                    var totalSale = db.POSEODs.Where(d => d.StoreNo == storeNo && listPOS.Contains(d.POSTerminalNo) && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(businessDate)).Sum(d => d.TotalSale);
                    
                    var listDataTransHeader = db.TransHeaders.Where(d => d.StoreNo == storeNo && d.DocumentType !="POSWEB" && DbFunctions.TruncateTime(d.OrderDate) == DbFunctions.TruncateTime(businessDate)).ToList(); // Khong lay cac don hang ban kenh POSWEB
                    var data = (from a in listDataTransHeader
                                where int.Parse(a.POSTerminalNo.Substring(a.POSTerminalNo.Length - 2, 2)) < 70  // 25/11/2022, A.Nguyên yêu cầu: Không lấy đơn hàng các POSTerminal có số thứ tự POS > = 70 trở lên
                                select new TransHeaderCentralModel
                                {
                                    StoreNo = a.StoreNo,
                                    PosTerminal = a.POSTerminalNo,
                                    OrderNo = a.OrderNo
                                }).ToList();

                    var countTransheader = data.Select(x =>x.OrderNo).Count();

                    // Vì giá trị countTransheader = 0 trong truong hop khong co du lieu ban hang cua cac may POS (khong phai la POSWeb), 
                    // nen totalSale luc nay = null. Do do gán TotalSale = 0 de so sanh voi countTransheader
                    if (totalSale == null)
                    {
                        totalSale = 0;
                    }

                    if (totalSale != countTransheader)
                    {
                        result = new Tuple<bool, int, int>(false, totalSale ?? 0, countTransheader);
                    }
                    else
                    {
                        result = new Tuple<bool, int, int>(true, totalSale ?? 0, countTransheader);
                    }                        
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, int, int>(false, 0, 0);
            }

            return result;

        }
        public List<PosTerminalModel> GetTerminalStore(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.POSTerminals
                                where a.StoreNo == storeNo
                                select new PosTerminalModel
                                {
                                    StoreNo = a.StoreNo,
                                    PosTerminalID = a.No,
                                    IPAddress = a.IPAddress,
                                    Placement = a.Placement
                                }).OrderBy(d => d.PosTerminalID).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<PosTerminalModel>));
            }
        }
        public List<PosTerminalMissOrderModel> ListPosTerminalMissOrderNo(string storeNo, DateTime businessDate)
        {
            var result = new List<PosTerminalMissOrderModel>();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var listTotalOrderEOD = db.POSEODs.Where(d => d.StoreNo == storeNo && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(businessDate))
                        .Select(a => new { StoreNo = a.StoreNo, PosTerminal = a.POSTerminalNo, TotalSale = a.TotalSale ?? 0 }).ToList();

                    // Lay cac don hàng ban tren máy POS (không phải là POSWeb)

                    var listOrderHeader = db.TransHeaders.Where(d => d.StoreNo == storeNo && d.DocumentType !="POSWEB" && DbFunctions.TruncateTime(d.OrderDate) == DbFunctions.TruncateTime(businessDate)).ToList();
                    var listCountOrder = listOrderHeader.GroupBy(d => new { d.StoreNo, d.POSTerminalNo })
                        .Select(a => new { StoreNo = a.Key.StoreNo, PosTerminal = a.Key.POSTerminalNo, TotalSale = a.Count() }).ToList();

                    var posMiss = listCountOrder.Where(a => !listTotalOrderEOD.Any(b => b.StoreNo == a.StoreNo && b.PosTerminal == a.PosTerminal && b.TotalSale == a.TotalSale)).ToList();
                    var listTerminal = GetTerminalStore(storeNo);
                    var listPOSTerminal = listTerminal.Where(b =>b.Placement !="POSWEB").ToList(); // Khong lay cac POSWEB

                    if (posMiss.Count > 0)
                    {
                        foreach (var item in posMiss)
                        {
                            var listOrder = listOrderHeader.Where(d => d.POSTerminalNo == item.PosTerminal && d.StoreNo == item.StoreNo)
                                .Select(a => new TransHeaderPOSModel
                                {
                                    StoreNo = a.StoreNo,
                                    PosTerminal = a.POSTerminalNo,
                                    OrderNo = a.OrderNo,
                                }).ToList();

                            result.Add(new PosTerminalMissOrderModel
                            {
                                StoreNo = item.StoreNo,
                                PosTerminalID = item.PosTerminal,
                                TotalSale = item.TotalSale,
                                IPAddress = listPOSTerminal.FirstOrDefault(d => d.PosTerminalID == item.PosTerminal).IPAddress,
                                //IPAddress = listTerminal.FirstOrDefault(d => d.PosTerminalID == item.PosTerminal).IPAddress,
                                ListOrderCentral = listOrder
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public bool InsertUpdatePOSEOD(List<POSEODModel> model)
        {
            var result = true;
            try
            {
                if (model != null && model.Count > 0)
                {
                    var ipServer = ServerIPConnection.GetIPServerByStore(model.FirstOrDefault().StoreNo);
                    using (var db = new CentralSalesContainer(ipServer))
                    {
                        foreach (var item in model)
                        {
                            var checkUpd = db.POSEODs.FirstOrDefault(d => d.StoreNo == item.StoreNo && d.POSTerminalNo == item.POSTerminalNo && d.BussinessDate == item.BussinessDate);
                            if (checkUpd != null)
                            {
                                checkUpd.TotalSale = item.TotalSale ?? 0;
                                checkUpd.CreatedDate = item.CreatedDate;
                            }
                            else
                            {
                                var insert = new POSEOD
                                {
                                    StoreNo = item.StoreNo,
                                    POSTerminalNo = item.POSTerminalNo,
                                    BussinessDate = item.BussinessDate,
                                    TotalSale = item.TotalSale,
                                    TotalAmount = item.TotalAmount,
                                    MaxBillNumber = item.MaxBillNumber,
                                    CreatedDate = item.CreatedDate
                                };
                                db.POSEODs.Add(insert);
                            }
                        }
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }
        public List<BusinessDateModel> ListCheckBusinessDate(string storeNo, DateTime businessDate)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var fromDate = businessDate.AddMonths(-1);
                    db.Database.CommandTimeout = 2 * 60;
                    var t1 = db.BussinessDates.Where(a => a.StoreNo == storeNo
                                        && DbFunctions.TruncateTime(a.BussinessDate1) < businessDate.Date
                                        && DbFunctions.TruncateTime(a.BussinessDate1) >= fromDate.Date).ToList()
                                        .Select(a => new
                                        {
                                            BussinessDate = a.BussinessDate1.Value.Date
                                        }).Distinct();

                    var t2 = db.BussinessDateConfirms.Where(a => a.StoreNo == storeNo
                                  && DbFunctions.TruncateTime(a.BussinessDate) < businessDate.Date
                                  && DbFunctions.TruncateTime(a.BussinessDate) >= fromDate.Date).ToList()
                                  .Select(a => new
                                  {
                                      BussinessDate = a.BussinessDate.Value.Date
                                  }).Distinct();

                    var result = t1.Except(t2).Select(a => new BusinessDateModel { BussinessDate = a.BussinessDate }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<BusinessDateModel>));
            }
        }
        public List<SaleBusinessStoreModel> SaleBusinessStore(string storeNo, DateTime businessDate)
        {
            var result = new List<SaleBusinessStoreModel>();
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new CentralSalesContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<SaleBusinessStoreModel>("[dbo].[SP_END_DATE_CONFIRM] @StoreNo, @BusinessDate ",
                    new SqlParameter("@StoreNo", storeNo),
                    new SqlParameter("@BusinessDate", businessDate)).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<ViewPopupNotEnd> CheckFinishSale(string storeNo, DateTime businessDate)
        {
            var result = new List<ViewPopupNotEnd>();
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new CentralSalesContainer(ipServer))
            {
                try
                {
                    var connectDB = db.Database.Connection.ConnectionString;
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.FinishSales.Where(d => d.StoreNo == storeNo
                             && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(businessDate))
                              .Select(a => new ViewPopupNotEnd
                              {
                                  StoreNo = a.StoreNo,
                                  StaffCode = a.StaffCode,
                                  PosTerminal = a.PosTerminal,
                                  IsFinished = a.IsFinished,
                                  BussinessDate = a.BussinessDate
                              }).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public void ConfirmRetryBusinessDate(List<BussinessDateConfirmModel> listmodel, int RetryConfirm)
        {
            /*
                     -- Server Test : 10.233.9.102
                     -- EInvoiceContainer (10.233.9.102) ; Database = EInvoices ; Table = EODCreatedInvoice
                     -- var dbInvoice = new EInvoiceContainer(ipServer)

                     -- Server PRD : 10.235.25.101
                     -- EInvoicePLHContainer (10.235.25.101) ; Database = EInvoice ; Table = EODCreatedInvoice
                     -- var dbInvoice = new EInvoicePLHContainer()
                     -- ActivitiesData/EF/Einvoices/EinvoicePLH.edmx
            */

            var result = new MessageCheckEndShiftResponse();
            try
            {
                var storeNo = listmodel.FirstOrDefault().StoreNo ?? "";
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var dbInvoice = new EInvoicePLHContainer())    // Update IsCompleted vào table EODCreatedInvoice để CHECK job EOD
                    {
                        dbInvoice.Database.CommandTimeout = 2 * 60;
                        if (RetryConfirm == 1)
                        {
                            var listInsert = new List<BussinessDateConfirmModel>();
                            var listInsertEOD = new List<BussinessDateConfirmModel>();

                            foreach (var itemRetry in listmodel)
                            {
                                var checkExitsRetry = db.BussinessDateConfirms.FirstOrDefault(d => d.Code == itemRetry.Code);

                                if (checkExitsRetry != null)
                                {
                                    checkExitsRetry.TotalAmount = itemRetry.TotalAmount;
                                    checkExitsRetry.UpdatedDate = itemRetry.CreatedDate;
                                    checkExitsRetry.UpdatedUser = itemRetry.CreatedUser;
                                    db.SaveChanges();

                                    var updEOD = dbInvoice.EODCreatedInvoices.FirstOrDefault(d => d.StoreNo == itemRetry.StoreNo && DbFunctions.TruncateTime(d.BusinessDate) == DbFunctions.TruncateTime(itemRetry.BussinessDate.Value) && d.IsCompleted == true);
                                    if (updEOD != null)
                                    {
                                        updEOD.IsCompleted = false;
                                        dbInvoice.SaveChanges();
                                    }
                                    else
                                    {
                                        listInsertEOD.Add(itemRetry);
                                    }
                                }
                                else
                                {
                                    listInsert.Add(itemRetry);
                                    listInsertEOD.Add(itemRetry);
                                }
                            }

                            if (listInsert.Count > 0)
                            {
                                db.BussinessDateConfirms.AddRange(listInsert.Select(a => new BussinessDateConfirm
                                {
                                    Code = a.Code,
                                    StoreNo = a.StoreNo,
                                    BussinessDate = a.BussinessDate,
                                    TotalAmount = a.TotalAmount,
                                    IsConfirm = a.IsConfirm,
                                    ConfirmDate = a.ConfirmDate,
                                    FileNameDetail = a.FileNameDetail,
                                    CreatedUser = a.CreatedUser,
                                    CreatedDate = a.CreatedDate
                                }));

                                db.SaveChanges();
                            }

                            if (listInsertEOD.Count > 0)    // insert vào table EODCreatedInvoice để CHECK job EOD
                            {
                                dbInvoice.EODCreatedInvoices.AddRange(listInsertEOD.Select(a => new EODCreatedInvoice
                                {
                                    BusinessDateCode = a.Code,
                                    BusinessDate = a.BussinessDate,
                                    StoreNo = a.StoreNo,
                                    IsCompleted = false,
                                    CreateDate = DateTime.Now
                                }));

                                dbInvoice.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        public MessageCheckEndShiftResponse ConfirmBusinessDate(List<BussinessDateConfirmModel> listmodel)
        {
            /*
                    -- Server Test : 10.233.9.102
                    -- EInvoiceContainer (10.233.9.102) ; Database = EInvoices ; Table = EODCreatedInvoice
                    -- var dbInvoice = new EInvoiceContainer(ipServer)

                    -- Server PRD : 10.235.25.101
                    -- EInvoicePLHContainer (10.235.25.101) ; Database = EInvoice ; Table = EODCreatedInvoice
                    -- var dbInvoice = new EInvoicePLHContainer()
                    -- StoreActivitiesData/EF/EInvoices/EInvoicePLH.edmx
            */

            var result = new MessageCheckEndShiftResponse();
            try
            {
                var storeNo = listmodel.FirstOrDefault().StoreNo ?? "";
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (var dbInvoice = new EInvoicePLHContainer())  // update IsCompleted vào table EODCreatedInvoice để CHECK job EOD
                    {
                        dbInvoice.Database.CommandTimeout = 2 * 60;

                        var model = listmodel.FirstOrDefault();
                        var checkExits = db.BussinessDateConfirms.FirstOrDefault(d => d.Code == model.Code);

                        if (checkExits != null)
                        {
                            checkExits.TotalAmount = model.TotalAmount;
                            checkExits.UpdatedDate = model.CreatedDate;
                            checkExits.UpdatedUser = model.CreatedUser;
                            checkExits.IsConfirm = true;
                            db.SaveChanges();

                            var openBusiness = db.BussinessDateOpens.FirstOrDefault(d => d.StoreNo == model.StoreNo && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(model.BussinessDate));
                            if (openBusiness != null)  // cập nhật ngày kinh doanh lên 1
                            {
                                openBusiness.BussinessDate = model.BussinessDate.Value.AddDays(1);
                                db.SaveChanges();
                            }

                            var updEOD = dbInvoice.EODCreatedInvoices.FirstOrDefault(d => d.StoreNo == model.StoreNo && DbFunctions.TruncateTime(d.BusinessDate) == DbFunctions.TruncateTime(model.BussinessDate.Value) && d.IsCompleted == true);
                            if (updEOD != null)
                            {
                                updEOD.IsCompleted = false;
                                updEOD.UpdatedDate = DateTime.Now;
                                dbInvoice.SaveChanges();
                            }

                            result = new MessageCheckEndShiftResponse {
                                Status = 1,
                                Message = $"Xác nhận lại kết thúc ngày {model.BussinessDate.Value.ToString("dd/MM/yyyy")} thành công"
                            };

                            return result;
                        }

                        var item = new BussinessDateConfirm
                        {
                            Code = model.Code,
                            StoreNo = model.StoreNo,
                            BussinessDate = model.BussinessDate,
                            TotalAmount = model.TotalAmount,
                            IsConfirm = model.IsConfirm,
                            ConfirmDate = model.ConfirmDate,
                            FileNameDetail = model.FileNameDetail,
                            CreatedUser = model.CreatedUser,
                            CreatedDate = model.CreatedDate
                        };

                        db.BussinessDateConfirms.Add(item);
                        db.SaveChanges();

                        var checkEOD = dbInvoice.EODCreatedInvoices.FirstOrDefault(d => d.StoreNo == model.StoreNo && DbFunctions.TruncateTime(d.BusinessDate) == DbFunctions.TruncateTime(model.BussinessDate.Value) && d.IsCompleted == true);
                        if (checkEOD != null)
                        {
                            checkEOD.IsCompleted = false;
                            checkEOD.UpdatedDate = DateTime.Now;
                            dbInvoice.SaveChanges();
                        }
                        else
                        {
                            var insertEOD = new EODCreatedInvoice
                            {
                                BusinessDateCode = model.Code,
                                BusinessDate = model.BussinessDate,
                                StoreNo = model.StoreNo,
                                IsCompleted = false,
                                CreateDate = DateTime.Now
                            };

                            dbInvoice.EODCreatedInvoices.Add(insertEOD);
                            dbInvoice.SaveChanges();
                        }

                        var insertDateOpen = db.BussinessDateOpens.FirstOrDefault(d => d.StoreNo == item.StoreNo);
                        if (insertDateOpen != null)    // cập nhật ngày kinh doanh lên 1
                        {
                            insertDateOpen.BussinessDate = model.BussinessDate.Value.AddDays(1);
                            db.SaveChanges();
                        }
                        
                        result = new MessageCheckEndShiftResponse {
                            Status = 1,
                            Message = "Xác nhận kết thúc ngày thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                result = new MessageCheckEndShiftResponse {
                    Status = 0,
                    Message = ex.Message
                };
            }
            return result;
        }
        public bool UpdateTotalSaleToPOSEOD(string storeNo, DateTime businessDate)
        {
            var result = true;
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var listTotalOrderEOD = db.POSEODs.Where(d => d.StoreNo == storeNo && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(businessDate));
                    var listOrderHeader = db.TransHeaders.Where(d => d.StoreNo == storeNo && DbFunctions.TruncateTime(d.OrderDate) == DbFunctions.TruncateTime(businessDate)).ToList();
                    var listCountOrderCentral = listOrderHeader.GroupBy(d => new { d.StoreNo, d.POSTerminalNo })
                       .Select(a => new { SiteNo = a.Key.StoreNo, PosTerminal = a.Key.POSTerminalNo, TotalSale = a.Count() }).ToList();

                    if (listCountOrderCentral != null && listCountOrderCentral.Count > 0)
                    {
                        var dateNow = DateTime.Now;
                        foreach (var item in listCountOrderCentral)
                        {
                            var checkEOD = listTotalOrderEOD.FirstOrDefault(d => d.StoreNo == item.SiteNo
                                            && d.POSTerminalNo == item.PosTerminal
                                            && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(businessDate));

                            if (checkEOD != null)
                            {
                                checkEOD.TotalSale = item.TotalSale;
                            }
                            else
                            {
                                db.POSEODs.Add(new POSEOD
                                {
                                    StoreNo = storeNo,
                                    POSTerminalNo = item.PosTerminal,
                                    TotalSale = item.TotalSale,
                                    BussinessDate = businessDate,
                                    CreatedDate = dateNow
                                });
                            }
                        }
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        // Lịch sử kết thúc ca tại POS
        public List<HistoryEndShiftModel> ListHistoryEndShift(string staffCode, string storeNo, DateTime bussinessDate, string status)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var result = new List<HistoryEndShiftModel>();
                    db.Database.CommandTimeout = 2 * 60;

                    var data = db.ShiftHeaders.Where(d => d.BussinessDate == DbFunctions.TruncateTime(bussinessDate));
                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.StoreNo == storeNo); 
                    }

                    var listOrder = (from a in db.TransHeaders.AsNoTracking()
                                     join b in db.TransPaymentEntries.AsNoTracking() on a.OrderNo equals b.OrderNo
                                     where DbFunctions.TruncateTime(a.OrderDate) == DbFunctions.TruncateTime(bussinessDate)
                                     && a.StoreNo == storeNo && b.TenderType == "CA00"   // Tien mat
                                     select new {
                                         a.POSTerminalNo,
                                         b.ShiftNo,
                                         a.SalesIsReturn,
                                         b.AmountTendered
                                     }).ToList();

                    if (!string.IsNullOrEmpty(staffCode))
                    {
                        data = data.Where(d => d.PosTerminal == staffCode);
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        var isShiftClosed = status == "0" ? false : true;
                        data = data.Where(d => d.IsShiftClosed == isShiftClosed);
                    }

                    var totalCA00 = listOrder.GroupBy(g => new { g.POSTerminalNo, g.ShiftNo }).Select(x => new { PosTerminal = x.Key.POSTerminalNo, ShiftNumber = x.Key.ShiftNo, TotalAmount = x.Sum(a => a.SalesIsReturn == 0 ? a.AmountTendered : (-1) * a.AmountTendered) }).ToList();

                    result = data.ToList().Select(a => new HistoryEndShiftModel
                    {
                        ShiftCode = a.ShiftCode,
                        StoreNo = a.StoreNo,
                        PosTerminal = a.PosTerminal,
                        Placement = (a.Source == "WEB") ? "POSWEB" : string.Empty,  // Loai POS : POS/WEB
                        Source = a.Source,                                          // Loai POS : POS/WEB
                        ShiftNumber = a.ShiftNumber,
                        IsShiftClosed = a.IsShiftClosed,
                        CreatedUser = a.UpdatedUser ?? "",
                        CloseShiftDate = a.CloseShiftDate ?? null,
                        StaffCode = a.StaffCode,
                        BussinessDate = a.BussinessDate ?? null,
                        CloseAmount = db.ShiftLines.Where(d => d.ShiftCode == a.ShiftCode).Sum(d => d.CashAmount) ?? 0,
                        OutAmount = a.OutAmount ?? 0,
                        QuantityBox = a.QuantityBox ?? 0,
                        QuantityCoupon = a.QuantityCoupon ?? 0,
                        QuantityVoucher = a.QuantityVoucher ?? 0,
                        AmountCA00 = totalCA00.Count > 0 ? totalCA00.FirstOrDefault(d => d.PosTerminal == a.PosTerminal && d.ShiftNumber == a.ShiftNumber)?.TotalAmount + (a.BeginAmount ?? 0) ?? 0 : 0
                    }).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<HistoryEndShiftModel>));
            }
        }
        public Tuple<int, string> DeleteShiftEnd(string shiftCode, string shiftNumber, string remark, string userName)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(shiftCode.Substring(0, 4));
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var stringConn = db.Database.Connection.ConnectionString;
                    db.Database.CommandTimeout = 2 * 60;

                    var result = db.Database.ExecuteSqlCommand("[dbo].[SP_SHIFT_END_DELETE] @ShifCode,@Remark,@CreatedUser ",
                        new SqlParameter("@ShifCode", shiftCode),
                        new SqlParameter("@Remark", remark),
                        new SqlParameter("@CreatedUser", userName));

                    if (result > 0)
                    {
                        return new Tuple<int, string>(1, $"Xóa thành công ca {shiftNumber}");
                    }
                    else
                    {
                        return new Tuple<int, string>(0, "Xóa thất bại");
                    }                       
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string>(0, $"Xóa ca {shiftNumber} error {ex.Message}");
            }
        }
        public List<StaffCodeModel> ListTypeUser(string storeNo)
        {
            try
            {   
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.POSTerminals
                                where a.StoreNo == storeNo   // && a.EmploymentType == 0
                                select new StaffCodeModel
                                {
                                    UserPOS = a.No,
                                    EmploymentType = 0
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<StaffCodeModel>));
            }
        }

        public ShiftHeaderDesciptionModel GetDescription(string storeNo, string shiftCode)
        {
            var result = new ShiftHeaderDesciptionModel();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.ShiftHeaderDescriptions.Where(d => d.ShiftCode == shiftCode)
                              .Select(a => new ShiftHeaderDesciptionModel
                              {
                                  ID = a.ID,
                                  ShiftCode = a.ShiftCode,
                                  StoreNo = a.StoreNo,
                                  BussinessDate = a.BussinessDate,
                                  Descriptions = a.Descriptions,
                                  CreatedDate = a.CreatedDate,
                                  CreatedUser = a.CreatedUser
                              }).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        // Update giai trinh ket ca
        public Tuple<bool, string> UpdateDescription(ShiftHeaderDesciptionModel model)
        {
            var result = new Tuple<bool, string>(true, "Success");
            try
            {
                var storeNo = model.StoreNo ?? "";
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkCode = db.ShiftHeaderDescriptions.FirstOrDefault(d => d.ShiftCode == model.ShiftCode);
                    if (checkCode != null)
                    {
                        checkCode.Descriptions = model.Descriptions;
                        checkCode.CreatedUser = model.CreatedUser;
                        checkCode.CreatedDate = model.CreatedDate;
                    }
                    else
                    {
                        db.ShiftHeaderDescriptions.Add(new ShiftHeaderDescription
                        {
                            ShiftCode = model.ShiftCode,
                            StoreNo = model.StoreNo,
                            BussinessDate = model.BussinessDate,
                            Descriptions = model.Descriptions,
                            CreatedDate = model.CreatedDate,
                            CreatedUser = model.CreatedUser
                        });
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }
        public Tuple<bool, string> DeleteDescription(ShiftHeaderDesciptionModel model)
        {
            var result = new Tuple<bool, string>(true, "Success");
            try
            {
                var storeNo = model.StoreNo ?? "";
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkCode = db.ShiftHeaderDescriptions.FirstOrDefault(d => d.ShiftCode == model.ShiftCode);
                    if (checkCode != null)
                    {
                        db.ShiftHeaderDescriptions.Remove(checkCode);
                        db.SaveChanges();
                    }
                    else
                    {
                        result = new Tuple<bool, string>(false, "Không lấy được nội dung để hủy giải trình");
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        // Lich su ket thuc ngay tai POS
        public List<HistoryConfirmEndDatePOSModel> HistoryConfirmDatePOS(string setDB, string listStore, string PosID, DateTime fromDate, string status, int pageSize, int pageNumber)
        {

            var result = new List<HistoryConfirmEndDatePOSModel>();
            var ipServer = string.IsNullOrEmpty(setDB) ? ServerIPConnection.GetIPServerReadByStore(listStore) : setDB;

            using (var db = new CentralSalesContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<HistoryConfirmEndDatePOSModel>("[dbo].[SP_END_DATE_CONFIRM_HISTORY] @ListStoreJson,@POSTerminal, @BusinessDate, @IsClosed, @PageSize, @PageNumber",
                        new SqlParameter("@ListStoreJson", listStore),
                        new SqlParameter("@POSTerminal", PosID),
                        new SqlParameter("@BusinessDate", fromDate),
                        new SqlParameter("@IsClosed", status),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                }
                catch (Exception ex)
                {
                    return result;
                }
                return result;
            }
        }
        public List<HistoryEndDateModel> ListHistoryConfirmEndDate(string setDB, string siteNo, DateTime businessDate, string isConfirm, int pageSize, int pageNumber)
        {
            var result = new List<HistoryEndDateModel>();
            try
            {
                var ipServer = string.IsNullOrEmpty(setDB) ? ServerIPConnection.GetIPServerReadByStore(siteNo) : setDB;
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 10 * 60;
                    result = db.Database.SqlQuery<HistoryEndDateModel>("[dbo].[SP_HISTORY_CONFIRM_END_DATE] @StoreNo, @BusinessDate, @ISConfirm, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", siteNo),
                        new SqlParameter("@BusinessDate", businessDate),
                        new SqlParameter("@ISConfirm", isConfirm),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        // Xem chi tiet tong hop ket thuc ngay (View PDF)
        public List<DataTable> ViewDetailEndDate(string storeNo, DateTime businessDate)
        {
            var result = new List<DataTable>();
            try
            {
                DataSet ds = new DataSet();
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    using (SqlConnection connection = new SqlConnection(db.Database.Connection.ConnectionString))
                    {
                        SqlCommand command = new SqlCommand("GET_GENERAL_END_DATE_PRINT_PDF", connection);
                        //SqlCommand command = new SqlCommand("SP_END_DATE_PRINT", connection);

                        using (var da = new SqlDataAdapter(command))
                        {
                            try
                            {
                                command.Parameters.AddWithValue("@StoreNo", storeNo);
                                command.Parameters.AddWithValue("@BusinessDate", businessDate);
                                //connection.Open();
                                command.CommandType = CommandType.StoredProcedure;
                                try
                                {
                                    da.Fill(ds);
                                    var lstDT = ds.Tables;
                                    for (int i = 0; i < lstDT.Count; i++)
                                    {
                                        result.Add(lstDT[i]);
                                    }
                                }
                                finally
                                {
                                }
                                connection.Close();
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public MessageCheckEndShiftResponse DateBusinessUpdate(BussinessDateOpenModel model)
        {
            var result = new MessageCheckEndShiftResponse();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(model.StoreNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    var date = DateTime.Now;
                    db.Database.CommandTimeout = 2 * 60;
                    var openBusiness = db.BussinessDateOpens.FirstOrDefault(d => d.StoreNo == model.StoreNo);
                    if (openBusiness != null)    // chỉ update ngày kinh doanh
                    {
                        var dateSite = openBusiness.BussinessDate.Value.Date;
                        openBusiness.BussinessDate = model.BussinessDate;
                        openBusiness.UpdatedDate = model.UpdatedDate;
                        openBusiness.UpdatedUser = model.UpdatedUser;
                        db.SaveChanges();
                        result = new MessageCheckEndShiftResponse {
                            Status = 1,
                            Message = "Thay đổi ngày kinh doanh thành công"
                        };
                    }
                    else // Thêm mới ngày kinh doanh
                    {
                        var insert = new BussinessDateOpen
                        {
                            Code = model.StoreNo,
                            StoreNo = model.StoreNo,
                            BussinessDate = model.BussinessDate,
                            CreatedUser = model.CreatedUser,
                            CreatedDate = model.CreatedDate
                        };
                        db.BussinessDateOpens.Add(insert);
                        db.SaveChanges();
                        result = new MessageCheckEndShiftResponse {
                            Status = 1,
                            Message = "Thay đổi ngày kinh doanh thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                result = new MessageCheckEndShiftResponse { Status = 0, Message = ex.Message };
            }
            return result;
        }
        public Tuple<bool, string> ConfirmBusinessDateCancel(BussinessDateConfirmModel model)
        {
            var result = new Tuple<bool, string>(true, "Success");
            try
            {
                var storeNo = model.StoreNo ?? "";
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    /*
                     -- Server Test : 10.233.9.102
                     -- EInvoiceContainer (10.233.9.102) ; Database = EInvoices ; Table = EODCreatedInvoice
                     -- var dbInvoice = new EInvoiceContainer(ipServer)

                     -- Server PRD : 10.235.25.101
                     -- EInvoicePLHContainer (10.235.25.101) ; Database = EInvoice ; Table = EODCreatedInvoice
                     -- var dbInvoice = new EInvoicePLHContainer()
                     */

                    using (var dbInvoice = new EInvoicePLHContainer())
                    {
                        dbInvoice.Database.CommandTimeout = 2 * 60;
                        var checkExits = db.BussinessDateConfirms.FirstOrDefault(d => d.Code == model.Code);
                        if (checkExits != null)
                        {
                            db.BussinessDateConfirms.Remove(checkExits);
                            db.SaveChanges();

                            var updEOD = dbInvoice.EODCreatedInvoices.FirstOrDefault(d => d.StoreNo == model.StoreNo && DbFunctions.TruncateTime(d.BusinessDate) == DbFunctions.TruncateTime(model.BussinessDate.Value) && d.IsCompleted == true);
                            if (updEOD != null)
                            {
                                updEOD.IsCompleted = false;
                                updEOD.UpdatedDate = DateTime.Now;
                                dbInvoice.SaveChanges();
                            }
                            result = new Tuple<bool, string>(true, $"Hủy xác nhận kết thúc ngày {model.BussinessDate.Value.ToString("dd/MM/yyyy")} thành công");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new Tuple<bool, string>(false, ex.Message);
            }
            return result;
        }

        public bool UpdateMisSaleLog(List<LogMisSaleModel> model)
        {
            var result = true;
            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (model != null && model.Count > 0)
                    {
                        var insertLog = new List<LogMisSale>(model.Select(a => new LogMisSale
                        {
                            StoreNo = a.StoreNo,
                            BussinessDate = a.BussinessDate,
                            TotalSalePOS = a.TotalSalePOS,
                            TotalSaleServer = a.TotalSaleServer,
                            CreatedDate = a.CreatedDate,
                            CreatedUser = a.CreatedUser
                        }));
                        db.LogMisSales.AddRange(insertLog);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        public List<TransHeader> ListTransPOS(DateTime businessDate, string storeNo, string staffCode)
        {
            var result = new List<TransHeader>();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var listShiftOrder = db.ShiftOrders.AsNoTracking().Where(d => d.StoreNo == storeNo && d.StaffCode == staffCode && DbFunctions.TruncateTime(d.BussinessDate) == DbFunctions.TruncateTime(businessDate)).Select(x => x.OrderNo).ToList();
                    result = db.TransHeaders.AsNoTracking().Where(d => !listShiftOrder.Contains(d.OrderNo) && d.StoreNo == storeNo && d.CashierID == staffCode && DbFunctions.TruncateTime(d.OrderDate) == DbFunctions.TruncateTime(businessDate)).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public MessageCheckEndShiftResponse FinishShift(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel)
        {
            var result = new MessageCheckEndShiftResponse();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(headerModel.StoreNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (lineModel.Count > 0)
                    {
                        var checkItemList = db.ShiftLines.Where(d => d.ShiftCode == headerModel.ShiftCode).ToList();
                        if (checkItemList.Count > 0)
                        {
                            db.ShiftLines.RemoveRange(checkItemList);
                            db.SaveChanges();
                        }

                        db.ShiftLines.AddRange(lineModel.Select(e => new ShiftLine
                        {
                            ShiftCode = e.ShiftCode,
                            LineNo = e.LineNo,
                            CashCode = e.CashCode,
                            CashValue = e.CashValue,
                            CashQuantity = e.CashQuantity,
                            CashAmount = e.CashAmount,
                            CreatedUser = e.CreatedUser,
                            CreatedDate = e.CreatedDate
                        }));
                        db.SaveChanges();
                    }

                    var listOrder = ListTransPOS((DateTime)headerModel.BussinessDate, headerModel.StoreNo, headerModel.StaffCode);
                    if (listOrder.Count > 0)
                    {
                        db.ShiftOrders.AddRange(listOrder.Select(e => new ShiftOrder
                        {
                            StoreNo = headerModel.StoreNo,
                            StaffCode = headerModel.StaffCode,
                            BussinessDate = (DateTime)headerModel.BussinessDate,
                            ShiftNumber = headerModel.ShiftNumber,
                            ShiftCode = headerModel.ShiftCode,
                            OrderNo = e.OrderNo,
                            CreatedUser = headerModel.CreatedUser,
                            CreatedDate = headerModel.CreatedDate
                        }));
                        db.SaveChanges();
                    }

                    var header = new ShiftHeader
                    {
                        ShiftCode = headerModel.ShiftCode,
                        StaffCode = headerModel.StaffCode,
                        StoreNo = headerModel.StoreNo,
                        PosTerminal = headerModel.PosTerminal,
                        BussinessDate = headerModel.BussinessDate,
                        ShiftNumber = headerModel.ShiftNumber,
                        CloseAmount = headerModel.CloseAmount,
                        BeginAmount = headerModel.BeginAmount,
                        QuantityBox = headerModel.QuantityBox,
                        QuantityCoupon = headerModel.QuantityCoupon,
                        QuantityVoucher = headerModel.QuantityVoucher,
                        IsShiftOpened = headerModel.IsShiftOpened,
                        IsShiftClosed = headerModel.IsShiftClosed,
                        OpenShiftDate = headerModel.OpenShiftDate,
                        CloseShiftDate = headerModel.CloseShiftDate,
                        CreatedDate = headerModel.CreatedDate,
                        CreatedUser = headerModel.CreatedUser,
                        Source = headerModel.Source
                    };

                    var checkShift = db.ShiftHeaders.AsNoTracking().FirstOrDefault(d => d.ShiftCode == headerModel.ShiftCode);
                    if (checkShift != null)
                    {
                        db.ShiftHeaders.Remove(checkShift);
                        db.SaveChanges();
                    }

                    db.ShiftHeaders.Add(header);
                    db.SaveChanges();
                    result = new MessageCheckEndShiftResponse {
                        Status = 1,
                        Message = $"Ca {headerModel.ShiftNumber} kiểm kê số tiền thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                result = new MessageCheckEndShiftResponse {
                    Status = 0,
                    Message = ex.Message
                };
            }
            return result;
        }

        public MessageCheckEndShiftResponse FinishShiftTH(ShiftHeaderModel headerModel, List<ShiftLineModel> lineModel)
        {
            var result = new MessageCheckEndShiftResponse();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(headerModel.StoreNo);
                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var header = new ShiftHeader
                    {
                        ShiftCode = headerModel.ShiftCode,
                        StaffCode = headerModel.StaffCode,
                        StoreNo = headerModel.StoreNo,
                        PosTerminal = headerModel.PosTerminal,
                        BussinessDate = headerModel.BussinessDate,
                        ShiftNumber = headerModel.ShiftNumber,
                        CloseAmount = headerModel.CloseAmount,
                        IsShiftOpened = headerModel.IsShiftOpened,
                        IsShiftClosed = headerModel.IsShiftClosed,
                        OpenShiftDate = headerModel.OpenShiftDate,
                        CloseShiftDate = headerModel.CloseShiftDate,
                        CreatedDate = headerModel.CreatedDate,
                        CreatedUser = headerModel.CreatedUser,
                        Source = headerModel.Source
                    };

                    var checkShift = db.ShiftHeaders.FirstOrDefault(d => d.ShiftCode == headerModel.ShiftCode);
                    if (checkShift != null)
                    {
                        db.ShiftHeaders.Remove(checkShift);
                        db.SaveChanges();
                    }
                    db.ShiftHeaders.Add(header);
                    int saveDb = db.SaveChanges();
                    if (saveDb >= 0)   //Save header success
                    {
                        if (lineModel.Count > 0)
                        {
                            var checkItemList = db.ShiftLines.Where(d => d.ShiftCode == headerModel.ShiftCode).ToList();
                            if (checkItemList.Count > 0)
                            {
                                db.ShiftLines.RemoveRange(checkItemList);
                                db.SaveChanges();
                            }
                            db.ShiftLines.AddRange(lineModel.Select(e => new ShiftLine
                            {
                                ShiftCode = e.ShiftCode,
                                LineNo = e.LineNo,
                                CashCode = e.CashCode,
                                CashValue = e.CashValue,
                                CashQuantity = e.CashQuantity,
                                CashAmount = e.CashAmount,
                                CreatedUser = e.CreatedUser,
                                CreatedDate = e.CreatedDate
                            }));

                            db.SaveChanges();
                            result = new MessageCheckEndShiftResponse {
                                Status = 1,
                                Message = "Kiểm kê số tiền thành công"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = new MessageCheckEndShiftResponse {
                    Status = 0,
                    Message = ex.Message
                };
            }
            return result;
        }

        public List<DataTable> ViewDetailEndShift(string storeNo, string staffCode, DateTime businessDate, string shifCode)
        {
            var result = new List<DataTable>();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
                DataSet ds = new DataSet();

                var ipServerRead = ipServer.Split('\\')[0];
                //using (var db = new CentralSalesContainer(ipServer))

                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;

                    using (SqlConnection connection = new SqlConnection(db.Database.Connection.ConnectionString))
                    {
                        SqlCommand command = new SqlCommand("SP_END_SHIFT_PRINT", connection);

                        using (var da = new SqlDataAdapter(command))
                        {
                            try
                            {
                                command.Parameters.AddWithValue("@StoreNo", storeNo);
                                command.Parameters.AddWithValue("@UserPOS", staffCode);
                                command.Parameters.AddWithValue("@BusinessDate", businessDate);
                                command.Parameters.AddWithValue("@ShifCode", shifCode);
                                command.CommandType = CommandType.StoredProcedure;

                                try
                                {
                                    da.Fill(ds);
                                    var lstDT = ds.Tables;

                                    for (int i = 0; i < lstDT.Count; i++)
                                    {
                                        result.Add(lstDT[i]);
                                    }
                                }
                                finally
                                {
                                }

                                connection.Close();

                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<DataTable> ViewDetailEndShiftTH(string storeNo, string staffCode, DateTime businessDate, string shifCode)
        {
            var result = new List<DataTable>();
            try
            {
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
                DataSet ds = new DataSet();

                var ipServerRead = ipServer.Split('\\')[0];
                //using (var db = new CentralSalesContainer(ipServer))

                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 5 * 60;

                    using (SqlConnection connection = new SqlConnection(db.Database.Connection.ConnectionString))
                    {
                        SqlCommand command = new SqlCommand("SP_END_SHIFT_TH_PRINT", connection);

                        using (var da = new SqlDataAdapter(command))
                        {
                            try
                            {
                                command.Parameters.AddWithValue("@StoreNo", storeNo);
                                command.Parameters.AddWithValue("@UserPOS", staffCode);
                                command.Parameters.AddWithValue("@BusinessDate", businessDate);
                                command.Parameters.AddWithValue("@ShifCode", shifCode); 
                                command.CommandType = CommandType.StoredProcedure;

                                try
                                {
                                    da.Fill(ds);
                                    var lstDT = ds.Tables;
                                    for (int i = 0; i < lstDT.Count; i++)
                                    {
                                        result.Add(lstDT[i]);
                                    }
                                }
                                finally
                                {
                                }

                                connection.Close();

                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        // Xem chi tiết lịch sử kết ca tại POS
        public List<DataTable> ViewDetailEndShiftPLG(string storeNo, string staffCode, DateTime businessDate, string shiftCode)
        {
            var result = new List<DataTable>();

            try
            {
                var ipServer = ServerIPConnection.GetIPServerReadByStore(storeNo);
                DataSet ds = new DataSet();

                var ipServerRead = ipServer.Split('\\')[0];
                //using (var db = new CentralSalesContainer(ipServer))

                using (var db = new ReadCentralSalesContainer(ipServerRead))
                {

                    db.Database.CommandTimeout = 5 * 60;

                    using (SqlConnection connection = new SqlConnection(db.Database.Connection.ConnectionString))
                    {
                        SqlCommand command = new SqlCommand("SP_END_SHIFT_PLG_PRINT", connection);
                        using (var da = new SqlDataAdapter(command))
                        {
                            try
                            {
                                command.Parameters.AddWithValue("@StoreNo", storeNo);
                                command.Parameters.AddWithValue("@PosTerminal", staffCode);
                                command.Parameters.AddWithValue("@BusinessDate", businessDate);
                                command.Parameters.AddWithValue("@ShifCode", shiftCode);
                                command.CommandType = CommandType.StoredProcedure;

                                try
                                {
                                    da.Fill(ds);
                                    var lstDT = ds.Tables;
                                    for (int i = 0; i < lstDT.Count; i++)
                                    {
                                        result.Add(lstDT[i]);
                                    }
                                }
                                finally
                                {
                                }

                                connection.Close();

                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
















    }
}
