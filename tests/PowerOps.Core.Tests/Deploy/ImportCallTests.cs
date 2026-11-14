using System.Text;
using System.Text.Json.Nodes;

using PowerOps.Core.Deploy;

namespace PowerOps.Core.Tests.Deploy;

public class ImportCallTests
{
    private const string Env = "00000000-0000-0000-0000-000000000001";
    private const string Url = "https://contoso-test.crm.dynamics.com";
    private const string PathHint = "must be a literal absolute path (no $, backtick, escape, ~ or wildcard)";
    private const string CantRead = "the command holds an import that the gate can't read";
    private const string Stray = "the command has a word the gate doesn't read";
    private const string EnvLine = "--environment must be an environment id or the https URL of a Dataverse host";
    private const string AnsiC = "the command has a $'...' word with an escape, which the gate can't read";
    private const string PaPush = "pa app push publishes an app that no plan holds";
    private const string PagesUpload = "pac pages upload uploads a site that no plan holds";
    private const string PagesCodeSite = "pac pages upload-code-site uploads a site that no plan holds";
    private const string PagesMigrate = "pac pages migrate-datamodel migrates a site's data model, which no plan holds";
    private const string Genpage = "pac model genpage upload publishes a page that no plan holds";
    private const string CopilotPush = "pac copilot push changes a live agent that no plan holds";
    private const string CopilotPublish = "pac copilot publish publishes an agent that no plan holds";
    private const string CopilotInit = "pac copilot init may import a solution that no plan holds";
    private const string ManageAgent = "copilot-studio's manage-agent push and publish change an agent that no plan holds";
    private const string SettingsApply = "powerops settings apply --apply imports a zip that no review check covers";
    private const string DeployApply = "powerops deploy apply --apply without --require-reviews imports even when no review covers the zip";
    private const string PlanCall = "powerops deploy apply /x/p.json --target " + Env;
    private const string SettingsCall = "powerops settings apply /x/s.zip --settings /x/s.json";

    // The manage-agent skill's own command form (skills-for-copilot-studio at 9c42d3a).
    private const string Bundle = "node ${CLAUDE_SKILL_DIR}/../../scripts/manage-agent.bundle.js";

    // Paths are built per OS, so the Windows leg of CI reads them as fully qualified.
    private static readonly string Dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "powerops-gate"));
    private static readonly string Zip = Path.Combine(Dir, "PowerOpsSeed.zip");
    private static readonly string Settings = Path.Combine(Dir, "PowerOpsSeed.settings.json");

    // Single quotes keep a Windows backslash literal.
    private static string Q(string path) => $"'{path}'";

    private static string Import(string extra = "") => $"pac solution import --path {Q(Zip)} --environment {Env} {extra}".TrimEnd();

    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());

    // The common fields and the order of the inputs recorded from Claude Code 2.1.312 and Codex 0.163.0 (ids and paths scrubbed).
    private static JsonObject Claude(string tool, JsonObject input, JsonObject? mcpServer = null)
    {
        var doc = new JsonObject
        {
            ["session_id"] = "<id>",
            ["transcript_path"] = "<path>",
            ["cwd"] = "<cwd>",
            ["scratchpad_dir"] = "<dir>",
            ["prompt_id"] = "<id>",
            ["permission_mode"] = "default",
            ["effort"] = new JsonObject { ["level"] = "medium" },
            ["hook_event_name"] = "PreToolUse",
            ["tool_name"] = tool,
            ["tool_input"] = input,
            ["tool_use_id"] = "<id>",
        };
        if (mcpServer is not null)
        {
            doc["mcp_server"] = mcpServer;
        }

        return doc;
    }

    private static JsonObject Codex(string tool, JsonObject input) => new()
    {
        ["session_id"] = "<id>",
        ["turn_id"] = "<id>",
        ["transcript_path"] = "<path>",
        ["cwd"] = "<cwd>",
        ["hook_event_name"] = "PreToolUse",
        ["model"] = "<model>",
        ["permission_mode"] = "default",
        ["tool_name"] = tool,
        ["tool_input"] = input,
        ["tool_use_id"] = "<id>",
    };

    private static ImportRead Bash(string command, string tool = "Bash") => ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = tool, ["tool_input"] = new JsonObject { ["command"] = command } }));

    private static ImportRead Mcp(JsonObject input, string tool = "mcp__pac-cli__solution_import") => ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = tool, ["tool_input"] = input }));

    private static JsonObject McpInput() => new() { ["--path"] = Zip, ["--environment"] = Env };

    private static ImportCall Checked(ImportRead read) => Assert.IsType<ImportRead.Check>(read).Call;

    private static ImportRead.Blocked Blocked(ImportRead read) => Assert.IsType<ImportRead.Blocked>(read);

    // ---- 1. not an import --------------------------------------------------------------------------------------

    [Theory]
    [InlineData("git status")]
    [InlineData("pac solution list")]
    [InlineData("pac auth list --verbose")]
    [InlineData("pac solution export --name Seed --path /x/seed.zip")]
    [InlineData("node \"${PLUGIN_ROOT}/scripts/solution-settings.js\" template --env https://contoso-test.crm.dynamics.com")]
    [InlineData("echo \"it's fine")]
    [InlineData("pa app run")]
    [InlineData("pa app list")]
    [InlineData("pa auth login")]
    [InlineData("npx pa app init")]
    [InlineData("npm run build")]
    [InlineData("pac pages download --path /x")]
    [InlineData("pac pages list")]
    [InlineData("pac pages download-code-site")]
    [InlineData("pac model genpage list")]
    [InlineData("pac model genpage download")]
    [InlineData("pac copilot pull")]
    [InlineData("pac copilot list")]
    [InlineData("pac copilot pack --publisher-prefix contoso")]
    [InlineData("pac copilot pack")]
    [InlineData("npm install @microsoft/power-apps")]
    [InlineData("npm install --save-dev @microsoft/power-apps-cli")]
    [InlineData("echo the app pushed")]
    [InlineData("cp -r public/pages uploads/")]
    [InlineData("git commit -m \"copilot publisher\"")]
    [InlineData("ls power-apps/src")]
    [InlineData("git add power-apps/")]
    // The manage-agent bundle's read-only verbs, as the word after the script.
    [InlineData(Bundle + " pull --workspace /x/agent")]
    [InlineData(Bundle + " clone --workspace /x/new --agent-id 00000000-0000-0000-0000-000000000002")]
    [InlineData(Bundle + " changes --workspace /x/agent")]
    [InlineData(Bundle + " validate --workspace /x/agent")]
    [InlineData(Bundle + " list-agents --no-owner")]
    [InlineData(Bundle + " list-envs")]
    [InlineData(Bundle + " auth --client-id 00000000-0000-0000-0000-000000000002")]
    [InlineData(Bundle + " pull --workspace /x/agent | jq .status")]
    [InlineData(Bundle + " pull --workspace /x/agent 2>&1")]
    // Known misses: a variable holds the flat CLI, and push alone can't be a marker (git push).
    [InlineData("$PA push")]
    [InlineData("PA=\"npx --no-install power-apps\"; $PA push")]
    // Known misses: the bundle's name is read as written, so a variable or a wildcard in it hides the call.
    [InlineData("node \"$S\" push")]
    [InlineData("node x/manage-agent.bundl?.js push")]
    // A certificate upload is a site-settings write of the preview group pac power-pages, unrouted by decision Q1 of M85.
    [InlineData("pac power-pages upload-certificate --path /x/cert.pfx")]
    [InlineData("pac Power-Pages Upload-Certificate --path /x/cert.pfx")]
    [InlineData("pac power-pages \"upload-certificate\" --path /x/cert.pfx")]
    [InlineData("pac \"power-pages\" \"upload-certificate\" --path /x/cert.pfx")]
    [InlineData("pac power-pages upload\"-certificate\" --path /x/cert.pfx")]
    [InlineData("pac power-pages \\\nupload-certificate --path /x")]
    [InlineData("pac power-pages upload\\\n-certificate --path /x")]
    [InlineData("pac power-pages upload-certificate --path /x/cert.pfx; echo ok")]
    [InlineData("pac power-pages create-website --name x")]
    [InlineData("X=1 pac org who")]
    [InlineData("PA_CLI_ORIGIN=x pa app run")]
    // The shell runs the word after the assignments, not pac or power-apps.cmd, and none of these holds a marker.
    [InlineData("X=\"$HOME\"/pac org who")]
    [InlineData("PATH=/x/pac $A $B")]
    [InlineData("X=/a/pac model genpage $Y")]
    [InlineData("PATH=/x/pac org who 'abc")]
    [InlineData("X=a/power-apps.cmd push")]
    // A known miss: the gate doesn't read inside a nested string, so a quote there hides settings apply, as it hides a pac import.
    [InlineData("bash -c 'powerops settings ap\"\"ply /x/s.zip --settings /x/s.json --apply'")]
    public void A_shell_command_without_an_import_is_not_one(string command)
    {
        Assert.IsType<ImportRead.NotImport>(Bash(command));
    }

    [Theory]
    [InlineData("Write")]
    [InlineData("mcp__other__list")]
    public void Another_tool_is_not_an_import(string tool)
    {
        Assert.IsType<ImportRead.NotImport>(ImportCall.Read(Bytes(Codex(tool, new JsonObject { ["command"] = "pac solution import" }))));
    }

    // ---- 2. readable forms -------------------------------------------------------------------------------------

    [Fact]
    public void Claudes_Bash_input_with_all_four_fields_gives_the_call()
    {
        var read = ImportCall.Read(Bytes(Claude("Bash", new JsonObject { ["command"] = $"pac solution import --environment {Env} --path {Q(Zip)} --settings-file {Q(Settings)} --stage-and-upgrade", ["description"] = "Import" })));

        Assert.Equal(new ImportCall(Zip, Env, Settings, true), Checked(read));
    }

    [Fact]
    public void Codexs_Bash_input_with_the_command_only_gives_the_call()
    {
        var read = ImportCall.Read(Bytes(Codex("Bash", new JsonObject { ["command"] = Import() })));

        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(read));
    }

    public static TheoryData<string> Readable()
    {
        var z = Q(Zip);
        var qz = $"\"{Zip}\"";
        return new()
        {
            { $"pac solution import --path {z} --environment {Env}" },
            { $"pac solution import -p {z} -env {Env}" },
            { $"pac solution import --path {z} --environment {Url} --async" },
            { $"pac solution import \\\n  --path {z} \\\n  --environment {Env} \\\n  --async --max-async-wait-time 30" },
            { $"pac solution import --path {qz} --environment '{Env}'" },
            { $"\"pac\" solution import --path {z} --environment {Env}" },
            { $"{Import()} | tail -5" },
            { $"{Import()} && pac solution list" },
            { $"{Import()} 2>&1" },
            { $"cd /work && {Import()} >&2" },
            { $"pac sol\"\"ution im''port --path {z} --environment {Env}" },
            { $"pac solution import --path {z} --environment {Env} -wt 30 -a" },
        };
    }

    [Theory]
    [MemberData(nameof(Readable))]
    public void A_readable_cli_import_gives_its_call(string command)
    {
        var call = Checked(Bash(command));

        Assert.Equal(Zip, call.Zip);
        Assert.True(call.Environment is Env or Url);
        Assert.Null(call.Settings);
        Assert.False(call.StageAndUpgrade);
    }

    [Theory]
    [InlineData("--stage-and-upgrade")]
    [InlineData("-up")]
    public void Stage_and_upgrade_by_either_name_is_read(string flag)
    {
        Assert.True(Checked(Bash(Import($"{flag} --settings-file {Q(Settings)}"))).StageAndUpgrade);
    }

    [Theory]
    [InlineData("mcp__plugin_dataverse_pac-cli__solution_import")]
    public void The_mcp_form_gives_its_call_under_any_servers_name(string tool)
    {
        var input = McpInput();
        input["--settings-file"] = Settings;
        input["--stage-and-upgrade"] = "true";
        input["--max-async-wait-time"] = "30";
        input["--async"] = "true";

        Assert.Equal(new ImportCall(Zip, Env, Settings, true), Checked(Mcp(input, tool)));
    }

    [Fact]
    public void The_mcp_server_key_is_ignored_beside_tool_input_and_inside_it()
    {
        var server = new JsonObject { ["name"] = "pac-cli", ["source"] = "dynamic" };
        var beside = ImportCall.Read(Bytes(Claude("mcp__pac-cli__solution_import", McpInput(), server)));
        var input = McpInput();
        input["mcp_server"] = server.DeepClone();
        var inside = ImportCall.Read(Bytes(Claude("mcp__pac-cli__solution_import", input)));
        var codex = ImportCall.Read(Bytes(Codex("mcp__pac_cli__solution_import", McpInput())));

        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(beside));
        Assert.Equal(Checked(beside), Checked(inside));
        Assert.Equal(Checked(beside), Checked(codex));
    }

    [Fact]
    public void A_quoted_windows_path_is_read_on_windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows paths");

        Assert.Equal(Zip, Checked(Bash($"pac solution import --path \"{Zip}\" --environment {Env}")).Zip);
    }

    // ---- 3. blocks ---------------------------------------------------------------------------------------------

    private static readonly string[] Unread =
    [
        "--force-overwrite", "--Path",
    ];

    public static TheoryData<string, string> Blocks()
    {
        var z = Q(Zip);
        var data = new TheoryData<string, string>
        {
            { $"pac solution import --environment {Env}", "--path is missing" },
            { $"pac solution import --path {z}", "--environment is missing" },
            { $"pac solution import --path {z} --environment", "--environment is missing" },
            { $"pac solution import --path ./solutions/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path $HOME/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path \"$ZIP\" --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path \"`x`\" --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path ~/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path *.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path /tmp/a\\ b.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path /tmp/$X/x.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path /tmp/*.zip --environment {Env}", $"--path {PathHint}" },
            { $"pac solution import --path \"/tmp/`x`.zip\" --environment {Env}", $"--path {PathHint}" },
            { "echo pac sol\"\"ution im''port", CantRead },
            { $"pac solution import --path //host/x.zip --environment {Env}", "--path must not be a network or device path" },
            { $"pac solution import --path {z} --environment {Env} --settings-file ./s.json", $"--settings-file {PathHint}" },
            { "pac sol\"\"ution im''port", "--path is missing" },
            { "pac $s import", CantRead },
            { $"$X/pac solution import --path {z} --environment {Env}", CantRead },
            { $"$'pac' $'solution' $'import' --path {z} --environment {Env}", CantRead },
            // Before \r\n a backslash escapes the \r, so the newline ends the command and --environment is not pac's.
            { $"pac solution import --path {z} \\\r\n  --environment {Env}", "--environment is missing" },
            { $"pac solution import --path {z} --environment {Env}\r", EnvLine },
            { $"{Import()} > log", Stray },
            { $"{Import()} < in", Stray },
            { $"{Import()} extra", Stray },
            { $"{Import()} 2>log", Stray },
            { $"pac solution import --path {z} --environment Test", EnvLine },
            { $"pac solution import --path {z} --environment http://contoso-test.crm.dynamics.com", EnvLine },
            { $"pac solution import --path {z} --environment -env", EnvLine },
            { $"pac solution import --path {z} --environment $E", EnvLine },
            { $"{Import()} -wt '$(x)'", "--max-async-wait-time must be a whole number of minutes" },
            { $"{Import()} -wt \"`x`\"", "--max-async-wait-time must be a whole number of minutes" },
            { $"{Import()} -wt soon", "--max-async-wait-time must be a whole number of minutes" },
            { $"{Import()} --path=/x.zip", "give --path and its value as separate words" },
            { $"{Import()} -p=/x.zip", "give --path and its value as separate words" },
            { $"{Import()} --settings-file=/s.json", "give --settings-file and its value as separate words" },
            { $"{Import()} --force-overwrite=true", "the gate doesn't read --force-overwrite" },
            { $"{Import()} && {Import()}", "the command holds more than one import" },
            { $"sh -c '{Import()}'", CantRead },
            { $"eval \"{Import()}\"", CantRead },
            { $"X=1 {Import()}", CantRead },
            { "echo \"pac solution import\"", CantRead },
            { "git commit -m \"docs: say how pac solution import works\"", CantRead },
            { "grep \"pac solution import\" docs", CantRead },
            { "grep ImportSolution src/", CantRead },
            { $"{Import()} 'unclosed", "the quotes are not closed" },
            { "echo it's; pac solution import", "the quotes are not closed" },
            { $"curl -X POST \"$URL/api/data/v9.2/ImportSolutionAsync\" -d @body.json", CantRead },
            { $"pac --verbose solution import --path {z} --environment {Env}", CantRead },
            // The always-blocked routes, with the skills' own command lines.
            { $"node \"${{PLUGIN_ROOT}}/scripts/solution-settings.js\" import --env {Url} --package {z} --settings {Q(Settings)}", "model-apps' solution-settings.js import builds its own import request, so its options can't be checked" },
            { $"node \"${{CLAUDE_PLUGIN_ROOT}}/scripts/encode-solution-file.js\" --zipPath {z}", "power-pages' import-solution sends the zip in a request body, so its zip and environment can't be checked" },
            { $"node \"${{CLAUDE_PLUGIN_ROOT}}/scripts/import-template-solution.js\" --zipPath {z} --envUrl {Url} --publishChanges true", "create-site's template import imports a template solution that no plan holds, and overwrites unmanaged customizations" },
            { $"pac pipeline deploy --environment {Env} --solutionName Seed --stageId {Env} --currentVersion 1.0.0.0 --newVersion 1.0.0.1 --wait", "pac pipeline deploy has no zip, and its target is a pipeline stage" },
            { $"pac package deploy --package {z} --environment {Url}", "pac package deploy imports a package that no plan holds" },
            { $"pac solution upgrade --solution-name Seed --environment {Env}", "pac solution upgrade applies a staged upgrade that no plan holds" },
            { "pac pcf push --publisher-prefix pub", "pac pcf push imports a solution that no plan holds" },
            { "pac application install --application-name x --environment " + Env, "pac application install installs an application that no plan holds" },
            { "pac code push --environment " + Env, "pac code push pushes an app that no plan holds" },
            { "pa app push", PaPush },
            { "pa app push --solution-id 00000000-0000-0000-0000-000000000002", PaPush },
            { "node_modules/.bin/pa app push", PaPush },
            { "./node_modules/.bin/pa app push", PaPush },
            { "npx pa app push", PaPush },
            { "npx --no-install pa app push --solution-id 00000000-0000-0000-0000-000000000002", PaPush },
            { "npx -y pa app push", PaPush },
            { "power-apps push", PaPush },
            { "npx power-apps push", PaPush },
            { "pac pages upload --path /x", PagesUpload },
            { "pac powerpages upload --path /x", PagesUpload },
            { "pac paportal upload --path /x", PagesUpload },
            { "pac pages upload-code-site --rootPath /x", PagesCodeSite },
            { "pac powerpages upload-code-site --rootPath /x", PagesCodeSite },
            { "pac paportal upload-code-site --rootPath /x", PagesCodeSite },
            { "pac pages migrate-datamodel --webSiteId x --mode all", PagesMigrate },
            { "pac powerpages migrate-datamodel --webSiteId x --mode all", PagesMigrate },
            { "pac paportal migrate-datamodel --webSiteId x --mode all", PagesMigrate },
            { "pac model genpage upload --app-id x --code-file /x.tsx", Genpage },
            { "pac copilot push", CopilotPush },
            { "pac copilot publish --bot x", CopilotPublish },
            { "pac copilot init", CopilotInit },
            { $"pac copilot init --environment {Env}", CopilotInit },
            { $"pac copilot init -env {Env} --agent-name x", CopilotInit },
            { "pac.exe copilot init", CopilotInit },
            { "sh -c 'pac copilot init'", CantRead },
            { "git commit -m \"pac copilot init\"", CantRead },
            { "sh -c 'pa app push'", CantRead },
            { "npx -p x pa app push", CantRead },
            { "pac --verbose pages upload", CantRead },
            { "pac model genpage $X", CantRead },
            { "git commit -m \"fix: gate pa app push\"", CantRead },
            { "grep \"pages upload\" docs", CantRead },
            { "bash -c \"pac copilot publish\"", CantRead },
            // Microsoft's code-apps skills set PA to the CLI they find and run $PA app push, or $PA push for the flat power-apps.
            { "PA=\"npx --no-install pa\"; $PA app push", CantRead },
            { "pa.cmd app push", PaPush },
            { "node_modules/.bin/power-apps.cmd push", PaPush },
            { "npx.cmd --no-install pa app push", PaPush },
            { Bundle + " push --workspace /x/agent", ManageAgent },
            { Bundle + " publish --workspace /x/agent --timeout 300000", ManageAgent },
            { Bundle + " push --force", ManageAgent },
            { "node.exe \"C:\\x\\manage-agent.bundle.js\" push", ManageAgent },
            // The bundle takes the first word that isn't an option or an option's value as its verb, so any other form is unread.
            { Bundle + " --workspace /x/agent push", CantRead },
            { Bundle + " \"\" push", CantRead },
            { Bundle + " \"$V\" --workspace /x/agent", CantRead },
            { Bundle + " Push", CantRead },
            { Bundle, CantRead },
            { Bundle + " delete", CantRead },
            { "node --no-warnings x/manage-agent.bundle.js push", CantRead },
            { "X=1 " + Bundle + " push", CantRead },
            { "sh -c '" + Bundle + " push'", CantRead },
            { "node x/manage-agent.bundle push", CantRead },
            { "grep -r manage-agent.bundle.js .", CantRead },
            // A pass holds no other marker: a backtick in a word runs a command.
            { $"{Bundle} pull --workspace \"`pac solution import --path {z} --environment {Env}`\"", CantRead },
            { $"node \"${{PLUGIN_ROOT}}/scripts/solution-settings.js\" template --env \"`pac solution import --path {z} --environment {Env}`\"", CantRead },
            // So does a $(...) in double quotes, which stays in the word; an unquoted one ends the segment and is read on its own.
            { $"node \"${{PLUGIN_ROOT}}/scripts/solution-settings.js\" template --env \"$(pac solution import --path {z} --environment {Env})\"", CantRead },
            { $"{Bundle} pull --workspace \"$(pac copilot push)\"", CantRead },
            // The route words are bare, so these block too while the hooks are on: reword them.
            { "git commit -m \"feat: add app push notifications\"", CantRead },
            { "git -C apps push origin main", CantRead },
            { "mv pages upload", CantRead },
            { $"pac catalog install --environment {Env} --catalog-item-id x", "pac catalog install installs a catalog item that no plan holds" },
            { $"pac app-management install-application-package --environment {Env} --unique-name x", "pac app-management install-application-package installs an application package that no plan holds" },
            { "sh -c 'pac catalog install --catalog-item-id x'", CantRead },
            { "bash -c \"pac app-management install-application-package --unique-name x\"", CantRead },
            { "sh -c \"pac pcf push\"", CantRead },
            { "sh -c 'pac application install --application-name x'", CantRead },
            { "bash -c \"pac code push\"", CantRead },
            { "pwsh -c 'Import-CrmSolution -Path x.zip'", CantRead },
            { "pac --verbose pcf push", CantRead },
            { $"pac solution import --path {z} --environment https://attacker.example", EnvLine },
            { $"pac solution import --path {z} --environment https://plan-env@other.crm.dynamics.com", EnvLine },
            { $"pac solution import --path {z} --environment https://x.crm.dynamics.com:444", EnvLine },
            // $'...' escapes can spell anything, so a word with one is not read, whatever else the command holds.
            { $"$'\\x70ac' $'\\x73olution' $'\\x69mport' --path {z} --environment {Env}", AnsiC },
            { "printf $'a\\nb'", AnsiC },
            // Every other spelling of the certificate upload stays unread, and a marker after it is read on its own.
            { "pac power-pages upload --path /x", CantRead },
            { "pac power-pages upload \\\n-certificate", CantRead },
            { "pac power-pages \"upload\" -certificate", CantRead },
            { "pac power-pages upload-certificate-x", CantRead },
            { "pac power-pages upload-code-site --rootPath /x", CantRead },
            { "pac power-pages migrate-datamodel --webSiteId x", CantRead },
            { "pac pages upload-certificate --path /x", CantRead },
            { "pac powerpages upload-certificate --path /x", CantRead },
            { "pac paportal upload-certificate --path /x", CantRead },
            { "pac \"power-pages upload-certificate\"", CantRead },
            { "pac \"power-pages upload-certificate\" --path /x", CantRead },
            { "pac power-pages $'upload-certificate'", CantRead },
            { "pac --verbose power-pages upload-certificate", CantRead },
            { "pac power-pages \\\r\nupload-certificate", CantRead },
            { $"pac power-pages upload-certificate --path \"`pac solution import --path {z} --environment {Env}`\"", CantRead },
            { "pac power-pages upload-certificate --path \"$(pac pages upload --path /x)\"", CantRead },
            { "pac PAGES Upload --path /x", PagesUpload },
            { "pac power-pages upload-certificate --path /x/cert.pfx && pac pages upload --path /x", PagesUpload },
            // M115: a call of PowerOps's own imports that isn't read strictly, and any other text that holds one, is unreadable.
            { $"{PlanCall} --apply --require-reviews --require-reviews false", CantRead },
            { $"{PlanCall} --apply --require-reviews --require-reviews:false", CantRead },
            { $"{PlanCall} --apply --require-reviews --require-reviews=false", CantRead },
            { $"{PlanCall} --require-reviews:false --apply --require-reviews", CantRead },
            { $"{PlanCall} --apply --require-reviews=false --require-reviews", CantRead },
            { $"{PlanCall} --apply --require-reviews \" false\"", CantRead },
            { $"{PlanCall} --apply --require-reviews 'false '", CantRead },
            { $"{PlanCall} --apply --require-reviews false", CantRead },
            { $"{PlanCall} --apply --require-reviews true", CantRead },
            { $"{PlanCall} --apply --out --require-reviews", CantRead },
            { $"{PlanCall} --apply --github-run --require-reviews", CantRead },
            { $"{PlanCall} --apply # --require-reviews", CantRead },
            { $"{PlanCall} --apply > --require-reviews", CantRead },
            { $"{PlanCall} --apply <<< --require-reviews", CantRead },
            { $"{PlanCall} --apply --require-reviews > /x/out false", CantRead },
            { $"{PlanCall} --apply --require-reviews 2> /x/err false", CantRead },
            { $"{PlanCall} --apply>/x/out.txt", CantRead },
            { $"{PlanCall} --apply>&2", CantRead },
            { $"{PlanCall} --apply&>/x/log", CantRead },
            { $"{PlanCall} --apply</dev/null", CantRead },
            { $"{SettingsCall} --apply>/x/out.txt", CantRead },
            { $"{SettingsCall} --apply>&2", CantRead },
            { $"{SettingsCall} --apply&>/x/log", CantRead },
            { $"{SettingsCall} --apply</dev/null", CantRead },
            { $"{PlanCall} --apply -- --require-reviews", CantRead },
            { $"{PlanCall} --apply=true --require-reviews", CantRead },
            { $"{PlanCall} --Apply --require-reviews", CantRead },
            { $"{PlanCall} --apply --require-reviews -h", CantRead },
            { $"{SettingsCall} ${{X:---app}}ly>/dev/null", CantRead },
            { $"{SettingsCall} --ap{{p,p}}ly>/dev/null", CantRead },
            { $"F=--apply; {SettingsCall} $F>&2", CantRead },
            { $"{PlanCall} --apply --out 2>&1 --require-reviews", CantRead },
            { $"{PlanCall} >/x/`id`.txt", CantRead },
            { $"powerops deploy apply --target {Env} --apply --require-reviews /x/p.json", CantRead },
            { $"powerops settings apply --apply /x/s.zip --settings /x/s.json", CantRead },
            { $"\"`pac solution import --path /x/s.zip --environment {Env}`/powerops\" deploy apply /x/p.json --target {Env}", CantRead },
            { $"dotnet run --project src/PowerOps.Cli -- deploy apply /x/p.json --target {Env} --apply --require-reviews", CantRead },
            { $"dotnet run --project src/PowerOps.Cli -- deploy apply /x/p.json --target {Env}", CantRead },
            { $"dotnet {SettingsCall}", CantRead },
            { $"env {SettingsCall} --apply", CantRead },
            { $"X=1 {PlanCall} --apply --require-reviews", CantRead },
            { $"sh -c \"{SettingsCall} --apply\"", CantRead },
            { "powerops \"$CMD\" apply", CantRead },
            { "powerops deploy \"$SUB\"", CantRead },
            { $"powerops deploy apply \"$PLAN\" --target {Env} --apply --require-reviews", CantRead },
            { $"powerops deploy apply \"$PLAN\" --target {Env}", CantRead },
            { $"powerops deploy apply ~/p.json --target {Env}", CantRead },
            { $"{PlanCall} $FLAGS", CantRead },
            { $"powerops deploy --format json apply /x/p.json --target {Env}", CantRead },
            { "grep -n \"settings apply\" docs/COMMANDS.md", CantRead },
            { "git commit -m \"fix: deploy apply\"", CantRead },
            { "terraform -chdir=deploy apply", CantRead },
            { "ls deploy apply", CantRead },
            // The program word must be literal, so a powerops call by a ~ or variable path is unreadable whatever it runs.
            { "~/.dotnet/tools/powerops analyze .", CantRead },
            { "\"$HOME/.dotnet/tools/powerops\" plan status docs/plans/x", CantRead },
        };

        foreach (var flag in Unread)
        {
            data.Add($"{Import()} {flag}", $"the gate doesn't read {flag}");
        }

        // A flag given twice: --path under one name, and every flag under its long then its short name.
        string[][] names = [["--path", "-p"], ["--environment", "-env"], ["--stage-and-upgrade", "-up"], ["--async", "-a"], ["--max-async-wait-time", "-wt"]];
        foreach (var pair in names)
        {
            var value = pair[0] switch { "--path" => z, "--environment" => Env, "--max-async-wait-time" => "5", _ => "" };
            foreach (var (first, second) in pair[0] == "--path" ? new[] { (pair[0], pair[0]), (pair[0], pair[1]) } : [(pair[0], pair[1])])
            {
                var others = pair[0] switch { "--path" => $"--environment {Env}", "--environment" => $"--path {z}", _ => $"--path {z} --environment {Env}" };
                data.Add($"pac solution import {others} {first} {value} {second} {value}", $"{pair[0]} is given twice");
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Blocks))]
    public void A_shell_block_gives_its_line(string command, string line)
    {
        Assert.Contains(line, Blocked(Bash(command)).Lines);
    }

    [Fact]
    public void A_reading_block_asks_for_a_readable_import_and_a_route_asks_for_the_plan()
    {
        Assert.True(Blocked(Bash("pac $s import")).Unreadable);
        Assert.False(Blocked(Bash("pac package deploy --package /x.zip --environment " + Url)).Unreadable);
    }

    // ---- 3. PowerOps's own imports (M115) -----------------------------------------------------------------------

    [Theory]
    [InlineData(SettingsCall + " --apply --target \"Contoso Test\"", SettingsApply)]
    [InlineData("powerops settings apply /x/s.zip --apply --settings /x/s.json --target \"Contoso Test\"", SettingsApply)]
    [InlineData(SettingsCall + " --target \"Contoso Test\" --apply", SettingsApply)]
    [InlineData("\"powerops\" settings apply /x/s.zip --settings /x/s.json --apply", SettingsApply)]
    [InlineData("/home/u/.dotnet/tools/powerops settings apply /x/s.zip --settings /x/s.json --apply", SettingsApply)]
    [InlineData("powerops.exe settings apply /x/s.zip --settings /x/s.json --apply", SettingsApply)]
    [InlineData("powerops Settings Apply /x/s.zip --settings /x/s.json --apply", SettingsApply)]
    [InlineData(PlanCall + " --apply", DeployApply)]
    [InlineData("powerops deploy apply /x/p.json --apply --target " + Env, DeployApply)]
    [InlineData("powerops deploy apply /x/p.json --format json --apply --out /x/r.json --target " + Env, DeployApply)]
    [InlineData("powerops deploy plan deploy --target " + Env + " --save deploy/powerops-plan.json && " + PlanCall + " --apply", DeployApply)]
    [InlineData("powerops deploy plan deploy --target " + Env + " --save deploy/powerops-plan.json; " + PlanCall + " --apply", DeployApply)]
    // The audit's G02 reading, which passed before M115.
    [InlineData("powerops deploy apply deploy/powerops-plan.json --target 00000000-0000-0000-0000-000000000001 --apply", DeployApply)]
    public void A_powerops_import_that_checks_no_review_gives_its_route(string command, string line)
    {
        foreach (var tool in new[] { "Bash", "Monitor" })
        {
            var blocked = Blocked(Bash(command, tool));

            Assert.Equal([line], blocked.Lines);
            Assert.False(blocked.Unreadable);
        }
    }

    [Theory]
    [InlineData(PlanCall + " --apply --require-reviews")]
    [InlineData("powerops deploy apply /x/p.json --target " + Env + " --require-reviews --apply")]
    [InlineData("powerops deploy apply /x/p.json --require-reviews --target " + Env + " --apply")]
    [InlineData(PlanCall + " --apply --require-reviews --format json")]
    [InlineData(PlanCall + " --apply --require-reviews --out /x/r.json")]
    [InlineData(PlanCall + " --apply --require-reviews 2>&1")]
    [InlineData(PlanCall + " --apply --require-reviews >&2")]
    [InlineData("powerops deploy apply \"/x/p.json\" --target " + Env + " --apply --require-reviews")]
    [InlineData(PlanCall)]
    [InlineData(PlanCall + " > /x/out.txt")]
    [InlineData(SettingsCall)]
    [InlineData(SettingsCall + " --target \"Contoso Test\"")]
    [InlineData("powerops settings validate src/ContosoApp --settings deploy/ContosoApp_managed.settings.json")]
    [InlineData("powerops deploy plan deploy --target " + Env + " --save deploy/powerops-plan.json")]
    [InlineData("powerops analyze .")]
    [InlineData("powerops plan status docs/plans/x")]
    [InlineData("powerops deploy gate \"$CLAUDE_PROJECT_DIR/deploy/powerops-plan.json\"")]
    [InlineData("powerops --version")]
    [InlineData("powerops")]
    public void A_guarded_powerops_import_a_dry_run_or_another_powerops_command_is_not_an_import(string command)
    {
        Assert.IsType<ImportRead.NotImport>(Bash(command));
        Assert.IsType<ImportRead.NotImport>(Bash(command, "Monitor"));
    }

    [Fact]
    public void A_flag_of_300_characters_with_a_control_character_is_printed_cleaned_and_cut()
    {
        var flag = "--" + new string('a', 296) + "\u0007bc";

        var line = Assert.Single(Blocked(Bash($"{Import()} {flag}")).Lines);

        Assert.Equal("the gate doesn't read " + flag[..100], line);
        Assert.DoesNotContain('\u0007', line);
    }

    [Fact]
    public void A_dv_solution_import_with_a_relative_path_and_an_unread_flag_gives_two_lines()
    {
        var lines = Blocked(Bash($"pac solution import --path ./solutions/x.zip --environment {Url} --async --activate-plugins")).Lines;

        Assert.Equal(2, lines.Count);
        Assert.Contains("the gate doesn't read --activate-plugins", lines);
        Assert.Contains($"--path {PathHint}", lines);
    }

    [Theory]
    [InlineData(@"\\host\share\x", true)]
    [InlineData("//host/share/x", true)]
    [InlineData(@"/\host\share\x", true)]
    [InlineData(@"\/host/share/x", true)]
    [InlineData(@"\\?\C:\x", true)]
    [InlineData(@"\\.\pipe\x", true)]
    [InlineData("/tmp/x", false)]
    [InlineData(@"/", false)]
    [InlineData(@"\x", false)]
    [InlineData(@"C:\x", false)]
    public void A_network_or_device_path_is_two_slashes_or_backslashes_in_any_mix(string value, bool expected)
    {
        Assert.Equal(expected, ImportCall.IsNetworkPath(value));
    }

    [Fact]
    public void A_posix_path_with_a_nul_is_not_a_literal_path()
    {
        Assert.Contains($"--path {PathHint}", Blocked(Bash($"pac solution import --path '{Zip}\0' --environment {Env}")).Lines);
    }

    [Fact]
    public void Windows_forms_that_are_not_fully_qualified_or_name_a_network_or_device_are_blocked_on_windows()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows paths");

        Assert.Contains($"--path {PathHint}", Blocked(Bash($"pac solution import --path /c/work/x.zip --environment {Env}")).Lines);
        Assert.Contains($"--path {PathHint}", Blocked(Bash($"pac solution import --path C:\\work\\x.zip --environment {Env}")).Lines);
        foreach (var path in new[] { @"\\host\share\x.zip", @"\\.\CON" })
        {
            var input = McpInput();
            input["--path"] = path;
            Assert.Contains("--path must not be a network or device path", Blocked(Mcp(input)).Lines);
        }

        // The ? of the \\?\ prefix is outside the MCP path characters, so that check blocks it first.
        var prefixed = McpInput();
        prefixed["--path"] = @"\\?\C:\x.zip";
        Assert.Contains(Blocked(Mcp(prefixed)).Lines, l => l.StartsWith("--path must hold only letters", StringComparison.Ordinal));
    }

    [Fact]
    public void An_import_in_powershell_is_blocked_whatever_its_form()
    {
        Assert.Contains("imports are read only in Bash commands, not in PowerShell", Blocked(Bash(Import(), "PowerShell")).Lines);
        Assert.Contains("imports are read only in Bash commands, not in PowerShell", Blocked(Bash("& pac.exe $args", "PowerShell")).Lines);
        Assert.IsType<ImportRead.NotImport>(Bash("Get-ChildItem", "PowerShell"));
    }

    [Fact]
    public void Two_names_of_a_value_flag_each_need_a_value()
    {
        Assert.Contains("--path is missing", Blocked(Bash($"pac solution import --environment {Env} -p")).Lines);
    }

    // ---- 3. MCP blocks -----------------------------------------------------------------------------------------

    [Fact]
    public void Missing_mcp_arguments_are_named()
    {
        var lines = Blocked(Mcp([])).Lines;

        Assert.Contains("--path is missing", lines);
        Assert.Contains("--environment is missing", lines);
    }

    [Theory]
    [InlineData("--max-async-wait-time", "soon", "--max-async-wait-time must be a whole number of minutes")]
    [InlineData("--max-async-wait-time", "$(x)", "--max-async-wait-time must be a whole number of minutes")]
    [InlineData("--environment", "Test", EnvLine)]
    [InlineData("--environment", "http://contoso-test.crm.dynamics.com", EnvLine)]
    [InlineData("--stage-and-upgrade", "false", "--stage-and-upgrade must be \"true\"")]
    [InlineData("--async", "--force-overwrite", "--async must be \"true\" or \"false\"")]
    [InlineData("--async", "", "--async must be \"true\" or \"false\"")]
    [InlineData("--environment", "https://attacker.example", EnvLine)]
    [InlineData("--environment", "https://plan-env@other.crm.dynamics.com", EnvLine)]
    [InlineData("--environment", "https://x.crm.dynamics.com:444", EnvLine)]
    [InlineData("--environment", "https://x.crm.dynamics.com/path", EnvLine)]
    [InlineData("--path", "/tmp/z.zip --force-overwrite", "--path must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--path", "/tmp/z.zip\t--force-overwrite", "--path must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--path", "-/tmp/z.zip", "--path must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--path", "/tmp/z.zip\n", "--path must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--settings-file", "/tmp/s.json\n--force-overwrite", "--settings-file must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--settings-file", "/tmp/s.json\t--force-overwrite", "--settings-file must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--settings-file", "--force-overwrite", "--settings-file must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--path", "/tmp/z.zip;touch${IFS}/tmp/pwn", "--path must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--path", "/tmp/$(id).zip", "--path must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--path", "/tmp/z.zip\"--force-overwrite", "--path must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--settings-file", "/tmp/s'.json", "--settings-file must hold only letters, digits and / \\ : . _ - ~, and not start with '-'")]
    [InlineData("--force-overwrite", "true", "the gate doesn't read --force-overwrite")]
    [InlineData("-p", "x", "the gate doesn't read -p")]
    [InlineData("--path", "./solutions/x.zip", $"--path {PathHint}")]
    [InlineData("--path", "//host/x.zip", "--path must not be a network or device path")]
    [InlineData("--path", @"/\host\share\x.zip", "--path must not be a network or device path")]
    [InlineData("--path", @"\/host/share/x.zip", "--path must not be a network or device path")]
    [InlineData("--path", @"\\.\CON", "--path must not be a network or device path")]
    [InlineData("--settings-file", @"/\host\s.json", "--settings-file must not be a network or device path")]
    [InlineData("--settings-file", "s.json", $"--settings-file {PathHint}")]
    public void An_mcp_argument_the_gate_cannot_pass_gives_its_line(string key, string value, string line)
    {
        var input = McpInput();
        input[key] = value;

        Assert.Contains(line, Blocked(Mcp(input)).Lines);
    }

    [Theory]
    [InlineData(@"C:\Users\RUNNER~1\AppData\Local\Temp\Seed_1_0_0_1-managed.zip")]
    [InlineData("/home/zoë/Résumé/Seed.zip")]
    public void An_mcp_path_of_letters_digits_and_path_signs_passes_the_character_check(string path)
    {
        var input = McpInput();
        input["--path"] = path;

        Assert.DoesNotContain((Mcp(input) as ImportRead.Blocked)?.Lines ?? [], l => l.Contains("must hold only", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("false")]
    public void The_mcp_async_switch_is_true_or_false(string value)
    {
        var input = McpInput();
        input["--async"] = value;

        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(Mcp(input)));
    }

    [Theory]
    [InlineData(Url)]
    [InlineData(Url + "/")]
    public void The_environment_may_be_a_dataverse_host_url_in_either_form(string env)
    {
        var input = McpInput();
        input["--environment"] = env;

        Assert.Equal(env, Checked(Mcp(input)).Environment);
        Assert.Equal(env, Checked(Bash($"pac solution import --path {Q(Zip)} --environment {env}")).Environment);
    }

    [Fact]
    public void An_mcp_number_value_blocks()
    {
        var input = McpInput();
        input["--max-async-wait-time"] = 30;

        Assert.Contains("--max-async-wait-time must be a string", Blocked(Mcp(input)).Lines);
    }

    [Fact]
    public void An_mcp_tool_input_that_is_a_string_blocks()
    {
        var read = ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = "mcp__pac_cli__solution_import", ["tool_input"] = "{bad" }));

        Assert.Equal(["the hook input has no tool_input object"], Blocked(read).Lines);
    }

    [Fact]
    public void A_key_that_appears_twice_blocks()
    {
        var json = "{\"tool_name\":\"mcp__pac-cli__solution_import\",\"tool_input\":{\"--path\":" + System.Text.Json.JsonSerializer.Serialize(Zip)
            + $",\"--environment\":\"{Env}\",\"--environment\":\"{Url}\"}}}}";

        Assert.Equal(["a key appears twice in the hook input"], Blocked(ImportCall.Read(Encoding.UTF8.GetBytes(json))).Lines);
    }

    // ---- Monitor, PowerShell and $'...' ------------------------------------------------------------------------

    [Fact]
    public void Monitor_runs_a_shell_command_like_Bash()
    {
        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(Bash(Import(), "Monitor")));
        Assert.Contains(CantRead, Blocked(Bash("sh -c 'pac solution import'", "Monitor")).Lines);
        Assert.Contains("pac pcf push imports a solution that no plan holds", Blocked(Bash("pac pcf push", "Monitor")).Lines);
        Assert.Contains(PaPush, Blocked(Bash("npx pa app push", "Monitor")).Lines);
        Assert.IsType<ImportRead.NotImport>(Bash("tail -f /var/log/syslog", "Monitor"));
    }

    [Fact]
    public void A_monitor_ws_watch_with_no_command_is_not_an_import()
    {
        var input = new JsonObject { ["ws"] = new JsonObject { ["url"] = "wss://example.test/feed" }, ["description"] = "Watch" };

        Assert.IsType<ImportRead.NotImport>(ImportCall.Read(Bytes(Claude("Monitor", input))));
    }

    [Fact]
    public void A_monitor_command_that_is_not_a_string_blocks()
    {
        var input = new JsonObject { ["command"] = 7, ["ws"] = new JsonObject() };

        Assert.Equal(["the hook input has no command string"], Blocked(ImportCall.Read(Bytes(Claude("Monitor", input)))).Lines);
    }

    [Theory]
    [InlineData("pac solution import --path /x.zip")]
    [InlineData("Import-CrmSolution -InFile x.zip")]
    [InlineData("& 'C:\\Tools\\pac.exe' auth list")]
    [InlineData("pa app push")]
    [InlineData("node x/manage-agent.bundle.js pull")]
    [InlineData("pac power-pages upload-certificate --path /x/cert.pfx")]
    [InlineData(SettingsCall + " --apply")]
    [InlineData(SettingsCall)]
    [InlineData(PlanCall + " --apply --require-reviews")]
    [InlineData(PlanCall)]
    public void A_pac_or_import_command_in_the_powershell_tool_is_blocked(string command)
    {
        Assert.Contains("imports are read only in Bash commands, not in PowerShell", Blocked(Bash(command, "PowerShell")).Lines);
    }

    [Fact]
    public void An_ansi_c_word_with_a_backslash_is_unreadable_and_one_without_is_as_before()
    {
        var z = Q(Zip);

        Assert.True(Blocked(Bash($"echo $'a\\x41'")).Unreadable);
        Assert.DoesNotContain(AnsiC, Blocked(Bash($"$'pac' solution import --path {z} --environment {Env}")).Lines);
        Assert.Contains(CantRead, Blocked(Bash($"$'pac' solution import --path {z} --environment {Env}")).Lines);
        Assert.IsType<ImportRead.NotImport>(Bash("echo $'a' 'b\\c'"));
    }

    [Fact]
    public void The_new_always_blocked_routes_ask_for_the_plan_not_a_rewrite()
    {
        foreach (var command in new[] { "pac pcf push", "pac application install --application-name x", "pac code push", "pac catalog install --catalog-item-id x", "pac app-management install-application-package --unique-name x", "pa app push", "npx --no-install pa app push", "power-apps push", "pac pages upload --path /x", "pac paportal upload-code-site --rootPath /x", "pac pages migrate-datamodel --webSiteId x", "pac model genpage upload --app-id x", "pac copilot push", "pac copilot publish --bot x", "pac copilot init", $"pac copilot init --environment {Env}", Bundle + " push --workspace /x/agent", Bundle + " publish --workspace /x/agent" })
        {
            Assert.False(Blocked(Bash(command)).Unreadable);
        }
    }

    // ---- 3. assignments and the certificate upload --------------------------------------------------------------

    [Theory]
    [InlineData("PA_CLI_ORIGIN=plugin/claude-code pa app push --solution-id x")]
    [InlineData("PA_CLI_ORIGIN+=x pa app push")]
    [InlineData("A=1 B=2 pa app push")]
    [InlineData("PA_CLI_ORIGIN=\"plugin/claude code\" pa app push")]
    [InlineData("PA_CLI_ORIGIN=x npx --no-install pa app push")]
    [InlineData("PA_CLI_ORIGIN=x node_modules/.bin/pa app push")]
    [InlineData("PA_CLI_ORIGIN=x power-apps push")]
    [InlineData("X=a/npx pa app push")]
    // The code-apps hook's own form (power-platform-skills at 4076781).
    [InlineData("if [ \"${PA_CLI_ORIGIN+x}\" != x ]; then export PA_CLI_ORIGIN='plugin/claude-code'; fi\npa app push --solution-id x")]
    public void A_pa_app_push_led_by_literal_assignments_gives_its_route(string command)
    {
        var blocked = Blocked(Bash(command));

        Assert.False(blocked.Unreadable);
        Assert.Equal([PaPush], blocked.Lines);
    }

    public static TheoryData<string> AssignedImports()
    {
        var import = $"solution import --path {Q(Zip)} --environment {Env}";
        return new()
        {
            { $"X=\"pac {import}\"" },
            { $"X=\"$(pac {import})\"" },
            { "PA_CLI_ORIGIN=$X pa app push" },
            { "PA_CLI_ORIGIN=`x` pa app push" },
            { $"PA_CLI_ORIGIN=\"$(pac {import})\" pa app push" },
            { $"PA_CLI_ORIGIN=\"`pac {import}`\" pa app push" },
            { "\"PA_CLI_ORIGIN=x\" pa app push" },
            { "env PA_CLI_ORIGIN=x pa app push" },
            { $"X=1 pac {import}" },
            { $"PATH=/tmp/x/pac {import}" },
            { $"PATH+=/tmp/x/pac {import}" },
            { $"PATH\\\n=/tmp/x/pac {import}" },
            { $"PA\\\nTH=/tmp/x/pac {import}" },
            { "X=a/pa app push" },
            { "X=a/power-apps push" },
            { "PA_CLI_ORIGIN=x pac app-management install-application-package --unique-name x" },
            { "PA_CLI_ORIGIN=x pac power-pages upload-certificate --path /x" },
            { "X=a/" + Bundle + " pull --workspace /x/agent" },
            { "PATH=/x/pac pcf push" },
            { "X=a/" + Bundle + " push" },
        };
    }

    [Theory]
    [MemberData(nameof(AssignedImports))]
    public void An_import_or_route_led_by_an_assignment_is_unreadable(string command)
    {
        var blocked = Blocked(Bash(command));

        Assert.True(blocked.Unreadable);
        Assert.Contains(CantRead, blocked.Lines);
    }

    [Theory]
    [InlineData("PATH=/x/pac pcf push")]
    [InlineData("X=a/" + Bundle + " push")]
    public void A_pac_or_node_route_led_by_an_assignment_loses_its_route_line(string command)
    {
        Assert.Equal([CantRead], Blocked(Bash(command)).Lines);
    }

    [Fact]
    public void A_certificate_upload_then_a_readable_import_gives_the_import()
    {
        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(Bash($"pac power-pages upload-certificate --path /x/cert.pfx; {Import()}")));
    }

    [Fact]
    public void A_certificate_upload_with_an_unquoted_command_substitution_reads_the_import_in_it()
    {
        Assert.Equal(new ImportCall(Zip, Env, null, false), Checked(Bash($"pac power-pages upload-certificate --path $({Import()})")));
    }

    [Fact]
    public void A_certificate_upload_with_an_unclosed_quote_says_only_that()
    {
        Assert.Equal(["the quotes are not closed"], Blocked(Bash("pac power-pages upload-certificate --path '/x/cert.pfx")).Lines);
    }

    // ---- 3. the write tools of pac's MCP server ----------------------------------------------------------------

    private const string AppPackage = "pac app-management install-application-package installs an application package that no plan holds";

    public static TheoryData<string, string, string> WriteTools() => new()
    {
        { "app-management_install-application-package", "app_management_install_application_package", AppPackage },
        { "app_management_install_application_package", "app_management_install_application_package", AppPackage },
        { "pages_upload", "pages_upload", PagesUpload },
        { "pages_upload-code-site", "pages_upload_code_site", PagesCodeSite },
        { "pages_upload_code_site", "pages_upload_code_site", PagesCodeSite },
        { "copilot_publish", "copilot_publish", CopilotPublish },
        { "model_genpage_upload", "model_genpage_upload", Genpage },
    };

    [Theory]
    [MemberData(nameof(WriteTools))]
    public void A_pac_mcp_write_tool_is_blocked_with_its_route_under_any_servers_name(string claudeTool, string codexTool, string line)
    {
        var input = new JsonObject { ["--path"] = "/x" };
        var reads = new[]
        {
            ImportCall.Read(Bytes(Claude($"mcp__pac-cli__{claudeTool}", input))),
            ImportCall.Read(Bytes(Codex($"mcp__pac_cli__{codexTool}", input.DeepClone().AsObject()))),
            ImportCall.Read(Bytes(Claude($"mcp__plugin_power-pages_pac-cli__{claudeTool}", input.DeepClone().AsObject()))),
            ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = $"mcp__pac-cli__{claudeTool}" })),
            ImportCall.Read(Bytes(new JsonObject { ["tool_name"] = $"mcp__pac-cli__{claudeTool}", ["tool_input"] = "text" })),
        };

        foreach (var read in reads)
        {
            var blocked = Blocked(read);
            Assert.Equal([line], blocked.Lines);
            Assert.False(blocked.Unreadable);
        }
    }

    [Theory]
    [InlineData("mcp__pac-cli__pages_download")]
    [InlineData("mcp__pac-cli__pages_list")]
    [InlineData("mcp__pac-cli__model_genpage_list")]
    [InlineData("mcp__pac-cli__copilot_publish_x")]
    [InlineData("mcp__pac-cli__app-management_get-application-package-install-status")]
    [InlineData("mcp__pac-cli__app-management_install-application-package_x")]
    [InlineData("mcp__pac-cli__power-pages_upload-certificate")]
    [InlineData("mcp__pac-cli__power-pages_delete-website")]
    public void Another_pac_mcp_tool_is_not_an_import(string tool)
    {
        Assert.IsType<ImportRead.NotImport>(ImportCall.Read(Bytes(Claude(tool, new JsonObject { ["--path"] = "/x" }))));
    }

    // ---- 3. input ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("not json", "the hook input is not JSON")]
    [InlineData("[]", "the hook input has no tool_name")]
    [InlineData("{}", "the hook input has no tool_name")]
    [InlineData("""{"tool_name":7}""", "the hook input has no tool_name")]
    [InlineData("""{"tool_name":"Bash","tool_input":{}}""", "the hook input has no command string")]
    [InlineData("""{"tool_name":"Bash","tool_input":{"command":7}}""", "the hook input has no command string")]
    [InlineData("""{"tool_name":"Bash"}""", "the hook input has no tool_input object")]
    [InlineData("""{"tool_name":"\ud800"}""", "the hook input is not JSON")]
    public void An_unreadable_input_blocks_with_the_reading_guidance(string json, string line)
    {
        var blocked = Blocked(ImportCall.Read(Encoding.UTF8.GetBytes(json)));

        Assert.Equal([line], blocked.Lines);
        Assert.True(blocked.Unreadable);
    }

    [Fact]
    public void An_input_over_1_mb_blocks_whatever_the_tool()
    {
        var json = Encoding.UTF8.GetBytes("{\"tool_name\":\"Write\",\"tool_input\":{\"content\":\"" + new string('x', ImportCall.MaxInputBytes) + "\"}}");

        Assert.Equal(["the hook input is larger than 1 MB"], Blocked(ImportCall.Read(json)).Lines);
    }

    [Fact]
    public void A_name_is_cleaned_and_cut_to_100_characters()
    {
        Assert.Equal("a b", ImportCall.Printable("a\u0007b"));
        Assert.Equal(100, ImportCall.Printable(new string('x', 300)).Length);
    }

    [Fact]
    public void Printable_does_not_cut_a_surrogate_pair_in_half()
    {
        Assert.Equal(new string('x', 99), ImportCall.Printable(new string('x', 99) + "\U0001F600"));
    }
}