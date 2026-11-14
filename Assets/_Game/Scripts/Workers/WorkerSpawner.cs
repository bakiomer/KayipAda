// WorkerSpawner.cs
// Kamptaki işçi çıkış noktası (her görev için bir tane: Oduncular, Taşıyıcılar).
// Görevin açma karesi (hireArea) ödenince, WorkerType.Count kadar işçiyi kamptan sırayla çıkarır.
// İşçiler, sahnede gizli duran hazır bir örnekten (template) kopyalanır; böylece ağaç/bina bağlantıları hazır gelir.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class WorkerSpawner : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Bu açılınca (açma karesi ödenince) işçiler çalışmaya başlar.")]
        [SerializeField] private AreaDefinition hireArea;
        [SerializeField] private WorkerType workerType;
        [Tooltip("Gizli hazır işçi; kopyaları sahneye çıkar.")]
        [SerializeField] private Worker template;
        [Tooltip("İşçilerin kamptan çıktığı nokta.")]
        [SerializeField] private Transform spawnPoint;

        private readonly List<Worker> workers = new List<Worker>();
        private float timer;

        private void Awake()
        {
            if (config == null || hireArea == null || workerType == null || template == null || spawnPoint == null)
            {
                Debug.LogError(name + ": İşçi çıkış noktasının ayarları eksik!", this);
                enabled = false;
                return;
            }
            template.gameObject.SetActive(false); // hazır örnek hiç çalışmaz
        }

        private void Update()
        {
            if (!AreaManager.Instance.IsUnlocked(hireArea)) return;
            if (workers.Count >= workerType.Count) return; // sayı ayar dosyasından her karede okunur

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
            Worker worker = Instantiate(template, spawnPoint.position, Quaternion.identity);
            worker.name = workerType.DisplayName + " " + (workers.Count + 1);
            worker.gameObject.SetActive(true);
            workers.Add(worker);
            Tween.PopIn(worker.transform, Vector3.one, config.PopInDuration); // kamptan zıplayarak çıkar
        }
    }
}
