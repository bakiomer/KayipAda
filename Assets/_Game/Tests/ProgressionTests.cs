// ProgressionTests.cs
// Bölüm B ve C testleri: geliştirme satın alma, gemi aşamaları, son gece düşmanları, topun ateş etmesi, kaçış ekranı.

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

        // Geminin şu anki aşamasını, gereken malzemeleri teslim ederek bitirir.
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

        [UnityTest]
        public IEnumerator Ship_StagesAdvance()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            Assert.IsNotNull(ship, "Gemi yok");
            Assert.AreEqual(0, ship.StageIndex);
            yield return CompleteStage(ship);
            Assert.AreEqual(1, ship.StageIndex, "Omurga bitince aşama ilerlemedi");
            Assert.IsTrue(AreaManager.Instance.IsUnlocked(ship.Config.KeelDoneArea), "Omurga bitince toplar açılmaya hazır olmalı");
            yield return new WaitForSeconds(2f);
            Teleport(ship.Delivery.transform.position + Vector3.back * 2f);
            yield return new WaitForSeconds(1.5f);
            Shot("C_gemi_omurga");

            yield return CompleteStage(ship);
            yield return new WaitForSeconds(3f);
            Shot("C_gemi_govde");
            Assert.AreEqual(2, ship.StageIndex);
        }

        private static IEnumerator FinishShip(ShipBuilder ship)
        {
            while (!ship.IsComplete) yield return CompleteStage(ship);
        }

        [UnityTest]
        public IEnumerator Night_EnemiesComeAndCannonFires()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            Teleport(ship.Delivery.transform.position + Vector3.back * 3f);
            foreach (UnlockPad pad in Object.FindObjectsByType<UnlockPad>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pad.name.StartsWith("Pad Cannon")) AreaManager.Instance.Unlock(pad.Area);
            }
            yield return FinishShip(ship);
            Assert.IsTrue(ship.IsComplete, "Gemi bitmedi");

            NightDefense night = NightDefense.Instance;
            yield return WaitFor(() => Enemy.Alive.Count > 0, ship.Config.NightStartDelay + 6f);
            Assert.Greater(Enemy.Alive.Count, 0, "Son gecede düşman gelmedi");
            Assert.AreEqual(NightDefense.Phase.Night, night.State);

            Cannon cannon = FindAny<Cannon>();
            Assert.IsNotNull(cannon, "Top yok");
            int ammo = cannon.Ammo;
            Time.timeScale = 3f;
            yield return WaitFor(() => cannon.Ammo < ammo, 40f);
            Time.timeScale = 1f;
            Assert.Less(cannon.Ammo, ammo, "Top ateş etmedi");
            Shot("C_son_gece");
        }

        [UnityTest]
        public IEnumerator Escape_ScreenAppearsAfterWaves()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            yield return FinishShip(ship);
            NightDefense night = NightDefense.Instance;
            EscapePanel escape = FindAny<EscapePanel>();
            Assert.IsNotNull(escape);

            Time.timeScale = 4f;
            float end = Time.time + 400f;
            while (!escape.IsShown && Time.time < end)
            {
                // Testte toplar yerine düşmanları doğrudan yen.
                for (int i = Enemy.Alive.Count - 1; i >= 0; i--) Enemy.Alive[i].Hit(1000);
                yield return null;
            }
            Time.timeScale = 1f;
            Assert.AreEqual(NightDefense.Phase.Won, night.State, "Dalgalar bitmedi");
            Assert.IsTrue(escape.IsShown, "Kaçış ekranı açılmadı");
            yield return new WaitForSeconds(0.8f);
            Shot("C_kacis_ekrani");
        }

        [UnityTest]
        public IEnumerator Defeat_RetryKeepsProgress()
        {
            ShipBuilder ship = ShipBuilder.Instance;
            yield return FinishShip(ship);
            NightDefense night = NightDefense.Instance;
            yield return WaitFor(() => night.State == NightDefense.Phase.Night, 10f);
            night.DamageShip(100000f);
            Assert.AreEqual(NightDefense.Phase.Lost, night.State, "Gemi batınca kaybedilmedi");
            yield return new WaitForSeconds(0.6f);
            Shot("C_tekrar_dene");
            night.Retry();
            Assert.AreEqual(NightDefense.Phase.Night, night.State, "Tekrar dene geceyi baştan başlatmadı");
            Assert.IsTrue(ship.IsComplete, "Tekrar denede gemi ilerlemesi silindi");
        }
    }
}
