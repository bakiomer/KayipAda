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
                yield return new WaitForSeconds(1.8f);
                Shot("D_gemi_" + (i + 1));
            }

            DevCommands.SkipToCeremony();
            Assert.IsTrue(ship.IsComplete, "Gemi bitmedi");
            Assert.IsTrue(LaunchCeremony.Instance.IsRunning, "Tören başlamadı");
            yield return new WaitForSeconds(ship.Config.GatherTime + ship.Config.SlideDuration * 0.8f);
            Shot("D_toren");
            EscapePanel escape = FindAny<EscapePanel>();
            yield return WaitFor(() => escape.IsShown, ship.Config.SlideDuration + ship.Config.SailDuration + 5f);
            Assert.IsTrue(escape.IsShown, "KAÇTIN! ekranı açılmadı");
            yield return new WaitForSeconds(0.8f);
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
    }
}
