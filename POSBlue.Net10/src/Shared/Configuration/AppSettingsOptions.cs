namespace VCM.POSBLUE.Shared.Configuration;

/// <summary>
/// Strongly-typed options bind từ section "AppSettings" trong appsettings.json.
/// Thay thế WebConfigurationManager.AppSettings[...] của bản cũ — inject qua IOptions&lt;AppSettingsOptions&gt;.
/// </summary>
public class AppSettingsOptions
{
    public const string SectionName = "AppSettings";

    public string Environment { get; set; } = "DEV";
    public string BasicAuthentication { get; set; } = "YES";
    public string RedisActive { get; set; } = "Sentinels";

    public string? Elasticsearch_USER { get; set; }
    public string? Elasticsearch_PASS { get; set; }
    public string? RabbitMQClusterUser { get; set; }

    public string ServiceName { get; set; } = "VCM.POSBLUE.API";
    public string? OtlpEndpoint { get; set; }
    public string WebApiSystem { get; set; } = "BLUEPOS";

    // SAP / Odoo
    public string? SAPUser { get; set; }
    public string? SAPPassword { get; set; }
    public string? OdooUser { get; set; }
    public string? OdooPassword { get; set; }
    public string? OdooEndPoint { get; set; }

    // PLG
    public string? PLG_ChecVoucher { get; set; }
    public string? PLG_CheckCoupon { get; set; }
    public string? PLG_RedeemVoucher { get; set; }
    public string? PLG_RedeemCoupon { get; set; }
    public string? PLGUser { get; set; }
    public string? PLGPassword { get; set; }
    public string? linkAPIPLH { get; set; }

    // CrownX
    public string? CXUrl { get; set; }
    public string? CXUser { get; set; }
    public string? CXPassword { get; set; }

    // VINID
    public string? LinkAPI { get; set; }
    public string? APISCANGO { get; set; }
    public string? API_KEY { get; set; }
    public string? API_SECRET { get; set; }
    public string? VINID_API_USER { get; set; }
    public string? VINID_API_PASS { get; set; }

    public string? clubCodeDefault { get; set; }
    public string? winCodeByStore { get; set; }
}
