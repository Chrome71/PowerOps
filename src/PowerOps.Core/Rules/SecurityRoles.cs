using PowerOps.Core.Model;
using PowerOps.Core.Readers;

namespace PowerOps.Core.Rules;

/// <summary>What PO032 to PO034 know about security roles: Learn's role template ids ("Security roles and templates"; constant in all environments), and the privilege names Learn prints for opening a model-driven app.</summary>
internal static class SecurityRoles
{
    public const string SystemAdmin = "627090ff-40a3-4053-8790-584edc5be201";

    public const string Customizer = "119f245c-3cc8-4b62-b31c-d1a046ced15d";

    /// <summary>The 17 standard role template ids, normalized.</summary>
    public static readonly IReadOnlySet<string> Templates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SystemAdmin,
        Customizer,
        "2d101bb3-5ced-4122-83f1-94d5efde4e3b", // Support
        "d892cc0b-28c7-4e88-bd92-72f2c366baed", // Proxy
        "236750cd-45ae-4939-ab12-b24b920ced93", // Basic User
        "85937b6b-91a1-46ed-9778-929fc9f61812", // Business Manager
        "29123793-6ae5-4955-9f1a-f10ceb9705f1", // Vice President of Sales
        "c0ed2f4f-6f92-4691-92ba-78f2931e8fba", // Sales Manager
        "a4be89ff-7c35-4d69-9900-999c3f603e6f", // Salesperson
        "ecfd0b44-5720-45e3-ae68-417ddb0fb654", // CSR
        "1808b939-dd07-4ca7-aa99-ddd2734378f1", // CSR Manager
        "09a25608-d28b-4d47-b57c-79271fe6a525", // Marketing Professional
        "debec338-bca7-4882-ae04-84e6ddda2984", // Schedule Manager
        "6caba073-59a8-4d6b-8e7b-4ccb50c5166b", // Vice President of Marketing
        "d9d602db-2761-4170-877f-983494567c08", // Marketing Manager
        "dcd60b89-421c-44ae-bff0-dd6323df885c", // Scheduler
        "b4b40b17-cf37-4ea8-b2c5-b580f2f48654", // Knowledge Manager
    };

    /// <summary>The privileges whose names Learn prints for opening a model-driven app: Read on Model-driven App, User and Saved View.</summary>
    public static readonly IReadOnlyList<string> AppOpening = ["prvReadAppModule", "prvReadUser", "prvReadUserQuery"];

    /// <summary>The verbs of a table privilege; `AppendTo` comes before `Append`, which is its prefix.</summary>
    public static readonly string[] Verbs = ["Create", "Read", "Write", "Delete", "AppendTo", "Append", "Assign", "Share"];

    public static bool IsAdmin(string id) => Is(id, SystemAdmin) || Is(id, Customizer);

    public static bool IsTemplate(string id) => SolutionParser.NormalizeId(id) is { } n && Templates.Contains(n);

    /// <summary>The roles of the solution that are not a documented template, in file order.</summary>
    public static IEnumerable<Role> Custom(Solution solution) =>
        solution.Roles.Where(r => !IsTemplate(r.Id)).OrderBy(r => r.File, StringComparer.Ordinal).ThenBy(r => r.Name, StringComparer.Ordinal);

    /// <summary>The table of a `prv&lt;verb&gt;&lt;table&gt;` privilege for the first of <paramref name="verbs"/> that prefixes it (ordinal), or null when none does or no table is left.</summary>
    public static string? Table(string privilege, params string[] verbs)
    {
        foreach (var verb in verbs)
        {
            if (privilege.StartsWith("prv" + verb, StringComparison.Ordinal))
            {
                return privilege.Length > 3 + verb.Length ? privilege[(3 + verb.Length)..] : null;
            }
        }

        return null;
    }

    private static bool Is(string id, string template) => string.Equals(SolutionParser.NormalizeId(id), template, StringComparison.OrdinalIgnoreCase);
}