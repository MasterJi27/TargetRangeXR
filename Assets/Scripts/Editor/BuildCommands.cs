using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TargetRange.EditorTools
{
    /// <summary>Command line entry points (used by build scripts) and the Windows player build.</summary>
    public static class BuildCommands
    {
        private const string ScenePath = "Assets/Scenes/Range.unity";

        /// <summary>Unity.exe -batchmode -executeMethod TargetRange.EditorTools.BuildCommands.Stage1</summary>
        public static void Stage1()
        {
            ProjectSetup.PrepareSettings();
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) ProjectSetup.ImportTextMeshProEssentials();
        }

        public static void Stage2() => SceneBuilder.BuildAll();

        [MenuItem("Target Range/3. Build Windows Player")]
        public static void BuildWindows()
        {
            string output = Path.GetFullPath("Builds/Windows/Target Range XR.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            string summary = $"Result: {report.summary.result}  Errors: {report.summary.totalErrors}  Warnings: {report.summary.totalWarnings}  " +
                             $"Size: {report.summary.totalSize / (1024 * 1024)} MB";
            Debug.Log("TRANGE_BUILD_RESULT " + summary);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + summary);
        }
    }
}
