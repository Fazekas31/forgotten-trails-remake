using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.World;
using ForgottenTrail.Gameplay.Barn;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenTrail.Tests.Barn
{
    public sealed class ScreenplayItemGrantTrackerTests
    {
        [Test]
        public void KnifeAndBarnKeyAreGrantedOnlyByTheirScreenplayInteractions()
        {
            var player = new GameObject("Player");
            var knifeClueObject = new GameObject("Saloon knife");
            var haleExchangeObject = new GameObject("Hale exchange");
            var keyVisual = new GameObject("Hale's visible barn key");

            try
            {
                var interactor = player.AddComponent<PlayerInteractor>();
                var knife = player.AddComponent<CombatKnifeInventory>();
                var key = player.AddComponent<BarnKeyInventory>();
                var grants = player.AddComponent<ScreenplayItemGrantTracker>();
                grants.Configure(interactor, knife, key, null, keyVisual);

                var knifeClue = knifeClueObject.AddComponent<InteractableClue>();
                knifeClue.Configure("saloon.knife", "Examinar a faca", "A faca está cravada no balcão.", 2.8f, false, default);
                var haleExchange = haleExchangeObject.AddComponent<InteractableClue>();
                haleExchange.Configure("sheriff.hale-account", "Falar com Hale", "", 2.8f, false, default);

                Assert.That(knife.HasKnife, Is.False);
                Assert.That(key.HasKey, Is.False);
                Assert.That(interactor.TryInteract(knifeClue, 1f), Is.True);
                Assert.That(knife.HasKnife, Is.True);
                Assert.That(key.HasKey, Is.False);
                Assert.That(interactor.TryInteract(haleExchange, 1f), Is.True);
                Assert.That(key.HasKey, Is.True);
                Assert.That(keyVisual.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(knifeClueObject);
                Object.DestroyImmediate(haleExchangeObject);
                Object.DestroyImmediate(keyVisual);
                Object.DestroyImmediate(player);
            }
        }
    }
}
