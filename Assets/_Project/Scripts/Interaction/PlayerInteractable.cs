using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Interaction
{
    /// <summary>One player-facing interaction in the world, including an optional journal or progression result.</summary>
    public abstract class PlayerInteractable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public virtual string InteractionId => gameObject.name;
        public virtual string JournalEntry => string.Empty;

        public abstract bool TryInteract(float distance, out string result, out DemoObjective? completedObjective);
    }
}
