using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Barn
{
    public enum BarnInteractionKind
    {
        Door,
        Remains,
        RafterCreature,
        Gideon,
        FinalShot,
        ForestThreshold
    }

    /// <summary>Routes a focused E interaction to the current screenplay beat in the barn.</summary>
    public sealed class BarnInteractionPoint : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private BarnEncounterSequence encounter;
        [SerializeField] private BarnInteractionKind kind;

        public override string Prompt => encounter != null ? encounter.GetPrompt(kind) : string.Empty;
        public override string InteractionId => "barn." + kind.ToString().ToLowerInvariant();

        public void Configure(BarnEncounterSequence sequence, BarnInteractionKind interactionKind)
        {
            encounter = sequence;
            kind = interactionKind;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange || encounter == null)
            {
                result = string.Empty;
                return false;
            }

            return encounter.TryInteract(kind, out result);
        }
    }
}
