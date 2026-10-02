using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Church
{
    /// <summary>Confronting Elias only exposes the contradiction after the player has examined the evidence.</summary>
    public sealed class ChurchFalseEliasEncounter : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private ChurchInvestigationTracker investigation;

        public override string Prompt => investigation != null && investigation.CanConfrontFalseElias
            ? "Confrontar o pastor"
            : "Observar o pastor";

        public override string InteractionId => ChurchInvestigationState.FalseEliasInteractionId;

        public override string JournalEntry => investigation != null && investigation.CanConfrontFalseElias
            ? "IGREJA — O FALSO ELIAS\nA corda cortada não poderia ter tocado o sino. O livro registra Elias como morto há anos. Sob a luz do altar, ele não projeta sombra. O homem diante de você não é o verdadeiro Elias."
            : string.Empty;

        public void Configure(ChurchInvestigationTracker churchInvestigation)
        {
            investigation = churchInvestigation;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (investigation == null || !investigation.CanConfrontFalseElias)
            {
                result = "Elias diz que puxou a corda. O cabo do sino termina em fibras rompidas; examine a luz do altar e o livro paroquial antes de acusá-lo.";
                return true;
            }

            result = "A corda está cortada, o registro marca Elias como morto e a luz não encontra a sombra dele. Você o chama de impostor.";
            return true;
        }
    }
}
