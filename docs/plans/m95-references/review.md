plan-hash: a7fa8846f2d1af79044469fd23ebd2c402f4773d483ef212acb3218d37558ab0
model: claude-opus-5-5
fresh-context: yes

Environment facts were not read: the request did not say that `pac` is signed in to the plan's environment, and the plan's `environment:` is null (this milestone changes PowerOps itself and calls no tenant; tenant: offline, no `pac` or `az` call was made).

This review replaces the review of the round-1 plan (plan-hash `9b0ec81`), which `plan status` reported stale. Its findings R1 to R13 were read against the current plan and are closed by it; they are not repeated. The numbers below are new.

`plan status` was run with the worktree's build (`dotnet src/PowerOps.Cli/bin/Debug/net10.0/powerops.dll`; `src/` is unchanged from `87a31dd` to `cb2c737`), because the `powerops` on PATH is at `b065f23`. Status `approved`, review `pending`, 0 open questions, 0 admin questions, no violation; one warning, the stale review this file replaces.

Tools (item 12): `dotnet`, `git`, `grep`, `gh` and `powerops` were found with `command -v`. `reports/leakscan.sh` and `reports/ci-watch.sh` exist in the main checkout, as step 5 says. The `fable-reviewer` agent and the "Update goldens" recipe (`AGENTS.md`) exist. No step names an MCP tool or a Microsoft skill.

Read besides the plan: `skills/powerops-implement/SKILL.md` sections 4 and 5; `skills/powerops-plan/references/copilot-studio.md`, `canvas.md`, `power-bi.md` and `licensing.md`; `.claude/agents/powerops-workflow-logic.md:32` and `:56`; `.claude/agents/powerops-copilot-studio.md:9`, `:32` and `:43`; `docs/HANDOFF.md` steps 3 to 8 and `:25`; `docs/GITHUB-ACTION.md` (`:16`, `:23`, `:29-40`, `:58`, `:80`, `:133`, `:168`); `action.yml` inputs; `docs/examples/README.md` (`:19-21`, `:40`, `:155`, `:189`) and `promote-dev-to-test.yml`; `docs/COMMANDS.md:100-101`, `:113`, `:227`; `docs/ecosystem/changes.md` (last id ECO-0019) and the rows of `compatibility.md`; `SolutionParser.cs` (`ReadMsapps`), `SolutionDiscovery.cs`; `tests/fixtures/powerops-seed-yaml/*/canvasapps/`; `HandoffDocTests.cs`, `PluginManifestTests.cs` (`:10`, `:104-126`, `:150-190`, `:207-211`), `SkillsCommandTests.cs` (`:437-441`, `:555`, `:819-890`), `EcosystemDocsTests.cs`; `tests/smoke/skills-smoke.sh`; `docs/plans/m93-design-process/plan.md:36`. Claims that hold at `87a31dd`: the HANDOFF step 3 paragraph and the sentences row 6 keeps or replaces; `implement` section 4 step 2 and section 5's last bullet; the workflow-logic step-2 line and `:56`; the pin of `powerbi-report-author validate`; the 3,615-word ceiling (the file is at 3,615); ECO-0020 is the next free id; the `licensing.md` label in three places; no smoke line reads a changed phrase; `diff` reads the `.meta.xml`. The Microsoft facts of A1 to A14 and the GitHub heads of A11 and A12 were not re-checked (offline review).

Not applicable to a PowerOps milestone with no Power Platform solution: item 1 (solution and publisher), 2 (connection references and environment variables), 3 (licences: the plan names connectors, agents and reports only as reference text, and builds none), 4 (DLP), 6 (managed or unmanaged), 7 (delegation) and 8 (security roles). Item 5's route is Git: a branch, local commits, the chain's merge lock; nothing goes to a tenant. Item 11 has no table, list, flow or screen; R7 covers a change no Goal line names.

## Findings

### High

#### R1. Step 3's done-condition needs the plugin-copy tests to pass before step 4 regenerates the copies
- Area: 13 Buildability (two plan lines that can't both hold); 9 Testability
- Finding: Step 3 changes `skills/` and `.claude/agents/powerops-workflow-logic.md`, and is done when `dotnet test --filter "SkillsCommandTests|PluginManifestTests|DocsDriftTests|HandoffDocTests|EcosystemDocsTests"` passes. `PluginManifestTests` compares `plugins/*/skills/` with `skills/` (`:104-126`) and `plugins/claude/agents/` with the generated specialists (`:150-190`), and fails on any difference until the "Update goldens" recipe runs. The plan runs that recipe in step 4, as its own commit. So step 3's check fails by construction, and an executor must either regenerate the copies in step 3 (against step 4) or commit with a failing check. M93 named the same split (its pins in tests of their own, green in step 3; the copy tests green in step 4).
- Suggested fix: In step 3's done-condition, run the classes without the two copy tests (or name them as expected to fail until step 4), and keep the full filter in step 4 and Done line 3; or fold step 4's recipe into step 3 and commit the copies with it.
- Resolution: resolved (round 3): step 3's check runs `SkillsCommandTests|DocsDriftTests|HandoffDocTests|EcosystemDocsTests` and row 9 (c)'s test by name; the whole of `PluginManifestTests` runs in step 4, after the copies are regenerated.

### Medium

#### R2. The pack limit of row 2 doesn't reach the Copilot Studio specialist's offline check or Hand-off's product table
- Area: 13 Buildability; 10 Unanswered areas
- Finding: Row 2 writes into `copilot-studio.md` that, observed in one run, `pac copilot pack` refused a workspace with connector tools, a knowledge file and a connection references file, so a check doesn't rest on pack for such a workspace and checks the exported solution. `.claude/agents/powerops-copilot-studio.md:32` (checklist step 5) still says "Check offline: `pac copilot pack` (local) for a workspace, then `powerops analyze`", and `:9` and `docs/HANDOFF.md:25` say a workspace "becomes a solution with `pac copilot pack`". Decisions Q4 re-read only item 7 and line 43 of that agent. The specialist that reads the reference keeps the route the trial saw fail, with no fallback.
- Suggested fix: Add a clause to the specialist's step 5 (when pack refuses the workspace, check the exported solution) and to `HANDOFF.md:25`, with a pin; or record in decisions Q4 why `:9`, `:32` and `HANDOFF.md:25` stay as they are. (`docs/COMMANDS.md:113` is frozen by Won't build.)
- Resolution: resolved (round 3): row 2 adds the fallback to `.claude/agents/powerops-copilot-studio.md` step 5 and `docs/HANDOFF.md:25`; line 9 describes the route and stays (decisions Q4).

#### R3. Row 7's second use of the Action names no code scanning category of its own
- Area: 9 Testability
- Finding: Row 7 tells `docs/GITHUB-ACTION.md` readers to run the canvas checks in a second use of the action with `path` on the deploy folder. Both uses then upload SARIF under the default `category` `powerops` (`action.yml`), and `GITHUB-ACTION.md:23` already says to set `category` "to tell several analyses in one repository apart". Without it, the second upload for the commit replaces the first one's alerts in code scanning, so the folder's findings vanish from the Security tab while the job summary still shows them. Row 7's paragraph names `path`, `tests` and `redact`, not `category` (and not `comment`, which stays at its default `'false'` only if the reader doesn't copy the full example's `comment: 'true'`).
- Suggested fix: Row 7's paragraph also sets `category` (for example `powerops-zips`) on the second use and leaves `comment` off there; add "category" to the row 9 (f) pin of that sentence.
- Resolution: resolved (round 3): the second use has its own `category`, as `GITHUB-ACTION.md:23` says, and no `tests`, `comment` or `target`.

#### R4. The ephemeral recipe's exception doesn't cover its "pack from an unpack" route
- Area: 13 Buildability; 5 Route
- Finding: Row 7 makes `ephemeral-pr.yml` the exception to the new default: private repository only, and its users leave out the `/deploy/*.zip` line. But `docs/examples/README.md:155` describes `deploy/` as zips "your own build makes, for example `pac solution pack --packagetype Managed` from an unpack made with `--packagetype Both`", and `:189` says the recipe doesn't pack them. With `*.msapp` still ignored, an unpack in Git holds no canvas package, and row 6 itself says a rebuild "exports again from Dev, never packs from Git" (A14: a YAML-layout pack with a missing `.msapp` exits 0 and leaves the app out; the XML layout is unknown). A user who packs `deploy/` from a fresh checkout ships zips without their canvas apps, and the import succeeds.
- Suggested fix: Row 7's exception sentence also says the zips in `deploy/` come from an export (or a pack on the machine that holds the `.msapp` files), never a pack from a checkout; and adjust `:155`'s example, with the pin of row 9 (f).
- Resolution: resolved (round 3): row 7 changes `examples/README.md:155`'s example source of the zips to a managed export (A14).

### Low

#### R5. "Formulas only from the `.msapp`" is true for the XML layout only
- Area: 9 Testability; the last Assumptions line
- Finding: Row 6's HANDOFF sentence and `implement` section 4 step 1's new sentence ("the folder in Git holds no `.msapp`", so canvas formulas are checked on the zip) and the last Assumptions line say `analyze` reads a canvas app's formulas only from its `.msapp`. `docs/COMMANDS.md:100` says the YAML layout's formulas are read from the folder (`Src/*.pa.yaml`, as in `tests/fixtures/powerops-seed-yaml/*/canvasapps/`). In HANDOFF step 3 (XML only) the sentence holds; in `powerops-implement`, which also meets Git integration folders, it overstates.
- Suggested fix: Scope the `implement` sentence and the Assumptions line to the XML layout ("in an XML unpack, formulas are read only from the `.msapp`").
- Resolution: resolved (round 3): row 6 says "in the XML layout of step 3" and cites `COMMANDS.md:100` for the YAML layout.

#### R6. Row 9 cites a shared helper that two of its classes don't have, and adds keep-guards that exist
- Area: 9 Testability
- Finding: Row 9 says each phrase is asserted "with the `Between` helper the test classes already use". `Between` is private to `SkillsCommandTests` (`:882`); `HandoffDocTests` and `PluginManifestTests` cut sections with `IndexOf`. Row 9 (d)'s "is covered by the sign-off" and "class 6" in section 5 are already pinned (`SkillsCommandTests.cs:437-441`), so a new assertion would duplicate them.
- Suggested fix: Say "`Between` in `SkillsCommandTests`, and the classes' own section slicing elsewhere"; in (d), cite `:437-441` and `:857` as the existing guards and add none.
- Resolution: resolved (round 3): row 9 names `Between` as `SkillsCommandTests`' own and an `IndexOf` slice elsewhere; row 9 (d) names the existing guards at `:437-441` and `:857` and adds no copy.

#### R7. Row 5's Licence fit pointer is named by no Goal line and pinned by no assertion
- Area: 11 Size; 9 Testability
- Finding: Row 5 adds "The Licence fit bullet points to `licensing.md` (M91)" to `power-bi.md`. The Goal's follow-ups are the `licensing.md` link title only, and row 9 (a) lists no phrase for the pointer.
- Suggested fix: Name the pointer in the Goal's follow-up clause and add "`licensing.md`" in section 1 of `power-bi.md` to row 9 (a); or drop it.
- Resolution: resolved (round 3): row 5 says what the pointer is for and row 9 (a) pins it; Done line 1 covers row 5.

#### R8. Row 5's render-and-measure step names no source of the screenshots
- Area: 13 Buildability
- Finding: Row 5 adds a render-and-measure step (screenshots and each visual's text contrast, read from the rendered report) after the first deploy to the Dev workspace, in `power-bi.md` and `implement` section 5. Section 5 today records the design tests "against screenshots the user gives you", and section 4's design check says where screenshots come from (a browser tool when the person is signed in, else the person). Row 5 says neither, nor what gives "not checked" when no screenshot exists.
- Suggested fix: Row 5: the screenshots come as section 5 says (from the person, or a browser tool when they are signed in), and with none the step's result is "not checked" and why.
- Resolution: resolved (round 3): row 5 names the source (a browser tool with the person signed in, reading computed colours, else the person) and "not checked" with neither.

#### R9. The plan holds URLs
- Area: The skill's rule on URLs, GUIDs, emails and names
- Finding: Assumptions A1 to A4, A6, A7, A10, A11, A13 and A14 give page and repository addresses (without a scheme). They are public Microsoft sources, not tenant values; no GUID, email or person's name was found in `plan.md` or `decisions.md`.
- Suggested fix: Keep the page titles and dates and drop the addresses, or accept them again as public sources (the round-1 review's R12 was accepted on that ground).
- Resolution: accepted (round 3): public Learn and GitHub addresses, as every reference and plan in this repo cites; no tenant value.
