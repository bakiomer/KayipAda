// AreaDefinition.cs
// Açılabilir bir bölgenin ayar dosyası (Kayalık, Meyve Bahçesi...): kimlik, görünen ad ve açma fiyatı.
// Dosyalar: Assets/_Game/Data/Areas. Kimlik (Id) kayıt sisteminde kullanılır, sonradan değiştirilmemeli.

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "NewArea", menuName = "Kayıp Ada/Açılabilir Alan")]
    public class AreaDefinition : ScriptableObject
    {
        [field: Tooltip("Kayıtta kullanılan benzersiz kimlik (küçük harf, boşluksuz). Örn: rocky")]
        [field: SerializeField] public string Id { get; private set; } = "new_area";

        [field: Tooltip("Açma karesinde yazan ad. Örn: Kayalık")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Yeni Alan";

        [field: Tooltip("Bölgeyi açmak için ödenecek toplam para.")]
        [field: SerializeField, Min(1)] public int Price { get; private set; } = 100;

#if UNITY_EDITOR
        // Sadece editör komutları için: yeni oluşturulan dosyaya ilk değerleri yazar.
        public void EditorSetup(string id, string displayName, int price)
        {
            Id = id;
            DisplayName = displayName;
            Price = price;
        }
#endif
    }
}
