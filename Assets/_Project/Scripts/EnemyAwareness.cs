using System;

namespace ForgottenTrail.Gameplay.Awareness
{
    public enum EnemyAlertState
    {
        Calm,
        Suspicious,
        Alerted
    }

    /// <summary>Combines heard noise with sustained line of sight into a readable alert state.</summary>
    public sealed class EnemyAwareness
    {
        private readonly float _hearingRange;
        private readonly float _visualAlertSeconds;
        private readonly float _suspicionDecayPerSecond;

        public EnemyAlertState State { get; private set; } = EnemyAlertState.Calm;
        public float Suspicion { get; private set; }

        public EnemyAwareness(float hearingRange, float visualAlertSeconds, float suspicionDecayPerSecond)
        {
            ValidatePositiveFinite(hearingRange, nameof(hearingRange));
            ValidatePositiveFinite(visualAlertSeconds, nameof(visualAlertSeconds));
            ValidatePositiveFinite(suspicionDecayPerSecond, nameof(suspicionDecayPerSecond));

            _hearingRange = hearingRange;
            _visualAlertSeconds = visualAlertSeconds;
            _suspicionDecayPerSecond = suspicionDecayPerSecond;
        }

        public bool HearNoise(float distance, float intensity)
        {
            if (!IsFinite(distance) || distance < 0f || !IsFinite(intensity) || intensity <= 0f || intensity > 1f)
                return false;

            var effectiveRange = _hearingRange * intensity;
            if (distance > effectiveRange)
                return false;

            var proximity = 1f - distance / effectiveRange;
            SetSuspicion(Math.Max(Suspicion, 0.25f + proximity * 0.2f));
            return true;
        }

        public void Tick(float elapsedSeconds, bool hasLineOfSight)
        {
            ValidateNonNegativeFinite(elapsedSeconds, nameof(elapsedSeconds));

            if (State == EnemyAlertState.Alerted || elapsedSeconds == 0f)
                return;

            var nextSuspicion = hasLineOfSight
                ? Suspicion + elapsedSeconds / _visualAlertSeconds
                : Suspicion - elapsedSeconds * _suspicionDecayPerSecond;

            SetSuspicion(Math.Max(0f, Math.Min(1f, nextSuspicion)));
        }

        private void SetSuspicion(float value)
        {
            Suspicion = Math.Max(0f, Math.Min(1f, value));
            State = Suspicion >= 1f
                ? EnemyAlertState.Alerted
                : Suspicion > 0f ? EnemyAlertState.Suspicious : EnemyAlertState.Calm;
        }

        private static void ValidatePositiveFinite(float value, string parameterName)
        {
            if (!IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and greater than zero.");
        }

        private static void ValidateNonNegativeFinite(float value, string parameterName)
        {
            if (!IsFinite(value) || value < 0f)
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and non-negative.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
