using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Alley
{
    /// <summary>Opens Chester's reinforced gate once he has trusted the player with his key.</summary>
    public sealed class BlacksmithIronGate : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private ChesterJackRouteTracker route;
        [SerializeField] private Transform swingingLeaf;
        [SerializeField] private FirstPersonController player;
        private Quaternion _closedRotation;
        private bool _isOpen;

        public override string Prompt => _isOpen ? "Examinar a grade aberta" : "Abrir a grade de ferro";
        public override string InteractionId => ChesterJackRouteState.GateInteractionId;
        public bool IsOpen => _isOpen;

        public void Configure(ChesterJackRouteTracker routeTracker, Transform gateLeaf, FirstPersonController playerController)
        {
            route = routeTracker;
            swingingLeaf = gateLeaf;
            player = playerController;
            CacheClosedRotation();
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (route == null || !route.State.HasMetChester)
            {
                result = "A corrente prende a grade reforçada. Um homem sussurra do outro lado: 'Fale comigo antes de tentar abrir.'";
                return true;
            }

            if (_isOpen)
            {
                result = "A grade de ferro está aberta. Jack fareja a saída entre as barras.";
                return true;
            }

            _isOpen = true;
            if (swingingLeaf != null)
                swingingLeaf.localRotation = _closedRotation * Quaternion.Euler(0f, -92f, 0f);
            if (player != null)
                player.EmitNoise(transform.position, 0.76f);
            result = "A chave de Chester gira com dificuldade. A grade range e abre para o beco.";
            return true;
        }

        private void Awake() => CacheClosedRotation();

        private void Start()
        {
            if (player == null)
                player = FindFirstObjectByType<FirstPersonController>();
        }

        private void CacheClosedRotation()
        {
            if (swingingLeaf != null)
                _closedRotation = swingingLeaf.localRotation;
        }
    }
}
