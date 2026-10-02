using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Church;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.Church
{
    public sealed class ChurchInvestigationStateTests
    {
        [Test]
        public void FalseEliasDeliversTheSheriffMissionInScreenplayOrder()
        {
            var progressionObject = new GameObject("Progression");
            var playerObject = new GameObject("Interactor");
            var journalObject = new GameObject("Journal");
            var priestObject = new GameObject("False Elias");

            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var interactor = playerObject.AddComponent<PlayerInteractor>();
                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                journal.Configure(string.Empty);
                var badgeInventory = playerObject.AddComponent<SheriffBadgeInventory>();
                interactor.Configure(null, progression, journal);
                var priest = priestObject.AddComponent<ChurchFalseEliasEncounter>();
                priest.Configure(progression, badgeInventory);

                Assert.That(interactor.TryInteract(priest, 1f), Is.False, "The church mission follows the saloon knife.");
                AdvanceToChurchMission(progression);

                var dialogue = new[]
                {
                    "Padre Elias: \"Pare onde está. Deixe-me ver seus olhos... Você ainda está consciente.\"",
                    "Protagonista: \"Procuro por Layla. Ela esteve aqui?\"",
                    "Padre Elias: \"Layla... Ela tentou aliviar as dores desta cidade, cuidou dos doentes com suas próprias mãos. Mas o fardo de Ash Creek quebra as costas dos inocentes.\"",
                    "Protagonista: \"Onde ela está agora?\"",
                    "Padre Elias: \"Se quiser respostas, tire esse peso dos meus ombros primeiro. O xerife Hale levou os sobreviventes e trancou o celeiro ao norte. Vá até a delegacia, pegue a chave com ele e traga o registro vermelho de evacuação. Faça o que minhas mãos fracas não podem mais fazer.\"",
                    "Padre Elias: \"Mostre isto a Hale e diga: 'O sino ainda toca pelos vivos'. Ele entenderá. E lembre-se: se ouvir alguém chamando seu nome na neblina, não responda.\""
                };

                for (var i = 0; i < dialogue.Length; i++)
                {
                    Assert.That(interactor.TryInteract(priest, 1f), Is.True);
                    Assert.That(interactor.LastInteractionText, Is.EqualTo(dialogue[i]));
                    Assert.That(progression.CurrentObjective, Is.EqualTo(i == dialogue.Length - 1
                        ? DemoObjective.FollowChesterAndJack
                        : DemoObjective.ReceiveSheriffMission));
                    Assert.That(badgeInventory.HasBadge, Is.EqualTo(i == dialogue.Length - 1),
                        "Elias hands over the badge with the final screenplay line.");
                }

                Assert.That(journal.Entries, Is.Empty, "The screenplay does not add an early church diary entry.");
                Assert.That(priest.TryInteract(1f, out _, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(priestObject);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(progressionObject);
            }
        }

        [Test]
        public void ReturningToChurchPlaysTheWarningVoicesAndActOneDiaryVerbatim()
        {
            var progressionObject = new GameObject("Progression");
            var playerObject = new GameObject("Interactor");
            var journalObject = new GameObject("Journal");
            var churchRoot = new GameObject("Church tracker");
            var returnRoot = new GameObject("Return reveal");
            returnRoot.transform.SetParent(churchRoot.transform, false);

            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var interactor = playerObject.AddComponent<PlayerInteractor>();
                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                journal.Configure(string.Empty);
                interactor.Configure(null, progression, journal);
                AdvanceToReturnToChurch(progression);

                var reveal = returnRoot.AddComponent<ChurchReturnRevealInteractable>();
                reveal.Configure(progression);
                var tracker = churchRoot.AddComponent<ChurchInvestigationTracker>();
                tracker.Configure(interactor, progression, null, returnRoot);

                var sequence = new[]
                {
                    "As coisas da mina aprenderam nossas vozes. Elas não enxergam, mas imitam os mortos para nos atrair. Não respondam quando chamarem seus nomes. Não abram o celeiro. Perdoe-me.",
                    "Voz do Falso Padre: \"Você pegou a chave, forasteiro? Tire o peso de nós... Abra o celeiro...\"",
                    "Voz de Layla: \"Tire esse peso de mim... Estou no celeiro...\"",
                    "O padre que me deu essa missão já estava morto antes de meus pés tocarem esta cidade. A coisa que roubou sua voz me usou para conseguir a chave do celeiro. O xerife diz que trancou o mal lá dentro; o registro diz que Layla está lá. Jack rosna para a estrada da colina. Não sei se Layla ainda é humana, mas carregarei esse fardo até a última porta."
                };

                for (var i = 0; i < sequence.Length; i++)
                {
                    Assert.That(interactor.TryInteract(reveal, 1f), Is.True);
                    Assert.That(interactor.LastInteractionText, Is.EqualTo(sequence[i]));
                }

                Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.ConfrontCreatureInBarn));
                Assert.That(journal.Entries, Is.EqualTo(new[] { sequence[sequence.Length - 1] }));
                Assert.That(interactor.TryInteract(reveal, 1f), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(churchRoot);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(progressionObject);
            }
        }

        private static void AdvanceToChurchMission(DemoProgressionComponent progression)
        {
            progression.TryComplete(DemoObjective.FindLukeAtGate);
            progression.TryComplete(DemoObjective.FollowBootprintsToSaloon);
            progression.TryComplete(DemoObjective.InvestigateSaloonClues);
            progression.TryComplete(DemoObjective.ExamineSaloonKnife);
        }

        private static void AdvanceToReturnToChurch(DemoProgressionComponent progression)
        {
            AdvanceToChurchMission(progression);
            progression.TryComplete(DemoObjective.ReceiveSheriffMission);
            progression.TryComplete(DemoObjective.FollowChesterAndJack);
            progression.TryComplete(DemoObjective.SearchSheriffOffice);
        }
    }
}
