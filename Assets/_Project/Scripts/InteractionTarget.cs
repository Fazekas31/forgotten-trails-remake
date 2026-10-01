using System;

namespace ForgottenTrail.Gameplay.Interaction
{
    /// <summary>Describes one inspectable clue and the player-facing text it reveals.</summary>
    public sealed class InteractionTarget
    {
        public string Id { get; }
        public string Prompt { get; }
        public string Description { get; }
        public float Range { get; }

        public InteractionTarget(string id, string prompt, string description, float range)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("An interaction target needs a stable id.", nameof(id));
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("An interaction target needs a player-facing prompt.", nameof(prompt));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("An interaction target needs a description.", nameof(description));
            if (float.IsNaN(range) || float.IsInfinity(range) || range <= 0f)
                throw new ArgumentOutOfRangeException(nameof(range), "Range must be a finite positive distance.");

            Id = id;
            Prompt = prompt;
            Description = description;
            Range = range;
        }

        public bool CanInteract(float distance)
        {
            return !float.IsNaN(distance)
                && !float.IsInfinity(distance)
                && distance >= 0f
                && distance <= Range;
        }

        public bool TryInteract(float distance, out string description)
        {
            if (!CanInteract(distance))
            {
                description = string.Empty;
                return false;
            }

            description = Description;
            return true;
        }
    }
}
