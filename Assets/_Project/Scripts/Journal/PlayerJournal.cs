using System;
using System.Collections.Generic;

namespace ForgottenTrail.Gameplay.Journal
{
    /// <summary>Ordered field notes: each clue is recorded once, in the order the player discovers it.</summary>
    public sealed class PlayerJournal
    {
        private readonly List<string> _entries = new List<string>();
        private readonly HashSet<string> _knownEntries = new HashSet<string>(StringComparer.Ordinal);
        private readonly IReadOnlyList<string> _readOnlyEntries;

        public IReadOnlyList<string> Entries => _readOnlyEntries;
        public bool IsOpen { get; private set; }

        public PlayerJournal()
        {
            _readOnlyEntries = _entries.AsReadOnly();
        }

        public bool Record(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry) || !_knownEntries.Add(entry))
                return false;

            _entries.Add(entry);
            return true;
        }

        public void ToggleOpen()
        {
            IsOpen = !IsOpen;
        }
    }
}
