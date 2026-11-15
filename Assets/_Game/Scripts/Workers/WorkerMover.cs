// WorkerMover.cs
// İşçinin yürümesi (yerde, XZ düzleminde): verilen hedefe NavMesh'in (IslandNavMesh) bulduğu yolla gider;
// NavMesh hazır değilse eski yol bulucuyu (Pathfinder) kullanır. Engeller ya da NavMesh değişirse yolu yeniden
// hesaplar. Bir yerde takılırsa (kısa sürede hiç ilerleyemezse) yolu yeniden bulur. Kinematic Rigidbody ile hareket eder;
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
        private int navVersion = -1;
        private Vector2 stuckFrom;
        private float stuckTimer;
        private const float StuckTime = 1.2f;      // bu sürede ...
        private const float StuckDistance = 0.15f; // ... bu kadar ilerleyemezse takıldı sayılır

        // Yürüme hızı; sahibi (Worker) her karede WorkerType'tan ayarlar.
        public float Speed { get; set; }

        // Hedefe varıldı mı? (Hedef yoksa false.)
        public bool HasArrived => hasDestination && pathIndex >= path.Count;

        // Şu an bir yere yürüyor mu? (Hedefi var ve henüz varmadı.)
        public bool IsWalking => hasDestination && pathIndex < path.Count && Speed > 0.01f;

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
            int nav = IslandNavMesh.Instance != null ? IslandNavMesh.Instance.BuiltVersion : -1;
            if (obstacleVersion != NavObstacle.Version || nav != navVersion) Repath(); // bina açıldı/kapandı, NavMesh yenilendi

            if (pathIndex >= path.Count) return;

            Vector2 position = new Vector2(body.position.x, body.position.z);
            Vector2 waypoint = path[pathIndex];
            Vector2 next = Vector2.MoveTowards(position, waypoint, Speed * Time.fixedDeltaTime);
            body.MovePosition(new Vector3(next.x, 0f, next.y));
            if (next == waypoint) pathIndex++;

            // Takılma kontrolü.
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= StuckTime)
            {
                if ((position - stuckFrom).sqrMagnitude < StuckDistance * StuckDistance && Speed > 0.01f) Repath();
                stuckFrom = position;
                stuckTimer = 0f;
            }
        }

        private void Repath()
        {
            Vector2 position = new Vector2(body.position.x, body.position.z);
            if (!IslandNavMesh.TryFindPath(position, destination, path)) Pathfinder.FindPath(position, destination, clearance, path);
            else if (path.Count > 0) path[path.Count - 1] = destination; // son nokta tam hedef (alanın içi)
            pathIndex = 0;
            obstacleVersion = NavObstacle.Version;
            navVersion = IslandNavMesh.Instance != null ? IslandNavMesh.Instance.BuiltVersion : -1;
            stuckFrom = position;
            stuckTimer = 0f;
        }
    }
}
