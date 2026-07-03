using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Common;
using VCM.BLUEPOS.Model.OptionModel;
using VCM.BLUEPOS.Model.Store;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class ViewMngInvoiceModel
    {
        public List<ArraySiteNoModel> ListStore { get; set; }
        public List<InvoiceTypeModel> ListInvoiceType { get; set; }
        public List<InvoicePatternByStore> ListInvoicePattern { get; set; }
        public List<InvoiceSerialByStore> ListInvoiceSerial { get; set; }
        public List<SQLServerModel> SetDB { get; set; }
    }
}
