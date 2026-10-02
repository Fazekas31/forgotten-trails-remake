using System.Collections;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Church
{
    /// <summary>Plays the screenplay's single bell toll and amber altar flicker on the first church entry.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ChurchEntryCue : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private Light altarLight;
        [SerializeField] private AudioSource bellSource;

        private AudioClip _bellClip;
        private bool _hasPlayed;
        private Color _originalColor;
        private float _originalIntensity;

        public void Configure(FirstPersonController investigator, DemoProgressionComponent demoProgression, Light light)
        {
            player = investigator;
            progression = demoProgression;
            altarLight = light;
            ConfigureLight();
        }

        private void Awake()
        {
            if (player == null)
                player = FindFirstObjectByType<FirstPersonController>();
            if (progression == null)
                progression = FindFirstObjectByType<DemoProgressionComponent>();
            if (altarLight == null)
                altarLight = transform.parent != null ? transform.parent.GetComponentInChildren<Light>(true) : null;
            if (bellSource == null)
                bellSource = gameObject.AddComponent<AudioSource>();

            bellSource.playOnAwake = false;
            bellSource.spatialBlend = 0.72f;
            bellSource.rolloffMode = AudioRolloffMode.Linear;
            bellSource.minDistance = 2.2f;
            bellSource.maxDistance = 24f;
            bellSource.dopplerLevel = 0f;
            _bellClip = CreateBellClip();
            ConfigureLight();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasPlayed || player == null || progression == null
                || progression.CurrentObjective != DemoObjective.ReceiveSheriffMission
                || other.GetComponentInParent<FirstPersonController>() != player)
                return;

            _hasPlayed = true;
            if (bellSource != null && _bellClip != null)
                bellSource.PlayOneShot(_bellClip, 0.7f);
            if (altarLight != null)
                StartCoroutine(FlickerAltarLight());
        }

        private IEnumerator FlickerAltarLight()
        {
            var coldColor = new Color(1f, 0.78f, 0.48f);
            altarLight.color = coldColor;
            altarLight.intensity = _originalIntensity * 0.18f;
            yield return new WaitForSeconds(0.11f);
            altarLight.intensity = _originalIntensity;
            yield return new WaitForSeconds(0.08f);
            altarLight.intensity = _originalIntensity * 0.32f;
            yield return new WaitForSeconds(0.09f);
            altarLight.color = _originalColor;
            altarLight.intensity = _originalIntensity;
        }

        private void ConfigureLight()
        {
            if (altarLight == null)
                return;
            _originalColor = altarLight.color;
            _originalIntensity = altarLight.intensity;
        }

        private void OnDestroy()
        {
            if (_bellClip != null)
                Destroy(_bellClip);
        }

        private static AudioClip CreateBellClip()
        {
            const int sampleRate = 22050;
            const float duration = 2.15f;
            var sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(time * 42f) * Mathf.Exp(-time * 1.85f);
                var fundamental = Mathf.Sin(time * Mathf.PI * 2f * 392f);
                var overtone = Mathf.Sin(time * Mathf.PI * 2f * 587f + 0.22f) * 0.43f;
                var highPartial = Mathf.Sin(time * Mathf.PI * 2f * 784f + 0.51f) * 0.18f;
                samples[i] = Mathf.Clamp((fundamental + overtone + highPartial) * envelope * 0.22f, -1f, 1f);
            }

            var clip = AudioClip.Create("Ash Creek — single church bell toll", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
