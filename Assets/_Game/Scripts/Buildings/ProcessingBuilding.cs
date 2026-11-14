// ProcessingBuilding.cs
// Genel işleme binası. Giriş yığınında yeterli ham madde varsa ve çıkışta yer varsa
// BuildingType'taki sürede işler: ham maddeler binaya uçup kaybolur, ürün çıkış yığınına uçar.
// Çalışırken üstte ilerleme halkası döner ve duman/talaş çıkar; ürün çıkınca bina zıplar.
// Çıkış dolunca durur (ilerleme bekler), ürün alınınca kaldığı yerden devam eder.

using UnityEngine;

namespace KayipAda
{
    public class ProcessingBuilding : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private BuildingType type;
        [SerializeField] private ItemPile inputPile;
        [SerializeField] private ItemPile outputPile;
        [SerializeField] private ProgressRing progressRing;
        [Tooltip("Ham maddenin girip ürünün çıktığı nokta (binanın ortası).")]
        [SerializeField] private Transform productSpawnPoint;
        [Tooltip("Ürün çıkınca zıplayan model.")]
        [SerializeField] private Transform bodyVisual;
        [Tooltip("Çalışırken çıkan duman/talaş (boş olabilir).")]
        [SerializeField] private ParticleSystem workEffect;

        public BuildingType Type => type;
        public ItemPile InputPile => inputPile;
        public ItemPile OutputPile => outputPile;

        // Girişte bir işlem için yeterli ham madde var mı? (Taşıyıcı işçi "yeni ürün gelecek mi" diye bakar.)
        public bool HasEnoughInput => inputPile.Count >= type.InputAmount;

        private float progress; // 0..1
        private bool working;

        private void Awake()
        {
            if (type == null || type.Input == null || type.Output == null || type.Output.ItemPrefab == null
                || inputPile == null || outputPile == null || progressRing == null || productSpawnPoint == null)
            {
                Debug.LogError(name + ": Bina ayarları veya bağlantıları eksik!", this);
                enabled = false;
                return;
            }

            ApplyCapacities();
            progressRing.SetProgress(0f);
        }

        private void Update()
        {
            // Her karede: Play sırasında ayar dosyasında yapılan değişiklik hemen etkilesin.
            ApplyCapacities();

            bool isWorking = false;
            if (!HasEnoughInput)
            {
                progress = 0f; // ham madde yok → işlem başlamamış sayılır
            }
            else if (HasRoomForOutput())
            {
                isWorking = true;
                progress += Time.deltaTime * UpgradeManager.Multiplier(UpgradeStat.BuildingSpeed) / type.ProcessTime;
                if (progress >= 1f)
                {
                    progress = 0f;
                    FinishProcess();
                }
            }
            // Ham madde var ama çıkış dolu → ilerleme olduğu yerde bekler.

            progressRing.SetProgress(progress);
            SetWorking(isWorking);
        }

        private void SetWorking(bool value)
        {
            if (value == working || workEffect == null) return;
            working = value;
            if (working) workEffect.Play();
            else workEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void ApplyCapacities()
        {
            inputPile.Capacity = type.InputCapacity;
            outputPile.Capacity = type.OutputCapacity;
        }

        private bool HasRoomForOutput()
        {
            return outputPile.Count + type.OutputAmount <= outputPile.Capacity;
        }

        // Ham maddeler binaya uçup kaybolur, ürünler binadan çıkış yığınına uçar.
        private void FinishProcess()
        {
            Vector3 center = productSpawnPoint.position;
            for (int i = 0; i < type.InputAmount; i++)
            {
                ResourceItem used = inputPile.RemoveTop();
                if (used != null) used.FlyToAndDestroy(center, config.ItemFlyHeight * 0.5f, config.ItemFlyDuration);
            }

            for (int i = 0; i < type.OutputAmount; i++)
            {
                outputPile.TryAdd(type.Output.Spawn(center));
            }

            if (bodyVisual != null) Tween.PunchScale(bodyVisual, config.NodeSquash, 0.3f);
            AudioManager.Play(SoundId.Produce, 0.4f);
        }
    }
}
