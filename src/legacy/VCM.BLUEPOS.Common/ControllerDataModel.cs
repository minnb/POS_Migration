using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Common
{
    public class ControllerDataModel
    {
        public string Controller { get; set; }
        public string ControllerAttributes { get; set; }
        public string ControllerDisplayName { get; set; }
        public string Action { get; set; }
        public string ActionDisplayName { get; set; }
        public string ReturnType { get; set; }
        public string Attributes { get; set; }
        public List<string> ListParent { get; set; }
    }
}
