using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{
    public class ComboxStoreZaloFoodModel
    {
        public string store_id { get; set; }            // = CH ZaloFood
        public string merchant_store_id { get; set; }   // = CH PLH
        public string merchant_name { get; set; }
        public string blocked { get; set; }
    }
    public class ZaloFood_Store_Model
    {
        public string store_id { get; set; }            // = CH ZaloFood
        public string merchant_store_id { get; set; }   // = CH PL
        public string merchant_name { get; set; }       // = DC CH PL
        public string blocked { get; set; }
        public string CrtUser { get; set; }
        public string CrtDate { get; set; }
        public string ChgUser { get; set; }
        public string ChgDate { get; set; }
        public int Total { get; set; }
    }

    public class ImportExcelStoreZaloFoodModel
    {
        public string store_id { get; set; }            // = CH ZaloFood
        public string merchant_store_id { get; set; }   // = CH PLH
        public string merchant_name { get; set; }
        public string blocked { get; set; }
        public string CrtUser { get; set; }
        public string CrtDate { get; set; }
        public string ChgUser { get; set; }
        public string ChgDate { get; set; }
        public string UserName { get; set; }
    }

    public class CheckZaloFoodStoreModel
    {
        public string store_id { get; set; }      
    }

}
