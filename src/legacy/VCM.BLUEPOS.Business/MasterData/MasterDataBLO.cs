using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Common;
using System.Data;
using VCM.BLUEPOS.Data.MasterData;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Employee;
using VCM.BLUEPOS.Model.Store;
using VCM.BLUEPOS.Model.POS;
using VCM.BLUEPOS.Model.Bank;
using VCM.BLUEPOS.Model.Voucher;
using VCM.BLUEPOS.Model.MasterData;


namespace VCM.BLUEPOS.Business.MasterData
{
    public interface IMasterDataBLO
    {
        List<EmployeeListResponseModel> GetEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportEmployeeListModel> ExportEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status);
        ResultResponseModel CreateEmployeeList(CreateEmployeeListModel req);
        ResultResponseModel UpdateEmployeeList(UpdateEmployeeListModel req);
        ResultResponseModel DeleteEmployeeList(DeleteEmployeeListModel req);
        List<SetupPOSListResponseModel> GetSetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportSetupPOSListModel> ExportExcel_SetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status);
        ResultResponseModel CreatePOSTerminalList(CreatePOSTerminalListModel req);
        ResultResponseModel UpdatePOSTerminalList(UpdatePOSTerminalListModel req);
        ResultResponseModel DeletePOSterminalList(DeletePOSTerminalListModel req);
        string GetMaxPosIDPOSTerminal(string storeNo);
        Tuple<int, string> ImportExcelPOSTerminal(string userName, DataTable dt);
        List<StoreListResponseModel> GetStoreList(string provinceNo, string chanelNo, string storeNo, string taxCode, out int totalRecord, int pageIndex = 1, int pageSize = 100);
        List<ExportExcelStoreListModel> ExportExcel_StoreList(string provinceNo, string chanelNo, string storeNo, string taxCode);
        ResultResponseModel DeleteStoreList(DeleteStoreListModel req);
        ResultResponseModel UpdateStoreList(UpdateStoreListModel req);
        ResultResponseModel CreateStoreList(CreateStoreListModel req);
        Tuple<bool, CheckStoreListModel> CheckStoreNo(string storeNo);
        List<BankListResponseModel> GetBankList(string bankCode, string bankName, out int totalRecord, int skip = 1, int take = 100);
        ResultResponseModel DeleteBankList(DeleteBankModel req);
        ResultResponseModel CreateBankList(CreateBankModel req);
        ResultResponseModel UpdateBankList(UpdateBankModel req);
        List<ExportBankListModel> ExportGetBankList(string BankCode, string BankName);
        ResultResponseModel CreateBankPOSList(CreateBankPOSListModel req);
        ResultResponseModel UpdateBankPOSList(UpdateBankPOSListModel req);
        ResultResponseModel DeleteBankPOSList(DeleteBankPOSListModel req);
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
        Tuple<bool, CheckProvinceResponseModel> CheckProvinceCode(string provinceCode);
        ResultResponseModel DeleteProvinceList(DeleteProvinceResponseModel req);
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
        Tuple<int, string> ImportExcelEWallet(string userName, DataTable dt);
        List<ChangePassWordModel> ChangePassWordList(string storeNo, string userName, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        ResultResponseModel ChangePassWord(ChangePassWordModel req);
        List<SetupImageSliderResponseModel> GetSetupImageSliderList(string styleProfile, string storeNo, string description, string fileName, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100);
        List<ExportExcel_ImagesSliderListResponseModel> ExportExcel_ImagesSliderList(string storeNo, string styleProfile, string description, string fileName, string status);
        Task<ResultResponseModel> CreateImageSliderAsync(CreateSetupImageSliderModel req);
        Task<ResultResponseModel> UpdateImageSliderAsync(UpdateSetupImageSliderModel req);
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
        List<SetupPrintByPOSWebResponseModel> SetupPrintByPOSWebList(string ipAddress, string storeNo, string status, out int totalRecord, int pageIndex, int pageSize);
        List<ExportEExcelSetupPrintByPOSWebModel> ExportExcel_SetupPrintByPOSWeb(string ipAddress, string storeNo, string status);
        ResultResponseModel CreateSetupPrintByPOSWeb(CreateSetupPrintByPOSWebModel req);
        ResultResponseModel UpdateSetupPrintByPOSWeb(UpdateSetupPrintByPOSWebModel req);
        ResultResponseModel DeleteSetupPrintByPOSWeb(DeleteSetupPrintByPOSWebModel req);
        Task<ResultResponseModel> CreateImageSlider_V2(CreateSetupImageSliderModel req);
        Task<ResultResponseModel> UpdateImageSlider_V2(UpdateSetupImageSliderModel req);
        string GetDescriptionImageSlider(string description);
        ResultResponseModel SaveImageSliderByBlock(ImageSliderBlockModel model);

    }

    public class MasterDataBLO : IMasterDataBLO
    {
        private MasterIData _data { get; set; }
        public MasterDataBLO()
        {
            _data = new MasterIData();
        }
        public List<EmployeeListResponseModel> GetEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetEmployeeList(staffCode, staffName, storeNo, typeGroup, status, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportEmployeeListModel> ExportEmployeeList(string staffCode, string staffName, string storeNo, string typeGroup, string status)
        {
            return _data.ExportEmployeeList(staffCode, staffName, storeNo, typeGroup, status);
        }
        public ResultResponseModel CreateEmployeeList(CreateEmployeeListModel req)
        {
            return _data.CreateEmployeeList(req);
        }
        public ResultResponseModel UpdateEmployeeList(UpdateEmployeeListModel req)
        {
            return _data.UpdateEmployeeList(req);
        }
        public ResultResponseModel DeleteEmployeeList(DeleteEmployeeListModel req)
        {
            return _data.DeleteEmployeeList(req);
        }
        public List<SetupPOSListResponseModel> GetSetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetSetupPOSList(ipAddress, posID, storeNo, chanelNo, posType, status, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportSetupPOSListModel> ExportExcel_SetupPOSList(string ipAddress, string posID, string storeNo, string chanelNo, string posType, string status)
        {
            return _data.ExportExcel_SetupPOSList(ipAddress, posID, storeNo, chanelNo, posType, status);
        }
        public ResultResponseModel CreatePOSTerminalList(CreatePOSTerminalListModel req)
        {
            return _data.CreatePOSTerminalList(req);
        }
        public ResultResponseModel UpdatePOSTerminalList(UpdatePOSTerminalListModel req)
        {
            return _data.UpdatePOSTerminalList(req);
        }
        public ResultResponseModel DeletePOSterminalList(DeletePOSTerminalListModel req)
        {
            return _data.DeletePOSterminalList(req);
        }

        public string GetMaxPosIDPOSTerminal(string storeNo)
        {
            return _data.GetMaxPosIDPOSTerminal(storeNo);
        }

        public Tuple<int, string> ImportExcelPOSTerminal(string userName, DataTable dt)
        {
            return _data.ImportExcelPOSTerminal(userName, dt);
        }

        public List<StoreListResponseModel> GetStoreList(string provinceNo, string chanelNo, string storeNo, string taxCode, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetStoreList(provinceNo, chanelNo, storeNo, taxCode, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelStoreListModel> ExportExcel_StoreList(string provinceNo, string chanelNo, string storeNo, string taxCode)
        {
            return _data.ExportExcel_StoreList(provinceNo, chanelNo, storeNo, taxCode);
        }

        public ResultResponseModel DeleteStoreList(DeleteStoreListModel req)
        {
            return _data.DeleteStoreList(req);
        }

        public ResultResponseModel UpdateStoreList(UpdateStoreListModel req)
        {
            return _data.UpdateStoreList(req);
        }
        public ResultResponseModel CreateStoreList(CreateStoreListModel req)
        {
            return _data.CreateStoreList(req);
        }
        public Tuple<bool, CheckStoreListModel> CheckStoreNo(string storeNo)
        {
            return _data.CheckStoreNo(storeNo);
        }
        public List<BankListResponseModel> GetBankList(string bankCode, string bankName, out int totalRecord, int skip = 1, int take = 100)
        {
            return _data.GetBankList(bankCode, bankName, out totalRecord, skip, take);
        }
        public ResultResponseModel DeleteBankList(DeleteBankModel req)
        {
            return _data.DeleteBankList(req);
        }
        public ResultResponseModel CreateBankList(CreateBankModel req)
        {
            return _data.CreateBankList(req);
        }
        public ResultResponseModel UpdateBankList(UpdateBankModel req)
        {
            return _data.UpdateBankList(req);
        }

        public List<ExportBankListModel> ExportGetBankList(string BankCode, string BankName)
        {
            return _data.ExportGetBankList(BankCode, BankName);

        }
        public List<BankPOSListResponseModel> GetBankPOSList(string storeNo, string textSearch, string bankCode, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
            return _data.GetBankPOSList(storeNo, textSearch, bankCode, status, out totalRecord, pageIndex, pageSize);
        }

        public List<ExportBankPOSListModel> ExportBankPOSList(string storeNo, string bankCode, string status, string textSearch)
        {
            return _data.ExportBankPOSList(storeNo, bankCode, status, textSearch);
        }
        public ResultResponseModel CreateBankPOSList(CreateBankPOSListModel req)
        {
            return _data.CreateBankPOSList(req);
        }
        public ResultResponseModel UpdateBankPOSList(UpdateBankPOSListModel req)
        {
            return _data.UpdateBankPOSList(req);
        }
        public ResultResponseModel DeleteBankPOSList(DeleteBankPOSListModel req)
        {
            return _data.DeleteBankPOSList(req);
        }
        public string GetBankPOSCode(string storeNo, string bankCode, string posNo)
        {
            return _data.GetBankPOSCode(storeNo, bankCode, posNo);
        }
        public string GetBankPOSName(string bankCode)
        {
            return _data.GetBankPOSName(bankCode);
        }
        public Tuple<int, string> ImportExcelBankPOS(string userName, DataTable dt)
        {
            return _data.ImportExcelBankPOS(userName, dt);
        }
        public List<POSVersionResponseModel> GetPOSVersionList(out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.GetPOSVersionList(out totalRecord, skip, take);
        }
        public ResultResponseModel UpdatePOSVersionList(UpdatePOSVersionModel req)
        {
            return _data.UpdatePOSVersionList(req);
        }
        public List<ServerInfoPOSDataSetupModel> GetServerInfoPOSDataSetupList()
        {
            return _data.GetServerInfoPOSDataSetupList();
        }
        public List<POSDocumentNosResponseModel> GetVoucherNumberToPOSList(string voucherType, string storeNo, string posID, string ipAddress, out int totalRecord, int skip, int take)
        {
            return _data.GetVoucherNumberToPOSList(voucherType, storeNo, posID, ipAddress, out totalRecord, skip, take);
        }
        public List<DocumentNoResponseModel> GetDocumentNoList(string storeNo, string posID, string voucherType, out int totalRecord, int skip, int take)
        {
            return _data.GetDocumentNoList(storeNo, posID, voucherType, out totalRecord, skip, take);
        }
        public ResultResponseModel UpdatePOSDocumentNo(UpdatePOSDocumentNoModel req)
        {
            return _data.UpdatePOSDocumentNo(req);
        }
        public ResultResponseModel InsertPOSDocumentNo(InsertPOSDocumentNoModel req)
        {
            return _data.InsertPOSDocumentNo(req);
        }
        public List<ProvinceListResponseModel> GetProvinceList(string provinceCode, string provinceName, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetProvinceList(provinceCode, provinceName, status, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel UpdateProvinceList(UpdateProvinceResponseModel req)
        {
            return _data.UpdateProvinceList(req);
        }
        public ResultResponseModel CreateProvinceList(CreateProvinceResponseModel req)
        {
            return _data.CreateProvinceList(req);
        }
        public Tuple<bool, CheckProvinceResponseModel> CheckProvinceCode(string provinceCode)
        {
            return _data.CheckProvinceCode(provinceCode);
        }
        public ResultResponseModel DeleteProvinceList(DeleteProvinceResponseModel req)
        {
            return _data.DeleteProvinceList(req);
        }
        public List<SalesOrderTypeListModel> GetSalesOrderTypeList(string salesType, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetSalesOrderTypeList(salesType, status, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel CreateSalesOrderType(CreateSalesOrderTypeModel req)
        {
            return _data.CreateSalesOrderType(req);
        }
        public TenderTypeList LoadTenderType(float percent)
        {
            return _data.LoadTenderType(percent);
        }
        public ResultResponseModel DeleteSalesOrderType(DeleteSalesOrderTypeModel req)
        {
            return _data.DeleteSalesOrderType(req);
        }
        public ResultResponseModel UpdateSalesOrderType(UpdateSalesOrderTypeModel req)
        {
            return _data.UpdateSalesOrderType(req);
        }
        public List<BankCardTypeResponseModel> LoadBankCardType(string bankCardCode, string bankCardName, string status, out int totalRecord, int skip = 0, int take = 100)
        {
            return _data.LoadBankCardType(bankCardCode, bankCardName, status, out totalRecord, skip, take);
        }
        public ResultResponseModel CreateBankCardType(CreateBankCardTypeModel req)
        {
            return _data.CreateBankCardType(req);
        }
        public ResultResponseModel UpdateBankCardType(UpdateBankCardTypeModel req)
        {
            return _data.UpdateBankCardType(req);
        }
        public ResultResponseModel DeleteBankCardType(DeleteBankCardTypeModel req)
        {
            return _data.DeleteBankCardType(req);
        }
        public List<EWalletListResponseModel> LoadSetupEWalletList(string storeNo, string eWalletType, string textSearch, string status, out int totalRecord, int pageIndex = 1, int pageSize = 100)
        {
           return _data.LoadSetupEWalletList(storeNo, eWalletType, textSearch, status, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcelEWalletListModel> ExportExcelSetupEWalletList(string storeNo, string eWalletType, string textSearch, string status)
        {
            return _data.ExportExcelSetupEWalletList(storeNo, eWalletType, textSearch, status);
        }
        public string GetEWalletTerminalCode(string storeNo, string eWalletCode, string posNo)
        {
            return _data.GetEWalletTerminalCode(storeNo, eWalletCode, posNo);
        }
        public ResultResponseModel CreateSetupEWallet(CreateEWalletListModel req)
        {
            return _data.CreateSetupEWallet(req);
        }
        public ResultResponseModel UpdateSetupEWallet(UpdateEWalletListModel req)
        {
            return _data.UpdateSetupEWallet(req);
        }
        public ResultResponseModel DeleteSetupEWalletList(DeleteEWalletListModel req)
        {
            return _data.DeleteSetupEWalletList(req);
        }
        public Tuple<int, string> ImportExcelEWallet(string userName, DataTable dt)
        {
            return _data.ImportExcelEWallet( userName, dt);
        }
        public List<ChangePassWordModel> ChangePassWordList(string storeNo, string userName, string textSearch, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.ChangePassWordList(storeNo, userName, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel ChangePassWord(ChangePassWordModel req)
        {
            return _data.ChangePassWord(req);
        }
        public List<SetupImageSliderResponseModel> GetSetupImageSliderList(string styleProfile, string storeNo,string description, string fileName, string status, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetSetupImageSliderList(styleProfile, storeNo, description, fileName, status, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportExcel_ImagesSliderListResponseModel> ExportExcel_ImagesSliderList(string storeNo, string styleProfile, string description, string fileName, string status)
        {
            return _data.ExportExcel_ImagesSliderList(storeNo, styleProfile, description, fileName, status);
        }

        public async Task<ResultResponseModel> CreateImageSliderAsync(CreateSetupImageSliderModel req)
        {
            return await _data.CreateImageSliderV2Async(req);
        }

        // ----- 25/10/2023 -----
        public Task<ResultResponseModel> CreateImageSlider_V2(CreateSetupImageSliderModel req)
        {
            return _data.CreateImageSlider_V2(req);
        }

        public Task<ResultResponseModel> UpdateImageSlider_V2(UpdateSetupImageSliderModel req)
        {
            return _data.UpdateImageSlider_V2(req);
        }
        public async Task<ResultResponseModel> UpdateImageSliderAsync(UpdateSetupImageSliderModel req)
        {
            return await _data.UpdateImageSliderV2Async(req);
        }
        public ResultResponseModel DeleteImageSlider(DeleteSetupImageSliderModel req)
        {
            return _data.DeleteImageSlider(req);
        }
        public UpdateSetupImageSliderModel ViewInfoImagesSlider(string Id, string pkey)
        {
            return _data.ViewInfoImagesSlider(Id,pkey);
        }
        public string GetDescriptionImageSlider(string description)
        {
            return _data.GetDescriptionImageSlider(description);
        }
        public ResultResponseModel SaveImageSliderByBlock(ImageSliderBlockModel model)
        {
            return _data.SaveImageSliderByBlock(model);
        }
        public List<SetupCurrencyRateListModel> GetSetupCurrencyRateList(string UnitCurrency, string Status, string ExchangeRate, out int totalRecord, int pageIndex = 0, int pageSize = 100)
        {
            return _data.GetSetupCurrencyRateList(UnitCurrency, Status, ExchangeRate, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel CreateCurrencyRate(CreateCurrencyRateModel req)
        {
            return _data.CreateCurrencyRate(req);
        }
        public ResultResponseModel UpdateCurrencyRate(UpdateCurrencyRateModel req)
        {
            return _data.UpdateCurrencyRate(req);
        }
        public List<QRInformationModel> GetInformationQRList(DateTime fromDate, DateTime toDate, string storeNo, string posID, string textSearch, string status, string contentType, out int totalRecord, int pageIndex = 0, int pageSize = 50)
        {
            return _data.GetInformationQRList(fromDate, toDate, storeNo, posID, textSearch, status, contentType, out totalRecord, pageIndex, pageSize);
        }
        public ResultResponseModel CreateInformationQRList(CreateInformationQRModel req)
        {
            return _data.CreateInformationQRList(req);
        }
        public ResultResponseModel UpdateQRInformationList(UpdateInformationQRModel req)
        {
            return _data.UpdateQRInformationList(req);
        }
        public ResultResponseModel DeleteQRInformationList(DeleteInformationQRModel req)
        {
            return _data.DeleteQRInformationList(req);
        }
        public List<AdminUserLoginPOSResponseModel> GetUserLoginPOSWebList(string storeNo, string posTerminal, string typePOS, string textSearch, out int totalRecord, int pageIndex, int pageSize)
        {
            return _data.GetUserLoginPOSWebList(storeNo, posTerminal, typePOS, textSearch, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportAdminUserLoginPOSResponseModel> ExportExcel_UserLoginPOSWebList(string storeNo, string posTerminal, string typePOS, string textSearch)
        {
            return _data.ExportExcel_UserLoginPOSWebList(storeNo, posTerminal, typePOS, textSearch);
        }
        public ResultResponseModel DeleteUserLoginPOSWeb(DeleteAdminUserLoginPOSWebModel req)
        {
            return _data.DeleteUserLoginPOSWeb(req);
        }
        public List<SetupPrintByPOSWebResponseModel> SetupPrintByPOSWebList(string ipAddress, string storeNo, string status, out int totalRecord, int pageIndex, int pageSize)
        {
            return _data.SetupPrintByPOSWebList(ipAddress, storeNo, status, out totalRecord, pageIndex, pageSize);
        }
        public List<ExportEExcelSetupPrintByPOSWebModel> ExportExcel_SetupPrintByPOSWeb(string ipAddress, string storeNo, string status)
        {
            return _data.ExportExcel_SetupPrintByPOSWeb(ipAddress, storeNo, status);
        }

        public ResultResponseModel CreateSetupPrintByPOSWeb(CreateSetupPrintByPOSWebModel req)
        {
            return _data.CreateSetupPrintByPOSWeb(req);
        }

        public ResultResponseModel UpdateSetupPrintByPOSWeb(UpdateSetupPrintByPOSWebModel req)
        {
            return _data.UpdateSetupPrintByPOSWeb(req);
        }

        public ResultResponseModel DeleteSetupPrintByPOSWeb(DeleteSetupPrintByPOSWebModel req)
        {
            return _data.DeleteSetupPrintByPOSWeb(req);
        }




    }
}
