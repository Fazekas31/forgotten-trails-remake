using System;

namespace ForgottenTrail.Gameplay.Saloon
{
    /// <summary>Allows the saloon's single scripted apparition beat to play once after the note is read.</summary>
    public sealed class SaloonApparitionState
    {
        public const string NoteInteractionId = "saloon.torn-note";

        public bool HasTriggered { get; private set; }

        public bool TryTrigger(string interactionId)
        {
            if (HasTriggered || !string.Equals(interactionId, NoteInteractionId, StringComparison.Ordinal))
                return false;

            HasTriggered = true;
            return true;
        }
    }
}
