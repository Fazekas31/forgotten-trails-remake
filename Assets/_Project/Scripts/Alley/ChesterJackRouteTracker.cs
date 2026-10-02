using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Alley
{
    /// <summary>Connects the alley evidence, Chester encounter, Jack rescue, and sheriff approach.</summary>
    public sealed class ChesterJackRouteTracker : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private PlayerJournalComponent journal;
        [SerializeField] private JackRescueInteractable jack;
        [SerializeField] private ChesterEncounterInteractable chester;

        private readonly ChesterJackRouteState _state = new ChesterJackRouteState();
        private bool _isSubscribed;
        private bool _isProgressionSubscribed;
        private bool _objectiveCompleted;

        public ChesterJackRouteState State => _state;
        public bool IsComplete => _state.IsComplete;

        public void Configure(
            PlayerInteractor playerInteractor,
            DemoProgressionComponent demoProgression,
            PlayerJournalComponent playerJournal,
            JackRescueInteractable dog,
            ChesterEncounterInteractable encounter)
        {
            Unsubscribe();
            interactor = playerInteractor;
            progression = demoProgression;
            journal = playerJournal;
            jack = dog;
            chester = encounter;
            Subscribe();
        }

        private void Awake()
        {
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
            if (progression == null) progression = FindFirstObjectByType<DemoProgressionComponent>();
            if (journal == null) journal = FindFirstObjectByType<PlayerJournalComponent>();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (!_isSubscribed && interactor != null)
            {
                interactor.InteractionCompleted += HandleInteractionCompleted;
                _isSubscribed = true;
            }

            if (!_isProgressionSubscribed && progression != null)
            {
                progression.ObjectiveChanged += OnObjectiveChanged;
                _isProgressionSubscribed = true;
            }

            OnObjectiveChanged(progression != null ? progression.CurrentObjective : DemoObjective.Complete);
        }

        private void Unsubscribe()
        {
            if (_isSubscribed && interactor != null)
            {
                interactor.InteractionCompleted -= HandleInteractionCompleted;
                _isSubscribed = false;
            }

            if (_isProgressionSubscribed && progression != null)
            {
                progression.ObjectiveChanged -= OnObjectiveChanged;
                _isProgressionSubscribed = false;
            }
        }

        public void HandleInteractionCompleted(string interactionId)
        {
            if (_state.TryRecord(interactionId))
            {
                if (interactionId == ChesterJackRouteState.JackRescueInteractionId)
                {
                    jack?.ReactToGunshot();
                    chester?.FallSilent();
                    journal?.Record("CHESTER — O ÚLTIMO TIRO\nO disparo seco ecoa pelo beco. Chester tirou a própria vida atrás da grade; não houve tempo de impedi-lo. Jack ficou sozinho ao meu lado.");
                }
                else if (interactionId == ChesterJackRouteState.JackCalmInteractionId)
                {
                    jack?.BeginFollowing();
                    if (journal != null)
                        journal.Record("JACK — COMPANHEIRO\nJack se acalmou com minha voz e segue logo atrás. Ele fareja o ar antes de cada curva. Preciso encontrar comida para mantê-lo vivo.");
                }
            }

            if (interactionId == ChesterJackRouteState.SheriffOfficeExitInteractionId)
                TryCompleteObjective();
        }

        private void TryCompleteObjective()
        {
            if (!_state.IsComplete || _objectiveCompleted || progression == null)
                return;
            if (!progression.TryComplete(DemoObjective.FollowChesterAndJack))
                return;

            _objectiveCompleted = true;
            if (journal != null)
                journal.Record("NOVA ROTA — DELEGACIA\nJack está com você. Siga pelo acesso lateral e procure o xerife Hale.");
        }

        private void OnObjectiveChanged(DemoObjective currentObjective)
        {
            if (currentObjective == DemoObjective.FollowChesterAndJack)
                TryCompleteObjective();
        }
    }
}
