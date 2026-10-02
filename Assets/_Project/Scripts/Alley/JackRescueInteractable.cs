using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.SheriffOffice;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Alley
{
    /// <summary>Frees and calms Jack, then keeps him close as the player escapes to the sheriff.</summary>
    public sealed class JackRescueInteractable : PlayerInteractable
    {
        private const float InteractionRange = 2.8f;

        [SerializeField] private ChesterJackRouteTracker route;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private AudioSource soundSource;
        [SerializeField] private float followSpeed = 3f;
        [SerializeField] private float followDistance = 1.45f;
        [SerializeField] private SheriffOfficeEscapeSequence sheriffEscape;

        private AudioClip _gunshotClip;
        private bool _isHeldAtBarn;
        private bool _isKnockedOutAtBarn;
        private bool _isRunningToAftermath;
        private Vector3 _aftermathTarget;
        private Vector3 _aftermathThreat;
        private float _aftermathGroundY;
        private float _followPausedUntil;
        private Collider _interactionCollider;

        public bool IsRunningToAftermath => _isRunningToAftermath;

        public override string Prompt => _isKnockedOutAtBarn ? string.Empty : sheriffEscape != null && sheriffEscape.IsActive
            ? sheriffEscape.JackIsQuiet ? "Jack está em silêncio" : "Comandar Jack para ficar em silêncio"
            : route == null || !route.State.HasOpenedGate
            ? "Chamar Jack através da grade"
            : !route.State.HasRescuedJack ? "Chamar Jack para fora" : !route.State.IsJackFollowing ? "Acalmar Jack" : "Jack está seguindo você";

        public override string InteractionId => route != null && route.State.HasRescuedJack
            ? ChesterJackRouteState.JackCalmInteractionId
            : ChesterJackRouteState.JackRescueInteractionId;

        public override string JournalEntry => string.Empty;

        public void Configure(ChesterJackRouteTracker tracker, FirstPersonController playerController, AudioSource audioSource)
        {
            route = tracker;
            player = playerController;
            soundSource = audioSource;
            ConfigureAudio();
        }

        public void ReactToGunshot()
        {
            if (soundSource != null && _gunshotClip != null)
                soundSource.PlayOneShot(_gunshotClip, 0.72f);
            if (player != null)
                player.EmitNoise(transform.position, 0.95f);
        }

        public void BeginFollowing()
        {
            _isHeldAtBarn = false;
            enabled = true;
        }

        public void HoldAtBarn(Vector3 position)
        {
            _isHeldAtBarn = true;
            _isKnockedOutAtBarn = false;
            transform.position = position;
        }

        public void KnockOutAtBarn(Vector3 position)
        {
            if (_interactionCollider == null)
                _interactionCollider = GetComponent<Collider>();
            _isHeldAtBarn = true;
            _isKnockedOutAtBarn = true;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 82f);
            if (_interactionCollider != null)
                _interactionCollider.enabled = false;
        }

        public void WakeAndRunToAftermath(Vector3 destination, Vector3 threatPosition)
        {
            if (_interactionCollider == null)
                _interactionCollider = GetComponent<Collider>();
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            _isHeldAtBarn = false;
            _isKnockedOutAtBarn = false;
            _isRunningToAftermath = true;
            _aftermathGroundY = transform.position.y;
            _aftermathTarget = destination;
            _aftermathTarget.y = _aftermathGroundY;
            _aftermathThreat = threatPosition;
            _followPausedUntil = Time.time + 2.5f;
            if (_interactionCollider != null)
                _interactionCollider.enabled = true;
            enabled = true;
        }

        public void ConfigureSheriffEscape(SheriffOfficeEscapeSequence escapeSequence)
        {
            sheriffEscape = escapeSequence;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (sheriffEscape != null && sheriffEscape.IsActive)
            {
                var commanded = sheriffEscape.CommandJackToStayQuiet();
                result = commanded ? "Jack obedece e fica em silêncio." : "Jack permanece em silêncio.";
                return true;
            }

            if (route == null || !route.State.HasOpenedGate)
            {
                result = "A grade reforçada continua fechada.";
                return true;
            }

            if (!route.State.HasRescuedJack)
            {
                result = "Jack passa pela grade aberta. Um disparo seco ecoa dentro da oficina; o cão treme e chora. Acalme-o antes de avançar.";
                return true;
            }

            if (!route.State.IsJackFollowing)
            {
                result = "Você se ajoelha e acalma Jack antes de seguir pelo beco.";
                return true;
            }

            result = "Jack segue atrás de você, atento aos sons no beco.";
            return true;
        }

        private void Awake()
        {
            _interactionCollider = GetComponent<Collider>();
            ConfigureAudio();
        }

        private void OnDestroy()
        {
            if (_gunshotClip != null)
                Destroy(_gunshotClip);
        }

        private void Update()
        {
            if (_isRunningToAftermath)
            {
                UpdateAftermathRun();
                return;
            }

            if (Time.time < _followPausedUntil)
                return;

            if (_isKnockedOutAtBarn || _isHeldAtBarn || route == null || !route.State.IsJackFollowing || player == null)
                return;

            var target = player.transform.position - player.transform.forward * followDistance;
            target.y = transform.position.y;
            var offset = target - transform.position;
            if (offset.sqrMagnitude < 0.36f)
                return;

            var direction = offset.normalized;
            if (Physics.SphereCast(transform.position + Vector3.up * 0.34f, 0.18f, direction, out var hit, 0.36f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(player.transform))
                return;

            transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void UpdateAftermathRun()
        {
            var current = transform.position;
            var offset = _aftermathTarget - current;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 0.04f)
            {
                transform.position = _aftermathTarget;
                FaceAftermathThreat();
                _isRunningToAftermath = false;
                return;
            }

            var direction = offset.normalized;
            var next = Vector3.MoveTowards(current, _aftermathTarget, Mathf.Max(followSpeed, 4.5f) * Time.deltaTime);
            next.y = _aftermathGroundY + Mathf.Abs(Mathf.Sin(Time.time * 17f)) * 0.045f;
            transform.position = next;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void FaceAftermathThreat()
        {
            var direction = Vector3.ProjectOnPlane(_aftermathThreat - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void ConfigureAudio()
        {
            if (soundSource == null)
                soundSource = GetComponent<AudioSource>();
            if (soundSource == null)
                soundSource = gameObject.AddComponent<AudioSource>();

            soundSource.playOnAwake = false;
            soundSource.spatialBlend = 1f;
            soundSource.rolloffMode = AudioRolloffMode.Linear;
            soundSource.minDistance = 2f;
            soundSource.maxDistance = 24f;
            soundSource.dopplerLevel = 0f;
            if (_gunshotClip != null)
                return;

            const int sampleRate = 22050;
            const float duration = 0.38f;
            var sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            uint noise = 0x43a2b19u;
            for (var i = 0; i < samples.Length; i++)
            {
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var time = i / (float)sampleRate;
                var crack = ((noise & 0xffff) / 32767.5f - 1f) * Mathf.Exp(-time * 30f);
                var lowThump = Mathf.Sin(time * Mathf.PI * 2f * 62f) * Mathf.Exp(-time * 9f);
                samples[i] = Mathf.Clamp((crack * 0.32f + lowThump * 0.58f) * 0.62f, -1f, 1f);
            }

            _gunshotClip = AudioClip.Create("Blacksmith alley — dry gunshot", sampleCount, 1, sampleRate, false);
            _gunshotClip.SetData(samples, 0);
        }
    }
}
