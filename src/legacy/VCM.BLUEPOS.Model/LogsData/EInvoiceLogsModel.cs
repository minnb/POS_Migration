using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.LogsData
{
    public class APIErrorMappingResponseModel
    {
        public int? ID { get; set; }
        public string ERRCode { get; set; }
        public string ERRFull { get; set; }
        public string ERRMSG { get; set; }
        public bool? IsRetry { get; set; }
        public int? RetryNumber { get; set; }
        public bool? IsIssueVatNumber { get; set; }
        public string Remark { get; set; }
        public string CreatedDateStr { get; set; }
    }

    public class ExportAPIErrorMappingModel
    {
        public int? ID { get; set; }
        public string ERRCode { get; set; }
        public string ERRFull { get; set; }
        public string ERRMSG { get; set; }
        public bool? IsRetry { get; set; }
        public int? RetryNumber { get; set; }
        public bool? IsIssueVatNumber { get; set; }
        public string Remark { get; set; }
        public string CreatedDateStr { get; set; }
    }

    public class InvoiceErrorsResponseModel
    {
        public int? ErrorID { get; set; }
        public string UserName { get; set; }
        public int? ErrorNumber { get; set; }
        public int? ErrorState { get; set; }
        public int? ErrorSeverity { get; set; }
        public int? ErrorLine { get; set; }
        public string ErrorProcedure { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDateTimeStr { get; set; }
        public int Total { get; set; }


    }

    public class ExportInvoiceErrorsResponseModel
    {
        public int? ErrorID { get; set; }
        public string UserName { get; set; }
        public int? ErrorNumber { get; set; }
        public int? ErrorState { get; set; }
        public int? ErrorSeverity { get; set; }
        public int? ErrorLine { get; set; }
        public string ErrorProcedure { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDateTimeStr { get; set; }
    }

    public class PublishLogResponseModel
    {
        public int? ID { get; set; }
        public string DocumentNo { get; set; }
        public string Key { get; set; }
        public string StoreNo { get; set; }
        public string BranchID { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoicePattern { get; set; }
        public string SerialNo { get; set; }
        public string InvoiceType { get; set; }
        public bool? IsEOD { get; set; }
        public string Xml { get; set; }
        public string ErrCode { get; set; }
        public string ErrMsg { get; set; }
        public string CreateDateStr { get; set; }
        public string LastUpdateStr { get; set; }
        public string FileName { get; set; }
        public int? Step { get; set; }
    }

    public class ExportPublishLogResponseModel
    {
        public int? ID { get; set; }
        public string DocumentNo { get; set; }
        public string Key { get; set; }
        public string StoreNo { get; set; }
        public string BranchID { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoicePattern { get; set; }
        public string SerialNo { get; set; }
        public string InvoiceType { get; set; }
        public bool? IsEOD { get; set; }
        public string Xml { get; set; }
        public string ErrCode { get; set; }
        public string ErrMsg { get; set; }
        public string CreateDateStr { get; set; }
        public string LastUpdateStr { get; set; }
        public string FileName { get; set; }
        public int? Step { get; set; }
    }


}
