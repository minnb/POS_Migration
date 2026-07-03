using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using VCM.BLUEPOS.Common.Helpers;
using VCM.BLUEPOS.Data.EF.Central;
using VCM.BLUEPOS.Data.EF;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.POS;
using VCM.BLUEPOS.Model.Bank;
using VCM.BLUEPOS.Model.Voucher;
using VCM.BLUEPOS.Model.MasterData;


namespace VCM.BLUEPOS.Data.MasterData
{
    public interface IMasterIData
    {
        List<EmployeeListResponseModel> GetEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportEmployeeListModel> ExportEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status);
        ResultResponseModel CreateEmployeeList(CreateEmployeeListModel req);
        ResultResponseModel UpdateEmployeeList(UpdateEmployeeListModel req);
        ResultResponseModel DeleteEmployeeList(DeleteEmployeeListModel req);
        List<SetupPOSListResponseModel> GetSetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        ResultResponseModel CreatePOSTerminalList(CreatePOSTerminalListModel req);
        ResultResponseModel UpdatePOSTerminalList(UpdatePOSTerminalListModel req);
        ResultResponseModel DeletePOSterminalList(DeletePOSTerminalListModel req);
        string GetMaxPosIDPOSTerminal(string storeNo);
        List<ExportSetupPOSListModel> ExportExcel_SetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status);
        List<StoreListResponseModel> GetStoreList(string provinceNo, string chanelNo, string storeNo, string taxCode, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelStoreListModel> ExportExcel_StoreList(string provinceNo, string chanelNo, string storeNo, string taxCode);
        ResultResponseModel DeleteStoreList(DeleteStoreListModel req);
        ResultResponseModel UpdateStoreList(UpdateStoreListModel req);
        ResultResponseModel CreateStoreList(CreateStoreListModel req);
        Tuple<bool, CheckStoreListModel> CheckStoreNo(string storeNo);
        List<BankListResponseModel> GetBankList(string bankCode, string bankName, out int totalRecord, int skip = 0, int take = 100);
        ResultResponseModel DeleteBankList(DeleteBankModel req);
        ResultResponseModel CreateBankList(CreateBankModel req);
        ResultResponseModel UpdateBankList(UpdateBankModel req);
        List<ExportBankListModel> ExportGetBankList(string BankCode, string BankName);
        ResultResponseModel DeleteBankPOSList(DeleteBankPOSListModel req);
        ResultResponseModel CreateBankPOSList(CreateBankPOSListModel req);
        ResultResponseModel UpdateBankPOSList(UpdateBankPOSListModel req);
        List<BankPOSListResponseModel> GetBankPOSList(string storeNo, string textSearch, string bankCode, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        string GetBankPOSCode(string storeNo, string bankCode, string posNo);
        string GetBankPOSName(string bankCode);
        List<ExportBankPOSListModel> ExportBankPOSList(string storeNo, string bankCode, string status, string textSearch);
        Tuple<int, string> ImportExcelBankPOS(string userName, DataTable dt);
        List<POSVersionResponseModel> GetPOSVersionList(out int totalRecord, int skip = 0, int take = 100);
        ResultResponseModel UpdatePOSVersionList(UpdatePOSVersionModel req);
        List<ServerInfoPOSDataSetupModel> GetServerInfoPOSDataSetupList();
        List<POSDocumentNosResponseModel> GetVoucherNumberToPOSList(string voucherType, string storeNo, string posID, string ipAddress, out int totalRecord, int skip, int take);
        List<DocumentNoResponseModel> GetDocumentNoList(string storeNo, string posID, string voucherType, out int totalRecord, int skip, int take);
        ResultResponseModel UpdatePOSDocumentNo(UpdatePOSDocumentNoModel req);
        ResultResponseModel InsertPOSDocumentNo(InsertPOSDocumentNoModel req);
        List<ProvinceListResponseModel> GetProvinceList(string provinceCode, string provinceName, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel UpdateProvinceList(UpdateProvinceResponseModel req);
        ResultResponseModel CreateProvinceList(CreateProvinceResponseModel req);
        ResultResponseModel DeleteProvinceList(DeleteProvinceResponseModel req);
        Tuple<bool, CheckProvinceResponseModel> CheckProvinceCode(string provinceCode);
        List<SalesOrderTypeListModel> GetSalesOrderTypeList(string salesType, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel CreateSalesOrderType(CreateSalesOrderTypeModel req);
        TenderTypeList LoadTenderType(float percent);
        ResultResponseModel DeleteSalesOrderType(DeleteSalesOrderTypeModel req);
        ResultResponseModel UpdateSalesOrderType(UpdateSalesOrderTypeModel req);
        List<BankCardTypeResponseModel> LoadBankCardType(string bankCardCode, string bankCardName, string status, out int totalRecord, int skip = 0, int take = 100);
        ResultResponseModel CreateBankCardType(CreateBankCardTypeModel req);
        ResultResponseModel UpdateBankCardType(UpdateBankCardTypeModel req);
        ResultResponseModel DeleteBankCardType(DeleteBankCardTypeModel req);
        List<EWalletListResponseModel> LoadSetupEWalletList(string storeNo, string eWalletType, string textSearch, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelEWalletListModel> ExportExcelSetupEWalletList(string storeNo, string eWalletType, string textSearch, string status);
        string GetEWalletTerminalCode(string storeNo, string eWalletCode, string posNo);
        ResultResponseModel CreateSetupEWallet(CreateEWalletListModel req);
        ResultResponseModel UpdateSetupEWallet(UpdateEWalletListModel req);
        ResultResponseModel DeleteSetupEWalletList(DeleteEWalletListModel req);
        Tuple<int, string> ImportExcelPOSTerminal(string userName, DataTable dt);
        List<ChangePassWordModel> ChangePassWordList(string storeNo, string userName, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel ChangePassWord(ChangePassWordModel req);
        List<SetupImageSliderResponseModel> GetSetupImageSliderList(string styleProfile, string storeNo, string description, string fileName, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel CreateImageSlider(CreateSetupImageSliderModel req);
        Task<ResultResponseModel> CreateImageSliderV2Async(CreateSetupImageSliderModel req);
        ResultResponseModel UpdateImageSlider(UpdateSetupImageSliderModel req);
        List<ExportExcel_ImagesSliderListResponseModel> ExportExcel_ImagesSliderList(string storeNo, string styleProfile, string description, string fileName, string status);
        Task<ResultResponseModel> UpdateImageSliderV2Async(UpdateSetupImageSliderModel req);
        ResultResponseModel DeleteImageSlider(DeleteSetupImageSliderModel req);
        UpdateSetupImageSliderModel ViewInfoImagesSlider(string Id, string pkey);
        List<SetupCurrencyRateListModel> GetSetupCurrencyRateList(string UnitCurrency, string Status, string ExchangeRate, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel CreateCurrencyRate(CreateCurrencyRateModel req);
        ResultResponseModel UpdateCurrencyRate(UpdateCurrencyRateModel req);
        List<QRInformationModel> GetInformationQRList(DateTime fromDate, DateTime toDate, string storeNo, string posID, string textSearch, string status, string contentType, out int totalRecord, int pageIndex = 0, int pageSize = 50);
        ResultResponseModel CreateInformationQRList(CreateInformationQRModel req);
        ResultResponseModel UpdateQRInformationList(UpdateInformationQRModel req);
        ResultResponseModel DeleteQRInformationList(DeleteInformationQRModel req);
        List<AdminUserLoginPOSResponseModel> GetUserLoginPOSWebList(string storeNo, string posTerminal, string typePOS, string textSearch, out int totalRecord, int pageIndex, int pageSize);
        List<ExportAdminUserLoginPOSResponseModel> ExportExcel_UserLoginPOSWebList(string storeNo, string posTerminal, string typePOS, string textSearch);
        ResultResponseModel DeleteUserLoginPOSWeb(DeleteAdminUserLoginPOSWebModel req);
        List<SetupPrintByPOSWebResponseModel> SetupPrintByPOSWebList(string ipAddress, string storeNo, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportEExcelSetupPrintByPOSWebModel> ExportExcel_SetupPrintByPOSWeb(string ipAddress, string storeNo, string status);
        ResultResponseModel CreateSetupPrintByPOSWeb(CreateSetupPrintByPOSWebModel req);
        ResultResponseModel UpdateSetupPrintByPOSWeb(UpdateSetupPrintByPOSWebModel req);
        ResultResponseModel DeleteSetupPrintByPOSWeb(DeleteSetupPrintByPOSWebModel req);
        Task<ResultResponseModel> CreateImageSlider_V2(CreateSetupImageSliderModel req);
        Task<ResultResponseModel> UpdateImageSlider_V2(UpdateSetupImageSliderModel req);
        string GetDescriptionImageSlider(string description);
        ResultResponseModel SaveImageSliderByBlock(ImageSliderBlockModel model);

    }

    public class MasterIData : IMasterIData
    {
        public List<EmployeeListResponseModel> GetEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<EmployeeListResponseModel>("[dbo].[GetEmployeeList] @StaffCode, @StaffName, @StoreNo, @TypeGroup, @Status, @PageSize, @PageNumber",
                        new SqlParameter("@StaffCode", staffCode ?? string.Empty),
                        new SqlParameter("@StaffName", staffName ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TypeGroup", typeGroup ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();
                    if (data == null || data.Count == 0)
                    {
                        return new List<EmployeeListResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<EmployeeListResponseModel>();
            }
        }

        public List<ExportEmployeeListModel> ExportEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportEmployeeListModel>("[dbo].[GetEmployeeList_Export] @StaffCode, @StaffName, @StoreNo, @TypeGroup, @Status",
                        new SqlParameter("@StaffCode", staffCode ?? string.Empty),
                        new SqlParameter("@StaffName", staffName ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TypeGroup", typeGroup ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportEmployeeListModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportEmployeeListModel>();
            }
        }

        public ResultResponseModel CreateEmployeeList(CreateEmployeeListModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var checkStaff = db.Staffs.Any(x => x.ID == req.StaffCode);
                    if (checkStaff)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Mã nhân viên: {req.StaffCode} này đã tồn tại. Vui lòng kiểm tra lại"
                        };
                    }

                    var maxCounter = db.Staffs.Max(x => x.Counter);
                    var item = new EF.Central.Staff
                    {
                        ID = req.StaffCode,
                        Password = req.PassWord,
                        StoreNo = req.StoreNo,
                        VoidTransaction = Convert.ToInt32(req.VoidTransaction),
                        FirstName = req.FirstName,
                        LastName = req.LastName,
                        EmploymentType = Convert.ToInt32(req.TypeGroup),
                        Blocked = byte.Parse(req.Status),
                        PermissionGroup = req.PermissionGroup,
                        LastDateModified = DateTime.Now,
                        Counter = Convert.ToInt32(maxCounter) + 1,
                        Pkey = req.StaffCode
                    };

                    db.Staffs.Add(item);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Item1 = req.StaffCode,
                        Item2 = req.FirstName,
                        Item3 = req.Status,
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message
                    };
                }
            }
        }

        public ResultResponseModel UpdateEmployeeList(UpdateEmployeeListModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var data = db.Staffs.FirstOrDefault(x => x.ID == req.StaffCode);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            IsStatus = 0,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var maxCounter = db.Staffs.Max(x => x.Counter);

                    data.FirstName = req.FirstName;
                    data.LastName = req.LastName;
                    data.Password = req.PassWord;
                    data.StoreNo = req.StoreNo;
                    data.VoidTransaction = Convert.ToInt32(req.VoidTransaction);
                    data.EmploymentType = Convert.ToInt32(req.TypeGroup);
                    data.Blocked = byte.Parse(req.Status);
                    data.PermissionGroup = req.PermissionGroup;
                    data.HomePhoneNo = req.HomePhoneNo;
                    data.LastDateModified = DateTime.Now;
                    data.Counter = Convert.ToInt32(maxCounter) + 1;

                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Item1 = req.StaffCode,
                        Item2 = req.FirstName,
                        Item3 = req.PassWord,
                        Item4 = req.StoreNo,
                        Item5 = req.VoidTransaction,
                        Item6 = req.TypeGroup,
                        Item7 = req.PermissionGroup,
                        Item8 = req.Status,
                        IsStatus = 1,
                        Message = "Cập nhật thành công"
                    };

                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message
                    };
                }
            }
        }

        public ResultResponseModel DeleteEmployeeList(DeleteEmployeeListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var data = db.Staffs.FirstOrDefault(x => x.ID.Trim().ToLower() == req.StaffCode.Trim().ToLower());
                    if (data == null)
                    {
                        return new ResultResponseModel { Status = Model.Enums.ResultEnum.Fail, Message = "Không tìm thấy dữ liệu" };
                    }

                    db.Staffs.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel { Status = Model.Enums.ResultEnum.Success, Message = "Xóa nhân viên thành công" };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel { Status = Model.Enums.ResultEnum.ErrorSystem, Message = ex.Message };
            }
        }

        public List<SetupPOSListResponseModel> GetSetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<SetupPOSListResponseModel>("[dbo].[GetPOSTerminalList] @StoreNo, @IPAddress, @POSID, @ChanelNo, @POSType, @Status, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@IPAddress", ipAddress == null ? string.Empty : ipAddress ?? string.Empty),
                        new SqlParameter("@POSID", posID == null ? string.Empty : posID ?? string.Empty),
                        new SqlParameter("@ChanelNo", chanelNo ?? string.Empty),
                        new SqlParameter("@POSType", posType ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Exp", 1),  // Không export
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SetupPOSListResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception)
            {
                totalRecord = 0;
                return new List<SetupPOSListResponseModel>();
            }
        }

        public List<ExportSetupPOSListModel> ExportExcel_SetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportSetupPOSListModel>("[dbo].[GetPOSTerminalList] @StoreNo, @IPAddress, @POSID, @ChanelNo, @POSType, @Status, @Exp, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@IPAddress", ipAddress ?? string.Empty),
                        new SqlParameter("@POSID", posID ?? string.Empty),
                        new SqlParameter("@ChanelNo", chanelNo ?? string.Empty),
                        new SqlParameter("@POSType", posType ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Exp", 2),  // export excel
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportSetupPOSListModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportSetupPOSListModel>();
            }
        }

        public string GetMaxPosIDPOSTerminal(string storeNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _listStoreNo = db.POSTerminals.Where(x => x.StoreNo == storeNo.Trim()).ToList();

                    // Case : POSID chua co dong nao : POSID = NO + "01"
                    if (_listStoreNo.Count == 0)
                    {
                        var _maxno = storeNo.ToString() + "01";
                        return _maxno;
                    }

                    // Case : POSID da ton tai thi POSID = Max(No) + 1 , POSID luôn là 6 chữ số
                    var _maxNo = _listStoreNo.Where(x => x.No.Length == 6).Max(x => x.No);
                    var posNo = _maxNo.Substring(_maxNo.Length - 2, 2);
                    var posNoNumber = Convert.ToInt32(posNo) + 1;
                    var posID = "";

                    if (posNoNumber < 10)
                    {
                        posID = $"{"0"}{posNoNumber}";
                    }
                    else
                    {
                        if (posNoNumber > 99)
                        {
                            posID = "99";
                        }
                        else
                        {
                            posID = Convert.ToString(posNoNumber);
                        }
                    }
                    var data = $"{storeNo}{posID}";
                    return data;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public ResultResponseModel CreatePOSTerminalList(CreatePOSTerminalListModel req)
        {
            try
            {   
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSTerminals.Select(x => x.No).FirstOrDefault();
                    var styleProfile = db.Stores.FirstOrDefault(x => x.No == req.StoreNo)?.StyleProfile;

                    if (req.SalesChanelType == "POSWEB") // Lay theo IPAdress của CentralSales
                    {
                        using (var dbGeneral = new CentralGeneralContainer())
                        {
                            var _ipPOSWEB = dbGeneral.StoreSetServers.Where(x =>x.StoreNo == req.StoreNo).FirstOrDefault()?.ServerIP;
                            req.IPAddress = _ipPOSWEB;
                        }
                    }

                    if (data == null)
                    {
                        var item1 = new POSTerminal
                        {
                            IPAddress = req.IPAddress,
                            No = req.POSID,
                            StoreNo = req.StoreNo,
                            Placement = (req.SalesChanelType == "POSWEB") ? "POSWEB": default(string), // mac dinh = NULL
                            Status = true,
                            TerminalNetworkID = $"{req.StoreNo}{"/"}{req.POSID}",
                            DefaultSalesType = req.DefaultSalesType,
                            StyleProfile = styleProfile,
                            CustomerDisplayText1 = req.CustomerDisplayText1,
                            Counter = 1,
                            Pkey = req.StoreNo,
                            CreatedDate = DateTime.Now,
                            CreatedBy = req.CreatedBy,
                            UpdatedDate = DateTime.Now,
                            UpdatedBy = req.CreatedBy
                        };

                        db.POSTerminals.Add(item1);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.IPAddress,
                            Item2 = req.DefaultSalesType,
                            Item3 = req.CustomerDisplayText1,
                            Item4 = req.POSID,
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        var checkPosID = db.POSTerminals.FirstOrDefault(x => x.No == req.POSID);
                        var checkIP = db.POSTerminals.FirstOrDefault(y => y.IPAddress == req.IPAddress);
                        if (checkPosID != null)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"Mã máy POS: {req.POSID} đã tồn tại trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }

                        if (checkIP != null && req.SalesChanelType !="POSWEB")
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"Địa chỉ IP: {req.IPAddress} của máy POS: {checkIP.No} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }

                        var maxCounter = db.POSTerminals.Max(x => x.Counter) ?? 0;
                        var item2 = new POSTerminal
                        {
                            IPAddress = req.IPAddress,
                            No = req.POSID,
                            StoreNo = req.StoreNo,
                            Placement = (req.SalesChanelType == "POSWEB") ? "POSWEB": default(string), // mac dinh = NULL
                            Status = true, // mac đinh co hieu luc khi them moi
                            TerminalNetworkID = $"{req.StoreNo}{"/"}{req.POSID}",
                            DefaultSalesType = req.DefaultSalesType,
                            StyleProfile = styleProfile,
                            CustomerDisplayText1 = req.CustomerDisplayText1,
                            Counter = maxCounter + 1,
                            Pkey = req.StoreNo,
                            CreatedDate = DateTime.Now,
                            CreatedBy = req.CreatedBy,
                            UpdatedDate = DateTime.Now,
                            UpdatedBy = req.CreatedBy
                        };

                        db.POSTerminals.Add(item2);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.IPAddress,
                            Item2 = req.DefaultSalesType,
                            Item3 = req.CustomerDisplayText1,
                            Item4 = req.POSID,
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới máy POS thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdatePOSTerminalList(UpdatePOSTerminalListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSTerminals.FirstOrDefault(x => x.No == req.POSID && x.StoreNo == req.StoreNo);
                    var checkStyleProfile = db.Stores.FirstOrDefault(y => y.No == req.StoreNo);
                    var checkIPNew = db.POSTerminals.FirstOrDefault(y => y.IPAddress == req.IPAddress);
                    var checkIPOld = data.IPAddress;

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 3,
                            Flag = 3,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    if (req.SalesChanelType != "POSWEB")
                    {
                        if (checkIPNew == null)
                        {
                            // Xóa máy POS có IP cũ, và cập nhật IP mới cho máy POS
                            var ipOld = db.POSTerminals.Where(x => x.IPAddress == checkIPOld && x.No == data.No && x.StoreNo == data.StoreNo).ToList();
                            foreach (var ip in ipOld)
                            {
                                db.POSTerminals.Remove(ip);
                                db.SaveChanges();
                            }

                            // Thêm mới IP
                            var maxCounter = db.POSTerminals.Max(x => x.Counter) ?? 0;                           
                            var itemInsert = new POSTerminal
                            {
                                IPAddress = req.IPAddress,
                                No = req.POSID,
                                StoreNo = req.StoreNo,
                                Status = req.Status,
                                TerminalNetworkID = $"{req.StoreNo}{"/"}{req.POSID}",
                                DefaultSalesType = req.DefaultSalesType,
                                StyleProfile = checkStyleProfile.StyleProfile,
                                CustomerDisplayText1 = req.CustomerDisplayText1,
                                Counter = maxCounter + 1,
                                Pkey = req.StoreNo,
                                CreatedDate = DateTime.Now,
                                CreatedBy = req.UpdatedBy,
                                UpdatedDate = DateTime.Now,
                                UpdatedBy = req.UpdatedBy
                            };

                            db.POSTerminals.Add(itemInsert);
                            db.SaveChanges();
                            return new ResultResponseModel
                            {
                                Item1 = req.IPAddress,
                                Item2 = req.DefaultSalesType,
                                Item3 = req.CustomerDisplayText1,
                                Item4 = Convert.ToString(req.Status),
                                Status = Model.Enums.ResultEnum.Success,
                                IsStatus = 1,
                                Flag = 1,
                                Message = "Cập nhật thành công"
                            };

                        }
                        else // IP mới = IP cũ thì giữ nguyên IP cũ, và chỉ cập nhật các trường khác
                        {
                            var maxCounter = db.POSTerminals.Max(x => x.Counter) ?? 0;
                            data.DefaultSalesType = req.DefaultSalesType;
                            data.Status = req.Status;
                            data.CustomerDisplayText1 = req.CustomerDisplayText1;
                            data.Counter = maxCounter + 1;
                            data.UpdatedDate = DateTime.Now;
                            data.UpdatedBy = req.UpdatedBy;

                            db.SaveChanges();
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Item1 = req.IPAddress,
                                IsStatus = 1,
                                Flag = 1,
                                Message = $"Cập nhật thành công"
                            };
                        }
                    }
                    else  // Loai = POSWEB : khong cap nhat Store / May POS / Loại / Kenh / IP
                    {
                        // Update
                        var maxCounter = db.POSTerminals.Max(x => x.Counter) ?? 0;
                        data.DefaultSalesType = req.DefaultSalesType;           // Quyen
                        data.Status = req.Status;                               // Trang thai
                        data.CustomerDisplayText1 = req.CustomerDisplayText1;   // Loi chao
                        data.Counter = maxCounter + 1;
                        data.UpdatedDate = DateTime.Now;
                        data.UpdatedBy = req.UpdatedBy;

                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Item1 = req.IPAddress,
                            IsStatus = 1,
                            Flag = 1,
                            Message = $"Cập nhật thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeletePOSterminalList(DeletePOSTerminalListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSTerminals.FirstOrDefault(x => x.No.Trim().ToLower() == req.POSID.Trim().ToLower());
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.POSTerminals.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa máy POS thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public Tuple<int, string> ImportExcelPOSTerminal(string userName, DataTable dt)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {                   
                    if (dt.Rows.Count > 0)
                    {
                        for (int y = 0; y < dt.Rows.Count; y++)
                        {
                            if (dt.Rows[y]["Placement"] == null || dt.Rows[y]["Placement"].ToString() == "")
                            {
                                if (dt.Rows[y]["IPAddress"] == null || dt.Rows[y]["IPAddress"].ToString() == "")
                                {
                                    return new Tuple<int, string>(0, $"Cột [IPAddress] đang bị null/rỗng");
                                }
                            }

                            if (dt.Rows[y]["No"] == null || dt.Rows[y]["No"].ToString() == "")
                            {
                                return new Tuple<int, string>(0, $"Cột [No] đang bị null/rỗng");
                            }

                            if (dt.Rows[y]["StoreNo"] == null || dt.Rows[y]["StoreNo"].ToString() == "")
                            {
                                return new Tuple<int, string>(0, $"Cột [StoreNo] đang bị null/rỗng");
                            }

                            if (dt.Rows[y]["StyleProfile"] == null || dt.Rows[y]["StyleProfile"].ToString() == "")
                            {
                                return new Tuple<int, string>(0, $"Cột [StyleProfile] đang bị null/rỗng");
                            }

                            if (dt.Rows[y]["DefaultSalesType"] == null || dt.Rows[y]["DefaultSalesType"].ToString() == "")
                            {
                                return new Tuple<int, string>(0, $"Cột [DefaultSalesType] đang bị null/rỗng");
                            }

                            if (dt.Rows[y]["CustomerDisplayText1"] == null || dt.Rows[y]["CustomerDisplayText1"].ToString() == "")
                            {
                                return new Tuple<int, string>(0, $"Cột [CustomerDisplayText1] đang bị null/rỗng");
                            }

                            var excel_Placement = dt.Rows[y]["Placement"].ToString(); 
                            var excel_IPAddress = dt.Rows[y]["IPAddress"].ToString().Trim();
                            var excel_No = dt.Rows[y]["No"].ToString().Trim();
                            var excel_StoreNo = dt.Rows[y]["StoreNo"].ToString().Trim();
                            var excel_StyleProfile = dt.Rows[y]["StyleProfile"].ToString().Trim().ToUpper();
                            var excel_DefaultSalesType = dt.Rows[y]["DefaultSalesType"].ToString().Trim().ToUpper();
                            var excel_CustomerDisplayText1 = dt.Rows[y]["CustomerDisplayText1"].ToString().Trim();

                            if (excel_Placement == "POSWEB") // Khai báo POS WEB
                            {
                                var checkNo = db.POSTerminals.FirstOrDefault(d => d.No == excel_No);
                                var maxCounter = db.POSTerminals.Max(x => x.Counter) ?? 0;

                                // Lấy IP theo Set Sever
                                var IPAdressSetServer = "";

                                using (var dbGeneral = new CentralGeneralContainer())
                                {
                                    var IPAdress = dbGeneral.StoreSetServers.Where(x =>x.StoreNo == excel_StoreNo).FirstOrDefault()?.ServerIP;
                                    IPAdressSetServer = IPAdress;
                                }
                                
                                if (checkNo == null)   // Insert
                                {
                                    db.POSTerminals.Add(new POSTerminal
                                    {
                                        IPAddress = IPAdressSetServer,
                                        No = excel_No,
                                        StoreNo = excel_StoreNo,
                                        Placement = excel_Placement.ToUpper().Trim(),
                                        TerminalNetworkID = $"{excel_StoreNo}/{excel_No}",
                                        CustomerDisplayText1 = excel_CustomerDisplayText1,
                                        DefaultSalesType = excel_DefaultSalesType.ToUpper().Trim(),
                                        StyleProfile = excel_StyleProfile.ToUpper().Trim(),
                                        Counter = maxCounter + 1,
                                        Pkey = excel_StoreNo,
                                        CreatedDate = DateTime.Now,
                                        CreatedBy = userName,
                                        UpdatedDate = DateTime.Now,
                                        UpdatedBy = userName,
                                        Status = true
                                    });
                                }
                                else   // update
                                {
                                    checkNo.IPAddress = IPAdressSetServer;
                                    checkNo.StoreNo = excel_StoreNo;
                                    checkNo.Placement = excel_Placement.ToUpper().Trim();
                                    checkNo.TerminalNetworkID = $"{excel_StoreNo}/{excel_No}";
                                    checkNo.CustomerDisplayText1 = excel_CustomerDisplayText1;
                                    checkNo.DefaultSalesType = excel_DefaultSalesType.ToUpper().Trim();
                                    checkNo.StyleProfile = excel_StyleProfile.ToUpper().Trim();
                                    checkNo.Counter = maxCounter + 1;
                                    checkNo.Pkey = excel_StoreNo;
                                    checkNo.UpdatedDate = DateTime.Now;
                                    checkNo.UpdatedBy = userName;
                                    checkNo.Status = true;
                                }

                                db.SaveChanges();
                            }
                            else  // POS PC
                            {
                                // Check IP trùng
                                var checkIP = db.POSTerminals.FirstOrDefault(x => x.IPAddress.ToString() == excel_IPAddress);

                                if (checkIP != null)
                                {
                                    return new Tuple<int, string>(0, $"Địa chỉ IP {checkIP.IPAddress} này đã có trong hệ thống. Vui lòng kiểm tra lại");
                                }

                                var checkNo = db.POSTerminals.FirstOrDefault(d => d.No == excel_No);
                                var maxCounter = db.POSTerminals.Max(x => x.Counter) ?? 0;

                                if (checkNo == null)   // Insert
                                {
                                    db.POSTerminals.Add(new POSTerminal
                                    {
                                        IPAddress = excel_IPAddress,
                                        No = excel_No,
                                        StoreNo = excel_StoreNo,
                                        TerminalNetworkID = $"{excel_StoreNo}/{excel_No}",
                                        CustomerDisplayText1 = excel_CustomerDisplayText1,
                                        DefaultSalesType = excel_DefaultSalesType.ToUpper().Trim(),
                                        StyleProfile = excel_StyleProfile.ToUpper().Trim(),
                                        Counter = maxCounter + 1,
                                        Pkey = excel_StoreNo,
                                        CreatedDate = DateTime.Now,
                                        CreatedBy = userName,
                                        UpdatedDate = DateTime.Now,
                                        UpdatedBy = userName,
                                        Status = true
                                    });
                                }
                                else   // update
                                {
                                    checkNo.IPAddress = excel_IPAddress;
                                    checkNo.StoreNo = excel_StoreNo;
                                    checkNo.TerminalNetworkID = $"{excel_StoreNo}/{excel_No}";
                                    checkNo.CustomerDisplayText1 = excel_CustomerDisplayText1;
                                    checkNo.DefaultSalesType = excel_DefaultSalesType.ToUpper().Trim();
                                    checkNo.StyleProfile = excel_StyleProfile.ToUpper().Trim();
                                    checkNo.Counter = maxCounter + 1;
                                    checkNo.Pkey = excel_StoreNo;
                                    checkNo.UpdatedDate = DateTime.Now;
                                    checkNo.UpdatedBy = userName;
                                    checkNo.Status = true;
                                }

                                db.SaveChanges();
                            }                           
                        }
                    }
                    return new Tuple<int, string>(dt.Rows.Count, "Import danh sách thành công");
                }
                catch (Exception ex)
                {
                    return new Tuple<int, string>(0, "Lỗi hệ thống, Import danh sách thất bại");
                }
            }

        }

        public List<StoreListResponseModel> GetStoreList(string provinceNo, string chanelNo, string storeNo, string taxCode, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<StoreListResponseModel>("[dbo].[GetStoreList] @ProvinceNo, @ChanelNo, @StoreNo, @TaxCode, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@ProvinceNo", provinceNo ?? string.Empty),
                        new SqlParameter("@ChanelNo", chanelNo ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TaxCode", taxCode ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<StoreListResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<StoreListResponseModel>();
            }
        }

        public List<ExportExcelStoreListModel> ExportExcel_StoreList(string provinceNo, string chanelNo, string storeNo, string taxCode)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportExcelStoreListModel>("[dbo].[GetStoreList] @ProvinceNo, @ChanelNo, @StoreNo, @TaxCode, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@ProvinceNo", provinceNo ?? string.Empty),
                        new SqlParameter("@ChanelNo", chanelNo ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TaxCode", taxCode ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportExcelStoreListModel>));
            }
        }

        public ResultResponseModel CreateStoreList(CreateStoreListModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var checkStoreNo = db.Stores.Any(x => x.No == req.StoreNo);
                    if (checkStoreNo)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 2,
                            Message = $"Đã tồn tại ST/CH : {req.StoreNo} này trong hệ thống. Vui lòng kiểm tra lại"
                        };
                    }

                    // Case: Khong co ma cua hang muon tao trong Table Store, 
                    // thi cho them moi cua hang

                    var maxCounter = db.Stores.Max(x => x.Counter) ?? 0;

                    bool isVoucherOnline = false;
                    if (req.VoucherOnline == "0")
                    {
                        isVoucherOnline = false;
                    }
                    else if (req.VoucherOnline == "1")
                    {
                        isVoucherOnline = true;
                    }

                    var _fromTime = "1900-01-01" + " " + Convert.ToDateTime(req.OpenTime).TimeOfDay;
                    var _toTime = "1900-01-01" + " " + Convert.ToDateTime(req.CloseTime).TimeOfDay;

                    var insert = new EF.Central.Store
                    {
                        No = req.StoreNo,
                        IssueVoucherOnline = isVoucherOnline,
                        Name = req.StoreName,
                        Address = req.Address,
                        TaxCode = req.TaxCode,
                        StoreOpenfrom = Convert.ToDateTime(_fromTime),
                        StoreOpento = Convert.ToDateTime(_toTime),
                        PhoneNo = req.PhoneNo,
                        PasswordWifi = req.PassWordWifi,
                        LocationCode = req.ProvinceNo,
                        StyleProfile = req.StyleProfile,
                        LastDateModified = DateTime.Now,
                        ClosingMethod = 0,
                        CountryCode = "VN",
                        CurrencyCode = "VNĐ",
                        CustomerDefault = "999999999",
                        Counter = maxCounter + 1,
                        Pkey = req.StoreNo
                    };

                    db.Stores.Add(insert);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Item1 = req.ProvinceNo,
                        Item2 = req.StyleProfile,
                        Item3 = req.StoreNo,
                        Item4 = req.TaxCode,
                        Status = Model.Enums.ResultEnum.Success,
                        IsStatus = 1,
                        Message = "Thêm mới thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message,
                        IsStatus = 3
                    };
                }

            }
        }
        public ResultResponseModel UpdateStoreList(UpdateStoreListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Stores.FirstOrDefault(x => x.No == req.StoreNo);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var maxCounter = db.Stores.Max(x => x.Counter) ?? 0;

                    bool isVoucherOnline = false;
                    if (req.VoucherOnline == "0")
                    {
                        isVoucherOnline = false;
                    }
                    else if (req.VoucherOnline == "1")
                    {
                        isVoucherOnline = true;
                    }

                    // Update : table Store
                    // **** Khong cap nhat : ma cua hang, chuoi sieu thi - cua hang, tinh - thanh pho
                    // Ngay 04/01/2021 : Cho phep cap nhat Tỉnh / Thành phố ( LocationCode )

                    var _fromTime = "1900-01-01" + " " + Convert.ToDateTime(req.OpenTime).TimeOfDay;
                    var _toTime = "1900-01-01" + " " + Convert.ToDateTime(req.CloseTime).TimeOfDay;

                    data.Name = req.StoreName;
                    data.IssueVoucherOnline = isVoucherOnline;
                    data.StoreOpenfrom = Convert.ToDateTime(_fromTime);
                    data.StoreOpento = Convert.ToDateTime(_toTime);
                    data.Address = req.Address;
                    data.TaxCode = req.TaxCode;
                    data.PhoneNo = req.PhoneNo;
                    data.PasswordWifi = req.PassWordWifi;
                    data.LocationCode = req.ProvinceNo;
                    data.LastDateModified = DateTime.Now;
                    data.Counter = maxCounter + 1;

                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Item1 = req.ProvinceNo,
                        Item2 = req.StyleProfile,
                        Item3 = req.StoreNo,
                        Item4 = req.TaxCode,
                        IsStatus = 1,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public ResultResponseModel DeleteStoreList(DeleteStoreListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var data = db.Stores.FirstOrDefault(x => x.No == req.StoreNo);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.Stores.Remove(data);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa ST/CH thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public Tuple<bool, CheckStoreListModel> CheckStoreNo(string storeNo)
        {
            try
            {
                var listdata = new CheckStoreListModel();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Stores.FirstOrDefault(x => x.No == storeNo.Trim());
                    if (data != null)
                    {
                        return new Tuple<bool, CheckStoreListModel>(true, new CheckStoreListModel
                        {
                            Message = $"Mã Cửa hàng : {storeNo} này đã tồn tại trong hệ thống",
                            Status = true
                        });
                    }
                    var _storeNo = storeNo.Trim();
                    var model = new CheckStoreListModel
                    {
                        StoreNo = _storeNo,
                        Message = string.Empty,
                        Status = false
                    };
                    return new Tuple<bool, CheckStoreListModel>(false, model);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, CheckStoreListModel>(true, new CheckStoreListModel
                {
                    Message = $"Mã Cửa hàng : {storeNo} này đã tồn tại trong hệ thống",
                    Status = false
                });
            }
        }

        public List<BankListResponseModel> GetBankList(string bankCode, string bankName, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                var data = new List<BankModel>();
                var listdata = new List<BankListResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    if (string.IsNullOrEmpty(bankCode) && string.IsNullOrEmpty(bankName))
                    {
                        data = (from a in db.Banks
                                select new BankModel
                                {
                                    BankCode = a.BankCode,
                                    BankName = a.BankName,
                                    Counter = a.Counter,
                                    Pkey = a.Pkey,
                                    CreatedDate = a.CreatedDate,
                                    UpdatedDate = a.UpdatedDate
                                }).OrderBy(d => d.BankCode).ToList();
                    }
                    else
                    {
                        data = (from a in db.Banks
                                where a.BankCode == bankCode || a.BankName == bankName
                                select new BankModel
                                {
                                    BankCode = a.BankCode,
                                    BankName = a.BankName,
                                    Counter = a.Counter,
                                    Pkey = a.Pkey,
                                    CreatedDate = a.CreatedDate,
                                    UpdatedDate = a.UpdatedDate
                                }).OrderBy(d => d.BankCode).ToList();
                    }

                    totalRecord = data.Count();
                    var result = data.OrderBy(x => x.BankCode).Skip(skip).Take(take).ToList();
                    listdata = (from b in result
                                select new BankListResponseModel
                                {
                                    BankCode = b.BankCode,
                                    BankName = b.BankName,
                                    Counter = b.Counter,
                                    Pkey = b.Pkey,
                                    CreatedDateStr = String.Format("{0:dd/MM/yyyy}", b.CreatedDate),
                                    UpdatedDateStr = String.Format("{0:dd/MM/yyyy}", b.UpdatedDate)

                                }).OrderBy(x => x.BankCode).ToList();
                    return listdata;
                }
            }
            catch (Exception)
            {
                totalRecord = 0;
                return (default(List<BankListResponseModel>));
            }
        }

        public List<ExportBankListModel> ExportGetBankList(string BankCode, string BankName)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportBankListModel>("[dbo].[GetBankList_Export] @BankCode, @BankName",
                        new SqlParameter("@BankCode", BankCode ?? string.Empty),
                        new SqlParameter("@BankName", BankName ?? string.Empty)).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                return (default(List<ExportBankListModel>));
            }
        }

        public ResultResponseModel CreateBankList(CreateBankModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var totalRow = db.Banks.Select(x => x.BankCode).FirstOrDefault();

                    if (totalRow == null || totalRow == "")
                    {
                        var insert = new EF.Central.Bank
                        {
                            BankCode = req.BankCode.ToUpper(),
                            BankName = req.BankName,
                            Counter = 1,
                            Pkey = req.BankCode,
                            CreatedDate = DateTime.Now
                        };

                        db.Banks.Add(insert);
                        db.SaveChanges();

                        return new ResultResponseModel
                        {
                            Item1 = req.BankCode,
                            Item2 = req.BankName,
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        var checkBankCode = db.Banks.FirstOrDefault(x => x.BankCode == req.BankCode);

                        if (checkBankCode != null)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"Mã ngân hàng : {req.BankCode} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }

                        var maxCounter = db.Banks.Max(x => x.Counter) ?? 0;
                        var insert = new EF.Central.Bank
                        {
                            BankCode = req.BankCode.ToUpper(),
                            BankName = req.BankName,
                            Counter = Convert.ToInt32(maxCounter) + 1,
                            Pkey = req.BankCode,
                            CreatedDate = DateTime.Now
                        };

                        db.Banks.Add(insert);
                        db.SaveChanges();

                        return new ResultResponseModel
                        {
                            Item1 = req.BankCode,
                            Item2 = req.BankName,
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateBankList(UpdateBankModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Banks.FirstOrDefault(x => x.BankCode == req.BankCode.ToUpper());

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var maxCounter = db.Banks.Max(x => x.Counter) ?? 0;

                    data.BankName = req.BankName;
                    data.Counter = Convert.ToInt32(maxCounter) + 1;
                    data.UpdatedDate = DateTime.Now;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.BankCode,
                        Item2 = req.BankName,
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeleteBankList(DeleteBankModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var data = db.Banks.FirstOrDefault(x => x.BankCode.Trim().ToLower() == req.BankCode.Trim().ToLower());

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.Banks.Remove(data);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeleteBankPOSList(DeleteBankPOSListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var data = db.POSTerminalBanks.FirstOrDefault(x => x.BankPOSCode == req.BankPOSCode);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.POSTerminalBanks.Remove(data);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public List<BankPOSListResponseModel> GetBankPOSList(string storeNo, string textSearch, string bankCode, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<BankPOSListResponseModel>("[dbo].[GetBankPOSList] @StoreNo, @TextSearch, @BankCode, @Status, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@BankCode", bankCode ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<BankPOSListResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<BankPOSListResponseModel>();
            }
        }

        public List<ExportBankPOSListModel> ExportBankPOSList(string storeNo, string bankCode, string status, string textSearch)
        {
            try
            {
                var data = new List<ExportBankPOSListModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportBankPOSListModel>("[dbo].[GetBankPOSList] @StoreNo, @TextSearch, @BankCode, @Status, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@BankCode", bankCode ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportBankPOSListModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportBankPOSListModel>();
            }
        }
        public ResultResponseModel CreateBankPOSList(CreateBankPOSListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var totalRow = db.POSTerminalBanks.Select(x => x.BankPOSCode).FirstOrDefault();

                    if (totalRow == null || totalRow == "")
                    {
                        var item1 = new POSTerminalBank
                        {
                            BankPOSCode = req.BankPOSCode,
                            BankPOSName = req.BankPOSName,
                            BankCode = req.BankCode,
                            StoreNoFull = req.StoreNo,
                            StoreNo = req.StoreNo,
                            POSNo = req.POSNo,
                            AccessKey = req.AccessKey,
                            PartnerId = req.PartnerID,
                            IsOnline = req.IsOnline,
                            Status = req.Status,
                            Counter = 1,
                            Pkey = req.BankPOSCode,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser,
                            POSTerminal = req.POSTerminal
                        };
                        db.POSTerminalBanks.Add(item1);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.StoreNo,
                            Item2 = req.BankCode,
                            Item3 = Convert.ToString(req.Status),
                            Status = Model.Enums.ResultEnum.Success,
                            Flag = 1,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        var checkBankPOSCode = db.POSTerminalBanks.FirstOrDefault(x => x.BankPOSCode == req.BankPOSCode);
                        if (checkBankPOSCode != null)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Flag = 0,
                                Message = $"Đã tồn tại Mã máy cà thẻ: {req.BankPOSCode} này trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }

                        var checkAccessKey = db.POSTerminalBanks.FirstOrDefault(x => x.AccessKey == req.AccessKey)?.AccessKey;
                        var maxCounter = db.POSTerminalBanks.Max(x => x.Counter) ?? 0;
                        var item2 = new POSTerminalBank
                        {
                            BankPOSCode = req.BankPOSCode,
                            BankPOSName = req.BankPOSName,
                            BankCode = req.BankCode,
                            StoreNoFull = req.StoreNo,
                            StoreNo = req.StoreNo,
                            POSNo = req.POSNo,
                            AccessKey = req.AccessKey,
                            PartnerId = req.PartnerID,
                            IsOnline = req.IsOnline,
                            Status = req.Status,
                            Counter = maxCounter + 1,
                            Pkey = req.BankPOSCode,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser,
                            POSTerminal = req.POSTerminal
                        };

                        db.POSTerminalBanks.Add(item2);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.StoreNo,
                            Item2 = req.BankCode,
                            Item3 = Convert.ToString(req.Status),
                            Status = Model.Enums.ResultEnum.Success,
                            Flag = 1,
                            Message = "Thêm mới thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateBankPOSList(UpdateBankPOSListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.POSTerminalBanks.Where(x => x.BankPOSCode == req.BankPOSCode).FirstOrDefault();
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var maxCounter = db.POSTerminalBanks.Max(x => x.Counter) ?? 0;
                    data.POSTerminal = req.POSTerminal;
                    data.AccessKey = req.AccessKey;
                    data.PartnerId = req.PartnerID;
                    data.IsOnline = req.IsOnline;
                    data.Status = req.Status;
                    data.Counter = maxCounter + 1;
                    data.UpdatedDate = DateTime.Now;
                    data.UpdatedUser = req.UpdatedUser;
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.StoreNo,
                        Item2 = req.BankCode,
                        Item3 = Convert.ToString(req.Status),
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }
        public string GetBankPOSCode(string storeNo, string bankCode, string posNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkBankPOSCode = db.POSTerminalBanks.FirstOrDefault(x => x.StoreNo == storeNo && x.POSNo == posNo && x.BankCode == bankCode);
                    if (checkBankPOSCode != null) // Da ton tai BankPOSCode
                    {
                        return null;
                    }

                    var result = storeNo + "_" + bankCode + "_" + posNo;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public string GetBankPOSName(string bankCode)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Banks.FirstOrDefault(x => x.BankCode == bankCode);
                    if (data == null)           // Không có ngân hàng
                    {
                        return null;
                    }
                    var result = data.BankName;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        // Import Excel: BankPOS
        public Tuple<int, string> ImportExcelBankPOS(string userName, DataTable dt)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 5 * 60;
                try
                {
                    var dateTime = DateTime.Now;
                    if (dt.Rows.Count > 0)
                    {
                        for (int y = 0; y < dt.Rows.Count; y++)
                        {
                            if (dt.Rows[y]["BankPOSName"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột BankPOSName đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["BankCode"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột BankCode đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["StoreNo"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột StoreNo đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["POSNo"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSNo đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["POSTerminal"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSTerminal đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["IsOnline"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột IsOnline đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["AccessKey"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột AccessKey đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["PartnerID"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột PartnerID đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            var excel_BankPOSName = dt.Rows[y]["BankPOSName"].ToString().Trim();
                            var excel_BankCode = dt.Rows[y]["BankCode"].ToString().ToUpper().Trim();
                            var excel_StoreNo = dt.Rows[y]["StoreNo"].ToString().Trim();
                            var excel_POSNo = dt.Rows[y]["POSNo"].ToString().Trim();
                            var excel_POSTerminal = dt.Rows[y]["POSTerminal"].ToString().Trim();
                            var excel_IsOnline = dt.Rows[y]["IsOnline"].ToString().Trim();
                            var excel_AccessKey = dt.Rows[y]["AccessKey"].ToString().Trim();
                            var excel_PartnerID = dt.Rows[y]["PartnerID"].ToString().Trim();

                            if (string.IsNullOrEmpty(excel_BankPOSName))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột BankPOSName rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_BankCode))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột BankCode rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_StoreNo))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột StoreNo rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_POSNo))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSNo rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_POSTerminal))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSTerminal rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_IsOnline))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột IsOnline rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_AccessKey))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột AccessKey rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_PartnerID))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột PartnerID rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                        }

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            var excel_BankPOSName = dt.Rows[i]["BankPOSName"].ToString().Trim();
                            var excel_BankCode = dt.Rows[i]["BankCode"].ToString().ToUpper().Trim();
                            var excel_StoreNo = dt.Rows[i]["StoreNo"].ToString().Trim();
                            var excel_POSNo = dt.Rows[i]["POSNo"].ToString().Trim();
                            var excel_POSTerminal = dt.Rows[i]["POSTerminal"].ToString().Trim();
                            var excel_IsOnline = dt.Rows[i]["IsOnline"].ToString().Trim();
                            var excel_BankPOSCode = $"{excel_StoreNo}{"_"}{excel_BankCode}{"_"}{excel_POSNo}";
                            var excel_AccessKey = dt.Rows[i]["AccessKey"].ToString().Trim();
                            var excel_PartnerID = dt.Rows[i]["PartnerID"].ToString().Trim();

                            var checkBankPOSCode = db.POSTerminalBanks.FirstOrDefault(d => d.BankPOSCode == excel_BankPOSCode);
                            var _maxCounter = db.POSTerminalBanks.Max(x => x.Counter);

                            if (checkBankPOSCode == null)
                            {
                                db.POSTerminalBanks.Add(new POSTerminalBank
                                {
                                    BankPOSCode = excel_BankPOSCode,
                                    BankPOSName = excel_BankPOSName,
                                    BankCode = excel_BankCode,
                                    //StoreNoFull = $"{"00"}{excel_StoreNo}",
                                    StoreNoFull = $"{excel_StoreNo}",
                                    StoreNo = excel_StoreNo,
                                    POSNo = excel_POSNo,
                                    POSTerminal = excel_POSTerminal,
                                    IsOnline = excel_IsOnline == "0" ? false : true,
                                    Status = true,
                                    CreatedDate = dateTime,
                                    CreatedUser = userName,
                                    AccessKey = excel_AccessKey,
                                    PartnerId = excel_PartnerID,
                                    Counter = Convert.ToInt32(_maxCounter) + 1,
                                    Pkey = excel_BankPOSCode
                                });

                            }
                            else
                            {
                                checkBankPOSCode.BankPOSName = excel_BankPOSName;
                                checkBankPOSCode.BankCode = excel_BankCode;
                                //checkBankPOSCode.StoreNoFull = $"{"00"}{excel_StoreNo}";
                                checkBankPOSCode.StoreNoFull = $"{excel_StoreNo}";
                                checkBankPOSCode.StoreNo = excel_StoreNo;
                                checkBankPOSCode.POSNo = excel_POSNo;
                                checkBankPOSCode.POSTerminal = excel_POSTerminal;
                                checkBankPOSCode.IsOnline = excel_IsOnline == "0" ? false : true;
                                checkBankPOSCode.Status = true;
                                checkBankPOSCode.UpdatedDate = dateTime;
                                checkBankPOSCode.UpdatedUser = userName;
                                checkBankPOSCode.AccessKey = excel_AccessKey;
                                checkBankPOSCode.PartnerId = excel_PartnerID;
                                checkBankPOSCode.Counter = Convert.ToInt32(_maxCounter) + 1;
                                checkBankPOSCode.Pkey = excel_BankPOSCode;
                            }
                            db.SaveChanges();
                        }
                    }
                    return new Tuple<int, string>(dt.Rows.Count, "Import POSBank thành công");
                }
                catch (Exception ex)
                {
                    return new Tuple<int, string>(0, "Lỗi hệ thống, import POSBank không thành công");
                }
            }
        }

        public List<POSVersionResponseModel> GetPOSVersionList(out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = (from x in db.POSVersions
                                select new POSVersionModel
                                {
                                    LastVersion = x.LastVersion,
                                    CurVersion = x.CurVersion,
                                    UpdateTime = x.UpdateTime,
                                    Counter = x.Counter,
                                    Source = x.Source,
                                    Pkey = x.Pkey,
                                    IsUpdate = x.IsUpdate,
                                    Folder = x.Folder
                                }).OrderBy(x => x.LastVersion).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<POSVersionResponseModel>();
                    }
                    totalRecord = data.Count();
                    var result = data.OrderBy(x => x.Source).Skip(skip).Take(take).ToList();
                    var listData = (from a in result
                                    select new POSVersionResponseModel
                                    {
                                        LastVersion = a.LastVersion,
                                        CurVersion = a.CurVersion,
                                        UpdateTimeStr = a.UpdateTime == null ? string.Empty : a.UpdateTime.Value.ToString("dd/MM/yyyy"),
                                        //UpdateTimeStr = String.Format("{0:dd/MM/yyyy}", a.UpdateTime),
                                        Counter = a.Counter,
                                        Source = a.Source,
                                        Pkey = a.Pkey,
                                        IsUpdate = a.IsUpdate,
                                        Folder = a.Folder
                                    }).ToList();
                    return listData;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<POSVersionResponseModel>();
            }
        }

        public ResultResponseModel UpdatePOSVersionList(UpdatePOSVersionModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var data = db.POSVersions.FirstOrDefault(x => x.Source.Trim() == req.Source);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    if (data.LastVersion == req.LastVersion)
                    {
                        var maxCounter = db.POSVersions.Max(x => x.Counter);
                        if (req.IsUpdate == "true")
                        {
                            data.IsUpdate = true;
                        }
                        else
                        {
                            data.IsUpdate = false;
                        }

                        data.Counter = Convert.ToInt32(maxCounter) + 1;
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Cập nhật thành công"
                        };
                    }

                    // Update table POSVersion
                    // Cap nhat CurVersion truoc khi cap nhat LastVersion, CurVersion = LastVersion (cũ) truoc khi duoc cap nhat

                    var _maxCounter = db.POSVersions.Max(x => x.Counter);
                    data.CurVersion = data.LastVersion;
                    data.LastVersion = req.LastVersion;

                    if (req.IsUpdate == "true")
                    {
                        data.IsUpdate = true;
                    }
                    else
                    {
                        data.IsUpdate = false;
                    }
                    data.Counter = Convert.ToInt32(_maxCounter) + 1;
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public List<ServerInfoPOSDataSetupModel> GetServerInfoPOSDataSetupList()
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = (from a in db.POSDataSetups
                                where a.Code == VCM.BLUEPOS.Common.Helpers.Constants.POSDataSetupConstant.FTPSERVER
                                || a.Code == VCM.BLUEPOS.Common.Helpers.Constants.POSDataSetupConstant.FTPUSERNAME
                                || a.Code == VCM.BLUEPOS.Common.Helpers.Constants.POSDataSetupConstant.FTPPASSWORD
                                || a.Code == VCM.BLUEPOS.Common.Helpers.Constants.POSDataSetupConstant.SCRIPTDB
                                || a.Code == VCM.BLUEPOS.Common.Helpers.Constants.POSDataSetupConstant.SOURCEPOS
                                || a.Code == VCM.BLUEPOS.Common.Helpers.Constants.POSDataSetupConstant.SOURCEJOB
                                || a.Code == VCM.BLUEPOS.Common.Helpers.Constants.POSDataSetupConstant.LINKAPI
                                select new ServerInfoPOSDataSetupModel
                                {
                                    Code = a.Code,
                                    Value = a.Value
                                }).ToList();
                    if (data == null)
                    {
                        return new List<ServerInfoPOSDataSetupModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ServerInfoPOSDataSetupModel>();
            }
        }


        public List<POSDocumentNosResponseModel> GetVoucherNumberToPOSList(string voucherType, string storeNo, string posID, string ipAddress, out int totalRecord, int skip, int take)
        {
            var data = new List<POSDocumentNosResponseModel>();
            var result = new List<POSDocumentNosResponseModel>();
            try
            {
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", ipAddress, "POS", "POS@1234", "StorePLH");
                    var sql = "Select * from [dbo].[POSDocumentNos] as P ";
                    sql += "Where P.[DocumentType] = '" + voucherType + "' ";
                    sql += "And P.[StoreNo] = '" + storeNo + "' ";
                    sql += "And P.[POSTerminal] = '" + posID + "' ";
                    sql += "Order By P.[LastDateTime] DESC ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter(sql, connection);
                            DataSet ds = new DataSet();
                            da.Fill(ds, "POSDocumentNos");
                            DataTable dt = ds.Tables["POSDocumentNos"];

                            data = (from DataRow dr in dt.Rows
                                    select new POSDocumentNosResponseModel()
                                    {
                                        StoreNo = dr["StoreNo"].ToString(),
                                        POSTerminal = dr["POSTerminal"].ToString(),
                                        TransDate = String.Format("{0:yyyy-MM-dd}", Convert.ToDateTime(dr["TransDate"])),
                                        LastNumber = dr["LastNumber"].ToString(),
                                        LastDateTime = String.Format("{0:dd/MM/yyyy hh:mm tt}", Convert.ToDateTime(dr["LastDateTime"])),
                                        DocumentType = dr["DocumentType"].ToString()
                                    }).ToList();

                            connection.Close();

                            if (data.Count == 0 || data == null) // Không có dữ liệu
                            {
                                var listData = new List<POSDocumentNosResponseModel>
                                {
                                    new POSDocumentNosResponseModel
                                    {
                                        StoreNo = storeNo,
                                        POSTerminal = posID,
                                        DocumentType = voucherType
                                    }
                                }.ToList();

                                totalRecord = listData.Count();
                                result = listData.Skip(skip).Take(take).ToList();
                                return result;
                            }

                            totalRecord = data.Count();
                            result = data.Skip(skip).Take(take).ToList();
                            return result;
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
            totalRecord = 0;
            return new List<POSDocumentNosResponseModel>();
        }
        public List<DocumentNoResponseModel> GetDocumentNoList(string storeNo, string posID, string voucherType, out int totalRecord, int skip, int take)
        {
            try
            {
                var ipServerRead = ServerIPConnection.GetIPServerReadByStore(storeNo);
                using (var db = new CentralSalesContainer(ipServerRead))
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<DocumentNoResponseModel>("[dbo].[GetDocumentNo] @StoreNo, @Posterminal, @DocumentType",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@Posterminal", posID ?? string.Empty),
                        new SqlParameter("@DocumentType", voucherType ?? string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<DocumentNoResponseModel>();
                    }

                    data = data.Where(d => d.DocumentNumber != "").ToList();
                    totalRecord = data.Count();
                    data = data.Skip(skip).Take(take).ToList();
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<DocumentNoResponseModel>();
            }
        }

        public ResultResponseModel UpdatePOSDocumentNo(UpdatePOSDocumentNoModel req)
        {
            try
            {
                if (!string.IsNullOrEmpty(req.IPAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", req.IPAddress, "POS", "POS@1234", "StorePLH");
                    var sql = "UPDATE [dbo].[POSDocumentNos] ";
                    sql += "SET [LastNumber] ='" + req.LastNumber + "' ";
                    sql += ", [LastDateTime] = GETDATE()";
                    sql += " WHERE [DocumentType] = '" + req.DocumentType + "' ";
                    sql += " AND [StoreNo] = '" + req.StoreNo + "' ";
                    sql += " AND [POSTerminal] = '" + req.POSTerminal + "' ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter();
                            SqlCommand cmd = new SqlCommand(sql, connection);
                            cmd.ExecuteNonQuery();
                            cmd.Dispose();
                            connection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Có lỗi xảy ra"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return new ResultResponseModel
            {
                Status = Model.Enums.ResultEnum.Success,
                Message = "Cập nhật thành công"
            };
        }

        public ResultResponseModel InsertPOSDocumentNo(InsertPOSDocumentNoModel req)
        {
            try
            {
                if (!string.IsNullOrEmpty(req.IPAddress))
                {
                    string connectStr = string.Format("Data Source = {0}; Initial Catalog = {3}; User ID = {1}; Password={2}", req.IPAddress, "POS", "POS@1234", "StorePLH");
                    var sql = "INSERT INTO [dbo].[POSDocumentNos] (StoreNo,POSTerminal,TransDate,LastNumber,LastDateTime,Counter,DocumentType) ";
                    sql += "Values('" + req.StoreNo + "', '" + req.POSTerminal + "', GETDATE(), '" + req.LastNumber + "', GETDATE(), '', '" + req.DocumentType + "') ";

                    try
                    {
                        using (SqlConnection connection = new SqlConnection(connectStr))
                        {
                            connection.Open();
                            SqlDataAdapter da = new SqlDataAdapter();
                            SqlCommand cmd = new SqlCommand(sql, connection);
                            cmd.ExecuteNonQuery();
                            cmd.Dispose();
                            connection.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Có lỗi xảy ra"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return new ResultResponseModel
            {
                Status = Model.Enums.ResultEnum.Success,
                Message = "Cập nhật thành công"
            };
        }

        public List<ProvinceListResponseModel> GetProvinceList(string provinceCode, string provinceName, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<ProvinceListResponseModel>("[dbo].[GetProvinceList] @ProvinceCode, @ProvinceName, @Status, @PageSize, @PageNumber",
                        new SqlParameter("@ProvinceCode", provinceCode ?? string.Empty),
                        new SqlParameter("@ProvinceName", provinceName ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ProvinceListResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ProvinceListResponseModel>();
            }
        }

        public ResultResponseModel CreateProvinceList(CreateProvinceResponseModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 2 * 60;
                try
                {
                    var checkProvince = db.Provinces.Any(x => x.ProvinceCode == req.ProvinceCode);
                    if (checkProvince)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = $"Đã tồn tại mã: {req.ProvinceCode} tỉnh - thành phố này trong hệ thống. Vui lòng kiểm tra lại"
                        };
                    }

                    // Case: Không có dòng dữ liệu nào trong bảng Province, thì counter = 1 khi thêm mới
                    var checkData = db.Provinces.ToList();
                    if (checkData == null || checkData.Count == 0)
                    {
                        var item1 = new Province
                        {
                            ProvinceCode = req.ProvinceCode,
                            ProvinceName = req.ProvinceName,
                            Enable = req.Status,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            Counter = 1,
                            PKey = req.ProvinceCode
                        };
                        db.Provinces.Add(item1);
                        db.SaveChanges();
                    }
                    else
                    {
                        var _maxCounter = db.Provinces.Max(x => x.Counter);
                        var item2 = new Province
                        {
                            ProvinceCode = req.ProvinceCode,
                            ProvinceName = req.ProvinceName,
                            Enable = req.Status,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            Counter = Convert.ToInt32(_maxCounter) + 1,
                            PKey = req.ProvinceCode
                        };
                        db.Provinces.Add(item2);
                        db.SaveChanges();
                    }

                    return new ResultResponseModel
                    {
                        Item1 = req.ProvinceCode,
                        Item2 = req.ProvinceName,
                        Item3 = Convert.ToString(req.Status),
                        isFlag = req.Status,
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message
                    };
                }
            }
        }

        public ResultResponseModel UpdateProvinceList(UpdateProvinceResponseModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Provinces.FirstOrDefault(x => x.ProvinceCode == req.ProvinceCode);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var maxCounter = db.Provinces.Max(x => x.Counter);
                    data.ProvinceName = req.ProvinceName;
                    data.Enable = req.Status;
                    data.LastUpdateDate = DateTime.Now;
                    data.LastUpdateUser = req.LastUpdateUser;
                    data.Counter = Convert.ToInt32(maxCounter) + 1;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item2 = req.ProvinceName,
                        Item3 = Convert.ToString(req.Status),
                        isFlag = req.Status,
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeleteProvinceList(DeleteProvinceResponseModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Provinces.FirstOrDefault(x => x.ProvinceCode == req.ProvinceCode);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.Provinces.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public Tuple<bool, CheckProvinceResponseModel> CheckProvinceCode(string provinceCode)
        {
            try
            {
                var listdata = new CheckProvinceResponseModel();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Provinces.Any(x => x.ProvinceCode == provinceCode);
                    if (data)
                    {
                        return new Tuple<bool, CheckProvinceResponseModel>(true, new CheckProvinceResponseModel { Message = $"Mã tỉnh - thành phố : {provinceCode} đã tồn tại trong hệ thống", Status = true });
                    }
                    var _provinceCode = provinceCode.Trim();
                    var model = new CheckProvinceResponseModel
                    {
                        ProvinceCode = _provinceCode,
                        Message = string.Empty,
                        Status = false
                    };
                    return new Tuple<bool, CheckProvinceResponseModel>(false, model);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<bool, CheckProvinceResponseModel>(true, new CheckProvinceResponseModel { Message = "Mã tỉnh - thành phố này đã tồn tại trong hệ thống", Status = false });
            }
        }


        public List<SalesOrderTypeListModel> GetSalesOrderTypeList(string salesType, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<SalesOrderTypeListModel>("[dbo].[GetSaleOrderType] @SalesType, @Status, @PageSize, @PageNumber",
                        new SqlParameter("@SalesType", salesType ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SalesOrderTypeListModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<SalesOrderTypeListModel>();
            }
        }

        public ResultResponseModel CreateSalesOrderType(CreateSalesOrderTypeModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _total = db.SalesOrderTypes.Select(x => x.Code).ToList();
                    if (_total.Count == 0 || _total == null)
                    {
                        var item1 = new SalesOrderType
                        {
                            Code = req.SalesTypeCode,
                            TenderType = req.TenderType,
                            PaymentTenderType = req.PaymentTenderType,
                            Description = req.Description,
                            Percent = double.Parse(req.Percent),
                            ImageName = req.ImageName,
                            IsActive = req.IsActive == "1" ? true : false,
                            Order = int.Parse(req.Order.ToString()),
                            AllowedReturnedOrder = req.ReturnOrder == "1" ? true : false,
                            IsBlockedLoyalty = req.BlockedLoyaltyStr == "1" ? true : false,
                            AllowedCardService = req.AllowedCardService == "1" ? true : false,
                            Counter = 1,
                            Pkey = req.SalesTypeCode,
                            CreatedDate = DateTime.Now,
                            CreatedBy = req.CreatedBy
                        };

                        db.SalesOrderTypes.Add(item1);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.SalesTypeCode,
                            Item2 = req.IsActive,
                            Status = Model.Enums.ResultEnum.Success,
                            Flag = 1,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        var checkCode = db.SalesOrderTypes.FirstOrDefault(x => x.Code == req.SalesTypeCode);
                        if (checkCode != null)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Flag = 0,
                                Message = $"Đã tồn tại mã : {req.SalesTypeCode} này trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }

                        var checkKey = db.SalesOrderTypes.FirstOrDefault(x => x.Pkey == req.SalesTypeCode)?.Code;
                        var maxCounter = db.SalesOrderTypes.Max(x => x.Counter);
                        var item2 = new SalesOrderType
                        {
                            Code = req.SalesTypeCode,
                            TenderType = req.TenderType,
                            PaymentTenderType = req.PaymentTenderType,
                            Description = req.Description,
                            Percent = double.Parse(req.Percent),
                            ImageName = req.ImageName,
                            IsActive = req.IsActive == "1" ? true : false,
                            Order = int.Parse(req.Order.ToString()),
                            AllowedReturnedOrder = req.ReturnOrder == "1" ? true : false,
                            IsBlockedLoyalty = req.BlockedLoyaltyStr == "1" ? true : false,
                            AllowedCardService = req.AllowedCardService == "1" ? true : false,
                            Counter = Convert.ToInt32(maxCounter) + 1,
                            Pkey = req.SalesTypeCode,
                            CreatedDate = DateTime.Now,
                            CreatedBy = req.CreatedBy
                        };

                        db.SalesOrderTypes.Add(item2);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.SalesTypeCode,
                            Item2 = req.IsActive,
                            Status = Model.Enums.ResultEnum.Success,
                            Flag = 1,
                            Message = "Thêm mới thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public TenderTypeList LoadTenderType(float percent)
        {
            var result = new TenderTypeList();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (percent > 0)
                    {
                        var data = db.TenderTypeSetups.Where(a => a.Status == true).Select(a => new TenderTypeListModel
                        {
                            Code = a.Code,
                            Description = a.Description,
                            Caption = a.Caption,
                            Status = a.Status
                        }).ToList();

                        result = new TenderTypeList
                        {
                            ListTenderType = data
                        };
                    }
                    else
                    {
                        var data = new List<TenderTypeListModel>();
                        result = new TenderTypeList
                        {
                            ListTenderType = data  // TenderType = ""
                        };
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        public ResultResponseModel DeleteSalesOrderType(DeleteSalesOrderTypeModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.SalesOrderTypes.FirstOrDefault(x => x.Code == req.SalesTypeCode);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.SalesOrderTypes.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateSalesOrderType(UpdateSalesOrderTypeModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.SalesOrderTypes.Where(x => x.Code == req.SalesTypeCode).ToList();
                    if (data == null || data.Count == 0)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Flag = 0,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var _maxCounter = db.SalesOrderTypes.Max(x => x.Counter);
                    data.FirstOrDefault().Code = req.SalesTypeCode;
                    data.FirstOrDefault().TenderType = req.TenderType;
                    data.FirstOrDefault().PaymentTenderType = req.PaymentTenderType;
                    data.FirstOrDefault().Description = req.Description;
                    data.FirstOrDefault().Percent = double.Parse(req.Percent);
                    data.FirstOrDefault().ImageName = req.ImageName;
                    data.FirstOrDefault().IsActive = req.IsActive == "1" ? true : false;
                    data.FirstOrDefault().Order = int.Parse(req.Order.ToString());
                    data.FirstOrDefault().AllowedReturnedOrder = req.ReturnOrder == "1" ? true : false;
                    data.FirstOrDefault().IsBlockedLoyalty = req.BlockedLoyaltyStr == "1" ? true : false;
                    data.FirstOrDefault().AllowedCardService = req.AllowedCardService == "1" ? true : false;
                    data.FirstOrDefault().Counter = Convert.ToInt32(_maxCounter) + 1;
                    data.FirstOrDefault().Pkey = req.SalesTypeCode;
                    data.FirstOrDefault().UpdatedDate = DateTime.Now;
                    data.FirstOrDefault().UpdatedBy = req.UpdatedBy;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.SalesTypeCode,
                        Item2 = req.IsActive,
                        Item3 = req.Description,
                        Item4 = req.TenderType,
                        Item5 = req.PaymentTenderType,
                        Status = Model.Enums.ResultEnum.Success,
                        Flag = 1,
                        Message = "Cập nhật thành công"
                    };

                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public List<BankCardTypeResponseModel> LoadBankCardType(string bankCardCode, string bankCardName, string status, out int totalRecord, int skip = 0, int take = 100)
        {
            try
            {
                var data = new List<BankCardTypeModel>();
                var listdata = new List<BankCardTypeResponseModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    if (string.IsNullOrEmpty(status)) // tat ca
                    {
                        if (string.IsNullOrEmpty(bankCardCode) && string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                        else if (!string.IsNullOrEmpty(bankCardCode) && string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Code == bankCardCode
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();

                        }
                        else if (string.IsNullOrEmpty(bankCardCode) && !string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Name == bankCardName
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                        else if (!string.IsNullOrEmpty(bankCardCode) && !string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Name == bankCardName && a.Code == bankCardCode
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                    }
                    else if (status == "0")  // co hieu luc
                    {
                        if (string.IsNullOrEmpty(bankCardCode) && string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Status == false
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                        else if (!string.IsNullOrEmpty(bankCardCode) && string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Code == bankCardCode && a.Status == false
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                        else if (string.IsNullOrEmpty(bankCardCode) && !string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Name == bankCardName && a.Status == false
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                        else if (!string.IsNullOrEmpty(bankCardCode) && !string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Name == bankCardName && a.Code == bankCardCode && a.Status == false
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }

                    }
                    else if (status == "1")  // het hieu luc
                    {
                        if (string.IsNullOrEmpty(bankCardCode) && string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Status == true
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                        else if (!string.IsNullOrEmpty(bankCardCode) && string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Code == bankCardCode && a.Status == true
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();

                        }
                        else if (string.IsNullOrEmpty(bankCardCode) && !string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Name == bankCardName && a.Status == true
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                        else if (!string.IsNullOrEmpty(bankCardCode) && !string.IsNullOrEmpty(bankCardName))
                        {
                            data = (from a in db.BankCardTypes
                                    where a.Name == bankCardName && a.Code == bankCardCode && a.Status == true
                                    select new BankCardTypeModel
                                    {
                                        Code = a.Code,
                                        Name = a.Name,
                                        Type = a.Type,
                                        Status = a.Status,
                                        Counter = a.Counter,
                                        Pkey = a.Pkey,
                                        CreatedDate = a.CreatedDate,
                                        UpdatedDate = a.UpdatedDate
                                    }).OrderBy(d => d.Code).ToList();
                        }
                    }

                    totalRecord = data.Count();
                    var result = data.OrderBy(x => x.Code).Skip(skip).Take(take).ToList();
                    listdata = (from b in result
                                select new BankCardTypeResponseModel
                                {
                                    BankCardCode = b.Code,
                                    BankCardName = b.Name,
                                    Type = b.Type,
                                    Status = b.Status == true ? "1" : "0",
                                    Counter = b.Counter,
                                    Pkey = b.Pkey,
                                    CreatedDateStr = String.Format("{0:dd/MM/yyyy}", b.CreatedDate),
                                    UpdatedDateStr = String.Format("{0:dd/MM/yyyy}", b.UpdatedDate)
                                }).OrderBy(x => x.BankCardCode).ToList();
                    return listdata;
                }
            }
            catch (Exception)
            {
                totalRecord = 0;
                return (default(List<BankCardTypeResponseModel>));
            }
        }

        public ResultResponseModel CreateBankCardType(CreateBankCardTypeModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _totalRow = db.BankCardTypes.Select(x => x.Code).ToList();
                    if (_totalRow == null || _totalRow.Count == 0)
                    {
                        var _Counter = 1;
                        var insert = new EF.Central.BankCardType
                        {
                            Code = req.BankCardCode.ToUpper(),
                            Name = req.BankCardName.ToUpper(),
                            Type = string.Empty,
                            Status = req.Status,
                            Counter = _Counter,
                            Pkey = req.BankCardCode.ToUpper(),
                            CreatedDate = DateTime.Now
                        };
                        db.BankCardTypes.Add(insert);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.BankCardCode,
                            Item2 = req.BankCardName,
                            Item3 = req.Status == true ? "1" : "0",
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        var _checkBankCardCode = db.BankCardTypes.FirstOrDefault(x => x.Code == req.BankCardCode);
                        if (_checkBankCardCode != null)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"Đã tồn tại mã thẻ ngân hàng này: {req.BankCardCode} trong hệ thống. Vui lòng kiểm tra lại"
                            };

                        }
                        var _maxCounter = db.BankCardTypes.Max(x => x.Counter);
                        var insert = new EF.Central.BankCardType
                        {
                            Code = req.BankCardCode.ToUpper(),
                            Name = req.BankCardName.ToUpper(),
                            Type = string.Empty,
                            Status = req.Status,
                            Counter = Convert.ToInt32(_maxCounter) + 1,
                            Pkey = req.BankCardCode.ToUpper(),
                            CreatedDate = DateTime.Now
                        };
                        db.BankCardTypes.Add(insert);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.BankCardCode,
                            Item2 = req.BankCardName,
                            Item3 = req.Status == true ? "1" : "0",
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateBankCardType(UpdateBankCardTypeModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.BankCardTypes.FirstOrDefault(x => x.Code.Trim().ToUpper() == req.BankCardCode.Trim().ToUpper());
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var _maxCounter = db.BankCardTypes.Max(x => x.Counter);
                    data.Name = req.BankCardName.ToUpper();
                    data.Type = string.Empty;
                    data.Status = req.Status;
                    data.Counter = Convert.ToInt32(_maxCounter) + 1;
                    data.UpdatedDate = DateTime.Now;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.BankCardCode,
                        Item2 = req.BankCardName,
                        Item3 = req.Status == true ? "1" : "0",
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeleteBankCardType(DeleteBankCardTypeModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var data = db.BankCardTypes.FirstOrDefault(x => x.Code.Trim().ToLower() == req.BankCardCode.Trim().ToLower());
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.BankCardTypes.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public List<EWalletListResponseModel> LoadSetupEWalletList(string storeNo, string eWalletType, string textSearch, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<EWalletListResponseModel>("[dbo].[GetEWalletList] @StoreNo, @EWalletCode, @TextSearch, @Status, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@EWalletCode", eWalletType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<EWalletListResponseModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<EWalletListResponseModel>();
            }
        }

        public List<ExportExcelEWalletListModel> ExportExcelSetupEWalletList(string storeNo, string eWalletType, string textSearch, string status)
        {
            try
            {
                var data = new List<ExportExcelEWalletListModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    data = db.Database.SqlQuery<ExportExcelEWalletListModel>("[dbo].[GetEWalletList] @StoreNo, @EWalletCode, @TextSearch, @Status, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@EWalletCode", eWalletType ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportExcelEWalletListModel>();
                    }
                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcelEWalletListModel>();
            }
        }

        public string GetEWalletTerminalCode(string storeNo, string eWalletCode, string posNo)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var checkTerminalCode = db.EWalletTerminals.FirstOrDefault(x => x.StoreNo == storeNo && x.POSNo == posNo && x.EWalletCode == eWalletCode);
                    if (checkTerminalCode != null) // Da ton tai TerminalCode
                    {
                        return null;
                    }
                    var result = storeNo + "_" + eWalletCode + "_" + posNo;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public ResultResponseModel CreateSetupEWallet(CreateEWalletListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _total = db.EWalletTerminals.Select(x => x.TerminalCode).ToList();
                    //var _tenderType = db.TenderTypeSetups.Where(d =>d.Caption == "EWALLET" && d.MayBeUsed == 1 &&  d.Description == req.EWalletName).FirstOrDefault()?.Code;

                    var listEWallet = db.OptionDatas.Where(x => x.Code == req.EWalletCode).FirstOrDefault();

                    if (_total.Count == 0 || _total == null)
                    {
                        var item1 = new EWalletTerminal
                        {
                            PartnerId = req.PartnerID,
                            TerminalCode = req.TerminalCode,
                            EWalletCode = req.EWalletCode,
                            EWalletName = listEWallet.Description,
                            StoreNoFull = req.StoreNo,
                            StoreNo = req.StoreNo,
                            POSNo = req.POSNo,
                            AccessKey = req.AccessKey,
                            PaymentType = req.PaymentType,
                            TenderType = req.TenderType,
                            POSTerminal = req.POSTerminal,
                            IsTPay = req.IsTPay,      
                            Status = req.Status,
                            Mode = req.Mode,
                            Counter = 1,
                            Pkey = $"{req.PartnerID}{"_"}{req.TerminalCode}",
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser
                        };

                        db.EWalletTerminals.Add(item1);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.StoreNo,
                            Item2 = req.EWalletCode,
                            Item3 = Convert.ToString(req.Status),
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        var checkTerminalCode = db.EWalletTerminals.FirstOrDefault(x => x.TerminalCode == req.TerminalCode.Trim());
                        if (checkTerminalCode != null)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Message = $"Đã tồn tại Mã máy thanh toán ví: {req.TerminalCode} này trong hệ thống. Vui lòng kiểm tra lại"
                            };
                        }

                        var checkAccessKey = db.EWalletTerminals.FirstOrDefault(x => x.AccessKey == req.AccessKey)?.AccessKey;
                        var maxCounter = db.EWalletTerminals.Max(x => x.Counter);

                        var item2 = new EWalletTerminal
                        {
                            PartnerId = req.PartnerID,
                            TerminalCode = req.TerminalCode,
                            EWalletCode = req.EWalletCode,
                            EWalletName = listEWallet.Description,
                            StoreNoFull = req.StoreNo,
                            StoreNo = req.StoreNo,
                            POSNo = req.POSNo,
                            AccessKey = req.AccessKey,
                            PaymentType = req.PaymentType,
                            TenderType = req.TenderType,
                            POSTerminal = req.POSTerminal,
                            IsTPay = req.IsTPay,
                            Status = req.Status,
                            Mode = req.Mode,
                            Counter = Convert.ToInt32(maxCounter) + 1,
                            Pkey = $"{req.PartnerID}{"_"}{req.TerminalCode}",
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser
                        };

                        db.EWalletTerminals.Add(item2);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.StoreNo,
                            Item2 = req.EWalletCode,
                            Item3 = Convert.ToString(req.Status),
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateSetupEWallet(UpdateEWalletListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.EWalletTerminals.Where(x => x.TerminalCode == req.TerminalCode).FirstOrDefault();
                    var listEWallet = db.OptionDatas.Where(x => x.Code == req.EWalletCode).FirstOrDefault();

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    // Không update key

                    var _maxCounter = db.EWalletTerminals.Max(x => x.Counter);
                    data.PartnerId = req.PartnerID;
                    data.EWalletCode = req.EWalletCode;
                    data.EWalletName = listEWallet.Description;
                    data.StoreNoFull = req.StoreNo;
                    data.StoreNo = req.StoreNo;
                    data.POSNo = req.POSNo;
                    data.AccessKey = req.AccessKey;
                    data.PaymentType = req.PaymentType;
                    data.TenderType = req.TenderType;
                    data.POSTerminal = req.POSTerminal;
                    data.IsTPay = req.IsTPay;
                    data.Status = req.Status;
                    data.Mode = req.Mode;
                    data.Counter = Convert.ToInt32(_maxCounter) + 1;
                    data.Pkey = $"{req.PartnerID}{"_"}{req.TerminalCode}";
                    data.UpdatedDate = DateTime.Now;
                    data.UpdatedUser = req.UpdatedUser;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.StoreNo,
                        Item2 = req.EWalletCode,
                        Item3 = Convert.ToString(req.Status),
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeleteSetupEWalletList(DeleteEWalletListModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.EWalletTerminals.FirstOrDefault(x => x.TerminalCode == req.TerminalCode);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.EWalletTerminals.Remove(data);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public Tuple<int, string> ImportExcelEWallet(string userName, DataTable dt)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 5 * 60;
                try
                {
                    var dateTime = DateTime.Now;

                    if (dt.Rows.Count > 0)
                    {
                        for (int y = 0; y < dt.Rows.Count; y++)
                        {
                            if (dt.Rows[y]["EWalletCode"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột EWalletCode đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            //if (dt.Rows[y]["EWalletName"] == null)
                            //{
                            //    return new Tuple<int, string>(0, $"Dữ liệu cột EWalletName đang bị null, Vui lòng kiểm tra dữ liệu import");
                            //}

                            if (dt.Rows[y]["StoreNo"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột StoreNo đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["POSNo"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSNo đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["POSTerminal"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSTerminal đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["PaymentType"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột PaymentType đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["TenderType"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột TenderType đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["Mode"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột Mode đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["AccessKey"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột AccessKey đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            if (dt.Rows[y]["PartnerID"] == null)
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột PartnerID đang bị null, Vui lòng kiểm tra dữ liệu import");
                            }

                            var excel_EWalletCode = dt.Rows[y]["EWalletCode"].ToString().ToUpper().Trim();
                            //var excel_EWalletName = dt.Rows[y]["EWalletName"].ToString().ToUpper().Trim();
                            var excel_StoreNo = dt.Rows[y]["StoreNo"].ToString().Trim();
                            var excel_POSNo = dt.Rows[y]["POSNo"].ToString().Trim();
                            var excel_POSTerminal = dt.Rows[y]["POSTerminal"].ToString().Trim();
                            var excel_PaymentType = dt.Rows[y]["PaymentType"].ToString().ToUpper().Trim();
                            var excel_TenderType = dt.Rows[y]["TenderType"].ToString().ToUpper().Trim();
                            var excel_Mode = dt.Rows[y]["Mode"].ToString().ToUpper().Trim();
                            var excel_AccessKey = dt.Rows[y]["AccessKey"].ToString().Trim();
                            var excel_PartnerID = dt.Rows[y]["PartnerID"].ToString().Trim();

                            if (string.IsNullOrEmpty(excel_EWalletCode))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột EWalletCode rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            //if (string.IsNullOrEmpty(excel_EWalletName))
                            //{
                            //    return new Tuple<int, string>(0, $"Dữ liệu cột EWalletName rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            //}

                            if (string.IsNullOrEmpty(excel_StoreNo))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột StoreNo rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_POSNo))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSNo rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_POSTerminal))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột POSTerminal rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_PaymentType))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột PaymentType rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_TenderType))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột TenderType rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_Mode))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột Mode rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_AccessKey))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột AccessKey rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                            if (string.IsNullOrEmpty(excel_PartnerID))
                            {
                                return new Tuple<int, string>(0, $"Dữ liệu cột PartnerID rỗng, Vui lòng kiểm tra lại dữ liệu import");
                            }

                        }

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            var excel_EWalletCode = dt.Rows[i]["EWalletCode"].ToString().ToUpper().Trim();
                            var excel_EWalletName = db.OptionDatas.Where(x =>x.Code == excel_EWalletCode).FirstOrDefault()?.Description;
                            if (excel_EWalletName == null || excel_EWalletName == "")
                            {
                                return new Tuple<int, string>(0, $"Tên ví điện tử trong bảng OptionData chưa khai báo");
                            }
                            
                            var excel_StoreNo = dt.Rows[i]["StoreNo"].ToString().Trim();
                            var excel_POSNo = dt.Rows[i]["POSNo"].ToString().Trim();
                            var excel_POSTerminal = dt.Rows[i]["POSTerminal"].ToString().Trim();
                            var excel_PaymentType = dt.Rows[i]["PaymentType"].ToString().ToUpper().Trim();
                            var excel_TenderType = dt.Rows[i]["TenderType"].ToString().ToUpper().Trim();
                            var excel_Mode = dt.Rows[i]["Mode"].ToString().ToUpper().Trim();
                            var excel_AccessKey = dt.Rows[i]["AccessKey"].ToString().Trim();
                            var excel_PartnerID = dt.Rows[i]["PartnerID"].ToString().Trim();

                            var excel_TerminalCode = $"{excel_StoreNo}{"_"}{excel_EWalletCode}{"_"}{excel_POSNo}";
                            var excel_Pkey = $"{excel_PartnerID}{"_"}{excel_StoreNo}{"_"}{excel_EWalletCode}{"_"}{excel_POSNo}";

                            var checkTerminalCode = db.EWalletTerminals.FirstOrDefault(d => d.TerminalCode == excel_TerminalCode);
                            var _maxCounter = db.EWalletTerminals.Max(x => x.Counter);

                            if (checkTerminalCode == null)
                            {
                                db.EWalletTerminals.Add(new EWalletTerminal
                                {
                                    PartnerId = excel_PartnerID,
                                    TerminalCode = excel_TerminalCode,
                                    EWalletCode = excel_EWalletCode,
                                    EWalletName = excel_EWalletName,
                                    StoreNoFull = excel_StoreNo,
                                    StoreNo = excel_StoreNo,
                                    POSNo = excel_POSNo,
                                    POSTerminal = excel_POSTerminal,
                                    AccessKey = excel_AccessKey,
                                    PaymentType = excel_PaymentType,
                                    TenderType = excel_TenderType,
                                    IsTPay = (excel_EWalletCode == "TPA") ? true : false,
                                    Status = true,
                                    Mode = excel_Mode, 
                                    Counter = Convert.ToInt32(_maxCounter) + 1,
                                    Pkey = excel_Pkey,
                                    CreatedDate = DateTime.Now,
                                    CreatedUser = userName
                                });

                            }
                            else
                            {
                                checkTerminalCode.PartnerId = excel_PartnerID;
                                checkTerminalCode.EWalletCode = excel_EWalletCode;
                                checkTerminalCode.EWalletName = excel_EWalletName;
                                checkTerminalCode.StoreNoFull = excel_StoreNo;
                                checkTerminalCode.StoreNo = excel_StoreNo;
                                checkTerminalCode.POSNo = excel_POSNo;
                                checkTerminalCode.POSTerminal = excel_POSTerminal;
                                checkTerminalCode.PaymentType = excel_PaymentType;
                                checkTerminalCode.TenderType = excel_TenderType;
                                checkTerminalCode.IsTPay = (excel_EWalletCode == "TPA") ? true : false;
                                checkTerminalCode.Status = true;
                                checkTerminalCode.Mode = excel_Mode;
                                checkTerminalCode.AccessKey = excel_AccessKey;
                                checkTerminalCode.Counter = Convert.ToInt32(_maxCounter) + 1;
                                checkTerminalCode.Pkey = excel_Pkey;
                                checkTerminalCode.UpdatedDate = DateTime.Now;
                                checkTerminalCode.UpdatedUser = userName;
                            }
                            db.SaveChanges();
                        }
                    }
                    return new Tuple<int, string>(dt.Rows.Count, "Import EWallet thành công");
                }
                catch (Exception ex)
                {
                    return new Tuple<int, string>(0, "Lỗi hệ thống, import EWallet không thành công");
                }
            }
        }
        public List<ChangePassWordModel> ChangePassWordList(string storeNo, string userName, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                var data = new List<ChangePassWordModel>();
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;

                    //data = db.Database.SqlQuery<ChangePassWordModel>("[dbo].[GetChangePassWordList] @StoreNo, @TextSearch, @PageSize, @PageNumber",
                    //        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                    //        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                    //        new SqlParameter("@PageSize", pageSize),
                    //        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    data = db.Database.SqlQuery<ChangePassWordModel>("[dbo].[GetChangePassWordListV2] @StoreNo, @UserName, @TextSearch, @PageSize, @PageNumber",
                            new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                            new SqlParameter("@UserName", userName ?? string.Empty),
                            new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                            new SqlParameter("@PageSize", pageSize),
                            new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ChangePassWordModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<ChangePassWordModel>();
            }
        }
        public ResultResponseModel ChangePassWord(ChangePassWordModel req)
        {
            using (var db = new CentralMDPartnerContainer())
            {
                db.Database.CommandTimeout = 5 * 60;

                try
                {
                    var data = db.Staffs.FirstOrDefault(x => x.ID == req.StaffCode.Trim() && x.Blocked == 0);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Flag = 0,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var maxCounter = db.Staffs.Max(x => x.Counter) ?? 0;
                    data.Password = req.PassWordNew.Trim();
                    data.StoreNo = req.StoreNo;
                    data.LastDateModified = DateTime.Now;
                    data.Counter = Convert.ToInt32(maxCounter) + 1;
                    data.Pkey = req.StaffCode;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.StaffCode,
                        Item2 = req.StaffName,
                        Item3 = req.StoreNo,
                        Item5 = req.PassWordNew,
                        Status = Model.Enums.ResultEnum.Success,
                        Flag = 1,
                        Message = "Đổi mật khẩu thành công"
                    };
                }
                catch (Exception ex)
                {
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.ErrorSystem,
                        Message = ex.Message
                    };
                }
            }
        }
        public List<SetupImageSliderResponseModel> GetSetupImageSliderList(string styleProfile, string storeNo, string description, string fileName, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    totalRecord = 0;
                    //var data = db.Database.SqlQuery<SetupImageSliderResponseModel>("[dbo].[GET_IMAGES_SLIDER_LIST] @StoreNo, @Description, @FileName, @Status, @PageSize, @PageNumber",
                    
                    var data = db.Database.SqlQuery<SetupImageSliderResponseModel>("[dbo].[GET_IMAGES_SLIDER_LIST_V2] @StoreNo, @StyleProfile, @Description, @FileName, @Status, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@StyleProfile", styleProfile ?? string.Empty),
                        new SqlParameter("@Description", description ?? string.Empty),
                        new SqlParameter("@FileName", fileName ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SetupImageSliderResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<SetupImageSliderResponseModel>();
            }
        }

        //------ 20/10/2023 ------
        public List<ExportExcel_ImagesSliderListResponseModel> ExportExcel_ImagesSliderList(string storeNo, string styleProfile, string description, string fileName, string status)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;

                    var data = db.Database.SqlQuery<ExportExcel_ImagesSliderListResponseModel>("[dbo].[GET_IMAGES_SLIDER_LIST_EXP_V2] @StoreNo, @StyleProfile, @Description, @FileName, @Status",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@StyleProfile", styleProfile ?? string.Empty),
                        new SqlParameter("@Description", description ?? string.Empty),
                        new SqlParameter("@FileName", fileName ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty)).ToList();

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportExcel_ImagesSliderListResponseModel>();
            }
        }

        public ResultResponseModel CreateImageSlider(CreateSetupImageSliderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var totalRow = db.ImageSliders.Select(x => x.Pkey).ToList();
                    var fileName = req.FileName.Split('\\')[2];    // Trong array bat dau tu 0

                    if (totalRow == null || totalRow.Count == 0)
                    {
                        if (req.StoreNo == "ALL")  // Chọn tất cả Store //req.StoreNo == "ALL" || req.IsSelectAll == true
                        {
                            var maxCounter = 1;
                            var ItemInsert = new EF.Central.ImageSlider
                            {
                                StoreNo = req.StoreNo,
                                Url = req.UrlFile.Trim(),
                                FileName = fileName.Trim(),
                                Description = req.Description,
                                Status = req.Status == "1" ? true : false,
                                CreatedDate = DateTime.Now,
                                CreatedUser = req.CreatedUser,
                                UpdatedDate = DateTime.Now,
                                UpdatedUser = req.CreatedUser,
                                Counter = maxCounter,
                                Pkey = $"{req.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                            };

                            db.ImageSliders.Add(ItemInsert);
                            db.SaveChanges();
                            ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                            db.SaveChanges();
                        }
                        else
                        {
                            if (req.CheckStore == false)
                            {
                                foreach (var item in req.StoreList)
                                {
                                    var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                                    if (_maxCounter == null || _maxCounter == 0)
                                    {
                                        _maxCounter = 1;
                                    }
                                    else
                                    {
                                        _maxCounter = _maxCounter + 1;
                                    }

                                    var ItemInsert = new EF.Central.ImageSlider
                                    {
                                        StoreNo = item.StoreNo,
                                        Url = req.UrlFile.Trim(),
                                        FileName = fileName.Trim(),
                                        Description = req.Description,
                                        Status = req.Status == "1" ? true : false,
                                        CreatedDate = DateTime.Now,
                                        CreatedUser = req.CreatedUser,
                                        UpdatedDate = DateTime.Now,
                                        UpdatedUser = req.CreatedUser,
                                        Counter = _maxCounter,
                                        Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                        //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                                    };

                                    db.ImageSliders.Add(ItemInsert);
                                    db.SaveChanges();
                                    ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                                    db.SaveChanges();
                                }
                            }
                            else // Loại trừ các ST/CH không áp dụng chương trình này
                            {
                                var listStore = req.StoreList.Where(x => !req.StoreList2.Any(y => y.StoreNo == x.StoreNo)).ToList();
                                foreach (var item in listStore)
                                {
                                    var data1 = db.ImageSliders.Where(x => x.StoreNo == item.StoreNo && x.Status == true).FirstOrDefault();
                                    if (data1 != null)
                                    {
                                        data1.Status = false;
                                        db.SaveChanges();

                                        // Insert row new
                                        var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                                        var ItemInsert = new EF.Central.ImageSlider
                                        {
                                            StoreNo = item.StoreNo,
                                            Url = req.UrlFile.Trim(),
                                            FileName = fileName.Trim(),
                                            Description = req.Description,
                                            Status = req.Status == "1" ? true : false,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser,
                                            UpdatedDate = DateTime.Now,
                                            UpdatedUser = req.CreatedUser,
                                            Counter = _maxCounter + 1,
                                            Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                            //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                                        };

                                        db.ImageSliders.Add(ItemInsert);
                                        db.SaveChanges();
                                        ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                                        db.SaveChanges();

                                    }
                                    else
                                    {
                                        var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                                        if (_maxCounter == null || _maxCounter == 0)
                                        {
                                            _maxCounter = 1;
                                        }
                                        else
                                        {
                                            _maxCounter = _maxCounter + 1;
                                        }

                                        var ItemInsert = new EF.Central.ImageSlider
                                        {
                                            StoreNo = item.StoreNo,
                                            Url = req.UrlFile.Trim(),
                                            FileName = fileName.Trim(),
                                            Description = req.Description,
                                            Status = req.Status == "1" ? true : false,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser,
                                            UpdatedDate = DateTime.Now,
                                            UpdatedUser = req.CreatedUser,
                                            Counter = _maxCounter,
                                            Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                            //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                                        };

                                        db.ImageSliders.Add(ItemInsert);
                                        db.SaveChanges();
                                        ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                                        db.SaveChanges();

                                    }
                                }
                            }
                        }

                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Item1 = fileName.Substring(0, fileName.LastIndexOf(".")),
                            Item2 = req.Status,
                            IsStatus = 1,
                            Message = "Thêm mới thành công"
                        };
                    }
                    else
                    {
                        if (req.StoreNo == "ALL")  // Chọn tất cả Store //req.StoreNo == "ALL" || req.IsSelectAll == true
                        {
                            //var data1 = db.ImageSliders.Where(x => x.StoreNo == req.StoreNo && x.Status == true).FirstOrDefault();

                            //if (data1 != null)
                            //{
                            //    // Update Status = false : hết hiệu lực
                            //    data1.Status = false;
                            //    db.SaveChanges();

                            //    var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                            //    var ItemInsert = new EF.Central.ImageSlider
                            //    {
                            //        StoreNo = req.StoreNo,
                            //        Url = req.UrlFile.Trim(),
                            //        FileName = fileName.Trim(),
                            //        Description = req.Description,
                            //        Status = req.Status == "1" ? true : false,
                            //        CreatedDate = DateTime.Now,
                            //        CreatedUser = req.CreatedUser,
                            //        UpdatedDate = DateTime.Now,
                            //        UpdatedUser = req.CreatedUser,
                            //        Counter = _maxCounter + 1,
                            //        Pkey = $"{req.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                            //        //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                            //    };

                            //    db.ImageSliders.Add(ItemInsert);
                            //    db.SaveChanges();

                            //    var id = db.ImageSliders.Max(y => y.ID);
                            //    ItemInsert.Pkey = $"{ItemInsert.Pkey}-{id}";
                            //    //ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                            //    db.SaveChanges();

                            //}
                            //else  // Thêm mới
                            //{
                            //    var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                            //    var ItemInsert = new EF.Central.ImageSlider
                            //    {
                            //        StoreNo = req.StoreNo,
                            //        Url = req.UrlFile.Trim(),
                            //        FileName = fileName.Trim(),
                            //        Description = req.Description,
                            //        Status = req.Status == "1" ? true : false,
                            //        CreatedDate = DateTime.Now,
                            //        CreatedUser = req.CreatedUser,
                            //        UpdatedDate = DateTime.Now,
                            //        UpdatedUser = req.CreatedUser,
                            //        Counter = _maxCounter + 1,
                            //        Pkey = $"{req.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                            //        //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                            //    };

                            //    db.ImageSliders.Add(ItemInsert);
                            //    db.SaveChanges();

                            //    var id = db.ImageSliders.Max(y => y.ID);
                            //    ItemInsert.Pkey = $"{ItemInsert.Pkey}-{id}";
                            //    //ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                            //    db.SaveChanges();
                            //}   


                            // 06/02/2023 : A.Nguyên, Tưởng : đề nghị sửa rule này, truong hop ALL thi cho them moi bình thuong, ko có rule gì cả

                            var _maxCounter = db.ImageSliders.Max(x => x.Counter);

                            var ItemInsert = new EF.Central.ImageSlider
                            {
                                StoreNo = req.StoreNo,
                                Url = req.UrlFile.Trim(),
                                FileName = fileName.Trim(),
                                Description = req.Description,
                                Status = req.Status == "1" ? true : false,
                                CreatedDate = DateTime.Now,
                                CreatedUser = req.CreatedUser,
                                UpdatedDate = DateTime.Now,
                                UpdatedUser = req.CreatedUser,
                                Counter = _maxCounter + 1,
                                Pkey = $"{req.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                            };

                            db.ImageSliders.Add(ItemInsert);
                            db.SaveChanges();

                            var id = db.ImageSliders.Max(y => y.ID);
                            ItemInsert.Pkey = $"{ItemInsert.Pkey}-{id}";
                            //ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                            db.SaveChanges();

                        }
                        else
                        {
                            if (req.CheckStore == false)
                            {
                                foreach (var item in req.StoreList)
                                {
                                    var data1 = db.ImageSliders.Where(x => x.StoreNo == item.StoreNo && x.Status == true).ToList();
                                    if (data1.Count > 0)
                                    {
                                        // Update Status = false : hết hiệu lực
                                        data1.FirstOrDefault().Status = false;
                                        db.SaveChanges();

                                        // Insert row new
                                        var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                                        var ItemInsert = new EF.Central.ImageSlider
                                        {
                                            StoreNo = item.StoreNo,
                                            Url = req.UrlFile.Trim(),
                                            FileName = fileName.Trim(),
                                            Description = req.Description,
                                            Status = req.Status == "1" ? true : false,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser,
                                            UpdatedDate = DateTime.Now,
                                            UpdatedUser = req.CreatedUser,
                                            Counter = _maxCounter + 1,
                                            Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                            //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                                        };

                                        db.ImageSliders.Add(ItemInsert);
                                        db.SaveChanges();
                                        ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                                        db.SaveChanges();
                                    }
                                    else // Thêm mới
                                    {
                                        var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                                        var ItemInsert = new EF.Central.ImageSlider
                                        {
                                            StoreNo = item.StoreNo,
                                            Url = req.UrlFile.Trim(),
                                            FileName = fileName.Trim(),
                                            Description = req.Description,
                                            Status = req.Status == "1" ? true : false,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser,
                                            UpdatedDate = DateTime.Now,
                                            UpdatedUser = req.CreatedUser,
                                            Counter = _maxCounter + 1,
                                            Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                            //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                                        };

                                        db.ImageSliders.Add(ItemInsert);
                                        db.SaveChanges();
                                        ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                                        db.SaveChanges();
                                    }
                                }
                            }
                            else  // Loại trừ các ST/CH không áp dụng chương trình này
                            {
                                var listStore = req.StoreList.Where(x => !req.StoreList2.Any(y => y.StoreNo == x.StoreNo)).ToList();
                                foreach (var item in listStore)
                                {
                                    //var data1 = db.ImageSliders.Where(x =>x.StoreNo == item.StoreNo && x.Status == true).FirstOrDefault();
                                    var data1 = db.ImageSliders.Where(x =>x.StoreNo == item.StoreNo && x.Status == true).ToList();
                                    if (data1.Count > 0)
                                    {
                                        // Update Status = false : hết hiệu lực
                                        data1.FirstOrDefault().Status = false;
                                        db.SaveChanges();

                                        // Insert row new
                                        var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                                        var ItemInsert = new EF.Central.ImageSlider
                                        {
                                            StoreNo = item.StoreNo,
                                            Url = req.UrlFile.Trim(),
                                            FileName = fileName.Trim(),
                                            Description = req.Description,
                                            Status = req.Status == "1" ? true : false,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser,
                                            UpdatedDate = DateTime.Now,
                                            UpdatedUser = req.CreatedUser,
                                            Counter = _maxCounter + 1,
                                            Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                            //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                                        };

                                        db.ImageSliders.Add(ItemInsert);
                                        db.SaveChanges();
                                        ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                                        db.SaveChanges();
                                    }
                                    else // Thêm mới
                                    {
                                        // Status = true : có hiệu lực
                                        var _maxCounter = db.ImageSliders.Max(x => x.Counter);
                                        var ItemInsert = new EF.Central.ImageSlider
                                        {
                                            StoreNo = item.StoreNo,
                                            Url = req.UrlFile.Trim(),
                                            FileName = fileName.Trim(),
                                            Description = req.Description,
                                            Status = req.Status == "1" ? true : false,
                                            CreatedDate = DateTime.Now,
                                            CreatedUser = req.CreatedUser,
                                            UpdatedDate = DateTime.Now,
                                            UpdatedUser = req.CreatedUser,
                                            Counter = _maxCounter + 1,
                                            Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                                            //Pkey = req.FileName.Substring(0, req.FileName.LastIndexOf("."))
                                        };

                                        db.ImageSliders.Add(ItemInsert);
                                        db.SaveChanges();
                                        ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                                        db.SaveChanges();

                                    }
                                }
                            } 
                        }

                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Item1 = fileName.Substring(0, fileName.LastIndexOf(".")),
                            Item2 = req.Status,
                            IsStatus = 1,
                            Message = "Thêm mới thành công"
                        };

                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = "Lỗi hệ thống: " + ex.Message
                };
            }
        }

        public async Task<ResultResponseModel> CreateImageSliderV2Async(CreateSetupImageSliderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var fileName = req.FileName.Split('\\').LastOrDefault() ;    // get filename

                    if (req.StoreNo == "ALL")  // Chọn tất cả Store
                    {
                        var maxCounter = await db.ImageSliders.AsNoTracking().MaxAsync(x=>x.Counter) ?? 0;
                        var ItemInsert = new EF.Central.ImageSlider
                        {
                            StoreNo = req.StoreNo,
                            Url = req.UrlFile.Trim(),
                            FileName = fileName.Trim(),
                            Description = req.Description,
                            //FromDate = req.FromDate,
                            //ToDate = req.ToDate,
                            Status = req.Status == "1" ? true : false,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser,
                            Counter = maxCounter + 1, //neu chua co row nao thi counter = 1 nguoi lai tang counter len 1
                            Pkey = $"{req.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                        };

                        db.ImageSliders.Add(ItemInsert);
                        await db.SaveChangesAsync();
                        ItemInsert.Pkey = $"{ItemInsert.Pkey}-{ItemInsert.ID}";
                        await db.SaveChangesAsync();

                    }
                    else // chon mot vai store
                    {
                        var listStore = req.StoreList.Where(x => !req.StoreList2.Any(y => y.StoreNo == x.StoreNo)).ToList();
                        var _maxCounter = await db.ImageSliders.AsNoTracking().MaxAsync(x => x.Counter) ?? 0;                        
                        
                        var lstImgSlider = listStore.Select(item => new ImageSlider
                        {
                            StoreNo = item.StoreNo,
                            Url = req.UrlFile.Trim(),
                            FileName = fileName.Trim(),
                            Description = req.Description,
                            //FromDate = Convert.ToDateTime(req.FromDate),
                            //ToDate = Convert.ToDateTime(req.ToDate),
                            Status = req.Status == "1" ? true : false,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser,
                            Counter = _maxCounter + 1,
                            Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                        }).ToList();

                        //foreach (var item in listStore)
                        //{
                        //    var ItemInsert = new EF.Central.ImageSlider
                        //    {
                        //        StoreNo = item.StoreNo,
                        //        Url = req.UrlFile.Trim(),
                        //        FileName = fileName.Trim(),
                        //        Description = req.Description,
                        //        Status = req.Status == "1" ? true : false,
                        //        CreatedDate = DateTime.Now,
                        //        CreatedUser = req.CreatedUser,
                        //        UpdatedDate = DateTime.Now,
                        //        UpdatedUser = req.CreatedUser,
                        //        Counter = _maxCounter, 
                        //        Pkey = $"{item.StoreNo}{"-"}{fileName.Substring(0, fileName.LastIndexOf("."))}"
                        //    };
                        //    lstImgSlider.Add(ItemInsert);

                        //}

                        db.ImageSliders.AddRange(lstImgSlider);
                        await db.SaveChangesAsync();       
                        
                        foreach (var item in lstImgSlider)
                        {
                            item.Pkey = $"{item.Pkey}-{item.ID}";
                        }

                        await db.SaveChangesAsync();
                    }

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Item1 = fileName.Substring(0, fileName.LastIndexOf(".")),
                        Item2 = req.Status,
                        IsStatus = 1,
                        Message = "Thêm mới thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = "Lỗi hệ thống: " + ex.Message
                };
            }
        }

        // ------ New: 25/10/2023 ------
        public async Task<ResultResponseModel> CreateImageSlider_V2(CreateSetupImageSliderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var fileName = string.IsNullOrEmpty(req.FileName) ? string.Empty : req.FileName.Split('\\').LastOrDefault();
                    var checkExisted = db.ImageSliders.Any();
                    if (!checkExisted)
                    {
                        var ItemInsert = new EF.Central.ImageSlider
                        {
                            StoreNo = req.StoreNo,
                            Url = string.IsNullOrEmpty(req.UrlFile) ? string.Empty: req.UrlFile.Trim(),
                            FileName = string.IsNullOrEmpty(fileName) ? string.Empty: fileName.Trim(),
                            Description = req.Description,
                            FromDate = req.FromDate,
                            ToDate = req.ToDate,
                            Status = req.Status == "1" ? true : false,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser,
                            Counter = 1, 
                            Pkey = $"{req.FromDate.ToString("yyyyMMdd")}{"-"}{req.ToDate.ToString("yyyyMMdd")}{"-"}{req.StoreNo}"
                        };

                        db.ImageSliders.Add(ItemInsert);
                        await db.SaveChangesAsync();

                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Item1 = fileName.Substring(0, fileName.LastIndexOf(".")),
                            Item2 = req.Status,
                            IsStatus = 1,
                            Message = "Thêm mới thành công"
                        };
                    }

                    // ----- Insert multi nên không cần check trùng -----
                   
                    if (req.StoreNo == "ALL")  // Chọn tất cả Store
                    {
                        var maxCounter = await db.ImageSliders.AsNoTracking().MaxAsync(x=>x.Counter) ?? 0;
                        var ItemInsert = new EF.Central.ImageSlider
                        {
                            StoreNo = req.StoreNo,
                            Url = string.IsNullOrEmpty(req.UrlFile) ? string.Empty: req.UrlFile.Trim(),
                            FileName = string.IsNullOrEmpty(fileName) ? string.Empty: fileName.Trim(),
                            Description = req.Description,
                            FromDate = req.FromDate,
                            ToDate = req.ToDate,    
                            Status = req.Status == "1" ? true : false,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser,
                            Counter = maxCounter + 1,
                            Pkey = $"{req.FromDate.ToString("yyyyMMdd")}{"-"}{req.ToDate.ToString("yyyyMMdd")}{"-"}{req.StoreNo}"
                        };

                        db.ImageSliders.Add(ItemInsert);
                        await db.SaveChangesAsync();

                    }
                    else // chon mot vai store
                    {
                        var listStore = req.StoreList.Where(x => !req.StoreList2.Any(y => y.StoreNo == x.StoreNo)).ToList();
                        var maxCounter = await db.ImageSliders.AsNoTracking().MaxAsync(x => x.Counter) ?? 0;
                        var lstImgSlider = listStore.Select(item => new ImageSlider
                        {
                            StoreNo = item.StoreNo,
                            Url = string.IsNullOrEmpty(req.UrlFile) ? string.Empty: req.UrlFile.Trim(),
                            FileName = string.IsNullOrEmpty(fileName) ? string.Empty: fileName.Trim(),
                            Description = req.Description,
                            FromDate = req.FromDate,
                            ToDate = req.ToDate,
                            Status = req.Status == "1" ? true : false,
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now,
                            UpdatedUser = req.CreatedUser,
                            Counter = maxCounter + 1,
                            Pkey = $"{req.FromDate.ToString("yyyyMMdd")}{"-"}{req.ToDate.ToString("yyyyMMdd")}{"-"}{item.StoreNo}"
                        }).ToList();

                        db.ImageSliders.AddRange(lstImgSlider);
                        await db.SaveChangesAsync();

                    }

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Item1 = fileName.Substring(0, fileName.LastIndexOf(".")),
                        Item2 = req.Status,
                        IsStatus = 1,
                        Message = "Thêm mới thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = "Lỗi hệ thống: " + ex.Message
                };
            }
        }

        public ResultResponseModel UpdateImageSlider(UpdateSetupImageSliderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.ImageSliders.FirstOrDefault(x => x.ID == req.ID || x.Pkey == req.Pkey);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 2,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }
                    else
                    {
                        if (req.Status == "1") // True
                        {
                            var data1 = db.ImageSliders.FirstOrDefault(x => x.StoreNo == req.StoreNo && x.Status == true);
                            if (data1 != null)
                            {
                                // Không cập nhật trạng thái
                                var maxCounter = db.ImageSliders.Max(x => x.Counter);
                                data1.Url = req.UrlFile.Trim();
                                data1.FileName = req.FileName.Trim();
                                data1.Description = req.Description;
                                data1.UpdatedDate = DateTime.Now;
                                data1.UpdatedUser = req.UpdatedUser;
                                data1.Counter = maxCounter + 1;
                                data1.Pkey = $"{data1.StoreNo}{"-"}{req.FileName.Substring(0, req.FileName.LastIndexOf("."))}";

                                db.SaveChanges();
                                data1.Pkey = $"{data1.Pkey}-{data1.ID}";
                                db.SaveChanges();

                                //return new ResultResponseModel
                                //{
                                //    Status = Model.Enums.ResultEnum.Fail,
                                //    IsStatus = 2,
                                //    Message = $"ST/CH {req.StoreNo} này có Pkey: {data1.Pkey} đang có hiệu lực. Vui lòng kiểm tra lại"
                                //};

                            }

                            var data2 = db.ImageSliders.FirstOrDefault(x => x.StoreNo == req.StoreNo && x.Pkey == req.Pkey);
                            var _maxCounter = db.ImageSliders.Max(x => x.Counter);

                            data2.StoreNo = req.StoreNo;
                            data2.Url = req.UrlFile.Trim();
                            data2.FileName = req.FileName.Trim();
                            data2.Description = req.Description;
                            data2.Status = req.Status == "1" ? true : false;
                            data2.UpdatedDate = DateTime.Now;
                            data2.UpdatedUser = req.UpdatedUser;
                            data2.Counter = _maxCounter + 1;
                            data2.Pkey = $"{req.StoreNo}{"-"}{req.FileName.Substring(0, req.FileName.LastIndexOf("."))}";

                            db.SaveChanges();
                            data2.Pkey = $"{data2.Pkey}-{data2.ID}";
                            db.SaveChanges();
                        }
                        else // False
                        {
                            var data3 = db.ImageSliders.FirstOrDefault(x => x.StoreNo == req.StoreNo && x.Pkey == req.Pkey);
                            var _maxCounter = db.ImageSliders.Max(x => x.Counter);

                            data3.StoreNo = req.StoreNo;
                            data3.Url = req.UrlFile.Trim();
                            data3.FileName = req.FileName.Trim();
                            data3.Description = req.Description;
                            data3.Status = req.Status == "1" ? true : false;
                            data3.UpdatedDate = DateTime.Now;
                            data3.UpdatedUser = req.UpdatedUser;
                            data3.Counter = _maxCounter + 1;
                            data3.Pkey = $"{req.StoreNo}{"-"}{req.FileName.Substring(0, req.FileName.LastIndexOf("."))}";

                            db.SaveChanges();
                            data3.Pkey = $"{data3.Pkey}-{data3.ID}";
                            db.SaveChanges();
                        }
                    }

                    return new ResultResponseModel
                    {
                        Item1 = req.FileName,
                        Item2 = req.Status,
                        Item3 = req.StoreNo,
                        Status = Model.Enums.ResultEnum.Success,
                        IsStatus = 1,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    IsStatus = 3
                };
            }
        }

        public async Task<ResultResponseModel> UpdateImageSliderV2Async(UpdateSetupImageSliderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = await db.ImageSliders.FirstOrDefaultAsync(x => x.ID == req.ID);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 2,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }    
                    
                    var _maxCounter = await db.ImageSliders.AsNoTracking().MaxAsync(x => x.Counter);
                    data.Url = req.UrlFile.Trim();
                    data.FileName = req.FileName.Trim();
                    data.Description = req.Description;
                    data.Status = req.Status.Equals("1") ? true : false;
                    data.UpdatedDate = DateTime.Now;
                    data.UpdatedUser = req.UpdatedUser;
                    data.Counter = _maxCounter + 1;
                    data.Pkey = $"{data.StoreNo}-{req.FileName.Substring(0, req.FileName.LastIndexOf("."))}-{data.ID}";
                    await db.SaveChangesAsync();
                    
                }
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.Success,
                    IsStatus = 1,
                    Message = "Cập nhật thành công"
                };

            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    IsStatus = 3
                };
            }
        }

        // ---- New : 25/10/2023 ----
        public async Task<ResultResponseModel> UpdateImageSlider_V2(UpdateSetupImageSliderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = await db.ImageSliders.FirstOrDefaultAsync(x => x.ID == req.ID);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 2,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    var maxCounter = await db.ImageSliders.AsNoTracking().MaxAsync(x => x.Counter) ?? 0;
                    data.Url = string.IsNullOrEmpty(req.UrlFile) ? string.Empty : req.UrlFile.Trim();
                    data.FileName = string.IsNullOrEmpty(req.FileName) ? string.Empty : req.FileName.Trim();
                    data.Description = req.Description;
                    data.FromDate = req.FromDate.Date;
                    data.ToDate = req.ToDate.Date;
                    data.Status = req.Status.Equals("1") ? true : false;
                    data.UpdatedDate = DateTime.Now;
                    data.UpdatedUser = req.UpdatedUser;
                    data.Counter = maxCounter + 1;
                    data.Pkey = $"{req.FromDate.ToString("yyyyMMdd")}-{req.ToDate.ToString("yyyyMMdd")}-{data.StoreNo}";
                    await db.SaveChangesAsync();
                }
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.Success,
                    IsStatus = 1,
                    Message = "Cập nhật thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    IsStatus = 3
                };
            }
        }
        public ResultResponseModel DeleteImageSlider(DeleteSetupImageSliderModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    var data = db.ImageSliders.FirstOrDefault(x => x.ID == req.ID || x.Pkey == req.Pkey);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 2,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.ImageSliders.Remove(data);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        IsStatus = 1,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    IsStatus = 3
                };
            }
        }
        public UpdateSetupImageSliderModel ViewInfoImagesSlider(string Id, string pkey)
        {
            var result = new UpdateSetupImageSliderModel();
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    long _id = long.Parse(Id, System.Globalization.NumberStyles.AllowThousands | System.Globalization.NumberStyles.AllowLeadingSign);
                    var inforImg = db.ImageSliders.Where(d => d.ID == _id || d.Pkey == pkey).Select(a => new UpdateSetupImageSliderModel
                    {
                        ID = a.ID,
                        ImageName = a.FileName,
                        UrlFile = a.Url,
                        Pkey = a.Pkey
                    }).ToList();

                    result = new UpdateSetupImageSliderModel
                    {
                        ID = inforImg.FirstOrDefault().ID,
                        ImageName = inforImg.FirstOrDefault().ImageName,
                        UrlFile = inforImg.FirstOrDefault().UrlFile,
                        Pkey = inforImg.FirstOrDefault().Pkey,
                        ImageBase64 = inforImg.FirstOrDefault().ImageBase64
                    };
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        // 07/08/2025, tungnt8
        public string GetDescriptionImageSlider(string description)
        {
            var result = string.Empty;
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.ImageSliders.AsNoTracking().Where(x => x.Description.ToLower() == description.ToLower().Trim())?.FirstOrDefault();
                    result = data.Description;
                }
            }
            catch (Exception ex)
            {
            }
            return result;
        }

        // 07/08/2025,tungnt8
        public ResultResponseModel SaveImageSliderByBlock(ImageSliderBlockModel model)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.ImageSliders.Where(x => x.Description.ToLower() == model.Description.ToLower()).ToList();
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            IsStatus = 2,
                            Message = "Không tìm thấy thông tin tên chương trình"
                        };
                    }
                    var maxCounter = db.ImageSliders.Max(x => x.Counter) ?? 0;
                    foreach (var item in data)
                    {
                        item.Status = model.Status.Equals("1") ? true : false;
                        item.UpdatedDate = DateTime.Now;
                        item.UpdatedUser = model.UpdatedUser;
                        item.Counter = maxCounter + 1;
                    }
                    db.SaveChanges();
                }
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.Success,
                    IsStatus = 1,
                    Message = "Khóa chương trình thành công"
                };
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message,
                    IsStatus = 3
                };
            }
        }
        public List<SetupCurrencyRateListModel> GetSetupCurrencyRateList(string UnitCurrency, string Status, string ExchangeRate, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<SetupCurrencyRateListModel>("[dbo].[PLG_GET_CURRENCY_RATE_LIST] @CurrCode, @Status, @ExchangeRate, @PageSize, @PageNumber",
                        new SqlParameter("@CurrCode", UnitCurrency ?? string.Empty),
                        new SqlParameter("@Status", Status ?? string.Empty),
                        new SqlParameter("@ExchangeRate", ExchangeRate ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SetupCurrencyRateListModel>();
                    }
                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<SetupCurrencyRateListModel>();
            }
        }
        public ResultResponseModel CreateCurrencyRate(CreateCurrencyRateModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 5 * 60;
                    var checkExisted = db.CurrencyRates.Any();
                    if (!checkExisted)
                    {
                        var item = new CurrencyRate
                        {
                            CurrencyCode = req.CurrencyCode,
                            StartingDate = req.StartingDate,
                            EndingDate = req.EndingDate,
                            ExchangeRate = req.ExchangeRate.Value,
                            Status = req.Status == "1" ? true : false,
                            Counter = 1,
                            Pkey = $"{req.CurrencyCode}-{req.ExchangeRate}-{req.StartingDate.ToString("yyyyMMdd")}",
                            CreatedDate = DateTime.Now,
                            CreatedUser = req.CreatedUser,
                            UpdatedDate = DateTime.Now
                        };
                        db.CurrencyRates.Add(item);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Item1 = req.CurrencyCode,
                            Item2 = req.Status,
                            Status = Model.Enums.ResultEnum.Success,
                            Flag = 1,
                            Message = "Thêm mới thành công"
                        };
                    }

                    var pkey = $"{req.CurrencyCode}-{req.ExchangeRate}-{req.StartingDate.ToString("yyyyMMdd")}";
                    var checkKey = db.CurrencyRates.FirstOrDefault(x => x.Pkey == pkey);
                    if (checkKey != null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Flag = 0,
                            Message = $"Đã tồn tại: {pkey} này trong hệ thống. Vui lòng kiểm tra lại"
                        };
                    }

                    var checkRangeDate = db.CurrencyRates.AsNoTracking()
                           .Any(x => (req.StartingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            || (req.EndingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.EndingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && DbFunctions.TruncateTime(x.EndingDate) <= req.EndingDate.Date && x.Status == true)
                            || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.StartingDate) && DbFunctions.TruncateTime(x.StartingDate) <= req.EndingDate.Date && x.Status == true)
                           );

                    if (checkRangeDate)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Flag = 0,
                            Message = $"Pkey {req.Pkey} này có thời gian hiệu lực: {req.StartingDate.ToString("dd/MM/yyyy")} -> {req.EndingDate.ToString("dd/MM/yyyy")} không hợp lệ"
                        };
                    }

                    // Insert
                    var maxCounterInsert = db.CurrencyRates.Max(x => x.Counter);
                    var item1 = new CurrencyRate
                    {
                        CurrencyCode = req.CurrencyCode,
                        StartingDate = req.StartingDate,
                        EndingDate = req.EndingDate,
                        ExchangeRate = req.ExchangeRate.Value,
                        Status = req.Status == "1" ? true : false,
                        Counter = Convert.ToInt32(maxCounterInsert) + 1,
                        Pkey = pkey,
                        CreatedDate = DateTime.Now,
                        CreatedUser = req.CreatedUser,
                        UpdatedDate = DateTime.Now
                    };

                    db.CurrencyRates.Add(item1);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Item1 = req.CurrencyCode,
                        Item2 = req.Status,
                        Status = Model.Enums.ResultEnum.Success,
                        Flag = 1,
                        Message = "Thêm mới thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateCurrencyRate(UpdateCurrencyRateModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var item = db.CurrencyRates.Where(x => x.Pkey == req.Pkey).FirstOrDefault();
                    if (item.Status == true)
                    {
                        if (req.Status == "1") //true
                        {
                            //var checkRangeDate = db.CurrencyRates.AsNoTracking()
                            //.Any(x => (req.StartingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            //|| (req.EndingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.EndingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            //|| (req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && DbFunctions.TruncateTime(x.EndingDate) <= req.EndingDate.Date && x.Status == true)
                            //|| (req.StartingDate.Date <= DbFunctions.TruncateTime(x.StartingDate) && DbFunctions.TruncateTime(x.StartingDate) <= req.EndingDate.Date && x.Status == true)
                            //);

                            //var checkRangeDate = db.CurrencyRates.AsNoTracking()
                            //.Any(x => (req.StartingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            //|| (req.EndingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.EndingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            //|| (req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && DbFunctions.TruncateTime(x.EndingDate) <= req.EndingDate.Date && x.Status == true)
                            //|| (req.StartingDate.Date <= DbFunctions.TruncateTime(x.StartingDate) && DbFunctions.TruncateTime(x.StartingDate) <= req.EndingDate.Date && x.Status == true)
                            //&& x.Pkey != req.Pkey);


                            //var checkRangeDate = db.CurrencyRates.AsNoTracking()
                            //.Where(x => (((req.StartingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            //|| (req.EndingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.EndingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            //|| (req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && DbFunctions.TruncateTime(x.EndingDate) <= req.EndingDate.Date && x.Status == true)
                            //|| (req.StartingDate.Date <= DbFunctions.TruncateTime(x.StartingDate) && DbFunctions.TruncateTime(x.StartingDate) <= req.EndingDate.Date && x.Status == true))
                            //&& x.Pkey != req.Pkey
                            //)).ToList();


                            var checkRangeDate = db.CurrencyRates.AsNoTracking()
                            .Any(x => (((req.StartingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            || (req.EndingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.EndingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                            || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && DbFunctions.TruncateTime(x.EndingDate) <= req.EndingDate.Date && x.Status == true)
                            || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.StartingDate) && DbFunctions.TruncateTime(x.StartingDate) <= req.EndingDate.Date && x.Status == true))
                            && x.Pkey != req.Pkey
                            ));

                            if (checkRangeDate)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Flag = 0,
                                    Message = $"Pkey {req.Pkey} này có thời gian hiệu lực: {req.StartingDate.ToString("dd/MM/yyyy")} -> {req.EndingDate.ToString("dd/MM/yyyy")} không hợp lệ"
                                };
                            }
                        }
                        else
                        {
                            var checkRangeDate = db.CurrencyRates.AsNoTracking()
                             .Any(x => (req.StartingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true && x.Pkey != req.Pkey)
                              || (req.EndingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.EndingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true && x.Pkey != req.Pkey)
                              || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && DbFunctions.TruncateTime(x.EndingDate) <= req.EndingDate.Date && x.Status == true && x.Pkey != req.Pkey)
                              || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.StartingDate) && DbFunctions.TruncateTime(x.StartingDate) <= req.EndingDate.Date && x.Status == true && x.Pkey != req.Pkey)
                             );

                            if (checkRangeDate)
                            {
                                return new ResultResponseModel
                                {
                                    Status = Model.Enums.ResultEnum.Fail,
                                    Flag = 0,
                                    Message = $"Pkey {req.Pkey} này có thời gian hiệu lực: {req.StartingDate.ToString("dd/MM/yyyy")} -> {req.EndingDate.ToString("dd/MM/yyyy")} không hợp lệ"
                                };
                            }
                        }                       
                    }
                    else // TH het hieu luc
                    {
                        if (item.Status == false && req.Status == "0")
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Flag = 0,
                                Message = $"Không được phép cập nhật trạng thái hết hiệu lực khi Pkey {req.Pkey} này đang hết hiệu lực."
                            };
                        }

                        var checkRangeDate = db.CurrencyRates.AsNoTracking()
                            .Any(x => (req.StartingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                             || (req.EndingDate.Date >= DbFunctions.TruncateTime(x.StartingDate) && req.EndingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && x.Status == true)
                             || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.EndingDate) && DbFunctions.TruncateTime(x.EndingDate) <= req.EndingDate.Date && x.Status == true)
                             || (req.StartingDate.Date <= DbFunctions.TruncateTime(x.StartingDate) && DbFunctions.TruncateTime(x.StartingDate) <= req.EndingDate.Date && x.Status == true)
                            );

                        if (checkRangeDate)
                        {
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Fail,
                                Flag = 0,
                                Message = $"Pkey {req.Pkey} này có thời gian hiệu lực: {req.StartingDate.ToString("dd/MM/yyyy")} -> {req.EndingDate.ToString("dd/MM/yyyy")} không hợp lệ"
                            };
                        }
                    }
                    
                    // Update
                    var maxCounterUpdate = db.CurrencyRates.Max(y => y.Counter);
                    item.EndingDate = req.EndingDate;
                    item.Status = req.Status == "1" ? true : false;
                    item.Counter = Convert.ToInt32(maxCounterUpdate) + 1;
                    item.ExchangeRate = req.ExchangeRate.Value;
                    item.Pkey = $"{req.CurrencyCode}-{req.ExchangeRate}-{req.StartingDate.ToString("yyyyMMdd")}";
                    item.UpdatedDate = DateTime.Now;
                    item.UpdatedUser = req.UpdatedUser;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.CurrencyCode,
                        Item2 = req.Status,
                        Status = Model.Enums.ResultEnum.Success,
                        Flag = 1,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }

        }

        public List<QRInformationModel> GetInformationQRList (DateTime fromDate, DateTime toDate, string storeNo, string posID, string textSearch, string status, string contentType, out int totalRecord, int pageIndex = 0, int pageSize = 50)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<QRInformationModel>("[dbo].[GET_QR_INFORMATION_LIST] @FromDate, @ToDate, @StoreNo, @POSID, @TextSearch, @Status, @ContentType, @PageSize, @PageNumber",
                        new SqlParameter("@FromDate", fromDate),
                        new SqlParameter("@ToDate", toDate),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@POSID", posID ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@ContentType", contentType ?? string.Empty),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<QRInformationModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;

                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<QRInformationModel>();
            }
        }
        public ResultResponseModel CreateInformationQRList(CreateInformationQRModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var _total = db.Marketings.Count();
                    var _maxCounter = db.Marketings.Max(x => x.Counter);

                    if (_total == 0)
                    {
                        var item1 = new Marketing
                        {
                            POSTerminal = req.POSID,
                            StoreNo = req.StoreNo,
                            QRLink = req.QRLink,
                            Content = req.Content,
                            Order = req.Order,
                            FromDate = req.FromDate,
                            ToDate = req.ToDate,
                            IsActive = req.IsActive,
                            ContentType = req.ContentType,
                            Counter = 1,
                            Pkey = req.POSID,
                            CreatedBy = req.CreatedBy,
                            CreatedDate = DateTime.Now,
                            UpdatedBy = string.Empty,
                            UpdatedDate = new DateTime(9999, 12, 31)
                            //Status = Convert.ToBoolean(string.Empty)
                        };

                        db.Marketings.Add(item1);
                        db.SaveChanges();
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Success,
                            Message = "Thêm mới thành công"
                        };

                    }
                    else
                    {
                        var checkPOSID = db.Marketings.FirstOrDefault(x => x.POSTerminal == req.POSID && x.IsActive == true);
                        if (checkPOSID == null)  // Them moi
                        {
                            var item2 = new Marketing
                            {
                                POSTerminal = req.POSID,
                                StoreNo = req.StoreNo,
                                QRLink = req.QRLink,
                                Content = req.Content,
                                Order = req.Order,
                                FromDate = req.FromDate,
                                ToDate = req.ToDate,
                                IsActive = req.IsActive,
                                ContentType = req.ContentType,
                                Counter = Convert.ToInt32(_maxCounter) + 1,
                                Pkey = req.POSID,
                                CreatedBy = req.CreatedBy,
                                CreatedDate = DateTime.Now,
                                UpdatedBy = string.Empty,
                                UpdatedDate = new DateTime(9999, 12, 31)
                                //Status = req.IsActive
                            };

                            db.Marketings.Add(item2);
                            db.SaveChanges();
                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Success,
                                Message = "Thêm mới thành công"
                            };
                        }
                        else
                        {
                            var item3 = new Marketing
                            {
                                POSTerminal = req.POSID,
                                StoreNo = req.StoreNo,
                                QRLink = req.QRLink,
                                Content = req.Content,
                                Order = req.Order,
                                FromDate = req.FromDate,
                                ToDate = req.ToDate,
                                IsActive = req.IsActive,
                                ContentType = req.ContentType,
                                Counter = Convert.ToInt32(_maxCounter) + 1,
                                Pkey = req.POSID,
                                CreatedBy = req.CreatedBy,
                                CreatedDate = DateTime.Now,
                                UpdatedBy = req.CreatedBy,
                                UpdatedDate = DateTime.Now
                                //Status = req.IsActive
                            };

                            db.Marketings.Add(item3);
                            db.SaveChanges();

                            return new ResultResponseModel
                            {
                                Status = Model.Enums.ResultEnum.Success,
                                Message = "Thêm mới thành công"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateQRInformationList(UpdateInformationQRModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Marketings.FirstOrDefault(x => x.ID == req.ID);
                    var maxCounter = db.Marketings.Max(x => x.Counter);

                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    data.QRLink = req.QRLink;
                    data.Content = req.Content;
                    data.Order = req.Order;
                    data.FromDate = req.FromDate;
                    data.ToDate = req.ToDate;
                    data.IsActive = req.IsActive;
                    data.ContentType = req.ContentType;
                    data.Counter = Convert.ToInt32(maxCounter) + 1;
                    data.UpdatedDate = DateTime.Now;
                    data.UpdatedBy = req.UpdatedBy;

                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeleteQRInformationList(DeleteInformationQRModel req)
        {
            try
            {
                using (var db = new CentralMDPartnerContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Marketings.FirstOrDefault(x => x.ID == req.ID);
                    if (data == null)
                    {
                        return new ResultResponseModel {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.Marketings.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public List<AdminUserLoginPOSResponseModel> GetUserLoginPOSWebList(string storeNo, string posTerminal, string typePOS, string textSearch, out int totalRecord, int pageIndex, int pageSize)
        {
            try
            {
                using (var db = new PLHWebMobileContainer())  // DB: PLHWebMobile
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;

                    var data = db.Database.SqlQuery<AdminUserLoginPOSResponseModel>("[dbo].[WEB_POS_USER_ADMIN_LOGIN] @StoreNo, @PosTerminal, @TypePOS, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@TypePOS", typePOS ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Export", 1),
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<AdminUserLoginPOSResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception ex)
            {
                totalRecord = 0;
                return new List<AdminUserLoginPOSResponseModel>();
            }
        }

        public List<ExportAdminUserLoginPOSResponseModel> ExportExcel_UserLoginPOSWebList(string storeNo, string posTerminal, string typePOS, string textSearch)
        {
            try
            {
                using (var db = new PLHWebMobileContainer())  // DB: PLHWebMobile
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.Database.SqlQuery<ExportAdminUserLoginPOSResponseModel>("[dbo].[WEB_POS_USER_ADMIN_LOGIN] @StoreNo, @PosTerminal, @TypePOS, @TextSearch, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@PosTerminal", posTerminal ?? string.Empty),
                        new SqlParameter("@TypePOS", typePOS ?? string.Empty),
                        new SqlParameter("@TextSearch", textSearch ?? string.Empty),
                        new SqlParameter("@Export", 2),
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportAdminUserLoginPOSResponseModel>();
                    }

                    return data;
                }
            }
            catch (Exception ex)
            {
                return new List<ExportAdminUserLoginPOSResponseModel>();
            }
        }

        public ResultResponseModel DeleteUserLoginPOSWeb(DeleteAdminUserLoginPOSWebModel req)
        {
            try
            {
                using (var db = new PLHWebMobileContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.AdminUserLoginPOS.FirstOrDefault(x => (x.UserName == req.UserName || x.StaffCode == req.StaffCode) && x.PosNo == req.PosNo);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu để xóa"
                        };
                    }

                    db.AdminUserLoginPOS.Remove(data);
                    db.SaveChanges();

                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public List<SetupPrintByPOSWebResponseModel> SetupPrintByPOSWebList(string ipAddress, string storeNo, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            try
            {
                using (var db = new PLHWebMobileContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    totalRecord = 0;
                    var data = db.Database.SqlQuery<SetupPrintByPOSWebResponseModel>("[dbo].[WEB_POS_SETUP_PRINT_LIST] @IPAddress, @StoreNo, @Status, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@IPAddress", ipAddress == null ? string.Empty : ipAddress ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Export", 1),  // Không export
                        new SqlParameter("@PageSize", pageSize),
                        new SqlParameter("@PageNumber", pageIndex)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<SetupPrintByPOSWebResponseModel>();
                    }

                    totalRecord = data.FirstOrDefault().Total;
                    return data;
                }
            }
            catch (Exception)
            {
                totalRecord = 0;
                return new List<SetupPrintByPOSWebResponseModel>();
            }
        }

        public List<ExportEExcelSetupPrintByPOSWebModel> ExportExcel_SetupPrintByPOSWeb(string ipAddress, string storeNo, string status)
        {
            try
            {
                using (var db = new PLHWebMobileContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    //totalRecord = 0;
                    var data = db.Database.SqlQuery<ExportEExcelSetupPrintByPOSWebModel>("[dbo].[WEB_POS_SETUP_PRINT_LIST] @IPAddress, @StoreNo, @Status, @Export, @PageSize, @PageNumber",
                        new SqlParameter("@IPAddress", ipAddress == null ? string.Empty : ipAddress ?? string.Empty),
                        new SqlParameter("@StoreNo", storeNo ?? string.Empty),
                        new SqlParameter("@Status", status ?? string.Empty),
                        new SqlParameter("@Export", 2),  // export excel
                        new SqlParameter("@PageSize", string.Empty),
                        new SqlParameter("@PageNumber", string.Empty)).ToList();

                    if (data == null || data.Count == 0)
                    {
                        return new List<ExportEExcelSetupPrintByPOSWebModel>();
                    }
                    return data;
                }
            }
            catch (Exception)
            {
                //totalRecord = 0;
                return new List<ExportEExcelSetupPrintByPOSWebModel>();
            }
        }

        public ResultResponseModel CreateSetupPrintByPOSWeb(CreateSetupPrintByPOSWebModel req)
        {
            try
            {   
                using (var db = new PLHWebMobileContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;

                    var checkIP = db.StoreSetupPrints.FirstOrDefault(x => x.IPAddress == req.IPAddress);
                    if (checkIP != null)
                    {
                        return new ResultResponseModel
                        {
                            Item1 = req.IPAddress,
                            Status = Model.Enums.ResultEnum.warning,
                            Message = $"IP {req.IPAddress} này đã tồn tại trong hệ thống. Vui lòng kiểm tra lại"
                        };
                    }

                    var item = new StoreSetupPrint
                    {
                        IPAddress = req.IPAddress,
                        StoreNo = req.StoreNo,
                        Status = req.Status == "1" ? true: false
                    };

                    db.StoreSetupPrints.Add(item);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.IPAddress,
                        Item2 = req.StoreNo,
                        Item3 = req.Status,
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Thêm mới thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel UpdateSetupPrintByPOSWeb(UpdateSetupPrintByPOSWebModel req)
        {
            try
            {
                using (var db = new PLHWebMobileContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.StoreSetupPrints.FirstOrDefault(x => x.IPAddress == req.IPAddress);
                    data.Status = req.Status == "1" ? true : false;
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Item1 = req.IPAddress,
                        Item2 = req.StoreNo,
                        Item3 = req.Status,
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Cập nhật thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

        public ResultResponseModel DeleteSetupPrintByPOSWeb(DeleteSetupPrintByPOSWebModel req)
        {
            try
            {
                using (var db = new PLHWebMobileContainer())
                {
                    db.Database.CommandTimeout = 2 * 60;
                    var data = db.StoreSetupPrints.FirstOrDefault(x => x.IPAddress == req.IPAddress);
                    if (data == null)
                    {
                        return new ResultResponseModel
                        {
                            Status = Model.Enums.ResultEnum.Fail,
                            Message = "Không tìm thấy dữ liệu"
                        };
                    }

                    db.StoreSetupPrints.Remove(data);
                    db.SaveChanges();
                    return new ResultResponseModel
                    {
                        Status = Model.Enums.ResultEnum.Success,
                        Message = "Xóa IPAddress máy in thành công"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultResponseModel
                {
                    Status = Model.Enums.ResultEnum.ErrorSystem,
                    Message = ex.Message
                };
            }
        }

















    }
}
