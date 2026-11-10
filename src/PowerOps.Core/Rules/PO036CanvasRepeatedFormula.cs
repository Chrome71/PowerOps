using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO036CanvasRepeatedFormula : IRule
{
    public string Id => "PO036";

    public Severity Severity => Severity.Warning;

    public string Title => "Canvas app repeats the same formula in three or more places";

    public string Fix => "Define the formula once as a named formula or user-defined function in App.Formulas, or in a component, and use that name in each place.";

    // PowerOps's own thresholds, not Microsoft's: they keep short repeats such as Parent.Width or ThisItem.Title quiet.
    private const int MinLength = 80;
    private const int MinPlaces = 3;

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        solution.CanvasFormulas
            .Select(f => (Formula: f, Text: Normalise(f.Formula)))
            .Where(x => x.Text.Length >= MinLength)
            // Keyed on the app's folder (canvasapps/<name>) as PO014 is, because display names need not be unique.
            .GroupBy(x => (App: string.Join('/', x.Formula.File.Split('/').Take(2)), x.Text))
            .Select(g => g.Select(x => x.Formula).DistinctBy(Place, StringComparer.Ordinal).OrderBy(Place, StringComparer.Ordinal).ToList())
            .Where(places => places.Count >= MinPlaces)
            // The formula's text stays out of the message: it can hold tenant values.
            .Select(places => new Finding(Id, Severity, $"Canvas app '{places[0].App}' has the same formula in {places.Count} places ({string.Join(", ", places.Take(3).Select(Place))}{(places.Count > 3 ? $", and {places.Count - 3} more" : "")}); a change must be made in each.", places[0].File, Place(places[0])));

    private static string Place(CanvasFormula f) => $"{f.Control}.{f.Property}";

    // shortcut: whitespace inside a string literal or a comment is collapsed too, so formulas that differ only there count as the same; upgrade to PO018's token scan if that gives false findings.
    /// <summary>The formula without its leading `=`, each run of whitespace as one space, trimmed; case is kept.</summary>
    private static string Normalise(string formula) =>
        string.Join(' ', (formula.StartsWith('=') ? formula[1..] : formula).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}