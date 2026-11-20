using UnityEngine;
using TargetRange.Core;

namespace TargetRange.Weapon
{
    /// <summary>
    /// A high-speed pooled projectile with continuous swept raycasting.
    /// Filters out shooter/gun colliders to prevent self-collision and guarantees reliable impact detection.
    /// </summary>
    public sealed class Bullet : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trail;

        private RangeConfig config;
        private BulletPool pool;
        private Vector3 direction;
        private float age;
        private Collider[] ignoredColliders;

        public void Setup(BulletPool owner, RangeConfig settings)
        {
            pool = owner;
            config = settings;
        }

        public void Launch(Vector3 position, Vector3 shootDirection, Collider[] ignored = null)
        {
            direction = shootDirection.normalized;
            age = 0f;
            ignoredColliders = ignored;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            if (trail != null) trail.Clear();
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= config.bulletLifetime)
            {
                pool.ReportMiss();
                pool.Release(this);
                return;
            }

            float stepLength = config.bulletSpeed * Time.deltaTime;
            RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, stepLength, config.bulletMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                {
                    if (IsIgnored(hit.collider)) continue;
                    Impact(hit);
                    return;
                }
            }
            transform.position += direction * stepLength;
        }

        private bool IsIgnored(Collider col)
        {
            if (ignoredColliders == null || col == null) return false;
            for (int i = 0; i < ignoredColliders.Length; i++)
            {
                if (ignoredColliders[i] == col) return true;
            }
            return false;
        }

        private void Impact(RaycastHit hit)
        {
            Vector3 push = direction * config.bulletImpulse + Vector3.up * (config.bulletImpulse * config.upwardBias);
            var shot = new ShotInfo(hit.point, direction, hit.normal, push);

            IShootable shootable = hit.collider.GetComponentInParent<IShootable>();
            shootable?.OnShot(shot);

            pool.ReportImpact(new ImpactInfo(hit.point, hit.normal, shootable != null));
            pool.Release(this);
        }
    }
}
