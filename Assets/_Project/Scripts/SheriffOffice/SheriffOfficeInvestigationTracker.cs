using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Connects Layla's evidence, Hale's cell, the Red Book, and the return route to the campaign.</summary>
    public sealed class SheriffOfficeInvestigationTracker : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private SheriffOfficeEscapeSequence escapeSequence;

        private readonly SheriffOfficeInvestigationState _state = new SheriffOfficeInvestigationState();
        private bool _isInteractionSubscribed;
        private bool _isProgressionSubscribed;
        private bool _objectiveCompleted;

        public SheriffOfficeInvestigationState State => _state;

        public void Configure(
            PlayerInteractor playerInteractor,
            DemoProgressionComponent demoProgression,
            SheriffOfficeEscapeSequence officeEscapeSequence = null)
        {
            Unsubscribe();
            interactor = playerInteractor;
            progression = demoProgression;
            escapeSequence = officeEscapeSequence;
            Subscribe();
        }

        private void Awake()
        {
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
            if (progression == null) progression = FindFirstObjectByType<DemoProgressionComponent>();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (!_isInteractionSubscribed && interactor != null)
            {
                interactor.InteractionCompleted += HandleInteractionCompleted;
                _isInteractionSubscribed = true;
            }

            if (!_isProgressionSubscribed && progression != null)
            {
                progression.ObjectiveChanged += HandleObjectiveChanged;
                _isProgressionSubscribed = true;
            }

            HandleObjectiveChanged(progression != null ? progression.CurrentObjective : DemoObjective.Complete);
        }

        private void Unsubscribe()
        {
            if (_isInteractionSubscribed && interactor != null)
            {
                interactor.InteractionCompleted -= HandleInteractionCompleted;
                _isInteractionSubscribed = false;
            }

            if (_isProgressionSubscribed && progression != null)
            {
                progression.ObjectiveChanged -= HandleObjectiveChanged;
                _isProgressionSubscribed = false;
            }
        }

        public void HandleInteractionCompleted(string interactionId)
        {
            if (interactionId == SheriffOfficeInvestigationState.QuietEscapeInteractionId
                && (escapeSequence == null || !escapeSequence.HasEscaped))
                return;

            if (_state.TryRecord(interactionId))
            {
                if (interactionId == SheriffOfficeInvestigationState.RedBookInteractionId)
                    escapeSequence?.Begin();
                TryCompleteObjective();
            }
        }

        private void HandleObjectiveChanged(DemoObjective currentObjective)
        {
            if (currentObjective == DemoObjective.SearchSheriffOffice)
                TryCompleteObjective();
        }

        private void TryCompleteObjective()
        {
            if (!_state.IsComplete || _objectiveCompleted || progression == null)
                return;
            if (!progression.TryComplete(DemoObjective.SearchSheriffOffice))
                return;

            _objectiveCompleted = true;
        }
    }
}
