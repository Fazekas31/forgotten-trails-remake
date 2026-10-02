using ForgottenTrail.Gameplay.Church;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Church
{
    public sealed class ChurchInvestigationStateTests
    {
        [Test]
        public void FalseEliasCanOnlyBeRecognizedAfterTheBellLightAndLedgerClues()
        {
            var state = new ChurchInvestigationState();

            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.BellRopeInteractionId), Is.True);
            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.UnshadowedLightInteractionId), Is.True);
            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.FalseEliasInteractionId), Is.False);
            Assert.That(state.HasRecognizedFalseElias, Is.False);

            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.ParishLedgerInteractionId), Is.True);
            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.FalseEliasInteractionId), Is.True);

            Assert.That(state.HasRecognizedFalseElias, Is.True);
        }

        [Test]
        public void DeputyBadgeIsOnlyRecoveredAfterTheFalseEliasEncounter()
        {
            var state = new ChurchInvestigationState();
            state.TryRecordInteraction(ChurchInvestigationState.BellRopeInteractionId);
            state.TryRecordInteraction(ChurchInvestigationState.UnshadowedLightInteractionId);
            state.TryRecordInteraction(ChurchInvestigationState.ParishLedgerInteractionId);

            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.DeputyBadgeInteractionId), Is.False);
            Assert.That(state.HasRecoveredDeputyBadge, Is.False);
            Assert.That(state.IsComplete, Is.False);

            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.FalseEliasInteractionId), Is.True);
            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.DeputyBadgeInteractionId), Is.True);
            Assert.That(state.HasRecoveredDeputyBadge, Is.True);
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.TryRecordInteraction(ChurchInvestigationState.DeputyBadgeInteractionId), Is.False);
        }
    }
}
