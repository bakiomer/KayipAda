// ResourceNodeType.cs
// Bir kaynak noktası türünün ayar dosyası (Ağaç, Kaya, Meyve Çalısı...).
// Hangi kaynağı verdiğini, kaç tane stoğu olduğunu, ne hızla toplandığını ve
// bitince kaç saniyede yeniden dolduğunu tutar. Dosyalar: Assets/_Game/Data/NodeTypes

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "NewNodeType", menuName = "Kayıp Ada/Kaynak Noktası Türü")]
    public class ResourceNodeType : ScriptableObject
    {
        [field: Tooltip("Bu noktadan çıkan kaynak.")]
        [field: SerializeField] public ResourceType Resource { get; private set; }

        [field: Tooltip("Nokta tükenmeden önce kaç kaynak verir.")]
        [field: SerializeField, Min(1)] public int Stock { get; private set; } = 8;

        [field: Tooltip("Kaç saniyede bir kaynak verir (büyük değer = daha yavaş toplama).")]
        [field: SerializeField, Min(0.01f)] public float GatherInterval { get; private set; } = 0.35f;

        [field: Tooltip("Tükendikten kaç saniye sonra yeniden dolar.")]
        [field: SerializeField, Min(0f)] public float RegrowTime { get; private set; } = 10f;

        [field: Tooltip("Her toplamada çalan vuruş sesi (ağaç: Chop, kaya: Mine).")]
        [field: SerializeField] public SoundId HitSound { get; private set; } = SoundId.Chop;

#if UNITY_EDITOR
        // Sadece editör komutları için: yeni oluşturulan dosyaya ilk değerleri yazar.
        public void EditorSetup(ResourceType resource, int stock, float gatherInterval, float regrowTime)
        {
            Resource = resource;
            Stock = stock;
            GatherInterval = gatherInterval;
            RegrowTime = regrowTime;
        }

        public void EditorSetHitSound(SoundId sound)
        {
            HitSound = sound;
        }
#endif
    }
}
