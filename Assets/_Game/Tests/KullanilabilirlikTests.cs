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
