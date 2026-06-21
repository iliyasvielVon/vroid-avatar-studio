using System.Collections.Generic;
using UnityEngine;

namespace AvatarStudio
{
    /// <summary>
    /// 换肤色. Multiplies the base/shade color of every skin material (MToon) by a tint so the
    /// underlying VRoid skin texture is preserved while the overall tone shifts.
    /// </summary>
    public class SkinColorChanger
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int ShadeColorId = Shader.PropertyToID("_ShadeColor");

        struct Entry
        {
            public Material Mat;
            public Color BaseColor;
            public Color ShadeColor;
            public bool HasShade;
        }

        readonly List<Entry> _entries = new();
        public int SkinMaterialCount => _entries.Count;

        // Tone presets (multiplicative tints): 白皙 / 自然 / 小麦 / 古铜
        public static readonly (string label, Color tint)[] Presets =
        {
            ("白皙", new Color(1.00f, 0.98f, 0.97f)),
            ("自然", new Color(1.00f, 0.93f, 0.87f)),
            ("健康", new Color(0.96f, 0.82f, 0.70f)),
            ("小麦", new Color(0.85f, 0.66f, 0.52f)),
            ("古铜", new Color(0.70f, 0.50f, 0.38f)),
        };

        public SkinColorChanger(AvatarContext ctx)
        {
            foreach (var m in ctx.SkinMaterials)
            {
                var e = new Entry { Mat = m };
                e.BaseColor = m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.white;
                if (m.HasProperty(ShadeColorId))
                {
                    e.ShadeColor = m.GetColor(ShadeColorId);
                    e.HasShade = true;
                }
                _entries.Add(e);
            }
        }

        /// <summary>Apply a multiplicative tint to all skin materials.</summary>
        public void SetTint(Color tint)
        {
            foreach (var e in _entries)
            {
                if (e.Mat.HasProperty(ColorId))
                    e.Mat.SetColor(ColorId, MultiplyRGB(e.BaseColor, tint));
                if (e.HasShade)
                    e.Mat.SetColor(ShadeColorId, MultiplyRGB(e.ShadeColor, tint));
            }
        }

        public void Reset() => SetTint(Color.white);

        static Color MultiplyRGB(Color a, Color b) =>
            new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
    }
}
