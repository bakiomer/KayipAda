// ResourceItem.cs
// Dünyada duran veya taşınan tek bir kaynak parçası.
// Tüm kaynak türleri aynı prefab'ı (ResourceItem.prefab) kullanır; Setup ile türü ve rengi atanır.
// Satılınca hedefe uçup küçülerek kaybolur.

using System.Collections;
using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ResourceItem : MonoBehaviour
    {
        public ResourceType Type { get; private set; }

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Yeni oluşturulan parçaya türünü verir (renk ve isim türden gelir).
        public void Setup(ResourceType type)
        {
            Type = type;
            spriteRenderer.color = type.Color;
            name = type.DisplayName;
        }

        public void SetSortingOrder(int order)
        {
            spriteRenderer.sortingOrder = order;
        }

        // Verilen noktaya uçar, küçülür ve kendini yok eder.
        public void FlyToAndDestroy(Vector3 target, float duration)
        {
            StartCoroutine(FlyRoutine(target, duration));
        }

        private IEnumerator FlyRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            Vector3 startScale = transform.localScale;

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(start, target, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
