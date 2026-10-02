using ForgottenTrail.Gameplay.Awareness;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Awareness
{
    public sealed class EnemyAwarenessTests
    {
        [Test]
        public void StartsCalm()
        {
            var awareness = new EnemyAwareness(10f, 2f, 0.25f);

            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Calm));
            Assert.That(awareness.Suspicion, Is.EqualTo(0f));
        }

        [Test]
        public void NoiseInsideScaledRangeRaisesSuspicionWithoutImmediateAlert()
        {
            var awareness = new EnemyAwareness(10f, 2f, 0.25f);

            var heard = awareness.HearNoise(4f, 0.5f);

            Assert.That(heard, Is.True);
            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Suspicious));
            Assert.That(awareness.Suspicion, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void NoiseBeyondScaledRangeIsIgnored()
        {
            var awareness = new EnemyAwareness(10f, 2f, 0.25f);

            var heard = awareness.HearNoise(5.1f, 0.5f);

            Assert.That(heard, Is.False);
            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Calm));
        }

        [Test]
        public void ContinuousLineOfSightRaisesSuspicionToAlert()
        {
            var awareness = new EnemyAwareness(10f, 2f, 0.25f);

            awareness.Tick(1.25f, hasLineOfSight: true);
            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Suspicious));

            awareness.Tick(0.75f, hasLineOfSight: true);

            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Alerted));
            Assert.That(awareness.Suspicion, Is.EqualTo(1f));
        }

        [Test]
        public void SuspicionDecaysToCalmAfterLineOfSightIsLost()
        {
            var awareness = new EnemyAwareness(10f, 2f, 0.5f);
            awareness.HearNoise(2f, 1f);

            awareness.Tick(2f, hasLineOfSight: false);

            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Calm));
            Assert.That(awareness.Suspicion, Is.EqualTo(0f));
        }

        [Test]
        public void AlertCanDeescalateAfterThePlayerBreaksLineOfSight()
        {
            var awareness = new EnemyAwareness(10f, 2f, 0.5f);
            awareness.Tick(2f, hasLineOfSight: true);
            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Alerted));

            awareness.Tick(0.8f, hasLineOfSight: false);

            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Suspicious));
            Assert.That(awareness.Suspicion, Is.EqualTo(0.6f).Within(0.001f));

            awareness.Tick(1.2f, hasLineOfSight: false);

            Assert.That(awareness.State, Is.EqualTo(EnemyAlertState.Calm));
            Assert.That(awareness.Suspicion, Is.EqualTo(0f));
        }
    }
}
