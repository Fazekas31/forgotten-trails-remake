using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Collects Hale's Red Book and records the evidence that points toward the barn.</summary>
    public sealed class RedBookInteractable : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private SheriffOfficeInvestigationTracker tracker;
        private bool _isCollected;
        private const string RecordText = "Padre Elias — Falecido.\nLAYLA — Transferida para o celeiro. Condição: consciente.\nNota: 'Não deixem que ela desça novamente à mina.'";

        public override string Prompt => tracker == null || !tracker.State.HasHeardHaleAccount
            ? string.Empty
            : _isCollected ? "Reler o Livro Vermelho" : "Examinar o Livro Vermelho";
        public override string InteractionId => SheriffOfficeInvestigationState.RedBookInteractionId;
        public override string JournalEntry => string.Empty;

        public void Configure(SheriffOfficeInvestigationTracker investigation) => tracker = investigation;

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (tracker == null || !tracker.State.HasHeardHaleAccount)
            {
                result = string.Empty;
                return false;
            }

            if (_isCollected)
            {
                result = RecordText;
                return true;
            }

            _isCollected = true;
            result = RecordText;
            return true;
        }
    }
}
