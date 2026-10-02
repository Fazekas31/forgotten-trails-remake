using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
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

        private AudioClip _gunshotClip;

        public override string Prompt => route == null || !route.State.HasOpenedGate
            ? "Chamar Jack através da grade"
            : !route.State.HasRescuedJack ? "Chamar Jack para fora" : !route.State.IsJackFollowing ? "Acalmar Jack" : "Jack está seguindo você";

        public override string InteractionId => route != null && route.State.HasRescuedJack
            ? ChesterJackRouteState.JackCalmInteractionId
            : ChesterJackRouteState.JackRescueInteractionId;

        public override string JournalEntry => route != null && route.State.IsJackFollowing
            ? "JACK — COMPANHEIRO\nJack se acalmou com sua voz e segue logo atrás. Ele fareja o ar antes de cada curva; observe quando os inimigos se aproximarem."
            : string.Empty;

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
            enabled = true;
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            completedObjective = null;
            if (distance < 0f || distance > InteractionRange)
            {
                result = string.Empty;
                return false;
            }

            if (route == null || !route.State.HasOpenedGate)
            {
                result = "Jack fareja sua mão através das barras. Chester precisa abrir a grade primeiro.";
                return true;
            }

            if (!route.State.HasRescuedJack)
            {
                result = "Jack passa pela grade aberta. Um disparo seco ecoa dentro da oficina; o cão treme e chora. Acalme-o antes de avançar.";
                return true;
            }

            if (!route.State.IsJackFollowing)
            {
                result = "Você se ajoelha e fala baixo até Jack parar de chorar. Ele encosta o focinho na sua mão e se levanta ao seu lado.";
                return true;
            }

            result = "Jack segue atrás de você, atento aos sons no beco.";
            return true;
        }

        private void Awake() => ConfigureAudio();

        private void OnDestroy()
        {
            if (_gunshotClip != null)
                Destroy(_gunshotClip);
        }

        private void Update()
        {
            if (route == null || !route.State.IsJackFollowing || player == null)
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
