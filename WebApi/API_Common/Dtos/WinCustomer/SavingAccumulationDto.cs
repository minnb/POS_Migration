using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.TCB
{
    public class SavingAccumulationValue 
    {
        public string OrderNo { get; set; }
        public string Value { get; set; }
    }
    public class SavingAccumulationDto
    {
        public string TenderType { get; set; }
        public string OrderNo{ get; set; }
        public string StoreNo { get; set; }
        public string MemberCard { get; set; }
        [Column(TypeName = "decimal(18, 0)")]
        public decimal TenderAmount { get; set; }
        public string CusType { get; set; }
        [Column(TypeName = "decimal(18, 0)")]
        public decimal Rounding1 { get; set; }
        [Column(TypeName = "decimal(18, 0)")]
        public decimal Rounding2 { get; set; }
        public string OperationId { get; set; }
        public string Response { get; set; }
    }

}
