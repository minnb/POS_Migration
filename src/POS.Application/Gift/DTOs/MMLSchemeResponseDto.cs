namespace POS.Application.Gift.DTOs;

public class MMLSchemeResponseDto
{
    public string? HeaderCode { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? Link { get; set; }
    public bool IsGenQR { get; set; }
    public bool Enabled { get; set; }
    public string? Description { get; set; }
    public string? Ref1 { get; set; }
    public string? Ref2 { get; set; }
    public string? Ref3 { get; set; }
    public string? Ref4 { get; set; }
    public string? Ref5 { get; set; }
}

public class MMLSchemeHeaderDto
{
    public string HeaderCode { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal MinAmount { get; set; }
    public bool IsMember { get; set; }
    public bool IsCallAPI { get; set; }
    public string? Ref1 { get; set; }
    public string? Ref2 { get; set; }
    public string? Ref3 { get; set; }
    public string? Ref4 { get; set; }
    public string? Ref5 { get; set; }
}

public class MMLSchemeItemDto
{
    public string? HeaderCode { get; set; }
    public string? Code { get; set; }
    public string? ItemNo { get; set; }
    public string? UOM { get; set; }
    public string? CategoryCode { get; set; }
    public bool Enabled { get; set; }
}

public class WinXQrCodeResult
{
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
}
