using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;

namespace TCX.WebApiCore.AppServices.FMV.Dtos
{
    public class AkaChainErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetails Error { get; set; } = new ErrorDetails();

    }
    public class ErrorDetails
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } 

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public string Details { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>(); 

        [JsonPropertyName("validationErrors")]
        public List<ValidationError> ValidationErrors { get; set; } = new List<ValidationError>();
    }

    public class ValidationError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("members")]
        public List<string> Members { get; set; } = new List<string>();
    }
}