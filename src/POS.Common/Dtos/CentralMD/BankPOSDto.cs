namespace POS.Common.Dtos.CentralMD;

public class BankPOSListDto
{
    public string  BankPOSCode  { get; set; } = string.Empty;
    public string? BankPOSName  { get; set; }
    public string? BankCode     { get; set; }
    public string? StoreNo      { get; set; }
    public string? POSNo        { get; set; }
    public string? POSTerminal  { get; set; }
    public string? AccessKey    { get; set; }
    public string? PartnerId    { get; set; }
    public bool    IsOnline     { get; set; }
    public int     Status       { get; set; }
    public int?    Counter      { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string?   CreatedUser { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string?   UpdatedUser { get; set; }
}

public class BankPOSSaveDto
{
    public string  BankPOSCode  { get; set; } = string.Empty;
    public string? BankPOSName  { get; set; }
    public string  BankCode     { get; set; } = string.Empty;
    public string  StoreNo      { get; set; } = string.Empty;
    public string  POSNo        { get; set; } = string.Empty;
    public string? POSTerminal  { get; set; }
    public string? AccessKey    { get; set; }
    public string? PartnerId    { get; set; }
    public bool    IsOnline     { get; set; } = true;
    public int     Status       { get; set; } = 1;
    public bool    IsNew        { get; set; }
}

public class BankDropdownDto
{
    public string  BankCode { get; set; } = string.Empty;
    public string? BankName { get; set; }
}
