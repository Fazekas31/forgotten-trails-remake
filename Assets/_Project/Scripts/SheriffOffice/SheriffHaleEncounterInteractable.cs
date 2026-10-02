using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Lets Hale recount the sealed town after Jack identifies Layla's bloodied handkerchief.</summary>
    public sealed class SheriffHaleEncounterInteractable : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private SheriffOfficeInvestigationTracker tracker;

        public override string Prompt => tracker == null || !tracker.State.HasLaylaEvidence
            ? "Aguardar a pista do lenço"
            : tracker.State.HasMetHale
                ? tracker.State.HasHeardHaleAccount ? string.Empty : "Perguntar sobre Layla"
                : "Mostrar o distintivo do delegado";
        public override string InteractionId => tracker != null && tracker.State.HasMetHale
            ? SheriffOfficeInvestigationState.HaleAccountInteractionId
            : SheriffOfficeInvestigationState.HaleInteractionId;

        public void Configure(SheriffOfficeInvestigationTracker investigation) => tracker = investigation;

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (tracker == null || !tracker.State.HasLaylaEvidence)
            {
                result = string.Empty;
                return false;
            }

            if (!tracker.State.HasMetHale)
            {
                result = "Protagonista: \"O sino ainda toca pelos vivos. Elias me mandou.\"\nXerife Hale: \"Elias morreu há três dias.\"";
                return true;
            }

            if (!tracker.State.HasHeardHaleAccount)
            {
                result = "Protagonista: \"Como assim? Falei com ele no altar! E Layla? O que aconteceu com ela?\"\nXerife Hale: \"Todo mundo veio bater na minha porta pedindo socorro e abrigo. Quando o mal subiu da mina, adivinha quem teve que fechar o cadeado? Eu tranquei o celeiro para impedir que aquilo saísse, não para proteger quem ficou dentro. Layla achou que podia salvar todo mundo. Tome a chave. Suba, leia o livro vermelho e assuma a responsabilidade se abrir aquela porta.\"";
                return true;
            }

            result = string.Empty;
            return true;
        }
    }
}
