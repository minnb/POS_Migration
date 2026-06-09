using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;

namespace TCX.API.Common.Dtos
{
    public class AkaChainErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetailsAkaChain Error { get; set; } = new ErrorDetailsAkaChain();

    }
    public class ErrorDetailsAkaChain
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
        public List<ValidationErrorAkaChain> ValidationErrors { get; set; } = new List<ValidationErrorAkaChain>();
    }

    public class ValidationErrorAkaChain
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("members")]
        public List<string> Members { get; set; } = new List<string>();
    }
}