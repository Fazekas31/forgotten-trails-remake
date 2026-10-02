using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Lantern;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.Lantern
{
    public sealed class HandLanternStateTests
    {
        [Test]
        public void LukeCanHandTheLanternToThePlayerWithinReach()
        {
            var lantern = new HandLanternState(2.8f);

            var pickedUp = lantern.TryPickUp(2.8f);

            Assert.That(pickedUp, Is.True);
            Assert.That(lantern.IsHeld, Is.True);
            Assert.That(lantern.IsLit, Is.True);
        }

        [Test]
        public void PlayerCannotTakeTheLanternFromBeyondReach()
        {
            var lantern = new HandLanternState(2.8f);

            var pickedUp = lantern.TryPickUp(2.81f);

            Assert.That(pickedUp, Is.False);
            Assert.That(lantern.IsHeld, Is.False);
            Assert.That(lantern.IsLit, Is.False);
        }

        [Test]
        public void PlayerCanToggleTheOilLampAfterReceivingIt()
        {
            var lantern = new HandLanternState(2.8f);

            var toggledBeforePickup = lantern.TryToggle();
            lantern.TryPickUp(1.0f);
            var turnedOff = lantern.TryToggle();
            var isLitAfterTurningOff = lantern.IsLit;
            var turnedOn = lantern.TryToggle();

            Assert.That(toggledBeforePickup, Is.False);
            Assert.That(turnedOff, Is.True);
            Assert.That(isLitAfterTurningOff, Is.False);
            Assert.That(turnedOn, Is.True);
            Assert.That(lantern.IsLit, Is.True);
        }

        [Test]
        public void ReceivingTheLanternKeepsItsGateObjectiveContract()
        {
            var pickupObject = new GameObject("lantern interaction test");
            try
            {
                var pickup = pickupObject.AddComponent<HandLanternPickup>();
                pickup.Configure(2.8f, null, null, null, null, null, null);

                var interacted = pickup.TryInteract(1f, out _, out var completedObjective);

                Assert.That(interacted, Is.True);
                Assert.That(completedObjective, Is.EqualTo(DemoObjective.FindLukeAtGate));
            }
            finally
            {
                Object.DestroyImmediate(pickupObject);
            }
        }

        [Test]
        public void LukeDialogueAndFirstDiaryEntryMatchTheScreenplay()
        {
            var interactorObject = new GameObject("Test player interactor");
            var progressionObject = new GameObject("Test demo progression");
            var journalObject = new GameObject("Test player journal");
            var pickupObject = new GameObject("Test oil lantern");

            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                journal.Configure(string.Empty);
                var interactor = interactorObject.AddComponent<PlayerInteractor>();
                interactor.Configure(null, progression, journal);
                var pickup = pickupObject.AddComponent<HandLanternPickup>();
                pickup.Configure(2.8f, null, null, null, null, null, null);

                Assert.That(interactor.TryInteract(pickup, 1f), Is.True);
                Assert.That(interactor.LastInteractionText, Is.EqualTo("Protagonista: \"Você precisa de ajuda. Deixe-me levantá-lo. Procuro abrigo e uma mulher chamada Layla.\"\nLuke: \"Não há camas limpas em Ash Creek, forasteiro. Nem descanso... Se quiser ver o amanhã, fique com isto.\"\nProtagonista: \"O que aconteceu com este lugar?\"\nLuke: \"Eu terminei minha marcha... Agora você vai carregar a escuridão por nós dois. Eles escutam tudo. Não faça barulho.\""));
                Assert.That(journal.Entries, Has.Some.EqualTo("Encontrei um homem ferido no portão de entrada. Ele me entregou seu lampião e disse que algo na cidade escuta tudo. Vim buscar Layla, mas parece que Ash Creek já começou a descarregar seu fardo em mim."));
            }
            finally
            {
                Object.DestroyImmediate(pickupObject);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(progressionObject);
                Object.DestroyImmediate(interactorObject);
            }
        }
    }
}
