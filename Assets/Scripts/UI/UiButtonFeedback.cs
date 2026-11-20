using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TargetRange.Feedback;

namespace TargetRange.UI
{
    /// <summary>Hover grow animation plus hover and click sounds for any Button.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private AudioService audioService;
        [SerializeField, Min(1f)] private float hoverScale = 1.05f;

        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
            GetComponent<Button>().onClick.AddListener(() => audioService.Play(SfxId.UiClick));
        }

        private void OnDisable() => transform.localScale = baseScale;

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = baseScale * hoverScale;
            audioService.Play(SfxId.UiHover);
        }

        public void OnPointerExit(PointerEventData eventData) => transform.localScale = baseScale;
    }
}
