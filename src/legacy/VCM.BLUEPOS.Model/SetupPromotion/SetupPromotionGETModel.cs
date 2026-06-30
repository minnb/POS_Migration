using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class SetupPromotionGETModel
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string FileNameRead { get; set; }
        public Nullable<System.DateTime> CREATEDDATE { get; set; }
        public string Remark { get; set; }
        public string LINEINDICATOR { get; set; }
        public string BBYNR { get; set; }
        public string GETTYPE { get; set; }
        public string MATGROUP { get; set; }
        public string MATERIALCODE { get; set; }
        public string DISTYPE { get; set; }
        public string QTY { get; set; }
        public string SCALETYPE { get; set; }
        public string BBYVAL { get; set; }
        public string BBYPER { get; set; }
        public string PRICEUNIT { get; set; }
        public string MEINH { get; set; }
    }
}
