using UnityEngine;

namespace Bo.SVGA
{
    public static class SVGAUtils
    {
        public static void ApplyTransform(RectTransform rt, global::Bo.SVGA.Transform t)
        {
            var a = t.A;
            var b = t.B;
            var c = t.C;
            var d = t.D;
            var tx = t.Tx;
            var ty = t.Ty;
            var sx = Mathf.Sqrt(a * a + b * b);
            var sy = Mathf.Sqrt(c * c + d * d);
            var rot = Mathf.Atan2(b, a) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            rt.localScale = new Vector3(sx == 0 ? 0.0001f : sx, sy == 0 ? 0.0001f : sy, 1);
        }

        public static Color ToColor(ShapeEntity.Types.ShapeStyle.Types.RGBAColor c)
        {
            if (c == null) return new Color(0, 0, 0, 0);
            return new Color(c.R, c.G, c.B, c.A);
        }
    }
}