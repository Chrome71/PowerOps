using System.Text.Json;
using System.Text.Json.Nodes;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class SettingsCommandTests
{
    private static readonly string Seed = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "powerops-seed", "v1");
    private static readonly string Valid = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "settings", "powerops-seed-v1.valid.json");
    private static readonly string Problems = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "settings", "powerops-seed-v1.problems.json");

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var o = new StringWriter();
        var e = new StringWriter();
        return (Program.Run(args, o, e), o.ToString(), e.ToString());
    }

    [Fact]
    public void Generate_prints_the_settings_file_and_matches_golden()
    {
        var (code, output, err) = Run("settings", "generate", Seed);

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Golden.Verify(output, "settings-generate.json");
    }

    [Fact]
    public void Generate_out_writes_the_file_without_a_bom_and_prints_nothing()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var (code, output, _) = Run("settings", "generate", Seed, "--out", file);

            Assert.Equal(0, code);
            Assert.Equal("", output);
            var bytes = File.ReadAllBytes(file);
            Assert.NotEqual(0xEF, bytes[0]);
            Assert.Equal(Run("settings", "generate", Seed).Out, System.Text.Encoding.UTF8.GetString(bytes));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Generate_out_into_a_missing_folder_exits_3_and_writes_nothing()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var (code, output, err) = Run("settings", "generate", Seed, "--out", Path.Combine(folder, "settings.json"));

        Assert.Equal(3, code);
        Assert.Equal("", output);
        Assert.StartsWith("error: ", err);
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public void Settings_on_the_exported_zip_match_the_unpacked_folder()
    {
        var zip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
        try
        {
            SolutionZip.Create(Seed, zip);

            Assert.Equal(Run("settings", "generate", Seed).Out, Run("settings", "generate", zip).Out);
            Assert.Equal((0, "no problems\n", ""), Run("settings", "validate", zip, "--settings", Valid));
        }
        finally
        {
            File.Delete(zip);
        }
    }

    [Fact]
    public void Generated_file_with_values_filled_in_validates_clean()
    {
        var node = JsonNode.Parse(Run("settings", "generate", Seed).Out)!;
        foreach (var e in node["EnvironmentVariables"]!.AsArray())
        {
            e!["Value"] = "fake-value";
        }

        foreach (var c in node["ConnectionReferences"]!.AsArray())
        {
            c!["ConnectionId"] = "fake-connection-id";
        }

        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(file, node.ToJsonString());

            var (code, output, _) = Run("settings", "validate", Seed, "--settings", file);

            Assert.Equal(0, code);
            Assert.Equal("no problems\n", output);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Generated_file_as_is_has_empty_connection_ids_and_values()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(file, Run("settings", "generate", Seed).Out);

            var (code, output, _) = Run("settings", "validate", Seed, "--settings", file);

            Assert.Equal(1, code);
            Assert.Equal(
                "emptyConnectionId connectionReference pops_SeedDataverse: has no ConnectionId\n" +
                "emptyValue environmentVariable pops_DefaultStatus: has an empty Value, which pac solution import rejects; set a value, or leave the entry out to keep its default or current value\n" +
                "2 problems\n",
                output);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Validate_valid_file_with_pac_extras_exits_0()
    {
        var (code, output, err) = Run("settings", "validate", Seed, "--settings", Valid);

        Assert.Equal(0, code);
        Assert.Equal("", err);
        Assert.Equal("no problems\n", output);
    }

    [Fact]
    public void Validate_problems_text_matches_golden()
    {
        var (code, output, _) = Run("settings", "validate", Seed, "--settings", Problems);

        Assert.Equal(1, code);
        Golden.Verify(output, "settings-validate.txt");
    }

    [Fact]
    public void Validate_problems_json_matches_golden()
    {
        var (code, output, _) = Run("settings", "validate", Seed, "--settings", Problems, "--format", "json");

        Assert.Equal(1, code);
        using var doc = JsonDocument.Parse(output);
        Assert.Equal("schemaVersion", doc.RootElement.EnumerateObject().First().Name);
        // The settings path is echoed as given; make it repo-relative for the golden.
        Golden.Verify(output.Replace(Problems.Replace("\\", "\\\\"), "tests/fixtures/settings/powerops-seed-v1.problems.json"), "settings-validate.json");
    }

    [Fact]
    public void Missing_settings_file_is_a_usage_error()
    {
        var (code, _, err) = Run("settings", "validate", Seed, "--settings", Path.Combine(Golden.RepoRoot, "nope.json"));

        Assert.Equal(2, code);
        Assert.StartsWith("error: settings file not found:", err);
    }

    [Fact]
    public void Missing_settings_option_is_a_usage_error() => Assert.Equal(2, Run("settings", "validate", Seed).Code);

    [Theory]
    [InlineData("generate")]
    [InlineData("validate", "--settings", "x.json")]
    public void Missing_solution_path_is_a_usage_error(string sub, params string[] rest)
    {
        var (code, _, err) = Run(["settings", sub, Path.Combine(Golden.RepoRoot, "nope"), .. rest]);

        Assert.Equal(2, code);
        Assert.StartsWith("error: path not found:", err);
    }

    [Fact]
    public void Invalid_json_settings_file_exits_3()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(file, "{ not json");

            var (code, output, err) = Run("settings", "validate", Seed, "--settings", file);

            Assert.Equal(3, code);
            Assert.Equal("", output);
            Assert.StartsWith($"error: {file}: not valid JSON", err);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Unreadable_solution_exits_3()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            File.WriteAllText(file, "x");

            Assert.Equal(3, Run("settings", "generate", file).Code);
        }
        finally
        {
            File.Delete(file);
        }
    }
}