using System.Collections.Generic;
using UnityEngine;

namespace AvatarStudio
{
    /// <summary>
    /// 捏脸 / shape system. Auto-discovers every blendshape on the loaded avatar and exposes
    /// each as a 0..1 weight. Works with VRoid expression blendshapes out of the box, and with
    /// custom face-proportion morphs (eye size, jaw width, ...) injected via Blender — see
    /// Tools/blender_add_face_morphs.py.
    /// </summary>
    public class FaceCustomizer
    {
        public struct Morph
        {
            public SkinnedMeshRenderer Renderer;
            public int Index;
            public string RawName;
            public string DisplayName;
            public string Group;
        }

        readonly List<Morph> _morphs = new();
        public IReadOnlyList<Morph> Morphs => _morphs;

        public FaceCustomizer(AvatarContext ctx)
        {
            foreach (var smr in ctx.SkinnedRenderers)
            {
                var mesh = smr.sharedMesh;
                if (mesh == null) continue;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string raw = mesh.GetBlendShapeName(i);
                    _morphs.Add(new Morph
                    {
                        Renderer = smr,
                        Index = i,
                        RawName = raw,
                        DisplayName = Prettify(raw, out var group),
                        Group = group,
                    });
                }
            }
        }

        public float GetWeight01(int morphIndex)
        {
            var m = _morphs[morphIndex];
            return Mathf.Clamp01(m.Renderer.GetBlendShapeWeight(m.Index) / 100f);
        }

        public void SetWeight01(int morphIndex, float value01)
        {
            var m = _morphs[morphIndex];
            m.Renderer.SetBlendShapeWeight(m.Index, Mathf.Clamp01(value01) * 100f);
        }

        public void ResetAll()
        {
            for (int i = 0; i < _morphs.Count; i++) SetWeight01(i, 0f);
        }

        /// <summary>Turn "Fcl_EYE_Close_R" into group "EYE" + name "Close R".</summary>
        static string Prettify(string raw, out string group)
        {
            group = "其他";
            if (string.IsNullOrEmpty(raw)) return raw;

            string s = raw;
            // VRoid blendshapes are usually "Fcl_<GROUP>_<Name>".
            if (s.StartsWith("Fcl_"))
            {
                var parts = s.Substring(4).Split('_');
                if (parts.Length >= 1) group = parts[0];
                if (parts.Length >= 2) s = string.Join(" ", parts, 1, parts.Length - 1);
                else s = parts.Length > 0 ? parts[0] : s;
            }
            else
            {
                int us = s.IndexOf('_');
                if (us > 0) group = s.Substring(0, us);
            }
            return s.Replace('_', ' ');
        }
    }
}
