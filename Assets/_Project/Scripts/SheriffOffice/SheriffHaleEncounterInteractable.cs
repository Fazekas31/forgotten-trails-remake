using System.Collections;
using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Plays Hale's two screenplay exchanges before he gives the barn key.</summary>
    public sealed class SheriffHaleEncounterInteractable : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private SheriffOfficeInvestigationTracker tracker;
        [SerializeField] private SheriffBadgeInventory badgeInventory;
        [SerializeField] private Transform revolver;
        private bool _isLoweringWeapon;

        public override string Prompt => tracker == null || badgeInventory == null || !badgeInventory.HasBadge || !tracker.State.HasLaylaEvidence
            ? string.Empty
            : tracker.State.HasMetHale
                ? tracker.State.HasHeardHaleAccount ? string.Empty : "Perguntar sobre Layla"
                : "Dizer o recado de Elias";
        public override string InteractionId => tracker != null && tracker.State.HasMetHale
            ? SheriffOfficeInvestigationState.HaleAccountInteractionId
            : SheriffOfficeInvestigationState.HaleInteractionId;

        public void Configure(SheriffOfficeInvestigationTracker investigation, SheriffBadgeInventory inventory, Transform heldRevolver = null)
        {
            tracker = investigation;
            badgeInventory = inventory;
            revolver = heldRevolver;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (tracker == null || badgeInventory == null || !badgeInventory.HasBadge || !tracker.State.HasLaylaEvidence)
            {
                result = string.Empty;
                return false;
            }

            if (!tracker.State.HasMetHale)
            {
                result = "Protagonista: \"O sino ainda toca pelos vivos. Elias me mandou.\"\nXerife Hale (abaixa a arma lentamente): \"Elias morreu há três dias.\"";
                if (revolver != null && !_isLoweringWeapon)
                {
                    if (Application.isPlaying)
                        StartCoroutine(LowerWeapon());
                    else
                        revolver.localRotation *= Quaternion.Euler(36f, 0f, 0f);
                }
                return true;
            }

            if (!tracker.State.HasHeardHaleAccount)
            {
                result = "Protagonista: \"Como assim? Falei com ele no altar! E Layla? O que aconteceu com ela?\"\nXerife Hale: \"Todo mundo veio bater na minha porta pedindo socorro e abrigo. Quando o mal subiu da mina, adivinha quem teve que fechar o cadeado? Eu tranquei o celeiro para impedir que aquilo saísse, não para proteger quem ficou dentro. Layla achou que podia salvar todo mundo. Tome a chave. Suba, leia o livro vermelho e assuma a responsabilidade se abrir aquela porta.\"";
                return true;
            }

            result = string.Empty;
            return false;
        }

        private IEnumerator LowerWeapon()
        {
            _isLoweringWeapon = true;
            var startRotation = revolver.localRotation;
            var endRotation = startRotation * Quaternion.Euler(36f, 0f, 0f);
            const float duration = 0.65f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                revolver.localRotation = Quaternion.Slerp(startRotation, endRotation, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            revolver.localRotation = endRotation;
            _isLoweringWeapon = false;
        }

    }
}
