// WorkerMover.cs
// İşçinin yürümesi: verilen hedefe Pathfinder'ın bulduğu yolla (engellerin etrafından) gider.
// Engeller değişirse (yeni bina açılırsa) yolu yeniden hesaplar. Kinematic Rigidbody2D ile hareket eder;
// bu yüzden oyuncu ve diğer işçilerle birbirini itmez, içlerinden geçer.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class WorkerMover : MonoBehaviour
    {
        [Tooltip("İşçinin yürüyebileceği dikdörtgen (dünya koordinatı). Harita yerleşimine aittir.")]
        [SerializeField] private Rect moveArea = new Rect(-5f, -5f, 10f, 10f);
        [Tooltip("Engellerden ne kadar uzak durulacağı (işçinin yarı genişliği kadar).")]
        [SerializeField] private float clearance = 0.4f;

        private readonly List<Vector2> path = new List<Vector2>();
        private Rigidbody2D body;
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
            body = GetComponent<Rigidbody2D>();
        }

        // Bu noktaya yürümeye başla. Aynı hedef tekrar verilirse yol yeniden hesaplanmaz.
        public void MoveTo(Vector2 target)
        {
            target.x = Mathf.Clamp(target.x, moveArea.xMin, moveArea.xMax);
            target.y = Mathf.Clamp(target.y, moveArea.yMin, moveArea.yMax);
            if (hasDestination && target == destination) return;

            destination = target;
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

            Vector2 waypoint = path[pathIndex];
            Vector2 next = Vector2.MoveTowards(body.position, waypoint, Speed * Time.fixedDeltaTime);
            body.MovePosition(next);
            if (next == waypoint) pathIndex++;
        }

        private void Repath()
        {
            Pathfinder.FindPath(body.position, destination, clearance, moveArea, path);
            pathIndex = 0;
            obstacleVersion = NavObstacle.Version;
        }
    }
}
