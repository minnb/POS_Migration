using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.PartnerApi
{
    partial class SetKeyRedis
    {
        public string Key { get; set; }
        public string HashField { get; set; }
        public JsonElement Data { get; set; }
    }
}
