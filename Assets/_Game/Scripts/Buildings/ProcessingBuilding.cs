// ProcessingBuilding.cs
// Genel işleme binası. Giriş yığınında yeterli ham madde varsa ve çıkışta yer varsa
// BuildingType'taki sürede işler: ham maddeler binaya uçup kaybolur, ürün çıkış yığınına uçar.
// Çalışırken üstte ilerleme halkası döner ve duman/talaş çıkar; ürün çıkınca bina zıplar.
// Çıkış dolunca durur (ilerleme bekler), ürün alınınca kaldığı yerden devam eder.
// Seviye: yanındaki küçük karelerle (levelAreas: Seviye 2, Seviye 3) yükselir; hız ve kapasite artar, bina büyür
// (GameConfig > Bina seviyeleri).

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
        [Tooltip("Seviye kareleri: açıldıkça bina seviye atlar (Seviye 2, Seviye 3).")]
        [SerializeField] private AreaDefinition[] levelAreas = new AreaDefinition[0];
        [Tooltip("Üretim hattının girişi: ham madde buraya uçar (boşsa ürün noktasına).")]
        [SerializeField] private Transform intakePoint;

        // Herhangi bir bina ürün çıkarınca (yan görevler): ürün türü, adet.
        public static event System.Action<ResourceType, int> AnyProduced;

        public BuildingType Type => type;
        public ItemPile InputPile => inputPile;
        public ItemPile OutputPile => outputPile;

        // Girişte bir işlem için yeterli ham madde var mı? (Taşıyıcı işçi "yeni ürün gelecek mi" diye bakar.)
        public bool HasEnoughInput => inputPile.Count >= type.InputAmount;

        // Şu an işliyor mu (makineler sadece işlerken hareket eder)? İşlemin ilerlemesi 0..1.
        public bool IsWorking => working;
        public float Progress => progress;
        // Bitmiş ürün sayısı (makine animasyonu "biten + ilerleme" ile üretime senkron döner).
        public int ProducedCount { get; private set; }
        // Üretim döngüsünün sürekli ölçüsü: her ürün 1 birim.
        public float Cycle => ProducedCount + progress;

        // 1, 2, 3...: açılmış seviye karesi sayısı + 1.
        public int Level
        {
            get
            {
                int level = 1;
                if (AreaManager.Instance == null) return level;
                foreach (AreaDefinition a in levelAreas) if (a != null && AreaManager.Instance.IsUnlocked(a)) level++;
                return level;
            }
        }

        public int MaxLevel => levelAreas.Length + 1;
        public AreaDefinition[] LevelAreas => levelAreas;

        // Toplam hız çarpanı: geliştirme × seviye.
        public float SpeedMultiplier => UpgradeManager.Multiplier(UpgradeStat.BuildingSpeed) * LevelValue(config.BuildingLevelSpeed, 1f);

        private float LevelValue(float[] values, float fallback)
        {
            int i = Level - 1;
            return values != null && values.Length > 0 ? values[Mathf.Clamp(i, 0, values.Length - 1)] : fallback;
        }

        private int shownLevel = 1;
        private Vector3 bodyScale = Vector3.one;

        // Bir ürün çıkınca.
        public event System.Action Produced;

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
            if (bodyVisual != null) bodyScale = bodyVisual.localScale;
        }

        private void Start()
        {
            shownLevel = Level;
            ApplyLevelScale();
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
        }

        private void OnDestroy()
        {
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked -= OnAreaUnlocked;
        }

        // Seviye atladı: bina büyür, parıltı + yazı.
        private void OnAreaUnlocked(AreaDefinition area)
        {
            if (System.Array.IndexOf(levelAreas, area) < 0 || Level == shownLevel) return;
            shownLevel = Level;
            ApplyLevelScale();
            if (bodyVisual != null) Tween.PunchScale(bodyVisual, 0.15f, 0.45f);
            if (Effects.Instance != null)
            {
                Effects.Instance.Sparkle(productSpawnPoint.position + Vector3.up);
                Effects.Instance.ShowText(productSpawnPoint.position + Vector3.up * 2.4f, type.DisplayName + " Seviye " + shownLevel + "!", new Color(0.6f, 1f, 0.6f));
            }
            AudioManager.Play(SoundId.Upgrade, 0.5f);
        }

        private void ApplyLevelScale()
        {
            if (bodyVisual != null) bodyVisual.localScale = bodyScale * LevelValue(config.BuildingLevelScale, 1f);
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
                progress += Time.deltaTime * SpeedMultiplier / type.ProcessTime;
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
            if (value == working) return;
            working = value;
            if (workEffect == null) return;
            if (working) workEffect.Play();
            else workEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void ApplyCapacities()
        {
            int extra = Mathf.RoundToInt(LevelValue(config.BuildingLevelCapacity, 0f));
            inputPile.Capacity = type.InputCapacity + extra;
            outputPile.Capacity = type.OutputCapacity + extra;
        }

        private bool HasRoomForOutput()
        {
            return outputPile.Count + type.OutputAmount <= outputPile.Capacity;
        }

        // Ham maddeler binaya uçup kaybolur, ürünler binadan çıkış yığınına uçar.
        private void FinishProcess()
        {
            Vector3 center = productSpawnPoint.position;
            Vector3 intake = intakePoint != null ? intakePoint.position : center; // ham madde üretim hattının girişine
            for (int i = 0; i < type.InputAmount; i++)
            {
                ResourceItem used = inputPile.RemoveTop();
                if (used != null) used.FlyToAndDestroy(intake, config.ItemFlyHeight * 0.5f, config.ItemFlyDuration);
            }

            for (int i = 0; i < type.OutputAmount; i++)
            {
                outputPile.TryAdd(type.Output.Spawn(center));
            }

            ProducedCount++;
            if (bodyVisual != null && intakePoint == null) Tween.PunchScale(bodyVisual, config.NodeSquash, 0.3f); // makineli binada makine kendisi oynar
            AudioManager.Play(SoundId.Produce, 0.4f);
            Produced?.Invoke();
            AnyProduced?.Invoke(type.Output, type.OutputAmount);
        }
    }
}
