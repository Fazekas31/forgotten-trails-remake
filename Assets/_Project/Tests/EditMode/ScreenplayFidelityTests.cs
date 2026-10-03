using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.Saloon;
using ForgottenTrail.Gameplay.World;
using ForgottenTrail.Gameplay.UI;
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
                Assert.That(interactor.TryInteract(noteObject.GetComponent<InteractableClue>(), 1f), Is.False);
                Assert.That(tracker.HasReadNote, Is.False);
                Assert.That(sequence.HasTriggered, Is.False);
                Assert.That(sequence.HasRecordedDiaryEntry, Is.False);
                Assert.That(journal.Entries, Is.Empty);

                Assert.That(progression.TryComplete(DemoObjective.FindLukeAtGate), Is.True);
                Assert.That(progression.TryComplete(DemoObjective.FollowBootprintsToSaloon), Is.True);
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
        public void AshCreekMainStreetAndBarnFollowTheMapWestToEast()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                Transform player = null;
                Transform mainStreet = null;
                Transform barnFloor = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == "Player — Investigator") player = root.transform;
                    if (root.name == "Main street") mainStreet = root.transform;
                    if (root.name == "Barn — floor") barnFloor = root.transform;
                }

                Assert.That(player, Is.Not.Null);
                Assert.That(mainStreet, Is.Not.Null);
                Assert.That(barnFloor, Is.Not.Null);
                Assert.That(mainStreet.position.x, Is.GreaterThan(20f));
                Assert.That(Mathf.Abs(mainStreet.position.z), Is.LessThan(0.01f));
                Assert.That(player.position.x, Is.LessThan(mainStreet.position.x));
                Assert.That(barnFloor.position.x, Is.GreaterThan(mainStreet.position.x + 30f));
                Assert.That(Mathf.Abs(barnFloor.position.z), Is.LessThan(0.01f));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void AshCreekSceneIsBrightAndReadableWithoutOversizedDebugText()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                EditorSceneManager.SetActiveScene(scene);
                Transform moon = null;
                var oldBackEntranceLabelFound = false;
                var warningWritingSize = float.PositiveInfinity;
                var brightestStreetPractical = 0f;
                var mainSignSize = 0f;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var light in root.GetComponentsInChildren<Light>(true))
                    {
                        if (light.name == "Moonlight — cool key") moon = light.transform;
                        if (light.name.StartsWith("Warm practical", System.StringComparison.Ordinal))
                            brightestStreetPractical = Mathf.Max(brightestStreetPractical, light.intensity);
                    }

                    foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
                    {
                        if (text.text.Contains("ENTRADA DOS FUNDOS")) oldBackEntranceLabelFound = true;
                        if (text.transform.root.name == "Clue — warning on wall")
                            warningWritingSize = Mathf.Min(warningWritingSize, text.characterSize);
                        if (text.text == "ASH CREEK")
                            mainSignSize = text.characterSize;
                    }
                }

                var volume = Object.FindFirstObjectByType<Volume>();
                var amberGlow = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Lamp_glass_-_amber.mat");
                Assert.That(amberGlow, Is.Not.Null);
                Assert.That(amberGlow.IsKeywordEnabled("_EMISSION"), Is.True, "Warm amber accents should retain their emission.");
                Assert.That(oldBackEntranceLabelFound, Is.False, "Debug navigation text must not be baked into the world.");
                Assert.That(warningWritingSize, Is.InRange(0.04f, 0.045f), "The wall clue should retain its earlier in-world scale.");
                Assert.That(mainSignSize, Is.InRange(0.15f, 0.17f), "In-world signs should keep their earlier restrained scale.");
                Assert.That(RenderSettings.ambientIntensity, Is.InRange(0.95f, 1.05f));
                Assert.That(moon, Is.Not.Null);
                Assert.That(moon.GetComponent<Light>().intensity, Is.InRange(0.54f, 0.62f));
                Assert.That(brightestStreetPractical, Is.InRange(0.8f, 0.95f));
                Assert.That(volume, Is.Not.Null);
                Assert.That(volume.sharedProfile.TryGet(out ColorAdjustments color), Is.True);
                Assert.That(color.postExposure.value, Is.InRange(0.30f, 0.40f));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCase(1080, 1f)]
        [TestCase(1440, 1.33f)]
        [TestCase(540, 0.85f)]
        public void HudTextScaleAdaptsToDisplayHeight(int height, float expectedScale)
        {
            Assert.That(HudTextScale.ScaleForHeight(height), Is.EqualTo(expectedScale).Within(0.01f));
        }

        [TestCase(960, 540)]
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void ObjectiveAndCreatureAlertPanelsDoNotOverlap(int width, int height)
        {
            var objective = HudTextScale.ObjectivePanelRect(width, height);
            var alert = HudTextScale.AlertPanelRect(width, height);
            Assert.That(alert.Overlaps(objective), Is.False);
        }

        [Test]
        public void SubtitleAndHudFontsStayLargeOnStandardAndHighResolutionDisplays()
        {
            Assert.That(HudTextScale.FontSize(30, 1080), Is.EqualTo(30));
            Assert.That(HudTextScale.FontSize(24, 1440), Is.EqualTo(32));
        }

        [Test]
        public void LukeIsVisibleBeforeTheEntranceGateAndHisLanternLightsHim()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                EditorSceneManager.SetActiveScene(scene);
                Transform player = null;
                Transform luke = null;
                Transform gate = null;
                Transform lamp = null;
                Light lanternLight = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == "Player — Investigator") player = root.transform;
                    if (root.name == "Luke — wounded at the gate") luke = root.transform;
                    if (root.name == "Entrance gate — lintel") gate = root.transform;
                    if (root.name == "Luke's oil lantern")
                    {
                        var light = root.GetComponentInChildren<Light>(true);
                        if (light != null)
                        {
                            lamp = light.transform;
                            lanternLight = light;
                        }
                    }
                }

                Assert.That(player, Is.Not.Null);
                Assert.That(luke, Is.Not.Null);
                Assert.That(gate, Is.Not.Null);
                Assert.That(lamp, Is.Not.Null);
                Assert.That(lanternLight, Is.Not.Null);
                Assert.That(lanternLight.intensity, Is.GreaterThanOrEqualTo(7f));
                Assert.That(lanternLight.range, Is.GreaterThanOrEqualTo(16f));
                Assert.That(lanternLight.spotAngle, Is.GreaterThanOrEqualTo(60f));
                Assert.That(Vector3.Distance(player.position, luke.position), Is.LessThanOrEqualTo(8f),
                    "Luke should be visible from the start of the approach.");
                Assert.That(luke.position.x, Is.LessThan(gate.position.x - 2f),
                    "Luke should be on the approach side of the entrance gate.");
                var targetDirection = (luke.position + Vector3.up * 0.38f - lamp.position).normalized;
                Assert.That(Vector3.Dot(lamp.forward, targetDirection), Is.GreaterThan(0.98f),
                    "The warm lantern beam should point at Luke.");
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
