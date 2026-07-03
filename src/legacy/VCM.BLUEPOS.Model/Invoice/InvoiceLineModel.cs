using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InvoiceLineModel
    {
        public int ID { get; set; }
        public string DocumentNo { get; set; }
        public string Key { get; set; }
        public string Code { get; set; }
        public string ProdName { get; set; }
        public string ProdUnit { get; set; }
        public double ProdQuantity { get; set; }
        public double ProdPrice { get; set; }
        public double Total { get; set; }
        public double VATRate { get; set; }
        public double VATAmount { get; set; }
        public double Amount { get; set; }
        public string OrderNo { get; set; }
        public Nullable<System.DateTime> OrderDate { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> LastUpdate { get; set; }
        public Nullable<double> VatVinIDAmount { get; set; }
        public Nullable<double> NetVinIDAmount { get; set; }
        public Nullable<double> TotalVinIDAmount { get; set; }
        public string FormatProdPrice { get { return ProdPrice == 0 ? "" : string.Format("{0:##,#}", ProdPrice); } }
        public string FormatTotal { get { return string.Format("{0:##,#}", Total); } }
        public string FormatVATAmount { get { return string.Format("{0:##,#}", VATAmount); } }
        public string FormatAmount { get { return string.Format("{0:##,#}", Amount); } }
        public string FormatVatVinIDAmount { get { return string.Format("{0:##,#}", VatVinIDAmount); } }
        public string FormatNetVinIDAmount { get { return string.Format("{0:##,#}", NetVinIDAmount); } }
        public string FormatTotalVinIDAmount { get { return string.Format("{0:##,#}", TotalVinIDAmount); } }
        public int TotalRow { get; set; }

    }
    public class InvoiceLineExcelModel
    {
        public string OrderNo { get; set; }                         //Số ĐH
        public string ProdName { get; set; }                        //Tên SP
        public string ProdUnit { get; set; }                        //ĐVT
        public double ProdQuantity { get; set; }                    //Số lượng
        public double ProdPrice { get; set; }                       //Đơn giá
        public double Total { get; set; }                           //Thành tiền<br /> trước thuế<br /> GTGT   
        public double VATRate { get; set; }                         //Thuế suất<br /> GTGT(%)
        public double VATAmount { get; set; }                       //Tiền thuế<br /> GTGT
        public double Amount { get; set; }                          //Thành tiền<br /> sau <br />thuế VAT
        public Nullable<double> TotalVinIDAmount { get; set; }      //Tiền thuế<br /> CVL
        public Nullable<double> NetVinIDAmount { get; set; }        //Tiền <br /> trước thuế<br /> CVL
        public Nullable<double> VatVinIDAmount { get; set; }        //Tiền thuế<br /> CVL      

    }


}
