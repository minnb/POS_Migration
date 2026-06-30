using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Authen;
using VCM.BLUEPOS.Model;
using VCM.BLUEPOS.Data;
using VCM.BLUEPOS.Data.Order;
using VCM.BLUEPOS.Model.Order;
using VCM.BLUEPOS.Model.Order.PrintInvoiceOrderSalesModel;

namespace VCM.BLUEPOS.Business.Order
{
    public interface IReturnOrderSalesPrintBLO
    {

    }

    public class ReturnOrderSalesPrintBLO : IReturnOrderSalesPrintBLO
    {
        private readonly IReturnOrderSalesPrintData _data;
        public ReturnOrderSalesPrintBLO()
        {
            _data = new ReturnOrderSalesPrintData();
        }











    }
}
