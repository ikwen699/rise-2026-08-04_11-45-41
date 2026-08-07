using UnityEngine;

namespace Rise.Systems
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Music")]
        public AudioClip dayMusic;
        public AudioClip nightMusic;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
        [Range(0f, 1f)] public float crossfadeSpeed = 0.4f;

        [Header("Ambient")]
        public AudioClip birdsAmbient;
        public AudioClip cricketsAmbient;
        public AudioClip townAmbient;
        public AudioClip rainAmbient;
        public AudioClip thunderClip;
        [Range(0f, 1f)] public float ambientVolume = 0.25f;

        [Header("SFX")]
        public AudioClip doorOpenClip;
        public AudioClip buyClip;
        public AudioClip questCompleteClip;

        private AudioSource _musicA;
        private AudioSource _musicB;
        private AudioSource _currentMusic;
        private AudioSource _ambientSource;
        private AudioSource _sfxSource;
        private AudioSource _rainSource;
        private bool _usingMusicA = true;
        private float _thunderTimer;

        private void Awake()
        {
            _musicA = CreateChannel("MusicA");
            _musicB = CreateChannel("MusicB");
            _ambientSource = CreateChannel("Ambient");
            _sfxSource = CreateChannel("SFX");
            _rainSource = CreateChannel("Rain");
            _rainSource.loop = true;
            _rainSource.volume = 0f;

            _musicA.loop = true;
            _musicB.loop = true;
            _ambientSource.loop = true;
            _musicA.volume = 0f;
            _musicB.volume = 0f;
            _ambientSource.volume = 0f;

            _currentMusic = _musicA;

            if (dayMusic != null)
            {
                _musicA.clip = dayMusic;
                _musicA.Play();
                _usingMusicA = true;
            }
        }

        private AudioSource CreateChannel(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            return go.AddComponent<AudioSource>();
        }

        public void UpdateCycle(float dayFactor, bool raining, bool stormy)
        {
            bool isDay = dayFactor > 0.4f;
            AudioClip targetClip = isDay ? dayMusic : nightMusic;
            if (targetClip == null) return;

            AudioSource fadeOut = _usingMusicA ? _musicA : _musicB;
            AudioSource fadeIn = _usingMusicA ? _musicB : _musicA;

            if (fadeIn.clip != targetClip)
            {
                fadeIn.clip = targetClip;
                fadeIn.Play();
            }

            float musicMod = stormy ? 0.6f : raining ? 0.8f : 1f;
            fadeOut.volume = Mathf.Lerp(fadeOut.volume, 0f, Time.deltaTime * crossfadeSpeed);
            fadeIn.volume = Mathf.Lerp(fadeIn.volume, musicVolume * musicMod, Time.deltaTime * crossfadeSpeed);

            if (fadeIn.volume > musicVolume * musicMod * 0.9f)
            {
                fadeOut.Stop();
                _currentMusic = fadeIn;
                _usingMusicA = !_usingMusicA;
            }

            if (raining && rainAmbient != null)
            {
                if (_rainSource.clip != rainAmbient)
                {
                    _rainSource.clip = rainAmbient;
                    _rainSource.Play();
                }
                float rainTarget = stormy ? ambientVolume * 0.9f : ambientVolume * 0.5f;
                _rainSource.volume = Mathf.Lerp(_rainSource.volume, rainTarget, Time.deltaTime * crossfadeSpeed);
            }
            else
            {
                _rainSource.volume = Mathf.Lerp(_rainSource.volume, 0f, Time.deltaTime * crossfadeSpeed);
            }

            if (stormy && thunderClip != null)
            {
                _thunderTimer -= Time.deltaTime;
                if (_thunderTimer <= 0f)
                {
                    PlaySFX(thunderClip);
                    _thunderTimer = Random.Range(8f, 20f);
                }
            }

            AudioClip targetAmbient = isDay ? birdsAmbient : cricketsAmbient;
            if (!raining && targetAmbient != null && _ambientSource.clip != targetAmbient)
            {
                _ambientSource.clip = targetAmbient;
                _ambientSource.Play();
            }

            float ambientTarget = raining ? 0f : (isDay ? ambientVolume : ambientVolume * 0.7f);
            if (!isDay && cricketsAmbient == null && !raining) ambientTarget = 0f;
            _ambientSource.volume = Mathf.Lerp(_ambientSource.volume, ambientTarget, Time.deltaTime * crossfadeSpeed);

            if (townAmbient != null && _ambientSource.clip == null)
            {
                _ambientSource.clip = townAmbient;
                _ambientSource.volume = ambientVolume * 0.5f;
                _ambientSource.Play();
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && _sfxSource != null)
                _sfxSource.PlayOneShot(clip, 0.6f);
        }

        public void PlayDoorOpen() => PlaySFX(doorOpenClip);
        public void PlayBuy() => PlaySFX(buyClip);
        public void PlayQuestComplete() => PlaySFX(questCompleteClip);

        public AudioSource CreateCarEngine(Transform parent)
        {
            GameObject go = new GameObject("CarEngine");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.spatialBlend = 1f;
            src.maxDistance = 15f;
            src.volume = 0f;
            return src;
        }

        public void UpdateCarEngine(AudioSource src, float speed, AudioClip engineClip)
        {
            if (src == null) return;
            if (engineClip != null && !src.isPlaying)
            {
                src.clip = engineClip;
                src.Play();
            }
            float targetVol = Mathf.Clamp01(speed / 20f) * 0.4f;
            src.volume = Mathf.Lerp(src.volume, targetVol, Time.deltaTime * 5f);
            src.pitch = Mathf.Lerp(0.8f, 1.5f, speed / 25f);
        }
    }
}
