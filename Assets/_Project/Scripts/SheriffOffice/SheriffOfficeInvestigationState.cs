using System;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Tracks the evidence, conversation, Red Book, and quiet escape sequence.</summary>
    public sealed class SheriffOfficeInvestigationState
    {
        public const string LaylaEvidenceInteractionId = "sheriff.layla-handkerchief";
        public const string HaleInteractionId = "sheriff.hale";
        public const string HaleAccountInteractionId = "sheriff.hale-account";
        public const string RedBookInteractionId = "sheriff.red-book";
        public const string QuietEscapeInteractionId = "sheriff.quiet-escape";

        private bool _hasLaylaEvidence;
        private bool _hasMetHale;
        private bool _hasHeardHaleAccount;
        private bool _hasFoundRedBook;
        private bool _hasEscaped;

        public bool HasLaylaEvidence => _hasLaylaEvidence;
        public bool HasMetHale => _hasMetHale;
        public bool HasHeardHaleAccount => _hasHeardHaleAccount;
        public bool HasFoundRedBook => _hasFoundRedBook;
        public bool HasEscaped => _hasEscaped;
        public bool IsComplete => _hasFoundRedBook && _hasEscaped;

        public bool TryRecord(string interactionId)
        {
            if (string.Equals(interactionId, LaylaEvidenceInteractionId, StringComparison.Ordinal))
                return SetOnce(ref _hasLaylaEvidence);

            if (string.Equals(interactionId, HaleInteractionId, StringComparison.Ordinal))
                return _hasLaylaEvidence && SetOnce(ref _hasMetHale);

            if (string.Equals(interactionId, HaleAccountInteractionId, StringComparison.Ordinal))
                return _hasMetHale && SetOnce(ref _hasHeardHaleAccount);

            if (string.Equals(interactionId, RedBookInteractionId, StringComparison.Ordinal))
                return _hasHeardHaleAccount && SetOnce(ref _hasFoundRedBook);

            if (string.Equals(interactionId, QuietEscapeInteractionId, StringComparison.Ordinal))
                return _hasFoundRedBook && SetOnce(ref _hasEscaped);

            return false;
        }

        private static bool SetOnce(ref bool value)
        {
            if (value)
                return false;

            value = true;
            return true;
        }
    }
}
