using System.Collections.Generic;
using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Church;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ForgottenTrail.Tests.Church
{
    public sealed class ChurchInvestigationSceneTests
    {
        [Test]
        public void AshCreekSceneContainsTheCluesEncounterAndHiddenBadgeForTheChurchBeat()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects();
                ChurchInvestigationTracker tracker = null;
                ChurchFalseEliasEncounter falseElias = null;
                var clueIds = new HashSet<string>();
                var deputyBadgeIsHidden = false;
                var badge = (UnityEngine.GameObject)null;
                var churchFloor = (UnityEngine.BoxCollider)null;
                InteractableClue chesterRoute = null;

                foreach (var rootObject in root)
                {
                    if (rootObject.name == "Church — floor")
                        churchFloor = rootObject.GetComponent<UnityEngine.BoxCollider>();

                    foreach (var clue in rootObject.GetComponentsInChildren<InteractableClue>(true))
                    {
                        if (clue.InteractionId == ChesterJackRouteState.SheriffOfficeExitInteractionId)
                            chesterRoute = clue;
                    }

                    var churchRoot = rootObject.name == "Church investigation — setpiece"
                        ? rootObject.transform
                        : rootObject.transform.Find("Church investigation — setpiece");
                    if (churchRoot == null)
                        continue;

                    tracker = churchRoot.GetComponent<ChurchInvestigationTracker>();
                    falseElias = churchRoot.GetComponentInChildren<ChurchFalseEliasEncounter>(true);
                    foreach (var clue in churchRoot.GetComponentsInChildren<InteractableClue>(true))
                    {
                        clueIds.Add(clue.InteractionId);
                        if (clue.InteractionId == ChurchInvestigationState.DeputyBadgeInteractionId)
                        {
                            deputyBadgeIsHidden = !clue.gameObject.activeSelf;
                            badge = clue.gameObject;
                        }
                    }
                }

                Assert.That(tracker, Is.Not.Null);
                Assert.That(falseElias, Is.Not.Null);
                Assert.That(falseElias.InteractionId, Is.EqualTo(ChurchInvestigationState.FalseEliasInteractionId));
                Assert.That(clueIds, Does.Contain(ChurchInvestigationState.BellRopeInteractionId));
                Assert.That(clueIds, Does.Contain(ChurchInvestigationState.UnshadowedLightInteractionId));
                Assert.That(clueIds, Does.Contain(ChurchInvestigationState.ParishLedgerInteractionId));
                Assert.That(clueIds, Does.Contain(ChurchInvestigationState.DeputyBadgeInteractionId));
                Assert.That(deputyBadgeIsHidden, Is.True);
                Assert.That(churchFloor, Is.Not.Null);
                Assert.That(badge, Is.Not.Null);
                Assert.That(WorldMinimumY(badge), Is.GreaterThan(churchFloor.bounds.max.y), "The hidden badge must rest visibly above the church floor.");
                var badgeBacking = badge.transform.Find("Church investigation — badge backing");
                Assert.That(badgeBacking, Is.Not.Null);
                Assert.That(WorldMinimumY(badgeBacking.gameObject), Is.GreaterThan(churchFloor.bounds.max.y), "The badge backing must also rest above the floor.");
                Assert.That(chesterRoute, Is.Not.Null, "The Chester and Jack trail needs a playable completion point.");
                Assert.That(chesterRoute.AdvancesObjective, Is.False, "The exit clue must wait for the rescue route tracker to confirm Chester and Jack's story beats.");
                Assert.That(chesterRoute.ObjectiveOnInspect, Is.EqualTo(DemoObjective.FollowChesterAndJack));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static float WorldMinimumY(UnityEngine.GameObject gameObject)
        {
            var mesh = gameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
            var minimumY = float.PositiveInfinity;
            foreach (var vertex in mesh.vertices)
                minimumY = UnityEngine.Mathf.Min(minimumY, gameObject.transform.TransformPoint(vertex).y);
            return minimumY;
        }
    }
}
