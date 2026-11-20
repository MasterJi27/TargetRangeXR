using NUnit.Framework;
using UnityEngine;
using TargetRange.Core;

namespace TargetRange.Tests.EditMode
{
    /// <summary>Fast tests for the plain C# rules: no scene, no Play mode.</summary>
    public class LogicTests
    {
        [Test]
        public void ScoreTally_AddsPointsAndComputesAccuracy()
        {
            var tally = new ScoreTally();
            tally.RegisterShot(); tally.RegisterShot(); tally.RegisterShot(); tally.RegisterShot();
            tally.RegisterHit(10);
            tally.RegisterHit(30);

            Assert.AreEqual(40, tally.Score);
            Assert.AreEqual(4, tally.Shots);
            Assert.AreEqual(2, tally.Hits);
            Assert.AreEqual(0.5f, tally.Accuracy, 0.0001f);
        }

        [Test]
        public void ScoreTally_AccuracyIsZeroBeforeFirstShot_AndResetClearsEverything()
        {
            var tally = new ScoreTally();
            Assert.AreEqual(0f, tally.Accuracy);

            tally.RegisterShot();
            tally.RegisterHit(20);
            tally.Reset();

            Assert.AreEqual(0, tally.Score);
            Assert.AreEqual(0, tally.Shots);
            Assert.AreEqual(0, tally.Hits);
        }

        [Test]
        public void ScoreTally_NegativePointsNeverLowerTheScore()
        {
            var tally = new ScoreTally();
            tally.RegisterHit(-5);
            Assert.AreEqual(0, tally.Score);
        }

        [Test]
        public void CountdownTimer_FinishesExactlyOnceAfterSixtySeconds()
        {
            var timer = new CountdownTimer(60f);
            Assert.IsFalse(timer.Advance(59f));
            Assert.AreEqual(1f, timer.Remaining, 0.0001f);

            Assert.IsTrue(timer.Advance(2f), "the call that reaches zero reports it");
            Assert.IsTrue(timer.IsFinished);
            Assert.AreEqual(0f, timer.Remaining);
            Assert.IsFalse(timer.Advance(1f), "it must not report the end a second time");
        }

        [Test]
        public void CountdownTimer_ResetRestoresTheFullDuration()
        {
            var timer = new CountdownTimer(60f);
            timer.Advance(45f);
            timer.Reset();
            Assert.AreEqual(60f, timer.Remaining);
            Assert.IsFalse(timer.IsFinished);
        }

        [Test]
        public void RangeConfig_DefaultsMatchTheAssignment()
        {
            var config = ScriptableObject.CreateInstance<RangeConfig>();
            Assert.AreEqual(60f, config.roundDurationSeconds, "the brief asks for a 60 second round");
            Object.DestroyImmediate(config);
        }

        [TestCase(0, "ROOKIE")]
        [TestCase(49, "ROOKIE")]
        [TestCase(50, "MARKSMAN")]
        [TestCase(120, "SHARPSHOOTER")]
        [TestCase(170, "DEAD EYE")]
        public void RangeConfig_PicksTheHighestRankReached(int score, string expected)
        {
            var config = ScriptableObject.CreateInstance<RangeConfig>();
            Assert.AreEqual(expected, config.RankFor(score));
            Object.DestroyImmediate(config);
        }
    }
}
