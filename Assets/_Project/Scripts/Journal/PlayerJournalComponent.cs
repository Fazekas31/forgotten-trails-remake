using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenTrail.Gameplay.Journal
{
    /// <summary>Unity input and compact field-notes overlay for the player's journal state.</summary>
    public sealed class PlayerJournalComponent : MonoBehaviour
    {
        [TextArea(2, 4)]
        [SerializeField] private string openingEntry = string.Empty;

        private PlayerJournal _journal;
        private Vector2 _scrollPosition;

        public IReadOnlyList<string> Entries => Journal.Entries;
        public bool IsOpen => Journal.IsOpen;
        private PlayerJournal Journal => _journal ?? (_journal = new PlayerJournal());

        public void Configure(string firstEntry)
        {
            openingEntry = firstEntry;
            Initialize();
        }

        public bool Record(string entry)
        {
            return Journal.Record(entry);
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_journal == null)
                _journal = new PlayerJournal();
            _journal.Record(openingEntry);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
                Journal.ToggleOpen();
        }

        private void OnGUI()
        {
            if (!IsOpen)
                return;

            var panel = new Rect(42f, 68f, Mathf.Min(540f, Screen.width - 84f), Screen.height - 136f);
            GUI.color = new Color(0.11f, 0.095f, 0.075f, 0.96f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            var heading = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.89f, 0.75f, 0.52f) }
            };
            GUI.Label(new Rect(panel.x + 24f, panel.y + 18f, panel.width - 48f, 32f), "CADERNO DE CAMPO", heading);
            var close = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 13,
                normal = { textColor = new Color(0.68f, 0.67f, 0.61f) }
            };
            GUI.Label(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, 26f), "[J] fechar", close);

            var contentRect = new Rect(panel.x + 24f, panel.y + 62f, panel.width - 48f, panel.height - 84f);
            var contentHeight = Mathf.Max(contentRect.height, Entries.Count * 116f);
            _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, new Rect(0f, 0f, contentRect.width - 18f, contentHeight));
            for (var i = 0; i < Entries.Count; i++)
            {
                var entryRect = new Rect(0f, i * 116f, contentRect.width - 28f, 104f);
                GUI.color = new Color(0.78f, 0.68f, 0.5f, 0.22f);
                GUI.Box(new Rect(entryRect.x, entryRect.y + 101f, entryRect.width, 1f), GUIContent.none);
                GUI.color = Color.white;
                var body = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    fontSize = 15,
                    normal = { textColor = new Color(0.88f, 0.84f, 0.75f) }
                };
                GUI.Label(entryRect, Entries[i], body);
            }
            GUI.EndScrollView();
        }
    }
}
