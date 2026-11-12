// BuildingType.cs
// Bir işleme binasının ayar dosyası (Bıçkıhane, Taş Atölyesi...): ne alır, ne üretir, oranı,
// işleme süresi ve giriş/çıkış kapasiteleri. Dosyalar: Assets/_Game/Data/Buildings

using UnityEngine;

namespace KayipAda
{
    [CreateAssetMenu(fileName = "NewBuildingType", menuName = "Kayıp Ada/İşleme Binası Türü")]
    public class BuildingType : ScriptableObject
    {
        [field: Tooltip("Binanın üstünde yazan ad.")]
        [field: SerializeField] public string DisplayName { get; private set; } = "Yeni Bina";

        [field: Header("Tarif")]
        [field: Tooltip("Binanın kabul ettiği ham madde.")]
        [field: SerializeField] public ResourceType Input { get; private set; }

        [field: Tooltip("Bir ürün için harcanan ham madde sayısı.")]
        [field: SerializeField, Min(1)] public int InputAmount { get; private set; } = 2;

        [field: Tooltip("Binanın ürettiği ürün.")]
        [field: SerializeField] public ResourceType Output { get; private set; }

        [field: Tooltip("Her işlemde çıkan ürün sayısı.")]
        [field: SerializeField, Min(1)] public int OutputAmount { get; private set; } = 1;

        [field: Tooltip("Bir işlemin süresi (saniye).")]
        [field: SerializeField, Min(0.1f)] public float ProcessTime { get; private set; } = 2f;

        [field: Header("Kapasite")]
        [field: Tooltip("Giriş alanında en fazla kaç ham madde bekleyebilir.")]
        [field: SerializeField, Min(1)] public int InputCapacity { get; private set; } = 10;

        [field: Tooltip("Çıkış alanında en fazla kaç ürün birikebilir. Dolunca bina durur.")]
        [field: SerializeField, Min(1)] public int OutputCapacity { get; private set; } = 10;

#if UNITY_EDITOR
        // Sadece editör komutları için: yeni oluşturulan dosyaya ilk değerleri yazar.
        public void EditorSetup(string displayName, ResourceType input, int inputAmount, ResourceType output, int outputAmount,
                                float processTime, int inputCapacity, int outputCapacity)
        {
            DisplayName = displayName;
            Input = input;
            InputAmount = inputAmount;
            Output = output;
            OutputAmount = outputAmount;
            ProcessTime = processTime;
            InputCapacity = inputCapacity;
            OutputCapacity = outputCapacity;
        }
#endif
    }
}
