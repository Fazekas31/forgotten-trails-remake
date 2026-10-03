using System.Collections.Generic;
using ForgottenTrail.Gameplay.UI;
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

            var scale = HudTextScale.Factor;
            var panel = new Rect(HudTextScale.Pixels(32f), HudTextScale.Pixels(48f), Mathf.Min(HudTextScale.Pixels(660f), Screen.width - HudTextScale.Pixels(64f)), Screen.height - HudTextScale.Pixels(96f));
            GUI.color = new Color(0.11f, 0.095f, 0.075f, 0.96f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            var heading = new GUIStyle(GUI.skin.label)
            {
                fontSize = HudTextScale.FontSize(30),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.89f, 0.75f, 0.52f) }
            };
            GUI.Label(new Rect(panel.x + 28f * scale, panel.y + 16f * scale, panel.width - 56f * scale, 42f * scale), "CADERNO DE CAMPO", heading);
            var close = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = HudTextScale.FontSize(18),
                normal = { textColor = new Color(0.68f, 0.67f, 0.61f) }
            };
            GUI.Label(new Rect(panel.x + 28f * scale, panel.y + 20f * scale, panel.width - 56f * scale, 36f * scale), "[J] fechar", close);

            var contentRect = new Rect(panel.x + 28f * scale, panel.y + 72f * scale, panel.width - 56f * scale, panel.height - 92f * scale);
            var entryWidth = contentRect.width - 28f;
            var body = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = HudTextScale.FontSize(22),
                normal = { textColor = new Color(0.88f, 0.84f, 0.75f) }
            };
            var entryHeights = new float[Entries.Count];
            var contentHeight = 0f;
            for (var i = 0; i < Entries.Count; i++)
            {
                entryHeights[i] = Mathf.Max(HudTextScale.Pixels(48f), body.CalcHeight(new GUIContent(Entries[i]), entryWidth));
                contentHeight += entryHeights[i] + HudTextScale.Pixels(20f);
            }

            contentHeight = Mathf.Max(contentRect.height, contentHeight);
            _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, new Rect(0f, 0f, contentRect.width - 18f, contentHeight));
            var y = 0f;
            for (var i = 0; i < Entries.Count; i++)
            {
                var entryRect = new Rect(0f, y, entryWidth, entryHeights[i]);
                GUI.Label(entryRect, Entries[i], body);
                y += entryHeights[i] + HudTextScale.Pixels(20f);
                if (i < Entries.Count - 1)
                {
                    GUI.color = new Color(0.78f, 0.68f, 0.5f, 0.22f);
                    GUI.Box(new Rect(entryRect.x, y - HudTextScale.Pixels(10f), entryRect.width, HudTextScale.Pixels(1f)), GUIContent.none);
                    GUI.color = Color.white;
                }
            }
            GUI.EndScrollView();
        }
    }
}
