using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Price
{
    public class CreatePriceModel
    {
        public int ID { get; set; }
        public string ChanelCode { get; set; }              // Vùng giá
        public string RegionCode { get; set; }              // Vùng giá
        public string ItemNo { get; set; }                  // Mã sản phẩm
        public string UOM { get; set; }                     // Đơn vị tính
        public double UnitPrice { get; set; }               // Đơn giá
        public DateTime StartDate { get; set; }             // Ngày bắt đầu có hiệu lực
        public DateTime EndDate { get; set; }               // Ngày hết hiệu lực
        public string CreatedBy { get; set; }               // User update
        public DateTime CreatedDate { get; set; }           // Ngày update
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class CreatePriceResponseModel
    {
        public int ID { get; set; }
        public string ChanelCode { get; set; }               // Vùng giá
        public string RegionCode { get; set; }               // Vùng giá
        public string ItemNo { get; set; }                  // Mã sản phẩm
        public string UOM { get; set; }                     // Đơn vị tính
        public string UnitPrice { get; set; }               // Đơn giá
        public DateTime StartDate { get; set; }             // Ngày bắt đầu có hiệu lực
        public string FromDate { get; set; }             // Ngày bắt đầu có hiệu lực
        public DateTime EndDate { get; set; }               // Ngày hết hiệu lực
        public string ToDate { get; set; }               // Ngày hết hiệu lực
        public string CreatedBy { get; set; }               // User update
        public DateTime CreatedDate { get; set; }           // Ngày update
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class PriceListResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemNo_PLG { get; set; }
        public string SalesCode { get; set; }
        public string SiteNo { get; set; }
        public string ItemName { get; set; }
        public string BarcodeNo { get; set; }
        public string UnitOfMeasureCode { get; set; }
        public string UnitPrice { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string EndingYearStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }
    }

    public class ExportPriceListModel
    {
        public string BarcodeNo { get; set; }
        public string SalesCode { get; set; }
        public string SiteNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemNo_PLG { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasureCode { get; set; }
        public string UnitPrice { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string EndingYearStr { get; set; }

    }

    public class UpdatePriceListModel
    {
        public int ID { get; set; }
        public string ChanelCode { get; set; }              // Vùng giá
        public string RegionCode { get; set; }              // Vùng giá
        public string SalesCode { get; set; }              // Vùng giá
        public string ItemNo { get; set; }                  // Mã sản phẩm
        public string UOM { get; set; }                     // Đơn vị tính
        public double UnitPrice { get; set; }               // Đơn giá
        public DateTime StartDate { get; set; }             // Ngày bắt đầu có hiệu lực
        public DateTime EndDate { get; set; }               // Ngày hết hiệu lực
        public string CreatedBy { get; set; }               // User update
        public DateTime CreatedDate { get; set; }           // Ngày update
        public long? Counter { get; set; }
        public string Pkey { get; set; }

    }

    public class UpdatePriceListResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string SalesCode { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasureCode { get; set; }
        public double UnitPrice { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }
    }





}
