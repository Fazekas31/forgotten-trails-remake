using UnityEngine;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Tracks Elias's sheriff badge so the player can present it to Hale.</summary>
    public sealed class SheriffBadgeInventory : MonoBehaviour
    {
        public bool HasBadge { get; private set; }

        public bool AcquireBadge()
        {
            if (HasBadge)
                return false;

            HasBadge = true;
            return true;
        }
    }
}
