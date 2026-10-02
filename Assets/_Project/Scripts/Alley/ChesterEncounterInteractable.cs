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

        public override string Prompt => route != null && route.State.HasMetChester ? "Chester permanece em silêncio" : "Falar com Chester";
        public override string InteractionId => ChesterJackRouteState.ChesterInteractionId;
        public override string JournalEntry => route != null && !route.State.HasMetChester
            ? "CHESTER — ATRÁS DA GRADE\nChester está em choque, com um único cartucho no revólver. Ele não quer sair do beco, mas pede que Jack não seja deixado para trás."
            : string.Empty;

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
                result = "Chester segura a chave, mas não responde às perguntas. As marcas no chão parecem levar até ele.";
                return true;
            }

            if (route.State.HasMetChester)
            {
                result = "Chester encara o chão, sem forças para repetir o que contou. A chave da grade está ao seu alcance.";
                return true;
            }

            result = "Chester pergunta se você é real. Ele viu as criaturas seguirem o som e entrega a chave da grade. Ao lado, Jack fareja o beco.";
            return true;
        }
    }
}
