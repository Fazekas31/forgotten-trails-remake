using ForgottenTrail.Gameplay.Lantern;
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
    }
}
