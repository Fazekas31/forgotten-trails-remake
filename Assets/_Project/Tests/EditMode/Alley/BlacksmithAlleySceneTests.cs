using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Enemies;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ForgottenTrail.Tests.Alley
{
    public sealed class BlacksmithAlleySceneTests
    {
        [Test]
        public void AshCreekSceneContainsTheStealthRouteRescueAndExitGate()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                ChesterJackRouteTracker tracker = null;
                BlacksmithIronGate gate = null;
                ChesterEncounterInteractable chester = null;
                JackRescueInteractable jack = null;
                EnemyAwarenessAgent watcher = null;
                InteractableClue trail = null;
                InteractableClue exit = null;
                var coverCount = 0;
                var gateBarCount = 0;
                var mainStreetIsBlocked = false;
                var blindCreatureCount = 0;

                foreach (var root in scene.GetRootGameObjects())
                {
                    tracker = tracker ?? root.GetComponent<ChesterJackRouteTracker>();
                    gate = gate ?? root.GetComponentInChildren<BlacksmithIronGate>(true);
                    chester = chester ?? root.GetComponentInChildren<ChesterEncounterInteractable>(true);
                    jack = jack ?? root.GetComponentInChildren<JackRescueInteractable>(true);
                    watcher = watcher ?? root.GetComponent<EnemyAwarenessAgent>();
                    foreach (var clue in root.GetComponentsInChildren<InteractableClue>(true))
                    {
                        if (clue.InteractionId == ChesterJackRouteState.TrailInteractionId)
                            trail = clue;
                        if (clue.InteractionId == ChesterJackRouteState.SheriffOfficeExitInteractionId)
                            exit = clue;
                    }

                    foreach (var child in root.GetComponentsInChildren<UnityEngine.Transform>(true))
                    {
                        if (child.name.StartsWith("Stealth cover —", System.StringComparison.Ordinal))
                            coverCount++;
                        if (child.name == "Blacksmith — gate iron bar")
                            gateBarCount++;
                        if (child.name == "Rubble — fallen timber blocking main street")
                            mainStreetIsBlocked = child.GetComponent<UnityEngine.Collider>() != null;
                        if (child.name == "Blind creature — foraging in the street")
                            blindCreatureCount++;
                    }
                }

                Assert.That(tracker, Is.Not.Null);
                Assert.That(tracker.State.HasFoundTrail, Is.False);
                Assert.That(gate, Is.Not.Null);
                Assert.That(gate.IsOpen, Is.False);
                Assert.That(chester, Is.Not.Null);
                var standingCollider = chester.GetComponent<UnityEngine.Collider>();
                Assert.That(standingCollider, Is.Not.Null);
                Assert.That(standingCollider.enabled, Is.True);
                chester.FallSilent();
                Assert.That(standingCollider.enabled, Is.False);
                Assert.That(chester.enabled, Is.False);
                Assert.That(jack, Is.Not.Null);
                Assert.That(watcher, Is.Not.Null);
                Assert.That(trail, Is.Not.Null);
                Assert.That(exit, Is.Not.Null);
                Assert.That(exit.AdvancesObjective, Is.False);
                Assert.That(exit.ObjectiveOnInspect, Is.EqualTo(DemoObjective.FollowChesterAndJack));
                Assert.That(coverCount, Is.GreaterThanOrEqualTo(3));
                Assert.That(gateBarCount, Is.GreaterThanOrEqualTo(12));
                Assert.That(mainStreetIsBlocked, Is.True);
                Assert.That(blindCreatureCount, Is.GreaterThanOrEqualTo(2));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
