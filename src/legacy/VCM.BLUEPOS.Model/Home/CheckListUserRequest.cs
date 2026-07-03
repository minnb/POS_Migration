using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
    public class CheckListUserRequest
    {
        public string UserCheck { get; set; }
        public DateTime DateTimeCheck { get; set; }
        public List<CheckListRequest> CheckListRequest { get; set; }
    }
}
