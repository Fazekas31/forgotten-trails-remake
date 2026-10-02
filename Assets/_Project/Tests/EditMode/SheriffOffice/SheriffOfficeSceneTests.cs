using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Combat;
using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.SheriffOffice;
using ForgottenTrail.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgottenTrail.Tests.SheriffOffice
{
    public sealed class SheriffOfficeSceneTests
    {
        [Test]
        public void AshCreekSceneContainsSheriffInvestigationAndNoEarlyRevolverPickup()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                SheriffOfficeInvestigationTracker tracker = null;
                SheriffHaleEncounterInteractable hale = null;
                RedBookInteractable redBook = null;
                SavingShotInventory inventory = null;
                SheriffOfficeEscapeSequence escapeSequence = null;
                SheriffOfficeEscapeInteractable escapeExit = null;
                InteractableClue handkerchiefEvidence = null;
                GameObject exitBarrier = null;
                var jailBarCount = 0;
                var collidableRayIgnoredBarCount = 0;
                var stairStepCount = 0;
                var hasLandingCollider = false;
                var hasRearLeftWall = false;
                var hasRearRightWall = false;
                var hasRevolverObject = false;

                foreach (var root in scene.GetRootGameObjects())
                {
                    tracker = tracker ?? root.GetComponent<SheriffOfficeInvestigationTracker>();
                    inventory = inventory ?? root.GetComponentInChildren<SavingShotInventory>(true);
                    hale = hale ?? root.GetComponentInChildren<SheriffHaleEncounterInteractable>(true);
                    redBook = redBook ?? root.GetComponentInChildren<RedBookInteractable>(true);
                    escapeSequence = escapeSequence ?? root.GetComponentInChildren<SheriffOfficeEscapeSequence>(true);
                    escapeExit = escapeExit ?? root.GetComponentInChildren<SheriffOfficeEscapeInteractable>(true);
                    foreach (var clue in root.GetComponentsInChildren<InteractableClue>(true))
                    {
                        if (clue.InteractionId == SheriffOfficeInvestigationState.LaylaEvidenceInteractionId)
                            handkerchiefEvidence = clue;
                    }

                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.IndexOf(".38 revolver", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || child.name.IndexOf("revolver pickup", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || child.name.IndexOf("revólver para coleta", System.StringComparison.OrdinalIgnoreCase) >= 0)
                            hasRevolverObject = true;
                        if (child.name == "Jail cell — front iron bar")
                        {
                            jailBarCount++;
                            if (child.GetComponent<Collider>() != null && child.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
                                collidableRayIgnoredBarCount++;
                        }
                        if (child.name == "Sheriff office — rear wall left")
                            hasRearLeftWall = true;
                        if (child.name == "Sheriff office — rear wall right")
                            hasRearRightWall = true;
                        if (child.name.StartsWith("Sheriff office — second floor stair step", System.StringComparison.Ordinal))
                            stairStepCount++;
                        if (child.name == "Sheriff office — second floor landing")
                            hasLandingCollider = child.GetComponent<Collider>() != null;
                        if (child.name == "Sheriff office — temporary rear exit barrier")
                            exitBarrier = child.gameObject;
                    }
                }

                Assert.That(tracker, Is.Not.Null);
                Assert.That(handkerchiefEvidence, Is.Not.Null);
                Assert.That(hale, Is.Not.Null);
                Assert.That(redBook, Is.Not.Null);
                Assert.That(hasRevolverObject, Is.False);
                Assert.That(inventory, Is.Not.Null);
                Assert.That(escapeSequence, Is.Not.Null);
                Assert.That(escapeExit, Is.Not.Null);
                Assert.That(exitBarrier, Is.Not.Null);
                Assert.That(exitBarrier.activeSelf, Is.False);
                Assert.That(inventory.HasRevolver, Is.False);
                Assert.That(inventory.RoundsRemaining, Is.Zero);
                Assert.That(jailBarCount, Is.GreaterThanOrEqualTo(8));
                Assert.That(collidableRayIgnoredBarCount, Is.EqualTo(jailBarCount));
                Assert.That(stairStepCount, Is.GreaterThanOrEqualTo(12));
                Assert.That(hasLandingCollider, Is.True);
                Assert.That(hasRearLeftWall, Is.True);
                Assert.That(hasRearRightWall, Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
