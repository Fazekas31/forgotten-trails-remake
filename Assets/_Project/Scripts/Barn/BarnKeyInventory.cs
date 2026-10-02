using UnityEngine;

namespace ForgottenTrail.Gameplay.Barn
{
    /// <summary>Tracks the iron barn key Hale hands over during the screenplay exchange.</summary>
    public sealed class BarnKeyInventory : MonoBehaviour
    {
        public bool HasKey { get; private set; }

        public bool AcquireKey()
        {
            if (HasKey)
                return false;

            HasKey = true;
            return true;
        }
    }
}
