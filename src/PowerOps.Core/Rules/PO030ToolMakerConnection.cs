using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO030ToolMakerConnection : IRule
{
    public string Id => "PO030";

    public Severity Severity => Severity.Warning;

    public string Title => "Agent tool uses the maker's credentials";

    public string Fix => "Set the tool to use end-user credentials, unless every user may act with the maker's permissions or a trigger calls the tool.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        Agents.Placed(solution).SelectMany(a => Agents.Tools(a)
            .Where(t => string.Equals(t.Mode, "Maker", StringComparison.OrdinalIgnoreCase))
            .Select(t => new Finding(Id, Severity, $"Tool '{t.SchemaName}' of agent '{a.SchemaName}' uses the maker's credentials, so every user acts with the maker's permissions.", t.File, t.SchemaName)));
}