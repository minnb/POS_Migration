using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Bank
{
    public class BankCardTypeResponseModel
    {
       public string BankCardCode { get; set; }
       public string BankCardName { get; set; }
       public string Type { get; set; }
       public long? Counter { get; set; }
       public string Pkey { get; set; }
       public string CreatedDateStr { get; set; }
       public string UpdatedDateStr { get; set; }
       public string Status { get; set; }
    }

    public class BankCardTypeModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? Status { get; set; }

    }

    public class CreateBankCardTypeModel
    {
        public string BankCardCode { get; set; }
        public string BankCardName { get; set; }
        public string Type { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? Status { get; set; }

    }

    public class UpdateBankCardTypeModel
    {
        public string BankCardCode { get; set; }
        public string BankCardName { get; set; }
        public string Type { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? Status { get; set; }

    }

    public class DeleteBankCardTypeModel
    {
        public string BankCardCode { get; set; }
        public string BankCardName { get; set; }
    }








}
