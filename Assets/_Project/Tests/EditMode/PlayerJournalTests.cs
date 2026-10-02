using ForgottenTrail.Gameplay.Journal;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Journal
{
    public sealed class PlayerJournalTests
    {
        [Test]
        public void RecordingANewClueMakesItAvailableInTheDiary()
        {
            var journal = new PlayerJournal();

            journal.Record("Pegadas saem do portão e seguem para o poço.");
            journal.Record("Luke entregou o lampião e alertou sobre os sons.");

            Assert.That(journal.Entries.Count, Is.EqualTo(2));
            Assert.That(journal.Entries[0], Is.EqualTo("Pegadas saem do portão e seguem para o poço."));
            Assert.That(journal.Entries[1], Is.EqualTo("Luke entregou o lampião e alertou sobre os sons."));
        }

        [Test]
        public void RecordingTheSameClueTwiceDoesNotCreateDuplicatePages()
        {
            var journal = new PlayerJournal();
            const string entry = "As marcas seguem para o saloon.";

            journal.Record(entry);
            journal.Record(entry);

            Assert.That(journal.Entries.Count, Is.EqualTo(1));
        }

        [Test]
        public void PlayerCanOpenAndCloseTheDiary()
        {
            var journal = new PlayerJournal();

            journal.ToggleOpen();
            Assert.That(journal.IsOpen, Is.True);

            journal.ToggleOpen();
            Assert.That(journal.IsOpen, Is.False);
        }
    }
}
