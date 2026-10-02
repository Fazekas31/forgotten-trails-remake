using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.Alley
{
    public sealed class ChesterEncounterInteractableTests
    {
        [Test]
        public void ChesterConversationMatchesTheScreenplayBeforeTheGateKeyIsGranted()
        {
            var interactorObject = new GameObject("Test player interactor");
            var progressionObject = new GameObject("Test demo progression");
            var journalObject = new GameObject("Test player journal");
            var trackerObject = new GameObject("Test Chester route tracker");
            var chesterObject = new GameObject("Test Chester");

            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                journal.Configure(string.Empty);
                var interactor = interactorObject.AddComponent<PlayerInteractor>();
                interactor.Configure(null, progression, journal);
                var tracker = trackerObject.AddComponent<ChesterJackRouteTracker>();
                tracker.Configure(interactor, progression, journal, null, null);
                tracker.HandleInteractionCompleted(ChesterJackRouteState.TrailInteractionId);
                var chester = chesterObject.AddComponent<ChesterEncounterInteractable>();
                chester.Configure(tracker, null, null);

                var expectedDialogue = new[]
                {
                    "Chester: \"Você é real... ou é outra daquelas vozes da montanha zombando de mim? Diga que você é de carne e osso!\"\nProtagonista: \"Sou real. Preciso passar para a delegacia. Procuro por Layla.\"",
                    "Chester: \"Layla foi pro celeiro tentar curar quem já estava condenado... Ninguém volta de lá! E eu não vou esperar aquelas coisas quebrarem essa porta. O silêncio dessa cidade tá comendo minha cabeça há dias.\"\nProtagonista: \"Guarde essa arma, Chester. Me ajude com a tranca e saia comigo.\"",
                    "Chester: \"Não tem mais estrada pra mim. Mas o Jack... ele não tem culpa da nossa ruína! Ele é forte, tem pernas e dentes. Mas fiquei sem comida... e se a barriga dele roncar, ele vai chorar no escuro. E se ele chorar, aquelas coisas descem e rasgam ele em pedaços! Eu não posso ver isso acontecer!\"",
                    "Chester: \"Prometa pra mim! Jure por quem você veio buscar! Cuide dele. Ache comida nos armários, divida seus mantimentos... mas não deixe meu garoto passar fome nem ganir na noite. Tire esse peso de mim... por favor. Leve o Jack!\"\nProtagonista: \"Eu prometo, Chester. Eu cuido dele.\"\nChester: \"Obrigado, forasteiro... Agora passe e não olhe pra trás.\""
                };

                for (var i = 0; i < expectedDialogue.Length; i++)
                {
                    Assert.That(interactor.TryInteract(chester, 1f), Is.True);
                    Assert.That(interactor.LastInteractionText, Is.EqualTo(expectedDialogue[i]));
                    Assert.That(tracker.State.HasMetChester, Is.EqualTo(i == expectedDialogue.Length - 1));
                }

                Assert.That(journal.Entries, Is.Empty);
                Assert.That(chester.TryInteract(1f, out _, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(chesterObject);
                Object.DestroyImmediate(trackerObject);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(progressionObject);
                Object.DestroyImmediate(interactorObject);
            }
        }
    }
}
