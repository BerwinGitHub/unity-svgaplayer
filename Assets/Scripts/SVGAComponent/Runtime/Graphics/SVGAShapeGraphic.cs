using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using pbc = Google.Protobuf.Collections;

namespace Bo.SVGA
{
    public class SVGAShapeGraphic : MaskableGraphic
    {
        private pbc::RepeatedField<ShapeEntity> _shapes;
        private List<ShapeEntity> _resolved = new List<ShapeEntity>();

        private List<ShapeEntity> _lastResolved = new List<ShapeEntity>();

        // private Vector2 _viewBox;
        private Layout _layout;
        private Transform _transform;
        private float _alpha = 1f;
        private List<SVGAFrameCache.ShapeMesh> _cacheMeshes;

        private int _spriteIdx;
        private int _frameIdx;

        public void SetShapes(pbc::RepeatedField<ShapeEntity> shapes)
        {
            _shapes = shapes;
            var newList = new List<ShapeEntity>();
            if (_shapes != null)
            {
                for (int i = 0; i < _shapes.Count; i++)
                {
                    var s = _shapes[i];
                    if (s.Type == ShapeEntity.Types.ShapeType.Keep) continue;
                    newList.Add(s);
                }
            }

            if (newList.Count > 0)
            {
                _resolved = newList;
                _lastResolved = new List<ShapeEntity>(_resolved);
            }
            else if (_lastResolved != null && _lastResolved.Count > 0)
            {
                _resolved = _lastResolved;
            }
        }

        public void SetFrame(float alpha, Vector2 viewBox, Layout layout, Transform t, int idx, string imageKey)
        {
            _alpha = alpha <= 0f ? 0f : (alpha >= 1f ? 1f : alpha);
            // _viewBox = viewBox;
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

        public void SetCacheMeshes(List<SVGAFrameCache.ShapeMesh> meshes, int spriteIdx, int frameIdx, string imageKey)
        {
            _cacheMeshes = meshes;
            _spriteIdx = spriteIdx;
            _frameIdx = frameIdx;
            if (_cacheMeshes == null || _cacheMeshes.Count <= 0)
            {
                SVGALog.LogWarning($"第{_spriteIdx}({imageKey})个 Sprite，第{_frameIdx}帧，没有缓存数据");
            }

            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_resolved == null || _resolved.Count == 0) return;
            if (_cacheMeshes != null && _cacheMeshes.Count > 0)
            {
                for (int s = 0; s < _resolved.Count && s < _cacheMeshes.Count; s++)
                {
                    var shape = _resolved[s];
                    var style = shape.Styles;
                    var mesh = _cacheMeshes[s];
                    if (style != null && style.Fill != null && mesh.FillPositions != null && mesh.FillIndices != null)
                    {
                        var col = SVGAUtils.ToColor(style.Fill);
                        col.a *= _alpha;
                        int start = vh.currentVertCount;
                        for (int i = 0; i < mesh.FillPositions.Length; i++)
                        {
                            var p = mesh.FillPositions[i];
                            vh.AddVert(new Vector3(p.x, p.y, 0), col, Vector2.zero);
                        }

                        for (int i = 0; i < mesh.FillIndices.Length; i += 3)
                        {
                            vh.AddTriangle(start + mesh.FillIndices[i], start + mesh.FillIndices[i + 1],
                                start + mesh.FillIndices[i + 2]);
                        }
                    }

                    if (style != null && style.Stroke != null && style.StrokeWidth > 0 &&
                        mesh.StrokePositions != null && mesh.StrokeIndices != null)
                    {
                        var scol = SVGAUtils.ToColor(style.Stroke);
                        scol.a *= _alpha;
                        int sstart = vh.currentVertCount;
                        for (int i = 0; i < mesh.StrokePositions.Length; i++)
                        {
                            var p = mesh.StrokePositions[i];
                            vh.AddVert(new Vector3(p.x, p.y, 0), scol, Vector2.zero);
                        }

                        for (int i = 0; i < mesh.StrokeIndices.Length; i += 3)
                        {
                            vh.AddTriangle(sstart + mesh.StrokeIndices[i], sstart + mesh.StrokeIndices[i + 1],
                                sstart + mesh.StrokeIndices[i + 2]);
                        }
                    }
                }

                return;
            }

            SVGALog.LogWarning($"第{_spriteIdx}个Sprite，第{_frameIdx}帧，没有缓存数据");
            // for (int s = 0; s < _resolved.Count; s++)
            // {
            //     var shape = _resolved[s];
            //     var style = shape.Styles;
            //
            //     var fill = style?.Fill;
            //     var stroke = style?.Stroke;
            //     var __origTransform = _transform;
            //     var st = shape.Transform;
            //     _transform = ComposeTransform(__origTransform, st);
            //     if (shape.Type == ShapeEntity.Types.ShapeType.Rect)
            //     {
            //         var r = shape.Rect;
            //         var x = r.X;
            //         var y = r.Y;
            //         var w = r.Width;
            //         var h = r.Height;
            //         var cr = r.CornerRadius;
            //         if (fill != null)
            //         {
            //             var col = SVGAUtils.ToColor(fill);
            //             col.a *= _alpha;
            //             if (cr > 0f)
            //             {
            //                 var pts = BuildRoundedRectPolyline(x, y, w, h, cr, 12);
            //                 var ui = new List<Vector2>(pts.Count + 1);
            //                 for (int i = 0; i < pts.Count; i++)
            //                 {
            //                     var tp = MapToUI(ApplyAffine(new Vector2(pts[i].x, pts[i].y)));
            //                     ui.Add(new Vector2(tp.x, tp.y));
            //                 }
            //
            //                 if ((ui[ui.Count - 1] - ui[0]).sqrMagnitude > 0.0001f) ui.Add(ui[0]);
            //                 SimplifyClosedPath(ui);
            //                 if (!TryConvexTriangulateUI(vh, ui, col))
            //                 {
            //                     var tess = new Bo.LibTessDotNet.Tess();
            //                     var contour = new Bo.LibTessDotNet.ContourVertex[ui.Count];
            //                     for (int i = 0; i < ui.Count; i++)
            //                     {
            //                         contour[i].Position.X = ui[i].x;
            //                         contour[i].Position.Y = ui[i].y;
            //                     }
            //
            //                     tess.AddContour(contour);
            //                     tess.Tessellate(Bo.LibTessDotNet.WindingRule.NonZero,
            //                         Bo.LibTessDotNet.ElementType.Polygons, 3);
            //                     if (tess.Elements == null || tess.Elements.Length == 0)
            //                     {
            //                         tess.Tessellate(Bo.LibTessDotNet.WindingRule.EvenOdd,
            //                             Bo.LibTessDotNet.ElementType.Polygons, 3);
            //                     }
            //
            //                     var v = tess.Vertices;
            //                     var ind = tess.Elements;
            //                     int start = vh.currentVertCount;
            //                     for (int i = 0; i < v.Length; i++)
            //                     {
            //                         vh.AddVert(new Vector3(v[i].Position.X, v[i].Position.Y), col, Vector2.zero);
            //                     }
            //
            //                     for (int i = 0; i < ind.Length; i += 3)
            //                     {
            //                         var i0 = ind[i];
            //                         var i1 = ind[i + 1];
            //                         var i2 = ind[i + 2];
            //                         if (i0 < 0 || i1 < 0 || i2 < 0) continue;
            //                         vh.AddTriangle(start + i0, start + i1, start + i2);
            //                     }
            //                 }
            //             }
            //             else
            //             {
            //                 var v0 = MapToUI(ApplyAffine(new Vector2(x, y)));
            //                 var v1 = MapToUI(ApplyAffine(new Vector2(x + w, y)));
            //                 var v2 = MapToUI(ApplyAffine(new Vector2(x + w, y + h)));
            //                 var v3 = MapToUI(ApplyAffine(new Vector2(x, y + h)));
            //                 int start = vh.currentVertCount;
            //                 vh.AddVert(v0, col, Vector2.zero);
            //                 vh.AddVert(v1, col, Vector2.zero);
            //                 vh.AddVert(v2, col, Vector2.zero);
            //                 vh.AddVert(v3, col, Vector2.zero);
            //                 vh.AddTriangle(start + 0, start + 1, start + 2);
            //                 vh.AddTriangle(start + 0, start + 2, start + 3);
            //             }
            //         }
            //
            //         if (stroke != null && style.StrokeWidth > 0)
            //         {
            //             if (cr > 0f)
            //             {
            //                 var pts = BuildRoundedRectPolyline(x, y, w, h, cr, 12);
            //                 var scol = SVGAUtils.ToColor(stroke);
            //                 scol.a *= _alpha;
            //                 DrawStrokePolyline(vh, pts, true, scol, style);
            //             }
            //             else
            //             {
            //                 var scol = SVGAUtils.ToColor(stroke);
            //                 scol.a *= _alpha;
            //                 DrawStrokeRect(vh, r, scol, style.StrokeWidth);
            //             }
            //         }
            //     }
            //     else if (shape.Type == ShapeEntity.Types.ShapeType.Ellipse)
            //     {
            //         var dPath = shape.Shape?.D;
            //         if (!string.IsNullOrEmpty(dPath))
            //         {
            //             var parser = new SVGAPathParser();
            //             var pts = parser.ParsePath(dPath);
            //             if (fill != null && pts != null && pts.Count > 0)
            //             {
            //                 var col = SVGAUtils.ToColor(fill);
            //                 col.a *= _alpha;
            //                 var ui = new List<Vector2>(pts.Count + 1);
            //                 for (int i = 0; i < pts.Count; i++)
            //                 {
            //                     var tp = MapToUI(ApplyAffine(new Vector2(pts[i].x, pts[i].y)));
            //                     ui.Add(new Vector2(tp.x, tp.y));
            //                 }
            //
            //                 if (ui.Count >= 2 && (ui[ui.Count - 1] - ui[0]).sqrMagnitude > 0.0001f) ui.Add(ui[0]);
            //                 SimplifyClosedPath(ui);
            //                 bool filledByFan = TryConvexTriangulateUI(vh, ui, col);
            //                 if (!filledByFan)
            //                 {
            //                     var tess = new Bo.LibTessDotNet.Tess();
            //                     var contour = new Bo.LibTessDotNet.ContourVertex[ui.Count];
            //                     for (int i = 0; i < ui.Count; i++)
            //                     {
            //                         contour[i].Position.X = ui[i].x;
            //                         contour[i].Position.Y = ui[i].y;
            //                     }
            //
            //                     tess.AddContour(contour);
            //                     tess.Tessellate(Bo.LibTessDotNet.WindingRule.NonZero,
            //                         Bo.LibTessDotNet.ElementType.Polygons, 3);
            //                     if (tess.Elements == null || tess.Elements.Length == 0)
            //                     {
            //                         tess.Tessellate(Bo.LibTessDotNet.WindingRule.EvenOdd,
            //                             Bo.LibTessDotNet.ElementType.Polygons, 3);
            //                     }
            //
            //                     var v = tess.Vertices;
            //                     var ind = tess.Elements;
            //                     int start = vh.currentVertCount;
            //                     for (int i = 0; i < v.Length; i++)
            //                     {
            //                         vh.AddVert(new Vector3(v[i].Position.X, v[i].Position.Y), col, Vector2.zero);
            //                     }
            //
            //                     for (int i = 0; i < ind.Length; i += 3)
            //                     {
            //                         var i0 = ind[i];
            //                         var i1 = ind[i + 1];
            //                         var i2 = ind[i + 2];
            //                         if (i0 < 0 || i1 < 0 || i2 < 0) continue;
            //                         vh.AddTriangle(start + i0, start + i1, start + i2);
            //                     }
            //                 }
            //             }
            //
            //             if (stroke != null && style.StrokeWidth > 0)
            //             {
            //                 var scol = SVGAUtils.ToColor(stroke);
            //                 scol.a *= _alpha;
            //                 if (pts != null && pts.Count > 1)
            //                 {
            //                     var c = new List<Vector3>(pts.Count);
            //                     for (int i = 0; i < pts.Count; i++)
            //                     {
            //                         c.Add(new Vector3(pts[i].x, pts[i].y, 0));
            //                     }
            //
            //                     bool closed = c.Count >= 2 && (c[0] - c[c.Count - 1]).sqrMagnitude < 1e-6f;
            //                     DrawStrokePolyline(vh, c, closed, scol, style);
            //                 }
            //             }
            //         }
            //         else
            //         {
            //             var e = shape.Ellipse;
            //             var cx = e.X;
            //             var cy = e.Y;
            //             var rx = e.RadiusX;
            //             var ry = e.RadiusY;
            //             const int seg = 36;
            //             var vertsLocal = new List<Vector3>(seg);
            //             for (int i = 0; i < seg; i++)
            //             {
            //                 var ang = i * Mathf.PI * 2f / seg;
            //                 vertsLocal.Add(new Vector3(cx + Mathf.Cos(ang) * rx, cy + Mathf.Sin(ang) * ry));
            //             }
            //
            //             if (fill != null)
            //             {
            //                 var col = SVGAUtils.ToColor(fill);
            //                 col.a *= _alpha;
            //                 var ui = new List<Vector2>(vertsLocal.Count + 1);
            //                 for (int i = 0; i < vertsLocal.Count; i++)
            //                 {
            //                     var tp = MapToUI(ApplyAffine(new Vector2(vertsLocal[i].x, vertsLocal[i].y)));
            //                     ui.Add(new Vector2(tp.x, tp.y));
            //                 }
            //
            //                 if ((ui[ui.Count - 1] - ui[0]).sqrMagnitude > 0.0001f) ui.Add(ui[0]);
            //                 SimplifyClosedPath(ui);
            //                 if (!TryConvexTriangulateUI(vh, ui, col))
            //                 {
            //                     var tess = new Bo.LibTessDotNet.Tess();
            //                     var contour = new Bo.LibTessDotNet.ContourVertex[ui.Count];
            //                     for (int i = 0; i < ui.Count; i++)
            //                     {
            //                         contour[i].Position.X = ui[i].x;
            //                         contour[i].Position.Y = ui[i].y;
            //                     }
            //
            //                     tess.AddContour(contour);
            //                     tess.Tessellate(Bo.LibTessDotNet.WindingRule.EvenOdd,
            //                         Bo.LibTessDotNet.ElementType.Polygons, 3);
            //                     var v = tess.Vertices;
            //                     var ind = tess.Elements;
            //                     int start = vh.currentVertCount;
            //                     for (int i = 0; i < v.Length; i++)
            //                     {
            //                         vh.AddVert(new Vector3(v[i].Position.X, v[i].Position.Y), col, Vector2.zero);
            //                     }
            //
            //                     for (int i = 0; i < ind.Length; i += 3)
            //                     {
            //                         var i0 = ind[i];
            //                         var i1 = ind[i + 1];
            //                         var i2 = ind[i + 2];
            //                         if (i0 < 0 || i1 < 0 || i2 < 0) continue;
            //                         vh.AddTriangle(start + i0, start + i1, start + i2);
            //                     }
            //                 }
            //             }
            //
            //             if (stroke != null && style.StrokeWidth > 0)
            //             {
            //                 var scol = SVGAUtils.ToColor(stroke);
            //                 scol.a *= _alpha;
            //                 DrawStrokePolyline(vh, vertsLocal, true, scol, style);
            //             }
            //         }
            //     }
            //     else if (shape.Type == ShapeEntity.Types.ShapeType.Shape)
            //     {
            //         var d = shape.Shape?.D;
            //         if (!string.IsNullOrEmpty(d))
            //         {
            //             var parser = new SVGAPathParser();
            //             var pts = parser.ParsePath(d);
            //             if (fill != null && pts != null && pts.Count > 0)
            //             {
            //                 var col = SVGAUtils.ToColor(fill);
            //                 col.a *= _alpha;
            //                 var ui = new List<Vector2>(pts.Count + 1);
            //                 for (int i = 0; i < pts.Count; i++)
            //                 {
            //                     var tp = MapToUI(ApplyAffine(new Vector2(pts[i].x, pts[i].y)));
            //                     ui.Add(new Vector2(tp.x, tp.y));
            //                 }
            //
            //                 if (ui.Count >= 2 && (ui[ui.Count - 1] - ui[0]).sqrMagnitude > 0.0001f) ui.Add(ui[0]);
            //                 SimplifyClosedPath(ui);
            //                 bool filledByFan = TryConvexTriangulateUI(vh, ui, col);
            //                 if (!filledByFan)
            //                 {
            //                     var tess = new Bo.LibTessDotNet.Tess();
            //                     var contour = new Bo.LibTessDotNet.ContourVertex[ui.Count];
            //                     for (int i = 0; i < ui.Count; i++)
            //                     {
            //                         contour[i].Position.X = ui[i].x;
            //                         contour[i].Position.Y = ui[i].y;
            //                     }
            //
            //                     tess.AddContour(contour);
            //                     tess.Tessellate(Bo.LibTessDotNet.WindingRule.NonZero,
            //                         Bo.LibTessDotNet.ElementType.Polygons, 3);
            //                     if (tess.Elements == null || tess.Elements.Length == 0)
            //                     {
            //                         tess.Tessellate(Bo.LibTessDotNet.WindingRule.EvenOdd,
            //                             Bo.LibTessDotNet.ElementType.Polygons, 3);
            //                     }
            //
            //                     var v = tess.Vertices;
            //                     var ind = tess.Elements;
            //                     int start = vh.currentVertCount;
            //                     for (int i = 0; i < v.Length; i++)
            //                     {
            //                         vh.AddVert(new Vector3(v[i].Position.X, v[i].Position.Y), col, Vector2.zero);
            //                     }
            //
            //                     for (int i = 0; i < ind.Length; i += 3)
            //                     {
            //                         var i0 = ind[i];
            //                         var i1 = ind[i + 1];
            //                         var i2 = ind[i + 2];
            //                         if (i0 < 0 || i1 < 0 || i2 < 0) continue;
            //                         vh.AddTriangle(start + i0, start + i1, start + i2);
            //                     }
            //                 }
            //             }
            //
            //             if (stroke != null && style.StrokeWidth > 0)
            //             {
            //                 var scol = SVGAUtils.ToColor(stroke);
            //                 scol.a *= _alpha;
            //                 if (pts != null && pts.Count > 1)
            //                 {
            //                     var c = new List<Vector3>(pts.Count);
            //                     for (int i = 0; i < pts.Count; i++)
            //                     {
            //                         c.Add(new Vector3(pts[i].x, pts[i].y, 0));
            //                     }
            //
            //                     bool closed = c.Count >= 2 && (c[0] - c[c.Count - 1]).sqrMagnitude < 1e-6f;
            //                     DrawStrokePolyline(vh, c, closed, scol, style);
            //                 }
            //             }
            //         }
            //     }
            //
            //     _transform = __origTransform;
            // }
        }

        private void DrawStrokeRect(VertexHelper vh, ShapeEntity.Types.RectArgs r, Color col, float w)
        {
            var x = r.X;
            var y = r.Y;
            var rw = r.Width;
            var rh = r.Height;
            var hw = w * 0.5f;
            AddQuad(vh, new Vector3(x - hw, y - hw), new Vector3(x + rw + hw, y + hw), col);
            AddQuad(vh, new Vector3(x + rw - hw, y - hw), new Vector3(x + rw + hw, y + rh + hw), col);
            AddQuad(vh, new Vector3(x - hw, y + rh - hw), new Vector3(x + rw + hw, y + rh + hw), col);
            AddQuad(vh, new Vector3(x - hw, y - hw), new Vector3(x + hw, y + rh + hw), col);
        }

        private void DrawStrokePolyline(VertexHelper vh, List<Vector3> pts, bool closed, Color col,
            ShapeEntity.Types.ShapeStyle style, bool uiSpace = false)
        {
            if (pts == null || pts.Count < 2) return;
            var w = style.StrokeWidth;
            var dash = style.LineDashI;
            var gap = style.LineDashII;
            var offset = style.LineDashIII;
            float acc = offset;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var p0 = pts[i];
                var p1 = pts[i + 1];
                var dir = (p1 - p0).normalized;
                var len = Vector3.Distance(p0, p1);
                float dpos = 0;
                while (dpos < len)
                {
                    var dlen = dash <= 0 ? len : Mathf.Min(dash, len - dpos);
                    var s = p0 + dir * dpos;
                    var e = p0 + dir * (dpos + dlen);
                    if (uiSpace) AddStrokeSegmentUI(vh, s, e, w, col, style);
                    else AddStrokeSegment(vh, s, e, w, col, style);
                    dpos += dlen + (gap <= 0 ? 0 : gap);
                }

                acc += len;
            }

            if (closed)
            {
                if (uiSpace) AddStrokeSegmentUI(vh, pts[pts.Count - 1], pts[0], w, col, style);
                else AddStrokeSegment(vh, pts[pts.Count - 1], pts[0], w, col, style);
            }
        }

        private void AddStrokeSegment(VertexHelper vh, Vector3 s, Vector3 e, float w, Color col,
            ShapeEntity.Types.ShapeStyle style)
        {
            var n = (new Vector3(-(e - s).y, (e - s).x)).normalized * (w * 0.5f);
            var v0 = MapToUI(ApplyAffine(new Vector2((s + n).x, (s + n).y)));
            var v1 = MapToUI(ApplyAffine(new Vector2((e + n).x, (e + n).y)));
            var v2 = MapToUI(ApplyAffine(new Vector2((e - n).x, (e - n).y)));
            var v3 = MapToUI(ApplyAffine(new Vector2((s - n).x, (s - n).y)));
            int start = vh.currentVertCount;
            vh.AddVert(v0, col, Vector2.zero);
            vh.AddVert(v1, col, Vector2.zero);
            vh.AddVert(v2, col, Vector2.zero);
            vh.AddVert(v3, col, Vector2.zero);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 0, start + 2, start + 3);
            if (style.LineCap == ShapeEntity.Types.ShapeStyle.Types.LineCap.Round)
            {
                var capSeg = 20;
                var dir = (e - s).normalized;
                var ang0 = Mathf.Atan2(dir.y, dir.x);
                for (int k = 0; k < 2; k++)
                {
                    var center = k == 0 ? s : e;
                    int cStart = vh.currentVertCount;
                    vh.AddVert(MapToUI(ApplyAffine(new Vector2(center.x, center.y))), col, Vector2.zero);
                    for (int i = 0; i <= capSeg; i++)
                    {
                        var ang = ang0 + (k == 0 ? 0.5f : -0.5f) * Mathf.PI + (i / (float)capSeg) * Mathf.PI;
                        var p = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * (w * 0.5f);
                        vh.AddVert(MapToUI(ApplyAffine(new Vector2(p.x, p.y))), col, Vector2.zero);
                    }

                    for (int i = 1; i <= capSeg; i++)
                    {
                        vh.AddTriangle(cStart, cStart + i, cStart + i + 1);
                    }
                }
            }
            else if (style.LineCap == ShapeEntity.Types.ShapeStyle.Types.LineCap.Square)
            {
                var dir = (e - s).normalized * (w * 0.5f);
                AddQuad(vh, s - dir - n, s - dir + n, col);
                AddQuad(vh, e + dir - n, e + dir + n, col);
            }
        }

        private void AddStrokeSegmentUI(VertexHelper vh, Vector3 s, Vector3 e, float w, Color col,
            ShapeEntity.Types.ShapeStyle style)
        {
            var n = (new Vector3(-(e - s).y, (e - s).x)).normalized * (w * 0.5f);
            var v0 = new Vector3((s + n).x, (s + n).y, 0);
            var v1 = new Vector3((e + n).x, (e + n).y, 0);
            var v2 = new Vector3((e - n).x, (e - n).y, 0);
            var v3 = new Vector3((s - n).x, (s - n).y, 0);
            int start = vh.currentVertCount;
            vh.AddVert(v0, col, Vector2.zero);
            vh.AddVert(v1, col, Vector2.zero);
            vh.AddVert(v2, col, Vector2.zero);
            vh.AddVert(v3, col, Vector2.zero);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 0, start + 2, start + 3);
            if (style.LineCap == ShapeEntity.Types.ShapeStyle.Types.LineCap.Round)
            {
                var capSeg = 20;
                var dir = (e - s).normalized;
                var ang0 = Mathf.Atan2(dir.y, dir.x);
                for (int k = 0; k < 2; k++)
                {
                    var center = k == 0 ? s : e;
                    int cStart = vh.currentVertCount;
                    vh.AddVert(new Vector3(center.x, center.y, 0), col, Vector2.zero);
                    for (int i = 0; i <= capSeg; i++)
                    {
                        var ang = ang0 + (k == 0 ? 0.5f : -0.5f) * Mathf.PI + (i / (float)capSeg) * Mathf.PI;
                        var p = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * (w * 0.5f);
                        vh.AddVert(new Vector3(p.x, p.y, 0), col, Vector2.zero);
                    }

                    for (int i = 1; i <= capSeg; i++)
                    {
                        vh.AddTriangle(cStart, cStart + i, cStart + i + 1);
                    }
                }
            }
            else if (style.LineCap == ShapeEntity.Types.ShapeStyle.Types.LineCap.Square)
            {
                var dir = (e - s).normalized * (w * 0.5f);
                AddQuadUI(vh, s - dir - n, s - dir + n, col);
                AddQuadUI(vh, e + dir - n, e + dir + n, col);
            }
        }

        private void AddQuad(VertexHelper vh, Vector3 min, Vector3 max, Color col)
        {
            int start = vh.currentVertCount;
            var p0 = MapToUI(ApplyAffine(new Vector2(min.x, min.y)));
            var p1 = MapToUI(ApplyAffine(new Vector2(max.x, min.y)));
            var p2 = MapToUI(ApplyAffine(new Vector2(max.x, max.y)));
            var p3 = MapToUI(ApplyAffine(new Vector2(min.x, max.y)));
            vh.AddVert(p0, col, Vector2.zero);
            vh.AddVert(p1, col, Vector2.zero);
            vh.AddVert(p2, col, Vector2.zero);
            vh.AddVert(p3, col, Vector2.zero);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 0, start + 2, start + 3);
        }

        private void AddQuadUI(VertexHelper vh, Vector3 min, Vector3 max, Color col)
        {
            int start = vh.currentVertCount;
            var p0 = new Vector3(min.x, min.y, 0);
            var p1 = new Vector3(max.x, min.y, 0);
            var p2 = new Vector3(max.x, max.y, 0);
            var p3 = new Vector3(min.x, max.y, 0);
            vh.AddVert(p0, col, Vector2.zero);
            vh.AddVert(p1, col, Vector2.zero);
            vh.AddVert(p2, col, Vector2.zero);
            vh.AddVert(p3, col, Vector2.zero);
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 0, start + 2, start + 3);
        }

        private List<Vector3> BuildRoundedRectPolyline(float x, float y, float w, float h, float r, int seg)
        {
            var pts = new List<Vector3>();
            float rr = Mathf.Max(0f, Mathf.Min(r, Mathf.Min(w, h) * 0.5f));
            float xMin = x;
            float yMin = y;
            float xMax = x + w;
            float yMax = y + h;
            if (rr <= 0f)
            {
                pts.Add(new Vector3(xMin, yMin));
                pts.Add(new Vector3(xMax, yMin));
                pts.Add(new Vector3(xMax, yMax));
                pts.Add(new Vector3(xMin, yMax));
                return pts;
            }

            int k = Mathf.Max(4, seg);
            // Top edge
            pts.Add(new Vector3(xMin + rr, yMin));
            pts.Add(new Vector3(xMax - rr, yMin));
            // Top-right corner
            var ctrTR = new Vector2(xMax - rr, yMin + rr);
            for (int i = 0; i <= k; i++)
            {
                float ang = -Mathf.PI * 0.5f + (i / (float)k) * (Mathf.PI * 0.5f);
                pts.Add(new Vector3(ctrTR.x + Mathf.Cos(ang) * rr, ctrTR.y + Mathf.Sin(ang) * rr));
            }

            // Right edge
            pts.Add(new Vector3(xMax, yMin + rr));
            pts.Add(new Vector3(xMax, yMax - rr));
            // Bottom-right corner
            var ctrBR = new Vector2(xMax - rr, yMax - rr);
            for (int i = 0; i <= k; i++)
            {
                float ang = 0f + (i / (float)k) * (Mathf.PI * 0.5f);
                pts.Add(new Vector3(ctrBR.x + Mathf.Cos(ang) * rr, ctrBR.y + Mathf.Sin(ang) * rr));
            }

            // Bottom edge
            pts.Add(new Vector3(xMax - rr, yMax));
            pts.Add(new Vector3(xMin + rr, yMax));
            // Bottom-left corner
            var ctrBL = new Vector2(xMin + rr, yMax - rr);
            for (int i = 0; i <= k; i++)
            {
                float ang = Mathf.PI * 0.5f + (i / (float)k) * (Mathf.PI * 0.5f);
                pts.Add(new Vector3(ctrBL.x + Mathf.Cos(ang) * rr, ctrBL.y + Mathf.Sin(ang) * rr));
            }

            // Left edge
            pts.Add(new Vector3(xMin, yMax - rr));
            pts.Add(new Vector3(xMin, yMin + rr));
            // Top-left corner
            var ctrTL = new Vector2(xMin + rr, yMin + rr);
            for (int i = 0; i <= k; i++)
            {
                float ang = Mathf.PI + (i / (float)k) * (Mathf.PI * 0.5f);
                pts.Add(new Vector3(ctrTL.x + Mathf.Cos(ang) * rr, ctrTL.y + Mathf.Sin(ang) * rr));
            }

            return pts;
        }


        private void SimplifyClosedPath(List<Vector2> closed)
        {
            if (closed == null || closed.Count <= 3) return;
            const float minDistSq = 0.01f * 0.01f;
            var tmp = new List<Vector2>(closed.Count);
            tmp.Add(closed[0]);
            for (int i = 1; i < closed.Count; i++)
            {
                if ((closed[i] - tmp[tmp.Count - 1]).sqrMagnitude > minDistSq)
                    tmp.Add(closed[i]);
            }

            if ((tmp[tmp.Count - 1] - tmp[0]).sqrMagnitude > minDistSq)
                tmp.Add(tmp[0]);
            const float collinearCrossTol = 1e-4f;
            var simplified = new List<Vector2>(tmp.Count);
            simplified.Add(tmp[0]);
            for (int i = 1; i < tmp.Count - 1; i++)
            {
                var a = simplified[simplified.Count - 1];
                var b = tmp[i];
                var c = tmp[i + 1];
                var ab = b - a;
                var bc = c - b;
                var cross = Mathf.Abs(ab.x * bc.y - ab.y * bc.x);
                var denom = (ab.sqrMagnitude + bc.sqrMagnitude);
                if (denom < 1e-8f || cross < collinearCrossTol)
                {
                    continue;
                }

                simplified.Add(b);
            }

            simplified.Add(tmp[tmp.Count - 1]);
            closed.Clear();
            closed.AddRange(simplified);
        }

        private bool TryConvexTriangulateUI(VertexHelper vh, List<Vector2> closed, Color col)
        {
            if (closed == null || closed.Count < 4) return false;
            int n = closed.Count - 1;
            if (n < 3) return false;

            bool? sign = null;
            for (int i = 0; i < n; i++)
            {
                var a = closed[i];
                var b = closed[(i + 1) % n];
                var c = closed[(i + 2) % n];
                var ab = b - a;
                var bc = c - b;
                float cross = ab.x * bc.y - ab.y * bc.x;
                if (Mathf.Abs(cross) < 1e-6f) continue;
                bool cs = cross > 0f;
                if (sign == null) sign = cs;
                else if (sign.Value != cs) return false;
            }

            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                vh.AddVert(new Vector3(closed[i].x, closed[i].y), col, Vector2.zero);
            }

            for (int i = 1; i < n - 1; i++)
            {
                vh.AddTriangle(start + 0, start + i, start + i + 1);
            }

            return true;
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

        private Vector3 MapToUI(Vector2 mv)
        {
            var rect = rectTransform.rect;
            return new Vector3(mv.x - rect.width * 0.5f, rect.height * 0.5f - mv.y, 0f);
        }

        private Transform ComposeTransform(Transform g, Transform l)
        {
            var gi = g ?? new Transform { A = 1f, D = 1f, B = 0f, C = 0f, Tx = 0f, Ty = 0f };
            var li = l ?? new Transform { A = 1f, D = 1f, B = 0f, C = 0f, Tx = 0f, Ty = 0f };
            var r = new Transform();
            r.A = gi.A * li.A + gi.C * li.B;
            r.B = gi.B * li.A + gi.D * li.B;
            r.C = gi.A * li.C + gi.C * li.D;
            r.D = gi.B * li.C + gi.D * li.D;
            r.Tx = gi.A * li.Tx + gi.C * li.Ty + gi.Tx;
            r.Ty = gi.B * li.Tx + gi.D * li.Ty + gi.Ty;
            return r;
        }
    }
}
