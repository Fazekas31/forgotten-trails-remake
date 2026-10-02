using UnityEngine;

namespace ForgottenTrail.Gameplay.Barn
{
    /// <summary>Owns the short procedural cues used by the barn climax and forest ending.</summary>
    public sealed class BarnScreenplayAudio : MonoBehaviour
    {
        private AudioSource _source;
        private AudioClip _whispers;
        private AudioClip _shriek;
        private AudioClip _timberCreak;
        private AudioClip _gunshot;
        private AudioClip _jackHowl;
        private AudioClip _jackGrowl;
        private AudioClip _churchBell;

        public void PlayDoorCreak(Vector3 position) => Play(_timberCreak, position, 0.55f);
        public void PlayMimicWhisper(Vector3 position, float volume = 0.3f) => Play(_whispers, position, volume);
        public void PlayMimicShriek(Vector3 position) => Play(_shriek, position, 0.95f);
        public void PlayGunshot(Vector3 position, float volume = 0.75f) => Play(_gunshot, position, volume);
        public void PlayJackHowl(Vector3 position) => Play(_jackHowl, position, 0.55f);
        public void PlayJackGrowl(Vector3 position) => Play(_jackGrowl, position, 0.72f);
        public void PlayMineWhispers(Vector3 position) => Play(_whispers, position, 0.85f);
        public void PlayChurchBell(Vector3 position) => Play(_churchBell, position, 1f);

        private void Awake()
        {
            if (Application.isPlaying)
                EnsureInitialized();
        }

        private void OnDestroy()
        {
            Release(_whispers);
            Release(_shriek);
            Release(_timberCreak);
            Release(_gunshot);
            Release(_jackHowl);
            Release(_jackGrowl);
            Release(_churchBell);
        }

        private void EnsureInitialized()
        {
            if (_source == null)
            {
                var sourceObject = new GameObject("Barn screenplay — spatial audio cues");
                sourceObject.transform.SetParent(transform, false);
                _source = sourceObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 1f;
                _source.rolloffMode = AudioRolloffMode.Linear;
                _source.minDistance = 2f;
                _source.maxDistance = 180f;
                _source.dopplerLevel = 0f;
            }

            if (_whispers == null) _whispers = CreateWhisperClip();
            if (_shriek == null) _shriek = CreateShriekClip();
            if (_timberCreak == null) _timberCreak = CreateWoodCreakClip();
            if (_gunshot == null) _gunshot = CreateGunshotClip();
            if (_jackHowl == null) _jackHowl = CreateJackHowlClip();
            if (_jackGrowl == null) _jackGrowl = CreateJackGrowlClip();
            if (_churchBell == null) _churchBell = CreateChurchBellClip();
        }

        private void Play(AudioClip clip, Vector3 position, float volume)
        {
            if (!Application.isPlaying)
                return;

            EnsureInitialized();
            if (clip == null || _source == null)
                return;

            _source.transform.position = position;
            _source.PlayOneShot(clip, volume);
        }

        private static AudioClip CreateWhisperClip()
        {
            const int sampleRate = 22050;
            const float duration = 1.7f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            uint noise = 0x6a09e667u;
            var filteredNoise = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var whiteNoise = (noise & 0xffff) / 32767.5f - 1f;
                filteredNoise = filteredNoise * 0.91f + whiteNoise * 0.09f;
                var voices = 0f;
                for (var voice = 0; voice < 6; voice++)
                {
                    var frequency = 175f + voice * 53f + Mathf.Sin(time * (1.2f + voice * 0.07f)) * 14f;
                    var pulse = 0.45f + 0.55f * Mathf.Sin(time * (2.7f + voice * 0.13f) + voice * 1.7f);
                    voices += Mathf.Sin(time * Mathf.PI * 2f * frequency + voice * 0.83f) * pulse;
                }

                var swell = 0.45f + 0.55f * Mathf.Sin(time * Mathf.PI * 1.15f);
                samples[i] = Mathf.Clamp((filteredNoise * 0.32f + voices * 0.11f) * swell, -0.42f, 0.42f);
            }

            return CreateClip("Ash Creek — overlapping wordless mine whispers", samples, sampleRate);
        }

        private static AudioClip CreateShriekClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.72f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            uint noise = 0x2c1b3c6du;
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI) * Mathf.Exp(-time * 0.45f);
                var pitch = Mathf.Lerp(620f, 1780f, time / duration) + Mathf.Sin(time * 31f) * 110f;
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var rasp = (noise & 0xffff) / 32767.5f - 1f;
                samples[i] = Mathf.Clamp((Mathf.Sin(time * Mathf.PI * 2f * pitch) * 0.35f + rasp * 0.18f) * envelope, -0.55f, 0.55f);
            }

            return CreateClip("Ash Creek — mimic shriek", samples, sampleRate);
        }

        private static AudioClip CreateWoodCreakClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.5f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            uint noise = 0xa54ff53au;
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI);
                var pitch = Mathf.Lerp(310f, 138f, time / duration);
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var grit = (noise & 0xffff) / 32767.5f - 1f;
                samples[i] = Mathf.Clamp((Mathf.Sin(time * Mathf.PI * 2f * pitch) * 0.24f + grit * 0.07f) * envelope, -0.3f, 0.3f);
            }

            return CreateClip("Ash Creek — strained barn timber", samples, sampleRate);
        }

        private static AudioClip CreateGunshotClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.62f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            uint noise = 0xbb67ae85u;
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var crack = ((noise & 0xffff) / 32767.5f - 1f) * Mathf.Exp(-time * 27f);
                var body = Mathf.Sin(time * Mathf.PI * 2f * 58f) * Mathf.Exp(-time * 7.5f);
                samples[i] = Mathf.Clamp(crack * 0.34f + body * 0.58f, -0.9f, 0.9f);
            }

            return CreateClip("Ash Creek — .38 and shotgun report", samples, sampleRate);
        }

        private static AudioClip CreateJackHowlClip()
        {
            const int sampleRate = 22050;
            const float duration = 1.65f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI) * Mathf.Exp(-time * 0.28f);
                var pitch = Mathf.Lerp(390f, 170f, time / duration) + Mathf.Sin(time * 12f) * 8f;
                var howl = Mathf.Sin(time * Mathf.PI * 2f * pitch) + Mathf.Sin(time * Mathf.PI * 2f * pitch * 1.51f) * 0.22f;
                samples[i] = howl * envelope * 0.18f;
            }

            return CreateClip("Jack — low mournful howl", samples, sampleRate);
        }

        private static AudioClip CreateJackGrowlClip()
        {
            const int sampleRate = 22050;
            const float duration = 1.3f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            uint noise = 0x510e527fu;
            var filteredNoise = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Sin(Mathf.Clamp01(time / duration) * Mathf.PI);
                var pulse = 0.72f + 0.28f * Mathf.Sin(time * Mathf.PI * 2f * 3.2f);
                var pitch = 76f + Mathf.Sin(time * Mathf.PI * 2f * 4.1f) * 5f;
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                var whiteNoise = (noise & 0xffff) / 32767.5f - 1f;
                filteredNoise = filteredNoise * 0.84f + whiteNoise * 0.16f;
                var throat = Mathf.Sin(time * Mathf.PI * 2f * pitch)
                    + Mathf.Sin(time * Mathf.PI * 2f * pitch * 1.9f) * 0.42f;
                samples[i] = Mathf.Clamp((throat * 0.7f + filteredNoise * 0.3f) * envelope * pulse * 0.3f, -0.65f, 0.65f);
            }

            return CreateClip("Jack — warning growl at the fallen mimic", samples, sampleRate);
        }

        private static AudioClip CreateChurchBellClip()
        {
            const int sampleRate = 22050;
            const float duration = 4.2f;
            var samples = new float[Mathf.RoundToInt(sampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Clamp01(time * 55f) * Mathf.Exp(-time * 0.92f);
                var hit = Mathf.Exp(-time * 36f);
                var bell = Mathf.Sin(time * Mathf.PI * 2f * 196f)
                    + Mathf.Sin(time * Mathf.PI * 2f * 326f + 0.16f) * 0.5f
                    + Mathf.Sin(time * Mathf.PI * 2f * 431f + 0.38f) * 0.27f
                    + Mathf.Sin(time * Mathf.PI * 2f * 548f + 0.62f) * 0.14f;
                samples[i] = bell * envelope * 0.31f + hit * Mathf.Sin(time * Mathf.PI * 2f * 83f) * 0.12f;
            }

            return CreateClip("Ash Creek — violent church bell toll", samples, sampleRate);
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
    }
}
