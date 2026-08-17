using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiDocAssistant.Mcp;

internal static class McpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
