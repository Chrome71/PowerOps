using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO039RareTrigger : IRule
{
    // The interval, per frequency, that reaches 90 days.
    private static readonly Dictionary<string, long> Thresholds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Day"] = 90,
        ["Week"] = 13,
        ["Month"] = 3,
        ["Hour"] = 2_160,
        ["Minute"] = 129_600,
        ["Second"] = 7_776_000,
    };

    public string Id => "PO039";

    public Severity Severity => Severity.Warning;

    public string Title => "Flow is scheduled about every 90 days or less often";

    public string Fix => "Trigger the flow at least every 90 days, or give it an owner with a premium or Process licence, or the service may turn it off.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        from x in RuleHelpers.CloudFlows(solution)
        from t in x.Flow.Triggers
        where t.Frequency is not null && t.Interval is > 0 && Thresholds.TryGetValue(t.Frequency, out var min) && (long)t.Interval >= min
        select new Finding(Id, Severity, $"Flow '{x.Workflow.Name}' runs on a Recurrence with frequency {t.Frequency} and interval {t.Interval}; a flow not triggered within 90 days may be turned off.", x.Workflow.File, t.Name);
}