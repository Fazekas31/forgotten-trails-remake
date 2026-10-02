namespace ForgottenTrail.Gameplay.Alley
{
    /// <summary>Gates the blacksmith alley route on its evidence, rescue, and escape beats.</summary>
    public sealed class ChesterJackRouteState
    {
        public const string TrailInteractionId = "alley.chester-jack-trail";
        public const string ChesterInteractionId = "alley.chester";
        public const string GateInteractionId = "alley.iron-gate";
        public const string JackRescueInteractionId = "alley.jack-rescue";
        public const string JackCalmInteractionId = "alley.jack-calm";
        public const string SheriffOfficeExitInteractionId = "route.chester-jack-sheriff-office";

        public bool HasFoundTrail { get; private set; }
        public bool HasMetChester { get; private set; }
        public bool HasOpenedGate { get; private set; }
        public bool HasRescuedJack { get; private set; }
        public bool IsJackFollowing { get; private set; }
        public bool IsComplete { get; private set; }

        public bool TryRecord(string interactionId)
        {
            if (interactionId == TrailInteractionId)
            {
                if (HasFoundTrail)
                    return false;
                HasFoundTrail = true;
                return true;
            }

            if (interactionId == ChesterInteractionId)
            {
                if (!HasFoundTrail || HasMetChester)
                    return false;
                HasMetChester = true;
                return true;
            }

            if (interactionId == GateInteractionId)
            {
                if (!HasMetChester || HasOpenedGate)
                    return false;
                HasOpenedGate = true;
                return true;
            }

            if (interactionId == JackRescueInteractionId)
            {
                if (!HasOpenedGate || HasRescuedJack)
                    return false;
                HasRescuedJack = true;
                return true;
            }

            if (interactionId == JackCalmInteractionId)
            {
                if (!HasRescuedJack || IsJackFollowing)
                    return false;
                IsJackFollowing = true;
                return true;
            }

            if (interactionId != SheriffOfficeExitInteractionId
                || !IsJackFollowing
                || IsComplete)
                return false;

            IsComplete = true;
            return true;
        }

    }
}
