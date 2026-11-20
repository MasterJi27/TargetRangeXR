using UnityEngine;

namespace TargetRange.Targets
{
    /// <summary>Slides a platform (with its target on top) back and forth along a line, like a target on a rail.</summary>
    public sealed class PlatformMover : MonoBehaviour
    {
        [SerializeField] private Vector3 axis = Vector3.right;
        [SerializeField, Min(0f)] private float amplitude = 2f;
        [SerializeField, Min(0.1f)] private float periodSeconds = 5f;
        [SerializeField, Range(0f, 1f), Tooltip("Where in the cycle this platform starts, so neighbours do not move in sync.")]
        private float phase;

        private Vector3 origin;
        private float elapsed;

        /// <summary>Current speed, handed to a target when it is knocked off so it keeps flying in the same direction.</summary>
        public Vector3 Velocity { get; private set; }

        private void Awake() => origin = transform.position;

        private void Update()
        {
            elapsed += Time.deltaTime;
            Apply();
        }

        public void ResetToStart()
        {
            elapsed = 0f;
            Apply();
        }

        private void Apply()
        {
            float angle = (elapsed / periodSeconds + phase) * Mathf.PI * 2f;
            Vector3 direction = axis.normalized;
            transform.position = origin + direction * (Mathf.Sin(angle) * amplitude);
            Velocity = direction * (Mathf.Cos(angle) * amplitude * Mathf.PI * 2f / periodSeconds);
        }
    }
}
