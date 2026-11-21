import { fit, hex } from "../../core/index.js";

//# palette option -> 5 numeric colors, dark to bright
export const pal = (o) => fit(o.palette, 5).map((c) => (typeof c === "string" ? hex(c) : c));

//# translucent radial fill: f(d) with d 0..1 from the centre returns [color, alpha] or null
export function sheen(p, r, f) {
  const n = Math.ceil(r);
  for (let y = -n; y <= n; y++) {
    for (let x = -n; x <= n; x++) {
      const d = Math.hypot(x + 0.5, y + 0.5) / r;
      if (d > 1) continue;
      const v = f(d);
      if (v && v[1] > 0.01) p.blend(x, y, v[0], Math.min(1, v[1]));
    }
  }
}

//# a four-point glint
export function star(p, x, y, s, c, tip) {
  p.px(x, y, tip ?? c);
  for (let i = 1; i <= s; i++) {
    p.px(x + i, y, c);
    p.px(x - i, y, c);
    p.px(x, y + i, c);
    p.px(x, y - i, c);
  }
}
