using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Church;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.SheriffOffice;
using ForgottenTrail.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ForgottenTrail.Tests.Church
{
    public sealed class ChurchInvestigationSceneTests
    {
        [Test]
        public void AshCreekSceneKeepsTheTwoChurchVisitsInTheirScreenplayStages()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                ChurchInvestigationTracker tracker = null;
                ChurchFalseEliasEncounter falseElias = null;
                ChurchReturnRevealInteractable returnReveal = null;
                var falseEliasStartsHidden = false;
                var returnRevealStartsHidden = false;
                var hasOpenConfessional = false;
                var trueEliasIsInsideConfessional = false;
                InteractableClue chesterRoute = null;
                var churchClueIds = new System.Collections.Generic.List<string>();
                ScreenplayOpeningLine openingLine = null;
                SheriffBadgeInventory badgeInventory = null;

                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    if (rootObject.name == "Player — Investigator")
                    {
                        openingLine = rootObject.GetComponent<ScreenplayOpeningLine>();
                        badgeInventory = rootObject.GetComponent<SheriffBadgeInventory>();
                    }
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
                    returnReveal = churchRoot.GetComponentInChildren<ChurchReturnRevealInteractable>(true);
                    falseEliasStartsHidden = falseElias != null && !falseElias.gameObject.activeSelf;
                    returnRevealStartsHidden = returnReveal != null && !returnReveal.gameObject.activeInHierarchy;
                    foreach (var clue in churchRoot.GetComponentsInChildren<InteractableClue>(true))
                        churchClueIds.Add(clue.InteractionId);
                    foreach (var child in churchRoot.GetComponentsInChildren<UnityEngine.Transform>(true))
                    {
                        if (child.name == "Church return — confessional door standing open")
                            hasOpenConfessional = true;
                        if (child.name == "True Elias — dead inside the confessional")
                            trueEliasIsInsideConfessional = true;
                    }
                }

                Assert.That(tracker, Is.Not.Null);
                Assert.That(openingLine, Is.Not.Null);
                Assert.That(badgeInventory, Is.Not.Null, "The player needs an inventory state for Elias's handoff.");
                Assert.That(ScreenplayOpeningLine.ScriptedText, Is.EqualTo("Protagonista: \"Ash Creek... A última carta de Layla veio daqui.\""));
                Assert.That(falseElias, Is.Not.Null);
                Assert.That(falseElias.InteractionId, Is.EqualTo(ChurchInvestigationState.FalseEliasInteractionId));
                Assert.That(returnReveal, Is.Not.Null);
                Assert.That(returnReveal.InteractionId, Is.EqualTo(ChurchInvestigationState.ReturnRevealInteractionId));
                Assert.That(falseEliasStartsHidden, Is.True, "The false priest encounter waits until the mission at the church.");
                Assert.That(returnRevealStartsHidden, Is.True, "The dead Elias and warning appear only on the return visit.");
                Assert.That(hasOpenConfessional, Is.True);
                Assert.That(trueEliasIsInsideConfessional, Is.True);
                Assert.That(churchClueIds, Is.Empty, "The script has no bell-rope, shadow, parish-ledger, or loose-badge investigation.");
                Assert.That(chesterRoute, Is.Not.Null, "The Chester and Jack trail needs a playable completion point.");
                Assert.That(chesterRoute.AdvancesObjective, Is.False, "The exit waits for the screenplay dialogue and Jack rescue.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
