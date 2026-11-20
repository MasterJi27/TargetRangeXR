using System.Collections;
using UnityEngine;

namespace TargetRange.UI
{
    /// <summary>Tiny coroutine animations. They use unscaled time so the end screen still animates when timeScale is 0.</summary>
    public static class UiTween
    {
        public static IEnumerator Fade(CanvasGroup group, float target, float duration)
        {
            float start = group.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            group.alpha = target;
        }

        /// <summary>Quickly scales up and settles back, used when a number changes.</summary>
        public static IEnumerator Punch(Transform target, float extraScale, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                target.localScale = Vector3.one * (1f + extraScale * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
