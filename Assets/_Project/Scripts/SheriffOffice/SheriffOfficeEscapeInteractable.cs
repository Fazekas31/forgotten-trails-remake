using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Exit action at the back door, gated by quiet movement and a dark lantern.</summary>
    public sealed class SheriffOfficeEscapeInteractable : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private SheriffOfficeEscapeSequence sequence;
        public override string Prompt => sequence == null || !sequence.IsActive ? string.Empty : "Escapar pelos fundos";
        public override string InteractionId => "sheriff.quiet-escape";
        public override string JournalEntry => string.Empty;

        public void Configure(SheriffOfficeEscapeSequence escapeSequence) => sequence = escapeSequence;

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange || sequence == null)
            {
                result = string.Empty;
                return false;
            }

            return sequence.TryLeaveQuietly(out result);
        }
    }
}
