using ForgottenTrail.Gameplay.Saloon;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Saloon
{
    public sealed class SaloonInvestigationStateTests
    {
        [Test]
        public void InvestigationCompletesOnlyAfterNoteBloodFootprintsAndBrokenFurnitureAreRecorded()
        {
            var state = new SaloonInvestigationState();

            Assert.That(state.TryRecord("saloon.torn-note"), Is.True);
            Assert.That(state.TryRecord("saloon.blood-trail"), Is.True);
            Assert.That(state.TryRecord("saloon.footprints"), Is.True);
            Assert.That(state.IsComplete, Is.False);

            Assert.That(state.TryRecord("saloon.broken-furniture"), Is.True);

            Assert.That(state.HasReadNote, Is.True);
            Assert.That(state.IsComplete, Is.True);
        }

        [Test]
        public void UnknownAndRepeatedCluesCannotStandInForMissingEvidence()
        {
            var state = new SaloonInvestigationState();

            Assert.That(state.TryRecord("saloon.knife"), Is.False);
            Assert.That(state.TryRecord("saloon.blood-trail"), Is.True);
            Assert.That(state.TryRecord("saloon.blood-trail"), Is.False);
            Assert.That(state.TryRecord("saloon.footprints"), Is.True);
            Assert.That(state.TryRecord("saloon.torn-note"), Is.True);

            Assert.That(state.IsComplete, Is.False);
        }
    }
}
