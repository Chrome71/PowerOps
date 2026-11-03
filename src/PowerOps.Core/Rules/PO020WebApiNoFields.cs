using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO020WebApiNoFields : IRule, INotChecked
{
    public string Id => "PO020";

    public Severity Severity => Severity.Warning;

    public string Title => "Power Pages Web API is enabled for a table with no fields setting";

    public string Fix => "Add a fields site setting for the table with the columns the site needs, or set its UseFieldsFromView site setting to True with a view that holds them.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        NoFields(solution).Where(t => !t.UseFieldsFromView)
            .Select(t => new Finding(
                Id,
                Severity,
                $"Site setting '{t.Enabled!.Name}' enables the Web API for '{t.Table}' with no fields setting and UseFieldsFromView not True; set the columns before Production.",
                t.Enabled.File,
                t.Enabled.Name));

    // The view behind UseFieldsFromView is not read, so the table is neither a finding nor a pass.
    public IEnumerable<string> NotChecked(Solution solution) =>
        NoFields(solution).Where(t => t.UseFieldsFromView)
            .Select(t => $"PO020 not checked for table '{t.Table}': UseFieldsFromView is True, and the view that holds the columns is not read.");

    private static IEnumerable<PowerPages.WebApiTable> NoFields(Solution solution) =>
        PowerPages.WebApiTables(solution).Where(t => t.Enabled is not null && t.Fields is null);
}