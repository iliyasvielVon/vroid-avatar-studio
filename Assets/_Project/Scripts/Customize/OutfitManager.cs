using System.Collections.Generic;
using UnityEngine;

namespace AvatarStudio
{
    /// <summary>
    /// 换装. Two strategies:
    ///  1) Toggle the avatar's own outfit/hair/accessory renderers on and off (works when a single
    ///     VRoid VRM already carries several wearables).
    ///  2) AttachSkinnedClothing: re-bind a SkinnedMeshRenderer coming from a *separate* export onto
    ///     the base skeleton by matching bone names — true mix-and-match across files.
    /// </summary>
    public class OutfitManager
    {
        public class Item
        {
            public Renderer Renderer;
            public string DisplayName;
            public string Category;
            public bool On => Renderer != null && Renderer.enabled;
        }

        readonly List<Item> _items = new();
        public IReadOnlyList<Item> Items => _items;

        public OutfitManager(AvatarContext ctx)
        {
            foreach (var r in ctx.AllRenderers)
            {
                string cat = Categorize(r.name, out bool isBodyOrFace);
                if (isBodyOrFace) continue; // never let the user toggle off bare skin/face
                _items.Add(new Item { Renderer = r, DisplayName = r.name, Category = cat });
            }
        }

        public void SetOn(Item item, bool on)
        {
            if (item?.Renderer != null) item.Renderer.enabled = on;
        }

        public void Toggle(Item item) => SetOn(item, !item.On);

        static string Categorize(string name, out bool isBodyOrFace)
        {
            string n = name.ToLowerInvariant();
            isBodyOrFace = n.Contains("body") || n.Contains("face") || n.Contains("skin");
            if (n.Contains("hair")) return "头发";
            if (n.Contains("tops") || n.Contains("shirt") || n.Contains("jacket") ||
                n.Contains("coat") || n.Contains("onepiece")) return "上装";
            if (n.Contains("bottoms") || n.Contains("pants") || n.Contains("skirt") ||
                n.Contains("trouser")) return "下装";
            if (n.Contains("shoes") || n.Contains("foot")) return "鞋子";
            if (n.Contains("accessory") || n.Contains("glass") || n.Contains("hat") ||
                n.Contains("cap")) return "配饰";
            return "其他";
        }

        /// <summary>
        /// Re-skin every SkinnedMeshRenderer found under <paramref name="clothingRoot"/> onto the
        /// target avatar's skeleton, matching bones by name. The clothing meshes are reparented so
        /// they animate with the body. Bones with no match keep their original transform.
        /// </summary>
        public static void AttachSkinnedClothing(GameObject clothingRoot, AvatarContext target)
        {
            var boneMap = new Dictionary<string, Transform>();
            foreach (var t in target.Root.GetComponentsInChildren<Transform>(true))
                boneMap[t.name] = t;

            foreach (var smr in clothingRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var oldBones = smr.bones;
                var newBones = new Transform[oldBones.Length];
                for (int i = 0; i < oldBones.Length; i++)
                {
                    newBones[i] = oldBones[i] != null && boneMap.TryGetValue(oldBones[i].name, out var b)
                        ? b
                        : oldBones[i];
                }
                smr.bones = newBones;

                if (smr.rootBone != null && boneMap.TryGetValue(smr.rootBone.name, out var root))
                    smr.rootBone = root;

                smr.transform.SetParent(target.Root.transform, false);
            }
        }
    }
}
