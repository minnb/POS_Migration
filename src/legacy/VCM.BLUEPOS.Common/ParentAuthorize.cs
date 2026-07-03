using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Common
{
    public class ParentAuthorize : Attribute
    {
        public string[] Parents;
        public ParentAuthorize(params string[] parents)
        {
            this.Parents = parents;
        }
    }
    public class DisplayName : Attribute
    {
        public string Name;
        public DisplayName(string name)
        {
            this.Name = name;
        }
    }
}
