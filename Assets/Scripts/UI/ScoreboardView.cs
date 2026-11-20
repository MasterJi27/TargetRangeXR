using TMPro;
using UnityEngine;
using TargetRange.Core;

namespace TargetRange.UI
{
    /// <summary>
    /// A live scoreboard showing real-time score, countdown timer, shots, hits, accuracy, and active streaks.
    /// Punches scale and highlights on hits and streaks for foolproof scoring feedback.
    /// </summary>
    public sealed class ScoreboardView : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.74f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.33f, 0.3f);

        private int lastScore = -1;
        private int lastSecond = -1;

        private void OnEnable()
        {
            game.StateChanged += OnStateChanged;
            game.TimeChanged += OnTimeChanged;
            game.ScoreChanged += OnScoreChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (game == null) return;
            game.StateChanged -= OnStateChanged;
            game.TimeChanged -= OnTimeChanged;
            game.ScoreChanged -= OnScoreChanged;
        }

        private void Refresh()
        {
            OnTimeChanged(game.RemainingSeconds);
            OnScoreChanged(0);
            OnStateChanged(game.State);
        }

        private void OnScoreChanged(int added)
        {
            scoreText.text = game.Score.Score.ToString("000");
            int acc = Mathf.RoundToInt(game.Score.Accuracy * 100f);
            int streak = game.Score.CurrentStreak;
            string streakTag = streak >= 3 ? $"  |  <color=#FFD700>STREAK x{streak}!</color>" : "";
            statsText.text = $"SHOTS {game.Score.Shots}   HITS {game.Score.Hits} ({acc}%){streakTag}";

            if (added > 0 && game.Score.Score != lastScore && isActiveAndEnabled)
            {
                StartCoroutine(UiTween.Punch(scoreText.transform, 0.25f, 0.3f));
            }
            lastScore = game.Score.Score;
        }

        private void OnTimeChanged(float seconds)
        {
            int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            timeText.text = $"{whole / 60:00}:{whole % 60:00}";

            float low = game.Config.lowTimeWarningSeconds;
            timeText.color = seconds <= low ? criticalColor : seconds <= low * 2f ? warningColor : normalColor;

            if (whole != lastSecond && seconds <= low && seconds > 0f && isActiveAndEnabled)
                StartCoroutine(UiTween.Punch(timeText.transform, 0.12f, 0.25f));
            lastSecond = whole;
        }

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Ready:
                    int best = game.Score.HighScore;
                    statusText.text = best > 0 ? $"RECORD: {best} PTS  |  GRAB / PRESS [E] TO START" : "GRAB OR PRESS [E] TO START";
                    OnTimeChanged(game.DurationSeconds);
                    break;
                case GameState.Playing:
                    statusText.text = "KNOCK THEM DOWN — HIT CENTER FOR BULLSEYE!";
                    break;
                default:
                    statusText.text = "TIME UP";
                    break;
            }
        }
    }
}
