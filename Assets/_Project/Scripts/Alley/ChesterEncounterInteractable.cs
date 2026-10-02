using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Alley
{
    /// <summary>Lets the player meet Chester through the bars before receiving the iron-gate key.</summary>
    public sealed class ChesterEncounterInteractable : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private ChesterJackRouteTracker route;
        [SerializeField] private GameObject livingPose;
        [SerializeField] private GameObject fallenPose;
        [SerializeField] private Collider standingCollider;
        private bool _hasFallenSilent;
        private int _dialogueIndex;

        private static readonly string[] Dialogue =
        {
            "Chester: \"Você é real... ou é outra daquelas vozes da montanha zombando de mim? Diga que você é de carne e osso!\"\nProtagonista: \"Sou real. Preciso passar para a delegacia. Procuro por Layla.\"",
            "Chester: \"Layla foi pro celeiro tentar curar quem já estava condenado... Ninguém volta de lá! E eu não vou esperar aquelas coisas quebrarem essa porta. O silêncio dessa cidade tá comendo minha cabeça há dias.\"\nProtagonista: \"Guarde essa arma, Chester. Me ajude com a tranca e saia comigo.\"",
            "Chester: \"Não tem mais estrada pra mim. Mas o Jack... ele não tem culpa da nossa ruína! Ele é forte, tem pernas e dentes. Mas fiquei sem comida... e se a barriga dele roncar, ele vai chorar no escuro. E se ele chorar, aquelas coisas descem e rasgam ele em pedaços! Eu não posso ver isso acontecer!\"",
            "Chester: \"Prometa pra mim! Jure por quem você veio buscar! Cuide dele. Ache comida nos armários, divida seus mantimentos... mas não deixe meu garoto passar fome nem ganir na noite. Tire esse peso de mim... por favor. Leve o Jack!\"\nProtagonista: \"Eu prometo, Chester. Eu cuido dele.\"\nChester: \"Obrigado, forasteiro... Agora passe e não olhe pra trás.\""
        };

        public override string Prompt => route == null || route.State.HasMetChester
            ? string.Empty
            : _dialogueIndex == 0 ? "Falar com Chester" : "Continuar conversa";
        public override string InteractionId => route != null && !route.State.HasMetChester && _dialogueIndex >= Dialogue.Length
            ? ChesterJackRouteState.ChesterInteractionId
            : ChesterJackRouteState.ChesterDialogueAdvanceInteractionId;

        private void Awake()
        {
            if (standingCollider == null)
                standingCollider = GetComponent<Collider>();
        }

        public void Configure(ChesterJackRouteTracker tracker, GameObject livingFigure, GameObject fallenFigure)
        {
            route = tracker;
            livingPose = livingFigure;
            fallenPose = fallenFigure;
            if (standingCollider == null)
                standingCollider = GetComponent<Collider>();
            if (fallenPose != null) fallenPose.SetActive(false);
        }

        public void FallSilent()
        {
            if (_hasFallenSilent)
                return;

            _hasFallenSilent = true;
            if (livingPose != null) livingPose.SetActive(false);
            if (fallenPose != null) fallenPose.SetActive(true);
            if (standingCollider == null)
                standingCollider = GetComponent<Collider>();
            if (standingCollider != null) standingCollider.enabled = false;
            enabled = false;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (route == null || !route.State.HasFoundTrail)
            {
                result = string.Empty;
                return false;
            }

            if (route.State.HasMetChester || _dialogueIndex >= Dialogue.Length)
            {
                result = string.Empty;
                return false;
            }

            result = Dialogue[_dialogueIndex++];
            return true;
        }
    }
}
