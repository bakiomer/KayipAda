// LaunchCeremony.cs
// Final anı: gemi bitince ~20-25 saniyelik suya indirme töreni (sinematik).
// 1) Oyuncu ve işçiler iskelede toplanıp sevinir (Cheer animasyonu), "GEMİ HAZIR!" bandı.
// 2) Kamera geminin etrafında yavaşça döner.
// 3) Gemi kızaktan denize doğru yana kayar; suya girince büyük su sıçraması; zafer müziği başlar,
//    havai fişekler art arda gökyüzünde patlar, ekranı konfeti kaplar, telefon titrer.
// 4) Gemi burnunu açık denize çevirir, yelken açıp uzaklaşır; kamera arkadan takip eder; ardından "KAÇTIN!".
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
        [Tooltip("Gemi karadayken karakterlerin içinden geçmesini engelleyen engeller (tören başlayınca kapanır).")]
        [SerializeField] private NavObstacle[] shipObstacles = new NavObstacle[0];
        [Tooltip("Kayma yönü (geminin yanından denize doğru, yatay).")]
        [SerializeField] private Vector3 slideDirection = Vector3.forward;
        [Tooltip("Gemi suya inince burnunu çevireceği açık deniz yönü.")]
        [SerializeField] private Vector3 sailDirection = Vector3.forward;
        [Tooltip("Oyuncu ve işçilerin toplanacağı noktalar (iskelede).")]
        [SerializeField] private Transform[] gatherPoints = new Transform[0];
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private EscapePanel escapePanel;

        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }
        public string Phase { get; private set; } = "";

        private int fireworks;
        private float nextFirework;

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
            Vector3 center = ShipCenter();

            // 1) Toplanma ve sevinç.
            Phase = "toplanma";
            if (CelebrationBanner.Instance != null) CelebrationBanner.Instance.Show("GEMİ HAZIR!", "Herkes iskeleye!", ship.GatherTime);
            GatherEveryone();
            Haptics.Vibrate();
            float gatherYaw = cameraFollow != null ? cameraFollow.transform.eulerAngles.y : 0f;
            for (float t = 0f; t < ship.GatherTime; t += Time.deltaTime)
            {
                Orbit(Vector3.Lerp(center, GatherCenter(), 0.4f), gatherYaw, ship.OrbitDistance, ship.OrbitHeight, 2.5f);
                yield return null;
            }

            // 2) Kamera geminin etrafında yavaşça döner.
            Phase = "kamera";
            float startYaw = cameraFollow != null ? cameraFollow.transform.eulerAngles.y : 0f;
            for (float t = 0f; t < ship.OrbitDuration; t += Time.deltaTime)
            {
                float yaw = startYaw - ship.OrbitDegrees * 0.5f + ship.OrbitDegrees * Mathf.SmoothStep(0f, 1f, t / ship.OrbitDuration);
                Orbit(center, yaw, ship.OrbitDistance, ship.OrbitHeight, 2.5f);
                yield return null;
            }

            // 3) Kızaktan kayma: yavaş başlar, hızlanır; suya girerken batar, yana yatar; büyük sıçrama.
            Phase = "kayma";
            foreach (NavObstacle o in shipObstacles) if (o != null) o.enabled = false;
            AudioManager.PlaySequence(SoundId.VictoryMusic);
            Vector3 dir = Flat(slideDirection);
            Vector3 start = shipRoot.position;
            float height = ShipHeight();
            Vector3 end = start + dir * ship.SlideDistance + Vector3.down * (height * ship.SinkFraction);
            Quaternion baseRotation = shipRoot.rotation;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, dir);
            bool splashed = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / ship.SlideDuration)
            {
                float k = t * t;
                shipRoot.position = Vector3.Lerp(start, end, k);
                shipRoot.rotation = Quaternion.AngleAxis(Mathf.Sin(t * Mathf.PI) * 7f, tiltAxis) * baseRotation;
                Orbit(ShipCenter(), cameraFollow != null ? cameraFollow.transform.eulerAngles.y : 0f, ship.OrbitDistance, ship.OrbitHeight, 2f);
                if (!splashed && t > 0.65f)
                {
                    splashed = true;
                    BigSplash(dir);
                }
                TickFireworks(splashed);
                yield return null;
            }

            // 4) Burnunu açık denize çevirir, yelken açıp uzaklaşır; kamera arkadan takip eder.
            Phase = "uzaklasma";
            Vector3 sail = Flat(sailDirection);
            Quaternion from = shipRoot.rotation;
            Quaternion bowForward = Quaternion.FromToRotation(Flat(shipRoot.forward), sail) * baseRotation;
            Vector3 position = shipRoot.position;
            for (float time = 0f; time < ship.TurnDuration + ship.SailDuration; time += Time.deltaTime)
            {
                float turn = Mathf.Clamp01(time / ship.TurnDuration);
                Quaternion rot = Quaternion.Slerp(from, bowForward, Mathf.SmoothStep(0f, 1f, turn));
                float speed = ship.SailSpeed * Mathf.Clamp01((time - ship.TurnDuration * 0.5f) / 1.5f);
                position += Flat(rot * Vector3.forward) * (speed * Time.deltaTime);
                shipRoot.position = position + Vector3.up * (Mathf.Sin(time * 1.6f) * 0.08f);
                shipRoot.rotation = Quaternion.AngleAxis(Mathf.Sin(time * 1.1f) * 2.5f, Flat(rot * Vector3.forward)) * rot;
                Follow(rot);
                TickFireworks(true);
                yield return null;
            }

            Phase = "bitti";
            IsRunning = false;
            IsFinished = true;
            SetCheer(false);
            if (escapePanel != null) escapePanel.Show();
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
        }

        // Kamera, merkezin çevresinde verilen açıda (yaw) ve yükseklikte durup merkeze bakar.
        private void Orbit(Vector3 center, float yaw, float distance, float height, float blend)
        {
            if (cameraFollow == null) return;
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -distance) + Vector3.up * height;
            Vector3 pos = center + offset;
            cameraFollow.SetShot(pos, Quaternion.LookRotation(center - pos), blend);
        }

        // Uzaklaşan gemiyi arkadan takip.
        private void Follow(Quaternion shipRotation)
        {
            if (cameraFollow == null) return;
            Vector3 forward = Flat(shipRotation * Vector3.forward);
            Vector3 center = ShipCenter();
            Vector3 pos = center - forward * ship.FollowDistance + Vector3.up * ship.FollowHeight;
            cameraFollow.SetShot(pos, Quaternion.LookRotation(center + forward * 4f - pos), 2f);
        }

        private Vector3 GatherCenter()
        {
            if (gatherPoints.Length == 0) return ShipCenter();
            Vector3 sum = Vector3.zero;
            foreach (Transform t in gatherPoints) sum += t.position;
            return sum / gatherPoints.Length;
        }

        private void GatherEveryone()
        {
            // Oyuncu iskeleye (toplanma yerinin ortasına), gemiye bakarak.
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null && gatherPoints.Length > 0)
            {
                Vector3 spot = gatherPoints[gatherPoints.Length / 2].position - Flat(slideDirection) * 1.2f;
                Rigidbody body = player.GetComponent<Rigidbody>();
                if (body != null) body.position = spot;
                player.transform.position = spot;
            }
            int i = 0;
            foreach (Worker worker in FindObjectsByType<Worker>(FindObjectsSortMode.None))
            {
                if (gatherPoints.Length == 0) break;
                Transform point = gatherPoints[i % gatherPoints.Length];
                worker.Gather(point.position + new Vector3((i / gatherPoints.Length) * 0.9f, 0f, 0f));
                i++;
            }
            SetCheer(true);
        }

        // Oyuncu ve işçiler sevinç animasyonu yapar (toplanma noktasına varınca da devam eder).
        private void SetCheer(bool on)
        {
            foreach (CharacterVisual visual in FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None)) visual.SetCheer(on);
        }

        private void TickFireworks(bool active)
        {
            if (!active || Time.time < nextFirework || fireworks >= ship.FireworkCount) return;
            // Geminin önünde ve iki yanında: kamera nereden bakarsa baksın görünsün.
            Vector3 center = ShipCenter();
            Vector3 ahead = Flat(Phase == "uzaklasma" ? shipRoot.forward : sailDirection);
            Vector3 side = Vector3.Cross(Vector3.up, ahead);
            Vector3 pos = center + ahead * Random.Range(6f, 14f) + side * Random.Range(-8f, 8f);
            pos.y = 0f;
            if (Effects.Instance != null) Effects.Instance.Firework(pos);
            AudioManager.Play(SoundId.Firework, 0.7f);
            fireworks++;
            nextFirework = Time.time + ship.FireworkInterval;
            if (fireworks == 1 || fireworks % 4 == 0) Haptics.Vibrate();
        }

        private void BigSplash(Vector3 dir)
        {
            Vector3 c = ShipCenter();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (Effects.Instance != null)
            {
                for (int i = -2; i <= 2; i++)
                {
                    Vector3 p = c + dir * 2f + side * (i * 1.8f);
                    p.y = 0f;
                    Effects.Instance.Splash(p);
                }
                Effects.Instance.ScreenConfetti(ship.ConfettiDuration);
            }
            AudioManager.Play(SoundId.Splash);
            Haptics.Vibrate();
        }

        private Vector3 ShipCenter()
        {
            Renderer[] renderers = builder.Model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return shipRoot.position;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return new Vector3(b.center.x, shipRoot.position.y + 1f, b.center.z);
        }

        private float ShipHeight()
        {
            Renderer[] renderers = builder.Model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 1f;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            return b.size.y;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }
    }
}
