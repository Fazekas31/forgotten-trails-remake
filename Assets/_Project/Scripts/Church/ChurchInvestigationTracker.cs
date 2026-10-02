using System;
using System.Collections;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Church
{
    /// <summary>Turns church evidence, the Elias confrontation, and the recovered badge into one route beat.</summary>
    public sealed class ChurchInvestigationTracker : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private PlayerJournalComponent journal;
        [SerializeField] private GameObject deputyBadge;
        [SerializeField] private AudioSource bellAudioSource;
        [SerializeField] private Light altarLight;

        private readonly ChurchInvestigationState _state = new ChurchInvestigationState();
        private AudioClip _bellClip;
        private Color _altarOriginalColor;
        private float _altarOriginalIntensity;
        private bool _hasCachedLight;
        private bool _objectiveCompleted;
        private bool _isSubscribed;

        public bool CanConfrontFalseElias => _state.HasExaminedRequiredClues;
        public bool HasRecognizedFalseElias => _state.HasRecognizedFalseElias;
        public bool HasRecoveredDeputyBadge => _state.HasRecoveredDeputyBadge;
        public bool IsComplete => _state.IsComplete;

        public void Configure(
            PlayerInteractor playerInteractor,
            DemoProgressionComponent demoProgression,
            PlayerJournalComponent playerJournal,
            GameObject sheriffBadge,
            AudioSource churchBellAudio,
            Light churchAltarLight)
        {
            Unsubscribe();
            interactor = playerInteractor;
            progression = demoProgression;
            journal = playerJournal;
            deputyBadge = sheriffBadge;
            bellAudioSource = churchBellAudio;
            altarLight = churchAltarLight;
            CacheAltarLight();
            if (deputyBadge != null)
                deputyBadge.SetActive(_state.HasRecognizedFalseElias && !_state.HasRecoveredDeputyBadge);
            Subscribe();
        }

        private void Awake()
        {
            if (interactor == null)
                interactor = FindFirstObjectByType<PlayerInteractor>();
            if (progression == null)
                progression = FindFirstObjectByType<DemoProgressionComponent>();
            if (journal == null)
                journal = FindFirstObjectByType<PlayerJournalComponent>();
            if (bellAudioSource == null)
                bellAudioSource = gameObject.AddComponent<AudioSource>();

            bellAudioSource.playOnAwake = false;
            bellAudioSource.spatialBlend = 0.72f;
            bellAudioSource.rolloffMode = AudioRolloffMode.Linear;
            bellAudioSource.minDistance = 2.2f;
            bellAudioSource.maxDistance = 22f;
            bellAudioSource.dopplerLevel = 0f;
            _bellClip = CreateBellClip();
            CacheAltarLight();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            StopAllCoroutines();
            RestoreAltarLight();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_bellClip != null)
                Destroy(_bellClip);
        }

        private void Subscribe()
        {
            if (_isSubscribed || interactor == null)
                return;

            interactor.InteractionCompleted += OnInteractionCompleted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || interactor == null)
                return;

            interactor.InteractionCompleted -= OnInteractionCompleted;
            _isSubscribed = false;
        }

        private void OnInteractionCompleted(string interactionId)
        {
            if (_state.TryRecordInteraction(interactionId))
            {
                if (interactionId == ChurchInvestigationState.BellRopeInteractionId)
                    PlayBellResponse();
                else if (interactionId == ChurchInvestigationState.UnshadowedLightInteractionId)
                    StartCoroutine(FlickerAltarLight());
                else if (interactionId == ChurchInvestigationState.FalseEliasInteractionId && deputyBadge != null)
                    deputyBadge.SetActive(true);
            }

            TryCompleteObjective();
        }

        private void TryCompleteObjective()
        {
            if (!_state.IsComplete || _objectiveCompleted || progression == null)
                return;
            if (!progression.TryComplete(DemoObjective.DiscoverChurchTruth))
                return;

            _objectiveCompleted = true;
            if (journal != null)
            {
                journal.Record("DISTINTIVO DO XERIFE — RECUPERADO\nA estrela estava presa sob a manga do impostor. No verso, as iniciais CH: o distintivo pertence a Chester.");
                journal.Record("NOVA ROTA — BECO DO FERREIRO\nO rastro de Jack entra no beco. Siga Chester e Jack pela passagem lateral para chegar ao escritório do xerife.");
            }
        }

        private void PlayBellResponse()
        {
            if (bellAudioSource != null && _bellClip != null)
                bellAudioSource.PlayOneShot(_bellClip, 0.7f);
        }

        private IEnumerator FlickerAltarLight()
        {
            if (altarLight == null)
                yield break;

            var coldColor = new Color(0.52f, 0.70f, 1f);
            for (var flicker = 0; flicker < 3; flicker++)
            {
                altarLight.color = coldColor;
                altarLight.intensity = _altarOriginalIntensity * 0.28f;
                yield return new WaitForSeconds(0.07f);
                altarLight.color = _altarOriginalColor;
                altarLight.intensity = _altarOriginalIntensity;
                yield return new WaitForSeconds(0.08f);
            }

            RestoreAltarLight();
        }

        private void CacheAltarLight()
        {
            if (altarLight == null)
                return;

            _altarOriginalColor = altarLight.color;
            _altarOriginalIntensity = altarLight.intensity;
            _hasCachedLight = true;
        }

        private void RestoreAltarLight()
        {
            if (!_hasCachedLight || altarLight == null)
                return;

            altarLight.color = _altarOriginalColor;
            altarLight.intensity = _altarOriginalIntensity;
        }

        private static AudioClip CreateBellClip()
        {
            const int sampleRate = 22050;
            const float duration = 1.8f;
            var sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(time * 36f) * Mathf.Exp(-time * 2.35f);
                var fundamental = Mathf.Sin(time * Mathf.PI * 2f * 392f);
                var overtone = Mathf.Sin(time * Mathf.PI * 2f * 587f + 0.22f) * 0.46f;
                var highPartial = Mathf.Sin(time * Mathf.PI * 2f * 784f + 0.51f) * 0.19f;
                samples[i] = Mathf.Clamp((fundamental + overtone + highPartial) * envelope * 0.22f, -1f, 1f);
            }

            var clip = AudioClip.Create("Ash Creek — bell answers the severed rope", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
