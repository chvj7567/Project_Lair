import { fit, band, clamp, smooth, ease, rand, TAU, WHITE, hex, mix } from "../../core/index.js";

export const meta = {
  name: "Knight nova",
  desc: "Gold shockwave ring racing out across the floor from a white flash, with glints along the front and a fading gold sheen behind it.",
  tags: ["nova", "aoe", "ground", "one-shot"],
  size: [128, 96],
  origin: [64, 62],
  duration: 0.833,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#5a3200", "#a85f06", "#e8a41c", "#ffd84a", "#fff6c8"], reach: 56, seed: 3 },
};

const SY = 0.5;

export function draw(p, t, o) {
  const C = fit(o.palette, 5).map((c) => (typeof c === "string" ? hex(c) : c)), REACH = o.reach, S = o.seed * 11;
  const k = clamp(t / 0.62);
  const r = REACH * ease.outCubic(k);
  const fade = 1 - smooth(0.52, 0.83, t);
  const ramp = [C[1], C[2], C[3], C[4]];

  //# translucent gold sheen on the floor inside the front, brighter near the rim
  if (r > 2 && fade > 0) {
    for (let y = Math.floor(-r * SY); y <= Math.ceil(r * SY); y++) {
      for (let x = Math.floor(-r); x <= Math.ceil(r); x++) {
        const d = Math.hypot((x + 0.5) / r, (y + 0.5) / (r * SY));
        if (d > 1) continue;
        const rim = Math.pow(d, 3);
        p.blend(x, y, rim > 0.55 ? C[3] : C[2], (0.1 + 0.34 * rim) * fade);
      }
    }
  }

  //# the ring: thick bright front, a thin lagging inner ring
  if (k > 0) {
    const th = Math.max(1.5, 5.5 * (1 - k * 0.8));
    band(p, 0, 0, r + 1, SY, th, fade, ramp, () => true, k < 0.25);
    if (r > 12) band(p, 0, 0, r * 0.72, SY, 1.6, 0.8 * fade, ramp, () => true, false);
  }
  if (r > 4 && fade > 0) p.glow(0, 0, r * 1.05, C[3], 0.1 * fade);

  //# glints riding the front: little stars at fixed angles, each twinkling at its own time
  for (let i = 0; i < 14; i++) {
    const a = (i / 14) * TAU + rand(i, 1, S) * 0.3, off = rand(i, 2, S) * 0.25, ki = clamp(k - off * 0.25);
    const ri = REACH * ease.outCubic(ki);
    if (ki <= 0 || ki >= 1 || fade <= 0.05) continue;
    const x = Math.cos(a) * ri, y = Math.sin(a) * ri * SY, h = 2 + Math.round(rand(i, 3, S) * 3) * (1 - ki);
    p.px(x, y - h, ki < 0.5 ? WHITE : C[4]);
    p.px(x, y - h * 0.4, C[3]);
    if (ki < 0.6) p.px(x, y - h * 1.7, C[4]);
    p.light(x, y - h, C[3], 0.35 * (1 - ki));
  }

  //# the flash at the feet
  if (t < 0.17) {
    const f = 1 - t / 0.17;
    p.ellipse(0, -1, 16 * (0.55 + 0.45 * f), 7 * (0.55 + 0.45 * f), C[3], 0.9);
    p.ellipse(0, -1, 9 * (0.6 + 0.4 * f), 4 * (0.6 + 0.4 * f), f > 0.5 ? WHITE : C[4]);
    p.glow(0, -4, 34, C[4], 0.55 * f);
  }
}
