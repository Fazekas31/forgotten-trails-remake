namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Rules for slipping past the blind creature after the Red Book reveals the truth.</summary>
    public sealed class SheriffOfficeEscapeState
    {
        public bool HasStarted { get; private set; }
        public bool HasEscaped { get; private set; }

        public bool Begin()
        {
            if (HasStarted || HasEscaped)
                return false;

            HasStarted = true;
            return true;
        }

        public bool TryEscape(bool isCrouching, bool lanternIsLit, bool jackIsQuiet)
        {
            if (!HasStarted || HasEscaped || !isCrouching || lanternIsLit || !jackIsQuiet)
                return false;

            HasEscaped = true;
            return true;
        }
    }
}
