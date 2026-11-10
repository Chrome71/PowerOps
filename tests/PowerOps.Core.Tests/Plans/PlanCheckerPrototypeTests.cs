using System.Text;

using PowerOps.Core.Plans;
using PowerOps.Core.Tests.Readers;

namespace PowerOps.Core.Tests.Plans;

public class PlanCheckerPrototypeTests : IDisposable
{
    private const string Approved = "- prototype: approved mockups/prototype.html (option a: fewest steps for Goal line 1) (answered by: person)";
    private const string File = "mockups/prototype.html";

    private readonly TempFolder _t = new();

    public void Dispose() => _t.Dispose();

    private string Folder => Path.Combine(_t.Path, "docs", "plans", "demo");

    private static string Plan(string screens = "- Home\n- Booking form\n", string status = "approved") =>
        $"---\nstatus: {status}\nreview: skipped\nsigned-off: null\nsigned-off-hash: null\nenvironment: null\n---\n# Plan\n\n## Data\n\nx\n\n{screens}\n## Steps\n\n1. y\n";

    private static string States(params string[] values) => string.Concat(values.Select(v => $"  <div data-state=\"{v}\">none</div>\n"));

    private static string Screen(string name, string? states = null) =>
        $"<section data-screen=\"{name}\">\n{states ?? States("empty", "loading", "error", "failed-save")}</section>\n";

    private static string Html(string? screens = null, string head = "<!-- route: canvas -->\n", string body = "<p>Sketch</p>\n") =>
        $"{head}<html><body>\n{body}{screens ?? Screen("Home")}</body></html>\n";

    // An option that passes every check; each letter's text differs.
    private static string Option(char letter) => Html(body: $"<p>Sketch, option {letter}</p>\n");

    private PlanReport Setup(string? html, string? log = Approved, string? plan = null, string? decisions = null, string?[]? options = null)
    {
        _t.Write("docs/plans/demo/plan.md", plan ?? Plan("## Screens\n\n- Home\n- Booking form\n"));
        Directory.CreateDirectory(Path.Combine(Folder, "mockups"));
        System.IO.File.Delete(Path.Combine(Folder, "decisions.md"));
        System.IO.File.Delete(Path.Combine(Folder, File));
        foreach (var old in Directory.GetFiles(Path.Combine(Folder, "mockups"), "option-*.html"))
        {
            System.IO.File.Delete(old);
        }

        // By default the three options an approved sketch is picked from; a null entry writes no file for that letter.
        options ??= [Option('a'), Option('b'), Option('c')];
        for (var i = 0; i < options.Length; i++)
        {
            if (options[i] is { } option)
            {
                _t.Write($"docs/plans/demo/mockups/option-{(char)('a' + i)}.html", option);
            }
        }

        if (log is not null || decisions is not null)
        {
            _t.Write("docs/plans/demo/decisions.md", decisions ?? $"# Decisions\n\n## Log\n\n{log}\n");
        }

        if (html is not null)
        {
            _t.Write("docs/plans/demo/" + File, html);
        }

        return PlanChecker.Check(Path.Combine(Folder, "plan.md"), _t.Path);
    }

    private PrototypeCheck Problems(string? html, string? log = Approved, string? plan = null, string?[]? options = null)
    {
        Setup(html, log, plan, options: options);
        var decisions = System.IO.File.Exists(Path.Combine(Folder, "decisions.md")) ? System.IO.File.ReadAllText(Path.Combine(Folder, "decisions.md")) : null;
        return PlanChecker.PrototypeProblems(Folder, PlanDocument.Parse(System.IO.File.ReadAllText(Path.Combine(Folder, "plan.md"))), decisions);
    }

    private static bool Mentions(PrototypeCheck r, string text) => r.File.Any(p => p.Contains(text, StringComparison.Ordinal));

    [Fact]
    public void A_passing_prototype_and_an_approved_line_have_no_problems_and_no_warnings()
    {
        var r = Problems(Html());

        Assert.Empty(r.Line);
        Assert.Empty(r.File);
        Assert.Empty(r.Warnings);
        Assert.True(r.Approved);
        Assert.Empty(Setup(Html()).Warnings);
    }

    [Theory]
    [InlineData("## Screens\n\nNone.\n")]
    [InlineData("## Screens\n\nNone. Forms are unchanged.\n")]
    [InlineData("## Screens\n\nNone\n")]
    [InlineData("## Screens\n\n\n")]
    [InlineData("")]
    public void Screens_that_name_nothing_need_no_line(string screens)
    {
        var r = Problems(null, log: null, plan: Plan(screens));

        Assert.Empty(r.Line);
        Assert.Empty(r.File);
    }

    [Theory]
    [InlineData("## Screens\n\n| Screen | Purpose |\n|---|---|\n| Home | x |\n")]
    [InlineData("## Screens\n\n- Home\n")]
    [InlineData("## Screens\n\n### Theme\n\nPrimary colour.\n")]
    [InlineData("## Screens\n\nPortal pages: Home.\n")]
    public void Screens_that_name_something_need_a_line(string screens)
    {
        var r = Problems(null, log: null, plan: Plan(screens));

        Assert.Single(r.Line);
        Assert.Contains("no valid prototype line", r.Line[0], StringComparison.Ordinal);
        Assert.False(r.Approved);
    }

    [Theory]
    [InlineData("- prototype: not needed (no screens)")]
    [InlineData("- prototype: not chosen")]
    [InlineData("- prototype: skipped")]
    [InlineData("- prototype: skipped (no time)")]
    [InlineData("- prototype: skipped () (answered by: person)")]
    [InlineData("- prototype: skipped (answered by: person)")]
    [InlineData("- prototype: skipped (no time) (answered by: agent)")]
    [InlineData("- prototype: approved mockups/other.html (answered by: person)")]
    [InlineData("- prototype: approved ../mockups/prototype.html (answered by: person)")]
    [InlineData("- prototype: approved")]
    public void The_old_and_incomplete_forms_count_as_missing(string log)
    {
        var r = Problems(Html(), log);

        Assert.Single(r.Line);
        Assert.False(r.Approved);
    }

    [Theory]
    [InlineData("- prototype: skipped (mock-data slice) (answered by: person)")]
    [InlineData("- prototype: skipped (no time): \"skip it\" (answered by: person)")]
    public void A_persons_skip_with_a_reason_passes_and_leaves_the_file_unchecked_for_sign_off(string log)
    {
        var r = Problems(null, log);

        Assert.Empty(r.Line);
        Assert.False(r.Approved);
        Assert.Empty(r.File);
    }

    [Fact]
    public void The_last_prototype_item_under_the_log_decides_and_a_line_outside_the_log_does_not_count()
    {
        var two = Problems(Html(), "- prototype: not chosen\n- prototype: skipped (later) (answered by: person)");
        Assert.Empty(two.Line);

        var reversed = Problems(Html(), "- prototype: skipped (earlier) (answered by: person)\n- prototype: not chosen");
        Assert.Single(reversed.Line);

        Assert.Single(Problems(null, log: null).Line);

        _t.Write("docs/plans/demo/decisions.md", $"## Round 1\n\n{Approved}\n\n## Log\n\n- other\n");
        var outside = PlanChecker.PrototypeProblems(Folder, PlanDocument.Parse(System.IO.File.ReadAllText(Path.Combine(Folder, "plan.md"))), System.IO.File.ReadAllText(Path.Combine(Folder, "decisions.md")));
        Assert.Single(outside.Line);
    }

    [Fact]
    public void An_approval_without_the_persons_mark_still_passes_but_warns_with_the_line_number()
    {
        var r = Problems(Html(), "- prototype: approved mockups/prototype.html (option a: fewest steps) (answered by: agent)");

        Assert.Empty(r.Line);
        Assert.True(r.Approved);
        var w = Assert.Single(r.Warnings);
        Assert.Contains("decisions.md:5", w, StringComparison.Ordinal);
        Assert.Contains("(answered by: person)", w, StringComparison.Ordinal);

        // Status carries the same warning, and none when the mark is there.
        Assert.Contains(Setup(Html(), "- prototype: approved mockups/prototype.html (option a: fewest steps)").Warnings, x => x.Contains("(answered by: person)", StringComparison.Ordinal));
        Assert.Contains("the person's brief lets it choose", w, StringComparison.Ordinal);
        Assert.DoesNotContain(Setup(Html()).Warnings, x => x.Contains("answered by", StringComparison.Ordinal));
    }

    [Fact]
    public void An_approved_line_without_a_file_is_a_file_problem()
    {
        var r = Problems(null);

        Assert.Empty(r.Line);
        Assert.True(r.Approved);
        Assert.Single(r.File);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void An_approved_sketch_needs_three_options(int count)
    {
        var r = Problems(Html(), options: [.. "abc"[..count].Select(Option)]);

        Assert.Empty(r.Line);
        Assert.Equal($"mockups/: {count} option files (option-*.html) pass the checks and differ; an approved sketch is picked from at least 3", Assert.Single(r.File));
    }

    [Theory]
    [InlineData("- prototype: approved mockups/prototype.html (answered by: person)")]
    [InlineData("- prototype: approved mockups/prototype.html (option z: the best one) (answered by: person)")]
    [InlineData("- prototype: approved mockups/prototype.html (option c: the best one) (answered by: person)")]
    public void An_approved_line_names_an_option_that_passes(string log)
    {
        // Option c fails its checks here, so naming it is no pick either.
        var r = Problems(Html(), log, options: [Option('a'), Option('b'), "<p>nothing</p>", Option('d')]);

        Assert.Empty(r.Line);
        Assert.Contains(r.File, p => p.StartsWith("decisions.md:5: the approved line names no option that passes the checks", StringComparison.Ordinal));
        Assert.DoesNotContain(Problems(Html(), "- prototype: approved mockups/prototype.html (option B: the best one) (answered by: person)").File, p => p.Contains("names no option", StringComparison.Ordinal));
    }

    [Fact]
    public void Options_that_are_the_same_text_count_once_and_a_fourth_option_is_fine()
    {
        var same = Problems(Html(), options: [Option('a'), Option('a'), Option('b')]);
        Assert.Contains("2 option files", Assert.Single(same.File), StringComparison.Ordinal);

        Assert.Empty(Problems(Html(), options: [Option('a'), Option('b'), Option('c'), Option('d')]).File);
    }

    [Fact]
    public void An_option_is_checked_like_the_sketch_under_its_own_path_and_a_failing_one_does_not_count()
    {
        var r = Problems(Html(), options: [Option('a'), Option('b'), "<p>nothing</p>"]);

        Assert.Contains(r.File, p => p.StartsWith("mockups/option-c.html:1:", StringComparison.Ordinal) && p.Contains("route:", StringComparison.Ordinal));
        Assert.DoesNotContain(r.File, p => p.StartsWith(File, StringComparison.Ordinal));
        Assert.Contains(r.File, p => p.StartsWith("mockups/: 2 option files", StringComparison.Ordinal));
    }

    [Fact]
    public void Before_the_pick_the_options_are_checked_and_their_number_is_not()
    {
        // powerops-prototype checks its three options with plan status before any prototype: line exists.
        var none = Problems(null, log: null, options: [Option('a'), Option('b')]);
        Assert.Empty(none.File);

        var failing = Setup(null, log: null, options: [Option('a'), "<script>x</script>"]);
        Assert.Contains(failing.Warnings, w => w.StartsWith("mockups/option-b.html:", StringComparison.Ordinal));
        Assert.DoesNotContain(failing.Warnings, w => w.Contains("option files", StringComparison.Ordinal));
    }

    [Fact]
    public void A_mockups_folder_that_is_a_link_is_a_problem_and_its_options_are_not_read()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "creating a symbolic link needs a privilege on Windows");
        Setup(Html());
        var mockups = Path.Combine(Folder, "mockups");
        var elsewhere = Path.Combine(_t.Path, "elsewhere");
        Directory.Move(mockups, elsewhere);
        Directory.CreateSymbolicLink(mockups, elsewhere);

        var r = PlanChecker.PrototypeProblems(Folder, PlanDocument.Parse(System.IO.File.ReadAllText(Path.Combine(Folder, "plan.md"))), "## Log\n\n" + Approved + "\n");

        Assert.Contains("symbolic link", Assert.Single(r.File), StringComparison.Ordinal);
    }

    [Fact]
    public void The_file_is_checked_whatever_the_log_says_once_it_exists()
    {
        var r = Problems("<p>nothing</p>", "- prototype: skipped (no time) (answered by: person)");

        Assert.Empty(r.Line);
        Assert.NotEmpty(r.File);
    }

    [Fact]
    public void Status_warns_for_draft_and_approved_plans_and_not_for_signed_off_ones()
    {
        var approved = Setup("<p>nothing</p>");
        Assert.Contains(approved.Warnings, w => w.Contains("data-screen", StringComparison.Ordinal));

        var missing = Setup(null, log: null);
        Assert.Contains(missing.Warnings, w => w.Contains("no valid prototype line", StringComparison.Ordinal));

        var draft = Setup(null, log: null, plan: Plan("## Screens\n\n- Home\n", "draft"));
        Assert.Contains(draft.Warnings, w => w.Contains("no valid prototype line", StringComparison.Ordinal));

        foreach (var status in new[] { "signed-off", "implementing", "done" })
        {
            var r = Setup("<p>nothing</p>", log: null, plan: Plan("## Screens\n\n- Home\n", status));
            Assert.DoesNotContain(r.Warnings, w => w.Contains("prototype", StringComparison.OrdinalIgnoreCase));
        }
    }

    // Each rule: a firing case and (above or below) a quiet case. The problem names the rule and the line, never the text.
    [Theory]
    [InlineData("<script>x</script>", "tag <script>")]
    [InlineData("<LINK rel=\"stylesheet\" href=\"#a\">", "tag <link>")]
    [InlineData("<meta name=\"a\">", "tag <meta>")]
    [InlineData("<base href=\"#\">", "tag <base>")]
    [InlineData("<iframe></iframe>", "tag <iframe>")]
    [InlineData("<frame>", "tag <frame>")]
    [InlineData("<object></object>", "tag <object>")]
    [InlineData("<embed>", "tag <embed>")]
    [InlineData("<img alt=\"a\">", "tag <img>")]
    [InlineData("<picture></picture>", "tag <picture>")]
    [InlineData("<svg></svg>", "tag <svg>")]
    [InlineData("<form></form>", "tag <form>")]
    [InlineData("<video></video>", "tag <video>")]
    [InlineData("<audio></audio>", "tag <audio>")]
    [InlineData("<source>", "tag <source>")]
    [InlineData("<\nScript\n>", "tag <script>")]
    public void A_banned_tag_fires_on_its_line(string tag, string rule)
    {
        var r = Problems(Html(body: $"<p>Sketch</p>\n{tag}\n"));

        var p = Assert.Single(r.File, x => x.Contains(rule, StringComparison.Ordinal));
        Assert.StartsWith($"{File}:4:", p, StringComparison.Ordinal);
    }

    // Decision 60: one meta, name="viewport" with a content value and nothing else, so a phone shows the narrow layout.
    [Theory]
    [InlineData("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")]
    [InlineData("<meta content=\"width=device-width, initial-scale=1\" name=\"viewport\">")]
    [InlineData("<META NAME=\"Viewport\" CONTENT=\"width=device-width\">")]
    [InlineData("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />")]
    [InlineData("<meta name=\"viewport\"/content=\"x\">")]
    [InlineData("<meta name=\"viewport\" content=\"\">")]
    [InlineData("<meta name=viewport content=width=device-width,initial-scale=1>")]
    [InlineData("<meta name=\"&#118;iewport\" content=\"x\">")]
    public void The_viewport_meta_is_quiet(string tag)
    {
        var r = Problems(Html(body: $"<p>Sketch</p>\n{tag}\n"));

        Assert.Empty(r.File);
    }

    [Theory]
    [InlineData("<meta http-equiv=\"refresh\" content=\"0\">")]
    [InlineData("<meta charset=\"utf-8\">")]
    [InlineData("<meta http-equiv=\"refresh\" name=\"viewport\" content=\"0\">")]
    [InlineData("<meta name=\"viewport\" http-equiv=\"refresh\" content=\"0\">")]
    [InlineData("<meta name=\"viewport\" content=\"0\" http-equiv=\"refresh\">")]
    [InlineData("<meta name=\"viewport\" content=\"0\" id=\"a\">")]
    [InlineData("<meta name=\"viewport\">")]
    [InlineData("<meta name=\"viewport\" content>")]
    [InlineData("<meta name=\"description\" content=\"x\">")]
    [InlineData("<meta name=\"viewport\" name=\"viewport\" content=\"x\">")]
    [InlineData("<meta name=\"viewport\" content=\"x\" content=\"y\">")]
    [InlineData("<meta name=\"viewport\" content=\"x\" \">")]
    [InlineData("<meta name=\"viewport\" content=\"x\" refresh>")]
    [InlineData("<meta name=\"viewport\" content=\"x\" =\"y\">")]
    [InlineData("<meta name=\" viewport\" content=\"x\">")]
    [InlineData("<meta name=viewport/ content=x>")]
    [InlineData("<meta name=viewport content=width=device-width, initial-scale=1>")]
    [InlineData("<meta name=\"view&shy;port\" content=\"x\">")]
    [InlineData("<meta n&#97;me=\"viewport\" content=\"x\">")]
    [InlineData("<meta name=\"viewport\" content=\"x\">")]
    public void Any_other_meta_fires(string tag)
    {
        var r = Problems(Html(body: $"<p>Sketch</p>\n{tag}\n"));

        var p = Assert.Single(r.File, x => x.Contains("tag <meta> is not allowed", StringComparison.Ordinal));
        Assert.StartsWith($"{File}:4:", p, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<div src=\"a\">", "attribute src")]
    [InlineData("<div srcset=\"a\">", "attribute srcset")]
    [InlineData("<div ACTION=\"a\">", "attribute action")]
    [InlineData("<div onclick=\"a\">", "event-handler attribute")]
    [InlineData("<div onmouseover=a>", "event-handler attribute")]
    [InlineData("<a href=\"https://elsewhere\">x</a>", "href must start with #")]
    [InlineData("<a href=\"javascript:void(0)\">x</a>", "href must start with #")]
    [InlineData("<a href=\"//host/x\">x</a>", "href must start with #")]
    [InlineData("<a href>x</a>", "href must start with #")]
    [InlineData("<style>a{background:URL(x)}</style>", "CSS url(")]
    [InlineData("<style>@import 'a';</style>", "CSS @import")]
    [InlineData("<style>@font-face{font-family:a}</style>", "CSS @font-face")]
    [InlineData("<style>a{x:image-set(b)}</style>", "CSS image-set(")]
    [InlineData("<div style=\"width:expression(1)\">", "CSS expression(")]
    [InlineData("<div style=\"background:url(x)\">", "CSS url(")]
    public void A_banned_attribute_href_or_css_fires_on_its_line(string markup, string rule)
    {
        var r = Problems(Html(body: $"<p>Sketch</p>\n{markup}\n"));

        var p = Assert.Single(r.File, x => x.Contains(rule, StringComparison.Ordinal));
        Assert.StartsWith($"{File}:4:", p, StringComparison.Ordinal);
    }

    [Fact]
    public void Quiet_cases_for_the_tag_attribute_and_css_rules()
    {
        var body = "<p>Sketch</p>\n<a href=\"#home\">Home</a>\n<div style=\"color:#123456\" title=\"src and onclick and url(\">x</div>\n<style>a{color:red}</style>\n<p>the link tag, an onboarding page and Url(s)</p>\n<form-like>x</form-like>\n";

        Assert.Empty(Problems(Html(body: body)).File);
    }

    [Fact]
    public void Problems_never_echo_the_matched_text()
    {
        var r = Problems(Html(body: "<p>Sketch</p>\n<a href=\"https://secret.example/x\" onclick=\"leak()\">x</a>\n<style>a{background:url(secret-pic)}</style>\n"));

        Assert.NotEmpty(r.File);
        Assert.DoesNotContain(r.File, p => p.Contains("secret", StringComparison.Ordinal) || p.Contains("leak", StringComparison.Ordinal));
    }

    [Fact]
    public void Comments_are_removed_first_and_keep_line_numbers()
    {
        var html = Html(body: "<p>Sketch</p>\n<!-- <script>x</script>\n url(x) -->\n<script>y</script>\n");

        var r = Problems(html);

        var p = Assert.Single(r.File);
        Assert.StartsWith($"{File}:6:", p, StringComparison.Ordinal);
        Assert.Contains("tag <script>", p, StringComparison.Ordinal);
    }

    [Fact]
    public void Sketch_in_body_text_is_required_and_a_comment_or_style_does_not_count()
    {
        Assert.Empty(Problems(Html(body: "<h1>Sketch: Home</h1>\n")).File);

        foreach (var body in new[] { "<!-- Sketch -->\n", "<style>.Sketch{color:red}</style>\n", "<p>Sketching only</p>\n", "<p title=\"Sketch\">x</p>\n" })
        {
            var r = Problems(Html(body: body));
            Assert.True(Mentions(r, "\"Sketch\""), body);
        }
    }

    [Fact]
    public void The_route_line_is_read_from_the_first_comment_only()
    {
        Assert.Empty(Problems(Html(head: "<!--\nroute: canvas\ncolours: x\n-->\n")).File);

        foreach (var head in new[] { "", "<!-- notes -->\n<!-- route: canvas -->\n", "<!-- the route: canvas -->\n" })
        {
            Assert.True(Mentions(Problems(Html(head: head)), "route:"), head);
        }
    }

    [Fact]
    public void One_to_eight_data_screen_attributes_are_needed()
    {
        Assert.True(Mentions(Problems(Html(screens: "<p>x</p>\n")), "data-screen"));

        var nine = string.Concat(Enumerable.Repeat(Screen("Home"), 9));
        var r = Problems(Html(screens: nine));
        Assert.Single(r.File, p => p.Contains("9 data-screen", StringComparison.Ordinal));

        Assert.Empty(Problems(Html(screens: string.Concat(Enumerable.Repeat(Screen("Home"), 8)))).File);
    }

    [Fact]
    public void A_screen_name_must_be_whole_words_of_the_screens_text_in_any_case()
    {
        Assert.Empty(Problems(Html(screens: Screen("BOOKING   form"))).File);
        Assert.Empty(Problems(Html(screens: Screen("Home") + Screen("booking form"))).File);

        foreach (var name in new[] { "Hom", "Dashboard", "form booking", "" })
        {
            var r = Problems(Html(screens: Screen(name)));
            var p = Assert.Single(r.File);
            Assert.Contains("data-screen value", p, StringComparison.Ordinal);
            Assert.DoesNotContain(name.Length > 0 ? name : "\0", p, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_four_states_are_needed_between_one_screen_and_the_next_and_none_text_is_fine()
    {
        Assert.Empty(Problems(Html(screens: Screen("Home") + Screen("Booking form"))).File);

        var r = Problems(Html(screens: Screen("Home", States("empty", "loading", "error")) + Screen("Booking form")));

        var p = Assert.Single(r.File);
        Assert.StartsWith($"{File}:4:", p, StringComparison.Ordinal);
        Assert.Contains("failed-save", p, StringComparison.Ordinal);

        // A state placed after the next screen does not count for the first.
        var late = Problems(Html(screens: Screen("Home", States("empty", "loading", "error")) + Screen("Booking form", States("empty", "loading", "error", "failed-save", "failed-save"))));
        Assert.Single(late.File);
    }

    [Fact]
    public void The_file_is_at_most_81920_bytes()
    {
        static string Padded(int extra) => Html(head: $"<!-- route: canvas {new string('x', extra)}-->\n");
        var room = 81_920 - Encoding.UTF8.GetByteCount(Padded(0));

        Assert.Empty(Problems(Padded(room)).File);
        Assert.Single(Problems(Padded(room + 1)).File, p => p.Contains("81920", StringComparison.Ordinal));
    }

    [Fact]
    public void A_link_a_binary_file_or_one_over_1_MB_is_a_problem_not_a_pass()
    {
        Setup(Html());
        var file = Path.Combine(Folder, "mockups", "prototype.html");

        System.IO.File.WriteAllBytes(file, [.. Encoding.UTF8.GetBytes(Html()), 0, 0]);
        Assert.Contains("binary", Assert.Single(Check()), StringComparison.Ordinal);

        System.IO.File.WriteAllText(file, Html() + new string(' ', (int)PlanChecker.MaxFileBytes));
        Assert.Contains("1 MB", Assert.Single(Check()), StringComparison.Ordinal);

        if (!OperatingSystem.IsWindows())
        {
            var target = Path.Combine(_t.Path, "elsewhere.html");
            System.IO.File.WriteAllText(target, Html());
            System.IO.File.Delete(file);
            System.IO.File.CreateSymbolicLink(file, target);
            Assert.Contains("symbolic link", Assert.Single(Check()), StringComparison.Ordinal);
        }

        List<string> Check() => [.. PlanChecker.PrototypeProblems(Folder, PlanDocument.Parse(System.IO.File.ReadAllText(Path.Combine(Folder, "plan.md"))), "## Log\n\n" + Approved + "\n").File];
    }

    [Fact]
    public void An_email_in_fake_data_fails_the_tenant_scan_not_the_prototype_rules()
    {
        var r = Setup(Html(body: "<p>Sketch</p>\n<p>ana@contoso.example</p>\n"));

        Assert.Contains(r.Violations, v => v.Contains("looks like a email", StringComparison.Ordinal));
        Assert.DoesNotContain(r.Warnings, w => w.Contains("prototype.html", StringComparison.Ordinal));
    }

    [Fact]
    public void Plans_signed_off_with_the_old_forms_are_not_rechecked_by_status()
    {
        var r = Setup(null, "- prototype: not chosen", Plan("## Screens\n\n- Home\n", "implementing"));

        Assert.DoesNotContain(r.Warnings, w => w.Contains("prototype", StringComparison.OrdinalIgnoreCase));
    }
}