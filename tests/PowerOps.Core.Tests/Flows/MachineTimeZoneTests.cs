using System.Text.Json.Nodes;

using PowerOps.Core.Flows;

namespace PowerOps.Core.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MachineTimeZoneCollection
{
    public const string Name = "Machine time zone";
}

/// <summary>The date functions must not depend on the time zone of the computer that runs them.</summary>
[Collection(MachineTimeZoneCollection.Name)]
public class MachineTimeZoneTests
{
    private sealed class NoContext : IExpressionContext
    {
        public JsonNode? Outputs(string action) => throw new NotSupportedException();

        public JsonNode? TriggerOutputs() => throw new NotSupportedException();

        public JsonNode? Variable(string name) => throw new NotSupportedException();

        public JsonNode? Parameter(string name) => throw new NotSupportedException();

        public JsonNode? Items(string loop) => throw new NotSupportedException();

        public JsonNode? Item() => throw new NotSupportedException();

        public JsonNode? Workflow() => throw new NotSupportedException();

        public JsonNode? Result(string scope) => throw new NotSupportedException();

        public string UtcNow() => "2018-04-15T13:00:00Z";

        public string Guid() => throw new NotSupportedException();

        public void ChargeCopy(long characters)
        {
        }
    }

    [Fact]
    public void Date_functions_give_the_same_results_on_a_computer_in_Los_Angeles()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "TZ sets the time zone of a Linux process");
        var saved = Environment.GetEnvironmentVariable("TZ");
        try
        {
            Environment.SetEnvironmentVariable("TZ", "America/Los_Angeles");
            TimeZoneInfo.ClearCachedData();
            Assert.Equal(TimeSpan.FromHours(-8), TimeZoneInfo.Local.BaseUtcOffset);

            (string Expression, string Expected)[] cases =
            [
                ("addHours('2018-03-15T10:30:00+02:00', 1)", "\"2018-03-15T09:30:00.0000000+00:00\""),
                ("startOfDay('2018-03-15T01:00:00Z')", "\"2018-03-15T00:00:00.0000000Z\""),
                ("dayOfWeek('2018-03-15T01:00:00Z')", "4"),
                ("convertFromUtc('2018-01-01T08:00:00Z', 'Pacific Standard Time')", "\"2018-01-01T00:00:00.0000000\""),
                ("convertTimeZone('2018-01-01T00:00:00', 'Pacific Standard Time', 'Eastern Standard Time')", "\"2018-01-01T03:00:00.0000000\""),
                ("formatDateTime('2018-03-15T00:00:00Z', 'zzz')", "\"+00:00\""),
            ];
            foreach (var (expression, expected) in cases)
            {
                var actual = Expression.Evaluate("@" + expression, new NoContext());
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), actual), $"{expression}: expected {expected}, got {actual?.ToJsonString()}");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", saved);
            TimeZoneInfo.ClearCachedData();
        }
    }
}