using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.CrownX
{
    public class CardCX_AssignCardResponse
    {
        public AssignCardData data { get; set; }
    }
    public class AssignCardData
    {
        public string clubCode { get; set; }
        public string merchantId { get; set; }
        public string otp { get; set; }
        public string actionType { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string cardId { get; set; }
        public long transactionTime { get; set; }
        public string referId { get; set; }
        public double fee { get; set; }
        public AssignCardCustomerIdentifier customerIdentifier { get; set; }
    }
    public class AssignCardCustomerIdentifier
    {
        public string id { get; set; }
        public string idType { get; set; }
    }

    public class CardCX_AssignCardPOSResponse
    {  
        public string actionType { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string cardId { get; set; }
        public double fee { get; set; }
        public AssignCardCustomerIdentifier customerIdentifier { get; set; }
    }
}
