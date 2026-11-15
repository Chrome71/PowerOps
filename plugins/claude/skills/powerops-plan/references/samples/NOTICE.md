# Samples: notice

This folder holds three design samples, one per product, with a map file each, and `kit.css`, the starting point of a new sketch file.

- `canvas-repairs-desk.html` and `canvas-repairs-desk.map.md`: a canvas app.
- `pages-tenant-repairs.html` and `pages-tenant-repairs.map.md`: a Power Pages site.
- `powerbi-repairs-performance.html` and `powerbi-repairs-performance.map.md`: a Power BI report.

## What they are

The samples are the level, not a template: copy their discipline, never their layout, content or brand. The owner passed them on 2026-11-11, and they ship unchanged, byte for byte.

`kit.css` is the kit the samples embed, with two changes: the disabled button text is the token `--fg-disabled` instead of a raw hex, and it holds the browser frame rules (`.frame.browser` and `.chrome`) copied from the Power Pages sample.

## What they lack

The samples predate the sketch file rules of `powerops-prototype` section 3, so `powerops plan status` would report these on them:

- no `route:` first comment;
- no `data-lead` in any frame;
- no `data-for` on the states;
- phone frames named as screens of their own: `<screen> (phone)` in the canvas and Power Pages samples, `Overview (phone layout)` in the Power BI sample;
- six state tiles in each sample, not four per screen: for five frames in the canvas and Power Pages samples, for two in the Power BI sample;
- no map in the plan-folder form: each map is named after its sample, not `mockups/prototype.map.md`;
- one raw hex outside `:root`: the embedded kit's `.btn[disabled]` text, `#BDBDBD`.

Their maps refer to files that are not shipped: all three to `../report.md` and `../notes/`, and the canvas and Power Pages maps to `_build/STORY.md`.

The Power Pages map holds a full Learn link with its scheme. A plan's map writes a Learn page by its path instead, as `learn.microsoft.com/power-pages/...`, because the scan of a plan folder flags `http`.

## Icons

The samples' inline SVG sprites hold icon paths from Fluent System Icons (github.com/microsoft/fluentui-system-icons), under the MIT licence:

```
MIT License

Copyright (c) 2020 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
