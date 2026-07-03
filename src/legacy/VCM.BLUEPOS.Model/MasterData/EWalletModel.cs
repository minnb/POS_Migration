using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MasterData
{
    public class EWalletListResponseModel
    {
        public int ID { get; set; }
        public string TerminalCode { get; set; }
        public string EWalletCode { get; set; }
        public string EWalletName { get; set; }
        public string StoreNoFull { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string POSTerminal { get; set; }
        public string POSNo { get; set; }
        public string AccessKey { get; set; }
        public string PartnerID { get; set; }
        public string PaymentType { get; set; }
        public string TenderType { get; set; }
        public string Status { get; set; }
        public string Mode { get; set; }
        public string IsTpay { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedUser { get; set; }
        public int Total { get; set; }
    }

    public class ExportExcelEWalletListModel
    {
        public string TerminalCode { get; set; }
        public string POSNo { get; set; }
        public string POSTerminal { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Status { get; set; }
        public string EWalletCode { get; set; }
        public string EWalletName { get; set; }
        public string PaymentType { get; set; }
        public string TenderType { get; set; }
        public string Mode { get; set; }
        public string IsTpay { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedUser { get; set; }

    }

    public class CreateEWalletListModel
    {
        public string PartnerID { get; set; }
        public string TerminalCode { get; set; }
        public string EWalletCode { get; set; }
        public string EWalletName { get; set; }
        public string StoreNoFull { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string POSTerminal { get; set; }
        public string POSNo { get; set; }
        public string AccessKey { get; set; }
        public string PaymentType { get; set; }
        public string TenderType { get; set; }
        public bool? IsTPay { get; set; }
        public bool? Status { get; set; }
        public string Mode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }

    }

    public class UpdateEWalletListModel
    {
        public string TerminalCode { get; set; }
        public string EWalletCode { get; set; }
        public string EWalletName { get; set; }
        public string StoreNoFull { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string POSTerminal { get; set; }
        public string POSNo { get; set; }
        public string AccessKey { get; set; }
        public string PartnerID { get; set; }
        public string PaymentType { get; set; }
        public string TenderType { get; set; }
        public bool? IsTPay { get; set; }
        public bool? Status { get; set; }
        public string Mode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }

    }

    public class DeleteEWalletListModel
    {
        public string TerminalCode { get; set; }
    }









}
