// KullanilabilirlikTests.cs
// Kullanılabilirlik ve cila turu testleri: paneller sadece oyuncu durunca açılır, hareketle anında kapanır, yanından
// geçerken açılmaz, X ile kapanınca tekrar durana kadar açılmaz; joystick hiç kilitlenmez.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class KullanilabilirlikTests : GameTestBase
    {
        [UnityTest]
        public IEnumerator Panel_OpensOnlyWhenStill_ClosesOnMove()
        {
            UpgradeStation station = Find<UpgradeStation>("UpgradeStation");
            UpgradePanel panel = Find<UpgradePanel>("UpgradePanel");
            Assert.IsNotNull(station, "Geliştirme tezgahı yok");
            Vector3 c = station.transform.position;

            // 1) Yanından geçerken (alanın içinden yürüyerek) açılmaz.
            Teleport(c + Vector3.left * 3.2f);
            yield return new WaitForSeconds(0.2f);
            Player.ScriptedInput = new Vector2(1f, 0f);
            bool openedWhileWalking = false;
            float end = Time.time + 1.6f;
            while (Time.time < end)
            {
                if (panel.IsOpen) openedWhileWalking = true;
                yield return null;
            }
            Player.ScriptedInput = Vector2.zero;
            Assert.IsFalse(openedWhileWalking, "Panel yanından geçerken açıldı");

            // 2) Alanda durunca açılır; yürümeyi engellemez.
            Teleport(c);
            yield return WaitFor(() => panel.IsOpen, 2f);
            Assert.IsTrue(panel.IsOpen, "Oyuncu durunca panel açılmadı");
            Assert.IsFalse(PlayerController.InputBlocked, "Panel joystick'i kilitledi");
            yield return new WaitForSeconds(0.4f);
            Shot("K_gelistirme_paneli");

            // 3) Hareket başladığı an kapanır.
            Player.ScriptedInput = new Vector2(0f, -1f);
            yield return null;
            yield return null;
            Assert.IsFalse(panel.IsOpen, "Hareket başlayınca panel kapanmadı");
            Player.ScriptedInput = Vector2.zero;

            // 4) Tekrar durunca açılır; X ile kapatınca, durmaya devam etse de tekrar açılmaz.
            Teleport(c);
            yield return WaitFor(() => panel.IsOpen, 2f);
            Assert.IsTrue(panel.IsOpen, "Tekrar durunca açılmadı");
            panel.Close(null);
            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(panel.IsOpen, "X ile kapanınca kendiliğinden yeniden açıldı");
        }

        [UnityTest]
        public IEnumerator AllPanelZones_OpenOnStillAndCloseOnMove()
        {
            DevCommands.UnlockAllRegions();
            DevCommands.UnlockAllBuildings();
            DevCommands.FillStorage();
            yield return new WaitForSeconds(3.5f);
            int checkedZones = 0;
            foreach (PanelZone zone in Object.FindObjectsByType<PanelZone>(FindObjectsSortMode.None))
            {
                if (!zone.isActiveAndEnabled) continue;
                Teleport(zone.transform.position + zone.transform.TransformVector(LocalCenter(zone)));
                yield return WaitFor(() => zone.PlayerInside && AnyPanelOpen(), 2.5f);
                Assert.IsTrue(AnyPanelOpen(), zone.name + " (" + zone.transform.parent?.name + "): durunca panel açılmadı");
                Assert.IsFalse(PlayerController.InputBlocked, zone.name + ": panel joystick'i kilitledi");
                Player.ScriptedInput = new Vector2(0f, -1f);
                yield return null;
                yield return null;
                Assert.IsFalse(AnyPanelOpen(), zone.name + ": hareketle kapanmadı");
                Player.ScriptedInput = Vector2.zero;
                Teleport(new Vector3(0f, 0f, -6.2f));
                yield return new WaitForSeconds(0.3f);
                checkedZones++;
            }
            Assert.GreaterOrEqual(checkedZones, 6, "Panel alanları eksik");
        }

        [UnityTest]
        public IEnumerator Camp_ZonesDoNotOverlap_AndFireIsOpen()
        {
            DevCommands.UnlockAllRegions();
            yield return new WaitForSeconds(3f);
            Vector3 camp = new Vector3(-3f, 0f, -3f);
            System.Collections.Generic.List<Collider> zones = new System.Collections.Generic.List<Collider>();
            foreach (InteractionZone z in Object.FindObjectsByType<InteractionZone>(FindObjectsSortMode.None))
            {
                Collider c = z.GetComponent<Collider>();
                if (c == null || !c.enabled || !z.isActiveAndEnabled) continue;
                Vector3 d = z.transform.position - camp;
                if (new Vector2(d.x, d.z).magnitude < 12f) zones.Add(c);
            }
            Assert.GreaterOrEqual(zones.Count, 5, "Kamp çevresinde alan bulunamadı");
            for (int i = 0; i < zones.Count; i++)
                for (int j = i + 1; j < zones.Count; j++)
                {
                    Collider a = zones[i], b = zones[j];
                    Vector3 pa = a.ClosestPoint(b.bounds.center);
                    Vector3 pb = b.ClosestPoint(pa);
                    pa = a.ClosestPoint(pb);
                    Assert.Greater(Vector3.Distance(pa, pb), 0.05f, "Alanlar çakışıyor: " + Path(a.transform) + " ↔ " + Path(b.transform));
                }
            // Oyuncunun başlangıç noktası hiçbir alanın içinde değil.
            Vector3 start = new Vector3(0f, 1f, -6.2f);
            foreach (Collider c in zones)
                Assert.Greater(Vector3.Distance(c.ClosestPoint(start), start), 0.3f, "Başlangıç noktası alanın içinde: " + Path(c.transform));
            // Ateşin çevresi açık: oyuncu ateşin etrafında dolaşabilir (her yönden 2.6 m'ye kadar yaklaşılır).
            Vector2 fire = new Vector2(0f, -2.9f);
            for (int k = 0; k < 8; k++)
            {
                if (k == 2) continue; // kuzey: kampın kendi barınağı (bilerek ateşin yanında)
                float a = k * Mathf.PI / 4f;
                Vector2 p = fire + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.6f;
                Vector2 pushed = NavObstacle.PushOut(p, 0.4f);
                Assert.Less((pushed - p).magnitude, 0.05f, "Ateşin çevresi kapalı (" + k + ")");
            }
            Teleport(new Vector3(-4.5f, 0f, -5.5f));
            yield return new WaitForSeconds(0.6f);
            Shot("K_kamp_duzeni");
        }

        [UnityTest]
        public IEnumerator Market_SaleOpensCashLidAndGrowsPile()
        {
            Market market = FindAny<Market>();
            Assert.IsNotNull(market, "Pazar yok");
            Vector3 d = market.transform.position - new Vector3(0f, 0f, -2.9f);
            Assert.Less(d.x, -5f, "Pazar kampın batısında değil");
            Assert.Less(new Vector2(d.x, d.z).magnitude, 11f, "Pazar kampa uzak");
            ResourceType wood = null;
            foreach (ResourceType t in Resources.FindObjectsOfTypeAll<ResourceType>()) if (t.Id == "wood") { wood = t; break; }
            Assert.IsNotNull(wood);
            DevCommands.FillPlayerStack(wood);
            int coins = CurrencyManager.Instance.Coins;
            Teleport(market.transform.position);
            bool lidOpened = false, grew = false;
            float end = Time.time + 4f;
            while (Time.time < end && !(lidOpened && grew))
            {
                if (market.LidAngle > 20f) lidOpened = true;
                if (market.IsDisplayBoosted("wood")) grew = true;
                yield return null;
            }
            Assert.IsTrue(lidOpened, "Kasa kapağı açılmadı");
            Assert.IsTrue(grew, "Odun yığını büyümedi");
            Assert.Greater(CurrencyManager.Instance.Coins, coins, "Para kazanılmadı");
            Shot("K_pazar_satis");
            yield return new WaitForSeconds(2.5f);
            Assert.Less(market.LidAngle, 1f, "Kasa kapağı kapanmadı");
            // Satıcı tezgahın içinde değil, tezgaha girilemiyor (yukarıdaki FixTests) — burada sadece genel görünüm.
            Teleport(market.transform.position + new Vector3(0f, 0f, -2.2f));
            yield return new WaitForSeconds(0.8f);
            Shot("K_pazar");
        }

        private static string Path(Transform t)
        {
            return t.parent != null ? t.parent.name + "/" + t.name : t.name;
        }

        private static Vector3 LocalCenter(PanelZone zone)
        {
            SphereCollider s = zone.GetComponent<SphereCollider>();
            if (s != null) return new Vector3(s.center.x, 0f, s.center.z);
            BoxCollider b = zone.GetComponent<BoxCollider>();
            return b != null ? new Vector3(b.center.x, 0f, b.center.z) : Vector3.zero;
        }

        private static bool AnyPanelOpen()
        {
            foreach (UpgradePanel p in Object.FindObjectsByType<UpgradePanel>(FindObjectsSortMode.None)) if (p.IsOpen) return true;
            return (BuildingUpgradePanel.Instance != null && BuildingUpgradePanel.Instance.IsOpen)
                || (StoragePanel.Instance != null && StoragePanel.Instance.IsOpen)
                || (QuestBoardPanel.Instance != null && QuestBoardPanel.Instance.IsOpen);
        }
    }
}
