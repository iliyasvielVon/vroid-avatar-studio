using System.IO;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using VRM;

namespace AvatarStudio
{
    /// <summary>
    /// Loads a VRoid-exported .vrm (VRM 0.x) at runtime via UniVRM and returns a ready AvatarContext.
    /// Drop .vrm files into StreamingAssets/Avatars and they will be picked up automatically.
    /// </summary>
    public static class VrmAvatarLoader
    {
        public static string AvatarsDirectory =>
            Path.Combine(Application.streamingAssetsPath, "Avatars");

        /// <summary>Returns the first *.vrm under StreamingAssets/Avatars, or null if none exists.</summary>
        public static string FindFirstVrm()
        {
            var dir = AvatarsDirectory;
            if (!Directory.Exists(dir)) return null;
            var files = Directory.GetFiles(dir, "*.vrm", SearchOption.TopDirectoryOnly);
            return files.Length > 0 ? files[0] : null;
        }

        public static async Task<AvatarContext> LoadAsync(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.LogWarning($"[VrmAvatarLoader] VRM not found at: {path}");
                return null;
            }

            RuntimeGltfInstance instance =
                await VrmUtility.LoadAsync(path, new RuntimeOnlyAwaitCaller());

            instance.ShowMeshes();
            instance.EnableUpdateWhenOffscreen();
            instance.Root.name = Path.GetFileNameWithoutExtension(path);

            return AvatarContext.Build(instance.Root);
        }
    }
}
