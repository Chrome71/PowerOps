#!/usr/bin/env python3
"""Measure and picture a PowerOps sketch with a headless Chrome or Chromium.

  measure.py measure <sketch>                       print the measured faults as JSON
  measure.py shoot <sketch> <out-folder> [--width N]  write a full-page PNG and one PNG per frame and state tile
  measure.py side-by-side <a> <b> <c> <out.png>     the three lead frames at one width, joined at half scale
  measure.py --self-test                            check the script on a built-in page

Python 3.8 or later, standard library only. The sketch is never changed: a copy with a measuring script added is kept in a
temp folder, which is removed, and served to Chrome from a loopback server that answers only that page, is Chrome's proxy for
every request and allows no script but the measuring one, so the page can show no other file. Every option takes --chrome <path>.
Exit codes: 0 done, 1 self-test failed, 2 usage error, 3 unreadable input, 77 no Chrome, or Chrome can't render.
"""
import argparse
import ast
import base64
import colorsys
import hashlib
import http.client
import http.server
import json
import math
import os
import re
import secrets
import shutil
import socket
import struct
import subprocess
import sys
import tempfile
import threading
import time
import zlib
from pathlib import Path

sys.dont_write_bytecode = True

TILE_W, TILE_H = 1400, 1200
CHROME_NAMES = ["google-chrome-stable", "google-chrome", "chromium", "chromium-browser", "chrome", "msedge"]
MARK = "powerops-measure"
CSP = "default-src 'none'; style-src 'unsafe-inline'; img-src data:; font-src data:; script-src 'nonce-%s'"


class Unrenderable(Exception):
    """Chrome is missing, or it ran and gave no result (exit 77)."""


class Unreadable(Exception):
    """The input can't be read (exit 3)."""


# The measuring script added to the copy. It writes its JSON, base64-encoded, into a <pre> that --dump-dom prints.
MEASURE_JS = r"""
(function () {
  var cache = new Map();
  function cs(e) { var s = cache.get(e); if (!s) { s = getComputedStyle(e); cache.set(e, s); } return s; }
  function rgba(v) {
    var m = /rgba?\(\s*([\d.]+)[ ,]+([\d.]+)[ ,]+([\d.]+)(?:\s*[,/]\s*([\d.]+)(%?))?\s*\)/.exec(v || "");
    if (!m) return null;
    var a = m[4] === undefined ? 1 : parseFloat(m[4]) / (m[5] ? 100 : 1);
    return [parseFloat(m[1]), parseFloat(m[2]), parseFloat(m[3]), a];
  }
  function over(top, below) {
    var a = top[3]; return [top[0] * a + below[0] * (1 - a), top[1] * a + below[1] * (1 - a), top[2] * a + below[2] * (1 - a), 1];
  }
  function lum(c) {
    function ch(v) { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); }
    return 0.2126 * ch(c[0]) + 0.7152 * ch(c[1]) + 0.0722 * ch(c[2]);
  }
  function ratio(a, b) { var x = lum(a), y = lum(b); return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05); }
  function hex(c) { return "#" + c.slice(0, 3).map(function (v) { return ("0" + Math.round(v).toString(16)).slice(-2); }).join("").toUpperCase(); }
  function desc(e) {
    var c = (e.getAttribute("class") || "").trim().split(/\s+/).filter(Boolean).slice(0, 2);
    return e.tagName.toLowerCase() + (c.length ? "." + c.join(".") : "");
  }
  function clip(t) { t = t.replace(/\s+/g, " ").trim(); return t.length > 40 ? t.slice(0, 37) + "..." : t; }
  function painted(s) {
    var bg = rgba(s.backgroundColor);
    var border = ["Top", "Right", "Bottom", "Left"].some(function (k) {
      var c = rgba(s["border" + k + "Color"]);
      return parseFloat(s["border" + k + "Width"]) > 0 && s["border" + k + "Style"] !== "none" && c && c[3] > 0;
    });
    return { bg: bg && bg[3] > 0 ? bg : null, image: s.backgroundImage !== "none", border: border, shadow: s.boxShadow !== "none" };
  }
  // The background under element e: composited ancestor colours, or null over an image or gradient.
  function background(e) {
    var layers = [];
    for (var n = e; n && n.nodeType === 1; n = n.parentElement) {
      var s = cs(n);
      if (s.backgroundImage !== "none") return null;
      var c = rgba(s.backgroundColor);
      if (c && c[3] > 0) { layers.push(c); if (c[3] >= 1) break; }
    }
    var out = [255, 255, 255, 1];
    for (var i = layers.length - 1; i >= 0; i--) out = over(layers[i], out);
    return out;
  }
  // For SVG text: the last solid shape drawn before it in its svg whose box holds the text's centre.
  function svgBackground(t, cx, cy) {
    var svg = t.ownerSVGElement; if (!svg) return undefined;
    var shapes = svg.querySelectorAll("rect,circle,ellipse,path,polygon"), found;
    for (var i = 0; i < shapes.length; i++) {
      var sh = shapes[i];
      if (!(sh.compareDocumentPosition(t) & Node.DOCUMENT_POSITION_FOLLOWING)) continue;
      var r = sh.getBoundingClientRect();
      if (cx < r.left || cx > r.right || cy < r.top || cy > r.bottom) continue;
      var s = cs(sh), f = rgba(s.fill);
      if (!f || s.fill === "none") continue;
      var a = f[3] * parseFloat(s.fillOpacity) * parseFloat(s.opacity);
      found = a >= 1 ? f : null;
    }
    return found;
  }
  var probe = document.createElement("canvas").getContext("2d");
  var generic = ["serif", "sans-serif", "monospace", "cursive", "fantasy", "system-ui", "ui-serif", "ui-sans-serif", "ui-monospace", "ui-rounded", "math", "emoji", "fangsong"];
  var fontCache = {};
  function hasFont(name) {
    if (name in fontCache) return fontCache[name];
    var sample = "mmmmmmmmmmlli1WQ@#0Ow", ok = false;
    ["monospace", "serif", "sans-serif"].forEach(function (base) {
      probe.font = "72px " + base; var w0 = probe.measureText(sample).width;
      probe.font = '72px "' + name.replace(/"/g, "") + '", ' + base;
      if (probe.measureText(sample).width !== w0) ok = true;
    });
    return (fontCache[name] = ok);
  }
  function rendered(stack) {
    var names = stack.split(",").map(function (f) { return f.trim().replace(/^["']|["']$/g, ""); }).filter(Boolean);
    for (var i = 0; i < names.length; i++) {
      if (generic.indexOf(names[i].toLowerCase()) >= 0 || hasFont(names[i])) return { asked: names[0], rendered: names[i], fallback: i > 0 };
    }
    return { asked: names[0] || "", rendered: "the browser default", fallback: true };
  }
  function inside(r, b, tol) { return r.left >= b.left - tol && r.right <= b.right + tol && r.top >= b.top - tol && r.bottom <= b.bottom + tol; }
  function hits(a, b) { return Math.min(a.right, b.right) - Math.max(a.left, b.left) > 1 && Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 1; }
  function opacity(e, stop) { var o = 1; for (var n = e; n && n !== stop; n = n.parentElement) o *= parseFloat(cs(n).opacity); return o; }

  var all = Array.prototype.slice.call(document.querySelectorAll("[data-screen]"));
  function kindOf(e) {
    var k = ["desktop", "phone", "browser", "report"].filter(function (c) { return e.classList.contains(c); });
    return k.length ? k[0] : "none";
  }
  var kinds = all.map(kindOf);
  var siteOnly = kinds.indexOf("browser") >= 0 && kinds.indexOf("desktop") < 0 && kinds.indexOf("report") < 0;
  var SHAPES = "rect,circle,ellipse,path,polygon,polyline,line,text,tspan,use";
  var reportFile = kinds.indexOf("report") >= 0 && kinds.indexOf("desktop") < 0 && kinds.indexOf("browser") < 0;
  var sx = window.scrollX, sy = window.scrollY;
  function pageBox(r) { return { x: Math.round(r.left + sx), y: Math.round(r.top + sy), w: Math.round(r.width), h: Math.round(r.height) }; }
  function nestedTile(e, root) { var t = e.closest("[data-state]"); return !!t && t !== root && root.contains(t); }
  // A report's svgs are charts, and so are those of its phone layout (G10).
  function reportLike(kind) { return kind === "report" || (kind === "phone" && reportFile); }
  function svgClass(svg, r, root, kind, colourCount) {
    if (r.width <= 24.01 && r.height <= 24.01) return "icon";
    var h = svg.closest("header"), inHeader = !!h && root.contains(h);
    if (reportLike(kind) && !inHeader) return "chart";
    var t = svg.querySelector("text");
    // A bar, a bullet or a meter is drawn of rectangles and lines only; a drawing has paths, polygons or circles.
    var names = drawn(svg, 0);
    if ((t && t.textContent.trim()) || (names.length && names.every(function (n) { return n === "rect" || n === "line"; }))) return "chart";
    return colourCount === 1 && Math.max(r.width, r.height) <= 48.01 ? "icon" : "imagery";  // a one-colour glyph up to 48 px is an icon
  }
  // The tag names of the shapes an svg draws, through its <use> references.
  function drawn(scope, depth) {
    var names = [];
    Array.prototype.forEach.call(scope.querySelectorAll(SHAPES), function (sh) {
      var n = sh.tagName.toLowerCase(), target;
      if (n !== "use") { names.push(n); return; }
      target = document.getElementById((sh.getAttribute("href") || sh.getAttribute("xlink:href") || "").replace(/^#/, ""));
      if (target && depth < 3) names = names.concat(target.matches(SHAPES) ? [target.tagName.toLowerCase()] : [], drawn(target, depth + 1));
    });
    return names;
  }
  // Every colour drawn in root but root's own fill and border (the frame is a surface): text, fills, strokes (borders,
  // outlines, unblurred box shadows), the same of ::before and ::after, and the fills and strokes of each svg's shapes.
  // Python drops the neutrals and adds the hue group.
  function colourUses(root, atoms, kind, skip) {
    var uses = {}, order = [];
    function add(c, under, role, element, extra) {
      if (!c || c[3] <= 0) return;
      var shown = c[3] < 1 && under ? over(c, under) : c, h = hex(shown), key = h + "|" + role + "|" + element + (extra ? "|" + extra.box.x + "," + extra.box.y : "");
      var u = uses[key];
      if (!u) {
        u = uses[key] = { hex: h, use: { role: role, element: element, count: 0 } };
        if (extra) { u.use.svg = extra.svg; u.use.box = extra.box; u.use.background = extra.background; }
        order.push(key);
      }
      u.use.count++;
    }
    function fade(c, o) { return c ? [c[0], c[1], c[2], c[3] * o] : null; }
    function strokes(s, o, under, element) {
      var seen = {};
      ["Top", "Right", "Bottom", "Left"].forEach(function (k) {
        var c = rgba(s["border" + k + "Color"]);
        if (parseFloat(s["border" + k + "Width"]) > 0 && s["border" + k + "Style"] !== "none" && s["border" + k + "Style"] !== "hidden" && c && !seen[hex(c) + c[3]]) {
          seen[hex(c) + c[3]] = true; add(fade(c, o), under, "stroke", element);
        }
      });
      if (s.outlineStyle && s.outlineStyle !== "none" && parseFloat(s.outlineWidth) > 0) add(fade(rgba(s.outlineColor), o), under, "stroke", element);
      // A box shadow with no blur draws a ring or a bar; a blurred one is a shadow, not a colour of the interface.
      (s.boxShadow === "none" ? "" : s.boxShadow).split(/,(?![^(]*\))/).forEach(function (layer) {
        var c = rgba(layer), len = layer.replace(/rgba?\([^)]*\)/, "").match(/-?[\d.]+px/g) || [];
        if (c && len.length >= 3 && parseFloat(len[2]) === 0) add(fade(c, o), under, "stroke", element);
      });
    }
    atoms.forEach(function (a) {
      if (a.svg || a.hidden) return;
      var bg = background(a.el);
      add(fade(rgba(cs(a.el).color), opacity(a.el, null)), bg, "text", desc(a.el));
    });
    Array.prototype.forEach.call(root.querySelectorAll("*"), function (e) {
      if (e instanceof SVGElement || (skip && skip.contains(e))) return;
      var s = cs(e); if (s.visibility !== "visible" || s.display === "none") return;
      var r = e.getBoundingClientRect(), o = opacity(e, null); if (r.width < 1 || r.height < 1 || o === 0) return;
      var under = e.parentElement ? background(e.parentElement) : null, own = background(e), d = desc(e);
      add(fade(rgba(s.backgroundColor), o), under, "fill", d);
      strokes(s, o, own, d);
      ["::before", "::after"].forEach(function (p) {
        var ps = getComputedStyle(e, p);
        if (ps.content === "none" || ps.content === "normal" || ps.display === "none" || ps.visibility !== "visible") return;
        var po = o * parseFloat(ps.opacity);
        add(fade(rgba(ps.backgroundColor), po), own, "fill", d + p);
        strokes(ps, po, own, d + p);
      });
    });
    Array.prototype.forEach.call(root.querySelectorAll("svg"), function (svg) {
      if (svg.parentElement && svg.parentElement.closest("svg")) return;
      var s = cs(svg), r = svg.getBoundingClientRect();
      if (s.visibility !== "visible" || s.display === "none" || r.width < 1 || r.height < 1 || opacity(svg, null) === 0) return;
      var under = background(svg), got = [];
      function shapes(scope, use, ref, depth) {
        Array.prototype.forEach.call(scope.querySelectorAll(SHAPES), function (sh) {
          if (!use && sh.closest("defs,symbol,clipPath,mask,pattern,marker")) return;
          if (sh.tagName.toLowerCase() === "use") {
            var target = document.getElementById((sh.getAttribute("href") || sh.getAttribute("xlink:href") || "").replace(/^#/, ""));
            if (target && depth < 3) { shapes(target, use || sh, target, depth + 1); if (target.matches(SHAPES)) paint(target, use || sh, target); }
            return;
          }
          if (!sh.querySelector("tspan")) paint(sh, use, ref);  // a text with tspans is painted by them
        });
      }
      // A shape cloned by <use> takes the paint its own attributes or the symbol's give, else the use element's.
      // shortcut: paint set by a CSS class inside a symbol is read as the use element's; no sketch does that yet.
      function paint(sh, use, ref) {
        var ss = cs(sh); if (!use && (ss.visibility !== "visible" || ss.display === "none")) return;
        var o = opacity(use || sh, null);
        ["fill", "stroke"].forEach(function (prop) {
          var from = use || sh;
          if (use) for (var n = sh, v; n; n = n === ref ? null : n.parentElement) {
            v = n.getAttribute(prop) || n.style[prop];
            if (v && !/^inherit$/i.test(v)) { from = /currentcolor/i.test(v) ? null : sh; break; }
          }
          var fs = from ? cs(from) : null, value = from ? fs[prop] : cs(use).color;
          if (value === "none" || (prop === "fill" && sh.tagName.toLowerCase() === "line") || (prop === "stroke" && !(parseFloat((fs || ss).strokeWidth) > 0))) return;
          got.push(fade(rgba(value), o * parseFloat(prop === "fill" ? (fs || ss).fillOpacity : (fs || ss).strokeOpacity)));
        });
      }
      shapes(svg, null, null, 0);
      var seen = {};
      got.forEach(function (c) { if (c && c[3] > 0) seen[hex(c[3] < 1 && under ? over(c, under) : c)] = true; });
      var extra = { svg: svgClass(svg, r, root, kind, Object.keys(seen).length), box: pageBox(r), background: under ? hex(under) : "unknown" };
      got.forEach(function (c) { add(c, under, "svg", desc(svg), extra); });
    });
    var byHex = {}, list = [];
    order.forEach(function (k) {
      var u = uses[k];
      if (!byHex[u.hex]) { byHex[u.hex] = { hex: u.hex, uses: [] }; list.push(byHex[u.hex]); }
      byHex[u.hex].uses.push(u.use);
    });
    return list;
  }
  // Reading order: top, then left, where two boxes that share at least half the lower one's height are on one line.
  function reading(p, q) {
    var shared = Math.min(p.bottom, q.bottom) - Math.max(p.top, q.top);
    return shared >= Math.min(p.bottom - p.top, q.bottom - q.top) / 2 ? p.left - q.left : p.top - q.top;
  }
  function covered(el, x, y) {
    // Hit-testing sees only the viewport, which can be shorter than the page: scroll the point into view, then back.
    var x0 = window.scrollX, y0 = window.scrollY;
    window.scrollTo(x0, y0 + y - window.innerHeight / 2);
    var hit = document.elementFromPoint(x, y - (window.scrollY - y0));
    window.scrollTo(x0, y0);
    if (!hit || hit === el || el.contains(hit)) return false;
    for (var n = hit; n && !n.contains(el); n = n.parentElement) { var c = rgba(cs(n).backgroundColor); if (c && c[3] >= 1) return true; }
    return false;
  }
  function besideName(e) {
    var av = e.closest(".av,.avatar"); if (!av || !av.parentElement) return false;
    var w = document.createTreeWalker(av.parentElement, NodeFilter.SHOW_TEXT);
    for (var n = w.nextNode(); n; n = w.nextNode()) if (!av.contains(n) && n.nodeValue.trim()) return true;
    return false;
  }
  // The frame's size against its kind's, each within 2 px (model-driven is desktop).
  function sizeOf(kind, r) {
    var w = Math.round(r.width), h = Math.round(r.height);
    function near(v, x) { return Math.abs(v - x) <= 2; }
    if (kind === "none") return "no size class";
    if (kind === "desktop") return near(r.width, 1366) && near(r.height, 768) ? "ok" : w + " by " + h + ", not 1366 by 768";
    if (kind === "report") return near(r.width, 1920) && near(r.height, 1080) ? "ok" : w + " by " + h + ", not 1920 by 1080";
    if (kind === "phone") return r.width >= 358 && r.width <= 432 ? "ok" : w + " wide, not 360 to 430";
    return r.width >= 1364 && r.width <= 1442 ? "ok" : w + " wide, not 1366 to 1440";
  }
  function measureBox(frame, kind, floor, tile) {
    var fr = frame.getBoundingClientRect(), fs = cs(frame);
    var content = {
      left: fr.left + parseFloat(fs.borderLeftWidth) + parseFloat(fs.paddingLeft),
      right: fr.right - parseFloat(fs.borderRightWidth) - parseFloat(fs.paddingRight),
      top: fr.top + parseFloat(fs.borderTopWidth) + parseFloat(fs.paddingTop),
      bottom: fr.bottom - parseFloat(fs.borderBottomWidth) - parseFloat(fs.paddingBottom)
    };
    // A tile's own header is the sketch's caption of the state, not the product, so a tile is measured without it.
    var caption = tile ? Array.prototype.filter.call(frame.children, function (c) { return c.tagName.toLowerCase() === "header"; })[0] : null;
    // Text atoms: each visible text node with its element, size and line boxes.
    var atoms = [], walker = document.createTreeWalker(frame, NodeFilter.SHOW_TEXT);
    for (var tn = walker.nextNode(); tn; tn = walker.nextNode()) {
      if (!tn.nodeValue.trim()) continue;
      var el = tn.parentElement; if (!el || el.closest("style,script,title,defs,symbol") || (caption && caption.contains(el))) continue;
      var s = cs(el);
      if (s.visibility !== "visible" || opacity(el, null) === 0) continue;
      var range = document.createRange(); range.selectNodeContents(tn);
      var rects = Array.prototype.slice.call(range.getClientRects()).filter(function (r) { return r.width >= 2 && r.height >= 2; });
      if (!rects.length) continue;
      var size = parseFloat(s.fontSize);
      // The glyphs' band: the middle 80% of the em around each line's centre, so a tight line height is not an overlap.
      var bands = rects.map(function (r) { var c = (r.top + r.bottom) / 2; return { left: r.left + 0.5, right: r.right - 0.5, top: c - 0.4 * size, bottom: c + 0.4 * size }; });
      atoms.push({ el: el, node: tn, text: clip(tn.nodeValue), size: size, weight: parseInt(s.fontWeight, 10) || 400, family: s.fontFamily, rects: rects, bands: bands, svg: el instanceof SVGElement });
    }
    var out = { name: frame.getAttribute("data-screen"), "class": frame.getAttribute("class") || "", kind: kind,
      box: pageBox(fr), floor: floor,
      overflow: [], overlaps: [], "under-floor": [], contrast: [], fonts: [], "empty-share": 0, "font-sizes": [], faults: [] };

    // Overflow: text outside a clipping ancestor, outside the nearest painted box, or outside the frame.
    atoms.forEach(function (a) {
      var nearest = true, why = null, ellipsis = false;
      for (var n = a.el; n && n !== frame.parentElement; n = n.parentElement) {
        if (n instanceof SVGElement && n.tagName.toLowerCase() !== "svg") continue;
        var s = cs(n), b = n.getBoundingClientRect(), p = painted(s);
        var clips = s.overflowX !== "visible" || s.overflowY !== "visible";
        var box = (p.bg || p.border || p.image) && s.display !== "inline" && s.display !== "contents";
        if ((clips || (box && nearest) || n === frame) && !a.bands.every(function (r) { return inside(r, b, 1); })) {
          // Text a clipping box hides entirely (below a scrolling list's fold, or screen-reader-only text) is not drawn, so not a fault.
          if (clips && !a.bands.some(function (r) { return hits(r, b); })) { why = "hidden"; break; }
          why = (clips ? "clipped by " : "spills out of ") + desc(n); ellipsis = clips && s.textOverflow === "ellipsis"; break;
        }
        if (box) nearest = false;
      }
      if (why === "hidden") a.hidden = true;
      else if (why) out.overflow.push({ text: a.text, element: desc(a.el), by: why, ellipsis: ellipsis });
    });
    // Overlaps: two text atoms whose glyph bands cross. In a tile (a crop that can show a dialog over the dimmed screen),
    // not where an opaque box drawn above one of them hides it.
    for (var i = 0; i < atoms.length; i++) for (var j = i + 1; j < atoms.length; j++) {
      var crossing = null;
      atoms[i].bands.forEach(function (r) { atoms[j].bands.forEach(function (q) { if (!crossing && hits(r, q)) crossing = [r, q]; }); });
      if (crossing && tile) {
        var px = (Math.max(crossing[0].left, crossing[1].left) + Math.min(crossing[0].right, crossing[1].right)) / 2;
        var py = (Math.max(crossing[0].top, crossing[1].top) + Math.min(crossing[0].bottom, crossing[1].bottom)) / 2;
        if (covered(atoms[i].el, px, py) || covered(atoms[j].el, px, py)) crossing = null;
      }
      if (crossing) out.overlaps.push({ a: atoms[i].text, b: atoms[j].text });
    }
    // Floor, contrast, fonts, sizes, lead.
    var pairs = {}, stacks = {}, sizes = {}, largest = null;
    atoms.forEach(function (a) {
      // Avatar initials repeat the name beside them, so the floor skips them (decision Q15) when the avatar's parent holds
      // text outside the avatar; a lone avatar's initials are text like any other. Their contrast still counts.
      if (a.size < floor - 0.01 && !besideName(a.el)) out["under-floor"].push({ text: a.text, size: Math.round(a.size * 10) / 10, element: desc(a.el) });
      sizes[Math.round(a.size * 2) / 2] = true;
      if (!largest || a.size > largest.size || (a.size === largest.size && a.weight > largest.weight)) largest = a;
      if (!stacks[a.family]) stacks[a.family] = rendered(a.family);
      if (a.el.closest(':disabled,[disabled],[aria-disabled="true"]')) return;
      var s = cs(a.el), fg = rgba(a.svg ? s.fill : s.color), r0 = a.rects[0];
      var cx = (r0.left + r0.right) / 2, cy = (r0.top + r0.bottom) / 2;
      var bg = a.svg ? svgBackground(a.el, cx, cy) : undefined;
      if (bg === undefined) bg = background(a.svg ? a.el.ownerSVGElement || a.el : a.el);
      var large = a.size >= 24 || (a.size >= 18.66 && a.weight >= 700), need = large ? 3 : 4.5;
      var key, entry;
      if (!fg || !bg) {
        key = "unknown|" + (fg ? hex(fg) : "?");
        entry = pairs[key] || (pairs[key] = { color: fg ? hex(fg) : "unknown", background: "unknown", ratio: null, need: need, count: 0, text: a.text, pass: null });
      } else {
        fg[3] *= a.svg ? parseFloat(s.fillOpacity) * opacity(a.el, a.el.ownerSVGElement) : opacity(a.el, null);
        var shown = over(fg, bg), q = Math.round(ratio(shown, bg) * 100) / 100;
        key = hex(shown) + "|" + hex(bg) + "|" + need;
        entry = pairs[key] || (pairs[key] = { color: hex(shown), background: hex(bg), ratio: q, need: need, count: 0, text: a.text, pass: q >= need });
      }
      entry.count++;
    });
    out.contrast = Object.keys(pairs).map(function (k) { return pairs[k]; });
    out.fonts = Object.keys(stacks).map(function (k) { return stacks[k]; });
    out["font-sizes"] = Object.keys(sizes).map(Number).sort(function (a, b) { return a - b; });
    out.colours = colourUses(frame, atoms, kind, caption);
    // A state tile has no lead, title, size or empty share.
    if (!tile) {
      var leads = frame.hasAttribute("data-lead") ? [frame] : Array.prototype.slice.call(frame.querySelectorAll("[data-lead]"));
      out["largest-text"] = largest ? { size: largest.size, text: largest.text, element: desc(largest.el) } : null;
      if (!leads.length) out.lead = "not marked";
      else {
        var inLead = atoms.filter(function (a) { return leads.some(function (l) { return l.contains(a.el); }); });
        if (!inLead.length) out.lead = "holds no text";
        else out.lead = inLead.some(function (a) { return a.size >= largest.size; }) ? "largest" : "not largest";
      }
      // The title: the first h1 in reading order (top, then left), else the first h2, else the first heading, outside nested tiles.
      var title = null;
      ["h1", "h2", "[role=heading]"].forEach(function (sel) {
        if (title) return;
        title = Array.prototype.filter.call(frame.querySelectorAll(sel), function (h) {
          var r = h.getBoundingClientRect(); return r.width >= 1 && r.height >= 1 && cs(h).visibility === "visible" && !nestedTile(h, frame);
        }).sort(function (a, b) { return reading(a.getBoundingClientRect(), b.getBoundingClientRect()); })[0] || null;
      });
      out.title = title ? { text: clip(title.innerText || title.textContent), element: desc(title), size: parseFloat(cs(title).fontSize), weight: parseInt(cs(title).fontWeight, 10) || 400 } : null;
      // The five largest texts of the first screen (the top 768 px of a desktop or browser frame, all of a phone or report
      // frame) outside nested tiles, by size, then weight, then reading order. Chrome is header, nav and footer.
      var fold = kind === "desktop" || kind === "browser" ? fr.top + 768 : Infinity;
      out["top-text"] = atoms.filter(function (a) { return !a.hidden && !nestedTile(a.el, frame) && a.rects[0].top < fold; })
        .sort(function (a, b) { return b.size - a.size || b.weight - a.weight || reading(a.rects[0], b.rects[0]); }).slice(0, 5)
        .map(function (a) {
          var c = a.el.closest("header,nav,footer");
          return { size: a.size, weight: a.weight, text: a.text, element: desc(a.el), title: !!title && title.contains(a.el),
            chrome: !!c && c !== frame && frame.contains(c), inLead: leads.length ? leads.some(function (l) { return l.contains(a.el); }) : null };
        });
      out.size = sizeOf(kind, fr);
      // Empty share: sampled points of the content area farther than 16 px (a gap inside a group, S5) from any element
      // that shows something: the block that holds a text, a graphic, a control or a painted box. The content area is
      // the frame's content box less the equal margins left and right of every text block, so a centred site's gutters
      // don't count and an empty side does.
      // Gate 7 follows this figure: a judge who reads it as a false alarm writes a fault line, and the gate still fails.
      var area = Math.max(1, (content.right - content.left) * (content.bottom - content.top)), filled = [], left = Infinity, right = -Infinity;
      var blocks = new Set();
      atoms.forEach(function (a) {
        var b = a.el;
        if (!a.svg) while (b !== frame && /^(inline|contents)$/.test(cs(b).display)) b = b.parentElement;
        var r = a.svg || b === frame ? null : b.getBoundingClientRect();
        if (!r || r.width * r.height >= 0.5 * area) { filled.push.apply(filled, a.rects); r = a.rects[0]; }
        else if (!blocks.has(b)) { blocks.add(b); filled.push(r); }
        left = Math.min(left, r.left); right = Math.max(right, r.right);
      });
      var frameBg = background(frame);
      Array.prototype.forEach.call(frame.querySelectorAll("*"), function (e) {
        if (e instanceof SVGElement && e.tagName.toLowerCase() !== "svg") return;
        var s = cs(e); if (s.visibility !== "visible" || s.display === "none") return;
        var r = e.getBoundingClientRect(); if (r.width < 1 || r.height < 1 || r.width * r.height >= 0.5 * area) return;
        var tag = e.tagName.toLowerCase(), p = painted(s);
        var visibleBg = p.bg && (!frameBg || hex(over(p.bg, frameBg)) !== hex(frameBg));
        if (["svg", "img", "canvas", "video", "input", "select", "textarea", "button", "hr"].indexOf(tag) >= 0 || visibleBg || p.image || p.border || p.shadow) filled.push(r);
      });
      var gutter = Math.max(0, Math.min(left - 16 - content.left, content.right - right - 16));
      var x0 = content.left + gutter, x1 = content.right - gutter;
      filled = filled.map(function (r) { return { left: r.left - 16, right: r.right + 16, top: r.top - 16, bottom: r.bottom + 16 }; })
        .filter(function (r) { return r.right > x0 && r.left < x1 && r.bottom > content.top && r.top < content.bottom; });
      var step = Math.max(4, Math.sqrt((x1 - x0) * (content.bottom - content.top) / 4000)), total = 0, empty = 0;
      for (var y = content.top + step / 2; y < content.bottom; y += step) for (var x = x0 + step / 2; x < x1; x += step) {
        total++;
        if (!filled.some(function (r) { return x >= r.left && x <= r.right && y >= r.top && y <= r.bottom; })) empty++;
      }
      out["empty-share"] = total ? Math.round(empty / total * 1000) / 1000 : 0;
    }

    var cut = out.overflow.filter(function (o) { return o.ellipsis; }).length;
    if (out.overflow.length > cut) out.faults.push("overflow: " + (out.overflow.length - cut) + " text box(es) clipped or spilling");
    if (cut) out.faults.push("ellipsis: " + cut + " text box(es) cut with an ellipsis");
    if (out.overlaps.length) out.faults.push("overlap: " + out.overlaps.length + " pair(s) of text boxes overlap");
    if (out["under-floor"].length) out.faults.push("floor: " + out["under-floor"].length + " text(s) under " + floor + " px");
    var low = out.contrast.filter(function (c) { return c.pass === false; });
    if (low.length) out.faults.push("contrast: " + low.length + " colour pair(s) under their ratio");
    var fb = out.fonts.filter(function (f) { return f.fallback; });
    if (fb.length) out.faults.push("font: " + fb.map(function (f) { return f.asked + " rendered as " + f.rendered; }).join("; "));
    if (out["empty-share"] > 0.15) out.faults.push("empty: about " + Math.round(out["empty-share"] * 100) + "% of the content area is empty");
    if (out.lead === "not largest") out.faults.push("lead: the largest text is outside the data-lead element");
    // S4 for an app, P4 on a site (five plus the headline), B8 on a report (four plus the callout).
    var site = kind === "browser" || (kind === "phone" && siteOnly), rule = kind === "report" ? ["B8", 5, "four plus the callout"]
      : site ? ["P4", 6, "five plus the headline"] : ["S4", 5, "three or four plus a display size"];
    if (!tile && out["font-sizes"].length > rule[1]) out.faults.push("sizes: " + out["font-sizes"].length + " distinct font sizes (" + rule[0] + " allows " + rule[2] + ")");
    return out;
  }
  function floorOf(kind) { return kind === "browser" || (kind === "phone" && siteOnly) ? 14 : 12; }
  var frames = all.map(function (frame, fi) { return measureBox(frame, kinds[fi], floorOf(kinds[fi]), false); });
  // State tiles: one inside a frame is measured with it; one outside every frame is measured like a frame, but for the
  // empty share, the lead and the size, on the floor of the frame its data-for names, else the file's.
  var states = Array.prototype.map.call(document.querySelectorAll("[data-state]"), function (e) {
    var tile = { state: e.getAttribute("data-state"), "for": e.getAttribute("data-for"), box: pageBox(e.getBoundingClientRect()),
      "in-frame": all.some(function (f) { return f.contains(e); }) };
    if (tile["in-frame"]) return tile;
    var fi = all.map(function (f) { return f.getAttribute("data-screen"); }).indexOf(tile["for"]);
    var kind = fi >= 0 ? kinds[fi] : reportFile ? "report" : "none", m = measureBox(e, kind, fi >= 0 ? floorOf(kind) : siteOnly ? 14 : 12, true);
    ["floor", "overflow", "overlaps", "under-floor", "contrast", "fonts", "colours", "faults"].forEach(function (k) { tile[k] = m[k]; });
    return tile;
  });
  var result = { width: document.documentElement.clientWidth,
    "page-height": Math.max(document.documentElement.scrollHeight, document.body ? document.body.scrollHeight : 0),
    frames: frames, states: states };
  var json = JSON.stringify(result).replace(/[\u0080-￿]/g, function (c) { return "\\u" + ("000" + c.charCodeAt(0).toString(16)).slice(-4); });
  var pre = document.createElement("pre"); pre.id = "MARK"; pre.textContent = btoa(json);
  document.documentElement.appendChild(pre);
}());
""".replace('"MARK"', '"' + MARK + '"')


def find_chrome(given):
    if given:
        return given
    if os.environ.get("CHROME"):
        return os.environ["CHROME"]
    for name in CHROME_NAMES:
        found = shutil.which(name)
        if found:
            return found
    for base, rel in [("ProgramFiles", r"Google\Chrome\Application\chrome.exe"), ("ProgramFiles(x86)", r"Google\Chrome\Application\chrome.exe"),
                      ("LocalAppData", r"Google\Chrome\Application\chrome.exe"), ("ProgramFiles(x86)", r"Microsoft\Edge\Application\msedge.exe"),
                      ("ProgramFiles", r"Microsoft\Edge\Application\msedge.exe")]:
        root = os.environ.get(base)
        if root and os.path.isfile(os.path.join(root, rel)):
            return os.path.join(root, rel)
    raise Unrenderable("no Chrome or Chromium found: pass --chrome <path> or set CHROME (tried " + ", ".join(CHROME_NAMES) + " on PATH and the default Windows install paths)")


def read_text(path):
    try:
        return Path(path).read_bytes().decode("utf-8-sig")
    except (OSError, UnicodeDecodeError) as e:
        raise Unreadable("%s: %s" % (path, e))


def sketch_hash(text):
    """SHA-256 of the text with CRLF made LF, as powerops plan status hashes it."""
    return hashlib.sha256(text.replace("\r\n", "\n").encode("utf-8")).hexdigest()


class Page(http.server.BaseHTTPRequestHandler):
    """Answers a GET of the session's page URL, in the absolute form a proxy request has, and 404 to anything else."""

    def log_message(self, *args):
        pass

    def do_GET(self):
        s = self.server.session
        if self.path == "/%s/sketch.html" % s.token:  # the page asked for directly: Chrome went past the proxy
            s.bypassed = True
        if self.path != s.url:
            self.send_error(404)
            return
        body = Path(s.page).read_bytes()
        self.send_response(200)
        for k, v in [("Content-Type", "text/html; charset=utf-8"), ("Content-Length", str(len(body))),
                     ("Content-Security-Policy", CSP % s.nonce), ("Cache-Control", "no-store")]:
            self.send_header(k, v)
        self.end_headers()
        self.wfile.write(body)


class Session:
    """A temp folder holding the copy and Chrome's profile, and the loopback server that serves the copy; both gone on exit."""

    def __init__(self, chrome):
        self.chrome = chrome
        self.dir = tempfile.mkdtemp(prefix="powerops-measure-")
        self.token, self.nonce, self.page, self.bypassed = secrets.token_hex(16), secrets.token_hex(16), None, False
        try:
            self.server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Page)
        except OSError as e:
            shutil.rmtree(self.dir, ignore_errors=True)
            raise Unrenderable("the measuring server could not start on 127.0.0.1: %s" % e)
        self.server.session = self
        self.port = self.server.server_address[1]
        self.url = "http://127.0.0.1:%d/%s/sketch.html" % (self.port, self.token)
        threading.Thread(target=self.server.serve_forever, daemon=True).start()

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        self.server.shutdown()
        self.server.server_close()
        for _ in range(10):  # Windows can hold Chrome's files for a moment after it exits
            shutil.rmtree(self.dir, ignore_errors=True)
            if not os.path.exists(self.dir):
                return
            time.sleep(0.3)

    def copy(self, text, name):
        script = '<script nonce="%s">' % self.nonce + MEASURE_JS + "</script>"
        at = text.lower().rfind("</body")
        page = text[:at] + script + text[at:] if at >= 0 else text + script
        path = os.path.join(self.dir, name)
        with open(path, "w", encoding="utf-8", newline="") as f:
            f.write(page)
        return path

    def run(self, page, width, height, extra):
        self.page, self.bypassed = page, False
        # Loopback goes through the proxy too, so the page reaches no other local service; the resolver must still find the proxy.
        args = [self.chrome, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run", "--no-default-browser-check",
                "--disable-background-networking", "--disable-component-update", "--disable-sync", "--disable-extensions",
                "--host-resolver-rules=MAP * ~NOTFOUND , EXCLUDE 127.0.0.1", "--proxy-server=http://127.0.0.1:%d" % self.port,
                "--proxy-bypass-list=<-loopback>", "--user-data-dir=" + os.path.join(self.dir, "profile"),
                "--window-size=%d,%d" % (width, height)] + extra + [self.url]
        try:
            done = subprocess.run(args, stdout=subprocess.PIPE, stderr=subprocess.PIPE, stdin=subprocess.DEVNULL, timeout=90)
        except OSError as e:
            raise Unrenderable("Chrome could not start: %s: %s" % (self.chrome, e))
        except subprocess.TimeoutExpired:
            raise Unrenderable("Chrome did not finish in 90 seconds: " + self.chrome)
        return done

    def measure(self, page, width, height):
        done = self.run(page, width, height, ["--dump-dom"])
        out = done.stdout.decode("utf-8", "replace")
        m = re.search(r'<pre id="%s">([A-Za-z0-9+/=]+)</pre>' % MARK, out)
        if not m:
            self.past_proxy()
            raise Unrenderable("Chrome ran but gave no measurement (exit %d): %s" % (done.returncode, tail(done.stderr)))
        return json.loads(base64.b64decode(m.group(1)).decode("ascii"))

    def shot(self, page, width, height, png):
        done = self.run(page, width, height, ["--screenshot=" + png])
        if not os.path.isfile(png):
            self.past_proxy()
            raise Unrenderable("Chrome ran but wrote no picture (exit %d): %s" % (done.returncode, tail(done.stderr)))

    def past_proxy(self):
        if self.bypassed:  # a proxy policy can override --proxy-server; the page then gets the 404, so the render fails
            raise Unrenderable("Chrome did not use the measuring proxy (a proxy policy?)")


def tail(err):
    lines = [l for l in err.decode("utf-8", "replace").splitlines() if l.strip()]
    return " | ".join(lines[-3:]) or "no message"


def auto_width(text):
    # 1:1 at the product's size: canvas and Power Pages 1440 wide, a file with a Power BI report frame 1968.
    return 1968 if re.search(r'class="[^"]*\bframe\b[^"]*\breport\b|class="[^"]*\breport\b[^"]*\bframe\b', text) else 1440


def layout(session, sketch, width):
    """Measure a sketch: at a first height to learn the page's height, then again at that height."""
    text = read_text(sketch)
    page = session.copy(text, "sketch.html")
    width = width or auto_width(text)
    first = session.measure(page, width, 1200)
    height = max(1, min(int(first["page-height"]), 60000))
    data = first if height == 1200 else session.measure(page, width, height)
    data["page-height"] = height
    return text, page, width, height, data


# A colour is neutral when its sRGB chroma (max minus min channel) is at most this: set so that every token of the five
# calibration sketches falls on its side (docs/plans/m117-judge-consistency/calibration.md).
NEUTRAL_CHROMA = 6
HUE_STEP = 15


def chroma(h):
    rgb = [int(h[i:i + 2], 16) for i in (1, 3, 5)]
    return max(rgb) - min(rgb)


def hue(h):
    return colorsys.rgb_to_hsv(*[int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)])[0] * 360


def hue_groups(hexes):
    """Hue groups numbered from 1: sorted around the circle from after the largest gap, each group anchored at its first colour."""
    hs = sorted(set(hexes), key=lambda h: (hue(h), h))
    if not hs:
        return {}
    hv = [hue(h) for h in hs]
    gaps = [hv[i + 1] - hv[i] for i in range(len(hs) - 1)] + [hv[0] + 360 - hv[-1]]
    start = (gaps.index(max(gaps)) + 1) % len(hs)  # the first largest gap, so a tie gives the same start every run
    groups, n, anchor = {}, 0, None
    for h in hs[start:] + hs[:start]:
        if anchor is None or (hue(h) - anchor) % 360 > HUE_STEP:
            n, anchor = n + 1, hue(h)
        groups[h] = n
    return groups


def report(sketch, text, width, data):
    out = {"sketch": str(sketch), "sketch-hash": sketch_hash(text), "width": width, "page-height": data["page-height"],
           "frames": data["frames"], "states": data["states"]}
    boxes = out["frames"] + [t for t in out["states"] if not t["in-frame"]]
    for b in boxes:
        b["colours"] = [c for c in b["colours"] if chroma(c["hex"]) > NEUTRAL_CHROMA]
    groups = hue_groups([c["hex"] for b in boxes for c in b["colours"]])  # one numbering for the whole file
    for b in boxes:
        b["colours"] = sorted(({"hex": c["hex"], "group": groups[c["hex"]], "uses": c["uses"]} for c in b["colours"]), key=lambda c: (c["group"], c["hex"]))
    out["faults"] = sum(len(b["faults"]) for b in boxes)
    return out


# --- PNG: a reader for Chrome's 8-bit RGB or RGBA output and a writer of RGB, with zlib and struct only.

def png_read(path, rows=None):
    data = Path(path).read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise Unreadable("%s: not a PNG" % path)
    pos, idat, head = 8, [], None
    while pos < len(data):
        n, kind = struct.unpack(">I4s", data[pos:pos + 8])
        body = data[pos + 8:pos + 8 + n]
        if kind == b"IHDR":
            head = struct.unpack(">IIBBBBB", body)
        elif kind == b"IDAT":
            idat.append(body)
        elif kind == b"IEND":
            break
        pos += 12 + n
    w, h, depth, ctype, _, _, interlace = head
    if depth != 8 or ctype not in (2, 6) or interlace:
        raise Unreadable("%s: only 8-bit RGB or RGBA PNGs without interlace are read" % path)
    bpp = 3 if ctype == 2 else 4
    stride = w * bpp
    h = min(h, rows) if rows else h  # decode only the top rows a crop needs
    raw = zlib.decompress(b"".join(idat))
    out, prev = bytearray(stride * h), bytearray(stride)
    for y in range(h):
        f = raw[y * (stride + 1)]
        line = bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        if f == 1:
            for x in range(bpp, stride):
                line[x] = (line[x] + line[x - bpp]) & 255
        elif f == 2:
            line = bytearray((a + b) & 255 for a, b in zip(line, prev))
        elif f == 3:
            for x in range(stride):
                line[x] = (line[x] + (((line[x - bpp] if x >= bpp else 0) + prev[x]) >> 1)) & 255
        elif f == 4:
            for x in range(stride):
                a = line[x - bpp] if x >= bpp else 0
                b = prev[x]
                c = prev[x - bpp] if x >= bpp else 0
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        elif f != 0:
            raise Unreadable("%s: bad PNG filter %d" % (path, f))
        out[y * stride:(y + 1) * stride] = line
        prev = line
    if bpp == 4:  # drop alpha: Chrome's screenshots are opaque
        rgb = bytearray(w * h * 3)
        for c in range(3):
            rgb[c::3] = out[c::4]
        out = rgb
    return w, h, out


def png_write(path, w, h, px):
    rows = b"".join(b"\x00" + bytes(px[y * w * 3:(y + 1) * w * 3]) for y in range(h))

    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body) & 0xFFFFFFFF)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(rows, 6)) + chunk(b"IEND", b""))


def crop(img, x, y, w, h):
    iw, ih, px = img
    x, y = max(0, x), max(0, y)
    w, h = max(0, min(w, iw - x)), max(0, min(h, ih - y))
    out = bytearray()
    for row in range(y, y + h):
        out += px[(row * iw + x) * 3:(row * iw + x + w) * 3]
    return w, h, out


def halve(img):
    w, h, px = img
    hw, hh = w // 2, h // 2
    out = bytearray(hw * hh * 3)
    for y in range(hh):
        r0 = px[(2 * y) * w * 3:(2 * y) * w * 3 + hw * 6]
        r1 = px[(2 * y + 1) * w * 3:(2 * y + 1) * w * 3 + hw * 6]
        row = bytearray(hw * 3)
        for c in range(3):
            row[c::3] = bytes((a + b + d + e + 2) >> 2 for a, b, d, e in zip(r0[c::6], r0[3 + c::6], r1[c::6], r1[3 + c::6]))
        out[y * hw * 3:(y + 1) * hw * 3] = row
    return hw, hh, out


def slug(name):
    return re.sub(r"[^a-z0-9]+", "-", (name or "").lower()).strip("-") or "unnamed"


def check_out(folder):
    resolved = Path(folder).resolve()
    parts = [p.lower() for p in resolved.parts]
    if any(parts[i:i + 2] == ["docs", "plans"] for i in range(len(parts))):
        raise ValueError("pictures go in a folder outside docs/plans/ (the plan-folder scan warns on binary files): " + folder)
    if resolved == Path(__file__).resolve().parent or Path(__file__).resolve().parent in resolved.parents:
        raise ValueError("pictures go in a folder outside this skill's folder: " + folder)
    return resolved


def shoot(sketch, out, width, chrome):
    out = check_out(out)
    out.mkdir(parents=True, exist_ok=True)
    stem = slug(Path(sketch).stem)
    with Session(chrome) as s:
        text, page, width, height, data = layout(s, sketch, width)
        full = str(out / (stem + ".full.png"))
        s.shot(page, width, height, full)
    t0 = time.time()
    img = png_read(full)
    written = [{"file": full, "w": img[0], "h": img[1]}]
    targets = [("%d-%s" % (i + 1, slug(f["name"])), f["box"]) for i, f in enumerate(data["frames"])]
    targets += [("state-%d-%s" % (i + 1, slug(t["state"])), t["box"]) for i, t in enumerate(data["states"])]
    for name, box in targets:
        cols, rows = max(1, math.ceil(box["w"] / TILE_W)), max(1, math.ceil(box["h"] / TILE_H))
        tw, th = math.ceil(box["w"] / cols), math.ceil(box["h"] / rows)
        for r in range(rows):
            for c in range(cols):
                part = crop(img, box["x"] + c * tw, box["y"] + r * th, tw, th)
                if not part[0] or not part[1]:
                    continue
                suffix = "" if rows * cols == 1 else ".%d-%d" % (r + 1, c + 1)
                path = str(out / ("%s.%s%s.png" % (stem, name, suffix)))
                png_write(path, *part)
                written.append({"file": path, "w": part[0], "h": part[1]})
    return {"sketch": str(sketch), "sketch-hash": sketch_hash(text), "width": width, "page-height": height,
            "crop-seconds": round(time.time() - t0, 1), "pictures": written}


def side_by_side(files, out_png, chrome):
    out = Path(out_png).resolve()
    check_out(str(out.parent))
    texts = [read_text(f) for f in files]
    width = max(auto_width(t) for t in texts)
    parts = []
    with Session(chrome) as s:
        for i, f in enumerate(files):
            _, page, _, height, data = layout(s, f, width)
            if not data["frames"]:
                raise Unreadable("%s: no data-screen frame" % f)
            png = os.path.join(s.dir, "shot%d.png" % i)
            s.shot(page, width, height, png)
            b = data["frames"][0]["box"]
            parts.append(halve(crop(png_read(png, b["y"] + b["h"]), b["x"], b["y"], b["w"], b["h"])))
    gap = 24
    w = sum(p[0] for p in parts) + gap * (len(parts) + 1)
    h = max(p[1] for p in parts) + 2 * gap
    px = bytearray(b"\xff" * (w * h * 3))
    x = gap
    for pw, ph, ppx in parts:
        for y in range(ph):
            at = ((gap + y) * w + x) * 3
            px[at:at + pw * 3] = ppx[y * pw * 3:(y + 1) * pw * 3]
        x += pw + gap
    out.parent.mkdir(parents=True, exist_ok=True)
    png_write(str(out), w, h, px)
    return {"picture": str(out), "w": w, "h": h, "width": width, "sketches": [str(f) for f in files]}


# --- Self-test: one planted fault of each kind, a clean frame and an unmarked one that must report nothing.

def frame_html(name, change="", kind=None):
    lead = "" if change == "unmarked" else " data-lead"
    big = '<h1%s style="font-size:32px;margin:0">Lead 42</h1>' % ("" if change == "lead" else lead)
    cards = [
        '<div class="card">%s<p>Open repairs today</p></div>' % big,
        '<div class="card"><p>Due this week</p><p style="background:linear-gradient(#fff,#eee)">On a gradient</p></div>',
        '<div class="card"><p>Assigned to a contractor</p><button disabled style="color:#BDBDBD;background:#fff;border:1px solid #eee;font:14px sans-serif">Assign</button></div>',
        '<div class="card"><p>Closed this month</p></div>']
    if change == "overflow":
        cards[1] = '<div class="card"><p style="width:60px;overflow:hidden;white-space:nowrap">A long label that is cut</p></div>'
    elif change == "ellipsis":
        cards[1] = '<div class="card"><p style="width:60px;overflow:hidden;white-space:nowrap;text-overflow:ellipsis">A long label that is cut</p></div>'
    elif change == "overlap":
        cards[1] = '<div class="card" style="position:relative"><p style="position:absolute;top:10px">First line</p><p style="position:absolute;top:14px;left:20px">Second line</p></div>'
    elif change == "floor":
        cards[1] = '<div class="card"><p style="font-size:10px">Tiny print</p></div>'
    elif change == "contrast":
        cards[1] = '<div class="card"><p style="color:#AAAAAA">Pale words</p></div>'
    elif change == "font":
        cards[1] = '<div class="card"><p style="font-family:\'PowerOpsNoSuchFont\',sans-serif">Missing face</p></div>'
    elif change == "empty":
        cards = [cards[0]]
    elif change == "lead":
        cards[1] = '<div class="card"><p data-lead>Marked but small</p></div>'
    elif change in ("sizes", "sizes7"):
        cards[1] = '<div class="card">' + "".join('<p style="font-size:%dpx;margin:0">Size %d</p>' % (n, n) for n in (16, 18, 20, 24, 28)[:4 if change == "sizes" else 5]) + "</div>"
    elif change == "avatar":
        cards[1] = ('<div class="card"><p><span class="av" style="font-size:12px;color:#06434A;background:#CBE4E7">MS</span> Maria S.</p>'
                    '<p><span class="initials" style="font-size:12px">MS</span></p><p><span class="avatar" style="font-size:12px">KT</span></p></div>')
    elif change == "colours":  # a hue in text; an icon, a chart and a drawing; the card's grey fill and the grey text are neutral
        cards[1] = ('<div class="card"><p style="color:#0B6A75">Brand link <span style="color:#616161">grey</span></p>'
                    '<svg width="20" height="20"><rect width="20" height="20" fill="#0E700E"/></svg>'
                    '<svg width="120" height="40"><rect width="60" height="30" fill="#B10E1C"/><text x="70" y="20" fill="#242424">12</text></svg>'
                    '<svg width="60" height="40"><circle cx="20" cy="20" r="20" fill="#C2570C"/><circle cx="20" cy="20" r="5" fill="#FFFFFF"/></svg>'
                    '<svg width="40" height="40"><path d="M0 0H40L20 40Z" fill="#0E700E"/></svg>'
                    '<svg width="80" height="20"><path d="M0 20L40 0L80 20Z" fill="#8A3707"/></svg>'
                    '<svg width="100" height="16"><rect width="100" height="6" fill="#EEACB2"/><line x1="80" x2="80" y2="16" stroke="#242424"/></svg></div>')
    elif change == "line":  # two 20 px texts on one line, the right one's box 8 px higher: the left one reads first
        cards[1] = ('<div class="card" style="display:flex;align-items:flex-end;gap:8px"><p style="font-size:20px;line-height:24px">Left</p>'
                    '<p style="font-size:20px;line-height:40px">Right</p></div>')
    elif change == "fold":  # an h2 left of the h1 on one line, and a larger text below the first 768 px
        cards = ['<div class="card"><h2 style="font-size:14px;margin:0">Side</h2></div>', cards[0],
                 '<div class="card" style="padding-top:780px;background:none"><p style="font-size:40px;margin:0">Below</p></div>']
    elif change == "nested":  # chrome, the title and a lead, and a nested tile whose large text top-text leaves out
        cards = ['<div class="card"><header><span style="font-size:24px">App</span></header><h2 style="font-size:20px;margin:0">Queue</h2>'
                 '<p style="font-size:16px" data-lead>Lead line</p></div>',
                 '<div class="card" data-state="loading"><p style="font-size:40px;margin:0">Huge</p></div>']
    kind = kind or ("browser" if change == "avatar" else "desktop")
    return '<section class="frame %s" data-screen="%s">%s</section>' % (kind, name, "".join(cards))


SELF_TEST_CSS = ("body{margin:0;background:#fff;font:14px/1.4 sans-serif;color:#242424}"
                 ".frame{width:600px;height:300px;margin:16px;display:grid;grid-template-columns:1fr 1fr;gap:8px;padding:8px;"
                 "box-sizing:border-box;border:1px solid #ccc;background:#fff}"
                 ".card{background:#F0F0F0;padding:8px;margin:0;overflow:hidden}.card p{margin:0 0 4px}")
KINDS = ["overflow", "ellipsis", "overlap", "floor", "contrast", "font", "empty", "lead", "sizes"]


def escape_sketch(work):
    """A green data: box that must show, and each way a page can reach a red file outside its folder, last the one that leaves the page."""
    outside = Path(work) / "outside"
    outside.mkdir()
    png_write(str(outside / "red.png"), 8, 8, b"\xff\x00\x00" * 64)
    (outside / "red.html").write_text('<body style="margin:0;background:#f00"></body>', encoding="utf-8")
    data = []
    for name, rgb in [("green.png", b"\x00\xff\x00"), ("white.png", b"\xff\xff\xff")]:
        png_write(os.path.join(work, name), 8, 8, rgb * 64)
        data.append("data:image/png;base64," + base64.b64encode(Path(work, name).read_bytes()).decode("ascii"))
    png, page, box = (outside / "red.png").as_uri(), (outside / "red.html").as_uri(), 'width="120" height="80"'
    control = '<table background="%s"><tr><td %s></td></tr></table>' % (data[0], box)
    return control, [
        '<iframe src="%s" %s></iframe>' % (page, box),
        '<table background="%s"><tr><td %s></td></tr></table>' % (png, box),
        '<svg %s><filter id="f" x="0" y="0" width="1" height="1"><feImage href="%s" %s preserveAspectRatio="none"/></filter><rect %s filter="url(#f)"/></svg>' % (box, png, box, box),
        '<div style="width:120px;height:80px;background:url(%s)"></div>' % png,
        '<script>document.body.style.background = "#f00"</script>',
        '<img src="%s" onload="document.body.style.background = \'#f00\'">' % data[1],
        '<script>location.replace("%s")</script>' % page]


def colours(img):
    """The red and the green pixels of a picture."""
    px = img[2]
    red = sum(1 for i in range(0, len(px), 3) if px[i] > 200 and px[i + 1] < 60 and px[i + 2] < 60)
    green = sum(1 for i in range(0, len(px), 3) if px[i + 1] > 200 and px[i] < 60 and px[i + 2] < 60)
    return red, green


def escape_case(chrome, work):
    control, parts = escape_sketch(work)
    with Session(chrome) as s:
        png = os.path.join(s.dir, "escape.png")
        s.shot(s.copy('<!DOCTYPE html><html><body style="margin:0">' + control + "".join(parts) + "</body></html>", "sketch.html"), 640, 480, png)
        red, green = colours(png_read(png))
    problems = ["the escape sketch's picture shows %d red pixels from files outside it" % red] if red else []
    return problems + ([] if green else ["the escape sketch's picture shows no green, so its check proves nothing"])


def server_checks(chrome):
    """The loopback server answers the page's absolute URL only, notes a request that went past the proxy, and is gone after the session."""
    problems = []
    with Session(chrome) as s:
        s.page = s.copy("<p>page</p>", "sketch.html")
        host = "http://127.0.0.1:%d" % s.port

        def ask(method, target):
            c = http.client.HTTPConnection("127.0.0.1", s.port, timeout=10)
            try:
                c.request(method, target)
                r = c.getresponse()
                r.read()
                return r
            finally:
                c.close()
        r = ask("GET", s.url)
        heads = (r.getheader("Content-Security-Policy"), r.getheader("Content-Type"), r.getheader("Cache-Control"))
        if r.status != 200 or heads != (CSP % s.nonce, "text/html; charset=utf-8", "no-store"):
            problems.append("the page's URL gives %d with %s" % (r.status, heads))
        for target in [host + "/%s/other.html" % s.token, host + "/%s/sketch.html" % ("0" * 32), "http://localhost:%d/%s/sketch.html" % (s.port, s.token)]:
            if ask("GET", target).status != 404:
                problems.append("%s is not 404" % target)
        if s.bypassed:
            problems.append("a proxy request was noted as going past the proxy")
        if ask("GET", "/%s/sketch.html" % s.token).status != 404 or not s.bypassed:
            problems.append("a direct request for the page was answered or not noted")
        for method, target in [("POST", s.url), ("CONNECT", "127.0.0.1:443")]:
            if ask(method, target).status == 200:
                problems.append("%s is answered" % method)
    try:
        socket.create_connection(("127.0.0.1", s.port), timeout=5).close()
        problems.append("the server still answers after the session")
    except OSError:
        pass
    if os.path.exists(s.dir):
        problems.append("the session's temp folder is still there")
    return problems


def new_field_checks(result, frames):
    """The fields of M117: ellipsis, sizes per statement, colours and hue groups, title and top-text, size, state tiles."""
    problems = []
    ov, el = frames["overflow"]["overflow"], frames["ellipsis"]["overflow"]
    if [o["ellipsis"] for o in ov] != [False] or [o["ellipsis"] for o in el] != [True]:
        problems.append("ellipsis: overflow says %s, ellipsis says %s" % (ov, el))
    six, seven = frames["p4-six"]["faults"], frames["p4-seven"]["faults"]
    if six or [f.split(":")[0] for f in seven] != ["sizes"] or "P4" not in seven[0] or "S4" not in frames["sizes"]["faults"][0]:
        problems.append("sizes per statement: six on a site %s, seven %s, six in an app %s" % (six, seven, frames["sizes"]["faults"]))
    got = {c["hex"]: sorted(set(u.get("svg", u["role"]) for u in c["uses"])) for c in frames["colours"]["colours"]}
    if got != {"#0B6A75": ["text"], "#0E700E": ["icon"], "#B10E1C": ["chart"], "#C2570C": ["imagery"], "#EEACB2": ["chart"], "#8A3707": ["imagery"]} or frames["clean"]["colours"]:
        problems.append("colours: %s, the clean frame %s" % (got, frames["clean"]["colours"]))
    svg = [u for c in frames["colours"]["colours"] for u in c["uses"] if u["role"] == "svg"]
    if not all(u["background"] == "#F0F0F0" and u["box"]["w"] in (20, 120, 60, 100, 40, 80) for u in svg) or len(set(c["group"] for c in frames["colours"]["colours"])) != 4:
        problems.append("colours: svg uses %s, groups %s" % (svg, [c["group"] for c in frames["colours"]["colours"]]))
    nested = frames["nested"]
    top = [(t["text"], t["title"], t["chrome"], t["inLead"]) for t in nested["top-text"]]
    if (nested["title"] or {}).get("text") != "Queue" or top[:3] != [("App", False, True, False), ("Queue", True, False, False), ("Lead line", False, False, True)] \
            or any(t["text"] == "Huge" for t in nested["top-text"]) or any(t["inLead"] is not None for t in frames["unmarked"]["top-text"]):
        problems.append("top-text: title %s, top %s" % (nested["title"], top))
    if [t["text"] for t in frames["line"]["top-text"][1:3]] != ["Left", "Right"]:
        problems.append("reading order: %s" % [t["text"] for t in frames["line"]["top-text"]])
    fold = frames["fold"]
    if (fold["title"] or {}).get("text") != "Lead 42" or any(t["text"] == "Below" for t in fold["top-text"]) or fold["top-text"][0]["text"] != "Lead 42":
        problems.append("first screen and title: title %s, top %s" % (fold["title"], [t["text"] for t in fold["top-text"]]))
    sizes = (frames["clean"]["size"], frames["no-size"]["size"], frames["phone"]["size"])
    if sizes != ("600 by 300, not 1366 by 768", "no size class", "ok"):
        problems.append("size: %s" % (sizes,))
    tiles = {t["state"]: t for t in result["states"]}
    err, inner = tiles.get("error", {}), tiles.get("loading", {})
    if [(u["text"], u["size"]) for u in err.get("under-floor", [])] != [("Tiny", 10)] or err.get("floor") != 12 or err.get("in-frame") is not False \
            or inner.get("in-frame") is not True or "faults" in inner or tiles["empty"].get("faults") != [] or tiles["confirm"].get("faults") != []:
        problems.append("state tiles: %s" % result["states"])
    if result["faults"] != sum(len(f["faults"]) for f in result["frames"]) + 1:
        problems.append("the top-level faults count %d leaves out the tile's fault" % result["faults"])
    for hexes in (["#FF0A00", "#F4B740", "#0B6A75", "#FF0016", "#8A3707", "#0E2A30"], ["#0E2A30", "#8A3707", "#FF0016", "#0B6A75", "#F4B740", "#FF0A00"]):
        g = hue_groups(hexes)  # reds at 2 and 355 degrees together, from either order
        if g != {"#FF0016": 1, "#FF0A00": 1, "#8A3707": 2, "#F4B740": 3, "#0B6A75": 4, "#0E2A30": 4}:
            problems.append("hue groups of %s: %s" % (hexes, g))
    if [chroma(h) > NEUTRAL_CHROMA for h in ("#F6F5F2", "#E2E6E6", "#F1FAF1", "#FDF3F4")] != [False, False, True, True]:
        problems.append("neutral: the page and board tokens are neutral, the status tints are not")
    return problems


def self_test(chrome):
    problems = []
    try:
        ast.parse(Path(__file__).read_text(encoding="utf-8"), feature_version=(3, 8))
    except SyntaxError as e:
        problems.append("the script does not parse as Python 3.8: %s" % e)
    sketch = ('<!DOCTYPE html><html lang="en"><head><meta name="viewport" content="width=device-width, initial-scale=1">'
              "\n<style>" + SELF_TEST_CSS + "</style></head>\n<body>\n" + frame_html("clean") + "\n" + frame_html("unmarked", "unmarked") + "\n"
              + "\n".join(frame_html(k, k) for k in KINDS) + "\n" + frame_html("avatar", "avatar") + "\n"
              + frame_html("p4-six", "sizes", "browser") + frame_html("p4-seven", "sizes7", "browser") + frame_html("colours", "colours")
              + frame_html("nested", "nested") + frame_html("line", "line") + frame_html("fold", "fold", 'browser" style="height:1000px') + frame_html("no-size", "", "x") + frame_html("phone", "", 'phone" style="width:390px')
              + '\n<div class="state" data-state="empty" data-for="clean">None</div>'
              + '\n<div class="state" data-state="error"><header style="font-size:10px">Caption</header><p style="font-size:10px">Tiny</p></div>'
              + '\n<div class="state" data-state="confirm"><div style="position:relative"><p style="margin:0">Under the dialog</p>'
              + '<div style="position:absolute;top:0;left:0;right:0;background:#fff"><p style="margin:0">Dialog text</p></div></div></div>\n</body></html>\n')
    work = tempfile.mkdtemp(prefix="powerops-selftest-")
    try:
        path = os.path.join(work, "self-test.html")
        with open(path, "w", encoding="utf-8", newline="") as f:
            f.write(sketch.replace("\n", "\r\n"))
        with Session(chrome) as s:
            text, _, width, _, data = layout(s, path, 640)
        result = report(path, text, width, data)
        if result["sketch-hash"] != hashlib.sha256(sketch.encode("utf-8")).hexdigest():
            problems.append("sketch-hash is not the hash of the text with CRLF made LF")
        frames = {f["name"]: f for f in result["frames"]}
        for name in ["clean", "unmarked"]:
            if frames[name]["faults"]:
                problems.append("the %s frame reports %s" % (name, frames[name]["faults"]))
        if frames["clean"]["lead"] != "largest" or frames["unmarked"]["lead"] != "not marked":
            problems.append("lead: clean says %r, unmarked says %r" % (frames["clean"]["lead"], frames["unmarked"]["lead"]))
        if not any(c["background"] == "unknown" for c in frames["clean"]["contrast"]):
            problems.append("text on a gradient did not give an unknown background")
        for kind in KINDS:
            got = [f.split(":")[0] for f in frames[kind]["faults"]]
            if got != [kind]:
                problems.append("the %s frame reports %s, not exactly [%s]" % (kind, frames[kind]["faults"], kind))
        # A site frame: 12 px initials in an avatar are not under the floor, the same text outside one is; the avatar's contrast still counts.
        av = frames["avatar"]
        if [(u["text"], u["element"]) for u in av["under-floor"]] != [("MS", "span.initials"), ("KT", "span.avatar")] or [f.split(":")[0] for f in av["faults"]] != ["floor"]:
            problems.append("the avatar frame reports %s under the floor, faults %s" % (av["under-floor"], av["faults"]))
        if not any(c["background"] == "#CBE4E7" and c["text"] == "MS" for c in av["contrast"]):
            problems.append("the avatar's initials have no contrast entry")
        problems += new_field_checks(result, frames)
        pics = shoot(path, os.path.join(work, "pictures"), 640, chrome)["pictures"]
        clean = next(p for p in pics if p["file"].endswith(".1-clean.png"))
        w, h, px = png_read(clean["file"])
        box = frames["clean"]["box"]
        if (w, h) != (box["w"], box["h"]):
            problems.append("the clean frame's picture is %dx%d, its box %dx%d" % (w, h, box["w"], box["h"]))
        at = ((h // 2) * w + 2) * 3  # inside the frame's 8 px padding, left of the cards: its white background
        if tuple(px[at:at + 3]) != (255, 255, 255):
            problems.append("the clean frame's picture has %s where the frame is white" % (tuple(px[at:at + 3]),))
        if len(pics) != 1 + len(frames) + len(result["states"]):
            problems.append("shoot wrote %d pictures, not %d" % (len(pics), 1 + len(frames) + len(result["states"])))
        joined = side_by_side([path, path, path], os.path.join(work, "pictures", "side.png"), chrome)
        if (joined["w"], joined["h"]) != (3 * (box["w"] // 2) + 4 * 24, box["h"] // 2 + 48):
            problems.append("side-by-side is %dx%d" % (joined["w"], joined["h"]))
        problems += escape_case(chrome, work) + server_checks(chrome)
    finally:
        shutil.rmtree(work, ignore_errors=True)
    for p in problems:
        print("FAIL " + p)
    print("self-test: %s (%d planted kinds, a clean and an unmarked frame, avatar initials on a site, sizes per statement, colours and hue groups, "
          "title and top-text, size, state tiles, shoot and side-by-side, a sketch reaching for files outside it, and the loopback server)"
          % ("failed" if problems else "passed", len(KINDS)))
    return 1 if problems else 0


def main(argv):
    parser = argparse.ArgumentParser(prog="measure.py", description="Measure and picture a PowerOps sketch with a headless Chrome.")
    parser.add_argument("--self-test", action="store_true", help="check the script on a built-in page")
    parser.add_argument("--chrome", help="the Chrome, Chromium or Edge program (else CHROME, else PATH)")
    sub = parser.add_subparsers(dest="command")
    m = sub.add_parser("measure", help="print the measured faults as JSON")
    m.add_argument("sketch")
    s = sub.add_parser("shoot", help="full-page PNG and one PNG per frame and state tile, in tiles of at most 1400 by 1200")
    s.add_argument("sketch")
    s.add_argument("out_folder")
    s.add_argument("--width", type=int, help="window width (default: 1440, or 1968 for a file with a report frame)")
    b = sub.add_parser("side-by-side", help="three lead frames at one width, joined left to right at half scale")
    b.add_argument("sketches", nargs=3)
    b.add_argument("out_png")
    for p in (m, s, b):
        p.add_argument("--chrome", default=argparse.SUPPRESS, help=argparse.SUPPRESS)
    args = parser.parse_args(argv)
    try:
        if args.self_test:
            return self_test(find_chrome(args.chrome))
        if not args.command:
            parser.print_usage(sys.stderr)
            return 2
        chrome = find_chrome(args.chrome)
        if args.command == "measure":
            with Session(chrome) as session:
                text, _, width, _, data = layout(session, args.sketch, None)
            result = report(args.sketch, text, width, data)
        elif args.command == "shoot":
            result = shoot(args.sketch, args.out_folder, args.width, chrome)
        else:
            result = side_by_side(args.sketches, args.out_png, chrome)
        print(json.dumps(result, indent=1))
        return 0
    except Unrenderable as e:
        print("not rendered: %s" % e, file=sys.stderr)
        return 77
    except Unreadable as e:
        print("error: %s" % e, file=sys.stderr)
        return 3
    except ValueError as e:
        print("error: %s" % e, file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
