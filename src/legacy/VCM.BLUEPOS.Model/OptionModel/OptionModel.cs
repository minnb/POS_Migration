using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.OptionModel
{
    public class OptionModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public int? Order { get; set; }
    }

    public class VoucherTypeModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public int? Order { get; set; }
    }

    public class IPAddressPOSTerminalByStoreModel
    {
        public string StoreNo { get; set; }
        public string IpAddress { get; set; }
        public string ComputerName { get; set; }
        public string PosID { get; set; }
        public string PosName { get; set; }
    }

    public class ArraySiteNoModel
    {
        public string Code { get; set; }  // 25/11/2024, tungnt8
        public string ServerIP { get; set; }
        public string ServerIPRead { get; set; }
        public string ServerStoreDesc { get; set; }
        public string SiteNo { get; set; }
        public string SiteName { get; set; }
        public string StyleProfile { get; set; }
        public bool? Permission { get; set; }
        public string RoleCode { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsAllStore { get; set; }
        public string ValueCombo { get { return SiteNo + "_" + StyleProfile; } }
        public string DisplayCombo { get { return SiteNo == "" ? SiteName : SiteNo + ":" + SiteName; } }
        public string DisplayComboSite { get { return (SiteNo == "" || SiteNo == "-1" ) ? SiteName : SiteNo + " - " + SiteName; } }
        //public string DisplayComboSite { get { return SiteNo == "" ? SiteName : SiteNo + " - " + SiteName; } }
        public string ValueJson { get { return JsonConvert.SerializeObject(new ParamJsonStoreList { SiteNo = SiteNo, SiteName = SiteName }); } }

    }

    public class ParamJsonStoreList
    {
        public string SiteNo { get; set; }
        public string SiteName { get; set; }
    }

    public class SalesTypeModel
    {
        public string Code { get; set; }
        public string TenderType { get; set; }
        public string Description { get; set; }
        public Nullable<double> Percent { get; set; }
        public string ImageName { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<int> Order { get; set; }
        public string HotKey { get; set; }
        public Nullable<int> Counter { get; set; }
        public string Pkey { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
    public class SalesType
    {
        public List<SalesTypeModel> ListSalesType { get; set; }
    }

    public class TenderTypeListModel
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Caption { get; set; }
        public bool? Status { get; set; }
    }

    public class TenderTypeList
    {
        public List<TenderTypeListModel> ListTenderType { get; set; }
    }

    public class OptionDataBySalesTypeModel
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Caption { get; set; }
        public bool? Status { get; set; }
    }

    public class OptionDataModel
    {
        public string Code { get; set; }
        public string Pkey { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int? Order { get; set; }
    }

    public class OptionSiteModel
    {
        public string StoreNoPLH { get; set; }
        public string StoreNamePLH { get; set; }
        public string StoreNoWCM { get; set; }
        public string StoreNameWCM { get; set; }
        public string Status { get; set; }
        public int? Order { get; set; }
    }

    public class SecurityStoreListByUserModel
    {
        public string SiteNo { get; set; }
        
    }





}
