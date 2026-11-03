using System.Text.Json;

using PowerOps.Core.Model;
using PowerOps.Core.Readers;

namespace PowerOps.Core.Rules;

/// <summary>What PO019 to PO023 share. Only active components count; the reader has already checked the kinds of the keys read here.</summary>
internal static class PowerPages
{
    public const int SiteSetting = 9;
    public const int WebRole = 11;
    public const int TablePermission = 18;

    /// <summary>The Web API settings of one table on one site, by key (`enabled`, `fields`, `usefieldsfromview`, lower case).</summary>
    public sealed record WebApiTable(string Table, IReadOnlyDictionary<string, PowerPageComponent> Settings)
    {
        public PowerPageComponent? Enabled => On(Settings, "enabled");

        public bool UseFieldsFromView => On(Settings, "usefieldsfromview") is not null;

        /// <summary>The `fields` setting when its value is not empty.</summary>
        public PowerPageComponent? Fields => Settings.TryGetValue("fields", out var f) && !string.IsNullOrWhiteSpace(Str(f, "value")) ? f : null;

        private static PowerPageComponent? On(IReadOnlyDictionary<string, PowerPageComponent> settings, string key) =>
            settings.TryGetValue(key, out var s) && string.Equals(Str(s, "value")?.Trim(), "true", StringComparison.OrdinalIgnoreCase) ? s : null;
    }

    public static IEnumerable<WebApiTable> WebApiTables(Solution s) =>
        s.PowerPageComponents
            .Where(c => c is { Type: SiteSetting, Active: true })
            .Select(c => (Component: c, Parts: c.Name.Split('/')))
            .Where(x => x.Parts.Length == 3 && x.Parts[1].Length > 0 && x.Parts[0].Equals("Webapi", StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => (x.Component.SiteId, Table: x.Parts[1].ToLowerInvariant()))
            .Select(g => new WebApiTable(
                g.First().Parts[1],
                g.GroupBy(x => x.Parts[2].ToLowerInvariant()).ToDictionary(k => k.Key, k => k.First().Component)));

    public static IEnumerable<PowerPageComponent> Permissions(Solution s) =>
        s.PowerPageComponents.Where(c => c is { Type: TablePermission, Active: true });

    /// <summary>A string key of `Content`, or null when it is absent or JSON null.</summary>
    public static string? Str(PowerPageComponent c, string key) =>
        c.Content.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>A boolean key of `Content`; false when it is absent or JSON null.</summary>
    public static bool Flag(PowerPageComponent c, string key) =>
        c.Content.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.True;

    /// <summary>The permission's own web role ids, normalized.</summary>
    public static IReadOnlyList<string> OwnRoles(PowerPageComponent permission) =>
        permission.Content.TryGetProperty("adx_entitypermission_webrole", out var v) && v.ValueKind == JsonValueKind.Array
            ? [.. v.EnumerateArray().Select(x => SolutionParser.NormalizeId(x.GetString())).OfType<string>()]
            : [];

    /// <summary>XML can't hold U+0000, so no component id is this.</summary>
    private const string NotAnId = "\0";

    /// <summary>The parent permission's id, normalized. A value of another JSON kind gives <see cref="NotAnId"/>, which matches no id: the permission has a parent that is not found.</summary>
    public static string? ParentId(PowerPageComponent permission) =>
        !permission.Content.TryGetProperty("parententitypermission", out var v) || v.ValueKind == JsonValueKind.Null
            ? null
            : v.ValueKind == JsonValueKind.String ? SolutionParser.NormalizeId(v.GetString()) : NotAnId;

    public static string Table(PowerPageComponent permission) => Str(permission, "entitylogicalname") ?? "";

    public static Dictionary<string, PowerPageComponent> ById(Solution s, int type) =>
        s.PowerPageComponents.Where(c => c.Type == type).GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());

    /// <summary>What a permission's chain of parents gives it. <c>Roles</c>: its own, or, when it has none, its parent's, up the chain. <c>Resolved</c> is false when the chain ends at a parent that is not in the solution before any role is found. <c>Off</c> is true when some ancestor is inactive.</summary>
    public sealed record Effect(IReadOnlySet<string> Roles, bool Resolved, bool Off);

    /// <summary>The <see cref="Effect"/> of each permission, computed once: an iterative walk up the parents with a memo, so a chain of n permissions costs O(n) in all.</summary>
    public sealed class Chains(IReadOnlyDictionary<string, PowerPageComponent> permissions)
    {
        private static readonly IReadOnlySet<string> None = new HashSet<string>();
        private readonly Dictionary<string, Effect> _memo = [];

        public Effect Of(PowerPageComponent permission)
        {
            var path = new List<PowerPageComponent>();
            var onPath = new HashSet<string>();
            var start = permissions.GetValueOrDefault(permission.Id) ?? permission;
            PowerPageComponent? current = start;
            while (current is not null && !_memo.ContainsKey(current.Id) && onPath.Add(current.Id))
            {
                path.Add(current);
                current = ParentId(current) is { } parentId ? permissions.GetValueOrDefault(parentId) : null;
            }

            for (var i = path.Count - 1; i >= 0; i--)
            {
                var node = path[i];
                var own = OwnRoles(node);
                var parent = ParentId(node) is { } id ? permissions.GetValueOrDefault(id) : null;
                // A parent that has no effect yet is on a cycle: it adds no roles and no inactive ancestor.
                var above = parent is null ? null : _memo.GetValueOrDefault(parent.Id) ?? new Effect(None, true, false);
                _memo[node.Id] = new Effect(
                    own.Count > 0 ? new HashSet<string>(own) : above?.Roles ?? None,
                    own.Count > 0 || ParentId(node) is null || (above?.Resolved ?? false),
                    parent is not null && (!parent.Active || above!.Off));
            }

            return _memo[start.Id];
        }
    }

    /// <summary>The name of the role's component, or its id when the solution doesn't hold it.</summary>
    public static string RoleName(string id, IReadOnlyDictionary<string, PowerPageComponent> roles) =>
        roles.TryGetValue(id, out var r) && r.Name.Length > 0 ? r.Name : id;
}