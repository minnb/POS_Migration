using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Enums;

namespace VCM.BLUEPOS.Model
{
    public class ResultResponseModel
    {
        public ResultEnum Status { get; set; }
        public string Message { get; set; }
        public string Item1 { get; set; }
        public string Item2 { get; set; }
        public string Item3 { get; set; }
        public string Item4 { get; set; }
        public string Item5 { get; set; }
        public string Item6 { get; set; }
        public string Item7 { get; set; }
        public string Item8 { get; set; }
        public string Item9 { get; set; }
        public string Item10 { get; set; }
        public string Item11 { get; set; }
        public string Item12 { get; set; }
        public int Flag { get; set; }
        public int IsStatus { get; set; }
        public bool? isFlag { get; set; }
        public string ListStoreNo { get; set; }
        public int Total { get; set; }
    }



}
