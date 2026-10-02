using System;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Saloon
{
    /// <summary>Connects required saloon evidence to the demo route objective.</summary>
    public sealed class SaloonInvestigationTracker : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private DemoProgressionComponent progression;

        private readonly SaloonInvestigationState _state = new SaloonInvestigationState();
        private bool _objectiveCompleted;
        private bool _isSubscribed;

        public event Action NoteRead;
        public event Action WarningRead;
        public event Action KnifeExamined;
        public event Action InvestigationCompleted;
        public bool IsComplete => _state.IsComplete;
        public bool HasReadNote => _state.HasReadNote;
        public bool HasReadWarning => _state.HasReadWarning;

        public void Configure(PlayerInteractor playerInteractor, DemoProgressionComponent demoProgression)
        {
            Unsubscribe();
            interactor = playerInteractor;
            progression = demoProgression;
            Subscribe();
        }

        private void Awake()
        {
            if (interactor == null)
                interactor = FindFirstObjectByType<PlayerInteractor>();
            if (progression == null)
                progression = FindFirstObjectByType<DemoProgressionComponent>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed || interactor == null)
                return;

            interactor.InteractionCompleted += OnInteractionCompleted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || interactor == null)
                return;

            interactor.InteractionCompleted -= OnInteractionCompleted;
            _isSubscribed = false;
        }

        private void OnInteractionCompleted(string interactionId)
        {
            var wasRecorded = _state.TryRecord(interactionId);
            if (wasRecorded && interactionId == SaloonApparitionState.NoteInteractionId)
                NoteRead?.Invoke();
            if (wasRecorded && interactionId == SaloonInvestigationState.WarningId)
                WarningRead?.Invoke();
            if (interactionId == "saloon.knife")
                KnifeExamined?.Invoke();
            if (!_state.IsComplete || _objectiveCompleted || progression == null)
                return;
            if (!progression.TryComplete(DemoObjective.InvestigateSaloonClues))
                return;

            _objectiveCompleted = true;
            InvestigationCompleted?.Invoke();
        }
    }
}
