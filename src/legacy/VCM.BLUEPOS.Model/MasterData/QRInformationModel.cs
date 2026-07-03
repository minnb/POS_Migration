using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MasterData
{
    public class QRInformationModel
    {
        public int ID { get; set; }
        public string POSID { get; set; } // POSTerminal
        public string StoreNo { get; set; }
        public string QRLink { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string IsActive { get; set; }
        public string ContentType { get; set; }
        //public long Counter { get; set; }
        //public string Pkey { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string CreatedDateStr { get; set; }
        public string UpdatedDateStr { get; set; }
        public int Total { get; set; }

    }

    public class CreateInformationQRModel
    {
        public long ID { get; set; }
        public string POSID { get; set; } // POSTerminal
        public string StoreNo { get; set; }
        public string QRLink { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsActive { get; set; }
        public string ContentType { get; set; }
        public long Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string CreatedDateStr { get; set; }
        public string UpdatedDateStr { get; set; }

    }

    public class UpdateInformationQRModel
    {
        public long ID { get; set; }
        public string POSID { get; set; } // POSTerminal
        public string StoreNo { get; set; }
        public string QRLink { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }

        public bool? IsActive { get; set; }
        public string ContentType { get; set; }
        public long Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string CreatedDateStr { get; set; }
        public string UpdatedDateStr { get; set; }

    }

    public class DeleteInformationQRModel
    {
        public long ID { get; set; }
        public string POSID { get; set; } // POSTerminal
        public string StoreNo { get; set; }
        public string QRLink { get; set; }
        public string Content { get; set; }
        public int Order { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsActive { get; set; }
        public string ContentType { get; set; }
        public long Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string CreatedDateStr { get; set; }
        public string UpdatedDateStr { get; set; }
    }



}
