using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TargetRange.Core;
using TargetRange.Targets;
using TargetRange.UI;
using TargetRange.Weapon;

namespace TargetRange.Tests.PlayMode
{
    /// <summary>Plays the real Range scene: grab, fire, knock targets down, run out the clock and restart.</summary>
    public class RangeTests
    {
        private GameManager game;
        private Pistol pistol;
        private BulletPool bullets;
        private Target[] targets;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            TestInputDevices.EnsurePresent();
            yield return SceneManager.LoadSceneAsync("Range");
            yield return null;
            Object.FindFirstObjectByType<XRDesktopBridge>().SetSimulatorMode(true);
            yield return null;
            yield return null;

            game = Object.FindAnyObjectByType<GameManager>();
            pistol = Object.FindAnyObjectByType<Pistol>();
            bullets = Object.FindAnyObjectByType<BulletPool>();
            targets = Object.FindObjectsByType<Target>();
        }

        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator Scene_StartsReadyWithTenTargetsAndAFullClock()
        {
            Assert.AreEqual(GameState.Ready, game.State);
            Assert.AreEqual(10, targets.Length, "5 static + 3 moving + 2 far targets");
            Assert.AreEqual(60f, game.RemainingSeconds, 0.01f);
            Assert.AreEqual(0, game.Score.Score);
            Assert.IsFalse(pistol.IsHeld);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GrabbingThePistol_StartsTheRound()
        {
            // The XR Input Modality Manager switches the controllers on a few frames after the simulator reports them.
            XRBaseInteractor hand = null;
            for (float deadline = Time.realtimeSinceStartup + 5f; Time.realtimeSinceStartup < deadline && hand == null;)
            {
                hand = TryFindRightHand();
                if (hand == null) yield return null;
            }
            if (hand == null)
            {
                foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
                    { Debug.Log($"XR TEST DEVICE {device.name} enabled={device.enabled} usages={string.Join(",", device.usages)}"); foreach (var control in device.allControls.Where(c => c.name == "isTracked" || c.name == "trackingState")) Debug.Log($"TRACK {control.path}={control.ReadValueAsObject()}"); }
                foreach (var item in Object.FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include))
                    Debug.Log($"XR TEST INTERACTOR {item.name} type={item.GetType().Name} enabled={item.enabled} active={item.gameObject.activeInHierarchy}");
            }
            foreach (var sim in Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>(FindObjectsInactive.Include)) Debug.Log($"SIM enabled={sim.enabled} active={sim.gameObject.activeInHierarchy}"); foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t => t.name.Contains("Controller") || t.name.Contains("Near-Far"))) Debug.Log($"NODE {t.name} self={t.gameObject.activeSelf} hierarchy={t.gameObject.activeInHierarchy} parent={t.parent?.name}"); Assert.IsNotNull(hand, "the right controller never became active");

            var grab = pistol.GetComponent<XRGrabInteractable>();
            grab.interactionManager.SelectEnter((IXRSelectInteractor)hand, grab);
            yield return null;

            Assert.IsTrue(pistol.IsHeld);
            Assert.AreEqual(GameState.Playing, game.State);
        }

        [UnityTest]
        public IEnumerator FiredBullet_KnocksATargetDown_AndScoresItsPointsOnce()
        {
            game.BeginRound();
            Target target = targets.First(t => t.Points == 10 && t.name.StartsWith("Near"));
            Vector3 aim = target.transform.position + Vector3.up * 0.4f; // the face centre
            Vector3 from = new Vector3(aim.x, aim.y, aim.z - 5f);

            bool knocked = false;
            target.Knocked += (_, __) => knocked = true;
            bullets.Fire(from, (aim - from).normalized);

            for (float deadline = Time.time + 2f; Time.time < deadline && !knocked;) yield return null;

            Assert.IsTrue(knocked, "the bullet should reach the target within a second");
            Assert.AreEqual(10, game.Score.Score);
            Assert.AreEqual(1, game.Score.Hits);

            // A second hit on the same, already falling target must not score again.
            target.OnShot(new ShotInfo(target.transform.position, Vector3.forward, Vector3.back, Vector3.forward));
            Assert.AreEqual(10, game.Score.Score);

            // The target is a real physics body now and leaves its pedestal.
            Assert.IsNull(target.transform.parent);
        }

        [UnityTest]
        public IEnumerator Pistol_DoesNotFireBeforeTheRoundHasStarted()
        {
            int shots = 0;
            pistol.ShotFired += () => shots++;
            Assert.AreEqual(GameState.Ready, game.State);
            game.RegisterShot();
            Assert.AreEqual(0, game.Score.Shots);
            Assert.AreEqual(0, shots);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WhenTheTimerEnds_TheGameFreezes_AndTheResultsPanelAppears()
        {
            game.BeginRound();
            game.Advance(61f);
            yield return null;

            Assert.AreEqual(GameState.Ended, game.State);
            Assert.AreEqual(0f, Time.timeScale, "the game is frozen");

            var results = Object.FindAnyObjectByType<ResultsView>(FindObjectsInactive.Include);
            Transform panel = results.transform.Find("Panel");
            Assert.IsTrue(panel.gameObject.activeSelf);
            Assert.IsNotNull(panel.Find("Restart Button"));
            Assert.IsNotNull(panel.Find("Exit Button"));
        }

        [UnityTest]
        public IEnumerator Restart_ResetsScoreTimeTargetsAndPistol()
        {
            game.BeginRound();
            Target target = targets[0];
            target.OnShot(new ShotInfo(target.transform.position, Vector3.forward, Vector3.back, Vector3.forward * 5f));
            Vector3 pistolHome = pistol.transform.position;
            game.Advance(61f);
            yield return null;

            game.RestartRound();
            yield return null;

            Assert.AreEqual(GameState.Ready, game.State);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(0, game.Score.Score);
            Assert.AreEqual(60f, game.RemainingSeconds, 0.01f);
            Assert.IsFalse(target.IsKnocked);
            Assert.IsNotNull(target.transform.parent, "the target is back on its platform");
            Assert.AreEqual(pistolHome, pistol.transform.position);
        }

        [UnityTest]
        public IEnumerator MovingTargets_ActuallyMove()
        {
            PlatformMover mover = Object.FindObjectsByType<PlatformMover>().First();
            Vector3 before = mover.transform.position;
            yield return new WaitForSeconds(1.2f);
            Assert.Greater(Vector3.Distance(before, mover.transform.position), 0.05f);
        }

        /// <summary>The active near/far interactor under the "Right Controller" object of the XR rig, or null.</summary>
        private static XRBaseInteractor TryFindRightHand()
        {
            XRBaseInteractor[] all = Object.FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include);
            return all.FirstOrDefault(i => i is NearFarInteractor && i.isActiveAndEnabled &&
                i.GetComponentsInParent<Transform>(true).Any(t => t.name.Contains("Right")));
        }
    }
}



