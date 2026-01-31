using UnityEngine;
using UnityEngine.UI;

namespace Bo.SVGA
{
    public class SVGAImageAffineGraphic : MaskableGraphic
    {
        private Texture2D _texture;
        private Vector2 _size;
        private Vector2 _viewBox;
        private Transform _transform;
        private Layout _layout;

        public override Texture mainTexture => _texture != null ? _texture : base.mainTexture;

        public void SetSpriteFromPng(byte[] pngBytes)
        {
            if (pngBytes == null) return;
            _texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _texture.LoadImage(pngBytes);
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.filterMode = FilterMode.Bilinear;
            _texture.anisoLevel = 0;
            var sh = Shader.Find("UI/SVGAPremulUI");
            if (sh == null) sh = Shader.Find("UI/Default");
            // SVGALog.Log("Shader: " + sh.name);  
            material = sh != null ? new Material(sh) : new Material(defaultGraphicMaterial);
            material.mainTexture = _texture;
            if (_size == Vector2.zero)
            {
                _size = new Vector2(_texture.width, _texture.height);
            }
            SetAllDirty();
        }

        /// <summary>
        /// 设置动画帧.
        /// </summary>
        /// <param name="alpha"></param>
        /// <param name="viewBox"></param>
        /// <param name="layout"></param>
        /// <param name="t"></param>
        public void SetFrame(float alpha, Vector2 viewBox, Layout layout, Transform t, int idx, string imageKey)
        {
            _viewBox = viewBox;
            _layout = layout;
            if (layout != null)
            {
                _size = new Vector2(layout.Width, layout.Height);
            }
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
            if (_texture == null) return;
            if (_size.x <= 0 || _size.y <= 0)
            {
                _size = new Vector2(_texture.width, _texture.height);
            }

            var w = _size.x;
            var h = _size.y;

            // SVGA quad corners in local element space (top-left origin)
            Vector2 p0 = new Vector2(0, 0);
            Vector2 p1 = new Vector2(w, 0);
            Vector2 p2 = new Vector2(w, h);
            Vector2 p3 = new Vector2(0, h);

            // Apply full affine transform (a,b,c,d,tx,ty) in SVGA movie space
            Vector2 t0 = ApplyAffine(p0);
            Vector2 t1 = ApplyAffine(p1);
            Vector2 t2 = ApplyAffine(p2);
            Vector2 t3 = ApplyAffine(p3);

            // Map SVGA movie space to Unity UI local space (center at (0,0), y up)
            Vector3 v0 = new Vector3(t0.x - _viewBox.x * 0.5f, _viewBox.y * 0.5f - t0.y);
            Vector3 v1 = new Vector3(t1.x - _viewBox.x * 0.5f, _viewBox.y * 0.5f - t1.y);
            Vector3 v2 = new Vector3(t2.x - _viewBox.x * 0.5f, _viewBox.y * 0.5f - t2.y);
            Vector3 v3 = new Vector3(t3.x - _viewBox.x * 0.5f, _viewBox.y * 0.5f - t3.y);

            var col = color;
            int start = vh.currentVertCount;
            vh.AddVert(v0, col, new Vector2(0, 1));
            vh.AddVert(v1, col, new Vector2(1, 1));
            vh.AddVert(v2, col, new Vector2(1, 0));
            vh.AddVert(v3, col, new Vector2(0, 0));
            vh.AddTriangle(start + 0, start + 1, start + 2);
            vh.AddTriangle(start + 0, start + 2, start + 3);
        }


        /// <summary>
        /// 应用仿射变换
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Vector2 ApplyAffine(Vector2 p)
        {
            if (_transform == null) return p;
            float x = p.x;
            float y = p.y;
            float a = _transform.A;
            float b = _transform.B;
            float c = _transform.C;
            float d = _transform.D;
            float tx = _transform.Tx;
            float ty = _transform.Ty;
            float nx = a * x + c * y + tx;
            float ny = b * x + d * y + ty;
            return new Vector2(nx, ny);
        }
    }
}
