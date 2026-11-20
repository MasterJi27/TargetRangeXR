using System.Collections;
using TMPro;
using UnityEngine;

namespace TargetRange.UI
{
    /// <summary>
    /// A floating 3D score popup that bursts at the target impact position,
    /// punches scale, floats up, billboards toward the player, and fades out cleanly.
    /// Provides immediate, foolproof visual feedback for every hit.
    /// </summary>
    public sealed class ScorePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;

        private static Camera mainCamera;
        private ScorePopupManager poolOwner;

        public void Initialize(ScorePopupManager owner)
        {
            poolOwner = owner;
            if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        public void Play(Vector3 worldPos, int points, bool isBullseye)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            transform.position = worldPos + Vector3.up * 0.1f;
            gameObject.SetActive(true);

            if (isBullseye)
            {
                textMesh.text = $"<size=130%><color=#FFD700>★ BULLSEYE! ★</color></size>\n<color=#FFA500>+{points}</color>";
            }
            else
            {
                string hexColor = points >= 30 ? "#00E5FF" : points >= 20 ? "#FFB300" : "#76FF03";
                textMesh.text = $"<color={hexColor}>+{points}</color>";
            }

            StopAllCoroutines();
            StartCoroutine(AnimatePopup());
        }

        private IEnumerator AnimatePopup()
        {
            float duration = 1.1f;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * 0.65f;
            Color baseColor = textMesh.color;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float progress = t / duration;

                // Billboard towards player head
                if (mainCamera != null)
                {
                    transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
                }

                // Smooth upward float
                transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, progress));

                // Punch scale bounce: expands quickly then settles
                float scale = progress < 0.2f
                    ? Mathf.Lerp(0f, 1.35f, progress / 0.2f)
                    : Mathf.Lerp(1.35f, 1.0f, (progress - 0.2f) / 0.8f);
                transform.localScale = Vector3.one * (scale * 0.45f);

                // Alpha fade out in second half
                float alpha = progress > 0.6f ? Mathf.Lerp(1f, 0f, (progress - 0.6f) / 0.4f) : 1f;
                baseColor.a = alpha;
                textMesh.color = baseColor;

                yield return null;
            }

            gameObject.SetActive(false);
            if (poolOwner != null) poolOwner.ReturnToPool(this);
        }
    }
}

