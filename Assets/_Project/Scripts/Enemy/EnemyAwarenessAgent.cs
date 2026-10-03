using ForgottenTrail.Gameplay.Awareness;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.UI;
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
        [SerializeField] private bool visionEnabled = true;
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

        public void ConfigureHearingOnly(FirstPersonController playerController, Transform earPoint)
        {
            player = playerController;
            eye = earPoint;
            visionEnabled = false;
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
            if (!visionEnabled)
                return false;

            var origin = eye != null ? eye.position : transform.position + Vector3.up * 1.5f;
            var target = player.ViewCamera != null
                ? player.ViewCamera.transform.position
                : player.transform.position + Vector3.up * 1.1f;
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

        private void OnGUI()
        {
            if (player == null || State == EnemyAlertState.Calm || Vector3.Distance(player.transform.position, transform.position) > viewDistance + 6f)
                return;

            var scale = HudTextScale.Factor;
            var panel = HudTextScale.AlertPanelRect(Screen.width, Screen.height);
            var color = State == EnemyAlertState.Alerted
                ? new Color(0.55f, 0.09f, 0.055f, 0.93f)
                : new Color(0.42f, 0.27f, 0.09f, 0.9f);
            GUI.color = color;
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            var label = visionEnabled
                ? State == EnemyAlertState.Alerted ? "VIGIA ALERTADO" : "O VIGIA SUSPEITA"
                : State == EnemyAlertState.Alerted ? "A CRIATURA OUVIU VOCÊ" : "A CRIATURA ESCUTA";
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = HudTextScale.FontSize(24),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.88f, 0.68f) }
            };
            GUI.Label(new Rect(panel.x + 8f * scale, panel.y + 3f * scale, panel.width - 16f * scale, 38f * scale), label, style);

            var meter = new Rect(panel.x + 18f * scale, panel.y + 52f * scale, panel.width - 36f * scale, 10f * scale);
            GUI.color = new Color(0.08f, 0.075f, 0.07f, 1f);
            GUI.DrawTexture(meter, Texture2D.whiteTexture);
            meter.width *= Mathf.Clamp01(Suspicion);
            GUI.color = State == EnemyAlertState.Alerted ? new Color(0.93f, 0.2f, 0.11f) : new Color(0.95f, 0.63f, 0.2f);
            GUI.DrawTexture(meter, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
