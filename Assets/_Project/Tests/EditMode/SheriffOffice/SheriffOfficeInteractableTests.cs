using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.SheriffOffice
{
    public sealed class SheriffOfficeInteractableTests
    {
        [Test]
        public void HaleDialogueBookAndEscapeFollowTheScreenplayWithoutGivingTheRevolverEarly()
        {
            var interactorObject = new GameObject("Test player interactor");
            var journalObject = new GameObject("Test player journal");
            var progressionObject = new GameObject("Test demo progression");
            var trackerObject = new GameObject("Test office tracker");
            var haleObject = new GameObject("Test Sheriff Hale");
            var bookObject = new GameObject("Test Red Book");
            var escapeObject = new GameObject("Test office escape");
            var escapeExitObject = new GameObject("Test office exit");

            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var interactor = interactorObject.AddComponent<PlayerInteractor>();
                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                journal.Configure("TESTE — ABERTURA");
                interactor.Configure(null, progression, journal);
                var tracker = trackerObject.AddComponent<SheriffOfficeInvestigationTracker>();
                var escape = escapeObject.AddComponent<SheriffOfficeEscapeSequence>();
                tracker.Configure(interactor, progression, escape);
                var hale = haleObject.AddComponent<SheriffHaleEncounterInteractable>();
                hale.Configure(tracker);
                var book = bookObject.AddComponent<RedBookInteractable>();
                book.Configure(tracker);
                var escapeExit = escapeExitObject.AddComponent<SheriffOfficeEscapeInteractable>();
                escapeExit.Configure(escape);

                Assert.That(hale.TryInteract(1f, out _, out _), Is.False);
                Assert.That(tracker.State.HasMetHale, Is.False);
                Assert.That(book.TryInteract(1f, out _, out _), Is.False);
                tracker.HandleInteractionCompleted(SheriffOfficeInvestigationState.RedBookInteractionId);
                Assert.That(tracker.State.HasFoundRedBook, Is.False);

                tracker.HandleInteractionCompleted(SheriffOfficeInvestigationState.LaylaEvidenceInteractionId);
                Assert.That(interactor.TryInteract(hale, 1f), Is.True);
                Assert.That(interactor.LastInteractionText, Is.EqualTo("Protagonista: \"O sino ainda toca pelos vivos. Elias me mandou.\"\nXerife Hale: \"Elias morreu há três dias.\""));
                Assert.That(journal.Entries, Has.Count.EqualTo(1));
                Assert.That(tracker.State.HasMetHale, Is.True);
                Assert.That(hale.InteractionId, Is.EqualTo(SheriffOfficeInvestigationState.HaleAccountInteractionId));
                Assert.That(interactor.TryInteract(hale, 1f), Is.True);
                Assert.That(interactor.LastInteractionText, Is.EqualTo("Protagonista: \"Como assim? Falei com ele no altar! E Layla? O que aconteceu com ela?\"\nXerife Hale: \"Todo mundo veio bater na minha porta pedindo socorro e abrigo. Quando o mal subiu da mina, adivinha quem teve que fechar o cadeado? Eu tranquei o celeiro para impedir que aquilo saísse, não para proteger quem ficou dentro. Layla achou que podia salvar todo mundo. Tome a chave. Suba, leia o livro vermelho e assuma a responsabilidade se abrir aquela porta.\""));
                Assert.That(tracker.State.HasHeardHaleAccount, Is.True);

                Assert.That(interactor.TryInteract(book, 1f), Is.True);
                Assert.That(interactor.LastInteractionText, Is.EqualTo("Padre Elias — Falecido.\nLAYLA — Transferida para o celeiro. Condição: consciente.\nNota: ‘Não deixem que ela desça novamente à mina.’"));
                Assert.That(tracker.State.HasFoundRedBook, Is.True);
                Assert.That(escape.CurrentMessage, Does.Contain("O sino ainda toca pelos vivos..."));
                Assert.That(journal.Entries, Has.Count.EqualTo(1));
                Assert.That(escape.IsActive, Is.True);
                Assert.That(escapeExit.TryInteract(1f, out var blockedExit, out _), Is.True);
                Assert.That(blockedExit, Does.Contain("Jack"));
                Assert.That(escape.CommandJackToStayQuiet(), Is.True);
                Assert.That(escapeExit.TryInteract(1f, out blockedExit, out _), Is.True);
                Assert.That(blockedExit, Does.Contain("Agache"));
                Assert.That(tracker.State.HasEscaped, Is.False);
                Assert.That(tracker.State.IsComplete, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(escapeExitObject);
                Object.DestroyImmediate(escapeObject);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(bookObject);
                Object.DestroyImmediate(haleObject);
                Object.DestroyImmediate(trackerObject);
                Object.DestroyImmediate(progressionObject);
                Object.DestroyImmediate(interactorObject);
            }
        }
    }
}
