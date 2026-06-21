using System.Collections.Generic;
using UnityEngine;

namespace AvatarStudio
{
    /// <summary>
    /// Caches everything the customization systems need from a freshly loaded VRM:
    /// the root, the humanoid Animator, every SkinnedMeshRenderer, the face renderer
    /// (the one carrying the most blendshapes), and the materials that look like skin.
    /// </summary>
    public class AvatarContext
    {
        public GameObject Root { get; private set; }
        public Animator Animator { get; private set; }
        public readonly List<SkinnedMeshRenderer> SkinnedRenderers = new();
        public readonly List<Renderer> AllRenderers = new();

        /// <summary>Renderer that owns the largest number of blendshapes (usually the VRoid "Face").</summary>
        public SkinnedMeshRenderer FaceRenderer { get; private set; }

        /// <summary>Materials whose name looks like bare skin (face / body).</summary>
        public readonly List<Material> SkinMaterials = new();

        public static AvatarContext Build(GameObject root)
        {
            var ctx = new AvatarContext { Root = root };
            ctx.Animator = root.GetComponent<Animator>();

            int bestBlendShapeCount = -1;
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                ctx.SkinnedRenderers.Add(smr);
                ctx.AllRenderers.Add(smr);
                int count = smr.sharedMesh != null ? smr.sharedMesh.blendShapeCount : 0;
                if (count > bestBlendShapeCount)
                {
                    bestBlendShapeCount = count;
                    ctx.FaceRenderer = smr;
                }
            }
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                ctx.AllRenderers.Add(mr);
            }

            CollectSkinMaterials(ctx);
            return ctx;
        }

        static void CollectSkinMaterials(AvatarContext ctx)
        {
            var seen = new HashSet<Material>();
            foreach (var r in ctx.AllRenderers)
            {
                foreach (var m in r.materials) // instanced copies, safe to tint
                {
                    if (m == null || seen.Contains(m)) continue;
                    seen.Add(m);
                    string n = m.name.ToUpperInvariant();
                    if (n.Contains("SKIN") || n.Contains("FACE") || n.Contains("BODY"))
                    {
                        ctx.SkinMaterials.Add(m);
                    }
                }
            }
        }
    }
}
