using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TargetRange.Core;
using TargetRange.Targets;
using TargetRange.Weapon;
using System.Linq;

namespace TargetRange.Tests.PlayMode
{
    /// <summary>
    /// Not a rule check: saves the README screenshots to Docs/Screenshots. It uses a separate "photo camera" so the
    /// views can be chosen freely (the headset camera is driven by the XR simulator).
    /// Run it in a real editor window (RunTestsWindowed.ps1), not in batch mode.
    /// </summary>
    public class ScreenshotCapture
    {
        [UnityTest]
        public IEnumerator CaptureCurrentXRDocumentation()
        {
            TestInputDevices.EnsurePresent();
            Time.timeScale = 1f;
            SetGameViewResolution(1920, 1080);
            yield return SceneManager.LoadSceneAsync("Range");
            yield return null;
            UnityEngine.Object.FindAnyObjectByType<XRDesktopBridge>().SetSimulatorMode(true);
            for (int i = 0; i < 60; i++) yield return null;
            yield return Snap("09_xr_rig_and_controllers.png");
            var hand = UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>()
                .First(h => h.GetComponentsInParent<Transform>(true).Any(t => t.name.Contains("Right")));
            var pistol = UnityEngine.Object.FindAnyObjectByType<Pistol>();
            var grab = pistol.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.interactionManager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)hand, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
            for (int i = 0; i < 30; i++) yield return null;
            Assert.IsTrue(pistol.IsHeld);
            yield return Snap("10_xr_pistol_grabbed.png");
            var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            game.Advance(61f);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Snap("11_xr_results.png");
            Assert.AreEqual(GameState.Ended, game.State);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator CaptureCurrentDesktopDocumentation()
        {
            TestInputDevices.EnsurePresent();
            Time.timeScale = 1f;
            SetGameViewResolution(1920, 1080);
            yield return SceneManager.LoadSceneAsync("Range");
            for (int i = 0; i < 45; i++) yield return null;
            var bridge = UnityEngine.Object.FindAnyObjectByType<XRDesktopBridge>();
            var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var pistol = UnityEngine.Object.FindAnyObjectByType<Pistol>();
            Assert.IsTrue(bridge.IsDesktopMode);
            yield return Snap("06_desktop_start.png");
            bridge.EquipDesktopPistol();
            var target = UnityEngine.Object.FindObjectsByType<Target>().First(t => t.name.StartsWith("Near") && Mathf.Abs(t.transform.position.x) < 0.1f);
            Camera.main.transform.LookAt(target.GetComponentInChildren<MeshCollider>().bounds.center);
            bridge.AimPistol();
            pistol.TryFire();
            for (float deadline = Time.time + 2f; Time.time < deadline && !target.IsKnocked;) yield return null;
            Assert.IsTrue(target.IsKnocked);
            game.Advance(12f);
            for (int i = 0; i < 8; i++) yield return null;
            yield return Snap("07_desktop_gameplay.png");
            game.Advance(61f);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Snap("08_desktop_results.png");
            Assert.AreEqual(GameState.Ended, game.State);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        private static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Screenshots"));

        /// <summary>Forces the Editor Game view to a fixed 1920x1080 (reflection, because Unity has no public API for it).</summary>
        internal static void SetGameViewResolution(int width, int height)
        {
            try
            {
                const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                Type sizesType = Type.GetType("UnityEditor.GameViewSizes, UnityEditor");
                Type singletonType = Type.GetType("UnityEditor.ScriptableSingleton`1, UnityEditor").MakeGenericType(sizesType);
                object sizes = singletonType.GetProperty("instance", all).GetValue(null);
                object group = sizesType.GetMethod("GetGroup", all).Invoke(sizes, new object[] { 0 });

                Type sizeType = Type.GetType("UnityEditor.GameViewSize, UnityEditor");
                Type sizeKind = Type.GetType("UnityEditor.GameViewSizeType, UnityEditor");
                object size = Activator.CreateInstance(sizeType, Enum.ToObject(sizeKind, 1), width, height, "Screenshot");
                group.GetType().GetMethod("AddCustomSize", all).Invoke(group, new[] { size });
                int index = (int)group.GetType().GetMethod("GetTotalCount", all).Invoke(group, null) - 1;

                Type gameView = Type.GetType("UnityEditor.GameView, UnityEditor");
                Type editorWindow = Type.GetType("UnityEditor.EditorWindow, UnityEditor");
                object window = editorWindow.GetMethod("GetWindow", new[] { typeof(Type) }).Invoke(null, new object[] { gameView });
                gameView.GetProperty("selectedSizeIndex", all).SetValue(window, index);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not set the Game view size: " + e.Message);
            }
        }

        private static IEnumerator Snap(string fileName)
        {
            Directory.CreateDirectory(OutputFolder);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutputFolder, fileName));
            for (int i = 0; i < 10; i++) yield return null;
        }

        /// <summary>The sample HUD class is internal to the package, so it is found by its type name.</summary>
        internal static void HideSimulatorHud()
        {
            foreach (MonoBehaviour b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>())
                if (b.GetType().Name == "XRDeviceSimulatorUI") b.gameObject.SetActive(false);
        }

        private static Camera MakePhotoCamera()
        {
            var go = new GameObject("Photo Camera");
            var cam = go.AddComponent<Camera>();
            cam.depth = 50;
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 60f;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            return cam;
        }

        private static void Aim(Camera cam, Vector3 position, Vector3 lookAt, float fov = 62f)
        {
            cam.transform.position = position;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            cam.fieldOfView = fov;
        }

        [UnityTest]
        public IEnumerator CaptureTheRange()
        {
            Time.timeScale = 1f;
            SetGameViewResolution(1920, 1080);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Range");
            yield return null;
            UnityEngine.Object.FindAnyObjectByType<XRDesktopBridge>().SetSimulatorMode(true);
            for (int i = 0; i < 40; i++) yield return null;

            Camera photo = MakePhotoCamera();

            // The simulator's own help window is useful when playing, but it would cover the screenshots.
            HideSimulatorHud();

            // 1. The whole hall from behind the firing line.
            Aim(photo, new Vector3(0f, 1.75f, -2.2f), new Vector3(0f, 1.3f, 9f), 66f);
            for (int i = 0; i < 6; i++) yield return null;
            yield return Snap("01_range_overview.png");

            // 2. The shooting bench with the pistol, the instruction board and the scoreboard.
            Aim(photo, new Vector3(0.05f, 1.42f, -0.05f), new Vector3(0.03f, 0.98f, 0.95f), 60f);
            yield return null;
            yield return Snap("02_bench_pistol_and_boards.png");

            // 3. The targets, moving ones included.
            Aim(photo, new Vector3(-1.2f, 1.5f, 3.2f), new Vector3(0.4f, 1.3f, 11f), 58f);
            yield return new WaitForSeconds(0.6f);
            yield return Snap("03_targets.png");

            // 4. The big scoreboard on the far wall, mid round.
            var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            game.BeginRound();
            game.Advance(14f);
            var first = UnityEngine.Object.FindAnyObjectByType<Target>();
            Aim(photo, new Vector3(0f, 1.9f, 5f), new Vector3(0f, 3.3f, 15.9f), 60f);
            for (int i = 0; i < 6; i++) yield return null;
            yield return Snap("04_wall_scoreboard.png");

            // 5. The end of round panel, seen through the headset camera like a player would.
            game.Advance(100f);
            photo.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Snap("05_results_panel.png");

            Assert.AreEqual(GameState.Ended, game.State);
            Assert.IsNotNull(first);
        }
    }
}


