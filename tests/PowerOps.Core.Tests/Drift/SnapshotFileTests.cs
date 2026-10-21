using PowerOps.Core.Drift;

namespace PowerOps.Core.Tests.Drift;

public class SnapshotFileTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string IdA = "00000000-0000-0000-0000-000000000001";
    private const string IdB = "00000000-0000-0000-0000-000000000002";

    private static Snapshot Small() => new(
        "Dev", "Seed", true, "1.0.0.0", false, true,
        [new(1, Guid.Parse(IdA), null), new(29, Guid.Parse(IdB), true)],
        [new("a_Var", Hash), new("b_Var", null)],
        [new("a_ref", "/providers/x", true)],
        [new(Guid.Parse(IdB), "Flow", "on")]);

    private static void AssertEqual(Snapshot e, Snapshot a)
    {
        Assert.Equal(e with { Components = [], Variables = [], References = [], Flows = [] }, a with { Components = [], Variables = [], References = [], Flows = [] });
        Assert.Equal(e.Components, a.Components);
        Assert.Equal(e.Variables, a.Variables);
        Assert.Equal(e.References, a.References);
        Assert.Equal(e.Flows, a.Flows);
    }

    // The JSON of Small() with replacements applied, for the rejection tests.
    private static string Json(params (string From, string To)[] edits)
    {
        var json = SnapshotFile.Write(Small());
        foreach (var (from, to) in edits)
        {
            Assert.Contains(from, json);
            json = json.Replace(from, to);
        }

        return json;
    }

    private static void Rejected(string json) => Assert.Throws<InvalidDataException>(() => SnapshotFile.Read(json));

    [Fact]
    public void Write_gives_the_exact_text() =>
        Assert.Equal(
            "{\n  \"schemaVersion\": 1,\n  \"environment\": \"Dev\",\n  \"solution\": \"Seed\",\n  \"installed\": true,\n  \"version\": \"1.0.0.0\",\n  \"managed\": false,\n  \"layersRead\": true,\n" +
            "  \"components\": [\n    {\n      \"type\": 1,\n      \"id\": \"00000000-0000-0000-0000-000000000001\",\n      \"unmanagedLayer\": null\n    },\n    {\n      \"type\": 29,\n      \"id\": \"00000000-0000-0000-0000-000000000002\",\n      \"unmanagedLayer\": true\n    }\n  ],\n" +
            "  \"variables\": [\n    {\n      \"schemaName\": \"a_Var\",\n      \"valueHash\": \"" + Hash + "\"\n    },\n    {\n      \"schemaName\": \"b_Var\",\n      \"valueHash\": null\n    }\n  ],\n" +
            "  \"references\": [\n    {\n      \"logicalName\": \"a_ref\",\n      \"connectorId\": \"/providers/x\",\n      \"bound\": true\n    }\n  ],\n" +
            "  \"flows\": [\n    {\n      \"id\": \"00000000-0000-0000-0000-000000000002\",\n      \"name\": \"Flow\",\n      \"state\": \"on\"\n    }\n  ]\n}\n",
            SnapshotFile.Write(Small()));

    [Fact]
    public void Round_trip_keeps_the_snapshot_and_a_null_version() =>
        AssertEqual(Small() with { Version = null, Managed = null }, SnapshotFile.Read(SnapshotFile.Write(Small() with { Version = null, Managed = null })));

    [Fact]
    public void Lists_come_out_sorted_ordinal()
    {
        var s = Small() with
        {
            Components = [new(29, Guid.Parse(IdB), null), new(29, Guid.Parse(IdA), null), new(1, Guid.Parse(IdB), null)],
            Variables = [new("b", null), new("C", null), new("a", null)],
            References = [new("z", "c", false), new("a", "c", false)],
            Flows = [new(Guid.Parse(IdB), "x", "off"), new(Guid.Parse(IdA), "y", "suspended")],
        };
        var read = SnapshotFile.Read(SnapshotFile.Write(s));

        Assert.Equal([(1, IdB), (29, IdA), (29, IdB)], read.Components.Select(c => (c.Type, c.Id.ToString())).ToArray());
        Assert.Equal(["C", "a", "b"], read.Variables.Select(v => v.SchemaName).ToArray());
        Assert.Equal(["a", "z"], read.References.Select(r => r.LogicalName).ToArray());
        Assert.Equal([IdA, IdB], read.Flows.Select(f => f.Id.ToString()).ToArray());
    }

    [Fact]
    public void Read_sorts_an_unsorted_file()
    {
        var json = Json(("\"a_Var\"", "\"zz\""));
        Assert.Equal(["b_Var", "zz"], SnapshotFile.Read(json).Variables.Select(v => v.SchemaName).ToArray());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("")]
    public void Non_objects_are_rejected(string json) => Rejected(json);

    [Fact]
    public void Wrong_or_missing_schema_version_is_rejected()
    {
        Rejected(Json(("\"schemaVersion\": 1", "\"schemaVersion\": 2")));
        Rejected(Json(("  \"schemaVersion\": 1,\n", "")));
        Rejected(Json(("\"schemaVersion\": 1", "\"schemaVersion\": \"1\"")));
        var e = Assert.Throws<InvalidDataException>(() => SnapshotFile.Read(Json(("\"schemaVersion\": 1", "\"schemaVersion\": 2"))));
        Assert.Contains("1", e.Message);
    }

    [Theory]
    [InlineData("\"environment\": \"Dev\"", "\"environment\": 5")]
    [InlineData("\"environment\": \"Dev\"", "\"environment\": \"\"")]
    [InlineData("\"solution\": \"Seed\"", "\"solution\": \"\"")]
    [InlineData("\"installed\": true", "\"installed\": \"yes\"")]
    [InlineData("\"layersRead\": true", "\"layersRead\": 1")]
    [InlineData("\"version\": \"1.0.0.0\"", "\"version\": 1")]
    [InlineData("\"managed\": false", "\"managed\": \"no\"")]
    [InlineData("\"type\": 1,", "\"type\": \"1\",")]
    [InlineData("\"unmanagedLayer\": true", "\"unmanagedLayer\": \"x\"")]
    [InlineData("\"bound\": true", "\"bound\": null")]
    [InlineData("\"connectorId\": \"/providers/x\"", "\"connectorId\": null")]
    [InlineData("\"name\": \"Flow\"", "\"name\": 3")]
    [InlineData("\"valueHash\": null", "\"valueHash\": 3")]
    public void Wrong_types_and_empty_names_are_rejected(string from, string to) => Rejected(Json((from, to)));

    [Theory]
    [InlineData("  \"environment\": \"Dev\",\n")]
    [InlineData("  \"solution\": \"Seed\",\n")]
    [InlineData("  \"installed\": true,\n")]
    [InlineData("  \"version\": \"1.0.0.0\",\n")]
    [InlineData("  \"managed\": false,\n")]
    [InlineData("  \"layersRead\": true,\n")]
    [InlineData("      \"bound\": true\n")]
    [InlineData("      \"state\": \"on\"\n")]
    public void Missing_properties_are_rejected(string line)
    {
        // Dropping a last property leaves a trailing comma, so also drop that comma.
        var json = Json((line, ""));
        Rejected(json.Replace(",\n    }", "\n    }"));
    }

    [Theory]
    [InlineData("components")]
    [InlineData("variables")]
    [InlineData("references")]
    [InlineData("flows")]
    public void A_missing_or_non_array_list_is_rejected(string list)
    {
        var json = Json();
        Rejected(json.Replace($"\"{list}\": [", $"\"{list}x\": ["));
        Rejected(System.Text.RegularExpressions.Regex.Replace(json, $"\"{list}\": \\[", $"\"{list}\": 5, \"zz\": ["));
    }

    [Fact]
    public void Duplicate_keys_are_rejected()
    {
        var s = Small();
        Rejected(SnapshotFile.Write(s with { Components = [.. s.Components, new(1, Guid.Parse(IdA), true)] }));
        Rejected(SnapshotFile.Write(s with { Variables = [new("a", null), new("A", null)] }));
        Rejected(SnapshotFile.Write(s with { References = [new("a", "c", true), new("A", "c", true)] }));
        Rejected(SnapshotFile.Write(s with { Flows = [.. s.Flows, new(s.Flows[0].Id, "other", "off")] }));
    }

    [Fact]
    public void Same_id_with_another_type_is_not_a_duplicate() =>
        Assert.Equal(2, SnapshotFile.Read(SnapshotFile.Write(Small() with { Components = [new(1, Guid.Parse(IdA), null), new(2, Guid.Parse(IdA), null)] })).Components.Count);

    [Theory]
    [InlineData("\"state\": \"on\"", "\"state\": \"On\"")]
    [InlineData("\"state\": \"on\"", "\"state\": \"missing\"")]
    [InlineData("\"state\": \"on\"", "\"state\": null")]
    public void Unknown_states_are_rejected(string from, string to) => Rejected(Json((from, to)));

    [Theory]
    [InlineData("0123456789ABCDEF0123456789abcdef0123456789abcdef0123456789abcdef")]
    [InlineData("0123456789abcdef")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg")]
    [InlineData("")]
    public void A_bad_value_hash_is_rejected(string hash) => Rejected(Json(($"\"{Hash}\"", $"\"{hash}\"")));

    [Fact]
    public void An_id_that_is_not_a_guid_is_rejected()
    {
        Rejected(Json(($"\"{IdA}\"", "\"nope\"")));
        Rejected(Json(($"\"{IdB}\"", "5")));
    }

    [Fact]
    public void Messages_do_not_quote_the_file()
    {
        const string Secret = "SECRET-MARKER-42";
        foreach (var json in new[]
        {
            Json(("\"Dev\"", $"\"{Secret}\""), ("\"state\": \"on\"", $"\"state\": \"{Secret}\"")),
            Json(($"\"{Hash}\"", $"\"{Secret}\"")),
            Json(($"\"{IdA}\"", $"\"{Secret}\"")),
            Json(("\"schemaVersion\": 1", $"\"schemaVersion\": \"{Secret}\"")),
            Json(("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"x\": " + Secret)),
            $"{{\"a\": \"{Secret}\"}}",
            Secret,
        })
        {
            var e = Assert.Throws<InvalidDataException>(() => SnapshotFile.Read(json));
            Assert.DoesNotContain(Secret, e.Message);
        }
    }

    [Fact]
    public void Load_reads_a_file_and_rejects_one_over_1_MB()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "s.json");
            File.WriteAllText(path, SnapshotFile.Write(Small()));
            AssertEqual(Small(), SnapshotFile.Load(path));

            File.WriteAllText(path, new string(' ', 1_048_577));
            Assert.Throws<InvalidDataException>(() => SnapshotFile.Load(path));
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Hash_of_a_value_with_a_space_a_comma_and_a_line_break_is_the_known_sha256()
    {
        const string Expected = "f73d3322a6860b8c820e97d3726a70aac6ed33ef0d8c4ae0567b7bdb177ac2f9";
        Assert.Equal(Expected, Snapshot.Hash("a b,\nc"));
        Assert.Equal(Expected, Snapshot.Hash("a b,\r\nc"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Hash_of_no_value_is_null(string? value) => Assert.Null(Snapshot.Hash(value));
}