// DiscoveryTests.cs
// Keşif turunun testleri: köprü adacığı açar (bulut dağılır, köprüden yürünür), sandık ödül verir, koleksiyon kaydedilir,
// yan görev tamamlanır, bina seviye atlar, kristal kaçıştan sonra kalır, kostüm değişir, altın figür takılır.
// Ekran görüntüleri: Docs/Screenshots/<önek>_B_*.png (Docs/Screenshots/Kesif'e kopyalanır).

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class DiscoveryTests : GameTestBase
    {
        private GameConfig Config => FindAny<DevMenu>().Config;

        private static BridgeBuilder Bridge(string id)
        {
            foreach (ConstructionSite s in ConstructionSite.All) if (s is BridgeBuilder b && s.Id == id) return b;
            return null;
        }

        private static ResourceType Resource(string id)
        {
            foreach (ResourceType t in Resources.FindObjectsOfTypeAll<ResourceType>()) if (t.Id == id) return t;
            return null;
        }

        // Köprü: kalas teslim edildikçe tahtalar belirir; bitince adacık açılır, bulut dağılır, köprüden adacığa yürünür.
        [UnityTest]
        public IEnumerator Bridge_DeliveringPlanksOpensIslet()
        {
            BridgeBuilder bridge = Bridge("bridge_wreck");
            Assert.IsNotNull(bridge, "Batık Koyu köprüsü yok");
            RegionCloud cloud = null;
            foreach (RegionCloud c in RegionCloud.All) if (c.Area == bridge.IslandArea) cloud = c;
            Assert.IsNotNull(cloud, "Adacığın bulutu yok");
            Assert.IsFalse(bridge.IsAvailable, "Bıçkıhane kapalıyken köprü görünüyor");

            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            yield return null;
            Assert.IsTrue(bridge.IsAvailable, "Bıçkıhane açılınca köprü görünmedi");
            Vector2 mid = Vector2.Lerp(new Vector2(bridge.EndA.x, bridge.EndA.z), new Vector2(bridge.EndB.x, bridge.EndB.z), 0.5f);
            Assert.IsFalse(IslandShape.Instance.Contains(mid, 0f), "Köprü bitmeden ortası yürünebilir");

            DevCommands.FillPlayerStack(Resource("plank"));
            Teleport(bridge.transform.position);
            yield return WaitFor(() => bridge.TotalDelivered >= 8, 8f);
            Assert.GreaterOrEqual(bridge.TotalDelivered, 8, "Köprüye kalas teslim edilmedi");
            Teleport(bridge.transform.position + bridge.transform.forward * -2.5f);
            yield return new WaitForSeconds(0.6f);
            Shot("B_kopru_yapiliyor");

            bridge.ForceComplete();
            yield return null;
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(bridge.IslandArea), "Köprü bitince adacık açılmadı");
            Assert.IsTrue(cloud.IsDispersed, "Bulut dağılmadı");
            Assert.IsTrue(IslandShape.Instance.Contains(mid, 0f), "Köprünün üstü yürünebilir olmadı");
            yield return new WaitForSeconds(Config.CloudFocusTime * 0.6f);
            Shot("B_bulut_dagiliyor");
            yield return WaitFor(() => !cloud.gameObject.activeSelf, 4f);
            yield return WaitFor(() => IslandNavMesh.IsUpToDate, 5f);
            Vector3 forge = new Vector3(-31f, 0f, 6f);
            Assert.IsTrue(IslandNavMesh.HasCompletePath(Player.transform.position, forge), "Köprüden adacığa yol yok (NavMesh)");

            // Oyuncu köprüden yürüyerek adacığa geçer.
            Teleport(bridge.EndA + (bridge.EndA - bridge.EndB).normalized * 0.8f);
            Vector3 dir = (bridge.EndB - bridge.EndA).normalized;
            Player.ScriptedInput = new Vector2(dir.x, dir.z);
            yield return new WaitForSeconds(3.5f);
            Player.ScriptedInput = Vector2.zero;
            Vector3 p = Player.transform.position;
            Assert.AreEqual(0, IslandShape.Instance.NearestLobe(new Vector2(p.x, p.z)), "Oyuncu köprüden adacığa geçemedi (" + p + ")");
            Shot("B_kopru_tamam");
        }

        // Sandık: yaklaşınca açılır, para + kristal verir; profilde kalır.
        [UnityTest]
        public IEnumerator Chest_OpensAndGivesRewards()
        {
            TreasureChest chest = null;
            foreach (TreasureChest c in TreasureChest.All) if (c.Id == "chest_3") chest = c;
            Assert.IsNotNull(chest, "Sandık yok");
            Assert.AreEqual(8, TreasureChest.All.Count, "8 sandık bekleniyordu");
            int coins = CurrencyManager.Instance.Coins, crystals = ProfileManager.Instance.Crystals;
            Teleport(chest.transform.position + Vector3.back * 3f);
            yield return new WaitForSeconds(0.3f);
            Shot("B_sandik_kapali");
            Teleport(chest.transform.position + Vector3.back * 1.2f);
            yield return WaitFor(() => chest.IsOpen, 2f);
            Assert.IsTrue(chest.IsOpen, "Sandık açılmadı");
            yield return new WaitForSeconds(0.5f);
            Shot("B_sandik_acildi");
            Assert.AreEqual(coins + chest.Coins, CurrencyManager.Instance.Coins, "Sandık parası verilmedi");
            Assert.AreEqual(crystals + chest.Crystals, ProfileManager.Instance.Crystals, "Sandık kristali verilmedi");
            Assert.IsTrue(ProfileManager.Instance.HasChest("chest_3"), "Sandık profile yazılmadı");
        }

        // Koleksiyon: alınınca 3D kart + "Koleksiyon 1/5"; profile kaydedilir ve geri yüklenir; 5/5 ek kristal.
        [UnityTest]
        public IEnumerator Collection_SavedAndShown()
        {
            Collectible item = null;
            foreach (Collectible c in Collectible.All) if (c.Id == "collect_bottle") item = c;
            Assert.IsNotNull(item, "Koleksiyon eşyası yok");
            Assert.AreEqual(5, Collectible.CountOfKind(Collectible.Kind.Collection), "5 koleksiyon eşyası bekleniyordu");
            Teleport(item.transform.position + Vector3.back * 0.6f);
            yield return WaitFor(() => item.IsTaken, 2f);
            Assert.IsTrue(CollectionPopup.Instance.IsShown, "Koleksiyon kartı açılmadı");
            yield return new WaitForSeconds(0.9f);
            Shot("B_koleksiyon_karti");
            Assert.AreEqual(1, ProfileManager.Instance.CollectionCount);

            SaveData data = new SaveData();
            ProfileManager.Instance.Collect(data);
            data.ResetRun(); // kaçış: ada sıfırlanır, koleksiyon kalır
            Assert.Contains("collect_bottle", data.Collection, "Koleksiyon kayda yazılmadı");

            int before = ProfileManager.Instance.Crystals;
            DevCommands.CompleteCollection();
            Assert.AreEqual(5, ProfileManager.Instance.CollectionCount, "Koleksiyon tamamlanmadı");
            Assert.AreEqual(before + Config.CollectionBonusCrystals, ProfileManager.Instance.Crystals, "5/5 ek kristal verilmedi");
        }

        // Yan görev: ilk görev (Odun Topla) oyunla ilerler ve tamamlanınca ödül verir; yerine yenisi gelir.
        [UnityTest]
        public IEnumerator Quest_CollectWoodCompletes()
        {
            QuestManager quests = QuestManager.Instance;
            Assert.IsNotNull(quests);
            Assert.AreEqual(15, quests.All.Count, "15 yan görev bekleniyordu");
            Assert.LessOrEqual(quests.Active.Count, 2, "Aynı anda en fazla 2 görev");
            QuestDefinition wood = quests.Active[0];
            Assert.AreEqual(QuestKind.Collect, wood.Kind);
            yield return new WaitForSeconds(0.6f);
            Shot("B_gorev_karti");

            foreach (UpgradeType t in UpgradeManager.Instance.Upgrades)
                if (t.Stat == UpgradeStat.PlayerCapacity) UpgradeManager.Instance.SetLevel(t, t.MaxLevel);
            int coins = CurrencyManager.Instance.Coins;
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (node.name.StartsWith("Node_Palm") == false) continue;
                Teleport(node.GetStandPoint(node.transform.position + Vector3.back * 3f));
                yield return WaitFor(() => node.IsDepleted || quests.Progress(wood) >= wood.Amount, 6f);
                if (quests.Progress(wood) >= wood.Amount) break;
            }
            Assert.GreaterOrEqual(quests.Progress(wood), wood.Amount, "Odun görevi ilerlemedi");
            Assert.Greater(CurrencyManager.Instance.Coins, coins, "Görev ödülü verilmedi");
            yield return new WaitForSeconds(Config.QuestRefillDelay + 1.6f);
            bool stillActive = false;
            foreach (QuestDefinition q in quests.Active) if (q == wood) stillActive = true;
            Assert.IsFalse(stillActive, "Biten görev kartta kaldı");
            Shot("B_gorev_yenisi");
        }

        // Onarım görevi: kuyuya malzeme teslim edilince kırık kuyu onarılır.
        [UnityTest]
        public IEnumerator Repair_WellGetsFixed()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            RepairSite well = null;
            foreach (ConstructionSite s in ConstructionSite.All) if (s.Id == "well") well = s as RepairSite;
            Assert.IsNotNull(well, "Kuyu yok");
            yield return null;
            Teleport(well.transform.position + well.transform.forward * -3f);
            yield return new WaitForSeconds(0.4f);
            Shot("B_kuyu_kirik");
            well.ForceComplete();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(well.IsCompleted, "Kuyu onarılmadı");
            Shot("B_kuyu_onarildi");
        }

        // Bina seviyesi: seviye karesi açılınca hız ve kapasite artar, bina büyür.
        [UnityTest]
        public IEnumerator BuildingLevel_RaisesSpeedAndSize()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            yield return new WaitForSeconds(0.8f);
            ProcessingBuilding sawmill = null;
            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None)) if (b.Type.Output.Id == "plank") sawmill = b;
            Assert.IsNotNull(sawmill);
            Assert.AreEqual(1, sawmill.Level);
            float speed = sawmill.SpeedMultiplier;
            Transform body = sawmill.transform.Find("Body");
            float scale = body.localScale.x;
            UnlockPad lv2 = Find<UnlockPad>("Pad SawmillLv2");
            Assert.IsNotNull(lv2, "Seviye karesi yok");
            Teleport(lv2.transform.position + Vector3.back * 2.5f);
            yield return new WaitForSeconds(0.5f);
            Shot("B_seviye_karesi");
            AreaManager.Instance.Unlock(lv2.Area);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(2, sawmill.Level, "Bina seviye atlamadı");
            Assert.Greater(sawmill.SpeedMultiplier, speed, "Seviyeyle hız artmadı");
            Assert.Greater(body.localScale.x, scale, "Bina büyümedi");
            Assert.Greater(sawmill.InputPile.Capacity, sawmill.Type.InputCapacity, "Seviyeyle kapasite artmadı");
            Shot("B_seviye_2");
        }

        // Kristal kalıcıdır: "Tekrar oyna" (ResetRun) kristali silmez.
        [UnityTest]
        public IEnumerator Crystals_PersistAfterEscape()
        {
            yield return null;
            ProfileManager.Instance.AddCrystals(7);
            SaveData data = new SaveData();
            ProfileManager.Instance.Collect(data);
            int crystals = data.Crystals;
            data.ResetRun();
            Assert.AreEqual(crystals, data.Crystals, "Kaçıştan sonra kristal silindi");
            Assert.GreaterOrEqual(crystals, 7);
        }

        // Kostüm: sıradaki kostümü giyince oyuncunun modeli değişir; yelken rengi değişir.
        [UnityTest]
        public IEnumerator Costume_ChangesPlayerAndSail()
        {
            yield return null;
            PlayerCostume costume = Player.GetComponent<PlayerCostume>();
            SailCostume sail = FindAny<SailCostume>();
            Assert.IsNotNull(costume);
            Assert.IsNotNull(sail);
            Assert.GreaterOrEqual(ProfileManager.Instance.Costumes.Count, 10, "En az 6 kostüm + 4 yelken bekleniyordu");
            string before = costume.AppliedId, sailBefore = sail.AppliedId;
            DevCommands.UnlockAllCostumes();
            DevCommands.WearNextCostume();
            yield return null;
            Assert.AreNotEqual(before, costume.AppliedId, "Kostüm değişmedi");
            Assert.AreNotEqual(sailBefore, sail.AppliedId, "Yelken değişmedi");
            Assert.IsNotNull(Player.transform.Find("Body/CostumeModel"), "Kostüm modeli takılmadı");
            Player.ScriptedInput = new Vector2(0.3f, 1f);
            yield return new WaitForSeconds(0.6f);
            Player.ScriptedInput = Vector2.zero;
            yield return new WaitForSeconds(0.3f);
            Shot("B_kostum");
        }

        // Altın figür: 3 parça → gemiye altın figür takılır (efsanevi gemi).
        [UnityTest]
        public IEnumerator Figure_ThreePartsMakeLegendaryShip()
        {
            yield return null;
            Assert.AreEqual(3, Collectible.CountOfKind(Collectible.Kind.FigurePart), "3 altın figür parçası bekleniyordu");
            foreach (Collectible c in new List<Collectible>(Collectible.All)) if (c.Type == Collectible.Kind.FigurePart) c.Take();
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(QuestManager.Instance.HasLegendaryFigure, "Figür tamamlanmadı");
            Transform figure = ShipBuilder.Instance.Model.Find("GoldenFigurehead");
            Assert.IsNotNull(figure, "Altın figür yok");
            Assert.IsFalse(figure.gameObject.activeSelf, "Gövde yokken figür havada görünüyor");
            ShipBuilder.Instance.ForceCompleteStage();
            ShipBuilder.Instance.ForceCompleteStage();
            ShipBuilder.Instance.ForceCompleteStage();
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(figure.gameObject.activeSelf, "Altın figür gemide görünmüyor");
            Teleport(figure.position + new Vector3(-3f, 0f, -5.5f));
            yield return new WaitForSeconds(1.5f);
            Shot("B_altin_figur");
        }

        // Adacıklar, tapınak ve Demirci: görünüş (bulutlar kalkık).
        [UnityTest]
        public IEnumerator Capture_IsletsAndForge()
        {
            DevCommands.CompleteBridges();
            DevCommands.ToggleClouds();
            AreaManager.Instance.Unlock(PadArea("Pad Forge"));
            yield return new WaitForSeconds(2.5f);
            ProcessingBuilding forge = null;
            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None)) if (b.Type.Output.Id == "nail") forge = b;
            Assert.IsNotNull(forge, "Demirci yok");
            for (int i = 0; i < 6; i++) forge.InputPile.TryAdd(forge.Type.Input.Spawn(forge.transform.position + Vector3.up));
            Teleport(forge.transform.position + new Vector3(0f, 0f, -4.4f));
            yield return new WaitForSeconds(1.6f);
            Assert.IsTrue(forge.IsWorking, "Demirci çalışmıyor");
            Shot("B_demirci");
            Teleport(new Vector3(29f, 0f, -14.5f));
            yield return new WaitForSeconds(1f);
            Shot("B_kayip_tapinak");
            Teleport(new Vector3(-31f, 0f, 3.5f));
            yield return new WaitForSeconds(1f);
            Shot("B_batik_koyu");
        }

        // Bulut: kapalı bölgenin üstünde; bölge açılınca dağılır.
        [UnityTest]
        public IEnumerator Cloud_DispersesWhenRegionOpens()
        {
            RegionCloud rocky = null;
            AreaDefinition area = PadArea("Pad Rocky");
            foreach (RegionCloud c in RegionCloud.All) if (c.Area == area) rocky = c;
            Assert.IsNotNull(rocky, "Kayalık bulutu yok");
            Teleport(new Vector3(-6.5f, 0f, 1.2f));
            yield return new WaitForSeconds(0.6f);
            Shot("B_bulut");
            AreaManager.Instance.Unlock(area);
            yield return new WaitForSeconds(0.9f);
            Assert.IsTrue(rocky.IsRevealing, "Bulut dağılmaya başlamadı");
            Shot("B_bulut_aciliyor");
            yield return WaitFor(() => !rocky.gameObject.activeSelf, 5f);
            Assert.IsFalse(rocky.gameObject.activeSelf, "Bulut kalkmadı");
        }
    }
}
