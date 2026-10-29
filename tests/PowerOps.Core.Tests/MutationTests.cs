using System.Text;
using System.Text.RegularExpressions;

using PowerOps.Core.Flows;
using PowerOps.Core.Output;
using PowerOps.Core.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests;

/// <summary>
/// A seeded mutation test over a copy of the fixtures (decision 31, no fuzzing library): each mutant is loaded and analyzed, and the flow tests
/// are run against it, and the outcome must be success or an <see cref="InvalidDataException"/> whose message starts with the path. Anything else fails with the seed and the mutation.
/// </summary>
public partial class MutationTests
{
    private const int Seed = 27;
    private const int Mutants = 1000;

    // A flow test folder and the solution it runs against, relative to the fixtures.
    private static readonly (string Solution, string Tests)[] Pairs = [("powerops-seed/v2", "flowtests/seed"), ("alm-makers", "flowtests/alm-makers")];

    [GeneratedRegex(@"""(?:[^""\\\r\n]|\\.)*""")]
    private static partial Regex JsonString();

    [GeneratedRegex(@"""[^""\\\r\n]+""\s*:")]
    private static partial Regex JsonKey();

    [Fact]
    public void Mutated_fixtures_load_and_run_or_fail_with_an_InvalidDataException_that_starts_with_the_path()
    {
        using var t = new Readers.TempFolder();
        Copy(Path.Combine(Golden.RepoRoot, "tests", "fixtures"), t.Path);
        var files = Directory.GetFiles(t.Path, "*", SearchOption.AllDirectories).Where(f => Path.GetExtension(f) is ".json" or ".xml" or ".yml" or ".yaml" or ".zip").Order(StringComparer.Ordinal).ToList();
        var roots = SolutionDiscovery.Find(t.Path).Select(s => Directory.Exists(s) || s.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? s : YamlReader.RootFolder(s)).ToList();
        var rng = new Random(Seed);

        for (var i = 0; i < Mutants; i++)
        {
            var file = files[rng.Next(files.Count)];
            var original = File.ReadAllBytes(file);
            var what = $"seed {Seed}, mutant {i}: {Mutate(file, [.. original], rng)} in {Path.GetRelativePath(t.Path, file)}";
            try
            {
                if (roots.FirstOrDefault(r => file.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.Ordinal) || file == r) is { } root)
                {
                    Expect(root, () => AnalysisResult.Run([root], t.Path));
                }

                foreach (var (solution, tests) in Pairs.Select(p => (Path.Combine(t.Path, p.Solution), Path.Combine(t.Path, p.Tests))).Where(p => file.StartsWith(p.Item1, StringComparison.Ordinal) || file.StartsWith(p.Item2, StringComparison.Ordinal)))
                {
                    Expect(solution, () =>
                    {
                        var loaded = SolutionLoader.Load(solution);
                        foreach (var testFile in FlowTestFiles.Find(tests))
                        {
                            Expect(testFile, () => FlowTestRunner.Run(testFile, FlowTestFile.Load(testFile), [loaded]));
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Assert.Fail($"{what}: {e}");
            }
            finally
            {
                File.WriteAllBytes(file, original);
            }
        }
    }

    // Runs the action; an InvalidDataException that starts with the path is an accepted outcome, anything else propagates.
    private static void Expect(string path, Action action)
    {
        try
        {
            action();
        }
        catch (InvalidDataException e) when (e.Message.StartsWith(path, StringComparison.Ordinal))
        {
        }
    }

    // Rewrites the file with one mutation and names it.
    private static string Mutate(string file, byte[] bytes, Random rng)
    {
        var json = file.EndsWith(".json", StringComparison.Ordinal);
        var kind = file.EndsWith(".zip", StringComparison.Ordinal) ? 0 : rng.Next(json ? 5 : 2);
        var text = Encoding.UTF8.GetString(bytes);
        var bom = bytes is [0xEF, 0xBB, 0xBF, ..];
        string Swap(string replacement)
        {
            var strings = JsonString().Matches(text);
            if (strings.Count == 0)
            {
                return text;
            }

            var m = strings[rng.Next(strings.Count)];
            return text[..m.Index] + replacement + text[(m.Index + m.Length)..];
        }

        string? changed = kind switch
        {
            0 => null,
            1 when json => Swap("\"\\ud800\""),
            2 when json => Swap("\"\\u0001\""),
            3 when json => JsonKey().Matches(text) is { Count: > 0 } keys && keys[rng.Next(keys.Count)] is var key ? text.Insert(key.Index, key.Value + "0,") : text,
            4 when json => Swap(new string('[', 300) + new string(']', 300)),
            _ => text.Insert(rng.Next(text.Length + 1), "\u0001"),
        };
        if (changed is null)
        {
            bytes[rng.Next(bytes.Length)] ^= (byte)(1 << rng.Next(8));
            File.WriteAllBytes(file, bytes);
        }
        else
        {
            File.WriteAllText(file, changed, new UTF8Encoding(bom));
        }

        return kind switch { 0 => "a bit flip", 1 => "a lone surrogate", 2 => "a control character", 3 => "a duplicate key", 4 => "300 levels of nesting", _ => "a raw control character" };
    }

    private static void Copy(string from, string to)
    {
        foreach (var f in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, f));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(f, target);
        }
    }
}