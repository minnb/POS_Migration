using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Common;
using VCM.BLUEPOS.Model.Authen;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.MCH;
using VCM.BLUEPOS.Model.StoreActivities;
using VCM.BLUEPOS.Model.Home;
using VCM.BLUEPOS.Model.Common.DocumentTypeModel;

namespace VCM.BLUEPOS.Business.Common
{
    public interface ICommonBLO
    {
        string GetIPSetServerByStore(string storeNo);
        List<DocumentTypeModel> GetComboxDocumentType();
        List<OptionModel> GetComboxArticleType();
        List<OptionModel> GetComboxUnitOfMeasure();
        List<OptionModel> GetComboxVoucherCouponList();
        List<StoreModel> GetComboxStoreByUser(string userName);
        Tuple<bool, Model.Employee.CheckEmployeeModel> CheckStaffCode(string staffCode);
        List<OptionModel> GetPermisionPOSTerminal();
        List<OptionModel> GetComboxProvince();
        List<OptionModel> GetComboxStoreList();
        List<OptionModel> LoadComboxBankList();
        List<OptionModel> LoadComboxPOSTerminalByStore(string storeNo);
        List<OptionModel> GetOrderType();
        List<RoleModel> GetRolePermissionByUser(string userName);
        List<SQLServerModel> GetInforStoreSet(string storeNo);
        List<SQLServerModel> ListSQLServer();
        List<IPAddressPOSTerminalByStoreModel> LoadIPAddressPOSTerminalByStore(string storeNo);
        List<IPAddressPOSTerminalByStoreModel> LoadPOSTerminalByStore(string storeNo);
        List<ArraySiteNoModel> LoadComboxStoreByUserName(string userLogin);
        List<ArraySiteNoModel> LoadStoreByUserNameSetDB(string userLogin, string ipServer, string ipWriteLog = "");
        List<ArraySiteNoModel> LoadStoreBySetServerChanelNo(string userLogin, string ipServer, string chanelNo);
        string GetIPAddessByPOSTerminal(string posID);
        Tuple<bool, ResultPingIPLocalModel> CheckPingIPLocal(string IPAddress);
        List<OptionModel> GetPaymentType();
        List<StaffCodeResponseModel> LoadComboxStaffCode(string storeNo);
        Task<VCM.BLUEPOS.Model.Employee.StaffModel> InfoStaffByAD(string userAD);
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
        List<ArraySiteNoModel> LoadComboxStoreByChanel(string chanelNo);
        List<ArraySiteNoModel> LoadComboxSiteByChanelAndStore(string chanelNo, string storeNo);
        List<OptionModel> LoadComboxPOSVersionSource();
        List<OptionModel> LoadComboxPOSVersionList();
        List<OptionModel> LoadComboxPOSVersionIsUpdate();
        List<StaffCodeModel> ListStaffCode(string storeNo);
        List<SQLServerModel> ListSQLServerExcute();
        List<IPAddressModel> GetListIP(string storeNo, string IpAddress, string computerName, out int totalRecord, int skip, int take);
        List<DataTable> getDataByStore(string storeNo, string groupTable, string typeCounter, long posLastCounter);
        void DeleteAll(string ConnectStringStore, List<DataTable> lstTable);
        void DeleteByPkey(string ConnectStringStore, List<DataTable> lstTable);
        int InsertDataBySQL(DataTable dtData, string ConnectStringStore, string storeNo, string ipPOS);
        SalesType LoadComboxSalesType();
        List<OptionModel> GetVoucherTenderType();
        SalesType LoadSalesOrderType();
        List<OptionModel> LoadTenderTypeForSalesType();
        List<OptionModel> LoadPaymentTenderTypeForSalesType();
        List<POSDataSetupModel> ListPOSSetup();
        IPAddressModel GetIPAddressByPOSTerminal(string posTerminal);
        List<OptionModel> LoadComboxEWalletList();
        List<ServerWebModel> ListServerWeb(string userAD, string ipWriteLog = "");
        List<OptionModel> LoadComboxSalesOrderType();
        ViewCheckListDailyModel CheckList();
        Tuple<bool, string> CheckListUser(CheckListUserRequest model);
        List<OptionModel> LoadComboxCategoryList();
        string LoadTenderTypeByWallet(string walletName);
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
        List<OptionDataModel> LoadComboxCupType(); // loai ly
        List<string> CheckSecurityStoreByUserName(string userName, string listStore);
        List<OptionSiteModel> LoadComboxStoreNoWinLife(string userName);
        List<OptionDataModel> LoadComboxMemberCodeType();
        StoreListSpecialComboModel LoadStoreByUpdateSpecialCombo(string channel, string code);
    }

    public class CommonBLO : ICommonBLO
    {
        private CommonData _data { get; set; }
        public CommonBLO()
        {
            _data = new CommonData();
        }
        public string GetIPSetServerByStore(string storeNo)
        {
            return _data.GetIPSetServerByStore(storeNo);
        }
        public List<DocumentTypeModel> GetComboxDocumentType()
        {
            return _data.GetComboxDocumentType();
        }
        public List<OptionModel> GetComboxArticleType()
        {
            return _data.GetComboxArticleType();
        }
        public List<OptionModel> GetComboxUnitOfMeasure()
        {
            return _data.GetComboxUnitOfMeasure();
        }
        public List<OptionModel> GetComboxVoucherCouponList()
        {
            return new List<OptionModel>
            {
                new OptionModel
                {
                     Value = "ZCPN",
                     Text = "ZCPN - Coupon"
                },
                new OptionModel
                {
                     Value = "ZVCP",
                     Text = "ZVCP - Voucher giấy"
                },
                new OptionModel
                {
                     Value = "ZVCE",
                     Text = "ZVCE - E-Voucher"
                },
                new OptionModel
                {
                     Value = "ZPPC",
                     Text = "ZPPC - Thẻ trả trước"
                }

                //new OptionModel
                //{
                //     Value = "ZCOU",
                //     Text = "ZCOU"
                //},
                
                //new OptionModel
                //{
                //     Value = "ZVCN",
                //     Text = "ZVCN"
                //},
                //new OptionModel
                //{
                //     Value = "ZVCO",
                //     Text = "ZVCO"
                //}
                
            };
        }
        public List<StoreModel> GetComboxStoreByUser(string userName)
        {
            return _data.GetComboxStoreByUser(userName);
        }
        public Tuple<bool, Model.Employee.CheckEmployeeModel> CheckStaffCode(string staffCode)
        {
            return _data.CheckStaffCode(staffCode);
        }
        public List<OptionModel> GetPermisionPOSTerminal()
        {
            return _data.GetPermisionPOSTerminal();
        }
        public List<OptionModel> GetComboxProvince()
        {
            return _data.GetComboxProvince();
        }
        public List<OptionModel> GetComboxStoreList()
        {
            return _data.GetComboxStoreList();
        }
        public List<OptionModel> LoadComboxBankList()
        {
            return _data.LoadComboxBankList();
        }
        public List<OptionModel> LoadComboxPOSTerminalByStore(string storeNo)
        {
            return _data.LoadComboxPOSTerminalByStore(storeNo);
        }
        public List<OptionModel> GetOrderType()
        {
            return _data.GetOrderType();
        }
        public List<RoleModel> GetRolePermissionByUser(string userName)
        {
            return _data.GetRolePermissionByUser(userName);
        }
        public List<SQLServerModel> GetInforStoreSet(string storeNo)
        {
            return _data.GetInforStoreSet(storeNo);
        }
        public List<SQLServerModel> ListSQLServer()
        {
            return _data.ListSQLServer();
        }
        public List<IPAddressPOSTerminalByStoreModel> LoadIPAddressPOSTerminalByStore(string storeNo)
        {
            return _data.LoadIPAddressPOSTerminalByStore(storeNo);
        }
        public List<IPAddressPOSTerminalByStoreModel> LoadPOSTerminalByStore(string storeNo)
        {
            return _data.LoadPOSTerminalByStore(storeNo);
        }
        public List<ArraySiteNoModel> LoadComboxStoreByUserName(string userLogin)
        {
            return _data.LoadComboxStoreByUserName(userLogin);
        }
        public List<ArraySiteNoModel> LoadStoreByUserNameSetDB(string userLogin, string ipServer, string ipWriteLog = "")
        {
            return _data.LoadStoreByUserNameSetDB(userLogin, ipServer, ipWriteLog);
        }
        public List<ArraySiteNoModel> LoadStoreBySetServerChanelNo(string userLogin, string ipServer, string chanelNo)
        {
            return _data.LoadStoreBySetServerChanelNo(userLogin, ipServer, chanelNo);
        }
        public string GetIPAddessByPOSTerminal(string posID)
        {
            return _data.GetIPAddessByPOSTerminal(posID);
        }
        public Tuple<bool, ResultPingIPLocalModel> CheckPingIPLocal(string IPAddress)
        {
            return _data.CheckPingIPLocal(IPAddress);
        }
        public List<OptionModel> GetPaymentType()
        {
            return _data.GetPaymentType();
        }
        public List<StaffCodeResponseModel> LoadComboxStaffCode(string storeNo)
        {
            return _data.LoadComboxStaffCode(storeNo);
        }
        public Task<VCM.BLUEPOS.Model.Employee.StaffModel> InfoStaffByAD(string userAD)
        {
            return _data.InfoStaffByAD(userAD);
        }
        public List<VCM.BLUEPOS.Model.Employee.StaffModel> InfoStaffByStore(string storeNo)
        {
            return _data.InfoStaffByStore(storeNo);
        }
        public VCM.BLUEPOS.Model.Common.StyleProfileModel GetStyleProfile(string storeNo)
        {
            return _data.GetStyleProfile(storeNo);
        }
        public string InsertBulk(DataTable table, string TableName, string storeNo, string orderNo = "", string posID = "")
        {
            return _data.InsertBulk(table, TableName, storeNo, orderNo, posID);
        }
        public void InsertTrans_Invoice(string storeNo, string OrderNo)
        {
            _data.InsertTrans_Invoice(storeNo, OrderNo);
        }
        public void InsertLogErrorWeb(LogErrorWebModel model)
        {
            _data.InsertLogErrorWeb(model);
        }
        public List<MCHComboboxModel> LoadComboxNganhHang()
        {
            return _data.LoadComboxNganhHang();
        }
        public List<MCHComboboxModel> LoadComboxMCH5(string MCH2)
        {
            return _data.LoadComboxMCH5(MCH2);
        }
        public List<VoucherReceiptTypeModel> GetVoucherReceiptType()
        {
            return _data.GetVoucherReceiptType();
        }
        public List<SyncTableListModel> ListTableSync()
        {
            return _data.ListTableSync();
        }
        public List<SyncJobLogModel> ListLogSyncJob(string processID)
        {
            return _data.ListLogSyncJob(processID);
        }
        public List<SAPConfigXMLModel> ListConfigXMLSAP()
        {
            return _data.ListConfigXMLSAP();
        }
        public List<ProvinceModel> GetComboxProvinceList()
        {
            return _data.GetComboxProvinceList();
        }
        public List<ArraySiteNoModel> LoadComboxStoreNo()
        {
            return _data.LoadComboxStoreNo();
        }
        public Tuple<bool, CheckStoreModel> CheckStoreNo(string storeNo)
        {
            return _data.CheckStoreNo(storeNo);
        }
        public List<ArraySiteNoModel> LoadComboxStoreByChanel(string chanelNo)
        {
            return _data.LoadComboxStoreByChanel(chanelNo);
        }
        public List<ArraySiteNoModel> LoadComboxSiteByChanelAndStore(string chanelNo, string storeNo)
        {
            return _data.LoadComboxSiteByChanelAndStore(chanelNo, storeNo);
        }
        public List<OptionModel> LoadComboxPOSVersionSource()
        {
            return _data.LoadComboxPOSVersionSource();
        }
        public List<OptionModel> LoadComboxPOSVersionList()
        {
            return _data.LoadComboxPOSVersionList();
        }
        public List<OptionModel> LoadComboxPOSVersionIsUpdate()
        {
            return _data.LoadComboxPOSVersionIsUpdate();
        }
        public List<StaffCodeModel> ListStaffCode(string storeNo)
        {
            return _data.ListStaffCode(storeNo);
        }
        public List<SQLServerModel> ListSQLServerExcute()
        {
            return _data.ListSQLServerExcute();
        }
        public List<IPAddressModel> GetListIP(string storeNo, string IpAddress, string computerName, out int totalRecord, int skip, int take)
        {
            return _data.GetListIP(storeNo, IpAddress, computerName, out totalRecord, skip, take);
        }
        public List<DataTable> getDataByStore(string storeNo, string groupTable, string typeCounter, long posLastCounter)
        {
            return _data.getDataByStore(storeNo, groupTable, typeCounter, posLastCounter);
        }
        public void DeleteAll(string ConnectStringStore, List<DataTable> lstTable)
        {
            _data.DeleteAll(ConnectStringStore, lstTable);
        }
        public void DeleteByPkey(string ConnectStringStore, List<DataTable> lstTable)
        {
            _data.DeleteByPkey(ConnectStringStore, lstTable);
        }
        public int InsertDataBySQL(DataTable dtData, string ConnectStringStore, string storeNo, string ipPOS)
        {
            return _data.InsertDataBySQL(dtData, ConnectStringStore, storeNo, ipPOS);
        }
        public SalesType LoadComboxSalesType()
        {
            return _data.LoadComboxSalesType();
        }
        public List<OptionModel> GetVoucherTenderType()
        {
            return _data.GetVoucherTenderType();
        }
        public SalesType LoadSalesOrderType()
        {
            return _data.LoadSalesOrderType();
        }
        public List<OptionModel> LoadTenderTypeForSalesType()
        {
            return _data.LoadTenderTypeForSalesType();
        }
        public List<OptionModel> LoadPaymentTenderTypeForSalesType()
        {
            return _data.LoadPaymentTenderTypeForSalesType();
        }
        public List<POSDataSetupModel> ListPOSSetup()
        {
            return _data.ListPOSSetup();
        }
        public IPAddressModel GetIPAddressByPOSTerminal(string posTerminal)
        {
            return _data.GetIPAddressByPOSTerminal(posTerminal);
        }
        public List<OptionModel> LoadComboxEWalletList()
        {
            return _data.LoadComboxEWalletList();
        }
        public List<ServerWebModel> ListServerWeb(string userAD, string ipWriteLog = "")
        {
            return _data.ListServerWeb(userAD, ipWriteLog);
        }
        public List<OptionModel> LoadComboxSalesOrderType()
        {
            return _data.LoadComboxSalesOrderType();
        }
        public ViewCheckListDailyModel CheckList()
        {
            return _data.CheckList();
        }
        public Tuple<bool, string> CheckListUser(CheckListUserRequest model)
        {
            return _data.CheckListUser(model);
        }
        public List<OptionModel> LoadComboxCategoryList()
        {
            return _data.LoadComboxCategoryList();
        }
        public string LoadTenderTypeByWallet(string EWalletCode)
        {
            return _data.LoadTenderTypeByWallet(EWalletCode);
        }
        public List<OptionModel> GetComboxStoreListByWinLife()
        {
            return _data.GetComboxStoreListByWinLife();
        }
        public Tuple<bool, string, List<ReportCheckListResponse>> ReportCheckList(DateTime fromDate, DateTime toDate)
        {
            return _data.ReportCheckList(fromDate, toDate);
        }
        public List<ScanOrderLoadModal> ScanOrderLoad(string serverIP, string storeNo, DateTime bussinessDate, string orderNo, int pageSize, int pageNumber)
        {
            return _data.ScanOrderLoad(serverIP, storeNo, bussinessDate, orderNo, pageSize, pageNumber);
        }
        public Tuple<int, string, ScanOrderInsertResponseModel> ScanOrderInsert(string storeNo, string barcode, string orderNo, string itemNo, string userName, string serverStore)
        {
            return _data.ScanOrderInsert(storeNo, barcode, orderNo, itemNo, userName, serverStore);
        }
        public Tuple<int, string> ScanOrderDelete(string storeNo, string orderNo, string userName, long id)
        {
            return _data.ScanOrderDelete(storeNo, orderNo, userName, id);
        }
        public List<OptionModel> LoadPaymentTypeForEWalletList()
        {
            return _data.LoadPaymentTypeForEWalletList();
        }
        public List<IPAddressPOSTerminalByStoreModel> LoadPOSTerminalByStoreSetupPrint(string storeNo)
        {
            return _data.LoadPOSTerminalByStoreSetupPrint(storeNo);
        }
        public List<IPAddressModel> GetIPAddressList(string storeNo, string IpAddress, string computerName, out int totalRecord, int skip, int take)
        {
            return _data.GetIPAddressList(storeNo, IpAddress, computerName, out totalRecord, skip, take);
        }
        public List<StorePartnerModel> LoadComboxStoreNoByUser(string userName)
        {
            return _data.LoadComboxStoreNoByUser(userName);
        }
        public List<string> GetFromChangeSalesType()
        {
            return _data.GetFromChangeSalesType();
        }
        public List<OptionModel> GetToChangeSalesType()
        {
            return _data.GetToChangeSalesType();
        }
        public List<OptionModel> GetComboxMemberCardType()
        {
            return _data.GetComboxMemberCardType();
        }
        public List<OptionDataBySalesTypeModel> LoadSalesTypeByMultipleCheck()
        {
            return _data.LoadSalesTypeByMultipleCheck();
        }
        public List<OptionDataModel> GetComboxOptionData(string caption)
        {
            return _data.GetComboxOptionData(caption);
        }
        public List<StorePartnerModel> LoadComboxStoreByUserChannel(string userName, string channelSales)
        {
            return _data.LoadComboxStoreByUserChannel(userName, channelSales);
        }
        public List<OptionSiteModel> LoadComboxStoreNoByWinLife()
        {
            return _data.LoadComboxStoreNoByWinLife();
        }
        public List<VoucherTypeModel> LoadComboxVoucherType()
        {
            return _data.LoadComboxVoucherType();
        }
        public List<ArraySiteNoModel> LoadStoreByUserName(string ipServer, string storeNo, string userName)
        {
            return _data.LoadStoreByUserName(ipServer, storeNo, userName);
        }
        public List<OptionDataModel> LoadComboxCupType()
        {
            return _data.LoadComboxCupType();
        }
        public List<string> CheckSecurityStoreByUserName(string userName, string listStore)
        {
            return _data.CheckSecurityStoreByUserName(userName, listStore);
        }
        public List<OptionSiteModel> LoadComboxStoreNoWinLife(string userName)
        {
            return _data.LoadComboxStoreNoWinLife(userName);
        }
        public List<OptionDataModel> LoadComboxMemberCodeType()
        {
            return _data.LoadComboxMemberCodeType();
        }
        public StoreListSpecialComboModel LoadStoreByUpdateSpecialCombo(string channel, string code)
        {
            return _data.LoadStoreByUpdateSpecialCombo(channel, code);
        }
        




    }
}
