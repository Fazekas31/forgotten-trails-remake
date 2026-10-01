using ForgottenTrail.Gameplay.Awareness;
using ForgottenTrail.Gameplay.Player;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Enemies
{
    public sealed class EnemyAwarenessAgent : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Transform eye;
        [SerializeField] private float hearingRange = 11f;
        [SerializeField] private float viewDistance = 17f;
        [SerializeField, Range(10f, 160f)] private float fieldOfView = 72f;
        [SerializeField] private float secondsOfSightToAlert = 2.2f;
        [SerializeField] private float suspicionDecayPerSecond = 0.3f;

        private EnemyAwareness _awareness;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _propertyBlock;
        private EnemyAlertState _lastState;

        public EnemyAlertState State => _awareness == null ? EnemyAlertState.Calm : _awareness.State;
        public float Suspicion => _awareness == null ? 0f : _awareness.Suspicion;

        public void Configure(FirstPersonController playerController, Transform eyePoint)
        {
            player = playerController;
            eye = eyePoint;
        }

        private void Awake()
        {
            _awareness = new EnemyAwareness(hearingRange, secondsOfSightToAlert, suspicionDecayPerSecond);
            _renderers = GetComponentsInChildren<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            ApplyStateColor();
        }

        private void Start()
        {
            if (player == null)
                player = FindFirstObjectByType<FirstPersonController>();
            if (player != null)
                player.NoiseEmitted += OnNoiseEmitted;
        }

        private void OnDestroy()
        {
            if (player != null)
                player.NoiseEmitted -= OnNoiseEmitted;
        }

        private void Update()
        {
            if (_awareness == null || player == null)
                return;

            _awareness.Tick(Time.deltaTime, CanSeePlayer());
            if (_lastState != State)
            {
                _lastState = State;
                ApplyStateColor();
            }
        }

        private void OnNoiseEmitted(Vector3 source, float intensity)
        {
            if (_awareness != null)
                _awareness.HearNoise(Vector3.Distance(transform.position, source), intensity);
        }

        private bool CanSeePlayer()
        {
            var origin = eye != null ? eye.position : transform.position + Vector3.up * 1.5f;
            var target = player.transform.position + Vector3.up * 1.1f;
            var toPlayer = target - origin;
            if (toPlayer.sqrMagnitude > viewDistance * viewDistance || Vector3.Angle(transform.forward, toPlayer) > fieldOfView * 0.5f)
                return false;

            if (!Physics.Raycast(origin, toPlayer.normalized, out var hit, viewDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return true;

            return hit.transform == player.transform || hit.transform.IsChildOf(player.transform);
        }

        private void ApplyStateColor()
        {
            if (_renderers == null)
                return;

            var color = State == EnemyAlertState.Alerted
                ? new Color(0.78f, 0.17f, 0.1f)
                : State == EnemyAlertState.Suspicious
                    ? new Color(0.73f, 0.48f, 0.17f)
                    : new Color(0.15f, 0.16f, 0.19f);

            foreach (var renderer in _renderers)
            {
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
