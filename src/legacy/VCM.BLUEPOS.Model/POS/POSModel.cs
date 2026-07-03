using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.POS
{
    public class SetupPOSListResponseModel
    {
        public int ID { get; set; }
        public string MACAddress { get; set; }
        public string IPAddress { get; set; }
        public string POSID { get; set; }
        public string StoreNo { get; set; }
        public int? TerminalType { get; set; }
        public string Placement { get; set; }
        public string TerminalNetworkID { get; set; }
        public string DefaultSalesType { get; set; }
        public string StyleProfile { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string CustomerDisplayText1 { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }

    }

    public class ExportSetupPOSListModel
    {
        public string POSID { get; set; }
        public string IPAddress { get; set; }
        public string StoreNo { get; set; }
        public string StyleProfile { get; set; }      // Kênh
        public string DefaultSalesType { get; set; }  // Quyền 
        public string Status { get; set; }
        public string TerminalNetworkID { get; set; } 
        public string Placement { get; set; }
        public string CustomerDisplayText1 { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDateStr { get; set; }      
        public string UpdatedBy { get; set; }
        public string UpdatedDateStr { get; set; }       
    }

    public class CreatePOSTerminalListModel
    {
        public string MACAddress { get; set; }
        public string IPAddress { get; set; }
        public string POSID { get; set; }
        public string StoreNo { get; set; }
        public string TerminalType { get; set; }
        public string TerminalNetworkID { get; set; }
        public string DefaultSalesType { get; set; }
        public string StyleProfile { get; set; }
        public string Description { get; set; }
        public string CustomerDisplayText1 { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public Int64? Counter { get; set; }
        public string Pkey { get; set; }
        public string SalesChanelType { get; set; }  // Loai POS bán hàng : POSWEB / TRAD

    }


    public class UpdatePOSTerminalListModel
    {
        public string MACAddress { get; set; }
        public string IPAddress { get; set; }
        public string POSID { get; set; }
        public string StoreNo { get; set; }
        public string TerminalType { get; set; }
        public string TerminalNetworkID { get; set; }
        public string DefaultSalesType { get; set; }
        public string StyleProfile { get; set; }
        public bool? Status { get; set; }
        public string Description { get; set; }
        public string CustomerDisplayText1 { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public Int64? Counter { get; set; }
        public string Pkey { get; set; }
        public string SalesChanelType { get; set; }  // Loai POS bán hàng : POSWEB / TRAD
    }

    public class DeletePOSTerminalListModel
    {
        public string IPAddress { get; set; }
        public string POSID { get; set; }
        public string StoreNo { get; set; }
        public string TerminalType { get; set; }
        public string Placement { get; set; }
        public string DefaultSalesType { get; set; }
        public string StyleProfile { get; set; }
        public string Description { get; set; }
        public string CustomerDisplayText1 { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public Int64? Counter { get; set; }
        public string Pkey { get; set; }
    }



}
