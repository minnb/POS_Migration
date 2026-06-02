using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public enum MMLSchemeItemEnum
    {
        WECO = 1,
        MML = 2,
    }
    public class MMLSchemeItem
    {
        public string HeaderCode { get; set; }
        public string Code { get; set; }
        public string ItemNo { get; set; }
        public string UOM { get; set; }
        public string CategoryCode { get; set; }
        public bool Enabled { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public string Ref5 { get; set; }
    }
}
