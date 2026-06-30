using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InfoInvoiceModel
    {
        public string InvoicePattern { get; set; }
        public string SerialNo { get; set; }
        public string NoseriCode { get; set; }
        public string LastUsed { get; set; }
        public string NumberOrder
        {
            get
            {
                var checkNumber = int.TryParse(LastUsed, out int number);
                var numberMax = 0;
                if (checkNumber)
                    numberMax = number + 1;
                if (numberMax < 10)
                    return "000000" + numberMax;
                else if (numberMax >= 10 && numberMax < 100)
                    return "00000" + numberMax;
                else if (numberMax >= 100 && numberMax < 1000)
                    return "0000" + numberMax;
                else if (numberMax >= 1000 && numberMax < 10000)
                    return "000" + numberMax;
                else if (numberMax >= 10000 && numberMax < 100000)
                    return "00" + numberMax;
                else if (numberMax >= 100000 && numberMax < 1000000)
                    return "0" + numberMax;
                else
                    return numberMax.ToString();

            }
        }
    }
    public class InvoicePatternByStore
    {
        public string InvoicePattern { get; set; }
    }
    public class InvoiceSerialByStore
    {
        public string InvoiceSerial { get; set; }
    }
}
