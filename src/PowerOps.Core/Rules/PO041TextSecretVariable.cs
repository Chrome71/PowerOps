using PowerOps.Core.Model;

namespace PowerOps.Core.Rules;

public sealed class PO041TextSecretVariable : IRule
{
    // The Dataverse type value of a Text (String) environment variable.
    private const string TextType = "100000000";

    private static readonly string[] SecretWords = ["secret", "password", "pwd", "token", "apikey", "api_key", "connectionstring"];

    // A name such as demo_SecretName holds the name of a secret, not the secret.
    private static readonly string[] NameSuffixes = ["name", "id", "url", "uri", "ref", "reference", "path", "date", "expiry", "expiration"];

    public string Id => "PO041";

    public Severity Severity => Severity.Warning;

    public string Title => "Text environment variable looks like it holds a secret";

    public string Fix => "Make it an environment variable of type Secret backed by Azure Key Vault, and remove its value from the solution and the settings files.";

    public IEnumerable<Finding> Evaluate(Solution solution) =>
        solution.EnvironmentVariables
            .Where(v => v.Type == TextType
                && SecretWords.Any(w => v.SchemaName.Contains(w, StringComparison.OrdinalIgnoreCase))
                && !NameSuffixes.Any(x => v.SchemaName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
            .Select(v => new Finding(Id, Severity, $"Environment variable '{v.SchemaName}' is of type Text, but its name suggests it holds a secret.", v.File, v.SchemaName));
}