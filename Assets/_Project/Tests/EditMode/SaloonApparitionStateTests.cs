using ForgottenTrail.Gameplay.Saloon;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Saloon
{
    public sealed class SaloonApparitionStateTests
    {
        [Test]
        public void ReadingAnotherClueDoesNotTriggerTheWindowApparition()
        {
            var state = new SaloonApparitionState();

            var triggered = state.TryTrigger("saloon.blood-trail");

            Assert.That(triggered, Is.False);
            Assert.That(state.HasTriggered, Is.False);
        }

        [Test]
        public void ReadingCarmensNoteTriggersTheApparitionOnlyOnce()
        {
            var state = new SaloonApparitionState();

            var firstReading = state.TryTrigger("saloon.torn-note");
            var secondReading = state.TryTrigger("saloon.torn-note");

            Assert.That(firstReading, Is.True);
            Assert.That(secondReading, Is.False);
            Assert.That(state.HasTriggered, Is.True);
        }
    }
}
