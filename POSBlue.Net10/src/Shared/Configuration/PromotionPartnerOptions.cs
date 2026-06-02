namespace VCM.POSBLUE.Shared.Configuration;

public class PromotionPartnerOptions
{
    public const string SectionName = "PromotionPartner";
    
    public string Url { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
}
