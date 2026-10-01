using ForgottenTrail.Gameplay.Progression;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Progression
{
    public sealed class DemoProgressionTests
    {
        [Test]
        public void StartsAtSaloonInvestigation()
        {
            var progression = new DemoProgression();

            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.InvestigateSaloonClues));
            Assert.That(progression.IsComplete, Is.False);
        }

        [Test]
        public void CannotSkipAheadToTheBarn()
        {
            var progression = new DemoProgression();

            var advanced = progression.TryComplete(DemoObjective.ConfrontCreatureInBarn);

            Assert.That(advanced, Is.False);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.InvestigateSaloonClues));
        }

        [Test]
        public void CompletingCurrentObjectiveAdvancesExactlyOneBeat()
        {
            var progression = new DemoProgression();

            var advanced = progression.TryComplete(DemoObjective.InvestigateSaloonClues);

            Assert.That(advanced, Is.True);
            Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.DiscoverChurchTruth));
        }

        [Test]
        public void ReachingTheForestCompletesTheDemo()
        {
            var progression = new DemoProgression();
            progression.TryComplete(DemoObjective.InvestigateSaloonClues);
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
