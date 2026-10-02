using ForgottenTrail.Gameplay.Combat;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using NUnit.Framework;

namespace ForgottenTrail.Tests.SheriffOffice
{
    public sealed class SheriffOfficeInvestigationStateTests
    {
        [Test]
        public void RedBookWaitsForTheHandkerchiefAndBothPartsOfTheHaleConversation()
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
        public void RevolverIsGrantedAtTheBarnAndHasRoundsForTheScriptedFight()
        {
            var inventory = new SavingShotInventoryState();

            Assert.That(inventory.TryAcquireRevolver(), Is.True);
            Assert.That(inventory.HasRevolver, Is.True);
            Assert.That(inventory.RoundsRemaining, Is.EqualTo(6));
            Assert.That(inventory.TryAcquireRevolver(), Is.False);
            for (var i = 0; i < 6; i++)
                Assert.That(inventory.TryFire(), Is.True);
            Assert.That(inventory.RoundsRemaining, Is.Zero);
            Assert.That(inventory.TryFire(), Is.False);
        }

        [Test]
        public void RevolverCannotBeGrantedWithAnInvalidRoundCount()
        {
            var inventory = new SavingShotInventoryState();

            Assert.That(inventory.TryAcquireRevolver(0), Is.False);
            Assert.That(inventory.TryAcquireRevolver(-1), Is.False);
            Assert.That(inventory.TryAcquireRevolver(SavingShotInventoryState.BarnRevolverRoundCount + 1), Is.False);
            Assert.That(inventory.HasRevolver, Is.False);
            Assert.That(inventory.RoundsRemaining, Is.Zero);
        }
    }
}
