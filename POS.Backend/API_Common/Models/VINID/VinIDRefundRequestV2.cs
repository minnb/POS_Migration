using System;
using System.Collections.Generic;

namespace VCM.POSBLUE.Model.VINID
{
    public class POSRefundRequestV2
    {
        public string BarcodeOrder { get; set; }//Don hang barcode 13 ky tu _888
        public string OrigOrderNo { get; set; }// Đơn hàng gốc
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string CardNumber { get; set; }
        public string ReturnNo { get; set; }//Số đơn hàng trả
        public Int64? RedeemingPoint { get; set; }//Điểm tiêu còn lại=> Sơn Truyền
        public List<POSRefundItem> Items { get; set; }
    }
    public class POSRefundItem
    {
        public Int64? OrderItemId { get; set; }// Id Của order ITem lúc scan
        public string BarcodeItem { get; set; }//Barcode item
        public Int64? ReturnSaleQuantity { get; set; }// Số hàng được trả lại
        public double? ReturnUnitQuantity { get; set; }//1 với hàng thường, là số cân trên mỗi gói với hàng freshfood
        public string Note { get; set; }
    }

    public class VinIDRefundRequestV2
    {
        public string return_no { get; set; }//Sơn Truyền
        public string return_date { get; set; }
        public string return_reason { get; set; }
        public Int64? redeeming_point { get; set; }//Điểm tiêu còn lại=> Sơn Truyền
        public string note { get; set; }
        public List<ItemRefundOE> items { get; set; }
    }
    public class ItemRefundOE
    {
        public Int64? order_item_id { get; set; }
        public string order_item_code { get; set; }
        public Int64? return_sale_quantity { get; set; }
        public double? return_unit_quantity { get; set; }
        public string note { get; set; }
    }
}
