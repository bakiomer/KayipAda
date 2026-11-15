// ProgressionTests.cs
// İlerleme testleri: geliştirme satın alma, lif → halat/yelken bezi zinciri, gemi aşamaları ve kilitleri,
// suya indirme töreni ve kaçış ekranı, hızlı test modu.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class ProgressionTests : GameTestBase
    {
        [UnityTest]
        public IEnumerator Upgrade_BuyingCapacityIncreasesStack()
        {
            UpgradeManager upgrades = UpgradeManager.Instance;
            Assert.IsNotNull(upgrades, "Geliştirme yöneticisi yok");
            UpgradeType capacity = null;
            foreach (UpgradeType t in upgrades.Upgrades) if (t.Stat == UpgradeStat.PlayerCapacity) capacity = t;
            Assert.IsNotNull(capacity, "Kapasite geliştirmesi yok");

            yield return null;
            int before = PlayerStack.Capacity;
            CurrencyManager.Instance.Add(upgrades.NextPrice(capacity));
            Assert.IsTrue(upgrades.TryBuy(capacity), "Satın alınamadı");
            yield return null;
            Assert.Greater(PlayerStack.Capacity, before, "Kapasite artmadı");

            // Maksimuma kadar al: MAKS'ta satın alma reddedilmeli.
            for (int i = 1; i < capacity.MaxLevel; i++)
            {
                CurrencyManager.Instance.Add(upgrades.NextPrice(capacity));
                upgrades.TryBuy(capacity);
            }
            Assert.IsTrue(upgrades.IsMaxed(capacity));
            Assert.IsFalse(upgrades.TryBuy(capacity), "Maksimumdan sonra satın alınabildi");
        }

        [UnityTest]
        public IEnumerator UpgradeStation_OpensPanelAndBlocksMovement()
        {
            UpgradeStation station = FindAny<UpgradeStation>();
            Assert.IsNotNull(station, "Geliştirme tezgahı yok");
            Teleport(station.transform.position);
            yield return WaitFor(() => PlayerController.InputBlocked, 3f);
            Assert.IsTrue(PlayerController.InputBlocked, "Panel açılınca oyuncu durmadı");
            yield return new WaitForSeconds(0.5f);
            Shot("B_gelistirme_paneli");
            FindAny<UpgradePanel>().Close();
            Assert.IsFalse(PlayerController.InputBlocked);
        }

        [UnityTest]
        public IEnumerator Ship_EachStageUnlocksNextTargetAndCelebrates()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            Assert.IsNotNull(ship, "Gemi yok");
            Assert.AreEqual(5, ship.Config.Stages.Length, "Gemi 5 aşamalı olmalı");
            Teleport(ship.Delivery.transform.position + Vector3.back * 2.5f);
            yield return new WaitForSeconds(1f);
            Shot("C_gemi_asama_0");

            // Omurganın yarısı: parçaların bir kısmı görünür (tek tek gelir).
            ShipConfig.Requirement keelReq = ship.Config.Stages[0].Requirements[0];
            int half = Mathf.Max(1, ship.Config.Stages[0].Requirements[0].Amount > 0 ? Mathf.CeilToInt(QuickAmount(keelReq) * 0.5f) : 1);
            for (int k = 0; k < half; k++) ship.Deliver(keelReq.Resource.Spawn(ship.Delivery.transform.position + Vector3.up));
            yield return new WaitForSeconds(1f);
            Assert.Greater(ship.ShownPieces(0), 0, "Omurga parçası gelmedi");
            Assert.Less(ship.ShownPieces(0), ship.PieceCount(0), "Omurganın yarısında tüm parçalar gelmiş olmamalı");
            Shot("C_gemi_omurga_yarim");

            for (int i = 0; i < ship.Config.Stages.Length; i++)
            {
                ShipConfig.Stage stage = ship.Config.Stages[i];
                yield return CompleteStage(ship);
                Assert.AreEqual(i + 1, ship.StageIndex, stage.Name + " bitince aşama ilerlemedi");
                if (i != 1) Assert.Greater(ship.PieceCount(i), 0, stage.Name + " aşamasının görünen parçası yok");
                Assert.AreEqual(ship.PieceCount(i), ship.ShownPieces(i), stage.Name + " bitince parçaların hepsi görünmedi");
                if (stage.UnlockOnComplete != null)
                {
                    Assert.IsTrue(AreaManager.Instance.IsUnlocked(stage.UnlockOnComplete), stage.Name + " bitince sonraki hedef açılmadı");
                    yield return null;
                    bool padShown = false;
                    foreach (UnlockPad pad in Object.FindObjectsByType<UnlockPad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (pad.RequiredArea == stage.UnlockOnComplete && pad.IsAvailable) padShown = true;
                    }
                    Assert.IsTrue(padShown, stage.Name + " sonrası yeni hedefin karesi görünmüyor");
                }
                if (i < ship.Config.Stages.Length - 1)
                {
                    Assert.IsTrue(CelebrationBanner.Instance != null && CelebrationBanner.Instance.IsShown, "Aşama kutlaması görünmedi");
                    yield return new WaitForSeconds(1.6f);
                    Shot("C_gemi_asama_" + (i + 1));
                }
            }
            Assert.IsTrue(ship.IsComplete);
        }

        [UnityTest]
        public IEnumerator FiberChain_RopeAndClothAreProduced()
        {
            AreaDefinition cove = PadArea("Pad Cove");
            AreaManager.Instance.Unlock(cove);
            AreaManager.Instance.Unlock(PadArea("Pad RopeWorkshop"));
            AreaManager.Instance.Unlock(PadArea("Pad Loom"));
            yield return new WaitForSeconds(1f);

            // Oyuncu koydaki keten tarlasından lif toplar.
            ResourceNode flax = null;
            foreach (ResourceNode n in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (n.name.StartsWith("Node_FlaxField")) flax = n;
            }
            Assert.IsNotNull(flax, "Keten tarlası yok (Palmiye Koyu açılmadı mı?)");
            Teleport(flax.GetStandPoint(flax.transform.position + Vector3.back * 3f));
            yield return WaitFor(() => PlayerStack.Count >= 4, 6f);
            Assert.GreaterOrEqual(PlayerStack.Count, 4, "Keten tarlasından lif toplanmadı");
            Shot("B_lif_topluyor");

            ProcessingBuilding rope = null, cloth = null;
            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None))
            {
                if (b.Type.Output.Id == "rope") rope = b;
                if (b.Type.Output.Id == "cloth") cloth = b;
            }
            Assert.IsNotNull(rope, "Halat Atölyesi yok");
            Assert.IsNotNull(cloth, "Dokuma Tezgahı yok");
            for (int i = 0; i < rope.Type.InputAmount * 2; i++) rope.InputPile.TryAdd(rope.Type.Input.Spawn(rope.transform.position + Vector3.up));
            for (int i = 0; i < cloth.Type.InputAmount * 2; i++) cloth.InputPile.TryAdd(cloth.Type.Input.Spawn(cloth.transform.position + Vector3.up));
            yield return WaitFor(() => rope.OutputPile.Count >= 1 && cloth.OutputPile.Count >= 1,
                                 Mathf.Max(rope.Type.ProcessTime, cloth.Type.ProcessTime) * 2f + 3f);
            Assert.GreaterOrEqual(rope.OutputPile.Count, 1, "Halat üretilmedi");
            Assert.GreaterOrEqual(cloth.OutputPile.Count, 1, "Yelken bezi üretilmedi");
            Teleport(rope.transform.position + new Vector3(-1.5f, 0f, -3.5f));
            yield return new WaitForSeconds(1.5f);
            Shot("B_halat_dokuma");
        }

        // Geminin şu anki aşamasını, gereken malzemeleri teslim ederek bitirir.
        private static int QuickAmount(ShipConfig.Requirement req)
        {
            GameConfig config = Object.FindAnyObjectByType<CameraFollow>().Config;
            return config.ScaleAmount(req.Amount);
        }

        private static IEnumerator CompleteStage(ShipBuilder ship)
        {
            ShipConfig.Stage stage = ship.Config.Stages[ship.StageIndex];
            foreach (ShipConfig.Requirement req in stage.Requirements)
            {
                while (ship.Needs(req.Resource))
                {
                    ship.Deliver(req.Resource.Spawn(ship.Delivery.transform.position + Vector3.up));
                }
            }
            yield return null;
        }

        private static IEnumerator FinishShip(ShipBuilder ship)
        {
            while (!ship.IsComplete) yield return CompleteStage(ship);
        }

        [UnityTest]
        public IEnumerator LaunchCeremony_ShipSlidesIntoSeaAndEscapeAppears()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            LaunchCeremony ceremony = LaunchCeremony.Instance;
            EscapePanel escape = FindAny<EscapePanel>();
            Assert.IsNotNull(ceremony, "Tören yok");
            Assert.IsNotNull(escape, "Kaçış ekranı yok");
            AreaManager.Instance.Unlock(PadArea("Pad Lumberjack"));
            yield return new WaitForSeconds(1f);

            Vector3 shipStart = ship.Model.position;
            yield return FinishShip(ship);
            Assert.IsTrue(ceremony.IsRunning, "Gemi bitince tören başlamadı");
            ShipConfig c = ship.Config;
            yield return new WaitForSeconds(c.GatherTime * 0.6f);
            Shot("C_toren_toplanma");
            yield return WaitFor(() => ceremony.Phase == "kamera", c.GatherTime + 2f);
            yield return new WaitForSeconds(c.OrbitDuration * 0.5f);
            Shot("C_toren_kamera");
            yield return WaitFor(() => ceremony.Phase == "kayma", c.OrbitDuration + 2f);
            yield return new WaitForSeconds(c.SlideDuration * 0.8f);
            Shot("C_toren_suya_iniyor");
            yield return WaitFor(() => ceremony.Phase == "uzaklasma", c.SlideDuration + 2f);
            yield return new WaitForSeconds(c.TurnDuration + 1.5f);
            Shot("C_toren_havai_fisek");
            yield return WaitFor(() => escape.IsShown, c.TurnDuration + c.SailDuration + 5f);
            Assert.IsTrue(escape.IsShown, "Kaçış ekranı açılmadı");
            Assert.Greater(Vector3.Distance(ship.Model.position, shipStart), c.SlideDistance * 0.9f, "Gemi suya kaymadı");
            Assert.IsFalse(IslandShape.Instance.Contains(new Vector2(ship.Model.position.x, ship.Model.position.z), 0f), "Gemi denize açılmadı");
            Assert.Less(ship.Model.position.y, shipStart.y, "Gemi suya inmedi");
            yield return new WaitForSeconds(3f); // yıldızlar ve sayaç dolsun
            Shot("C_kacis_ekrani");
            Assert.GreaterOrEqual(escape.StarCount, 1, "Yıldız yok");
        }

        [UnityTest]
        public IEnumerator QuickTestMode_LowersPricesAndAmounts()
        {
            GameConfig config = (GameConfig)typeof(AreaManager)
                .GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(AreaManager.Instance);
            bool original = config.QuickTestMode; // kullanıcı modu açık bırakmış olabilir
            config.EditorSetQuickTest(false);
            AreaDefinition rocky = PadArea("Pad Rocky");
            int normalPrice = AreaManager.Instance.PriceOf(rocky);
            ShipConfig.Requirement keel = ShipBuilder.Instance.Config.Stages[0].Requirements[0];
            try
            {
                config.EditorSetQuickTest(true);
                Assert.Less(AreaManager.Instance.PriceOf(rocky), normalPrice, "Hızlı test modu fiyatı düşürmedi");
                Assert.Less(config.ScaleAmount(keel.Amount), keel.Amount, "Hızlı test modu malzemeyi azaltmadı");
            }
            finally
            {
                config.EditorSetQuickTest(false);
            }
            Assert.AreEqual(normalPrice, AreaManager.Instance.PriceOf(rocky), "Kapatınca normal fiyata dönmedi");
            config.EditorSetQuickTest(original);
            yield return null;
        }
    }
}
