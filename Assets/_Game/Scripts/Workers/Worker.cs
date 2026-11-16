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
        private float cheerTimer;
        private CharacterVisual visual;

        private void Update()
        {
            // Her karede: Play sırasında ayar dosyasında yapılan değişiklik hemen etkilesin.
            ApplyStats();
            if (celebrating) { UpdateCelebration(); return; }
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
            Stack.WorkTarget = zone;
            Mover.MoveTo(zone.GetStandPoint(transform.position));
        }

        // Yapacak iş yok: olduğu yerde bekle.
        protected void Wait()
        {
            Stack.WorkTarget = null;
            Mover.Stop();
        }

        private void ApplyStats()
        {
            Stack.Capacity = type.CarryCapacity + UpgradeManager.Extra(UpgradeStat.WorkerCapacity, type.CarryCapacity);
            Mover.Speed = type.MoveSpeed * UpgradeManager.Multiplier(UpgradeStat.WorkerSpeed);
        }
    }
}
