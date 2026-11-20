using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TargetRange.Core;
using TargetRange.Feedback;
using TargetRange.Targets;
using TargetRange.UI;
using TargetRange.Weapon;

namespace TargetRange.Tests.PlayMode
{
    /// <summary>
    /// Records the gameplay video. A scripted "player" uses the XR Device Simulator the way a person would: it holds Space
    /// (control the right hand), grabs the pistol, aims with mouse movement and fires with the left mouse button.
    /// Frames are written at a fixed 30 fps (so the video is smooth no matter how slow the PC is) and every sound is
    /// logged with its time, so the audio track can be rebuilt from the game's own sound files.
    /// Run with RunTestsWindowed.ps1 (needs a real window and the keyboard focus).
    /// </summary>
    public class DemoRecorder
    {
        private const int Fps = 30;

        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "_demo"));
        private static string Stills => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs", "Screenshots"));

        private int frame;
        private float Time30 => frame / (float)Fps;
        private readonly List<string> audioLog = new List<string>();

        private GameManager game;
        private Pistol pistol;
        private Vector2 mouseDelta;
        private bool leftClick;

        // Auto calibrated mouse directions (the simulator's axis signs are found by watching the aim error shrink).
        private float yawSign = 1f, pitchSign = -1f;

        [UnityTest, Timeout(1500000)]
        public IEnumerator RecordGameplayVideo()
        {
            ScreenshotCapture.SetGameViewResolution(1920, 1080);
            Directory.CreateDirectory(Stills);
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
            Directory.CreateDirectory(Path.Combine(Root, "frames"));

            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Range");
            yield return null;
            Object.FindFirstObjectByType<XRDesktopBridge>().SetSimulatorMode(true);
            game = Object.FindAnyObjectByType<GameManager>();
            pistol = Object.FindAnyObjectByType<Pistol>();
            var audio = Object.FindAnyObjectByType<AudioService>();
            audio.Played += id => audioLog.Add($"{Time30:0.000} {id}");

            // From here on, game time = video time: every rendered frame advances the game by exactly 1/30 s.
            Time.captureFramerate = Fps;

            // 1. The player looks around the range for a moment (the first frames also let the XR rig start up).
            for (int i = 0; i < 4 * Fps; i++) yield return Frame();
            Assert.IsTrue(Application.isFocused, "the editor window needs keyboard focus, or the XR simulator ignores input");
            yield return Still("06_gameplay_ready.png");

            // 2. Hold Space = "control the right hand", then grab the pistol (what pressing G near it does).
            Keys(Key.Space);
            for (int i = 0; i < Fps; i++) yield return Frame();

            XRBaseInteractor hand = null;
            for (int i = 0; i < 20 * Fps && hand == null; i++)
            {
                hand = Object.FindObjectsByType<XRBaseInteractor>().FirstOrDefault(h => h is NearFarInteractor &&
                    h.GetComponentsInParent<Transform>(true).Any(t => t.name.Contains("Right")));
                if (hand == null) yield return Frame();
            }
            Assert.IsNotNull(hand, "right hand interactor not active");

            var grab = pistol.GetComponent<XRGrabInteractable>();
            grab.interactionManager.SelectEnter((IXRSelectInteractor)hand, grab);
            for (int i = 0; i < Fps; i++) yield return Frame();
            Assert.AreEqual(GameState.Playing, game.State);
            yield return Still("07_gameplay_pistol_in_hand.png");

            // 3. Shoot the targets: near row, moving row, far row, then round again as they respawn.
            Target[] all = Object.FindObjectsByType<Target>();
            var order = new List<Target>();
            order.AddRange(all.Where(t => t.name.StartsWith("Near")).OrderBy(t => t.transform.position.x));
            order.AddRange(all.Where(t => t.name.StartsWith("Moving")).OrderBy(t => t.transform.position.x));
            order.AddRange(all.Where(t => t.name.StartsWith("Far")).OrderBy(t => t.transform.position.x));

            int next = 0, shotsAtTarget = 0, stills = 0;
            int guard = 0;
            while (game.State == GameState.Playing && guard++ < 70 * Fps)
            {
                Target goal = order[next % order.Count];
                if (goal.IsKnocked) { next++; shotsAtTarget = 0; yield return Frame(); continue; }

                // aim
                int settled = 0;
                for (int i = 0; i < 4 * Fps && game.State == GameState.Playing && settled < 3; i++)
                {
                    float error = AimAt(goal);
                    settled = error < 0.5f ? settled + 1 : 0;
                    yield return Frame();
                }
                if (game.State != GameState.Playing) break;

                // fire: a short left mouse click
                int scoreBefore = game.Score.Score;
                leftClick = true; mouseDelta = Vector2.zero;
                yield return Frame(); yield return Frame(); yield return Frame();
                leftClick = false;
                for (int i = 0; i < Fps / 2; i++) { AimAt(goal); yield return Frame(); }

                if (game.Score.Score > scoreBefore)
                {
                    if (stills == 1) yield return Still("08_gameplay_target_knocked.png");
                    stills++;
                    next++; shotsAtTarget = 0;
                }
                else if (++shotsAtTarget >= 2) { next++; shotsAtTarget = 0; }

                for (int i = 0; i < Fps / 3; i++) { AimAt(goal); yield return Frame(); }
            }

            // 4. The round has ended: show the results panel, then press Restart.
            for (int i = 0; i < 3 * Fps; i++) yield return Frame();
            Debug.Log($"DEMO end of round score={game.Score.Score} shots={game.Score.Shots} hits={game.Score.Hits} yawSign={yawSign} pitchSign={pitchSign}");
            Assert.AreEqual(GameState.Ended, game.State);
            yield return Still("09_results_panel.png");
            for (int i = 0; i < 2 * Fps; i++) yield return Frame();

            Keys();
            var results = Object.FindAnyObjectByType<ResultsView>();
            results.transform.Find("Panel/Restart Button").GetComponent<Button>().onClick.Invoke();
            for (int i = 0; i < 3 * Fps; i++) yield return Frame();
            Assert.AreEqual(GameState.Ready, game.State);

            Debug.Log($"DEMO score={game.Score.Score} yawSign={yawSign} pitchSign={pitchSign}");
            Time.captureFramerate = 0;
            File.WriteAllText(Path.Combine(Root, "audio_events.txt"), string.Join("\n", audioLog));
            File.WriteAllText(Path.Combine(Root, "summary.txt"), $"frames={frame} fps={Fps}");
            Debug.Log($"DEMO_DONE frames={frame} sounds={audioLog.Count}");
        }

        // ------------------------------------------------------------------ per frame work
        private IEnumerator Frame()
        {
            PushInput();
            ScreenCapture.CaptureScreenshot(Path.Combine(Root, "frames", $"f_{frame:00000}.jpg"));
            frame++;
            mouseDelta = Vector2.zero;
            yield return null;
        }

        private IEnumerator Still(string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Stills, name));
            yield return Frame();
        }

        private void PushInput()
        {
            MouseState state = new MouseState { delta = mouseDelta };
            if (leftClick) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(Mouse.current, state);
        }

        private static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));

        // ------------------------------------------------------------------ aiming
        private float lastYawError, lastPitchError;
        private int wrongWay;

        /// <summary>One step of a simple controller: turn the mouse a little towards the target. Returns the aim error in degrees.</summary>
        private float AimAt(Target target)
        {
            Transform muzzle = pistol.Muzzle;
            Collider face = target.GetComponentInChildren<MeshCollider>();
            Vector3 point = face.bounds.center;

            // Moving targets: aim where the target will be when the bullet arrives.
            var mover = target.GetComponentInParent<PlatformMover>();
            if (mover != null)
            {
                float flight = Vector3.Distance(point, muzzle.position) / game.Config.bulletSpeed;
                point += mover.Velocity * flight;
            }

            Vector3 toTarget = point - muzzle.position;
            Vector3 forward = muzzle.forward;

            float yawError = Vector3.SignedAngle(Vector3.ProjectOnPlane(forward, Vector3.up), Vector3.ProjectOnPlane(toTarget, Vector3.up), Vector3.up);
            float pitchOf(Vector3 v) => Mathf.Atan2(v.y, new Vector2(v.x, v.z).magnitude) * Mathf.Rad2Deg;
            float pitchError = pitchOf(toTarget) - pitchOf(forward); // positive: the target is higher

            // Did the last command make things worse? Then the axis points the other way: flip it.
            if (Mathf.Abs(mouseDelta.x) > 1f || Mathf.Abs(mouseDelta.y) > 1f)
            {
                bool worse = Mathf.Abs(yawError) > Mathf.Abs(lastYawError) + 0.05f || Mathf.Abs(pitchError) > Mathf.Abs(lastPitchError) + 0.05f;
                wrongWay = worse ? wrongWay + 1 : 0;
                if (wrongWay >= 3)
                {
                    if (Mathf.Abs(yawError) > Mathf.Abs(lastYawError)) yawSign = -yawSign;
                    if (Mathf.Abs(pitchError) > Mathf.Abs(lastPitchError)) pitchSign = -pitchSign;
                    wrongWay = 0;
                }
            }
            lastYawError = yawError;
            lastPitchError = pitchError;

            const float pixelsPerDegree = 1f / 0.19f; // measured: 20 px of mouse = about 3.9 degrees
            mouseDelta = new Vector2(
                Mathf.Clamp(yawSign * yawError * pixelsPerDegree * 0.45f, -35f, 35f),
                Mathf.Clamp(pitchSign * pitchError * pixelsPerDegree * 0.45f, -35f, 35f));

            return Mathf.Sqrt(yawError * yawError + pitchError * pitchError);
        }
    }
}
