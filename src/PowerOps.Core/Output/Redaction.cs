using System.Text;
using System.Text.RegularExpressions;

namespace PowerOps.Core.Output;

/// <summary>Opt-in <c>--redact</c> of <c>analyze</c> and <c>diff</c> (M61): URLs in the texts that quote values are replaced with <see cref="Placeholder"/>. Help links come from the rule list, not from these texts, so they stay.</summary>
public static partial class Redaction
{
    public const string Placeholder = "[redacted URL]";

    // NonBacktracking: the backtracking engine took quadratic time on a long run of letters with no "://".
    [GeneratedRegex(@"\b[a-z][a-z0-9+.-]*://[^\s'""<>]*", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex Url();

    public static string Text(string text) => Url().Replace(text, Placeholder);

    /// <summary>A copy whose finding messages and components and whose not-checked notes are redacted; nothing else changes.</summary>
    public static AnalysisResult Apply(AnalysisResult result) => result with
    {
        Solutions = [.. result.Solutions.Select(s => s with
        {
            Findings = [.. s.Findings.Select(f => f with { Message = Text(f.Message), Component = f.Component is null ? null : Text(f.Component) })],
            NotChecked = [.. s.NotChecked.Select(Text)],
        })],
    };

    /// <summary>Redacts each string or chunk written through it; <c>WriteLine(string)</c> reaches <see cref="Write(string?)"/> as one call. Does not dispose <paramref name="inner"/>.</summary>
    public sealed class Writer : TextWriter
    {
        private readonly TextWriter _inner;

        public Writer(TextWriter inner)
        {
            _inner = inner;
            NewLine = inner.NewLine;
        }

        public override Encoding Encoding => _inner.Encoding;

        public override void Write(char value) => _inner.Write(value);

        public override void Write(string? value) => _inner.Write(value is null ? null : Text(value));

        public override void Write(char[] buffer, int index, int count) => _inner.Write(Text(new string(buffer, index, count)));

        public override void Write(ReadOnlySpan<char> buffer) => _inner.Write(Text(new string(buffer)));

        public override void Flush() => _inner.Flush();
    }
}