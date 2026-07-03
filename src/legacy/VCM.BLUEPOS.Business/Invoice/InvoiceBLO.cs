using VCM.BLUEPOS.Data.Invoice;
using VCM.BLUEPOS.Model.Invoice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Invoice
{
    public interface IInvoiceBLO
    {
        List<BranchModel> ListBranch();
        List<StoreNoseriVATModel> ListNoseriByType(string invoiceType);
        List<StoreNoseriVATModel> LoadNoseriVAT(string branchNo, string storeNo, string noseriCode, string invoiceType, string status, out int total, int skip, int take);
        bool UpdateNoseri(StoreNoseriVATModel model);
        List<VatNumberModel> LoadVATNumber(string branchNo, string noseriCode, string invoicePattern, string serialNo, string taxCode, string status, out int total, int skip, int take);
        Tuple<bool, string> UpdateVatNumber(VatNumberModel model);

        //List<InvoiceHearderModel> LoadInvoice(InvoiceHearderRequest model);

        List<ExportInvoiceReportModel> ReportInvoiceLoad(InvoiceHearderRequest model);
        InfoInvoiceModel InfoInvoice(string siteNo);
        List<CustomerVATModel> LoadCustomer(string TenKH, string SoDT, string MST, string CongTy, out int totalRecord, int skip, int take);
        List<TransHeaderModel> ListTranHeader(string siteCode, string orderNo, string listOrderSelected, int pageSize, int pageNumber);
        List<ListOrderDetailModel> DetailListOrder(string storeNo, string OrderList);
        InvoiceByViewHeaderModel ViewInvoiceHeader(string storeNo, string OrderList);
        PrintAgainModel GetInfoByFKey(string storeNo, string fKey);
        PrintAgainModel GetInfoByKey(string storeNo, string key);
        List<ListInvoiceReplaceModel> ListOrderReplace(string storeNo, int isEOD, string key, string fKey, string typeChange);
        List<PosTerminalNoModel> GetInfoPosTerminal(string storeNo, string Key);
        Tuple<bool, string> EditEmail(string storeNo, string invoiceType, string key, string email);
        Tuple<bool, string> SendEmail(string key, string email, string storeNo);
        List<InvoiceLineModel> ListInvoiceLine(string storeNo, string DocumentNo, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take);
        List<TransHeaderModel> CheckListOrder(string listOrder, string siteCode);
        List<TransHeaderModel> CheckListOrderCreatedInvoice(string listOrder, string siteCode);
        CheckPublishInvoiceModel CheckPublishOrderInvoice(string listOrder, string storeNo);
        bool ExcuteStagingInvcoie(string orderNo, string storeNo);
        bool CreatedInvoice(string storeNo, List<InvoiceCreatedModel> listInvoice);
        List<InfoInvoiceModel> ListInvoicePattern(string siteNo, string invoiceType);
        bool CheckInvoiceNo(string storeNo, string invoiceNo, string invoicePattern, string serialNo, string noseriCode, string type);

        //List<BranchModel> LoadBranch(string branchNo, string branchName, string compTaxCode, out int total, int skip, int take);
        List<BranchModel> LoadBranch(string branchNo, string branchName, string compTaxCode, string type, out int total, int skip, int take);

        Tuple<bool, string> UpdateBranch(BranchModel model);
        List<InvoiceTypeModel> ListInvoiceType();
        ViewMngInvoiceModel ViewMngInvoi();
        List<InfoInvoiceModel> ListSerialByInvoiceType(string siteNo, string invoiceType);
        List<InvoiceCreatedModel> LoadCustIssueVAT(InvoiceCusIssueVATRequest model);
        InvoiceResultModel GetInfoInvoice(string storeNo, int isEOD, string key);
        InvoiceCreatedModel GetInfoVAT(string ipServer, string storeNo, string orderNo);
        Tuple<bool, string> UpdateInfoVAT(InvoiceCreatedModel model);
        DetailInvoiceWinlifeModel ViewDetailListOrder(string storeNo, string OrderList);
        BranchModel GetBranchNo(string branchNo);

        // 25/11/2024, tungnt8
        Tuple<bool, string, List<string>> CheckListInvoiceNo(string storeNo, List<string> listInvoiceNo, string invoicePattern, string serialNo, string noseriCode, string type);
        CheckInvoiceMTTModel CheckInvoiceByMTT(string storeNo);
        List<CreatedDummyMTTModel> CreatedInvoiceDummyMTT(string ipSetServer, string listInvoice);
        List<InvoiceLineModel> GetDetailInvoiceList(string storeNo, string DocumentNo, string typePublish, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take);
        List<CustomerInfoInvoiceMTTModel> GetCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model);
        List<ExportCustomerInfoInvoiceMTTModel> ExportCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model);
        List<InvoiceHearderModel> GetInvoiceList(InvoiceHearderRequest model);
        PrintAgainModel GetInfoByKeyV2(string storeNo, string key, string type);
        List<PosTerminalNoModel> GetInfoPosTerminalV2(string storeNo, string Key, string Type);
        InvoiceResultModel GetInfoInvoiceV2(string storeNo, int isEOD, string key, string Type);
        List<InvoiceCreatedModel> LoadCustIssueVATV2(InvoiceCusIssueVATRequest model);



    }


    public class InvoiceBLO : IInvoiceBLO
    {
        private InvoiceData data { get; set; }
        public InvoiceBLO()
        {
            data = new InvoiceData();
        }
        public List<BranchModel> ListBranch()
        {
            return data.ListBranch();
        }
        public List<StoreNoseriVATModel> ListNoseriByType(string invoiceType)
        {
            return data.ListNoseriByType(invoiceType);
        }
        public List<StoreNoseriVATModel> LoadNoseriVAT(string branchNo, string storeNo, string noseriCode, string invoiceType, string status, out int total, int skip, int take)
        {
            return data.LoadNoseriVAT(branchNo, storeNo, noseriCode, invoiceType, status, out total, skip, take);
        }
        public bool UpdateNoseri(StoreNoseriVATModel model)
        {
            return data.UpdateNoseri(model);
        }
        public List<VatNumberModel> LoadVATNumber(string branchNo, string noseriCode, string invoicePattern, string serialNo, string taxCode, string status, out int total, int skip, int take)
        {
            return data.LoadVATNumber(branchNo, noseriCode, invoicePattern, serialNo, taxCode, status, out total, skip, take);
        }
        public Tuple<bool, string> UpdateVatNumber(VatNumberModel model)
        {
            return data.UpdateVatNumber(model);
        }

        //public List<InvoiceHearderModel> LoadInvoice(InvoiceHearderRequest model)
        //{
        //    return data.LoadInvoice(model);
        //}

        public List<InvoiceHearderModel> GetInvoiceList(InvoiceHearderRequest model)
        {
            return data.GetInvoiceList(model);
        }
        public List<ExportInvoiceReportModel> ReportInvoiceLoad(InvoiceHearderRequest model)
        {
            return data.ReportInvoiceLoad(model);
        }
        public InfoInvoiceModel InfoInvoice(string siteNo)
        {
            return data.InfoInvoice(siteNo);
        }
        public List<CustomerVATModel> LoadCustomer(string TenKH, string SoDT, string MST, string CongTy, out int totalRecord, int skip, int take)
        {
            return data.LoadCustomer(TenKH, SoDT, MST, CongTy, out totalRecord, skip, take);
        }
        public List<TransHeaderModel> ListTranHeader(string siteCode, string orderNo, string listOrderSelected, int pageSize, int pageNumber)
        {
            return data.ListTranHeader(siteCode, orderNo, listOrderSelected, pageSize, pageNumber);
        }
        public List<ListOrderDetailModel> DetailListOrder(string storeNo, string OrderList)
        {
            return data.DetailListOrder(storeNo, OrderList);
        }
        public InvoiceByViewHeaderModel ViewInvoiceHeader(string storeNo, string OrderList)
        {
            return data.ViewInvoiceHeader(storeNo, OrderList);
        }
        public PrintAgainModel GetInfoByFKey(string storeNo, string fKey)
        {
            return data.GetInfoByFKey(storeNo, fKey);
        }
        public PrintAgainModel GetInfoByKey(string storeNo, string key)
        {
            return data.GetInfoByKey(storeNo, key);
        }
        public PrintAgainModel GetInfoByKeyV2(string storeNo, string key, string type)
        {
            return data.GetInfoByKeyV2(storeNo, key, type);
        }
        public List<ListInvoiceReplaceModel> ListOrderReplace(string storeNo, int isEOD, string key, string fKey, string typeChange)
        {
            return data.ListOrderReplace(storeNo, isEOD, key, fKey, typeChange);
        }
        public List<PosTerminalNoModel> GetInfoPosTerminal(string storeNo, string Key)
        {
            return data.GetInfoPosTerminal(storeNo, Key);
        }

        public List<PosTerminalNoModel> GetInfoPosTerminalV2(string storeNo, string Key, string Type)
        {
            return data.GetInfoPosTerminalV2(storeNo, Key,Type);
        }
        public Tuple<bool, string> EditEmail(string storeNo, string invoiceType, string key, string email)
        {
            return data.EditEmail(storeNo, invoiceType, key, email);
        }
        public Tuple<bool, string> SendEmail(string key, string email, string storeNo)
        {
            return data.SendEmail(key, email, storeNo);
        }
        public List<InvoiceLineModel> ListInvoiceLine(string storeNo, string DocumentNo, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take)
        {
            return data.ListInvoiceLine(storeNo, DocumentNo, TypeInvoice, orderNo, out recordsTotal, skip, take);
        }
        public List<InvoiceLineModel> GetDetailInvoiceList(string storeNo, string DocumentNo,string typePublish, string TypeInvoice, string orderNo, out int recordsTotal, int skip, int take)
        {
            return data.GetDetailInvoiceList(storeNo, DocumentNo, typePublish, TypeInvoice, orderNo, out recordsTotal, skip, take);
        }
        public List<TransHeaderModel> CheckListOrder(string listOrder, string siteCode)
        {
            return data.CheckListOrder(listOrder, siteCode);
        }
        public List<TransHeaderModel> CheckListOrderCreatedInvoice(string listOrder, string siteCode)
        {
            return data.CheckListOrderCreatedInvoice(listOrder, siteCode);
        }
        public CheckPublishInvoiceModel CheckPublishOrderInvoice(string listOrder, string storeNo)
        {
            return data.CheckPublishOrderInvoice(listOrder, storeNo);
        }
        public bool ExcuteStagingInvcoie(string orderNo, string storeNo)
        {
            return data.ExcuteStagingInvcoie(orderNo, storeNo);
        }
        public bool CreatedInvoice(string storeNo, List<InvoiceCreatedModel> listInvoice)
        {
            return data.CreatedInvoice(storeNo, listInvoice);
        }
        public List<InfoInvoiceModel> ListInvoicePattern(string siteNo, string invoiceType)
        {
            return data.ListInvoicePattern(siteNo, invoiceType);
        }
        public bool CheckInvoiceNo(string storeNo, string invoiceNo, string invoicePattern, string serialNo, string noseriCode, string type)
        {
            return data.CheckInvoiceNo(storeNo, invoiceNo, invoicePattern, serialNo, noseriCode, type);
        }
        //public List<BranchModel> LoadBranch(string branchNo, string branchName, string compTaxCode, out int total, int skip, int take)
        //{
        //    return data.LoadBranch(branchNo, branchName, compTaxCode, out total, skip, take);
        //}
        public List<BranchModel> LoadBranch(string branchNo, string branchName, string compTaxCode, string type, out int total, int skip, int take)
        {
            return data.LoadBranch(branchNo, branchName, compTaxCode, type, out total, skip, take);
        }
        public Tuple<bool, string> UpdateBranch(BranchModel model)
        {
            return data.UpdateBranch(model);
        }
        public List<InvoiceTypeModel> ListInvoiceType()
        {
            return data.ListInvoiceType();
        }
        public ViewMngInvoiceModel ViewMngInvoi()
        {
            return data.ViewMngInvoi();
        }
        public List<InfoInvoiceModel> ListSerialByInvoiceType(string siteNo, string invoiceType)
        {
            return data.ListSerialByInvoiceType(siteNo, invoiceType);
        }
        public List<InvoiceCreatedModel> LoadCustIssueVAT(InvoiceCusIssueVATRequest model)
        {
            return data.LoadCustIssueVAT(model);
        }

        public List<InvoiceCreatedModel> LoadCustIssueVATV2(InvoiceCusIssueVATRequest model)
        {
            return data.LoadCustIssueVATV2(model);
        }
        public InvoiceResultModel GetInfoInvoice(string storeNo, int isEOD, string key)
        {
            return data.GetInfoInvoice(storeNo, isEOD, key);
        }
        public InvoiceResultModel GetInfoInvoiceV2(string storeNo, int isEOD, string key, string Type)
        {
            return data.GetInfoInvoiceV2(storeNo, isEOD, key, Type);
        }
        public InvoiceCreatedModel GetInfoVAT(string ipServer, string storeNo, string orderNo)
        {
            return data.GetInfoVAT(ipServer, storeNo, orderNo);
        }
        public Tuple<bool, string> UpdateInfoVAT(InvoiceCreatedModel model)
        {
            return data.UpdateInfoVAT(model);
        }
        public List<CustomerInfoInvoiceMTTModel> GetCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model)
        {
            return data.GetCustomerInfoInvoiceMTT(model);
        }

        public List<ExportCustomerInfoInvoiceMTTModel> ExportCustomerInfoInvoiceMTT(CustomerInfoInvoiceMTTRequest model)
        {
            return data.ExportCustomerInfoInvoiceMTT(model);
        }
        public DetailInvoiceWinlifeModel ViewDetailListOrder(string storeNo, string OrderList)
        {
            return data.ViewDetailListOrder(storeNo, OrderList);
        }
        public BranchModel GetBranchNo(string branchNo)
        {
            return data.GetBranchNo(branchNo);
        }
        public Tuple<bool, string, List<string>> CheckListInvoiceNo(string storeNo, List<string> listInvoiceNo, string invoicePattern, string serialNo, string noseriCode, string type)
        {
            return data.CheckListInvoiceNo(storeNo, listInvoiceNo, invoicePattern, serialNo, noseriCode, type);
        }
        public CheckInvoiceMTTModel CheckInvoiceByMTT(string storeNo)
        {
            return data.CheckInvoiceByMTT(storeNo);
        }
        public List<CreatedDummyMTTModel> CreatedInvoiceDummyMTT(string ipSetServer, string listInvoice)
        {
            return data.CreatedInvoiceDummyMTT(ipSetServer, listInvoice);
        }


    }
}
