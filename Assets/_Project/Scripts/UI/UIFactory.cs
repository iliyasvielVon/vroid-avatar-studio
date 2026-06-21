using System;
using UnityEngine;
using UnityEngine.UI;

namespace AvatarStudio
{
    /// <summary>Minimal code-only uGUI builders so the whole 形象页 can be created at runtime.</summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new(0.10f, 0.11f, 0.15f, 0.92f);
        public static readonly Color AccentColor = new(0.45f, 0.72f, 1.00f, 1f);
        public static readonly Color RowColor = new(1f, 1f, 1f, 0.04f);
        public static readonly Color TextColor = new(0.93f, 0.95f, 1.00f, 1f);

        /// <summary>Global UI-click counter — used by the on-screen diagnostic HUD.</summary>
        public static int Clicks;

        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 16);
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        static GameObject New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void Stretch(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static Image Panel(Transform parent, string name, Color color, bool raycast = true)
        {
            var go = New(name, parent);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var go = New("Label", parent);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; // labels must never swallow clicks meant for controls behind them
            return t;
        }

        public static Button Button(Transform parent, string text, Color bg, Action onClick)
        {
            var go = New("Button", parent);
            var img = go.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = true;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // Explicit, visible click feedback. ColorTint drives the image from the ColorBlock, so the
            // block must be based on `bg` (otherwise every button would render the default white).
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = bg;
            cb.highlightedColor = Brighten(bg, 1.25f);
            cb.pressedColor = Brighten(bg, 0.7f);
            cb.selectedColor = bg;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            btn.colors = cb;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() => { Clicks++; onClick?.Invoke(); });

            var label = Label(go.transform, text, 22, TextAnchor.MiddleCenter, TextColor);
            Stretch(label.rectTransform, 0, 0, 0, 0);
            return btn;
        }

        static Color Brighten(Color c, float f) =>
            new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);

        /// <summary>Recolor a button's base (normal/selected) color so it survives the ColorTint transition.</summary>
        public static void SetBaseColor(Button btn, Color bg)
        {
            if (btn == null) return;
            var cb = btn.colors;
            cb.normalColor = bg;
            cb.highlightedColor = Brighten(bg, 1.25f);
            cb.pressedColor = Brighten(bg, 0.7f);
            cb.selectedColor = bg;
            btn.colors = cb;
            if (btn.targetGraphic is Graphic g) g.color = bg; // immediate, no wait for state refresh
        }

        /// <summary>A horizontal "label + slider" row. Returns the Slider.</summary>
        public static Slider SliderRow(Transform parent, string label, float value, Action<float> onChanged)
        {
            var row = New("Row", parent);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = RowColor;
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 46;
            le.preferredHeight = 46;

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;
            hl.padding = new RectOffset(12, 12, 4, 4);
            hl.spacing = 10;

            var lab = Label(row.transform, label, 18, TextAnchor.MiddleLeft, TextColor);
            var labLe = lab.gameObject.AddComponent<LayoutElement>();
            labLe.minWidth = 150; labLe.preferredWidth = 150;

            var slider = BuildSlider(row.transform, value);
            var sLe = slider.gameObject.AddComponent<LayoutElement>();
            sLe.flexibleWidth = 1;
            if (onChanged != null) slider.onValueChanged.AddListener(v => onChanged(v));
            return slider;
        }

        public static Slider BuildSlider(Transform parent, float value)
        {
            var go = New("Slider", parent);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = value;

            var bg = Panel(go.transform, "Background", new Color(1, 1, 1, 0.12f));
            Stretch(bg.rectTransform, 0, 0, 0, 0);
            bg.rectTransform.anchorMin = new Vector2(0, 0.4f);
            bg.rectTransform.anchorMax = new Vector2(1, 0.6f);
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;

            var fillArea = New("Fill Area", go.transform);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.4f);
            faRt.anchorMax = new Vector2(1, 0.6f);
            faRt.offsetMin = new Vector2(0, 0);
            faRt.offsetMax = new Vector2(0, 0);
            var fill = Panel(fillArea.transform, "Fill", AccentColor);
            fill.rectTransform.anchorMin = new Vector2(0, 0);
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.sizeDelta = new Vector2(10, 0);

            var handleArea = New("Handle Slide Area", go.transform);
            var haRt = handleArea.GetComponent<RectTransform>();
            Stretch(haRt, 0, 0, 0, 0);
            var handle = Panel(handleArea.transform, "Handle", Color.white);
            handle.rectTransform.sizeDelta = new Vector2(18, 18);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        /// <summary>A vertical scroll list. Returns the content transform you add rows to.</summary>
        public static RectTransform ScrollList(Transform parent)
        {
            var viewportGo = New("ScrollView", parent);
            Stretch(viewportGo.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24;

            var viewport = Panel(viewportGo.transform, "Viewport", new Color(0, 0, 0, 0));
            Stretch(viewport.rectTransform, 0, 0, 0, 0);
            // RectMask2D clips by rectangle and (unlike Mask) does not depend on the masking image's
            // alpha. A Mask on a fully-transparent image writes no stencil, which clips every child
            // away — that is exactly why the slider/button rows were invisible.
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;

            var content = New("Content", viewport.transform);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = new Vector2(0, 0);
            crt.offsetMax = new Vector2(0, 0);

            var vl = content.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth = true; vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            vl.spacing = 6;
            vl.padding = new RectOffset(6, 6, 6, 6);
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = crt;
            return crt;
        }

        public static Text SectionHeader(Transform parent, string text)
        {
            var lab = Label(parent, text, 18, TextAnchor.MiddleLeft, AccentColor);
            var le = lab.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 30; le.preferredHeight = 30;
            return lab;
        }
    }
}
