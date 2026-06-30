using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
    public class ServerWebModel
    {
        public string IPSERVER { get; set; }
        public string LINKF5 { get; set; }
        public string IPVIP { get; set; }
        public string LINKMICRO { get; set; }
        public string TYPESERVICE { get; set; }
        public string GROUPTYPE { get; set; }
        public string NOTE { get; set; }
        public Nullable<bool> ENABLE { get; set; }
        public Nullable<int> STT { get; set; }
        public string FOLDERJOB { get; set; }

    }
}
