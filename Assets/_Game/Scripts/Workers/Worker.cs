// Worker.cs
// Tüm işçilerin ortak temeli: ayarlarını (hız, kapasite) her karede WorkerType'tan uygular,
// bir alana yürümeyi ve beklemeyi sağlar. Ne zaman ne yapacağına (durum makinesi) alt sınıf karar verir:
// LumberjackWorker (Oduncu) ve CarrierWorker (Taşıyıcı).

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(CarryStack), typeof(WorkerMover))]
    public abstract class Worker : MonoBehaviour
    {
        [SerializeField] private WorkerType type;

        protected CarryStack Stack { get; private set; }
        protected WorkerMover Mover { get; private set; }

        // Gidilen yere varıldı mı?
        protected bool HasArrived => Mover.HasArrived;

        protected virtual void Awake()
        {
            Stack = GetComponent<CarryStack>();
            Mover = GetComponent<WorkerMover>();
            if (type == null)
            {
                Debug.LogError(name + ": Worker Type atanmamış!", this);
                enabled = false;
                return;
            }
            ApplyStats();
        }

        private bool gathering;
        private bool celebrating;
        private float idleTime;
        private bool resting;
        private float restTimer;
        private Transform seat;

        // Boşta kalınca kamp ateşinin başında kısa süre dinleniyor mu?
        public bool IsResting => resting;
        private float cheerTimer;
        private CharacterVisual visual;

        private void Update()
        {
            // Her karede: Play sırasında ayar dosyasında yapılan değişiklik hemen etkilesin.
            ApplyStats();
            if (celebrating) { UpdateCelebration(); return; }
            if (resting) { UpdateRest(); return; }
            if (!gathering) Think();
        }

        // Barakadan yeni çıkan işçi: önce kapının önüne yürür, kısa bir sevinç yapar, sonra işe koşar.
        public void Celebrate(Vector3 point, float seconds)
        {
            celebrating = true;
            cheerTimer = seconds;
            Stack.WorkTarget = null;
            Mover.MoveTo(point);
        }

        private void UpdateCelebration()
        {
            if (!Mover.HasArrived) return;
            if (visual == null) visual = GetComponentInChildren<CharacterVisual>();
            if (cheerTimer > 0f && visual != null) visual.SetCheer(true);
            cheerTimer -= Time.deltaTime;
            if (cheerTimer > 0f) return;
            if (visual != null) visual.SetCheer(false);
            celebrating = false;
        }

        // Final töreni: işi bırakıp verilen noktada toplan.
        public void Gather(Vector3 point)
        {
            StopRest();
            gathering = true;
            celebrating = false;
            Stack.WorkTarget = null;
            Mover.MoveTo(point);
        }

        // Alt sınıfın durum makinesi: her karede bir kez çağrılır.
        protected abstract void Think();

        // Bir alana yürü. İşçi bu alanı hedef alır; alan da sadece hedefi olan işçiye hizmet eder.
        protected void GoTo(InteractionZone zone)
        {
            idleTime = 0f;
            Stack.WorkTarget = zone;
            Mover.MoveTo(zone.GetStandPoint(transform.position));
        }

        // Yapacak iş yok: olduğu yerde bekle.
        protected void Wait()
        {
            Stack.WorkTarget = null;
            Mover.Stop();
            // Uzun süre boş kalınca kamp ateşinin başına gelip dinlenir (sonra işe döner).
            idleTime += Time.deltaTime;
            CampSite camp = CampSite.Instance;
            if (camp == null || !Stack.IsEmpty || idleTime < camp.RestAfter) return;
            seat = camp.ClaimSeat(this);
            if (seat == null) { idleTime = 0f; return; }
            resting = true;
            restTimer = camp.RestTime;
            Mover.MoveTo(seat.position);
        }

        private void UpdateRest()
        {
            if (!Mover.HasArrived) return;
            CampSite camp = CampSite.Instance;
            if (visual == null) visual = GetComponentInChildren<CharacterVisual>();
            if (visual != null && camp != null) visual.Face(camp.FirePosition - transform.position);
            restTimer -= Time.deltaTime;
            if (restTimer <= 0f) StopRest();
        }

        private void StopRest()
        {
            if (!resting) return;
            resting = false;
            idleTime = 0f;
            if (CampSite.Instance != null) CampSite.Instance.ReleaseSeat(this);
        }

        private void ApplyStats()
        {
            Stack.Capacity = type.CarryCapacity + UpgradeManager.Extra(UpgradeStat.WorkerCapacity, type.CarryCapacity);
            Mover.Speed = type.MoveSpeed * UpgradeManager.Multiplier(UpgradeStat.WorkerSpeed);
        }
    }
}
