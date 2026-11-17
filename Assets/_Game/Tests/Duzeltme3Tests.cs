// Duzeltme3Tests.cs
// Oyuncu testi düzeltme turu: koleksiyon eşyası kaybolur, uzun iskele yürünebilir, köprülerde takılma yok,
// yükseltme tabelası paneli, ağacın yeniden büyümesi (kütük → gömülür → fidan → genç → tam), tanıtım kartı,
// bölge kimlikleri ve bina girişlerinin açık kalması, Ambar (bırakma, alma, kapasite, taşıyıcı, kayıt), görev kartının
// küçülmesi, onarım göstergesi, final töreni (kalabalık dağınık, kızakta raylar). Ekran görüntüleri <önek>_D3_*.png.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class Duzeltme3Tests : GameTestBase
    {
        private GameConfig Config => FindAny<DevMenu>().Config;

        private static BridgeBuilder Bridge(string id)
        {
            foreach (ConstructionSite s in ConstructionSite.All) if (s is BridgeBuilder b && s.Id == id) return b;
            return null;
        }

        // Toplanan koleksiyon eşyası yerden hemen kaybolur; profilde "bulundu" kalır.
        [UnityTest]
        public IEnumerator Collection_DisappearsWhenTaken()
        {
            Collectible item = null;
            foreach (Collectible c in Collectible.All) if (c.Id == "collect_bottle") item = c;
            Assert.IsNotNull(item, "Koleksiyon eşyası yok");
            Teleport(item.transform.position + Vector3.back * 0.6f);
            yield return WaitFor(() => item.IsTaken, 2f);
            Assert.IsTrue(item.IsTaken, "Eşya alınmadı");
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(item.gameObject.activeSelf, "Toplanan koleksiyon eşyası yerde kaldı");
            Assert.IsTrue(ProfileManager.Instance.HasCollectible("collect_bottle"), "Eşya profilde bulundu olarak işaretlenmedi");
            Assert.AreEqual(5, Collectible.CountOfKind(Collectible.Kind.Collection), "Koleksiyon sayısı bozuldu");
        }

        // Denize uzanan iskele: oyuncu baştan ucuna kadar yürür; ucunda kaptan dürbünü durur.
        [UnityTest]
        public IEnumerator Pier_WalkableToTheEnd()
        {
            Pier pier = FindAny<Pier>();
            Assert.IsNotNull(pier, "Uzun iskele yok");
            Assert.IsNull(GameObject.Find("Shipyard/Dock"), "Eski iki ayaklı iskele duruyor");
            Vector3 dir = (pier.EndB - pier.EndA).normalized;
            Assert.Greater(Vector3.Distance(pier.EndA, pier.EndB), 9f, "İskele kısa");
            Teleport(pier.EndA - dir * 1.5f);
            Player.ScriptedInput = new Vector2(dir.x, dir.z);
            yield return new WaitForSeconds(4f);
            Player.ScriptedInput = Vector2.zero;
            Vector3 p = Player.transform.position;
            Assert.Greater(Vector3.Dot(p - pier.EndA, dir), Vector3.Distance(pier.EndA, pier.EndB) - 1.5f, "Oyuncu iskelenin ucuna yürüyemedi (" + p + ")");
            Collectible spyglass = null;
            foreach (Collectible c in Collectible.All) if (c.Id == "collect_spyglass") spyglass = c;
            Assert.IsNotNull(spyglass);
            Assert.Less(Vector3.Distance(spyglass.transform.position, pier.EndB), 2f, "Dürbün iskelenin ucunda değil");
            Shot("D3_iskele_ucu");
            yield return WaitFor(() => !CollectionPopup.Instance.IsShown, 6f);
            Teleport(pier.EndA - dir * 4f);
            yield return new WaitForSeconds(1f);
            Shot("D3_uzun_iskele");
        }

        // Köprülerde takılma yok: iki köprüde de iki yöne (hafif çapraz) yürüyünce karşıya geçilir.
        [UnityTest]
        public IEnumerator Bridges_NoStuck()
        {
            DevCommands.CompleteBridges();
            yield return new WaitForSeconds(6f); // açılış çekimleri
            foreach (string id in new[] { "bridge_wreck", "bridge_temple" })
            {
                BridgeBuilder bridge = Bridge(id);
                Assert.IsNotNull(bridge, id);
                Vector3 a = bridge.EndA, b = bridge.EndB, dir = (b - a).normalized, side = Vector3.Cross(Vector3.up, dir);
                float length = Vector3.Distance(a, b);
                for (int pass = 0; pass < 2; pass++)
                {
                    Vector3 from = pass == 0 ? a - dir * 1.2f : b + dir * 1.2f;
                    Vector3 go = (pass == 0 ? dir : -dir) + side * 0.18f;
                    Teleport(from);
                    yield return null;
                    Player.ScriptedInput = new Vector2(go.x, go.z).normalized;
                    yield return new WaitForSeconds(length / 3.2f + 1.5f);
                    Player.ScriptedInput = Vector2.zero;
                    Vector3 p = Player.transform.position;
                    float along = Vector3.Dot(p - a, dir);
                    if (pass == 0) Assert.Greater(along, length, id + ": karşıya geçilemedi (" + p + ")");
                    else Assert.Less(along, 0f, id + ": geri dönülemedi (" + p + ")");
                }
                Teleport(Vector3.Lerp(a, b, 0.5f));
                yield return new WaitForSeconds(0.8f);
                Shot("D3_kopru_" + id);
            }
        }

        // Yükseltme tabelası: binanın yanında; yaklaşınca alttan panel (seviye, hız/kapasite artışı, fiyat, Yükselt).
        [UnityTest]
        public IEnumerator UpgradeSign_OpensPanelAndLevelsUp()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            yield return new WaitForSeconds(0.8f);
            BuildingUpgradeSign sign = null;
            foreach (BuildingUpgradeSign s in Object.FindObjectsByType<BuildingUpgradeSign>(FindObjectsSortMode.None))
                if (s.Building != null && s.Building.Type.Output.Id == "plank") sign = s;
            Assert.IsNotNull(sign, "Bıçkıhane'nin Yükselt tabelası yok");
            ProcessingBuilding sawmill = sign.Building;
            // Tabela girişlerin önünde değil (binanın yanında).
            Assert.Greater(Vector3.Distance(sign.transform.position, sawmill.InputPile.transform.position), 2f, "Tabela girişin önünde");
            BuildingUpgradePanel panel = BuildingUpgradePanel.Instance;
            Assert.IsNotNull(panel, "Bina yükseltme paneli yok");
            Assert.IsFalse(panel.IsOpen);

            Teleport(sign.transform.position + Vector3.back * 0.8f);
            yield return WaitFor(() => panel.IsOpen, 2f);
            Assert.IsTrue(panel.IsOpen, "Tabelaya gelince panel açılmadı");
            Assert.IsFalse(PlayerController.InputBlocked, "Panel yürümeyi engellememeli");
            StringAssert.Contains("→", panel.SpeedLine, "Hız artışı yazmıyor");
            StringAssert.Contains("→", panel.CapacityLine, "Kapasite artışı yazmıyor");
            yield return new WaitForSeconds(0.4f);
            Shot("D3_yukseltme_paneli");

            CurrencyManager.Instance.Add(5000);
            float speed = sawmill.SpeedMultiplier;
            panel.Buy();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(2, sawmill.Level, "Yükselt butonu seviye atlatmadı");
            Assert.Greater(sawmill.SpeedMultiplier, speed);
            panel.Buy();
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(3, sawmill.Level);
            Assert.IsNull(sign.NextLevelArea, "En son seviyede hâlâ yükseltme var");
            Shot("D3_yukseltme_maks");

            Teleport(sign.transform.position + Vector3.back * 6f);
            yield return WaitFor(() => !panel.IsOpen, 2f);
            Assert.IsFalse(panel.IsOpen, "Uzaklaşınca panel kapanmadı");
        }

        // Ağaç: kütük bir süre kalır, gömülür; aynı yerde fidan çıkar, aşama aşama büyür.
        [UnityTest]
        public IEnumerator Tree_RegrowsFromSapling()
        {
            ResourceNode palm = null;
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                if (node.name.StartsWith("Node_Palm")) { palm = node; break; }
            Assert.IsNotNull(palm);
            Assert.AreEqual(ResourceNode.RegrowStyle.Sapling, palm.Style, "Palmiye fidan biçiminde büyümüyor");
            Transform visual = palm.transform.Find("Visual"), remnant = palm.transform.Find("Remnant");
            float full = visual.localScale.y;

            Teleport(palm.GetStandPoint(palm.transform.position + Vector3.back * 3f));
            float end = Time.time + 25f;
            while (!palm.IsDepleted && Time.time < end)
            {
                while (!PlayerStack.IsEmpty)
                {
                    ResourceItem item = PlayerStack.RemoveTop();
                    if (item != null) Object.Destroy(item.gameObject);
                }
                yield return null;
            }
            Assert.IsTrue(palm.IsDepleted, "Palmiye tükenmedi");
            Teleport(palm.transform.position + new Vector3(-1.5f, 0f, -3.5f));
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(remnant.gameObject.activeSelf, "Kütük kalmadı");
            Assert.AreEqual(-1, palm.GrowStage, "Kütük evresinde fidan çıktı");

            Time.timeScale = 3f;
            yield return WaitFor(() => palm.GrowStage == 0, 20f);
            Assert.AreEqual(0, palm.GrowStage, "Fidan çıkmadı");
            yield return new WaitForSeconds(0.6f);
            Assert.IsFalse(remnant.gameObject.activeSelf, "Kütük gömülmedi (fidan kütükten çıkıyor)");
            Assert.Greater(visual.localScale.y, 0.02f, "Fidan görünmüyor");
            Assert.Less(visual.localScale.y, full * 0.5f, "Fidan çok büyük");
            Time.timeScale = 1f;
            Shot("D3_fidan");
            Time.timeScale = 3f;
            yield return WaitFor(() => palm.GrowStage == 1, 20f);
            yield return new WaitForSeconds(0.5f);
            float young = visual.localScale.y;
            Assert.Greater(young, full * 0.45f, "Genç ağaç evresi yok");
            Time.timeScale = 1f;
            Shot("D3_genc_agac");
            Time.timeScale = 3f;
            yield return WaitFor(() => !palm.IsDepleted, 20f);
            Time.timeScale = 1f;
            yield return new WaitForSeconds(1f);
            Assert.Greater(visual.localScale.y, full * 0.95f, "Ağaç tam boyuna dönmedi");
        }

        // Tanıtım tabelası: kapalı bölgenin girişinde; yaklaşınca kart açılır; bölge açılınca açılmaz.
        [UnityTest]
        public IEnumerator InfoSign_CardOpensNearby()
        {
            AreaDefinition rocky = PadArea("Pad Rocky");
            InfoSign sign = InfoSign.For(rocky);
            Assert.IsNotNull(sign, "Kayalık tanıtım tabelası yok");
            Assert.GreaterOrEqual(InfoSign.All.Count, 5, "Bölge ve köprü tabelaları eksik");
            Assert.IsFalse(sign.IsCardShown);
            Teleport(sign.transform.position + Vector3.back * 1.6f);
            yield return WaitFor(() => sign.IsCardShown, 2f);
            Assert.IsTrue(sign.IsCardShown, "Yaklaşınca tanıtım kartı açılmadı");
            yield return new WaitForSeconds(0.5f);
            Shot("D3_tanitim_karti");
            Teleport(sign.transform.position + Vector3.back * 7f);
            yield return WaitFor(() => !sign.IsCardShown, 2f);
            Assert.IsFalse(sign.IsCardShown, "Uzaklaşınca kart kapanmadı");

            // Köprü tabelası.
            InfoSign bridgeSign = InfoSign.For(Bridge("bridge_wreck").IslandArea);
            Assert.IsNotNull(bridgeSign, "Batık Koyu köprü tabelası yok");
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            Teleport(bridgeSign.transform.position + Vector3.back * 1.4f);
            yield return WaitFor(() => bridgeSign.IsCardShown, 2f);
            Assert.IsTrue(bridgeSign.IsCardShown, "Köprü tanıtım kartı açılmadı");
            yield return new WaitForSeconds(2.8f);
            Shot("D3_kopru_tanitim");

            AreaManager.Instance.Unlock(rocky);
            Teleport(sign.transform.position + Vector3.back * 1.6f);
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(sign.IsCardShown, "Açılmış bölgenin kartı açılıyor");
        }

        // Bölge kimlikleri ve yerleşim: taş ocağı, bahçe kapısı/kuyu, kurutma rafları; Halat Atölyesi ile Dokuma Tezgahı
        // ayrık; hiçbir bina girişi/çıkışı bir engelle kapanmıyor.
        [UnityTest]
        public IEnumerator Regions_IdentityAndClearEntrances()
        {
            Assert.IsNotNull(GameObject.Find("Area Rocky/Quarry/Rails/MineCart"), "Taş ocağında maden arabası yok");
            Assert.IsNotNull(GameObject.Find("Area Rocky/Quarry/PickaxeRack"), "Kazma rafı yok");
            Assert.IsNotNull(GameObject.Find("Area Orchard/Arch"), "Bahçenin kemerli kapısı yok");
            Assert.IsNotNull(GameObject.Find("Area Orchard/OrchardProps/WellRing"), "Bahçe kuyusu yok");
            Assert.IsNotNull(GameObject.Find("Area Cove/CoveProps/DryingRack"), "Kurutma rafı yok");
            Assert.IsNull(GameObject.Find("Area Rocky/Fence"), "Kayalıkta yüksek çit kalmış");

            DevCommands.UnlockAllRegions();
            DevCommands.UnlockAllBuildings();
            yield return new WaitForSeconds(3.5f);
            ProcessingBuilding rope = null, loom = null;
            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None))
            {
                if (b.Type.Output.Id == "rope") rope = b;
                if (b.Type.Output.Id == "cloth") loom = b;
            }
            Assert.IsNotNull(rope);
            Assert.IsNotNull(loom);
            Assert.Greater(Vector3.Distance(rope.transform.position, loom.transform.position), 9f, "Halat Atölyesi ile Dokuma Tezgahı hâlâ yan yana");

            foreach (BuildingInput input in Object.FindObjectsByType<BuildingInput>(FindObjectsSortMode.None)) AssertClear(input.transform);
            foreach (BuildingOutput output in Object.FindObjectsByType<BuildingOutput>(FindObjectsSortMode.None)) AssertClear(output.transform);

            Teleport(new Vector3(13f, 0f, 2.5f));
            yield return new WaitForSeconds(1f);
            Shot("D3_palmiye_koyu");
            Teleport(new Vector3(5.25f, 0f, 5.6f));
            yield return new WaitForSeconds(1f);
            Shot("D3_meyve_bahcesi");
        }

        private static void AssertClear(Transform slot)
        {
            Vector2 p = new Vector2(slot.position.x, slot.position.z);
            foreach (NavObstacle o in NavObstacle.Active)
                Assert.IsFalse(o.Contains(p, 0.45f), slot.parent.name + "/" + slot.name + " bir engelle kapanıyor: " + o.name);
        }
            private static ResourceType Resource(string id)
        {
            foreach (ResourceType t in Resources.FindObjectsOfTypeAll<ResourceType>()) if (t.Id == id) return t;
            return null;
        }

        // Ambar: bırakma alanında sırttakiler ambara akar; alma alanında panel açılır, seçilen kaynaktan alınır.
        [UnityTest]
        public IEnumerator Storage_DropAndPick()
        {
            Storage storage = Storage.Instance;
            Assert.IsNotNull(storage, "Ambar yok");
            Assert.IsFalse(storage.IsOpen, "Ambar başta açık olmamalı");
            Assert.IsNotNull(Find<UnlockPad>("Pad Storage"), "Ambarın açma karesi yok");
            AreaManager.Instance.Unlock(PadArea("Pad Rocky"));
            AreaManager.Instance.Unlock(storage.Area);
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(storage.IsOpen);
            StorageDropZone drop = FindAny<StorageDropZone>();
            StoragePickZone pick = FindAny<StoragePickZone>();
            Assert.IsNotNull(drop);
            Assert.IsNotNull(pick);

            ResourceType plank = Resource("plank");
            DevCommands.FillPlayerStack(plank);
            int carried = PlayerStack.Count;
            Assert.Greater(carried, 3);
            Teleport(drop.transform.position);
            yield return WaitFor(() => PlayerStack.IsEmpty, 6f);
            Assert.IsTrue(PlayerStack.IsEmpty, "Sırttakiler ambara akmadı");
            Assert.AreEqual(carried, storage.CountOf(plank), "Ambara giden kalas sayısı yanlış");
            Shot("D3_ambar_birakma");

            Teleport(pick.transform.position);
            StoragePanel panel = StoragePanel.Instance;
            yield return WaitFor(() => panel.IsOpen, 2f);
            Assert.IsTrue(panel.IsOpen, "Alma alanında Ambar paneli açılmadı");
            Assert.AreEqual(plank, panel.Selected, "Ambardaki kaynak seçili gelmedi");
            yield return new WaitForSeconds(0.4f);
            Shot("D3_ambar_paneli");
            panel.Take();
            yield return WaitFor(() => PlayerStack.Count >= carried, 6f);
            Assert.AreEqual(carried, PlayerStack.Count, "\"Al\" ile ambardan alınamadı");
            Assert.AreEqual(0, storage.CountOf(plank));
            Teleport(pick.transform.position + Vector3.back * 6f);
            yield return WaitFor(() => !panel.IsOpen, 2f);
            Assert.IsFalse(panel.IsOpen, "Uzaklaşınca Ambar paneli kapanmadı");
        }

        // Ambar kapasitesi: dolunca bırakma durur (sırttakiler kalır); Yükselt tabelası kapasiteyi artırır.
        [UnityTest]
        public IEnumerator Storage_CapacityAndUpgrade()
        {
            Storage storage = Storage.Instance;
            AreaManager.Instance.Unlock(storage.Area);
            yield return new WaitForSeconds(0.8f);
            int cap = storage.Capacity;
            Assert.AreEqual(Config.StorageCapacity[0], cap);
            storage.Add(Resource("stone"), cap + 50);
            Assert.AreEqual(cap, storage.Total, "Kapasite aşıldı");
            Assert.IsTrue(storage.IsFull);
            DevCommands.FillPlayerStack(Resource("wood"));
            int carried = PlayerStack.Count;
            Teleport(FindAny<StorageDropZone>().transform.position);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(carried, PlayerStack.Count, "Ambar doluyken bırakma durmadı");
            Assert.AreEqual(cap, storage.Total);
            Shot("D3_ambar_dolu");

            BuildingUpgradeSign sign = null;
            foreach (BuildingUpgradeSign s in Object.FindObjectsByType<BuildingUpgradeSign>(FindObjectsSortMode.None)) if (s.Storage == storage) sign = s;
            Assert.IsNotNull(sign, "Ambarın Yükselt tabelası yok");
            CurrencyManager.Instance.Add(5000);
            Assert.IsTrue(sign.TryUpgrade(), "Ambar yükseltilemedi");
            Assert.AreEqual(Config.StorageCapacity[1], storage.Capacity, "Ambar kapasitesi artmadı");
            yield return WaitFor(() => storage.Total > cap, 4f);
            Assert.Greater(storage.Total, cap, "Kapasite artınca bırakma devam etmedi");
        }

        // Taşıyıcı işçi ambardan gemi teslim alanına ihtiyaç olan malzemeyi taşır.
        [UnityTest]
        public IEnumerator Storage_CarrierDeliversToShip()
        {
            Storage storage = Storage.Instance;
            AreaManager.Instance.Unlock(storage.Area);
            ResourceType plank = Resource("plank");
            Assert.IsTrue(ShipBuilder.Instance.Needs(plank), "Gemi ilk aşamada kalas istemeli");
            storage.Add(plank, 12);
            int before = ShipBuilder.Instance.Delivered(plank);
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill")); // Taşıyıcı Bıçkıhane'den sonra tutulur
            Hire("carrier");
            CarrierWorker carrier = null;
            yield return WaitFor(() => (carrier = Object.FindAnyObjectByType<CarrierWorker>()) != null, 8f); // sadece sahnedeki (aktif)
            Assert.IsNotNull(carrier, "Taşıyıcı çıkmadı");
            yield return WaitFor(() => carrier.IsStorageRun, 15f);
            Assert.IsTrue(carrier.IsStorageRun, "Taşıyıcı ambara gitmedi");
            Time.timeScale = 3f;
            yield return WaitFor(() => ShipBuilder.Instance.Delivered(plank) > before, 30f);
            Time.timeScale = 1f;
            Assert.Greater(ShipBuilder.Instance.Delivered(plank), before, "Taşıyıcı ambardan gemiye kalas götürmedi");
            Assert.Less(storage.CountOf(plank), 12, "Ambardaki kalas azalmadı");
            Shot("D3_tasiyici_ambar");
        }

        // Ambar içeriği kayda yazılır; ada sıfırlanınca (kaçış) boşalır.
        [UnityTest]
        public IEnumerator Storage_SavedAndClearedOnReset()
        {
            yield return null;
            Storage storage = Storage.Instance;
            AreaManager.Instance.Unlock(storage.Area);
            storage.Add(Resource("rope"), 7);
            SaveData data = new SaveData();
            storage.Collect(data.Storage);
            Assert.AreEqual(1, data.Storage.Count);
            Assert.AreEqual(7, data.Storage[0].Value);
            storage.Clear();
            storage.Restore(data.Storage);
            Assert.AreEqual(7, storage.CountOf(Resource("rope")), "Ambar kayıttan geri yüklenmedi");
            data.ResetRun();
            Assert.AreEqual(0, data.Storage.Count, "Kaçışta ambar boşalmadı");
        }

        // Görev kartı: büyük yazı + ikon; dokununca tek satıra iner, tekrar dokununca açılır.
        [UnityTest]
        public IEnumerator QuestCard_CollapsesOnTap()
        {
            yield return new WaitForSeconds(0.6f);
            QuestCard card = null;
            foreach (QuestCard c in Object.FindObjectsByType<QuestCard>(FindObjectsSortMode.None)) if (c.Quest != null) { card = c; break; }
            Assert.IsNotNull(card, "Görev kartı yok");
            float tall = ((RectTransform)card.transform).rect.height;
            card.Toggle();
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(card.IsCollapsed);
            Assert.Less(((RectTransform)card.transform).rect.height, tall * 0.7f, "Kart küçülmedi");
            StringAssert.Contains("/", card.TitleText, "Tek satırda ilerleme yok");
            Shot("D3_gorev_karti_kucuk");
            card.Toggle();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(card.IsCollapsed);
            Shot("D3_gorev_karti");
        }

        // Onarım/inşa göstergeleri gemi göstergesi gibi: kutusuz ikon + sayı + ince çubuk (eski "Kuyu: 0/14" yazısı yok).
        [UnityTest]
        public IEnumerator Sites_ShowIconCountBars()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            yield return new WaitForSeconds(0.5f);
            foreach (ConstructionSite site in ConstructionSite.All)
            {
                Assert.IsNotNull(site.ProgressView, site.name + " kutusuz göstergeye geçmemiş");
            }
            ConstructionSite well = null;
            foreach (ConstructionSite site in ConstructionSite.All) if (site.Id == "well") well = site;
            Teleport(well.transform.position + Vector3.back * 3f);
            yield return new WaitForSeconds(0.8f);
            Shot("D3_kuyu_gostergesi");
        }

        // Final: kalabalık sahil boyunca dağılır (tek noktada yığılmaz), farklı yönlere bakar; gemi iki rayın üstünde.
        [UnityTest]
        public IEnumerator Ceremony_CrowdSpreadAndRails()
        {
            Assert.IsNotNull(GameObject.Find("Shipyard/Slipway"), "Kızak yok");
            Assert.AreEqual(2, CountRails(), "Kızakta iki ray olmalı");
            DevCommands.HireAllWorkers();
            yield return new WaitForSeconds(2f);
            DevCommands.SkipToCeremony();
            yield return WaitFor(() => LaunchCeremony.Instance.IsRunning, 5f);
            yield return new WaitForSeconds(1.6f);
            Shot("D3_final_genis");
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            int count = 0;
            foreach (CharacterVisual v in Object.FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None))
            {
                Vector3 p = v.transform.position;
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
                count++;
            }
            Assert.Greater(count, 3);
            Assert.Greater(Vector3.Distance(min, max), 8f, "Kalabalık tek noktaya yığıldı");
            yield return WaitFor(() => LaunchCeremony.Instance.Phase == "uzaklasma", 20f);
            yield return new WaitForSeconds(2.5f);
            Shot("D3_final_adadan_takip");
        }

        private static int CountRails()
        {
            int n = 0;
            foreach (Transform t in GameObject.Find("Shipyard/Slipway").transform) if (t.name == "Rail") n++;
            return n;
        }
    }
}
