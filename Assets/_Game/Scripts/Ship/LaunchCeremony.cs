// LaunchCeremony.cs
// Final anı: gemi bitince suya indirme töreni.
// 1) Oyuncu durur, işçiler iskelede toplanır, "Gemi hazır!" kutlaması.
// 2) Gemi kızaktan suya kayar (yavaş başlayıp hızlanır), suya girince su sıçrar, havai fişekler patlar.
// 3) Gemi yelken açıp uzaklaşır, kamera onu takip eder; ardından "KAÇTIN!" ekranı.
// Bütün süreler ShipConfig'tedir.

using System.Collections;
using UnityEngine;

namespace KayipAda
{
    public class LaunchCeremony : MonoBehaviour
    {
        public static LaunchCeremony Instance { get; private set; }

        [SerializeField] private ShipConfig ship;
        [SerializeField] private ShipBuilder builder;
        [Tooltip("Kızağın üstünde duran gemi kökü (kayan kısım).")]
        [SerializeField] private Transform shipRoot;
        [Tooltip("Gemi karadayken karakterlerin içinden geçmesini engelleyen engel (tören başlayınca kapanır).")]
        [SerializeField] private NavObstacle shipObstacle;
        [Tooltip("Kayma yönü (denize doğru, yatay).")]
        [SerializeField] private Vector3 slideDirection = Vector3.forward;
        [Tooltip("İşçilerin toplanacağı noktalar (iskelede).")]
        [SerializeField] private Transform[] gatherPoints = new Transform[0];
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private EscapePanel escapePanel;

        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            builder.ShipCompleted += Begin;
            if (builder.IsComplete) Begin(); // kayıttan bitmiş gemi geldiyse tören yeniden oynar
        }

        private void OnDestroy()
        {
            if (builder != null) builder.ShipCompleted -= Begin;
        }

        public void Begin()
        {
            if (IsRunning || IsFinished) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            IsRunning = true;
            PlayerController.InputBlocked = true;
            if (CelebrationBanner.Instance != null)
            {
                CelebrationBanner.Instance.Show("GEMİ HAZIR!", "Suya indirme töreni başlıyor", ship.GatherTime + 0.5f);
            }
            GatherWorkers();
            yield return new WaitForSeconds(ship.GatherTime);

            // Kızaktan kayma: yavaş başlar, hızlanır; gemi suya girerken biraz batar ve öne eğilir.
            if (shipObstacle != null) shipObstacle.enabled = false;
            if (cameraFollow != null) cameraFollow.SetTarget(builder.Model);
            Vector3 dir = new Vector3(slideDirection.x, 0f, slideDirection.z).normalized;
            Vector3 start = shipRoot.position;
            float height = ShipHeight();
            Vector3 end = start + dir * ship.SlideDistance + Vector3.down * (height * ship.SinkFraction);
            Quaternion baseRotation = shipRoot.rotation;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, dir);
            bool splashed = false;
            int fireworks = 0;
            float nextFirework = 0f;

            for (float t = 0f; t < 1f; t += Time.deltaTime / ship.SlideDuration)
            {
                float k = t * t;
                shipRoot.position = Vector3.Lerp(start, end, k);
                shipRoot.rotation = baseRotation * Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI) * 4f, tiltAxis);
                if (!splashed && t > 0.6f)
                {
                    splashed = true;
                    Splash(dir);
                }
                if (splashed && Time.time >= nextFirework && fireworks < ship.FireworkCount)
                {
                    Firework();
                    fireworks++;
                    nextFirework = Time.time + ship.FireworkInterval;
                }
                yield return null;
            }
            shipRoot.rotation = baseRotation;

            // Yelken açıp uzaklaşma (kamera takip eder), havai fişekler sürer.
            AudioManager.Play(SoundId.Victory);
            Vector3 sailStart = shipRoot.position;
            for (float time = 0f; time < ship.SailDuration; time += Time.deltaTime)
            {
                float speed = ship.SailSpeed * Mathf.Clamp01(time / 1.5f); // yavaşça hızlanır
                sailStart += dir * (speed * Time.deltaTime);
                shipRoot.position = sailStart + Vector3.up * (Mathf.Sin(time * 1.6f) * 0.08f);
                shipRoot.rotation = baseRotation * Quaternion.AngleAxis(Mathf.Sin(time * 1.1f) * 2.5f, dir);
                if (Time.time >= nextFirework && fireworks < ship.FireworkCount)
                {
                    Firework();
                    fireworks++;
                    nextFirework = Time.time + ship.FireworkInterval;
                }
                yield return null;
            }

            IsRunning = false;
            IsFinished = true;
            if (escapePanel != null) escapePanel.Show();
        }

        private void GatherWorkers()
        {
            Worker[] workers = FindObjectsByType<Worker>(FindObjectsSortMode.None);
            for (int i = 0; i < workers.Length; i++)
            {
                if (gatherPoints.Length == 0) break;
                Transform point = gatherPoints[i % gatherPoints.Length];
                Vector3 offset = new Vector3((i / gatherPoints.Length) * 0.9f, 0f, 0f);
                workers[i].Gather(point.position + offset);
            }
        }

        private float ShipHeight()
        {
            Renderer[] renderers = builder.Model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 1f;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b.size.y;
        }

        private void Splash(Vector3 dir)
        {
            Vector3 bow = builder.Model.position + dir * 3f;
            bow.y = 0f;
            if (Effects.Instance != null)
            {
                Effects.Instance.Splash(bow);
                Effects.Instance.Splash(builder.Model.position - dir * 1.5f + Vector3.right * 1.5f);
            }
            AudioManager.Play(SoundId.Splash);
        }

        private void Firework()
        {
            Vector3 center = builder.Model.position;
            Vector3 pos = center + new Vector3(Random.Range(-7f, 7f), Random.Range(9f, 14f), Random.Range(-2f, 6f));
            if (Effects.Instance != null) Effects.Instance.Firework(pos);
            AudioManager.Play(SoundId.Firework, 0.7f);
        }
    }
}
