// IslandNavMesh.cs
// İşçilerin yürüyebildiği alan (Unity NavMesh): ada zemininden kurulur; açık olan tüm engeller (bina gövdesi,
// ağaç/kaya gövdesi, çitler, kapalı kapılar, gemi) yürünemez kutu olarak oyulur. Engeller değiştiğinde
// (bölge/bina açıldı, kapı açıldı, gemi suya indi) NavMesh arka planda yeniden kurulur.
// İşçiler (WorkerMover) yolu buradan sorar; NavMesh hazır değilse eski yol bulucuya (Pathfinder) düşer.
// Ek paket gerekmez (UnityEngine.AI modülü). Oyuncu NavMesh kullanmaz; engellere çarpıp kayar.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace KayipAda
{
    [DefaultExecutionOrder(-50)]
    public class IslandNavMesh : MonoBehaviour
    {
        public static IslandNavMesh Instance { get; private set; }

        [SerializeField] private MeshFilter ground;
        [Tooltip("Adacıkların zeminleri (köprüyle bağlanınca yürünebilir).")]
        [SerializeField] private MeshFilter[] extraGrounds = new MeshFilter[0];
        [Tooltip("İşçinin yarıçapı (engellerden bu kadar uzak durur).")]
        [SerializeField] private float agentRadius = 0.4f;
        [Tooltip("Yürünebilen en dik eğim (derece). Ada kenarındaki suya inen etek bundan diktir.")]
        [SerializeField] private float maxSlope = 12f;
        [Tooltip("Yol bulmada hücre boyu (metre). Küçüldükçe daha hassas, daha yavaş.")]
        [SerializeField] private float voxelSize = 0.15f;

        private NavMeshData data;
        private NavMeshDataInstance instance;
        private int builtVersion = -1;
        private AsyncOperation pending;
        private int pendingVersion;
        private readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();

        public bool IsReady => data != null && builtVersion >= 0;
        public int BuiltVersion => builtVersion;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            data = new NavMeshData(0);
            instance = NavMesh.AddNavMeshData(data);
            Rebuild(false); // ilk kurulum hemen (işçiler ilk karede yol ister)
        }

        private void OnDestroy()
        {
            instance.Remove();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (pending != null && !pending.isDone) return;
            if (pending != null)
            {
                builtVersion = pendingVersion;
                pending = null;
            }
            if (NavObstacle.Version != builtVersion) Rebuild(true);
        }

        private void Rebuild(bool async)
        {
            CollectSources();
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = agentRadius;
            settings.agentHeight = 1.8f;
            settings.agentClimb = 0.2f;
            settings.agentSlope = maxSlope;
            settings.overrideVoxelSize = true;
            settings.voxelSize = voxelSize;
            Bounds bounds = ground != null ? ground.GetComponent<Renderer>().bounds : new Bounds(Vector3.zero, Vector3.one * 100f);
            foreach (MeshFilter extra in extraGrounds) if (extra != null) bounds.Encapsulate(extra.GetComponent<Renderer>().bounds);
            foreach (Pier pier in Pier.All) if (pier != null) { bounds.Encapsulate(pier.EndA); bounds.Encapsulate(pier.EndB); }
            bounds.Expand(new Vector3(2f, 6f, 2f));
            int version = NavObstacle.Version;
            if (async)
            {
                pending = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, bounds);
                pendingVersion = version;
            }
            else
            {
                NavMeshBuilder.UpdateNavMeshData(data, settings, sources, bounds);
                builtVersion = version;
            }
        }

        private void CollectSources()
        {
            sources.Clear();
            if (ground != null && ground.sharedMesh != null)
            {
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = ground.sharedMesh,
                    transform = ground.transform.localToWorldMatrix,
                    area = 0
                });
            }
            foreach (MeshFilter extra in extraGrounds)
            {
                if (extra == null || extra.sharedMesh == null) continue;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = extra.sharedMesh,
                    transform = extra.transform.localToWorldMatrix,
                    area = 0
                });
            }
            // Tamamlanmış köprüler: üstü yürünebilir şerit (iki kıyıya biraz taşar).
            foreach (BridgeBuilder bridge in BridgeBuilder.Open)
            {
                if (bridge == null) continue;
                Vector3 a = bridge.EndA, b = bridge.EndB;
                Vector3 mid = (a + b) * 0.5f;
                Vector3 d = b - a;
                d.y = 0f;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(new Vector3(mid.x, -0.1f, mid.z), Quaternion.LookRotation(d.normalized), Vector3.one),
                    size = new Vector3(bridge.HalfWidth * 2f - 0.3f, 0.2f, d.magnitude + 1f),
                    area = 0
                });
            }
            // Denize uzanan iskele: üstü yürünebilir.
            foreach (Pier pier in Pier.All)
            {
                if (pier == null) continue;
                Vector3 a = pier.EndA, b = pier.EndB, mid = (a + b) * 0.5f, d = b - a;
                d.y = 0f;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(new Vector3(mid.x, -0.1f, mid.z), Quaternion.LookRotation(d.normalized), Vector3.one),
                    size = new Vector3(pier.HalfWidth * 2f - 0.3f, 0.2f, d.magnitude + 1f),
                    area = 0
                });
            }
            foreach (NavObstacle o in NavObstacle.Active)
            {
                Vector2 c = o.Center;
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.ModifierBox,
                    transform = Matrix4x4.TRS(new Vector3(c.x, 0.5f, c.y), Quaternion.Euler(0f, o.Yaw, 0f), Vector3.one),
                    size = new Vector3(o.Size.x, 4f, o.Size.y),
                    area = 1 // yürünemez
                });
            }
        }

        // from → to arası yol (XZ köşe noktaları). NavMesh hazır değilse ya da yol yoksa false.
        public static bool TryFindPath(Vector2 from, Vector2 to, List<Vector2> result)
        {
            result.Clear();
            if (Instance == null || !Instance.IsReady) return false;
            if (!NavMesh.SamplePosition(new Vector3(from.x, 0f, from.y), out NavMeshHit a, 2f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(new Vector3(to.x, 0f, to.y), out NavMeshHit b, 2.5f, NavMesh.AllAreas)) return false;
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path)) return false;
            if (path.status == NavMeshPathStatus.PathInvalid) return false;
            Vector3[] corners = path.corners;
            for (int i = 1; i < corners.Length; i++) result.Add(new Vector2(corners[i].x, corners[i].z));
            if (result.Count == 0) result.Add(new Vector2(b.position.x, b.position.z));
            return true;
        }

        // Testler için: iki nokta arasında tam (kesintisiz) yol var mı?
        public static bool HasCompletePath(Vector3 from, Vector3 to)
        {
            if (Instance == null || !Instance.IsReady) return false;
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 1.5f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(to, out NavMeshHit b, 1.5f, NavMesh.AllAreas)) return false;
            NavMeshPath path = new NavMeshPath();
            return NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        // Testler için: NavMesh güncel mi (tüm engel değişiklikleri işlendi mi)?
        public static bool IsUpToDate => Instance != null && Instance.IsReady && Instance.builtVersion == NavObstacle.Version && Instance.pending == null;
    }
}
