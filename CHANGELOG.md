# Changelog

## 0.1.0 (2026-11-07)


### ⚠ BREAKING CHANGES

* deploy plan names the deletions it can't check, and two diff fixes (M53) ([#100](https://github.com/Chrome71/PowerOps/issues/100))
* the authoring agent cannot approve its own deploy (M52) ([#91](https://github.com/Chrome71/PowerOps/issues/91))
* agent lines in diff (schemaVersion 2), gate pac copilot init, agent hand-off (M49) ([#88](https://github.com/Chrome71/PowerOps/issues/88))

### Features

* **action:** flow tests as a merge gate (M15) ([#37](https://github.com/Chrome71/PowerOps/issues/37)) ([5790114](https://github.com/Chrome71/PowerOps/commit/5790114ad450a5839ac9a8b0739048cf330c1ac2))
* add CI integration (M2) ([#9](https://github.com/Chrome71/PowerOps/issues/9)) ([548cb11](https://github.com/Chrome71/PowerOps/commit/548cb11e31c6905f9609e5d8b9c275c2cf6c6dd1))
* add opt-in --redact to analyze and diff, with output and storage guidance (M61) ([#103](https://github.com/Chrome71/PowerOps/issues/103)) ([41847c4](https://github.com/Chrome71/PowerOps/commit/41847c4a4b82add695a75e08387bfd0fffd5ee47))
* add planning skills and plan commands (M3) ([#11](https://github.com/Chrome71/PowerOps/issues/11)) ([170ae2e](https://github.com/Chrome71/PowerOps/commit/170ae2e06b7361593a43428e515006f054838886))
* add product references, the sketch offer and the Power BI branch (M42) ([#80](https://github.com/Chrome71/PowerOps/issues/80)) ([3597789](https://github.com/Chrome71/PowerOps/commit/3597789bb2a56202286f0e19ba607a46e02c91de))
* add the agent base (M4) ([#13](https://github.com/Chrome71/PowerOps/issues/13)) ([e24d097](https://github.com/Chrome71/PowerOps/commit/e24d097338ec2a130da8d549674cf1b63e8a5457))
* add the flow size and nested-loop rules (M43) ([#81](https://github.com/Chrome71/PowerOps/issues/81)) ([27f85e5](https://github.com/Chrome71/PowerOps/commit/27f85e52b5441e20985c26698a5461198dc8f968))
* add the Power Pages reader and boundary rules (M45) ([#82](https://github.com/Chrome71/PowerOps/issues/82)) ([a8f2bf4](https://github.com/Chrome71/PowerOps/commit/a8f2bf4b032f93f25059c7070433cbb08a57627f))
* add the security-role rules PO032 to PO034 (M60) ([#105](https://github.com/Chrome71/PowerOps/issues/105)) ([f22c002](https://github.com/Chrome71/PowerOps/commit/f22c00264d5207c33aeab64c2ae0d3739bab1cfb))
* add the size brake to the plan skills (M41) ([#79](https://github.com/Chrome71/PowerOps/issues/79)) ([ff04d60](https://github.com/Chrome71/PowerOps/commit/ff04d602baa61cb4dd84e70960cced7e4cd2cdb7))
* agent lines in diff (schemaVersion 2), gate pac copilot init, agent hand-off (M49) ([#88](https://github.com/Chrome71/PowerOps/issues/88)) ([4c9ceee](https://github.com/Chrome71/PowerOps/commit/4c9ceee3f8e12cc19c4a46f7a33a63960182e407))
* **analyze:** offline analyze command for zip and XML-unpacked solutions (M0 + M1) ([#5](https://github.com/Chrome71/PowerOps/issues/5)) ([2dc9258](https://github.com/Chrome71/PowerOps/commit/2dc92587fb8528ecbba63d27790fcef55390fcad))
* check review reports against the solution they name (M62) ([#104](https://github.com/Chrome71/PowerOps/issues/104)) ([707c577](https://github.com/Chrome71/PowerOps/commit/707c577574ff00f6456c63e275b58e12b6d0e542))
* **cli:** deploy_apply behind the user's typed yes (M19) ([#41](https://github.com/Chrome71/PowerOps/issues/41)) ([bec64c9](https://github.com/Chrome71/PowerOps/commit/bec64c986d5039fd633cb66daf6f5ea8e3ecb6f3))
* **cli:** powerops deploy gate before Microsoft's imports (M21) ([#56](https://github.com/Chrome71/PowerOps/issues/56)) ([c703306](https://github.com/Chrome71/PowerOps/commit/c70330638d2fde40b0537af06d8952988058bcf5))
* **cli:** powerops mcp with the read-only tools (M18) ([#40](https://github.com/Chrome71/PowerOps/issues/40)) ([592eb2a](https://github.com/Chrome71/PowerOps/commit/592eb2adbe96c197f0e59191d5247ac7e6349b05))
* **cli:** powerops test runs offline flow tests (M14) ([#35](https://github.com/Chrome71/PowerOps/issues/35)) ([69a9e8a](https://github.com/Chrome71/PowerOps/commit/69a9e8a7f7c7fc42734cd4db5f58280b68ac8456))
* **core:** simulate float, the hour, minute and second adders, time-zone conversion, startOfDay and dayOfWeek (M59) ([#102](https://github.com/Chrome71/PowerOps/issues/102)) ([39fecb9](https://github.com/Chrome71/PowerOps/commit/39fecb92f649fb25f60bb0927d8685c3001533c7))
* **core:** simulate formatDateTime, int and the format argument of addDays (M58 part 1) ([#94](https://github.com/Chrome71/PowerOps/issues/94)) ([431df9b](https://github.com/Chrome71/PowerOps/commit/431df9b67d2ef6c23bf85fd9b839abe956f2ce49))
* deploy plan names the deletions it can't check, and two diff fixes (M53) ([#100](https://github.com/Chrome71/PowerOps/issues/100)) ([f098f1a](https://github.com/Chrome71/PowerOps/commit/f098f1a7e0860f363c2c8b75a6d3bf7be5ac39bc))
* **deploy:** add powerops deploy apply and the Dev to Test promotion (M10) ([#25](https://github.com/Chrome71/PowerOps/issues/25)) ([a90c9a8](https://github.com/Chrome71/PowerOps/commit/a90c9a8e5d12eee17c20f34a32b31d1359742911))
* **deploy:** add powerops deploy plan and the PR comment (M9) ([#24](https://github.com/Chrome71/PowerOps/issues/24)) ([874aedc](https://github.com/Chrome71/PowerOps/commit/874aedc188446c5aa8e17581c7d3b98af46fceaa))
* **diff:** add powerops diff (M6) ([#16](https://github.com/Chrome71/PowerOps/issues/16)) ([e2fd5a2](https://github.com/Chrome71/PowerOps/commit/e2fd5a22c07b99c85de1c03dac08d2d7781427d0))
* **drift:** add powerops drift (M8) ([#23](https://github.com/Chrome71/PowerOps/issues/23)) ([f428e87](https://github.com/Chrome71/PowerOps/commit/f428e876fbcdbee96e265b788fc70cea0708469a))
* gate the manage-agent skill's push and publish ([#89](https://github.com/Chrome71/PowerOps/issues/89)) ([a894772](https://github.com/Chrome71/PowerOps/commit/a894772a833263c1a545da90a9d42f88c367d205))
* give the review roles diff limits and a six-line report header, frozen through an eval (M55) ([#101](https://github.com/Chrome71/PowerOps/issues/101)) ([0fb8f3d](https://github.com/Chrome71/PowerOps/commit/0fb8f3db931286a7b4e467f91cdeb1ea97f3ca30))
* identify every build and ship releases with a checksum the Action can pin (M56) ([#95](https://github.com/Chrome71/PowerOps/issues/95)) ([d535ec2](https://github.com/Chrome71/PowerOps/commit/d535ec2453ffa34218de6dd78a0a1ec1ca8ed4e2))
* M36, the rule and drift notice from the M35 tenant run ([#68](https://github.com/Chrome71/PowerOps/issues/68)) ([4c4cfa1](https://github.com/Chrome71/PowerOps/commit/4c4cfa1c563a83c5de5c15f7153ba2259871b223))
* M37, a deploy plan notice for a same-version zip that differs ([#70](https://github.com/Chrome71/PowerOps/issues/70)) ([155b944](https://github.com/Chrome71/PowerOps/commit/155b9445663f147d036bd577db88a30db6070024))
* **plugins:** register powerops mcp in both plugins (M20) ([#42](https://github.com/Chrome71/PowerOps/issues/42)) ([b4abce4](https://github.com/Chrome71/PowerOps/commit/b4abce4cb00421c7017076de7b53e53a7ce063c7))
* read canvas formulas from .msapp files and add PO018 (M44) ([#83](https://github.com/Chrome71/PowerOps/issues/83)) ([471af36](https://github.com/Chrome71/PowerOps/commit/471af3605d66bfadcd49392c371c98504d61c7a3))
* read code app dependencies and add PO026 (M47) ([#85](https://github.com/Chrome71/PowerOps/issues/85)) ([b0d88a1](https://github.com/Chrome71/PowerOps/commit/b0d88a1bfc4f6c08355c9d43b6cff24cdad4651d))
* read Copilot Studio agents and add PO027 to PO031 (M48) ([#86](https://github.com/Chrome71/PowerOps/issues/86)) ([d113fc9](https://github.com/Chrome71/PowerOps/commit/d113fc96da85059dba004aa46ea86f9d0cdd40a7))
* read model-driven app modules and site maps and add PO024 and PO025 (M46) ([#84](https://github.com/Chrome71/PowerOps/issues/84)) ([c4dcf52](https://github.com/Chrome71/PowerOps/commit/c4dcf52286256a2cb764dc0eb589ce217a1b3788))
* read the Dataverse Git integration YAML layout (M5) ([#14](https://github.com/Chrome71/PowerOps/issues/14)) ([9cad29d](https://github.com/Chrome71/PowerOps/commit/9cad29d8a39189f6847c7390daf505316c14d040))
* read the new Copilot Studio export layout (M51) ([#99](https://github.com/Chrome71/PowerOps/issues/99)) ([a75673e](https://github.com/Chrome71/PowerOps/commit/a75673e43aa7b872b9c2449a1f413fdaa58ea7c6))
* say what diff, analyze and drift did not compare or check (M50) ([#96](https://github.com/Chrome71/PowerOps/issues/96)) ([9b3fb0d](https://github.com/Chrome71/PowerOps/commit/9b3fb0d202f415502b3f481aec1981fdd801951f))
* scaffold PowerOps CLI, CI and release pipeline ([a038942](https://github.com/Chrome71/PowerOps/commit/a038942b200dffce369ff9c6358f32a54a4720a4))
* **settings:** add offline settings generate and validate (M7, part 1) ([#15](https://github.com/Chrome71/PowerOps/issues/15)) ([a1cb550](https://github.com/Chrome71/PowerOps/commit/a1cb55030123671ce81f768e29d5d9d13dbdff52))
* **settings:** add settings apply, the live half of M7 ([#18](https://github.com/Chrome71/PowerOps/issues/18)) ([714f7da](https://github.com/Chrome71/PowerOps/commit/714f7da3929f38e6cc9cef44db5a42cd624c543a))
* ship the Verifier and Security reviewer roles ([#76](https://github.com/Chrome71/PowerOps/issues/76)) ([2b25cc3](https://github.com/Chrome71/PowerOps/commit/2b25cc3c41d06cc2b782fc4032379f0cc8d136d3))
* **skills:** plugin marketplaces for Claude Code and Codex (M13) ([#30](https://github.com/Chrome71/PowerOps/issues/30)) ([2866aa6](https://github.com/Chrome71/PowerOps/commit/2866aa621f0bd0d5ccf0352cc91a3ce14c478209))
* the authoring agent cannot approve its own deploy (M52) ([#91](https://github.com/Chrome71/PowerOps/issues/91)) ([7a006df](https://github.com/Chrome71/PowerOps/commit/7a006df6d2c9d14ffdf8d5954cc75d79cf9c4a29))
* the build skills never delete in a tenant, keep a write journal and mark who answered (M54) ([#93](https://github.com/Chrome71/PowerOps/issues/93)) ([9fcbd45](https://github.com/Chrome71/PowerOps/commit/9fcbd457c96c99b640ae0652de42ff4bedd5e86b))


### Bug Fixes

* analyze and diff input limits (M25) ([#46](https://github.com/Chrome71/PowerOps/issues/46)) ([d1fb482](https://github.com/Chrome71/PowerOps/commit/d1fb482c7b41f2fbb29f61b96a04ce440d568600))
* **analyze:** reject deeply nested XML, name bad flow files, dedupe trailing slashes ([#10](https://github.com/Chrome71/PowerOps/issues/10)) ([c1a9995](https://github.com/Chrome71/PowerOps/commit/c1a9995615a0247ddc7a57210c43368c4b12627f))
* **ci:** M33 grading follow-ups (pin-drift hashes only, stronger guard tests, no temp leak) ([#65](https://github.com/Chrome71/PowerOps/issues/65)) ([4988088](https://github.com/Chrome71/PowerOps/commit/4988088c2d91ee5777a3105d00ba3b5ba34bc1fd))
* **cli:** deploy_apply and MCP inputs (M24) ([#57](https://github.com/Chrome71/PowerOps/issues/57)) ([6f449f1](https://github.com/Chrome71/PowerOps/commit/6f449f103b6b7c527f2f3dcd729d21e459993993))
* **core:** follow a recorded cloud run in the flow simulator (M58 part 2) ([#97](https://github.com/Chrome71/PowerOps/issues/97)) ([3675054](https://github.com/Chrome71/PowerOps/commit/367505419f07546289f25e900a6495bd0b3bd254))
* **core:** write paths through SafeWrite (M28) ([#58](https://github.com/Chrome71/PowerOps/issues/58)) ([20f1ed5](https://github.com/Chrome71/PowerOps/commit/20f1ed53f97330cd1ccd87e72d09f1e8507b86c6))
* **diff:** accept YAML layout root folders and name YAML canvas apps ([#17](https://github.com/Chrome71/PowerOps/issues/17)) ([53fe2e4](https://github.com/Chrome71/PowerOps/commit/53fe2e41a1286bfcc427f0360e66db0487ec222c))
* **diff:** ignore operationMetadataId; record BUILD-PLAN verification ([#19](https://github.com/Chrome71/PowerOps/issues/19)) ([b632b2d](https://github.com/Chrome71/PowerOps/commit/b632b2d2f48d16d3d8bedcb65c1ae4a8c24cc6ca))
* **drift:** leave model-driven apps, sitemaps and app settings out of the id comparison ([#29](https://github.com/Chrome71/PowerOps/issues/29)) ([d932d5e](https://github.com/Chrome71/PowerOps/commit/d932d5e78140c3c1617ece8557de6d8f9245056c))
* errors, exit codes and refusals (M27) ([#49](https://github.com/Chrome71/PowerOps/issues/49)) ([4569772](https://github.com/Chrome71/PowerOps/commit/4569772e34b43878e077b86e84dc92e49f742cea))
* gate pac's site, page, app and agent writes, and stop PO005's false agent warnings ([#78](https://github.com/Chrome71/PowerOps/issues/78)) ([1c3a2ce](https://github.com/Chrome71/PowerOps/commit/1c3a2ce3f2dae7c6bcfd435d6819e7748e7a3fed))
* harden Markdown output, folder reading, the flow simulator and the Action against untrusted input ([#109](https://github.com/Chrome71/PowerOps/issues/109)) ([46eb4e4](https://github.com/Chrome71/PowerOps/commit/46eb4e4310587a8092286bcaecaa7637f55d200c))
* label agent tool mode changes in diff, write SARIF paths outside the folder as file URIs, explain deep flow tests ([#112](https://github.com/Chrome71/PowerOps/issues/112)) ([67dca91](https://github.com/Chrome71/PowerOps/commit/67dca915bacddd4bf1fa45c9202c9864f17c5d43))
* M33, fixes from the review of 2026-10-29 ([#63](https://github.com/Chrome71/PowerOps/issues/63)) ([391a6e0](https://github.com/Chrome71/PowerOps/commit/391a6e07420c816526e218eebc4fc92a905c153a))
* M34, fixes from the Windows live run of 2026-10-30 ([#66](https://github.com/Chrome71/PowerOps/issues/66)) ([248c832](https://github.com/Chrome71/PowerOps/commit/248c8328b7ee92333d9354dd2faf21be31ac2530))
* name a missing read privilege as the likely cause of no layer rows, and list the plan app's read-only role ([#92](https://github.com/Chrome71/PowerOps/issues/92)) ([b8ad904](https://github.com/Chrome71/PowerOps/commit/b8ad9049cc52d8dcb2284f581d6f470384e0120e))
* output and Action hygiene (M30) ([#50](https://github.com/Chrome71/PowerOps/issues/50)) ([7c3bd62](https://github.com/Chrome71/PowerOps/commit/7c3bd6209d53fc3ba20da4496417fbc7cf5f580d))
* plan gate discovery and deploy pre-checks ([#110](https://github.com/Chrome71/PowerOps/issues/110)) ([c658238](https://github.com/Chrome71/PowerOps/commit/c658238cc394ca0a76ab17b28ba5127fa229297b))
* plan, settings, baseline and simulator limits (M26) ([#47](https://github.com/Chrome71/PowerOps/issues/47)) ([6159788](https://github.com/Chrome71/PowerOps/commit/6159788f97e8da552b4da278d196b10bfbfbc4b3))
* quick security fixes (M22) ([#44](https://github.com/Chrome71/PowerOps/issues/44)) ([e83df9d](https://github.com/Chrome71/PowerOps/commit/e83df9d034b7021312b040c0550377c16d56fea9))
* **test:** pin JsonSchema.Net to 8.0.5 (MIT) in the core tests ([#22](https://github.com/Chrome71/PowerOps/issues/22)) ([1be0ffd](https://github.com/Chrome71/PowerOps/commit/1be0ffd01e15ccbab8695fa5de778307f3d6abbe))


### Performance Improvements

* parse flows once per analyze, harden ZipGuard's Zip64 count and remove duplicate code ([#113](https://github.com/Chrome71/PowerOps/issues/113)) ([c2302a6](https://github.com/Chrome71/PowerOps/commit/c2302a61294e303f5b1f12328df7e369f57ce8fa))
