namespace TargetRange.Core
{
    /// <summary>The three phases of a round. The scoreboard, the pistol and the results panel all react to these.</summary>
    public enum GameState
    {
        /// <summary>Waiting for the player to pick up the pistol. Timer and score are at their start values.</summary>
        Ready,
        /// <summary>The 60 second round is running.</summary>
        Playing,
        /// <summary>Time is up: the game is frozen and the results panel is visible.</summary>
        Ended
    }
}
