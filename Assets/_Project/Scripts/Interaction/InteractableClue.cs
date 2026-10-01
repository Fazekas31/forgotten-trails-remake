using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.World
{
    public sealed class InteractableClue : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private string prompt = "Examinar";
        [TextArea(2, 5)]
        [SerializeField] private string description;
        [SerializeField] private float range = 2.2f;
        [SerializeField] private bool advancesObjective;
        [SerializeField] private DemoObjective objectiveOnInspect;

        private InteractionTarget _target;

        public bool AdvancesObjective => advancesObjective;
        public DemoObjective ObjectiveOnInspect => objectiveOnInspect;
        public string Prompt => Target.Prompt;

        private InteractionTarget Target
        {
            get
            {
                if (_target == null)
                    _target = CreateTarget();
                return _target;
            }
        }

        public void Configure(string targetId, string targetPrompt, string targetDescription, float interactionRange, bool advances, DemoObjective objective)
        {
            id = targetId;
            prompt = targetPrompt;
            description = targetDescription;
            range = interactionRange;
            advancesObjective = advances;
            objectiveOnInspect = objective;
            _target = CreateTarget();
        }

        public bool TryInteract(float distance, out string result)
        {
            return Target.TryInteract(distance, out result);
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
