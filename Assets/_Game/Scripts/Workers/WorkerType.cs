// WorkerType.cs
// Bir işçi görevinin ayar dosyası (Oduncu, Taşıyıcı, Lifçi): yürüme hızı, taşıma kapasitesi ve Tayfa Barakası'ndaki
// tutma bilgileri (fiyatlar, en fazla kaç tane, hangi bina açılınca tutulabilir, kartta görünen resim).
// Dosyalar: Assets/_Game/Data/Workers.

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "NewWorkerType", menuName = "Kayıp Ada/İşçi Türü")]
    public class WorkerType : ScriptableObject
    {
        [field: Tooltip("Kayıt için kimlik (küçük harf). Örn: lumberjack")]
        [field: SerializeField] public string Id { get; private set; } = "";

        [field: Tooltip("İşçinin görünen adı. Örn: Oduncu")]
        [field: SerializeField] public string DisplayName { get; private set; } = "İşçi";

        [field: Tooltip("Yürüme hızı (birim/saniye). Oyuncununki GameConfig'te.")]
        [field: SerializeField, Min(0.1f)] public float MoveSpeed { get; private set; } = 3f;

        [field: Tooltip("Sırtında taşıyabileceği en fazla kaynak sayısı.")]
        [field: SerializeField, Min(1)] public int CarryCapacity { get; private set; } = 5;

        [field: Header("Tayfa Barakası")]
        [field: Tooltip("Sırayla her işçinin tutma fiyatı. Dizinin uzunluğu = en fazla işçi sayısı.")]
        [field: SerializeField] public int[] HirePrices { get; private set; } = { 400 };

        [field: Tooltip("Bu alan (bina) açılmadan işçi tutulamaz. Boşsa her zaman açık.")]
        [field: SerializeField] public AreaDefinition RequiredArea { get; private set; }

        [field: Tooltip("Kilitliyken kartta yazan yazı. Örn: Bıçkıhane gerekli")]
        [field: SerializeField] public string LockedText { get; private set; } = "";

        [field: Tooltip("Kartta görünen işçi resmi.")]
        [field: SerializeField] public Sprite Portrait { get; private set; }

        [field: Tooltip("Eski kayıtlar için: bu açma karesi ödenmişse 1 işçi tutulmuş sayılır.")]
        [field: SerializeField] public AreaDefinition LegacyHireArea { get; private set; }

        public int MaxCount => HirePrices != null ? HirePrices.Length : 0;

#if UNITY_EDITOR
        // Sadece editör komutları için: yeni oluşturulan dosyaya ilk değerleri yazar.
        public void EditorSetup(string displayName, float moveSpeed, int carryCapacity)
        {
            DisplayName = displayName;
            MoveSpeed = moveSpeed;
            CarryCapacity = carryCapacity;
        }

        // Tayfa Barakası bilgileri (eski dosyalarda yoksa komut bir kez yazar).
        public void EditorSetupCrew(string id, int[] hirePrices, AreaDefinition requiredArea, string lockedText, AreaDefinition legacy)
        {
            Id = id;
            HirePrices = hirePrices;
            RequiredArea = requiredArea;
            LockedText = lockedText;
            LegacyHireArea = legacy;
        }

        public void EditorSetPortrait(Sprite portrait)
        {
            Portrait = portrait;
        }
#endif
    }
}
