using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TargetRange.Core;

namespace TargetRange.Weapon
{
    /// <summary>
    /// The interactable pistol. Seamlessly integrates Unity XR Interaction Toolkit (6DOF VR controller grab & trigger)
    /// with desktop/simulator keyboard & mouse controls (quick 'E' pick up, direct mouse fire, and laser aiming).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class Pistol : MonoBehaviour
    {
        [SerializeField] private RangeConfig config;
        [SerializeField] private GameManager game;
        [SerializeField] private BulletPool bulletPool;
        [SerializeField] private Transform muzzle;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private Light flashLight;
        [SerializeField] private LaserSight laser;
        [SerializeField, Tooltip("Parts that slide back when the gun fires (slide, serrations, sights).")]
        private Transform[] slideParts;
        [SerializeField] private Transform triggerPart;

        private XRGrabInteractable grab;
        private Rigidbody body;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private Vector3[] slideRestPositions;
        private Collider[] gunColliders;
        private bool desktopHeld;
        private float nextShotTime;
        private float releasedAt = -1f;

        public event Action ShotFired;
        public event Action DryFired;
        public event Action Grabbed;

        public Transform Muzzle => muzzle;
        public bool IsHeld => desktopHeld || grab.isSelected;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            homePosition = transform.position;
            homeRotation = transform.rotation;
            gunColliders = GetComponentsInChildren<Collider>(true);

            slideRestPositions = new Vector3[slideParts.Length];
            for (int i = 0; i < slideParts.Length; i++) slideRestPositions[i] = slideParts[i].localPosition;
        }

        private void OnEnable()
        {
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
            grab.activated.AddListener(OnTriggerPulled);
            game.RoundReset += ReturnToTable;
        }

        private void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
            grab.activated.RemoveListener(OnTriggerPulled);
            if (game != null) game.RoundReset -= ReturnToTable;
        }

        private void Update()
        {
            if (IsHeld || releasedAt < 0f) return;

            // Physics drop safety: restore physics if dropped
            if (body.isKinematic && Time.time - releasedAt > 0.05f) body.isKinematic = false;

            // Auto-return to table after delay so the pistol is never lost
            if (Time.time - releasedAt > config.returnToTableDelay) ReturnToTable();
        }

        /// <summary>Desktop holding shares firing and feedback with XR, but never selects an XR hand.</summary>
        public void SetDesktopHeld(bool held)
        {
            if (!held) { ReturnToTable(); return; }
            if (game.State == GameState.Ended) return;
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            releasedAt = -1f;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            grab.enabled = false;
            desktopHeld = true;
            laser.SetVisible(false); // desktop uses the centre reticle; XR keeps its barrel laser
            Grabbed?.Invoke();
            game.BeginRound();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            releasedAt = -1f;
            laser.SetVisible(true);
            Grabbed?.Invoke();
            game.BeginRound();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            releasedAt = Time.time;
            laser.SetVisible(false);
        }

        private void OnTriggerPulled(ActivateEventArgs args)
        {
            TryFire(args.interactorObject?.transform);
        }

        public void TryFire(Transform interactorTransform = null)
        {
            if (!IsHeld || game.State != GameState.Playing || Time.time < nextShotTime)
            {
                if (Time.time >= nextShotTime) DryFired?.Invoke();
                return;
            }

            nextShotTime = Time.time + config.secondsBetweenShots;

            // Safe swept firing: ignore gun colliders so bullets never self-collide
            bulletPool.Fire(muzzle.position, muzzle.forward, gunColliders);
            game.RegisterShot();

            PlayShotEffects();

            if (interactorTransform != null) SendHaptics(interactorTransform);
            ShotFired?.Invoke();
        }

        private void PlayShotEffects()
        {
            muzzleFlash.Play(true);
            StopAllCoroutines();
            StartCoroutine(RecoilAndFlash());
        }

        private IEnumerator RecoilAndFlash()
        {
            flashLight.enabled = true;
            const float back = 0.035f, forward = 0.09f;

            for (float t = 0f; t < back; t += Time.deltaTime)
            {
                SetSlideOffset(config.recoilDistance * (t / back));
                if (t > 0.03f) flashLight.enabled = false;
                yield return null;
            }

            flashLight.enabled = false;
            for (float t = 0f; t < forward; t += Time.deltaTime)
            {
                SetSlideOffset(config.recoilDistance * (1f - t / forward));
                yield return null;
            }
            SetSlideOffset(0f);
        }

        private void SetSlideOffset(float distance)
        {
            for (int i = 0; i < slideParts.Length; i++)
                slideParts[i].localPosition = slideRestPositions[i] - Vector3.forward * distance;
            if (triggerPart != null) triggerPart.localRotation = Quaternion.Euler(distance > 0.002f ? -14f : 0f, 0f, 0f);
        }

        private void SendHaptics(Transform interactor)
        {
            var player = interactor.GetComponentInParent<HapticImpulsePlayer>();
            if (player != null) player.SendHapticImpulse(config.hapticAmplitude, config.hapticDuration);
        }

        /// <summary>Puts the pistol back on the table, letting go of it first if a hand still holds it.</summary>
        public void ReturnToTable()
        {
            StopAllCoroutines();
            flashLight.enabled = false;
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            desktopHeld = false;
            grab.enabled = true;
            nextShotTime = 0f;
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);

            releasedAt = -1f;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true; // resting on the table without physics so it never tips over
            transform.SetPositionAndRotation(homePosition, homeRotation);
            laser.SetVisible(false);
            SetSlideOffset(0f);
        }
    }
}
