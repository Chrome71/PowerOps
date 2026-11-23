using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Json.Schema;

using PowerOps.Tests.Shared;

namespace PowerOps.Cli.Tests;

public class ReaderOracleFactsTests
{
    private static readonly string OracleDir = Path.Combine(Golden.RepoRoot, "bench", "oracle-facts");
    private static readonly string CanaryDir = Path.Combine(Golden.RepoRoot, "bench", "canaries", "readers");
    private static readonly Lazy<JsonSchema> Schema = new(() => JsonSchema.FromText(File.ReadAllText(Path.Combine(OracleDir, "schema.json"))));

    public static TheoryData<string> FactFiles() =>
        new(Directory.GetFiles(Path.Combine(OracleDir, "facts"), "*.json")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(FactFiles))]
    public void Every_oracle_fact_file_validates_against_schema(string fileName)
    {
        var path = Path.Combine(OracleDir, "facts", fileName);
        var node = JsonNode.Parse(File.ReadAllText(path))!;
        var errors = OutputSchemaTests.Errors(Schema.Value, node);

        Assert.True(errors.Length == 0, $"{fileName} does not match bench/oracle-facts/schema.json:\n{errors}");
        Assert.Equal(Path.GetFileNameWithoutExtension(fileName), (string?)node["fact_id"]);
    }

    [Fact]
    public void Oracle_facts_cover_xml_zip_and_yaml_and_preserve_absent_vs_false()
    {
        var all = Directory.GetFiles(Path.Combine(OracleDir, "facts"), "*.json")
            .ToDictionary(f => Path.GetFileNameWithoutExtension(f)!, f => JsonNode.Parse(File.ReadAllText(f))!.AsObject());

        Assert.Equal(13, all.Count);
        Assert.Equal(["xml", "yaml", "zip"], all.Values.Select(v => (string)v["layout"]!).Distinct().Order(StringComparer.Ordinal));

        // Absent table IsAuditEnabled AND absent <attributes> element must stay null (never coerced to false or []).
        var dataverseTools = all["agents-dataverse-tools"];
        var demoTicket = dataverseTools["tables"]!.AsArray().Single(t => (string?)t!["name"] == "demo_Ticket")!;
        var demoQueue = dataverseTools["tables"]!.AsArray().Single(t => (string?)t!["name"] == "demo_Queue")!;
        Assert.Null(demoTicket["is_audit_enabled"]);
        Assert.Null(demoQueue["is_audit_enabled"]);
        Assert.Null(demoQueue["columns"]);

        // Schema also accepts an explicit empty columns array (representing explicit <attributes/>) and null solution.managed.
        var explicitEmpty = dataverseTools.DeepClone().AsObject();
        explicitEmpty["tables"]!.AsArray().Single(t => (string?)t!["name"] == "demo_Queue")!["columns"] = new JsonArray();
        explicitEmpty["solution"]!["managed"] = null;
        Assert.Equal("", OutputSchemaTests.Errors(Schema.Value, explicitEmpty));

        // Explicit 0/false must stay false (never dropped to null).
        var seedXml = all["powerops-seed-v1-xml"];
        var seedYaml = all["powerops-seed-v1-yaml"];
        var xmlTable = seedXml["tables"]!.AsArray().Single()!;
        var yamlTable = seedYaml["tables"]!.AsArray().Single()!;
        Assert.False((bool?)xmlTable["is_audit_enabled"]);
        Assert.False((bool?)yamlTable["is_audit_enabled"]);
        Assert.Equal(xmlTable["columns"]!.ToJsonString(), yamlTable["columns"]!.ToJsonString());
        Assert.Equal(seedXml["flows"]!.AsArray()[0]!["triggers"]!.ToJsonString(), seedYaml["flows"]!.AsArray()[0]!["triggers"]!.ToJsonString());
        Assert.Equal(seedXml["flows"]!.AsArray()[0]!["connection_bindings"]!.ToJsonString(), seedYaml["flows"]!.AsArray()[0]!["connection_bindings"]!.ToJsonString());

        // Both FieldSecurityProfile XML layouts are covered, distinguishing explicit canreadunmasked=0 from absent null.
        var clsPerms = all["column-level-security-unmanaged"]["field_security_profiles"]!.AsArray().Single()!["permissions"]!.AsArray();
        Assert.Equal(4, clsPerms.Count);
        Assert.All(clsPerms, p => Assert.Null(p!["can_read_unmasked"]));

        var sol5 = all["eval-solution-05"];
        var popsNotes = sol5["tables"]!.AsArray().Single()!["columns"]!.AsArray().Single(c => (string?)c!["logical_name"] == "pops_notes")!;
        Assert.False((bool?)popsNotes["is_secured"]);
        Assert.False((bool?)popsNotes["is_audit_enabled"]);
        var sol5Perm = Assert.Single(sol5["field_security_profiles"]!.AsArray().Single()!["permissions"]!.AsArray())!;
        Assert.Equal(0, (int?)sol5Perm["can_read_unmasked"]);

        // Direct connection binding (eval-solution-04) and .msapp hosts (eval-solution-09).
        var sol4Bindings = all["eval-solution-04"]["flows"]!.AsArray()
            .Single(f => (string?)f!["name"] == "Email daily work item summary")!["connection_bindings"]!.AsArray();
        var outlook = sol4Bindings.Single(b => (string?)b!["key"] == "shared_office365")!;
        Assert.Null(outlook["connection_reference_logical_name"]);
        Assert.Equal("shared-office365-00000000-0000-0000-0000-000000000001", (string?)outlook["direct_connection_name"]);

        var sol9Hosts = all["eval-solution-09"]["msapp_hosts"]!.AsArray();
        Assert.Equal(2, sol9Hosts.Count);
        Assert.All(sol9Hosts, h => Assert.Equal("dev.contoso.example", (string?)h!["host"]));
    }

    private static JsonObject SampleFact() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(OracleDir, "facts", "equipment-checkout.json")))!.AsObject();

    [Theory]
    [InlineData("schemaVersion", null)]
    [InlineData("layout", "\"other\"")]
    [InlineData("source_sha256", "\"not-a-hash\"")]
    [InlineData("source_sha256", "\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\\n\"")]
    [InlineData("solution_path", "\"../outside\"")]
    [InlineData("solution_path", "\"tests/fixtures/bad\\u0000name\"")]
    [InlineData("extra", "true")]
    public void Invalid_oracle_fact_records_are_rejected(string field, string? patch)
    {
        var fact = SampleFact();
        if (patch is null)
        {
            fact.Remove(field);
        }
        else
        {
            fact[field] = JsonNode.Parse(patch);
        }

        Assert.False(Schema.Value.Evaluate(JsonSerializer.SerializeToElement(fact)).IsValid);
    }

    [Fact]
    public void Invalid_nested_column_flag_and_malformed_host_are_rejected_by_schema()
    {
        var sol5 = JsonNode.Parse(File.ReadAllText(Path.Combine(OracleDir, "facts", "eval-solution-05.json")))!.AsObject();
        sol5["tables"]!.AsArray()[0]!["columns"]!.AsArray()[0]!["is_secured"] = "0";
        Assert.False(Schema.Value.Evaluate(JsonSerializer.SerializeToElement(sol5)).IsValid);

        var sol9 = JsonNode.Parse(File.ReadAllText(Path.Combine(OracleDir, "facts", "eval-solution-09.json")))!.AsObject();
        sol9["msapp_hosts"]!.AsArray()[0]!["host"] = "a..b";
        Assert.False(Schema.Value.Evaluate(JsonSerializer.SerializeToElement(sol9)).IsValid);
    }

    [Fact]
    public async Task Commitment_verifier_passes_and_rejects_tampering_duplicates_and_escapes()
    {
        var check = await RunVerifier("--check");
        Assert.True(check.Code == 0, check.Output);

        var copy = Directory.CreateTempSubdirectory("powerops-c0-tamper-");
        try
        {
            CopyDirectory(Path.Combine(Golden.RepoRoot, "bench", "oracle-facts"), Path.Combine(copy.FullName, "bench", "oracle-facts"));
            CopyDirectory(Path.Combine(Golden.RepoRoot, "bench", "canaries", "readers"), Path.Combine(copy.FullName, "bench", "canaries", "readers"));
            CopyDirectory(Path.Combine(Golden.RepoRoot, "tests", "fixtures"), Path.Combine(copy.FullName, "tests", "fixtures"));
            CopyDirectory(Path.Combine(Golden.RepoRoot, "tests", "eval", "roles", "cases"), Path.Combine(copy.FullName, "tests", "eval", "roles", "cases"));

            // Windows CRLF checkout of text files must still pass.
            var manifest = Path.Combine(copy.FullName, "bench", "oracle-facts", "manifest.sha256");
            var originalManifest = File.ReadAllText(manifest);
            File.WriteAllText(manifest, originalManifest.ReplaceLineEndings("\r\n"));
            var crlfCheck = await RunVerifier("--root", copy.FullName, "--check");
            Assert.True(crlfCheck.Code == 0, crlfCheck.Output);

            // Prepending a conflicting duplicate manifest row must fail.
            File.WriteAllText(manifest, new string('0', 64) + "  bench/oracle-facts/schema.json\n" + originalManifest);
            var dupCheck = await RunVerifier("--root", copy.FullName, "--check");
            Assert.NotEqual(0, dupCheck.Code);
            Assert.Contains("duplicate manifest.sha256 entry: bench/oracle-facts/schema.json", dupCheck.Output, StringComparison.Ordinal);
            File.WriteAllText(manifest, originalManifest);

            // Escaping solution_path must be refused before reading outside root.
            var eqPath = Path.Combine(copy.FullName, "bench", "oracle-facts", "facts", "equipment-checkout.json");
            var eqOriginal = File.ReadAllText(eqPath);
            var eqJson = JsonNode.Parse(eqOriginal)!.AsObject();
            eqJson["solution_path"] = "../outside";
            File.WriteAllText(eqPath, eqJson.ToJsonString());
            var escapeCheck = await RunVerifier("--root", copy.FullName, "--check");
            Assert.NotEqual(0, escapeCheck.Code);
            Assert.Contains("invalid relative path", escapeCheck.Output, StringComparison.Ordinal);
            File.WriteAllText(eqPath, eqOriginal);

            // Symlinked fact file must be refused before reading (Linux/macOS).
            if (!OperatingSystem.IsWindows())
            {
                var outside = Path.Combine(Path.GetTempPath(), $"powerops-c0-outside-{Guid.NewGuid():N}.json");
                File.WriteAllText(outside, eqOriginal);
                try
                {
                    File.Delete(eqPath);
                    File.CreateSymbolicLink(eqPath, outside);
                    var linkCheck = await RunVerifier("--root", copy.FullName, "--check");
                    Assert.NotEqual(0, linkCheck.Code);
                    Assert.Contains("symlink refused", linkCheck.Output, StringComparison.Ordinal);
                }
                finally
                {
                    File.Delete(eqPath);
                    File.WriteAllText(eqPath, eqOriginal);
                    File.Delete(outside);
                }
            }

            // Tampering with one canary file must fail input_sha256 verification.
            var canaryRole = Path.Combine(copy.FullName, "bench", "canaries", "readers", "cases", "canary-d", "Roles", "cnry_Operator.xml");
            File.WriteAllText(canaryRole, File.ReadAllText(canaryRole).Replace("Global", "Basic", StringComparison.Ordinal));
            var tamperCheck = await RunVerifier("--root", copy.FullName, "--check");
            Assert.NotEqual(0, tamperCheck.Code);
            Assert.Contains("canary canary-d: input_sha256 mismatch", tamperCheck.Output, StringComparison.Ordinal);
        }
        finally
        {
            copy.Delete(recursive: true);
        }
    }

    [Fact]
    public void Opaque_canaries_contain_no_defect_hints_and_no_path_leaks()
    {
        string[] forbiddenHints = ["defective", "skip", "canary_kind", "defect_class", "roles-folder"];
        var casesDir = Path.Combine(CanaryDir, "cases");
        foreach (var file in Directory.GetFiles(casesDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(casesDir, file).Replace('\\', '/');
            foreach (var hint in forbiddenHints)
            {
                Assert.DoesNotContain(hint, rel, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain("/" + "home/", rel, StringComparison.Ordinal);
            Assert.DoesNotContain("file:" + "//", rel, StringComparison.OrdinalIgnoreCase);

            if (file.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase))
            {
                using var zip = ZipFile.OpenRead(file);
                foreach (var entry in zip.Entries)
                {
                    foreach (var hint in forbiddenHints)
                    {
                        Assert.DoesNotContain(hint, entry.FullName, StringComparison.OrdinalIgnoreCase);
                    }

                    Assert.DoesNotContain("/" + "home/", entry.FullName, StringComparison.Ordinal);
                    Assert.DoesNotContain("file:" + "//", entry.FullName, StringComparison.OrdinalIgnoreCase);

                    using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                    var content = reader.ReadToEnd();
                    foreach (var hint in forbiddenHints)
                    {
                        Assert.DoesNotContain(hint, content, StringComparison.OrdinalIgnoreCase);
                    }

                    Assert.DoesNotContain("/" + "home/", content, StringComparison.Ordinal);
                    Assert.DoesNotContain("file:" + "//", content, StringComparison.OrdinalIgnoreCase);
                }

                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var hint in forbiddenHints)
            {
                Assert.DoesNotContain(hint, text, StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var root in new[] { OracleDir, CanaryDir })
        {
            foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".pyc", StringComparison.OrdinalIgnoreCase)
                    || file.Contains("__pycache__", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.DoesNotContain("/" + "home/", text, StringComparison.Ordinal);
                Assert.DoesNotContain("file:" + "//", text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(dest, Path.GetRelativePath(source, dir)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(dest, Path.GetRelativePath(source, file)), overwrite: true);
        }
    }

    private static async Task<(int Code, string Output)> RunVerifier(params string[] args)
    {
        var script = Path.Combine(CanaryDir, "verify_commitments.py");
        foreach (var (file, prefix) in new[] { ("python3", Array.Empty<string>()), ("py", ["-3"]), ("python", []) })
        {
            try
            {
                var version = await Run(file, [.. prefix, "--version"]);
                if (version.Code != 0 || !version.Output.TrimStart().StartsWith("Python 3.", StringComparison.Ordinal))
                {
                    continue;
                }
            }
            catch (Win32Exception)
            {
                continue;
            }

            return await Run(file, [.. prefix, script, .. args]);
        }

        const string reason = "Python 3 is missing (tried python3, py -3 and python).";
        Assert.False(Environment.GetEnvironmentVariable("POWEROPS_REQUIRE_TOOLS") == "1", reason);
        throw Xunit.Sdk.SkipException.ForSkip(reason);
    }

    private static async Task<(int Code, string Output)> Run(string file, string[] args)
    {
        var start = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Environment = { ["PYTHONDONTWRITEBYTECODE"] = "1" },
        };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await stdout + await stderr);
    }
}