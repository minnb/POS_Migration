using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos;

public class RedisDto
{
    [Required]
    public string StoreNo { get; set; } = string.Empty;
    [Required]
    public string PosNo { get; set; } = string.Empty;
    [Required]
    public string Key { get; set; } = string.Empty;
}

public class CreateKeyRedisDto
{
    [Required]
    public string AppCode { get; set; } = string.Empty;
    [Required]
    public string Prefix { get; set; } = string.Empty;
    [Required]
    public string Key { get; set; } = string.Empty;
    public int ExpTime { get; set; }
    [Required]
    public string Value { get; set; } = string.Empty;
}

public class SetRedisApiPartnerDto
{
    [Required]
    public string Key { get; set; } = string.Empty;
    [Required]
    public string HashField { get; set; } = string.Empty;
    [Required]
    public object Data { get; set; } = new();
}
