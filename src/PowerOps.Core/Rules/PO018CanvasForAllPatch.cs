using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO018CanvasForAllPatch : IRule, INotChecked
{
    public string Id => "PO018";

    public Severity Severity => Severity.Warning;

    public string Title => "Canvas app formula calls Patch or ForAll inside ForAll";

    public string Fix => "Loop once and pass the loop's result to a single Patch call, and don't nest ForAll.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var f in solution.CanvasFormulas)
        {
            var (patch, nested) = Scan(f.Formula);
            var where = $"{f.Control}.{f.Property}";
            if (patch)
            {
                yield return new Finding(Id, Severity, $"Canvas app '{f.App}' calls Patch inside ForAll in {where}, which sends one request per record.", f.File, where);
            }

            if (nested)
            {
                yield return new Finding(Id, Severity, $"Canvas app '{f.App}' nests ForAll inside ForAll in {where}, which multiplies the iterations.", f.File, where);
            }
        }
    }

    // An .msapp with no source in it is neither a finding nor a pass.
    public IEnumerable<string> NotChecked(Solution solution) =>
        solution.CanvasAppsWithoutSource.Select(file => $"PO014 and PO018 not checked for canvas app '{file}': its .msapp has no Src folder (older app, resave in Studio).");

    // shortcut: a token scan, so a Patch inside a user-defined function that ForAll calls is not seen, upgrade if a rule needs the syntax tree.
    /// <summary>One linear pass: strings, quoted names and comments are skipped, and a stack of open parentheses says which ones belong to a ForAll call.</summary>
    private static (bool PatchInForAll, bool ForAllInForAll) Scan(string text)
    {
        var open = new Stack<bool>(); // one entry per open parenthesis: true when a ForAll call's
        var inForAll = 0;
        bool patch = false, nested = false;
        var i = 0;
        while (i < text.Length && !(patch && nested))
        {
            var c = text[i];
            if (c is '"' or '\'')
            {
                i = EndOfQuoted(text, i);
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                var end = text.AsSpan(i).IndexOfAny('\r', '\n');
                i = end < 0 ? text.Length : i + end;
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 2;
            }
            else if (c == '(')
            {
                open.Push(false);
                i++;
            }
            else if (c == ')')
            {
                if (open.Count > 0 && open.Pop())
                {
                    inForAll--;
                }

                i++;
            }
            else if (char.IsLetterOrDigit(c) || c == '_')
            {
                var start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                var name = text.AsSpan(start, i - start);
                var isForAll = name.Equals("ForAll", StringComparison.OrdinalIgnoreCase);
                if (!isForAll && !name.Equals("Patch", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var next = i;
                while (next < text.Length && char.IsWhiteSpace(text[next]))
                {
                    next++;
                }

                if (next >= text.Length || text[next] != '(')
                {
                    continue;
                }

                patch |= !isForAll && inForAll > 0;
                nested |= isForAll && inForAll > 0;
                open.Push(isForAll);
                inForAll += isForAll ? 1 : 0;
                i = next + 1;
            }
            else
            {
                i++;
            }
        }

        return (patch, nested);
    }

    /// <summary>The index after the string or quoted name that starts at <paramref name="start"/>; a doubled quote inside it is a quote, and an unclosed one runs to the end.</summary>
    private static int EndOfQuoted(string text, int start)
    {
        var quote = text[start];
        for (var i = start + 1; i < text.Length; i++)
        {
            if (text[i] != quote)
            {
                continue;
            }

            if (i + 1 < text.Length && text[i + 1] == quote)
            {
                i++;
                continue;
            }

            return i + 1;
        }

        return text.Length;
    }
}