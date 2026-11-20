using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TargetRange.UI
{
    /// <summary>
    /// Singleton pool for 3D floating score popups.
    /// Reuses label objects; text formatting and pool growth can still allocate.
    /// </summary>
    public sealed class ScorePopupManager : MonoBehaviour
    {
        private static ScorePopupManager instance;

        [SerializeField] private TMP_FontAsset popupFont;
        [SerializeField] private int initialPoolSize = 10;

        private readonly Queue<ScorePopup> available = new Queue<ScorePopup>();

        private void Awake()
        {
            instance = this;
            WarmPool();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void WarmPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                available.Enqueue(CreateInstance());
            }
        }

        private ScorePopup CreateInstance()
        {
            var go = new GameObject("ScorePopup");
            go.transform.SetParent(transform, false);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = popupFont;
            tmp.fontSize = 7.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 50;

            var popup = go.AddComponent<ScorePopup>();
            popup.Initialize(this);
            go.SetActive(false);
            return popup;
        }

        public static void Show(Vector3 worldPos, int points, bool isBullseye)
        {
            if (instance == null) return;

            ScorePopup popup = instance.available.Count > 0 ? instance.available.Dequeue() : instance.CreateInstance();
            popup.Play(worldPos, points, isBullseye);
        }

        public void ReturnToPool(ScorePopup popup)
        {
            available.Enqueue(popup);
        }
    }
}

