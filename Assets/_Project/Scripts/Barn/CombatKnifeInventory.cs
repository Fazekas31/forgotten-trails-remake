using UnityEngine;

namespace ForgottenTrail.Gameplay.Barn
{
    /// <summary>Tracks the knife the player examines in the saloon and uses during the barn ambush.</summary>
    public sealed class CombatKnifeInventory : MonoBehaviour
    {
        public bool HasKnife { get; private set; }

        public bool AcquireKnife()
        {
            if (HasKnife)
                return false;

            HasKnife = true;
            return true;
        }

        public bool BreakAgainstMimic()
        {
            if (!HasKnife)
                return false;

            HasKnife = false;
            return true;
        }
    }
}
