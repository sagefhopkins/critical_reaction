using System.Collections;
using UnityEngine;

namespace Audio
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("Music Clips")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip lobbyMusic;

        [Header("Transition SFX")]
        [SerializeField] private AudioClip gameStartTransition;

        [Header("Settings")]
        [SerializeField] private float defaultCrossfadeDuration = 1.5f;
        [SerializeField] private float gameStartFadeOutDuration = 0.8f;
        [SerializeField] private float gameStartFadeInDelay = 0.3f;
        [SerializeField] [Range(0f, 1f)] private float maxVolume = 1f;

        private float userVolume = 1f;
        private float EffectiveVolume => maxVolume * userVolume;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource sfxSource;
        private bool sourceAActive = true;
        private Coroutine crossfadeRoutine;

        private AudioSource ActiveSource => sourceAActive ? sourceA : sourceB;
        private AudioSource InactiveSource => sourceAActive ? sourceB : sourceA;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();

            sourceA.loop = true;
            sourceB.loop = true;
            sfxSource.loop = false;

            sourceA.playOnAwake = false;
            sourceB.playOnAwake = false;
            sfxSource.playOnAwake = false;

            sourceA.volume = 0f;
            sourceB.volume = 0f;

            ApplyVolumeFromPrefs();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            if (mainMenuMusic != null)
                PlayImmediate(mainMenuMusic);
        }

        public void ApplyVolumeFromPrefs()
        {
            int vol = PlayerPrefs.GetInt("MusicVolume", 100);
            userVolume = vol / 100f;
            UpdateActiveVolume();
        }

        public void SetVolume(float volume01)
        {
            userVolume = Mathf.Clamp01(volume01);
            UpdateActiveVolume();
        }

        private void UpdateActiveVolume()
        {
            if (ActiveSource.isPlaying)
                ActiveSource.volume = EffectiveVolume;
        }

        public void PlayImmediate(AudioClip clip)
        {
            if (clip == null) return;

            StopCrossfade();

            InactiveSource.Stop();
            InactiveSource.volume = 0f;

            ActiveSource.clip = clip;
            ActiveSource.volume = EffectiveVolume;
            ActiveSource.Play();
        }

        public void CrossfadeTo(AudioClip clip, float duration = -1f)
        {
            if (clip == null) return;

            if (ActiveSource.clip == clip && ActiveSource.isPlaying)
                return;

            if (duration < 0f)
                duration = defaultCrossfadeDuration;

            StopCrossfade();
            crossfadeRoutine = StartCoroutine(CrossfadeCoroutine(clip, duration));
        }

        public void PlayTransitionIntoMusic(AudioClip transitionClip, AudioClip musicClip,
            float fadeOutDuration = -1f, float fadeInDelay = -1f)
        {
            if (fadeOutDuration < 0f) fadeOutDuration = gameStartFadeOutDuration;
            if (fadeInDelay < 0f) fadeInDelay = gameStartFadeInDelay;

            StopCrossfade();
            crossfadeRoutine = StartCoroutine(TransitionCoroutine(
                transitionClip, musicClip, fadeOutDuration, fadeInDelay));
        }

        public void PlayMainMenuMusic(float duration = -1f)
        {
            CrossfadeTo(mainMenuMusic, duration);
        }

        public void PlayLobbyMusic(float duration = -1f)
        {
            CrossfadeTo(lobbyMusic, duration);
        }

        public void PlayGameStartTransition(AudioClip levelMusicClip)
        {
            PlayTransitionIntoMusic(gameStartTransition, levelMusicClip);
        }

        public void PlayLevelMusic(AudioClip levelMusicClip, float duration = -1f)
        {
            CrossfadeTo(levelMusicClip, duration);
        }

        public void StopMusic(float fadeDuration = 1f)
        {
            StopCrossfade();
            crossfadeRoutine = StartCoroutine(FadeOutCoroutine(fadeDuration));
        }

        private void StopCrossfade()
        {
            if (crossfadeRoutine != null)
            {
                StopCoroutine(crossfadeRoutine);
                crossfadeRoutine = null;
            }
        }

        private IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration)
        {
            AudioSource fadeOut = ActiveSource;
            AudioSource fadeIn = InactiveSource;

            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();

            float startVolume = fadeOut.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float smooth = t * t * (3f - 2f * t);

                fadeOut.volume = Mathf.Lerp(startVolume, 0f, smooth);
                fadeIn.volume = Mathf.Lerp(0f, EffectiveVolume, smooth);

                yield return null;
            }

            fadeOut.Stop();
            fadeOut.volume = 0f;
            fadeIn.volume = EffectiveVolume;

            sourceAActive = !sourceAActive;
            crossfadeRoutine = null;
        }

        private IEnumerator TransitionCoroutine(AudioClip transitionClip, AudioClip musicClip,
            float fadeOutDuration, float fadeInDelay)
        {
            if (transitionClip != null)
            {
                sfxSource.volume = EffectiveVolume;
                sfxSource.PlayOneShot(transitionClip);
            }

            AudioSource fadeOut = ActiveSource;
            float startVolume = fadeOut.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                fadeOut.volume = Mathf.Lerp(startVolume, 0f, t * t);
                yield return null;
            }

            fadeOut.Stop();
            fadeOut.volume = 0f;

            if (fadeInDelay > 0f)
                yield return new WaitForSecondsRealtime(fadeInDelay);

            if (musicClip != null)
            {
                AudioSource fadeIn = InactiveSource;
                fadeIn.clip = musicClip;
                fadeIn.volume = 0f;
                fadeIn.Play();

                elapsed = 0f;
                float fadeInDuration = defaultCrossfadeDuration;

                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeInDuration);
                    fadeIn.volume = Mathf.Lerp(0f, EffectiveVolume, t * t * (3f - 2f * t));
                    yield return null;
                }

                fadeIn.volume = EffectiveVolume;
                sourceAActive = !sourceAActive;
            }

            crossfadeRoutine = null;
        }

        private IEnumerator FadeOutCoroutine(float duration)
        {
            AudioSource source = ActiveSource;
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
            crossfadeRoutine = null;
        }
    }
}
