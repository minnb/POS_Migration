namespace POS.Common.Dtos.RptCentralSale;

public sealed class RevenueByStoreDto
{
    public int       ID              { get; set; }
    public string?   StoreNo         { get; set; }
    public string?   StoreName       { get; set; }
    public DateTime? BussinessDate   { get; set; }
    public int?      CountOrderTotal { get; set; }
    public decimal?  TruocVAT        { get; set; }
    public decimal?  SauVAT          { get; set; }
    public decimal?  ThueVAT         { get; set; }
    // Grand total — SP inject cùng giá trị cho mọi row
    public decimal?  SumTruocVAT     { get; set; }
    public decimal?  SumSauVAT       { get; set; }
    public decimal?  SumVAT          { get; set; }
    public double?   SumTotalOrder   { get; set; }
    public int?      Total           { get; set; }
}
