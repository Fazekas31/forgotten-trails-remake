using System;
using System.Collections.Generic;

namespace ForgottenTrail.Gameplay.Church
{
    /// <summary>Tracks the evidence needed to recognize the false priest in Ash Creek's church.</summary>
    public sealed class ChurchInvestigationState
    {
        public const string BellRopeInteractionId = "church.bell-rope";
        public const string UnshadowedLightInteractionId = "church.unshadowed-light";
        public const string ParishLedgerInteractionId = "church.parish-ledger";
        public const string FalseEliasInteractionId = "church.false-elias";
        public const string DeputyBadgeInteractionId = "church.deputy-badge";

        private static readonly HashSet<string> RequiredClueIds = new HashSet<string>(StringComparer.Ordinal)
        {
            BellRopeInteractionId,
            UnshadowedLightInteractionId,
            ParishLedgerInteractionId
        };

        private readonly HashSet<string> _recordedClues = new HashSet<string>(StringComparer.Ordinal);

        public bool HasExaminedRequiredClues => _recordedClues.Count == RequiredClueIds.Count;
        public bool HasRecognizedFalseElias { get; private set; }
        public bool HasRecoveredDeputyBadge { get; private set; }
        public bool IsComplete => HasRecognizedFalseElias && HasRecoveredDeputyBadge;

        public bool TryRecordInteraction(string interactionId)
        {
            if (RequiredClueIds.Contains(interactionId))
                return _recordedClues.Add(interactionId);

            if (string.Equals(interactionId, FalseEliasInteractionId, StringComparison.Ordinal))
            {
                if (!HasExaminedRequiredClues || HasRecognizedFalseElias)
                    return false;

                HasRecognizedFalseElias = true;
                return true;
            }

            if (!string.Equals(interactionId, DeputyBadgeInteractionId, StringComparison.Ordinal)
                || !HasRecognizedFalseElias
                || HasRecoveredDeputyBadge)
                return false;

            HasRecoveredDeputyBadge = true;
            return true;
        }
    }
}
