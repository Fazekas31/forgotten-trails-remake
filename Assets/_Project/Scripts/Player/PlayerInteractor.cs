using System;
using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Progression;
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
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var crosshair = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                normal = { textColor = new Color(0.9f, 0.83f, 0.69f) }
            };
            GUI.Label(new Rect(center.x - 12f, center.y - 14f, 24f, 28f), "+", crosshair);

            if (_focused != null)
            {
                var prompt = new GUIStyle(crosshair) { fontSize = 16, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(center.x - 180f, center.y + 28f, 360f, 30f), "[E]  " + _focused.Prompt, prompt);
            }

            if (!string.IsNullOrEmpty(_lastDescription) && Time.time < _descriptionUntil)
            {
                var panel = new Rect(Screen.width * 0.22f, Screen.height - 136f, Screen.width * 0.56f, 88f);
                GUI.color = new Color(0.08f, 0.075f, 0.065f, 0.9f);
                GUI.Box(panel, GUIContent.none);
                GUI.color = Color.white;
                var details = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = 15,
                    normal = { textColor = new Color(0.91f, 0.86f, 0.74f) }
                };
                GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, panel.height - 24f), _lastDescription, details);
            }
        }
    }
}
