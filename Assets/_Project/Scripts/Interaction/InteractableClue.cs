using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.World
{
    public sealed class InteractableClue : PlayerInteractable
    {
        [SerializeField] private string id;
        [SerializeField] private string prompt = "Examinar";
        [TextArea(2, 5)]
        [SerializeField] private string description;
        [TextArea(1, 3)]
        [SerializeField] private string journalEntry;
        [SerializeField] private float range = 2.2f;
        [SerializeField] private bool advancesObjective;
        [SerializeField] private DemoObjective objectiveOnInspect;

        private InteractionTarget _target;

        public string Id => Target.Id;
        public override string InteractionId => Id;
        public bool AdvancesObjective => advancesObjective;
        public DemoObjective ObjectiveOnInspect => objectiveOnInspect;
        public override string Prompt => Target.Prompt;
        public override string JournalEntry => journalEntry;

        private InteractionTarget Target
        {
            get
            {
                if (_target == null)
                    _target = CreateTarget();
                return _target;
            }
        }

        public void Configure(string targetId, string targetPrompt, string targetDescription, float interactionRange, bool advances, DemoObjective objective, string entry = "")
        {
            id = targetId;
            prompt = targetPrompt;
            description = targetDescription;
            journalEntry = entry;
            range = interactionRange;
            advancesObjective = advances;
            objectiveOnInspect = objective;
            _target = CreateTarget();
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            var interacted = Target.TryInteract(distance, out result);
            completedObjective = interacted && advancesObjective ? (DemoObjective?)objectiveOnInspect : null;
            return interacted;
        }

        private void Awake()
        {
            _target = CreateTarget();
        }

        private InteractionTarget CreateTarget()
        {
            var targetId = string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
            var targetPrompt = string.IsNullOrWhiteSpace(prompt) ? "Examinar" : prompt;
            var targetDescription = string.IsNullOrWhiteSpace(description) ? gameObject.name : description;
            return new InteractionTarget(targetId, targetPrompt, targetDescription, range);
        }
    }
}
