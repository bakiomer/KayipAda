// Machine.cs
// İşleme binasının görünen makinesi ve üretim hattı. Animasyon binanın üretim döngüsüne SENKRONDUR: faz =
// biten ürün + ilerleme (ProcessingBuilding.Cycle); hız binanın üretim hızından ve seviyesinden gelir, bir ürünün
// tamamlandığı an animasyonun bitiş anına denk gelir (son çekiç vuruşu, bobinin dolması, rulonun son katı).
// Giriş boşken ya da çıkış doluyken faz ilerlemez: makine durur; yeniden başlarken gösterilen faz gerçeği yumuşakça
// izler (hızlanarak başlar, yavaşlayarak durur).
// Üretim hattı: girişe bırakılan malzemenin kopyası kızak/makaralar üstünde makineye kayar, makine çalışır, ürünün
// kopyası çıkış kızağından aşağı kayar; ürün tamamlanınca gerçek ürün çıkış yığınına düşer.
// Türler: Crane (taş ocağı vinci + yontma tezgahı), RopeWheel (su çarkı + makaralar + burulan iplikler + bobin),
// Loom (dokuma tezgahı: çerçeve, mekik, kalınlaşan çizgili rulo), Anvil (demirci: eksantrikli çekiç, körük, ocak),
// Saw (bıçkıhane: kütük kızağı). Sayılar GameConfig > "Bina tarifleri ve makineler".

using UnityEngine;

namespace KayipAda
{
    public class Machine : MonoBehaviour
    {
        public enum Kind { RopeWheel, Loom, Anvil, Crane, Saw }

        [SerializeField] private GameConfig config;
        [SerializeField] private ProcessingBuilding building;
        [SerializeField] private Kind kind;

        [Header("Üretim hattı")]
        [SerializeField] private Transform feedProxy;
        [SerializeField] private Transform feedFrom;
        [SerializeField] private Transform feedTo;
        [SerializeField] private Transform outProxy;
        [SerializeField] private Transform outFrom;
        [SerializeField] private Transform outTo;
        [Tooltip("Kızaktaki makaralar (malzeme geçerken döner).")]
        [SerializeField] private Transform[] rollers = new Transform[0];

        [Header("Halat (su çarkı)")]
        [SerializeField] private Transform waterWheel;
        [SerializeField] private Transform[] spools = new Transform[0];
        [SerializeField] private Renderer[] strands = new Renderer[0];
        [SerializeField] private Transform bobin;
        [SerializeField] private ParticleSystem water;

        [Header("Dokuma")]
        [SerializeField] private Transform heddle;
        [SerializeField] private float heddleTravel = 0.22f;
        [SerializeField] private Transform shuttle;
        [SerializeField] private float shuttleTravel = 0.8f;
        [SerializeField] private Transform clothRoll;
        [SerializeField] private Renderer clothSheet;

        [Header("Demirci")]
        [SerializeField] private Transform hammer;
        [SerializeField] private Transform cam;
        [SerializeField] private Transform bellows;
        [SerializeField] private Transform ember;
        [SerializeField] private ParticleSystem sparks;

        [Header("Taş ocağı (vinç)")]
        [SerializeField] private Transform boom;
        [SerializeField] private float boomSwing = 70f;
        [SerializeField] private Transform drum;
        [SerializeField] private Transform hook;
        [SerializeField] private Transform block;
        [SerializeField] private Transform blockPit;
        [SerializeField] private Transform blockBench;
        [SerializeField] private Transform chisel;

        [Header("Ortak")]
        [Tooltip("İşlerken çıkan toz/kırıntı (boş olabilir).")]
        [SerializeField] private ParticleSystem dust;
        [Tooltip("Bir üründeki vuruş / mekik geçişi / çark turu sayısı.")]
        [SerializeField] private int beatsPerProduct = 3;

        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");
        private MaterialPropertyBlock block_;
        private float shown = -1f;      // gösterilen faz (gerçeği yumuşakça izler)
        private float speed;            // 0..1 hareket şiddeti (ses ve partikül için)
        private Transform listener;
        private bool dustOn;
        private Quaternion wheelBase, hammerBase, boomBase, drumBase, camBase;
        private Quaternion[] spoolBase, rollerBase;
        private Vector3 heddleBase, shuttleBase, rollScale, bobinScale, bellowsScale, emberScale, hookBase, chiselBase;

        public bool IsMoving => speed > 0.05f;
        public float ShownCycle => shown;

        private void Awake()
        {
            block_ = new MaterialPropertyBlock();
            if (waterWheel != null) wheelBase = waterWheel.localRotation;
            if (hammer != null) hammerBase = hammer.localRotation;
            if (boom != null) boomBase = boom.localRotation;
            if (drum != null) drumBase = drum.localRotation;
            if (cam != null) camBase = cam.localRotation;
            if (heddle != null) heddleBase = heddle.localPosition;
            if (shuttle != null) shuttleBase = shuttle.localPosition;
            if (clothRoll != null) rollScale = clothRoll.localScale;
            if (bobin != null) bobinScale = bobin.localScale;
            if (bellows != null) bellowsScale = bellows.localScale;
            if (ember != null) emberScale = ember.localScale;
            if (hook != null) hookBase = hook.localPosition;
            if (chisel != null) chiselBase = chisel.localPosition;
            spoolBase = new Quaternion[spools.Length];
            for (int i = 0; i < spools.Length; i++) spoolBase[i] = spools[i].localRotation;
            rollerBase = new Quaternion[rollers.Length];
            for (int i = 0; i < rollers.Length; i++) rollerBase[i] = rollers[i].localRotation;
        }

        private void Start()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            listener = player != null ? player.transform : null;
            if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (water != null) water.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Apply(0f, 0f, 0f);
        }

        private void Update()
        {
            if (building == null) return;
            float target = building.Cycle;
            if (shown < 0f || target - shown > 1.5f || target < shown - 0.01f) shown = target; // ilk kare / büyük atlama
            // Yumuşak izleme: gerçek fazın gerisinde kalır, yeniden başlarken hızlanır, dururken yavaşlar.
            float before = shown;
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-config.MachineFollow * Time.deltaTime));
            float delta = shown - before;
            float rate = Time.deltaTime > 0f ? delta / Time.deltaTime : 0f;
            float expected = building.SpeedMultiplier / Mathf.Max(0.05f, building.Type.ProcessTime);
            speed = Mathf.Clamp01(rate / Mathf.Max(0.001f, expected));
            SetParticles(building.IsWorking && speed > 0.2f);
            Apply(shown, before, delta);
        }

        // Faz → poz. "frac" bu ürünün ilerlemesi (0..1); son an = ürün tamamlanır.
        private void Apply(float cycle, float before, float delta)
        {
            float frac = cycle - Mathf.Floor(cycle);
            UpdateLine(frac, delta);
            switch (kind)
            {
                case Kind.RopeWheel: Rope(cycle, before, frac); break;
                case Kind.Loom: Loom(cycle, before, frac); break;
                case Kind.Anvil: Anvil(cycle, before, frac); break;
                case Kind.Crane: Crane(cycle, before, frac); break;
            }
        }

        // Üretim hattı: başta girişten makineye kayan malzeme, sonda çıkışa kayan ürün.
        private void UpdateLine(float frac, float delta)
        {
            bool active = building.HasEnoughInput || frac > 0.001f;
            if (feedProxy != null && feedFrom != null && feedTo != null)
            {
                float k = Mathf.Clamp01(frac / 0.28f);
                bool show = active && frac > 0.001f && frac < 0.3f;
                if (feedProxy.gameObject.activeSelf != show) feedProxy.gameObject.SetActive(show);
                if (show) feedProxy.position = Vector3.Lerp(feedFrom.position, feedTo.position, Mathf.SmoothStep(0f, 1f, k));
            }
            if (outProxy != null && outFrom != null && outTo != null)
            {
                float k = Mathf.Clamp01((frac - 0.84f) / 0.16f);
                bool show = frac > 0.84f;
                if (outProxy.gameObject.activeSelf != show) outProxy.gameObject.SetActive(show);
                if (show) outProxy.position = Vector3.Lerp(outFrom.position, outTo.position, k * k);
            }
            for (int i = 0; i < rollers.Length; i++)
                rollers[i].localRotation = rollers[i].localRotation * Quaternion.Euler(delta * 720f, 0f, 0f);
        }

        // Su çarkı: bir üründe beats tur; makaralar daha hızlı; iplikler burulur (doku kayar); bobin dolar, sonunda boşalır.
        private void Rope(float cycle, float before, float frac)
        {
            float turns = cycle * beatsPerProduct;
            if (waterWheel != null) waterWheel.localRotation = wheelBase * Quaternion.Euler(0f, 0f, -turns * 360f);
            for (int i = 0; i < spools.Length; i++)
                spools[i].localRotation = spoolBase[i] * Quaternion.Euler(0f, 0f, turns * 360f * 2.5f * (i % 2 == 0 ? 1f : -1f));
            foreach (Renderer r in strands)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block_);
                block_.SetVector(BaseMapSt, new Vector4(1f, 4f, 0f, -turns * 1.5f));
                r.SetPropertyBlock(block_);
            }
            if (bobin != null)
            {
                bobin.localRotation = Quaternion.Euler(0f, 0f, turns * 360f * 1.5f);
                float fill = Mathf.Lerp(0.45f, 1f, frac);
                bobin.localScale = new Vector3(bobinScale.x * fill, bobinScale.y * fill, bobinScale.z);
            }
            if (Mathf.FloorToInt(turns) != Mathf.FloorToInt(before * beatsPerProduct)) PlayNear(SoundId.MachineCreak, 0.5f);
        }

        // Dokuma: her ürün beats mekik geçişi; çerçeve her geçişte inip kalkar; ön rulo kalınlaşır, bez uzar.
        private void Loom(float cycle, float before, float frac)
        {
            float picks = cycle * beatsPerProduct;
            if (shuttle != null) shuttle.localPosition = shuttleBase + Vector3.right * (Mathf.Sin(picks * Mathf.PI) * shuttleTravel);
            if (heddle != null) heddle.localPosition = heddleBase + Vector3.up * (Mathf.Cos(picks * Mathf.PI * 2f) * 0.5f + 0.5f) * heddleTravel;
            if (clothRoll != null)
            {
                float r = Mathf.Lerp(0.7f, 1.35f, frac);
                clothRoll.localScale = new Vector3(rollScale.x, rollScale.y * r, rollScale.z * r);
                clothRoll.localRotation = Quaternion.Euler(-picks * 60f, 0f, 0f);
            }
            if (clothSheet != null)
            {
                clothSheet.GetPropertyBlock(block_);
                block_.SetVector(BaseMapSt, new Vector4(1f, 2f, 0f, picks * 0.12f));
                clothSheet.SetPropertyBlock(block_);
            }
            if (Mathf.FloorToInt(picks + 0.5f) != Mathf.FloorToInt(before * beatsPerProduct + 0.5f)) PlayNear(SoundId.LoomTick, 0.7f);
        }

        // Demirci: eksantrik döner, çekiç kalkar ve her vuruş sonunda örse iner (son vuruş = ürün tamam); körük ve ocak
        // ışığı birlikte nefes alır; vuruşta kıvılcım ve çınlama.
        private void Anvil(float cycle, float before, float frac)
        {
            float beats = cycle * beatsPerProduct;
            float s = beats - Mathf.Floor(beats);
            float lift = s < 0.8f ? Mathf.SmoothStep(0f, 1f, s / 0.8f) : 1f - (s - 0.8f) / 0.2f;
            if (hammer != null) hammer.localRotation = hammerBase * Quaternion.Euler(-lift * 38f, 0f, 0f);
            if (cam != null) cam.localRotation = camBase * Quaternion.Euler(0f, 0f, -beats * 360f);
            float breath = Mathf.Sin(cycle * Mathf.PI * 2f) * 0.5f + 0.5f;
            if (bellows != null) bellows.localScale = new Vector3(bellowsScale.x, bellowsScale.y * Mathf.Lerp(0.45f, 1f, breath), bellowsScale.z);
            if (ember != null) ember.localScale = emberScale * Mathf.Lerp(1.12f, 0.92f, breath);
            // Hurda ocakta kızarır (kopya turuncuya döner), örste çekiçlenir.
            if (feedProxy != null && feedProxy.gameObject.activeSelf)
                feedProxy.localScale = Vector3.one * (1f + Mathf.Sin(frac * 40f) * 0.04f);
            if (Mathf.FloorToInt(beats) == Mathf.FloorToInt(before * beatsPerProduct)) return;
            if (sparks != null) sparks.Emit(10);
            PlayNear(SoundId.Anvil, 0.6f);
        }

        // Vinç: 0-0.30 blok çukurdan kalkar (tambur sarar), 0.30-0.45 kol tezgaha döner, 0.45-0.55 blok iner,
        // 0.55-0.95 keski blokta yontar (beats vuruş), 0.95-1 tuğla çıkışa kayar, kol geri döner.
        private void Crane(float cycle, float before, float frac)
        {
            float rise = Mathf.Clamp01(frac / 0.3f);
            float swing = frac < 0.3f ? 0f : frac < 0.45f ? Mathf.SmoothStep(0f, 1f, (frac - 0.3f) / 0.15f) : frac < 0.95f ? 1f : 1f - (frac - 0.95f) / 0.05f;
            float lower = Mathf.Clamp01((frac - 0.45f) / 0.1f);
            if (boom != null) boom.localRotation = boomBase * Quaternion.Euler(0f, swing * boomSwing, 0f);
            // Tambur halatı sararken döner; kanca halat boyuyla yükselir.
            float wound = rise - lower;
            if (drum != null) drum.localRotation = drumBase * Quaternion.Euler(0f, 0f, wound * 540f);
            if (hook != null) hook.localPosition = hookBase + Vector3.up * (wound * 1.2f - 0.6f);
            if (block != null && blockPit != null && blockBench != null)
            {
                bool carried = frac < 0.55f;
                if (frac < 0.3f) block.position = Vector3.Lerp(blockPit.position, hook.position + Vector3.down * 0.35f, rise);
                else if (carried) block.position = hook.position + Vector3.down * 0.35f;
                else block.position = blockBench.position;
                float carve = Mathf.Clamp01((frac - 0.55f) / 0.4f);
                block.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, carve);
                bool show = frac < 0.95f && (building.HasEnoughInput || frac > 0.001f);
                if (block.gameObject.activeSelf != show) block.gameObject.SetActive(show);
            }
            if (chisel != null)
            {
                float hits = Mathf.Clamp01((frac - 0.55f) / 0.4f) * beatsPerProduct;
                float s = hits - Mathf.Floor(hits);
                float lift = frac < 0.55f || frac > 0.95f ? 1f : (s < 0.7f ? Mathf.SmoothStep(0f, 1f, s / 0.7f) : 1f - (s - 0.7f) / 0.3f);
                chisel.localPosition = chiselBase + Vector3.up * (lift * 0.35f);
                float prevHits = Mathf.Clamp01((before - Mathf.Floor(cycle) - 0.55f) / 0.4f) * beatsPerProduct;
                if (frac > 0.55f && frac < 0.96f && Mathf.FloorToInt(hits) != Mathf.FloorToInt(prevHits))
                {
                    if (dust != null) dust.Emit(6);
                    PlayNear(SoundId.Mine, 0.45f);
                }
            }
        }

        private void SetParticles(bool on)
        {
            if (on == dustOn) return;
            dustOn = on;
            if (water != null) { if (on) water.Play(); else water.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
            if (dust == null || kind == Kind.Crane) return;
            if (on) dust.Play();
            else dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // Oyuncu yakınsa duyulur, uzaklaştıkça kısılır.
        private void PlayNear(SoundId id, float volume)
        {
            if (listener == null) return;
            float d = Vector3.Distance(listener.position, transform.position);
            float v = 1f - Mathf.InverseLerp(config.MachineSoundNear, config.MachineSoundFar, d);
            if (v > 0.05f) AudioManager.Play(id, volume * v * Mathf.Max(0.3f, speed));
        }
    }
}
