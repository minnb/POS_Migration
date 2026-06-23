namespace POS.Common.Dtos.RptCentralSale;

public sealed class DetailRevenueSalesDto
{
    public int     LineNo              { get; set; }
    public string  OrderNo             { get; set; } = string.Empty;
    public string  StartingTimeStr     { get; set; } = string.Empty;
    public string  OrderTimeStr        { get; set; } = string.Empty;
    public string  OrderDateStr        { get; set; } = string.Empty;
    public string  StoreNo             { get; set; } = string.Empty;
    public string  POSTerminalNo       { get; set; } = string.Empty;
    public string  CashierID           { get; set; } = string.Empty;
    public string  OrigOrder           { get; set; } = string.Empty;
    public string  OrderType           { get; set; } = string.Empty;
    public string  Barcode             { get; set; } = string.Empty;
    public string  ItemNo              { get; set; } = string.Empty;
    public string  Description         { get; set; } = string.Empty;
    public string  UnitOfMeasure       { get; set; } = string.Empty;
    public double  Quantity            { get; set; }
    public double  UnitPrice           { get; set; }
    public double  DiscountAmount      { get; set; }
    public string? VATCode             { get; set; }
    public double  VATAmount           { get; set; }
    public double  LineAmountIncVAT    { get; set; }
    public double  LineAmountBeforeVAT { get; set; }
    public string? CardVinID           { get; set; }
    public double  MemberPointsEarn    { get; set; }
    public double  MemberPointsRedeem  { get; set; }
    public string  BlockedMemberPoint  { get; set; } = string.Empty;
    public string? AmountCalPoint      { get; set; }
    public string? FormalityStr        { get; set; }
    public string? SalesType           { get; set; }
    public string? PartnerStr          { get; set; }
    public string  DivisionCode        { get; set; } = string.Empty;
    public string? UserID              { get; set; }
    public string  SerialNumber        { get; set; } = string.Empty;
    public string? PhoneWinMember      { get; set; }
    public string? PromotionID         { get; set; }
    public string? CouponCode          { get; set; }
    public int     Total               { get; set; }
}
