using System;
using System.Collections.Generic;

namespace ForgottenTrail.Gameplay.Saloon
{
    /// <summary>Tracks the four required saloon evidence points without depending on Unity objects.</summary>
    public sealed class SaloonInvestigationState
    {
        private static readonly HashSet<string> RequiredClueIds = new HashSet<string>(StringComparer.Ordinal)
        {
            SaloonApparitionState.NoteInteractionId,
            BloodTrailId,
            FootprintsId,
            BrokenFurnitureId
        };

        public const string BloodTrailId = "saloon.blood-trail";
        public const string FootprintsId = "saloon.footprints";
        public const string BrokenFurnitureId = "saloon.broken-furniture";

        private readonly HashSet<string> _recorded = new HashSet<string>(StringComparer.Ordinal);

        public bool HasReadNote => _recorded.Contains(SaloonApparitionState.NoteInteractionId);
        public bool IsComplete => _recorded.Count == RequiredClueIds.Count;

        public bool TryRecord(string interactionId)
        {
            if (!RequiredClueIds.Contains(interactionId))
                return false;

            return _recorded.Add(interactionId);
        }
    }
}
