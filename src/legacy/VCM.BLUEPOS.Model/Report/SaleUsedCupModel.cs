using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class SaleUsedCupModel
    {
        public string StoreNo { get; set; }
        public DateTime BussinessDate { get; set; }
        public string ShiftNo { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string Size { get; set; }
        public int Quantity { get; set; }
    }

    public class DetailCupRequest
    {
        public string StoreNo { get; set; }
        public DateTime BussinessDate { get; set; }
        public string ShiftNo { get; set; }
        public string ItemCup { get; set; }       
        public string Size { get; set; }
        public List<OptionModel.OptionModel> SalesOrderType { get; set; }

    }

}
