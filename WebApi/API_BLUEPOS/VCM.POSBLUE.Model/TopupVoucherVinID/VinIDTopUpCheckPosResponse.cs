using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDTopUpCheckPosResponse
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string DOB { get; set; }
        public string Gender { get; set; }
        public string Identify { get; set; }
        public string ArticleNo { get; set; }
        public string Status { get; set; }// A: Active | I: Inactive | B: Block
        public string CardStatus { get; set; }// A: Active | I: Inactive
    }
}
