using PowerOps.Core.Drift;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Drift;

public class DriftParsersTests
{
    private const string Planted = "PLANTED-SECRET-77";
    private const string FlowId = "4c8dd007-dfcb-f111-a05c-0022480697a8";

    private static string Sample(string name) =>
        File.ReadAllText(Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", "drift", "sample", name));

    private static readonly string CaptureDir = Path.Combine(Golden.RepoRoot, "tests", "fixtures", "pac", "drift", "capture");

    private static string Cap(string name) => File.ReadAllText(Path.Combine(CaptureDir, name));

    private static string Rows(params string[] rows) => $$"""{"@odata.context":"x","value":[{{string.Join(",", rows)}}]}""";

    [Theory]
    [InlineData("dev", false)]
    [InlineData("test", true)]
    public void Read2_parses_the_solution(string env, bool managed)
    {
        var s = DriftParsers.Solution(Sample($"{env}-read2.json"));

        Assert.Equal((Guid.Parse(env == "dev" ? "00000000-0000-0000-0000-0000000000a1" : "00000000-0000-0000-0000-0000000000a2"), "1.0.0.0", managed), s);
    }

    [Fact]
    public void More_than_one_solution_row_is_rejected()
    {
        var row = """{"solutionid":"00000000-0000-0000-0000-0000000000a1","version":"1","ismanaged":false}""";

        Assert.Throws<InvalidDataException>(() => DriftParsers.Solution(Rows(row, row)));
    }

    [Fact]
    public void Read3_parses_the_components()
    {
        var c = DriftParsers.Components(Sample("dev-read3.json"));

        Assert.Equal(
            [(1, Guid.Parse("00000000-0000-0000-0000-0000000000d1")), (29, Guid.Parse(FlowId)), (380, Guid.Parse("00000000-0000-0000-0000-0000000000d2"))],
            c);
    }

    [Fact]
    public void Read4_parses_the_flows() =>
        Assert.Equal([new SnapshotFlow(Guid.Parse(FlowId), "Notify on new work item", "on")], DriftParsers.Flows(Sample("test-read4.json")));

    [Theory]
    [InlineData(0, "off")]
    [InlineData(1, "on")]
    [InlineData(2, "suspended")]
    public void Flow_state_codes_map_to_names(int code, string state) =>
        Assert.Equal(state, DriftParsers.Flows(Rows($$"""{"workflowid":"{{FlowId}}","name":"F","statecode":{{code}}}""")).Single().State);

    [Theory]
    [InlineData("3")]
    [InlineData("-1")]
    [InlineData("1.5")]
    public void An_unknown_statecode_is_rejected(string code) =>
        Assert.Throws<InvalidDataException>(() => DriftParsers.Flows(Rows($$"""{"workflowid":"{{FlowId}}","name":"F","statecode":{{code}}}""")));

    [Fact]
    public void Read5_parses_the_variable_and_hashes_the_multi_line_value()
    {
        var v = DriftParsers.Variables(Sample("dev-read5.json")).Single();

        Assert.Equal("pops_DefaultStatus", v.SchemaName);
        Assert.Equal(Snapshot.Hash("Open, then\nreview"), v.ValueHash);
        Assert.NotNull(v.ValueHash);
    }

    [Theory]
    [InlineData("""{"schemaname":"a"}""")]
    [InlineData("""{"schemaname":"a","v.value":""}""")]
    [InlineData("""{"schemaname":"a","v.value":null}""")]
    public void An_absent_or_empty_value_has_no_hash(string row) =>
        Assert.Null(DriftParsers.Variables(Rows(row)).Single().ValueHash);

    [Fact]
    public void A_value_of_another_type_is_rejected() =>
        Assert.Throws<InvalidDataException>(() => DriftParsers.Variables(Rows("""{"schemaname":"a","v.value":5}""")));

    [Fact]
    public void Read6_parses_a_bound_reference() =>
        Assert.Equal(
            [new SnapshotReference(Guid.Parse("00000000-0000-0000-0000-0000000000d3"), "pops_sharedoffice365_6a2f1", "/providers/Microsoft.PowerApps/apis/shared_office365", true)],
            DriftParsers.References(Sample("dev-read6.json")));

    [Theory]
    [InlineData("")]
    [InlineData(""","connectionid":""")]
    [InlineData(""","connectionid":null""")]
    public void An_absent_or_empty_connectionid_is_unbound(string connection)
    {
        var tail = connection.EndsWith(':') ? connection + "\"\"" : connection;

        Assert.False(DriftParsers.References(Rows($$"""{"connectionreferenceid":"{{FlowId}}","connectionreferencelogicalname":"r","connectorid":"c"{{tail}}}""")).Single().Bound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\"connectionreferenceid\":null,")]
    [InlineData("\"connectionreferenceid\":\"not-a-guid\",")]
    [InlineData("\"connectionreferenceid\":5,")]
    public void A_missing_or_invalid_connectionreferenceid_is_rejected(string id) =>
        Assert.Throws<InvalidDataException>(() => DriftParsers.References(Rows($$"""{{{id}}"connectionreferencelogicalname":"r","connectorid":"c"}""")));

    private static string Layer(string solution, int order) => $$"""{"msdyn_componentid":"{{FlowId}}","msdyn_solutionname":"{{solution}}","msdyn_order":{{order}}}""";

    [Fact]
    public void Read7_finds_the_active_layer()
    {
        Assert.True(DriftParsers.Layers(Rows(Layer("Active", 2), Layer("PowerOpsSeed", 1))));
        Assert.False(DriftParsers.Layers(Rows(Layer("PowerOpsSeed", 1))));
        Assert.False(DriftParsers.Layers(Rows(Layer("active", 2))));
    }

    [Fact]
    public void Read7_with_no_rows_is_rejected() =>
        Assert.Contains("no layer rows", Assert.Throws<InvalidDataException>(() => DriftParsers.Layers(Rows())).Message);

    [Fact]
    public void Read7_ignores_every_other_property_and_never_quotes_the_component_data()
    {
        var row = $$"""{"msdyn_solutionname":"Active","msdyn_changes":"{{Planted}}","msdyn_componentjson":{"x":"{{Planted}}"},"msdyn_name":"n","extra":[1]}""";

        Assert.True(DriftParsers.Layers(Rows(row)));
        Assert.DoesNotContain(Planted, Assert.Throws<InvalidDataException>(() => DriftParsers.Layers(Rows($$"""{"msdyn_solutionname":5,"msdyn_changes":"{{Planted}}"}"""))).Message);
    }

    [Fact]
    public void The_captured_reads_2_to_6_parse()
    {
        var (dev, test) = (DriftParsers.Solution(Cap("dev-read2.json")), DriftParsers.Solution(Cap("test-read2.json")));
        Assert.Equal(("1.0.0.0", false), (dev!.Value.Version, dev.Value.Managed));
        Assert.Equal(("1.0.0.0", true), (test!.Value.Version, test.Value.Managed));
        Assert.Null(DriftParsers.Solution(Cap("dev-read2-missing.json")));

        var (devComponents, testComponents) = (DriftParsers.Components(Cap("dev-read3.json")), DriftParsers.Components(Cap("test-read3.json")));
        Assert.Equal([1, 29, 300, 380, 10164, 10164], devComponents.Select(c => c.Type).Order());
        Assert.Equal([1, 29, 300, 380, 10164, 10164], testComponents.Select(c => c.Type).Order());
        Assert.Equal([(29, Guid.Parse(FlowId))], devComponents.Intersect(testComponents));

        Assert.Equal([new SnapshotFlow(Guid.Parse(FlowId), "Stamp work item status", "on")], DriftParsers.Flows(Cap("dev-read4.json")));
        Assert.Equal(DriftParsers.Flows(Cap("dev-read4.json")), DriftParsers.Flows(Cap("test-read4.json")));
        Assert.All(new[] { "dev", "test" }, env =>
        {
            Assert.Equal(new SnapshotVariable("pops_DefaultStatus", Snapshot.Hash("Open, then\nreview")), Assert.Single(DriftParsers.Variables(Cap($"{env}-read5.json"))));
            var references = DriftParsers.References(Cap($"{env}-read6.json"));
            Assert.Equal(["pops_SeedDataverse", "pops_SpareDataverse"], references.Select(r => r.LogicalName).Order());
            Assert.All(references, r => Assert.True(r.Bound));
        });
    }

    [Fact]
    public void The_captured_error_body_has_its_code_and_a_placeholder_message() =>
        Assert.Equal("<error message>", DriftParsers.ErrorMessage(Cap("dev-malformed.json")));

    public static TheoryData<string> LayerFiles() => [.. Directory.GetFiles(CaptureDir, "test-*-read7-*.json").Select(f => Path.GetFileName(f)!)];

    [Theory]
    [MemberData(nameof(LayerFiles))]
    public void A_captured_layer_read_has_an_unmanaged_layer_only_for_the_edited_flow(string name) =>
        Assert.Equal(name == "test-edited-read7-29-1.json", DriftParsers.Layers(Cap(name)));

    [Fact]
    public void Empty_value_arrays_give_empty_lists()
    {
        Assert.Empty(DriftParsers.Components(Rows()));
        Assert.Empty(DriftParsers.Flows(Rows()));
        Assert.Empty(DriftParsers.Variables(Rows()));
        Assert.Empty(DriftParsers.References(Rows()));
    }

    [Fact]
    public void More_records_is_rejected_for_every_read()
    {
        var text = Sample("morerecords.json");

        Assert.Contains("more than one page", Assert.Throws<InvalidDataException>(() => DriftParsers.Components(text)).Message);
        Assert.Throws<InvalidDataException>(() => DriftParsers.Flows(text));
        Assert.Throws<InvalidDataException>(() => DriftParsers.Variables(text));
        Assert.Throws<InvalidDataException>(() => DriftParsers.References(text));
        Assert.Throws<InvalidDataException>(() => DriftParsers.Solution(text));
    }

    [Fact]
    public void A_label_where_a_number_belongs_is_rejected() =>
        Assert.Throws<InvalidDataException>(() => DriftParsers.Components(Rows($$"""{"componenttype":"Entity","objectid":"{{FlowId}}"}""")));

    [Theory]
    [InlineData("""{"componenttype":1}""")]
    [InlineData("""{"componenttype":1,"objectid":null}""")]
    [InlineData("""{"componenttype":1,"objectid":"not-a-guid"}""")]
    [InlineData("""{"objectid":"4c8dd007-dfcb-f111-a05c-0022480697a8"}""")]
    [InlineData("5")]
    public void A_missing_null_or_malformed_required_property_is_rejected(string row) =>
        Assert.Throws<InvalidDataException>(() => DriftParsers.Components(Rows(row)));

    [Theory]
    [InlineData("""{"solutionid":"00000000-0000-0000-0000-0000000000a1","version":"1","ismanaged":"false"}""")]
    [InlineData("""{"solutionid":"00000000-0000-0000-0000-0000000000a1","version":"1"}""")]
    [InlineData("""{"solutionid":"00000000-0000-0000-0000-0000000000a1","version":1,"ismanaged":false}""")]
    public void A_solution_row_of_the_wrong_shape_is_rejected(string row) =>
        Assert.Throws<InvalidDataException>(() => DriftParsers.Solution(Rows(row)));

    [Theory]
    [InlineData("""{"@odata.context":"x"}""")]
    [InlineData("""{"value":{"a":1}}""")]
    [InlineData("""{"value":null}""")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("Error: something went wrong")]
    [InlineData("")]
    [InlineData("""{"error":{"code":"1","message":"m"}}""")]
    public void Output_that_is_not_an_object_with_a_value_array_is_rejected(string text)
    {
        Assert.Throws<InvalidDataException>(() => DriftParsers.Components(text));
        Assert.Throws<InvalidDataException>(() => DriftParsers.Solution(text));
    }

    [Fact]
    public void Duplicate_keys_in_one_read_are_rejected()
    {
        var component = $$"""{"componenttype":1,"objectid":"{{FlowId}}"}""";
        var flow = $$"""{"workflowid":"{{FlowId}}","name":"F","statecode":1}""";

        Assert.Throws<InvalidDataException>(() => DriftParsers.Components(Rows(component, component)));
        Assert.Throws<InvalidDataException>(() => DriftParsers.Flows(Rows(flow, flow.Replace("\"F\"", "\"G\""))));
        Assert.Throws<InvalidDataException>(() => DriftParsers.Variables(Rows("""{"schemaname":"Abc"}""", """{"schemaname":"aBC"}""")));
        Assert.Throws<InvalidDataException>(() => DriftParsers.References(Rows(
            $$"""{"connectionreferenceid":"{{FlowId}}","connectionreferencelogicalname":"Abc","connectorid":"c"}""",
            $$"""{"connectionreferenceid":"{{FlowId}}","connectionreferencelogicalname":"aBC","connectorid":"c"}""")));
    }

    [Fact]
    public void The_same_id_with_another_type_is_not_a_duplicate() =>
        Assert.Equal(2, DriftParsers.Components(Rows($$"""{"componenttype":1,"objectid":"{{FlowId}}"}""", $$"""{"componenttype":29,"objectid":"{{FlowId}}"}""")).Count);

    [Theory]
    [InlineData("https://contoso-dev.crm.dynamics.com/")]
    [InlineData("https://contoso-dev.crm.dynamics.com")]
    [InlineData("https://CONTOSO-dev.crm4.dynamics.com/")]
    public void A_valid_environment_url_is_returned_without_the_trailing_slash(string url)
    {
        var uri = DriftParsers.EnvironmentUrl(url);

        Assert.Equal(url.TrimEnd('/').ToLowerInvariant(), uri.GetLeftPart(UriPartial.Authority));
        Assert.Equal("https", uri.Scheme);
    }

    public static TheoryData<string> BadUrls() =>
    [
        "", " ", "http://contoso.crm.dynamics.com/", "contoso.crm.dynamics.com", $"https://{Planted}.example.com/org/path",
        $"https://{Planted}:secret@contoso.crm.dynamics.com/", "https://contoso.crm.dynamics.com:443/", "https://contoso.crm.dynamics.com:8443",
        $"https://contoso.crm.dynamics.com/?q={Planted}", $"https://contoso.crm.dynamics.com/#{Planted}", "https://contoso.crm.dynamics.com//",
        $"https://{Planted}%2e.com/", $"https://{Planted}&x.com/", "https://contoso.crm.dynamics.com/\n", "https://contoso.crm.dynamics.com\n", "https://",
        "https:///", "https://contoso crm.com/", "https://contoso_dev.com/",
    ];

    [Theory]
    [MemberData(nameof(BadUrls))]
    public void A_bad_environment_url_is_rejected_without_quoting_it(string url)
    {
        var e = Assert.Throws<InvalidDataException>(() => DriftParsers.EnvironmentUrl(url));

        Assert.DoesNotContain(Planted, e.Message);
        Assert.DoesNotContain("example.com", e.Message);
        Assert.DoesNotContain("contoso", e.Message);
    }

    [Theory]
    [InlineData("https://contoso.crm.dynamics.com")]
    [InlineData("https://contoso.crm4.dynamics.com")]
    [InlineData("https://contoso.api.crm9.dynamics.com")]
    [InlineData("https://contoso.crm.microsoftdynamics.us")]
    [InlineData("https://contoso.crm.appsplatform.us")]
    [InlineData("https://contoso.crm.dynamics.cn")]
    [InlineData("https://CONTOSO.CRM.Dynamics.COM")]
    public void A_documented_dataverse_host_is_allowed(string url) => Assert.True(DriftParsers.IsDataverseHost(new Uri(url)));

    [Theory]
    [InlineData("https://localhost")]
    [InlineData("https://contoso")]
    [InlineData("https://dynamics.com")]
    [InlineData("https://evildynamics.com")]
    [InlineData("https://contoso.dynamics.com.evil.example")]
    [InlineData("https://contoso.crm.microsoftdynamics.de")]
    [InlineData("https://contoso.appsplatform.us")]
    [InlineData("https://contoso.microsoftdynamics.us")]
    [InlineData("https://10.0.0.1")]
    [InlineData("https://[::1]")]
    public void Any_other_host_is_refused(string url) => Assert.False(DriftParsers.IsDataverseHost(new Uri(url)));

    [Fact]
    public void A_null_environment_url_is_rejected() => Assert.Throws<InvalidDataException>(() => DriftParsers.EnvironmentUrl(null));

    [Fact]
    public void No_rejection_message_quotes_the_input()
    {
        string[] bad =
        [
            Planted,
            $$"""{"value":"{{Planted}}"}""",
            $$"""{"value":[{"componenttype":"{{Planted}}","objectid":"{{Planted}}"}]}""",
            $$"""{"value":[{"workflowid":"{{FlowId}}","name":"{{Planted}}","statecode":99}]}""",
            $$"""{"value":[{"schemaname":"{{Planted}}"},{"schemaname":"{{Planted}}"}]}""",
            $$"""{"@Microsoft.Dynamics.CRM.morerecords":true,"value":[],"x":"{{Planted}}"}""",
        ];
        Action<string>[] parsers =
        [
            t => DriftParsers.Solution(t), t => DriftParsers.Components(t), t => DriftParsers.Flows(t), t => DriftParsers.Variables(t), t => DriftParsers.References(t),
        ];

        var thrown = 0;
        foreach (var text in bad)
        {
            foreach (var parse in parsers)
            {
                try
                {
                    parse(text);
                }
                catch (InvalidDataException e)
                {
                    thrown++;
                    Assert.DoesNotContain(Planted, e.Message);
                    Assert.Null(e.InnerException);
                }
            }
        }

        Assert.True(thrown >= bad.Length);
    }

    [Fact]
    public void ErrorMessage_reads_the_documented_error_body() =>
        Assert.Equal("Entity 'nosuchtable' With Id = 00000000-0000-0000-0000-000000000000 Does Not Exist", DriftParsers.ErrorMessage(Sample("error-400.json")));

    [Theory]
    [InlineData("")]
    [InlineData("Bad Request")]
    [InlineData("<html><body>502</body></html>")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{"value":[]}""")]
    [InlineData("""{"error":"text"}""")]
    [InlineData("""{"error":{"code":"1"}}""")]
    [InlineData("""{"error":{"message":5}}""")]
    [InlineData("""{"error":null}""")]
    public void ErrorMessage_is_null_for_anything_else(string body) => Assert.Null(DriftParsers.ErrorMessage(body));
}