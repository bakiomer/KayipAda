// BuildingsTests.cs
// Bina kimliği ve üretim hattı turu: makineler yalnızca üretimde ve üretim hızıyla senkron çalışır (gösterilen faz
// binanın üretim döngüsünü izler, ürün tamamlanınca bir tur biter); kayalara girilemez (kenarından kayılır).
// Ekran görüntüleri Docs/Screenshots/<önek>_BN_*.png (sonra Docs/Screenshots/Binalar'a kopyalanır).

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace KayipAda.Tests
{
    public class BuildingsTests : GameTestBase
    {
        private static ProcessingBuilding Building(string output)
        {
            foreach (ProcessingBuilding b in Object.FindObjectsByType<ProcessingBuilding>(FindObjectsSortMode.None))
                if (b.Type.Output.Id == output) return b;
            return null;
        }

        private static void Feed(ProcessingBuilding b, int times)
        {
            for (int i = 0; i < b.Type.InputAmount * times; i++)
                if (!b.InputPile.TryAdd(b.Type.Input.Spawn(b.transform.position + Vector3.up))) break;
        }

        // Makineler üretimle senkron: çalışırken hareket eder, gösterilen faz üretim döngüsünün yakınında; ürün çıkar.
        [UnityTest]
        public IEnumerator Machines_SyncWithProduction()
        {
            DevCommands.UnlockAllRegions();
            DevCommands.UnlockAllBuildings();
            DevCommands.CompleteBridges();
            yield return new WaitForSeconds(6f);
            string[] outputs = { "plank", "brick", "rope", "cloth", "nail" };
            string[] shots = { "BN_bickihane", "BN_tas_atolyesi", "BN_halat_atolyesi", "BN_dokuma_tezgahi", "BN_demirci" };
            List<ProcessingBuilding> buildings = new List<ProcessingBuilding>();
            foreach (string o in outputs)
            {
                ProcessingBuilding b = Building(o);
                Assert.IsNotNull(b, o + " binası yok");
                Assert.IsNotNull(b.GetComponentInChildren<Machine>(), b.name + " binasında makine/üretim hattı yok");
                Feed(b, 4);
                buildings.Add(b);
            }
            yield return new WaitForSeconds(1.2f);
            for (int i = 0; i < buildings.Count; i++)
            {
                ProcessingBuilding b = buildings[i];
                Machine m = b.GetComponentInChildren<Machine>();
                Assert.IsTrue(b.IsWorking, b.name + " çalışmıyor");
                Assert.IsTrue(m.IsMoving, b.name + " makinesi üretirken hareket etmiyor");
                Assert.Less(Mathf.Abs(m.ShownCycle - b.Cycle), 0.3f, b.name + " makinesi üretimle senkron değil");
                Teleport(b.transform.position + new Vector3(0f, 0f, -4.6f));
                yield return new WaitForSeconds(0.9f);
                Shot(shots[i]);
            }
            // Ürün tamamlanınca döngü bir tur ilerler; makine son anla birlikte gelir.
            foreach (ProcessingBuilding b in buildings)
            {
                // Çıkış dolunca bina durur (istenen davranış): çıkışı boşaltıp yeniden besle.
                while (!b.OutputPile.IsEmpty) { ResourceItem it = b.OutputPile.RemoveTop(); if (it != null) Object.Destroy(it.gameObject); }
                Feed(b, 2);
                int before = b.ProducedCount;
                yield return WaitFor(() => b.ProducedCount > before, 15f);
                Assert.Greater(b.ProducedCount, before, b.name + " ürün çıkarmadı");
                Machine m = b.GetComponentInChildren<Machine>();
                Assert.Less(Mathf.Abs(m.ShownCycle - b.Cycle), 0.3f, b.name + " ürün çıkınca animasyon geride kaldı");
            }
        }

        // Kayalara girilemez: kayaya doğru yürüyen oyuncu kayanın gövdesine girmez.
        [UnityTest]
        public IEnumerator Rocks_CannotBeEntered()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Rocky"));
            yield return new WaitForSeconds(4f);
            int checkedRocks = 0;
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (!node.name.StartsWith("Node_Rock")) continue;
                NavObstacle o = node.GetComponent<NavObstacle>();
                Vector3 c = node.transform.position;
                Teleport(c + Vector3.back * 3f);
                Player.ScriptedInput = new Vector2(0f, 1f);
                yield return new WaitForSeconds(1.5f);
                Player.ScriptedInput = Vector2.zero;
                Vector2 p = new Vector2(Player.transform.position.x, Player.transform.position.z);
                Assert.IsFalse(o.Contains(p, -0.05f), node.name + " kayasının içine girildi");
                checkedRocks++;
            }
            Assert.GreaterOrEqual(checkedRocks, 3);
            Teleport(new Vector3(-6.5f, 0f, 7.5f));
            yield return new WaitForSeconds(1f);
            Shot("BN_kayalik");
        }
            // Yakınlık görünürlüğü: uzakta halkalar soluk ve küçük (gizli değil), Yükselt tabelası yok; yakında hepsi tam.
        [UnityTest]
        public IEnumerator Proximity_VisibilityByDistance()
        {
            AreaManager.Instance.Unlock(PadArea("Pad Sawmill"));
            yield return new WaitForSeconds(1f);
            ProcessingBuilding saw = Building("plank");
            InteractionVisibility vis = saw.GetComponent<InteractionVisibility>();
            Assert.IsNotNull(vis, "Bıçkıhane'de yakınlık görünürlüğü yok");
            CanvasGroup group = saw.GetComponentInChildren<CanvasGroup>();
            Assert.IsNotNull(group);
            Teleport(saw.transform.position + new Vector3(-4f, 0f, 15f));
            yield return new WaitForSeconds(1.5f);
            Assert.Less(vis.Nearness, 0.2f, "Uzakken tam görünüyor");
            Assert.IsFalse(vis.SignVisible, "Uzakken Yükselt tabelası görünüyor");
            Assert.Greater(group.alpha, 0.2f, "Uzakta halkalar tamamen gizlenmemeli");
            Assert.Less(group.alpha, 0.7f, "Uzakta halkalar soluk olmalı");
            Teleport(saw.transform.position + new Vector3(0f, 0f, -9f));
            yield return new WaitForSeconds(0.6f);
            Shot("BN_yakinlik_uzak");
            Teleport(saw.transform.position + new Vector3(-2.2f, 0f, -2.4f)); // köprü başının önizleme alanından uzakta
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(vis.Nearness, 0.9f, "Yakındayken tam görünmüyor");
            Assert.IsTrue(vis.SignVisible, "Yakındayken Yükselt tabelası görünmüyor");
            Assert.Greater(group.alpha, 0.95f);
            Shot("BN_yakinlik_yakin");
        }

        // Ambar burnun üstünde, gemiye yakın; bırak/al alanlarına ulaşılır; kıyı dalgaları burnun kenarında da var.
        [UnityTest]
        public IEnumerator Storage_OnCapeNearShip()
        {
            Storage storage = Storage.Instance;
            Assert.IsNotNull(storage);
            Vector2 p = new Vector2(storage.transform.position.x, storage.transform.position.z);
            Assert.Less(IslandShape.Instance.SignedDistance(p), -1f, "Ambar karada değil");
            // Burun: ambarın denize bakan arkası da kara (ana adaya bağlı çıkıntı).
            Vector3 back = storage.transform.position + storage.transform.forward * 3.4f;
            Assert.Less(IslandShape.Instance.SignedDistance(new Vector2(back.x, back.z)), 0.2f, "Ambarın arkası denizde");
            float toShip = Vector3.Distance(storage.transform.position, ShipBuilder.Instance.Delivery.transform.position);
            Assert.Less(toShip, 18f, "Ambar gemiye uzak");
            AreaManager.Instance.Unlock(storage.Area);
            yield return new WaitForSeconds(1.5f);
            yield return WaitFor(() => IslandNavMesh.IsUpToDate, 5f);
            Vector3 start = Player.transform.position;
            Assert.IsTrue(IslandNavMesh.HasCompletePath(start, FindAny<StorageDropZone>().transform.position), "Bırakma alanına ulaşılamıyor");
            Assert.IsTrue(IslandNavMesh.HasCompletePath(start, FindAny<StoragePickZone>().transform.position), "Alma alanına ulaşılamıyor");
            Assert.IsTrue(IslandNavMesh.HasCompletePath(FindAny<StoragePickZone>().transform.position, ShipBuilder.Instance.Delivery.transform.position), "Ambardan gemiye yol yok");
            DevCommands.FillStorage();
            Teleport(storage.transform.position - storage.transform.forward * 3.5f);
            yield return new WaitForSeconds(1.2f);
            Shot("BN_ambar");
        }
    }
}
