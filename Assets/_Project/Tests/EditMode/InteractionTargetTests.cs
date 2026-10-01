using System;
using ForgottenTrail.Gameplay.Interaction;
using NUnit.Framework;

namespace ForgottenTrail.Tests.Interaction
{
    public sealed class InteractionTargetTests
    {
        [Test]
        public void CanInteract_ReturnsFalse_WhenTargetIsBeyondRange()
        {
            var target = new InteractionTarget("saloon.blood-trail", "Examinar rastros", "Sangue seco aponta para a porta dos fundos.", 2.0f);

            Assert.That(target.CanInteract(2.01f), Is.False);
        }

        [Test]
        public void CanInteract_ReturnsTrue_AtTheRangeBoundary()
        {
            var target = new InteractionTarget("saloon.blood-trail", "Examinar rastros", "Sangue seco aponta para a porta dos fundos.", 2.0f);

            Assert.That(target.CanInteract(2.0f), Is.True);
        }

        [Test]
        public void TryInteract_ReturnsDescription_WhenTargetIsInRange()
        {
            const string description = "Sangue seco aponta para a porta dos fundos.";
            var target = new InteractionTarget("saloon.blood-trail", "Examinar rastros", description, 2.0f);

            var interacted = target.TryInteract(1.25f, out var result);

            Assert.That(interacted, Is.True);
            Assert.That(result, Is.EqualTo(description));
        }

        [Test]
        public void Constructor_RejectsBlankIdentity()
        {
            Assert.Throws<ArgumentException>(() => new InteractionTarget(" ", "Examinar", "Texto", 2.0f));
        }
    }
}
