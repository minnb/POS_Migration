using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.OptionModel;

namespace VCM.BLUEPOS.Model.Report
{
    public class ViewSalesTypeModel
    {
        public List<ArraySiteNoModel> ListStore { get; set; }       
        public List<SQLServerModel> SetDB { get; set; }
        public List<OptionModel.OptionModel> SalesType { get; set; }
    }
}
