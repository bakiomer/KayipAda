// ResourceNode.cs
// Kaynak noktası: ağaç, kaya ve meyve çalısı aynı bileşeni kullanır; farkı ResourceNodeType ayar dosyasıdır.
// Karakter alanın içindeyken aralıklarla kaynak verir. Stoğu sınırlıdır: bitince nokta küçülüp solar,
// ayar dosyasındaki süre dolunca yeniden dolar ve eski haline döner.

using UnityEngine;

namespace KayipAda
{
    public class ResourceNode : InteractionZone
    {
        [SerializeField] private ResourceNodeType nodeType;
        [SerializeField] private ResourceItem itemPrefab;
        [Tooltip("Küçülme/solma/zıplama efektlerinin uygulanacağı görsel kök.")]
        [SerializeField] private Transform visual;

        public int Stock { get; private set; }
        public bool IsDepleted => Stock <= 0;

        private float gatherTimer;
        private float regrowTimer;
        private float alpha = 1f;
        private Vector3 visualBaseScale;
        private SpriteRenderer[] renderers;
        private Color[] baseColors;

        private void Awake()
        {
            if (nodeType == null || nodeType.Resource == null || itemPrefab == null || visual == null)
            {
                Debug.LogError(name + ": Node Type, kaynağı, Item Prefab veya Visual atanmamış!", this);
                enabled = false; // Hata yağmuru olmasın diye bu noktayı kapat
                return;
            }

            visualBaseScale = visual.localScale;
            renderers = visual.GetComponentsInChildren<SpriteRenderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;

            Stock = nodeType.Stock;
        }

        protected override void OnVisitorEnter()
        {
            gatherTimer = 0f; // girer girmez ilk kaynağı ver
        }

        private void Update()
        {
            UpdateRegrow();
            UpdateGather();
            UpdateVisual();
        }

        private void UpdateRegrow()
        {
            if (!IsDepleted) return;

            regrowTimer -= Time.deltaTime;
            if (regrowTimer > 0f) return;

            Stock = nodeType.Stock;
            visual.localScale = visualBaseScale * config.NodeBounceScale; // dolunca küçük bir zıplama
        }

        private void UpdateGather()
        {
            if (IsDepleted || Visitor == null || Visitor.IsFull) return;

            gatherTimer -= Time.deltaTime;
            if (gatherTimer > 0f) return;
            gatherTimer = nodeType.GatherInterval;

            ResourceItem item = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            item.Setup(nodeType.Resource);
            Visitor.TryAdd(item);

            Stock--;
            if (IsDepleted)
            {
                regrowTimer = nodeType.RegrowTime;
            }
            else
            {
                visual.localScale = visualBaseScale * config.NodeBounceScale;
            }
        }

        // Görseli hedef boyuta ve saydamlığa doğru yumuşakça götürür (tükenmişse küçük ve soluk).
        private void UpdateVisual()
        {
            Vector3 targetScale = IsDepleted ? visualBaseScale * config.NodeDepletedScale : visualBaseScale;
            float targetAlpha = IsDepleted ? config.NodeDepletedAlpha : 1f;
            float step = config.NodeAnimSpeed * Time.deltaTime;

            visual.localScale = Vector3.Lerp(visual.localScale, targetScale, step);
            alpha = Mathf.Lerp(alpha, targetAlpha, step);

            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = baseColors[i];
                c.a *= alpha;
                renderers[i].color = c;
            }
        }
    }
}
