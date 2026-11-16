// AudioManager.cs
// Tek ses yöneticisi: AudioManager.Play(SoundId.Sell) ile her yerden ses çalınır.
// Aynı ses kısa sürede üst üste çalınırsa perdesi hafifçe yükselir (toplarken/satarken "tıngır" hissi).
// Arka planda sürekli bir dalga sesi ve ara sıra kuş cıvıltıları çalar (ses dosyası yoksa kendisi üretir).
// Her çalışta perde hafifçe rastgele değişir (Entry.PitchJitter). Müzik (final) ayrı kaynaktan sırayla çalar.
// Ayarlar: ses açık/kapalı (PlayerPrefs "ses").
// Ses dosyası eksikse sessizce geçer, hata vermez.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-90)]
    public class AudioManager : MonoBehaviour
    {
        private const int VoiceCount = 10; // aynı anda en fazla çalınan efekt sesi

        private static AudioManager instance;

        [SerializeField] private GameConfig config;
        [SerializeField] private SoundLibrary library;

        private readonly List<AudioSource> voices = new List<AudioSource>();
        private readonly Dictionary<SoundId, float> lastPlayTime = new Dictionary<SoundId, float>();
        private readonly Dictionary<SoundId, float> comboPitch = new Dictionary<SoundId, float>();
        private AudioSource ambientSource;
        private AudioSource birdSource;
        private AudioSource musicSource;
        private AudioSource backgroundSource;
        private float duckUntil = -1f;
        private float backgroundLevel = 1f;   // 0..1, ducking ve finale geçiş bunu değiştirir
        private bool victoryStarted;
        private readonly Queue<AudioClip> musicQueue = new Queue<AudioClip>();
        private float musicVolume = 1f;
        private int nextVoice;

        private const string SoundPref = "ses";

        // Ayarlar: ses açık mı? (cihazda saklanır)
        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundPref, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundPref, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 1f : 0f;
            }
        }

        private void Awake()
        {
            instance = this;
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D ses: mobilde her yerden aynı duyulsun
                voices.Add(source);
            }

            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
            ambientSource.spatialBlend = 0f;

            birdSource = gameObject.AddComponent<AudioSource>();
            birdSource.loop = true;
            birdSource.playOnAwake = false;
            birdSource.spatialBlend = 0f;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;

            backgroundSource = gameObject.AddComponent<AudioSource>();
            backgroundSource.loop = true;
            backgroundSource.playOnAwake = false;
            backgroundSource.spatialBlend = 0f;
            AudioListener.volume = SoundOn ? 1f : 0f;
        }

        private void Start()
        {
            AudioClip ambient = library != null && library.Ambient != null ? library.Ambient : CreateWaveClip();
            ambientSource.clip = ambient;
            ambientSource.volume = config.AmbientVolume;
            ambientSource.Play();
            // Gerçek ortam kaydı (dalga + kuş) varsa kodla üretilen kuşlar gerekmez.
            if (library == null || library.Ambient == null)
            {
                birdSource.clip = CreateBirdClip();
                birdSource.volume = config.BirdVolume;
                birdSource.Play();
            }
            if (library != null && library.Music != null)
            {
                backgroundSource.clip = library.Music;
                backgroundSource.volume = config.BackgroundMusicVolume;
                backgroundSource.Play();
            }
        }

        private void Update()
        {
            ambientSource.volume = config.AmbientVolume;
            birdSource.volume = config.BirdVolume;

            // Arka plan müziği: kutlamalarda kısılır (ducking), finalde tamamen söner (zafer müziğine geçiş).
            float target = victoryStarted ? 0f : Time.unscaledTime < duckUntil ? config.MusicDuckLevel : 1f;
            float speed = victoryStarted ? 1f / Mathf.Max(0.05f, config.MusicCrossfade) : 1f / Mathf.Max(0.05f, config.MusicDuckFade);
            backgroundLevel = Mathf.MoveTowards(backgroundLevel, target, Time.unscaledDeltaTime * speed);
            backgroundSource.volume = config.BackgroundMusicVolume * backgroundLevel;
            if (!musicSource.isPlaying && musicQueue.Count > 0)
            {
                musicSource.clip = musicQueue.Dequeue();
                musicSource.volume = musicVolume * config.MusicVolume;
                musicSource.Play();
            }
        }

        // Kutlama: arka plan müziği birkaç saniye kısılır.
        public static void Duck(float seconds)
        {
            if (instance == null) return;
            instance.duckUntil = Mathf.Max(instance.duckUntil, Time.unscaledTime + seconds);
        }

        // Türün tüm seslerini sırayla çalar (final müziği); arka plan müziği yumuşakça söner.
        public static void PlaySequence(SoundId id)
        {
            if (instance == null || instance.library == null) return;
            instance.victoryStarted = true;
            SoundLibrary.Entry entry = instance.library.Find(id);
            if (entry == null || entry.Clips == null) return;
            instance.musicQueue.Clear();
            instance.musicSource.Stop();
            instance.musicVolume = entry.Volume;
            foreach (AudioClip clip in entry.Clips) if (clip != null) instance.musicQueue.Enqueue(clip);
        }

        public static void Play(SoundId id, float volumeScale = 1f)
        {
            if (instance != null) instance.PlayInternal(id, volumeScale);
        }

        private void PlayInternal(SoundId id, float volumeScale)
        {
            if (library == null) return;
            SoundLibrary.Entry entry = library.Find(id);
            if (entry == null || entry.Clips == null || entry.Clips.Length == 0) return;
            AudioClip clip = entry.Clips[Random.Range(0, entry.Clips.Length)];
            if (clip == null) return;

            // Kısa sürede tekrar çalınıyorsa perdeyi biraz artır, değilse sıfırla.
            float now = Time.unscaledTime;
            float pitch = 0f;
            if (lastPlayTime.TryGetValue(id, out float last) && now - last < config.PitchComboWindow)
            {
                comboPitch.TryGetValue(id, out pitch);
                pitch = Mathf.Min(pitch + config.PitchComboStep, config.PitchComboMax);
            }
            comboPitch[id] = pitch;
            lastPlayTime[id] = now;

            AudioSource source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Count;
            source.pitch = 1f + pitch + Random.Range(-entry.PitchJitter, entry.PitchJitter);
            source.PlayOneShot(clip, entry.Volume * volumeScale * config.SfxVolume);
        }

        // Dosya yoksa: yavaşça kabarıp sönen, yumuşatılmış gürültüden "dalga" sesi (8 sn, döngüye uygun).
        private static AudioClip CreateWaveClip()
        {
            const int rate = 22050;
            const int seconds = 8;
            int length = rate * seconds;
            float[] data = new float[length];
            System.Random random = new System.Random(7);
            float low = 0f;
            float lower = 0f;
            for (int i = 0; i < length; i++)
            {
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.08f;     // yumuşat (kumsal hışırtısı)
                lower += (low - lower) * 0.05f;   // daha da yumuşat (uzak uğultu)
                float t = (float)i / length;
                // İki dalga kabarması; başta ve sonda aynı değer → döngü dikişsiz.
                float swell = 0.35f + 0.65f * Mathf.Pow(Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f, 2f);
                data[i] = (low * 0.6f + lower * 1.8f) * swell;
            }
            AudioClip clip = AudioClip.Create("Waves (üretilmiş)", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Dosya yoksa: 12 saniyelik döngüde birkaç kısa kuş cıvıltısı (yükselip alçalan tiz sesler), arası sessiz.
        private static AudioClip CreateBirdClip()
        {
            const int rate = 22050;
            const int seconds = 12;
            int length = rate * seconds;
            float[] data = new float[length];
            System.Random random = new System.Random(11);
            float[] starts = { 1.2f, 1.5f, 4.8f, 7.3f, 7.55f, 7.8f, 10.4f };
            foreach (float start in starts)
            {
                float baseFreq = 2600f + (float)random.NextDouble() * 1400f;
                float duration = 0.09f + (float)random.NextDouble() * 0.08f;
                int from = (int)(start * rate), count = (int)(duration * rate);
                float phase = 0f;
                for (int i = 0; i < count && from + i < length; i++)
                {
                    float t = (float)i / count;
                    float freq = baseFreq * (1f + 0.35f * Mathf.Sin(t * Mathf.PI)); // yükselip alçalan
                    phase += 2f * Mathf.PI * freq / rate;
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    data[from + i] += Mathf.Sin(phase) * envelope * 0.25f;
                }
            }
            AudioClip clip = AudioClip.Create("Birds (üretilmiş)", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
