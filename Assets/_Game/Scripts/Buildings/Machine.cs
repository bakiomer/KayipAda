// Machine.cs
// İşleme binasının görünen makinesi; SADECE bina işlerken hareket eder (yavaşça hızlanır/durur).
// - Halat çarkı (RopeWheel): büyük ahşap çark ve kolu döner, lif teli titreşerek akar, makara dolar (işlem ilerledikçe
//   kalınlaşır); her turda ahşap gıcırtısı, arada lif sürtünme sesi, lif tozu.
// - Dokuma tezgahı (Loom): mekik sağa-sola gider, tarak (çerçeve) inip kalkar, ön silindir döner ve kumaş uzar;
//   mekik her uca varınca ritmik "tık-tak" sesi.
// Sesler oyuncuya yakınken duyulur (GameConfig > Bina tarifleri ve makineler).

using UnityEngine;

namespace KayipAda
{
    public class Machine : MonoBehaviour
    {
        public enum Kind { RopeWheel, Loom }

        [SerializeField] private GameConfig config;
        [SerializeField] private ProcessingBuilding building;
        [SerializeField] private Kind kind;

        [Header("Halat çarkı")]
        [SerializeField] private Transform wheel;
        [SerializeField] private Transform crank;
        [SerializeField] private Transform spool;
        [SerializeField] private Transform strand;

        [Header("Dokuma tezgahı")]
        [SerializeField] private Transform shuttle;
        [SerializeField] private float shuttleTravel = 0.7f;
        [SerializeField] private Transform beater;
        [SerializeField] private float beaterTravel = 0.18f;
        [SerializeField] private Transform roller;
        [SerializeField] private Transform cloth;

        [Header("Ortak")]
        [Tooltip("İşlerken çıkan lif tozu (boş olabilir).")]
        [SerializeField] private ParticleSystem dust;
        [Tooltip("Tur başına saniye (normal hızda).")]
        [SerializeField] private float cycleTime = 1.2f;

        private float speed;     // 0..1: yumuşak hızlanma/durma
        private float phase;     // tur sayacı
        private Quaternion wheelBase, crankBase, spoolBase, rollerBase;
        private Vector3 shuttleBase, beaterBase, strandBase, spoolScale, clothScale;
        private Transform listener;
        private bool dustOn;

        public bool IsMoving => speed > 0.05f;

        private void Awake()
        {
            if (wheel != null) wheelBase = wheel.localRotation;
            if (crank != null) crankBase = crank.localRotation;
            if (spool != null) { spoolBase = spool.localRotation; spoolScale = spool.localScale; }
            if (strand != null) strandBase = strand.localPosition;
            if (shuttle != null) shuttleBase = shuttle.localPosition;
            if (beater != null) beaterBase = beater.localPosition;
            if (roller != null) rollerBase = roller.localRotation;
            if (cloth != null) clothScale = cloth.localScale;
        }

        private void Start()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            listener = player != null ? player.transform : null;
            if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (building == null) return;
            bool working = building.IsWorking;
            speed = Mathf.MoveTowards(speed, working ? 1f : 0f, Time.deltaTime * 2.5f);
            SetDust(working);
            if (speed <= 0f) return;

            float rate = config.MachineSpeed * UpgradeManager.Multiplier(UpgradeStat.BuildingSpeed) / Mathf.Max(0.1f, cycleTime);
            float before = phase;
            phase += Time.deltaTime * speed * rate;
            float progress = building.Progress;

            if (kind == Kind.RopeWheel) AnimateRope(before, progress);
            else AnimateLoom(before, progress);
        }

        private void AnimateRope(float before, float progress)
        {
            float angle = phase * 360f;
            if (wheel != null) wheel.localRotation = wheelBase * Quaternion.Euler(0f, 0f, angle);
            if (crank != null) crank.localRotation = crankBase * Quaternion.Euler(0f, 0f, angle);
            if (spool != null)
            {
                spool.localRotation = spoolBase * Quaternion.Euler(0f, 0f, -angle * 3f);
                float fill = Mathf.Lerp(0.55f, 1f, progress); // işlem ilerledikçe makara kalınlaşır
                spool.localScale = new Vector3(spoolScale.x * fill, spoolScale.y * fill, spoolScale.z);
            }
            if (strand != null) strand.localPosition = strandBase + Vector3.up * (Mathf.Sin(phase * Mathf.PI * 8f) * 0.02f * speed);

            if (Mathf.FloorToInt(phase) != Mathf.FloorToInt(before)) PlayNear(SoundId.MachineCreak, 0.55f);
            if (Mathf.FloorToInt(phase * 2f + 0.5f) != Mathf.FloorToInt(before * 2f + 0.5f)) PlayNear(SoundId.MachineFriction, 0.4f);
        }

        private void AnimateLoom(float before, float progress)
        {
            // Mekik: bir turda bir kez sağa, bir kez sola (her uçta "tık").
            if (shuttle != null) shuttle.localPosition = shuttleBase + Vector3.right * (Mathf.Sin(phase * Mathf.PI) * shuttleTravel);
            // Tarak: mekik her geçtiğinde bir kez inip kalkar.
            if (beater != null) beater.localPosition = beaterBase + Vector3.up * ((1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.5f * beaterTravel);
            if (roller != null) roller.localRotation = rollerBase * Quaternion.Euler(phase * 40f, 0f, 0f);
            if (cloth != null) cloth.localScale = new Vector3(clothScale.x, clothScale.y, clothScale.z * Mathf.Lerp(0.35f, 1f, progress));

            // Mekik uca vardı (sinüs tepe/dip): yarım tur sınırları 0.5, 1.5, 2.5...
            if (Mathf.FloorToInt(phase + 0.5f) != Mathf.FloorToInt(before + 0.5f)) PlayNear(SoundId.LoomTick, 0.7f);
        }

        private void SetDust(bool on)
        {
            if (dust == null || on == dustOn) return;
            dustOn = on;
            if (on) dust.Play();
            else dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // Oyuncu yakınsa duyulur, uzaklaştıkça kısılır.
        private void PlayNear(SoundId id, float volume)
        {
            if (listener == null) return;
            float d = Vector3.Distance(listener.position, transform.position);
            float v = 1f - Mathf.InverseLerp(config.MachineSoundNear, config.MachineSoundFar, d);
            if (v > 0.05f) AudioManager.Play(id, volume * v * speed);
        }
    }
}
