// TreasureChest.cs
// Gizli hazine sandığı: hafifçe parlar (ışıltı + yerde altın hale). Oyuncu yaklaşınca kapak açılır (kapalı sandık
// açık, altın dolu sandığa döner), para ve kristal fışkırır; bazılarından nadir kostüm çıkar. Bulunan sandıklar
// profilde kalır: yeniden oynayınca açık ve soluk görünür, ödül vermez.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class TreasureChest : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [Tooltip("Kayıt kimliği (küçük harf).")]
        [SerializeField] private string id = "";
        [SerializeField, Min(0)] private int coins = 60;
        [SerializeField, Min(0)] private int crystals = 3;
        [Tooltip("Sandıktan çıkan kostüm (boş olabilir).")]
        [SerializeField] private CostumeDefinition costume;
        [SerializeField] private GameObject closedModel;
        [SerializeField] private GameObject openModel;
        [Tooltip("Bulunmamışken parlayan kısım (ışıltı, hale).")]
        [SerializeField] private GameObject glow;
        [Tooltip("Test Menüsü \"Tüm Sandıkları Göster\": uzaktan görünen ışık sütunu.")]
        [SerializeField] private GameObject beacon;

        private static readonly List<TreasureChest> all = new List<TreasureChest>();
        public static IReadOnlyList<TreasureChest> All => all;
        public static event Action<TreasureChest> Opened;

        private Transform player;

        public string Id => id;
        public bool IsOpen { get; private set; }
        public int Coins => coins;
        public int Crystals => crystals;

        private void Awake()
        {
            all.Add(this);
            if (beacon != null) beacon.SetActive(false);
        }

        private void OnDestroy()
        {
            all.Remove(this);
        }

        private void Start()
        {
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
            if (ProfileManager.Instance != null && ProfileManager.Instance.HasChest(id)) ShowFound();
        }

        // Daha önce bulunmuş: açık ve soluk, ödülsüz.
        private void ShowFound()
        {
            IsOpen = true;
            closedModel.SetActive(false);
            openModel.SetActive(true);
            if (glow != null) glow.SetActive(false);
            Faded.Apply(openModel, config.FoundItemTint);
        }

        private void Update()
        {
            if (IsOpen || player == null) return;
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < config.ChestOpenDistance * config.ChestOpenDistance) Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            if (ProfileManager.Instance != null) ProfileManager.Instance.MarkChest(id);
            closedModel.SetActive(false);
            openModel.SetActive(true);
            Tween.PopIn(openModel.transform, openModel.transform.localScale, 0.35f);
            if (glow != null) glow.SetActive(false);
            if (beacon != null) beacon.SetActive(false);

            Vector3 top = transform.position + Vector3.up * 0.9f;
            AudioManager.Play(SoundId.ChestOpen);
            Haptics.Vibrate();
            if (Effects.Instance != null)
            {
                Effects.Instance.GoldBurst(top);
                if (coins > 0) Effects.Instance.CoinPop(top, coins);
                if (crystals > 0) Effects.Instance.ShowText(top + Vector3.up * 1.3f, "+" + crystals + " Kristal", UiColors.Crystal);
            }
            if (coins > 0 && CurrencyManager.Instance != null) CurrencyManager.Instance.Add(coins);
            if (crystals > 0 && ProfileManager.Instance != null) ProfileManager.Instance.AddCrystals(crystals);
            if (costume != null && ProfileManager.Instance != null && !ProfileManager.Instance.Owns(costume))
            {
                ProfileManager.Instance.Unlock(costume);
                if (CelebrationBanner.Instance != null) CelebrationBanner.Instance.Show("Yeni kostüm!", costume.DisplayName, 2.2f);
            }
            Opened?.Invoke(this);
        }

        // Test Menüsü: bulunmamış tüm sandıkların üstünde ışık sütunu.
        public static void ShowAll(bool show)
        {
            foreach (TreasureChest c in all)
                if (c != null && c.beacon != null) c.beacon.SetActive(show && !c.IsOpen);
        }
    }
}
