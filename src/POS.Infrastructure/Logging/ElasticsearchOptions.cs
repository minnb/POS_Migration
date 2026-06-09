namespace POS.Infrastructure.Logging;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string[] Nodes { get; set; } = [];
    public string IndexFormat { get; set; } = "posblue-logs-{0:yyyy.MM.dd}";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
