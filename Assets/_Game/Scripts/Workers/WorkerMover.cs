// WorkerMover.cs
// İşçinin yürümesi (yerde, XZ düzleminde): verilen hedefe Pathfinder'ın bulduğu yolla (engellerin etrafından) gider.
// Engeller değişirse (yeni bina açılırsa) yolu yeniden hesaplar. Kinematic Rigidbody ile hareket eder;
// bu yüzden oyuncu ve diğer işçilerle birbirini itmez, içlerinden geçer. Dönme ve animasyon CharacterVisual'da.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Rigidbody))]
    public class WorkerMover : MonoBehaviour
    {
        [Tooltip("Engellerden ne kadar uzak durulacağı (işçinin yarı genişliği kadar).")]
        [SerializeField] private float clearance = 0.45f;
        [Tooltip("Kıyıdan ne kadar içeride kalır (metre).")]
        [SerializeField] private float coastMargin = 1.2f;

        private readonly List<Vector2> path = new List<Vector2>();
        private Rigidbody body;
        private int pathIndex;
        private Vector2 destination;
        private bool hasDestination;
        private int obstacleVersion;

        // Yürüme hızı; sahibi (Worker) her karede WorkerType'tan ayarlar.
        public float Speed { get; set; }

        // Hedefe varıldı mı? (Hedef yoksa false.)
        public bool HasArrived => hasDestination && pathIndex >= path.Count;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        // Bu noktaya yürümeye başla. Aynı hedef tekrar verilirse yol yeniden hesaplanmaz.
        public void MoveTo(Vector3 target)
        {
            Vector2 flat = new Vector2(target.x, target.z);
            if (IslandShape.Instance != null) flat = IslandShape.Instance.ClampInside(flat, coastMargin);
            if (hasDestination && flat == destination) return;

            destination = flat;
            hasDestination = true;
            Repath();
        }

        // Olduğu yerde dur.
        public void Stop()
        {
            hasDestination = false;
            path.Clear();
            pathIndex = 0;
        }

        private void FixedUpdate()
        {
            if (!hasDestination) return;
            if (obstacleVersion != NavObstacle.Version) Repath(); // bina açıldı/kapandı

            if (pathIndex >= path.Count) return;

            Vector2 position = new Vector2(body.position.x, body.position.z);
            Vector2 waypoint = path[pathIndex];
            Vector2 next = Vector2.MoveTowards(position, waypoint, Speed * Time.fixedDeltaTime);
            body.MovePosition(new Vector3(next.x, 0f, next.y));
            if (next == waypoint) pathIndex++;
        }

        private void Repath()
        {
            Vector2 position = new Vector2(body.position.x, body.position.z);
            Pathfinder.FindPath(position, destination, clearance, path);
            pathIndex = 0;
            obstacleVersion = NavObstacle.Version;
        }
    }
}
