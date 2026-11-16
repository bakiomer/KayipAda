// IslandLife.cs
// Adadaki canlılık (abartısız, telefonda ucuz): sahilde yengeçler, gökyüzünde martılar (ara sıra uzaktan martı sesi),
// sudan arada bir zıplayan küçük balıklar, bahçede kelebekler. Hepsi oyun başında oluşturulur; hiçbirinin çarpışması
// yoktur, toplama ve yürümeyi etkilemez. Sayılar ve süreler GameConfig'in "Canlılar" bölümünde.

using UnityEngine;

namespace KayipAda
{
    public class IslandLife : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Crab crabPrefab;
        [SerializeField] private Flyer gullPrefab;
        [SerializeField] private Flyer butterflyPrefab;
        [SerializeField] private GameObject fishPrefab;
        [Tooltip("Kelebeklerin uçtuğu alanın ortası (bahçe) ve yarıçapı.")]
        [SerializeField] private Vector3 butterflyCenter;
        [SerializeField] private float butterflyRadius = 2.5f;

        private float nextFish;
        private float nextGullCall;

        private void Start()
        {
            IslandShape island = IslandShape.Instance;
            if (island == null) return;
            PlayerController player = FindAnyObjectByType<PlayerController>();
            Transform playerT = player != null ? player.transform : null;

            for (int i = 0; i < config.CrabCount && crabPrefab != null; i++)
            {
                Crab crab = Instantiate(crabPrefab, RandomBeachPoint(), Quaternion.identity, transform);
                crab.Setup(this, playerT);
            }
            Vector3 islandCenter = new Vector3(island.Center.x, 0f, island.Center.y);
            for (int i = 0; i < config.GullCount && gullPrefab != null; i++)
            {
                Flyer gull = Instantiate(gullPrefab, islandCenter, Quaternion.identity, transform);
                gull.Setup(islandCenter + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f)),
                           Random.Range(config.GullCircleMin, config.GullCircleMax));
            }
            for (int i = 0; i < config.ButterflyCount && butterflyPrefab != null; i++)
            {
                Flyer b = Instantiate(butterflyPrefab, butterflyCenter, Quaternion.identity, transform);
                b.Setup(butterflyCenter, butterflyRadius);
            }
            nextFish = Time.time + config.FishJumpInterval;
            nextGullCall = Time.time + Random.Range(config.GullCallIntervalMin, config.GullCallIntervalMax);
        }

        private void Update()
        {
            if (Time.time >= nextFish && fishPrefab != null)
            {
                nextFish = Time.time + config.FishJumpInterval * Random.Range(0.7f, 1.3f);
                JumpFish();
            }
            if (config.GullCount > 0 && Time.time >= nextGullCall)
            {
                nextGullCall = Time.time + Random.Range(config.GullCallIntervalMin, config.GullCallIntervalMax);
                AudioManager.Play(SoundId.Seagull);
            }
        }

        // Kıyının biraz açığında bir noktadan küçük bir balık kavis çizerek zıplar; çıkarken ve girerken küçük sıçrama.
        private void JumpFish()
        {
            IslandShape island = IslandShape.Instance;
            Vector3 from = RandomSeaPoint(island);
            Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            Vector3 to = from + dir * Random.Range(1.2f, 2f);
            GameObject fish = Instantiate(fishPrefab, from, Quaternion.LookRotation(dir), transform);
            if (Effects.Instance != null) Effects.Instance.SmallSplash(from);
            Tween.MoveArc(fish.transform, from, () => to, config.FishJumpHeight, config.FishJumpDuration, () =>
            {
                if (Effects.Instance != null) Effects.Instance.SmallSplash(to);
                Destroy(fish);
            });
            Tween.Value(fish, config.FishJumpDuration, k => fish.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(Mathf.Lerp(-45f, 45f, k), 0f, 0f));
        }

        private Vector3 RandomSeaPoint(IslandShape island)
        {
            for (int i = 0; i < 30; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 coast = island.CoastPoint(a);
                Vector2 outward = (coast - island.Center).normalized;
                Vector2 p = coast + outward * Random.Range(2f, 5f);
                if (island.SignedDistance(p) > 1.5f) return new Vector3(p.x, -0.1f, p.y);
            }
            return Vector3.zero;
        }

        // Kum şeridinde (kıyıya yakın, çimenlikten önce) rastgele nokta.
        public Vector3 RandomBeachPoint()
        {
            IslandShape island = IslandShape.Instance;
            for (int i = 0; i < 40; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 coast = island.CoastPoint(a);
                Vector2 inward = (island.Center - coast).normalized;
                Vector2 p = coast + inward * Random.Range(0.8f, 2.4f);
                Vector3 w = new Vector3(p.x, 0f, p.y);
                if (IsBeach(w) && NavObstacle.PushOut(p, 0.5f) == p) return w;
            }
            return transform.position;
        }

        public bool IsBeach(Vector3 p)
        {
            IslandShape island = IslandShape.Instance;
            if (island == null) return false;
            float sd = island.SignedDistance(new Vector2(p.x, p.z));
            return sd < -config.CrabBeachInner && sd > -config.CrabBeachOuter;
        }
    }
}
