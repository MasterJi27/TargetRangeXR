using UnityEngine;

namespace TargetRange.Core
{
    /// <summary>
    /// Every tunable number of the game in one asset (Assets/Settings/RangeConfig.asset).
    /// Gameplay balancing lives here; UI layout and desktop controls have their own settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Target Range/Range Config", fileName = "RangeConfig")]
    public sealed class RangeConfig : ScriptableObject
    {
        [Header("Round")]
        [Min(1f)] public float roundDurationSeconds = 60f;
        [Min(0f)] public float lowTimeWarningSeconds = 10f;

        [Header("Bullet")]
        [Min(1f)] public float bulletSpeed = 55f;
        [Min(0.1f)] public float bulletLifetime = 2.5f;
        [Tooltip("Momentum handed to a target on impact (kg m/s). A target weighs about 1.5 kg.")]
        [Min(0f)] public float bulletImpulse = 7f;
        [Tooltip("Extra upward push so a hit target tips off its platform instead of sliding.")]
        [Range(0f, 1f)] public float upwardBias = 0.18f;
        public LayerMask bulletMask = ~0;

        [Header("Pistol")]
        [Min(0.02f)] public float secondsBetweenShots = 0.18f;
        [Min(0f)] public float recoilDistance = 0.022f;
        [Range(0f, 1f)] public float hapticAmplitude = 0.7f;
        [Min(0f)] public float hapticDuration = 0.08f;
        [Tooltip("A released pistol flies back to the table after this many seconds.")]
        [Min(1f)] public float returnToTableDelay = 4f;

        [Header("Targets")]
        [Min(0.1f)] public float knockedTargetVanishDelay = 1.6f;
        [Min(0.05f)] public float vanishDuration = 0.35f;
        [Tooltip("A knocked target comes back on its platform after this many seconds, so there is always something to shoot.")]
        [Min(1f)] public float targetRespawnDelay = 6f;
        [Min(0.05f)] public float respawnGrowDuration = 0.3f;

        [Header("Results")]
        [Min(0.3f)] public float resultsDistance = 1.7f;
        [Tooltip("Score needed for each rank on the results panel, lowest first.")]
        public RankThreshold[] ranks =
        {
            new RankThreshold { minScore = 0, title = "ROOKIE" },
            new RankThreshold { minScore = 50, title = "MARKSMAN" },
            new RankThreshold { minScore = 100, title = "SHARPSHOOTER" },
            new RankThreshold { minScore = 160, title = "DEAD EYE" }
        };

        public string RankFor(int score)
        {
            string best = ranks.Length > 0 ? ranks[0].title : string.Empty;
            foreach (RankThreshold rank in ranks)
                if (score >= rank.minScore) best = rank.title;
            return best;
        }
    }

    [System.Serializable]
    public struct RankThreshold
    {
        public int minScore;
        public string title;
    }
}
