// ResourceNode.cs
// Kaynak noktası (şimdilik ağaç). Karakter alanın içindeyken belirli aralıklarla
// kendi kaynak türünden bir parça verir. Yığın doluysa vermez. Verirken görseli hafifçe "zıplar".
// Aynı bileşen ileride kaya ve meyve çalısı için de kullanılacak; sadece ResourceType değişir.

using UnityEngine;

namespace KayipAda
{
    public class ResourceNode : InteractionZone
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private ResourceItem itemPrefab;
        [Tooltip("Zıplama efektinin uygulanacağı görsel (örn. ağacın tepesi).")]
        [SerializeField] private Transform visual;

        private float timer;
        private Vector3 visualBaseScale;

        private void Awake()
        {
            if (visual == null) visual = transform;
            visualBaseScale = visual.localScale;

            if (resourceType == null || itemPrefab == null)
            {
                Debug.LogError(name + ": Resource Type veya Item Prefab atanmamış!", this);
                enabled = false; // Hata yağmuru olmasın diye bu noktayı kapat
            }
        }

        protected override void OnVisitorEnter()
        {
            timer = 0f; // girer girmez ilk kaynağı ver
        }

        private void Update()
        {
            // Zıplama efektini yavaşça normale döndür.
            visual.localScale = Vector3.Lerp(visual.localScale, visualBaseScale, config.NodeBounceRecover * Time.deltaTime);

            if (Visitor == null || Visitor.IsFull) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = config.GatherInterval;

            GiveItem();
        }

        private void GiveItem()
        {
            ResourceItem item = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            item.Setup(resourceType);
            Visitor.TryAdd(item);
            visual.localScale = visualBaseScale * config.NodeBounceScale;
        }
    }
}
