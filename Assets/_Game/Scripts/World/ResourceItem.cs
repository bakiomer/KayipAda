// ResourceItem.cs
// Taşınabilir tek bir kaynak (şimdilik sadece odun).
// Türünü ve satış değerini tutar; satılınca hedefe uçup kaybolur.

using System.Collections;
using UnityEngine;

namespace KayipAda
{
    public enum ResourceType
    {
        Wood,
    }

    public class ResourceItem : MonoBehaviour
    {
        [SerializeField] private ResourceType type = ResourceType.Wood;
        [SerializeField] private int sellValue = 5;

        public ResourceType Type => type;
        public int SellValue => sellValue;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetSortingOrder(int order)
        {
            if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
        }

        // Verilen noktaya uçar, küçülür ve kendini yok eder.
        public void FlyToAndDestroy(Vector3 target)
        {
            StartCoroutine(FlyRoutine(target));
        }

        private IEnumerator FlyRoutine(Vector3 target)
        {
            Vector3 start = transform.position;
            Vector3 startScale = transform.localScale;
            const float duration = 0.25f;

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(start, target, t);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.3f, t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
