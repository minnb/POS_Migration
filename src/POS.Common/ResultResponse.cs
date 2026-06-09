using System.Net;
using Newtonsoft.Json;

namespace POS.Common;

// Giữ nguyên 4 field này, đây là contract với POS client
public class ResultResponse
{
    [JsonProperty("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("Status")]
    public HttpStatusCode Status { get; set; }

    [JsonProperty("Data")]
    public object? Data { get; set; }

    [JsonProperty("MessageTechnical")]
    public string MessageTechnical { get; set; } = string.Empty;
}
