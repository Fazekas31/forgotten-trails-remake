using System;
using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.Saloon;
using ForgottenTrail.Gameplay.World;
using ForgottenTrail.Gameplay.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenTrail.Gameplay.Player
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private PlayerJournalComponent journal;
        [SerializeField] private float interactionDistance = 2.8f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;

        private PlayerInteractable _focused;
        private float _focusedDistance;
        private string _lastDescription;
        private float _descriptionUntil;

        public event Action<string> InteractionCompleted;
        public string LastInteractionText => _lastDescription;

        public void Configure(Camera camera, DemoProgressionComponent progressionController, PlayerJournalComponent playerJournal = null)
        {
            viewCamera = camera;
            progression = progressionController;
            journal = playerJournal;
        }

        private void Update()
        {
            if (viewCamera == null)
                return;

            _focused = null;
            _focusedDistance = 0f;
            if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out var hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
            {
                var interactable = hit.collider.GetComponentInParent<PlayerInteractable>();
                if (interactable != null && interactable.isActiveAndEnabled)
                {
                    _focused = interactable;
                    _focusedDistance = hit.distance;
                }
            }

            if (_focused == null || (journal != null && journal.IsOpen) || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
                return;

            TryInteract(_focused, _focusedDistance);
        }

        public bool TryInteract(PlayerInteractable target, float distance)
        {
            if (target == null)
                return false;

            if (target is InteractableClue clue && progression != null)
            {
                var objectiveIsNotActive = clue.AdvancesObjective
                    && clue.ObjectiveOnInspect != progression.CurrentObjective;
                var saloonNoteIsEarly = clue.InteractionId == SaloonApparitionState.NoteInteractionId
                    && progression.CurrentObjective != DemoObjective.InvestigateSaloonClues;
                if (objectiveIsNotActive || saloonNoteIsEarly)
                    return false;
            }

            var journalEntry = target.JournalEntry;
            if (!target.TryInteract(distance, out var description, out var completedObjective))
                return false;

            var interactionId = target.InteractionId;
            _lastDescription = description;
            _descriptionUntil = Time.time + 6f;
            if (!string.IsNullOrWhiteSpace(journalEntry) && journal != null)
                journal.Record(journalEntry);
            if (completedObjective.HasValue && progression != null)
                progression.TryComplete(completedObjective.Value);
            InteractionCompleted?.Invoke(interactionId);
            return true;
        }

        private void OnGUI()
        {
            var scale = HudTextScale.Factor;
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var crosshair = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = HudTextScale.FontSize(30),
                normal = { textColor = new Color(0.95f, 0.89f, 0.77f) }
            };
            var crosshairSize = HudTextScale.Pixels(42f);
            GUI.Label(new Rect(center.x - crosshairSize * 0.5f, center.y - crosshairSize * 0.5f, crosshairSize, crosshairSize), "+", crosshair);

            if (_focused != null)
            {
                var prompt = new GUIStyle(crosshair) { fontSize = HudTextScale.FontSize(26), fontStyle = FontStyle.Bold };
                var promptWidth = Mathf.Min(Screen.width - HudTextScale.Pixels(48f), HudTextScale.Pixels(760f));
                var promptRect = new Rect(center.x - promptWidth * 0.5f, center.y + HudTextScale.Pixels(36f), promptWidth, HudTextScale.Pixels(48f));
                GUI.color = new Color(0.055f, 0.06f, 0.075f, 0.78f);
                GUI.Box(promptRect, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(promptRect.x + 12f * scale, promptRect.y + 4f * scale, promptRect.width - 24f * scale, promptRect.height - 8f * scale), "[E]  " + _focused.Prompt, prompt);
            }

            if (!string.IsNullOrEmpty(_lastDescription) && Time.time < _descriptionUntil)
            {
                var panelWidth = Mathf.Min(Screen.width * 0.68f, HudTextScale.Pixels(1120f));
                var details = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = HudTextScale.FontSize(24),
                    normal = { textColor = new Color(0.95f, 0.91f, 0.82f) }
                };
                var contentWidth = panelWidth - HudTextScale.Pixels(56f);
                var contentHeight = details.CalcHeight(new GUIContent(_lastDescription), contentWidth);
                var panelHeight = Mathf.Clamp(contentHeight + HudTextScale.Pixels(40f), HudTextScale.Pixels(112f), Screen.height * 0.44f);
                var panel = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - panelHeight - HudTextScale.Pixels(30f), panelWidth, panelHeight);
                GUI.color = new Color(0.035f, 0.04f, 0.05f, 0.94f);
                GUI.Box(panel, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 28f * scale, panel.y + 18f * scale, panel.width - 56f * scale, panel.height - 36f * scale), _lastDescription, details);
            }
        }
    }
}
