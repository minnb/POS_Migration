using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
    public class ComboboxModel
    {
        public string value { get; set; }
        public string Display { get; set; }
        public string UOM { get; set; }

        public static List<ComboboxModel> ComboLineType()
        {
            var listCombo = new List<ComboboxModel>();
            listCombo.Add(new ComboboxModel { value = "0", Display = "Theo item" });
            listCombo.Add(new ComboboxModel { value = "1", Display = "Theo group item" });
            return listCombo;
        }
        public static List<ComboboxModel> ComboScaleType()
        {
            var listCombo = new List<ComboboxModel>();
            listCombo.Add(new ComboboxModel { value = "A", Display = "Tối thiểu (From)" });
            listCombo.Add(new ComboboxModel { value = "B", Display = "Tối đa (Up to)" });
            listCombo.Add(new ComboboxModel { value = "C", Display = "Vòng quay (Equal)" });
            return listCombo;
        }

        public static List<ComboboxModel> ComboDiscountType()
        {
            var listCombo = new List<ComboboxModel>();
            listCombo.Add(new ComboboxModel { value = "0", Display = "Discount Percent (%)", UOM = "%" });//%
            listCombo.Add(new ComboboxModel { value = "1", Display = "Discount Amount (VNĐ)", UOM = "VNĐ" });//R
            listCombo.Add(new ComboboxModel { value = "2", Display = "Discount Price (VNĐ)", UOM = "VNĐ" });//P
            return listCombo;
        }
    }
}
