using System;
using UnityEngine;

namespace TargetRange.Core
{
    /// <summary>
    /// Owns the round: Ready -> Playing -> Ended -> (Restart) -> Ready.
    /// It does not know about the pistol, targets or UI. They subscribe to its events instead,
    /// which keeps every script small and easy to explain on its own.
    /// </summary>
    [DefaultExecutionOrder(-100)] // run Awake before every other script, so they can safely read the timer in their OnEnable
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private RangeConfig config;

        private CountdownTimer timer;

        public GameState State { get; private set; } = GameState.Ready;
        public float RemainingSeconds => timer.Remaining;
        public float DurationSeconds => timer.Duration;
        public RangeConfig Config => config;
        public ScoreTally Score { get; } = new ScoreTally();

        /// <summary>Raised whenever the phase changes.</summary>
        public event Action<GameState> StateChanged;
        /// <summary>Raised every frame while playing, with the seconds left.</summary>
        public event Action<float> TimeChanged;
        /// <summary>Raised after the score, shots or hits changed. The argument is how many points were just added.</summary>
        public event Action<int> ScoreChanged;
        /// <summary>Raised on Restart so targets and the pistol can put themselves back to the start layout.</summary>
        public event Action RoundReset;

        private void Awake()
        {
            timer = new CountdownTimer(config.roundDurationSeconds);
            Time.timeScale = 1f;
        }

        private void Start() => EnterState(GameState.Ready);

        private void Update()
        {
            if (State == GameState.Playing) Advance(Time.deltaTime);
        }

        /// <summary>Moves the clock forward. Update calls this every frame; tests call it to fast forward a whole round.</summary>
        public void Advance(float deltaTime)
        {
            if (State != GameState.Playing) return;

            bool finished = timer.Advance(deltaTime);
            TimeChanged?.Invoke(timer.Remaining);
            if (finished) EndRound();
        }

        /// <summary>Called when the player first grabs the pistol.</summary>
        public void BeginRound()
        {
            if (State != GameState.Ready) return;
            timer.Reset();
            Score.Reset();
            ScoreChanged?.Invoke(0);
            EnterState(GameState.Playing);
        }

        public void EndRound()
        {
            if (State != GameState.Playing) return;
            EnterState(GameState.Ended);
        }

        public void RestartRound()
        {
            Time.timeScale = 1f;
            timer.Reset();
            Score.Reset();
            RoundReset?.Invoke();
            ScoreChanged?.Invoke(0);
            TimeChanged?.Invoke(timer.Remaining);
            EnterState(GameState.Ready);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void RegisterShot()
        {
            if (State != GameState.Playing) return;
            Score.RegisterShot();
            ScoreChanged?.Invoke(0);
        }

        public int RegisterHit(int points, bool isBullseye = false)
        {
            if (State != GameState.Playing) return 0;
            int added = Score.RegisterHit(points, isBullseye);
            ScoreChanged?.Invoke(added);
            return added;
        }

        public void RegisterMiss()
        {
            if (State != GameState.Playing) return;
            Score.RegisterMiss();
        }

        private void EnterState(GameState next)
        {
            State = next;
            // Freezing the game = stopping simulated time: physics, particles and moving platforms stop.
            // UI animations use unscaled time, so the results panel still fades in.
            Time.timeScale = next == GameState.Ended ? 0f : 1f;
            StateChanged?.Invoke(next);
        }
    }
}
