using ForgottenTrail.Gameplay.Alley;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Alley
{
    public sealed class ChesterJackRouteStateTests
    {
        [Test]
        public void CannotCompleteTheSheriffRouteBeforeFindingChesterAndCalmingJack()
        {
            var state = new ChesterJackRouteState();

            Assert.That(state.TryRecord(ChesterJackRouteState.SheriffOfficeExitInteractionId), Is.False);
            Assert.That(state.TryRecord(ChesterJackRouteState.GateInteractionId), Is.False);
            Assert.That(state.TryRecord(ChesterJackRouteState.JackRescueInteractionId), Is.False);
            Assert.That(state.IsComplete, Is.False);
        }

        [Test]
        public void RouteRequiresTracksChesterGateJackThenTheSheriffExitInOrder()
        {
            var state = new ChesterJackRouteState();

            Assert.That(state.TryRecord(ChesterJackRouteState.TrailInteractionId), Is.True);
            Assert.That(state.TryRecord(ChesterJackRouteState.GateInteractionId), Is.False);
            Assert.That(state.TryRecord(ChesterJackRouteState.ChesterInteractionId), Is.True);
            Assert.That(state.TryRecord(ChesterJackRouteState.GateInteractionId), Is.True);
            Assert.That(state.TryRecord(ChesterJackRouteState.JackRescueInteractionId), Is.True);
            Assert.That(state.HasRescuedJack, Is.True);
            Assert.That(state.IsJackFollowing, Is.False);
            Assert.That(state.TryRecord(ChesterJackRouteState.JackCalmInteractionId), Is.True);
            Assert.That(state.IsJackFollowing, Is.True);
            Assert.That(state.IsComplete, Is.False);

            Assert.That(state.TryRecord(ChesterJackRouteState.SheriffOfficeExitInteractionId), Is.True);
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.TryRecord(ChesterJackRouteState.SheriffOfficeExitInteractionId), Is.False);
        }
    }
}
