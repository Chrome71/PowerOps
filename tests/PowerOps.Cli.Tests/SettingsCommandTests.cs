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

    // The text without its last line, settings-hash: <64 lowercase hex> (M96), which the tests of that line check.
    private static string Hashless(string output)
    {
        Assert.Matches("\nsettings-hash: [0-9a-f]{64}\n$", output);
        return output[..output.LastIndexOf("settings-hash: ", StringComparison.Ordinal)];
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
            var r = Run("settings", "validate", zip, "--settings", Valid);
            Assert.Equal((0, "no problems\n", ""), (r.Code, Hashless(r.Out), r.Err));
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
            Assert.Equal("no problems\n", Hashless(output));
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
                Hashless(output));
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
        Assert.Equal("no problems\n", Hashless(output));
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

    // ---- settings-hash (M96 row 1): the SHA-256 of the file's bytes, which deploy plan records as its settingsSha256 ----

    // The bytes of each file and their SHA-256, computed once with sha256sum.
    internal const string PlainSettings = """{"EnvironmentVariables":[{"SchemaName":"pops_DefaultStatus","Value":"Open"}],"ConnectionReferences":[{"LogicalName":"pops_SeedDataverse","ConnectionId":"fake-connection-id","ConnectorId":"/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps"}]}""";
    internal const string PlainSha256 = "abc99232c27f450bf1a79ee2edb32522d58f09aad1420f94fb59b19545b691d4";
    internal const string BomCrlfSettings = "\uFEFF{\r\n\"EnvironmentVariables\":[{\"SchemaName\":\"pops_DefaultStatus\",\"Value\":\"Open\"}],\r\n\"ConnectionReferences\":[{\"LogicalName\":\"pops_SeedDataverse\",\"ConnectionId\":\"fake-connection-id\",\"ConnectorId\":\"/providers/Microsoft.PowerApps/apis/shared_commondataserviceforapps\"}]\r\n}\r\n";
    internal const string BomCrlfSha256 = "64faf0940bbe0ec26903d3cde8cef20f48b4222b9d53a07008003e5be5ca4d8d";
    private const string EmptySettings = """{"EnvironmentVariables":[],"ConnectionReferences":[]}""";
    private const string EmptySha256 = "4dcfc0cb195a42fd227309ae44ee2c3c1ed07d95b58689f85f61b38c1c9ae5f8";

    [Theory]
    [InlineData(PlainSettings, PlainSha256, 0)]
    [InlineData(BomCrlfSettings, BomCrlfSha256, 0)]
    [InlineData(EmptySettings, EmptySha256, 1)]
    public void The_last_line_and_the_json_field_give_the_settings_files_sha256(string text, string sha256, int exit)
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            // UTF-8 with no BOM of its own, so the bytes are exactly the text's (a leading U+FEFF is written as EF BB BF).
            File.WriteAllText(file, text);

            var (code, output, err) = Run("settings", "validate", Seed, "--settings", file);
            var (jsonCode, json, _) = Run("settings", "validate", Seed, "--settings", file, "--format", "json");

            Assert.Equal((exit, exit, ""), (code, jsonCode, err));
            Assert.EndsWith($"\nsettings-hash: {sha256}\n", output);
            Assert.Matches("^(no problems|[0-9]+ problems?)$", output.Split('\n')[^3]);
            var names = JsonNode.Parse(json)!.AsObject().Select(p => p.Key).ToList();
            Assert.Equal(["schemaVersion", "solution", "settings", "settingsSha256", "problems"], names);
            Assert.Equal(sha256, (string?)JsonNode.Parse(json)!["settingsSha256"]);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Exit_2_and_3_print_no_settings_hash()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(file, "{" + new string(' ', 1024 * 1024) + "}");

            var large = Run("settings", "validate", Seed, "--settings", file);
            var absent = Run("settings", "validate", Seed, "--settings", file + ".absent");
            File.WriteAllText(file, "{ not json");
            var invalid = Run("settings", "validate", Seed, "--settings", file, "--format", "json");

            Assert.Equal([(3, ""), (2, ""), (3, "")], new[] { large, absent, invalid }.Select(r => (r.Code, r.Out)));
        }
        finally
        {
            File.Delete(file);
        }
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