using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.SheriffOffice
{
    public sealed class SheriffOfficeInvestigationTrackerTests
    {
        [Test]
        public void CompletedOfficeInvestigationAdvancesWhenCampaignReachesTheSheriffBeat()
        {
            var interactorObject = new GameObject("Test player interactor");
            var progressionObject = new GameObject("Test demo progression");
            var journalObject = new GameObject("Test player journal");
            var escapeObject = new GameObject("Test sheriff office escape");
            var trackerObject = new GameObject("Test sheriff office investigation");

            try
            {
                var interactor = interactorObject.AddComponent<PlayerInteractor>();
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                var escape = escapeObject.AddComponent<SheriffOfficeEscapeSequence>();
                var tracker = trackerObject.AddComponent<SheriffOfficeInvestigationTracker>();
                tracker.Configure(interactor, progression, escape);

                tracker.HandleInteractionCompleted(SheriffOfficeInvestigationState.LaylaEvidenceInteractionId);
                tracker.HandleInteractionCompleted(SheriffOfficeInvestigationState.HaleInteractionId);
                tracker.HandleInteractionCompleted(SheriffOfficeInvestigationState.HaleAccountInteractionId);
                tracker.HandleInteractionCompleted(SheriffOfficeInvestigationState.RedBookInteractionId);
                escape.State.Begin();
                escape.State.TryEscape(true, false, true);
                tracker.HandleInteractionCompleted(SheriffOfficeInvestigationState.QuietEscapeInteractionId);

                Assert.That(tracker.State.IsComplete, Is.True);
                Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.FindLukeAtGate));

                progression.TryComplete(DemoObjective.FindLukeAtGate);
                progression.TryComplete(DemoObjective.FollowBootprintsToSaloon);
                progression.TryComplete(DemoObjective.InvestigateSaloonClues);
                progression.TryComplete(DemoObjective.ExamineSaloonKnife);
                progression.TryComplete(DemoObjective.DiscoverChurchTruth);
                progression.TryComplete(DemoObjective.FollowChesterAndJack);

                Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.ReturnToChurch));
                Assert.That(journal.Entries, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(trackerObject);
                Object.DestroyImmediate(escapeObject);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(progressionObject);
                Object.DestroyImmediate(interactorObject);
            }
        }
    }
}
