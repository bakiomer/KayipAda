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

        [UnityTest]
        public IEnumerator Temple_PlatesSolveRiddleAndChestRises()
        {
            DevCommands.CompleteBridges();
            yield return new WaitForSeconds(4f);
            TempleRiddle riddle = FindAny<TempleRiddle>();
            Assert.IsNotNull(riddle, "Tapınak bulmacası yok");
            Assert.IsTrue(riddle.isActiveAndEnabled, "Tapınak adası açılmadı");
            TreasureChest chest = riddle.ChestHolder.GetComponentInChildren<TreasureChest>(true);
            Assert.IsNotNull(chest, "Bulmaca sandığı yok");
            Assert.Less(riddle.ChestHolder.position.y, -1f, "Sandık baştan yerin üstünde");
            Assert.IsFalse(chest.IsOpen);
            Assert.AreEqual(3, riddle.Plates.Length);
            for (int i = 0; i < riddle.Plates.Length; i++)
            {
                Teleport(riddle.Plates[i].position);
                yield return new WaitForSeconds(0.35f);
                Assert.AreEqual(i + 1, riddle.LitCount, "Levha " + (i + 1) + " yanmadı");
                if (i == 1) Shot("K_tapinak_levhalar");
                if (i < 2) { Teleport(riddle.transform.position + new Vector3(-4.5f, 0f, -1f)); yield return new WaitForSeconds(0.2f); }
            }
            Assert.IsTrue(riddle.Solved, "Üç levha yanınca bulmaca çözülmedi");
            Teleport(riddle.transform.position + new Vector3(-3.6f, 0f, -3.6f));
            yield return new WaitForSeconds(1.6f);
            Shot("K_tapinak_cember");
            yield return new WaitForSeconds(1.4f);
            Assert.Greater(riddle.ChestHolder.position.y, -0.05f, "Sandık yükselmedi");
            Teleport(riddle.ChestHolder.position + Vector3.back * 1.3f);
            yield return WaitFor(() => chest.IsOpen, 2f);
            Assert.IsTrue(chest.IsOpen, "Sandık açılmadı");
            Assert.IsNotNull(riddle.Reward, "Koleksiyon eşyası bağlı değil");
            yield return WaitFor(() => riddle.Reward.IsTaken, 3f);
            Assert.IsTrue(riddle.Reward.IsTaken, "Sandıktan çıkan koleksiyon eşyası alınamadı");
            Shot("K_tapinak_sandik");
        }

        [UnityTest]
        public IEnumerator Temple_RingAndColumnsSolid_PlatesWalkable_StateRestores()
        {
            DevCommands.CompleteBridges();
            yield return new WaitForSeconds(4f);
            TempleRiddle riddle = FindAny<TempleRiddle>();
            Vector3 c = riddle.transform.position;
            Vector2 center = new Vector2(c.x, c.z);
            Assert.Greater((NavObstacle.PushOut(center, 0.4f) - center).magnitude, 1.2f, "Taş çemberin içinden geçiliyor");
            int columns = 0;
            foreach (Transform t in riddle.transform.parent)
            {
                if (t.name != "Column") continue;
                Vector2 p = new Vector2(t.position.x, t.position.z);
                Assert.Greater((NavObstacle.PushOut(p, 0.4f) - p).magnitude, 0.5f, "Sütunun içinden geçiliyor");
                columns++;
            }
            Assert.GreaterOrEqual(columns, 5, "Sütun bulunamadı");
            foreach (Transform plate in riddle.Plates)
            {
                Vector2 p = new Vector2(plate.position.x, plate.position.z);
                Assert.Less((NavObstacle.PushOut(p, 0.4f) - p).magnitude, 0.05f, "Levhanın üstüne basılamıyor");
            }
            // Kayıttan: çözülmüş durum geri gelir (sandık yerinde), yeni oyunda sıfır.
            riddle.Restore(true);
            Assert.IsTrue(riddle.Solved);
            Assert.Greater(riddle.ChestHolder.position.y, -0.05f, "Kayıttan yüklenince sandık yerinde değil");
            riddle.Restore(false);
            Assert.IsFalse(riddle.Solved);
            Assert.AreEqual(0, riddle.LitCount);
            SaveData data = new SaveData { TempleSolved = true };
            data.ResetRun();
            Assert.IsFalse(data.TempleSolved, "Yeni oyunda tapınak sıfırlanmadı");
        }

        [UnityTest]
        public IEnumerator Camera_DefaultIsFartherAndPresetsChange()
        {
            CameraFollow cam = CameraFollow.Main;
            Assert.IsNotNull(cam);
            GameConfig cfg = cam.Config;
            Assert.AreEqual(3, cfg.CameraZoomPresets.Length, "Yakın/Normal/Uzak ön ayarları yok");
            Assert.AreEqual("Normal", cam.ZoomName);
            float ratio = cam.Distance / cfg.CameraDistance;
            Assert.That(ratio, Is.InRange(1.09f, 1.16f), "Varsayılan kamera %10-15 geriden değil");
            Quaternion rot = cam.transform.rotation;
            yield return new WaitForSeconds(1.5f);
            Shot("K_kamera_normal");
            float[] distances = new float[3];
            for (int i = 0; i < 3; i++)
            {
                cam.SetZoom(i);
                yield return new WaitForSeconds(2f);
                distances[i] = Vector3.Distance(cam.transform.position, Player.transform.position);
                Assert.AreEqual(Quaternion.Angle(rot, cam.transform.rotation), 0f, 0.5f, "Kamera açısı değişti");
                Shot("K_kamera_" + CameraFollow.ZoomNames[i].ToLowerInvariant().Replace("ı", "i"));
            }
            Assert.Less(distances[0], distances[1], "Yakın, Normal'den uzak");
            Assert.Less(distances[1], distances[2], "Normal, Uzak'tan uzak");
            cam.CycleZoom(); // Uzak → Yakın
            Assert.AreEqual(0, cam.ZoomIndex);
            PlayerPrefs.DeleteKey("kayipada_camera_zoom");
        }

        [UnityTest]
        public IEnumerator Celebration_NoRepeatedMelodyAndPriorities()
        {
            yield return new WaitForSeconds(0.5f);
            GameConfig cfg = CameraFollow.Main.Config;
            int m0 = AudioManager.MelodiesPlayed;
            AudioManager.Celebrate(CelebrationTier.Medium);
            Assert.AreEqual(m0 + 1, AudioManager.MelodiesPlayed, "Orta kutlama melodi çalmadı");
            AudioClip first = AudioManager.LastMelody;
            Assert.IsNotNull(first);
            Assert.That(first.length, Is.InRange(0.9f, 2.2f), "Orta melodi 1-2 sn değil");
            // Art arda açılış: melodi tekrar çalmaz, sadece efekt.
            AudioManager.Celebrate(CelebrationTier.Medium);
            AudioManager.Celebrate(CelebrationTier.Medium);
            Assert.AreEqual(m0 + 1, AudioManager.MelodiesPlayed, "Art arda açılışta melodi tekrar çaldı");
            Assert.AreEqual(CelebrationTier.Small, AudioManager.LastCelebrationPlayed);
            AudioManager.Celebrate(CelebrationTier.Small);
            Assert.AreEqual(m0 + 1, AudioManager.MelodiesPlayed, "Küçük kutlama melodi çaldı");
            // Büyük kutlama önceliklidir (ortayı keser) ama büyük büyüğün üstüne binmez.
            AudioManager.Celebrate(CelebrationTier.Big);
            Assert.AreEqual(m0 + 2, AudioManager.MelodiesPlayed, "Büyük kutlama çalmadı");
            Assert.That(AudioManager.LastMelody.length, Is.InRange(1.9f, 4.2f), "Büyük müzik 2-4 sn değil");
            AudioManager.Celebrate(CelebrationTier.Big);
            Assert.AreEqual(m0 + 2, AudioManager.MelodiesPlayed, "Büyük kutlamalar üst üste bindi");
            // Bekleme süresi geçince yeni orta melodi çalar; aynı melodi üst üste gelmez.
            AudioClip prev = null;
            for (int i = 0; i < 2; i++)
            {
                yield return new WaitForSecondsRealtime(cfg.CelebrationMelodyCooldown + 0.3f);
                int before = AudioManager.MelodiesPlayed;
                AudioManager.Celebrate(CelebrationTier.Medium);
                Assert.AreEqual(before + 1, AudioManager.MelodiesPlayed, "Bekleme sonrası melodi çalmadı");
                if (prev != null) Assert.AreNotEqual(prev, AudioManager.LastMelody, "Aynı melodi üst üste çaldı");
                prev = AudioManager.LastMelody;
            }
        }

        [UnityTest]
        public IEnumerator Quests_SellCountsInInfiniteMoneyMode()
        {
            QuestManager qm = QuestManager.Instance;
            QuestDefinition sell = null, wood = null;
            foreach (QuestDefinition q in qm.Active) { if (q.Kind == QuestKind.Sell) sell = q; if (q.Kind == QuestKind.Collect) wood = q; }
            Assert.IsNotNull(sell, "Satış görevi aktif değil");
            Assert.IsNotNull(wood, "Odun toplama görevi aktif değil");
            DevCommands.SetInfiniteMoney(CameraFollow.Main.Config, true);
            ResourceType woodType = null;
            foreach (ResourceType t in Resources.FindObjectsOfTypeAll<ResourceType>()) if (t.Id == "wood") woodType = t;
            DevCommands.FillPlayerStack(woodType);
            Teleport(FindAny<Market>().transform.position);
            yield return WaitFor(() => qm.Progress(sell) >= 5 || qm.IsCompleted(sell), 5f);
            Assert.IsTrue(qm.Progress(sell) >= 5 || qm.IsCompleted(sell), "Sınırsız Para modunda satış sayılmadı");
            DevCommands.SetInfiniteMoney(CameraFollow.Main.Config, false);
            // Odun toplama: oyuncu palmiyeden toplarken ilerler.
            int before = qm.Progress(wood);
            ResourceNode palm = null;
            foreach (ResourceNode n in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
                if (n.isActiveAndEnabled && n.Type != null && n.Type.Resource == woodType) { palm = n; break; }
            Assert.IsNotNull(palm);
            Teleport(palm.GetStandPoint(Player.transform.position));
            yield return WaitFor(() => qm.Progress(wood) > before || qm.IsCompleted(wood), 6f);
            Assert.IsTrue(qm.Progress(wood) > before || qm.IsCompleted(wood), "Odun toplama görevi ilerlemedi");
        }

        [UnityTest]
        public IEnumerator Workers_AreHumanPirates()
        {
            yield return null;
            int checkedWorkers = 0;
            foreach (Worker w in Object.FindObjectsByType<Worker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (SkinnedMeshRenderer r in w.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    string n = (r.sharedMesh != null ? r.sharedMesh.name : r.name).ToLowerInvariant();
                    Assert.IsFalse(n.Contains("mako") || n.Contains("shark") || n.Contains("skeleton"), w.name + " insan değil: " + n);
                }
                checkedWorkers++;
            }
            Assert.GreaterOrEqual(checkedWorkers, 4, "İşçi şablonları bulunamadı");
            Assert.IsNotNull(GameObject.Find("StrawHat") ?? FindHidden("StrawHat"), "Lifçi'nin hasır şapkası yok");
            Assert.IsNotNull(GameObject.Find("Apron") ?? FindHidden("Apron"), "Demirci Çırağı'nın önlüğü yok");
        }

        private static GameObject FindHidden(string name)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t.name == name) return t.gameObject;
            return null;
        }

        [UnityTest]
        public IEnumerator FloatingText_StaysOnScreen_AndSignsAreCapped()
        {
            yield return new WaitForSeconds(0.5f);
            Camera cam = Camera.main;
            // Ekranın sağ kenarına çok yakın uzun yazı.
            Vector3 edge = cam.ViewportToWorldPoint(new Vector3(0.99f, 0.5f, 30f));
            edge.y = 1f;
            Effects.Instance.ShowText(edge, "Demirci Seviye 2!", Color.green);
            yield return new WaitForSeconds(0.4f);
            FloatingText ft = null;
            foreach (FloatingText f in Object.FindObjectsByType<FloatingText>(FindObjectsSortMode.None)) if (f.Text == "Demirci Seviye 2!") ft = f;
            Assert.IsNotNull(ft, "Yazı çıkmadı");
            Bounds b = ft.GetComponentInChildren<TMPro.TMP_Text>().bounds;
            TMPro.TMP_Text label = ft.GetComponentInChildren<TMPro.TMP_Text>();
            Vector3 right = cam.WorldToViewportPoint(label.transform.TransformPoint(new Vector3(label.textBounds.max.x, 0f, 0f)));
            Vector3 left = cam.WorldToViewportPoint(label.transform.TransformPoint(new Vector3(label.textBounds.min.x, 0f, 0f)));
            Assert.LessOrEqual(Mathf.Max(left.x, right.x), 1.001f, "Uçan yazı sağ kenarda kesiliyor");
            Assert.GreaterOrEqual(Mathf.Min(left.x, right.x), -0.001f, "Uçan yazı sol kenarda kesiliyor");
            Shot("K_ucan_yazi_kenar");

            // Kamera bir tabelaya çok yakınken tabela ekranı kaplamaz.
            GameConfig cfg = CameraFollow.Main.Config;
            ScreenSizeLimit sign = null;
            foreach (ScreenSizeLimit s in Object.FindObjectsByType<ScreenSizeLimit>(FindObjectsSortMode.None)) if (s.transform.parent != null && s.transform.parent.name == "UpgradeStation") sign = s;
            Assert.IsNotNull(sign, "Tabela sınırlayıcı yok");
            Vector3 p = sign.transform.position - cam.transform.forward * 4f;
            CameraFollow.Main.SetShot(p, cam.transform.rotation, 50f);
            yield return new WaitForSeconds(1f);
            Assert.LessOrEqual(sign.LastScreenHeight, cfg.SignMaxScreenHeight + 0.002f, "Tabela yakında ekranı kaplıyor");
            Assert.LessOrEqual(sign.LastScreenWidth, cfg.SignMaxScreenWidth + 0.002f);
            Shot("K_tabela_yakin");
            CameraFollow.Main.ClearShot();
        }

        [UnityTest]
        public IEnumerator Bridge_FrameHiddenUntilPlanks()
        {
            yield return null;
            BridgeBuilder wreck = null;
            foreach (BridgeBuilder b in Object.FindObjectsByType<BridgeBuilder>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (b.Id == "bridge_wreck") wreck = b;
            Assert.IsNotNull(wreck);
            Assert.Greater(wreck.FrameCount, 3);
            Assert.AreEqual(0, wreck.VisibleFrames, "Köprü kirişleri baştan denizde görünüyor");
            DevCommands.CompleteBridges();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(wreck.FrameCount, wreck.VisibleFrames, "Köprü bitince iskelet tamam değil");
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
