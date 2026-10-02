using ForgottenTrail.Gameplay.Progression;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Progression
{
    public sealed class DemoProgressionTests
    {
        [Test]
        public void StartsByAskingThePlayerToFindLukeAtTheGate()
        {
            var progression = new DemoProgression();

            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.FindLukeAtGate));
            Assert.That(progression.IsComplete, Is.False);
        }

        [Test]
        public void FindingLukeAdvancesTheObjectiveToFollowingTheBootprints()
        {
            var progression = new DemoProgression();

            var advanced = progression.TryComplete(DemoObjective.FindLukeAtGate);

            Assert.That(advanced, Is.True);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.FollowBootprintsToSaloon));
        }

        [Test]
        public void CannotSkipAheadToTheBarn()
        {
            var progression = new DemoProgression();

            var advanced = progression.TryComplete(DemoObjective.ConfrontCreatureInBarn);

            Assert.That(advanced, Is.False);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.FindLukeAtGate));
        }

        [Test]
        public void CompletingCurrentObjectiveAdvancesExactlyOneBeat()
        {
            var progression = new DemoProgression();

            var advanced = progression.TryComplete(DemoObjective.FindLukeAtGate);

            Assert.That(advanced, Is.True);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.FollowBootprintsToSaloon));
        }

        [Test]
        public void SaloonInvestigationRequiresTheKnifeBeforeTheChurchObjective()
        {
            var progression = new DemoProgression();
            progression.TryComplete(DemoObjective.FindLukeAtGate);
            progression.TryComplete(DemoObjective.FollowBootprintsToSaloon);

            var foundNote = progression.TryComplete(DemoObjective.InvestigateSaloonClues);
            var skippedKnife = progression.TryComplete(DemoObjective.DiscoverChurchTruth);

            Assert.That(foundNote, Is.True);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.ExamineSaloonKnife));
            Assert.That(skippedKnife, Is.False);

            var examinedKnife = progression.TryComplete(DemoObjective.ExamineSaloonKnife);

            Assert.That(examinedKnife, Is.True);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.DiscoverChurchTruth));
        }

        [Test]
        public void ReachingTheForestCompletesTheDemo()
        {
            var progression = new DemoProgression();
            progression.TryComplete(DemoObjective.FindLukeAtGate);
            progression.TryComplete(DemoObjective.FollowBootprintsToSaloon);
            progression.TryComplete(DemoObjective.InvestigateSaloonClues);
            progression.TryComplete(DemoObjective.ExamineSaloonKnife);
            progression.TryComplete(DemoObjective.DiscoverChurchTruth);
            progression.TryComplete(DemoObjective.SearchSheriffOffice);
            progression.TryComplete(DemoObjective.ReturnToChurch);
            progression.TryComplete(DemoObjective.ConfrontCreatureInBarn);

            var advanced = progression.TryComplete(DemoObjective.ReachForest);

            Assert.That(advanced, Is.True);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.Complete));
            Assert.That(progression.IsComplete, Is.True);
        }
    }
}
