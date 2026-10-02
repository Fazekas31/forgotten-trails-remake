namespace ForgottenTrail.Gameplay.Combat
{
    /// <summary>Holds Gideon's .38 revolver and six rounds given at the barn.</summary>
    public sealed class SavingShotInventoryState
    {
        public const int BarnRevolverRoundCount = 6;
        private bool _hasRevolver;
        private int _roundsRemaining;

        public bool HasRevolver => _hasRevolver;
        public int RoundsRemaining => _roundsRemaining;

        public bool TryAcquireRevolver(int rounds = BarnRevolverRoundCount)
        {
            if (_hasRevolver || rounds <= 0 || rounds > BarnRevolverRoundCount)
                return false;

            _hasRevolver = true;
            _roundsRemaining = rounds;
            return true;
        }

        public bool TryFire()
        {
            if (!_hasRevolver || _roundsRemaining <= 0)
                return false;

            _roundsRemaining--;
            return true;
        }

        public bool TryFireAtTarget(bool targetIsReady)
        {
            return targetIsReady && TryFire();
        }
    }
}
