using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class SetupPromotionBUYModel
    {
        public int ID { get; set; }
        public string Key { get; set; }
        public string FileNameRead { get; set; }
        public Nullable<System.DateTime> CREATEDDATE { get; set; }
        public string Remark { get; set; }
        public string LINEINDICATOR { get; set; }
        public string BUYTYPE { get; set; }
        public string MATGROUP { get; set; }
        public string BBYNR { get; set; }
        public string MAT_NR { get; set; }
        public string MAT_QUAN { get; set; }
        public string MEINH { get; set; }
        public string ScaleType { get; set; }
        public string DiscountType { get; set; }
        public string DiscountValue { get; set; }
    }
}
