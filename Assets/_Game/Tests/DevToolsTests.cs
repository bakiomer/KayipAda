// DevToolsTests.cs
// Test Menüsü komutlarını ve kaynak noktalarının küçülüp kütük bırakıp yeniden büyümesini dener.
// Test Menüsü ile gemi aşamaları, final töreni ve "KAÇTIN!" ekranının ekran görüntülerini alır (D_*).

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class DevToolsTests : GameTestBase
    {
        [UnityTest]
        public IEnumerator DevMenu_CommandsWorkThroughWholeGame()
        {
            DevMenu menu = DevMenu.Instance;
            Assert.IsNotNull(menu, "Test Menüsü yok");
            Assert.IsTrue(DevCommands.Available, "Editor'da Test Menüsü açık olmalı");
            GameConfig config = menu.Config;

            menu.Open();
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(menu.IsOpen);
            Assert.IsTrue(PlayerController.InputBlocked, "Menü açıkken oyuncu yürümemeli");
            Shot("D_test_menusu");
            menu.Close();
            Assert.IsFalse(PlayerController.InputBlocked);

            int before = CurrencyManager.Instance.Coins;
            DevCommands.AddMoney(config);
            Assert.AreEqual(before + config.DevMoneyAmount, CurrencyManager.Instance.Coins, "+Para çalışmadı");

            DevCommands.SetInfiniteMoney(config, true);
            int shown = CurrencyManager.Instance.Coins;
            Assert.IsTrue(CurrencyManager.Instance.TrySpend(500));
            Assert.AreEqual(shown, CurrencyManager.Instance.Coins, "Sınırsız parada para azalmamalı");
            DevCommands.SetInfiniteMoney(config, false);
            Assert.AreEqual(before + config.DevMoneyAmount, CurrencyManager.Instance.Coins, "Sınırsız para kapanınca eski para dönmeli");

            DevCommands.UnlockAllRegions();
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(PadArea("Pad Rocky")), "Kayalık açılmadı");
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(PadArea("Pad Orchard")), "Meyve Bahçesi açılmadı");
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(PadArea("Pad Cove")), "Palmiye Koyu açılmadı");
            DevCommands.UnlockAllBuildings();
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(PadArea("Pad Loom")), "Dokuma Tezgahı açılmadı");
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(PadArea("Pad RopeWorkshop")), "Halat Atölyesi açılmadı");
            DevCommands.HireAllWorkers();
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(PadArea("Pad FiberWorker")), "Lifçi çıkmadı");
            yield return new WaitForSeconds(3f);
            Assert.GreaterOrEqual(Object.FindObjectsByType<Worker>(FindObjectsSortMode.None).Length, 3, "İşçiler çıkmadı");

            ShipBuilder ship = ShipBuilder.Instance;
            ResourceType rope = ship.Config.Stages[2].Requirements[1].Resource;
            DevCommands.FillPlayerStack(rope);
            yield return null;
            Assert.IsTrue(PlayerStack.IsFull && PlayerStack.CountOf(rope) == PlayerStack.Count, "Yığın seçilen malzemeyle dolmadı");

            DevCommands.SetSpeed(config.DevSpeeds[config.DevSpeeds.Length - 1]);
            Assert.AreEqual(config.DevSpeeds[config.DevSpeeds.Length - 1], Time.timeScale);
            DevCommands.SetSpeed(config.DevSpeeds[0]);

            // Gemi aşamaları tek tek (her birinde ekran görüntüsü).
            Teleport(ship.Delivery.transform.position + Vector3.back * 2.5f);
            yield return new WaitForSeconds(1f);
            Shot("D_gemi_0");
            for (int i = 0; i < ship.Config.Stages.Length - 1; i++)
            {
                DevCommands.CompleteShipStage();
                Assert.AreEqual(i + 1, ship.StageIndex, "Aşama tamamlanmadı");
                Assert.AreEqual(ship.PieceCount(i), ship.ShownPieces(i), "Aşama parçaları görünmedi");
                yield return new WaitForSeconds(1.8f);
                Shot("D_gemi_" + (i + 1));
            }

            DevCommands.SkipToCeremony();
            Assert.IsTrue(ship.IsComplete, "Gemi bitmedi");
            Assert.IsTrue(LaunchCeremony.Instance.IsRunning, "Tören başlamadı");
            LaunchCeremony ceremony = LaunchCeremony.Instance;
            ShipConfig c = ship.Config;
            yield return WaitFor(() => ceremony.Phase == "kayma", c.GatherTime + c.OrbitDuration + 3f);
            yield return new WaitForSeconds(c.SlideDuration * 0.8f);
            Shot("D_toren");
            EscapePanel escape = FindAny<EscapePanel>();
            yield return WaitFor(() => escape.IsShown, c.SlideDuration + c.TurnDuration + c.SailDuration + 5f);
            Assert.IsTrue(escape.IsShown, "KAÇTIN! ekranı açılmadı");
            yield return new WaitForSeconds(3f);
            Shot("D_kacis");
        }

        [UnityTest]
        public IEnumerator ResourceNode_ShrinksLeavesStumpAndRegrows()
        {
            ResourceNode palm = null;
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (node.name.StartsWith("Node_Palm")) { palm = node; break; }
            }
            Assert.IsNotNull(palm, "Palmiye yok");
            Transform visual = palm.transform.Find("Visual");
            Transform remnant = palm.transform.Find("Remnant");
            Assert.IsNotNull(remnant, "Palmiyenin kütüğü yok");
            float fullScale = visual.localScale.y;

            Teleport(palm.GetStandPoint(palm.transform.position + Vector3.back * 3f));
            int startStock = palm.Stock;
            yield return WaitFor(() => palm.Stock <= startStock / 2, 10f);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(visual.localScale.y, fullScale * 0.95f, "Toplandıkça palmiye küçülmedi");
            Shot("D_palmiye_kuculuyor");

            // Yığın dolunca boşalt; palmiye tükenene kadar topla.
            float end = Time.time + 20f;
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
            Teleport(palm.transform.position + Vector3.back * 3.5f + Vector3.left * 1.5f);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(remnant.gameObject.activeSelf, "Tükenince kütük kalmadı");
            Shot("D_kutuk");

            Time.timeScale = 4f;
            yield return WaitFor(() => !palm.IsDepleted, 30f);
            Time.timeScale = 1f;
            Assert.IsFalse(palm.IsDepleted, "Palmiye yeniden dolmadı");
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(remnant.gameObject.activeSelf, "Palmiye büyüyünce kütük kaybolmadı");
            Assert.Greater(visual.localScale.y, fullScale * 0.95f, "Palmiye eski boyuna dönmedi");
        }

        // Kapalı bölgenin çitinden ve kapısından geçilemez; bölge açılınca kapı açılır ve içeri girilir.
        [UnityTest]
        public IEnumerator Collision_ClosedAreaFenceBlocksUntilUnlocked()
        {
            Rect rocky = new Rect(-11f, 6f, 9f, 9f); // MapLayout.RockyRect (kapı güney kenarın ortasında, x = -6.5)
            Vector2 north = new Vector2(0f, 1f);

            // Çitin düz kısmına doğru yürü: geçemez.
            Teleport(new Vector3(-3.5f, 0f, rocky.yMin - 1.2f));
            Player.ScriptedInput = north;
            yield return new WaitForSeconds(1.5f);
            Assert.Less(Player.transform.position.z, rocky.yMin, "Oyuncu kapalı bölgenin çitinden geçti");

            // Kapalı kapıya doğru yürü: geçemez.
            Teleport(new Vector3(-6.5f, 0f, rocky.yMin - 0.9f));
            yield return new WaitForSeconds(1.5f);
            Assert.Less(Player.transform.position.z, rocky.yMin, "Oyuncu kapalı kapıdan geçti");
            Player.ScriptedInput = Vector2.zero;
            Shot("E_kapi_kapali");

            // Bölge açılınca kapı açılır, içeri girilir.
            AreaManager.Instance.Unlock(PadArea("Pad Rocky"));
            yield return new WaitForSeconds(1.5f);
            Shot("E_kapi_acildi");
            Teleport(new Vector3(-6.5f, 0f, rocky.yMin - 0.9f));
            Player.ScriptedInput = north;
            yield return new WaitForSeconds(1.2f);
            Player.ScriptedInput = Vector2.zero;
            Assert.Greater(Player.transform.position.z, rocky.yMin + 0.8f, "Bölge açılınca kapıdan girilemedi");

            // Kayma: çite çapraz yürüyünce takılmadan yanından kayar (x değişir).
            Teleport(new Vector3(-3.5f, 0f, rocky.yMin - 0.6f));
            Player.ScriptedInput = new Vector2(0.7f, 0.7f);
            float startX = Player.transform.position.x;
            yield return new WaitForSeconds(0.8f);
            Player.ScriptedInput = Vector2.zero;
            Assert.Greater(Player.transform.position.x - startX, 1f, "Oyuncu çite çapraz yürürken kaymadı (takıldı)");
        }

        // Hiçbir açma karesi, bina girişi/çıkışı, teslim alanı ya da Pazar engeller yüzünden ulaşılamaz olmasın.
        [UnityTest]
        public IEnumerator Reachability_AllTargetsReachable()
        {
            Vector3 start = Player.transform.position;
            yield return WaitFor(() => IslandNavMesh.IsUpToDate, 5f);
            foreach (UnlockPad pad in Object.FindObjectsByType<UnlockPad>(FindObjectsSortMode.None))
            {
                Assert.IsTrue(IslandNavMesh.HasCompletePath(start, pad.transform.position), pad.name + " karesine ulaşılamıyor (başlangıç)");
            }

            DevCommands.UnlockAllRegions();
            DevCommands.UnlockAllBuildings();
            DevCommands.HireAllWorkers();
            foreach (UnlockPad pad in Object.FindObjectsByType<UnlockPad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!AreaManager.Instance.IsUnlocked(pad.Area)) AreaManager.Instance.Unlock(pad.Area);
            }
            yield return new WaitForSeconds(2f); // kapılar açılsın
            yield return WaitFor(() => IslandNavMesh.IsUpToDate, 5f);
            Assert.IsTrue(IslandNavMesh.IsUpToDate, "NavMesh güncellenmedi");

            foreach (BuildingInput input in Object.FindObjectsByType<BuildingInput>(FindObjectsSortMode.None))
                Assert.IsTrue(IslandNavMesh.HasCompletePath(start, input.transform.position), input.transform.parent.name + " girişine ulaşılamıyor");
            foreach (BuildingOutput output in Object.FindObjectsByType<BuildingOutput>(FindObjectsSortMode.None))
                Assert.IsTrue(IslandNavMesh.HasCompletePath(start, output.transform.position), output.transform.parent.name + " çıkışına ulaşılamıyor");
            Assert.IsTrue(IslandNavMesh.HasCompletePath(start, ShipBuilder.Instance.Delivery.transform.position), "Gemi teslim alanına ulaşılamıyor");
            Assert.IsTrue(IslandNavMesh.HasCompletePath(start, FindAny<Market>().transform.position + Vector3.back * 1.5f), "Pazar'a ulaşılamıyor");
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                Assert.IsTrue(IslandNavMesh.HasCompletePath(start, node.GetStandPoint(start)), node.name + " kaynağına ulaşılamıyor");

            // İşçiler takılmıyor: yürüyen (hedefi olan) hiçbir işçi 3 saniye boyunca yerinde saymamalı.
            Worker[] workers = Object.FindObjectsByType<Worker>(FindObjectsSortMode.None);
            Assert.Greater(workers.Length, 2, "İşçiler çıkmadı");
            float[] stuckFor = new float[workers.Length];
            Vector3[] last = new Vector3[workers.Length];
            for (int i = 0; i < workers.Length; i++) last[i] = workers[i].transform.position;
            for (float t = 0f; t < 12f; t += 0.5f)
            {
                yield return new WaitForSeconds(0.5f);
                for (int i = 0; i < workers.Length; i++)
                {
                    WorkerMover mover = workers[i].GetComponent<WorkerMover>();
                    bool walking = mover != null && mover.IsWalking;
                    float moved = Vector3.Distance(last[i], workers[i].transform.position);
                    stuckFor[i] = walking && moved < 0.05f ? stuckFor[i] + 0.5f : 0f;
                    last[i] = workers[i].transform.position;
                    Assert.Less(stuckFor[i], 3f, workers[i].name + " yürümeye çalışırken takıldı (" + workers[i].transform.position + ")");
                }
            }
            Shot("E_hepsi_acik_isciler");
        }
    }
}
