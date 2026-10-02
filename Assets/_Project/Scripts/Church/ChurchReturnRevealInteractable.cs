using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Church
{
    /// <summary>Reveals the dead Elias, his warning, and Layla's voice when the player returns from the office.</summary>
    public sealed class ChurchReturnRevealInteractable : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;
        private const string ActOneDiary = "O padre que me deu essa missão já estava morto antes de meus pés tocarem esta cidade. A coisa que roubou sua voz me usou para conseguir a chave do celeiro. O xerife diz que trancou o mal lá dentro; o registro diz que Layla está lá. Jack rosna para a estrada da colina. Não sei se Layla ainda é humana, mas carregarei esse fardo até a última porta.";

        private static readonly string[] ReturnSequence =
        {
            "As coisas da mina aprenderam nossas vozes. Elas não enxergam, mas imitam os mortos para nos atrair. Não respondam quando chamarem seus nomes. Não abram o celeiro. Perdoe-me.",
            "Voz do Falso Padre: \"Você pegou a chave, forasteiro? Tire o peso de nós... Abra o celeiro...\"",
            "Voz de Layla: \"Tire esse peso de mim... Estou no celeiro...\"",
            ActOneDiary
        };

        [SerializeField] private DemoProgressionComponent progression;
        private int _sequenceIndex;

        public override string Prompt => IsAvailable
            ? _sequenceIndex == 0 ? "Examinar o bilhete" : "Continuar"
            : string.Empty;
        public override string InteractionId => ChurchInvestigationState.ReturnRevealInteractionId;
        public override string JournalEntry => _sequenceIndex == ReturnSequence.Length - 1 ? ActOneDiary : string.Empty;

        private bool IsAvailable => progression != null
            && progression.CurrentObjective == DemoObjective.ReturnToChurch
            && _sequenceIndex < ReturnSequence.Length;

        public void Configure(DemoProgressionComponent demoProgression)
        {
            progression = demoProgression;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange || !IsAvailable)
            {
                result = string.Empty;
                return false;
            }

            result = ReturnSequence[_sequenceIndex++];
            if (_sequenceIndex == ReturnSequence.Length)
                completedObjective = DemoObjective.ReturnToChurch;
            return true;
        }
    }
}
