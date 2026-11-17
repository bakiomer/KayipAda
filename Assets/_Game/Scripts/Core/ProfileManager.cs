// ProfileManager.cs
// Kaçıştan sonra da kalan oyuncu profili: kristal (ikinci para birimi), bulunan sandıklar, koleksiyon parçaları,
// sahip olunan/giyilen kostümler. Kristal SADECE sandık, görev, koleksiyon ve kaçıştan gelir; "Tekrar oyna" ile
// sıfırlanmaz. Kayıt sistemi (SaveManager) bu bilgileri SaveData'nın kalıcı kısmında saklar.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-100)]
    public class ProfileManager : MonoBehaviour
    {
        public static ProfileManager Instance { get; private set; }

        public event Action<int> CrystalsChanged;
        public event Action CostumesChanged;

        [SerializeField] private GameConfig config;
        [Tooltip("Tüm kostümler (karakter + yelken/bayrak).")]
        [SerializeField] private CostumeDefinition[] costumes = new CostumeDefinition[0];

        private int crystals;
        private readonly HashSet<string> chests = new HashSet<string>();
        private readonly HashSet<string> collection = new HashSet<string>();
        private readonly HashSet<string> owned = new HashSet<string>();
        private string characterCostume = "";
        private string sailCostume = "";

        public int Crystals => crystals;
        public IReadOnlyList<CostumeDefinition> Costumes => costumes;
        public IEnumerable<string> FoundChests => chests;
        public IEnumerable<string> FoundCollectibles => collection;
        public IEnumerable<string> OwnedCostumes => owned;
        public int ChestCount => chests.Count;
        public int CollectionCount => collection.Count;
        public bool CollectionRewarded { get; set; }

        private void Awake()
        {
            Instance = this;
            foreach (CostumeDefinition c in costumes) if (c != null && c.OwnedByDefault) owned.Add(c.Id);
        }

        // ---------------------------------------------------------------- Kristal

        public void AddCrystals(int amount)
        {
            if (amount <= 0) return;
            crystals += amount;
            CrystalsChanged?.Invoke(crystals);
        }

        public bool TrySpendCrystals(int amount)
        {
            if (amount < 0 || crystals < amount) return false;
            crystals -= amount;
            CrystalsChanged?.Invoke(crystals);
            return true;
        }

        // ---------------------------------------------------------------- Sandıklar ve koleksiyon

        public bool HasChest(string id) => chests.Contains(id);
        public bool HasCollectible(string id) => collection.Contains(id);

        // İlk kez bulunduysa true döner.
        public bool MarkChest(string id) => chests.Add(id);
        public bool MarkCollectible(string id) => collection.Add(id);

        // ---------------------------------------------------------------- Kostümler

        public bool Owns(CostumeDefinition c) => c != null && owned.Contains(c.Id);

        public void Unlock(CostumeDefinition c)
        {
            if (c == null || !owned.Add(c.Id)) return;
            CostumesChanged?.Invoke();
        }

        // Kristalle satın al (sadece sandıktan çıkanlar satılmaz).
        public bool TryBuy(CostumeDefinition c)
        {
            if (c == null || Owns(c) || c.ChestOnly) return false;
            if (!TrySpendCrystals(c.CrystalPrice)) return false;
            Unlock(c);
            return true;
        }

        public CostumeDefinition Equipped(CostumeSlot slot)
        {
            string id = slot == CostumeSlot.Character ? characterCostume : sailCostume;
            CostumeDefinition fallback = null;
            foreach (CostumeDefinition c in costumes)
            {
                if (c == null || c.Slot != slot) continue;
                if (c.Id == id) return c;
                if (fallback == null && c.OwnedByDefault) fallback = c;
            }
            return fallback;
        }

        public bool Equip(CostumeDefinition c)
        {
            if (c == null || !Owns(c)) return false;
            if (c.Slot == CostumeSlot.Character) characterCostume = c.Id;
            else sailCostume = c.Id;
            CostumesChanged?.Invoke();
            return true;
        }

        // Sahip olunanlar arasında sıradakini giyer (Test Menüsü).
        public CostumeDefinition EquipNext(CostumeSlot slot)
        {
            List<CostumeDefinition> list = new List<CostumeDefinition>();
            foreach (CostumeDefinition c in costumes) if (c != null && c.Slot == slot && Owns(c)) list.Add(c);
            if (list.Count == 0) return null;
            int index = list.IndexOf(Equipped(slot));
            CostumeDefinition next = list[(index + 1) % list.Count];
            Equip(next);
            return next;
        }

        public CostumeDefinition FindCostume(string id)
        {
            foreach (CostumeDefinition c in costumes) if (c != null && c.Id == id) return c;
            return null;
        }

        // ---------------------------------------------------------------- Kayıt

        public void Restore(SaveData data)
        {
            crystals = data.Crystals;
            chests.Clear();
            collection.Clear();
            foreach (string id in data.FoundChests) chests.Add(id);
            foreach (string id in data.Collection) collection.Add(id);
            foreach (string id in data.OwnedCostumes) owned.Add(id);
            characterCostume = data.CharacterCostume ?? "";
            sailCostume = data.SailCostume ?? "";
            CollectionRewarded = data.CollectionRewarded;
            CrystalsChanged?.Invoke(crystals);
            CostumesChanged?.Invoke();
        }

        public void Collect(SaveData data)
        {
            data.Crystals = crystals;
            data.FoundChests = new List<string>(chests);
            data.Collection = new List<string>(collection);
            data.OwnedCostumes = new List<string>(owned);
            data.CharacterCostume = characterCostume;
            data.SailCostume = sailCostume;
            data.CollectionRewarded = CollectionRewarded;
        }

        // Test Menüsü: tümünü aç.
        public void UnlockAll()
        {
            foreach (CostumeDefinition c in costumes) if (c != null) owned.Add(c.Id);
            CostumesChanged?.Invoke();
        }
    }
}
