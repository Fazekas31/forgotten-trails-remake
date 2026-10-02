using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Church
{
    /// <summary>Shows the scripted church encounter on arrival and its revelation only after the sheriff's office.</summary>
    public sealed class ChurchInvestigationTracker : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private GameObject firstVisitElias;
        [SerializeField] private GameObject returnReveal;
        [SerializeField] private ChurchFalseEliasEncounter missionEncounter;
        [SerializeField] private ChurchReturnRevealInteractable returnEncounter;
        [SerializeField] private SheriffBadgeInventory badgeInventory;
        [SerializeField] private GameObject badgeVisual;
        [SerializeField] private Light altarLight;
        [SerializeField] private GameObject altarFlame;

        private bool _isInteractionSubscribed;
        private bool _isProgressionSubscribed;

        public void Configure(
            PlayerInteractor playerInteractor,
            DemoProgressionComponent demoProgression,
            GameObject falseElias,
            GameObject returnSetpiece,
            SheriffBadgeInventory inventory = null,
            GameObject badge = null)
        {
            Unsubscribe();
            interactor = playerInteractor;
            progression = demoProgression;
            firstVisitElias = falseElias;
            returnReveal = returnSetpiece;
            badgeInventory = inventory;
            badgeVisual = badge;
            missionEncounter = falseElias != null ? falseElias.GetComponent<ChurchFalseEliasEncounter>() : null;
            returnEncounter = returnSetpiece != null ? returnSetpiece.GetComponentInChildren<ChurchReturnRevealInteractable>(true) : null;

            missionEncounter?.Configure(progression, badgeInventory, badgeVisual);
            returnEncounter?.Configure(progression);
            Subscribe();
            ApplyObjective(progression != null ? progression.CurrentObjective : DemoObjective.Complete);
        }

        private void Awake()
        {
            if (interactor == null)
                interactor = FindFirstObjectByType<PlayerInteractor>();
            if (progression == null)
                progression = FindFirstObjectByType<DemoProgressionComponent>();
            if (badgeInventory == null)
                badgeInventory = FindFirstObjectByType<SheriffBadgeInventory>();
            if (firstVisitElias == null)
            {
                var actor = transform.Find("False Elias");
                firstVisitElias = actor != null ? actor.gameObject : null;
            }
            if (returnReveal == null)
            {
                var reveal = transform.Find("Church return — Elias and the warning");
                returnReveal = reveal != null ? reveal.gameObject : null;
            }
            if (missionEncounter == null && firstVisitElias != null)
                missionEncounter = firstVisitElias.GetComponent<ChurchFalseEliasEncounter>();
            if (badgeVisual == null && firstVisitElias != null)
            {
                var badge = firstVisitElias.transform.Find("Church — Elias's sheriff badge");
                badgeVisual = badge != null ? badge.gameObject : null;
            }
            if (returnEncounter == null && returnReveal != null)
                returnEncounter = returnReveal.GetComponentInChildren<ChurchReturnRevealInteractable>(true);
            if (altarLight == null)
                altarLight = GetComponentInChildren<Light>(true);
            if (altarFlame == null)
            {
                var flame = transform.Find("Church — amber altar flame");
                altarFlame = flame != null ? flame.gameObject : null;
            }

            missionEncounter?.Configure(progression, badgeInventory, badgeVisual);
            returnEncounter?.Configure(progression);
        }

        private void OnEnable()
        {
            Subscribe();
            ApplyObjective(progression != null ? progression.CurrentObjective : DemoObjective.Complete);
        }

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
                progression.ObjectiveChanged += ApplyObjective;
                _isProgressionSubscribed = true;
            }
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
                progression.ObjectiveChanged -= ApplyObjective;
                _isProgressionSubscribed = false;
            }
        }

        private void HandleInteractionCompleted(string interactionId)
        {
            if (interactionId == ChurchInvestigationState.ReturnRevealInteractionId
                && progression != null
                && progression.CurrentObjective == DemoObjective.DiscoverChurchTruth)
                progression.TryComplete(DemoObjective.DiscoverChurchTruth);
        }

        private void ApplyObjective(DemoObjective currentObjective)
        {
            if (firstVisitElias != null)
                firstVisitElias.SetActive(currentObjective == DemoObjective.ReceiveSheriffMission);
            if (returnReveal != null)
                returnReveal.SetActive(HasReturnedToChurch(currentObjective));
            var hasReturned = HasReturnedToChurch(currentObjective);
            if (altarLight != null)
                altarLight.enabled = !hasReturned;
            if (altarFlame != null)
                altarFlame.SetActive(!hasReturned);
        }

        private static bool HasReturnedToChurch(DemoObjective objective)
        {
            return objective == DemoObjective.ReturnToChurch
                || objective == DemoObjective.DiscoverChurchTruth
                || objective == DemoObjective.ConfrontCreatureInBarn
                || objective == DemoObjective.ReachForest
                || objective == DemoObjective.Complete;
        }
    }
}
