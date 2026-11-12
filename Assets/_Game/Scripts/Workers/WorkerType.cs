// WorkerType.cs
// Bir işçi görevinin ayar dosyası (Oduncu, Taşıyıcı): yürüme hızı, taşıma kapasitesi ve kaç işçi çalışacağı.
// Dosyalar: Assets/_Game/Data/Workers. İşçiyi tutma fiyatı ise Data/Areas altındaki Hire... dosyasındadır.

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "NewWorkerType", menuName = "Kayıp Ada/İşçi Türü")]
    public class WorkerType : ScriptableObject
    {
        [field: Tooltip("İşçinin görünen adı. Örn: Oduncu")]
        [field: SerializeField] public string DisplayName { get; private set; } = "İşçi";

        [field: Tooltip("Yürüme hızı (birim/saniye). Oyuncununki GameConfig'te.")]
        [field: SerializeField, Min(0.1f)] public float MoveSpeed { get; private set; } = 3f;

        [field: Tooltip("Sırtında taşıyabileceği en fazla kaynak sayısı.")]
        [field: SerializeField, Min(1)] public int CarryCapacity { get; private set; } = 5;

        [field: Tooltip("Bu görev tutulunca kaç işçi çalışır. Play sırasında artırılırsa yeni işçi kamptan çıkar.")]
        [field: SerializeField, Min(0)] public int Count { get; private set; } = 1;

#if UNITY_EDITOR
        // Sadece editör komutları için: yeni oluşturulan dosyaya ilk değerleri yazar.
        public void EditorSetup(string displayName, float moveSpeed, int carryCapacity, int count)
        {
            DisplayName = displayName;
            MoveSpeed = moveSpeed;
            CarryCapacity = carryCapacity;
            Count = count;
        }
#endif
    }
}
