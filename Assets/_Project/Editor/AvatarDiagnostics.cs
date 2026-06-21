using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UniGLTF;
using VRM;

namespace AvatarStudio.EditorTools
{
    /// <summary>
    /// Headless sanity check: loads the test VRM in the Editor and verifies that the
    /// customization *logic* layer actually mutates the model (blendshapes, skin tint, outfit).
    /// Run: Unity.exe -batchmode -quit -projectPath ... -executeMethod AvatarStudio.EditorTools.AvatarDiagnostics.Run
    /// Temporary — delete after diagnosis.
    /// </summary>
    public static class AvatarDiagnostics
    {
        static readonly StringBuilder _buf = new StringBuilder();
        static string OutPath => Path.Combine(Directory.GetCurrentDirectory(), "diag_log.txt");

        [MenuItem("AvatarStudio/Run Diagnostics")]
        public static void Run()
        {
            _buf.Clear();
            try
            {
                string path = VrmAvatarLoader.FindFirstVrm();
                D($"VRM path = {path}");
                if (path == null) { D("NO VRM FOUND"); return; }

                var task = VrmUtility.LoadAsync(path, new ImmediateCaller());
                task.Wait();
                var instance = task.Result;
                instance.ShowMeshes();
                instance.Root.name = "DIAG";
                var ctx = AvatarContext.Build(instance.Root);

                D($"Root='{ctx.Root.name}' Animator={(ctx.Animator!=null)} isHuman={(ctx.Animator!=null && ctx.Animator.isHuman)}");
                D($"SkinnedRenderers={ctx.SkinnedRenderers.Count} AllRenderers={ctx.AllRenderers.Count} SkinMaterials={ctx.SkinMaterials.Count}");
                foreach (var r in ctx.AllRenderers)
                    D($"  renderer '{r.name}' type={r.GetType().Name} enabled={r.enabled} mats={r.sharedMaterials.Length}");

                // ---- FACE ----
                var face = new FaceCustomizer(ctx);
                D($"[FACE] morphs={face.Morphs.Count}");
                if (face.Morphs.Count > 0)
                {
                    int idx = 0;
                    // pick an obvious one if present
                    for (int i = 0; i < face.Morphs.Count; i++)
                        if (face.Morphs[i].RawName.Contains("MTH_A") || face.Morphs[i].RawName.Contains("EYE_Close")) { idx = i; break; }
                    var m = face.Morphs[idx];
                    float before = m.Renderer.GetBlendShapeWeight(m.Index);
                    face.SetWeight01(idx, 1f);
                    float after = m.Renderer.GetBlendShapeWeight(m.Index);
                    D($"[FACE] set '{m.RawName}' on renderer '{m.Renderer.name}' before={before} after={after}  => {(Math.Abs(after-100f)<0.1f ? "OK writes" : "FAILED")}");
                }

                // ---- SKIN ----
                var skin = new SkinColorChanger(ctx);
                D($"[SKIN] skinMaterials={skin.SkinMaterialCount}");
                int colorId = Shader.PropertyToID("_Color");
                foreach (var mat in ctx.SkinMaterials)
                {
                    bool has = mat.HasProperty(colorId);
                    D($"  skinMat '{mat.name}' shader='{mat.shader.name}' has_Color={has} color={(has?mat.GetColor(colorId).ToString():"-")}");
                }
                if (skin.SkinMaterialCount > 0)
                {
                    var first = ctx.SkinMaterials[0];
                    Color b = first.HasProperty(colorId) ? first.GetColor(colorId) : Color.magenta;
                    skin.SetTint(new Color(0.70f, 0.50f, 0.38f)); // 古铜
                    Color a = first.HasProperty(colorId) ? first.GetColor(colorId) : Color.magenta;
                    D($"[SKIN] tint applied to '{first.name}' before={b} after={a} => {(b!=a?"OK changes":"FAILED no change")}");
                }

                // ---- OUTFIT ----
                var outfit = new OutfitManager(ctx);
                D($"[OUTFIT] toggleable items={outfit.Items.Count}");
                foreach (var it in outfit.Items)
                    D($"  item '{it.DisplayName}' cat={it.Category} on={it.On}");

                D("DIAGNOSTICS DONE");
            }
            catch (Exception e)
            {
                D("DIAG EXCEPTION: " + e);
            }
            finally
            {
                try { File.WriteAllText(OutPath, _buf.ToString()); Debug.Log("##DIAG## written to " + OutPath); } catch { }
            }
        }

        static void D(string s)
        {
            _buf.AppendLine(s);
            Debug.Log("##DIAG## " + s);
        }
    }
}
