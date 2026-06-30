using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
    public class HomeViewModel
    {
        public string IPServerHost { get; set; }
        public string Role { get; set; }
        public List<ServerWebModel> ListServerWeb { get; set; }
    }  
}
