namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignDetailItemModel
    {
        public int Sequence { get; set; }          // 1: Món 1, 2: Món 2...
        public string ItemNo { get; set; }         // Mã item
        public string ItemName { get; set; }         // Ten san pham
        public int UnitPrice { get; set; }         // Đơn giá bán sản phẩm
        public int SpecialPrice { get; set; }      // Giá bán của món
        public int Discount { get; set; }          // Giảm giá
        public int SalesPrice { get; set; }        // Giá hiển thị trên app
        public bool IsTopping { get; set; }
    }
}
