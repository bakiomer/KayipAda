// AudioManager.cs
// Tek ses yöneticisi: AudioManager.Play(SoundId.Sell) ile her yerden ses çalınır.
// Aynı ses kısa sürede üst üste çalınırsa perdesi hafifçe yükselir (toplarken/satarken "tıngır" hissi).
// Arka planda sürekli bir dalga sesi çalar; ses dosyası yoksa yumuşak bir dalga sesi kendisi üretir.
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
        private int nextVoice;

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
        }

        private void Start()
        {
            AudioClip ambient = library != null && library.Ambient != null ? library.Ambient : CreateWaveClip();
            ambientSource.clip = ambient;
            ambientSource.volume = config.AmbientVolume;
            ambientSource.Play();
        }

        private void Update()
        {
            ambientSource.volume = config.AmbientVolume;
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
            source.pitch = 1f + pitch + Random.Range(-0.03f, 0.03f);
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
    }
}
