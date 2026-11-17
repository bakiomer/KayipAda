// Storage.cs
// Ambar (depo): gemi tezgahının yakınındaki korsan deposu. Oyuncu bırakma alanına girince sırtındaki kaynaklar ambara
// akar (Pazar'a satmadan saklama); alma alanında alttan açılan panelden seçilen kaynak kapasitesi kadar alınır.
// Kapasite sınırlıdır ve Ambar seviyesiyle artar (GameConfig.StorageCapacity); dolunca bırakma durur, uyarı çıkar.
// Taşıyıcı işçiler ambardan gemi teslim alanına ve bina girişlerine ihtiyaç olan malzemeyi taşır.
// İçerik kayda yazılır (SaveData.Storage); ada sıfırlanınca (kaçış) boşalır. Ambar dolulukla içindeki yığınlar büyür.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class Storage : MonoBehaviour
    {
        public static Storage Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [Tooltip("Ambarın açılma tanımı (kapalıyken bırakılamaz/alınamaz).")]
        [SerializeField] private AreaDefinition area;
        [Tooltip("Seviye 2, Seviye 3 (kapasite artar).")]
        [SerializeField] private AreaDefinition[] levelAreas = new AreaDefinition[0];
        [Tooltip("Ambarda saklanabilen kaynaklar (panel sırası).")]
        [SerializeField] private ResourceType[] types = new ResourceType[0];
        [Tooltip("Parçaların uçtuğu / çıktığı nokta (kapı).")]
        [SerializeField] private Transform door;
        [Tooltip("Doluluğa göre görünen yığınlar (kasa, fıçı, çuval).")]
        [SerializeField] private Transform[] fillVisuals = new Transform[0];

        private readonly Dictionary<ResourceType, int> counts = new Dictionary<ResourceType, int>();
        private float warnTimer;
        private Vector3[] fillScales;

        public event Action Changed;

        public IReadOnlyList<ResourceType> Types => types;
        public AreaDefinition Area => area;
        public AreaDefinition[] LevelAreas => levelAreas;
        public Vector3 DoorPosition => door != null ? door.position : transform.position + Vector3.up;
        public bool IsOpen => area == null || (AreaManager.Instance != null && AreaManager.Instance.IsUnlocked(area));

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
        public float TransferInterval => config.StorageTransferInterval;
        public int Capacity => CapacityAt(Level);
        public int CapacityAt(int level) => config.StorageCapacity.Length == 0 ? 100 : config.StorageCapacity[Mathf.Clamp(level - 1, 0, config.StorageCapacity.Length - 1)];

        public int Total
        {
            get { int n = 0; foreach (int c in counts.Values) n += c; return n; }
        }

        public bool IsFull => Total >= Capacity;
        public int CountOf(ResourceType type) => type != null && counts.TryGetValue(type, out int n) ? n : 0;

        private void Awake()
        {
            Instance = this;
            fillScales = new Vector3[fillVisuals.Length];
            for (int i = 0; i < fillVisuals.Length; i++) fillScales[i] = fillVisuals[i].localScale;
            RefreshFill(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (warnTimer > 0f) warnTimer -= Time.deltaTime;
        }

        // Bir parçayı ambara koyar: parça kapıya uçar, sayı hemen artar (fazla dolmasın). Doluysa false.
        public bool TryStore(ResourceItem item)
        {
            if (item == null || !IsOpen || IsFull) return false;
            counts[item.Type] = CountOf(item.Type) + 1;
            item.FlyToAndDestroy(DoorPosition, config.ItemFlyHeight, config.ItemFlyDuration);
            AudioManager.Play(SoundId.Drop, 0.6f);
            Changed?.Invoke();
            RefreshFill(true);
            return true;
        }

        // Doğrudan ekle (kayıt, Test Menüsü). Kapasiteyi aşmaz; eklenen sayıyı döner.
        public int Add(ResourceType type, int amount)
        {
            if (type == null || amount <= 0) return 0;
            int n = Mathf.Min(amount, Capacity - Total);
            if (n <= 0) return 0;
            counts[type] = CountOf(type) + n;
            Changed?.Invoke();
            RefreshFill(false);
            return n;
        }

        // Ambardan bir parça çıkarır (kapıda belirir); yoksa null.
        public ResourceItem Take(ResourceType type)
        {
            if (!IsOpen || CountOf(type) <= 0) return null;
            counts[type] = CountOf(type) - 1;
            Changed?.Invoke();
            RefreshFill(true);
            return type.Spawn(DoorPosition);
        }

        // Dolu uyarısı (sık tekrarlamaz).
        public void WarnFull(Vector3 at)
        {
            if (warnTimer > 0f) return;
            warnTimer = config.StorageFullWarningInterval;
            if (Effects.Instance != null) Effects.Instance.ShowText(at + Vector3.up * 2.4f, "Ambar dolu!", new Color(1f, 0.45f, 0.35f));
            Tween.Shake(transform, 3f, 0.3f);
            AudioManager.Play(SoundId.Button, 0.6f);
        }

        // Doluluğa göre yığınlar görünür.
        private void RefreshFill(bool animate)
        {
            if (fillVisuals.Length == 0) return;
            float k = Capacity > 0 ? (float)Total / Capacity : 0f;
            int show = Mathf.CeilToInt(k * fillVisuals.Length);
            for (int i = 0; i < fillVisuals.Length; i++)
            {
                bool on = i < show;
                if (fillVisuals[i].gameObject.activeSelf == on) continue;
                fillVisuals[i].gameObject.SetActive(on);
                if (on && animate) Tween.PopIn(fillVisuals[i], fillScales[i], 0.3f);
            }
        }

        // Kayıt.
        public void Collect(List<SaveData.IdValue> list)
        {
            list.Clear();
            foreach (KeyValuePair<ResourceType, int> c in counts) if (c.Key != null && c.Value > 0) list.Add(new SaveData.IdValue(c.Key.Id, c.Value));
        }

        public void Restore(List<SaveData.IdValue> list)
        {
            counts.Clear();
            foreach (SaveData.IdValue v in list)
                foreach (ResourceType t in types) if (t != null && t.Id == v.Id) counts[t] = v.Value;
            Changed?.Invoke();
            RefreshFill(false);
        }

        public void Clear()
        {
            counts.Clear();
            Changed?.Invoke();
            RefreshFill(false);
        }
    }
}
