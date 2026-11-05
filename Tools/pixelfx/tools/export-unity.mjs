#!/usr/bin/env node
// Render a pixelfx effect to a sprite sheet and drop it into the Unity project.
//
//   node tools/export-unity.mjs melee/sword-slash                       -> SwordSlashFx.png/.json
//   node tools/export-unity.mjs fire/torch --name TorchFx --opts '{"palette":["#102","#215","#43a","#8cf","#fff"]}'
//   node tools/export-unity.mjs melee/sword-slash --actor                keep the stock character in the sheet
//
// Output: Assets/_Lair/Art/Sprites/PixelFx/<Name>.png + <Name>.json (Unity then slices it, see PixelFxImporter.cs).
// Then in Unity: menu Lair > PixelFx > Build Prefabs  ->  Assets/_Lair/Art/FX/PixelFx/<Name>.prefab

import { spawnSync } from "node:child_process";
import { copyFileSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const OUT = join(ROOT, "..", "..", "Assets", "_Lair", "Art", "Sprites", "PixelFx");
const args = process.argv.slice(2), id = args.find((a) => !a.startsWith("--") && args[args.indexOf(a) - 1] !== "--name" && args[args.indexOf(a) - 1] !== "--opts");
const opt = (k) => (args.includes(k) ? args[args.indexOf(k) + 1] : undefined);
if (!id || !id.includes("/")) { console.error("usage: export-unity.mjs <category/name> [--name Name] [--opts JSON] [--actor]"); process.exit(2); }

const leaf = id.split("/")[1];
const pascal = leaf.split("-").map((s) => s[0].toUpperCase() + s.slice(1)).join("");
const name = opt("--name") ?? `${pascal}Fx`;
if (!/^[A-Za-z][A-Za-z0-9_]*$/.test(name)) { console.error(`bad --name: ${name}`); process.exit(2); }

const r = spawnSync(process.execPath, [join(ROOT, "tools", "render.mjs"), id, "--sheet", ...(args.includes("--actor") ? [] : ["--no-actor"]), ...(opt("--opts") ? ["--opts", opt("--opts")] : [])], { stdio: "inherit" });
if (r.status !== 0) process.exit(r.status ?? 1);

mkdirSync(OUT, { recursive: true });
const src = join(ROOT, "records", id, leaf);
copyFileSync(src + ".png", join(OUT, name + ".png"));
const json = JSON.parse(readFileSync(src + ".json", "utf8"));
json.meta.image = name + ".png";
writeFileSync(join(OUT, name + ".json"), JSON.stringify(json, null, 1));
console.log(`-> ${join(OUT, name)}.png/.json  (${json.meta.pixelfx.frames} frames @ ${json.meta.pixelfx.fps} fps)`);
