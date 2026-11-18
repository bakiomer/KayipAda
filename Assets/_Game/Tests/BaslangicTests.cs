// BaslangicTests.cs
// Doğal başlangıç ve açılma, kamp, gemi kıyısı: başta kapalı alanlarda kaynak noktası ve hazır bina yok (sadece küçük
// ipuçları; süs palmiyeleri toplanmaz); açılınca kamera odaklanır, ipuçları kaybolur, kaynaklar belirir ve toplanır;
// kamp gemiyle büyür ve kayda girer; görev panosu açılır; boşta işçi ateşin başında dinlenir; gemiyle deniz arasında
// yürünebilir kum şeridi yok. Ekran görüntüleri <önek>_BN_*.png.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class BaslangicTests : GameTestBase
    {
        private GameConfig Config => FindAny<DevMenu>().Config;

        private static UnlockableArea AreaRoot(string name) => GameObject.Find(name).GetComponent<UnlockableArea>();

        [UnityTest]
        public IEnumerator Start_LockedAreasHaveNoNodesOrBuildings()
        {
            yield return new WaitForSeconds(0.5f);
            Rect[] rects = { new Rect(-11f, 6f, 9f, 9f), new Rect(2f, 6f, 6.5f, 9f), new Rect(10.5f, -3.5f, 5.5f, 7f) };
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                Vector2 p = new Vector2(node.transform.position.x, node.transform.position.z);
                foreach (Rect r in rects) Assert.IsFalse(r.Contains(p), "Kapalı alanda kaynak noktası var: " + node.name);
            }
            Assert.AreEqual(0, Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None).Length, "Başta hazır bina var");
            foreach (string area in new[] { "Area Rocky", "Area Orchard", "Area Cove" })
            {
                Transform locked = GameObject.Find(area).transform.Find("Locked");
                Assert.IsTrue(locked.gameObject.activeSelf, area + " ipuçları görünmüyor");
                Assert.Greater(locked.childCount, 2, area + " bomboş (ipucu yok)");
            }
            // Koy'daki süs palmiyeleri kaynak noktası değil (toplanamaz).
            foreach (Transform t in GameObject.Find("Area Cove/Locked").transform)
                Assert.IsNull(t.GetComponent<ResourceNode>(), "Koy'un süs palmiyesi toplanabiliyor");
            Assert.IsNull(GameObject.Find("Thicket Rocky"), "Eski çalılık duruyor");
            Teleport(new Vector3(-6.5f, 0f, 3.5f));
            yield return new WaitForSeconds(0.8f);
            Shot("BN_baslangic_kayalik");
            Teleport(new Vector3(9f, 0f, -1.5f));
            yield return new WaitForSeconds(0.8f);
            Shot("BN_baslangic_koy");
        }

        // Bölge açılınca: kamera odaklanır → yaprak girdabı → ipuçları kaybolur → zemin, kayalar, süsler belirir → kamera döner.
        [UnityTest]
        public IEnumerator Reveal_RegionAppearsAndIsCollectible()
        {
            UnlockableArea rocky = AreaRoot("Area Rocky");
            Teleport(new Vector3(-6.5f, 0f, 3.2f));
            yield return new WaitForSeconds(0.5f);
            AreaManager.Instance.Unlock(rocky.Area);
            yield return null;
            Assert.IsTrue(rocky.IsRevealing, "Açılma efekti başlamadı");
            Assert.IsTrue(CameraFollow.Main.HasShot, "Kamera alana odaklanmadı");
            yield return new WaitForSeconds(0.35f);
            Shot("BN_acilma_1_yapraklar");
            yield return new WaitForSeconds(0.5f);
            Shot("BN_acilma_2_ipuclari_kayboluyor");
            yield return new WaitForSeconds(0.7f);
            Shot("BN_acilma_3_kayalar_yukseliyor");
            yield return WaitFor(() => !rocky.IsRevealing, Config.RevealTime + 2f);
            Assert.IsFalse(CameraFollow.Main.HasShot, "Kamera oyuncuya dönmedi");
            yield return new WaitForSeconds(1f);
            Shot("BN_acilma_4_acildi");
            ResourceNode stone = null;
            foreach (ResourceNode n in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None)) if (n.name.StartsWith("Node_Rock")) stone = n;
            Assert.IsNotNull(stone, "Açılınca kaya belirmedi");
            Teleport(stone.GetStandPoint(stone.transform.position + Vector3.back * 3f));
            yield return WaitFor(() => !PlayerStack.IsEmpty, 6f);
            Assert.IsFalse(PlayerStack.IsEmpty, "Açılan kayadan taş toplanamadı");
        }

        // Kamp: gemi 2. aşamada kulübe, 4. aşamada ev; kayda girer; görev panosu açılır.
        [UnityTest]
        public IEnumerator Camp_GrowsSavesAndQuestBoard()
        {
            CampSite camp = CampSite.Instance;
            Assert.IsNotNull(camp, "Kamp yok");
            Assert.AreEqual(0, camp.Level);
            Assert.Less(Vector3.Distance(Player.transform.position, camp.FirePosition), 5f, "Oyuncu kampta başlamıyor");
            Teleport(camp.FirePosition + new Vector3(0f, 0f, -3.6f));
            yield return new WaitForSeconds(0.8f);
            Shot("BN_kamp_cadir");
            ShipBuilder.Instance.ForceCompleteStage();
            yield return new WaitForSeconds(1.5f);
            ShipBuilder.Instance.ForceCompleteStage();
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(1, camp.Level, "Gemi 2. aşamada kamp büyümedi");
            Teleport(camp.FirePosition + new Vector3(0f, 0f, -3.6f));
            yield return new WaitForSeconds(1f);
            Shot("BN_kamp_kulube");
            SaveData data = new SaveData();
            data.CampLevel = camp.Level;
            Assert.AreEqual(1, data.CampLevel);
            data.ResetRun();
            Assert.AreEqual(0, data.CampLevel, "Kaçışta kamp sıfırlanmadı");
            DevCommands.GrowCamp();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(2, camp.Level);
            Shot("BN_kamp_ev");

            QuestBoard board = FindAny<QuestBoard>();
            Assert.IsNotNull(board, "Görev panosu yok");
            Teleport(board.transform.position);
            yield return WaitFor(() => QuestBoardPanel.Instance.IsOpen, 2f);
            Assert.IsTrue(QuestBoardPanel.Instance.IsOpen, "Görev panosu açılmadı");
            StringAssert.Contains("/", QuestBoardPanel.Instance.ActiveText);
            yield return new WaitForSeconds(0.4f);
            Shot("BN_gorev_panosu");
        }

        // Boşta kalan işçi kısa süre ateşin başında dinlenir.
        [UnityTest]
        public IEnumerator Workers_RestByFire()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            Hire("carrier");
            Worker worker = null;
            yield return WaitFor(() =>
            {
                foreach (Worker w in Object.FindObjectsByType<Worker>(FindObjectsSortMode.None)) if (w.IsResting) { worker = w; return true; }
                return false;
            }, 25f);
            Assert.IsNotNull(worker, "Boştaki işçi ateşin başına gelmedi");
            yield return WaitFor(() => Vector3.Distance(worker.transform.position, CampSite.Instance.FirePosition) < 3f, 15f);
            Teleport(CampSite.Instance.FirePosition + new Vector3(0f, 0f, -3.6f));
            yield return new WaitForSeconds(0.8f);
            Shot("BN_kamp_atesi");
        }

        // Gemi kıyıya yakın: gemiyle deniz arasında yürünebilir kum şeridi yok.
        [UnityTest]
        public IEnumerator Ship_NoSandStripOnSeaSide()
        {
            yield return null;
            Transform obstacleT = GameObject.Find("Shipyard/ShipObstacle").transform;
            NavObstacle ship = obstacleT.GetComponent<NavObstacle>();
            Vector3 center = obstacleT.position;
            // Gemiden denize doğru: her nokta ya gemi engelinin içinde ya da denizde.
            Vector3 sea = Vector3.forward;
            Vector2 c2 = new Vector2(center.x, center.z);
            for (float d = 0f; d < 8f; d += 0.25f)
            {
                Vector2 p = c2 + new Vector2(sea.x, sea.z) * d;
                bool inShip = ship.Contains(p, 0.3f);
                bool inSea = IslandShape.Instance.SignedDistance(p) > -0.6f;
                Assert.IsTrue(inShip || inSea, "Gemiyle deniz arasında yürünebilir şerit var (" + p + ")");
            }
            Teleport(center + Vector3.back * 6f);
            yield return new WaitForSeconds(0.8f);
            Shot("BN_gemi_kiyi");
        }
    }
}
