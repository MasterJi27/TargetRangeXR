using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TargetRange.Core;

namespace TargetRange.UI
{
    /// <summary>
    /// The end of round panel. A world-space canvas that animates directly in front of the player's view
    /// when time runs out, displaying final score, rank, star rating, best streak, and accuracy.
    /// Buttons are clickable with controller rays or mouse clicks.
    /// </summary>
    public sealed class ResultsView : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private Transform head;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private PanelFader fader;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;
        [SerializeField, Tooltip("The small boards on the bench. They are hidden while the results are up so they do not poke through the panel.")]
        private GameObject[] hideWhileShown;

        private void Awake()
        {
            restartButton.onClick.AddListener(game.RestartRound);
            exitButton.onClick.AddListener(game.QuitGame);
            foreach (var label in new[] { rankText, statsText })
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 22;
                label.fontSizeMax = label.fontSize;
            }
            panelRoot.SetActive(false);
        }

        private void OnEnable() => game.StateChanged += OnStateChanged;

        private void OnDisable()
        {
            if (game != null) game.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            foreach (GameObject board in hideWhileShown)
            {
                if (board != null) board.SetActive(state != GameState.Ended);
            }

            if (state != GameState.Ended)
            {
                panelRoot.SetActive(false);
                return;
            }

            ScoreTally tally = game.Score;
            scoreText.text = tally.Score.ToString();

            string rank = game.Config.RankFor(tally.Score);
            string recordTag = tally.IsNewHighScore ? "  NEW RECORD!" : $"  (Best: {tally.HighScore})";
            rankText.text = $"{rank}{recordTag}";

            statsText.text = $"{tally.Hits} targets down ({tally.Bullseyes} Bullseyes)   |   Streak: x{tally.BestStreak}   |   {Mathf.RoundToInt(tally.Accuracy * 100f)}% accuracy";

            PlaceInFrontOfHead();
            panelRoot.SetActive(true);
            fader.Show(true);
        }

        /// <summary>Puts the panel where the player is looking, level with the head, turned to face the player.</summary>
        private void PlaceInFrontOfHead()
        {
            var bridge = FindAnyObjectByType<XRDesktopBridge>();
            var canvas = GetComponent<Canvas>();
            if (bridge != null && bridge.IsDesktopMode)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;
                var scaler = GetComponent<CanvasScaler>();
                if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600, 1000);
                scaler.matchWidthOrHeight = 0.5f;
                var rect = (RectTransform)panelRoot.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1100, 800);
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                return;
            }
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)transform;
            canvasRect.sizeDelta = new Vector2(1100, 800);
            canvasRect.localScale = Vector3.one * (1.15f / 1100f);
            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (head == null) return;

            Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
            flatForward.Normalize();

            transform.position = head.position + flatForward * game.Config.resultsDistance + Vector3.down * 0.12f;
            transform.rotation = Quaternion.LookRotation(flatForward);
        }
    }
}

