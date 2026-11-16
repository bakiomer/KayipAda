// WorkerSpawner.cs
// Tayfa Barakası'nın kapısındaki işçi çıkış noktası (her işçi türü için bir tane: Oduncular, Taşıyıcılar, Lifçiler).
// CrewManager'daki tutulan sayıya ulaşana kadar işçileri kapıdan sırayla çıkarır. Oyun sırasında tutulan işçi
// kapının önüne yürür, kısa bir sevinç yapar (konfeti) ve işe koşar. Kayıttan gelenler doğrudan işe başlar.
// İşçiler sahnede gizli duran hazır bir örnekten (template) kopyalanır; böylece ağaç/bina bağlantıları hazır gelir.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class WorkerSpawner : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private WorkerType workerType;
        [Tooltip("Gizli hazır işçi; kopyaları sahneye çıkar.")]
        [SerializeField] private Worker template;
        [Tooltip("İşçilerin çıktığı nokta (baraka kapısı).")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Yeni tutulan işçinin sevinç yaptığı nokta (kapının önü).")]
        [SerializeField] private Transform cheerPoint;

        private readonly List<Worker> workers = new List<Worker>();
        private float timer;
        private int celebrateFrom = -1; // bu sıradan sonraki işçiler sevinçle çıkar (oyun sırasında tutulanlar)

        public WorkerType Type => workerType;
        public int SpawnedCount => workers.Count;

        private void Awake()
        {
            if (config == null || workerType == null || template == null || spawnPoint == null)
            {
                Debug.LogError(name + ": İşçi çıkış noktasının ayarları eksik!", this);
                enabled = false;
                return;
            }
            template.gameObject.SetActive(false); // hazır örnek hiç çalışmaz
        }

        private void Start()
        {
            if (CrewManager.Instance != null) CrewManager.Instance.Changed += OnCrewChanged;
        }

        private void OnDestroy()
        {
            if (CrewManager.Instance != null) CrewManager.Instance.Changed -= OnCrewChanged;
        }

        private void OnCrewChanged(WorkerType type)
        {
            if (type == workerType && celebrateFrom < 0 && Time.timeSinceLevelLoad > 0.5f) celebrateFrom = workers.Count;
        }

        private void Update()
        {
            if (CrewManager.Instance == null) return;
            if (workers.Count >= CrewManager.Instance.GetHired(workerType)) return; // sayı her karede okunur

            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                return;
            }
            timer = config.WorkerSpawnInterval;
            Spawn();
        }

        private void Spawn()
        {
            Worker worker = Instantiate(template, spawnPoint.position, spawnPoint.rotation);
            worker.name = workerType.DisplayName + " " + (workers.Count + 1);
            worker.gameObject.SetActive(true);
            bool celebrate = celebrateFrom >= 0 && workers.Count >= celebrateFrom && cheerPoint != null;
            workers.Add(worker);
            Tween.PopIn(worker.transform, Vector3.one, config.PopInDuration); // kapıdan zıplayarak çıkar
            if (!celebrate) return;
            worker.Celebrate(cheerPoint.position, config.HireCheerTime);
            if (Effects.Instance != null)
            {
                Effects.Instance.Confetti(cheerPoint.position + Vector3.up * 1.2f);
                Effects.Instance.ShowText(cheerPoint.position + Vector3.up * 2.6f, workerType.DisplayName + " işe başladı!", new Color(0.6f, 1f, 0.6f));
            }
            AudioManager.Play(SoundId.Unlock, 0.7f);
        }
    }
}
