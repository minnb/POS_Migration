using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{
    public class ComboxStoreBeFoodModel
    {
        public string restaurant_id { get; set; }  // = CH BeFood
        public string partner_restaurant_id { get; set; }  // = CH PL
        public string address { get; set; }
       
    }
    public class BeFood_Store_Model
    {
        public string restaurant_id { get; set; } // = CH BeFood
        public string partner_restaurant_id { get; set; }  // = CH PL
        public string address { get; set; } // = DC CH PL
        public int is_header { get; set; }
        public string IsOpen { get; set; }
        public string CrtUser { get; set; }
        public string CrtDate { get; set; }
        public string ChgUser { get; set; }
        public string ChgDate { get; set; }
        public int Total { get; set; }
    }

    public class ImportExcelStoreBeFoodModel
    {
        public string restaurant_id { get; set; } // = CH BeFood
        public string partner_restaurant_id { get; set; }  // = CH PL
        public string restaurant_name { get; set; }
        public string foody_service { get; set; }
        public int is_header { get; set; }
        public string IsOpen { get; set; }
        public string CrtUser { get; set; }
        public string CrtDate { get; set; }
        public string ChgUser { get; set; }
        public string ChgDate { get; set; }
        public string UserName { get; set; }
    }
    public class CheckBeFoodStoreModel
    {
        public string restaurant_id { get; set; } // = CH BeFood       
    }

    public class ViewItemResponseModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AvailableStatus { get; set; }
        public int Price { get; set; }
        public string Photo { get; set; }
        public string CategoryID { get; set; }
        public bool IsTopping { get; set; }
        public bool IsCampaign { get; set; }
        public string CategoryName { get; set; }
    }
    public class BeFood_Category
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Sequence { get; set; }
        public string AvailableStatus { get; set; }
    }

    
}
