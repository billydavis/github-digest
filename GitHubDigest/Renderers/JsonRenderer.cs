using GitHubDigest.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitHubDigest.Renderers;

public class JsonRenderer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Render(DigestReport report) => JsonSerializer.Serialize(report, Options);
}
