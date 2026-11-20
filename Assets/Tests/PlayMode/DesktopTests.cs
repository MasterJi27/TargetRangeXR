using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TargetRange.Core;
using TargetRange.Targets;
using TargetRange.Weapon;

namespace TargetRange.Tests.PlayMode
{
    /// <summary>Regression coverage for the desktop input-owner and weapon reset bugs.</summary>
    public class DesktopTests
    {
        private GameManager game;
        private XRDesktopBridge bridge;
        private Pistol pistol;
        [UnitySetUp]
        public IEnumerator Load()
        {
            TestInputDevices.EnsurePresent();
            yield return SceneManager.LoadSceneAsync("Range");
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            bridge = Object.FindAnyObjectByType<XRDesktopBridge>();
            pistol = Object.FindAnyObjectByType<Pistol>();
        }
        [TearDown]
        public void Cleanup() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; }
        [UnityTest]
        public IEnumerator LaunchWaitsForPickup_AndResetDoesNotAutoStart()
        {
            Assert.IsTrue(bridge.IsDesktopMode);
            Assert.AreEqual(GameState.Ready, game.State);
            Assert.IsFalse(pistol.IsHeld);
            Vector3 home = pistol.transform.position;
            bridge.EquipDesktopPistol();
            Assert.IsTrue(pistol.IsHeld);
            Assert.AreEqual(GameState.Playing, game.State);
            pistol.TryFire();
            Assert.AreEqual(1, game.Score.Shots);
            pistol.TryFire();
            Assert.AreEqual(1, game.Score.Shots, "same-frame fire is rate limited");
            game.Advance(61);
            pistol.TryFire();
            Assert.AreEqual(1, game.Score.Shots, "ended rounds cannot fire");
            game.RestartRound();
            yield return null;
            Assert.AreEqual(GameState.Ready, game.State);
            Assert.IsFalse(pistol.IsHeld);
            Assert.AreEqual(home, pistol.transform.position);
            Assert.AreEqual(0, Object.FindAnyObjectByType<BulletPool>().ActiveCount, "old bullets cannot score in a new round");
            bridge.EquipDesktopPistol();
            pistol.TryFire();
            Assert.AreEqual(1, game.Score.Shots, "restart clears fire cooldown");
        }
        [UnityTest]
        public IEnumerator ReticleAim_FiresFromMuzzle_AndKnocksTargetDown()
        {
            bridge.EquipDesktopPistol();
            var target = System.Array.Find(Object.FindObjectsByType<Target>(), t => t.name.StartsWith("Near") && Mathf.Abs(t.transform.position.x) < 0.1f);
            Assert.IsNotNull(target);
            Vector3 point = target.GetComponentInChildren<MeshCollider>().bounds.center;
            Camera.main.transform.LookAt(point);
            bridge.AimPistol();
            Assert.Less(Vector3.Angle(pistol.Muzzle.forward, point - pistol.Muzzle.position), 1f);
            pistol.TryFire();
            for (float deadline = Time.time + 2f; Time.time < deadline && !target.IsKnocked;) yield return null;
            Assert.IsTrue(target.IsKnocked, "a real projectile must reach the target under the centre reticle");
            Assert.AreEqual(1, game.Score.Hits);
        }
        [UnityTest]
        public IEnumerator DroppedPistolCannotFire_AndModeSwitchClearsHold()
        {
            bridge.EquipDesktopPistol();
            pistol.ReturnToTable();
            pistol.TryFire();
            Assert.AreEqual(0, game.Score.Shots);
            bridge.SetSimulatorMode(true);
            yield return null;
            Assert.IsFalse(bridge.IsDesktopMode);
            Assert.IsFalse(pistol.IsHeld);
            Assert.AreEqual(GameState.Ready, game.State);
            bridge.SetSimulatorMode(false);
            yield return null;
            Assert.IsTrue(bridge.IsDesktopMode);
            Assert.IsFalse(pistol.IsHeld);
            Assert.AreEqual(60f, game.RemainingSeconds, 0.01f);
        }
    }
}

