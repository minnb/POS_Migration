using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos
{
    public class SysWebApiUserDto
    {
        public string AppCode { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public string Authorization { get; set; }
        public bool Blocked { get; set; }

    }
}
