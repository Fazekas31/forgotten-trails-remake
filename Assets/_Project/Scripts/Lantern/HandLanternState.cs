using System;

namespace ForgottenTrail.Gameplay.Lantern
{
    /// <summary>Portable lantern rules without Unity dependencies, so the pickup and toggle contract stays testable.</summary>
    public sealed class HandLanternState
    {
        private readonly float _pickupRange;

        public bool IsHeld { get; private set; }
        public bool IsLit { get; private set; }

        public HandLanternState(float pickupRange)
        {
            if (float.IsNaN(pickupRange) || float.IsInfinity(pickupRange) || pickupRange <= 0f)
                throw new ArgumentOutOfRangeException(nameof(pickupRange), "Pickup range must be a finite positive distance.");

            _pickupRange = pickupRange;
        }

        public bool TryPickUp(float distance)
        {
            if (IsHeld || float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f || distance > _pickupRange)
                return false;

            IsHeld = true;
            IsLit = true;
            return true;
        }

        public bool TryToggle()
        {
            if (!IsHeld)
                return false;

            IsLit = !IsLit;
            return true;
        }
    }
}
