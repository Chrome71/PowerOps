using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public static class Analyzer
{
    public static IReadOnlyList<IRule> Rules { get; } = new IRule[]
    {
        new PO001UndefinedConnectionReference(),
        new PO002RequiredEnvironmentVariableWithoutValue(),
        new PO003DependencyWithoutOwningSolution(),
        new PO004UndefinedChildFlow(),
        new PO005UnusedConnectionReference(),
        new PO006HardCodedEnvironmentUrl(),
        new PO007EnvironmentSpecificDefaultValue(),
        new PO008ShippedCurrentValue(),
        new PO009ForeignPrefix(),
        new PO010DefaultPublisher(),
        new PO011FlowOff(),
        new PO012RowUpdatedTriggerWithoutFilterColumns(),
        new PO013RootComponentWithoutSourceFolder(),
        new PO014CanvasHardCodedUrl(),
        new PO015CreateTriggerWithFilterColumns(),
        new PO016FlowSizeOrDepth(),
        new PO017NestedLoop(),
        new PO018CanvasForAllPatch(),
        new PO019WebApiWildcardFields(),
        new PO020WebApiNoFields(),
        new PO021PermissionWithoutRole(),
        new PO022ChildPermissionRoles(),
        new PO023AnonymousWrite(),
        new PO024SiteMapPageMissing(),
        new PO025AppPageLimits(),
        new PO026CodeAppDependencyMissing(),
        new PO027McpToolWithoutGenerativeOrchestration(),
        new PO028AgentInstructionsLength(),
        new PO029AgentWithoutAuthentication(),
        new PO030ToolMakerConnection(),
        new PO031AgentToolCount(),
        new PO032AppSharedWithAdminsOnly(),
        new PO033RoleWithoutAppBaseline(),
        new PO034RoleWritesUnusedTable(),
        new PO035NavigationName(),
        new PO036CanvasRepeatedFormula(),
        new PO037CatchTellsNoOne(),
        new PO038StaticResultOn(),
        new PO039RareTrigger(),
        new PO040LongApproval(),
    }.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();

    public static string HelpUri(string ruleId) => $"{Output.SarifReport.Repo}/blob/main/docs/rules/{ruleId}.md";

    public static IRule? Find(string ruleId) => Rules.FirstOrDefault(r => r.Id == ruleId);

    /// <summary>What the rules that implement <see cref="INotChecked"/> did not check, distinct and sorted (ordinal).</summary>
    public static IReadOnlyList<string> NotChecked(Solution solution) =>
        Rules.OfType<INotChecked>().SelectMany(r => r.NotChecked(solution)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    /// <summary>All findings, sorted by rule id, component, message, file (ordinal). File comes last because it differs between zip and unpacked layouts.</summary>
    public static IReadOnlyList<Finding> Run(Solution solution) =>
        Rules.SelectMany(r => r.Evaluate(solution))
            .OrderBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.Component, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ThenBy(f => f.File, StringComparer.Ordinal)
            .ToList();
}