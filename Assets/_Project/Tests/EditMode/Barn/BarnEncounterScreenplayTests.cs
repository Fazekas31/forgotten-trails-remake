using System;
using System.Reflection;
using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Barn;
using ForgottenTrail.Gameplay.Combat;
using ForgottenTrail.Gameplay.Progression;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.Barn
{
    public sealed class BarnEncounterScreenplayTests
    {
        [Test]
        public void CompletingProgressionDoesNotInterruptTheFinalTitleCard()
        {
            var sequenceObject = new GameObject("Barn ending sequence test");
            var progressionObject = new GameObject("Demo progression test");
            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var sequence = sequenceObject.AddComponent<BarnEncounterSequence>();
                sequence.Configure(
                    progression, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null, null, null);

                var stageField = typeof(BarnEncounterSequence).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(stageField, Is.Not.Null);
                stageField.SetValue(sequence, Enum.Parse(stageField.FieldType, "Ending"));

                foreach (var objective in new[]
                {
                    DemoObjective.FindLukeAtGate,
                    DemoObjective.FollowBootprintsToSaloon,
                    DemoObjective.InvestigateSaloonClues,
                    DemoObjective.ExamineSaloonKnife,
                    DemoObjective.ReceiveSheriffMission,
                    DemoObjective.FollowChesterAndJack,
                    DemoObjective.SearchSheriffOffice,
                    DemoObjective.ReturnToChurch,
                    DemoObjective.DiscoverChurchTruth,
                    DemoObjective.ConfrontCreatureInBarn,
                    DemoObjective.ReachForest
                })
                    Assert.That(progression.TryComplete(objective), Is.True, objective.ToString());

                Assert.That(progression.IsComplete, Is.True);
                Assert.That(sequence.IsShowingEnding, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequenceObject);
                UnityEngine.Object.DestroyImmediate(progressionObject);
            }
        }

        [Test]
        public void EnteringForestDoesNotShowNarrationOverTheHardCutAndTitle()
        {
            var sequenceObject = new GameObject("Barn ending interaction test");
            var progressionObject = new GameObject("Forest ending progression test");
            try
            {
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                var sequence = sequenceObject.AddComponent<BarnEncounterSequence>();
                sequence.Configure(
                    progression, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null, null, null);

                foreach (var objective in new[]
                {
                    DemoObjective.FindLukeAtGate,
                    DemoObjective.FollowBootprintsToSaloon,
                    DemoObjective.InvestigateSaloonClues,
                    DemoObjective.ExamineSaloonKnife,
                    DemoObjective.ReceiveSheriffMission,
                    DemoObjective.FollowChesterAndJack,
                    DemoObjective.SearchSheriffOffice,
                    DemoObjective.ReturnToChurch,
                    DemoObjective.DiscoverChurchTruth,
                    DemoObjective.ConfrontCreatureInBarn
                })
                    Assert.That(progression.TryComplete(objective), Is.True, objective.ToString());

                var stageField = typeof(BarnEncounterSequence).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(stageField, Is.Not.Null);
                stageField.SetValue(sequence, Enum.Parse(stageField.FieldType, "ForestOpen"));

                Assert.That(sequence.TryInteract(BarnInteractionKind.ForestThreshold, out var result), Is.True);
                Assert.That(result, Is.Empty);
                Assert.That(sequence.IsShowingEnding, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequenceObject);
                UnityEngine.Object.DestroyImmediate(progressionObject);
            }
        }

        [Test]
        public void GideonDialogueWaitsUntilJackReachesTheAftermathPosition()
        {
            var sequenceObject = new GameObject("Barn aftermath order test");
            var jackObject = new GameObject("Jack aftermath order test");
            try
            {
                jackObject.AddComponent<BoxCollider>();
                var jack = jackObject.AddComponent<JackRescueInteractable>();
                jack.KnockOutAtBarn(new Vector3(5f, 0.1f, 105f));
                jack.WakeAndRunToAftermath(Vector3.one, new Vector3(0f, 0.1f, 7f));

                var sequence = sequenceObject.AddComponent<BarnEncounterSequence>();
                sequence.Configure(
                    null, null, null, null, jack, null, null, null,
                    null, null, null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null, null, null);
                var stageField = typeof(BarnEncounterSequence).GetField("_stage", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(stageField, Is.Not.Null);
                stageField.SetValue(sequence, Enum.Parse(stageField.FieldType, "GideonDialogue"));

                Assert.That(sequence.GetPrompt(BarnInteractionKind.Gideon), Is.Empty);
                Assert.That(sequence.TryInteract(BarnInteractionKind.Gideon, out var result), Is.False);
                Assert.That(result, Is.Empty);
                Assert.That(jack.IsRunningToAftermath, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequenceObject);
                UnityEngine.Object.DestroyImmediate(jackObject);
            }
        }

        [Test]
        public void GideonDialogueMatchesTheOfficialScreenplayWordForWordAndInOrder()
        {
            var sequenceObject = new GameObject("Barn screenplay test");
            try
            {
                var sequence = sequenceObject.AddComponent<BarnEncounterSequence>();
                CollectionAssert.AreEqual(new[]
                {
                    "Protagonista: \"O que... o que era isso?\"",
                    "Gideon: (Recarregando a espingarda com calma assustadora) \"O resultado da ganância de Ash Creek. Eles cavaram fundo demais na mina e acordaram os imitadores. Eles vestem nossas vozes como quem veste um casaco.\"",
                    "Protagonista: \"Eu vim buscar Layla. O registro dizia que ela estava aqui. Aquele bicho... usou a voz dela.\"",
                    "Gideon: \"Ela esteve. Mas sua garota era esperta. Quando ela viu os primeiros doentes mudando, ela percebeu que trancar portas não ia adiantar. Ela fugiu antes que o xerife trancasse esse abatedouro. Mas ela não foi embora pela estrada principal.\"",
                    "Protagonista: \"Para onde ela foi?\"",
                    "Gideon: \"Ela achou que podia destruir a raiz disso tudo. Ela seguiu o rastro das criaturas de volta para o buraco de onde rastejaram. Ela entrou na Floresta dos Suspiros. O caminho direto para o coração da mina.\"",
                    "Jack vai até a beira da porta, fareja o vento que vem das árvores negras e solta um uivo baixo e fúnebre.",
                    "Gideon: (Olhando para as árvores escuras) \"A cidade já está morta, cowboy. A floresta é onde eles não precisam mais fingir que são humanos. Se quiser achar a sua Layla... é lá que o verdadeiro pesadelo começa.\""
                }, sequence.ScriptedGideonDialogue);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequenceObject);
            }
        }

        [Test]
        public void KnifeBreaksOnTheBarnMimicAndCannotBeUsedAgain()
        {
            var knifeObject = new GameObject("Combat knife inventory test");
            try
            {
                var knife = knifeObject.AddComponent<CombatKnifeInventory>();

                Assert.That(knife.AcquireKnife(), Is.True);
                Assert.That(knife.HasKnife, Is.True);
                Assert.That(knife.BreakAgainstMimic(), Is.True);
                Assert.That(knife.HasKnife, Is.False);
                Assert.That(knife.BreakAgainstMimic(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(knifeObject);
            }
        }

        [Test]
        public void MissedOrUnlitShotsDoNotConsumeARevolverRound()
        {
            var revolver = new SavingShotInventoryState();

            Assert.That(revolver.TryAcquireRevolver(), Is.True);
            Assert.That(revolver.TryFireAtTarget(false), Is.False);
            Assert.That(revolver.RoundsRemaining, Is.EqualTo(6));
            Assert.That(revolver.TryFireAtTarget(true), Is.True);
            Assert.That(revolver.RoundsRemaining, Is.EqualTo(5));
        }

        [Test]
        public void MimicAppearsOnlyWhenThePlayerAimsTheLitLanternAtTheRaftersAndTheKnifeQteBreaksIt()
        {
            var root = new GameObject("Barn lantern reveal test fixture");
            try
            {
                var progressionObject = Child("Progression", root.transform);
                var progression = progressionObject.AddComponent<DemoProgressionComponent>();
                foreach (var objective in new[]
                {
                    DemoObjective.FindLukeAtGate,
                    DemoObjective.FollowBootprintsToSaloon,
                    DemoObjective.InvestigateSaloonClues,
                    DemoObjective.ExamineSaloonKnife,
                    DemoObjective.ReceiveSheriffMission,
                    DemoObjective.FollowChesterAndJack,
                    DemoObjective.SearchSheriffOffice,
                    DemoObjective.ReturnToChurch,
                    DemoObjective.DiscoverChurchTruth
                })
                    Assert.That(progression.TryComplete(objective), Is.True, objective.ToString());

                var playerObject = Child("Player", root.transform);
                var cameraObject = Child("View camera", playerObject.transform);
                cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                var camera = cameraObject.AddComponent<Camera>();
                playerObject.AddComponent<CharacterController>();
                var player = playerObject.AddComponent<ForgottenTrail.Gameplay.Player.FirstPersonController>();
                player.ConfigureViewCamera(camera);
                var knife = playerObject.AddComponent<CombatKnifeInventory>();
                var key = playerObject.AddComponent<BarnKeyInventory>();
                knife.AcquireKnife();
                key.AcquireKey();

                var lanternObject = Child("Lantern", root.transform);
                var lantern = lanternObject.AddComponent<ForgottenTrail.Gameplay.Lantern.HandLanternPickup>();
                lantern.Configure(2.8f, null, null, null, null, null, null);
                Assert.That(lantern.TryInteract(1f, out _, out _), Is.True);

                var creatureObject = Child("Hidden mimic", root.transform);
                creatureObject.AddComponent<MeshRenderer>();
                var rafterObject = Child("Rafter target", root.transform);
                rafterObject.transform.position = new Vector3(0f, 5.8f, 3.6f);
                var rafterPoint = rafterObject.AddComponent<BarnInteractionPoint>();
                var sequenceObject = Child("Barn encounter", root.transform);
                var sequence = sequenceObject.AddComponent<BarnEncounterSequence>();
                sequence.Configure(
                    progression, player, null, lantern, null, key, knife, null,
                    null, null, null, null, creatureObject, null, null,
                    null, null, null, null, null, rafterPoint, null, null, null,
                    null, null, null, null, null);

                Assert.That(sequence.TryInteract(BarnInteractionKind.Door, out var doorLine), Is.True);
                Assert.That(doorLine, Is.EqualTo("Voz distorcida de Layla: \"Você veio... Eu sabia que você carregaria esse peso por mim. Abra a porta.\""));
                Assert.That(sequence.TryInteract(BarnInteractionKind.Remains, out var remainsLines), Is.True);
                Assert.That(remainsLines, Is.EqualTo("Protagonista: \"Meu Deus... Hale não os trancou aqui para protegê-los. Ele trancou a comida delas.\"\nVoz de Layla: \"Por que você demorou tanto?\""));
                Assert.That(creatureObject.activeSelf, Is.False);
                Assert.That(sequence.TryRevealCreatureWithLantern(), Is.False);

                Assert.That(player.ViewCamera, Is.Not.Null);
                camera.transform.LookAt(rafterObject.transform.position);
                Assert.That(lantern.IsHeld, Is.True);
                Assert.That(lantern.IsLit, Is.True);
                Assert.That(Vector3.Angle(camera.transform.forward, rafterObject.transform.position - camera.transform.position), Is.LessThan(1f));
                Assert.That(sequence.TryRevealCreatureWithLantern(), Is.True);
                Assert.That(creatureObject.activeSelf, Is.True);
                Assert.That(sequence.TryInteract(BarnInteractionKind.RafterCreature, out _), Is.True);
                Assert.That(sequence.CanDefendAgainstMimic, Is.True);
                Assert.That(sequence.TryDefendAgainstMimic(), Is.True);
                Assert.That(knife.HasKnife, Is.False);
                Assert.That(sequence.TryDefendAgainstMimic(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        [Test]
        public void JackRemainsVisibleWhileUnconsciousAndCanWakeAfterTheFight()
        {
            var jackObject = new GameObject("Jack barn test");
            try
            {
                var collider = jackObject.AddComponent<BoxCollider>();
                var jack = jackObject.AddComponent<JackRescueInteractable>();
                jack.KnockOutAtBarn(new Vector3(5f, 0.1f, 105f));

                Assert.That(jackObject.activeSelf, Is.True);
                Assert.That(collider.enabled, Is.False);
                Assert.That(jack.Prompt, Is.Empty);

                jack.WakeAndRunToAftermath(Vector3.one, new Vector3(0f, 0.1f, 7f));

                Assert.That(jackObject.activeSelf, Is.True);
                Assert.That(collider.enabled, Is.True);
                Assert.That(jack.IsRunningToAftermath, Is.True);
                Assert.That(jack.transform.position, Is.EqualTo(new Vector3(5f, 0.1f, 105f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(jackObject);
            }
        }
    }
}
