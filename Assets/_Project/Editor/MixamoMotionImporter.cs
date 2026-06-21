using System.IO;
using UnityEditor;
using UnityEngine;

namespace AvatarStudio.EditorTools
{
    /// <summary>
    /// Auto-configures any model (.fbx) dropped into a Resources/Motions folder as a looping Humanoid
    /// animation, so Mixamo downloads "just work": drop the FBX in, and it shows up in the 动作 tab at
    /// runtime (loaded via Resources.LoadAll&lt;AnimationClip&gt;("Motions")) retargeted onto the VRoid
    /// avatar. No manual rig/loop fiddling in the Inspector.
    /// </summary>
    public class MixamoMotionImporter : AssetPostprocessor
    {
        const string MotionsFolder = "/Resources/Motions/";

        bool IsMotion => assetPath.Replace('\\', '/').Contains(MotionsFolder);

        void OnPreprocessModel()
        {
            if (!IsMotion) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            // Mixamo FBX ship no meshes/materials worth keeping — we only want the motion.
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        void OnPreprocessAnimation()
        {
            if (!IsMotion) return;
            var importer = (ModelImporter)assetImporter;
            string niceName = Path.GetFileNameWithoutExtension(assetPath);

            var clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = true;
                // Name the clip after the file so the runtime list reads "Wave", not "mixamo.com".
                clips[i].name = clips.Length == 1 ? niceName : $"{niceName}_{i}";
            }
            importer.clipAnimations = clips;
        }
    }
}
