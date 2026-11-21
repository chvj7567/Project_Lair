import { TAU, WHITE, band, rand } from "../../core/index.js";
import { pal, sheen } from "./_lib.js";

export const meta = {
  name: "Berserker blood frenzy",
  desc: "A crimson aura pulsing under the berserker: a throbbing ring, sparks orbiting it and rings of heat rippling outward, seamless.",
  tags: ["aura", "loop", "ground"],
  size: [64, 64],
  origin: [32, 32],
  duration: 0.667,
  loop: true,
  fps: 12,
  bg: "ground",
  options: { palette: ["#2a0a0c", "#7a1418", "#c4281e", "#ff5a3a", "#ffd0a0"], reach: 28, seed: 31 },
};

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed, ph = t / meta.duration;
  const beat = 0.5 + 0.5 * Math.sin(ph * TAU);
  sheen(p, R * 0.9, (d) => [d > 0.55 ? C[2] : C[1], 0.22 + 0.3 * d * d + 0.12 * beat]);
  band(p, 0, 0, R * (0.86 + 0.04 * beat), 1, 3.5 + beat, 1, [C[1], C[2], C[3], C[4]], () => true, false);
  //# sixteen flame spikes round the rim, throbbing with the beat
  for (let i = 0; i < 16; i++) {
    const a = (i / 16) * TAU + ph * (TAU / 16), r0 = R * 0.9, r1 = R * (1.0 + 0.1 * beat + (i % 2) * 0.06) + 2;
    p.line(Math.cos(a) * r0, Math.sin(a) * r0, Math.cos(a) * r1, Math.sin(a) * r1, i % 2 ? C[3] : C[4], 1);
  }
  //# two ripples (half a loop apart) spreading out and fading, seamless
  for (let i = 0; i < 2; i++) {
    const u = (ph + i * 0.5) % 1, rr = R * (0.35 + 0.6 * u);
    band(p, 0, 0, rr, 1, 1.6, (1 - u) * 0.9, [C[1], C[2], C[2], C[3]], () => true, false);
  }
  //# six sparks orbiting, one full turn per loop
  for (let i = 0; i < 6; i++) {
    const a = ph * TAU + (i / 6) * TAU, rr = R * 0.82;
    const x = Math.cos(a) * rr, y = Math.sin(a) * rr;
    p.px(x, y, i % 2 ? C[4] : WHITE);
    p.px(x - Math.sin(a), y + Math.cos(a), C[3]);
    p.px(x - Math.sin(a) * 2, y + Math.cos(a) * 2, C[2]);
    p.light(x, y, C[3], 0.3);
  }
  //# heat motes drifting inward on a cycle
  for (let i = 0; i < 8; i++) {
    const u = (ph * 1 + rand(i, 1, S)) % 1, a = rand(i, 2, S) * TAU, rr = R * (0.95 - 0.7 * u);
    p.dot(Math.cos(a) * rr, Math.sin(a) * rr, C[3], Math.sin(u * Math.PI));
  }
}
