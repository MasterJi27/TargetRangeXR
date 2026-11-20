using UnityEngine;
using TargetRange.Core;
using TargetRange.Targets;
using TargetRange.Weapon;

namespace TargetRange.Feedback
{
    /// <summary>
    /// The central hub connecting game events to auditory and visual feedback.
    /// Handles gunshots, hits, bullseye chimes, grab sounds, and countdown audio.
    /// </summary>
    public sealed class FeedbackController : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private Pistol pistol;
        [SerializeField] private BulletPool bulletPool;
        [SerializeField] private AudioService audioService;
        [SerializeField] private EffectPool impactEffects;

        private int lastWholeSecond = -1;

        private void OnEnable()
        {
            game.StateChanged += OnStateChanged;
            game.TimeChanged += OnTimeChanged;
            pistol.ShotFired += OnShotFired;
            pistol.DryFired += OnDryFired;
            pistol.Grabbed += OnGrabbed;
            bulletPool.Impact += OnImpact;
            Target.AnyTargetHit += OnTargetHit;
        }

        private void OnDisable()
        {
            if (game != null)
            {
                game.StateChanged -= OnStateChanged;
                game.TimeChanged -= OnTimeChanged;
            }
            if (pistol != null)
            {
                pistol.ShotFired -= OnShotFired;
                pistol.DryFired -= OnDryFired;
                pistol.Grabbed -= OnGrabbed;
            }
            if (bulletPool != null) bulletPool.Impact -= OnImpact;
            Target.AnyTargetHit -= OnTargetHit;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing) audioService.Play(SfxId.GameStart);
            else if (state == GameState.Ended) audioService.Play(SfxId.GameEnd);
            lastWholeSecond = -1;
        }

        private void OnTimeChanged(float remaining)
        {
            int whole = Mathf.CeilToInt(remaining);
            if (whole != lastWholeSecond && whole > 0 && remaining <= game.Config.lowTimeWarningSeconds)
                audioService.Play(SfxId.Tick);
            lastWholeSecond = whole;
        }

        private void OnShotFired() => audioService.PlayAt(SfxId.Shot, pistol.Muzzle.position);
        private void OnDryFired() => audioService.PlayAt(SfxId.DryFire, pistol.Muzzle.position);
        private void OnGrabbed() => audioService.PlayAt(SfxId.Grab, pistol.transform.position);

        private void OnImpact(ImpactInfo impact)
        {
            audioService.PlayAt(SfxId.Hit, impact.Point);
            impactEffects.Play(impact.Point, impact.Normal);
        }

        private void OnTargetHit(Vector3 point, bool isBullseye)
        {
            if (isBullseye)
            {
                audioService.PlayAt(SfxId.Bullseye, point);
            }
        }
    }
}
