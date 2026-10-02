using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.Alley
{
    public sealed class ChesterJackRouteTrackerTests
    {
        [Test]
        public void RouteCompletedEarlyAdvancesWhenCampaignReachesChesterBeat()
        {
            var interactorObject = new GameObject("Test player interactor");
            var progressionObject = new GameObject("Test demo progression");
            var journalObject = new GameObject("Test player journal");
            var trackerObject = new GameObject("Test Chester and Jack route");

            try
            {
                var interactor = interactorObject.AddComponent<PlayerInteractor>();
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                journal.Configure(string.Empty);
                var tracker = trackerObject.AddComponent<ChesterJackRouteTracker>();
                tracker.Configure(interactor, progression, journal, null, null);

                var routeInteractions = new[]
                {
                    ChesterJackRouteState.TrailInteractionId,
                    ChesterJackRouteState.ChesterInteractionId,
                    ChesterJackRouteState.GateInteractionId,
                    ChesterJackRouteState.JackRescueInteractionId,
                    ChesterJackRouteState.JackCalmInteractionId,
                    ChesterJackRouteState.SheriffOfficeExitInteractionId
                };
                foreach (var interactionId in routeInteractions)
                    tracker.HandleInteractionCompleted(interactionId);

                Assert.That(tracker.IsComplete, Is.True);
                Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.FindLukeAtGate));
                Assert.That(journal.Entries, Has.Some.EqualTo("Chester não suportou o horror de Ash Creek e tirou a própria vida. Antes do fim, me confiou Jack, seu cão. Ele me implorou para mantê-lo alimentado para que a fome não o faça latir no escuro. O peso da vida desse animal agora está nos meus ombros."));

                progression.TryComplete(DemoObjective.FindLukeAtGate);
                progression.TryComplete(DemoObjective.FollowBootprintsToSaloon);
                progression.TryComplete(DemoObjective.InvestigateSaloonClues);
                progression.TryComplete(DemoObjective.ExamineSaloonKnife);
                progression.TryComplete(DemoObjective.ReceiveSheriffMission);

                Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.SearchSheriffOffice));
            }
            finally
            {
                Object.DestroyImmediate(trackerObject);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(progressionObject);
                Object.DestroyImmediate(interactorObject);
            }
        }
    }
}
