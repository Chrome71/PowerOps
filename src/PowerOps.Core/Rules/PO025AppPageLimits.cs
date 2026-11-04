using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO025AppPageLimits : IRule, INotChecked
{
    private const int MaxPages = 25;
    private const int MaxConnectors = 10;
    private const int MaxConnectionReferences = 20;

    public string Id => "PO025";

    public Severity Severity => Severity.Warning;

    public string Title => "Model-driven app has more than 25 custom pages, 10 connectors or 20 connection references";

    public string Fix => "Move custom pages to another app, or remove pages and connections, until the app has at most 25 custom pages, 10 connectors and 20 connection references.";

    public IEnumerable<Finding> Evaluate(Solution solution)
    {
        foreach (var app in Apps(solution))
        {
            if (app.Pages > MaxPages)
            {
                yield return Report(app, $"App '{app.Module.UniqueName}' has {app.Pages} custom pages, more than {MaxPages}.");
            }

            if (app.Connectors > MaxConnectors)
            {
                yield return Report(app, $"The custom pages of app '{app.Module.UniqueName}' use {app.Connectors} connectors, more than {MaxConnectors}.");
            }

            if (app.References > MaxConnectionReferences)
            {
                yield return Report(app, $"The custom pages of app '{app.Module.UniqueName}' use {app.References} connection references, more than {MaxConnectionReferences}.");
            }
        }
    }

    // A page's name is known even when its metadata is not, so it counts toward 25; only its connections are unknown.
    public IEnumerable<string> NotChecked(Solution solution)
    {
        foreach (var a in Apps(solution))
        {
            if (a.Unread > 0)
            {
                yield return $"PO025 not checked for app '{a.Module.UniqueName}': {a.Unread} of its custom pages have no readable metadata in the solution, so their connectors and connection references are not counted.";
            }

            if (a.Named > 1)
            {
                yield return $"PO025 not checked for app '{a.Module.UniqueName}': it names {a.Named} site maps in the solution, and only '{a.Counted}' is counted.";
            }

            foreach (var name in a.Missing)
            {
                yield return $"PO025 not checked for app '{a.Module.UniqueName}': its site map '{name}' is not in the solution, so the custom pages it opens are not counted.";
            }
        }
    }

    private Finding Report(AppCounts app, string message) => new(Id, Severity, message, app.Module.File, app.Module.UniqueName);

    private sealed record AppCounts(AppModule Module, int Pages, int Connectors, int References, int Unread, int Named, string? Counted, List<string> Missing);

    // The distinct pages of a set, the connectors and keys over its held pages, and the pages with no readable metadata.
    private sealed record Tally(HashSet<string> Pages, HashSet<string> Connectors, HashSet<string> References, int Unread);

    private static Tally Count(IEnumerable<string> pages, Dictionary<string, CanvasAppMeta> canvas)
    {
        var tally = new Tally(new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        var unread = 0;
        foreach (var page in pages)
        {
            if (!tally.Pages.Add(page))
            {
                continue;
            }

            if (canvas.TryGetValue(page, out var app) && app.Connections is { } connections)
            {
                tally.References.UnionWith(connections.Keys);
                tally.Connectors.UnionWith(connections.Values.Where(id => id.Length > 0));
            }
            else
            {
                unread++;
            }
        }

        return tally with { Unread = unread };
    }

    // Where two records share a name the first in ordinal file order is used; Learn doesn't say how to count from an export, so these are distinct ids and distinct keys over the pages held.
    private static IEnumerable<AppCounts> Apps(Solution solution)
    {
        var maps = First(solution.SiteMaps.OrderBy(m => m.File, StringComparer.Ordinal), m => m.UniqueName);
        var canvas = First(solution.CanvasApps, c => c.Name);
        var tallies = new Dictionary<string, Tally>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in solution.AppModules.OrderBy(a => a.File, StringComparer.Ordinal))
        {
            // An appelement may be a component library (CanvasAppType 1), so it is not a custom page when its metadata says another type; a site map target always is.
            var own = module.Pages.Where(n => !canvas.TryGetValue(n, out var held) || held.Type is null or 2);
            var named = module.SiteMaps.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToList();
            var known = named.Where(maps.ContainsKey).ToList();

            // Learn says an app can have its own site map and every observed export names one, so the others are noted, not unioned.
            var map = known.Count > 0 ? maps[known[0]] : null;
            if (map is null)
            {
                tallies.TryAdd("", Count([], canvas));
            }
            else if (!tallies.ContainsKey(map.UniqueName))
            {
                tallies[map.UniqueName] = Count(map.Pages, canvas);
            }

            var basis = tallies[map?.UniqueName ?? ""];
            var extras = Count(own.Where(n => !basis.Pages.Contains(n)), canvas);
            yield return new AppCounts(
                module,
                basis.Pages.Count + extras.Pages.Count,
                basis.Connectors.Count + extras.Connectors.Count(c => !basis.Connectors.Contains(c)),
                basis.References.Count + extras.References.Count(k => !basis.References.Contains(k)),
                basis.Unread + extras.Unread,
                known.Count,
                known.Count > 0 ? known[0] : null,
                named.Where(n => !maps.ContainsKey(n)).ToList());
        }
    }

    // The reader lists the files in ordinal order, so the first record of a name is the first in file order.
    private static Dictionary<string, T> First<T>(IEnumerable<T> records, Func<T, string> name)
    {
        var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in records)
        {
            result.TryAdd(name(r), r);
        }

        return result;
    }
}