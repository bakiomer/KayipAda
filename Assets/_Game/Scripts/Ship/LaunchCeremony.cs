// LaunchCeremony.cs
// Final anı: gemi bitince ~20-25 saniyelik suya indirme töreni (sinematik).
// 1) Geniş açı: ada, uzun iskele ve gemi birlikte görünür. Oyuncu ve işçiler sahil boyunca dağılır (tek noktaya
//    yığılmaz); kimi kameraya dönüp zıplar / şapkasını havaya atar / el sallar, kimi gemiye el sallar; herkes farklı
//    yöne bakar, animasyonlar farklı zamanlarda başlar. "GEMİ HAZIR!" bandı.
// 2) Kamera gemiye geçer ve etrafında yavaşça döner.
// 3) Gemi iki rayın üstünde beşiğiyle birlikte kayarak suya iner; büyük su sıçraması; zafer müziği, havai fişekler,
//    ekranı kaplayan konfeti, titreşim.
// 4) Gemi burnunu açık denize çevirip uzaklaşır; kamera adanın üstünden (kalabalık önde) takip eder; "KAÇTIN!".
// Kamerayı kapatan yüksek nesneler (palmiye, kaya...) tören boyunca gizlenir. Bütün süreler ShipConfig'tedir.

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
        [Tooltip("Güneş: törende gün batımı tonuna döner.")]
        [SerializeField] private Light sun;
        [SerializeField] private EscapePanel escapePanel;
        [Tooltip("Havaya atılan şapka (törende bazı karakterler atar).")]
        [SerializeField] private GameObject hatPrefab;
        [Tooltip("Uzun iskelenin ortası (geniş açıda görünsün).")]
        [SerializeField] private Transform pierFocus;

        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }
        public string Phase { get; private set; } = "";

        private int fireworks;
        private float nextFirework;
        private readonly System.Collections.Generic.List<Renderer> occluders = new System.Collections.Generic.List<Renderer>();
        private float occlusionTimer;
        private Vector3 cameraFacing = Vector3.back; // geniş açıda kameraya dönük karakterlerin baktığı yön

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

            // 1) Geniş açı: ada, iskele ve gemi birlikte; kalabalık sahil boyunca dağılır ve sevinir.
            Phase = "toplanma";
            if (CelebrationBanner.Instance != null) CelebrationBanner.Instance.Show("GEMİ HAZIR!", "Herkes sahile!", ship.GatherTime);
            Vector3 bow = Flat(shipRoot.forward);
            Vector3 sea = Flat(slideDirection);
            Vector3 crowd = GatherCenter();
            Vector3 wideLook = (center + crowd + (pierFocus != null ? pierFocus.position : center)) / 3f;
            wideLook.y = 0.5f;
            Vector3 widePos = wideLook + new Vector3(0f, ship.WideShotHeight, -ship.WideShotDistance);
            cameraFacing = Flat(widePos - crowd);
            GatherEveryone();
            yield return null; // oyuncu ışınlandı: kalabalığın yeri güncel
            Haptics.Vibrate();
            StartCoroutine(Sunset());
            CollectOccluders();
            if (cameraFollow != null) cameraFollow.SetShot(widePos, Quaternion.LookRotation(wideLook - widePos), 2.2f);
            for (float t = 0f; t < ship.GatherTime; t += Time.deltaTime)
            {
                if (cameraFollow != null) cameraFollow.SetShot(widePos, Quaternion.LookRotation(wideLook - widePos), 2.2f);
                HideOccluders(center, crowd);
                yield return null;
            }
            float gatherYaw = YawOf(widePos - center);

            // 2) Kamera alçaktan, burun tarafından deniz tarafına doğru yavaşça döner (sonunda ada arkada kalır).
            Phase = "kamera";
            float endYaw = YawOf(sea + bow * 0.45f);
            for (float t = 0f; t < ship.OrbitDuration; t += Time.deltaTime)
            {
                float yaw = Mathf.LerpAngle(gatherYaw, endYaw, Mathf.SmoothStep(0f, 1f, t / ship.OrbitDuration));
                Orbit(center, yaw, ship.OrbitDistance, ship.OrbitHeight, 2.5f);
                HideOccluders(center, crowd);
                yield return null;
            }

            // 3) Kızaktan kayma: yavaş başlar, hızlanır; suya girerken batar, yana yatar; büyük sıçrama.
            Phase = "kayma";
            foreach (NavObstacle o in shipObstacles) if (o != null) o.enabled = false;
            AudioManager.Play(SoundId.ShipSlide);
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
                Orbit(ShipCenter(), endYaw, ship.OrbitDistance, ship.OrbitHeight, 2f); // denizden adaya bakar
                HideOccluders(ShipCenter(), crowd);
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
                FollowFromIsland(crowd, time / (ship.TurnDuration + ship.SailDuration));
                HideOccluders(ShipCenter(), crowd);
                TickFireworks(true);
                yield return null;
            }

            Phase = "bitti";
            IsRunning = false;
            IsFinished = true;
            SetCheer(false);
            foreach (Renderer r in occluders) if (r != null) r.enabled = true;
            if (escapePanel != null) escapePanel.Show();
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
        }

        // Merkezden kameraya doğru yöne karşılık gelen açı (Orbit için).
        private static float YawOf(Vector3 fromCenterToCamera)
        {
            Vector3 d = Flat(fromCenterToCamera);
            return Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg;
        }

        // Kamera, merkezin çevresinde verilen açıda (yaw) ve yükseklikte durup merkeze bakar.
        private void Orbit(Vector3 center, float yaw, float distance, float height, float blend)
        {
            if (cameraFollow == null) return;
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, -distance) + Vector3.up * height;
            Vector3 pos = center + offset;
            cameraFollow.SetShot(pos, Quaternion.LookRotation(center - pos), blend);
        }

        // Uzaklaşan gemi: kamera geminin önünde ve yanında, alçaktan geriye (gemiye ve arkadaki adaya) bakar.
        // Kamera adanın değil açık denizin tarafında durur (önünü kapatan direk/nesne olmasın).
        private void Follow(Quaternion shipRotation)
        {
            if (cameraFollow == null) return;
            Vector3 forward = Flat(shipRotation * Vector3.forward);
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 center = ShipCenter();
            Vector3 island = IslandCenter();
            if (Vector3.Dot(side, center - island) < 0f) side = -side;
            Vector3 pos = center + forward * (ship.FollowDistance * 0.55f) + side * (ship.FollowDistance * 0.75f) + Vector3.up * ship.FollowHeight;
            Vector3 look = Vector3.Lerp(center, island, 0.25f) + Vector3.up * 1.5f;
            cameraFollow.SetShot(pos, Quaternion.LookRotation(look - pos), 2f);
        }

        // Uzaklaşan gemi adanın üstünden takip edilir: kamera kalabalığın gerisinde ve yukarıda, gemiye bakar
        // (kalabalık önde el sallar). Gemi uzaklaştıkça kamera biraz yükselir.
        private void FollowFromIsland(Vector3 crowd, float progress)
        {
            if (cameraFollow == null) return;
            Vector3 shipPos = ShipCenter();
            Vector3 toShip = Flat(shipPos - crowd);
            float k = Mathf.Clamp01(progress);
            Vector3 pos = crowd - toShip * Mathf.Lerp(ship.IslandFollowBack, ship.IslandFollowBack * 0.6f, k)
                          + Vector3.up * Mathf.Lerp(ship.IslandFollowHeight, ship.IslandFollowHeight * 1.35f, k);
            Vector3 look = Vector3.Lerp(crowd, shipPos, 0.6f) + Vector3.up * 0.8f;
            cameraFollow.SetShot(pos, Quaternion.LookRotation(look - pos), 2.2f);
        }

        // Kamerayı kapatabilecek yüksek nesneler (palmiye, kaya, yamaç, bina): tören boyunca, kameradan gemiye ve
        // kalabalığa giden çizgiyi kesenler gizlenir.
        private void CollectOccluders()
        {
            occluders.Clear();
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                Bounds b = r.bounds;
                if (b.size.y < 1.3f || b.size.x > 14f || b.size.z > 14f) continue; // küçük süs / zemin / su değil
                if (r.transform.IsChildOf(shipRoot) || r.GetComponentInParent<CharacterVisual>() != null) continue;
                if (r.gameObject.layer == 5) continue; // arayüz
                occluders.Add(r);
            }
        }

        private void HideOccluders(Vector3 shipCenter, Vector3 crowd)
        {
            if (cameraFollow == null || occluders.Count == 0) return;
            occlusionTimer -= Time.deltaTime;
            if (occlusionTimer > 0f) return;
            occlusionTimer = 0.1f;
            Vector3 cam = cameraFollow.transform.position;
            Vector3 crowdEye = crowd + Vector3.up * 1f;
            Ray toShip = new Ray(cam, shipCenter - cam), toCrowd = new Ray(cam, crowdEye - cam);
            float dShip = Vector3.Distance(cam, shipCenter), dCrowd = Vector3.Distance(cam, crowdEye);
            foreach (Renderer r in occluders)
            {
                if (r == null) continue;
                Bounds b = r.bounds;
                b.Expand(0.4f);
                bool blocks = (b.IntersectRay(toShip, out float a) && a < dShip - 1.5f) || (b.IntersectRay(toCrowd, out float c) && c < dCrowd - 1.2f);
                if (r.enabled == blocks) r.enabled = !blocks;
            }
        }

        private static Vector3 IslandCenter()
        {
            if (IslandShape.Instance == null) return Vector3.zero;
            Vector2 c = IslandShape.Instance.Center;
            return new Vector3(c.x, 0f, c.y);
        }

        // Işık yavaşça gün batımı tonuna döner (güneş alçalır, renk turuncuya, ortam ışığı sıcak-mora).
        private IEnumerator Sunset()
        {
            if (sun == null) yield break;
            Color startColor = sun.color;
            float startIntensity = sun.intensity;
            Quaternion startRot = sun.transform.rotation;
            Quaternion endRot = Quaternion.Euler(ship.SunsetPitch, sun.transform.eulerAngles.y, 0f);
            Color sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, groundColor = RenderSettings.ambientGroundColor;
            for (float t = 0f; t < ship.SunsetTime; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / ship.SunsetTime);
                sun.color = Color.Lerp(startColor, ship.SunsetColor, k);
                sun.intensity = Mathf.Lerp(startIntensity, ship.SunsetIntensity, k);
                sun.transform.rotation = Quaternion.Slerp(startRot, endRot, k);
                RenderSettings.ambientSkyColor = Color.Lerp(sky, ship.SunsetSky, k);
                RenderSettings.ambientEquatorColor = Color.Lerp(equator, ship.SunsetEquator, k);
                RenderSettings.ambientGroundColor = Color.Lerp(groundColor, ship.SunsetEquator * 0.6f, k);
                yield return null;
            }
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
            if (gatherPoints.Length == 0) return;
            // Oyuncu kalabalığın ortasında; işçiler sahil boyunca dağılır (tek noktaya yığılmaz).
            PlayerController player = FindAnyObjectByType<PlayerController>();
            int middle = gatherPoints.Length / 2;
            if (player != null)
            {
                Vector3 spot = gatherPoints[middle].position;
                Rigidbody body = player.GetComponent<Rigidbody>();
                if (body != null) body.position = spot;
                player.transform.position = spot;
                StartCoroutine(Celebrate(player.GetComponent<CharacterVisual>(), 0, 0.2f, true));
            }
            int i = 0;
            Vector3 back = -Flat(slideDirection);
            Worker[] all = FindObjectsByType<Worker>(FindObjectsSortMode.None);
            foreach (Worker worker in all)
            {
                // İşçiler bütün sahil boyunca eşit aralıklarla (oyuncunun yeri hariç) dağılır.
                int slot = Mathf.Clamp(Mathf.RoundToInt((i + 0.5f) * gatherPoints.Length / Mathf.Max(1, all.Length)), 0, gatherPoints.Length - 1);
                if (slot == middle) slot = (slot + 1) % gatherPoints.Length;
                Vector3 spot = gatherPoints[slot].position + back * ((i / (gatherPoints.Length - 1)) * 1.1f);
                Rigidbody wb = worker.GetComponent<Rigidbody>();
                if (wb != null) wb.position = spot;
                worker.transform.position = spot;
                worker.Gather(spot);
                StartCoroutine(Celebrate(worker.GetComponent<CharacterVisual>(), i + 1, Random.Range(0.1f, 1.4f), false));
                i++;
            }
        }

        // Her karakter farklı kutlar: kameraya dönüp zıplar / şapka atar / el sallar ya da gemiye el sallar.
        // Hepsi farklı zamanda başlar; bakış yönleri biraz rastgele.
        private IEnumerator Celebrate(CharacterVisual visual, int index, float delay, bool isPlayer)
        {
            if (visual == null) yield break;
            int style = isPlayer ? 0 : index % 4; // 0: zıpla + şapka, 1: gemiye el salla, 2: kameraya el salla, 3: evet (gemiye)
            bool faceCamera = style == 0 || style == 2;
            Vector3 face = faceCamera ? cameraFacing : Flat(slideDirection);
            face = Quaternion.Euler(0f, Random.Range(-28f, 28f), 0f) * face;
            yield return new WaitForSeconds(delay);
            visual.Face(face);
            visual.SetCheer(true, style == 0 ? 2 : style == 3 ? 0 : 1);
            if (style == 0 || (style == 2 && index % 3 == 0)) StartCoroutine(TossHat(visual.transform, Random.Range(0f, 0.8f)));
            if (style == 0) StartCoroutine(Hop(visual));
            // Kutlama sürerken arada bir yön değiştirir (hep aynı yöne bakmasın).
            while (IsRunning)
            {
                yield return new WaitForSeconds(Random.Range(2.5f, 4.5f));
                if (!IsRunning) break;
                face = Quaternion.Euler(0f, Random.Range(-35f, 35f), 0f) * (faceCamera ? cameraFacing : Flat(slideDirection));
                visual.Face(face);
            }
        }

        // Zıplama: modeli kısa aralıklarla yukarı sıçratır.
        private IEnumerator Hop(CharacterVisual visual)
        {
            Transform model = visual.Model;
            if (model == null) yield break;
            Vector3 basePos = model.localPosition;
            float t = 0f;
            while (IsRunning && model != null)
            {
                t += Time.deltaTime;
                model.localPosition = basePos + Vector3.up * (Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.4f)) * 0.45f);
                yield return null;
            }
            if (model != null) model.localPosition = basePos;
        }

        // Şapka havaya atılır, dönerek yükselir ve tekrar yakalanır (birkaç kez).
        private IEnumerator TossHat(Transform who, float delay)
        {
            if (hatPrefab == null || who == null) yield break;
            yield return new WaitForSeconds(delay);
            GameObject hat = Instantiate(hatPrefab);
            while (IsRunning && who != null)
            {
                Vector3 head = who.position + Vector3.up * 1.9f;
                for (float t = 0f; t < 1f && IsRunning; t += Time.deltaTime / 1.1f)
                {
                    hat.transform.position = head + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 2.6f);
                    hat.transform.rotation = Quaternion.Euler(t * 40f, t * 720f, 0f);
                    yield return null;
                }
                hat.transform.position = head;
                yield return new WaitForSeconds(Random.Range(0.9f, 1.8f));
            }
            Destroy(hat);
        }

        // Tören bitince sevinç durur.
        private void SetCheer(bool on)
        {
            foreach (CharacterVisual visual in FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None)) visual.SetCheer(on, 0);
        }

        private void TickFireworks(bool active)
        {
            if (!active || Time.time < nextFirework || fireworks >= ship.FireworkCount) return;
            // Gemi ile ada arasında, iki yana dağılmış: patlamalar geminin ve adanın üzerinde görünür.
            Vector3 center = ShipCenter();
            Vector3 island = IslandCenter();
            Vector3 side = Vector3.Cross(Vector3.up, Flat(island - center));
            Vector3 pos = Vector3.Lerp(center, island, Random.Range(0.1f, 0.55f)) + side * Random.Range(-9f, 9f);
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
