using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.Saloon;
using ForgottenTrail.Gameplay.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgottenTrail.Tests.Screenplay
{
    public sealed class ScreenplayFidelityTests
    {
        [Test]
        public void SaloonBeatsFollowTheScreenplayFromNoteToWarningToKnifeAndDiary()
        {
            var progressionObject = new GameObject("Saloon screenplay progression test");
            var interactorObject = new GameObject("Saloon screenplay interactor test");
            var trackerObject = new GameObject("Saloon screenplay tracker test");
            var sequenceObject = new GameObject("Saloon screenplay sequence test");
            var journalObject = new GameObject("Saloon screenplay journal test");
            var noteObject = new GameObject("Saloon screenplay note test");
            var warningObject = new GameObject("Saloon screenplay warning test");
            var bloodObject = new GameObject("Saloon screenplay blood test");
            var footprintsObject = new GameObject("Saloon screenplay footprints test");
            var furnitureObject = new GameObject("Saloon screenplay furniture test");
            var knifeObject = new GameObject("Saloon screenplay knife test");
            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                Assert.That(progression.TryComplete(DemoObjective.FindLukeAtGate), Is.True);
                Assert.That(progression.TryComplete(DemoObjective.FollowBootprintsToSaloon), Is.True);

                var journal = journalObject.AddComponent<PlayerJournalComponent>();
                journal.Configure(string.Empty);
                var interactor = interactorObject.AddComponent<PlayerInteractor>();
                interactor.Configure(null, progression, journal);
                var tracker = trackerObject.AddComponent<SaloonInvestigationTracker>();
                tracker.Configure(interactor, progression);
                var sequence = sequenceObject.AddComponent<SaloonApparitionSequence>();
                sequence.Configure(null, progression, journal, tracker, null, null, null, null, null);

                ConfigureClue(noteObject, SaloonApparitionState.NoteInteractionId, false, DemoObjective.InvestigateSaloonClues);
                ConfigureClue(warningObject, SaloonInvestigationState.WarningId, false, DemoObjective.InvestigateSaloonClues);
                ConfigureClue(bloodObject, SaloonInvestigationState.BloodTrailId, false, DemoObjective.InvestigateSaloonClues);
                ConfigureClue(footprintsObject, SaloonInvestigationState.FootprintsId, false, DemoObjective.InvestigateSaloonClues);
                ConfigureClue(furnitureObject, SaloonInvestigationState.BrokenFurnitureId, false, DemoObjective.InvestigateSaloonClues);
                ConfigureClue(knifeObject, "saloon.knife", true, DemoObjective.ExamineSaloonKnife);

                Assert.That(interactor.TryInteract(knifeObject.GetComponent<InteractableClue>(), 1f), Is.False);
                Assert.That(sequence.HasRecordedDiaryEntry, Is.False);
                Assert.That(journal.Entries, Is.Empty);

                Assert.That(interactor.TryInteract(noteObject.GetComponent<InteractableClue>(), 1f), Is.True);
                Assert.That(sequence.HasTriggered, Is.True);
                Assert.That(sequence.HasTriggeredBang, Is.False);
                Assert.That(journal.Entries, Is.Empty);

                Assert.That(interactor.TryInteract(warningObject.GetComponent<InteractableClue>(), 1f), Is.True);
                Assert.That(sequence.HasTriggeredBang, Is.True);
                Assert.That(sequence.HasRecordedDiaryEntry, Is.False);
                Assert.That(journal.Entries, Is.Empty);

                Assert.That(interactor.TryInteract(bloodObject.GetComponent<InteractableClue>(), 1f), Is.True);
                Assert.That(interactor.TryInteract(footprintsObject.GetComponent<InteractableClue>(), 1f), Is.True);
                Assert.That(interactor.TryInteract(furnitureObject.GetComponent<InteractableClue>(), 1f), Is.True);
                Assert.That(tracker.IsComplete, Is.True);
                Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.ExamineSaloonKnife));

                Assert.That(interactor.TryInteract(knifeObject.GetComponent<InteractableClue>(), 1f), Is.True);
                Assert.That(sequence.HasRecordedDiaryEntry, Is.True);
                Assert.That(journal.Entries, Has.Count.EqualTo(1));
                Assert.That(journal.Entries[0], Does.Contain("Encontrei uma faca."));
                Assert.That(progression.CurrentObjective, Is.EqualTo(DemoObjective.ReceiveSheriffMission));
            }
            finally
            {
                Object.DestroyImmediate(knifeObject);
                Object.DestroyImmediate(furnitureObject);
                Object.DestroyImmediate(footprintsObject);
                Object.DestroyImmediate(bloodObject);
                Object.DestroyImmediate(warningObject);
                Object.DestroyImmediate(noteObject);
                Object.DestroyImmediate(journalObject);
                Object.DestroyImmediate(sequenceObject);
                Object.DestroyImmediate(trackerObject);
                Object.DestroyImmediate(interactorObject);
                Object.DestroyImmediate(progressionObject);
            }
        }

        private static void ConfigureClue(GameObject clueObject, string id, bool advancesObjective, DemoObjective objective)
        {
            clueObject.AddComponent<BoxCollider>();
            clueObject.AddComponent<InteractableClue>().Configure(id, "Examinar", id, 2.8f, advancesObjective, objective);
        }

        [Test]
        public void AshCreekSceneHasItsArrivalHorseAndTheScreenplaySetPieces()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                Transform arrivalHorse = null;
                Transform falseEliasTorso = null;
                Transform chesterRevolver = null;
                Transform chesterNoose = null;
                Transform lukeReachingArm = null;
                ScreenplayOpeningLine openingLine = null;

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == "Player — Investigator")
                    {
                        openingLine = root.GetComponent<ScreenplayOpeningLine>();
                        arrivalHorse = root.transform.Find("Arrival horse — Ash Creek mount");
                    }

                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == "False Elias — reclined torso")
                            falseEliasTorso = child;
                        if (child.name == "Chester — one-round revolver")
                            chesterRevolver = child;
                        if (child.name == "Chester — hanging noose from workshop beam")
                            chesterNoose = child;
                        if (child.name == "Luke — reaching arm pivot")
                            lukeReachingArm = child;
                    }
                }

                Assert.That(openingLine, Is.Not.Null);
                Assert.That(openingLine.IsArrivalConfigured, Is.True);
                Assert.That(arrivalHorse, Is.Not.Null);
                Assert.That(falseEliasTorso, Is.Not.Null);
                Assert.That(Mathf.Abs(falseEliasTorso.localEulerAngles.z), Is.GreaterThan(20f));
                Assert.That(chesterRevolver, Is.Not.Null);
                Assert.That(chesterNoose, Is.Not.Null);
                Assert.That(lukeReachingArm, Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void AshCreekVolumeProfilePersistsItsThreeConfiguredOverrides()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Rendering/AshCreekVolumeProfile.asset");

            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.Has<ColorAdjustments>(), Is.True);
            Assert.That(profile.Has<Tonemapping>(), Is.True);
            Assert.That(profile.Has<Vignette>(), Is.True);

            Assert.That(profile.TryGet(out ColorAdjustments color), Is.True);
            Assert.That(color.postExposure.overrideState, Is.True);
            Assert.That(color.contrast.overrideState, Is.True);
            Assert.That(color.saturation.overrideState, Is.True);
            Assert.That(profile.TryGet(out Tonemapping tone), Is.True);
            Assert.That(tone.mode.overrideState, Is.True);
            Assert.That(profile.TryGet(out Vignette vignette), Is.True);
            Assert.That(vignette.intensity.overrideState, Is.True);
            Assert.That(vignette.smoothness.overrideState, Is.True);
        }
    }
}
