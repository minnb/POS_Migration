using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupExtraFee
{
    public class ExtraFeeHeaderModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string StoreGroup { get; set; }
        public string StoreGroupName { get; set; }
        public string ExtraFeeValue { get; set; }
        public List<SalesOrderTypeModel> SalesType { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string PKey { get; set; }
        public Nullable<long> Counter { get; set; }

        public string ListSalesType
        {
            get
            {
                var returnSale = "";
                if(SalesType != null)
                {
                    if (SalesType.Count == 1)
                    {
                        returnSale = SalesType.FirstOrDefault().Description;
                    }
                    else
                    {
                        returnSale = $"[{string.Join(",", SalesType.OrderBy(d => d.Code).Select(a => a.Code))}]";
                    }
                }
                return returnSale;
            }
        }
    }
    public class SalesOrderTypeModel
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }
}
