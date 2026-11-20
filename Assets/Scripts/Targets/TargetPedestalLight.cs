using System.Collections;
using UnityEngine;

namespace TargetRange.Targets
{
    /// <summary>
    /// Visual status indicator mounted on each target platform.
    /// Glows vivid green when the target is armed and ready; pulses red when knocked down and respawning.
    /// </summary>
    public sealed class TargetPedestalLight : MonoBehaviour
    {
        [SerializeField] private MeshRenderer lightRenderer;
        [SerializeField] private Light pointLight;

        private static readonly Color ReadyColor = new Color(0.1f, 1.0f, 0.35f);
        private static readonly Color KnockedColor = new Color(1.0f, 0.22f, 0.15f);

        private Material materialInstance;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            if (lightRenderer != null)
            {
                materialInstance = lightRenderer.material; // unique instance
            }
            SetTargetActive(true);
        }

        public void SetTargetActive(bool active)
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }

            if (active)
            {
                ApplyColor(ReadyColor, 2.2f);
            }
            else
            {
                pulseRoutine = StartCoroutine(PulseKnocked());
            }
        }

        private IEnumerator PulseKnocked()
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                float intensity = Mathf.Lerp(0.8f, 2.8f, t);
                ApplyColor(KnockedColor, intensity);
                yield return null;
            }
        }

        private void ApplyColor(Color baseColor, float intensity)
        {
            Color emission = baseColor * intensity;
            if (materialInstance != null)
            {
                materialInstance.SetColor("_EmissionColor", emission);
                materialInstance.EnableKeyword("_EMISSION");
            }
            if (pointLight != null)
            {
                pointLight.color = baseColor;
                pointLight.intensity = intensity * 0.8f;
            }
        }

        private void OnDestroy()
        {
            if (materialInstance != null)
            {
                Destroy(materialInstance);
            }
        }
    }
}

