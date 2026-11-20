using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace TargetRange.EditorTools
{
    /// <summary>
    /// One time project configuration: player settings, input backend, layers and TextMeshPro.
    /// Menu: Target Range > 1. Prepare Project Settings
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("Target Range/1. Prepare Project Settings")]
        public static void PrepareSettings()
        {
            PlayerSettings.companyName = "Raghav Kathuria";
            PlayerSettings.productName = "Target Range XR";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });

            // Use only the new Input System package (value 1 = "Input System Package (New)").
            var projectSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            projectSettings.FindProperty("activeInputHandler").intValue = 1;
            projectSettings.ApplyModifiedPropertiesWithoutUndo();

            SetLayer(AssetPipeline.LayerEnvironment, "Environment");
            SetLayer(AssetPipeline.LayerTargets, "Targets");
            SetLayer(AssetPipeline.LayerPlayer, "Player");
            AssetDatabase.SaveAssets();
            Debug.Log("TRANGE_SETTINGS_OK");
        }

        [MenuItem("Target Range/0. Import TextMeshPro Essentials")]
        public static void ImportTextMeshProEssentials()
        {
            const string package = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh();
            Debug.Log("TRANGE_TMP_IMPORTED");
        }

        private static void SetLayer(int index, string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tagManager.FindProperty("layers").GetArrayElementAtIndex(index).stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureFolders()
        {
            foreach (string dir in new[] { "Assets/Art/Materials", "Assets/Prefabs", "Assets/Scenes", "Assets/Settings" })
                Directory.CreateDirectory(dir);
        }
    }
}
