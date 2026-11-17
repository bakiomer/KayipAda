// CostumeDefinition.cs
// Bir kostümün ayar dosyası: karakter kostümü (oyuncunun görünüşü) ya da yelken/bayrak (geminin rengi).
// Ad, kristal fiyatı, nadirlik; bazı nadir kostümler sadece sandıktan çıkar (ChestOnly). Dosyalar: Data/Costumes.

using UnityEngine;

namespace KayipAda
{
    public enum CostumeSlot { Character, Sail }

    public enum CostumeRarity { Common, Rare, Epic, Legendary }

    [CreateAssetMenu(fileName = "NewCostume", menuName = "Kayıp Ada/Kostüm")]
    public class CostumeDefinition : ScriptableObject
    {
        [field: Tooltip("Kayıt için kimlik (küçük harf).")]
        [field: SerializeField] public string Id { get; private set; } = "";

        [field: SerializeField] public string DisplayName { get; private set; } = "Kostüm";

        [field: SerializeField] public CostumeSlot Slot { get; private set; } = CostumeSlot.Character;

        [field: Tooltip("Kristal fiyatı (sandıktan çıkanlarda kullanılmaz).")]
        [field: SerializeField, Min(0)] public int CrystalPrice { get; private set; } = 50;

        [field: SerializeField] public CostumeRarity Rarity { get; private set; } = CostumeRarity.Common;

        [field: Tooltip("Sadece sandıktan çıkar (satın alınamaz).")]
        [field: SerializeField] public bool ChestOnly { get; private set; }

        [field: Tooltip("Oyun başında sahip olunur (varsayılan kostüm).")]
        [field: SerializeField] public bool OwnedByDefault { get; private set; }

        [field: Tooltip("Karakter kostümü: oyuncunun yerine geçen model (animasyonlu). Boşsa varsayılan model kalır.")]
        [field: SerializeField] public GameObject CharacterModel { get; private set; }

        [field: Tooltip("Renk tonu: karakterde modele, yelkende yelken ve bayrağa uygulanır (beyaz = değişmez).")]
        [field: SerializeField] public Color Tint { get; private set; } = Color.white;

#if UNITY_EDITOR
        public void EditorSetup(string id, string displayName, CostumeSlot slot, int price, CostumeRarity rarity, bool chestOnly,
                                bool ownedByDefault, Color tint)
        {
            Id = id;
            DisplayName = displayName;
            Slot = slot;
            CrystalPrice = price;
            Rarity = rarity;
            ChestOnly = chestOnly;
            OwnedByDefault = ownedByDefault;
            Tint = tint;
        }

        public void EditorSetModel(GameObject model)
        {
            CharacterModel = model;
        }
#endif
    }
}
