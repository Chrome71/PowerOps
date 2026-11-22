using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerOps.Core.Output;

/// <summary>One line of <c>powerops doctor</c>. <paramref name="Status"/> is one of <c>ok</c>, <c>missing</c>, <c>not-installed</c>, <c>failed</c>, <c>not-read</c>; the text prints <paramref name="Label"/> and the status with spaces.</summary>
public sealed record DoctorCheck(string Id, [property: JsonIgnore] string Label, string Status, string? Detail = null);

public static class DoctorReport
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public const string LiveHint = "Not checked: the sign-ins, licences and environment type. `powerops doctor --live` reads them through pac, az and Microsoft Graph.";

    /// <summary>Exit 1 when a check is missing or failed; not-installed and not-read never count.</summary>
    public static int ExitCode(IReadOnlyList<DoctorCheck> checks) => checks.Any(c => c.Status is "missing" or "failed") ? 1 : 0;

    public static void WriteJson(IReadOnlyList<DoctorCheck> checks, bool live, TextWriter writer) =>
        writer.Write(JsonSerializer.Serialize(new Doc(JsonOutput.SchemaVersion, live, checks), Options) + "\n");

    public static void WriteText(IReadOnlyList<DoctorCheck> checks, bool live, TextWriter writer)
    {
        foreach (var c in checks)
        {
            writer.Write($"{c.Label}: {c.Status.Replace('-', ' ')}{(c.Detail is null ? "" : " - " + c.Detail)}\n");
        }

        if (!live)
        {
            writer.Write(LiveHint + "\n");
        }
    }

    private sealed record Doc(int SchemaVersion, bool Live, IReadOnlyList<DoctorCheck> Checks);
}