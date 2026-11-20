using System;
using System.Collections;
using UnityEngine;
using TargetRange.Core;
using TargetRange.UI;
using TargetRange.Weapon;

namespace TargetRange.Targets
{
    /// <summary>
    /// A reactive target standing on a platform.
    /// Sits still (kinematic) until a bullet hits it.
    /// Then it becomes a dynamic physics object, receives the bullet's physical impulse,
    /// falls off the platform, triggers 3D score popups with bullseye detection,
    /// notifies the pedestal light, scores its points, and smoothly respawns.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Target : MonoBehaviour, IShootable
    {
        [SerializeField] private GameManager game;
        [SerializeField, Min(0)] private int points = 10;
        [SerializeField, Tooltip("Set only for targets that ride on a moving platform.")]
        private PlatformMover mover;
        [SerializeField] private TargetPedestalLight pedestalLight;
        [SerializeField] private Transform faceTransform;

        private Rigidbody body;
        private Transform homeParent;
        private Vector3 homeLocalPosition;
        private Quaternion homeLocalRotation;
        private Vector3 homeScale;
        private Collider[] colliders;

        public int Points => points;
        public bool IsKnocked { get; private set; }

        /// <summary>Raised whenever any target is hit (arg: hit position, isBullseye).</summary>
        public static event Action<Vector3, bool> AnyTargetHit;

        /// <summary>Raised once when the target is knocked off its platform (argument: points it is worth).</summary>
        public event Action<Target, int> Knocked;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            homeParent = transform.parent;
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
            homeScale = transform.localScale;
            colliders = GetComponentsInChildren<Collider>(true);
            body.isKinematic = true;
            if (faceTransform == null) faceTransform = transform.Find("Face");
            if (pedestalLight == null && transform.parent != null)
                pedestalLight = transform.parent.GetComponentInChildren<TargetPedestalLight>();

            game.RoundReset += ResetTarget;
        }

        private void OnDestroy()
        {
            if (game != null) game.RoundReset -= ResetTarget;
        }

        public void OnShot(in ShotInfo shot)
        {
            if (game.State != GameState.Playing) return;

            if (IsKnocked)
            {
                // Already falling: a second bullet just pushes it further, it never scores twice.
                body.AddForceAtPosition(shot.Impulse, shot.Point, ForceMode.Impulse);
                return;
            }

            IsKnocked = true;
            transform.SetParent(null, true); // leave the moving platform
            body.isKinematic = false;
            body.linearVelocity = mover != null ? mover.Velocity : Vector3.zero;
            body.AddForceAtPosition(shot.Impulse, shot.Point, ForceMode.Impulse);

            // Precision Bullseye detection: hit within inner circle
            bool isBullseye = false;
            if (faceTransform != null)
            {
                float dist = Vector3.Distance(shot.Point, faceTransform.position);
                if (dist <= 0.12f) isBullseye = true;
            }

            int basePoints = points + (isBullseye ? 30 : 0);
            int addedPoints = game.RegisterHit(basePoints, isBullseye);

            // Visual 3D score popup proof
            ScorePopupManager.Show(shot.Point, addedPoints, isBullseye);
            AnyTargetHit?.Invoke(shot.Point, isBullseye);

            // Update pedestal status light
            if (pedestalLight != null) pedestalLight.SetTargetActive(false);

            Knocked?.Invoke(this, addedPoints);
            StartCoroutine(VanishAndRespawn());
        }

        /// <summary>Falls, shrinks away, waits, then grows back on its platform so the lane never runs empty.</summary>
        private IEnumerator VanishAndRespawn()
        {
            RangeConfig settings = game.Config;
            yield return new WaitForSeconds(settings.knockedTargetVanishDelay);

            for (float t = 0f; t < settings.vanishDuration; t += Time.deltaTime)
            {
                transform.localScale = homeScale * (1f - t / settings.vanishDuration);
                yield return null;
            }

            transform.localScale = Vector3.zero;
            body.isKinematic = true;
            SetCollidersEnabled(false);

            yield return new WaitForSeconds(settings.targetRespawnDelay);
            PutBackOnPlatform();

            for (float t = 0f; t < settings.respawnGrowDuration; t += Time.deltaTime)
            {
                transform.localScale = homeScale * Mathf.SmoothStep(0f, 1f, t / settings.respawnGrowDuration);
                yield return null;
            }
            transform.localScale = homeScale;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            foreach (Collider c in colliders) c.enabled = enabled;
        }

        /// <summary>Restart button: forget everything that happened this round.</summary>
        private void ResetTarget()
        {
            StopAllCoroutines();
            PutBackOnPlatform();
            transform.localScale = homeScale;
        }

        private void PutBackOnPlatform()
        {
            SetCollidersEnabled(true);
            IsKnocked = false;

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            transform.SetParent(homeParent, false);
            transform.SetLocalPositionAndRotation(homeLocalPosition, homeLocalRotation);
            if (mover != null) mover.ResetToStart();
            if (pedestalLight != null) pedestalLight.SetTargetActive(true);
        }
    }
}
