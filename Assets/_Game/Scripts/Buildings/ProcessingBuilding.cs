// ProcessingBuilding.cs
// Genel işleme binası. Giriş yığınında yeterli ham madde varsa ve çıkışta yer varsa
// BuildingType'taki sürede işler: ham maddeleri harcar, ürünü çıkış yığınına koyar.
// Çıkış dolunca durur (ilerleme bekler), oyuncu ürün alınca kaldığı yerden devam eder.
// Giriş/çıkış alanları (BuildingInput / BuildingOutput) bu bileşenin yığınlarını kullanır.

using UnityEngine;

namespace KayipAda
{
    public class ProcessingBuilding : MonoBehaviour
    {
        [SerializeField] private BuildingType type;
        [SerializeField] private ItemPile inputPile;
        [SerializeField] private ItemPile outputPile;
        [SerializeField] private ProgressBar progressBar;
        [SerializeField] private ResourceItem itemPrefab;
        [Tooltip("Üretilen ürünün çıktığı nokta (binanın gövdesi).")]
        [SerializeField] private Transform productSpawnPoint;

        public BuildingType Type => type;
        public ItemPile InputPile => inputPile;
        public ItemPile OutputPile => outputPile;

        private float progress; // 0..1

        private void Awake()
        {
            if (type == null || type.Input == null || type.Output == null || inputPile == null || outputPile == null
                || progressBar == null || itemPrefab == null || productSpawnPoint == null)
            {
                Debug.LogError(name + ": Bina ayarları veya bağlantıları eksik!", this);
                enabled = false;
                return;
            }

            ApplyCapacities();
            progressBar.SetProgress(0f);
        }

        private void Update()
        {
            // Her karede: Play sırasında ayar dosyasında yapılan değişiklik hemen etkilesin.
            ApplyCapacities();

            if (!HasEnoughInput())
            {
                progress = 0f; // ham madde yok → işlem başlamamış sayılır
            }
            else if (HasRoomForOutput())
            {
                progress += Time.deltaTime / type.ProcessTime;
                if (progress >= 1f)
                {
                    progress = 0f;
                    FinishProcess();
                }
            }
            // Ham madde var ama çıkış dolu → ilerleme olduğu yerde bekler.

            progressBar.SetProgress(progress);
        }

        private void ApplyCapacities()
        {
            inputPile.Capacity = type.InputCapacity;
            outputPile.Capacity = type.OutputCapacity;
        }

        private bool HasEnoughInput()
        {
            return inputPile.Count >= type.InputAmount;
        }

        private bool HasRoomForOutput()
        {
            return outputPile.Count + type.OutputAmount <= outputPile.Capacity;
        }

        // Ham maddeleri harcar, ürünleri üretip çıkış yığınına gönderir.
        private void FinishProcess()
        {
            for (int i = 0; i < type.InputAmount; i++)
            {
                ResourceItem used = inputPile.RemoveTop();
                Destroy(used.gameObject);
            }

            for (int i = 0; i < type.OutputAmount; i++)
            {
                ResourceItem product = Instantiate(itemPrefab, productSpawnPoint.position, Quaternion.identity);
                product.Setup(type.Output);
                outputPile.TryAdd(product); // binadan çıkış yığınına doğru kayar
            }
        }
    }
}
