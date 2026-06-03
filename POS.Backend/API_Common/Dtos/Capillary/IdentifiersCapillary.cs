using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Capillary
{
    public class IdentifiersCapillary
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
    public class RegisteredBy
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
    public class FieldCapillary
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Custom_fields
    {
        public List<FieldCapillary> Field { get; set; }
    }
    public class Extended_fields
    {
        public List<FieldCapillary> Field { get; set; }
    }
    public class Iden_mode_field
    {
        public string Iden_mode { get; set; }
    }
}
