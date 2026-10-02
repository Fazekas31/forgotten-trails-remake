using System;
using System.Collections.Generic;

namespace ForgottenTrail.Gameplay.Saloon
{
    /// <summary>Tracks the four required saloon evidence points without depending on Unity objects.</summary>
    public sealed class SaloonInvestigationState
    {
        public const string BloodTrailId = "saloon.blood-trail";
        public const string FootprintsId = "saloon.footprints";
        public const string BrokenFurnitureId = "saloon.broken-furniture";

        private readonly HashSet<string> _recorded = new HashSet<string>(StringComparer.Ordinal);

        public bool HasReadNote => _recorded.Contains(SaloonApparitionState.NoteInteractionId);
        public bool IsComplete => _recorded.Count == 4;

        public bool TryRecord(string interactionId)
        {
            if (!IsRequiredClue(interactionId))
                return false;

            return _recorded.Add(interactionId);
        }

        private static bool IsRequiredClue(string interactionId)
        {
            return string.Equals(interactionId, SaloonApparitionState.NoteInteractionId, StringComparison.Ordinal)
                || string.Equals(interactionId, BloodTrailId, StringComparison.Ordinal)
                || string.Equals(interactionId, FootprintsId, StringComparison.Ordinal)
                || string.Equals(interactionId, BrokenFurnitureId, StringComparison.Ordinal);
        }
    }
}
