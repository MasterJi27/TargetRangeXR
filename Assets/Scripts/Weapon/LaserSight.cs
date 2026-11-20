using UnityEngine;
using TargetRange.Core;

namespace TargetRange.Weapon
{
    /// <summary>
    /// A thin red laser from the muzzle with a dot where it lands. It is an aiming aid for the desktop simulator
    /// (where judging depth is hard) and only shows while the pistol is in a hand.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LaserSight : MonoBehaviour
    {
        [SerializeField] private RangeConfig config;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform dot;
        [SerializeField, Min(1f)] private float maxDistance = 30f;

        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            enabled = visible;
            line.enabled = visible;
            if (dot != null) dot.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            Vector3 start = muzzle.position;
            Vector3 end = start + muzzle.forward * maxDistance;
            bool hitSomething = Physics.Raycast(start, muzzle.forward, out RaycastHit hit, maxDistance, config.bulletMask, QueryTriggerInteraction.Ignore);
            if (hitSomething) end = hit.point;

            line.SetPosition(0, start);
            line.SetPosition(1, end);

            if (dot == null) return;
            dot.gameObject.SetActive(hitSomething);
            if (hitSomething) dot.SetPositionAndRotation(hit.point + hit.normal * 0.004f, Quaternion.LookRotation(-hit.normal));
        }
    }
}
