using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Bo.SVGA
{
    public enum SpriteEntityType
    {
        Image = 0,
        Shape = 1,
    }

    public class SVGASpriteEntity : MonoBehaviour, IPointerClickHandler
    {
        public SpriteEntityType SpriteEntityType = SpriteEntityType.Image;
        public string ImageKey { get; private set; }
        public SVGADat Data { get; private set; }
        public SpriteEntity SrcData { get; private set; }
        public Graphic Graphic { get; private set; }
        public RectTransform RectTransform { get; private set; }
        public Action<PointerEventData> OnClick;

        private FrameEntity _lastFrameEntity;

        private class TextContext
        {
            public Text Text;
            public Color OriginalColor;
            public Vector3 Offset = Vector3.zero;

            public TextContext(Text text, Color c, Vector3 offset)
            {
                Text = text;
                OriginalColor = c;
                Offset = offset;
            }
        }

        private List<TextContext> _textContexts = new List<TextContext>();

        private SVGAMaskGraphic _maskGraphic;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(eventData);
        }

        public static SVGASpriteEntity Create(SpriteEntityType type, SVGADat data, SpriteEntity spriteEntity,
            UnityEngine.Transform parent)
        {
            var go = new GameObject(spriteEntity.ImageKey);
            go.transform.SetParent(parent, false);
            var comp = go.AddComponent<SVGASpriteEntity>();
            comp.Initialize(type, data, spriteEntity);
            comp.SetRaycastTarget(false);
            return comp;
        }

        private void Initialize(SpriteEntityType type, SVGADat data, SpriteEntity spriteEntity)
        {
            SpriteEntityType = type;
            Data = data;
            SrcData = spriteEntity;
            ImageKey = SrcData.ImageKey;
            RectTransform = gameObject.AddComponent<RectTransform>();
            RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            RectTransform.pivot = new Vector2(0.5f, 0.5f);
            RectTransform.anchoredPosition = Vector2.zero;

            if (SpriteEntityType == SpriteEntityType.Image)
            {
                var g = gameObject.AddComponent<SVGAImageAffineGraphic>();
                if (gameObject.GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
                Graphic = g;
                var bin = Data.GetImageBinary(ImageKey);
                if (bin != null) g.SetSpriteFromPng(bin);
            }
            else
            {
                var g = gameObject.AddComponent<SVGAShapeGraphic>();
                if (gameObject.GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
                Graphic = g;
            }
        }

        /// <summary>
        /// 应用一帧, 用于播放时更新显示
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="cacheMeshes"></param>
        public void ApplyFrame(FrameEntity frame, List<SVGAFrameCache.ShapeMesh> cacheMeshes, int spriteIdx,
            int frameIdx, string imageKey)
        {
            _lastFrameEntity = frame;
            if (frame == null) return;
            var alpha = frame.Alpha;
            var layout = frame.Layout;
            if (layout != null)
            {
                RectTransform.sizeDelta = new Vector2(layout.Width, layout.Height);
            }

            var t = frame.Transform;
            if (SpriteEntityType == SpriteEntityType.Image)
            {
                var ig = Graphic as SVGAImageAffineGraphic;
                ig?.SetFrame(alpha, Data.Size, layout, t, frameIdx, imageKey);
                RectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                var sg = Graphic as SVGAShapeGraphic;
                sg?.SetFrame(alpha, Data.Size, layout, t, frameIdx, imageKey);
                RectTransform.anchoredPosition = Vector2.zero;
            }

            if (Graphic != null)
            {
                if (SpriteEntityType == SpriteEntityType.Image)
                {
                    var c = Graphic.color;
                    c.a = frame.Alpha <= 0 ? 0 : frame.Alpha >= 1 ? 1 : frame.Alpha;
                    Graphic.color = c;
                }
            }

            if (!string.IsNullOrEmpty(frame.ClipPath))
            {
                EnsureMask();
                _maskGraphic.SetFrame(Data.Size, layout, t);
                _maskGraphic.SetPath(frame.ClipPath);
            }
            else if (_maskGraphic != null)
            {
                _maskGraphic.gameObject.SetActive(false);
            }

            if (SpriteEntityType == SpriteEntityType.Shape)
            {
                var sg = Graphic as SVGAShapeGraphic;
                sg?.SetShapes(frame.Shapes);
                if (cacheMeshes != null)
                {
                    sg?.SetCacheMeshes(cacheMeshes, spriteIdx, frameIdx, imageKey);
                }

                Graphic.SetAllDirty();
            }

            UpdateTexts(frame);
        }

        private void UpdateTexts(FrameEntity frame)
        {
            if (_textContexts.Count == 0 || frame == null) return;

            // Prepare transform data
            var t = frame.Transform;
            float a = t != null ? t.A : 1f;
            float b = t != null ? t.B : 0f;
            float c = t != null ? t.C : 0f;
            float d = t != null ? t.D : 1f;
            float tx = t != null ? t.Tx : 0f;
            float ty = t != null ? t.Ty : 0f;

            // Layout data
            float lx = 0f;
            float ly = 0f;
            if (frame.Layout != null)
            {
                lx = frame.Layout.X;
                ly = frame.Layout.Y;
            }

            // Current Size (Updated in ApplyFrame)
            float width = RectTransform.sizeDelta.x;
            float height = RectTransform.sizeDelta.y;

            // ViewBox size
            float vw = Data != null ? Data.Size.x : 0f;
            float vh = Data != null ? Data.Size.y : 0f;

            for (int i = _textContexts.Count - 1; i >= 0; i--)
            {
                var ctx = _textContexts[i];
                if (ctx.Text == null)
                {
                    _textContexts.RemoveAt(i);
                    continue;
                }

                // Update Alpha
                var col = ctx.OriginalColor;
                col.a *= frame.Alpha;
                ctx.Text.color = col;

                // Update Size
                var rt = ctx.Text.rectTransform;
                rt.sizeDelta = new Vector2(width, height);

                // Update Transform
                // Center of the sprite in SVGA space
                float cx = width * 0.5f;
                float cy = height * 0.5f;

                // Apply Affine Transform to Center
                // x' = a*x + c*y + tx
                // y' = b*x + d*y + ty
                float nx = a * cx + c * cy + tx + lx;
                float ny = b * cx + d * cy + ty + ly;

                // Convert to Unity Local Position (relative to ViewBox center)
                // Unity X = SVGA X - ViewBoxWidth/2
                // Unity Y = ViewBoxHeight/2 - SVGA Y
                float unityX = nx - vw * 0.5f;
                float unityY = vh * 0.5f - ny;

                rt.localPosition = new Vector3(unityX, unityY, 0) + ctx.Offset;

                // Update Rotation & Scale
                // ScaleX = sqrt(a^2 + b^2)
                // ScaleY = sqrt(c^2 + d^2)
                float sx = Mathf.Sqrt(a * a + b * b);
                float sy = Mathf.Sqrt(c * c + d * d);

                // Rotation = atan2(b, a)
                float angle = Mathf.Atan2(b, a) * Mathf.Rad2Deg;

                rt.localScale = new Vector3(sx, sy, 1f);
                rt.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }

        private void EnsureMask()
        {
            if (_maskGraphic != null)
            {
                _maskGraphic.gameObject.SetActive(true);
                return;
            }

            var maskGo = new GameObject($"{ImageKey}_mask");
            var currentParent = transform.parent;
            maskGo.transform.SetParent(currentParent, false);
            maskGo.transform.SetSiblingIndex(transform.GetSiblingIndex());
            var rt = maskGo.AddComponent<RectTransform>();
            var childRt = RectTransform;
            rt.anchorMin = childRt.anchorMin;
            rt.anchorMax = childRt.anchorMax;
            rt.pivot = childRt.pivot;
            rt.anchoredPosition = childRt.anchoredPosition;
            rt.sizeDelta = childRt.sizeDelta;
            if (maskGo.GetComponent<CanvasRenderer>() == null) maskGo.AddComponent<CanvasRenderer>();
            _maskGraphic = maskGo.AddComponent<SVGAMaskGraphic>();
            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            transform.SetParent(maskGo.transform, false);
        }

        public void SetRaycastTarget(bool enable)
        {
            Graphic.raycastTarget = enable;
        }

        public Text AddText(string text, Font font, int fontSize, Color color, Vector3 offset)
        {
            if (offset == null)
                offset = Vector3.zero;
            GameObject obj = new GameObject($"{ImageKey}_label");
            obj.transform.SetParent(transform);
            RectTransform tran = obj.GetComponent<RectTransform>();
            if (tran == null)
                tran = obj.AddComponent<RectTransform>();
            var thisTrans = GetComponent<RectTransform>();
            tran.sizeDelta = thisTrans.sizeDelta;
            tran.localScale = Vector3.one;
            tran.anchoredPosition = Vector2.zero;
            tran.localPosition = thisTrans.localPosition + offset;
            var textComp = obj.AddComponent<Text>();
            if (font != null)
                textComp.font = font;
            textComp.fontSize = fontSize;
            textComp.text = text;
            textComp.color = color;
            textComp.alignment = TextAnchor.MiddleCenter;
            _textContexts.Add(new TextContext(textComp, color, offset));
            UpdateTexts(_lastFrameEntity);
            return textComp;
        }
    }
}