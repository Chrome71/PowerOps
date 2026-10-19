using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public interface IRule
{
    string Id { get; }

    Severity Severity { get; }

    string Title { get; }

    IEnumerable<Finding> Evaluate(Solution solution);
}