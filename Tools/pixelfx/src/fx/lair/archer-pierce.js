import { fit, clamp, smooth, ease, rand, WHITE, hex } from "../../core/index.js";

export const meta = {
  name: "Archer piercing shot",
  desc: "A lane of green light: a white-hot bolt streaks up the lane leaving a bright core line and glowing rails, then the lane fades.",
  tags: ["arrow", "beam", "ground", "one-shot"],
  size: [24, 32],
  origin: [12, 32],
  duration: 0.5,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#0b3b1a", "#1e7a34", "#58c64a", "#b6f07a", "#f2ffd0"], seed: 7 },
};

//# forward is up the sheet and the sheet is stretched ~9x lengthwise in game: vertical lines stay crisp, blobs become streaks
export function draw(p, t, o) {
  const C = fit(o.palette, 5).map((c) => (typeof c === "string" ? hex(c) : c)), S = o.seed * 5;
  const k = clamp(t / 0.25), head = -31 * ease.outCubic(k);
  const fade = 1 - smooth(0.25, 0.5, t);
  if (fade <= 0) return;

  //# glowing lane: translucent, brighter toward the centre, pulsing up the lane with the bolt
  for (let y = -31; y <= 0; y++) {
    if (y < head - 1) continue;
    const behind = clamp((y - head) / 31);
    for (let x = -11; x <= 11; x++) {
      const d = Math.abs(x) / 11;
      const a = Math.min(0.95, (0.22 + 0.6 * (1 - d * d)) * (0.6 + 0.4 * (1 - behind)) * fade);
      p.blend(x, y, d < 0.35 ? C[3] : C[2], a);
    }
  }

  //# rails and core line
  for (let y = -31; y <= 0; y++) {
    if (y < head) continue;
    const u = clamp((y - head) / 31);
    p.dot(-10, y, C[3], fade);
    p.dot(10, y, C[3], fade);
    p.dot(-9, y, C[2], fade);
    p.dot(9, y, C[2], fade);
    p.px(0, y, u < 0.15 ? WHITE : C[4]);
    p.px(-1, y, u < 0.15 ? C[4] : C[3]);
    p.px(1, y, u < 0.15 ? C[4] : C[3]);
    if (fade > 0.5 || (y + t * 40) % 3 < 1.5) {
      p.dot(-2, y, C[2], fade);
      p.dot(2, y, C[2], fade);
    }
  }

  //# feather ticks along the lane
  for (let i = 0; i < 6; i++) {
    const y = -4 - i * 5, on = y > head;
    if (on) {
      p.px(-4, y, C[3]);
      p.px(4, y, C[3]);
    }
  }

  //# the bolt head: a white-hot blob with a halo
  if (k < 1) {
    p.ellipse(0, head + 1, 8, 3, C[3]);
    p.ellipse(0, head + 1, 4, 2.2, WHITE);
    p.glow(0, head, 10, C[3], 0.6);
  }
  //# muzzle flash at the base
  if (t < 0.17) p.glow(0, -2, 12, C[4], 0.6 * (1 - t / 0.17));
}
