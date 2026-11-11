// ResourceTree.cs
// Oyuncu yakınındayken (trigger alanı içindeyken) belirli aralıklarla odun verir.
// Oyuncunun yığını doluysa odun vermez. Odun verirken ağaç hafifçe "zıplar".
// Not: Unity'de zaten "Tree" adlı bir sınıf olduğu için adı ResourceTree.

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Collider2D))]
    public class ResourceTree : MonoBehaviour
    {
        [SerializeField] private ResourceItem woodPrefab;
        [Tooltip("Kaç saniyede bir odun verilir.")]
        [SerializeField] private float giveInterval = 0.35f;
        [Tooltip("Zıplama efektinin uygulanacağı görsel (ağacın tepesi).")]
        [SerializeField] private Transform visual;

        private ResourceStack visitor; // Şu an alanda olan oyuncunun yığını
        private float timer;
        private Vector3 visualBaseScale;

        private void Awake()
        {
            if (visual == null) visual = transform;
            visualBaseScale = visual.localScale;

            if (woodPrefab == null)
            {
                Debug.LogError(name + ": Wood Prefab atanmamış! Inspector'dan Wood prefab'ını sürükleyin.", this);
                enabled = false; // Hata yağmuru olmasın diye bu ağacı kapat
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out ResourceStack stack))
            {
                visitor = stack;
                timer = 0f; // girer girmez ilk odunu ver
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out ResourceStack stack) && stack == visitor)
            {
                visitor = null;
            }
        }

        private void Update()
        {
            // Zıplama efektini yavaşça normale döndür.
            visual.localScale = Vector3.Lerp(visual.localScale, visualBaseScale, 10f * Time.deltaTime);

            if (visitor == null || visitor.IsFull) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = giveInterval;

            GiveWood();
        }

        private void GiveWood()
        {
            ResourceItem wood = Instantiate(woodPrefab, transform.position, Quaternion.identity);
            visitor.TryAdd(wood);
            visual.localScale = visualBaseScale * 1.15f;
        }
    }
}
