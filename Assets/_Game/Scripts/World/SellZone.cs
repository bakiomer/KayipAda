// SellZone.cs
// Oyuncu bu alana girince sırtındaki kaynaklar tek tek alınır ve satılır.
// Her kaynağın satış değeri MoneyManager'a eklenir.

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Collider2D))]
    public class SellZone : MonoBehaviour
    {
        [Tooltip("Kaç saniyede bir kaynak satılır.")]
        [SerializeField] private float sellInterval = 0.1f;

        private ResourceStack visitor;
        private float timer;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out ResourceStack stack))
            {
                visitor = stack;
                timer = 0f;
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
            if (visitor == null || visitor.IsEmpty) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = sellInterval;

            ResourceItem item = visitor.RemoveTop();
            MoneyManager.Instance.Add(item.SellValue);
            item.FlyToAndDestroy(transform.position);
        }
    }
}
