using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.SheriffOffice;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Barn
{
    /// <summary>Grants the knife and barn key only after their exact screenplay interactions.</summary>
    public sealed class ScreenplayItemGrantTracker : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private CombatKnifeInventory knifeInventory;
        [SerializeField] private BarnKeyInventory barnKeyInventory;
        [SerializeField] private GameObject knifeVisual;
        [SerializeField] private GameObject barnKeyVisual;

        private bool _isSubscribed;

        public void Configure(
            PlayerInteractor playerInteractor,
            CombatKnifeInventory knife,
            BarnKeyInventory key,
            GameObject knifeProp = null,
            GameObject keyProp = null)
        {
            Unsubscribe();
            interactor = playerInteractor;
            knifeInventory = knife;
            barnKeyInventory = key;
            knifeVisual = knifeProp;
            barnKeyVisual = keyProp;
            Subscribe();
        }

        private void Awake()
        {
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (knifeInventory == null) knifeInventory = GetComponent<CombatKnifeInventory>();
            if (barnKeyInventory == null) barnKeyInventory = GetComponent<BarnKeyInventory>();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_isSubscribed || interactor == null)
                return;

            interactor.InteractionCompleted += HandleInteractionCompleted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || interactor == null)
                return;

            interactor.InteractionCompleted -= HandleInteractionCompleted;
            _isSubscribed = false;
        }

        private void HandleInteractionCompleted(string interactionId)
        {
            if (interactionId == "saloon.knife")
            {
                if (knifeInventory != null && knifeInventory.AcquireKnife() && knifeVisual != null)
                    knifeVisual.SetActive(false);
                return;
            }

            if (interactionId == SheriffOfficeInvestigationState.HaleAccountInteractionId
                && barnKeyInventory != null
                && barnKeyInventory.AcquireKey()
                && barnKeyVisual != null)
                barnKeyVisual.SetActive(false);
        }
    }
}
