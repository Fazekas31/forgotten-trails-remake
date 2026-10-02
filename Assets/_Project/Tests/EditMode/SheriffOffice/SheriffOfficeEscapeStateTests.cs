using ForgottenTrail.Gameplay.SheriffOffice;
using NUnit.Framework;

namespace ForgottenTrail.Tests.SheriffOffice
{
    public sealed class SheriffOfficeEscapeStateTests
    {
        [Test]
        public void EscapeStartsOnlyOnceAfterTheBookBeat()
        {
            var state = new SheriffOfficeEscapeState();

            Assert.That(state.TryEscape(true, false, true), Is.False);
            Assert.That(state.Begin(), Is.True);
            Assert.That(state.Begin(), Is.False);
            Assert.That(state.HasEscaped, Is.False);
        }

        [Test]
        public void EscapeRequiresCrouchingWithTheLanternOffAndCanCompleteOnce()
        {
            var state = new SheriffOfficeEscapeState();
            state.Begin();

            Assert.That(state.TryEscape(false, false, true), Is.False);
            Assert.That(state.TryEscape(true, true, true), Is.False);
            Assert.That(state.TryEscape(true, false, false), Is.False);
            Assert.That(state.TryEscape(true, false, true), Is.True);
            Assert.That(state.TryEscape(true, false, true), Is.False);
            Assert.That(state.HasEscaped, Is.True);
        }
    }
}
