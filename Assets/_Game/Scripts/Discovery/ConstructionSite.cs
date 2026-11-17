// ConstructionSite.cs
// Malzeme teslim edilerek yapılan şeylerin ortak temeli (köprü, kırık kuyu/çit/iskele onarımı). Oyuncu alana girince
// sırtındaki uygun malzemeler tek tek yapıya uçar; her parça yerine oturunca alt sınıf görselini günceller.
// Hepsi tamamlanınca OnCompleted çağrılır. İlerleme kayda yazılır (SaveManager: Sites/SitesDone).
// "requiredArea" açılmadan alan görünmez (ör. ikinci köprü Halat Atölyesi'nden sonra).

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KayipAda
{
    public abstract class ConstructionSite : InteractionZone
    {
        [Serializable]
        public class Requirement
        {
            public ResourceType Resource;
            [Min(1)] public int Amount = 10;
        }

        [Tooltip("Kayıt kimliği (küçük harf).")]
        [SerializeField] private string id = "";
        [Tooltip("Ad (yazılarda): Köprü, Kuyu...")]
        [SerializeField] private string displayName = "Yapı";
        [SerializeField] private Requirement[] requirements = new Requirement[0];
        [Tooltip("Bu açılmadan alan görünmez (boşsa hep görünür).")]
        [SerializeField] private AreaDefinition requiredArea;
        [Tooltip("Alan açıkken görünen kısım (yer işareti, ilerleme yazısı).")]
        [SerializeField] private GameObject zoneVisual;
        [Tooltip("İlerleme yazısı (\"Köprü: 18/40\"); göstergesi varsa kullanılmaz.")]
        [SerializeField] private TMP_Text progressLabel;
        [Tooltip("Gemi göstergesi gibi kutusuz gösterge: ad + her malzeme için ikon + \"0/8\" + ince çubuk.")]
        [SerializeField] private ShipProgressView progressView;
        [Tooltip("Malzemenin uçtuğu nokta (alt sınıf parça yerini vermezse).")]
        [SerializeField] private Transform dropPoint;

        private static readonly List<ConstructionSite> all = new List<ConstructionSite>();
        public static IReadOnlyList<ConstructionSite> All => all;

        // Herhangi bir yapı tamamlanınca (kimlik).
        public static event Action<string> AnyCompleted;

        private readonly Dictionary<ResourceType, int> delivered = new Dictionary<ResourceType, int>();
        private readonly Dictionary<ResourceType, int> inFlight = new Dictionary<ResourceType, int>();
        private float timer;

        public string Id => id;
        public string DisplayName => displayName;
        public bool IsCompleted { get; private set; }
        public IReadOnlyList<Requirement> Requirements => requirements;
        public bool IsAvailable => requiredArea == null || (AreaManager.Instance != null && AreaManager.Instance.IsUnlocked(requiredArea));
        public int TotalRequired { get { int n = 0; foreach (Requirement r in requirements) n += r.Amount; return n; } }
        public int TotalDelivered { get { int n = 0; foreach (Requirement r in requirements) n += Delivered(r.Resource); return n; } }

        public ShipProgressView ProgressView => progressView;

        public int Delivered(ResourceType type) => type != null && delivered.TryGetValue(type, out int n) ? n : 0;

        protected virtual void Awake()
        {
            all.Add(this);
        }

        protected virtual void OnDestroy()
        {
            all.Remove(this);
        }

        protected virtual void Start()
        {
            RefreshVisibility();
            RefreshLabel();
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
        }

        private void OnAreaUnlocked(AreaDefinition area)
        {
            if (area == requiredArea) RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (zoneVisual != null) zoneVisual.SetActive(!IsCompleted && IsAvailable);
        }

        private void Update()
        {
            if (IsCompleted || !IsAvailable) return;
            if (timer > 0f) { timer -= Time.deltaTime; return; }
            for (int i = 0; i < Visitors.Count; i++)
            {
                CarryStack visitor = Visitors[i];
                if (!visitor.IsPlayer || visitor.IsEmpty) continue;
                foreach (Requirement r in requirements)
                {
                    int pending = Delivered(r.Resource) + InFlight(r.Resource);
                    if (pending >= r.Amount) continue;
                    ResourceItem item = visitor.RemoveTop(r.Resource);
                    if (item == null) continue;
                    Send(item, r.Resource, pending);
                    timer = config.BuildingTransferInterval;
                    return;
                }
            }
        }

        private int InFlight(ResourceType type) => inFlight.TryGetValue(type, out int n) ? n : 0;

        private void Send(ResourceItem item, ResourceType type, int index)
        {
            inFlight[type] = InFlight(type) + 1;
            Vector3 target = PiecePosition(type, index);
            item.FlyToAndDestroy(target, config.ItemFlyHeight, config.ItemFlyDuration, () =>
            {
                inFlight[type] = Mathf.Max(0, InFlight(type) - 1);
                Add(type, 1, true);
            });
            AudioManager.Play(SoundId.Drop);
        }

        private void Add(ResourceType type, int amount, bool animate)
        {
            int before = Delivered(type);
            delivered[type] = before + amount;
            for (int i = before; i < before + amount; i++) OnPieceAdded(type, i, animate);
            if (animate) AudioManager.Play(SoundId.PartPlace, 0.6f);
            RefreshLabel();
            if (IsCompleted) return;
            foreach (Requirement r in requirements) if (Delivered(r.Resource) < r.Amount) return;
            IsCompleted = true;
            RefreshVisibility();
            OnCompleted(animate);
            AnyCompleted?.Invoke(id);
        }

        private void RefreshLabel()
        {
            if (progressView != null)
            {
                ResourceType[] types = new ResourceType[requirements.Length];
                int[] have = new int[requirements.Length], need = new int[requirements.Length];
                for (int i = 0; i < requirements.Length; i++)
                {
                    types[i] = requirements[i].Resource;
                    have[i] = Delivered(requirements[i].Resource);
                    need[i] = requirements[i].Amount;
                }
                progressView.Show(TextUtil.UpperTr(displayName), types, have, need);
                return;
            }
            if (progressLabel == null) return;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(displayName).Append(": ").Append(TotalDelivered).Append('/').Append(TotalRequired);
            if (requirements.Length > 1)
            {
                foreach (Requirement r in requirements)
                    sb.Append('\n').Append("<size=70%>").Append(r.Resource.DisplayName).Append(' ').Append(Delivered(r.Resource)).Append('/').Append(r.Amount).Append("</size>");
            }
            progressLabel.text = sb.ToString();
        }

        // Malzemenin uçacağı yer (köprüde sıradaki tahtanın yeri).
        protected virtual Vector3 PiecePosition(ResourceType type, int index)
        {
            return dropPoint != null ? dropPoint.position : transform.position + Vector3.up;
        }

        protected abstract void OnPieceAdded(ResourceType type, int index, bool animate);
        protected abstract void OnCompleted(bool animate);

        // Kayıt / Test Menüsü.
        public void Restore(Dictionary<string, int> counts, bool completed)
        {
            foreach (Requirement r in requirements)
            {
                int n = completed ? r.Amount : (counts.TryGetValue(id + "/" + r.Resource.Id, out int c) ? c : 0);
                n = Mathf.Min(n, r.Amount) - Delivered(r.Resource);
                if (n > 0) Add(r.Resource, n, false);
            }
        }

        public void Collect(List<SaveData.IdValue> list)
        {
            foreach (Requirement r in requirements) list.Add(new SaveData.IdValue(id + "/" + r.Resource.Id, Delivered(r.Resource)));
        }

        public void ForceComplete()
        {
            foreach (Requirement r in requirements)
            {
                int n = r.Amount - Delivered(r.Resource);
                if (n > 0) Add(r.Resource, n, true);
            }
        }
    }
}
