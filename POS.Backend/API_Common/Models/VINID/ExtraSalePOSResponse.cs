using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class ExtraSalePOSResponse
    {
        public Nullable<bool> OverQuota { get; set; }
        public Nullable<double> ExtraEarnByItems { get; set; }
        public Nullable<double> ExtraEarnByCampaign { get; set; }
        public string CompanyCode { get; set; }
        public string EmployCode { get; set; }
        public List<ExtraSaleItemPOSResponse> ListItem { get; set; }
    }

    public class ExtraSaleItemPOSResponse
    {     
        public Nullable<double> RecordNo { get; set; }
        public string Barcode { get; set; }
        public string Article { get; set; }     
        public string ArticleName { get; set; }
        public string UOM { get; set; }
        public Nullable<double> Quantity { get; set; }     
        public Nullable<double> SalePrice { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public Nullable<double> LineAmount { get; set; }
        public Nullable<double> ExtraQuantityEarn { get; set; }
        public Nullable<double> ExtraAmountEarn { get; set; }    
    }
}
