import { clamp, smooth, ease, rand, TAU, WHITE, hex, band } from "../../core/index.js";
import { pal, sheen } from "./_lib.js";

export const meta = {
  name: "Berserker chain pull",
  desc: "Iron chains lash out across the floor circle and drag inward: radial link lines with a hooked head each haul toward the centre while a red ring contracts and flashes.",
  tags: ["chain", "pull", "aoe", "ground", "one-shot"],
  size: [160, 160],
  origin: [80, 80],
  duration: 0.667,
  loop: false,
  fps: 12,
  bg: "ground",
  options: { palette: ["#2a0e10", "#7a2020", "#c43a2a", "#ff7a4a", "#ffe0c0"], reach: 76, seed: 21 },
};

const N = 10;

export function draw(p, t, o) {
  const C = pal(o), R = o.reach, S = o.seed * 5;
  const K = [hex("#2c303c"), hex("#6a7388"), hex("#b8c4d8"), hex("#f2f6ff")];
  const out = ease.outCubic(clamp(t / 0.15)), fade = 1 - smooth(0.5, 0.667, t);
  if (fade <= 0) return;
  //# the head sweeps from the rim inward as the pull proceeds
  const pull = ease.inOutCubic(clamp((t - 0.12) / 0.4));
  const head = R * out * (1 - 0.82 * pull);
  sheen(p, R * out, (d) => [d > 0.7 ? C[2] : C[1], (0.05 + 0.22 * d * d) * fade]);
  band(p, 0, 0, Math.max(4, head), 1, 3, fade, [C[1], C[2], C[3], C[4]], () => true, t < 0.2);
  for (let i = 0; i < N; i++) {
    const a = (i / N) * TAU + rand(i, 1, S) * 0.15, ca = Math.cos(a), sa = Math.sin(a);
    const lo = 6 + 3 * rand(i, 2, S);
    //# links: alternating bright and dark dashes along the line, two px thick
    for (let d = lo; d <= head; d += 1) {
      const link = Math.floor(d / 3) % 2 === 0;
      const x = ca * d, y = sa * d;
      p.dot(x, y, link ? K[2] : K[1], fade);
      p.dot(x - sa, y + ca, link ? K[1] : K[0], fade);
      if (link && (d / 3) % 2 < 1) p.dot(x + sa, y - ca, K[3], fade * 0.8);
    }
    //# hooked head
    const hx = ca * head, hy = sa * head;
    p.circle(hx, hy, 2.5, K[2], fade);
    p.px(hx, hy, WHITE);
    p.line(hx, hy, hx - sa * 4 + ca * 3, hy + ca * 4 + sa * 3, K[3], fade);
    p.light(hx, hy, C[3], 0.35 * fade);
  }
  //# the haul lands: a red flash at the centre
  const land = clamp((t - 0.5) / 0.12);
  if (land > 0 && land < 1) {
    p.circle(0, 0, 12 * (1 - land) + 3, land < 0.4 ? WHITE : C[3], 0.95);
    p.glow(0, 0, 36, C[3], 0.5 * (1 - land));
  }
}
