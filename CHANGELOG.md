# Changelog

## 0.1.0 (2026-10-31)


### Features

* **action:** flow tests as a merge gate (M15) ([#37](https://github.com/Chrome71/PowerOps/issues/37)) ([5790114](https://github.com/Chrome71/PowerOps/commit/5790114ad450a5839ac9a8b0739048cf330c1ac2))
* add CI integration (M2) ([#9](https://github.com/Chrome71/PowerOps/issues/9)) ([548cb11](https://github.com/Chrome71/PowerOps/commit/548cb11e31c6905f9609e5d8b9c275c2cf6c6dd1))
* add planning skills and plan commands (M3) ([#11](https://github.com/Chrome71/PowerOps/issues/11)) ([170ae2e](https://github.com/Chrome71/PowerOps/commit/170ae2e06b7361593a43428e515006f054838886))
* add the agent base (M4) ([#13](https://github.com/Chrome71/PowerOps/issues/13)) ([e24d097](https://github.com/Chrome71/PowerOps/commit/e24d097338ec2a130da8d549674cf1b63e8a5457))
* **analyze:** offline analyze command for zip and XML-unpacked solutions (M0 + M1) ([#5](https://github.com/Chrome71/PowerOps/issues/5)) ([2dc9258](https://github.com/Chrome71/PowerOps/commit/2dc92587fb8528ecbba63d27790fcef55390fcad))
* **cli:** deploy_apply behind the user's typed yes (M19) ([#41](https://github.com/Chrome71/PowerOps/issues/41)) ([bec64c9](https://github.com/Chrome71/PowerOps/commit/bec64c986d5039fd633cb66daf6f5ea8e3ecb6f3))
* **cli:** powerops deploy gate before Microsoft's imports (M21) ([#56](https://github.com/Chrome71/PowerOps/issues/56)) ([c703306](https://github.com/Chrome71/PowerOps/commit/c70330638d2fde40b0537af06d8952988058bcf5))
* **cli:** powerops mcp with the read-only tools (M18) ([#40](https://github.com/Chrome71/PowerOps/issues/40)) ([592eb2a](https://github.com/Chrome71/PowerOps/commit/592eb2adbe96c197f0e59191d5247ac7e6349b05))
* **cli:** powerops test runs offline flow tests (M14) ([#35](https://github.com/Chrome71/PowerOps/issues/35)) ([69a9e8a](https://github.com/Chrome71/PowerOps/commit/69a9e8a7f7c7fc42734cd4db5f58280b68ac8456))
* **deploy:** add powerops deploy apply and the Dev to Test promotion (M10) ([#25](https://github.com/Chrome71/PowerOps/issues/25)) ([a90c9a8](https://github.com/Chrome71/PowerOps/commit/a90c9a8e5d12eee17c20f34a32b31d1359742911))
* **deploy:** add powerops deploy plan and the PR comment (M9) ([#24](https://github.com/Chrome71/PowerOps/issues/24)) ([874aedc](https://github.com/Chrome71/PowerOps/commit/874aedc188446c5aa8e17581c7d3b98af46fceaa))
* **diff:** add powerops diff (M6) ([#16](https://github.com/Chrome71/PowerOps/issues/16)) ([e2fd5a2](https://github.com/Chrome71/PowerOps/commit/e2fd5a22c07b99c85de1c03dac08d2d7781427d0))
* **drift:** add powerops drift (M8) ([#23](https://github.com/Chrome71/PowerOps/issues/23)) ([f428e87](https://github.com/Chrome71/PowerOps/commit/f428e876fbcdbee96e265b788fc70cea0708469a))
* M36, the rule and drift notice from the M35 tenant run ([#68](https://github.com/Chrome71/PowerOps/issues/68)) ([4c4cfa1](https://github.com/Chrome71/PowerOps/commit/4c4cfa1c563a83c5de5c15f7153ba2259871b223))
* **plugins:** register powerops mcp in both plugins (M20) ([#42](https://github.com/Chrome71/PowerOps/issues/42)) ([b4abce4](https://github.com/Chrome71/PowerOps/commit/b4abce4cb00421c7017076de7b53e53a7ce063c7))
* read the Dataverse Git integration YAML layout (M5) ([#14](https://github.com/Chrome71/PowerOps/issues/14)) ([9cad29d](https://github.com/Chrome71/PowerOps/commit/9cad29d8a39189f6847c7390daf505316c14d040))
* scaffold PowerOps CLI, CI and release pipeline ([a038942](https://github.com/Chrome71/PowerOps/commit/a038942b200dffce369ff9c6358f32a54a4720a4))
* **settings:** add offline settings generate and validate (M7, part 1) ([#15](https://github.com/Chrome71/PowerOps/issues/15)) ([a1cb550](https://github.com/Chrome71/PowerOps/commit/a1cb55030123671ce81f768e29d5d9d13dbdff52))
* **settings:** add settings apply, the live half of M7 ([#18](https://github.com/Chrome71/PowerOps/issues/18)) ([714f7da](https://github.com/Chrome71/PowerOps/commit/714f7da3929f38e6cc9cef44db5a42cd624c543a))
* **skills:** plugin marketplaces for Claude Code and Codex (M13) ([#30](https://github.com/Chrome71/PowerOps/issues/30)) ([2866aa6](https://github.com/Chrome71/PowerOps/commit/2866aa621f0bd0d5ccf0352cc91a3ce14c478209))


### Bug Fixes

* analyze and diff input limits (M25) ([#46](https://github.com/Chrome71/PowerOps/issues/46)) ([d1fb482](https://github.com/Chrome71/PowerOps/commit/d1fb482c7b41f2fbb29f61b96a04ce440d568600))
* **analyze:** reject deeply nested XML, name bad flow files, dedupe trailing slashes ([#10](https://github.com/Chrome71/PowerOps/issues/10)) ([c1a9995](https://github.com/Chrome71/PowerOps/commit/c1a9995615a0247ddc7a57210c43368c4b12627f))
* **ci:** M33 grading follow-ups (pin-drift hashes only, stronger guard tests, no temp leak) ([#65](https://github.com/Chrome71/PowerOps/issues/65)) ([4988088](https://github.com/Chrome71/PowerOps/commit/4988088c2d91ee5777a3105d00ba3b5ba34bc1fd))
* **cli:** deploy_apply and MCP inputs (M24) ([#57](https://github.com/Chrome71/PowerOps/issues/57)) ([6f449f1](https://github.com/Chrome71/PowerOps/commit/6f449f103b6b7c527f2f3dcd729d21e459993993))
* **core:** write paths through SafeWrite (M28) ([#58](https://github.com/Chrome71/PowerOps/issues/58)) ([20f1ed5](https://github.com/Chrome71/PowerOps/commit/20f1ed53f97330cd1ccd87e72d09f1e8507b86c6))
* **diff:** accept YAML layout root folders and name YAML canvas apps ([#17](https://github.com/Chrome71/PowerOps/issues/17)) ([53fe2e4](https://github.com/Chrome71/PowerOps/commit/53fe2e41a1286bfcc427f0360e66db0487ec222c))
* **diff:** ignore operationMetadataId; record BUILD-PLAN verification ([#19](https://github.com/Chrome71/PowerOps/issues/19)) ([b632b2d](https://github.com/Chrome71/PowerOps/commit/b632b2d2f48d16d3d8bedcb65c1ae4a8c24cc6ca))
* **drift:** leave model-driven apps, sitemaps and app settings out of the id comparison ([#29](https://github.com/Chrome71/PowerOps/issues/29)) ([d932d5e](https://github.com/Chrome71/PowerOps/commit/d932d5e78140c3c1617ece8557de6d8f9245056c))
* errors, exit codes and refusals (M27) ([#49](https://github.com/Chrome71/PowerOps/issues/49)) ([4569772](https://github.com/Chrome71/PowerOps/commit/4569772e34b43878e077b86e84dc92e49f742cea))
* M33, fixes from the review of 2026-10-29 ([#63](https://github.com/Chrome71/PowerOps/issues/63)) ([391a6e0](https://github.com/Chrome71/PowerOps/commit/391a6e07420c816526e218eebc4fc92a905c153a))
* M34, fixes from the Windows live run of 2026-10-30 ([#66](https://github.com/Chrome71/PowerOps/issues/66)) ([248c832](https://github.com/Chrome71/PowerOps/commit/248c8328b7ee92333d9354dd2faf21be31ac2530))
* output and Action hygiene (M30) ([#50](https://github.com/Chrome71/PowerOps/issues/50)) ([7c3bd62](https://github.com/Chrome71/PowerOps/commit/7c3bd6209d53fc3ba20da4496417fbc7cf5f580d))
* plan, settings, baseline and simulator limits (M26) ([#47](https://github.com/Chrome71/PowerOps/issues/47)) ([6159788](https://github.com/Chrome71/PowerOps/commit/6159788f97e8da552b4da278d196b10bfbfbc4b3))
* quick security fixes (M22) ([#44](https://github.com/Chrome71/PowerOps/issues/44)) ([e83df9d](https://github.com/Chrome71/PowerOps/commit/e83df9d034b7021312b040c0550377c16d56fea9))
* **test:** pin JsonSchema.Net to 8.0.5 (MIT) in the core tests ([#22](https://github.com/Chrome71/PowerOps/issues/22)) ([1be0ffd](https://github.com/Chrome71/PowerOps/commit/1be0ffd01e15ccbab8695fa5de778307f3d6abbe))
