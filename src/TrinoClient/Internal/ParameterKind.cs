using System.Text.Json.Serialization;

namespace TrinoClient.Internal
{
    [JsonConverter(typeof(JsonStringEnumConverter))]

    public enum ParameterKind
    {
        TYPE,
        NamedType,
        LONG,
        VARIABLE
    }
}
