using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPrice
{
    public class ExcelExamplePriceModel
    {
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
    }

    public class ImportResultSalePriceModel
    {
        public string ItemNo { get; set; }
        public string uom { get; set; }
        public string barcode { get; set; }
        public string text { get; set; }
        public string id { get; set; }
        public string ErrorMessage { get; set; }
        public string UnitPrice { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
    }
    public class LoadExcelExamplePriceModel
    {
        public string Uom { get; set; }
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public string UnitPrice { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
    }

    public class ItemResultCombo
    { 
        public string id { get; set; }
        public string ItemNo { get; set; }
        public string barcode { get; set; }
        public string uom { get; set; }
        public string Description { get; set; }
        public string text { get; set; }
        public int TotalRecords { get; set; }
        
    }
    public class PagingResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalRecord { get; set; }
    }

    public class LoadSalesPriceModel
    {
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public string UOM { get; set; }
        public string UnitPrice { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
    }
}
