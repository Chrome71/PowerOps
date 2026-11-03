using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO019WebApiWildcardFields : IRule
{
    public string Id => "PO019";

    public Severity Severity => Severity.Warning;

    public string Title => "Power Pages Web API fields setting is *";

    public string Fix => "Replace * with the comma-separated logical names of the columns the site needs, or set UseFieldsFromView to True with a view that holds them.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        PowerPages.WebApiTables(solution)
            .Where(t => t.Enabled is not null && t.Fields is { } f && PowerPages.Str(f, "value")!.Trim() == "*")
            .Select(t => new Finding(
                Id,
                Severity,
                $"Site setting '{t.Fields!.Name}' is '*' with the Web API enabled for '{t.Table}', which Power Pages no longer supports; list the columns before Production.",
                t.Fields.File,
                t.Fields.Name));
}