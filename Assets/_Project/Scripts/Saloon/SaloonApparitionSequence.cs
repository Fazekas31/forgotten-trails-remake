using System.Collections;
using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Saloon
{
    /// <summary>Plays the one-shot window apparition and downstairs saloon slam after the note is read.</summary>
    public sealed class SaloonApparitionSequence : MonoBehaviour
    {
        private const float DoorSlamDuration = 0.14f;
        private const float DoorReboundDuration = 0.24f;

        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private PlayerJournalComponent journal;
        [SerializeField] private SaloonInvestigationTracker investigation;
        [SerializeField] private Transform apparition;
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private AudioSource bangAudioSource;
        [SerializeField] private Light bangFlash;

        private readonly SaloonApparitionState _state = new SaloonApparitionState();
        private Quaternion _leftDoorOpen;
        private Quaternion _rightDoorOpen;
        private AudioClip _bangClip;
        private bool _isSubscribed;

        public bool HasTriggered => _state.HasTriggered;

        public void Configure(
            FirstPersonController controller,
            DemoProgressionComponent demoProgression,
            PlayerJournalComponent playerJournal,
            SaloonInvestigationTracker saloonInvestigation,
            Transform apparitionTransform,
            Transform leftDoorTransform,
            Transform rightDoorTransform,
            AudioSource soundSource,
            Light flashLight)
        {
            Unsubscribe();
            playerController = controller;
            progression = demoProgression;
            journal = playerJournal;
            investigation = saloonInvestigation;
            apparition = apparitionTransform;
            leftDoor = leftDoorTransform;
            rightDoor = rightDoorTransform;
            bangAudioSource = soundSource;
            bangFlash = flashLight;
            CacheDoorPose();
            Subscribe();
        }

        private void Awake()
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<FirstPersonController>();
            if (progression == null)
                progression = FindFirstObjectByType<DemoProgressionComponent>();
            if (journal == null)
                journal = FindFirstObjectByType<PlayerJournalComponent>();
            if (investigation == null)
                investigation = FindFirstObjectByType<SaloonInvestigationTracker>();
            if (bangAudioSource == null)
                bangAudioSource = gameObject.AddComponent<AudioSource>();

            bangAudioSource.playOnAwake = false;
            bangAudioSource.spatialBlend = 1f;
            bangAudioSource.rolloffMode = AudioRolloffMode.Linear;
            bangAudioSource.minDistance = 2.5f;
            bangAudioSource.maxDistance = 18f;
            bangAudioSource.dopplerLevel = 0f;
            _bangClip = CreateBangClip();
        }

        private void Start()
        {
            if (apparition != null)
                apparition.gameObject.SetActive(false);
            if (bangFlash != null)
            {
                bangFlash.shadows = LightShadows.None;
                bangFlash.intensity = 0f;
                bangFlash.enabled = false;
            }

            CacheDoorPose();
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestorePlayerControl();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RestorePlayerControl();
            if (_bangClip != null)
                Destroy(_bangClip);
        }

        private void Subscribe()
        {
            if (_isSubscribed || investigation == null)
                return;

            investigation.InvestigationCompleted += OnInvestigationCompleted;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || investigation == null)
                return;

            investigation.InvestigationCompleted -= OnInvestigationCompleted;
            _isSubscribed = false;
        }

        private void OnInvestigationCompleted()
        {
            if (progression == null
                || progression.CurrentObjective != DemoObjective.ExamineSaloonKnife
                || investigation == null
                || !investigation.IsComplete
                || !investigation.HasReadNote)
                return;
            if (!_state.TryTrigger(SaloonApparitionState.NoteInteractionId))
                return;

            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            var originalApparitionPosition = apparition != null ? apparition.position : Vector3.zero;
            if (playerController != null)
                playerController.SetGameplayInputEnabled(false);

            try
            {
                if (apparition != null)
                {
                    apparition.gameObject.SetActive(true);
                    if (playerController != null)
                        playerController.FaceTarget(apparition.position);
                }

                yield return new WaitForSeconds(0.72f);

                if (apparition != null)
                {
                    apparition.position += new Vector3(1.25f, 0.08f, -0.22f);
                    yield return new WaitForSeconds(0.34f);
                    apparition.gameObject.SetActive(false);
                    apparition.position = originalApparitionPosition;
                }

                PlayBang();
                yield return AnimateDoors(_leftDoorOpen, _rightDoorOpen, Quaternion.identity, Quaternion.identity, DoorSlamDuration);
                yield return new WaitForSeconds(0.18f);
                yield return AnimateDoors(Quaternion.identity, Quaternion.identity, _leftDoorOpen, _rightDoorOpen, DoorReboundDuration);
                if (journal != null)
                    journal.Record("O saloon estava vazio, mas algo me observou pela janela e fugiu quando desci. Encontrei uma faca. Um aviso diz que levaram sobreviventes ao celeiro. Antes de ir até lá, preciso ver de onde vem a luz na igreja.");
                yield return new WaitForSeconds(0.35f);
            }
            finally
            {
                if (apparition != null)
                {
                    apparition.gameObject.SetActive(false);
                    apparition.position = originalApparitionPosition;
                }
                SetDoorPose(_leftDoorOpen, _rightDoorOpen);
                RestorePlayerControl();
            }
        }

        private IEnumerator AnimateDoors(Quaternion leftFrom, Quaternion rightFrom, Quaternion leftTo, Quaternion rightTo, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                SetDoorPose(Quaternion.Slerp(leftFrom, leftTo, t), Quaternion.Slerp(rightFrom, rightTo, t));
                yield return null;
            }

            SetDoorPose(leftTo, rightTo);
        }

        private void PlayBang()
        {
            if (bangAudioSource != null && _bangClip != null)
                bangAudioSource.PlayOneShot(_bangClip, 0.82f);

            if (bangFlash != null)
                StartCoroutine(FlashBang());
        }

        private IEnumerator FlashBang()
        {
            bangFlash.enabled = true;
            bangFlash.intensity = 3.4f;
            yield return new WaitForSeconds(0.045f);
            bangFlash.intensity = 1.1f;
            yield return new WaitForSeconds(0.09f);
            bangFlash.intensity = 0f;
            bangFlash.enabled = false;
        }

        private void CacheDoorPose()
        {
            if (leftDoor != null)
                _leftDoorOpen = leftDoor.localRotation;
            if (rightDoor != null)
                _rightDoorOpen = rightDoor.localRotation;
        }

        private void SetDoorPose(Quaternion leftRotation, Quaternion rightRotation)
        {
            if (leftDoor != null)
                leftDoor.localRotation = leftRotation;
            if (rightDoor != null)
                rightDoor.localRotation = rightRotation;
        }

        private void RestorePlayerControl()
        {
            if (playerController != null)
                playerController.SetGameplayInputEnabled(true);
        }

        private static AudioClip CreateBangClip()
        {
            const int sampleRate = 22050;
            const float lengthSeconds = 0.46f;
            var sampleCount = Mathf.RoundToInt(sampleRate * lengthSeconds);
            var samples = new float[sampleCount];
            var random = new System.Random(19331);

            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var lowRumble = Mathf.Sin(time * Mathf.PI * 2f * 52f) * Mathf.Exp(-time * 7.5f);
                var crack = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-time * 31f);
                var glass = Mathf.Sin(time * Mathf.PI * 2f * 1180f) * Mathf.Exp(-time * 13f);
                samples[i] = Mathf.Clamp((lowRumble * 0.62f) + (crack * 0.28f) + (glass * 0.1f), -1f, 1f) * 0.78f;
            }

            var clip = AudioClip.Create("Saloon — glass and double-door bang", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
