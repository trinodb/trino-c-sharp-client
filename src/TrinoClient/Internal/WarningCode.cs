using System.Text.Json.Serialization;

namespace TrinoClient.Internal
{
    public class WarningCode
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        public override string ToString()
        {
            return Name + ":" + Code;
        }
    }
}
