using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

/// <summary>A rule whose input the export may not hold; it says what it did not check, and <c>analyze</c> prints each line as a <c>note:</c> on stderr.</summary>
public interface INotChecked
{
    IEnumerable<string> NotChecked(Solution solution);
}