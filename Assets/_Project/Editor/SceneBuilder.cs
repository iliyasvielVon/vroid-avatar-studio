using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AvatarStudio.EditorTools
{
    /// <summary>
    /// Generates a runnable 形象 scene from code (camera + light + a CharacterScreen bootstrap).
    /// Run from the menu, or in batch: -executeMethod AvatarStudio.EditorTools.SceneBuilder.BuildScene
    /// </summary>
    public static class SceneBuilder
    {
        const string SceneDir = "Assets/_Project/Scenes";
        const string ScenePath = SceneDir + "/Character.unity";

        [MenuItem("AvatarStudio/Build Character Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.18f, 0.24f);
            camGo.transform.position = new Vector3(0, 1f, 3f);

            var lightGo = new GameObject("Key Light", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

            var boot = new GameObject("CharacterScreen");
            boot.AddComponent<CharacterScreen>();

            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            // Make sure the StreamingAssets/Avatars drop folder exists.
            Directory.CreateDirectory("Assets/StreamingAssets/Avatars");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SceneBuilder] Created {ScenePath}");
        }
    }
}
