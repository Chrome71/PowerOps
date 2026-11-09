using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using PowerOps.Core.Deploy;

namespace PowerOps.Core.Output;

/// <summary>The text and JSON reports of a <see cref="DeployApplyResult"/>. The result holds no environment id or URL, folder path, connection id, setting value or <c>pac</c> text, so none can appear here.</summary>
public static class DeployApplyReport
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>Target and hash, one line per solution, per flow and per agent, then a summary; "\n" line endings.</summary>
    public static void WriteText(DeployApplyResult result, TextWriter writer)
    {
        static string C(string? s) => TextReport.Clean(s);
        var text = new StringBuilder($"Target: {C(result.Target)}\nPlan hash: {C(result.Hash)}\nExecutor: {C(result.Executor)}\n");
        if (result.Approval is { } approval)
        {
            text.Append($"Approval: {C(approval.Mode)}{(approval.Receipt is null ? "" : $", receipt {C(approval.Receipt)}")}\n");
        }

        if (result.AppliedAt is not null)
        {
            text.Append($"Applied at: {C(result.AppliedAt)}\n");
        }

        foreach (var s in result.Solutions)
        {
            text.Append($"{s.Order} {C(s.Solution)} ({C(s.Zip)}): {C(s.Action)}, {C(s.Import)}\n");
        }

        foreach (var f in result.Flows)
        {
            text.Append($"flow {C(f.Solution)}/{C(f.Name)}: expected {C(f.ExpectedState)}, now {(f.StateAfter is null ? "not read" : C(f.StateAfter))}\n");
        }

        foreach (var a in result.Agents)
        {
            var what = a.State switch
            {
                null => "in the import; the import may leave it published",
                AgentState.Found => a.PublishedOn == true ? "found, publishedon set" : "found, publishedon empty",
                AgentState.Missing => "missing",
                _ => "not read",
            };
            text.Append($"agent {C(a.Solution)}/{C(a.Name)}: {C(what)}\n");
        }

        writer.Write(text.Append(Summary(result)).Append('\n').ToString());
    }

    /// <summary>Writes the JSON report (stable property order, nulls written, "\n" line endings, trailing newline).</summary>
    public static void WriteJson(DeployApplyResult result, TextWriter writer)
    {
        var doc = new Doc(JsonOutput.SchemaVersion, result.Tool, result.Applied, result.Target, result.Hash, result.Status, result.Message, result.AppliedAt, result.Executor, result.Approval, result.Solutions, result.Flows, result.Agents);
        writer.Write(JsonSerializer.Serialize(doc, Options) + "\n");
    }

    private static string Summary(DeployApplyResult r)
    {
        var message = r.Message is null ? "" : ": " + TextReport.Clean(r.Message);
        switch (r.Status)
        {
            case ApplyStatus.Planned:
                var n = r.Solutions.Count(s => s.Import == ImportStatus.Planned);
                return $"dry run: would import {n.ToString(CultureInfo.InvariantCulture)} solution{(n == 1 ? "" : "s")}";
            case ApplyStatus.Imported:
                var off = r.Flows.Count(f => f.StateAfter is not null && f.StateAfter != f.ExpectedState);
                return message.Length > 0 || off == 0
                    ? "applied" + message
                    : $"applied, but {off.ToString(CultureInfo.InvariantCulture)} flow{(off == 1 ? " is" : "s are")} not in {(off == 1 ? "its" : "their")} expected state";
            default:
                return r.Status + message;
        }
    }

    private sealed record Doc(
        int SchemaVersion, string Tool, bool Applied, string Target, string Hash, string Status, string? Message, string? AppliedAt, string Executor, Approval? Approval,
        IReadOnlyList<AppliedSolution> Solutions, IReadOnlyList<AppliedFlow> Flows, IReadOnlyList<AppliedAgent> Agents);
}