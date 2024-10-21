using System.Text.Json.Serialization;

namespace TrinoClient.Internal
{
    public class Warning
    {
        [JsonPropertyName("warningCode")]
        public WarningCode WarningCode { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
