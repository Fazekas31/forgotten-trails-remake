namespace ForgottenTrail.Gameplay.Combat
{
    /// <summary>Holds the one revolver round reserved for the demo's final encounter.</summary>
    public sealed class SavingShotInventoryState
    {
        private bool _hasRevolver;
        private int _roundsRemaining;

        public bool HasRevolver => _hasRevolver;
        public int RoundsRemaining => _roundsRemaining;

        public bool TryAcquireRevolver()
        {
            if (_hasRevolver)
                return false;

            _hasRevolver = true;
            _roundsRemaining = 1;
            return true;
        }

        public bool TryFire()
        {
            if (!_hasRevolver || _roundsRemaining <= 0)
                return false;

            _roundsRemaining--;
            return true;
        }
    }
}
