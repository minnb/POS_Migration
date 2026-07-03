using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using VCM.BLUEPOS.Common.Helpers;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF.IFSAP;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Common.DocumentTypeModel;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Model.StoreActivities;
using VCM.BLUEPOS.Model.Home;
using System.Data.Entity;
using System.Diagnostics;


namespace VCM.BLUEPOS.Data.Common
{
    public interface ICommonData
    {
        List<RoleModel> GetRolePermissionByUser(string userName);
        string GetIPSetServerByStore(string storeNo);
        List<DocumentTypeModel> GetComboxDocumentType();
        List<OptionModel> GetOrderType();
        List<OptionModel> GetComboxArticleType();
        List<OptionModel> GetComboxUnitOfMeasure();
        List<OptionModel> GetComboxVoucherCouponList();
        List<StoreModel> GetComboxStoreByUser(string userName);
        Tuple<bool, CheckEmployeeModel> CheckStaffCode(string staffCode);
        List<OptionModel> GetPermisionPOSTerminal();
        List<OptionModel> GetComboxProvince();
        List<OptionModel> GetComboxStoreList();
        List<OptionModel> LoadComboxBankList();
        List<OptionModel> LoadComboxPOSTerminalByStore(string storeNo);
        List<SQLServerModel> GetInforStoreSet(string storeNo);
        List<SQLServerModel> ListSQLServer();
        List<IPAddressPOSTerminalByStoreModel> LoadIPAddressPOSTerminalByStore(string storeNo);
        List<IPAddressPOSTerminalByStoreModel> LoadPOSTerminalByStore(string storeNo);
        List<ArraySiteNoModel> LoadComboxStoreByUserName(string userLogin);
        List<ArraySiteNoModel> LoadStoreByUserNameSetDB(string userLogin, string ipServer, string ipWriteLog);
        List<ArraySiteNoModel> LoadStoreBySetServerChanelNo(string userLogin, string ipServer, string chanelNo);
        string GetIPAddessByPOSTerminal(string posID);
        Tuple<bool, ResultPingIPLocalModel> CheckPingIPLocal(string IPAddress);
        List<OptionModel> GetPaymentType();
        List<StaffCodeResponseModel> LoadComboxStaffCode(string storeNo);
        Task<VCM.BLUEPOS.Model.Employee.StaffModel> InfoStaffByAD(string manv);
        List<VCM.BLUEPOS.Model.Employee.StaffModel> InfoStaffByStore(string storeNo);
        VCM.BLUEPOS.Model.Common.StyleProfileModel GetStyleProfile(string storeNo);
        string InsertBulk(DataTable table, string TableName, string storeNo, string orderNo = "", string posID = "");
        void InsertTrans_Invoice(string storeNo, string OrderNo);
        void InsertLogErrorWeb(LogErrorWebModel model);
        List<MCHComboboxModel> LoadComboxNganhHang();
        List<MCHComboboxModel> LoadComboxMCH5(string MCH2);
        List<VoucherReceiptTypeModel> GetVoucherReceiptType();
        List<SyncTableListModel> ListTableSync();
        List<SyncJobLogModel> ListLogSyncJob(string processID);
        List<SAPConfigXMLModel> ListConfigXMLSAP();
        List<ProvinceModel> GetComboxProvinceList();
        List<ArraySiteNoModel> LoadComboxStoreNo();
        Tuple<bool, CheckStoreModel> CheckStoreNo(string storeNo);
        List<SQLServerModel> ListSQLServerExcute();
        List<IPAddressModel> GetListIP(string storeNo, string IpAddress, string computerName, out int totalRecord, int skip, int take);
        List<DataTable> getDataByStore(string storeNo, string groupTable, string typeCounter, long posLastCounter);
        void DeleteAll(string ConnectStringStore, List<DataTable> lstTable);
        void DeleteByPkey(string ConnectStringStore, List<DataTable> lstTable);
        int InsertDataBySQL(DataTable dtData, string ConnectStringStore, string storeNo, string ipPOS);
        List<ArraySiteNoModel> LoadComboxStoreByChanel(string chanelNo);
        List<ArraySiteNoModel> LoadComboxSiteByChanelAndStore(string chanelNo, string storeNo);
        List<OptionModel> LoadComboxPOSVersionSource();
        List<OptionModel> LoadComboxPOSVersionList();
        List<OptionModel> LoadComboxPOSVersionIsUpdate();
        List<StaffCodeModel> ListStaffCode(string storeNo);
        SalesType LoadComboxSalesType();
        List<OptionModel> GetVoucherTenderType();
        SalesType LoadSalesOrderType();
        List<OptionModel> LoadTenderTypeForSalesType();
        List<OptionModel> LoadPaymentTenderTypeForSalesType();
        List<POSDataSetupModel> ListPOSSetup();
        IPAddressModel GetIPAddressByPOSTerminal(string posTerminal);
        List<OptionModel> LoadComboxEWalletList();
        List<ServerWebModel> ListServerWeb(string userAD, string ipWriteLog);
        List<OptionModel> LoadComboxSalesOrderType();
        ViewCheckListDailyModel CheckList();
        Tuple<bool, string> CheckListUser(CheckListUserRequest model);
        List<OptionModel> LoadComboxCategoryList();
        string LoadTenderTypeByWallet(string EwalletCode);
        List<OptionModel> GetComboxStoreListByWinLife();
        Tuple<bool, string, List<ReportCheckListResponse>> ReportCheckList(DateTime fromDate, DateTime toDate);
        List<ScanOrderLoadModal> ScanOrderLoad(string serverIP, string storeNo, DateTime bussinessDate, string orderNo, int pageSize, int pageNumber);
        Tuple<int, string, ScanOrderInsertResponseModel> ScanOrderInsert(string storeNo, string barcode, string orderNo, string itemNo, string userName, string serverStore);
        Tuple<int, string> ScanOrderDelete(string storeNo, string orderNo, string userName, long id);
        List<OptionModel> LoadPaymentTypeForEWalletList();
        List<IPAddressPOSTerminalByStoreModel> LoadPOSTerminalByStoreSetupPrint(string storeNo);
        List<IPAddressModel> GetIPAddressList(string storeNo, string IpAddress, string computerName, out int totalRecord, int skip, int take);
        List<StorePartnerModel> LoadComboxStoreNoByUser(string userName);
        List<string> GetFromChangeSalesType();
        List<OptionModel> GetToChangeSalesType();
        List<OptionModel> GetComboxMemberCardType();
        List<OptionDataBySalesTypeModel> LoadSalesTypeByMultipleCheck();
        List<OptionDataModel> GetComboxOptionData(string caption);
        List<StorePartnerModel> LoadComboxStoreByUserChannel(string userName, string channelSales);
        List<OptionSiteModel> LoadComboxStoreNoByWinLife();
        List<VoucherTypeModel> LoadComboxVoucherType();
        List<ArraySiteNoModel> LoadStoreByUserName(string ipServer, string storeNo, string userName);
        List<OptionDataModel> LoadComboxCupType();
        List<string> CheckSecurityStoreByUserName(string userName, string listStore);
        List<OptionSiteModel> LoadComboxStoreNoWinLife(string userName);
        List<OptionDataModel> LoadComboxMemberCodeType();
        StoreListSpecialComboModel LoadStoreByUpdateSpecialCombo(string channel, string code);
        

    }

    public class CommonData : ICommonData
    {
        public List<DocumentTypeModel> GetComboxDocumentType()
        {
            try
            {
                var result = new List<DocumentTypeModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.OptionDatas.AsNoTracking()
                            .Where(x =>x.Caption == "DOCUMENTLOG")
                            .Select(a => new DocumentTypeModel
                            {
                                Value = a.Code,
                                Text = a.Description,
                                Order = a.Order
                            }).OrderBy(b =>b.Order).ToList();  // tang dan
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<DocumentTypeModel>();
            }
        }

        public List<OptionModel> GetComboxArticleType()
        {
            try
            {
                var result = new List<OptionModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.ArticleTypes.ToList()
                            .Select(a => new OptionModel
                            {
                                Value = a.Code,
                                Text = a.Code
                            }).Distinct().OrderByDescending(X => X.Value).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<OptionModel>));
            }
        }
        public List<OptionModel> GetComboxUnitOfMeasure()
        {
            try
            {
                var result = new List<OptionModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.UnitOfMeasures.ToList()
                            .Select(a => new OptionModel
                            {
                                Value = a.Code,
                                Text = a.Code
                            }).Distinct().OrderByDescending(X => X.Value).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<OptionModel>));
            }
        }
        public List<OptionModel> GetComboxVoucherCouponList()
        {
            return new List<OptionModel>
            {
                new OptionModel
                {
                     Value = "ZCOU",
                     Text = "ZCOU"
                },

                new OptionModel
                {
                     Value = "ZCPN",
                     Text = "ZCPN"
                },

                new OptionModel
                {
                     Value = "ZVCN",
                     Text = "ZVCN"
                },

                new OptionModel
                {
                     Value = "ZVCO",
                     Text = "ZVCO"
                }

            };
        }
        public List<StoreModel> GetComboxStoreByUser(string userName)
        {
            var result = new List<StoreModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new AdminUserModel
                    {
                        TypeLogin = a.TypeLogin,
                        UserName = a.UserName,
                        StaffCode = a.StaffCode,
                        StoreNo = a.StoreNo,
                        IsAllStore = a.IsAllStore
                    }).FirstOrDefault();

                    if (data != null)
                    {
                        if (data.IsAllStore == true)
                        {
                            result = db.Stores.Where(d => d.ClosingMethod == 0)
                                      .Select(a => new StoreModel
                                      {
                                          StoreNo = a.No,
                                          StoreName = a.Name
                                      }).ToList();
                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(data.StoreNo);
                            result = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0)
                                      .Select(a => new StoreModel
                                      {
                                          StoreNo = a.No,
                                          StoreName = a.Name
                                      }).ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public Tuple<bool, CheckEmployeeModel> CheckStaffCode(string staffCode)
        {
            try
            {
                var listdata = new CheckEmployeeModel();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Staffs.FirstOrDefault(x => x.ID == staffCode);
                    if (data != null)
                    {
                        return new Tuple<bool, CheckEmployeeModel>(true, new CheckEmployeeModel
                        {
                            Message = $"Mã nhân viên : {staffCode} này đã tồn tại. Vui lòng kiểm tra lại",
                            Status = true
                        });
                    }

                    // Nhân viên mới mã số phải từ 8 ký tự số trở lên
                    var _staffCode = staffCode;
                    var model = new CheckEmployeeModel
                    {
                        StaffCode = _staffCode,
                        Message = string.Empty,
                        Status = false
                    };
                    return new Tuple<bool, CheckEmployeeModel>(false, model);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, CheckEmployeeModel>(true, new CheckEmployeeModel
                {
                    Message = $"Mã nhân viên : {staffCode} này đã tồn tại. Vui lòng kiểm tra lại",
                    Status = false
                });
            }
        }
        public List<OptionModel> GetPermisionPOSTerminal()
        {
            return new List<OptionModel>
            {
                new OptionModel
                {
                     Value = "ALL",
                     Text = "All"
                },

                new OptionModel
                {
                     Value = "SALES",
                     Text = "Bán hàng"
                },

                new OptionModel
                {
                     Value = "RETURN",
                     Text = "Trả hàng"
                }
            };
        }
        public List<OptionModel> GetComboxProvince()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Provinces
                                select new OptionModel
                                {
                                    Value = a.ProvinceCode,
                                    Text = a.ProvinceName
                                }).OrderBy(x => x.Value).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> GetComboxStoreList()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Stores
                                where a.ClosingMethod == 0
                                select new OptionModel
                                {
                                    Value = a.No,
                                    Text = a.Name
                                }).OrderBy(x => x.Value).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> GetComboxStoreListByWinLife()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Stores
                                where a.ClosingMethod == 0
                                select new OptionModel
                                {
                                    Value = a.No,
                                    Text = a.Name
                                }).OrderBy(x => x.Value).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> LoadComboxBankList()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Banks
                                select new OptionModel
                                {
                                    Value = a.BankCode,
                                    Text = a.BankName
                                }).OrderBy(x => x.Value).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> LoadComboxPOSTerminalByStore(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.POSTerminals
                                where a.StoreNo == storeNo
                                select new OptionModel
                                {
                                    Value = a.No,
                                    Text = a.No
                                }).OrderBy(x => x.Value).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> GetOrderType()
        {
            return new List<OptionModel>
            {
                // TransactionType : 1 = dh bán , 2 = dh trả
                new OptionModel
                {
                     Value = "1",
                     Text = "Đơn hàng bán"
                },
                new OptionModel
                {
                     Value ="2",
                     Text = "Đơn hàng trả"
                }
            };
        }
        public List<RoleModel> GetRolePermissionByUser(string userName)
        {
            var result = new List<RoleModel>();
            try
            {              
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = (from a in db.AdminUsers.AsNoTracking()                                      
                            where a.UserName == userName && a.IsActive == true
                            select new RoleModel
                            {
                                RoleCode = a.RoleCode
                            }).ToList();
                }         
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<string> GetFromChangeSalesType()
        {
            var result = new List<string>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.OptionDatas.AsNoTracking()
                            .Where(a => a.Caption == "FromChangeSalesType" && a.Status == true)
                            .Select(a => a.Code).ToList();                                              
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<OptionModel> GetToChangeSalesType()
        {
            var result = new List<OptionModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = (from a in db.OptionDatas.AsNoTracking()
                              where a.Caption == "ToChangeSalesType" && a.Status == true
                              select new OptionModel
                              {
                                  Value = a.Code,
                                  Text = a.Description
                              }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<SQLServerModel> GetInforStoreSet(string storeNo)
        {
            var result = new List<SQLServerModel>();
            try
            {
                using (var dbGeneral = new CentralGeneralContainer())
                {
                    dbGeneral.Database.CommandTimeout = 2 * 60;
                    result = (from a in dbGeneral.StoreSetServers
                              join b in dbGeneral.SQLServers on a.ServerIP equals b.IPServer
                              where a.StoreNo == storeNo
                              select new SQLServerModel
                              {
                                  IPServer = b.IPServer,
                                  IPServerRead = a.ServerRead,
                                  Description = b.Description
                              }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<SQLServerModel> ListSQLServer()
        {
            try
            {
                var result = new List<SQLServerModel>();
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.SQLServers.Where(d => d.Status == true && d.IsSetDB == true).Select(a => new SQLServerModel
                    {
                        ID = a.ID,
                        IPServer = a.IPServer,
                        IPServerRead = a.IPServerRead,
                        NameSvr = a.NameSvr,
                        UserID = a.UserID,
                        Password = a.Password,
                        Status = a.Status,
                        Description = a.Description
                    }).OrderBy(d => d.IPServer).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<SQLServerModel>));
            }
        }
        public List<IPAddressPOSTerminalByStoreModel> LoadPOSTerminalByStore(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.POSTerminals
                                where a.StoreNo == storeNo
                                select new IPAddressPOSTerminalByStoreModel
                                {
                                    StoreNo = a.StoreNo,
                                    IpAddress = a.IPAddress,
                                    PosID = a.No,
                                    PosName = a.No
                                }).OrderBy(x => x.PosID).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<IPAddressPOSTerminalByStoreModel>();
                    }

                    var result = (from b in data
                                  select new IPAddressPOSTerminalByStoreModel
                                  {
                                      StoreNo = b.StoreNo,
                                      IpAddress = b.IpAddress,
                                      PosID = b.PosID,
                                      PosName = b.PosName
                                  }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<IPAddressPOSTerminalByStoreModel>();
            }
        }
        public List<IPAddressPOSTerminalByStoreModel> LoadIPAddressPOSTerminalByStore(string storeNo)
        {
            try
            {
                var result = new List<IPAddressPOSTerminalByStoreModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.POSTerminals.Where(d => d.StoreNo == storeNo).ToList().Select(a => new IPAddressPOSTerminalByStoreModel
                    {
                        StoreNo = a.StoreNo,
                        IpAddress = a.IPAddress,
                        PosID = a.No,
                        PosName = a.No
                    }).OrderBy(d => d.PosID).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<IPAddressPOSTerminalByStoreModel>));
            }
        }
        public List<ArraySiteNoModel> LoadStoreByUserNameSetDB(string userLogin, string ipServer, string ipWriteLog)
        {
            long milliseconds = 0;
            var st1 = new Stopwatch();
            if (userLogin == "vupt" || userLogin == "trangttq" || userLogin == "namdv" || userLogin == "honglk")
            {
                st1.Start();
                LogsFile.WriteLogFileV2("LoadSetDBFromCentral", $"{userLogin}_Begin", ipWriteLog);
            }

            var result = new List<ArraySiteNoModel>();

            try
            {
                var storeSet = new List<StoreSetServerModel>();
                using (var dbGeneral = new CentralGeneralContainer())
                {
                    dbGeneral.Database.CommandTimeout = 5 * 60;
                    if (string.IsNullOrEmpty(ipServer))
                    {
                        storeSet = dbGeneral.StoreSetServers.Select(a => new StoreSetServerModel
                        {
                            ServerIP = a.ServerIP,
                            ServerRead = a.ServerRead,
                            StoreNo = a.StoreNo,
                            Description = a.Description,
                            Status = a.Status
                        }).ToList();
                    }
                    else
                    {
                        storeSet = dbGeneral.StoreSetServers.Where(d => d.ServerIP == ipServer).Select(a => new StoreSetServerModel
                        {
                            ServerIP = a.ServerIP,
                            ServerRead = a.ServerRead,
                            StoreNo = a.StoreNo,
                            Description = a.Description,
                            Status = a.Status
                        }).ToList();
                    }

                    if (storeSet != null && storeSet.Count > 0)
                    {
                        using (var db = new CentralMDPartnerContainer())
                        {
                            db.Database.CommandTimeout = 2 * 60;
                            var data = db.AdminUsers.Where(d => d.UserName == userLogin && d.IsActive == true).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive
                            }).FirstOrDefault();

                            if (data != null)
                            {
                                if (data.Permission == true)  // IsAllStore = 1 , tat ca cua hang
                                {
                                    var storePermision = db.Stores.Where(a => a.ClosingMethod == 0).ToList();
                                    result = (from a in storePermision
                                              join b in storeSet on a.No equals b.StoreNo
                                              select new ArraySiteNoModel
                                              {
                                                  SiteNo = a.No,
                                                  SiteName = a.Name,
                                                  StyleProfile = a.StyleProfile,
                                                  IsAllStore = data.IsAllStore
                                              }).ToList();
                                }
                                else    // IsAllStore = 0
                                {
                                    var listStore = JsonConvert.DeserializeObject<List<string>>(data.SiteNo);
                                    var storePermision = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0).ToList();
                                    result = (from a in storePermision
                                              join b in storeSet on a.No equals b.StoreNo
                                              select new ArraySiteNoModel
                                              {
                                                  SiteNo = a.No,
                                                  SiteName = a.Name,
                                                  StyleProfile = a.StyleProfile,
                                                  IsAllStore = data.IsAllStore
                                              }).ToList();
                                }
                            }
                        }
                    }

                    if (userLogin == "vupt" || userLogin == "trangttq" || userLogin == "namdv" || userLogin == "honglk")
                    {
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        LogsFile.WriteLogFileV2("LoadSetDBFromCentral", $"{userLogin}_End:{milliseconds} milliseconds", ipWriteLog);
                    }

                }
            }
            catch (Exception ex)
            {
                if (userLogin == "vupt" || userLogin == "trangttq" || userLogin == "namdv" || userLogin == "honglk")
                {
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                    LogsFile.WriteLogFileV2("LoadSetDBFromCentral", $"{userLogin}_Error {JsonConvert.SerializeObject(ex)}:{milliseconds} milliseconds", ipWriteLog);
                }
            }

            return result;

        }
        public List<ArraySiteNoModel> LoadComboxStoreByUserName(string userLogin)
        {
            var result = new List<ArraySiteNoModel>();
            try
            {
                var storeSet = new List<StoreSetServerModel>();
                using (var dbGeneral = new CentralGeneralContainer())
                {
                    dbGeneral.Database.CommandTimeout = 5 * 60;
                    storeSet = dbGeneral.StoreSetServers.Select(a => new StoreSetServerModel
                    {
                        ServerIP = a.ServerIP,
                        ServerRead = a.ServerRead,
                        StoreNo = a.StoreNo,
                        Description = a.Description,
                        Status = a.Status
                    }).ToList();

                    if (storeSet != null && storeSet.Count > 0)
                    {
                        using (var db = new CentralMDPartnerContainer())
                        {
                            db.Database.CommandTimeout = 5 * 60;
                            var data = db.AdminUsers.Where(d => d.UserName == userLogin && d.IsActive == true).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive
                            }).FirstOrDefault();

                            if (data != null)
                            {
                                if (data.Permission == true)  // quyen All : IsAllStore = 1
                                {
                                    var storePermision = db.Stores.AsNoTracking().Where(a => a.ClosingMethod == 0).ToList();
                                    result = (from a in storePermision
                                              join b in storeSet on a.No equals b.StoreNo
                                              join c in dbGeneral.SQLServers on b.ServerIP equals c.IPServer
                                              select new ArraySiteNoModel
                                              {
                                                  Code = c.Code,
                                                  ServerIP = c.IPServer,
                                                  ServerIPRead = c.IPServerRead,
                                                  ServerStoreDesc = c.NameSvr,
                                                  SiteNo = a.No,
                                                  SiteName = a.Name,
                                                  StyleProfile = a.StyleProfile 
                                              }).ToList();
                                }
                                else    // IsAllStore = 0
                                {
                                    var listStore = JsonConvert.DeserializeObject<List<string>>(data.SiteNo);
                                    var storePermision = db.Stores.AsNoTracking().Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0).ToList();
                                    result = (from a in storePermision
                                              join b in storeSet on a.No equals b.StoreNo
                                              join c in dbGeneral.SQLServers on b.ServerIP equals c.IPServer
                                              select new ArraySiteNoModel
                                              {
                                                  Code = c.Code,
                                                  ServerIP = c.IPServer,
                                                  ServerIPRead = c.IPServerRead,
                                                  ServerStoreDesc = c.NameSvr,
                                                  SiteNo = a.No,
                                                  SiteName = a.Name,
                                                  StyleProfile = a.StyleProfile
                                              }).ToList();
                                }
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
        public List<ArraySiteNoModel> LoadStoreBySetServerChanelNo(string userLogin, string ipServer, string chanelNo)
        {
            var result = new List<ArraySiteNoModel>();
            try
            {
                var storeSet = new List<StoreSetServerModel>();
                using (var dbGeneral = new CentralGeneralContainer())
                {
                    dbGeneral.Database.CommandTimeout = 2 * 60;
                    if (string.IsNullOrEmpty(ipServer))
                    {
                        storeSet = dbGeneral.StoreSetServers.Select(a => new StoreSetServerModel
                        {
                            ServerIP = a.ServerIP,
                            ServerRead = a.ServerRead,
                            StoreNo = a.StoreNo,
                            Description = a.Description,
                            Status = a.Status
                        }).ToList();
                    }
                    else
                    {
                        storeSet = dbGeneral.StoreSetServers.Where(d => d.ServerIP == ipServer).Select(a => new StoreSetServerModel
                        {
                            ServerIP = a.ServerIP,
                            ServerRead = a.ServerRead,
                            StoreNo = a.StoreNo,
                            Description = a.Description,
                            Status = a.Status
                        }).ToList();
                    }

                    if (storeSet != null && storeSet.Count > 0)
                    {
                        using (var db = new CentralMDPartnerContainer())
                        {
                            db.Database.CommandTimeout = 2 * 60;
                            var data = db.AdminUsers.Where(d => d.UserName == userLogin).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive
                            }).FirstOrDefault();

                            if (data != null)
                            {
                                if (data.Permission == true)  // IsAllStore = 1
                                {
                                    if (chanelNo == "-1")  // Tat ca
                                    {
                                        var storePermision = db.Stores.Where(a => a.ClosingMethod == 0).ToList();
                                        result = (from a in storePermision
                                                  join b in storeSet on a.No equals b.StoreNo
                                                  select new ArraySiteNoModel
                                                  {
                                                      SiteNo = a.No,
                                                      SiteName = a.Name,
                                                      StyleProfile = a.StyleProfile
                                                  }).ToList();
                                    }
                                    else
                                    {
                                        var storePermision = db.Stores.Where(a => a.ClosingMethod == 0 && a.StyleProfile == chanelNo).ToList();
                                        result = (from a in storePermision
                                                  join b in storeSet on a.No equals b.StoreNo
                                                  select new ArraySiteNoModel
                                                  {
                                                      SiteNo = a.No,
                                                      SiteName = a.Name,
                                                      StyleProfile = a.StyleProfile
                                                  }).ToList();
                                    }

                                }
                                else    // IsAllStore = 0
                                {
                                    var listStore = JsonConvert.DeserializeObject<List<string>>(data.SiteNo);
                                    if (chanelNo == "-1")  // Tat ca
                                    {
                                        var storePermision = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0).ToList();
                                        result = (from a in storePermision
                                                  join b in storeSet on a.No equals b.StoreNo
                                                  select new ArraySiteNoModel
                                                  {
                                                      SiteNo = a.No,
                                                      SiteName = a.Name,
                                                      StyleProfile = a.StyleProfile
                                                  }).ToList();
                                    }
                                    else
                                    {
                                        var storePermision = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0 && a.StyleProfile == chanelNo).ToList();
                                        result = (from a in storePermision
                                                  join b in storeSet on a.No equals b.StoreNo
                                                  select new ArraySiteNoModel
                                                  {
                                                      SiteNo = a.No,
                                                      SiteName = a.Name,
                                                      StyleProfile = a.StyleProfile
                                                  }).ToList();
                                    }
                                }
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
        public List<ArraySiteNoModel> LoadComboxStoreNo()
        {
            var data = new List<ArraySiteNoModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Stores.Where(a => a.ClosingMethod == 0).Select(a => new ArraySiteNoModel
                    {
                        SiteNo = a.No,
                        SiteName = a.Name,
                        StyleProfile = a.StyleProfile
                    }).OrderBy(x => x.SiteNo).Distinct().ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return data;
        }
        public Tuple<bool, ResultPingIPLocalModel> CheckPingIPLocal(string IPAddress)
        {
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(IPAddress);
                bool bIsConnected = false;
                var messagePort = "";
                if (pingReply.Status == IPStatus.Success)
                {
                    bIsConnected = true;
                    ping.Dispose();
                }
                else
                {
                    ping.Dispose();
                    return new Tuple<bool, ResultPingIPLocalModel>(false, new ResultPingIPLocalModel
                    {
                        bIsConnected = false,
                        IPAddress = IPAddress,
                        Status = false,
                        Message = $"{IPAddress} không ping được (hoặc chưa mở máy Pos bán hàng). Vui lòng kiểm tra lại"
                    });
                }

                if (!Utils.CheckPort(IPAddress, 1433, ref messagePort))
                {
                    return new Tuple<bool, ResultPingIPLocalModel>(false, new ResultPingIPLocalModel
                    {
                        bIsConnected = false,
                        IPAddress = IPAddress,
                        Status = false,
                        Message = $"Không telnet được Port 1433. Vui lòng kiểm tra lại"
                    });
                }
                else
                {
                    return new Tuple<bool, ResultPingIPLocalModel>(true, new ResultPingIPLocalModel
                    {
                        bIsConnected = true,
                        IPAddress = IPAddress,
                        Status = true
                    });
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, ResultPingIPLocalModel>(false, new ResultPingIPLocalModel
                {
                    bIsConnected = false,
                    IPAddress = IPAddress,
                    Status = false,
                    Message = $"Có lỗi xảy ra"
                });
            }
        }
        public string GetIPAddessByPOSTerminal(string posID)
        {
            try
            {
                var result = string.Empty;
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.POSTerminals.Where(d => d.No == posID).FirstOrDefault()?.IPAddress;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public string GetIPSetServerByStore(string storeNo)
        {
            try
            {
                var result = string.Empty;
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.StoreSetServers.Where(x => x.StoreNo == storeNo.Trim()).FirstOrDefault()?.ServerIP;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public List<OptionModel> GetPaymentType()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.TenderTypeSetups
                                where a.MayBeUsed == 1
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.Description
                                }).OrderBy(x => x.Value).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<StaffCodeResponseModel> LoadComboxStaffCode(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Staffs
                                where a.StoreNo == storeNo && a.Blocked == 0   // && a.EmploymentType == 0
                                select new StaffCodeResponseModel
                                {
                                    UserPOS = a.ID,
                                    EmploymentType = a.EmploymentType
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<StaffCodeResponseModel>));
            }
        }
        public async Task<VCM.BLUEPOS.Model.Employee.StaffModel> InfoStaffByAD(string manv)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Staffs.Where(d => d.ID == manv).Select(x => new VCM.BLUEPOS.Model.Employee.StaffModel
                    {
                        ID = x.ID,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        StoreNo = x.StoreNo,
                        EmploymentType = x.EmploymentType
                    }).FirstOrDefault();

                    if (data == null)
                    {
                        return default(VCM.BLUEPOS.Model.Employee.StaffModel);
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return default(VCM.BLUEPOS.Model.Employee.StaffModel);
            }
        }
        public List<VCM.BLUEPOS.Model.Employee.StaffModel> InfoStaffByStore(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Staffs.Where(d => d.StoreNo == storeNo && d.Blocked == 0).Select(x => new VCM.BLUEPOS.Model.Employee.StaffModel
                    {
                        ID = x.ID,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        StoreNo = x.StoreNo,
                        EmploymentType = x.EmploymentType
                    }).ToList();

                    if (data == null)
                    {
                        return new List<VCM.BLUEPOS.Model.Employee.StaffModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<VCM.BLUEPOS.Model.Employee.StaffModel>();
            }
        }
        public VCM.BLUEPOS.Model.Common.StyleProfileModel GetStyleProfile(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Stores
                                where a.No == storeNo
                                select new VCM.BLUEPOS.Model.Common.StyleProfileModel
                                {
                                    StyleProfile = a.StyleProfile
                                }).FirstOrDefault();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(VCM.BLUEPOS.Model.Common.StyleProfileModel));
            }
        }
        public string InsertBulk(DataTable table, string TableName, string storeNo, string orderNo = "", string posID = "")
        {
            string Result = "@@-ERR";
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            var messLog = $"{TableName}-{ipServer}";

            try
            {
                //var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
                using (var db = new CentralSalesStagingContainer(ipServer))
                {
                    var bulkCopy = new SqlBulkCopy(db.Database.Connection.ConnectionString, SqlBulkCopyOptions.KeepIdentity);
                    foreach (DataColumn col in table.Columns)
                    {
                        //check neu trong table duoi DB k ton tai column nay thi k add vao

                        if (col.ColumnName.ToUpper() != "timestamp".ToUpper())
                        {
                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                        }
                    }

                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = TableName;
                    bulkCopy.WriteToServer(table);
                    bulkCopy.Close();
                    Result = "OK";
                }
            }
            catch (Exception ex)
            {
                Result = "@@-" + JsonConvert.SerializeObject(ex);
                messLog = $"{TableName}-{ipServer}-{ JsonConvert.SerializeObject(ex)}";
            }
            try
            {
                var jsonData = JsonConvert.SerializeObject(table);
                var dateTime = DateTime.Now;
                var modelLog = new LogErrorWebModel
                {
                    StoreNo = storeNo,
                    OrderNo = orderNo,
                    PosTerminal = posID,
                    Action = "UploadSale",
                    Description = jsonData,
                    Msg = messLog,
                    CreatedUser = "",
                    CreatedDate = dateTime
                };
                InsertLogErrorWeb(modelLog);
            }
            catch (Exception ex)
            {
                messLog = $"{TableName}-{ipServer}-{ JsonConvert.SerializeObject(ex)}";
                var jsonData = JsonConvert.SerializeObject(table);
                var dateTime = DateTime.Now;
                var modelLog = new LogErrorWebModel
                {
                    StoreNo = storeNo,
                    OrderNo = orderNo,
                    PosTerminal = posID,
                    Action = "UploadSale",
                    Description = jsonData,
                    Msg = messLog,
                    CreatedUser = "",
                    CreatedDate = dateTime
                };
                InsertLogErrorWeb(modelLog);
            }
            return Result;
        }
        public void InsertTrans_Invoice(string storeNo, string OrderNo)
        {
            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new CentralSalesStagingContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    //db.Database.ExecuteSqlCommand("[dbo].[InsertTrans] @OrderNo ",
                    //new SqlParameter("@OrderNo", OrderNo));

                    db.Database.ExecuteSqlCommand("[dbo].[InsertStagingInvoice] @OrderNo ",
                            new SqlParameter("@OrderNo", OrderNo));
                }
                catch (Exception ex)
                {
                }
            }
        }
        public void InsertLogErrorWeb(LogErrorWebModel model)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var insert = new LogErrorWeb
                    {
                        StoreNo = model.StoreNo,
                        PosTerminal = model.PosTerminal,
                        OrderNo = model.OrderNo,
                        Action = model.Action,
                        Description = model.Description,
                        Msg = model.Msg,
                        CreatedUser = model.CreatedUser,
                        CreatedDate = model.CreatedDate
                    };
                    db.LogErrorWebs.Add(insert);
                    db.SaveChanges();
                }
            }
            catch (Exception Ex)
            {
            }
        }
        public List<MCHComboboxModel> LoadComboxNganhHang()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.PosGroups.Select(a => new MCHComboboxModel
                    {
                        Code = a.GroupCode,
                        Name = a.Description
                    }).OrderBy(d => d.Code).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<MCHComboboxModel>));
            }
        }
        public List<MCHComboboxModel> LoadComboxMCH5(string MCH2)
        {
            var data = new List<MCHComboboxModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = (from a in db.PosGroups
                            join b in db.PosGroupItems on a.GroupCode equals b.GroupCode
                            join c in db.Items on b.ItemNo equals c.No
                            join d in db.Items on b.UnitOfMeasure equals d.BaseUnitOfMeasure
                            where a.GroupCode == MCH2
                            select new MCHComboboxModel
                            {
                                Code = c.No,
                                Name = c.Description
                            }).Distinct().OrderBy(d => d.Code).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return data;
        }
        public List<VoucherReceiptTypeModel> GetVoucherReceiptType()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    // ZRET = Biên nhận mua hàng = Biên nhận thanh toán ; ZIVO = Voucher
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.TenderTypeSetups
                                where a.Code == "ZRET" || a.Code == "ZIVO"
                                select new VoucherReceiptTypeModel
                                {
                                    Value = a.Code,
                                    Text = a.Description
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<VoucherReceiptTypeModel>));
            }
        }
        public List<SyncTableListModel> ListTableSync()
        {
            try
            {
                var result = new List<SyncTableListModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.SyncTableLists.GroupBy(d => d.GroupName).Select(a => new SyncTableListModel
                    {
                        GroupName = a.Key
                    }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<SyncTableListModel>));
            }
        }
        public List<SyncJobLogModel> ListLogSyncJob(string processID)
        {
            try
            {
                var result = new List<SyncJobLogModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.SyncJobLogs.Where(d => d.ProcessID == processID && d.ActionTable == "Zip").Select(a => new SyncJobLogModel
                    {
                        Type = a.Type,
                        Message = a.Message,
                        TotalRecord = a.TotalRecord,
                        SiteNo = a.SiteNo,
                        FileName = a.FileName,
                        TableName = a.TableName
                    }).OrderBy(d => d.SiteNo).OrderBy(d => d.Message).ToList();
                    return result;
                }
            }
            catch (Exception Ex)
            {
                return (default(List<SyncJobLogModel>));
            }
        }
        public List<SAPConfigXMLModel> ListConfigXMLSAP()
        {
            try
            {
                var result = new List<SAPConfigXMLModel>();
                using (var db = new IFSAPContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.ConfigXMLs.GroupBy(d => d.TransType).Select(a => new SAPConfigXMLModel
                    {
                        TransType = a.Key
                    }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return (default(List<SAPConfigXMLModel>));
            }
        }
        public List<ProvinceModel> GetComboxProvinceList()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.Provinces
                                where a.Enable == true
                                select new ProvinceModel
                                {
                                    ProvinceCode = a.ProvinceCode,
                                    ProvinceName = a.ProvinceName
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ProvinceModel>));
            }
        }
        public Tuple<bool, CheckStoreModel> CheckStoreNo(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Stores.FirstOrDefault(x => x.No == storeNo);
                    if (data == null)
                    {
                        return new Tuple<bool, CheckStoreModel>(false, new CheckStoreModel
                        {
                            Message = $"Mã {storeNo} ST/CH này không tồn tại trong hệ thống",
                            Status = false
                        });
                    }
                    var model = new CheckStoreModel
                    {
                        StoreNo = data.No,
                        StoreName = data.Name,
                        StyleProfile = data.StyleProfile,
                        Message = string.Empty,
                        Status = true
                    };
                    return new Tuple<bool, CheckStoreModel>(true, model);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, CheckStoreModel>(false, new CheckStoreModel
                {
                    Message = $"Mã {storeNo} ST/CH này không tồn tại trong hệ thống",
                    Status = false
                });
            }
        }
        public List<ArraySiteNoModel> LoadComboxStoreByChanel(string chanelNo)
        {
            //honglk: 09.03.2026: khong filter theo kenh
            //chanelNo = "-1";
            //end

            var result = new List<ArraySiteNoModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (chanelNo == "-1" || chanelNo == "ALL")  // Tat ca
                    {
                        var listData = db.Stores.Where(a => a.ClosingMethod == 0).ToList();
                        result = (from a in listData
                                  select new ArraySiteNoModel
                                  {
                                      SiteNo = a.No,
                                      SiteName = a.Name,
                                      StyleProfile = a.StyleProfile
                                  }).ToList();
                    }
                    else
                    {
                        var listData = db.Stores.Where(a => a.ClosingMethod == 0 && a.StyleProfile == chanelNo).ToList();
                        result = (from a in listData
                                  select new ArraySiteNoModel
                                  {
                                      SiteNo = a.No,
                                      SiteName = a.Name,
                                      StyleProfile = a.StyleProfile
                                  }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<ArraySiteNoModel> LoadComboxSiteByChanelAndStore(string chanelNo, string storeNo)
        {
            var result = new List<ArraySiteNoModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    //var listData = db.Stores.Where(a => a.StyleProfile == chanelNo && a.No == storeNo && a.ClosingMethod == 0).ToList();
                    var listData = db.Stores.Where(a => a.No == storeNo && a.ClosingMethod == 0).ToList();
                    result = (from a in listData
                              select new ArraySiteNoModel
                              {
                                  SiteNo = a.No,
                                  SiteName = a.Name,
                                  StyleProfile = a.StyleProfile
                              }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<SQLServerModel> ListSQLServerExcute()
        {
            try
            {
                var result = new List<SQLServerModel>();
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.SQLServers.Where(d => d.Status == true).Select(a => new SQLServerModel
                    {
                        ID = a.ID,
                        IPServer = a.IPServer,
                        NameSvr = a.NameSvr,
                        UserID = a.UserID,
                        Password = a.Password,
                        Status = a.Status,
                        Description = a.Description
                    }).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                return (default(List<SQLServerModel>));
            }
        }
        public List<IPAddressModel> GetListIP(string storeNo, string IpAddress, string computerName, out int totalRecord, int skip, int take)
        {
            try
            {
                var result = new List<IPAddressModel>();
                List<string> filterSearch = new List<string>();
                var ListSearch = IpAddress.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                foreach (var item in ListSearch)
                {
                    filterSearch.Add(item.Trim().ToLower());
                }
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSMonitors.AsQueryable();

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.StoreNo == storeNo);
                    }
                    if (!string.IsNullOrEmpty(computerName))
                    {
                        data = data.Where(d => d.ComputerName.Contains(computerName));
                    }
                    if (!string.IsNullOrEmpty(IpAddress))
                    {
                        data = data.Where(a => filterSearch.Contains(a.IpAddress));
                    }

                    totalRecord = data.Count();
                    data = data.OrderByDescending(d => d.StoreNo).Skip(skip).Take(take);

                    result = data.ToList().Select(a => new IPAddressModel
                    {
                        StoreNo = a.StoreNo,
                        IpAddress = a.IpAddress,
                        PosTerminalID = a.PosTerminalID,
                        ComputerName = a.ComputerName

                    }).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                totalRecord = 0;
                return (default(List<IPAddressModel>));
            }
        }
        public List<DataTable> getDataByStore(string storeNo, string groupTable, string typeCounter, long posLastCounter)
        {
            List<DataTable> lstTable = new List<DataTable>();
            DataSet dsData = new DataSet();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    //db.Database.CommandTimeout = 2 * 60;
                    using (var con = new SqlConnection(db.Database.Connection.ConnectionString))
                    {
                        var listTable = db.SyncTableLists.Where(d => d.GroupName == groupTable && d.Enabled == true).ToList();
                        var listCounterStore = db.SyncChangeLogStores.Where(d => d.SiteCode == storeNo).ToList();

                        foreach (var item in listTable)
                        {

                            DataSet ds = new DataSet();
                            using (var cmd = new SqlCommand("SyncGetDataByStore", con))
                            using (var da = new SqlDataAdapter(cmd))
                            {
                                if (typeCounter == "LAST")
                                    posLastCounter = listCounterStore.FirstOrDefault(d => d.TableName == item.TableName)?.LastCounter ?? 0;


                                cmd.CommandTimeout = 120;
                                cmd.Parameters.AddWithValue("@POSLastCounter", posLastCounter);
                                cmd.Parameters.AddWithValue("@TableName", item.TableName);
                                cmd.Parameters.AddWithValue("@StoreNo", storeNo);
                                cmd.CommandType = CommandType.StoredProcedure;
                                da.Fill(ds);

                                var dtData = ds.Tables[0];
                                dtData.TableName = item.TableName;

                                if (dtData != null && dtData.Rows.Count > 0)
                                {
                                    lstTable.Add(dtData);
                                }

                                SyncJobLog_Insert("WEB", dtData.TableName, dtData.Rows.Count, dtData.Rows.Count, dtData.TableName, "PROC", "SyncGetDataByStore", storeNo, "", 0, "", 0, "");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    SyncJobLog_Insert("WEB", JsonConvert.SerializeObject(ex), 0, 0, "", "PROC", "SyncGetDataByStore", storeNo, "", 0, "", 0, "");
                }
            }
            return lstTable;
        }
        public List<OptionModel> LoadComboxPOSVersionSource()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSVersions.ToList();
                    if (data == null)
                    {
                        return new List<OptionModel>();
                    }
                    var result = data
                        .Select(x => new OptionModel
                        {
                            Value = x.Source,
                            Text = x.Source
                        }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> LoadComboxPOSVersionList()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSVersions.ToList();
                    if (data == null)
                    {
                        return new List<OptionModel>();
                    }
                    var result = data
                        .Select(x => new OptionModel
                        {
                            Value = x.LastVersion,
                            Text = x.LastVersion
                        }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<OptionModel> LoadComboxPOSVersionIsUpdate()
        {
            return new List<OptionModel>
            {
                new OptionModel
                {
                     Value = "Không",
                     Text = "false"
                },
                new OptionModel
                {
                     Value ="Có",
                     Text = "true"
                }
            };
        }
        public List<StaffCodeModel> ListStaffCode(string storeNo)
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
            catch (Exception ex)
            {
                return (default(List<StaffCodeModel>));
            }
        }
        public void DeleteAll(string ConnectStringStore, List<DataTable> lstTable)
        {
            StringBuilder strBuilder = new StringBuilder();

            using (SqlConnection connection = new SqlConnection(ConnectStringStore))
            {
                foreach (DataTable table in lstTable)
                {
                    if (table.Rows.Count > 0)
                    {
                        string QueryDelete = string.Format("TRUNCATE TABLE [{0}] ;", table.TableName);
                        strBuilder.AppendLine(QueryDelete);
                    }
                }
                if (strBuilder.Length > 0)
                {
                    SqlCommand command = new SqlCommand(strBuilder.ToString(), connection);
                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch
                    {
                    }
                }
            }
        }
        public void DeleteByPkey(string ConnectStringStore, List<DataTable> lstTable)
        {
            using (SqlConnection connection = new SqlConnection(ConnectStringStore))
            {

                foreach (DataTable table in lstTable)
                {
                    try
                    {
                        DataView view = new DataView(table);
                        DataTable distinctValues = view.ToTable(true, "Pkey");
                        StringBuilder strBuilder = new StringBuilder();

                        foreach (DataRow rowPkey in distinctValues.Rows)
                        {
                            string QueryDelete = string.Format("DELETE FROM [{0}] WHERE Pkey= '{1}';", table.TableName, rowPkey["Pkey"].ToString());
                            strBuilder.AppendLine(QueryDelete);
                        }
                        if (strBuilder.Length > 0)
                        {
                            SqlCommand command = new SqlCommand(strBuilder.ToString(), connection);
                            try
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                connection.Close();
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
        public int InsertDataBySQL(DataTable dtData, string ConnectStringStore, string storeNo, string ipPOS)
        {
            if (dtData.Rows.Count > 0)
            {
                string TableName = dtData.TableName;
                try
                {
                    var bulkCopy = new SqlBulkCopy(ConnectStringStore, SqlBulkCopyOptions.KeepIdentity);
                    foreach (DataColumn col in dtData.Columns)
                    {

                        if (col.ColumnName != "timestamp" && (col.ColumnName.ToUpper() != "ID" && TableName.ToLower() != "staff"))
                        {
                            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                        }
                    }
                    bulkCopy.BulkCopyTimeout = 1200;
                    bulkCopy.DestinationTableName = "[" + TableName + "]";
                    bulkCopy.WriteToServer(dtData);
                    bulkCopy.Close();

                    SyncJobLog_Insert("WEB", ipPOS, dtData.Rows.Count, dtData.Rows.Count, TableName, "SQL", "InsertDataBySQL", storeNo, "", 0, "", 0, "");
                    return dtData.Rows.Count;
                }
                catch (Exception ex)
                {
                    SyncJobLog_Insert("WEB", JsonConvert.SerializeObject(ex), dtData.Rows.Count, dtData.Rows.Count, TableName, "SQL", "InsertDataBySQL", storeNo, "", 0, "", 0, "");
                    return 0;
                }
            }
            else
            {
                SyncJobLog_Insert("WEB", "Không lấy được data synctableList", dtData.Rows.Count, dtData.Rows.Count, "", "SQL", "InsertDataBySQL", storeNo, "", 0, "", 0, "");
                return 0;
            }
        }
        public SalesType LoadComboxSalesType()
        {
            var result = new SalesType();
            using (var db = new CentralMDPartnerContainer())
            {
                try
                {
                    var salesType = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypeModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();

                    result = new SalesType
                    {
                        ListSalesType = salesType
                    };
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<OptionModel> GetVoucherTenderType()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    // ZRET = Biên nhận mua hàng = Biên nhận thanh toán ; 
                    // ZIVO = Voucher

                    /* ------ Phuc Long ------
                        --- V000 = Voucher
                        --- ???  = Biên nhận mua hàng
                    */

                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.TenderTypeSetups
                                where a.Code == "V000"
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.Description
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<OptionModel>));
            }
        }
        public SalesType LoadSalesOrderType()
        {
            var result = new SalesType();
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var salesType = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new SalesTypeModel
                    {
                        Code = a.Code,
                        TenderType = a.TenderType,
                        Description = a.Description,
                        Percent = a.Percent,
                        ImageName = a.ImageName,
                        Pkey = a.Pkey
                    }).ToList();

                    result = new SalesType
                    {
                        ListSalesType = salesType
                    };
                }
                catch (Exception ex)
                {
                }
                return result;
            }
        }
        public List<OptionModel> LoadComboxSalesOrderType()
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var data = (from a in db.SalesOrderTypes
                                where a.IsActive == true
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.Description
                                }).ToList();
                    return data;
                }
                catch (Exception ex)
                {
                    return (default(List<OptionModel>));
                }
            }
        }
        public List<OptionModel> LoadTenderTypeForSalesType()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.TenderTypeSetups
                                where a.MayBeUsed == 0 && a.Caption == "DELIVERY"
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.Description
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<OptionModel>));
            }
        }
        public List<OptionModel> LoadPaymentTenderTypeForSalesType()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.TenderTypeSetups
                                where a.MayBeUsed == 1 && a.Caption == "DELIVERY"
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.Description
                                }).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<OptionModel>));
            }
        }
        public List<POSDataSetupModel> ListPOSSetup()
        {
            try
            {
                var result = new List<POSDataSetupModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.POSDataSetups.Select(a => new POSDataSetupModel
                    {
                        Code = a.Code,
                        Value = a.Value
                    }).ToList();
                    return result;
                }
            }
            catch (Exception)
            {
                return (default(List<POSDataSetupModel>));
            }
        }
        public IPAddressModel GetIPAddressByPOSTerminal(string posTerminal)
        {
            var result = new IPAddressModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.POSTerminals.Where(d => d.No == posTerminal)
                        .Select(a => new IPAddressModel
                        {
                            StoreNo = a.StoreNo,
                            IpAddress = a.IPAddress,
                            PosTerminalID = a.No
                        }).FirstOrDefault();
                    return result;
                }
            }
            catch
            {
                return result;
            }
        }

        //public List<OptionModel> LoadComboxEWalletList()
        //{
        //    try
        //    {
        //        using (var db = new CentralMDPartnerContainer())
        //        {
        //            db.Database.CommandTimeout = 2 * 60;
        //            var data = (from a in db.TenderTypeSetups
        //                        where a.Caption == "EWALLET" && a.MayBeUsed == 1
        //                        select new OptionModel
        //                        {
        //                            Value = a.Code,
        //                            Text = a.Description
        //                        }).OrderBy(x => x.Value).ToList();

        //            if (data == null || data.Count == 0)
        //            {
        //                return new List<OptionModel>();
        //            }
        //            return data;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return new List<OptionModel>();
        //    }
        //}
        public List<OptionModel> LoadComboxEWalletList()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.OptionDatas
                                where a.Caption == "EWALLET" && a.MayBeUsed == 1 && a.Status == true
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.Description
                                }).OrderBy(x => x.Value).ToList();
                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<ServerWebModel> ListServerWeb(string userAD, string ipWriteLog = "")
        {
            long milliseconds = 0;
            var st1 = new Stopwatch();
            if (userAD == "vupt" || userAD == "trangttq" || userAD == "namdv" || userAD == "honglk")
            {
                st1.Start();
                LogsFile.WriteLogFileV2("GetServerWebHome", $"{userAD}_Begin", ipWriteLog);
            }

            var result = new List<ServerWebModel>();

            try
            {
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.SERVERWEBs.AsNoTracking().Where(d => d.ENABLE == true).Select(a => new ServerWebModel
                    {
                        IPSERVER = a.IPSERVER,
                        IPVIP = a.IPVIP,
                        LINKF5 = a.LINKF5,
                        LINKMICRO = a.LINKMICRO,
                        TYPESERVICE = a.TYPESERVICE,
                        FOLDERJOB = a.FOLDERJOB,
                        GROUPTYPE = a.GROUPTYPE,
                        NOTE = a.NOTE,
                        STT = a.STT
                    }).OrderBy(d => d.IPVIP).ThenBy(d => d.STT).ToList();

                    if (userAD == "vupt" || userAD == "trangttq" || userAD == "namdv" || userAD == "honglk")
                    {
                        milliseconds = st1.ElapsedMilliseconds;
                        st1.Stop();
                        LogsFile.WriteLogFileV2("GetServerWebHome", $"{userAD}_End:{milliseconds} milliseconds", ipWriteLog);
                    }
                }
            }
            catch (Exception ex)
            {
                if (userAD == "vupt" || userAD == "trangttq" || userAD == "namdv" || userAD == "honglk")
                {
                    var jsonErr = JsonConvert.SerializeObject(ex);
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                    LogsFile.WriteLogFileV2("GetServerWebHome", $"{userAD}_Error {jsonErr}:{milliseconds} milliseconds", ipWriteLog);
                }
            }
            return result;
        }
        public ViewCheckListDailyModel CheckList()
        {
            try
            {
                var result = new ViewCheckListDailyModel();
                var checkList = new List<CheckListDailyModel>();
                using (var db = new CentralGeneralContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var date = DateTime.Now.Date;
                    var checkListUser = db.CheckListUsers
                        .Where(d => //d.UserCheck == userName &&
                         DbFunctions.TruncateTime(d.DateTimeCheck) == date);

                    checkList = db.CheckListDailies.
                        Select(a => new CheckListDailyModel
                        {
                            ID = a.ID,
                            CheckListName = a.CheckListName,
                            TimeCheck = a.TimeCheck,
                            OwnerCheck = a.OwnerCheck,
                            Note = a.Note
                            //UserCheck = checkListUser.FirstOrDefault(d => d.CheckListID == a.ID).UserCheck,
                            //DateTimeCheck = checkListUser.FirstOrDefault(d => d.CheckListID == a.ID).DateTimeCheck,
                            //IsCheck = checkListUser.Any(d => d.CheckListID == a.ID)                            

                        }).ToList();

                    var totalListUserCheck = checkListUser.Select(a => new CheckListUserModel
                    {
                        UserCheck = a.UserCheck,
                        DateTimeCheck = a.DateTimeCheck,
                        CheckListID = a.CheckListID
                    }).ToList();


                    result.CheckList = checkList;
                    result.ListUserCheck = totalListUserCheck;

                    return result;
                }
            }
            catch (Exception)
            {
                return new ViewCheckListDailyModel();
            }
        }
        public Tuple<bool, string> CheckListUser(CheckListUserRequest model)
        {
            var result = new Tuple<bool, string>(false, "");
            using (var db = new CentralGeneralContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var checkTime = db.CheckListUsers.Where(d => d.UserCheck == model.UserCheck && DbFunctions.TruncateTime(d.DateTimeCheck) == model.DateTimeCheck.Date).ToList();
                    var totalCheckList = db.CheckListDailies.Count();
                    if (checkTime.Count == totalCheckList)
                    {
                        result = new Tuple<bool, string>(false, $"User {model.UserCheck} đã thực hiện rồi");
                        return result;
                    }

                    var checkListID = checkTime.Select(a => (int)a.CheckListID).ToList();
                    var checkListRequest = model.CheckListRequest.Where(a => !checkListID.Any(b => b == a.ID)).ToList();

                    var insertCheckList = checkListRequest.Select(a => new CheckListUser
                    {
                        UserCheck = model.UserCheck,
                        DateTimeCheck = model.DateTimeCheck,
                        CheckListID = a.ID

                    }).ToList();

                    db.CheckListUsers.AddRange(insertCheckList);
                    db.SaveChanges();

                    result = new Tuple<bool, string>(true, $"User {model.UserCheck} đã cập nhật thành công check list");
                    return result;
                }
                catch (Exception ex)
                {
                    var message = $"Lỗi hệ thống:{JsonConvert.SerializeObject(ex)}";
                    result = new Tuple<bool, string>(false, $"{message}");
                    return result;
                }
            }
        }
        public void SyncJobLog_Insert(string type, string message, int totalRecord, int recordOnfile, string tableName, string fileName, string actionTable, string siteNo, string posID, int lastCounter, string processID, int minCounter, string counterList)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var exec = db.Database.ExecuteSqlCommand("[dbo].[SyncJobLog_Insert] @Type, @Message, @TotalRecord, @RecordOnFile,@TableName, @FileName, @ActionTable,@SiteNo,@PosTerminal,@LastCounter,@ProcessID,@MinCounter,@CounterList",
                    new SqlParameter("@Type", type),
                    new SqlParameter("@Message", message),
                    new SqlParameter("@TotalRecord", totalRecord),
                    new SqlParameter("@RecordOnFile", recordOnfile),
                    new SqlParameter("@TableName", tableName),
                    new SqlParameter("@FileName", fileName),
                    new SqlParameter("@ActionTable", actionTable),
                    new SqlParameter("@SiteNo", siteNo),
                    new SqlParameter("@PosTerminal", posID),
                    new SqlParameter("@LastCounter", lastCounter),
                    new SqlParameter("@ProcessID", processID),
                    new SqlParameter("@MinCounter", minCounter),
                    new SqlParameter("@CounterList", counterList)
                   );
                }
            }
            catch
            {

            }
        }
        public List<OptionModel> LoadComboxCategoryList()
        {
            /*--- Nganh hang cha ---*/
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.PosGroups
                                where (a.ParentCode == "" || a.ParentCode == null) && a.Status == 1
                                select new OptionModel
                                {
                                    Value = a.GroupCode,
                                    Text = a.Description
                                }).OrderBy(x => x.Value).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public string LoadTenderTypeByWallet(string EWalletCode)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.OptionDatas.Where(d => d.Caption == "EWALLET" && d.Status == true && d.Code == EWalletCode).FirstOrDefault()?.TenderType;
                    return result;

                    //var result = db.TenderTypeSetups.Where(d => d.Caption == "EWALLET" && d.MayBeUsed == 1 && d.Description == EWalletCode).FirstOrDefault()?.Code;
                    //return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public Tuple<bool, string, List<ReportCheckListResponse>> ReportCheckList(DateTime fromDate, DateTime toDate)
        {
            var dataResponse = new List<ReportCheckListResponse>();
            var result = new Tuple<bool, string, List<ReportCheckListResponse>>(false, "", dataResponse);
            using (var db = new CentralGeneralContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {

                    dataResponse = db.Database.SqlQuery<ReportCheckListResponse>("[dbo].[SP_REPORT_CHECKLIST] @FromDate, @ToDate ",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate)
                       ).ToList();

                    result = new Tuple<bool, string, List<ReportCheckListResponse>>(true, $"Use", dataResponse);
                    return result;
                }
                catch (Exception ex)
                {
                    var message = $"Lỗi hệ thống:{JsonConvert.SerializeObject(ex)}";
                    result = new Tuple<bool, string, List<ReportCheckListResponse>>(false, $"{message}", dataResponse);
                    return result;
                }
            }
        }
        public List<ScanOrderLoadModal> ScanOrderLoad(string serverIP, string storeNo, DateTime bussinessDate, string orderNo, int pageSize, int pageNumber)
        {
            var result = new List<ScanOrderLoadModal>();


            var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);
            using (var db = new CentralSalesContainer(ipServer))
            {
                try
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.Database.SqlQuery<ScanOrderLoadModal>("[dbo].[SP_SCAN_ORDER_LOAD] @StoreNo, @BussinessDate,@OrderNo, @PageSize, @PageNumber ",
                        new SqlParameter("@StoreNo", storeNo),
                        new SqlParameter("@BussinessDate", bussinessDate),
                        new SqlParameter("@OrderNo", orderNo),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageNumber)
                        ).ToList();
                }
                catch (Exception ex)
                {
                }
                return result;
            }

        }
        public Tuple<int, string, ScanOrderInsertResponseModel> ScanOrderInsert(string storeNo, string barcode, string orderNo, string itemNo, string userName, string serverStore)
        {
            string storeOrder = orderNo.Substring(0, 4);

            var data = new ScanOrderInsertResponseModel();
            try
            {

                var ipServerOrder = ServerIPConnection.GetIPServerByStore(storeOrder);
                var ipServerStore = serverStore;

                using (var db = new CentralSalesContainer(ipServerOrder))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var getInfoByOrderNo = db.Database.SqlQuery<ScanOrderInsertResponseModel>("[dbo].[SP_SCAN_GET_INFO_ORDER] @StoreNo,@Barcode,@OrderNo,@ItemNo",
                     new SqlParameter("@StoreNo", storeNo),
                     new SqlParameter("@Barcode", barcode),
                     new SqlParameter("@OrderNo", orderNo),
                     new SqlParameter("@ItemNo", itemNo)
                     ).FirstOrDefault();


                    using (var dbStore = new CentralSalesContainer(ipServerStore))
                    {
                        dbStore.Database.CommandTimeout = 2 * 60;

                        data = dbStore.Database.SqlQuery<ScanOrderInsertResponseModel>("[dbo].[SP_SCAN_ORDER_INSERT] @StoreNo,@Barcode,@OrderNo,@ItemNo,@UserScan,@TotalAmount,@SalesIsReturn,@OrderTime,@OrderDate,@IsCentral ",
                        new SqlParameter("@StoreNo", storeNo),
                        new SqlParameter("@Barcode", barcode),
                        new SqlParameter("@OrderNo", orderNo),
                        new SqlParameter("@ItemNo", itemNo),
                        new SqlParameter("@UserScan", userName),
                        new SqlParameter("@TotalAmount", getInfoByOrderNo.TotalAmount),
                        new SqlParameter("@SalesIsReturn", getInfoByOrderNo.SalesIsReturn),
                        new SqlParameter("@OrderTime", getInfoByOrderNo.OrderTime),
                        new SqlParameter("@OrderDate", getInfoByOrderNo.OrderDate),
                        new SqlParameter("@IsCentral", getInfoByOrderNo.IsCentral)

                        ).FirstOrDefault();

                        return new Tuple<int, string, ScanOrderInsertResponseModel>(1, $"OK", data);
                    }

                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string, ScanOrderInsertResponseModel>(0, $"Đơn hàng không đúng định dạng trong hệ thống", data);
            }
        }
        public Tuple<int, string> ScanOrderDelete(string storeNo, string orderNo, string userName, long id)
        {
            try
            {
                var ipServer = ServerIPConnection.GetIPServerByStore(storeNo);

                using (var db = new CentralSalesContainer(ipServer))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var result = db.Database.ExecuteSqlCommand("[dbo].[SP_SCAN_ORDER_DELETE] @OrderNo, @UserDelete,@ID ",
                    new SqlParameter("@OrderNo", orderNo),
                       new SqlParameter("@UserDelete", userName),
                         new SqlParameter("@ID", id)
                    );
                    if (result > 0)
                        return new Tuple<int, string>(1, $"Xóa thành công đơn hàng {orderNo}");
                    else
                        return new Tuple<int, string>(0, "Xóa thất bại");
                }
            }
            catch (Exception ex)
            {
                return new Tuple<int, string>(0, $"Xóa đơn hàng {orderNo} error {ex.Message}");
            }
        }
        public List<OptionModel> LoadPaymentTypeForEWalletList()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.OptionDatas
                                where a.Caption == "PAYMENT" && a.Status == true
                                select new OptionModel
                                {
                                    Value = a.Code,
                                    Text = a.PaymentTypeName
                                }).OrderBy(x => x.Value).ToList();
                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionModel>();
            }
        }
        public List<IPAddressPOSTerminalByStoreModel> LoadPOSTerminalByStoreSetupPrint(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.POSTerminals
                                where a.StoreNo == storeNo && a.Placement !="POSWEB" && a.Status == true
                                select new IPAddressPOSTerminalByStoreModel
                                {
                                    StoreNo = a.StoreNo,
                                    IpAddress = a.IPAddress,
                                    PosID = a.No,
                                    PosName = a.No
                                }).OrderBy(x => x.IpAddress).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<IPAddressPOSTerminalByStoreModel>();
                    }

                    var result = (from b in data
                                  select new IPAddressPOSTerminalByStoreModel
                                  {
                                      StoreNo = b.StoreNo,
                                      IpAddress = b.IpAddress,
                                      PosID = b.PosID,
                                      PosName = b.PosName
                                  }).ToList();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return new List<IPAddressPOSTerminalByStoreModel>();
            }
        }
        public List<IPAddressModel> GetIPAddressList(string storeNo, string IpAddress, string computerName, out int totalRecord, int skip, int take)
        {
            try
            {
                var result = new List<IPAddressModel>();
                List<string> filterSearch = new List<string>();
                var ListSearch = IpAddress.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();

                foreach (var item in ListSearch)
                {
                    filterSearch.Add(item.Trim().ToLower());
                }

                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSMonitors.AsQueryable();

                    if (!string.IsNullOrEmpty(storeNo))
                    {
                        data = data.Where(d => d.StoreNo == storeNo);
                    }
                    if (!string.IsNullOrEmpty(computerName))
                    {
                        data = data.Where(d => d.ComputerName.Contains(computerName));
                    }
                    if (!string.IsNullOrEmpty(IpAddress))
                    {
                        data = data.Where(a => filterSearch.Contains(a.IpAddress));
                    }

                    totalRecord = data.Count();
                    data = data.OrderByDescending(d => d.StoreNo).Skip(skip).Take(take);

                    result = data.Select(a => new IPAddressModel
                    {
                        StoreNo = a.StoreNo,
                        IpAddress = a.IpAddress,
                        PosTerminalID = a.PosTerminalID,
                        ComputerName = a.ComputerName
                    }).ToList();

                    return result;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return (default(List<IPAddressModel>));
            }
        }
        public List<StorePartnerModel> LoadComboxStoreNoByUser(string userName)
        {
            var result = new List<StorePartnerModel>();

            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;

                    var data = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new AdminUserModel
                    {
                        TypeLogin = a.TypeLogin,
                        UserName = a.UserName,
                        StaffCode = a.StaffCode,
                        StoreNo = a.StoreNo,
                        IsAllStore = a.IsAllStore
                    }).FirstOrDefault();

                    if (data != null)
                    {
                        if (data.IsAllStore == true)
                        {
                            result = db.Stores.Where(d => d.ClosingMethod == 0)
                                      .Select(a => new StorePartnerModel
                                      {
                                          SiteNo = a.No,
                                          SiteName = a.Name,
                                          StyleProfile = a.StyleProfile
                                      }).ToList();
                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(data.StoreNo);
                            result = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0)
                                      .Select(a => new StorePartnerModel
                                      {
                                          SiteNo = a.No,
                                          SiteName = a.Name,
                                          StyleProfile = a.StyleProfile
                                      }).ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<OptionModel> GetComboxMemberCardType()
        {
            var result = new List<OptionModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = (from a in db.OptionDatas.AsNoTracking()
                              where a.Caption == "GiftRedeem" && a.Status == true
                              select new OptionModel
                              {
                                  Value = a.Code,
                                  Text = a.Description
                              }).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<OptionDataBySalesTypeModel> LoadSalesTypeByMultipleCheck()
        {
            var result = new List<OptionDataBySalesTypeModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.SalesOrderTypes.Where(a => a.IsActive == true).Select(a => new OptionDataBySalesTypeModel
                    {
                        Code = a.Code,
                        Description = a.Description
                    }).ToList();
                }                
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public List<OptionDataModel> GetComboxOptionData(string caption)
        {
            var result = new List<OptionDataModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.OptionDatas.Where(a => a.Status == true && a.Caption == caption.Trim())
                        .Select(a => new OptionDataModel
                        {
                            Code = a.Code,
                            Description = a.Description,
                            Order = a.Order
                        }).OrderBy(d => d.Order).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public List<StorePartnerModel> LoadComboxStoreByUserChannel(string userName, string channelSales)
        {
            var result = new List<StorePartnerModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new AdminUserModel
                    {
                        TypeLogin = a.TypeLogin,
                        UserName = a.UserName,
                        StaffCode = a.StaffCode,
                        StoreNo = a.StoreNo,
                        IsAllStore = a.IsAllStore
                    }).FirstOrDefault();

                    if (data != null)
                    {
                        if (data.IsAllStore == true)
                        {
                            if (channelSales == null)
                            {
                                result = db.Stores.Where(d => d.ClosingMethod == 0)
                                     .Select(a => new StorePartnerModel
                                     {
                                         SiteNo = a.No,
                                         SiteName = a.Name,
                                         StyleProfile = a.StyleProfile
                                     }).ToList();
                            }
                            else
                            {
                                result = db.Stores.Where(d => d.ClosingMethod == 0 && d.StyleProfile == channelSales)
                                      .Select(a => new StorePartnerModel
                                      {
                                          SiteNo = a.No,
                                          SiteName = a.Name,
                                          StyleProfile = a.StyleProfile
                                      }).ToList();
                            }
                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(data.StoreNo);
                            result = db.Stores.Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0)
                                      .Select(a => new StorePartnerModel
                                      {
                                          SiteNo = a.No,
                                          SiteName = a.Name,
                                          StyleProfile = a.StyleProfile
                                      }).ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<OptionSiteModel> LoadComboxStoreNoByWinLife()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.StoreNoMappings.AsNoTracking()
                                join b in db.Stores.AsNoTracking() on a.StoreNoPLH equals b.No
                                where a.ModelType == "WIN_LIFE" && b.ClosingMethod == 0
                                select new OptionSiteModel
                                {
                                    StoreNoPLH = a.StoreNoPLH, // Ma CH PLH
                                    StoreNoWCM = a.StoreNoBlue,
                                    StoreNamePLH = b.Name,
                                    StoreNameWCM = string.Empty
                                }).OrderBy(x => x.StoreNoPLH).Distinct().ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<OptionSiteModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<OptionSiteModel>();
            }
        }

        // 29/03/2024 
        public List<OptionSiteModel> LoadComboxStoreNoWinLife(string userName)
        {
            var result = new List<OptionSiteModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.AdminUsers.Where(d => d.UserName == userName).Select(a => new AdminUserModel
                    {
                        TypeLogin = a.TypeLogin,
                        UserName = a.UserName,
                        StaffCode = a.StaffCode,
                        StoreNo = a.StoreNo,
                        IsAllStore = a.IsAllStore
                    }).FirstOrDefault();

                    if (data != null)
                    {
                        if (data.IsAllStore == true)
                        {
                            result = (from a in db.StoreNoMappings.AsNoTracking()
                                    join b in db.Stores.AsNoTracking() on a.StoreNoPLH equals b.No
                                    where a.ModelType == "WIN_LIFE" && b.ClosingMethod == 0
                                    select new OptionSiteModel
                                    {
                                        StoreNoPLH = a.StoreNoPLH,
                                        StoreNoWCM = a.StoreNoBlue,
                                        StoreNamePLH = b.Name,
                                        StoreNameWCM = string.Empty
                                    }).OrderBy(x => x.StoreNoPLH).Distinct().ToList();
                        }
                        else
                        {
                            var listStore = JsonConvert.DeserializeObject<List<string>>(data.StoreNo);
                            result = (from a in db.StoreNoMappings.Where(a => listStore.Contains(a.StoreNoPLH) && a.ModelType == "WIN_LIFE")
                                      join b in db.Stores.AsNoTracking() on a.StoreNoPLH equals b.No
                                      where b.ClosingMethod == 0
                                      select new OptionSiteModel
                                      {
                                            StoreNoPLH = a.StoreNoPLH,
                                            StoreNoWCM = a.StoreNoBlue,
                                            StoreNamePLH = b.Name,
                                            StoreNameWCM = string.Empty
                                      }).OrderBy(x => x.StoreNoPLH).Distinct().ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<VoucherTypeModel> LoadComboxVoucherType()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.VoucherTypes.AsNoTracking()
                                where a.IsVoucher == true && a.Enabled == true
                                select new VoucherTypeModel
                                {
                                    Value = a.Code,
                                    Text = a.VoucherName,
                                    Order = a.Order
                                }).OrderBy(x => x.Order).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<VoucherTypeModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<VoucherTypeModel>();
            }
        }
        public List<ArraySiteNoModel> LoadStoreByUserName(string ipServer, string storeNo, string userName)
        {
            var result = new List<ArraySiteNoModel>();
            try
            {
                var storeSet = new List<StoreSetServerModel>();
                var ipServerSet = string.IsNullOrEmpty(ipServer) ? ServerIPConnection.GetIPServerReadByStore(storeNo) : ipServer;
                using (var dbGeneral = new CentralGeneralContainer())
                {
                    dbGeneral.Database.CommandTimeout = 5 * 60;
                    storeSet = dbGeneral.StoreSetServers.AsNoTracking().Where(d => d.ServerIP == ipServerSet || d.ServerRead == ipServerSet).Select(a => new StoreSetServerModel
                    {
                        ServerIP = a.ServerIP,
                        ServerRead = a.ServerRead,
                        StoreNo = a.StoreNo,
                        Description = a.Description,
                        Status = a.Status
                    }).ToList();

                    if (storeSet != null && storeSet.Count > 0)
                    {
                        using (var db = new CentralMDPartnerContainer())
                        {
                            db.Database.CommandTimeout = 5 * 60;
                            var data = db.AdminUsers.AsNoTracking().Where(d => d.UserName == userName && d.IsActive == true).Select(a => new ArraySiteNoModel
                            {
                                SiteNo = a.StoreNo,
                                Permission = a.IsAllStore,
                                RoleCode = a.RoleCode,
                                IsActive = a.IsActive
                            }).FirstOrDefault();

                            if (data != null)
                            {
                                if (data.Permission == true)  // IsAllStore = 1
                                {
                                    var storePermision = db.Stores.AsNoTracking().Where(a => a.ClosingMethod == 0).ToList();
                                    result = (from a in storePermision
                                              join b in storeSet on a.No equals b.StoreNo
                                              join c in dbGeneral.SQLServers on b.ServerIP equals c.IPServer
                                              select new ArraySiteNoModel
                                              {
                                                  ServerIP = b.ServerIP,
                                                  ServerIPRead = b.ServerRead,
                                                  ServerStoreDesc = c.NameSvr,
                                                  SiteNo = a.No,
                                                  SiteName = a.Name,
                                                  StyleProfile = a.StyleProfile
                                              }).ToList();
                                }
                                else    // IsAllStore = 0
                                {
                                    var listStore = JsonConvert.DeserializeObject<List<string>>(data.SiteNo);
                                    var storePermision = db.Stores.AsNoTracking().Where(a => listStore.Contains(a.No) && a.ClosingMethod == 0).ToList();
                                    result = (from a in storePermision
                                              join b in storeSet on a.No equals b.StoreNo
                                              join c in dbGeneral.SQLServers on b.ServerIP equals c.IPServer
                                              select new ArraySiteNoModel
                                              {
                                                  ServerIP = b.ServerIP,
                                                  ServerIPRead = b.ServerRead,
                                                  ServerStoreDesc = c.NameSvr,
                                                  SiteNo = a.No,
                                                  SiteName = a.Name,
                                                  StyleProfile = a.StyleProfile
                                              }).ToList();
                                }
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
        public List<OptionDataModel> LoadComboxCupType()
        {
            var result = new List<OptionDataModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    result = db.OptionDatas.Where(a => a.Status == true && a.Caption == "CupType")
                        .Select(a => new OptionDataModel
                        {
                            Code = a.Code,
                            Description = a.Description,
                            Order = a.Order
                        }).OrderBy(d => d.Order).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public List<string> CheckSecurityStoreByUserName(string userName, string listStore)
        {
            var result = new List<string>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var data = db.AdminUsers.AsNoTracking().Where(d => d.UserName == userName && d.IsActive == true).Select(a => new ArraySiteNoModel
                    {
                        SiteNo = a.StoreNo,
                        Permission = a.IsAllStore,
                        RoleCode = a.RoleCode,
                        IsActive = a.IsActive
                    }).FirstOrDefault();

                    if (data != null)
                    {
                        var storeList = listStore.Split(new char[] { ';' }).Where(x => !string.IsNullOrEmpty(x)).ToList();
                        if (data.Permission == true) // quyền all ---> IsAllStore = 1
                        {
                            var data1 = db.Stores.AsNoTracking().Where(d =>d.ClosingMethod == 0).Select(a =>a.No).ToList();
                            var storePermision = data1.Intersect(storeList).ToList();
                            result = storePermision;
                        }
                        else // IsAllStore = 0
                        {
                            var storeAdminUser = JsonConvert.DeserializeObject<List<string>>(data.SiteNo);
                            var storePermision = storeAdminUser.Intersect(storeList).ToList();
                            result = storePermision;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new List<string>();
            }
            return result;
        }
        public List<OptionDataModel> LoadComboxMemberCodeType()
        {
            var result = new List<OptionDataModel>();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    result = db.OptionDatas.AsNoTracking().Where(x => x.Status == true && x.Caption == "MEMBERCODETYPE").Select(a => new OptionDataModel
                    {
                        Code = a.Code,
                        Description = a.Description,
                        Order = a.Order
                    }).OrderBy(x =>x.Order).ToList();
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }
        public StoreListSpecialComboModel LoadStoreByUpdateSpecialCombo(string channel, string code)
        {
            var result = new List<StorePartnerModel>();
            var result1 = new List<StorePartnerModel>();
            var listData = new StoreListSpecialComboModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    if (channel == null)
                    {
                        result = db.Stores.Where(d => d.ClosingMethod == 0)
                             .Select(a => new StorePartnerModel
                             {
                                 SiteNo = a.No,
                                 SiteName = a.Name,
                                 StyleProfile = a.StyleProfile
                             }).ToList();
                    }
                    else
                    {
                        result = db.Stores.Where(d => d.ClosingMethod == 0 && d.StyleProfile == channel)
                              .Select(a => new StorePartnerModel
                              {
                                  SiteNo = a.No,
                                  SiteName = a.Name,
                                  StyleProfile = a.StyleProfile
                              }).ToList();
                    }

                    var listStoreCombo = db.SpecialComboStores.AsNoTracking().Where(x => x.Code == code).Select(x =>x.StoreNo).ToList();
                    result1 = result.Where(d => listStoreCombo.Contains(d.SiteNo)).ToList();

                    listData = new StoreListSpecialComboModel
                              {
                                    StoreList = result,
                                    StoreListSelected = result1                                
                              };
                }
            }
            catch (Exception ex)
            {
            }
            return listData;
        }

        


    }
}
