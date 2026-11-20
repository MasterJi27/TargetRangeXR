using UnityEngine;

namespace TargetRange.UI
{
    /// <summary>Fades a whole panel in or out and keeps it from catching clicks while hidden.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PanelFader : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float duration = 0.25f;

        private CanvasGroup group;
        private Coroutine running;

        private CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        public void Show(bool visible, bool instant = false)
        {
            Group.interactable = visible;
            Group.blocksRaycasts = visible;

            if (running != null) StopCoroutine(running);
            if (visible && !instant) Group.alpha = 0f; // always fade in from transparent
            if (instant || !isActiveAndEnabled)
            {
                Group.alpha = visible ? 1f : 0f;
                return;
            }
            running = StartCoroutine(UiTween.Fade(Group, visible ? 1f : 0f, duration));
        }
    }
}
