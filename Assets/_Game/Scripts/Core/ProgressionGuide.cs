// ProgressionGuide.cs
// Oyuncu hep bir sonraki hedefi görsün: açılmayı bekleyen en ucuz açma karesinin üstünde zıplayan bir ok durur
// ve karenin halkası nabız gibi atar. Hedef ekranın dışındaysa ekranın kenarında hedefi gösteren bir ok çıkar.
// Açılacak kare kalmadıysa hedef gemi teslim alanıdır (gemi bitmemişse).

using UnityEngine;

namespace KayipAda
{
    public class ProgressionGuide : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Hedefin üstünde zıplayan ok (dünyada).")]
        [SerializeField] private Transform worldArrow;
        [Tooltip("Hedef ekran dışındayken kenarda duran ok (arayüzde).")]
        [SerializeField] private RectTransform screenArrow;
        [SerializeField] private Camera cam;
        [Tooltip("Tayfa Barakası: açılmış ama hiç tutulmamış işçi türü varsa (ve en ucuz hedefse) ok buraya bakar.")]
        [SerializeField] private Transform crewStation;
        [Tooltip("Gemi bu malzemeyi isteyip köprü bitmemişse ok köprüyü gösterir (çivi → Batık Koyu köprüsü).")]
        [SerializeField] private BridgeBuilder keyBridge;
        [SerializeField] private ResourceType keyResource;

        private UnlockPad[] pads = new UnlockPad[0];
        private UnlockPad current;

        private void Start()
        {
            pads = FindObjectsByType<UnlockPad>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cam == null) cam = Camera.main;
        }

        private void LateUpdate()
        {
            Transform target = PickTarget();
            bool blocked = PlayerController.InputBlocked;
            bool hasTarget = target != null && !blocked;

            if (worldArrow != null)
            {
                worldArrow.gameObject.SetActive(hasTarget);
                if (hasTarget)
                {
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * config.GuidePulseSpeed * 0.75f)) * config.GuideArrowBob;
                    worldArrow.position = target.position + Vector3.up * (config.GuideArrowHeight + bob);
                }
            }
            UpdateScreenArrow(hasTarget ? target : null);
        }

        private Transform PickTarget()
        {
            UnlockPad best = null;
            int bestPrice = int.MaxValue;
            foreach (UnlockPad pad in pads)
            {
                if (pad == null || !pad.IsAvailable || pad.GuideIgnore) continue;
                int price = AreaManager.Instance.GetRemaining(pad.Area);
                if (price < bestPrice)
                {
                    best = pad;
                    bestPrice = price;
                }
            }

            // İlk işçi tutma da bir hedef: fiyatı en ucuz kareden ucuzsa ok barakayı gösterir.
            bool crewFirst = false;
            CrewManager crew = CrewManager.Instance;
            if (crew != null && crewStation != null)
            {
                foreach (WorkerType type in crew.Types)
                {
                    if (type == null || crew.GetHired(type) > 0 || !crew.IsAvailable(type)) continue;
                    int price = crew.NextPrice(type);
                    if (price < bestPrice) { bestPrice = price; crewFirst = true; }
                }
            }
            if (crewFirst) best = null;

            if (best != current)
            {
                if (current != null) current.SetHighlighted(false);
                current = best;
                if (current != null) current.SetHighlighted(true);
            }
            if (crewFirst) return crewStation;
            if (best != null)
            {
                // Kapalı bölgenin tanıtım tabelası henüz okunmadıysa ok önce tabelayı gösterir (kare hemen yanında).
                InfoSign sign = InfoSign.For(best.Area);
                return sign != null && !sign.HasBeenSeen ? sign.transform : best.transform;
            }

            ShipBuilder needs = ShipBuilder.Instance;
            if (keyBridge != null && keyResource != null && !keyBridge.IsCompleted && keyBridge.IsAvailable && needs != null && needs.Needs(keyResource))
            {
                InfoSign sign = InfoSign.For(keyBridge.IslandArea);
                return sign != null && !sign.HasBeenSeen ? sign.transform : keyBridge.transform;
            }

            ShipBuilder ship = ShipBuilder.Instance;
            if (ship != null && !ship.IsComplete && ship.Delivery != null) return ship.Delivery.transform;
            return null;
        }

        private void UpdateScreenArrow(Transform target)
        {
            if (screenArrow == null) return;
            if (target == null || cam == null)
            {
                screenArrow.gameObject.SetActive(false);
                return;
            }

            Vector3 vp = cam.WorldToViewportPoint(target.position);
            bool onScreen = vp.z > 0f && vp.x > 0.05f && vp.x < 0.95f && vp.y > 0.08f && vp.y < 0.92f;
            screenArrow.gameObject.SetActive(!onScreen);
            if (onScreen) return;

            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (vp.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

            // Kenara yerleştir (ekranın %42'si kadar merkezden uzak, en/boy oranına göre).
            RectTransform canvas = (RectTransform)screenArrow.parent;
            Vector2 half = canvas.rect.size * 0.5f * 0.86f;
            float scale = Mathf.Min(half.x / Mathf.Max(Mathf.Abs(dir.x), 0.001f), half.y / Mathf.Max(Mathf.Abs(dir.y), 0.001f));
            screenArrow.anchoredPosition = dir * scale;
            screenArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
        }
    }
}
