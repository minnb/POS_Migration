using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Bank
{
    public class BankListResponseModel
    {
        public int ID { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDateStr { get; set; }
        public string UpdatedDateStr { get; set; }
    }

    public class BankModel
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class CreateBankModel
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class UpdateBankModel
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class DeleteBankModel
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public int? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class ExportBankListModel
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string CreatedDateStr { get; set; }
        public string UpdatedDateStr { get; set; }
    }

    public class DeleteBankPOSListModel
    {
        public string BankPOSCode { get; set; }
    }

    public class CreateBankPOSListModel
    {
        public string BankPOSCode { get; set; }
        public string BankPOSName { get; set; }
        public string BankCode { get; set; }
        public string StoreNoFull { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string POSNo { get; set; }
        public string AccessKey { get; set; }
        public string PartnerID { get; set; }
        public bool? IsOnline { get; set; }
        public bool? Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedUser { get; set; }

    }

    public class UpdateBankPOSListModel
    {
        public string BankPOSCode { get; set; }
        public string BankPOSName { get; set; }
        public string BankCode { get; set; }
        public string StoreNoFull { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string POSNo { get; set; }
        public string AccessKey { get; set; }
        public string PartnerID { get; set; }
        public bool? IsOnline { get; set; }
        public bool? Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedUser { get; set; }

    }
    public class BankPOSListResponseModel
    {
        public string BankPOSCode { get; set; }
        public string BankPOSName { get; set; }
        public string BankCode { get; set; }
        public string StoreNoFull { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string POSTerminal { get; set; }
        public string POSNo { get; set; }
        public string AccessKey { get; set; }
        public string PartnerID { get; set; }
        public string IsOnline { get; set; }
        public string Status { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedUser { get; set; }
        public int Total { get; set; }
    }

    public class ExportBankPOSListModel
    {
        public string BankPOSCode { get; set; }
        public string POSTerminal { get; set; }
        public string POSNo { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string BankCode { get; set; }
        public string BankPOSName { get; set; }
        public string IsOnline { get; set; }
        public string Status { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedUser { get; set; }

    }


}
