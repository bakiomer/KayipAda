// FixTests.cs
// Düzeltme turunun testleri: teslim alanına her yönden takılmadan ulaşma (gemi/kızak çarpışması), Tayfa Barakası'ndan
// işçi tutma (kapıdan çıkar, kilitli tür tutulamaz), makinelerin sadece üretirken hareket etmesi, iz havuzunun sınırı,
// Pazar tezgahının altından geçilememesi. Ekran görüntüleri: Docs/Screenshots/<önek>_A_*.png

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class FixTests : GameTestBase
    {
        private float CameraYaw => FindAny<DevMenu>().Config.CameraYaw;

        // Dünya yönünü joystick girdisine çevirir (joystick yukarısı = kameranın baktığı yön).
        private Vector2 InputFor(Vector3 worldDir)
        {
            Vector3 local = Quaternion.Euler(0f, -CameraYaw, 0f) * worldDir;
            Vector2 v = new Vector2(local.x, local.z);
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.zero;
        }

        // Oyuncuyu NavMesh yolunu izleyerek hedefe yürütür. Takılırsa (1.5 sn boyunca ilerleyemezse) hata mesajı döner.
        private IEnumerator WalkTo(Vector3 target, float timeout, List<string> errors, string label)
        {
            List<Vector2> path = new List<Vector2>();
            Vector3 p = Player.transform.position;
            if (!IslandNavMesh.TryFindPath(new Vector2(p.x, p.z), new Vector2(target.x, target.z), path) || path.Count == 0)
            {
                errors.Add(label + ": yol bulunamadı");
                yield break;
            }
            int index = 0;
            float stuck = 0f, end = Time.time + timeout;
            Vector3 last = p;
            while (Time.time < end)
            {
                Vector3 pos = Player.transform.position;
                if (Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(target.x, target.z)) < 1.1f) break;
                Vector2 corner = path[Mathf.Min(index, path.Count - 1)];
                Vector3 dir = new Vector3(corner.x - pos.x, 0f, corner.y - pos.z);
                if (dir.magnitude < 0.45f && index < path.Count - 1) { index++; continue; }
                Player.ScriptedInput = InputFor(dir);
                yield return new WaitForFixedUpdate();
                float moved = Vector3.Distance(Player.transform.position, last);
                last = Player.transform.position;
                stuck = moved < 0.004f ? stuck + Time.fixedDeltaTime : 0f;
                if (stuck > 1.5f)
                {
                    errors.Add(label + ": takıldı " + Player.transform.position.ToString("F1"));
                    break;
                }
            }
            Player.ScriptedInput = Vector2.zero;
            Vector3 f = Player.transform.position;
            if (Vector2.Distance(new Vector2(f.x, f.z), new Vector2(target.x, target.z)) > 1.6f && (errors.Count == 0 || !errors[errors.Count - 1].StartsWith(label)))
                errors.Add(label + ": varamadı " + f.ToString("F1"));
        }

        // Takılma testi: teslim alanına 8 yönden (karadan) yürünür; hiçbirinde takılmaz. Gemiye çapraz yürüyünce kenarından kayar.
        [UnityTest]
        public IEnumerator Stuck_DeliveryReachableFromAllSides()
        {
            yield return WaitFor(() => IslandNavMesh.IsUpToDate, 5f);
            Vector3 delivery = ShipBuilder.Instance.Delivery.transform.position;
            List<string> errors = new List<string>();
            int tried = 0;
            for (int i = 0; i < 32; i++)
            {
                Vector3 start = delivery + Quaternion.Euler(0f, (i % 16) * 22.5f, 0f) * Vector3.forward * (i < 16 ? 6f : 9.5f);
                Vector2 s2 = new Vector2(start.x, start.z);
                if (!IslandShape.Instance.Contains(s2, 1.6f)) continue;
                bool blocked = false;
                foreach (NavObstacle o in NavObstacle.Active) if (o.Contains(s2, 0.8f)) blocked = true;
                if (blocked || !IslandNavMesh.HasCompletePath(start, delivery)) continue;
                tried++;
                Teleport(start);
                yield return null;
                yield return WalkTo(delivery, 14f, errors, "Yön " + ((i % 16) * 22.5f) + "° " + (i < 16 ? "yakın" : "uzak"));
            }
            Assert.GreaterOrEqual(tried, 8, "Yeterli başlangıç noktası yok");
            Assert.IsEmpty(errors, string.Join("\n", errors));

            // Gemiye (yuvarlak köşeli engel) çapraz yürü: durmaz, kenarından kayar.
            NavObstacle ship = null;
            foreach (NavObstacle o in NavObstacle.Active) if (o.name == "ShipObstacle") ship = o;
            Assert.IsNotNull(ship, "Gemi engeli yok");
            Vector3 toShip = new Vector3(ship.Center.x, 0f, ship.Center.y) - delivery;
            toShip.y = 0f;
            Vector3 along = Quaternion.Euler(0f, ship.Yaw, 0f) * Vector3.forward;
            Teleport(delivery);
            Player.ScriptedInput = InputFor(toShip.normalized + along * 0.6f);
            Vector3 before = Player.transform.position;
            yield return new WaitForSeconds(2f);
            Player.ScriptedInput = Vector2.zero;
            float slid = Vector3.Distance(before, Player.transform.position);
            Assert.Greater(slid, 2.5f, "Oyuncu gemiye çapraz yürürken kaymadı (takıldı)");
            Assert.IsFalse(ship.Contains(new Vector2(Player.transform.position.x, Player.transform.position.z), 0.2f), "Oyuncu gemi/kızağın içine girdi");
            Teleport(delivery + Vector3.back * 1.5f);
            yield return new WaitForSeconds(0.6f);
            Shot("A_gemi_teslim_alani");
        }

        // Tayfa Barakası: kart "TUT" ile işçi tutulur, işçi kapıdan çıkar; binası açılmamış tür tutulamaz.
        [UnityTest]
        public IEnumerator Barracks_HireSpawnsWorkerAndRespectsLocks()
        {
            CrewManager crew = CrewManager.Instance;
            Assert.IsNotNull(crew, "Tayfa yöneticisi yok");
            WorkerType lumberjack = null, fiber = null;
            foreach (WorkerType t in crew.Types) { if (t.Id == "lumberjack") lumberjack = t; if (t.Id == "fiber") fiber = t; }
            Assert.IsNotNull(lumberjack);
            Assert.IsNotNull(fiber);
            Assert.IsNull(Find<UnlockPad>("Pad Lumberjack"), "Eski işçi karesi duruyor");

            CurrencyManager.Instance.Add(5000);
            Assert.IsFalse(crew.TryHire(lumberjack), "Bıçkıhane kapalıyken Oduncu tutuldu");
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            Assert.IsFalse(crew.TryHire(fiber), "Halat Atölyesi kapalıyken Lifçi tutuldu");

            // Oyuncu barakanın alanına girer → panel açılır; kartla işçi tutulur.
            UpgradeStation barracks = Find<UpgradeStation>("CrewBarracks");
            Assert.IsNotNull(barracks, "Tayfa Barakası yok");
            Teleport(barracks.transform.position);
            yield return new WaitForSeconds(0.8f);
            UpgradePanel panel = Find<UpgradePanel>("CrewPanel");
            Assert.IsTrue(panel.IsOpen, "Baraka paneli açılmadı");
            Shot("A_tayfa_barakasi_panel");
            CrewCard card = null;
            foreach (CrewCard c in panel.GetComponentsInChildren<CrewCard>(true)) if (c.Type == lumberjack) card = c;
            int coins = CurrencyManager.Instance.Coins;
            int price = crew.NextPrice(lumberjack);
            panel.Hire(card);
            Assert.AreEqual(1, crew.GetHired(lumberjack), "Oduncu tutulmadı");
            Assert.AreEqual(coins - price, CurrencyManager.Instance.Coins, "Fiyat düşülmedi");
            panel.Close();

            Transform door = barracks.transform.Find("Door");
            yield return WaitFor(() => Object.FindObjectsByType<LumberjackWorker>(FindObjectsSortMode.None).Length > 0, 3f);
            LumberjackWorker worker = Object.FindAnyObjectByType<LumberjackWorker>();
            Assert.IsNotNull(worker, "İşçi çıkmadı");
            Assert.Less(Vector3.Distance(worker.transform.position, door.position), 2.5f, "İşçi baraka kapısından çıkmadı");
            Teleport(barracks.transform.position + barracks.transform.forward * -2.5f + Vector3.right * 1.5f);
            yield return new WaitForSeconds(1.3f);
            Shot("A_isci_kapidan_cikti");
            yield return new WaitForSeconds(3f);
            Assert.Greater(Vector3.Distance(worker.transform.position, door.position), 2.5f, "İşçi işe koşmadı");

            AreaManager.Instance.Unlock(PadArea("Pad RopeWorkshop"));
            Assert.IsTrue(crew.TryHire(fiber), "Halat Atölyesi açıkken Lifçi tutulamadı");
        }

        // Makineler sadece işlerken hareket eder: halat çarkı ve dokuma tezgahı.
        [UnityTest]
        public IEnumerator Machines_MoveOnlyWhileProducing()
        {
            AreaManager.Instance.Unlock(PadArea("Pad RopeWorkshop"));
            AreaManager.Instance.Unlock(PadArea("Pad Loom"));
            yield return new WaitForSeconds(1.2f);
            Machine[] machines = Object.FindObjectsByType<Machine>(FindObjectsSortMode.None);
            Assert.AreEqual(2, machines.Length, "İki makine bekleniyordu");
            yield return new WaitForSeconds(0.5f);
            foreach (Machine m in machines) Assert.IsFalse(m.IsMoving, m.name + " boşken hareket ediyor");

            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None))
            {
                if (b.Type.Input.Id != "fiber") continue;
                for (int i = 0; i < b.Type.InputAmount * 2; i++) b.InputPile.TryAdd(b.Type.Input.Spawn(b.transform.position + Vector3.up));
            }
            yield return new WaitForSeconds(1.5f);
            foreach (Machine m in machines) Assert.IsTrue(m.IsMoving, m.name + " üretirken hareket etmiyor");
            Machine rope = machines[0].GetComponentInParent<ProcessingBuilding>().Type.Output.Id == "rope" ? machines[0] : machines[1];
            Machine loom = rope == machines[0] ? machines[1] : machines[0];
            Teleport(rope.transform.position + new Vector3(0f, 0f, -4.2f));
            yield return new WaitForSeconds(0.7f);
            Shot("A_halat_carki");
            Teleport(loom.transform.position + new Vector3(0f, 0f, -4.2f));
            yield return new WaitForSeconds(0.7f);
            Shot("A_dokuma_tezgahi");

            // Ham madde bitince durur.
            Teleport(new Vector3(0f, 0f, -6f));
            float end = Time.time + 25f;
            yield return WaitFor(() => { foreach (Machine m in machines) if (m.IsMoving) return false; return true; }, 25f);
            foreach (Machine m in machines) Assert.IsFalse(m.IsMoving, m.name + " iş bitince durmadı");
        }

        // Ayak izleri havuzdan gelir ve sınırı aşmaz; kumda iz çıkar.
        [UnityTest]
        public IEnumerator Trails_PooledAndLimited()
        {
            TrailPool pool = TrailPool.Instance;
            Assert.IsNotNull(pool, "İz havuzu yok");
            int max = FindAny<DevMenu>().Config.TrailMaxCount;
            // Kumsalda (Pazar'ın batısı) uzun yürüyüş.
            Vector3 start = new Vector3(-2f, 0f, -14.5f);
            while (IslandShape.Instance.SignedDistance(new Vector2(start.x, start.z)) > -1.9f) start.z += 0.2f;
            Teleport(start);
            Player.ScriptedInput = InputFor(Vector3.right);
            int most = 0;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                most = Mathf.Max(most, pool.ActiveCount);
                Assert.LessOrEqual(pool.ActiveCount, max, "İz sayısı sınırı aştı");
                yield return null;
            }
            Player.ScriptedInput = Vector2.zero;
            Assert.Greater(most, 2, "Kumda ayak izi çıkmadı");
            Assert.LessOrEqual(pool.transform.childCount, max + 1, "Havuz sınırdan fazla nesne üretti");
            Shot("A_ayak_izleri");
        }

        // Pazar: tezgahın altından/içinden geçilmez; satış alanı tezgahın önünde.
        [UnityTest]
        public IEnumerator Market_StallIsSolidAndSaleZoneInFront()
        {
            Market market = FindAny<Market>();
            Vector3 front = market.transform.position;
            Teleport(front);
            Player.ScriptedInput = InputFor(market.transform.forward);
            yield return new WaitForSeconds(1.5f);
            Player.ScriptedInput = Vector2.zero;
            float depth = Vector3.Dot(Player.transform.position - front, market.transform.forward);
            Assert.Less(depth, 1.7f, "Oyuncu Pazar tezgahının içine girdi");
            Teleport(front + market.transform.forward * -2.6f);
            yield return new WaitForSeconds(0.8f);
            Shot("A_liman_pazari");
        }
    }
}
