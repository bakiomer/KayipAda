// ResourceType.cs
// Bir kaynak türünün ayar dosyası (odun, ileride taş, meyve, kalas...).
// Her tür için Assets/_Game/Data/ResourceTypes altında bir .asset dosyası bulunur.
// Yeni kaynak eklemek için: Project penceresinde sağ tık > Create > Kayıp Ada > Kaynak Türü.

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

        [field: Tooltip("Görsel gelene kadar kutunun rengi.")]
        [field: SerializeField] public Color Color { get; private set; } = Color.white;

        [field: Tooltip("Pazarda bir tanesinin satış fiyatı.")]
        [field: SerializeField, Min(0)] public int SellPrice { get; private set; } = 1;

#if UNITY_EDITOR
        // Sadece editör komutları için: yeni oluşturulan dosyaya ilk değerleri yazar.
        public void EditorSetup(string id, string displayName, Color color, int sellPrice)
        {
            Id = id;
            DisplayName = displayName;
            Color = color;
            SellPrice = sellPrice;
        }
#endif
    }
}
