using ForgottenTrail.Gameplay.Combat;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using NUnit.Framework;

namespace ForgottenTrail.Tests.SheriffOffice
{
    public sealed class SheriffOfficeInvestigationStateTests
    {
        [Test]
        public void RedBookRequiresBothScreenplayPartsOfTheHaleConversation()
        {
            var state = new SheriffOfficeInvestigationState();

            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.RedBookInteractionId), Is.False);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.HaleInteractionId), Is.False);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.LaylaEvidenceInteractionId), Is.True);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.HaleInteractionId), Is.True);
            Assert.That(state.HasMetHale, Is.True);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.RedBookInteractionId), Is.False);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.HaleAccountInteractionId), Is.True);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.RedBookInteractionId), Is.True);
            Assert.That(state.HasFoundRedBook, Is.True);
        }

        [Test]
        public void OfficeRouteCompletesAfterTheBookAndQuietEscapeWithoutARevolver()
        {
            var state = new SheriffOfficeInvestigationState();
            state.TryRecord(SheriffOfficeInvestigationState.LaylaEvidenceInteractionId);
            state.TryRecord(SheriffOfficeInvestigationState.HaleInteractionId);
            state.TryRecord(SheriffOfficeInvestigationState.HaleAccountInteractionId);
            state.TryRecord(SheriffOfficeInvestigationState.RedBookInteractionId);

            Assert.That(state.IsComplete, Is.False);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.QuietEscapeInteractionId), Is.True);
            Assert.That(state.IsComplete, Is.True);
            Assert.That(state.TryRecord(SheriffOfficeInvestigationState.QuietEscapeInteractionId), Is.False);
        }

        [Test]
        public void SavingShotCanBeAcquiredOnceAndFiredOnlyOnce()
        {
            var inventory = new SavingShotInventoryState();

            Assert.That(inventory.TryAcquireRevolver(), Is.True);
            Assert.That(inventory.HasRevolver, Is.True);
            Assert.That(inventory.RoundsRemaining, Is.EqualTo(1));
            Assert.That(inventory.TryAcquireRevolver(), Is.False);
            Assert.That(inventory.TryFire(), Is.True);
            Assert.That(inventory.RoundsRemaining, Is.Zero);
            Assert.That(inventory.TryFire(), Is.False);
        }
    }
}
