using PowerOps.Core.Plans;

namespace PowerOps.Core.Tests.Plans;

public class PlanDocumentTests
{
    private const string Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void Parses_a_valid_header_and_strips_inline_comments_and_the_bom()
    {
        var text = "﻿---\nstatus: signed-off   # draft | approved\nreview: pending   # pending | done | skipped\n\n" +
            $"signed-off: 2026-10-19\nsigned-off-hash: {Hash}\nenvironment: Contoso Dev\n---\n# Plan\n";

        var doc = PlanDocument.Parse(text);

        Assert.Null(doc.Error);
        Assert.Equal(new PlanHeader("signed-off", "pending", new DateOnly(2026, 10, 19), Hash, "Contoso Dev"), doc.Header);
        Assert.NotNull(doc.BodyHash);
    }

    [Fact]
    public void Null_and_empty_values_are_null()
    {
        var doc = PlanDocument.Parse("---\nstatus: draft\nreview: skipped\nsigned-off: null\nsigned-off-hash:\nenvironment: # none yet\n---\n");

        Assert.Equal(new PlanHeader("draft", "skipped", null, null, null), doc.Header);
    }

    [Theory]
    [InlineData("status: draft\nreview: pending\n---\nbody", "does not start with")]
    [InlineData("---\nstatus: draft\nreview: pending\nbody", "no closing")]
    [InlineData("---\nstatus: draft\nreview: pending\ncolour: red\n---\n", "unknown key 'colour'")]
    [InlineData("---\nstatus: draft\nstatus: draft\nreview: pending\n---\n", "duplicate key 'status'")]
    [InlineData("---\nstatus: aproved\nreview: pending\n---\n", "status 'aproved' is not one of draft, approved, signed-off, implementing, done")]
    [InlineData("---\nstatus: draft\nreview: maybe\n---\n", "review 'maybe' is not one of pending, done, skipped")]
    [InlineData("---\nstatus: draft\n---\n", "review is missing")]
    [InlineData("---\nreview: done\n---\n", "status is missing")]
    [InlineData("---\nstatus: draft\nreview: done\nsigned-off: 19/10/2026\n---\n", "not a date")]
    [InlineData("---\nstatus: draft\nreview: done\nsigned-off: 2026-13-40\n---\n", "not a date")]
    [InlineData("---\nstatus: draft\nreview: done\nsigned-off-hash: ABC\n---\n", "64 lowercase hex")]
    [InlineData("---\nstatus: draft\nreview done\n---\n", "has no ':'")]
    [InlineData("", "does not start with")]
    public void Malformed_headers_have_a_specific_reason(string text, string reason)
    {
        var doc = PlanDocument.Parse(text);

        Assert.Null(doc.Header);
        Assert.Contains(reason, doc.Error);
    }

    [Fact]
    public void Body_hash_is_computed_even_when_a_value_is_invalid()
    {
        var doc = PlanDocument.Parse("---\nstatus: nope\nreview: done\n---\nbody\n");

        Assert.NotNull(doc.Error);
        Assert.Matches("^[0-9a-f]{64}$", doc.BodyHash);
    }

    [Fact]
    public void Body_hash_has_no_value_when_a_fence_is_missing()
    {
        Assert.Null(PlanDocument.Parse("# no header\n").BodyHash);
        Assert.Null(PlanDocument.Parse("---\nstatus: draft\n").BodyHash);
    }

    [Fact]
    public void Body_hash_is_the_same_for_crlf_and_lf_and_ignores_the_header()
    {
        var lf = PlanDocument.Parse("---\nstatus: draft\nreview: pending\n---\n# Plan\n\ntext\n");
        var crlf = PlanDocument.Parse("---\r\nstatus: approved\r\nreview: done\r\n---\r\n# Plan\r\n\r\ntext\r\n");
        var edited = PlanDocument.Parse("---\nstatus: draft\nreview: pending\n---\n# Plan\n\ntext!\n");

        Assert.Equal(lf.BodyHash, crlf.BodyHash);
        Assert.NotEqual(lf.BodyHash, edited.BodyHash);
    }

    [Fact]
    public void Counts_top_level_list_items_under_the_two_headings_only()
    {
        var doc = PlanDocument.Parse("""
            ---
            status: draft
            review: pending
            ---
            # Plan
            - not counted
            ## open QUESTIONS
            - one
            * two
            3. three
              - nested, not counted
            ### Sub heading still inside
            - four
            ## Other
            - not counted
            ## Questions for your admin
            - five
            # Next
            - not counted
            """.Replace("\r\n", "\n"));

        Assert.Equal(5, doc.OpenQuestions);
    }
}