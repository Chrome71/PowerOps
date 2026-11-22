using System.Security.Cryptography;
using System.Text;

using PowerOps.Core.Plans;
using PowerOps.Core.Tests.Readers;
using PowerOps.Tests.Shared;

namespace PowerOps.Core.Tests.Plans;

public class PlanCheckerPrototypeTests : IDisposable
{
    private const string Approved = "- prototype: approved mockups/prototype.html (option a: fewest steps for Goal line 1) (answered by: person)";

    // M124 row 3: an approval the person did not mark keeps the old gate (three options, a named passing option, a current verdict).
    private const string Unmarked = "- prototype: approved mockups/prototype.html (option a: fewest steps for Goal line 1)";

    // A person's pass of one sketch with no option named (the `main` mode).
    private const string Pass = "- prototype: approved mockups/prototype.html (main screen: fewest steps for Goal line 1) (answered by: person)";
    private const string NotAtBaseline = "the verdict is not at baseline";
    private const string Zeros = "0000000000000000000000000000000000000000000000000000000000000000";
    private const string File = "mockups/prototype.html";
    private const string NoVerdict = "no current judge verdict";

    // The map names the plan's screens; the judge file is bound to the sketch Setup writes ({hash} is filled in).
    private const string DefaultMap = "# Map\n\n- Home: a container with a header\n- Booking form: a form\n";
    private const string FreshJudge = "judge: fresh context\nsketch-hash: {hash}\n\n## Round 1\n\nverdict: at baseline\n";

    private static readonly string[] Four = ["empty", "loading", "error", "failed-save"];

    private readonly TempFolder _t = new();

    public void Dispose() => _t.Dispose();

    private string Folder => Path.Combine(_t.Path, "docs", "plans", "demo");

    private static string Plan(string screens = "- Home\n- Booking form\n", string status = "approved") =>
        $"---\nstatus: {status}\nreview: skipped\nsigned-off: null\nsigned-off-hash: null\nenvironment: null\n---\n# Plan\n\n## Data\n\nx\n\n{screens}\n## Steps\n\n1. y\n";

    /// <summary>SHA-256 hex (lowercase) of the text with CRLF made LF: the sketch-hash a judge writes.</summary>
    private static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n"))));

    private static string States(string screen, params string[] values) => string.Concat(values.Select(v => $"<div data-state=\"{v}\" data-for=\"{screen}\">none</div>\n"));

    private static string Frame(string name, string cls = "frame desktop", string inner = "  <h1 data-lead>Title</h1>\n") =>
        $"<section class=\"{cls}\" data-screen=\"{name}\">\n{inner}</section>\n";

    // A frame and its four state tiles.
    private static string Screen(string name, string? states = null) => Frame(name) + (states ?? States(name, Four));

    private static string Html(string? screens = null, string head = "<!-- route: canvas -->\n", string body = "<p>Sketch</p>\n") =>
        $"{head}<html><body>\n{body}{screens ?? Screen("Home")}</body></html>\n";

    // An option that passes every check: one lead frame, no states; each letter's text differs.
    private static string Option(char letter) => Html(body: $"<p>Sketch, option {letter}</p>\n", screens: Frame("Home"));

    private PlanReport Setup(string? html, string? log = Approved, string? plan = null, string? decisions = null, string?[]? options = null, string? map = DefaultMap, string? judge = FreshJudge)
    {
        _t.Write("docs/plans/demo/plan.md", plan ?? Plan("## Screens\n\n- Home\n- Booking form\n"));
        Directory.CreateDirectory(Path.Combine(Folder, "mockups"));
        foreach (var old in new[] { "decisions.md", File, "mockups/prototype.map.md", "mockups/prototype.judge.md" })
        {
            System.IO.File.Delete(Path.Combine(Folder, old));
        }

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
            if (map is not null)
            {
                _t.Write("docs/plans/demo/mockups/prototype.map.md", map);
            }

            if (judge is not null)
            {
                _t.Write("docs/plans/demo/mockups/prototype.judge.md", judge.Replace("{hash}", Hash(html), StringComparison.Ordinal));
            }
        }

        return PlanChecker.Check(Path.Combine(Folder, "plan.md"), _t.Path);
    }

    private PrototypeCheck Problems(string? html, string? log = Approved, string? plan = null, string?[]? options = null, string? map = DefaultMap, string? judge = FreshJudge)
    {
        Setup(html, log, plan, options: options, map: map, judge: judge);
        return Recheck();
    }

    // The prototype gate on the folder as it is now.
    private PrototypeCheck Recheck()
    {
        var decisions = System.IO.File.Exists(Path.Combine(Folder, "decisions.md")) ? System.IO.File.ReadAllText(Path.Combine(Folder, "decisions.md")) : null;
        return PlanChecker.PrototypeProblems(Folder, PlanDocument.Parse(System.IO.File.ReadAllText(Path.Combine(Folder, "plan.md"))), decisions);
    }

    private SignOffResult SignOff() => PlanSignOff.Run(Path.Combine(Folder, "plan.md"), _t.Path, new DateOnly(2026, 11, 14));

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
        Assert.Single(Recheck().Line);
    }

    [Fact]
    public void A_pick_logged_in_the_rounds_before_the_approval_line_signs_off_and_quiets_status_while_draft()
    {
        // M93: the pick is logged while the plan is a draft, so its line comes before the approval line.
        var decisions = $"# Decisions\n\n## Log\n\n{Approved}\n- approved (answered by: person)\n";

        var draft = Setup(Html(), plan: Plan("## Screens\n\n- Home\n- Booking form\n", "draft"), decisions: decisions);
        Assert.DoesNotContain(draft.Warnings, w => w.Contains("prototype", StringComparison.OrdinalIgnoreCase));

        Setup(Html(), decisions: decisions);
        var r = SignOff();

        Assert.Equal(0, r.ExitCode);
        Assert.Empty(r.Err);
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
        var r = Problems(Html(), Unmarked, options: [.. "abc"[..count].Select(Option)]);

        Assert.Empty(r.Line);
        Assert.Equal($"mockups/: {count} option files (option-*.html) pass the checks and differ; an approved sketch is picked from at least 3", Assert.Single(r.File));
    }

    [Theory]
    [InlineData("- prototype: approved mockups/prototype.html")]
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
        var same = Problems(Html(), Unmarked, options: [Option('a'), Option('a'), Option('b')]);
        Assert.Contains("2 option files", Assert.Single(same.File), StringComparison.Ordinal);

        Assert.Empty(Problems(Html(), Unmarked, options: [Option('a'), Option('b'), Option('c'), Option('d')]).File);
    }

    [Fact]
    public void An_option_is_checked_like_the_sketch_under_its_own_path_and_a_failing_one_does_not_count()
    {
        var r = Problems(Html(), Unmarked, options: [Option('a'), Option('b'), "<p>nothing</p>"]);

        Assert.Contains(r.File, p => p.StartsWith("mockups/option-c.html:1:", StringComparison.Ordinal) && p.Contains("route:", StringComparison.Ordinal));
        Assert.DoesNotContain(r.File, p => p.StartsWith(File, StringComparison.Ordinal));
        Assert.Contains(r.File, p => p.StartsWith("mockups/: 2 option files", StringComparison.Ordinal));
    }

    [Fact]
    public void Before_the_pick_the_options_are_checked_and_their_number_is_not()
    {
        // powerops-prototype checks its three options with plan status before any prototype: line exists; they need no map and no judge file.
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
    [InlineData("<meta name=\"viewport\"\u00A0content=\"x\">")]
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
        var body = "<p>Sketch</p>\n<a href=\"#home\">Home</a>\n<div style=\"color:var(--brand)\" title=\"src and onclick and url\">x</div>\n<style>a{color:red}</style>\n<p>the link tag, an onboarding page and Url(s)</p>\n<form-like>x</form-like>\n";

        Assert.Empty(Problems(Html(body: body)).File);
    }

    [Fact]
    public void Problems_never_echo_the_matched_text()
    {
        var r = Problems(Html(body: "<p>Sketch</p>\n<a href=\"https://secret.example/x\" onclick=\"leak()\">x</a>\n<style>a{background:url(secret-pic)}</style>\n<svg><use xlink:href=\"secret.svg#i\"/><rect fill=\"url(#secret)\"/></svg>\n<p style=\"color:#5ec2e7\">x</p>\n"));

        Assert.NotEmpty(r.File);
        Assert.DoesNotContain(r.File, p => p.Contains("secret", StringComparison.Ordinal) || p.Contains("leak", StringComparison.Ordinal) || p.Contains("5ec2e7", StringComparison.Ordinal));
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
    public void One_to_eight_distinct_screen_names_are_needed_and_a_name_may_repeat_across_frames()
    {
        Assert.True(Mentions(Problems(Html(screens: "<p>x</p>\n")), "data-screen"));

        // Row 13 (d): the limit counts distinct names, so one screen drawn in nine frames is one name.
        string[] names = ["One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine"];
        var plan = Plan("## Screens\n\n- Home\n" + string.Concat(names.Select(n => $"- {n}\n")));
        var map = string.Join(", ", names);
        var nine = Problems(Html(screens: string.Concat(names.Select(n => Screen(n)))), plan: plan, map: map);
        Assert.Single(nine.File, p => p.Contains("9 distinct data-screen names", StringComparison.Ordinal));

        Assert.Empty(Problems(Html(screens: string.Concat(names[..8].Select(n => Screen(n)))), plan: plan, map: map).File);
        Assert.Empty(Problems(Html(screens: string.Concat(Enumerable.Repeat(Frame("Home"), 9)) + States("Home", Four))).File);
    }

    [Fact]
    public void A_screen_name_must_be_whole_words_of_the_screens_text_in_any_case()
    {
        Assert.Empty(Problems(Html(screens: Screen("BOOKING   form"))).File);
        Assert.Empty(Problems(Html(screens: Screen("Home") + Screen("booking form"))).File);

        foreach (var name in new[] { "Hom", "Dashboard", "form booking", "" })
        {
            // The map names the screen, so only the Screens rule speaks.
            var r = Problems(Html(screens: Screen(name)), map: DefaultMap + name + "\n");
            var p = Assert.Single(r.File);
            Assert.Contains("data-screen value is not a phrase of the Screens text", p, StringComparison.Ordinal);
            Assert.DoesNotContain(name.Length > 0 ? name : "\0", p, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Each_screen_name_needs_the_four_states_by_data_for_and_none_text_is_fine()
    {
        // Row 13 (f). The states may sit anywhere in the file; one set serves every frame of a name.
        Assert.Empty(Problems(Html(screens: Screen("Home") + Screen("Booking form"))).File);
        Assert.Empty(Problems(Html(screens: States("Home", Four) + Frame("Home") + Frame("Home", "frame phone"))).File);

        // Extra states (confirm, success) are allowed.
        Assert.Empty(Problems(Html(screens: Screen("Home") + States("Home", "confirm", "success"))).File);

        var r = Problems(Html(screens: Screen("Home", States("Home", "empty", "loading", "error")) + Screen("Booking form")));
        var p = Assert.Single(r.File);
        Assert.StartsWith($"{File}:4:", p, StringComparison.Ordinal);
        Assert.Contains("data-screen has no data-state failed-save with data-for naming it", p, StringComparison.Ordinal);

        // A state whose data-for names the other screen does not count for the first.
        var other = Problems(Html(screens: Screen("Home", States("Home", "empty", "loading", "error")) + Screen("Booking form", States("Booking form", "empty", "loading", "error", "failed-save", "failed-save"))));
        Assert.Contains("failed-save", Assert.Single(other.File), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<div data-state=\"confirm\">none</div>")]
    [InlineData("<div data-state=\"confirm\" data-for=\"\">none</div>")]
    [InlineData("<div data-state=\"confirm\" data-for=\"Booking form\">none</div>")]
    public void A_state_without_data_for_naming_a_screen_of_the_file_fires_on_its_line(string state)
    {
        // Booking form is a screen of the plan but not of this file.
        var r = Problems(Html(body: $"<p>Sketch</p>\n{state}\n"));

        var p = Assert.Single(r.File);
        Assert.Equal($"{File}:4: data-state has no data-for naming a data-screen of the file", p);
        Assert.Empty(Problems(Html(body: "<p>Sketch</p>\n<div data-state=\"confirm\" data-for=\"home\">none</div>\n")).File);
    }

    [Theory]
    [InlineData("frame desktop")]
    [InlineData("frame phone")]
    [InlineData("browser frame")]
    [InlineData("frame report wide")]
    public void A_frame_is_the_data_screen_element_with_the_frame_and_a_size_class(string cls)
    {
        Assert.Empty(Problems(Html(screens: Frame("Home", cls) + States("Home", Four))).File);
    }

    [Fact]
    public void A_frame_may_be_the_parent_of_the_data_screen_element_and_void_or_self_closing_elements_never_become_it()
    {
        var child = "<div class=\"frame desktop\">\n  <br><input><svg class=\"i\"><use href=\"#i\"/></svg><path d=\"M0 0\"/>\n  <main data-screen=\"Home\"><h1 data-lead>Home</h1></main>\n</div>\n";
        Assert.Empty(Problems(Html(screens: child + States("Home", Four))).File);
    }

    [Theory]
    [InlineData("<section data-screen=\"Home\"><h1 data-lead>x</h1></section>")]
    [InlineData("<section class=\"frame\" data-screen=\"Home\"><h1 data-lead>x</h1></section>")]
    [InlineData("<section class=\"desktop\" data-screen=\"Home\"><h1 data-lead>x</h1></section>")]
    [InlineData("<section class=\"frames desktop\" data-screen=\"Home\"><h1 data-lead>x</h1></section>")]
    [InlineData("<div class=\"wrap\"><section data-screen=\"Home\"><h1 data-lead>x</h1></section></div>")]
    public void A_data_screen_with_no_frame_fires_on_its_line(string markup)
    {
        var r = Problems(Html(screens: $"{markup}\n{States("Home", Four)}"));

        var p = Assert.Single(r.File);
        Assert.Equal($"{File}:4: data-screen has no frame: give it, or its parent element, the class frame and one of desktop, phone, browser or report", p);
    }

    [Theory]
    [InlineData("<p>x</p>\n", 0)]
    [InlineData("<h1 data-lead>x</h1>\n<p data-lead>y</p>\n", 2)]
    public void A_frame_holds_exactly_one_data_lead(string inner, int count)
    {
        var r = Problems(Html(screens: Frame("Home", inner: inner) + States("Home", Four)));

        Assert.Equal($"{File}:4: the frame holds {count} elements with data-lead; it needs exactly 1", Assert.Single(r.File));

        // A data-lead outside the frame does not count for it.
        Assert.Single(Problems(Html(body: "<p data-lead>Sketch</p>\n", screens: Frame("Home", inner: inner) + States("Home", Four))).File);
    }

    [Fact]
    public void At_most_one_filled_primary_button_in_each_frame()
    {
        // Row 13 (k): btn and primary together; the second one fires on its line.
        var two = Frame("Home", inner: "  <h1 data-lead>x</h1>\n  <button class=\"btn primary\">Save</button>\n  <a class=\"primary fr btn\" href=\"#x\">Send</a>\n");
        var r = Problems(Html(screens: two + States("Home", Four)));
        Assert.Equal($"{File}:7: more than one filled primary button (class btn primary) in the frame", Assert.Single(r.File));

        var quiet = Frame("Home", inner: "  <h1 data-lead>x</h1>\n  <button class=\"btn primary\">Save</button>\n  <button class=\"btn-primary\">A</button><button class=\"primary\">B</button><button class=\"btn subtle\">C</button>\n")
            + Frame("Home", "frame phone", "  <h1 data-lead>x</h1>\n  <button class=\"btn primary\">Save</button>\n");
        Assert.Empty(Problems(Html(body: "<p>Sketch</p>\n<button class=\"btn primary\">Outside</button>\n", screens: quiet + States("Home", Four))).File);
    }

    [Fact]
    public void Inline_svg_is_allowed_with_symbols_use_presentation_hex_and_text()
    {
        var svg = "<svg viewBox=\"0 0 10 10\" class=\"i\"><symbol id=\"i-add\"><path d=\"M0 0h4\"/></symbol><use href=\"#i-add\"/><use xlink:href=\"#i-add\"></use><g><rect fill=\"#0F6CBD\" stroke=\"#fff\" width=\"4\" height=\"4\"/><circle r=\"2\"/><line x1=\"0\" x2=\"1\"/><text x=\"1\">12</text></g></svg>\n";

        Assert.Empty(Problems(Html(body: $"<p>Sketch</p>\n{svg}<a href=\"#x\">after</a>\n")).File);
    }

    [Theory]
    [InlineData("foreignObject", "<foreignObject></foreignObject>")]
    [InlineData("image", "<image width=\"1\"/>")]
    [InlineData("a", "<a href=\"#x\"><text>x</text></a>")]
    [InlineData("style", "<style>rect{fill:red}</style>")]
    [InlineData("animate", "<animate attributeName=\"x\"/>")]
    [InlineData("set", "<set attributeName=\"x\"/>")]
    [InlineData("animateMotion", "<animateMotion dur=\"1s\"></animateMotion>")]
    [InlineData("animateTransform", "<g><animateTransform type=\"scale\"/></g>")]
    public void A_banned_element_inside_svg_fires_on_its_line(string name, string markup)
    {
        var r = Problems(Html(body: $"<p>Sketch</p>\n<svg>\n{markup}\n</svg>\n"));

        var p = Assert.Single(r.File);
        Assert.Equal($"{File}:5: tag <{name}> is not allowed inside svg", p);
    }

    [Fact]
    public void A_script_inside_svg_fires_once_as_a_banned_tag()
    {
        var r = Problems(Html(body: "<p>Sketch</p>\n<svg>\n<script>x</script>\n</svg>\n"));

        Assert.Equal($"{File}:5: tag <script> is not allowed", Assert.Single(r.File));
    }

    [Theory]
    [InlineData("<svg><use xlink:href=\"icons.svg#i\"/></svg>", "xlink:href must start with #")]
    [InlineData("<svg><rect fill=\"url(#g)\"/></svg>", "attribute value url( is not allowed")]
    [InlineData("<div title=\"see url(x)\">x</div>", "attribute value url( is not allowed")]
    [InlineData("<div data-x=\"URL&#40;x)\">x</div>", "attribute value url( is not allowed")]
    public void Xlink_href_and_url_in_any_attribute_value_fire_on_their_line(string markup, string rule)
    {
        var r = Problems(Html(body: $"<p>Sketch</p>\n{markup}\n"));

        Assert.Equal($"{File}:4: {rule}", Assert.Single(r.File));
    }

    // Decisions Q3: a light tag stack. In each case the a after the first line is outside any svg, and the a on line 5 is inside one.
    [Theory]
    [InlineData("<!-- <svg> -->")]
    [InlineData("<style>/* paste at the top of the one <style> element: <svg> */ a{color:red}</style>")]
    [InlineData("<svg viewBox=\"0 0 1 1\"/>")]
    [InlineData("<svg><use href=\"#i\"/><path d=\"M0 0\"/><circle r=\"1\"/></svg>")]
    [InlineData("<svg><g><path d=\"M0 0\"></svg>")]
    [InlineData("<div><svg><g></div>")]
    [InlineData("<br><hr><input></p></span>")]
    public void The_tag_stack_survives_comments_style_text_self_closing_void_and_stray_or_implied_end_tags(string first)
    {
        var r = Problems(Html(body: $"<p>Sketch</p>\n{first}\n<svg></p></span><a href=\"#x\">in</a></svg>\n<a href=\"#y\">out</a>\n"));

        Assert.Equal($"{File}:5: tag <a> is not allowed inside svg", Assert.Single(r.File));
    }

    [Fact]
    public void The_prototype_is_at_most_163840_bytes_and_an_option_at_most_61440()
    {
        static string Padded(int extra, string? screens = null) => Html(head: $"<!-- route: canvas {new string('x', extra)}-->\n", screens: screens);
        var room = PlanChecker.MaxPrototypeBytes - Encoding.UTF8.GetByteCount(Padded(0));
        Assert.Equal(163_840, PlanChecker.MaxPrototypeBytes);

        Assert.Empty(Problems(Padded(room)).File);
        Assert.Equal($"{File}:1: larger than 163840 bytes", Assert.Single(Problems(Padded(room + 1)).File));

        var optionRoom = 61_440 - Encoding.UTF8.GetByteCount(Padded(0, Frame("Home")));
        Assert.Empty(Problems(Html(), options: [Option('a'), Option('b'), Padded(optionRoom, Frame("Home"))]).File);
        var big = Problems(Html(), options: [Option('a'), Option('b'), Padded(optionRoom + 1, Frame("Home"))]);
        Assert.Contains("mockups/option-c.html:1: larger than 61440 bytes", big.File);
    }

    [Fact]
    public void An_option_holds_exactly_one_data_screen_no_state_and_a_frame()
    {
        // Row 14: one lead frame per option.
        Assert.Empty(Problems(null, log: null, options: [Option('a')]).File);

        var two = Problems(null, log: null, options: [Html(screens: Frame("Home") + Frame("Booking form"))]);
        Assert.Equal("mockups/option-a.html:1: 2 data-screen attributes; an option holds exactly 1", Assert.Single(two.File));

        var none = Problems(null, log: null, options: [Html(screens: "<p>x</p>\n")]);
        Assert.Equal("mockups/option-a.html:1: 0 data-screen attributes; an option holds exactly 1", Assert.Single(none.File));

        var state = Problems(null, log: null, options: [Html(screens: Frame("Home") + States("Home", "empty"))]);
        Assert.Equal("mockups/option-a.html:7: data-state is not allowed in an option", Assert.Single(state.File));

        var unframed = Problems(null, log: null, options: [Html(screens: "<section data-screen=\"Home\"><h1 data-lead>x</h1></section>\n")]);
        Assert.Contains("data-screen has no frame", Assert.Single(unframed.File), StringComparison.Ordinal);

        var leadless = Problems(null, log: null, options: [Html(screens: Frame("Home", inner: "<p>x</p>\n"))]);
        Assert.Contains("0 elements with data-lead", Assert.Single(leadless.File), StringComparison.Ordinal);
    }

    [Fact]
    public void The_map_beside_the_prototype_names_every_screen()
    {
        // Row 13 (h): any case, as a phrase; nothing else in it is read.
        Assert.Empty(Problems(Html(screens: Screen("Home") + Screen("Booking form")), map: "home: Container\nBOOKING\nFORM: a Form <script>\n").File);

        Assert.Equal("mockups/prototype.map.md: not found; it names each screen's parts beside the sketch", Assert.Single(Problems(Html(), map: null).File));
        Assert.Equal("mockups/prototype.map.md: is empty", Assert.Single(Problems(Html(), map: " \n\n").File));

        var missing = Problems(Html(screens: Screen("Home") + Screen("Booking form")), map: "- Home: Container\n- Booking: a form\n");
        Assert.Equal($"{File}:11: data-screen value is not a phrase of mockups/prototype.map.md", Assert.Single(missing.File));

        Setup(Html());
        System.IO.File.WriteAllBytes(Path.Combine(Folder, "mockups", "prototype.map.md"), [.. Encoding.UTF8.GetBytes(DefaultMap), 0, 0]);
        Assert.Contains("prototype.map.md: a binary file", Assert.Single(Recheck().File), StringComparison.Ordinal);
    }

    [Fact]
    public void A_map_naming_learn_pages_by_path_leaves_the_tenant_scan_quiet()
    {
        var byPath = DefaultMap + "- Icon: modern Icon control, GA, learn.microsoft.com/power-apps/maker/canvas-apps/controls/modern-controls/modern-control-icon\n";
        var quiet = Setup(Html(), map: byPath);
        Assert.Empty(quiet.Violations);
        Assert.DoesNotContain(quiet.Warnings, w => w.Contains("prototype", StringComparison.Ordinal));

        var link = Setup(Html(), map: DefaultMap + "- Icon: https://learn.microsoft.com/power-apps/x\n");
        Assert.Equal("docs/plans/demo/mockups/prototype.map.md:5: looks like a URL; remove it, or add <!-- plan-status: allow --> to the line", Assert.Single(link.Violations));
    }

    [Fact]
    public void A_fresh_judge_verdict_at_baseline_bound_to_the_file_is_quiet_and_the_last_verdict_decides()
    {
        Assert.Empty(Problems(Html()).File);
        Assert.Empty(Problems(Html(), judge: "judge: fresh context\nsketch-hash: {hash}\n\n## Round 1\n\nverdict: not at baseline\n\n## Round 2\n\nverdict: at baseline\n").File);

        var reversed = Problems(Html(), judge: "judge: fresh context\nsketch-hash: {hash}\n\n## Round 1\n\nverdict: at baseline\n\n## Round 2\n\nverdict: not at baseline\n");
        Assert.Contains("verdict is not at baseline", Assert.Single(reversed.File), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("sketch-hash: {hash}\n\nverdict: at baseline\n")]
    [InlineData("judge: me\nsketch-hash: {hash}\n\nverdict: at baseline\n")]
    [InlineData("judge: fresh context\n\nverdict: at baseline\n")]
    [InlineData("judge: fresh context\nsketch-hash: 0000000000000000000000000000000000000000000000000000000000000000\n\nverdict: at baseline\n")]
    [InlineData("judge: fresh context\nsketch-hash: {hash}\n")]
    [InlineData("judge: fresh context\nsketch-hash: {hash}\n\nverdict: at the baseline\n")]
    [InlineData("judge: fresh context\nsketch-hash: {hash}\n\nverdict: pass\n")]
    [InlineData("judge: fresh context\nsketch-hash: {hash}\n\nverdict: At baseline\n")]
    [InlineData("judge: fresh context\nsketch-hash: {hash}\n\nverdict: at baseline (mostly)\n")]
    public void A_missing_judge_file_a_wrong_hash_no_judge_line_or_odd_verdict_is_no_current_judge_verdict(string? judge)
    {
        var r = Problems(Html(), Unmarked, judge: judge);

        Assert.Equal($"mockups/prototype.judge.md: {NoVerdict}: have powerops-review-sketch judge mockups/prototype.html again (its verdict counts for the sketch-hash it names)", Assert.Single(r.File));
    }

    [Fact]
    public void An_edit_after_the_verdict_unbinds_it_and_a_crlf_copy_of_the_judged_file_still_matches()
    {
        Setup(Html(), Unmarked);
        var file = Path.Combine(Folder, "mockups", "prototype.html");

        System.IO.File.WriteAllText(file, Html().Replace("\n", "\r\n", StringComparison.Ordinal));
        Assert.Empty(Recheck().File);

        System.IO.File.WriteAllText(file, Html(body: "<p>Sketch, edited</p>\n"));
        Assert.Contains(NoVerdict, Assert.Single(Recheck().File), StringComparison.Ordinal);
    }

    [Fact]
    public void A_verdict_not_at_baseline_needs_the_persons_pass_with_a_below_baseline_reason()
    {
        const string NotAt = "judge: fresh context\nsketch-hash: {hash}\n\nverdict: not at baseline\n";

        var r = Problems(Html(), judge: NotAt);
        Assert.Equal("mockups/prototype.judge.md: the verdict is not at baseline: only a person passes it, logged with (below baseline: <why>) and (answered by: person) on the prototype line", Assert.Single(r.File));
        Assert.Equal(1, SignOff().ExitCode);

        var agent = Problems(Html(), "- prototype: approved mockups/prototype.html (option a: fewest steps) (below baseline: the chart is crowded) (answered by: agent)", judge: NotAt);
        Assert.Contains(agent.File, p => p.Contains("verdict is not at baseline", StringComparison.Ordinal));

        Assert.Contains(Problems(Html(), "- prototype: approved mockups/prototype.html (option a: fewest steps) (below baseline: ) (answered by: person)", judge: NotAt).File, p => p.Contains("verdict is not at baseline", StringComparison.Ordinal));

        // The person's route: no problem and no warning, and the plan signs off.
        const string Below = "- prototype: approved mockups/prototype.html (option a: fewest steps) (below baseline: the chart is crowded, fixed in the build) (answered by: person)";
        var person = Problems(Html(), Below, judge: NotAt);
        Assert.Empty(person.File);
        Assert.Empty(person.Warnings);
        Assert.DoesNotContain(Setup(Html(), Below, judge: NotAt).Warnings, w => w.Contains("prototype", StringComparison.Ordinal));
        Assert.Equal(0, SignOff().ExitCode);
    }

    [Fact]
    public void A_self_judged_sketch_warns_and_only_the_persons_approval_signs_it_off()
    {
        const string Self = "judge: self-judged\nsketch-hash: {hash}\n\nverdict: at baseline\n";
        const string Warning = "mockups/prototype.judge.md: self-judged (the author judged its own sketch): only a person may approve it";

        var person = Problems(Html(), judge: Self);
        Assert.Empty(person.File);
        Assert.Equal(Warning, Assert.Single(person.Warnings));
        Assert.Contains(Warning, Setup(Html(), judge: Self).Warnings);
        var signed = SignOff();
        Assert.Equal(0, signed.ExitCode);
        Assert.Contains($"warning: {Warning}", signed.Err);

        const string ByAgent = "- prototype: approved mockups/prototype.html (option a: fewest steps) (answered by: agent)";
        var agent = Problems(Html(), ByAgent, judge: Self);
        Assert.Contains(Warning, agent.Warnings);
        Assert.Equal("mockups/prototype.judge.md: self-judged, so the approval must end (answered by: person)", Assert.Single(agent.File));
        Setup(Html(), ByAgent, judge: Self);
        Assert.Contains(SignOff().Err, e => e.Contains("self-judged, so the approval must end", StringComparison.Ordinal));
    }

    [Fact]
    public void Sign_off_refuses_an_approved_prototype_with_no_current_judge_verdict()
    {
        Setup(Html(), Unmarked, judge: null);
        var r = SignOff();

        Assert.Equal(1, r.ExitCode);
        Assert.Contains(r.Err, e => e.Contains(NoVerdict, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("#BDBDBD")]
    [InlineData("#fff")]
    [InlineData("#0f6cbd80")]
    public void A_hex_colour_outside_root_fires_on_its_line_in_the_style_element_and_in_style_attributes(string hex)
    {
        // Row 13 (j), decisions Q5.
        var rule = Problems(Html(body: $"<p>Sketch</p>\n<style>\n:root{{--brand:#0F6CBD}}\n.btn[disabled]{{color:{hex}}}\n</style>\n"));
        Assert.Equal($"{File}:6: hex colour outside :root; use a token (var(--name))", Assert.Single(rule.File));

        var media = Problems(Html(body: $"<p>Sketch</p>\n<style>@media (max-width:600px){{\n.a{{border:1px solid {hex}}}}}</style>\n"));
        Assert.Equal($"{File}:5: hex colour outside :root; use a token (var(--name))", Assert.Single(media.File));

        var attribute = Problems(Html(body: $"<p>Sketch</p>\n<p style=\"color:{hex}\">x</p>\n"));
        Assert.Equal($"{File}:4: hex colour outside :root; use a token (var(--name))", Assert.Single(attribute.File));
    }

    [Fact]
    public void Hex_in_root_blocks_selectors_comments_text_and_svg_attributes_is_quiet()
    {
        var css = "<style>\n/* brand #0F6CBD */\n:root{--brand:#0F6CBD;--ink:#242424}\n#home, a#x{color:var(--brand)}\n:root{--muted:#616161}\n@media (min-width:600px){:root{--gap:#fff}}\n</style>\n";
        var body = $"<p>Sketch</p>\n{css}<p>Order #123 and #fff</p>\n<svg><rect fill=\"#0F6CBD\" stroke=\"#fff\" stop-color=\"#000\"/></svg>\n";

        Assert.Empty(Problems(Html(body: body)).File);
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

    // A sketch in the format before M97: data-screen sections under body, the states after each, no map, no judge file.
    private static string OldSketch(char letter = 'p') =>
        $"<!-- route: canvas -->\n<html><body>\n<p>Sketch {letter}</p>\n<section data-screen=\"Home\">\n{string.Concat(Four.Select(s => $"  <div data-state=\"{s}\">none</div>\n"))}</section>\n<svg><rect/></svg>\n</body></html>\n";

    [Fact]
    public void A_signed_off_plan_with_an_old_format_sketch_gets_no_violation_and_no_warning()
    {
        // Signed off before this change: with a person's skip, so the tool signs; then the old sketch and its approval are what the folder holds.
        Setup(null, "- prototype: skipped (signed before M97) (answered by: person)", options: []);
        Assert.Equal(0, SignOff().ExitCode);
        _t.Write("docs/plans/demo/decisions.md", $"# Decisions\n\n## Log\n\n{Approved}\n");
        _t.Write("docs/plans/demo/" + File, OldSketch());
        foreach (var letter in "abc")
        {
            _t.Write($"docs/plans/demo/mockups/option-{letter}.html", OldSketch(letter));
        }

        var r = PlanChecker.Check(Path.Combine(Folder, "plan.md"), _t.Path);

        Assert.Equal("signed-off", r.Status);
        Assert.Empty(r.Violations);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void An_approved_plan_with_an_old_format_sketch_warns_is_refused_and_signs_off_after_a_persons_skip()
    {
        // Decisions Q13: the new problems are warnings in status and refusals at sign-off; the route is a new options run or a person's skip.
        Setup(OldSketch(), Unmarked, options: [OldSketch('a'), OldSketch('b'), OldSketch('c')], map: null, judge: null);
        var status = PlanChecker.Check(Path.Combine(Folder, "plan.md"), _t.Path);
        Assert.Empty(status.Violations);
        Assert.Contains(status.Warnings, w => w.Contains("data-state has no data-for", StringComparison.Ordinal));
        Assert.Contains(status.Warnings, w => w.Contains(NoVerdict, StringComparison.Ordinal));
        Assert.Equal(1, SignOff().ExitCode);

        _t.Write("docs/plans/demo/decisions.md", "# Decisions\n\n## Log\n\n- prototype: skipped (sketched before the baseline) (answered by: person)\n");
        Assert.Equal(0, SignOff().ExitCode);
    }

    // Row 13, decisions Q8: the three passed samples predate the markers (route:, data-lead, data-for, the map, the judge file), so they get exactly these problems:
    // no tag, attribute, CSS, href, url( or size problem, and one token problem, the embedded kit's .btn[disabled] color:#BDBDBD.
    [Theory]
    [InlineData("canvas-repairs-desk", "Repairs, Log a repair, Repairs (phone), Repair (phone), Log a repair (phone)", 93, new[] { 463, 544, 624, 657, 686 }, new[] { 714, 724, 739, 754, 768, 781 })]
    [InlineData("pages-tenant-repairs", "Home, Report a repair, Your repairs, Home (phone), Your repairs (phone)", 92, new[] { 551, 666, 768, 857, 877 }, new[] { 906, 915, 929, 952, 973, 990 })]
    [InlineData("powerbi-repairs-performance", "Overview, Overview (phone layout)", 93, new[] { 303, 399 }, new[] { 456, 517, 518, 519, 528, 565 })]
    public void The_shipped_samples_get_only_the_problems_of_the_markers_after_them(string sample, string screens, int token, int[] frames, int[] states)
    {
        // Normalised, so a CRLF checkout reads the same; no byte or hash pin.
        var text = System.IO.File.ReadAllText(Path.Combine(Golden.RepoRoot, "skills", "powerops-plan", "references", "samples", sample + ".html")).Replace("\r\n", "\n", StringComparison.Ordinal);

        // An unmarked approval, so the judge file is asked for (M124 row 3) and the three options with it.
        var r = Problems(text, Unmarked, plan: Plan($"## Screens\n\n{screens}\n"), options: [], map: null, judge: null);

        // Each sample puts data-screen on the frame itself, so a frame's line is its screen's line.
        string[] expected =
        [
            $"{File}:1: no route: line in the first comment",
            $"{File}:{token}: hex colour outside :root; use a token (var(--name))",
            .. frames.Select(l => $"{File}:{l}: the frame holds 0 elements with data-lead; it needs exactly 1"),
            .. states.Select(l => $"{File}:{l}: data-state has no data-for naming a data-screen of the file"),
            .. frames.Select(l => $"{File}:{l}: data-screen has no data-state empty, loading, error, failed-save with data-for naming it"),
            "mockups/prototype.map.md: not found; it names each screen's parts beside the sketch",
            $"mockups/prototype.judge.md: {NoVerdict}: have powerops-review-sketch judge mockups/prototype.html again (its verdict counts for the sketch-hash it names)",
            "mockups/: 0 option files (option-*.html) pass the checks and differ; an approved sketch is picked from at least 3",
        ];
        Assert.Equal(expected, r.File);
        Assert.Equal(".btn[disabled]{background:var(--fill);border-color:var(--fill);color:#BDBDBD}", text.Split('\n')[token - 1]);
    }

    // M124 Data row 3: a person's pass (an approval ending (answered by: person)) is the gate; the judge and the options are the skill's, not the checker's.
    [Fact]
    public void A_persons_pass_needs_no_judge_file_and_no_options_and_signs_off()
    {
        var r = Problems(Html(), Pass, options: [], judge: null);

        Assert.Empty(r.Line);
        Assert.Empty(r.File);
        Assert.Empty(r.Warnings);
        Assert.True(r.Approved);
        Assert.DoesNotContain(Setup(Html(), Pass, options: [], judge: null).Warnings, w => w.Contains("prototype", StringComparison.Ordinal));

        var signed = SignOff();
        Assert.Equal(0, signed.ExitCode);
        Assert.Empty(signed.Err);
    }

    [Theory]
    [InlineData("judge: fresh context\nsketch-hash: " + Zeros + "\n\nverdict: at baseline\n")]
    [InlineData("judge: me\nsketch-hash: {hash}\n\nverdict: at baseline\n")]
    [InlineData("judge: fresh context\nsketch-hash: {hash}\n")]
    [InlineData("judge: fresh context\nsketch-hash: " + Zeros + "\n\n## Round 1\n\nverdict: not at baseline\n\n## Round 2\n\nverdict: at baseline\n")]
    [InlineData("sketch-hash: {hash}\n\nverdict: pass\n")]
    public void A_persons_pass_with_a_stale_or_odd_judge_file_at_baseline_or_without_a_verdict_is_satisfied(string judge)
    {
        var r = Problems(Html(), Pass, options: [], judge: judge);

        Assert.Empty(r.Line);
        Assert.Empty(r.File);
        Assert.Equal(0, SignOff().ExitCode);
    }

    [Theory]
    [InlineData("judge: fresh context\nsketch-hash: " + Zeros + "\n\nverdict: not at baseline\n")]
    [InlineData("judge: me\nsketch-hash: {hash}\n\nverdict: not at baseline\n")]
    [InlineData("judge: fresh context\nsketch-hash: " + Zeros + "\n\n## Round 1\n\nverdict: at baseline\n\n## Round 2\n\nverdict: not at baseline\n")]
    public void A_persons_pass_after_a_stale_or_odd_not_at_baseline_verdict_still_needs_the_below_baseline_reason(string judge)
    {
        // The person ran the judge, so its verdict stands until a new round says at baseline; an edit after it does not drop the reason.
        var r = Problems(Html(), Pass, options: [], judge: judge);
        Assert.Equal("mockups/prototype.judge.md: the verdict is not at baseline: only a person passes it, logged with (below baseline: <why>) and (answered by: person) on the prototype line", Assert.Single(r.File));
        Assert.Equal(1, SignOff().ExitCode);

        const string Below = "- prototype: approved mockups/prototype.html (main screen: fewest steps) (below baseline: the chart is crowded) (answered by: person)";
        Assert.Empty(Problems(Html(), Below, options: [], judge: judge).File);
        Assert.Equal(0, SignOff().ExitCode);
    }

    [Fact]
    public void A_persons_pass_after_a_current_not_at_baseline_verdict_still_needs_the_below_baseline_reason()
    {
        const string NotAt = "judge: fresh context\nsketch-hash: {hash}\n\nverdict: not at baseline\n";

        Assert.Contains(NotAtBaseline, Assert.Single(Problems(Html(), Pass, options: [], judge: NotAt).File), StringComparison.Ordinal);
        Assert.Equal(1, SignOff().ExitCode);
    }

    [Fact]
    public void A_persons_pass_of_a_self_judged_sketch_still_warns()
    {
        const string Self = "judge: self-judged\nsketch-hash: {hash}\n\nverdict: at baseline\n";

        var r = Problems(Html(), Pass, options: [], judge: Self);
        Assert.Empty(r.File);
        Assert.Equal(PlanChecker.SelfJudgedWarning, Assert.Single(r.Warnings));
        var signed = SignOff();
        Assert.Equal(0, signed.ExitCode);
        Assert.Contains($"warning: {PlanChecker.SelfJudgedWarning}", signed.Err);
    }

    [Theory]
    [InlineData("- prototype: approved mockups/prototype.html (option b: the best one) (answered by: person)")]
    [InlineData("- prototype: approved mockups/prototype.html (option c: the best one) (answered by: person)")]
    public void A_persons_pass_that_names_an_option_needs_that_option_to_exist_and_pass(string log)
    {
        // Option b is missing and option c fails its checks: the line is the record of a pick, so the pick must hold.
        var r = Problems(Html(), log, options: [Option('a'), null, "<p>nothing</p>"], judge: null);

        Assert.Contains(r.File, p => p.StartsWith("decisions.md:5: the approved line names no option that passes the checks", StringComparison.Ordinal));
        Assert.DoesNotContain(r.File, p => p.Contains("option files", StringComparison.Ordinal));
        Assert.Equal(1, SignOff().ExitCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_persons_pass_that_names_no_option_needs_no_three_options(int count)
    {
        var r = Problems(Html(), Pass, options: [.. "abc"[..count].Select(Option)], judge: null);

        Assert.Empty(r.File);
        Assert.Equal(0, SignOff().ExitCode);
    }

    [Fact]
    public void A_persons_pass_that_names_a_passing_option_is_fine_with_fewer_than_three()
    {
        var r = Problems(Html(), Approved, options: [Option('a')], judge: null);

        Assert.Empty(r.File);
        Assert.Equal(0, SignOff().ExitCode);
    }

    [Fact]
    public void An_unmarked_approval_keeps_three_passing_options_a_named_passing_option_and_a_current_verdict()
    {
        // Checker level: each rule fires alone.
        Assert.Contains("2 option files", Assert.Single(Problems(Html(), Unmarked, options: [Option('a'), Option('b')]).File), StringComparison.Ordinal);
        Assert.Contains("names no option that passes", Assert.Single(Problems(Html(), "- prototype: approved mockups/prototype.html (main screen: fewest steps)").File), StringComparison.Ordinal);
        Assert.Contains(NoVerdict, Assert.Single(Problems(Html(), Unmarked, judge: null).File), StringComparison.Ordinal);
        var ok = Problems(Html(), Unmarked);
        Assert.Empty(ok.File);
        Assert.Contains(ok.Warnings, w => w.Contains("prototype approval is not marked (answered by: person)", StringComparison.Ordinal));
    }

    [Fact]
    public void Sign_off_of_an_unmarked_approval_is_refused_with_fewer_than_three_options_no_named_option_or_no_verdict_and_passes_with_them()
    {
        Setup(Html(), Unmarked, options: [Option('a'), Option('b')]);
        var two = SignOff();
        Assert.Equal(1, two.ExitCode);
        Assert.Contains("error: mockups/: 2 option files (option-*.html) pass the checks and differ; an approved sketch is picked from at least 3", two.Err);

        Setup(Html(), "- prototype: approved mockups/prototype.html (main screen: fewest steps)");
        Assert.Contains(SignOff().Err, e => e.Contains("names no option that passes", StringComparison.Ordinal));

        Setup(Html(), Unmarked, judge: null);
        var none = SignOff();
        Assert.Equal(1, none.ExitCode);
        Assert.Contains(none.Err, e => e.Contains(NoVerdict, StringComparison.Ordinal));

        Setup(Html(), Unmarked);
        Assert.Equal(0, SignOff().ExitCode);
    }

    [Fact]
    public void A_persons_skip_with_a_reason_and_a_sketch_without_a_judge_file_has_no_judge_problem_and_signs_off()
    {
        const string Skip = "- prototype: skipped (mock-data slice) (answered by: person)";

        var r = Problems(Html(), Skip, options: [], judge: null);
        Assert.Empty(r.Line);
        Assert.Empty(r.File);
        Assert.DoesNotContain(Setup(Html(), Skip, options: [], judge: null).Warnings, w => w.Contains(NoVerdict, StringComparison.Ordinal));
        Assert.Equal(0, SignOff().ExitCode);
    }

    [Theory]
    [InlineData("- prototype: skipped () (answered by: person)")]
    [InlineData("- prototype: skipped (answered by: person)")]
    [InlineData("- prototype: skipped (no time)")]
    [InlineData("- prototype: skipped (no time) (answered by: agent)")]
    public void Sign_off_refuses_a_skip_with_no_reason_or_no_persons_mark(string log)
    {
        Setup(Html(), log, options: [], judge: null);
        var r = SignOff();

        Assert.Equal(1, r.ExitCode);
        Assert.Contains("no valid prototype line", Assert.Single(r.Err), StringComparison.Ordinal);
    }

    [Fact]
    public void Before_any_line_a_sketch_without_a_judge_file_does_not_warn_and_a_stale_judge_file_still_does()
    {
        // So the main mode's plan status is clean before the person answers.
        var draft = Plan("## Screens\n\n- Home\n- Booking form\n", "draft");
        var none = Setup(Html(), log: null, plan: draft, options: [], judge: null);
        Assert.DoesNotContain(none.Warnings, w => w.Contains(NoVerdict, StringComparison.Ordinal));
        Assert.Empty(Recheck().File);

        var stale = Setup(Html(), log: null, plan: draft, options: [], judge: "judge: fresh context\nsketch-hash: " + Zeros + "\n\nverdict: at baseline\n");
        Assert.Contains(stale.Warnings, w => w.Contains(NoVerdict, StringComparison.Ordinal));
    }

    [Fact]
    public void The_line_problem_names_the_pass_form()
    {
        var r = Problems(null, log: null);

        Assert.Contains("`prototype: approved mockups/prototype.html (<why>) (answered by: person)`", Assert.Single(r.Line), StringComparison.Ordinal);
        Assert.Contains("`prototype: approved mockups/prototype.html (option <letter>: <why>)`", r.Line[0], StringComparison.Ordinal);
        Assert.Contains("`prototype: skipped (<reason>) (answered by: person)`", r.Line[0], StringComparison.Ordinal);
    }
}