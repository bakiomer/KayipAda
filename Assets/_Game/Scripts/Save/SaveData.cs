// SaveData.cs
// Cihazda JSON olarak saklanan kayıt: para, açık alanlar, yarım ödemeler, geliştirmeler, gemi ilerlemesi,
// oyun süresi, köprü/onarım ilerlemesi, yan görevler, altın figür; kalıcı kısım (kaçıştan sonra da silinmez):
// kaptanlık, kristal, bulunan sandıklar, koleksiyon, kostümler.

using System;
using System.Collections.Generic;

namespace KayipAda
{
    [Serializable]
    public class SaveData
    {
        [Serializable]
        public class IdValue
        {
            public string Id;
            public int Value;
            public IdValue(string id, int value) { Id = id; Value = value; }
        }

        // Bu adadaki ilerleme ("Tekrar oyna" ile sıfırlanır)
        public int Coins;
        public List<string> Unlocked = new List<string>();
        public List<IdValue> Paid = new List<IdValue>();
        public List<IdValue> Upgrades = new List<IdValue>();
        public List<IdValue> Crew = new List<IdValue>(); // Tayfa Barakası'nda tutulan işçi sayıları
        public List<IdValue> Sites = new List<IdValue>();  // köprü/onarım teslimleri ("kimlik/kaynak" → adet)
        public List<string> SitesDone = new List<string>();
        public List<string> QuestsDone = new List<string>();
        public List<IdValue> QuestProgress = new List<IdValue>();
        public List<string> FigureParts = new List<string>();
        public int ShipStage;
        public List<IdValue> ShipDelivered = new List<IdValue>();
        public float PlayTime;
        public int EarnedCoins; // bu adada kazanılan toplam para (kaçış ekranında sayılır)

        // Kalıcı (kaçıştan sonra da kalır)
        public int CaptainXp;
        public int Escapes;
        public int Crystals;
        public List<string> FoundChests = new List<string>();
        public List<string> Collection = new List<string>();
        public bool CollectionRewarded;
        public List<string> OwnedCostumes = new List<string>();
        public string CharacterCostume = "";
        public string SailCostume = "";
        public int LegendaryShips;

        public void ResetRun()
        {
            Coins = 0;
            Unlocked.Clear();
            Paid.Clear();
            Upgrades.Clear();
            Crew.Clear();
            Sites.Clear();
            SitesDone.Clear();
            QuestsDone.Clear();
            QuestProgress.Clear();
            FigureParts.Clear();
            ShipStage = 0;
            ShipDelivered.Clear();
            PlayTime = 0f;
            EarnedCoins = 0;
        }

        public static Dictionary<string, int> ToDictionary(List<IdValue> list)
        {
            Dictionary<string, int> d = new Dictionary<string, int>();
            foreach (IdValue v in list) if (v != null && v.Id != null) d[v.Id] = v.Value;
            return d;
        }
    }
}
