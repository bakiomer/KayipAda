// QuestManager.cs
// İsteğe bağlı yan görevler: aynı anda en fazla GameConfig.MaxActiveQuests görev sol üstte kart olarak görünür.
// Görevler listedeki sırayla, gerektirdiği bölge açılınca çıkar. İlerleme oyun olaylarından gelir (toplama, satış,
// üretim, onarım, sandık, bölge açılması, altın figür parçası). Bitince ödül (para/kristal) + kısa kutlama, yerine
// sıradaki görev gelir. Altın figür parçaları da burada sayılır (3'ü bulununca gemiye altın figür takılır).
// İlerleme bu adaya aittir ("Tekrar oyna" ile sıfırlanır); kristaller profilde kalır.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-90)]
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public event Action<QuestDefinition> QuestCompleted;
        public event Action FigureChanged;

        [SerializeField] private GameConfig config;
        [SerializeField] private QuestDefinition[] quests = new QuestDefinition[0];
        [Tooltip("Sol üstteki görev kartları (en fazla görev sayısı kadar).")]
        [SerializeField] private QuestCard[] cards = new QuestCard[0];

        private readonly List<QuestDefinition> active = new List<QuestDefinition>();
        private readonly Dictionary<string, int> progress = new Dictionary<string, int>();
        private readonly HashSet<string> completed = new HashSet<string>();
        private readonly HashSet<string> figureParts = new HashSet<string>();
        private float refillDelay;

        public IReadOnlyList<QuestDefinition> Active => active;
        public IReadOnlyList<QuestDefinition> All => quests;
        public int CompletedCount => completed.Count;
        public int FigurePartCount => figureParts.Count;
        public bool HasLegendaryFigure => figureParts.Count >= config.FigurePartsNeeded;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            ResourceNode.Gathered += OnGathered;
            Market.Sold += OnSold;
            ProcessingBuilding.AnyProduced += OnProduced;
            ConstructionSite.AnyCompleted += OnSiteCompleted;
            TreasureChest.Opened += OnChestOpened;
        }

        private void OnDisable()
        {
            ResourceNode.Gathered -= OnGathered;
            Market.Sold -= OnSold;
            ProcessingBuilding.AnyProduced -= OnProduced;
            ConstructionSite.AnyCompleted -= OnSiteCompleted;
            TreasureChest.Opened -= OnChestOpened;
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked -= OnAreaUnlocked;
        }

        private void Start()
        {
            if (AreaManager.Instance != null) AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
            Refill();
        }

        private void Update()
        {
            if (refillDelay <= 0f) return;
            refillDelay -= Time.deltaTime;
            if (refillDelay <= 0f) Refill();
        }

        // ---------------------------------------------------------------- Olaylar

        private void OnGathered(ResourceType type, bool byPlayer) { if (byPlayer) Advance(QuestKind.Collect, type, 1); }
        private void OnSold(ResourceType type, bool byPlayer) { if (byPlayer) Advance(QuestKind.Sell, type, 1); }
        private void OnProduced(ResourceType type, int amount) => Advance(QuestKind.Produce, type, amount);
        private void OnChestOpened(TreasureChest chest) => Advance(QuestKind.FindChest, null, 1);
        private void OnSiteCompleted(string id) => CheckStates();
        private void OnAreaUnlocked(AreaDefinition area) { CheckStates(); Refill(); }

        private void Advance(QuestKind kind, ResourceType type, int amount)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                QuestDefinition q = active[i];
                if (q.Kind != kind) continue;
                if (q.Resource != null && q.Resource != type) continue;
                SetProgress(q, Progress(q) + amount);
            }
        }

        // Durum görevleri (onarım, keşif, figür): gerçekleşmiş mi?
        private void CheckStates()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                QuestDefinition q = active[i];
                int state = StateProgress(q);
                if (state >= 0) SetProgress(q, state);
            }
        }

        private int StateProgress(QuestDefinition q)
        {
            switch (q.Kind)
            {
                case QuestKind.Repair:
                    foreach (ConstructionSite s in ConstructionSite.All) if (s != null && s.Id == q.Target) return s.IsCompleted ? q.Amount : 0;
                    return 0;
                case QuestKind.Explore:
                    return q.ExploreArea != null && AreaManager.Instance != null && AreaManager.Instance.IsUnlocked(q.ExploreArea) ? q.Amount : 0;
                case QuestKind.FigurePart:
                    return figureParts.Count;
                case QuestKind.FindChest:
                    // Bütün sandıklar daha önce bulunduysa (yeniden oyun) görev kendiliğinden tamam sayılır.
                    bool anyLeft = false;
                    foreach (TreasureChest c in TreasureChest.All) if (c != null && !c.IsOpen) anyLeft = true;
                    return anyLeft ? -1 : q.Amount;
                default:
                    return -1;
            }
        }

        public int Progress(QuestDefinition q) => progress.TryGetValue(q.Id, out int n) ? n : 0;
        public bool IsCompleted(QuestDefinition q) => q != null && completed.Contains(q.Id);

        private void SetProgress(QuestDefinition q, int value)
        {
            progress[q.Id] = Mathf.Min(value, q.Amount);
            CardFor(q)?.Refresh(Progress(q));
            if (Progress(q) >= q.Amount) Complete(q);
        }

        private QuestCard CardFor(QuestDefinition q)
        {
            foreach (QuestCard card in cards) if (card != null && card.Quest == q) return card;
            return null;
        }

        // ---------------------------------------------------------------- Tamamlama ve yeni görev

        private void Complete(QuestDefinition q)
        {
            if (completed.Contains(q.Id)) return;
            completed.Add(q.Id);
            QuestCard card = CardFor(q);
            if (card != null) card.PlayComplete();
            active.Remove(q);
            if (q.RewardCoins > 0 && CurrencyManager.Instance != null) CurrencyManager.Instance.Add(q.RewardCoins);
            if (q.RewardCrystals > 0 && ProfileManager.Instance != null) ProfileManager.Instance.AddCrystals(q.RewardCrystals);
            AudioManager.Play(SoundId.QuestDone);
            Haptics.Vibrate();
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (Effects.Instance != null && player != null)
            {
                Vector3 p = player.transform.position + Vector3.up * 2.6f;
                Effects.Instance.Confetti(p);
                string reward = (q.RewardCoins > 0 ? "+" + q.RewardCoins + " para  " : "") + (q.RewardCrystals > 0 ? "+" + q.RewardCrystals + " kristal" : "");
                Effects.Instance.ShowText(p, "Görev tamam! " + reward, UiColors.Reward);
            }
            QuestCompleted?.Invoke(q);
            refillDelay = config.QuestRefillDelay;
        }

        private void Refill()
        {
            for (int c = 0; c < cards.Length; c++) if (cards[c] != null && cards[c].IsPlaying) return; // kart animasyonu bitsin
            foreach (QuestDefinition q in quests)
            {
                if (active.Count >= config.MaxActiveQuests) break;
                if (q == null || completed.Contains(q.Id) || active.Contains(q)) continue;
                if (q.RequiredArea != null && (AreaManager.Instance == null || !AreaManager.Instance.IsUnlocked(q.RequiredArea))) continue;
                active.Add(q);
            }
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                if (i < active.Count) cards[i].Bind(active[i], Progress(active[i]));
                else cards[i].Clear();
            }
            CheckStates();
        }

        // ---------------------------------------------------------------- Altın figür

        public bool HasFigurePart(string id) => figureParts.Contains(id);

        public void AddFigurePart(string id)
        {
            if (!figureParts.Add(id)) return;
            int n = figureParts.Count;
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (Effects.Instance != null && player != null)
                Effects.Instance.ShowText(player.transform.position + Vector3.up * 2.4f, "Altın Figür " + Mathf.Min(n, config.FigurePartsNeeded) + "/" + config.FigurePartsNeeded, UiColors.Gold);
            FigureChanged?.Invoke();
            if (n == config.FigurePartsNeeded && CelebrationBanner.Instance != null)
                CelebrationBanner.Instance.Show("Altın Figür tamam!", "Geminin burnuna takıldı", 2.4f);
            CheckStates();
        }

        // ---------------------------------------------------------------- Kayıt ve Test Menüsü

        public void Restore(SaveData data)
        {
            completed.Clear();
            foreach (string id in data.QuestsDone) completed.Add(id);
            progress.Clear();
            foreach (SaveData.IdValue v in data.QuestProgress) if (v != null && v.Id != null) progress[v.Id] = v.Value;
            figureParts.Clear();
            foreach (string id in data.FigureParts) figureParts.Add(id);
            active.Clear();
            Refill();
            FigureChanged?.Invoke();
        }

        public void Collect(SaveData data)
        {
            data.QuestsDone = new List<string>(completed);
            data.QuestProgress.Clear();
            foreach (KeyValuePair<string, int> p in progress) data.QuestProgress.Add(new SaveData.IdValue(p.Key, p.Value));
            data.FigureParts = new List<string>(figureParts);
        }

        // Test Menüsü: açık görevleri (ve ardından gelenleri) hemen tamamlar.
        public void CompleteAllActive()
        {
            for (int guard = 0; guard < quests.Length && active.Count > 0; guard++)
            {
                List<QuestDefinition> now = new List<QuestDefinition>(active);
                foreach (QuestDefinition q in now) SetProgress(q, q.Amount);
                foreach (QuestCard card in cards) if (card != null) card.StopAnimation();
                Refill();
            }
        }
    }
}
