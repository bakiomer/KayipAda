// ShipBuilder.cs
// İskeledeki gemi inşası (4 aşama: Omurga, Gövde, Direk ve Yelken, Erzak; ShipConfig'te).
// Teslim edilen her malzemeyle gemi aşağıdan yukarıya biraz daha belirir (BuildReveal shader), son aşamada
// güvertede erzak sandıkları zıplayarak çıkar. Teslim alanının üstünde "Gövde: 12/30" gibi 3D gösterge vardır.
// Gemi bitince son gece başlar (NightDefense). Kaçışta gemi yelken açıp uzaklaşır.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class ShipBuilder : MonoBehaviour
    {
        public static ShipBuilder Instance { get; private set; }

        public event Action StageCompleted;
        public event Action ShipCompleted;

        [SerializeField] private ShipConfig ship;
        [SerializeField] private GameConfig config;
        [Tooltip("Aşağıdan yukarıya belirecek gemi modeli (BuildReveal malzemeli).")]
        [SerializeField] private Transform shipModel;
        [Tooltip("Son aşamada (erzak) güvertede beliren nesneler.")]
        [SerializeField] private Transform suppliesRoot;
        [Tooltip("İnşa iskelesi (gemi bitince kaybolur).")]
        [SerializeField] private GameObject scaffolding;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private ProgressRing progressRing;
        [Tooltip("Malzemelerin uçtuğu nokta (gemi gövdesi).")]
        [SerializeField] private Transform deliveryTarget;
        [Tooltip("Teslim alanı (oyuncu ve Taşıyıcı işçiler buraya gelir).")]
        [SerializeField] private ShipDelivery delivery;

        private readonly Dictionary<string, int> delivered = new Dictionary<string, int>();
        private Renderer[] renderers;
        private readonly List<Material> revealMaterials = new List<Material>();
        private float reveal;
        private float shownReveal;
        private float minY, maxY;
        private Vector3[] supplyScales;

        public int StageIndex { get; private set; }
        public bool IsComplete => StageIndex >= ship.Stages.Length;

        // Oyuncu gemiye ilk malzemeyi getirdi mi? (Taşıyıcı işçiler ancak bundan sonra gemiye yardım eder;
        // öncesinde ürünleri Pazar'da satıp para kazandırırlar.)
        public bool IsStarted => StageIndex > 0 || delivered.Count > 0;
        public ShipConfig Config => ship;
        public ShipDelivery Delivery => delivery;
        public Transform DeliveryTarget => deliveryTarget;
        public Transform Model => shipModel;

        private void Awake()
        {
            Instance = this;
            renderers = shipModel.GetComponentsInChildren<Renderer>(true);
            // Her parçanın TÜM malzemelerine kendi kopyası: belirme oranı sadece bu gemiyi etkilesin.
            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = new Material(mats[i]);
                    revealMaterials.Add(mats[i]);
                }
                r.sharedMaterials = mats;
            }
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(shipModel.position, Vector3.one);
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
            minY = b.min.y;
            maxY = b.max.y;

            supplyScales = new Vector3[suppliesRoot != null ? suppliesRoot.childCount : 0];
            for (int i = 0; i < supplyScales.Length; i++) supplyScales[i] = suppliesRoot.GetChild(i).localScale;
            SetSupplies(0, false);
        }

        private void Start()
        {
            reveal = TargetReveal();
            shownReveal = reveal;
            ApplyReveal();
            RefreshLabel();
            if (IsComplete && scaffolding != null) scaffolding.SetActive(false);
        }

        // Bu kaynak şu anki aşamada hâlâ gerekli mi?
        public bool Needs(ResourceType type)
        {
            if (IsComplete || type == null) return false;
            foreach (ShipConfig.Requirement req in ship.Stages[StageIndex].Requirements)
            {
                if (req.Resource == type) return Delivered(type) < Required(req);
            }
            return false;
        }

        public int Delivered(ResourceType type) => delivered.TryGetValue(type.Id, out int n) ? n : 0;
        private int Required(ShipConfig.Requirement req) => config.ScaleAmount(req.Amount);

        // Teslim alanı bir parça teslim edince çağırır.
        public void Deliver(ResourceItem item)
        {
            ResourceType type = item.Type;
            if (!Needs(type))
            {
                Destroy(item.gameObject);
                return;
            }
            delivered[type.Id] = Delivered(type) + 1;
            item.FlyToAndDestroy(deliveryTarget.position, config.ItemFlyHeight * 1.5f, config.ItemFlyDuration * 1.4f, () =>
            {
                AudioManager.Play(SoundId.Build, 0.6f);
                if (Effects.Instance != null && UnityEngine.Random.value < 0.35f) Effects.Instance.Sparkle(deliveryTarget.position);
            });

            if (StageDone()) CompleteStage();
            reveal = TargetReveal();
            RefreshLabel();
        }

        private bool StageDone()
        {
            foreach (ShipConfig.Requirement req in ship.Stages[StageIndex].Requirements)
            {
                if (Delivered(req.Resource) < Required(req)) return false;
            }
            return true;
        }

        private void CompleteStage()
        {
            ShipConfig.Stage finished = ship.Stages[StageIndex];
            if (StageIndex == 0 && ship.KeelDoneArea != null) AreaManager.Instance.Unlock(ship.KeelDoneArea);
            StageIndex++;
            delivered.Clear();

            if (Effects.Instance != null)
            {
                Effects.Instance.Confetti(deliveryTarget.position + Vector3.up);
                Effects.Instance.ShowText(deliveryTarget.position + Vector3.up * 3f, finished.Name + " tamam!", new Color(0.6f, 1f, 0.6f));
            }
            AudioManager.Play(SoundId.Unlock);
            StageCompleted?.Invoke();

            if (IsComplete)
            {
                SetSupplies(supplyScales.Length, true);
                if (scaffolding != null) Tween.PopOut(scaffolding.transform, 0.5f, () => scaffolding.SetActive(false));
                ShipCompleted?.Invoke();
            }
        }

        // Kayıt sistemi için: aşama ve teslim edilenler.
        public IReadOnlyDictionary<string, int> DeliveredCounts => delivered;

        public void Restore(int stage, IDictionary<string, int> counts)
        {
            StageIndex = Mathf.Clamp(stage, 0, ship.Stages.Length);
            delivered.Clear();
            if (counts != null) foreach (KeyValuePair<string, int> p in counts) delivered[p.Key] = p.Value;
            reveal = TargetReveal();
            shownReveal = reveal;
            ApplyReveal();
            SetSupplies(IsComplete ? supplyScales.Length : 0, false);
            if (scaffolding != null) scaffolding.SetActive(!IsComplete);
            RefreshLabel();
        }

        // Geminin görünmesi gereken oranı: biten aşamaların sonu + şu anki aşamanın ilerlemesi.
        private float TargetReveal()
        {
            if (IsComplete) return 1f;
            float start = StageIndex == 0 ? 0f : ship.Stages[StageIndex - 1].RevealAtEnd;
            float end = ship.Stages[StageIndex].RevealAtEnd;
            return Mathf.Lerp(start, end, StageProgress());
        }

        // Şu anki aşamanın ilerlemesi (0..1): tüm malzemelerin toplamı üzerinden.
        public float StageProgress()
        {
            if (IsComplete) return 1f;
            int have = 0, need = 0;
            foreach (ShipConfig.Requirement req in ship.Stages[StageIndex].Requirements)
            {
                int required = Required(req);
                need += required;
                have += Mathf.Min(Delivered(req.Resource), required);
            }
            return need == 0 ? 1f : (float)have / need;
        }

        private void Update()
        {
            if (Mathf.Abs(shownReveal - reveal) > 0.0005f)
            {
                shownReveal = Mathf.MoveTowards(shownReveal, reveal, Time.deltaTime * 0.25f);
                ApplyReveal();
            }

            // Erzak aşamasında sandıklar teslim edildikçe tek tek belirir.
            if (!IsComplete && StageIndex == ship.Stages.Length - 1)
            {
                SetSupplies(Mathf.FloorToInt(StageProgress() * supplyScales.Length), true);
            }
        }

        private void ApplyReveal()
        {
            foreach (Material m in revealMaterials)
            {
                m.SetFloat("_Reveal", shownReveal);
                m.SetFloat("_MinY", minY);
                m.SetFloat("_MaxY", maxY);
            }
        }

        private int suppliesShown = -1;

        private void SetSupplies(int count, bool animate)
        {
            if (suppliesRoot == null || count == suppliesShown) return;
            for (int i = 0; i < suppliesRoot.childCount; i++)
            {
                Transform child = suppliesRoot.GetChild(i);
                bool show = i < count;
                bool wasShown = child.gameObject.activeSelf;
                child.gameObject.SetActive(show);
                if (show && !wasShown && animate) Tween.PopIn(child, supplyScales[i], config.PopInDuration);
            }
            suppliesShown = count;
        }

        private void RefreshLabel()
        {
            if (progressRing != null) progressRing.SetProgress(StageProgress());
            if (progressLabel == null) return;
            if (IsComplete)
            {
                progressLabel.text = "GEMİ HAZIR!";
                return;
            }

            ShipConfig.Stage stage = ship.Stages[StageIndex];
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<size=120%>").Append(stage.Name).Append("</size>  (").Append(StageIndex + 1).Append('/').Append(ship.Stages.Length).Append(')');
            foreach (ShipConfig.Requirement req in stage.Requirements)
            {
                sb.Append('\n').Append(req.Resource.DisplayName).Append(": ")
                  .Append(Mathf.Min(Delivered(req.Resource), Required(req))).Append('/').Append(Required(req));
            }
            progressLabel.text = sb.ToString();
        }

        // Kaçış: gemi denize doğru süzülür (kamera takip eder).
        public void Sail(Vector3 direction)
        {
            Transform mover = transform;
            Vector3 from = mover.position;
            Tween.Value(this, ship.SailDuration, t =>
            {
                float ease = t * t; // yavaş kalkış, sonra hızlanır
                mover.position = from + direction.normalized * (ship.SailSpeed * ship.SailDuration * ease)
                               + Vector3.up * Mathf.Sin(Time.time * 1.5f) * 0.08f;
                mover.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.2f) * 2f, mover.eulerAngles.y, Mathf.Sin(Time.time) * 3f);
            });
        }
    }
}
