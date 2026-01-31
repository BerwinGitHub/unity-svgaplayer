using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bo.SVGA
{
    public class SVGAMaskGraphic : MaskableGraphic
    {
        private string _path;
        private List<List<Vector2>> _contours = new List<List<Vector2>>();
        private Vector2 _viewBox;
        private Layout _layout;
        private Transform _transform;

        public void SetPath(string d)
        {
            _path = d;
            ParsePath();
            SetAllDirty();
        }

        public void SetFrame(Vector2 viewBox, Layout layout, Transform t)
        {
            _viewBox = viewBox;
            _layout = layout;
            var eff = new Transform
            {
                A = t != null ? t.A : 1f,
                B = t != null ? t.B : 0f,
                C = t != null ? t.C : 0f,
                D = t != null ? t.D : 1f
            };
            float baseTx = t != null ? t.Tx : 0f;
            float baseTy = t != null ? t.Ty : 0f;
            float lx = layout != null ? layout.X : 0f;
            float ly = layout != null ? layout.Y : 0f;
            eff.Tx = baseTx + lx;
            eff.Ty = baseTy + ly;
            _transform = eff;
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_contours == null || _contours.Count == 0) return;
            var tess = new Bo.LibTessDotNet.Tess();
            for (int ci = 0; ci < _contours.Count; ci++)
            {
                var c = _contours[ci];
                if (c == null || c.Count < 3) continue;
                var contour = new LibTessDotNet.ContourVertex[c.Count];
                for (int i = 0; i < c.Count; i++)
                {
                    var p = MapToUI(ApplyAffine(c[i]));
                    contour[i].Position.X = p.x;
                    contour[i].Position.Y = p.y;
                }
                tess.AddContour(contour, LibTessDotNet.ContourOrientation.Original);
            }
            tess.Tessellate(LibTessDotNet.WindingRule.NonZero, LibTessDotNet.ElementType.Polygons, 3);
            if (tess.Elements == null || tess.Elements.Length == 0)
            {
                tess.Tessellate(LibTessDotNet.WindingRule.EvenOdd, LibTessDotNet.ElementType.Polygons, 3);
            }
            var verts = tess.Vertices;
            var indices = tess.Elements;
            for (int i = 0; i < verts.Length; i++)
            {
                vh.AddVert(new Vector3(verts[i].Position.X, verts[i].Position.Y), Color.white, Vector2.zero);
            }
            for (int i = 0; i < indices.Length; i += 3)
            {
                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];
                if (i0 < 0 || i1 < 0 || i2 < 0) continue;
                vh.AddTriangle(i0, i1, i2);
            }
        }

        private void ParsePath()
        {
            _contours.Clear();
            if (string.IsNullOrEmpty(_path)) return;
            var parser = new SVGAPathParser();
            var pts = parser.ParsePath(_path);
            if (pts != null && pts.Count >= 3)
            {
                var c = new List<Vector2>(pts.Count + 1);
                for (int i = 0; i < pts.Count; i++) c.Add(pts[i]);
                if ((c[c.Count - 1] - c[0]).sqrMagnitude > 0.0001f) c.Add(c[0]);
                _contours.Add(c);
            }
        }

        private Vector2 ApplyAffine(Vector2 p)
        {
            if (_transform == null) return p;
            float a = _transform.A;
            float b = _transform.B;
            float c = _transform.C;
            float d = _transform.D;
            float tx = _transform.Tx;
            float ty = _transform.Ty;
            float nx = a * p.x + c * p.y + tx;
            float ny = b * p.x + d * p.y + ty;
            return new Vector2(nx, ny);
        }

        private Vector2 MapToUI(Vector2 mv)
        {
            return new Vector2(mv.x - _viewBox.x * 0.5f, _viewBox.y * 0.5f - mv.y);
        }
    }
}