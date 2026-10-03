using System.Collections;
using ForgottenTrail.Gameplay.UI;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Player
{
    /// <summary>Displays the protagonist's opening line from the screenplay at the start of the demo.</summary>
    public sealed class ScreenplayOpeningLine : MonoBehaviour
    {
        public const string ScriptedText = "Protagonista: \"Ash Creek... A última carta de Layla veio daqui.\"";

        [SerializeField] private float displayDuration = 6f;
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private Transform arrivalHorse;
        [SerializeField] private Transform woundedMan;
        [SerializeField] private Transform woundedArm;
        [SerializeField] private float rideDuration = 4.2f;
        [SerializeField] private float dismountDuration = 0.45f;

        private AudioSource _rideAudio;
        private AudioClip _windClip;
        private AudioClip _hoofClip;
        private AudioClip _wolfClip;
        private AudioClip _whinnyClip;
        private float _visibleUntil;
        private bool _arrivalStarted;

        public bool IsArrivalConfigured => playerController != null && arrivalHorse != null && woundedMan != null && woundedArm != null;

        public void ConfigureArrival(FirstPersonController controller, Transform horse, Transform injuredMan, Transform reachingArm)
        {
            playerController = controller;
            arrivalHorse = horse;
            woundedMan = injuredMan;
            woundedArm = reachingArm;
        }

        public void Begin(float duration = 6f)
        {
            displayDuration = Mathf.Max(0f, duration);
            _visibleUntil = Time.time + displayDuration;
        }

        private void Start()
        {
            if (IsArrivalConfigured)
                StartCoroutine(PlayArrival());
            else
                Begin(displayDuration);
        }

        private void OnDestroy()
        {
            Release(_windClip);
            Release(_hoofClip);
            Release(_wolfClip);
            Release(_whinnyClip);
        }

        private IEnumerator PlayArrival()
        {
            if (_arrivalStarted)
                yield break;

            _arrivalStarted = true;
            _visibleUntil = 0f;
            playerController.SetGameplayInputEnabled(false);
            var interactor = playerController.GetComponent<PlayerInteractor>();
            var restoreInteractor = interactor != null && interactor.enabled;
            if (restoreInteractor)
                interactor.enabled = false;

            var player = playerController.transform;
            var camera = playerController.ViewCamera;
            var cameraTransform = camera != null ? camera.transform : null;
            var cameraStart = cameraTransform != null ? cameraTransform.localPosition : Vector3.zero;
            var cameraMounted = cameraStart + Vector3.up * 0.43f;
            var start = player.position;
            var direction = Vector3.ProjectOnPlane(woundedMan.position - start, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.001f)
                direction = player.forward;
            var stop = woundedMan.position - direction * 1.35f;
            stop.y = start.y;
            player.rotation = Quaternion.LookRotation(direction, Vector3.up);
            var horseLocalStart = arrivalHorse.localPosition;
            var horseLocalRotation = arrivalHorse.localRotation;
            var armStartRotation = woundedArm.localRotation;

            PrepareRideAudio();
            var elapsed = 0f;
            var nextHoofAt = 0f;
            var wolfPlayed = false;
            var brakingDuration = Mathf.Min(0.16f, rideDuration * 0.12f);
            var approachDuration = rideDuration - brakingDuration;
            while (elapsed < rideDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed < approachDuration
                    ? Mathf.Lerp(0f, 0.88f, Mathf.Clamp01(elapsed / approachDuration))
                    : Mathf.Lerp(0.88f, 1f, Mathf.Clamp01((elapsed - approachDuration) / brakingDuration));
                player.position = Vector3.Lerp(start, stop, t);
                if (cameraTransform != null)
                {
                    cameraTransform.localPosition = Vector3.Lerp(cameraStart, cameraMounted, t);
                    cameraTransform.localPosition += Vector3.up * Mathf.Sin(elapsed * 15f) * 0.025f;
                    if (elapsed >= approachDuration)
                    {
                        var brakeT = Mathf.Clamp01((elapsed - approachDuration) / brakingDuration);
                        cameraTransform.localPosition += Vector3.up * Mathf.Sin(brakeT * Mathf.PI) * 0.065f;
                    }
                }
                arrivalHorse.localPosition = horseLocalStart + Vector3.up * Mathf.Sin(elapsed * 15f) * 0.035f;
                arrivalHorse.localRotation = horseLocalRotation * Quaternion.Euler(Mathf.Sin(elapsed * 7.5f) * 1.5f, 0f, 0f);
                var reachT = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.96f, t));
                woundedArm.localRotation = Quaternion.Slerp(armStartRotation, Quaternion.Euler(0f, 0f, -25f), reachT);

                if (elapsed >= nextHoofAt && _rideAudio != null)
                {
                    _rideAudio.PlayOneShot(_hoofClip, 0.5f);
                    nextHoofAt += 0.36f;
                }
                if (!wolfPlayed && elapsed >= rideDuration * 0.58f && _rideAudio != null)
                {
                    _rideAudio.PlayOneShot(_wolfClip, 0.24f);
                    wolfPlayed = true;
                }
                yield return null;
            }

            player.position = stop;
            if (cameraTransform != null)
                cameraTransform.localPosition = cameraMounted;
            if (_rideAudio != null)
            {
                _rideAudio.Stop();
                _rideAudio.PlayOneShot(_whinnyClip, 0.7f);
            }
            woundedArm.localRotation = Quaternion.Euler(0f, 0f, -25f);
            arrivalHorse.SetParent(null, true);
            arrivalHorse.position = player.position - player.right * 1.15f;
            arrivalHorse.position = new Vector3(arrivalHorse.position.x, 0f, arrivalHorse.position.z);
            arrivalHorse.rotation = player.rotation;
            arrivalHorse.localRotation = Quaternion.Euler(0f, player.eulerAngles.y, 0f);

            if (cameraTransform != null)
            {
                elapsed = 0f;
                while (elapsed < dismountDuration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dismountDuration));
                    cameraTransform.localPosition = Vector3.Lerp(cameraMounted, cameraStart, t);
                    yield return null;
                }
                cameraTransform.localPosition = cameraStart;
            }

            var face = woundedMan.Find("Luke — head");
            playerController.FaceTarget(face != null ? face.position : woundedMan.position + Vector3.up * 0.9f);
            playerController.SetGameplayInputEnabled(true);
            if (restoreInteractor)
                interactor.enabled = true;
            Begin(displayDuration);
        }

        private void PrepareRideAudio()
        {
            if (_rideAudio == null)
                _rideAudio = playerController.gameObject.AddComponent<AudioSource>();
            _rideAudio.playOnAwake = false;
            _rideAudio.spatialBlend = 0f;
            _rideAudio.loop = true;
            _rideAudio.volume = 0.2f;
            _windClip = CreateWindClip();
            _hoofClip = CreateHoofClip();
            _wolfClip = CreateWolfHowlClip();
            _whinnyClip = CreateHorseWhinnyClip();
            _rideAudio.clip = _windClip;
            _rideAudio.Play();
        }

        private static AudioClip CreateWindClip()
        {
            const int sampleRate = 22050;
            const float duration = 3f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            uint noise = 0x6a09e667u;
            var lowPass = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var whiteNoise = (noise & 0xffff) / 32767.5f - 1f;
                lowPass = lowPass * 0.96f + whiteNoise * 0.04f;
                var time = i / (float)sampleRate;
                samples[i] = (lowPass * 0.24f + Mathf.Sin(time * Mathf.PI * 2f * 48f) * 0.08f)
                    * (0.65f + Mathf.Sin(time * Mathf.PI * 2f * 0.23f) * 0.18f);
            }
            return CreateClip("Ash Creek arrival — night wind", samples, sampleRate);
        }

        private static AudioClip CreateHoofClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.16f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            uint noise = 0x3c6ef372u;
            for (var i = 0; i < samples.Length; i++)
            {
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var time = i / (float)sampleRate;
                var grit = (noise & 0xffff) / 32767.5f - 1f;
                samples[i] = (grit * 0.2f + Mathf.Sin(time * Mathf.PI * 2f * 92f) * 0.42f) * Mathf.Exp(-time * 25f);
            }
            return CreateClip("Ash Creek arrival — horse hoof on wet trail", samples, sampleRate);
        }

        private static AudioClip CreateWolfHowlClip()
        {
            const int sampleRate = 22050;
            const float duration = 2.1f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var pitch = Mathf.Lerp(250f, 125f, time / duration) + Mathf.Sin(time * 5f) * 9f;
                var envelope = Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI) * Mathf.Sin(time * Mathf.PI * 1.8f);
                samples[i] = Mathf.Sin(time * Mathf.PI * 2f * pitch) * envelope * 0.2f;
            }
            return CreateClip("Ash Creek arrival — distant wolf howl", samples, sampleRate);
        }

        private static AudioClip CreateHorseWhinnyClip()
        {
            const int sampleRate = 22050;
            const float duration = 1.25f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            var phase = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var t = time / duration;
                var pitch = t < 0.34f
                    ? Mathf.Lerp(360f, 690f, t / 0.34f)
                    : Mathf.Lerp(690f, 310f, (t - 0.34f) / 0.66f);
                pitch += Mathf.Sin(time * Mathf.PI * 2f * 7.2f) * 18f;
                phase += Mathf.PI * 2f * pitch / sampleRate;
                var envelope = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.08f))
                    * (1f - Mathf.SmoothStep(0.58f, 1f, t));
                var tremolo = 0.84f + Mathf.Sin(time * Mathf.PI * 2f * 8f) * 0.16f;
                samples[i] = (Mathf.Sin(phase) * 0.48f + Mathf.Sin(phase * 2.03f) * 0.22f
                    + Mathf.Sin(phase * 3.01f) * 0.11f + Mathf.Sin(phase * 4.98f) * 0.045f) * envelope * tremolo * 0.42f;
            }
            return CreateClip("Ash Creek arrival — horse whinny", samples, sampleRate);
        }

        private static AudioClip CreateClip(string clipName, float[] samples, int sampleRate)
        {
            var clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void Release(AudioClip clip)
        {
            if (clip == null)
                return;
            if (Application.isPlaying)
                Destroy(clip);
            else
                DestroyImmediate(clip);
        }

        private void OnGUI()
        {
            if (Time.time >= _visibleUntil)
                return;

            var scale = HudTextScale.Factor;
            var width = Mathf.Min(Screen.width * 0.84f, HudTextScale.Pixels(1400f));
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = HudTextScale.FontSize(30),
                normal = { textColor = new Color(0.91f, 0.86f, 0.74f) }
            };
            var height = style.CalcHeight(new GUIContent(ScriptedText), width - HudTextScale.Pixels(48f)) + HudTextScale.Pixels(32f);
            var y = Mathf.Min(Screen.height * 0.72f, Screen.height - height - HudTextScale.Pixels(24f));
            var panel = new Rect((Screen.width - width) * 0.5f, y, width, height);
            GUI.color = new Color(0.08f, 0.075f, 0.065f, 0.82f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 24f * scale, panel.y + 12f * scale, panel.width - 48f * scale, panel.height - 24f * scale), ScriptedText, style);
        }
    }
}
