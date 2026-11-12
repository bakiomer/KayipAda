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

        private void Update()
        {
            // Her karede: Play sırasında ayar dosyasında yapılan değişiklik hemen etkilesin.
            ApplyStats();
            Think();
        }

        // Alt sınıfın durum makinesi: her karede bir kez çağrılır.
        protected abstract void Think();

        // Bir alana yürü. İşçi bu alanı hedef alır; alan da sadece hedefi olan işçiye hizmet eder.
        protected void GoTo(InteractionZone zone)
        {
            Stack.WorkTarget = zone;
            Mover.MoveTo(zone.transform.position);
        }

        // Yapacak iş yok: olduğu yerde bekle.
        protected void Wait()
        {
            Stack.WorkTarget = null;
            Mover.Stop();
        }

        private void ApplyStats()
        {
            Stack.Capacity = type.CarryCapacity;
            Mover.Speed = type.MoveSpeed;
        }
    }
}
