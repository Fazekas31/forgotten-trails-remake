using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Church
{
    /// <summary>Plays Elias's first-visit dialogue in screenplay order and gives the sheriff's badge.</summary>
    public sealed class ChurchFalseEliasEncounter : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        private static readonly string[] Dialogue =
        {
            "Padre Elias: \"Pare onde está. Deixe-me ver seus olhos... Você ainda está consciente.\"",
            "Protagonista: \"Procuro por Layla. Ela esteve aqui?\"",
            "Padre Elias: \"Layla... Ela tentou aliviar as dores desta cidade, cuidou dos doentes com suas próprias mãos. Mas o fardo de Ash Creek quebra as costas dos inocentes.\"",
            "Protagonista: \"Onde ela está agora?\"",
            "Padre Elias: \"Se quiser respostas, tire esse peso dos meus ombros primeiro. O xerife Hale levou os sobreviventes e trancou o celeiro ao norte. Vá até a delegacia, pegue a chave com ele e traga o registro vermelho de evacuação. Faça o que minhas mãos fracas não podem mais fazer.\"",
            "Padre Elias: \"Mostre isto a Hale e diga: 'O sino ainda toca pelos vivos'. Ele entenderá. E lembre-se: se ouvir alguém chamando seu nome na neblina, não responda.\""
        };

        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private SheriffBadgeInventory badgeInventory;
        [SerializeField] private GameObject badgeVisual;
        private int _dialogueIndex;

        public override string Prompt => IsMissionAvailable
            ? _dialogueIndex == 0 ? "Falar com o padre" : "Continuar conversa"
            : string.Empty;
        public override string InteractionId => ChurchInvestigationState.FalseEliasInteractionId;
        public override string JournalEntry => string.Empty;

        private bool IsMissionAvailable => progression != null
            && progression.CurrentObjective == DemoObjective.ReceiveSheriffMission
            && _dialogueIndex < Dialogue.Length;

        public void Configure(DemoProgressionComponent demoProgression, SheriffBadgeInventory inventory = null, GameObject badge = null)
        {
            progression = demoProgression;
            badgeInventory = inventory;
            badgeVisual = badge;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange || !IsMissionAvailable)
            {
                result = string.Empty;
                return false;
            }

            result = Dialogue[_dialogueIndex++];
            if (_dialogueIndex == Dialogue.Length)
            {
                badgeInventory?.AcquireBadge();
                if (badgeVisual != null)
                    badgeVisual.SetActive(false);
                completedObjective = DemoObjective.ReceiveSheriffMission;
            }
            return true;
        }
    }
}
