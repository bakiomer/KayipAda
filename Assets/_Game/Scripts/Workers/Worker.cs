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

        private void Update()
        {
            // Her karede: Play sırasında ayar dosyasında yapılan değişiklik hemen etkilesin.
            ApplyStats();
            if (!gathering) Think();
        }

        // Final töreni: işi bırakıp verilen noktada toplan.
        public void Gather(Vector3 point)
        {
            gathering = true;
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
