using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class SetupItemModel
    {
        public string ItemNo { get; set; }       
        public string ItemNo2 { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
    }
    public class CheckItemGrabFoodModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public string CategoryID { get; set; }
        public string CategoryName { get; set; }
    }

    public class ToppingGrabFoodModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string UOM { get; set; }
        public string ModifierGroupID { get; set; }
        public string AvailableStatus { get; set; }
        public bool? isTopping { get; set; }
    }

    public class ListItemModel
    {
        public string No { get; set; }
        public string Name { get; set; }
        public string Sa { get; set; }
        
    }

}
