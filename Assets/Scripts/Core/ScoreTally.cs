using UnityEngine;

namespace TargetRange.Core
{
    /// <summary>
    /// The score rules: points, shots, targets hit, streaks, bullseyes and accuracy.
    /// Includes high score persistence and streak multipliers to make the gameplay feel rewarding and scoring-proof.
    /// </summary>
    public sealed class ScoreTally
    {
        private const string HighScoreKey = "TargetRange_HighScore";

        public int Score { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int CurrentStreak { get; private set; }
        public int BestStreak { get; private set; }
        public int Bullseyes { get; private set; }
        public int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);
        public bool IsNewHighScore { get; private set; }

        /// <summary>Share of shots that knocked a target down, from 0 to 1. Zero before the first shot.</summary>
        public float Accuracy => Shots == 0 ? 0f : Mathf.Clamp01((float)Hits / Shots);

        public void RegisterShot()
        {
            Shots++;
        }

        public int RegisterHit(int basePoints, bool isBullseye = false)
        {
            Hits++;
            CurrentStreak++;
            if (CurrentStreak > BestStreak) BestStreak = CurrentStreak;
            if (isBullseye) Bullseyes++;

            // Streak multiplier: 1x for 1-2 hits, 1.5x for 3-4 hits, 2x for 5+ hits
            float multiplier = CurrentStreak >= 5 ? 2.0f : CurrentStreak >= 3 ? 1.5f : 1.0f;
            int finalPoints = Mathf.RoundToInt(Mathf.Max(0, basePoints) * multiplier);
            Score += finalPoints;

            if (Score > HighScore)
            {
                IsNewHighScore = true;
                PlayerPrefs.SetInt(HighScoreKey, Score);
                PlayerPrefs.Save();
            }

            return finalPoints;
        }

        public void RegisterMiss()
        {
            CurrentStreak = 0;
        }

        public void Reset()
        {
            IsNewHighScore = false;
            Score = 0;
            Shots = 0;
            Hits = 0;
            CurrentStreak = 0;
            BestStreak = 0;
            Bullseyes = 0;
        }
    }
}
