using UnityEngine;

namespace TargetRange.Core
{
    /// <summary>Plain C# countdown (no MonoBehaviour) so it can be unit tested without entering Play mode.</summary>
    public sealed class CountdownTimer
    {
        public float Duration { get; private set; }
        public float Remaining { get; private set; }
        public bool IsFinished => Remaining <= 0f;

        public CountdownTimer(float duration) => Reset(duration);

        public void Reset(float duration)
        {
            Duration = Mathf.Max(0f, duration);
            Remaining = Duration;
        }

        public void Reset() => Reset(Duration);

        /// <summary>Advances the clock. Returns true only on the call that reaches zero.</summary>
        public bool Advance(float deltaTime)
        {
            if (IsFinished) return false;
            Remaining = Mathf.Max(0f, Remaining - deltaTime);
            return IsFinished;
        }
    }
}
