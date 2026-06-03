namespace TCX.API.Common.Dtos.Loyalty
{
    public class TransLineLoyalty
    {
        public int LineNo { get; set; }
        public int? ParentLineNo { get; set; }
        public string ItemCode { get; set; }
        public string Barcode { get; set; }
        public string PackId { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal? ParentQuantity { get; set; }
        public decimal VatAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public int DiscountType { get; set; }
        public decimal LineAmountIncVAT { get; set; }
        public string Size { get; set; }
        public string CardLevel { get; set; }
        public string DivisionCode { get; set; }
        public bool IsAward { get; set; } = true; //true: award points line, false: not award points line
    }
}
