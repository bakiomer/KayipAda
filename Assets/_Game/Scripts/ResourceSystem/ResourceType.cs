// ResourceType.cs
// Bir kaynak türünün ayar dosyası (odun, taş, meyve, kalas, tuğla...).
// Her tür için Assets/_Game/Data/ResourceTypes altında bir .asset dosyası bulunur.
// Görünüşü ItemPrefab'tadır (Assets/_Game/Prefabs/Items); modeli değiştirmek için o prefab değiştirilir.

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "NewResourceType", menuName = "Kayıp Ada/Kaynak Türü")]
    public class ResourceType : ScriptableObject
    {
        [field: Tooltip("Kodda ve kayıt dosyasında kullanılan benzersiz kimlik (küçük harf, boşluksuz). Örn: wood")]
        [field: SerializeField] public string Id { get; private set; } = "new_resource";

        [field: Tooltip("Oyuncuya gösterilen ad. Örn: Odun")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Yeni Kaynak";

        [field: Tooltip("Arayüzde ve yazılarda kullanılan tema rengi.")]
        [field: SerializeField] public Color Color { get; private set; } = Color.white;

        [field: Tooltip("Pazarda bir tanesinin satış fiyatı.")]
        [field: SerializeField, Min(0)] public int SellPrice { get; private set; } = 1;

        [field: Tooltip("Bu kaynağın dünyadaki modeli (ResourceItem bileşenli prefab).")]
        [field: SerializeField] public ResourceItem ItemPrefab { get; private set; }

        // Yeni bir parça oluşturur (kaynak noktası ve binalar kullanır).
        public ResourceItem Spawn(Vector3 position)
        {
            ResourceItem item = Instantiate(ItemPrefab, position, Quaternion.identity);
            item.Setup(this);
            return item;
        }

#if UNITY_EDITOR
        // Sadece editör komutları için: yeni oluşturulan dosyaya ilk değerleri yazar.
        public void EditorSetup(string id, string displayName, Color color, int sellPrice)
        {
            Id = id;
            DisplayName = displayName;
            Color = color;
            SellPrice = sellPrice;
        }

        // Model prefab'ı oynanış ayarı değildir; sahne kurma komutu her seferinde bağlar.
        public void EditorSetPrefab(ResourceItem prefab)
        {
            ItemPrefab = prefab;
        }
#endif
    }
}
